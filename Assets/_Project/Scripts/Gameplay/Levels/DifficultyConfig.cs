using UnityEngine;

namespace PatternGame.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Pattern Game/Difficulty Config")]
    public sealed class DifficultyConfig : ScriptableObject
    {
        const float MinimumWallSpeed = 0.1f;

        [SerializeField, Min(1)]
        int levelsToReachPeakDifficulty = 30;

        [SerializeField, Range(0, LevelGenerationSettings.MaximumSpawnDistance)]
        int minimumSpawnDistance = 2;

        [SerializeField]
        AnimationCurve wallCellCountOverProgress = AnimationCurve.EaseInOut(0f, 6f, 1f, 14f);

        [SerializeField]
        AnimationCurve patternCellCountOverProgress = AnimationCurve.EaseInOut(0f, 2f, 1f, 5f);

        [SerializeField]
        AnimationCurve compactnessOverProgress = AnimationCurve.Linear(0f, 0.85f, 1f, 0.15f);

        [SerializeField]
        AnimationCurve wallSpeedOverProgress = AnimationCurve.EaseInOut(0f, 3f, 1f, 9f);

        public int MinimumSpawnDistance => minimumSpawnDistance;

        public int LargestReachablePatternCellCount =>
            LevelGenerationSettings.MaximumPatternCellCountFor(minimumSpawnDistance);

        public float GetProgress(int levelIndex)
        {
            if (levelsToReachPeakDifficulty <= 1)
            {
                return 1f;
            }

            return Mathf.Clamp01(levelIndex / (float)levelsToReachPeakDifficulty);
        }

        public LevelGenerationSettings GetSettings(int levelIndex)
        {
            float progress = GetProgress(levelIndex);

            int wallCellCount = Mathf.RoundToInt(wallCellCountOverProgress.Evaluate(progress));
            int patternCellCount = Mathf.RoundToInt(patternCellCountOverProgress.Evaluate(progress));
            float compactness = compactnessOverProgress.Evaluate(progress);

            return new LevelGenerationSettings(
                wallCellCount,
                patternCellCount,
                compactness,
                minimumSpawnDistance);
        }

        public float GetWallSpeed(int levelIndex)
        {
            return Mathf.Max(MinimumWallSpeed, wallSpeedOverProgress.Evaluate(GetProgress(levelIndex)));
        }

        void OnValidate()
        {
            levelsToReachPeakDifficulty = Mathf.Max(1, levelsToReachPeakDifficulty);
            minimumSpawnDistance = Mathf.Clamp(minimumSpawnDistance, 0, LevelGenerationSettings.MaximumSpawnDistance);
        }
    }
}
