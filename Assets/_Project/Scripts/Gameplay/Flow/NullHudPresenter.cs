namespace PatternGame.Gameplay.Flow
{
    public sealed class NullHudPresenter : IHudPresenter
    {
        public void ShowLevel(int levelNumber)
        {
        }

        public void ShowGameOver(int completedLevels, int bestLevel)
        {
        }

        public void HideGameOver()
        {
        }

        public void Clear()
        {
        }
    }
}
