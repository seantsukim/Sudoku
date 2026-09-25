# Sudoku

A randomly generated Sudoku game built with ASP.NET Core (.NET 8) and a plain HTML/CSS/JavaScript page.

## Run it

```bash
dotnet run --project src/SudokuWeb
```

Your default browser opens the game automatically at http://localhost:5135 once the server is ready.
If no browser opens (for example, on a machine without a desktop), open that address yourself.
To start without opening a browser: `dotnet run --project src/SudokuWeb -- --OpenBrowser false`.

Keep the terminal open while you play, and press `Ctrl+C` to stop the app.

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
| Browser launcher | `src/SudokuWeb/Services/BrowserLauncher.cs` | Opens the game in your default browser when you start it with `dotnet run` (controlled by `OpenBrowser` in `appsettings.Development.json`). |
| YouTube player | `src/SudokuWeb/wwwroot/video.js` | A panel in the top-right corner. Paste a YouTube link and press Play to watch or listen while you solve. The last video is remembered. |
| Webpage | `src/SudokuWeb/wwwroot/` | Draws the board. Starting numbers are fixed, and empty cells are 1-9 dropdowns. Changing the difficulty starts a new board at that level. "Check Answer" highlights mistakes in red. |
