namespace PatternGame.Gameplay.Flow
{
    public interface IHudPresenter
    {
        void ShowLevel(int levelNumber);

        void ShowGameOver(int completedLevels, int bestLevel);

        void HideGameOver();

        void Clear();
    }
}
