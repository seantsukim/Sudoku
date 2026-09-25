using System.Text;
using System.Text.Json;

namespace SudokuWeb.Services;

/// <summary>One video the player can search for (loaded from Data/videos.json).</summary>
/// <param name="Id">The 11-character YouTube video id used to embed the video.</param>
public record VideoInfo(string Id, string Title, string Channel, string[] Tags);

/// <summary>
/// Search-as-you-type index over the video list.
///
/// How it works (N = total number of words across all videos):
///   * Build, once at startup — O(N log N):
///       every word of every title, channel and tag is put into one array of
///       (word, video) entries, which is then sorted alphabetically.
///   * Search, on every keystroke — O(log N + M) per typed word (M = matches):
///       because the array is sorted, all words that start with what the user
///       typed sit next to each other. Binary search jumps to the first one,
///       then we walk forward until the prefix stops matching.
///   * Ranking — O(K log K) for the K matching videos:
///       results are sorted by relevance score, then by title.
/// </summary>
public class VideoSearchIndex
{
    // How much a match counts towards a video's relevance score.
    // Matching the title beats matching the channel or a tag, and a whole word beats a prefix.
    private const int TitleWordScore = 6;
    private const int TitlePrefixScore = 4;
    private const int OtherWordScore = 3;
    private const int OtherPrefixScore = 2;

    /// <summary>A single word in the index, pointing back at the video it came from.</summary>
    private readonly record struct Entry(string Word, int VideoIndex, bool InTitle);

    private readonly List<VideoInfo> _videos;
    private readonly Entry[] _entries; // sorted by Word (ordinal), built once

    public VideoSearchIndex(IEnumerable<VideoInfo> videos)
    {
        _videos = videos.ToList();

        // ---- Step 1: collect every (word, video) pair ----
        var entries = new List<Entry>();
        for (int i = 0; i < _videos.Count; i++)
        {
            VideoInfo video = _videos[i];
            var titleWords = Tokenize(video.Title).ToHashSet();
            var otherWords = Tokenize(video.Channel)
                .Concat(video.Tags.SelectMany(Tokenize))
                .Where(w => !titleWords.Contains(w))  // a word already in the title is counted there
                .ToHashSet();

            entries.AddRange(titleWords.Select(w => new Entry(w, i, InTitle: true)));
            entries.AddRange(otherWords.Select(w => new Entry(w, i, InTitle: false)));
        }

        // ---- Step 2: sort all words alphabetically — the O(N log N) step ----
        // Ordinal (plain character code) order keeps every word sharing a prefix in one block.
        _entries = entries.ToArray();
        Array.Sort(_entries, (a, b) => string.CompareOrdinal(a.Word, b.Word));
    }

    /// <summary>Number of videos in the index.</summary>
    public int Count => _videos.Count;

    /// <summary>Reads the video list from a JSON file and builds the index.</summary>
    public static VideoSearchIndex LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        var videos = JsonSerializer.Deserialize<List<VideoInfo>>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                     ?? [];
        return new VideoSearchIndex(videos);
    }

    /// <summary>
    /// Finds the videos matching what the user has typed so far.
    /// Every typed word must match the start of some word in the video's title,
    /// channel or tags ("lof stud" finds "lofi hip hop radio - beats to relax/study to").
    /// </summary>
    public List<VideoInfo> Search(string? query, int limit = 8)
    {
        var terms = Tokenize(query ?? "").Distinct().ToList();
        if (terms.Count == 0 || limit <= 0)
        {
            return [];
        }

        // Running total score per video. Starts as "all videos matching the first word",
        // then shrinks to videos that also match each following word (AND search).
        Dictionary<int, int>? scores = null;

        foreach (string term in terms)
        {
            Dictionary<int, int> termScores = FindPrefixMatches(term);

            if (scores is null)
            {
                scores = termScores;
            }
            else
            {
                // Keep only videos matching every word so far, adding up their scores.
                scores = scores
                    .Where(kv => termScores.ContainsKey(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value + termScores[kv.Key]);
            }

            if (scores.Count == 0)
            {
                return []; // no video matches all the words; stop early
            }
        }

        // ---- Rank: best score first, then alphabetical by title ----
        return scores!
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => _videos[kv.Key].Title, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(kv => _videos[kv.Key])
            .ToList();
    }

    /// <summary>
    /// Returns every video with a word that starts with <paramref name="prefix"/>,
    /// with the best score that video got for this prefix.
    /// </summary>
    private Dictionary<int, int> FindPrefixMatches(string prefix)
    {
        var best = new Dictionary<int, int>();

        // Binary search jumps to the first word >= prefix: O(log N).
        // From there, all words that start with the prefix follow one after another.
        for (int i = LowerBound(prefix); i < _entries.Length && _entries[i].Word.StartsWith(prefix, StringComparison.Ordinal); i++)
        {
            Entry entry = _entries[i];
            bool wholeWord = entry.Word.Length == prefix.Length;
            int score = entry.InTitle
                ? (wholeWord ? TitleWordScore : TitlePrefixScore)
                : (wholeWord ? OtherWordScore : OtherPrefixScore);

            if (!best.TryGetValue(entry.VideoIndex, out int existing) || score > existing)
            {
                best[entry.VideoIndex] = score;
            }
        }

        return best;
    }

    /// <summary>
    /// Classic binary search: the index of the first entry whose word is
    /// alphabetically >= <paramref name="target"/> (or the array length if none).
    /// </summary>
    private int LowerBound(string target)
    {
        int low = 0;
        int high = _entries.Length; // search window is [low, high)

        while (low < high)
        {
            int mid = low + (high - low) / 2;
            if (string.CompareOrdinal(_entries[mid].Word, target) < 0)
            {
                low = mid + 1;   // mid is too small: answer is to the right
            }
            else
            {
                high = mid;      // mid might be the answer: keep it in the window
            }
        }

        return low;
    }

    /// <summary>
    /// Splits text into lowercase search words. Apostrophes are dropped so that
    /// "Don't" becomes "dont"; any other symbol or space separates words.
    /// </summary>
    public static IEnumerable<string> Tokenize(string text)
    {
        var word = new StringBuilder();
        foreach (char ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                word.Append(char.ToLowerInvariant(ch));
            }
            else if (ch is '\'' or '’')
            {
                continue; // skip apostrophes inside words
            }
            else if (word.Length > 0)
            {
                yield return word.ToString();
                word.Clear();
            }
        }

        if (word.Length > 0)
        {
            yield return word.ToString();
        }
    }
}
