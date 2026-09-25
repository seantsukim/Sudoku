namespace SudokuWeb.Services;

/// <summary>
/// Generates random, solvable Sudoku puzzles that follow the standard rules:
///   * every row contains the digits 1-9 exactly once,
///   * every column contains the digits 1-9 exactly once,
///   * every 3x3 box contains the digits 1-9 exactly once.
///
/// Boards are represented as a 9x9 <c>int[,]</c> where 0 means "empty cell".
/// </summary>
public class SudokuGenerator
{
    // Size of the whole board (9x9) and of each inner box (3x3).
    public const int Size = 9;
    public const int BoxSize = 3;

    private readonly Random _random;

    /// <param name="random">Optional random source; pass a seeded one for repeatable boards (e.g. in tests).</param>
    public SudokuGenerator(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Creates a new puzzle together with its solution.
    /// </summary>
    /// <param name="cluesToKeep">
    /// How many filled-in cells the player starts with. Fewer clues = harder puzzle.
    /// 17 is the mathematical minimum for a uniquely solvable Sudoku; we clamp to that.
    /// </param>
    /// <returns>The puzzle (with 0s for blanks) and the full solution it was carved from.</returns>
    public (int[,] Puzzle, int[,] Solution) Generate(int cluesToKeep = 36)
    {
        cluesToKeep = Math.Clamp(cluesToKeep, 17, Size * Size);

        // Step 1: build a complete, valid board at random. This is the answer key.
        var solution = new int[Size, Size];
        FillBoard(solution);

        // Step 2: copy it and remove numbers while the puzzle stays uniquely solvable.
        var puzzle = (int[,])solution.Clone();
        RemoveCells(puzzle, cluesToKeep);

        return (puzzle, solution);
    }

    // ------------------------------------------------------------------
    // Step 1: Filling a complete board
    // ------------------------------------------------------------------

    /// <summary>
    /// Fills every empty cell using backtracking. The candidate digits are
    /// shuffled for each cell so every call produces a different board.
    /// </summary>
    /// <returns>True when the board was completely filled.</returns>
    private bool FillBoard(int[,] board)
    {
        // Find the next empty cell; if there is none the board is complete.
        if (!TryFindEmptyCell(board, out int row, out int col))
        {
            return true;
        }

        // Try the digits 1-9 in a random order.
        foreach (int digit in ShuffledDigits())
        {
            if (IsPlacementValid(board, row, col, digit))
            {
                board[row, col] = digit;

                // Recurse into the rest of the board; if it works out we're done.
                if (FillBoard(board))
                {
                    return true;
                }

                // Dead end further on: undo this choice and try the next digit (backtrack).
                board[row, col] = 0;
            }
        }

        // No digit fits here, so an earlier choice must change.
        return false;
    }

    // ------------------------------------------------------------------
    // Step 2: Removing cells to create the puzzle
    // ------------------------------------------------------------------

    /// <summary>
    /// Blanks out cells in random order. A removal is only kept if the puzzle
    /// still has exactly one solution, which guarantees the player's puzzle is
    /// solvable and that the stored solution is the one and only correct answer.
    /// </summary>
    private void RemoveCells(int[,] puzzle, int cluesToKeep)
    {
        // Visit every cell position (0..80) once in a random order.
        var positions = Enumerable.Range(0, Size * Size).OrderBy(_ => _random.Next()).ToList();
        int filledCells = Size * Size;

        foreach (int position in positions)
        {
            if (filledCells <= cluesToKeep)
            {
                break;
            }

            int row = position / Size;
            int col = position % Size;
            int backup = puzzle[row, col];

            // Tentatively clear the cell.
            puzzle[row, col] = 0;

            // If more than one solution now exists, the removal made the puzzle
            // ambiguous, so put the number back.
            if (CountSolutions(puzzle, limit: 2) != 1)
            {
                puzzle[row, col] = backup;
            }
            else
            {
                filledCells--;
            }
        }
    }

    /// <summary>
    /// Counts the solutions of a board with backtracking, stopping early once
    /// <paramref name="limit"/> is reached (we only need to know "one" vs "more than one").
    /// The board is restored to its original state before returning.
    /// </summary>
    public static int CountSolutions(int[,] board, int limit = 2)
    {
        if (!TryFindEmptyCell(board, out int row, out int col))
        {
            // No empty cells left: this is one complete solution.
            return 1;
        }

        int count = 0;
        for (int digit = 1; digit <= Size && count < limit; digit++)
        {
            if (IsPlacementValid(board, row, col, digit))
            {
                board[row, col] = digit;
                count += CountSolutions(board, limit - count);
                board[row, col] = 0;
            }
        }

        return count;
    }

    // ------------------------------------------------------------------
    // Sudoku rule helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Checks the three Sudoku rules for placing <paramref name="digit"/> at (row, col):
    /// the digit must not already appear in the same row, column, or 3x3 box.
    /// </summary>
    public static bool IsPlacementValid(int[,] board, int row, int col, int digit)
    {
        // Row and column check.
        for (int i = 0; i < Size; i++)
        {
            if (board[row, i] == digit || board[i, col] == digit)
            {
                return false;
            }
        }

        // 3x3 box check: find the top-left corner of the box that contains (row, col).
        int boxRow = row - row % BoxSize;
        int boxCol = col - col % BoxSize;
        for (int r = boxRow; r < boxRow + BoxSize; r++)
        {
            for (int c = boxCol; c < boxCol + BoxSize; c++)
            {
                if (board[r, c] == digit)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true if a completely filled board satisfies every row, column and box rule.
    /// </summary>
    public static bool IsValidSolution(int[,] board)
    {
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                int digit = board[row, col];
                if (digit < 1 || digit > Size)
                {
                    return false;
                }

                // Temporarily clear the cell so the rule check doesn't see the digit itself.
                board[row, col] = 0;
                bool ok = IsPlacementValid(board, row, col, digit);
                board[row, col] = digit;

                if (!ok)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>Finds the first empty (0) cell scanning row by row.</summary>
    private static bool TryFindEmptyCell(int[,] board, out int row, out int col)
    {
        for (row = 0; row < Size; row++)
        {
            for (col = 0; col < Size; col++)
            {
                if (board[row, col] == 0)
                {
                    return true;
                }
            }
        }

        row = col = -1;
        return false;
    }

    /// <summary>The digits 1-9 in random order (Fisher-Yates shuffle).</summary>
    private int[] ShuffledDigits()
    {
        int[] digits = Enumerable.Range(1, Size).ToArray();
        for (int i = digits.Length - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (digits[i], digits[j]) = (digits[j], digits[i]);
        }

        return digits;
    }
}
