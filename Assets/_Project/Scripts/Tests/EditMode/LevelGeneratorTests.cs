using System;
using NUnit.Framework;
using PatternGame.Gameplay.Levels;
using PatternGame.Grid;

namespace PatternGame.Tests.EditMode
{
    [TestFixture]
    public sealed class LevelGeneratorTests
    {
        const int SweepSeedCount = 5000;

        LevelGenerator generator;

        [SetUp]
        public void SetUp()
        {
            generator = new LevelGenerator();
        }

        static LevelGenerationSettings SweepSettings(int seed)
        {
            return new LevelGenerationSettings(6 + (seed % 9), 2 + (seed % 5), (seed % 11) / 10f, 2);
        }

        [Test]
        public void Generate_IsDeterministicForTheSameSeedAndSettings()
        {
            var settings = new LevelGenerationSettings(9, 4, 0.4f, 2);

            for (int seed = 1; seed <= 1000; seed++)
            {
                var first = generator.Generate(seed, settings);
                var second = generator.Generate(seed, settings);

                if (first.WallMask != second.WallMask
                    || first.KeyPieceShape != second.KeyPieceShape
                    || first.SolutionColumn != second.SolutionColumn
                    || first.SolutionRow != second.SolutionRow
                    || first.SpawnColumn != second.SpawnColumn
                    || first.SpawnRow != second.SpawnRow)
                {
                    Assert.Fail($"Seed {seed} produced two different levels.");
                }
            }
        }

        [Test]
        public void Generate_ProducesDifferentLevelsForDifferentSeeds()
        {
            var settings = new LevelGenerationSettings(9, 4, 0.4f, 2);
            int identicalWalls = 0;

            for (int seed = 1; seed <= 500; seed++)
            {
                if (generator.Generate(seed, settings).WallMask == generator.Generate(seed + 10000, settings).WallMask)
                {
                    identicalWalls++;
                }
            }

            Assert.Less(identicalWalls, 100, "Different seeds produced suspiciously similar levels.");
        }

        [Test]
        public void Generate_RecordsTheSeedInTheLevelData()
        {
            var settings = new LevelGenerationSettings(8, 3, 0.5f, 2);

            Assert.AreEqual(777, generator.Generate(777, settings).Seed);
        }

        [Test]
        public void Generate_ProducesTheRequestedNumberOfPatternCells()
        {
            for (int patternCellCount = 1; patternCellCount <= 6; patternCellCount++)
            {
                var settings = new LevelGenerationSettings(10, patternCellCount, 0.5f, 1);

                for (int seed = 1; seed <= 1000; seed++)
                {
                    var level = generator.Generate(seed, settings);

                    Assert.IsTrue(level.TryGetTargetMask(out var targetMask));

                    if (level.KeyPieceShape.Count != patternCellCount || targetMask.Count != patternCellCount)
                    {
                        Assert.Fail(
                            $"Seed {seed} with {patternCellCount} requested cells produced a piece of "
                            + $"{level.KeyPieceShape.Count} and a target of {targetMask.Count} cells.");
                    }
                }
            }
        }

        [Test]
        public void Generate_ProducesTheRequestedNumberOfWallCells()
        {
            for (int wallCellCount = 4; wallCellCount <= GridMask.CellCount; wallCellCount++)
            {
                var settings = new LevelGenerationSettings(wallCellCount, 4, 0.5f, 2);

                for (int seed = 1; seed <= 500; seed++)
                {
                    var level = generator.Generate(seed, settings);

                    if (level.WallMask.Count != settings.WallCellCount)
                    {
                        Assert.Fail(
                            $"Seed {seed} with {settings.WallCellCount} requested wall cells produced "
                            + $"{level.WallMask.Count}:\n{level.WallMask}");
                    }
                }
            }
        }

        [Test]
        public void Generate_AlwaysProducesAConnectedPattern()
        {
            for (int seed = 1; seed <= SweepSeedCount; seed++)
            {
                var level = generator.Generate(seed, SweepSettings(seed));

                if (!GridPattern.IsConnected(level.KeyPieceShape))
                {
                    Assert.Fail($"Seed {seed} produced a disconnected pattern:\n{level.KeyPieceShape}");
                }
            }
        }

        [Test]
        public void Generate_WallAlwaysContainsTheTargetPattern()
        {
            for (int seed = 1; seed <= SweepSeedCount; seed++)
            {
                var level = generator.Generate(seed, SweepSettings(seed));

                Assert.IsTrue(level.TryGetTargetMask(out var targetMask), $"Seed {seed}: the target leaves the board.");

                if (!level.WallMask.Contains(targetMask))
                {
                    Assert.Fail(
                        $"Seed {seed}: the target is not part of the wall.\nWall:\n{level.WallMask}\nTarget:\n{targetMask}");
                }
            }
        }

