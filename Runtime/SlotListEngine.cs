using System.Collections.Generic;
using ToolsCore;
using UnityEngine;
using UnityEngine.UI;

namespace SlotList
{
    /// <summary>
    /// Everything a SlotList does, without the MonoBehaviour: setup, layout, recycling, binding, expansion,
    /// paging and pull detection, navigation, rebuilds and deferred work. SlotList forwards to it.
    /// Split into partial files: setup and binding (this file), sizing and measuring, rebuilds and lifecycle, interaction, paging.
    /// </summary>
    internal sealed partial class SlotListEngine<T> : ISlotCallbacks, IItemSizes, ISlotSizeListener, ISlotListEngine
        where T : class
    {
        private const float PULL_THRESHOLD = 75f;
        private const float MOVING_THRESHOLD = 20f;
        private const float EXPANDED_SIZE_FALLBACK_MULTIPLIER = 2f;
        private const float DEFAULT_ITEM_SIZE = 100f;
        private const int MAX_MEASURE_PASSES = 8;
        private const float SIZE_TOLERANCE = 0.5f;
        private const int HEADER_POOL_SIZE = 2;
        private const int WARMUP_PER_FRAME = 2;
        private const float PLACEHOLDER_MIN_CHANGE = 0.5f;
        private const float PLACEHOLDER_RELATIVE_CHANGE = 0.005f;

        private readonly MonoBehaviour host;
        private readonly ISlotList owner;
        private readonly ISlotListHooks hooks;
        private readonly SlotListSettings settings;
        private readonly GameObject slotPrefab;
        private readonly GameObject headerPrefab;
        private readonly IPrefabInstantiator instantiator;
        private readonly LayoutModel layout = new();
        private readonly Dictionary<int, float> pendingResizes = new();
        private readonly HashSet<int> newlyMeasuredLines = new();
        private readonly HashSet<int> remeasuring = new();
        private readonly List<int> changedBuffer = new();
        private readonly System.Func<int, bool> isHeaderFunc;
        private readonly System.Func<int, float> itemSizeFunc;
        private readonly System.Func<int, float> lineSizeFunc;
        private readonly System.Func<int, bool> isHeaderEntryFunc;
        private float appliedPlaceholder = float.NaN;
        private bool slotRootHasLayout;
        private float lastContentSize = -1f;

        private ScrollRectBinding components;
        private AxisMetrics viewportMetrics;
        private SlotsPerLineResolver gridResolver;
        private VisibleRangeTracker visibleRange;
        private VisibilityPass visibilityPass;
        private ExpansionState expansion;
        private SlotRecycler recycler;
        private SlotPool slotPool;
        private PoolSizeEstimator visibleCountEstimator;
        private PullGesture pullGesture;
        private ScrollMover mover;
        private DragState dragState;

        private float viewportSize;
        private bool isInitialized;
        private bool isLayoutSized;
        private float placeholderSize = DEFAULT_ITEM_SIZE;
        private bool isHeaderLayoutSized;
        private float headerExtent = DEFAULT_ITEM_SIZE;
        private float headerPlaceholderSize = DEFAULT_ITEM_SIZE;
        private bool sizesChanged;
        private bool hasWarnedAboutMissingLayout;
        private bool needsPositionRecalculation = true;
        private bool forceRebindAll;

        public IItemSource<T> Source { get; private set; }
        public bool HasReachedLastPage { get; set; }
        public bool IsInitialized => isInitialized;
        public bool IsScrolling => (dragState?.IsScrolling ?? false) || scrollAnimation.IsRunning;
        public bool IsDragging => dragState?.IsDragging ?? false;
        public int VisibleStartIndex => isBento ? FirstItemInVisibleLines() : ToReal(FirstItemIn(FirstVisibleLine));
        public int VisibleEndIndex => isBento ? LastItemInVisibleLines() : ToReal(LastItemIn(LastVisibleLine));
        public bool IsFirstItemVisible => isInitialized && !isLooping && FirstVisibleLine == 0;
        public bool IsLastItemVisible => isInitialized && !isLooping && layout.Lines.Length > 0 && LastVisibleLine == layout.Lines.Length - 1;

        private int FirstVisibleLine => visibleRange?.VisibleStartIndex ?? -1;
        private int LastVisibleLine => visibleRange?.VisibleEndIndex ?? -1;
        /// <summary>Entries laid out: the items, or every copy of them in a looping list.</summary>
        public int ItemCount => isLooping ? loopEntries : RealCount;
        public WorkCounters Work { get; } = new();

        public SlotListEngine(MonoBehaviour host, ISlotList owner, ISlotListHooks hooks, SlotListSettings settings, GameObject slotPrefab, GameObject headerPrefab, IPrefabInstantiator instantiator)
        {
            this.headerPrefab = headerPrefab;
            this.host = host;
            this.owner = owner;
            this.hooks = hooks;
            this.settings = settings;
            this.slotPrefab = slotPrefab;
            this.instantiator = instantiator ?? ToolInjectionServices.PrefabInstantiator;
            layout.Work = Work;

            // Cached once: a method group passed as a delegate allocates a new delegate on every call.
            isHeaderFunc = hooks.IsHeader;
            itemSizeFunc = GetItemSize;
            lineSizeFunc = GetLineSizeConsideringExpansion;
            isHeaderEntryFunc = IsHeaderEntry;
        }

        // ---------- Setup ----------

        /// <summary>Starts the list. Calling it again after initialization only changes the source it reads from.</summary>
        public void Initialize(IItemSource<T> source)
        {
            AttachSource(source);
            InitializeBase();
        }

        private void InitializeBase()
        {
            if (isInitialized)
                return;

            if (slotPrefab == null)
            {
                Debug.LogError($"[SlotList] Slot Prefab is not assigned to {host.gameObject.name}. Assign a prefab implementing ISlot in the Inspector.");
                return;
            }

            HasReachedLastPage = !hooks.SupportsPaging();
            ReadSizingFromPrefab();

            if (!BindScrollRect())
                return;

            UpdateBento();
            UpdateLooping();
            layout.CreatePlacements(ItemCount);
            isInitialized = true;

            CreateParts();
            BuildLines();
            RecalculateContentSize();
            layout.RecomputeOffsets();
            components.ScrollRect.onValueChanged.AddListener(OnScroll);
            CreatePool();

            lastCrossAxisSize = viewportMetrics.GetCrossAxisViewportSize();
            BuildFirstScreen();
            ScheduleDeferredPoolWarmup();
        }

        private bool BindScrollRect()
        {
            components = new ScrollRectBinding();
            if (!components.TryBind(host, settings.IsVertical, settings.IsReversed, slotPrefab))
                return false;

            viewportMetrics = new AxisMetrics(components.Direction, components.Viewport, components.Content, settings.IsVertical, settings.IsReversed);
            gridResolver = new SlotsPerLineResolver(settings.slotsPerLine, settings.autoSlotsPerLine, settings.targetSlotSize, settings.slotSpacing, settings.minSlotsPerLine, settings.maxSlotsPerLine);

            viewportSize = viewportMetrics.GetViewportSize();
            gridResolver.Resolve(viewportMetrics.GetCrossAxisViewportSize());
            layout.SetSpacing(settings.paddingStart, settings.paddingEnd, settings.lineSpacing, headerPrefab != null ? settings.sectionSpacing : 0f);
            return true;
        }

        private void CreateParts()
        {
            visibleRange = new VisibleRangeTracker();
            expansion = new ExpansionState(layout, this, isLayoutSized);
            pullGesture = new PullGesture(PULL_THRESHOLD, settings.IsVertical, settings.IsReversed);
            mover = new ScrollMover(components.ScrollRect, settings.IsVertical, settings.IsReversed);
            dragState = new DragState(MOVING_THRESHOLD);
            visibilityPass = new VisibilityPass(viewportMetrics, layout, new VisibleRangeFinder(settings.IsVertical, settings.IsReversed), visibleRange, Work);
        }

        private void CreatePool()
        {
            int visibleCount = GetActualVisibleItemCount();
            int poolItemsNeeded = isBento ? BentoPoolSize() : Mathf.CeilToInt(visibleCount * settings.EffectivePoolBuffer) * gridResolver.Current;
            int totalItemsNeeded = Mathf.Min(poolItemsNeeded, ItemCount);

            slotPool?.DestroyAllSlots();
            slotPool = new SlotPool(slotPrefab, components.Content, totalItemsNeeded, instantiator, headerPrefab, headerPrefab != null ? HEADER_POOL_SIZE : 0, settings.IsVertical);
            recycler = new SlotRecycler(slotPool, components, layout, expansion, this, settings, Work);
            recycler.BentoColumns = isBento ? bentoColumns : 0;
            recycler.BentoTiles = bentoTiles;

            int poolBufferEnd = Mathf.Min(visibleCount + (poolItemsNeeded - visibleCount), layout.Lines.Length - 1);
            recycler.AppearPolicy.ConfigureNonAnimatingRange(visibleCount, poolBufferEnd);
            visibilityPass.UpdateWiring(recycler);
        }

        private void BuildLines(int fromItem = 0)
        {
            if (isBento)
            {
                BuildBentoLines(fromItem);
                return;
            }

            layout.BuildLines(ItemCount, gridResolver.Current, isHeaderFunc, itemSizeFunc, lineSizeFunc, fromItem);
        }

        private float GetLineSizeConsideringExpansion(int itemIndex)
            => expansion.GetSizeOrDefault(itemIndex, GetItemSize(itemIndex));

        private float GetDefaultItemSizeFromPrefab()
        {
            ISlot slot = slotPrefab != null ? slotPrefab.GetComponent<ISlot>() : null;
            return slot?.DefaultExtent ?? DEFAULT_ITEM_SIZE;
        }

        /// <summary>The first item index in a line, or -1.</summary>
        private int FirstItemIn(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= layout.Lines.Length)
                return -1;

            Line line = layout.Lines[lineIndex];
            for (int i = 0; i < line.SlotCount; i++)
            {
                Placement placement = line[i];
                if (placement != null)
                    return placement.DataIndex;
            }

            return -1;
        }

