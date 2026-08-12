using UnityEngine;
using PatternGame.Grid;

namespace PatternGame.Gameplay.Levels
{
    public readonly struct LevelGenerationSettings
    {
        public const int MinimumPatternCellCount = 1;
        public const int MaximumWallCellCount = GridMask.CellCount;
        public const int MaximumSpawnDistance = GridMask.Width / 2 + GridMask.Height / 2;

        public LevelGenerationSettings(
            int wallCellCount,
            int patternCellCount,
            float compactness,
            int minimumSpawnDistance)
        {
            MinimumSpawnDistance = Mathf.Clamp(minimumSpawnDistance, 0, MaximumSpawnDistance);
            Compactness = Mathf.Clamp01(compactness);
            PatternCellCount = Mathf.Clamp(
                patternCellCount,
                MinimumPatternCellCount,
                MaximumPatternCellCountFor(MinimumSpawnDistance));
            WallCellCount = Mathf.Clamp(wallCellCount, PatternCellCount, MaximumWallCellCount);
        }

        public int WallCellCount { get; }

        public int PatternCellCount { get; }

        public float Compactness { get; }

        public int MinimumSpawnDistance { get; }

        public int ExtraWallCellCount => WallCellCount - PatternCellCount;

        public int RequiredSpawnDistance => Mathf.Max(1, MinimumSpawnDistance);

        public static int MaximumPatternCellCountFor(int minimumSpawnDistance)
        {
            int requiredDistance = Mathf.Clamp(minimumSpawnDistance, 1, MaximumSpawnDistance);
            int largestArea = MinimumPatternCellCount;

            for (int width = 1; width <= GridMask.Width; width++)
            {
                for (int height = 1; height <= GridMask.Height; height++)
                {
                    if (GuaranteedSpawnDistance(width, height) < requiredDistance)
                    {
                        continue;
                    }

                    int area = width * height;

                    if (area > largestArea)
                    {
                        largestArea = area;
                    }
                }
            }

            return largestArea;
        }

        public static int GuaranteedSpawnDistance(int boxWidth, int boxHeight)
        {
            int columnRange = GridMask.Width - boxWidth;
            int rowRange = GridMask.Height - boxHeight;

            return (columnRange + 1) / 2 + (rowRange + 1) / 2;
        }
    }
}
