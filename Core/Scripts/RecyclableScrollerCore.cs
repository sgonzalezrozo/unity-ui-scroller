using System;
using Tools.RecyclableScroller.Abstractions.Scripts;
using Tools.RecyclableScroller.Core.Scripts.Algorithms;
using Tools.RecyclableScroller.Core.Scripts.Interfaces;
using Tools.RecyclableScroller.Core.Scripts.Pooling;
using Tools.RecyclableScroller.Core.Scripts.Size;
using UnityEngine;

namespace Tools.RecyclableScroller.Core.Scripts
{
    /// <summary>
    /// Pure C# recyclable scroller engine. Framework-agnostic.
    /// Contains all recycling, visibility, expansion, pagination logic.
    /// Depends ONLY on abstractions, not on uGUI or UIToolkit.
    ///
    /// Generic over T to ensure type-safe data binding.
    /// All rendering/input/async work delegated to framework adapters.
    /// </summary>
    public class RecyclableScrollerCore<T> where T : class
    {
        // Framework adapters (injected)
        private readonly IViewportProvider viewportProvider;
        private readonly ICellContainer cellContainer;
        private readonly IInputHandler inputHandler;
        private readonly IAsyncScheduler asyncScheduler;
        private readonly IRebindTracker rebindTracker;

        // Core state
        private IRecyclableDataSource<T> dataSource;
        private CellItem[] cellItems;
        private CellLine[] cellLines;
        private ScrollStateManager scrollStateManager;

        // Managers
        private CellExpansionController expansion;
        private CellPool cellPool;
        private LinePositionCache positionCache;
        private CachedViewportMetrics cachedMetrics;
        private DynamicCellSizeCoordinator<T> sizeCoordinator;

        // Configuration
        private GridCountResolver gridResolver;
        private IScrollDirection scrollDirection;

        // Layout parameters (from RecyclableScrollRect serialized fields)
        private float lineSpacing;
        private float gridCellSpacing;
        private float paddingStart;
        private float paddingEnd;
        private bool isVertical;
        private bool reverse;
#pragma warning disable CS0414
        private uint gridCount = 1;
        private bool useDynamicGridCount;
        private float dynamicGridCellSize = 240f;
        private uint minDynamicGridCount = 1;
        private uint maxDynamicGridCount = 8;
        private bool allowMultipleExpandedCells;
        private float estimatedCellHeight = 100f;
        private int cellsPerFrame = 5;

        // State flags
        private bool isInitialized;
        private bool needsPositionRecalculation = true;
        private bool forceRebindAll;
#pragma warning restore CS0414
        private bool hasReachedLastPage;

        // Events
        public event Action OnInitialized;
#pragma warning disable CS0067
        public event Action<int> OnPaginationThresholdReached;
        public event Action OnPullToRefreshDetected;
#pragma warning restore CS0067

        public bool IsInitialized => isInitialized;
        public int VisibleStartIndex => scrollStateManager?.VisibleStartIndex ?? -1;
        public int VisibleEndIndex => scrollStateManager?.VisibleEndIndex ?? -1;
        public bool IsFirstItemVisible => isInitialized && VisibleStartIndex == 0;
        public bool IsLastItemVisible => isInitialized && cellLines is { Length: > 0 } && VisibleEndIndex == cellLines.Length - 1;

        public RecyclableScrollerCore(
            IViewportProvider viewportProvider,
            ICellContainer cellContainer,
            IInputHandler inputHandler,
            IAsyncScheduler asyncScheduler,
            IRebindTracker rebindTracker = null)
        {
            this.viewportProvider = viewportProvider ?? throw new ArgumentNullException(nameof(viewportProvider));
            this.cellContainer = cellContainer ?? throw new ArgumentNullException(nameof(cellContainer));
            this.inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
            this.asyncScheduler = asyncScheduler ?? throw new ArgumentNullException(nameof(asyncScheduler));
            this.rebindTracker = rebindTracker ?? new DefaultRebindTracker();
        }

