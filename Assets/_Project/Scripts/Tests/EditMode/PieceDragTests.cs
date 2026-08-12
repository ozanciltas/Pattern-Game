using System;
using NUnit.Framework;
using PatternGame.Gameplay;
using PatternGame.Gameplay.Levels;
using PatternGame.Grid;

namespace PatternGame.Tests.EditMode
{
    [TestFixture]
    public sealed class PieceDragTests
    {
        Playfield playfield;
        PieceDrag drag;

        [SetUp]
        public void SetUp()
        {
            playfield = new Playfield();
            drag = new PieceDrag(playfield);
        }

        [Test]
        public void Constructor_RejectsAMissingPlayfield()
        {
            Assert.Throws<ArgumentNullException>(() => new PieceDrag(null));
        }

        [Test]
        public void TryBegin_FailsWhenNoLevelIsLoaded()
        {
            Assert.IsFalse(drag.TryBegin(0, 0));
            Assert.IsFalse(drag.IsActive);
        }

        [Test]
        public void TryBegin_FailsWhenThePressMissesThePiece()
        {
            LoadHorizontalPairAt(0, 0);

            Assert.IsFalse(drag.TryBegin(3, 4));
            Assert.IsFalse(drag.IsActive);
        }

        [Test]
        public void TryBegin_FailsForCellsOutsideTheBoard()
        {
            LoadHorizontalPairAt(0, 0);

            Assert.IsFalse(drag.TryBegin(-1, 0));
            Assert.IsFalse(drag.TryBegin(0, GridMask.Height));
        }

        [Test]
        public void TryBegin_SucceedsOnAnyCellOfThePiece()
        {
            LoadHorizontalPairAt(1, 2);

            Assert.IsTrue(drag.TryBegin(1, 2));
            Assert.IsTrue(drag.IsActive);

            drag.End();

            Assert.IsTrue(drag.TryBegin(2, 2));
            Assert.IsTrue(drag.IsActive);
        }

        [Test]
        public void TryBegin_RecordsTheGrabOffset()
        {
            LoadHorizontalPairAt(1, 2);

            Assert.IsTrue(drag.TryBegin(2, 2));

            Assert.AreEqual(-1, drag.GrabOffsetColumn);
            Assert.AreEqual(0, drag.GrabOffsetRow);
        }

        [Test]
        public void ThePieceNeverTeleportsToThePointer()
        {
            LoadHorizontalPairAt(0, 0);

            Assert.IsTrue(drag.TryBegin(1, 0));

            drag.MoveTo(1, 0);

            Assert.AreEqual(0, playfield.PieceColumn, "Grabbing a cell must not shift the piece.");
            Assert.AreEqual(0, playfield.PieceRow);
        }

        [Test]
        public void ThePieceFollowsThePointerDeltaExactly()
        {
            LoadHorizontalPairAt(0, 0);

            Assert.IsTrue(drag.TryBegin(1, 0));

            drag.MoveTo(2, 3);

            Assert.AreEqual(1, playfield.PieceColumn);
            Assert.AreEqual(3, playfield.PieceRow);

            drag.MoveTo(1, 1);

            Assert.AreEqual(0, playfield.PieceColumn);
            Assert.AreEqual(1, playfield.PieceRow);
        }

        [Test]
        public void MoveTo_DoesNothingBeforeTheDragBegins()
        {
            LoadHorizontalPairAt(1, 1);

            drag.MoveTo(3, 4);

            Assert.AreEqual(1, playfield.PieceColumn);
            Assert.AreEqual(1, playfield.PieceRow);
        }

        [Test]
        public void MoveTo_DoesNothingAfterTheDragEnds()
        {
            LoadHorizontalPairAt(0, 0);

            Assert.IsTrue(drag.TryBegin(0, 0));

            drag.MoveTo(1, 1);
            drag.End();
            drag.MoveTo(2, 3);

            Assert.IsFalse(drag.IsActive);
            Assert.AreEqual(1, playfield.PieceColumn);
            Assert.AreEqual(1, playfield.PieceRow);
        }

        [Test]
        public void ThePieceStopsAtTheBoardEdgeInsteadOfLeavingIt()
        {
            LoadHorizontalPairAt(0, 0);

            Assert.IsTrue(drag.TryBegin(0, 0));

            drag.MoveTo(GridMask.Width - 1, 0);

            Assert.AreEqual(GridMask.Width - 2, playfield.PieceColumn, "A two cell piece can reach column 2 at most.");
            Assert.AreEqual(0, playfield.PieceRow);
        }

        [Test]
        public void ABlockedAxisDoesNotFreezeTheOtherOne()
        {
            LoadHorizontalPairAt(0, 0);

            Assert.IsTrue(drag.TryBegin(0, 0));

            drag.MoveTo(GridMask.Width, 1);

            Assert.AreEqual(GridMask.Width - 2, playfield.PieceColumn, "The piece should slide as far right as it fits.");
            Assert.AreEqual(1, playfield.PieceRow, "A blocked column must not block the row.");
        }

        [Test]
        public void DraggingNeverChangesTheShape()
        {
            LoadHorizontalPairAt(0, 0);

            int cellCount = playfield.PieceMask.Count;

            Assert.IsTrue(drag.TryBegin(0, 0));

            for (int row = -2; row < GridMask.Height + 2; row++)
            {
                for (int column = -2; column < GridMask.Width + 2; column++)
                {
                    drag.MoveTo(column, row);

                    Assert.AreEqual(cellCount, playfield.PieceMask.Count, $"Shape changed at pointer ({column},{row}).");
                    Assert.IsTrue(GridPattern.IsConnected(playfield.PieceMask));
                }
            }
        }

        [Test]
        public void ARestartedDragUsesTheNewGrabPoint()
        {
            LoadHorizontalPairAt(0, 0);

            Assert.IsTrue(drag.TryBegin(0, 0));
            drag.MoveTo(2, 2);
            drag.End();

            Assert.AreEqual(2, playfield.PieceColumn);
            Assert.AreEqual(2, playfield.PieceRow);

            Assert.IsTrue(drag.TryBegin(3, 2));
            Assert.AreEqual(-1, drag.GrabOffsetColumn);

            drag.MoveTo(3, 3);

            Assert.AreEqual(2, playfield.PieceColumn, "The piece must not jump when grabbed a second time.");
            Assert.AreEqual(3, playfield.PieceRow);
        }

        void LoadHorizontalPairAt(int column, int row)
        {
            GridMask shape = GridMask.Empty.WithCell(0, 0).WithCell(1, 0);

            playfield.Load(new LevelData(shape, shape, 0, 0, column, row, 1));
        }
    }
}
