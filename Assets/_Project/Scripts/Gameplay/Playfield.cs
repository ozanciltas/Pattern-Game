using System;
using PatternGame.Gameplay.Levels;
using PatternGame.Grid;

namespace PatternGame.Gameplay
{
    public enum MatchResult
    {
        PatternMatched,
        Mismatched
    }

    public sealed class Playfield
    {
        LevelData level;
        GridMask targetMask;
        GridMask pieceMask;
        int pieceColumn;
        int pieceRow;
        int minPieceColumn;
        int minPieceRow;
        int maxPieceColumn;
        int maxPieceRow;
        bool hasLevel;

        public bool HasLevel => hasLevel;

        public LevelData Level => level;

        public GridMask WallMask => level.WallMask;

        public GridMask TargetMask => targetMask;

        public GridMask PieceMask => pieceMask;

        public int PieceColumn => pieceColumn;

        public int PieceRow => pieceRow;

        public int MinPieceColumn => minPieceColumn;

        public int MinPieceRow => minPieceRow;

        public int MaxPieceColumn => maxPieceColumn;

        public int MaxPieceRow => maxPieceRow;

        public void Load(in LevelData levelData)
        {
            level = levelData;
            hasLevel = true;

            CachePlacementLimits(levelData.KeyPieceShape);

            if (!levelData.TryGetTargetMask(out targetMask))
            {
                hasLevel = false;

                throw new ArgumentException(
                    $"Solution placement ({levelData.SolutionColumn}, {levelData.SolutionRow}) does not fit the key piece.",
                    nameof(levelData));
            }

            if (!TryMovePieceTo(levelData.SpawnColumn, levelData.SpawnRow))
            {
                hasLevel = false;

                throw new ArgumentException(
                    $"Spawn placement ({levelData.SpawnColumn}, {levelData.SpawnRow}) does not fit the key piece.",
                    nameof(levelData));
            }
        }

        public void Clear()
        {
            hasLevel = false;
            level = default;
            targetMask = GridMask.Empty;
            pieceMask = GridMask.Empty;
            pieceColumn = 0;
            pieceRow = 0;
            minPieceColumn = 0;
            minPieceRow = 0;
            maxPieceColumn = 0;
            maxPieceRow = 0;
        }

        public bool TryMovePieceTo(int column, int row)
        {
            ThrowIfNoLevelIsLoaded();

            if (!level.KeyPieceShape.TryTranslate(column, row, out GridMask movedMask))
            {
                return false;
            }

            pieceMask = movedMask;
            pieceColumn = column;
            pieceRow = row;
            return true;
        }

        public bool TryMovePieceBy(int deltaColumn, int deltaRow)
        {
            return TryMovePieceTo(pieceColumn + deltaColumn, pieceRow + deltaRow);
        }

        public void ResetPieceToSpawn()
        {
            ThrowIfNoLevelIsLoaded();
            TryMovePieceTo(level.SpawnColumn, level.SpawnRow);
        }

        public MatchResult Evaluate()
        {
            ThrowIfNoLevelIsLoaded();

            return pieceMask == targetMask ? MatchResult.PatternMatched : MatchResult.Mismatched;
        }

        void CachePlacementLimits(GridMask shape)
        {
            if (!shape.TryGetBounds(out int minColumn, out int minRow, out int maxColumn, out int maxRow))
            {
                minPieceColumn = 0;
                minPieceRow = 0;
                maxPieceColumn = 0;
                maxPieceRow = 0;
                return;
            }

            minPieceColumn = -minColumn;
            minPieceRow = -minRow;
            maxPieceColumn = GridMask.Width - 1 - maxColumn;
            maxPieceRow = GridMask.Height - 1 - maxRow;
        }

        void ThrowIfNoLevelIsLoaded()
        {
            if (!hasLevel)
            {
                throw new InvalidOperationException("No level is loaded into the playfield.");
            }
        }
    }
}
