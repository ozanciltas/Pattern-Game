using System;
using NUnit.Framework;
using PatternGame.Gameplay;
using PatternGame.Gameplay.Levels;
using PatternGame.Grid;

namespace PatternGame.Tests.EditMode
{
    [TestFixture]
    public sealed class PlayfieldTests
    {
        const int SweepSeedCount = 2000;

        LevelGenerator generator;
        Playfield playfield;

        [SetUp]
        public void SetUp()
        {
            generator = new LevelGenerator();
            playfield = new Playfield();
        }

        [Test]
        public void Load_PlacesThePieceAtItsSpawnPlacement()
        {
            var level = generator.Generate(11, SettingsFor(11));

            playfield.Load(level);

            Assert.IsTrue(playfield.HasLevel);
            Assert.AreEqual(level.SpawnColumn, playfield.PieceColumn);
            Assert.AreEqual(level.SpawnRow, playfield.PieceRow);
            Assert.IsTrue(level.TryGetSpawnMask(out var spawnMask));
            Assert.AreEqual(spawnMask, playfield.PieceMask);
        }

        [Test]
        public void Load_CachesTheTargetMaskOfTheSolutionPlacement()
        {
            var level = generator.Generate(12, SettingsFor(12));

            playfield.Load(level);

            Assert.IsTrue(level.TryGetTargetMask(out var expectedTarget));
            Assert.AreEqual(expectedTarget, playfield.TargetMask);
            Assert.IsTrue(level.WallMask.Contains(playfield.TargetMask), "The wall must contain its own target.");
        }

        [Test]
        public void Load_NeverStartsAlreadySolved()
        {
            for (int seed = 1; seed <= SweepSeedCount; seed++)
            {
                var level = generator.Generate(seed, SettingsFor(seed));

                playfield.Load(level);

                if (playfield.Evaluate() == MatchResult.PatternMatched)
                {
                    Assert.Fail($"Seed {seed} loaded a level that is already solved.");
                }
            }
        }

        [Test]
        public void Evaluate_ReturnsPatternMatchedAtTheSolution()
        {
            for (int seed = 1; seed <= SweepSeedCount; seed++)
            {
                var level = generator.Generate(seed, SettingsFor(seed));

                playfield.Load(level);

                Assert.IsTrue(playfield.TryMovePieceTo(level.SolutionColumn, level.SolutionRow));

                if (playfield.Evaluate() != MatchResult.PatternMatched)
                {
                    Assert.Fail(
                        $"Seed {seed}: the piece on the solution did not match.\n"
                        + $"Target:\n{playfield.TargetMask}\nPiece:\n{playfield.PieceMask}");
                }
            }
        }

        [Test]
        public void Evaluate_ReturnsMismatchedEverywhereExceptTheSolution()
        {
            for (int seed = 1; seed <= 500; seed++)
            {
                var level = generator.Generate(seed, SettingsFor(seed));

                playfield.Load(level);

                for (int row = 0; row < GridMask.Height; row++)
                {
                    for (int column = 0; column < GridMask.Width; column++)
                    {
                        if (!playfield.TryMovePieceTo(column, row))
                        {
                            continue;
                        }

                        bool isSolution = column == level.SolutionColumn && row == level.SolutionRow;
                        var expected = isSolution ? MatchResult.PatternMatched : MatchResult.Mismatched;

                        if (playfield.Evaluate() != expected)
                        {
                            Assert.Fail($"Seed {seed}: placement ({column},{row}) evaluated as the wrong result.");
                        }
                    }
                }
            }
        }

        [Test]
        public void Evaluate_IgnoresWallCellsOutsideTheTarget()
        {
            var target = GridPattern.Parse(
                "....",
                "....",
                ".##.",
                "..#.",
                "....");

            var wall = target
                .WithCell(0, 0)
                .WithCell(3, 4);

            playfield.Load(new LevelData(wall, GridMask.Empty.WithCell(0, 1).WithCell(1, 1).WithCell(1, 0), 1, 1, 0, 3, 7));

            Assert.AreEqual(target, playfield.TargetMask);
            Assert.IsTrue(playfield.TryMovePieceTo(1, 1));
            Assert.AreEqual(MatchResult.PatternMatched, playfield.Evaluate());
        }

