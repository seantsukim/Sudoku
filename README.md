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
| YouTube player | `src/SudokuWeb/wwwroot/video.js` | A panel in the top-right corner. Type in its search bar and matching videos appear as you type; click one (or use the arrow keys and Enter) to play it while you solve. The last video is remembered. |
| Video search | `src/SudokuWeb/Services/VideoSearchIndex.cs`, `src/SudokuWeb/Data/videos.json` | Searches the built-in video list. At startup every word from the titles, channels and tags is sorted once (O(N log N)); each keystroke then finds matching words with binary search (O(log N) + matches). Endpoint: `GET /api/videos/search?q=...`. |
| Webpage | `src/SudokuWeb/wwwroot/` | Draws the board. Starting numbers are fixed, and empty cells are 1-9 dropdowns. "Check Answer" highlights mistakes in red. |

## Adding videos to the search

The player searches the list in `src/SudokuWeb/Data/videos.json`. To add a video, add an entry with its YouTube id
(the part after `v=` in a YouTube link), a title, the channel name, and a few tags to search by, then restart the app:

```json
{ "id": "jfKfPfyJRdk", "title": "lofi hip hop radio - beats to relax/study to", "channel": "Lofi Girl", "tags": ["lofi", "study"] }
```

Some video owners don't allow their videos to be played on other websites; those show an error in the player instead.
