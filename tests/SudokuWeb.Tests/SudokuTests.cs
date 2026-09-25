using SudokuWeb.Services;

namespace SudokuWeb.Tests;

/// <summary>Tests for puzzle generation and answer checking.</summary>
public class SudokuTests
{
    // Generated solutions must obey the row, column and 3x3 box rules.
    [Fact]
    public void Generate_ProducesValidSolution()
    {
        var (_, solution) = new SudokuGenerator().Generate();

        Assert.True(SudokuGenerator.IsValidSolution(solution));
    }

    // Every clue in the puzzle must match the solution it was carved from.
    [Fact]
    public void Generate_PuzzleCluesMatchSolution()
    {
        var (puzzle, solution) = new SudokuGenerator().Generate();

        for (int r = 0; r < 9; r++)
        for (int c = 0; c < 9; c++)
        {
            Assert.True(puzzle[r, c] == 0 || puzzle[r, c] == solution[r, c]);
        }
    }

    // The puzzle must have exactly one solution, so the stored answer is the only correct one.
    [Theory]
    [InlineData(40)]
    [InlineData(34)]
    [InlineData(28)]
    public void Generate_PuzzleHasUniqueSolution(int clues)
    {
        var (puzzle, _) = new SudokuGenerator().Generate(clues);

        Assert.Equal(1, SudokuGenerator.CountSolutions(puzzle, limit: 2));
    }

    // Two boards in a row should (practically always) differ.
    [Fact]
    public void Generate_IsRandom()
    {
        var generator = new SudokuGenerator();
        var first = SudokuChecker.ToJagged(generator.Generate().Solution);
        var second = SudokuChecker.ToJagged(generator.Generate().Solution);

        Assert.NotEqual(first.SelectMany(x => x), second.SelectMany(x => x));
    }

    [Fact]
    public void Check_CorrectBoard_IsSolved()
    {
        var (_, solution) = new SudokuGenerator().Generate();

        var result = SudokuChecker.Check(SudokuChecker.ToJagged(solution), solution);

        Assert.True(result.Solved);
        Assert.Empty(result.IncorrectCells);
        Assert.Equal(0, result.EmptyCells);
    }

    [Fact]
    public void Check_ReportsWrongAndEmptyCells()
    {
        var (_, solution) = new SudokuGenerator().Generate();
        var board = SudokuChecker.ToJagged(solution);
        board[0][0] = solution[0, 0] % 9 + 1; // a different digit -> wrong
        board[4][4] = 0;                      // left empty

        var result = SudokuChecker.Check(board, solution);

        Assert.False(result.Solved);
        Assert.Equal(1, result.EmptyCells);
        Assert.Single(result.IncorrectCells);
        Assert.Equal(new[] { 0, 0 }, result.IncorrectCells[0]);
    }

    [Fact]
    public void IsWellFormed_RejectsBadInput()
    {
        Assert.False(SudokuChecker.IsWellFormed(null));
        Assert.False(SudokuChecker.IsWellFormed(new int[8][]));
        var board = Enumerable.Range(0, 9).Select(_ => new int[9]).ToArray();
        Assert.True(SudokuChecker.IsWellFormed(board));
        board[3][3] = 10;
        Assert.False(SudokuChecker.IsWellFormed(board));
    }
}
