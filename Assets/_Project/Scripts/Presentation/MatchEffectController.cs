using System.Collections.Generic;
using UnityEngine;
using PatternGame.Core.Randomness;
using PatternGame.Gameplay.Flow;
using PatternGame.Grid;

namespace PatternGame.Presentation
{
    public sealed class MatchEffectController : MonoBehaviour, IEffectPresenter
    {
        [SerializeField]
        GridDefinition gridDefinition;

        [SerializeField]
        Transform cellSpace;

        [SerializeField]
        ParticleSystem targetBurstPrefab;

        [SerializeField]
        ParticleSystem wallBurstPrefab;

        [SerializeField, Min(0)]
        int wallBurstCount = 2;

        [SerializeField]
        string colorPropertyName = "_BaseColor";

        readonly List<ParticleSystem> liveBursts = new List<ParticleSystem>();
        readonly List<int> candidateCells = new List<int>(GridMask.CellCount);

        ColorPalette palette;
        MaterialPropertyBlock propertyBlock;
        int colorPropertyId;
        bool isReady;

        public void Initialize(ColorPalette colorPalette)
        {
            palette = colorPalette;
        }

        public void PlayMatch(GridMask wallMask, GridMask targetMask, int paletteIndex, int seed)
        {
            if (!EnsureReady() || targetMask.IsEmpty)
            {
                return;
            }

            ColorPair pair = default;
            bool hasPair = palette != null && palette.TryGetPair(paletteIndex, out pair);

            Spawn(targetBurstPrefab, GetCentroid(targetMask), hasPair, pair.KeyPieceColor);

            var random = new DeterministicRandom(seed);

            SpawnWallBursts(wallMask & ~targetMask, hasPair, pair.WallColor, random);
        }

        public void Clear()
        {
            for (int index = 0; index < liveBursts.Count; index++)
            {
                if (liveBursts[index] != null)
                {
                    Destroy(liveBursts[index].gameObject);
                }
            }

            liveBursts.Clear();
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

            if (gridDefinition == null)
            {
                Debug.LogError($"{name}: Grid Definition is not assigned.", this);
                return false;
            }

            if (cellSpace == null)
            {
                Debug.LogError($"{name}: Cell Space is not assigned.", this);
                return false;
            }

            if (targetBurstPrefab == null)
            {
                Debug.LogError($"{name}: Target Burst Prefab is not assigned.", this);
                return false;
            }

            if (wallBurstPrefab == null)
            {
                Debug.LogError($"{name}: Wall Burst Prefab is not assigned.", this);
                return false;
            }

            propertyBlock = new MaterialPropertyBlock();
            colorPropertyId = Shader.PropertyToID(colorPropertyName);
            isReady = true;
            return true;
        }

        void SpawnWallBursts(GridMask mask, bool tint, Color color, DeterministicRandom random)
        {
            CollectCells(mask);

            int burstCount = Mathf.Min(wallBurstCount, candidateCells.Count);

            for (int slot = 0; slot < burstCount; slot++)
            {
                int pick = slot + random.NextInt(candidateCells.Count - slot);
                int cellIndex = candidateCells[pick];

                candidateCells[pick] = candidateCells[slot];
                candidateCells[slot] = cellIndex;

                Spawn(wallBurstPrefab, GetCellWorldPosition(cellIndex), tint, color);
            }
        }

        void Spawn(ParticleSystem prefab, Vector3 worldPosition, bool tint, Color color)
        {
            ParticleSystem burst = Instantiate(prefab, worldPosition, cellSpace.rotation, transform);

            ParticleSystem.MainModule main = burst.main;
            main.stopAction = ParticleSystemStopAction.Destroy;

            if (tint)
            {
                ApplyColor(burst, color);
            }

            PruneFinishedBursts();
            liveBursts.Add(burst);
        }

        void ApplyColor(ParticleSystem burst, Color color)
        {
            propertyBlock.SetColor(colorPropertyId, color);

            Renderer[] renderers = burst.GetComponentsInChildren<Renderer>(true);

            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].SetPropertyBlock(propertyBlock);
            }
        }

        void PruneFinishedBursts()
        {
            for (int index = liveBursts.Count - 1; index >= 0; index--)
            {
                if (liveBursts[index] == null)
                {
                    liveBursts.RemoveAt(index);
                }
            }
        }

        void CollectCells(GridMask mask)
        {
            candidateCells.Clear();

            for (int index = 0; index < GridMask.CellCount; index++)
            {
                if ((mask.Bits & (1u << index)) != 0u)
                {
                    candidateCells.Add(index);
                }
            }
        }

        Vector3 GetCellWorldPosition(int index)
        {
            Vector3 localPosition = gridDefinition.GetCellLocalPosition(
                GridMask.ColumnOf(index),
                GridMask.RowOf(index));

            return cellSpace.TransformPoint(localPosition);
        }

        Vector3 GetCentroid(GridMask mask)
        {
            CollectCells(mask);

            Vector3 sum = Vector3.zero;

            for (int index = 0; index < candidateCells.Count; index++)
            {
                sum += gridDefinition.GetCellLocalPosition(
                    GridMask.ColumnOf(candidateCells[index]),
                    GridMask.RowOf(candidateCells[index]));
            }

            return cellSpace.TransformPoint(sum / candidateCells.Count);
        }
    }
}
