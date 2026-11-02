using System;
using Tools.Core;
using Tools.RecyclableScroller.Core.Scripts;
using Tools.RecyclableScroller.Core.Scripts.Directions;
using Tools.RecyclableScroller.Core.Scripts.Interfaces;
using Tools.RecyclableScroller.GUI.Scripts.Adapters;
using Tools.RecyclableScroller.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using ScrollComponentBinder = Tools.RecyclableScroller.Scripts.ScrollComponentBinder;

namespace Tools.RecyclableScroller.GUI.Scripts
{
    /// <summary>
    /// uGUI wrapper for RecyclableScrollerCore<T>.
    /// Routes all framework-specific concerns through adapters while delegating core logic to framework-agnostic engine.
    ///
    /// This is the main integration point for uGUI - it handles:
    /// - Adapter instantiation (viewport, input, cells, async scheduling)
    /// - Event hook-up and bridging
    /// - Configuration and initialization
    /// - Public API forwarding to core
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1)]
    public abstract class RecyclableScrollRectGUI<T> : RecyclableScrollRectBase, IRecyclableScrollRect
        where T : class
    {
        // ... [SerializeField configuration fields] ...
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

        // DI injection
        internal IPrefabInstantiator injectedInstantiator;

        // Framework adapters
        private GUIViewportProvider viewportProvider;
        private GUIInputHandler inputHandler;
        private GUICellContainer cellContainer;
        private GUIAsyncScheduler asyncScheduler;

        // Helper components
        private ScrollComponentBinder components;
        private GridCountResolver gridResolver;
        private IScrollDirection scrollDirection;

        // Core engine
        private RecyclableScrollerCore<T> core;

        // State
        protected Core.Scripts.Interfaces.IRecyclableDataSource<T> DataSource { get; set; }
        private bool isInitialized;

        // Events
        public Action OnInitialized;
        public event Action OnPaginationThresholdReached;
        public event Action OnPullToRefreshDetected;

        // Public API
        public bool IsInitialized => isInitialized;
        public int VisibleStartIndex => core?.VisibleStartIndex ?? -1;
        public int VisibleEndIndex => core?.VisibleEndIndex ?? -1;
        public bool IsFirstItemVisible => isInitialized && VisibleStartIndex == 0;
        public bool IsLastItemVisible => isInitialized && (core?.IsLastItemVisible ?? false);

        public GameObject CellPrefab
        {
            get => cellPrefab;
            set => cellPrefab = value;
        }

        public int GetItemCount()
            => core?.GetItemCount() ?? 0;

        /// <summary>
        /// Initialize the scroller with a data source and optional size calculator.
        /// Creates adapters, instantiates core engine, and sets up all event handlers.
        /// </summary>
        public void Initialize(Core.Scripts.Interfaces.IRecyclableDataSource<T> source)
            => Initialize(source, null);

        public void Initialize(Core.Scripts.Interfaces.IRecyclableDataSource<T> source, Core.Scripts.Interfaces.ICellSizeCalculator<T> calculator)
        {
            if (!Core.Scripts.DataSources.DataSourceGuard<T>.ValidateForInitialize(gameObject, source))
                return;

            DataSource = source;

            // Setup framework binding
            components = new ScrollComponentBinder();
            if (!components.TryBind(this, isVertical, reverse, cellPrefab))
                return;

            // Resolve scroll direction using Core factory
            scrollDirection = ScrollDirectionFactory.CreateDirection(isVertical, reverse);

            // Resolve grid layout
            gridResolver = new GridCountResolver(
                gridCount, useDynamicGridCount, dynamicGridCellSize, gridCellSpacing,
                minDynamicGridCount, maxDynamicGridCount);

            // Create framework adapters
            viewportProvider = new GUIViewportProvider(components.ScrollRect, scrollDirection, isVertical);
            inputHandler = new GUIInputHandler(components.ScrollRect);

            int poolSize = Mathf.CeilToInt(10 * (overridePoolPercentage ? poolPercentage : 1.4f));
            cellContainer = new GUICellContainer(
                cellPrefab, components.Content, poolSize,
                injectedInstantiator ?? ToolInjectionServices.PrefabInstantiator,
                headerPrefab);
            asyncScheduler = new GUIAsyncScheduler(this);

            // Create core engine
            core = new RecyclableScrollerCore<T>(viewportProvider, cellContainer, inputHandler, asyncScheduler);

            // Wire up core events
            core.OnInitialized += () => OnInitialized?.Invoke();
            core.OnPaginationThresholdReached += itemCount => OnPaginationThresholdReached?.Invoke();
            core.OnPullToRefreshDetected += () => OnPullToRefreshDetected?.Invoke();

            // Initialize core with configuration
            core.Initialize(
                source,
                calculator,
                scrollDirection,
                gridResolver,
                isVertical, reverse,
                lineSpacing, gridCellSpacing, paddingStart, paddingEnd,
                gridCount, useDynamicGridCount, dynamicGridCellSize,
                minDynamicGridCount, maxDynamicGridCount, allowMultipleExpandedCells,
                estimatedCellHeight, cellsPerFrame);

            // Setup input listener
            inputHandler.SetupScrollListener();

            // Prewarm pool (deferred to avoid frame spike)
            cellContainer.Prewarm();

            isInitialized = true;
        }

        public void SetDataSource(Core.Scripts.Interfaces.IRecyclableDataSource<T> newSource)
        {
            if (!isInitialized || newSource == null)
                return;

            DataSource = newSource;
            core?.SetDataSource(newSource);
        }

        // Scroll navigation
        public void Refresh()
        {
            if (!isInitialized)
                return;
            core?.Refresh();
        }

        public void ScrollToItem(int index)
        {
            if (!isInitialized)
                return;
            core?.ScrollToItem(index);
        }

        public void ScrollToStart()
        {
            if (!isInitialized)
                return;
            core?.ScrollToStart();
        }

        public void ScrollToEnd()
        {
            if (!isInitialized)
                return;
            core?.ScrollToEnd();
        }

        public void ForceStopScrolling()
        {
            if (!isInitialized)
                return;
            inputHandler?.ForceStop();
        }

        // Expansion API
        public bool IsIndexExpanded(int index)
            => core?.IsIndexExpanded(index) ?? false;

        public void ExpandCell(int index, float newSize = -1)
        {
            if (!isInitialized)
                return;
            core?.ExpandCell(index, newSize);
        }

        public void CellWantToCollapse(int index)
        {
            if (!isInitialized)
                return;
            core?.CellWantToCollapse(index);
        }

        public void UpdateExpandedCellSize(int index, float newSize = -1)
        {
            if (!isInitialized)
                return;
            core?.UpdateExpandedCellSize(index, newSize);
        }

        public bool IsItemVisible(int index)
            => core?.IsItemVisible(index) ?? false;

        public void Clear()
        {
            if (!isInitialized)
                return;
            core?.Clear();
        }

        // Unity lifecycle
        protected override void OnEnable()
        {
            base.OnEnable();
            if (!isInitialized)
                return;

            // Trigger refresh on re-enable if needed
            // (existing behavior would go here)
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!isInitialized)
                return;

            if (Application.isEditor && !Application.isPlaying)
                return;

            // Clean up display (existing behavior)
        }

        protected override void OnDestroy()
        {
            if (core != null)
            {
                core.Cleanup();
                core = null;
            }

            if (components?.ScrollRect != null)
            {
                // Remove scroll listener if added
                components.ScrollRect.onValueChanged.RemoveAllListeners();
            }

            cellContainer?.Cleanup();
            viewportProvider = null;
            inputHandler?.Cleanup();
            base.OnDestroy();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (!isInitialized || gridResolver is not { IsDynamic: true })
                return;

            // Notify viewport provider of dimension change
            viewportProvider?.InvalidateViewportDimensions();

            // Grid may need recalculation (existing behavior)
        }

        // Drag event handling
        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            if (isInitialized)
                inputHandler?.HandlePointerDown();
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            if (isInitialized)
                inputHandler?.HandlePointerUp();
        }
    }
}
