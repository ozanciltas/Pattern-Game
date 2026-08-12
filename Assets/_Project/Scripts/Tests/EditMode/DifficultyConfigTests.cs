using NUnit.Framework;
using UnityEngine;
using PatternGame.Gameplay.Levels;
using PatternGame.Grid;

namespace PatternGame.Tests.EditMode
{
    [TestFixture]
    public sealed class DifficultyConfigTests
    {
        DifficultyConfig config;
        LevelGenerator generator;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<DifficultyConfig>();
            generator = new LevelGenerator();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void GetProgress_StaysInsideTheUnitInterval()
        {
            for (int levelIndex = -50; levelIndex <= 500; levelIndex++)
            {
                float progress = config.GetProgress(levelIndex);

                if (progress < 0f || progress > 1f)
                {
                    Assert.Fail($"Level {levelIndex} produced a progress of {progress}.");
                }
            }
        }

        [Test]
        public void GetProgress_StartsAtZeroAndReachesOne()
        {
            Assert.AreEqual(0f, config.GetProgress(0));
            Assert.AreEqual(1f, config.GetProgress(1000));
        }

        [Test]
        public void GetSettings_NeverExceedsTheGeometricMaximum()
        {
            for (int levelIndex = 0; levelIndex <= 500; levelIndex++)
            {
                var settings = config.GetSettings(levelIndex);

                Assert.GreaterOrEqual(settings.PatternCellCount, LevelGenerationSettings.MinimumPatternCellCount);
                Assert.LessOrEqual(
                    settings.PatternCellCount,
                    config.LargestReachablePatternCellCount,
                    $"Level {levelIndex} asked for more pattern cells than the board can offer.");
            }
        }

        [Test]
        public void GetSettings_NeverAsksForAWallSmallerThanItsPattern()
        {
            for (int levelIndex = 0; levelIndex <= 500; levelIndex++)
            {
                var settings = config.GetSettings(levelIndex);

                Assert.GreaterOrEqual(
                    settings.WallCellCount,
                    settings.PatternCellCount,
                    $"Level {levelIndex} asked for a wall that cannot contain its own target.");

                Assert.LessOrEqual(settings.WallCellCount, LevelGenerationSettings.MaximumWallCellCount);
                Assert.GreaterOrEqual(settings.ExtraWallCellCount, 0);
            }
        }

        [Test]
        public void GetSettings_ProducesValidCompactness()
        {
            for (int levelIndex = 0; levelIndex <= 500; levelIndex++)
            {
                float compactness = config.GetSettings(levelIndex).Compactness;

                if (compactness < 0f || compactness > 1f)
                {
                    Assert.Fail($"Level {levelIndex} produced a compactness of {compactness}.");
                }
            }
        }

        [Test]
        public void GetWallSpeed_IsAlwaysPositive()
        {
            for (int levelIndex = -10; levelIndex <= 500; levelIndex++)
            {
                Assert.Greater(config.GetWallSpeed(levelIndex), 0f, $"Level {levelIndex} produced a non-positive speed.");
            }
        }

        [Test]
        public void GetSettings_FeedTheGeneratorValidLevelsForEveryLevelIndex()
        {
            for (int levelIndex = 0; levelIndex <= 200; levelIndex++)
            {
                var settings = config.GetSettings(levelIndex);
                var level = generator.Generate(levelIndex * 7919 + 13, settings);

                Assert.AreEqual(
                    settings.PatternCellCount,
                    level.KeyPieceShape.Count,
                    $"Level {levelIndex} produced the wrong piece size.");

                Assert.AreEqual(
                    settings.WallCellCount,
                    level.WallMask.Count,
                    $"Level {levelIndex} produced the wrong wall size.");

                Assert.IsTrue(
                    GridPattern.IsConnected(level.KeyPieceShape),
                    $"Level {levelIndex} produced a disconnected piece.");

                Assert.IsTrue(
                    level.TryGetTargetMask(out var targetMask),
                    $"Level {levelIndex} produced a solution placement outside the board.");

                Assert.IsTrue(
                    level.TryGetSpawnMask(out var spawnMask),
                    $"Level {levelIndex} produced a spawn placement outside the board.");

                Assert.AreNotEqual(
                    targetMask,
                    spawnMask,
                    $"Level {levelIndex} spawned the piece already solved.");

                Assert.IsTrue(
                    level.WallMask.Contains(targetMask),
                    $"Level {levelIndex} produced a wall that does not contain its target.");
            }
        }

        [Test]
        public void DefaultCurves_MakeLaterLevelsHarder()
        {
            var early = config.GetSettings(0);
            var late = config.GetSettings(1000);

            Assert.Greater(late.PatternCellCount, early.PatternCellCount, "Later levels should use bigger patterns.");
            Assert.Greater(late.WallCellCount, early.WallCellCount, "Later levels should use busier walls.");
            Assert.Less(late.Compactness, early.Compactness, "Later levels should use more sprawling shapes.");
            Assert.Greater(config.GetWallSpeed(1000), config.GetWallSpeed(0), "Later levels should move faster.");
        }

        [Test]
        public void LargestReachablePatternCellCount_MatchesTheConfiguredSpawnDistance()
        {
            Assert.AreEqual(
                LevelGenerationSettings.MaximumPatternCellCountFor(config.MinimumSpawnDistance),
                config.LargestReachablePatternCellCount);

            Assert.LessOrEqual(config.LargestReachablePatternCellCount, GridMask.CellCount - 1);
        }
    }
}