        /// <summary>
        /// Initialize with data source and optional size calculator.
        /// Must be called before any other operations.
        /// </summary>
        public void Initialize(
            IRecyclableDataSource<T> source,
            ICellSizeCalculator<T> sizeCalculator = null,
            IScrollDirection direction = null,
            object gridResolver = null,
            bool isVerticalScroll = true,
            bool reverseScroll = false,
            float lineSpc = 0f,
            float gridCellSpc = 0f,
            float paddingStrt = 0f,
            float paddingEnd_ = 0f,
            uint gridCnt = 1,
            bool useDynamicGrid = false,
            float dynamicCellSize = 240f,
            uint minGridCnt = 1,
            uint maxGridCnt = 8,
            bool allowMultipleExp = false,
            float estCellHeight = 100f,
            int cellsPerFrm = 5)
        {
            if (isInitialized)
                return;

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            dataSource = source;

            // Initialize core components
            cachedMetrics = new CachedViewportMetrics(viewportProvider);
            scrollStateManager = new ScrollStateManager();
            expansion = new CellExpansionController();
            positionCache = new LinePositionCache();

            // Initialize cell items
            InitializeCellItems();

            // Initialize size coordinator
            sizeCoordinator = new DynamicCellSizeCoordinator<T>(
                null,
                cellsPerFrm,
                estCellHeight,
                RecalculateContentSize,
                RebuildVisibleCells,
                MarkPositionsDirty);

            if (sizeCalculator != null)
            {
                sizeCoordinator.ConfigureCalculator(sizeCalculator, dataSource, null, 0f);
            }

            // Build initial structure (simplified - no dependency on IScrollDirection)
            cellLines = new CellLine[Mathf.Max(1, Mathf.CeilToInt(dataSource.ItemCount / Mathf.Max(1, (int)gridCnt)))];
            for (int i = 0; i < cellLines.Length; i++)
                cellLines[i] = new CellLine(i, (int)gridCnt);

            positionCache.Rebuild(cellLines, paddingStrt, lineSpc);

            // Setup input handlers
            inputHandler.OnScroll += HandleScroll;
            inputHandler.OnDragBegin += HandleDragBegin;
            inputHandler.OnDragEnd += HandleDragEnd;

            isInitialized = true;
            OnInitialized?.Invoke();
        }

        private void InitializeCellItems()
        {
            int count = dataSource?.ItemCount ?? 0;
            cellItems = new CellItem[count];
            for (int i = 0; i < count; i++)
                cellItems[i] = new CellItem(i);
        }

        private void RecalculateContentSize()
        {
            if (!isInitialized)
                return;
            cachedMetrics.Invalidate();
        }

        private void RebuildVisibleCells()
        {
            if (!isInitialized)
                return;
            // Will be implemented by GUI integration
        }

        private void MarkPositionsDirty()
        {
            // Mark that positions need recalculation
        }

        private float GetDefaultCellSize(int index)
        {
            return estimatedCellHeight;
        }


        private void HandleScroll(float velocityMagnitude)
        {
            if (!isInitialized)
                return;

            // Update scroll position tracking
            scrollStateManager.UpdateLastScrollPosition(viewportProvider.GetNormalizedScrollPosition());

            RebuildVisibleCells();
        }

        private void HandleDragBegin()
        {
            if (!isInitialized)
                return;
            // Drag begin handling
        }

        private void HandleDragEnd()
        {
            if (!isInitialized)
                return;
            // Drag end handling
        }

        // Public API
        public void Refresh()
        {
            if (!isInitialized)
            {
                Debug.LogWarning($"[RecyclableScrollerCore] Refresh aborted - not initialized");
                return;
            }

            // Rebuild structure from scratch
            forceRebindAll = true;
            RebuildVisibleCells();
        }

        public void ScrollToItem(int index)
        {
            if (!isInitialized || index < 0 || index >= cellItems.Length)
                return;

            int lineIndex = index / gridResolver.Current;
            if (lineIndex >= cellLines.Length)
                return;

            float linePosition = positionCache.GetPosition(lineIndex);
            float normalizedPos = scrollDirection.CalculateNormalizedScrollPosition(
                linePosition,
                cachedMetrics.GetContentSize(),
                cachedMetrics.GetViewportSize());

            inputHandler.SetScrollVelocity(Vector2.zero);  // Smooth scroll would go here
            RebuildVisibleCells();
        }

