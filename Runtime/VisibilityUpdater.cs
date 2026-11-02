using UnityEngine;

namespace Tools.RecyclableScroller.Scripts
{
    public class VisibilityUpdater
    {
        private readonly ViewportMetrics viewportMetrics;
        private readonly LinePositionCache positionCache;
        private readonly VisibleLineFinder lineFinder;
        private readonly ScrollStateManager scrollStateManager;
        private readonly CellAnimationMarker animationMarker;

        private CellLineRecycler cellLineRecycler;
        private CellLineDisplayer displayer;
        private float lastViewportSize;

        public float LastViewportSize => lastViewportSize;

        public VisibilityUpdater(
            ViewportMetrics viewportMetrics,
            LinePositionCache positionCache,
            VisibleLineFinder lineFinder,
            ScrollStateManager scrollStateManager,
            CellAnimationMarker animationMarker)
        {
            this.viewportMetrics = viewportMetrics;
            this.positionCache = positionCache;
            this.lineFinder = lineFinder;
            this.scrollStateManager = scrollStateManager;
            this.animationMarker = animationMarker;
        }

        public void UpdateWiring(CellLineRecycler recycler, CellLineDisplayer displayer)
        {
            cellLineRecycler = recycler;
            this.displayer = displayer;
        }

        public void RebuildVisibleCells(CellLine[] cellLines, float paddingStart, float lineSpacing, ref bool needsPositionRecalculation, bool forceRebindAll)
        {
            if (cellLines == null || cellLines.Length == 0 || displayer == null || cellLineRecycler == null)
                return;

            lastViewportSize = viewportMetrics.GetViewportSize();

            if (needsPositionRecalculation)
            {
                positionCache.Rebuild(cellLines, paddingStart, lineSpacing);
                needsPositionRecalculation = false;
            }

            float scrollPosition = viewportMetrics.GetScrollPosition();
            Vector2 contentAnchoredPosition = viewportMetrics.GetContentAnchoredPosition();

            int newStartLine = lineFinder.FindStartLine(scrollPosition, contentAnchoredPosition, cellLines, positionCache);
            int newEndLine = lineFinder.FindEndLine(scrollPosition, lastViewportSize, newStartLine, cellLines, positionCache);

            if (!scrollStateManager.HasVisibilityRangeChanged(newStartLine, newEndLine))
                return;

            cellLineRecycler.RecycleOutOfViewLines(newStartLine, newEndLine);
            displayer.ShowLines(newStartLine, newEndLine, cellLines, forceRebindAll);

            if (!animationMarker.HasMarkedOnce)
                animationMarker.NotifyFirstVisibleLinesDisplayed();

            scrollStateManager.UpdateVisibilityRange(newStartLine, newEndLine);
        }
    }
}