        [Test]
        public void Generate_SolutionOffsetMapsTheKeyPieceOntoTheTarget()
        {
            for (int seed = 1; seed <= SweepSeedCount; seed++)
            {
                var level = generator.Generate(seed, SweepSettings(seed));

                Assert.IsTrue(level.TryGetTargetMask(out var targetMask));

                Assert.IsTrue(
                    GridPattern.TryFindOffset(level.KeyPieceShape, targetMask, out int column, out int row),
                    $"Seed {seed}: the piece shape does not map onto the target at any offset.");

                Assert.AreEqual(level.SolutionColumn, column, $"Seed {seed}: recorded solution column is wrong.");
                Assert.AreEqual(level.SolutionRow, row, $"Seed {seed}: recorded solution row is wrong.");
            }
        }

        [Test]
        public void Generate_KeyPieceShapeIsAnchoredToTheOrigin()
        {
            for (int seed = 1; seed <= SweepSeedCount; seed++)
            {
                var shape = generator.Generate(seed, SweepSettings(seed)).KeyPieceShape;

                bool touchesFirstColumn = false;
                bool touchesFirstRow = false;

                for (int index = 0; index < GridMask.CellCount; index++)
                {
                    int column = GridMask.ColumnOf(index);
                    int row = GridMask.RowOf(index);

                    if (!shape.IsOccupied(column, row))
                    {
                        continue;
                    }

                    if (column == 0) touchesFirstColumn = true;
                    if (row == 0) touchesFirstRow = true;
                }

                if (!touchesFirstColumn || !touchesFirstRow)
                {
                    Assert.Fail($"Seed {seed} produced a shape that is not normalized:\n{shape}");
                }
            }
        }

        [Test]
        public void Generate_SpawnPlacementAlwaysFitsOnTheBoard()
        {
            for (int seed = 1; seed <= SweepSeedCount; seed++)
            {
                var level = generator.Generate(seed, SweepSettings(seed));

                if (!level.TryGetSpawnMask(out var spawnMask))
                {
                    Assert.Fail($"Seed {seed} produced a spawn placement that leaves the board.");
                }

                if (spawnMask.Count != level.KeyPieceShape.Count)
                {
                    Assert.Fail($"Seed {seed} lost cells when the piece moved to its spawn placement.");
                }
            }
        }

        [Test]
        public void Generate_NeverSpawnsThePieceAlreadyOnTheTarget()
        {
            for (int seed = 1; seed <= SweepSeedCount; seed++)
            {
                var level = generator.Generate(seed, SweepSettings(seed));

                Assert.IsTrue(level.TryGetSpawnMask(out var spawnMask));
                Assert.IsTrue(level.TryGetTargetMask(out var targetMask));

                if (spawnMask == targetMask)
                {
                    Assert.Fail($"Seed {seed} spawned the piece already sitting on the target.");
                }
            }
        }

        [Test]
        public void Generate_RespectsTheMinimumSpawnDistance()
        {
            const int minimumSpawnDistance = 2;

            for (int seed = 1; seed <= SweepSeedCount; seed++)
            {
                var settings = new LevelGenerationSettings(
                    6 + (seed % 9),
                    2 + (seed % 5),
                    (seed % 11) / 10f,
                    minimumSpawnDistance);

                var level = generator.Generate(seed, settings);

                int distance = Math.Abs(level.SpawnColumn - level.SolutionColumn)
                    + Math.Abs(level.SpawnRow - level.SolutionRow);

                if (distance < minimumSpawnDistance)
                {
                    Assert.Fail(
                        $"Seed {seed}: spawn ({level.SpawnColumn},{level.SpawnRow}) is only {distance} steps "
                        + $"from the solution ({level.SolutionColumn},{level.SolutionRow}).");
                }
            }
        }

        [Test]
        public void Compactness_ProducesTighterPatternsThanSprawl()
        {
            long sprawlingArea = 0;
            long compactArea = 0;

            for (int seed = 1; seed <= SweepSeedCount; seed++)
            {
                sprawlingArea += GridPattern.BoundingBoxArea(
                    generator.Generate(seed, new LevelGenerationSettings(10, 5, 0f, 2)).KeyPieceShape);

                compactArea += GridPattern.BoundingBoxArea(
                    generator.Generate(seed, new LevelGenerationSettings(10, 5, 1f, 2)).KeyPieceShape);
            }

            Assert.Less(
                compactArea,
                sprawlingArea,
                $"Compact shapes spanned {compactArea} cells of bounding box against {sprawlingArea} for sprawling ones.");
        }

