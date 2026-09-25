using SudokuWeb.Services;

namespace SudokuWeb.Tests;

/// <summary>Tests for the search-as-you-type video index.</summary>
public class VideoSearchIndexTests
{
    // A small, fixed catalog so results are predictable.
    private static readonly VideoInfo[] Catalog =
    [
        new("aaaaaaaaaaa", "lofi hip hop radio - beats to relax/study to", "Lofi Girl", ["lofi", "study"]),
        new("bbbbbbbbbbb", "synthwave radio - beats to chill/game to", "Lofi Girl", ["synthwave", "gaming"]),
        new("ccccccccccc", "Bohemian Rhapsody", "Queen", ["rock", "70s"]),
        new("ddddddddddd", "Don't Stop Believin'", "Journey", ["rock", "80s"]),
        new("eeeeeeeeeee", "Rock Lobster", "The B-52's", ["new wave", "80s"]),
    ];

    private static readonly VideoSearchIndex Index = new(Catalog);

    private static string[] Titles(string query, int limit = 8) =>
        Index.Search(query, limit).Select(v => v.Title).ToArray();

    [Fact]
    public void Search_MatchesPrefixOfAWord()
    {
        // "boh" is the start of "Bohemian", typed partway through.
        Assert.Equal(["Bohemian Rhapsody"], Titles("boh"));
    }

    [Fact]
    public void Search_IsCaseInsensitive()
    {
        Assert.Equal(Titles("queen"), Titles("QUEEN"));
        Assert.Single(Titles("QuEeN"));
    }

    [Fact]
    public void Search_AllWordsMustMatch()
    {
        // Both lofi-channel videos say "radio", but only one mentions "study".
        Assert.Equal(2, Titles("radio").Length);
        Assert.Equal(["lofi hip hop radio - beats to relax/study to"], Titles("radio stu"));
    }

    [Fact]
    public void Search_MatchesChannelAndTags()
    {
        Assert.Equal(["Don't Stop Believin'"], Titles("journey"));           // channel
        Assert.Equal(2, Titles("80s").Length);                               // tag
    }

    [Fact]
    public void Search_IgnoresApostrophesAndPunctuation()
    {
        Assert.Equal(["Don't Stop Believin'"], Titles("dont stop"));
        Assert.Equal(["Don't Stop Believin'"], Titles("don't"));
    }

    [Fact]
    public void Search_TitleMatchRanksAboveTagMatch()
    {
        // "Rock Lobster" has "rock" in its title; the other two only have it as a tag.
        string[] titles = Titles("rock");
        Assert.Equal(3, titles.Length);
        Assert.Equal("Rock Lobster", titles[0]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData("zzzz")]
    public void Search_NoMatches_ReturnsEmpty(string query)
    {
        Assert.Empty(Titles(query));
    }

    [Fact]
    public void Search_RespectsLimit()
    {
        Assert.Single(Titles("radio", limit: 1));
    }

    [Fact]
    public void Tokenize_SplitsAndLowercases()
    {
        Assert.Equal(["dont", "stop", "believin"], VideoSearchIndex.Tokenize("Don't Stop Believin'"));
        Assert.Equal(["relax", "study", "to"], VideoSearchIndex.Tokenize("relax/study to"));
    }

    // The binary-search index must find exactly the same videos as checking every video one by one.
    [Fact]
    public void Search_MatchesBruteForce_OnRandomData()
    {
        var random = new Random(1234);
        string RandomWord() => new(Enumerable.Range(0, random.Next(1, 6)).Select(_ => (char)('a' + random.Next(4))).ToArray());

        var videos = Enumerable.Range(0, 300)
            .Select(i => new VideoInfo(i.ToString("D11"),
                string.Join(' ', Enumerable.Range(0, 3).Select(_ => RandomWord())),
                RandomWord(),
                [RandomWord()]))
            .ToList();
        var index = new VideoSearchIndex(videos);

        for (int trial = 0; trial < 200; trial++)
        {
            string query = trial % 2 == 0 ? RandomWord() : $"{RandomWord()} {RandomWord()}";
            var terms = VideoSearchIndex.Tokenize(query).ToList();

            // Brute force: a video matches if every term is the start of one of its words.
            var expected = videos
                .Where(v =>
                {
                    var words = VideoSearchIndex.Tokenize($"{v.Title} {v.Channel} {string.Join(' ', v.Tags)}").ToList();
                    return terms.All(t => words.Any(w => w.StartsWith(t, StringComparison.Ordinal)));
                })
                .Select(v => v.Id)
                .OrderBy(id => id);

            var actual = index.Search(query, limit: int.MaxValue).Select(v => v.Id).OrderBy(id => id);

            Assert.Equal(expected, actual);
        }
    }

    // The shipped video list loads, and every entry has a unique, well-formed YouTube id.
    [Fact]
    public void BundledCatalog_LoadsAndIsSearchable()
    {
        var index = VideoSearchIndex.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Data", "videos.json"));

        Assert.True(index.Count > 20);
        Assert.NotEmpty(index.Search("lofi"));

        var all = index.Search("music video", limit: 1000).Concat(index.Search("lofi", 1000)).ToList();
        Assert.All(all, v => Assert.Matches("^[A-Za-z0-9_-]{11}$", v.Id));
    }
}
