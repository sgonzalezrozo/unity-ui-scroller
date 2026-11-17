using UnityEngine;

namespace UIScroller
{
    internal class VisibilityPass
    {
        private readonly AxisMetrics viewportMetrics;
        private readonly LayoutModel layout;
        private readonly VisibleRangeFinder lineFinder;
        private readonly VisibleRangeTracker scrollStateManager;
        private readonly WorkCounters work;
        private const float MIN_SCROLL_STEP = 0.5f;
        private float lastCheckedScroll = float.NaN;

        private SlotRecycler recycler;
        private float lastViewportSize;

        public float LastViewportSize => lastViewportSize;

        /// <summary>Lines added above the first visible one: a bento tile can start that many rows up and still reach the screen.</summary>
        public int ExtraLinesAbove { get; set; }

        public VisibilityPass(
            AxisMetrics viewportMetrics,
            LayoutModel layout,
            VisibleRangeFinder lineFinder,
            VisibleRangeTracker scrollStateManager,
            WorkCounters work)
        {
            this.work = work;
            this.viewportMetrics = viewportMetrics;
            this.layout = layout;
            this.lineFinder = lineFinder;
            this.scrollStateManager = scrollStateManager;
        }

        public void UpdateWiring(SlotRecycler slotRecycler)
            => recycler = slotRecycler;

        public void RebuildVisibleItems(ref bool needsPositionRecalculation, bool forceRebindAll)
        {
            using var _ = ScrollerMarkers.UpdateVisible.Auto();
            ListView<Line> lines = layout.Lines;
            if (lines.Length == 0 || recycler == null)
                return;

            lastViewportSize = viewportMetrics.GetViewportSize();

            if (needsPositionRecalculation)
            {
                layout.RecomputeOffsets();
                needsPositionRecalculation = false;
            }

            // C10: the content's position is read once, for both the scroll position and the at-start check.
            Vector2 contentAnchoredPosition = viewportMetrics.GetContentAnchoredPosition();
            float scrollPosition = viewportMetrics.ScrollPositionOf(contentAnchoredPosition);

            // P4: a scroll under half a pixel since the last check can't change what's visible. A reset range (a rebuild)
            // always checks. Measured from the last check, not the last event, so small steps still add up.
            bool rangeIsKnown = scrollStateManager.VisibleStartIndex >= 0;
            if (rangeIsKnown && Mathf.Abs(scrollPosition - lastCheckedScroll) < MIN_SCROLL_STEP)
                return;

            lastCheckedScroll = scrollPosition;
            work.RangeChecks++;

            int newStartLine = lineFinder.FindFirstLine(scrollPosition, contentAnchoredPosition, layout);
            int newEndLine = lineFinder.FindLastLine(scrollPosition, lastViewportSize, newStartLine, layout);
            newStartLine = Mathf.Max(0, newStartLine - ExtraLinesAbove);

            if (!scrollStateManager.HasVisibilityRangeChanged(newStartLine, newEndLine))
                return;

            // Every line of this pass is placed at the offsets it started with, even when a slot measured on the way
            // changes a size: the engine then compares those offsets with the new ones to keep the screen still.
            layout.FreezeOffsets();
            try
            {
                recycler.RecycleOutOfViewLines(newStartLine, newEndLine);
                recycler.ShowLines(newStartLine, newEndLine, forceRebindAll);
            }
            finally
            {
                layout.UnfreezeOffsets();
            }

            scrollStateManager.UpdateVisibilityRange(newStartLine, newEndLine);
        }
    }
}

