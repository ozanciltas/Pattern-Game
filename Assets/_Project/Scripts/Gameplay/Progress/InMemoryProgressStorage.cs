using System;

namespace PatternGame.Gameplay.Progress
{
    public sealed class InMemoryProgressStorage : IProgressStorage
    {
        int bestLevel;

        public InMemoryProgressStorage()
            : this(0)
        {
        }

        public InMemoryProgressStorage(int bestLevel)
        {
            this.bestLevel = Math.Max(0, bestLevel);
        }

        public int LoadBestLevel()
        {
            return bestLevel;
        }

        public void SaveBestLevel(int value)
        {
            bestLevel = Math.Max(0, value);
        }
    }
}
