using NUnit.Framework;
using UnityEngine;
using PatternGame.Gameplay;
using PatternGame.Gameplay.Flow;
using PatternGame.Gameplay.Levels;
using PatternGame.Gameplay.Progress;
using PatternGame.Grid;

namespace PatternGame.Tests.EditMode
{
    [TestFixture]
    public sealed class GameFlowTests
    {
        const float ReadyDelay = 0.5f;
        const float FrameTime = 1f / 60f;
        const int PaletteVariantCount = 4;

        DifficultyConfig config;
        Playfield playfield;
        InMemoryProgressStorage storage;
        GameSession session;
        FakeWall wall;
        FakePiece piece;
        FakeInput input;
        FakeEffects effects;
        FakeHud hud;
        GameFlow flow;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<DifficultyConfig>();
            playfield = new Playfield();
            storage = new InMemoryProgressStorage();
            session = new GameSession(new LevelGenerator(), config, playfield, storage, PaletteVariantCount);
            wall = new FakeWall();
            piece = new FakePiece();
            input = new FakeInput();
            effects = new FakeEffects();
            hud = new FakeHud();
            flow = new GameFlow(session, wall, piece, input, effects, hud, ReadyDelay);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void StartRun_EntersReadyAndPresentsTheLevel()
        {
            flow.StartRun(101);

            Assert.IsTrue(flow.IsInState<ReadyState>());
            Assert.AreEqual(1, wall.PrepareCount);
            Assert.AreEqual(1, piece.PrepareCount);
            Assert.AreEqual(playfield.WallMask, wall.PreparedWallMask);
            Assert.AreEqual(playfield.TargetMask, wall.PreparedTargetMask);
            Assert.AreEqual(playfield.Level.KeyPieceShape, piece.PreparedShape);
            Assert.AreEqual(playfield.Level.SpawnColumn, piece.PreparedColumn);
            Assert.AreEqual(session.PaletteIndex, wall.PreparedPaletteIndex);
            Assert.AreEqual(session.PaletteIndex, piece.PreparedPaletteIndex);
        }

        [Test]
        public void ReadyState_HidesTheBoardUntilTheWallIsLaunched()
        {
            flow.StartRun(101);

            Assert.IsFalse(wall.IsVisible, "The wall must not be visible while the level is being presented.");
            Assert.IsFalse(piece.IsVisible);

            flow.Tick(ReadyDelay * 0.5f);

            Assert.IsFalse(wall.IsVisible);
            Assert.IsFalse(piece.IsVisible);

            flow.Tick(ReadyDelay);

            Assert.IsTrue(flow.IsInState<PlayingState>());
            Assert.IsTrue(wall.IsVisible, "Playing must reveal the wall.");
            Assert.IsTrue(piece.IsVisible);
        }

        [Test]
        public void ReadyState_HidesTheBoardOnlyAfterPreparingIt()
        {
            flow.StartRun(101);

            Assert.AreEqual(1, wall.PrepareCount);
            Assert.IsTrue(
                wall.WasPreparedBeforeBeingHidden,
                "Hiding before Prepare would leave the freshly built cells visible.");
        }

        [Test]
        public void ReadyState_DoesNotLaunchTheWallBeforeTheDelayElapses()
        {
            flow.StartRun(101);
            flow.Tick(ReadyDelay * 0.5f);

            Assert.IsTrue(flow.IsInState<ReadyState>());
            Assert.AreEqual(0, wall.LaunchCount);
        }

        [Test]
        public void ReadyState_MovesToPlayingAndLaunchesAtTheSessionSpeed()
        {
            flow.StartRun(101);

            float expectedSpeed = session.CurrentWallSpeed;

            flow.Tick(ReadyDelay);

            Assert.IsTrue(flow.IsInState<PlayingState>());
            Assert.AreEqual(1, wall.LaunchCount);
            Assert.AreEqual(expectedSpeed, wall.LaunchedSpeed);
        }

