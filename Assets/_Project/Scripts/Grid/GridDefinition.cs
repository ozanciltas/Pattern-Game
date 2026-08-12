using UnityEngine;

namespace PatternGame.Grid
{
    [CreateAssetMenu(fileName = "GridDefinition", menuName = "Pattern Game/Grid Definition")]
    public sealed class GridDefinition : ScriptableObject
    {
        [SerializeField, Min(0.01f)]
        float cellSize = 1f;

        [SerializeField, Min(0f)]
        float cellSpacing = 0.05f;

        public float CellSize => cellSize;

        public float CellSpacing => cellSpacing;

        public float CellPitch => cellSize + cellSpacing;

        public float BoardWorldWidth => GridMask.Width * cellSize + (GridMask.Width - 1) * cellSpacing;

        public float BoardWorldHeight => GridMask.Height * cellSize + (GridMask.Height - 1) * cellSpacing;

        public Vector3 GetCellLocalPosition(int column, int row)
        {
            return new Vector3(GetColumnOffset(column), GetRowOffset(row), 0f);
        }

        public float GetColumnOffset(int column)
        {
            return (column - (GridMask.Width - 1) * 0.5f) * CellPitch;
        }

        public float GetRowOffset(int row)
        {
            return (row - (GridMask.Height - 1) * 0.5f) * CellPitch;
        }

        public void GetNearestCell(Vector3 localPosition, out int column, out int row)
        {
            column = Mathf.RoundToInt(localPosition.x / CellPitch + (GridMask.Width - 1) * 0.5f);
            row = Mathf.RoundToInt(localPosition.y / CellPitch + (GridMask.Height - 1) * 0.5f);
        }

        public void GetNearestCellOnBoard(Vector3 localPosition, out int column, out int row)
        {
            GetNearestCell(localPosition, out column, out row);

            column = Mathf.Clamp(column, 0, GridMask.Width - 1);
            row = Mathf.Clamp(row, 0, GridMask.Height - 1);
        }

        void OnValidate()
        {
            cellSize = Mathf.Max(0.01f, cellSize);
            cellSpacing = Mathf.Max(0f, cellSpacing);
        }
    }
}
