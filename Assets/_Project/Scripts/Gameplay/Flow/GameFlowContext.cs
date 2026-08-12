using System;
using PatternGame.Core.States;

namespace PatternGame.Gameplay.Flow
{
    public sealed class GameFlowContext
    {
        public GameFlowContext(
            GameSession session,
            IWallPresenter wall,
            IPiecePresenter piece,
            IPieceInput input,
            IEffectPresenter effects,
            IHudPresenter hud,
            StateMachine stateMachine,
            float readyDelay)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Wall = wall ?? throw new ArgumentNullException(nameof(wall));
            Piece = piece ?? throw new ArgumentNullException(nameof(piece));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Hud = hud ?? throw new ArgumentNullException(nameof(hud));
            StateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            ReadyDelay = Math.Max(0f, readyDelay);
        }

        public GameSession Session { get; }

        public IWallPresenter Wall { get; }

        public IPiecePresenter Piece { get; }

        public IPieceInput Input { get; }

        public IEffectPresenter Effects { get; }

        public IHudPresenter Hud { get; }

        public StateMachine StateMachine { get; }

        public float ReadyDelay { get; }
    }
}