        [Test]
        public void ReadyState_ShowsTheCurrentLevelNumber()
        {
            flow.StartRun(101);

            Assert.AreEqual(1, hud.ShownLevelNumber);
            Assert.AreEqual(1, hud.HideGameOverCount);

            SolveCurrentLevel();
            EnterPlayingFromReady();

            wall.HasArrived = true;
            flow.Tick(FrameTime);

            Assert.AreEqual(2, hud.ShownLevelNumber, "The next level must be announced.");
        }

        [Test]
        public void PlayingState_TicksInputWallAndPiece()
        {
            EnterPlaying();

            wall.TickCount = 0;
            piece.TickCount = 0;
            input.TickCount = 0;

            flow.Tick(FrameTime);

            Assert.AreEqual(1, input.TickCount);
            Assert.AreEqual(1, wall.TickCount);
            Assert.AreEqual(1, piece.TickCount);
        }

        [Test]
        public void InputIsOnlyTickedWhilePlaying()
        {
            flow.StartRun(101);
            flow.Tick(ReadyDelay * 0.5f);

            Assert.AreEqual(0, input.TickCount, "Ready state must not accept input.");

            flow.Tick(ReadyDelay);
            flow.Tick(FrameTime);

            Assert.Greater(input.TickCount, 0);

            wall.HasArrived = true;
            flow.Tick(FrameTime);

            Assert.IsTrue(flow.IsInState<GameOverState>());

            int ticksWhenRunEnded = input.TickCount;

            flow.Tick(FrameTime);
            flow.Tick(FrameTime);

            Assert.AreEqual(ticksWhenRunEnded, input.TickCount, "Game over must not accept input.");
        }

        [Test]
        public void TheLastInputBeforeImpactStillCounts()
        {
            EnterPlaying();

            input.OnTick = SolveCurrentLevel;
            wall.HasArrived = true;

            flow.Tick(FrameTime);

            Assert.IsTrue(flow.IsInState<ReadyState>(), "Input must be processed before the wall closes in.");
            Assert.AreEqual(1, session.CompletedLevels);
        }

        [Test]
        public void LeavingPlayingCancelsTheDragAndStopsTheWall()
        {
            EnterPlaying();

            wall.HasArrived = true;
            flow.Tick(FrameTime);

            Assert.GreaterOrEqual(input.CancelCount, 1);
            Assert.GreaterOrEqual(wall.StopCount, 1);
        }

        [Test]
        public void AFailedAttemptEndsTheRun()
        {
            EnterPlaying();

            wall.HasArrived = true;
            flow.Tick(FrameTime);

            Assert.IsTrue(flow.IsInState<GameOverState>());
            Assert.IsFalse(session.IsRunning);
            Assert.AreEqual(0, session.CompletedLevels);
            Assert.IsTrue(playfield.HasLevel, "The failed board must stay on screen.");
        }

        [Test]
        public void AFailedAttemptLeavesTheBoardVisibleAndReportsTheScore()
        {
            EnterPlaying();
            SolveCurrentLevel();

            wall.HasArrived = true;
            flow.Tick(FrameTime);
            wall.HasArrived = false;

            EnterPlayingFromReady();

            wall.HasArrived = true;
            flow.Tick(FrameTime);

            Assert.IsTrue(flow.IsInState<GameOverState>());
            Assert.IsTrue(wall.IsVisible, "The player must be able to see the board they failed on.");
            Assert.IsTrue(piece.IsVisible);
            Assert.AreEqual(1, hud.GameOverCount);
            Assert.AreEqual(1, hud.ShownCompletedLevels);
            Assert.AreEqual(1, hud.ShownBestLevel);
        }

