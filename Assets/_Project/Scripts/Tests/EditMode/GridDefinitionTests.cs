using NUnit.Framework;
using UnityEngine;
using PatternGame.Grid;

namespace PatternGame.Tests.EditMode
{
    [TestFixture]
    public sealed class GridDefinitionTests
    {
        GridDefinition definition;

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<GridDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Board_IsCentredOnTheOrigin()
        {
            Vector3 bottomLeft = definition.GetCellLocalPosition(0, 0);
            Vector3 topRight = definition.GetCellLocalPosition(GridMask.Width - 1, GridMask.Height - 1);

            Assert.That(bottomLeft.x, Is.EqualTo(-topRight.x).Within(0.0001f));
            Assert.That(bottomLeft.y, Is.EqualTo(-topRight.y).Within(0.0001f));
        }

        [Test]
        public void Cells_SitOnTheGridPlane()
        {
            for (int row = 0; row < GridMask.Height; row++)
            {
                for (int column = 0; column < GridMask.Width; column++)
                {
                    Assert.AreEqual(0f, definition.GetCellLocalPosition(column, row).z);
                }
            }
        }

        [Test]
        public void NeighbouringCells_AreExactlyOnePitchApart()
        {
            float pitch = definition.CellPitch;

            Vector3 origin = definition.GetCellLocalPosition(1, 1);
            Vector3 toTheRight = definition.GetCellLocalPosition(2, 1);
            Vector3 above = definition.GetCellLocalPosition(1, 2);

            Assert.That(toTheRight.x - origin.x, Is.EqualTo(pitch).Within(0.0001f));
            Assert.That(above.y - origin.y, Is.EqualTo(pitch).Within(0.0001f));
        }

        [Test]
        public void GetNearestCell_RoundTripsEveryCellCentre()
        {
            for (int row = 0; row < GridMask.Height; row++)
            {
                for (int column = 0; column < GridMask.Width; column++)
                {
                    Vector3 centre = definition.GetCellLocalPosition(column, row);

                    definition.GetNearestCell(centre, out int resolvedColumn, out int resolvedRow);

                    Assert.AreEqual(column, resolvedColumn, $"Column mismatch for cell ({column},{row}).");
                    Assert.AreEqual(row, resolvedRow, $"Row mismatch for cell ({column},{row}).");
                }
            }
        }

        [Test]
        public void GetNearestCell_SnapsToTheClosestCell()
        {
            Vector3 justPastTheCentre = definition.GetCellLocalPosition(1, 1)
                + new Vector3(definition.CellPitch * 0.4f, definition.CellPitch * 0.4f, 0f);

            definition.GetNearestCell(justPastTheCentre, out int column, out int row);

            Assert.AreEqual(1, column);
            Assert.AreEqual(1, row);

            Vector3 pastTheHalfwayPoint = definition.GetCellLocalPosition(1, 1)
                + new Vector3(definition.CellPitch * 0.6f, definition.CellPitch * 0.6f, 0f);

            definition.GetNearestCell(pastTheHalfwayPoint, out column, out row);

            Assert.AreEqual(2, column);
            Assert.AreEqual(2, row);
        }

        [Test]
        public void GetNearestCell_CanReportCellsOutsideTheBoard()
        {
            Vector3 wellBelowTheBoard = definition.GetCellLocalPosition(0, 0)
                - new Vector3(definition.CellPitch * 2f, definition.CellPitch * 2f, 0f);

            definition.GetNearestCell(wellBelowTheBoard, out int column, out int row);

            Assert.AreEqual(-2, column);
            Assert.AreEqual(-2, row);
        }

        [Test]
        public void GetNearestCellOnBoard_ClampsToTheBoard()
        {
            Vector3 farOutside = new Vector3(1000f, 1000f, 0f);

            definition.GetNearestCellOnBoard(farOutside, out int column, out int row);

            Assert.AreEqual(GridMask.Width - 1, column);
            Assert.AreEqual(GridMask.Height - 1, row);

            definition.GetNearestCellOnBoard(-farOutside, out column, out row);

            Assert.AreEqual(0, column);
            Assert.AreEqual(0, row);
        }

        [Test]
        public void BoardDimensions_CoverEveryCellAndGap()
        {
            float expectedWidth = GridMask.Width * definition.CellSize
                + (GridMask.Width - 1) * definition.CellSpacing;

            float expectedHeight = GridMask.Height * definition.CellSize
                + (GridMask.Height - 1) * definition.CellSpacing;

            Assert.That(definition.BoardWorldWidth, Is.EqualTo(expectedWidth).Within(0.0001f));
            Assert.That(definition.BoardWorldHeight, Is.EqualTo(expectedHeight).Within(0.0001f));
        }

        [Test]
        public void BoardIsTallerThanItIsWide()
        {
            Assert.Greater(
                definition.BoardWorldHeight,
                definition.BoardWorldWidth,
                "The board must stay portrait shaped for a phone screen.");
        }

        [Test]
        public void CellExtents_StayInsideTheBoardDimensions()
        {
            float halfCell = definition.CellSize * 0.5f;

            float rightEdge = definition.GetColumnOffset(GridMask.Width - 1) + halfCell;
            float topEdge = definition.GetRowOffset(GridMask.Height - 1) + halfCell;

            Assert.That(rightEdge, Is.EqualTo(definition.BoardWorldWidth * 0.5f).Within(0.0001f));
            Assert.That(topEdge, Is.EqualTo(definition.BoardWorldHeight * 0.5f).Within(0.0001f));
        }
    }
}
