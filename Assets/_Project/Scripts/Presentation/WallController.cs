using UnityEngine;
using PatternGame.Gameplay;
using PatternGame.Gameplay.Flow;
using PatternGame.Grid;

namespace PatternGame.Presentation
{
    public sealed class WallController : MonoBehaviour, IWallPresenter
    {
        [SerializeField]
        MaskView bodyMaskView;

        [SerializeField]
        MaskView targetMaskView;

        [SerializeField, Min(0.1f)]
        float spawnDistance = 12f;

        [SerializeField]
        float arrivalDistance;

        WallApproach approach;
        ColorPalette palette;

        public bool IsMoving => approach != null && approach.IsMoving;

        public bool HasArrived => approach != null && approach.HasArrived;

        public float NormalizedProgress => approach?.NormalizedProgress ?? 0f;

        public float Distance => approach?.Distance ?? spawnDistance;

        public void Initialize(ColorPalette colorPalette)
        {
            palette = colorPalette;
        }

        public void Prepare(GridMask wallMask, GridMask targetMask, int paletteIndex)
        {
            if (!EnsureReady())
            {
                return;
            }

            bodyMaskView.SetMask(wallMask & ~targetMask);
            targetMaskView.SetMask(targetMask);
            ApplyPalette(paletteIndex);
            approach.Reset();
            ApplyDistanceToTransform();
        }

        public void SetVisible(bool isVisible)
        {
            if (!EnsureReady())
            {
                return;
            }

            bodyMaskView.SetVisible(isVisible);
            targetMaskView.SetVisible(isVisible);
        }

        public void Launch(float speed)
        {
            if (!EnsureReady())
            {
                return;
            }

            approach.Launch(speed);
        }

        public void Tick(float deltaTime)
        {
            if (approach == null || !approach.IsMoving)
            {
                return;
            }

            approach.Tick(deltaTime);
            ApplyDistanceToTransform();
        }

        public void Stop()
        {
            approach?.Stop();
        }

        public void Clear()
        {
            if (!EnsureReady())
            {
                return;
            }

            bodyMaskView.Clear();
            targetMaskView.Clear();
            approach.Reset();
            ApplyDistanceToTransform();
        }

        void Awake()
        {
            EnsureReady();
        }

        bool EnsureReady()
        {
            if (approach != null)
            {
                return true;
            }

            if (bodyMaskView == null)
            {
                Debug.LogError($"{name}: Body Mask View is not assigned.", this);
                return false;
            }

            if (targetMaskView == null)
            {
                Debug.LogError($"{name}: Target Mask View is not assigned.", this);
                return false;
            }

            if (spawnDistance <= arrivalDistance)
            {
                Debug.LogError(
                    $"{name}: Spawn Distance ({spawnDistance}) must be greater than Arrival Distance ({arrivalDistance}).",
                    this);
                return false;
            }

            approach = new WallApproach(spawnDistance, arrivalDistance);
            ApplyDistanceToTransform();
            return true;
        }

        void ApplyPalette(int paletteIndex)
        {
            if (palette == null || !palette.TryGetPair(paletteIndex, out ColorPair pair))
            {
                return;
            }

            bodyMaskView.SetColor(pair.WallColor);
            targetMaskView.SetColor(pair.KeyPieceColor);
        }

        void ApplyDistanceToTransform()
        {
            Vector3 localPosition = transform.localPosition;
            localPosition.z = approach.Distance;
            transform.localPosition = localPosition;
        }

        void OnValidate()
        {
            if (spawnDistance <= arrivalDistance)
            {
                spawnDistance = arrivalDistance + 1f;
            }
        }

        [ContextMenu("Preview Sample Wall")]
        void PreviewSampleWall()
        {
            GridMask targetMask = GridMask.Empty.WithCell(1, 1).WithCell(1, 2).WithCell(2, 2);

            GridMask wallMask = targetMask
                .WithCell(0, 0)
                .WithCell(3, 0)
                .WithCell(0, 2)
                .WithCell(2, 3)
                .WithCell(3, 3)
                .WithCell(1, 4);

            Prepare(wallMask, targetMask, 0);
        }

        [ContextMenu("Move To Spawn Distance")]
        void MoveToSpawnDistance()
        {
            if (!EnsureReady())
            {
                return;
            }

            approach.Reset();
            ApplyDistanceToTransform();
        }

        [ContextMenu("Move To Arrival Distance")]
        void MoveToArrivalDistance()
        {
            if (!EnsureReady())
            {
                return;
            }

            approach.Reset();
            approach.Launch(1f);
            approach.Tick(approach.SpawnDistance - approach.ArrivalDistance);
            ApplyDistanceToTransform();
        }
    }
}
