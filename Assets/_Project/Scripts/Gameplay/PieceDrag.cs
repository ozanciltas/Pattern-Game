using System;
using UnityEngine;
using PatternGame.Grid;

namespace PatternGame.Gameplay
{
    public sealed class PieceDrag
    {
        readonly Playfield playfield;

        int grabOffsetColumn;
        int grabOffsetRow;
        bool isActive;

        public PieceDrag(Playfield playfield)
        {
            this.playfield = playfield ?? throw new ArgumentNullException(nameof(playfield));
        }

        public bool IsActive => isActive;

        public int GrabOffsetColumn => grabOffsetColumn;

        public int GrabOffsetRow => grabOffsetRow;

        public bool TryBegin(int pointerColumn, int pointerRow)
        {
            isActive = false;

            if (!playfield.HasLevel)
            {
                return false;
            }

            if (!GridMask.IsValidCell(pointerColumn, pointerRow))
            {
                return false;
            }

            if (!playfield.PieceMask.IsOccupied(pointerColumn, pointerRow))
            {
                return false;
            }

            grabOffsetColumn = playfield.PieceColumn - pointerColumn;
            grabOffsetRow = playfield.PieceRow - pointerRow;
            isActive = true;
            return true;
        }

        public void MoveTo(int pointerColumn, int pointerRow)
        {
            if (!isActive)
            {
                return;
            }

            int targetColumn = Mathf.Clamp(
                pointerColumn + grabOffsetColumn,
                playfield.MinPieceColumn,
                playfield.MaxPieceColumn);

            int targetRow = Mathf.Clamp(
                pointerRow + grabOffsetRow,
                playfield.MinPieceRow,
                playfield.MaxPieceRow);

            playfield.TryMovePieceTo(targetColumn, targetRow);
        }

        public void End()
        {
            isActive = false;
        }
    }
}
