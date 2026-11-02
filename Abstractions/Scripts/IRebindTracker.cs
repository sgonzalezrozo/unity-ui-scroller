namespace RecyclableScroller.Abstractions
{
    /// <summary>
    /// Determines which cells need rebinding based on scroll/grid/data changes.
    /// Optimizes by rebinding only necessary cells instead of all visible ones.
    /// </summary>
    public enum RebindStrategy
    {
        /// <summary>No cells need rebinding</summary>
        None = 0,

        /// <summary>Only newly visible cells need rebinding</summary>
        VisibleOnly = 1,

        /// <summary>All cells in grid need rebinding (grid structure changed)</summary>
        GridChanged = 2,

        /// <summary>All cells need rebinding (data source changed)</summary>
        DataSourceChanged = 3
    }

    /// <summary>
    /// Implements smart rebinding heuristic based on what changed.
    /// </summary>
    public interface IRebindTracker
    {
        /// <summary>
        /// Determine rebind strategy based on state changes.
        ///
        /// Logic:
        /// - Data changed → rebind all (data is new)
        /// - Grid structure changed → rebind all (layout changed)
        /// - Just scrolled → rebind only newly visible (data same, just different view)
        /// - No change → no rebind (everything already correct)
        /// </summary>
        RebindStrategy DetermineStrategy(
            int oldStartLine,
            int oldEndLine,
            int newStartLine,
            int newEndLine,
            bool gridChanged,
            bool dataSourceChanged);
    }
}

