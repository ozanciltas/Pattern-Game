using System;
using NUnit.Framework;
using PatternGame.Grid;
using static PatternGame.Tests.EditMode.GridPattern;

namespace PatternGame.Tests.EditMode
{
    [TestFixture]
    public sealed class GridMaskTests
    {
        [Test]
        public void Empty_ContainsNoCells()
        {
            Assert.IsTrue(GridMask.Empty.IsEmpty);
            Assert.AreEqual(0, GridMask.Empty.Count);
        }

        [Test]
        public void Full_ContainsEveryCell()
        {
            Assert.IsTrue(GridMask.Full.IsFull);
            Assert.AreEqual(GridMask.CellCount, GridMask.Full.Count);
        }

        [Test]
        public void Constructor_DiscardsBitsOutsideTheBoard()
        {
            var mask = new GridMask(uint.MaxValue);

            Assert.AreEqual(GridMask.Full, mask);
            Assert.AreEqual(GridMask.CellCount, mask.Count);
        }

        [Test]
        public void WithCell_MarksOnlyThatCell()
        {
            var mask = GridMask.Empty.WithCell(2, 3);

            Assert.AreEqual(1, mask.Count);
            Assert.IsTrue(mask.IsOccupied(2, 3));
            Assert.IsFalse(mask.IsOccupied(3, 2));
        }

        [Test]
        public void WithCell_IsIdempotent()
        {
            var once = GridMask.Empty.WithCell(1, 1);
            var twice = once.WithCell(1, 1);

            Assert.AreEqual(once, twice);
        }

        [Test]
        public void WithoutCell_ClearsOnlyThatCell()
        {
            var mask = GridMask.Empty.WithCell(0, 0).WithCell(1, 0).WithoutCell(0, 0);

            Assert.AreEqual(1, mask.Count);
            Assert.IsFalse(mask.IsOccupied(0, 0));
            Assert.IsTrue(mask.IsOccupied(1, 0));
        }

        [Test]
        public void Complement_OfFullIsEmpty()
        {
            Assert.AreEqual(GridMask.Empty, GridMask.Full.Complement);
            Assert.AreEqual(GridMask.Full, GridMask.Empty.Complement);
        }

        [Test]
        public void Complement_NeverOverlapsAndAlwaysCompletesTheBoard()
        {
            uint failingBits = 0u;
            bool failed = false;

            for (uint bits = 0u; bits < (1u << GridMask.CellCount); bits++)
            {
                var mask = new GridMask(bits);
                var complement = mask.Complement;

                if (mask.Overlaps(complement) || !(mask | complement).IsFull ||
                    mask.Count + complement.Count != GridMask.CellCount)
                {
                    failingBits = bits;
                    failed = true;
                    break;
                }
            }

            Assert.IsFalse(failed, $"Complement invariant broken for bits 0x{failingBits:X5}.");
        }

        [Test]
        public void Count_MatchesNaiveBitCountForEveryPossibleBoard()
        {
            uint failingBits = 0u;
            int expectedCount = 0;
            int actualCount = 0;
            bool failed = false;

            for (uint bits = 0u; bits < (1u << GridMask.CellCount); bits++)
            {
                int naiveCount = 0;

                for (int index = 0; index < GridMask.CellCount; index++)
                {
                    if ((bits & (1u << index)) != 0u)
                    {
                        naiveCount++;
                    }
                }

                int maskCount = new GridMask(bits).Count;

                if (maskCount != naiveCount)
                {
                    failingBits = bits;
                    expectedCount = naiveCount;
                    actualCount = maskCount;
                    failed = true;
                    break;
                }
            }

            Assert.IsFalse(
                failed,
                $"PopCount mismatch for bits 0x{failingBits:X5}: expected {expectedCount} but was {actualCount}.");
        }

        [Test]
        public void PerfectFit_Holds_WhenPieceExactlyFillsTheHole()
        {
            var wall = Parse(
                "####",
                "####",
                "#..#",
                "#..#",
                "####");

            var piece = wall.Complement;

            Assert.IsFalse(wall.Overlaps(piece));
            Assert.IsTrue((wall | piece).IsFull);
        }

        [Test]
        public void PerfectFit_Fails_WhenPieceIsShiftedByOneCell()
        {
            var wall = Parse(
                "####",
                "####",
                "#..#",
                "#..#",
                "####");

            Assert.IsTrue(wall.Complement.TryTranslate(0, 1, out var shiftedPiece));
            Assert.IsTrue(wall.Overlaps(shiftedPiece));
            Assert.IsFalse((wall | shiftedPiece).IsFull);
        }

        [Test]
        public void Contains_DetectsSubsets()
        {
            var outer = Parse(
                "....",
                "....",
                "....",
                "##..",
                "##..");

            var inner = GridMask.Empty.WithCell(0, 0);

            Assert.IsTrue(outer.Contains(inner));
            Assert.IsFalse(inner.Contains(outer));
            Assert.IsTrue(outer.Contains(outer));
        }

        [Test]
        public void Overlaps_IsFalseForDisjointMasks()
        {
            var left = GridMask.Empty.WithCell(0, 0);
            var right = GridMask.Empty.WithCell(1, 0);

            Assert.IsFalse(left.Overlaps(right));
            Assert.IsTrue(left.Overlaps(left));
        }