        /// <summary>The last item index in a line, or -1.</summary>
        private int LastItemIn(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= layout.Lines.Length)
                return -1;

            Line line = layout.Lines[lineIndex];
            for (int i = line.SlotCount - 1; i >= 0; i--)
            {
                Placement placement = line[i];
                if (placement != null)
                    return placement.DataIndex;
            }

            return -1;
        }

        private int GetActualVisibleItemCount()
        {
            visibleCountEstimator ??= new PoolSizeEstimator(layout);
            return visibleCountEstimator.CalculateVisibleCount(viewportSize, settings.paddingStart, settings.paddingEnd, settings.lineSpacing, GetItemSize);
        }

        float IItemSizes.GetDefaultSize(int index)
            => GetItemSize(index);

        float IItemSizes.GetExpandedSize(int index)
            => GetItemSize(index) * EXPANDED_SIZE_FALLBACK_MULTIPLIER;

        void IItemSizes.OnCollapsed(int index)
        {
        }

        void IItemSizes.OnExpanded(int index, float newSize)
        {
        }

        // ---------- Binding ----------

        int ISlotCallbacks.ItemCount => ItemCount;

        bool ISlotCallbacks.IsHeader(int index)
            => hooks.IsHeader(index);

        void ISlotCallbacks.BindItem(SlotHandle slot, int index)
            => hooks.BindItem(slot, ToReal(index));

        void ISlotCallbacks.SetupExpandable(SlotHandle slot, int index)
        {
            if (slot?.Expandable == null || index < 0 || index >= layout.Placements.Length || layout.Placements[index] == null)
                return;

            slot.Expandable.Owner = owner;
            slot.Expandable.ItemIndex = index;
        }

        /// <summary>The default binding: sets Data (and IsExpanded on expandable slots), or hides the slot for a null item.</summary>
        public void BindDefault(SlotHandle slot, int index)
        {
            if (slot == null)
                return;

            // Already a real item: ISlotCallbacks.BindItem converts the entry before the list's BindItem runs.
            T item = Source.GetItem(index);
            if (item == null)
            {
                slot.GameObject.SetActive(false);
                return;
            }

            if (slot.Slot is IExpandableSlot<T> expandable)
            {
                expandable.Data = item;
                expandable.IsExpanded = IsExpanded(index);
                return;
            }

            if (slot.Slot is ISlot<T> plain)
            {
                plain.Data = item;
                return;
            }

            Debug.LogError($"[SlotList] Index {index} - the slot does not implement ISlot<{typeof(T).Name}> or IExpandableSlot<{typeof(T).Name}>");
        }
    }
}