        public void ScrollToStart()
        {
            if (!isInitialized)
                return;

            float target = isVertical
                ? (reverse ? 0f : 1f)
                : (reverse ? 1f : 0f);

            // Scroll to normalized position
            inputHandler.ForceStop();
            RebuildVisibleCells();
        }

        public void ScrollToEnd()
        {
            if (!isInitialized)
                return;

            float target = isVertical
                ? (reverse ? 1f : 0f)
                : (reverse ? 0f : 1f);

            inputHandler.ForceStop();
            RebuildVisibleCells();
        }

        public void ExpandCell(int index, float newSize = -1)
        {
            if (!isInitialized)
                return;

            if (newSize < 0)
                newSize = estimatedCellHeight * 2f;

            expansion.Expand(
                index,
                newSize,
                gridResolver.Current,
                cellLines,
                cellItems,
                allowMultipleExpandedCells,
                GetDefaultCellSize,
                (col, sz) => OnCellExpanded(col, sz));

            RecalculateContentSize();
            MarkPositionsDirty();
        }

        public void CellWantToCollapse(int index)
        {
            if (!isInitialized)
                return;

            expansion.Collapse(
                index,
                gridResolver.Current,
                cellLines,
                cellItems,
                GetDefaultCellSize,
                idx => OnCellCollapsed(idx));

            RecalculateContentSize();
            MarkPositionsDirty();
        }

        public void UpdateExpandedCellSize(int index, float newSize = -1)
        {
            if (!isInitialized)
                return;

            if (newSize < 0)
                newSize = estimatedCellHeight * 2f;

            expansion.UpdateExpandedSize(
                index,
                newSize,
                gridResolver.Current,
                cellLines,
                (idx, sz) => OnCellExpanded(idx, sz));

            RecalculateContentSize();
            MarkPositionsDirty();
        }

        private void OnCellExpanded(int index, float newSize)
        {
            // Cell expansion handling
        }

        private void OnCellCollapsed(int index)
        {
            // Cell collapse handling
        }

        public bool IsIndexExpanded(int index)
            => expansion?.IsExpanded(index) ?? false;

        public int GetItemCount()
            => dataSource?.ItemCount ?? 0;

        public bool IsItemVisible(int index)
        {
            if (!isInitialized || cellLines == null || cellItems == null || index < 0 || index >= cellItems.Length)
                return false;

            int startLine = VisibleStartIndex;
            int endLine = VisibleEndIndex;

            if (startLine < 0 || endLine < 0)
                return false;

            for (int i = startLine; i <= endLine && i < cellLines.Length; i++)
            {
                if (LineContainsDataIndex(cellLines[i], index))
                    return true;
            }

            return false;
        }

        private static bool LineContainsDataIndex(CellLine line, int dataIndex)
        {
            if (line?.Cells == null)
                return false;

            for (int j = 0; j < line.Cells.Length; j++)
            {
                if (line.Cells[j] != null && line.Cells[j].DataIndex == dataIndex)
                    return true;
            }

            return false;
        }

        public void SetDataSource(IRecyclableDataSource<T> newSource)
        {
            if (!isInitialized)
                return;

            if (newSource == null)
                throw new ArgumentNullException(nameof(newSource));

            sizeCoordinator?.UpdateDataSource(newSource);
            dataSource = newSource;
            Refresh();
        }

        public void Clear()
        {
            if (!isInitialized)
                return;

            expansion.ClearAll();
            scrollStateManager.ResetState();
            MarkPositionsDirty();
        }

        public void Cleanup()
        {
            if (!isInitialized)
                return;

            inputHandler.OnScroll -= HandleScroll;
            inputHandler.OnDragBegin -= HandleDragBegin;
            inputHandler.OnDragEnd -= HandleDragEnd;

            cachedMetrics?.Cleanup();
            cellContainer?.DestroyAllCells();

            isInitialized = false;
        }
    }
}

