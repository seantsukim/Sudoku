using Microsoft.Extensions.Caching.Memory;

namespace SudokuWeb.Services;

/// <summary>
/// Keeps each generated puzzle's solution on the server, keyed by a game id.
/// The browser only ever receives the puzzle (with blanks), so the answer key
/// can't be read from the page. When the player clicks "Check", their board is
/// compared against the solution saved here at generation time.
/// </summary>
public class SudokuGameStore
{
    // Games are dropped after this much inactivity so memory doesn't grow forever.
    private static readonly TimeSpan GameLifetime = TimeSpan.FromHours(6);

    private readonly IMemoryCache _cache;

    public SudokuGameStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>Saves a solution and returns the new game's id.</summary>
    public string Save(int[,] solution)
    {
        string id = Guid.NewGuid().ToString("N");
        _cache.Set(CacheKey(id), solution, new MemoryCacheEntryOptions { SlidingExpiration = GameLifetime });
        return id;
    }

    /// <summary>Looks up the solution for a game id; false if unknown or expired.</summary>
    public bool TryGetSolution(string id, out int[,] solution)
    {
        if (_cache.TryGetValue(CacheKey(id), out int[,]? found) && found is not null)
        {
            solution = found;
            return true;
        }

        solution = new int[0, 0];
        return false;
    }

    private static string CacheKey(string id) => $"sudoku:{id}";
}
