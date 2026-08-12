using System;
using PatternGame.Core.States;

namespace PatternGame.Gameplay.Flow
{
    public sealed class GameFlow
    {
        readonly StateMachine stateMachine;
        readonly GameFlowContext context;

        public GameFlow(
            GameSession session,
            IWallPresenter wall,
            IPiecePresenter piece,
            IPieceInput input,
            IEffectPresenter effects,
            IHudPresenter hud,
            float readyDelay)
        {
            stateMachine = new StateMachine();
            context = new GameFlowContext(session, wall, piece, input, effects, hud, stateMachine, readyDelay);

            stateMachine.AddState(new ReadyState(context));
            stateMachine.AddState(new PlayingState(context));
            stateMachine.AddState(new EvaluatingState(context));
            stateMachine.AddState(new GameOverState(context));
        }

        public GameSession Session => context.Session;

        public Type CurrentStateType => stateMachine.CurrentStateType;

        public bool IsInState<TState>() where TState : IState
        {
            return stateMachine.IsInState<TState>();
        }

        public void StartRun(int seed)
        {
            context.Session.StartNewRun(seed);
            stateMachine.ChangeState<ReadyState>();
        }

        public void Tick(float deltaTime)
        {
            stateMachine.Tick(deltaTime);
        }

        public void Stop()
        {
            stateMachine.Stop();
            context.Session.EndRun();
            context.Wall.Clear();
            context.Piece.Clear();
            context.Effects.Clear();
            context.Hud.Clear();
        }
    }
}
