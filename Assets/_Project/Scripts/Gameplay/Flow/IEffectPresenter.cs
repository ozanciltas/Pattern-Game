using PatternGame.Grid;

namespace PatternGame.Gameplay.Flow
{
    public interface IEffectPresenter
    {
        void PlayMatch(GridMask wallMask, GridMask targetMask, int paletteIndex, int seed);

        void Clear();
    }
}