        [Test]
        public void ASolvedAttemptPlaysTheMatchEffectForThatBoard()
        {
            EnterPlaying();

            GridMask solvedWall = playfield.WallMask;
            GridMask solvedTarget = playfield.TargetMask;
            int solvedSeed = playfield.Level.Seed;
            int solvedPaletteIndex = session.PaletteIndex;

            SolveCurrentLevel();

            wall.HasArrived = true;
            flow.Tick(FrameTime);

            Assert.AreEqual(1, effects.PlayCount);
            Assert.AreEqual(solvedWall, effects.LastWallMask, "The effect must use the board that was solved.");
            Assert.AreEqual(solvedTarget, effects.LastTargetMask);
            Assert.AreEqual(solvedSeed, effects.LastSeed);
            Assert.AreEqual(solvedPaletteIndex, effects.LastPaletteIndex);
        }

        [Test]
        public void AFailedAttemptPlaysNoMatchEffect()
        {
            EnterPlaying();

            wall.HasArrived = true;
            flow.Tick(FrameTime);

            Assert.AreEqual(0, effects.PlayCount);
        }

        [Test]
        public void ASolvedAttemptAdvancesToTheNextLevelInASingleFrame()
        {
            EnterPlaying();

            GridMask firstWall = playfield.WallMask;
            SolveCurrentLevel();

            wall.HasArrived = true;
            flow.Tick(FrameTime);

            Assert.IsTrue(flow.IsInState<ReadyState>(), "A solved level should land back in Ready.");
            Assert.AreEqual(1, session.CompletedLevels);
            Assert.AreEqual(2, wall.PrepareCount, "The next level must be presented.");
            Assert.AreNotEqual(firstWall, wall.PreparedWallMask, "The next level must be a different wall.");
        }

        [Test]
        public void TheWallIsRelaunchedForEachNewLevel()
        {
            EnterPlaying();
            SolveCurrentLevel();

            wall.HasArrived = true;
            flow.Tick(FrameTime);
            wall.HasArrived = false;

            Assert.AreEqual(1, wall.LaunchCount);

            flow.Tick(ReadyDelay);

            Assert.IsTrue(flow.IsInState<PlayingState>());
            Assert.AreEqual(2, wall.LaunchCount);
        }

        [Test]
        public void ARunCanBeRestartedFromGameOver()
        {
            EnterPlaying();

            wall.HasArrived = true;
            flow.Tick(FrameTime);

            Assert.IsTrue(flow.IsInState<GameOverState>());

            wall.HasArrived = false;
            flow.StartRun(999);

            Assert.IsTrue(flow.IsInState<ReadyState>());
            Assert.IsTrue(session.IsRunning);
            Assert.AreEqual(0, session.CompletedLevels);
            Assert.AreEqual(999, session.Seed);
            Assert.AreEqual(2, hud.HideGameOverCount, "Restarting must take the game over screen down.");
        }

        [Test]
        public void Stop_ClearsEveryPresenter()
        {
            EnterPlaying();

            flow.Stop();

            Assert.AreEqual(1, wall.ClearCount);
            Assert.AreEqual(1, piece.ClearCount);
            Assert.AreEqual(1, effects.ClearCount);
            Assert.AreEqual(1, hud.ClearCount);
            Assert.IsFalse(session.IsRunning);
            Assert.IsFalse(playfield.HasLevel);
        }

        [Test]
        public void APerfectPlayerClearsTenLevelsThroughTheFlow()
        {
            flow.StartRun(4242);

            for (int level = 0; level < 10; level++)
            {
                while (!flow.IsInState<PlayingState>())
                {
                    flow.Tick(FrameTime);
                }

                SolveCurrentLevel();

                wall.HasArrived = true;
                flow.Tick(FrameTime);
                wall.HasArrived = false;

                Assert.IsTrue(flow.IsInState<ReadyState>(), $"Level {level} did not advance.");
                Assert.AreEqual(level + 1, session.CompletedLevels);
            }

            Assert.IsTrue(session.IsRunning);
            Assert.AreEqual(10, session.BestLevel);
            Assert.AreEqual(10, storage.LoadBestLevel());
        }

        void EnterPlaying()
        {
            flow.StartRun(101);
            flow.Tick(ReadyDelay);

            Assert.IsTrue(flow.IsInState<PlayingState>());
        }

