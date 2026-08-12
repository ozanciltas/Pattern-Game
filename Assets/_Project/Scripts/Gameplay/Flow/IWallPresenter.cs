using PatternGame.Grid;

namespace PatternGame.Gameplay.Flow
{
    public interface IWallPresenter
    {
        bool HasArrived { get; }

        void Prepare(GridMask wallMask, GridMask targetMask, int paletteIndex);

        void SetVisible(bool isVisible);

        void Launch(float speed);

        void Tick(float deltaTime);

        void Stop();

        void Clear();
    }
}
