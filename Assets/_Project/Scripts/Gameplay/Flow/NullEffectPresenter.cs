using PatternGame.Grid;

namespace PatternGame.Gameplay.Flow
{
    public sealed class NullEffectPresenter : IEffectPresenter
    {
        public void PlayMatch(GridMask wallMask, GridMask targetMask, int paletteIndex, int seed)
        {
        }

        public void Clear()
        {
        }
    }
}
