using System;
using PatternGame.Core.States;
using PatternGame.Gameplay.Levels;
using PatternGame.Grid;

namespace PatternGame.Gameplay.Flow
{
    public sealed class ReadyState : ITickableState
    {
        readonly GameFlowContext context;

        float elapsedTime;

        public ReadyState(GameFlowContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Enter()
        {
            elapsedTime = 0f;

            Playfield playfield = context.Session.Playfield;
            LevelData level = playfield.Level;
            int paletteIndex = context.Session.PaletteIndex;

            context.Wall.Prepare(level.WallMask, playfield.TargetMask, paletteIndex);
            context.Piece.Prepare(level.KeyPieceShape, level.SpawnColumn, level.SpawnRow, paletteIndex);
            context.Wall.SetVisible(false);
            context.Piece.SetVisible(false);
            context.Hud.HideGameOver();
            context.Hud.ShowLevel(context.Session.CurrentLevelNumber);
        }

        public void Tick(float deltaTime)
        {
            context.Piece.Tick(deltaTime);

            elapsedTime += deltaTime;

            if (elapsedTime >= context.ReadyDelay)
            {
                context.StateMachine.ChangeState<PlayingState>();
            }
        }

        public void Exit()
        {
        }
    }

    public sealed class PlayingState : ITickableState
    {
        readonly GameFlowContext context;

        public PlayingState(GameFlowContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Enter()
        {
            context.Wall.SetVisible(true);
            context.Piece.SetVisible(true);
            context.Wall.Launch(context.Session.CurrentWallSpeed);
        }

        public void Tick(float deltaTime)
        {
            context.Input.Tick();
            context.Piece.Tick(deltaTime);
            context.Wall.Tick(deltaTime);

            if (context.Wall.HasArrived)
            {
                context.StateMachine.ChangeState<EvaluatingState>();
            }
        }

        public void Exit()
        {
            context.Input.Cancel();
            context.Wall.Stop();
        }
    }

    public sealed class EvaluatingState : IState
    {
        readonly GameFlowContext context;

        public EvaluatingState(GameFlowContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public MatchResult LastResult { get; private set; }

        public void Enter()
        {
            Playfield playfield = context.Session.Playfield;

            GridMask solvedWallMask = playfield.WallMask;
            GridMask solvedTargetMask = playfield.TargetMask;
            int solvedPaletteIndex = context.Session.PaletteIndex;
            int solvedSeed = playfield.Level.Seed;

            LastResult = context.Session.ResolveAttempt();

            if (LastResult == MatchResult.PatternMatched)
            {
                context.Effects.PlayMatch(solvedWallMask, solvedTargetMask, solvedPaletteIndex, solvedSeed);
                context.StateMachine.ChangeState<ReadyState>();
            }
            else
            {
                context.StateMachine.ChangeState<GameOverState>();
            }
        }

        public void Exit()
        {
        }
    }

    public sealed class GameOverState : ITickableState
    {
        readonly GameFlowContext context;

        public GameOverState(GameFlowContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Enter()
        {
            context.Input.Cancel();
            context.Wall.Stop();
            context.Hud.ShowGameOver(context.Session.CompletedLevels, context.Session.BestLevel);
        }

        public void Tick(float deltaTime)
        {
            context.Piece.Tick(deltaTime);
        }

        public void Exit()
        {
        }
    }
}
