using PatternGame.Grid;

namespace PatternGame.Gameplay.Levels
{
    public readonly struct LevelData
    {
        public LevelData(
            GridMask wallMask,
            GridMask keyPieceShape,
            int solutionColumn,
            int solutionRow,
            int spawnColumn,
            int spawnRow,
            int seed)
        {
            WallMask = wallMask;
            KeyPieceShape = keyPieceShape;
            SolutionColumn = solutionColumn;
            SolutionRow = solutionRow;
            SpawnColumn = spawnColumn;
            SpawnRow = spawnRow;
            Seed = seed;
        }

        public GridMask WallMask { get; }

        public GridMask KeyPieceShape { get; }

        public int SolutionColumn { get; }

        public int SolutionRow { get; }

        public int SpawnColumn { get; }

        public int SpawnRow { get; }

        public int Seed { get; }

        public bool TryGetTargetMask(out GridMask targetMask)
        {
            return KeyPieceShape.TryTranslate(SolutionColumn, SolutionRow, out targetMask);
        }

        public bool TryGetSpawnMask(out GridMask spawnMask)
        {
            return KeyPieceShape.TryTranslate(SpawnColumn, SpawnRow, out spawnMask);
        }
    }
}
