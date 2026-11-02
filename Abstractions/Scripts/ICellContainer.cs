namespace Tools.RecyclableScroller.Abstractions.Scripts
{
    /// <summary>
    /// Abstracts cell lifecycle management (creation, pooling, destruction).
    /// Implementations handle framework-specific instantiation:
    /// - uGUI: RectTransform-based cells via Instantiate()
    /// - UIToolkit: VisualElement-based cells via VisualTreeAsset.Instantiate()
    /// </summary>
    public interface ICellContainer
    {
        /// <summary>
        /// Get or create a cell from the pool. Cell is automatically activated.
        /// </summary>
        /// <param name="isHeader">True if this is a header cell (may use different prefab)</param>
        /// <returns>Cell container (RectTransform for uGUI, VisualElement for UIToolkit).
        /// Never null if pool has capacity.</returns>
        object GetOrCreateCell(bool isHeader);

        /// <summary>
        /// Return a cell to the pool for reuse. Cell should be deactivated internally.
        /// </summary>
        void ReturnCell(object cell);

        /// <summary>
        /// Total cells currently allocated (active + pooled).
        /// </summary>
        int TotalCellCount { get; }

        /// <summary>
        /// Cells currently available in pool (not in use).
        /// </summary>
        int PooledCellCount { get; }

        /// <summary>
        /// Destroy all cells and clear pool.
        /// Called on cleanup or data source change.
        /// </summary>
        void DestroyAllCells();
    }
}

