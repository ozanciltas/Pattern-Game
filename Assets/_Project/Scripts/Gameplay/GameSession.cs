using System;
using PatternGame.Gameplay.Levels;
using PatternGame.Gameplay.Progress;

namespace PatternGame.Gameplay
{
    public sealed class GameSession
    {
        readonly LevelGenerator levelGenerator;
        readonly DifficultyConfig difficultyConfig;
        readonly Playfield playfield;
        readonly IProgressStorage progressStorage;
        readonly int paletteVariantCount;

        LevelGenerationSettings currentSettings;
        int seed;
        int levelIndex;
        int bestLevel;
        int paletteIndex;
        bool hasPaletteIndex;
        bool isRunning;

        public GameSession(
            LevelGenerator levelGenerator,
            DifficultyConfig difficultyConfig,
            Playfield playfield,
            IProgressStorage progressStorage,
            int paletteVariantCount)
        {
            if (levelGenerator == null)
            {
                throw new ArgumentNullException(nameof(levelGenerator));
            }

            if (difficultyConfig == null)
            {
                throw new ArgumentNullException(nameof(difficultyConfig));
            }

            if (playfield == null)
            {
                throw new ArgumentNullException(nameof(playfield));
            }

            if (progressStorage == null)
            {
                throw new ArgumentNullException(nameof(progressStorage));
            }

            this.levelGenerator = levelGenerator;
            this.difficultyConfig = difficultyConfig;
            this.playfield = playfield;
            this.progressStorage = progressStorage;
            this.paletteVariantCount = Math.Max(1, paletteVariantCount);

            bestLevel = Math.Max(0, progressStorage.LoadBestLevel());
        }

        public Playfield Playfield => playfield;

        public int Seed => seed;

        public int LevelIndex => levelIndex;

        public int CompletedLevels => levelIndex;

        public int CurrentLevelNumber => levelIndex + 1;

        public int BestLevel => bestLevel;

        public int PaletteIndex => paletteIndex;

        public int PaletteVariantCount => paletteVariantCount;

        public bool IsRunning => isRunning;

        public LevelGenerationSettings CurrentSettings => currentSettings;

        public float CurrentWallSpeed => difficultyConfig.GetWallSpeed(levelIndex);

        public static int DeriveLevelSeed(int runSeed, int levelIndex)
        {
            unchecked
            {
                uint hash = (uint)runSeed + 0x9E3779B9u * (uint)(levelIndex + 1);
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                hash *= 0x846CA68Bu;
                hash ^= hash >> 16;
                return (int)hash;
            }
        }

        public void StartNewRun(int runSeed)
        {
            seed = runSeed;
            levelIndex = 0;
            paletteIndex = 0;
            hasPaletteIndex = false;
            isRunning = true;

            LoadCurrentLevel();
        }

        public MatchResult ResolveAttempt()
        {
            if (!isRunning)
            {
                throw new InvalidOperationException("No run is in progress.");
            }

            MatchResult result = playfield.Evaluate();

            if (result == MatchResult.PatternMatched)
            {
                levelIndex++;
                RecordBestLevel();
                LoadCurrentLevel();
            }
            else
            {
                isRunning = false;
            }

            return result;
        }

        public void EndRun()
        {
            isRunning = false;
            playfield.Clear();
        }

        void RecordBestLevel()
        {
            if (levelIndex <= bestLevel)
            {
                return;
            }

            bestLevel = levelIndex;
            progressStorage.SaveBestLevel(bestLevel);
        }

        void LoadCurrentLevel()
        {
            currentSettings = difficultyConfig.GetSettings(levelIndex);

            int levelSeed = DeriveLevelSeed(seed, levelIndex);
            LevelData level = levelGenerator.Generate(levelSeed, currentSettings);

            playfield.Load(level);

            paletteIndex = ChoosePaletteIndex(levelSeed);
            hasPaletteIndex = true;
        }

        int ChoosePaletteIndex(int levelSeed)
        {
            if (paletteVariantCount <= 1)
            {
                return 0;
            }

            if (!hasPaletteIndex)
            {
                return (int)((uint)levelSeed % (uint)paletteVariantCount);
            }

            int candidate = (int)((uint)levelSeed % (uint)(paletteVariantCount - 1));

            return candidate >= paletteIndex ? candidate + 1 : candidate;
        }
    }
}
