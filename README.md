# Sudoku

A randomly generated Sudoku game built with ASP.NET Core (.NET 8) and a plain HTML/CSS/JavaScript page.

## Run it

```bash
dotnet run --project src/SudokuWeb
```

Then open the URL shown in the console (e.g. http://localhost:5135).

## Run the tests

```bash
dotnet test
```

## How it works

| Part | File | What it does |
| --- | --- | --- |
| Generator | `src/SudokuWeb/Services/SudokuGenerator.cs` | Fills a complete valid 9x9 board with randomized backtracking, then removes numbers only while the puzzle keeps exactly one solution. |
| Game store | `src/SudokuWeb/Services/SudokuGameStore.cs` | Keeps each puzzle's solution on the server, keyed by a game id, so the answer is never sent to the browser. |
| Checker | `src/SudokuWeb/Services/SudokuChecker.cs` | Compares the player's board with the stored solution and reports wrong and empty cells. |
| API | `src/SudokuWeb/Program.cs` | `GET /api/sudoku/new?difficulty=easy\|medium\|hard` and `POST /api/sudoku/{gameId}/check`. |
| Webpage | `src/SudokuWeb/wwwroot/` | Draws the board. Starting numbers are fixed, and empty cells are 1-9 dropdowns. "Check Answer" highlights mistakes in red. |
