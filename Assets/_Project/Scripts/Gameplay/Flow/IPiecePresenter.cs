using PatternGame.Grid;

namespace PatternGame.Gameplay.Flow
{
    public interface IPiecePresenter
    {
        void Prepare(GridMask normalizedShape, int column, int row, int paletteIndex);

        void SetVisible(bool isVisible);

        void Tick(float deltaTime);

        void Clear();
    }
}