        [Test]
        public void TryTranslate_MovesEveryCellByTheGivenOffset()
        {
            var shape = Parse(
                "....",
                "....",
                "....",
                "##..",
                ".#..");

            Assert.IsTrue(shape.TryTranslate(1, 1, out var moved));

            var expected = Parse(
                "....",
                "....",
                ".##.",
                "..#.",
                "....");

            Assert.AreEqual(expected, moved, $"Expected:\n{expected}\nActual:\n{moved}");
        }

        [Test]
        public void TryTranslate_RejectsHorizontalWrapAround()
        {
            var rightEdgeCell = GridMask.Empty.WithCell(GridMask.Width - 1, 0);

            Assert.IsFalse(rightEdgeCell.TryTranslate(1, 0, out _));
        }

        [Test]
        public void TryTranslate_RejectsMovesThatLeaveTheBoard()
        {
            var shape = GridMask.Empty.WithCell(0, 0);

            Assert.IsFalse(shape.TryTranslate(-1, 0, out _));
            Assert.IsFalse(shape.TryTranslate(0, -1, out _));
            Assert.IsFalse(shape.TryTranslate(GridMask.Width, 0, out _));
            Assert.IsFalse(shape.TryTranslate(0, GridMask.Height, out _));
        }

        [Test]
        public void TryTranslate_PreservesCellCountAndRoundTrips()
        {
            var shape = Parse(
                "....",
                "....",
                "#...",
                "##..",
                ".#..");

            Assert.IsTrue(shape.TryTranslate(2, 1, out var moved));
            Assert.AreEqual(shape.Count, moved.Count);

            Assert.IsTrue(moved.TryTranslate(-2, -1, out var restored));
            Assert.AreEqual(shape, restored);
        }

        [Test]
        public void TryTranslate_ByZeroReturnsTheSameMask()
        {
            var shape = Parse(
                "....",
                "....",
                "....",
                ".##.",
                "..#.");

            Assert.IsTrue(shape.TryTranslate(0, 0, out var moved));
            Assert.AreEqual(shape, moved);
        }

        [Test]
        public void ToIndex_AndBackRoundTripsForEveryCell()
        {
            for (int row = 0; row < GridMask.Height; row++)
            {
                for (int column = 0; column < GridMask.Width; column++)
                {
                    int index = GridMask.ToIndex(column, row);

                    Assert.AreEqual(column, GridMask.ColumnOf(index));
                    Assert.AreEqual(row, GridMask.RowOf(index));
                }
            }
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(GridMask.Width, 0)]
        [TestCase(0, GridMask.Height)]
        public void ToIndex_ThrowsForCellsOutsideTheBoard(int column, int row)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GridMask.ToIndex(column, row));
        }

        [Test]
        public void IsOccupied_ThrowsForCellsOutsideTheBoard()
        {
            var mask = GridMask.Empty;

            Assert.Throws<ArgumentOutOfRangeException>(() => mask.IsOccupied(GridMask.Width, 0));
        }

        [Test]
        public void Equality_IsDrivenByOccupiedCells()
        {
            var left = GridMask.Empty.WithCell(1, 2);
            var right = GridMask.Empty.WithCell(1, 2);
            var other = GridMask.Empty.WithCell(2, 1);

            Assert.IsTrue(left == right);
            Assert.IsFalse(left != right);
            Assert.IsTrue(left.Equals(right));
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
            Assert.IsTrue(left != other);
        }

        [Test]
        public void TryGetBounds_FailsForAnEmptyMask()
        {
            Assert.IsFalse(GridMask.Empty.TryGetBounds(out _, out _, out _, out _));
        }

        [Test]
        public void TryGetBounds_CoversTheWholeBoardWhenFull()
        {
            Assert.IsTrue(GridMask.Full.TryGetBounds(out int minColumn, out int minRow, out int maxColumn, out int maxRow));

            Assert.AreEqual(0, minColumn);
            Assert.AreEqual(0, minRow);
            Assert.AreEqual(GridMask.Width - 1, maxColumn);
            Assert.AreEqual(GridMask.Height - 1, maxRow);
        }

        [Test]
        public void TryGetBounds_WrapsTheOccupiedCellsTightly()
        {
            var shape = Parse(
                "....",
                "..#.",
                ".##.",
                ".#..",
                "....");

            Assert.IsTrue(shape.TryGetBounds(out int minColumn, out int minRow, out int maxColumn, out int maxRow));

            Assert.AreEqual(1, minColumn);
            Assert.AreEqual(1, minRow);
            Assert.AreEqual(2, maxColumn);
            Assert.AreEqual(3, maxRow);
        }

        [Test]
        public void TryGetBounds_ReportsASingleCellAsAOneByOneBox()
        {
            var shape = GridMask.Empty.WithCell(2, 3);

            Assert.IsTrue(shape.TryGetBounds(out int minColumn, out int minRow, out int maxColumn, out int maxRow));

            Assert.AreEqual(2, minColumn);
            Assert.AreEqual(2, maxColumn);
            Assert.AreEqual(3, minRow);
            Assert.AreEqual(3, maxRow);
        }

        [Test]
        public void ToString_RendersTopRowFirst()
        {
            var mask = GridMask.Empty.WithCell(0, 0).WithCell(GridMask.Width - 1, GridMask.Height - 1);

            const string expected =
                ". . . #\n" +
                ". . . .\n" +
                ". . . .\n" +
                ". . . .\n" +
                "# . . .";

            Assert.AreEqual(expected, mask.ToString());
        }
    }
}
