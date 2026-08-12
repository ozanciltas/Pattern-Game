using UnityEngine;
using PatternGame.Core.Input;
using PatternGame.Gameplay;
using PatternGame.Gameplay.Flow;

namespace PatternGame.Presentation
{
    public sealed class DragController : MonoBehaviour, IPieceInput
    {
        [SerializeField]
        BoardPointer boardPointer;

        [SerializeField]
        KeyPieceController keyPieceController;

        IPointerInput pointerInput;
        Playfield playfield;
        PieceDrag pieceDrag;
        bool isInitialized;
        bool hasReportedMissingSetup;

        public bool IsDragging => pieceDrag != null && pieceDrag.IsActive;

        public void Initialize(IPointerInput input, Playfield targetPlayfield)
        {
            pointerInput = input;
            playfield = targetPlayfield;

            if (pointerInput == null || playfield == null || boardPointer == null || keyPieceController == null)
            {
                Debug.LogError($"{name}: Drag Controller is missing one of its dependencies.", this);
                return;
            }

            pieceDrag = new PieceDrag(playfield);
            isInitialized = true;
        }

        public void Cancel()
        {
            pieceDrag?.End();
        }

        public void Tick()
        {
            if (!IsReady())
            {
                return;
            }

            ProcessPointer();

            if (playfield.HasLevel)
            {
                keyPieceController.MoveTo(playfield.PieceColumn, playfield.PieceRow);
            }
        }

        void ProcessPointer()
        {
            if (pointerInput.PressedThisFrame
                && boardPointer.TryGetCell(pointerInput.ScreenPosition, out int pressColumn, out int pressRow))
            {
                pieceDrag.TryBegin(pressColumn, pressRow);
            }

            if (pieceDrag.IsActive
                && pointerInput.IsPressed
                && boardPointer.TryGetCellOnBoard(pointerInput.ScreenPosition, out int dragColumn, out int dragRow))
            {
                pieceDrag.MoveTo(dragColumn, dragRow);
            }

            if (pointerInput.ReleasedThisFrame)
            {
                pieceDrag.End();
            }
        }

        bool IsReady()
        {
            if (isInitialized)
            {
                return true;
            }

            if (!hasReportedMissingSetup)
            {
                hasReportedMissingSetup = true;
                Debug.LogError($"{name}: Tick was called before Initialize.", this);
            }

            return false;
        }
    }
}
