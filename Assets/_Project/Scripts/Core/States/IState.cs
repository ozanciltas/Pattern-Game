namespace PatternGame.Core.States
{
    public interface IState
    {
        void Enter();

        void Exit();
    }

    public interface ITickableState : IState
    {
        void Tick(float deltaTime);
    }
}
