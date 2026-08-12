using NUnit.Framework;
using PatternGame.Grid;

namespace PatternGame.Tests.EditMode
{
    public static class GridPattern
    {
        public static GridMask Parse(params string[] rowsFromTop)
        {
            Assert.AreEqual(
                GridMask.Height,
                rowsFromTop.Length,
                "A grid pattern must describe every row of the board.");

            var mask = GridMask.Empty;

            for (int lineIndex = 0; lineIndex < rowsFromTop.Length; lineIndex++)
            {
                string line = rowsFromTop[lineIndex].Replace(" ", string.Empty);
                int row = GridMask.Height - 1 - lineIndex;

                Assert.AreEqual(
                    GridMask.Width,
                    line.Length,
                    $"Row {row} of the pattern must describe every column.");

                for (int column = 0; column < line.Length; column++)
                {
                    if (line[column] == '#')
                    {
                        mask = mask.WithCell(column, row);
                    }
                }
            }

            return mask;
        }

        public static bool IsConnected(GridMask shape)
        {
            return !shape.IsEmpty && FirstIsland(shape) == shape;
        }

        public static int CountIslands(GridMask shape)
        {
            var remaining = shape;
            int islands = 0;

            while (!remaining.IsEmpty)
            {
                remaining &= ~FirstIsland(remaining);
                islands++;
            }

            return islands;
        }

        static GridMask FirstIsland(GridMask shape)
        {
            if (shape.IsEmpty)
            {
                return GridMask.Empty;
            }

            var reached = GridMask.Empty;

            for (int index = 0; index < GridMask.CellCount; index++)
            {
                int column = GridMask.ColumnOf(index);
                int row = GridMask.RowOf(index);

                if (shape.IsOccupied(column, row))
                {
                    reached = reached.WithCell(column, row);
                    break;
                }
            }

            bool grew = true;

            while (grew)
            {
                grew = false;

                for (int index = 0; index < GridMask.CellCount; index++)
                {
                    int column = GridMask.ColumnOf(index);
                    int row = GridMask.RowOf(index);

                    if (!shape.IsOccupied(column, row) || reached.IsOccupied(column, row))
                    {
                        continue;
                    }

                    if (TouchesReachedCell(reached, column, row))
                    {
                        reached = reached.WithCell(column, row);
                        grew = true;
                    }
                }
            }

            return reached;
        }

        public static bool TryFindOffset(GridMask shape, GridMask target, out int column, out int row)
        {
            for (int candidateRow = 0; candidateRow < GridMask.Height; candidateRow++)
            {
                for (int candidateColumn = 0; candidateColumn < GridMask.Width; candidateColumn++)
                {
                    if (shape.TryTranslate(candidateColumn, candidateRow, out var moved) && moved == target)
                    {
                        column = candidateColumn;
                        row = candidateRow;
                        return true;
                    }
                }
            }

            column = 0;
            row = 0;
            return false;
        }

        public static int BoundingBoxArea(GridMask shape)
        {
            if (shape.IsEmpty)
            {
                return 0;
            }

            int minimumColumn = GridMask.Width - 1;
            int minimumRow = GridMask.Height - 1;
            int maximumColumn = 0;
            int maximumRow = 0;

            for (int index = 0; index < GridMask.CellCount; index++)
            {
                int column = GridMask.ColumnOf(index);
                int row = GridMask.RowOf(index);

                if (!shape.IsOccupied(column, row))
                {
                    continue;
                }

                if (column < minimumColumn) minimumColumn = column;
                if (column > maximumColumn) maximumColumn = column;
                if (row < minimumRow) minimumRow = row;
                if (row > maximumRow) maximumRow = row;
            }

            return (maximumColumn - minimumColumn + 1) * (maximumRow - minimumRow + 1);
        }

        static bool TouchesReachedCell(GridMask reached, int column, int row)
        {
            return (GridMask.IsValidCell(column - 1, row) && reached.IsOccupied(column - 1, row))
                || (GridMask.IsValidCell(column + 1, row) && reached.IsOccupied(column + 1, row))
                || (GridMask.IsValidCell(column, row - 1) && reached.IsOccupied(column, row - 1))
                || (GridMask.IsValidCell(column, row + 1) && reached.IsOccupied(column, row + 1));
        }
    }
}
