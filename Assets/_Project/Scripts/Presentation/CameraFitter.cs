using UnityEngine;
using PatternGame.Grid;

namespace PatternGame.Presentation
{
    [ExecuteAlways]
    public sealed class CameraFitter : MonoBehaviour
    {
        [SerializeField]
        Camera targetCamera;

        [SerializeField]
        Transform boardTransform;

        [SerializeField]
        GridDefinition gridDefinition;

        [SerializeField, Min(1f)]
        float margin = 1.1f;

        [SerializeField]
        bool alignRotationToBoard = true;

        bool hasReportedMissingReferences;

        public void Fit()
        {
            if (!HasRequiredReferences())
            {
                return;
            }

            float width = gridDefinition.BoardWorldWidth * margin;
            float height = gridDefinition.BoardWorldHeight * margin;
            float aspect = targetCamera.aspect;

            if (aspect <= 0f)
            {
                return;
            }

            if (alignRotationToBoard)
            {
                targetCamera.transform.rotation = boardTransform.rotation;
            }

            if (targetCamera.orthographic)
            {
                targetCamera.orthographicSize = Mathf.Max(height * 0.5f, width * 0.5f / aspect);
                return;
            }

            targetCamera.transform.position =
                boardTransform.position - targetCamera.transform.forward * DistanceThatFits(width, height, aspect);
        }

        float DistanceThatFits(float width, float height, float aspect)
        {
            float verticalTangent = Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float horizontalTangent = verticalTangent * aspect;

            return Mathf.Max(height * 0.5f / verticalTangent, width * 0.5f / horizontalTangent);
        }

        void OnEnable()
        {
            Fit();
        }

        void OnValidate()
        {
            margin = Mathf.Max(1f, margin);
            Fit();
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

        [ContextMenu("Fit Now")]
        void FitNow()
        {
            Fit();
        }
    }
}