        void EnterPlayingFromReady()
        {
            flow.Tick(ReadyDelay);

            Assert.IsTrue(flow.IsInState<PlayingState>());
        }

        void SolveCurrentLevel()
        {
            LevelData level = playfield.Level;

            Assert.IsTrue(playfield.TryMovePieceTo(level.SolutionColumn, level.SolutionRow));
        }

        sealed class FakeWall : IWallPresenter
        {
            public GridMask PreparedWallMask;
            public GridMask PreparedTargetMask;
            public int PreparedPaletteIndex;
            public float LaunchedSpeed;
            public int PrepareCount;
            public int LaunchCount;
            public int TickCount;
            public int StopCount;
            public int ClearCount;
            public bool IsVisible = true;
            public bool WasPreparedBeforeBeingHidden;

            public bool HasArrived { get; set; }

            public void Prepare(GridMask wallMask, GridMask targetMask, int paletteIndex)
            {
                PreparedWallMask = wallMask;
                PreparedTargetMask = targetMask;
                PreparedPaletteIndex = paletteIndex;
                PrepareCount++;
                HasArrived = false;
            }

            public void SetVisible(bool isVisible)
            {
                if (!isVisible)
                {
                    WasPreparedBeforeBeingHidden = PrepareCount > 0;
                }

                IsVisible = isVisible;
            }

            public void Launch(float speed)
            {
                LaunchedSpeed = speed;
                LaunchCount++;
            }

            public void Tick(float deltaTime)
            {
                TickCount++;
            }

            public void Stop()
            {
                StopCount++;
            }

            public void Clear()
            {
                ClearCount++;
            }
        }

        sealed class FakePiece : IPiecePresenter
        {
            public GridMask PreparedShape;
            public int PreparedColumn;
            public int PreparedRow;
            public int PreparedPaletteIndex;
            public int PrepareCount;
            public int TickCount;
            public int ClearCount;
            public bool IsVisible = true;

            public void Prepare(GridMask normalizedShape, int column, int row, int paletteIndex)
            {
                PreparedShape = normalizedShape;
                PreparedColumn = column;
                PreparedRow = row;
                PreparedPaletteIndex = paletteIndex;
                PrepareCount++;
            }

            public void SetVisible(bool isVisible)
            {
                IsVisible = isVisible;
            }

            public void Tick(float deltaTime)
            {
                TickCount++;
            }

            public void Clear()
            {
                ClearCount++;
            }
        }

        sealed class FakeInput : IPieceInput
        {
            public System.Action OnTick;
            public int TickCount;
            public int CancelCount;

            public void Tick()
            {
                TickCount++;
                OnTick?.Invoke();
            }

            public void Cancel()
            {
                CancelCount++;
            }
        }

        sealed class FakeEffects : IEffectPresenter
        {
            public GridMask LastWallMask;
            public GridMask LastTargetMask;
            public int LastPaletteIndex;
            public int LastSeed;
            public int PlayCount;
            public int ClearCount;

            public void PlayMatch(GridMask wallMask, GridMask targetMask, int paletteIndex, int seed)
            {
                LastWallMask = wallMask;
                LastTargetMask = targetMask;
                LastPaletteIndex = paletteIndex;
                LastSeed = seed;
                PlayCount++;
            }

            public void Clear()
            {
                ClearCount++;
            }
        }

        sealed class FakeHud : IHudPresenter
        {
            public int ShownLevelNumber;
            public int ShownCompletedLevels;
            public int ShownBestLevel;
            public int GameOverCount;
            public int HideGameOverCount;
            public int ClearCount;

            public void ShowLevel(int levelNumber)
            {
                ShownLevelNumber = levelNumber;
            }

            public void ShowGameOver(int completedLevels, int bestLevel)
            {
                ShownCompletedLevels = completedLevels;
                ShownBestLevel = bestLevel;
                GameOverCount++;
            }

            public void HideGameOver()
            {
                HideGameOverCount++;
            }

            public void Clear()
            {
                ClearCount++;
            }
        }
    }
}
