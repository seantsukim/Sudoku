namespace SudokuWeb.Services;

/// <summary>Result of comparing a player's board to the stored solution.</summary>
/// <param name="Solved">True only when every cell is filled and matches the solution.</param>
/// <param name="EmptyCells">How many cells the player has not filled in yet.</param>
/// <param name="IncorrectCells">[row, col] pairs of filled cells that don't match the solution.</param>
public record CheckResult(bool Solved, int EmptyCells, List<int[]> IncorrectCells);

/// <summary>
/// Verifies a player's answer against the solution that was created
/// together with the puzzle.
/// </summary>
public static class SudokuChecker
{
    /// <summary>
    /// Compares the player's board with the solution cell by cell.
    /// A 0 in the player's board counts as an empty cell (not as a mistake).
    /// </summary>
    public static CheckResult Check(int[][] playerBoard, int[,] solution)
    {
        int emptyCells = 0;
        var incorrect = new List<int[]>();

        for (int row = 0; row < SudokuGenerator.Size; row++)
        {
            for (int col = 0; col < SudokuGenerator.Size; col++)
            {
                int value = playerBoard[row][col];
                if (value == 0)
                {
                    emptyCells++;
                }
                else if (value != solution[row, col])
                {
                    incorrect.Add([row, col]);
                }
            }
        }

        bool solved = emptyCells == 0 && incorrect.Count == 0;
        return new CheckResult(solved, emptyCells, incorrect);
    }

    /// <summary>Makes sure the submitted board is 9x9 and only contains 0-9.</summary>
    public static bool IsWellFormed(int[][]? board)
    {
        return board is { Length: SudokuGenerator.Size }
            && board.All(row => row is { Length: SudokuGenerator.Size } && row.All(v => v is >= 0 and <= 9));
    }

    /// <summary>Converts a 2D array into a jagged array, which serializes to JSON cleanly.</summary>
    public static int[][] ToJagged(int[,] board)
    {
        var result = new int[SudokuGenerator.Size][];
        for (int row = 0; row < SudokuGenerator.Size; row++)
        {
            result[row] = new int[SudokuGenerator.Size];
            for (int col = 0; col < SudokuGenerator.Size; col++)
            {
                result[row][col] = board[row, col];
            }
        }

        return result;
    }
}
