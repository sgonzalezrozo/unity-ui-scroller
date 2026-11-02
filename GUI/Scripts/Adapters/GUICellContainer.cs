using Tools.Core;
using Tools.RecyclableScroller.Abstractions.Scripts;
using Tools.RecyclableScroller.Scripts;
using UnityEngine;

namespace Tools.RecyclableScroller.GUI.Scripts.Adapters
{
    /// <summary>
    /// Adapts uGUI pooling to ICellContainer.
    /// Wraps CellPool and manages RectTransform lifecycle (activation/deactivation).
    /// </summary>
    public class GUICellContainer : ICellContainer
    {
        private readonly CellPool cellPool;

        public int TotalCellCount => cellPool?.TotalCount ?? 0;
        public int PooledCellCount => cellPool?.AvailableCount ?? 0;

        public GUICellContainer(
            GameObject cellPrefab,
            RectTransform contentTransform,
            int poolSize,
            IPrefabInstantiator instantiator,
            GameObject headerPrefab = null,
            int headerPoolSize = 0)
        {
            cellPool = new CellPool(
                cellPrefab,
                contentTransform,
                poolSize,
                instantiator,
                headerPrefab,
                headerPoolSize);
        }

        public object GetOrCreateCell(bool isHeader)
        {
            return cellPool.GetCell(isHeader);
        }

        public void ReturnCell(object cell)
        {
            if (cell is RectTransform rt)
                cellPool.ReturnCell(rt);
        }

        public void DestroyAllCells()
        {
            cellPool?.DestroyAllCells();
        }

        public void Prewarm()
        {
            cellPool?.PrewarmPoolDeferred();
        }

        public void Cleanup()
        {
            DestroyAllCells();
        }
    }
}

