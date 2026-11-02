using System.Collections.Generic;
using RecyclableScroller.Abstractions;
using UnityEngine.UIElements;

namespace RecyclableScroller.UIToolkit.Adapters
{
    /// <summary>
    /// UIToolkit cell pooling using VisualElement.Instantiate().
    /// Manages cell lifecycle (creation, pooling, deactivation).
    /// </summary>
    public class UIToolkitCellContainer : ICellContainer
    {
        private readonly VisualTreeAsset cellAsset;
        private readonly VisualTreeAsset headerAsset;
        private readonly VisualElement container;

        private readonly Queue<VisualElement> pooledCells = new();
        private readonly Queue<VisualElement> pooledHeaders = new();
        private readonly List<VisualElement> activeCells = new();

        private int initialPoolSize;
        private int initialHeaderPoolSize;

        public int TotalCellCount => activeCells.Count + pooledCells.Count + pooledHeaders.Count;
        public int PooledCellCount => pooledCells.Count + pooledHeaders.Count;

        public UIToolkitCellContainer(
            VisualTreeAsset cellAsset,
            VisualElement container,
            int poolSize,
            VisualTreeAsset headerAsset = null,
            int headerPoolSize = 0)
        {
            this.cellAsset = cellAsset;
            this.headerAsset = headerAsset;
            this.container = container;
            this.initialPoolSize = poolSize;
            this.initialHeaderPoolSize = headerPoolSize;
        }

        public object GetOrCreateCell(bool isHeader)
        {
            Queue<VisualElement> pool = isHeader ? pooledHeaders : pooledCells;

            // Try to get from pool
            if (pool.Count > 0)
            {
                VisualElement cell = pool.Dequeue();
                cell.style.display = DisplayStyle.Flex;
                activeCells.Add(cell);
                return cell;
            }

            // Create new
            VisualTreeAsset asset = isHeader ? headerAsset : cellAsset;
            if (asset == null)
                return null;

            VisualElement newCell = asset.Instantiate();
            container.Add(newCell);
            activeCells.Add(newCell);

            return newCell;
        }

        public void ReturnCell(object cell)
        {
            if (cell is VisualElement ve)
            {
                ve.style.display = DisplayStyle.None;
                activeCells.Remove(ve);

                // Return to appropriate pool
                // (Simplified: we'll check by class name, more robust approach would track metadata)
                if (ve.GetType().Name.Contains("Header") || headerAsset != null)
                    pooledHeaders.Enqueue(ve);
                else
                    pooledCells.Enqueue(ve);
            }
        }

        public void DestroyAllCells()
        {
            foreach (var cell in activeCells)
                cell.RemoveFromHierarchy();

            foreach (var cell in pooledCells)
                cell.RemoveFromHierarchy();

            foreach (var cell in pooledHeaders)
                cell.RemoveFromHierarchy();

            activeCells.Clear();
            pooledCells.Clear();
            pooledHeaders.Clear();
        }

        public void Prewarm()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                object cell = GetOrCreateCell(false);
                if (cell != null)
                    ReturnCell(cell);
            }

            if (initialHeaderPoolSize > 0)
            {
                for (int i = 0; i < initialHeaderPoolSize; i++)
                {
                    object cell = GetOrCreateCell(true);
                    if (cell != null)
                        ReturnCell(cell);
                }
            }
        }

        public void Cleanup()
        {
            DestroyAllCells();
        }
    }
}

