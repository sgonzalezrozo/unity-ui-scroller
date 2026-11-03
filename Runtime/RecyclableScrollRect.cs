using System;
using ToolsCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecyclableScroller.Runtime
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1)]
    /// <summary>A ScrollRect that recycles cells. Create a concrete subclass to add it to a GameObject.</summary>
    public abstract class RecyclableScrollRect<T> : RecyclableScrollRectBase, IRecyclableScrollRect
        where T : class
    {
        private const float PULL_THRESHOLD = 75f;
        private const float MOVING_THRESHOLD = 20f;
        private const float DEFAULT_POOL_PERCENTAGE = 1.4f;
        private const float DYNAMIC_GRID_MIN_DELTA = 5f;
        private const float PAGINATION_TRIGGER_NORMALIZED = 0.96f;
        private const float PAGINATION_RESET_NORMALIZED = 0.95f;
        private const float EXPANDED_SIZE_FALLBACK_MULTIPLIER = 2f;

        /// <summary>Runs when initialization finishes.</summary>
        public Action OnInitialized;

        [SerializeField] protected GameObject cellPrefab;
        [SerializeField] protected GameObject headerPrefab;
        [SerializeField] private float lineSpacing;
        [SerializeField] private float gridCellSpacing;
        [SerializeField] private float paddingStart;
        [SerializeField] private float paddingEnd;
        [SerializeField] private bool isVertical = true;
        [SerializeField] private bool reverse;
        [SerializeField] private uint gridCount = 1;
        [SerializeField] private bool useDynamicGridCount;
        [SerializeField] private float dynamicGridCellSize = 240f;
        [SerializeField] private uint minDynamicGridCount = 1;
        [SerializeField] private uint maxDynamicGridCount = 8;
        [SerializeField] private bool allowMultipleExpandedCells;
        [SerializeField] private bool scrollToInitOnEnable;
        [SerializeField] private bool overridePoolPercentage;
        [SerializeField] [Range(1.4f, 3.0f)] private float poolPercentage = 1.4f;
        [SerializeField] private float estimatedCellHeight = 100f;
        [SerializeField] private int cellsPerFrame = 5;

        internal IPrefabInstantiator injectedInstantiator;

        public ScrollStateManager scrollStateManager;

        protected CellItem[] cellItems;
        protected CellLine[] cellLines;
        protected bool hasReachedLastPage;

        protected IRecyclableDataSource<T> DataSource { get; set; }

        private ScrollComponentBinder components;
        private ViewportMetrics viewportMetrics;
        private GridCountResolver gridResolver;
        private LinePositionCache positionCache;
        private VisibleLineFinder visibleLineFinder;
        private LineContentSizeCalculator contentSizeCalculator;
        private CellLineBuilder cellLineBuilder;
        private CellAnimationMarker animationMarker;
        private CellExpansionController expansion;
        private CellLineDisplayer displayer;
        private PaginationBoundaryDetector paginationDetector;
        private PullToRefreshDetector pullDetector;
        private ScrollNavigator navigator;
        private CellPool cellPool;
        private CellLineRecycler cellLineRecycler;
        private VisibleCellCalculator visibleCellCalculator;

        private VisibilityUpdater visibilityUpdater;
        private ScrollActivityTracker activityTracker;
        private ExpansionOperationHandler expansionHandler;
        private RebuildCoordinator rebuildCoordinator;
        private RebuildCoroutineScheduler coroutineScheduler;
        private DynamicCellSizeCoordinator<T> sizeCoordinator;

        private float viewportSize;
        private bool isInitialized;
        private bool needsPositionRecalculation = true;
        private bool forceRebindAll;

        private Func<int, bool> cachedIsHeader;
        private Func<int, float> cachedGetCellSize;
        private Func<int, float> cachedGetDefaultSize;
        private Func<int, float> cachedGetExpandedSize;
        private Func<int, float> cachedLineSizeForFirstCell;
        private Func<int> cachedGetItemCount;
        private Func<int, bool> cachedIsIndexExpanded;
        private Action<RectTransform, int> cachedSetupExpandable;
        private Action<RectTransform, int> cachedBindCell;
        private Action<int> cachedOnCellCollapsed;
        private Action<int, float> cachedOnCellExpanded;

        /// <summary>True once initialization has finished.</summary>
        public bool IsInitialized => isInitialized;
        /// <summary>True while the list is moving.</summary>
        public bool IsScrolling => activityTracker?.IsScrolling ?? false;
        /// <summary>True while the user is dragging.</summary>
        public bool IsDragging => activityTracker?.IsDragging ?? false;
        /// <summary>The first visible line index, or -1 before initialization.</summary>
        public int VisibleStartIndex => scrollStateManager?.VisibleStartIndex ?? -1;
        /// <summary>The last visible line index, or -1 before initialization.</summary>
        public int VisibleEndIndex => scrollStateManager?.VisibleEndIndex ?? -1;
        /// <summary>True when the start of the list is visible.</summary>
        public bool IsFirstItemVisible => isInitialized && VisibleStartIndex == 0;
        /// <summary>True when the end of the list is visible.</summary>
        public bool IsLastItemVisible => isInitialized && cellLines is { Length: > 0 } && VisibleEndIndex == cellLines.Length - 1;

        /// <summary>The prefab used for cells.</summary>
        public GameObject CellPrefab
        {
            get => cellPrefab;
            set => cellPrefab = value;
        }

        /// <summary>Gets the number of items in the data source.</summary>
        public int GetItemCount()
            => DataSource?.ItemCount ?? 0;

        protected virtual bool SupportsPagination()
            => DataSource is PaginatedDataSource<T>;

        protected virtual bool IsHeader(int dataIndex)
            => false;

        protected virtual int GetEstimatedHeaderCount()
            => 0;

        protected virtual void OnBeforeRebuildStructures()
            => sizeCoordinator?.ClearCaches();

        protected virtual void OnAfterRebuildStructures()
            => sizeCoordinator?.RebuildAllCalculatedSizes(cellItems, cellLines);

        protected virtual void OnCellCollapsed(int index)
            => sizeCoordinator?.OnCellCollapsed(index, cellItems);

        protected virtual void OnCellExpanded(int index, float newSize)
            => sizeCoordinator?.OnCellExpanded(index, newSize);

        protected virtual float GetExpandedSizeForCell(int index)
            => sizeCoordinator?.GetExpandedSize(index, estimatedCellHeight * EXPANDED_SIZE_FALLBACK_MULTIPLIER)
               ?? (GetDefaultCellSize(index) * EXPANDED_SIZE_FALLBACK_MULTIPLIER);

        protected virtual void OnPullToRefreshDetected()
        {
        }

        protected virtual void OnPaginationThresholdReached()
        {
        }

        protected virtual void InitializeCellItems()
        {
            int count = DataSource?.ItemCount ?? 0;
            cellItems = new CellItem[count];
            for (int i = 0; i < count; i++)
                cellItems[i] = new CellItem(i);
        }

        protected virtual int GetDataSourceItemCount()
            => DataSource?.ItemCount ?? 0;

        protected virtual float GetCellSize(int index)
            => sizeCoordinator != null
                ? sizeCoordinator.GetCellSize(index, cellPrefab)
                : GetDefaultCellSizeFromPrefab();

        protected virtual float GetDefaultCellSize(int index)
            => sizeCoordinator != null
                ? sizeCoordinator.GetCellSize(index, cellPrefab)
                : GetDefaultCellSizeFromPrefab();

        protected virtual void BindCell(RectTransform cell, int index)
        {
            if (cell == null)
                return;

            T item = DataSource.GetItem(index);
            if (item == null)
            {
                cell.gameObject.SetActive(false);
                return;
            }

            if (TryBindExpandable(cell, index, item))
                return;

            if (TryBindStandard(cell, index, item))
                return;

            Debug.LogError($"[RecyclableScrollRect] Index {index} - Cell does not implement IRecyclableCell<{typeof(T).Name}> or IExpandableRecyclableCell<{typeof(T).Name}>");
        }

        private bool TryBindExpandable(RectTransform cell, int index, T item)
        {
            IExpandableRecyclableCell<T> expandableCellComponent = cell.GetComponent<IExpandableRecyclableCell<T>>();
            if (expandableCellComponent == null)
                return false;

            expandableCellComponent.Data = item;
            expandableCellComponent.IsExpanded = IsIndexExpanded(index);
            sizeCoordinator?.OnExpandableBound(index, item);
            return true;
        }

        private bool TryBindStandard(RectTransform cell, int index, T item)
        {
            IRecyclableCell<T> cellComponent = cell.GetComponent<IRecyclableCell<T>>();
            if (cellComponent == null)
                return false;

            cellComponent.Data = item;
            sizeCoordinator?.OnCellBoundIfDynamic(index, cellItems, cellLines, cachedIsIndexExpanded);
            return true;
        }

        private float GetDefaultCellSizeFromPrefab()
        {
            IRecyclableCell cellComponent = cellPrefab != null ? cellPrefab.GetComponent<IRecyclableCell>() : null;
            return cellComponent?.DefaultCellSize ?? 100f;
        }

        /// <summary>Starts the scroller with a data source.</summary>
        public void Initialize(IRecyclableDataSource<T> source)
            => Initialize(source, null);

        /// <summary>Starts the scroller with a data source and size calculator.</summary>
        public void Initialize(IRecyclableDataSource<T> source, ICellSizeCalculator<T> calculator)
        {
            if (!DataSourceGuard<T>.ValidateForInitialize(gameObject, source))
                return;

            DataSource = source;

            InitializeBase();

            if (!isInitialized)
                return;

            sizeCoordinator.ConfigureCalculator(calculator, source, cellPrefab, GetContainerWidth());

            if (sizeCoordinator.IsDynamicSizing)
            {
                sizeCoordinator.PreCalculateAllCellSizes();
                RebuildLinesStructure();
                RecalculateContentSize();
                positionCache.Rebuild(cellLines, paddingStart, lineSpacing);

                if (sizeCoordinator.CalculatedCount < DataSource.ItemCount)
                    sizeCoordinator.StartBatchedCalculation(cellItems, cellLines, cachedIsIndexExpanded, GetActualVisibleCellCount());
            }
        }

        /// <summary>Replaces the data source and refreshes.</summary>
        public void SetDataSource(IRecyclableDataSource<T> newSource)
        {
            if (!DataSourceGuard<T>.ValidateForSetDataSource(gameObject, newSource, isInitialized))
                return;

            sizeCoordinator?.UpdateDataSource(newSource);
            DataSource = newSource;
            Refresh();
        }

        private float GetContainerWidth()
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect == null)
                return 0;

            ScrollRect sr = GetComponent<ScrollRect>();
            if (sr == null)
                return rect.rect.width;

            RectTransform viewport = sr.viewport != null ? sr.viewport : sr.content?.parent as RectTransform;
            return viewport != null ? viewport.rect.width : rect.rect.width;
        }

        protected void InitializeBase()
        {
            if (isInitialized)
                return;

            if (!ValidateCellPrefab())
                return;

            if (!SupportsPagination())
                hasReachedLastPage = true;

            if (!InitializeCoreComponents())
                return;

            InitializeCellItems();
            if (cellItems == null || cellItems.Length == 0)
                return;

            isInitialized = true;

            InitializeSupportingSystems();
            BuildInitialLayout();
            InitializePool();
            InitializeOrchestrators();

            coroutineScheduler.ScheduleInitialBuild();
            coroutineScheduler.ScheduleDeferredPoolWarmup(cellPool);
        }

        private bool ValidateCellPrefab()
        {
            if (cellPrefab != null)
                return true;

            Debug.LogError($"[RecyclableScrollRect] Cell Prefab is not assigned to {gameObject.name}. Assign a prefab implementing IRecyclableCell in the Inspector under the RecyclableScrollRect component.");
            return false;
        }

        private bool InitializeCoreComponents()
        {
            components = new ScrollComponentBinder();
            if (!components.TryBind(this, isVertical, reverse, cellPrefab))
                return false;

            viewportMetrics = new ViewportMetrics(components.Direction, components.Viewport, components.Content, isVertical);
            gridResolver = new GridCountResolver(gridCount, useDynamicGridCount, dynamicGridCellSize, gridCellSpacing, minDynamicGridCount, maxDynamicGridCount);

            viewportSize = viewportMetrics.GetViewportSize();
            gridResolver.Resolve(viewportMetrics.GetCrossAxisViewportSize());

            contentSizeCalculator = new LineContentSizeCalculator();
            return true;
        }

        private void InitializeSupportingSystems()
        {
            scrollStateManager = new ScrollStateManager();
            positionCache = new LinePositionCache();
            visibleLineFinder = new VisibleLineFinder(isVertical, reverse);
            cellLineBuilder = new CellLineBuilder();
            animationMarker = new CellAnimationMarker();
            expansion = new CellExpansionController();
            paginationDetector = new PaginationBoundaryDetector(PAGINATION_TRIGGER_NORMALIZED, PAGINATION_RESET_NORMALIZED);
            pullDetector = new PullToRefreshDetector(PULL_THRESHOLD, isVertical, reverse);
            navigator = new ScrollNavigator(components.ScrollRect, components.Direction, isVertical, reverse);
            activityTracker = new ScrollActivityTracker(MOVING_THRESHOLD);

            CacheDelegates();

            visibilityUpdater = new VisibilityUpdater(viewportMetrics, positionCache, visibleLineFinder, scrollStateManager, animationMarker);

            sizeCoordinator = new DynamicCellSizeCoordinator<T>(
                this, cellsPerFrame, estimatedCellHeight,
                RecalculateContentSize, RebuildVisibleCells, MarkPositionsDirty);
        }

        private void CacheDelegates()
        {
            cachedIsHeader = IsHeader;
            cachedGetCellSize = GetCellSize;
            cachedGetDefaultSize = GetDefaultCellSize;
            cachedGetExpandedSize = GetExpandedSizeForCell;
            cachedLineSizeForFirstCell = GetLineSizeConsideringExpansion;
            cachedGetItemCount = GetDataSourceItemCount;
            cachedIsIndexExpanded = IsIndexExpanded;
            cachedSetupExpandable = SetupExpandableCell;
            cachedBindCell = BindCell;
            cachedOnCellCollapsed = OnCellCollapsed;
            cachedOnCellExpanded = OnCellExpanded;
        }

        private void BuildInitialLayout()
        {
            RebuildLinesStructure();
            RecalculateContentSize();
            positionCache.Rebuild(cellLines, paddingStart, lineSpacing);
            components.ScrollRect.onValueChanged.AddListener(OnScroll);
        }

        private void InitializePool()
        {
            int visibleCount = GetActualVisibleCellCount();
            float effectivePoolPercentage = overridePoolPercentage ? poolPercentage : DEFAULT_POOL_PERCENTAGE;
            int poolCellsNeeded = Mathf.CeilToInt(visibleCount * effectivePoolPercentage) * gridResolver.Current;
            int totalCellsNeeded = Mathf.Min(poolCellsNeeded, GetDataSourceItemCount());

            cellPool?.DestroyAllCells();

            IPrefabInstantiator resolvedInstantiator = injectedInstantiator ?? ToolInjectionServices.PrefabInstantiator;
            cellPool = new CellPool(cellPrefab, components.Content, totalCellsNeeded, resolvedInstantiator);

            cellLineRecycler = new CellLineRecycler(cellLines, cellPool);

            int poolBufferStart = visibleCount;
            int poolBufferEnd = Mathf.Min(poolBufferStart + (poolCellsNeeded - visibleCount), cellLines.Length - 1);
            animationMarker.ConfigureNonAnimatingRange(poolBufferStart, poolBufferEnd);

            displayer = BuildDisplayer();
            visibilityUpdater.UpdateWiring(cellLineRecycler, displayer);
        }

        private void InitializeOrchestrators()
        {
            expansionHandler = new ExpansionOperationHandler(
                expansion, gridResolver, scrollStateManager,
                cachedGetDefaultSize, cachedGetExpandedSize,
                cachedOnCellCollapsed, cachedOnCellExpanded,
                RecalculateContentSize, MarkPositionsDirty, RebuildVisibleCells);

            rebuildCoordinator = new RebuildCoordinator(
                gridResolver, viewportMetrics, cellLineBuilder, positionCache, scrollStateManager, expansion,
                cachedGetItemCount, () => cellItems, () => cellLines, lines => cellLines = lines,
                InitializeCellItems, force => forceRebindAll = force,
                RecalculateContentSize, RebuildVisibleCells,
                CleanupActiveLines, RecreateRecyclerAndDisplayer,
                OnBeforeRebuildStructures, OnAfterRebuildStructures,
                CleanDataReferencesInCellItems,
                cachedIsHeader, cachedGetCellSize, cachedLineSizeForFirstCell,
                paddingStart, lineSpacing,
                viewportMetrics.GetCrossAxisViewportSize());

            coroutineScheduler = new RebuildCoroutineScheduler(
                this, () => isInitialized,
                viewportMetrics, scrollStateManager, navigator, gridResolver, rebuildCoordinator,
                RecalculateContentSize, RebuildVisibleCells,
                size => viewportSize = size,
                () => components.Content.anchoredPosition = Vector2.zero,
                () => OnInitialized?.Invoke(),
                () => scrollToInitOnEnable);
        }

        private CellLineDisplayer BuildDisplayer()
            => new(
                cellPool,
                components.Direction,
                positionCache,
                expansion,
                animationMarker,
                cachedBindCell,
                cachedIsHeader,
                cachedGetItemCount,
                cachedSetupExpandable,
                components.Viewport,
                components.Content,
                gridCellSpacing);

        private void RecreateRecyclerAndDisplayer()
        {
            cellLineRecycler = new CellLineRecycler(cellLines, cellPool);
            displayer = BuildDisplayer();
            visibilityUpdater.UpdateWiring(cellLineRecycler, displayer);
        }

        private void RebuildLinesStructure()
        {
            cellLines = cellLineBuilder.Build(
                cellItems,
                GetDataSourceItemCount(),
                gridResolver.Current,
                cachedIsHeader,
                cachedGetCellSize,
                cachedLineSizeForFirstCell);
        }

        private float GetLineSizeConsideringExpansion(int cellIndex)
            => expansion.GetSizeOrDefault(cellIndex, GetDefaultCellSize(cellIndex));

        protected float GetViewportSize()
            => viewportMetrics?.GetViewportSize() ?? 0f;

        protected int CalculateVisibleCountForViewport(float viewport)
        {
            visibleCellCalculator ??= new VisibleCellCalculator(cellItems);
            return visibleCellCalculator.CalculateVisibleCellCount(viewport, paddingStart, paddingEnd, lineSpacing, GetCellSize);
        }

        private int GetActualVisibleCellCount()
            => CalculateVisibleCountForViewport(viewportSize);

        protected void RecalculateContentSize()
        {
            if (!isInitialized || contentSizeCalculator == null)
                return;

            float totalSize = contentSizeCalculator.CalculateTotalSize(cellLines, paddingStart, paddingEnd, lineSpacing);
            contentSizeCalculator.ApplyToContent(components.Content, totalSize, isVertical);
            MarkPositionsDirty();
        }

        protected void RebuildVisibleCells()
        {
            if (!isInitialized)
                return;

            visibilityUpdater.RebuildVisibleCells(cellLines, paddingStart, lineSpacing, ref needsPositionRecalculation, forceRebindAll);
            viewportSize = visibilityUpdater.LastViewportSize;
        }

        private void MarkPositionsDirty()
            => needsPositionRecalculation = true;

        private void OnScroll(Vector2 position)
        {
            if (!isInitialized)
                return;

            ScrollTickSignal signal = activityTracker.HandleScrollTick(
                components.ScrollRect.velocity.magnitude,
                viewportMetrics.GetScrollPosition());

            CheckPaginationBoundary();

            if (signal == ScrollTickSignal.RebuildVisible)
                RebuildVisibleCells();
        }

        private void CheckPaginationBoundary()
        {
            if (!SupportsPagination() || hasReachedLastPage)
                return;

            PaginationSignal signal = paginationDetector.Evaluate(
                viewportMetrics.GetScrollPosition(),
                viewportMetrics.GetContentSize(),
                viewportSize);

            if (signal == PaginationSignal.Trigger)
                OnPaginationThresholdReached();
        }

        private void SetupExpandableCell(RectTransform cell, int index)
        {
            if (cell == null || index < 0 || index >= cellItems.Length)
                return;

            CellItem cellItem = cellItems[index];
            if (cellItem == null)
                return;

            cellItem.CacheComponentReferences(cell);

            IExpandableRecyclableCellBase expandable = cellItem.GetExpandableCell();
            if (expandable == null)
                return;

            expandable.ScrollRect = this;
            expandable.CellDataIndex = index;
        }

        /// <summary>Rebuilds the list from the current data source.</summary>
        public void Refresh()
        {
            if (!isInitialized)
            {
                Debug.LogWarning($"[RecyclableScrollRect] Refresh aborted - not initialized");
                return;
            }

            rebuildCoordinator.Refresh();
        }

        private void CleanDataReferencesInCellItems()
        {
            if (cellItems == null || cellItems.Length == 0)
                return;

            for (int i = 0; i < cellItems.Length; i++)
            {
                if (cellItems[i] != null)
                    cellItems[i].DataIndex = i;
            }
        }

        /// <summary>Clears displayed cells and resets the scroll position.</summary>
        public void Clear()
        {
            if (!isInitialized)
                return;

            CleanupActiveLines();
            expansion.ClearAll();
            scrollStateManager.ResetState();
            components.Content.anchoredPosition = Vector2.zero;
        }

        private void CleanupActiveLines()
            => displayer?.CleanupActiveLines(cellLines);

        /// <summary>Checks whether an item is visible.</summary>
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

        /// <summary>Jumps to an item.</summary>
        public void ScrollToItem(int index)
        {
            if (!isInitialized || index < 0 || index >= cellItems.Length)
                return;

            int lineIndex = index / gridResolver.Current;
            if (lineIndex >= cellLines.Length)
                return;

            float linePosition = positionCache.GetPosition(lineIndex);
            navigator.ScrollToLinePosition(linePosition, viewportMetrics.GetContentSize(), viewportSize);
            RebuildVisibleCells();
        }

        /// <summary>Jumps to the start of the list.</summary>
        public void ScrollToStart()
        {
            if (!isInitialized)
                return;

            navigator.ScrollToStart();
            RebuildVisibleCells();
        }

        /// <summary>Jumps to the end of the list.</summary>
        public void ScrollToEnd()
        {
            if (!isInitialized)
                return;

            navigator.ScrollToEnd();
            RebuildVisibleCells();
        }

        /// <summary>Stops any scroll movement.</summary>
        public void ForceStopScrolling()
        {
            if (!isInitialized)
                return;

            navigator.ForceStop();
            activityTracker.ForceStopScrolling();
        }

        /// <summary>Checks whether a cell is expanded.</summary>
        public bool IsIndexExpanded(int index)
            => expansion?.IsExpanded(index) ?? false;

        /// <summary>Collapses an expanded cell.</summary>
        public void CellWantToCollapse(int index)
        {
            if (!isInitialized)
                return;

            expansionHandler.CellWantToCollapse(index, cellLines, cellItems);
        }

        /// <summary>Resizes an expanded cell. Not supported in grids.</summary>
        public void UpdateExpandedCellSize(int index, float newSize = -1)
        {
            if (!isInitialized)
                return;

            expansionHandler.UpdateExpandedCellSize(index, newSize, cellLines, cellItems);
        }

        /// <summary>Expands a cell. Ignored in grids.</summary>
        public void ExpandCell(int index, float newSize = -1)
        {
            if (!isInitialized)
                return;

            expansionHandler.ExpandCell(index, newSize, cellLines, cellItems, allowMultipleExpandedCells);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            paginationDetector?.ResetTriggerFlag();
            activityTracker.HandleDragBegin(isInitialized, isInitialized ? viewportMetrics.GetScrollPosition() : 0f);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            activityTracker.HandleDragEnd();

            if (!isInitialized)
                return;

            if (pullDetector.WasPullDetected(viewportMetrics.GetContentAnchoredPosition(), viewportMetrics.GetContentSize()))
                OnPullToRefreshDetected();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!isInitialized)
                return;

            coroutineScheduler.ScheduleReEnableRebuild();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!isInitialized)
                return;

            if (Application.isEditor && !Application.isPlaying)
                return;

            CleanupActiveLines();
        }

        protected override void OnDestroy()
        {
            coroutineScheduler?.CancelPendingGridUpdate();

            if (components?.ScrollRect != null)
                components.ScrollRect.onValueChanged.RemoveListener(OnScroll);

            cellPool?.DestroyAllCells();
            base.OnDestroy();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (!isInitialized || gridResolver is not { IsDynamic: true })
                return;

            float currentCrossAxisSize = viewportMetrics.GetCrossAxisViewportSize();
            if (Mathf.Abs(currentCrossAxisSize - rebuildCoordinator.LastCrossAxisSize) < DYNAMIC_GRID_MIN_DELTA)
                return;

            rebuildCoordinator.UpdateLastCrossAxisSize(currentCrossAxisSize);
            coroutineScheduler.ScheduleDeferredGridUpdate();
        }
    }
}
