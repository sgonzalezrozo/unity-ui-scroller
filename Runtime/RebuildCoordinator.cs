using System;

namespace Tools.RecyclableScroller.Scripts
{
    public class RebuildCoordinator
    {
        private readonly GridCountResolver gridResolver;
        private readonly ViewportMetrics viewportMetrics;
        private readonly CellLineBuilder cellLineBuilder;
        private readonly LinePositionCache positionCache;
        private readonly ScrollStateManager scrollStateManager;
        private readonly CellExpansionController expansion;
        private readonly Func<int> getItemCount;
        private readonly Func<CellItem[]> getCellItems;
        private readonly Action<CellLine[]> setCellLines;
        private readonly Func<CellLine[]> getCellLines;
        private readonly Action initializeCellItems;
        private readonly Action<bool> setForceRebindAll;
        private readonly Action recalculateContentSize;
        private readonly Action rebuildVisibleCells;
        private readonly Action cleanupActiveLines;
        private readonly Action recreateRecyclerAndDisplayer;
        private readonly Action onBeforeRebuildStructures;
        private readonly Action onAfterRebuildStructures;
        private readonly Action cleanDataReferencesInCellItems;
        private readonly Func<int, bool> isHeader;
        private readonly Func<int, float> getCellSize;
        private readonly Func<int, float> lineSizeForFirstCell;

        private readonly float paddingStart;
        private readonly float lineSpacing;

        private bool isRebuildingLayout;
        private float lastCrossAxisSize;

        public float LastCrossAxisSize => lastCrossAxisSize;

        public RebuildCoordinator(
            GridCountResolver gridResolver,
            ViewportMetrics viewportMetrics,
            CellLineBuilder cellLineBuilder,
            LinePositionCache positionCache,
            ScrollStateManager scrollStateManager,
            CellExpansionController expansion,
            Func<int> getItemCount,
            Func<CellItem[]> getCellItems,
            Func<CellLine[]> getCellLines,
            Action<CellLine[]> setCellLines,
            Action initializeCellItems,
            Action<bool> setForceRebindAll,
            Action recalculateContentSize,
            Action rebuildVisibleCells,
            Action cleanupActiveLines,
            Action recreateRecyclerAndDisplayer,
            Action onBeforeRebuildStructures,
            Action onAfterRebuildStructures,
            Action cleanDataReferencesInCellItems,
            Func<int, bool> isHeader,
            Func<int, float> getCellSize,
            Func<int, float> lineSizeForFirstCell,
            float paddingStart,
            float lineSpacing,
            float initialCrossAxisSize)
        {
            this.gridResolver = gridResolver;
            this.viewportMetrics = viewportMetrics;
            this.cellLineBuilder = cellLineBuilder;
            this.positionCache = positionCache;
            this.scrollStateManager = scrollStateManager;
            this.expansion = expansion;
            this.getItemCount = getItemCount;
            this.getCellItems = getCellItems;
            this.getCellLines = getCellLines;
            this.setCellLines = setCellLines;
            this.initializeCellItems = initializeCellItems;
            this.setForceRebindAll = setForceRebindAll;
            this.recalculateContentSize = recalculateContentSize;
            this.rebuildVisibleCells = rebuildVisibleCells;
            this.cleanupActiveLines = cleanupActiveLines;
            this.recreateRecyclerAndDisplayer = recreateRecyclerAndDisplayer;
            this.onBeforeRebuildStructures = onBeforeRebuildStructures;
            this.onAfterRebuildStructures = onAfterRebuildStructures;
            this.cleanDataReferencesInCellItems = cleanDataReferencesInCellItems;
            this.isHeader = isHeader;
            this.getCellSize = getCellSize;
            this.lineSizeForFirstCell = lineSizeForFirstCell;
            this.paddingStart = paddingStart;
            this.lineSpacing = lineSpacing;
            lastCrossAxisSize = initialCrossAxisSize;
        }

        public void Refresh()
        {
            int effectiveGridCount = gridResolver.Calculate(viewportMetrics.GetCrossAxisViewportSize());
            bool gridCountChanged = effectiveGridCount != gridResolver.Current;
            gridResolver.Resolve(viewportMetrics.GetCrossAxisViewportSize());

            int newItemCount = getItemCount();
            int oldItemCount = getCellItems()?.Length ?? 0;

            setForceRebindAll(true);

            if (newItemCount != oldItemCount || gridCountChanged)
                RebuildStructures(rebuildItems: true);
            else
                RebuildForSameStructure(cleanDataIndices: true);

            setForceRebindAll(false);
        }

        public void RebuildLayout()
        {
            if (isRebuildingLayout)
                return;

            isRebuildingLayout = true;

            try
            {
                int newGridCount = gridResolver.Calculate(viewportMetrics.GetCrossAxisViewportSize());
                lastCrossAxisSize = viewportMetrics.GetCrossAxisViewportSize();
                bool gridCountChanged = newGridCount != gridResolver.Current;
                gridResolver.Resolve(viewportMetrics.GetCrossAxisViewportSize());

                int newItemCount = getItemCount();
                int oldItemCount = getCellItems()?.Length ?? 0;

                setForceRebindAll(true);

                if (newItemCount != oldItemCount || gridCountChanged)
                    RebuildStructures(rebuildItems: true);
                else
                    RebuildForSameStructure(cleanDataIndices: false);

                setForceRebindAll(false);
            }
            finally
            {
                isRebuildingLayout = false;
            }
        }

        public void UpdateLastCrossAxisSize(float crossAxisSize)
            => lastCrossAxisSize = crossAxisSize;

        public void RebuildLinesStructure()
        {
            CellLine[] lines = cellLineBuilder.Build(
                getCellItems(),
                getItemCount(),
                gridResolver.Current,
                isHeader,
                getCellSize,
                lineSizeForFirstCell);
            setCellLines(lines);
        }

        private void RebuildStructures(bool rebuildItems)
        {
            cleanupActiveLines();
            expansion.ClearAll();

            onBeforeRebuildStructures();

            if (rebuildItems)
                initializeCellItems();

            RebuildLinesStructure();
            recreateRecyclerAndDisplayer();

            onAfterRebuildStructures();

            recalculateContentSize();
            positionCache.Rebuild(getCellLines(), paddingStart, lineSpacing);

            scrollStateManager.ResetState();
            rebuildVisibleCells();
        }

        private void RebuildForSameStructure(bool cleanDataIndices)
        {
            bool hadExpanded = expansion.HasAny;

            expansion.ClearAll();
            scrollStateManager.ResetState();

            onBeforeRebuildStructures();

            if (cleanDataIndices)
                cleanDataReferencesInCellItems();

            if (hadExpanded)
            {
                RebuildLinesStructure();
                onAfterRebuildStructures();
                recalculateContentSize();
                positionCache.Rebuild(getCellLines(), paddingStart, lineSpacing);
            }
            else
            {
                onAfterRebuildStructures();
                recalculateContentSize();
            }

            cleanupActiveLines();
            rebuildVisibleCells();
        }
    }
}

