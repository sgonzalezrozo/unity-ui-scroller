using System;
using RecyclableScroller.Core.Interfaces;
using RecyclableScroller.Core.Pooling;

namespace RecyclableScroller.Core.Size
{
    /// <summary>
    /// Stub for DynamicCellSizeCoordinator.
    /// Full implementation to be moved from Scripts folder during Phase 2 continuation.
    /// </summary>
    public class DynamicCellSizeCoordinator<T> where T : class
    {
        public DynamicCellSizeCoordinator(
            object host,
            int cellsPerFrame,
            float estimatedCellHeight,
            Action recalculateContentSize,
            Action rebuildVisibleCells,
            Action markPositionsDirty)
        {
        }

        public bool IsDynamicSizing { get; private set; }

        public void ConfigureCalculator(ICellSizeCalculator<T> calculator, IRecyclableDataSource<T> source, object cellPrefab, float containerWidth)
        {
            IsDynamicSizing = calculator != null;
        }

        public void UpdateDataSource(IRecyclableDataSource<T> source)
        {
        }

        public float GetCellSize(int index, object cellPrefab)
        {
            return 100f;
        }

        public void OnCellExpanded(int index, float newSize)
        {
        }

        public void OnCellCollapsed(int index, CellItem[] cellItems)
        {
        }
    }
}

