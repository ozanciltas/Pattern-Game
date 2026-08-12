using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PatternGame.Gameplay;
using PatternGame.Gameplay.Levels;
using PatternGame.Gameplay.Progress;

namespace PatternGame.Tests.EditMode
{
    [TestFixture]
    public sealed class GameSessionTests
    {
        const int PaletteVariantCount = 4;

        DifficultyConfig config;
        LevelGenerator generator;
        Playfield playfield;
        InMemoryProgressStorage storage;
        GameSession session;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<DifficultyConfig>();
            generator = new LevelGenerator();
            playfield = new Playfield();
            storage = new InMemoryProgressStorage();
            session = new GameSession(generator, config, playfield, storage, PaletteVariantCount);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Constructor_RejectsMissingDependencies()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GameSession(null, config, playfield, storage, PaletteVariantCount));
            Assert.Throws<ArgumentNullException>(
                () => new GameSession(generator, null, playfield, storage, PaletteVariantCount));
            Assert.Throws<ArgumentNullException>(
                () => new GameSession(generator, config, null, storage, PaletteVariantCount));
            Assert.Throws<ArgumentNullException>(
                () => new GameSession(generator, config, playfield, null, PaletteVariantCount));
        }

        [Test]
        public void StartNewRun_LoadsTheFirstLevelAndResetsProgress()
        {
            session.StartNewRun(1234);

            Assert.IsTrue(session.IsRunning);
            Assert.AreEqual(1234, session.Seed);
            Assert.AreEqual(0, session.LevelIndex);
            Assert.AreEqual(1, session.CurrentLevelNumber);
            Assert.IsTrue(playfield.HasLevel);
            Assert.AreEqual(MatchResult.Mismatched, playfield.Evaluate());
        }

        [Test]
        public void StartNewRun_ClearsProgressFromAPreviousRun()
        {
            session.StartNewRun(1);
            SolveCurrentLevel();
            session.ResolveAttempt();

            Assert.AreEqual(1, session.CompletedLevels);

            session.StartNewRun(2);

            Assert.AreEqual(0, session.CompletedLevels);
            Assert.AreEqual(1, session.CurrentLevelNumber);
            Assert.AreEqual(2, session.Seed);
        }

        [Test]
        public void ResolveAttempt_AtTheSolution_AdvancesToTheNextLevel()
        {
            session.StartNewRun(99);

            SolveCurrentLevel();

            Assert.AreEqual(MatchResult.PatternMatched, session.ResolveAttempt());
            Assert.AreEqual(1, session.LevelIndex);
            Assert.AreEqual(1, session.CompletedLevels);
            Assert.AreEqual(2, session.CurrentLevelNumber);
            Assert.IsTrue(session.IsRunning);
            Assert.IsTrue(playfield.HasLevel);
        }

        [Test]
        public void ResolveAttempt_WhenMismatched_EndsTheRunButKeepsTheBoard()
        {
            session.StartNewRun(99);

            Assert.AreEqual(MatchResult.Mismatched, session.ResolveAttempt());
            Assert.IsFalse(session.IsRunning);
            Assert.AreEqual(0, session.CompletedLevels);
            Assert.IsTrue(playfield.HasLevel, "The failed board should stay visible for the game over screen.");
        }

        [Test]
        public void ResolveAttempt_ThrowsWhenNoRunIsInProgress()
        {
            Assert.Throws<InvalidOperationException>(() => session.ResolveAttempt());

            session.StartNewRun(1);
            session.ResolveAttempt();

            Assert.Throws<InvalidOperationException>(() => session.ResolveAttempt());
        }

        [Test]
        public void EndRun_ClearsTheBoard()
        {
            session.StartNewRun(7);
            session.EndRun();

            Assert.IsFalse(session.IsRunning);
            Assert.IsFalse(playfield.HasLevel);
        }

        [Test]
        public void BestLevel_StartsFromStoredProgress()
        {
            var loadedSession = new GameSession(
                generator,
                config,
                new Playfield(),
                new InMemoryProgressStorage(7),
                PaletteVariantCount);

            Assert.AreEqual(7, loadedSession.BestLevel);
        }

        [Test]
        public void BestLevel_FollowsCompletedLevelsAndIsPersisted()
        {
            session.StartNewRun(11);

            for (int level = 0; level < 3; level++)
            {
                SolveCurrentLevel();
                session.ResolveAttempt();
            }

            Assert.AreEqual(3, session.BestLevel);
            Assert.AreEqual(3, storage.LoadBestLevel());
        }

        [Test]
        public void BestLevel_IsNotLoweredByAWorseRun()
        {
            session.StartNewRun(11);

            for (int level = 0; level < 3; level++)
            {
                SolveCurrentLevel();
                session.ResolveAttempt();
            }

            session.StartNewRun(12);
            SolveCurrentLevel();
            session.ResolveAttempt();

            Assert.AreEqual(1, session.CompletedLevels);
            Assert.AreEqual(3, session.BestLevel);
            Assert.AreEqual(3, storage.LoadBestLevel());
        }

        [Test]
        public void PaletteIndex_NeverRepeatsThePreviousPair()
        {
            session.StartNewRun(2718);

            int previousIndex = session.PaletteIndex;

            for (int level = 0; level < 60; level++)
            {
                SolveCurrentLevel();
                session.ResolveAttempt();

                Assert.AreNotEqual(previousIndex, session.PaletteIndex, $"Level {level} repeated the previous pair.");
                Assert.GreaterOrEqual(session.PaletteIndex, 0);
                Assert.Less(session.PaletteIndex, PaletteVariantCount);

                previousIndex = session.PaletteIndex;
            }
        }

        [Test]
        public void PaletteIndex_IsAlwaysZeroForASinglePair()
        {
            var singlePairSession = new GameSession(
                generator,
                config,
                new Playfield(),
                new InMemoryProgressStorage(),
                1);

            singlePairSession.StartNewRun(5);

            Assert.AreEqual(0, singlePairSession.PaletteIndex);
        }

        [Test]
        public void SameSeed_ReplaysTheSameSequenceOfLevels()
        {
            var firstRun = PlayPerfectly(5150, 40);
            var secondRun = PlayPerfectly(5150, 40);

            CollectionAssert.AreEqual(firstRun, secondRun);
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var firstRun = PlayPerfectly(1, 40);
            var secondRun = PlayPerfectly(2, 40);

            CollectionAssert.AreNotEqual(firstRun, secondRun);
        }

        [Test]
        public void DeriveLevelSeed_ProducesDistinctSeedsAcrossALongRun()
        {
            var seen = new HashSet<int>();

            for (int levelIndex = 0; levelIndex < 5000; levelIndex++)
            {
                int derived = GameSession.DeriveLevelSeed(2024, levelIndex);

                Assert.IsTrue(seen.Add(derived), $"Level {levelIndex} reused an earlier seed.");
            }
        }

        [Test]
        public void DeriveLevelSeed_IsStableForTheSameInputs()
        {
            for (int levelIndex = 0; levelIndex < 100; levelIndex++)
            {
                Assert.AreEqual(
                    GameSession.DeriveLevelSeed(77, levelIndex),
                    GameSession.DeriveLevelSeed(77, levelIndex));
            }
        }

        [Test]
        public void CurrentWallSpeed_GrowsAsLevelsAreCompleted()
        {
            session.StartNewRun(3);

            float startingSpeed = session.CurrentWallSpeed;

            for (int level = 0; level < 25; level++)
            {
                SolveCurrentLevel();
                Assert.AreEqual(MatchResult.PatternMatched, session.ResolveAttempt());
            }

            Assert.Greater(session.CurrentWallSpeed, startingSpeed);
        }

        [Test]
        public void APerfectPlayerCanClearAHundredLevels()
        {
            session.StartNewRun(31337);

            for (int level = 0; level < 100; level++)
            {
                Assert.IsTrue(playfield.HasLevel, $"Level {level} failed to load.");
                Assert.AreEqual(MatchResult.Mismatched, playfield.Evaluate(), $"Level {level} started already solved.");

                SolveCurrentLevel();

                Assert.AreEqual(
                    MatchResult.PatternMatched,
                    session.ResolveAttempt(),
                    $"Level {level} could not be solved.");

                Assert.IsTrue(session.IsRunning);
            }

            Assert.AreEqual(100, session.CompletedLevels);
            Assert.AreEqual(100, session.BestLevel);
        }

        void SolveCurrentLevel()
        {
            var level = playfield.Level;

            Assert.IsTrue(
                playfield.TryMovePieceTo(level.SolutionColumn, level.SolutionRow),
                "The current level has no reachable solution.");
        }

        List<uint> PlayPerfectly(int runSeed, int levelCount)
        {
            var walls = new List<uint>();
            var localPlayfield = new Playfield();

            var localSession = new GameSession(
                generator,
                config,
                localPlayfield,
                new InMemoryProgressStorage(),
                PaletteVariantCount);

            localSession.StartNewRun(runSeed);

            for (int level = 0; level < levelCount; level++)
            {
                walls.Add(localPlayfield.WallMask.Bits);

                var data = localPlayfield.Level;

                Assert.IsTrue(localPlayfield.TryMovePieceTo(data.SolutionColumn, data.SolutionRow));
                Assert.AreEqual(MatchResult.PatternMatched, localSession.ResolveAttempt());
            }

            return walls;
        }
    }
}
