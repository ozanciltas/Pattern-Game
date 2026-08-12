using UnityEngine;
using PatternGame.Gameplay.Flow;
using PatternGame.Grid;

namespace PatternGame.Presentation
{
    public sealed class KeyPieceController : MonoBehaviour, IPiecePresenter
    {
        const float SnapThreshold = 0.0001f;

        [SerializeField]
        MaskView maskView;

        [SerializeField]
        GridDefinition gridDefinition;

        [SerializeField, Min(0f)]
        float followSharpness = 20f;

        ColorPalette palette;
        Vector3 targetLocalPosition;
        int column;
        int row;
        bool isReady;

        public int Column => column;

        public int Row => row;

        public bool IsSettled => (transform.localPosition - targetLocalPosition).sqrMagnitude < SnapThreshold;

        public void Initialize(ColorPalette colorPalette)
        {
            palette = colorPalette;
        }

        public void Prepare(GridMask normalizedShape, int startColumn, int startRow, int paletteIndex)
        {
            if (!EnsureReady())
            {
                return;
            }

            maskView.SetMask(normalizedShape);
            ApplyPalette(paletteIndex);
            SnapTo(startColumn, startRow);
        }

        public void SetVisible(bool isVisible)
        {
            if (!EnsureReady())
            {
                return;
            }

            maskView.SetVisible(isVisible);
        }

        public void MoveTo(int targetColumn, int targetRow)
        {
            if (!EnsureReady())
            {
                return;
            }

            column = targetColumn;
            row = targetRow;
            targetLocalPosition = CalculateLocalPosition(targetColumn, targetRow);
        }

        public void SnapTo(int targetColumn, int targetRow)
        {
            MoveTo(targetColumn, targetRow);

            if (isReady)
            {
                transform.localPosition = targetLocalPosition;
            }
        }

        public void Tick(float deltaTime)
        {
            if (!isReady || deltaTime <= 0f)
            {
                return;
            }

            if (followSharpness <= 0f || IsSettled)
            {
                transform.localPosition = targetLocalPosition;
                return;
            }

            float blend = 1f - Mathf.Exp(-followSharpness * deltaTime);

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, blend);
        }

        public void Clear()
        {
            if (!EnsureReady())
            {
                return;
            }

            maskView.Clear();
        }

        void Awake()
        {
            EnsureReady();
        }

        bool EnsureReady()
        {
            if (isReady)
            {
                return true;
            }

            if (maskView == null)
            {
                Debug.LogError($"{name}: Mask View is not assigned.", this);
                return false;
            }

            if (gridDefinition == null)
            {
                Debug.LogError($"{name}: Grid Definition is not assigned.", this);
                return false;
            }

            isReady = true;
            targetLocalPosition = transform.localPosition;
            return true;
        }

        void ApplyPalette(int paletteIndex)
        {
            if (palette == null || !palette.TryGetPair(paletteIndex, out ColorPair pair))
            {
                return;
            }

            maskView.SetColor(pair.KeyPieceColor);
        }

        Vector3 CalculateLocalPosition(int targetColumn, int targetRow)
        {
            Vector3 anchorOffset = gridDefinition.GetCellLocalPosition(targetColumn, targetRow)
                - gridDefinition.GetCellLocalPosition(0, 0);

            return new Vector3(anchorOffset.x, anchorOffset.y, transform.localPosition.z);
        }

        [ContextMenu("Preview Sample Piece")]
        void PreviewSamplePiece()
        {
            GridMask sample = GridMask.Empty
                .WithCell(0, 0)
                .WithCell(0, 1)
                .WithCell(1, 1);

            Prepare(sample, 0, 0, 0);
        }

        [ContextMenu("Preview Sample Piece At Centre")]
        void PreviewSamplePieceAtCentre()
        {
            GridMask sample = GridMask.Empty
                .WithCell(0, 0)
                .WithCell(0, 1)
                .WithCell(1, 1);

            Prepare(sample, 1, 2, 0);
        }
    }
}