        [Test]
        public void TryMovePieceTo_RejectsPlacementsThatLeaveTheBoard()
        {
            var level = generator.Generate(21, SettingsFor(21));
            playfield.Load(level);

            int columnBefore = playfield.PieceColumn;
            int rowBefore = playfield.PieceRow;
            var maskBefore = playfield.PieceMask;

            Assert.IsFalse(playfield.TryMovePieceTo(-1, 0));
            Assert.IsFalse(playfield.TryMovePieceTo(0, -1));
            Assert.IsFalse(playfield.TryMovePieceTo(GridMask.Width, 0));
            Assert.IsFalse(playfield.TryMovePieceTo(0, GridMask.Height));

            Assert.AreEqual(columnBefore, playfield.PieceColumn);
            Assert.AreEqual(rowBefore, playfield.PieceRow);
            Assert.AreEqual(maskBefore, playfield.PieceMask);
        }

        [Test]
        public void TryMovePieceTo_IsNeverBlockedByTheWall()
        {
            for (int seed = 1; seed <= 500; seed++)
            {
                var level = generator.Generate(seed, SettingsFor(seed));

                playfield.Load(level);

                for (int row = 0; row < GridMask.Height; row++)
                {
                    for (int column = 0; column < GridMask.Width; column++)
                    {
                        bool fitsOnBoard = level.KeyPieceShape.TryTranslate(column, row, out _);

                        if (playfield.TryMovePieceTo(column, row) != fitsOnBoard)
                        {
                            Assert.Fail(
                                $"Seed {seed}: placement ({column},{row}) was rejected for a reason "
                                + "other than leaving the board.");
                        }
                    }
                }
            }
        }

        [Test]
        public void TryMovePieceBy_MovesRelativeToTheCurrentPlacement()
        {
            var level = generator.Generate(33, new LevelGenerationSettings(6, 2, 1f, 2));
            playfield.Load(level);

            playfield.TryMovePieceTo(0, 0);

            Assert.IsTrue(playfield.TryMovePieceBy(1, 1));
            Assert.AreEqual(1, playfield.PieceColumn);
            Assert.AreEqual(1, playfield.PieceRow);

            Assert.IsTrue(playfield.TryMovePieceBy(-1, 0));
            Assert.AreEqual(0, playfield.PieceColumn);
            Assert.AreEqual(1, playfield.PieceRow);
        }

        [Test]
        public void ResetPieceToSpawn_RestoresTheStartingPlacement()
        {
            var level = generator.Generate(44, SettingsFor(44));
            playfield.Load(level);

            Assert.IsTrue(playfield.TryMovePieceTo(level.SolutionColumn, level.SolutionRow));
            Assert.AreEqual(MatchResult.PatternMatched, playfield.Evaluate());

            playfield.ResetPieceToSpawn();

            Assert.AreEqual(level.SpawnColumn, playfield.PieceColumn);
            Assert.AreEqual(level.SpawnRow, playfield.PieceRow);
            Assert.AreEqual(MatchResult.Mismatched, playfield.Evaluate());
        }

        [Test]
        public void PlacementLimits_MatchTheShapeExtents()
        {
            GridMask shape = GridMask.Empty.WithCell(0, 0).WithCell(1, 0).WithCell(1, 1);

            playfield.Load(new LevelData(shape, shape, 0, 0, 1, 2, 1));

            Assert.AreEqual(0, playfield.MinPieceColumn);
            Assert.AreEqual(0, playfield.MinPieceRow);
            Assert.AreEqual(GridMask.Width - 2, playfield.MaxPieceColumn, "A two column shape stops one column early.");
            Assert.AreEqual(GridMask.Height - 2, playfield.MaxPieceRow, "A two row shape stops one row early.");
        }

