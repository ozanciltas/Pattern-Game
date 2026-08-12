namespace PatternGame.Gameplay.Progress
{
    public interface IProgressStorage
    {
        int LoadBestLevel();

        void SaveBestLevel(int bestLevel);
    }
}
