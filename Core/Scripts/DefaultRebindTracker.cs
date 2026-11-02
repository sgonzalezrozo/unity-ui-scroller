using Tools.RecyclableScroller.Abstractions.Scripts;

namespace Tools.RecyclableScroller.Core.Scripts
{
    /// <summary>
    /// Default implementation of rebind strategy tracker.
    /// Logic:
    /// - Data changed → rebind all (new data, different content)
    /// - Grid changed → rebind all (layout completely different)
    /// - Scrolled → rebind only new visible cells (same data, different window)
    /// - No change → no rebind (everything already correct)
    /// </summary>
    public class DefaultRebindTracker : IRebindTracker
    {
        public RebindStrategy DetermineStrategy(
            int oldStartLine,
            int oldEndLine,
            int newStartLine,
            int newEndLine,
            bool gridChanged,
            bool dataSourceChanged)
        {
            // Highest priority: data fundamentally changed
            if (dataSourceChanged)
                return RebindStrategy.DataSourceChanged;

            // Next: grid structure changed (columns/rows completely different)
            if (gridChanged)
                return RebindStrategy.GridChanged;

            // Check if visible range actually changed
            // This handles all scroll movements (both directions)
            bool startLineChanged = oldStartLine != newStartLine;
            bool endLineChanged = oldEndLine != newEndLine;

            if (startLineChanged || endLineChanged)
                return RebindStrategy.VisibleOnly;

            // Nothing changed
            return RebindStrategy.None;
        }
    }
}