        [Test]
        public void Compactness_ProducesFewerWallIslandsThanSprawl()
        {
            long sprawlingIslands = 0;
            long compactIslands = 0;

            for (int seed = 1; seed <= SweepSeedCount; seed++)
            {
                sprawlingIslands += GridPattern.CountIslands(
                    generator.Generate(seed, new LevelGenerationSettings(10, 3, 0f, 2)).WallMask);

                compactIslands += GridPattern.CountIslands(
                    generator.Generate(seed, new LevelGenerationSettings(10, 3, 1f, 2)).WallMask);
            }

            Assert.AreEqual(SweepSeedCount, compactIslands, "A fully compact wall must always be a single island.");
            Assert.Less(compactIslands, sprawlingIslands, "Sprawling walls did not break into more islands.");
        }

        [Test]
        public void Settings_ClampValuesOutsideTheSupportedRange()
        {
            var tooSmall = new LevelGenerationSettings(-10, -3, -3f, -5);
            var tooLarge = new LevelGenerationSettings(999, 999, 7f, 4);

            Assert.AreEqual(LevelGenerationSettings.MinimumPatternCellCount, tooSmall.PatternCellCount);
            Assert.AreEqual(LevelGenerationSettings.MinimumPatternCellCount, tooSmall.WallCellCount);
            Assert.AreEqual(0f, tooSmall.Compactness);
            Assert.AreEqual(0, tooSmall.MinimumSpawnDistance);

            Assert.AreEqual(LevelGenerationSettings.MaximumPatternCellCountFor(4), tooLarge.PatternCellCount);
            Assert.AreEqual(LevelGenerationSettings.MaximumWallCellCount, tooLarge.WallCellCount);
            Assert.AreEqual(1f, tooLarge.Compactness);
            Assert.AreEqual(4, tooLarge.MinimumSpawnDistance);
        }

        [Test]
        public void Settings_WallCellCountIsNeverBelowThePatternCellCount()
        {
            var settings = new LevelGenerationSettings(2, 6, 0.5f, 1);

            Assert.AreEqual(6, settings.PatternCellCount);
            Assert.AreEqual(6, settings.WallCellCount);
            Assert.AreEqual(0, settings.ExtraWallCellCount);
        }

        [Test]
        public void MaximumPatternCellCount_ShrinksAsTheRequiredSpawnDistanceGrows()
        {
            int previous = int.MaxValue;

            for (int distance = 1; distance <= LevelGenerationSettings.MaximumSpawnDistance; distance++)
            {
                int maximum = LevelGenerationSettings.MaximumPatternCellCountFor(distance);

                Assert.LessOrEqual(maximum, previous, $"Distance {distance} allowed a larger pattern than {distance - 1}.");
                Assert.GreaterOrEqual(maximum, LevelGenerationSettings.MinimumPatternCellCount);

                previous = maximum;
            }
        }

        [Test]
        public void Generate_HandlesASingleCellPattern()
        {
            var settings = new LevelGenerationSettings(8, 1, 0.5f, 1);

            for (int seed = 1; seed <= 500; seed++)
            {
                var level = generator.Generate(seed, settings);

                Assert.AreEqual(1, level.KeyPieceShape.Count);
                Assert.AreEqual(8, level.WallMask.Count);
            }
        }

        [Test]
        public void Generate_HandlesAWallThatCoversTheWholeBoard()
        {
            var settings = new LevelGenerationSettings(GridMask.CellCount, 3, 0.5f, 2);

            for (int seed = 1; seed <= 500; seed++)
            {
                var level = generator.Generate(seed, settings);

                Assert.IsTrue(level.WallMask.IsFull, $"Seed {seed} did not fill the board.");
                Assert.IsTrue(level.TryGetTargetMask(out var targetMask));
                Assert.AreEqual(3, targetMask.Count);
                Assert.IsTrue(level.WallMask.Contains(targetMask));
            }
        }

        [Test]
        public void Generate_HandlesTheLargestAllowedPattern()
        {
            int largestPattern = LevelGenerationSettings.MaximumPatternCellCountFor(0);
            var settings = new LevelGenerationSettings(GridMask.CellCount, largestPattern, 0.5f, 0);

            for (int seed = 1; seed <= 200; seed++)
            {
                var level = generator.Generate(seed, settings);

                Assert.AreEqual(largestPattern, level.KeyPieceShape.Count);
                Assert.IsTrue(GridPattern.IsConnected(level.KeyPieceShape));
                Assert.IsTrue(level.TryGetSpawnMask(out var spawnMask));
                Assert.IsTrue(level.TryGetTargetMask(out var targetMask));
                Assert.AreNotEqual(targetMask, spawnMask, $"Seed {seed} spawned the largest piece already solved.");
            }
        }
    }
}
