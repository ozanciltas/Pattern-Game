using UnityEngine;
using PatternGame.Grid;

namespace PatternGame.Presentation
{
    public sealed class BoardPointer : MonoBehaviour
    {
        [SerializeField]
        Camera targetCamera;

        [SerializeField]
        Transform boardTransform;

        [SerializeField]
        GridDefinition gridDefinition;

        bool hasReportedMissingReferences;

        public bool TryGetLocalPoint(Vector2 screenPosition, out Vector3 localPoint)
        {
            localPoint = Vector3.zero;

            if (!HasRequiredReferences())
            {
                return false;
            }

            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            var boardPlane = new Plane(-boardTransform.forward, boardTransform.position);

            if (!boardPlane.Raycast(ray, out float distanceAlongRay))
            {
                return false;
            }

            Vector3 worldPoint = ray.GetPoint(distanceAlongRay);
            localPoint = boardTransform.InverseTransformPoint(worldPoint);
            return true;
        }

        public bool TryGetCell(Vector2 screenPosition, out int column, out int row)
        {
            column = 0;
            row = 0;

            if (!TryGetLocalPoint(screenPosition, out Vector3 localPoint))
            {
                return false;
            }

            gridDefinition.GetNearestCell(localPoint, out column, out row);
            return true;
        }

        public bool TryGetCellOnBoard(Vector2 screenPosition, out int column, out int row)
        {
            column = 0;
            row = 0;

            if (!TryGetLocalPoint(screenPosition, out Vector3 localPoint))
            {
                return false;
            }

            gridDefinition.GetNearestCellOnBoard(localPoint, out column, out row);
            return true;
        }

        bool HasRequiredReferences()
        {
            if (targetCamera != null && boardTransform != null && gridDefinition != null)
            {
                return true;
            }

            if (!hasReportedMissingReferences)
            {
                hasReportedMissingReferences = true;

                Debug.LogError(
                    $"{name}: Target Camera, Board Transform and Grid Definition must all be assigned.",
                    this);
            }

            return false;
        }

        void OnDrawGizmosSelected()
        {
            if (boardTransform == null || gridDefinition == null)
            {
                return;
            }

            Gizmos.matrix = boardTransform.localToWorldMatrix;
            Gizmos.color = Color.cyan;

            Gizmos.DrawWireCube(
                Vector3.zero,
                new Vector3(gridDefinition.BoardWorldWidth, gridDefinition.BoardWorldHeight, 0f));
        }
    }
}
