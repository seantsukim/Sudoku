using SudokuWeb.Services;

// ----------------------------------------------------------------------
// App setup: register services
// ----------------------------------------------------------------------
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();                  // in-memory storage for game solutions
builder.Services.AddSingleton<SudokuGameStore>();   // saves/looks up solutions by game id
builder.Services.AddSingleton<SudokuGenerator>();   // builds random, uniquely solvable puzzles

var app = builder.Build();

// Serve the webpage (wwwroot/index.html, app.js, styles.css).
app.UseDefaultFiles();
app.UseStaticFiles();

// ----------------------------------------------------------------------
// API endpoints used by the webpage
// ----------------------------------------------------------------------

// GET /api/sudoku/new?difficulty=easy|medium|hard
// Generates a new puzzle, stores its solution on the server, and returns
// only the puzzle (0 = empty cell) plus the id needed to check it later.
app.MapGet("/api/sudoku/new", (string? difficulty, SudokuGenerator generator, SudokuGameStore store) =>
{
    // Number of starting clues for each difficulty level.
    int clues = difficulty?.ToLowerInvariant() switch
    {
        "easy" => 40,
        "hard" => 28,
        _ => 34, // medium (default)
    };

    var (puzzle, solution) = generator.Generate(clues);
    string gameId = store.Save(solution);

    return Results.Ok(new NewGameResponse(gameId, SudokuChecker.ToJagged(puzzle)));
});

// POST /api/sudoku/{gameId}/check   body: { "board": [[...9 numbers...], ...9 rows] }
// Compares the player's board with the solution saved when the puzzle was created.
app.MapPost("/api/sudoku/{gameId}/check", (string gameId, CheckRequest request, SudokuGameStore store) =>
{
    if (!SudokuChecker.IsWellFormed(request.Board))
    {
        return Results.BadRequest(new { error = "Board must be 9 rows of 9 numbers between 0 and 9." });
    }

    if (!store.TryGetSolution(gameId, out var solution))
    {
        return Results.NotFound(new { error = "Game not found or expired. Please start a new game." });
    }

    return Results.Ok(SudokuChecker.Check(request.Board!, solution));
});

app.Run();

// ----------------------------------------------------------------------
// Request/response shapes (serialized as camelCase JSON)
// ----------------------------------------------------------------------
public record NewGameResponse(string GameId, int[][] Puzzle);
public record CheckRequest(int[][]? Board);

// Exposes Program to the test project (for WebApplicationFactory-style tests if added later).
public partial class Program;