        [Test]
        public void PlacementLimits_AllowEveryPlacementForASingleCell()
        {
            GridMask shape = GridMask.Empty.WithCell(0, 0);

            playfield.Load(new LevelData(shape, shape, 0, 0, 1, 2, 1));

            Assert.AreEqual(0, playfield.MinPieceColumn);
            Assert.AreEqual(GridMask.Width - 1, playfield.MaxPieceColumn);
            Assert.AreEqual(GridMask.Height - 1, playfield.MaxPieceRow);
        }

        [Test]
        public void EveryPlacementInsideTheLimitsIsAccepted()
        {
            for (int seed = 1; seed <= 500; seed++)
            {
                var level = generator.Generate(seed, SettingsFor(seed));

                playfield.Load(level);

                for (int row = playfield.MinPieceRow; row <= playfield.MaxPieceRow; row++)
                {
                    for (int column = playfield.MinPieceColumn; column <= playfield.MaxPieceColumn; column++)
                    {
                        if (!playfield.TryMovePieceTo(column, row))
                        {
                            Assert.Fail($"Seed {seed}: placement ({column},{row}) is inside the limits but was rejected.");
                        }
                    }
                }
            }
        }

        [Test]
        public void PlacementsOutsideTheLimitsAreRejected()
        {
            for (int seed = 1; seed <= 500; seed++)
            {
                var level = generator.Generate(seed, SettingsFor(seed));

                playfield.Load(level);

                Assert.IsFalse(playfield.TryMovePieceTo(playfield.MinPieceColumn - 1, playfield.MinPieceRow));
                Assert.IsFalse(playfield.TryMovePieceTo(playfield.MaxPieceColumn + 1, playfield.MinPieceRow));
                Assert.IsFalse(playfield.TryMovePieceTo(playfield.MinPieceColumn, playfield.MinPieceRow - 1));
                Assert.IsFalse(playfield.TryMovePieceTo(playfield.MinPieceColumn, playfield.MaxPieceRow + 1));
            }
        }

        [Test]
        public void Clear_LeavesThePlayfieldWithoutALevel()
        {
            playfield.Load(generator.Generate(55, SettingsFor(55)));
            playfield.Clear();

            Assert.IsFalse(playfield.HasLevel);
            Assert.AreEqual(GridMask.Empty, playfield.TargetMask);
            Assert.Throws<InvalidOperationException>(() => playfield.Evaluate());
        }

        [Test]
        public void OperationsThrowBeforeALevelIsLoaded()
        {
            Assert.Throws<InvalidOperationException>(() => playfield.Evaluate());
            Assert.Throws<InvalidOperationException>(() => playfield.TryMovePieceTo(0, 0));
            Assert.Throws<InvalidOperationException>(() => playfield.ResetPieceToSpawn());
        }

        [Test]
        public void Load_RejectsALevelWhoseSpawnPlacementDoesNotFit()
        {
            var shape = GridPattern.Parse(
                "....",
                "....",
                "....",
                "....",
                "###.");

            var brokenLevel = new LevelData(shape, shape, 0, 0, GridMask.Width - 1, 0, 1);

            Assert.Throws<ArgumentException>(() => playfield.Load(brokenLevel));
            Assert.IsFalse(playfield.HasLevel);
        }

        [Test]
        public void Load_RejectsALevelWhoseSolutionPlacementDoesNotFit()
        {
            var shape = GridPattern.Parse(
                "....",
                "....",
                "....",
                "....",
                "###.");

            var brokenLevel = new LevelData(shape, shape, GridMask.Width - 1, 0, 0, 0, 1);

            Assert.Throws<ArgumentException>(() => playfield.Load(brokenLevel));
            Assert.IsFalse(playfield.HasLevel);
        }

        static LevelGenerationSettings SettingsFor(int seed)
        {
            return new LevelGenerationSettings(6 + (seed % 6), 2 + (seed % 4), 0.5f, 2);
        }
    }
}
