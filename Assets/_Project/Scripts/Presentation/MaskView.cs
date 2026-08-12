using System.Collections.Generic;
using UnityEngine;
using PatternGame.Grid;

namespace PatternGame.Presentation
{
    public sealed class MaskView : MonoBehaviour
    {
        [SerializeField]
        GridDefinition gridDefinition;

        [SerializeField]
        Transform cellPrefab;

        [SerializeField]
        bool scaleCellsToGrid = true;

        [SerializeField]
        string colorPropertyName = "_BaseColor";

        Transform[] cells;
        Renderer[] cellRenderers;
        MaterialPropertyBlock propertyBlock;
        GridMask currentMask;
        int colorPropertyId;
        bool isBuilt;

        public GridMask CurrentMask => currentMask;

        public bool IsBuilt => isBuilt;

        public void SetMask(GridMask mask)
        {
            EnsureBuilt();

            if (!isBuilt)
            {
                return;
            }

            uint changedBits = currentMask.Bits ^ mask.Bits;

            if (changedBits == 0u)
            {
                return;
            }

            for (int index = 0; index < GridMask.CellCount; index++)
            {
                uint bit = 1u << index;

                if ((changedBits & bit) == 0u)
                {
                    continue;
                }

                cells[index].gameObject.SetActive((mask.Bits & bit) != 0u);
            }

            currentMask = mask;
        }

        public void Clear()
        {
            SetMask(GridMask.Empty);
        }

        public void SetVisible(bool isVisible)
        {
            EnsureBuilt();

            if (!isBuilt)
            {
                return;
            }

            for (int index = 0; index < cellRenderers.Length; index++)
            {
                if (cellRenderers[index] != null)
                {
                    cellRenderers[index].enabled = isVisible;
                }
            }
        }

        public void SetColor(Color color)
        {
            EnsureBuilt();

            if (!isBuilt)
            {
                return;
            }

            propertyBlock.SetColor(colorPropertyId, color);

            for (int index = 0; index < cellRenderers.Length; index++)
            {
                if (cellRenderers[index] != null)
                {
                    cellRenderers[index].SetPropertyBlock(propertyBlock);
                }
            }
        }

        void Awake()
        {
            EnsureBuilt();
        }

        void EnsureBuilt()
        {
            if (isBuilt)
            {
                return;
            }

            if (gridDefinition == null)
            {
                Debug.LogError($"{name}: Grid Definition is not assigned.", this);
                return;
            }

            if (cellPrefab == null)
            {
                Debug.LogError($"{name}: Cell Prefab is not assigned.", this);
                return;
            }

            cells = new Transform[GridMask.CellCount];
            propertyBlock = new MaterialPropertyBlock();
            colorPropertyId = Shader.PropertyToID(colorPropertyName);

            var renderers = new List<Renderer>(GridMask.CellCount);

            for (int index = 0; index < GridMask.CellCount; index++)
            {
                int column = GridMask.ColumnOf(index);
                int row = GridMask.RowOf(index);

                Transform cell = Instantiate(cellPrefab, transform);
                cell.name = $"Cell {column},{row}";
                cell.localPosition = gridDefinition.GetCellLocalPosition(column, row);
                cell.localRotation = Quaternion.identity;

                if (scaleCellsToGrid)
                {
                    float depth = cell.localScale.z;
                    cell.localScale = new Vector3(gridDefinition.CellSize, gridDefinition.CellSize, depth);
                }

                if (!Application.isPlaying)
                {
                    cell.gameObject.hideFlags = HideFlags.DontSave;
                }

                cell.gameObject.SetActive(false);

                cells[index] = cell;
                renderers.AddRange(cell.GetComponentsInChildren<Renderer>(true));
            }

            cellRenderers = renderers.ToArray();
            currentMask = GridMask.Empty;
            isBuilt = true;
        }

        [ContextMenu("Preview Full Board")]
        void PreviewFullBoard()
        {
            SetMask(GridMask.Full);
        }

        [ContextMenu("Preview Empty Board")]
        void PreviewEmptyBoard()
        {
            SetMask(GridMask.Empty);
        }

        [ContextMenu("Destroy Cells")]
        void DestroyCells()
        {
            if (cells != null)
            {
                for (int index = 0; index < cells.Length; index++)
                {
                    if (cells[index] == null)
                    {
                        continue;
                    }

                    if (Application.isPlaying)
                    {
                        Destroy(cells[index].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(cells[index].gameObject);
                    }
                }
            }

            cells = null;
            cellRenderers = null;
            currentMask = GridMask.Empty;
            isBuilt = false;
        }
    }
}
