using System;
using ToolsCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if SLOTLIST_LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
#endif

namespace SlotList
{
    /// <summary>
    /// What every list shares: settings, slot prefab, events, the API and Unity callbacks. The work happens in an internal
    /// engine. Derive from <see cref="SlotList{T}"/> for a plain list or <see cref="SectionedSlotList{TItem, THeader}"/>
    /// for sections with headers, not from this class.
    /// </summary>
    public abstract partial class SlotListBase : ScrollRect, ISlotList, ISlotListHooks
    {
        /// <summary>Runs when initialization finishes.</summary>
        public Action OnInitialized;

        /// <summary>Raised when the list asks a paged source for its next page.</summary>
        public event Action PageRequested;

        /// <summary>Raised after a page was added, with the number of items it added.</summary>
        public event Action<int> PageLoaded;

        /// <summary>Raised when a page failed to load. With no subscriber, the failure is logged as a warning.</summary>
        public event Action<Exception> PageFailed;

        /// <summary>Raised once when a paged source has no more pages.</summary>
        public event Action ReachedEnd;

        /// <summary>Raised after an item was expanded, with its index.</summary>
        public event Action<int> ItemExpanded;

        /// <summary>Raised after an item was collapsed, with its index, including one closed because another opened.</summary>
        public event Action<int> ItemCollapsed;

        /// <summary>Raised after the list rebuilt its lines: Refresh, SetSource, or a change of slots per line.</summary>
        public event Action Rebuilt;

        /// <summary>
        /// Raised when the list changes Content's size along the scroll axis, with the new size: items measured,
        /// opened or closed, pages added, Refresh. Not raised by scrolling.
        /// </summary>
        public event Action<float> ContentSizeChanged;

        /// <summary>Raised when the list settles on an item after a drag (Settings.snap), with the item's index. With page snapping, the first item on the page.</summary>
        public event Action<int> SnappedTo;

        [SerializeField] protected GameObject slotPrefab;
        [SerializeField] private SlotListSettings settings = new();
        [SerializeField] private TextAsset bentoLayoutJson;

        private BentoLayout bentoLayout;

        internal IPrefabInstantiator injectedInstantiator;

        /// <summary>Seconds per frame for scroll animations. Tests replace it; null uses unscaled time, like ScrollRect.</summary>
        internal Func<float> frameDeltaTime;

        private protected ISlotListEngine engine;

        /// <summary>The engine's work counters, for tests. Null before initialization.</summary>
        internal WorkCounters Work => engine?.Work;
        // Our own copy of the dragging pointer. The Input System UI module reuses one event object for every mouse
        // button, so the object a drag event came with may say Middle by LateUpdate, and ScrollRect ignores a
        // restart that isn't the left button: the drag would keep its old start, one loop copy away.
        private PointerEventData dragRestart;
        private bool isLeftDragging;
#if SLOTLIST_LOCALIZATION
        private bool isListeningToLocale;
#endif

        // Only SlotList<T> and SectionedSlotList in this assembly can derive from it.
        private protected SlotListBase()
        {
        }

        /// <summary>True once initialization has finished.</summary>
        public bool IsInitialized => engine?.IsInitialized ?? false;
        /// <summary>True while the list is moving.</summary>
        public bool IsScrolling => engine?.IsScrolling ?? false;
        /// <summary>True while the user is dragging.</summary>
        public bool IsDragging => engine?.IsDragging ?? false;
        /// <summary>The first visible item index, or -1 before initialization.</summary>
        public int VisibleStartIndex => engine?.VisibleStartIndex ?? -1;
        /// <summary>The last visible item index, or -1 before initialization.</summary>
        public int VisibleEndIndex => engine?.VisibleEndIndex ?? -1;
        /// <summary>True when the start of the list is visible.</summary>
        public bool IsFirstItemVisible => engine?.IsFirstItemVisible ?? false;
        /// <summary>True when the end of the list is visible.</summary>
        public bool IsLastItemVisible => engine?.IsLastItemVisible ?? false;

        /// <summary>The prefab used for slots. Setting it only has an effect before Initialize.</summary>
        public GameObject SlotPrefab
        {
            get => slotPrefab;
            set => slotPrefab = value;
        }

        /// <summary>The list's layout and behaviour settings. Changes apply on the next Initialize.</summary>
        public SlotListSettings Settings => settings;

        /// <summary>True once a paged source has no more pages. Set it to true to stop the list asking for more.</summary>
        protected bool HasReachedLastPage
        {
            get => engine?.HasReachedLastPage ?? false;
            set
            {
                if (engine != null)
                    engine.HasReachedLastPage = value;
            }
        }

        /// <summary>Gets the number of entries in the data source (items, plus headers in a sectioned list).</summary>
        public int GetItemCount()
            => engine?.ItemCount ?? 0;

        // ---------- Hooks a subclass can override ----------

        protected virtual void OnPullToRefreshDetected()
        {
        }

        protected virtual void OnPaginationThresholdReached()
        {
        }

        private protected virtual bool IsHeaderEntry(int index) => false;

        private protected virtual bool SupportsPagingEntries() => false;

        private protected virtual bool SupportsLooping() => true;

        /// <summary>Called by Refresh before the engine rebuilds, so a source can recount.</summary>
        private protected virtual void PrepareRefresh()
        {
        }

        private protected abstract void BindEntry(SlotHandle slot, int index);

        /// <summary>A new engine for entries of type <typeparamref name="TEntry"/>, with this list's settings and prefabs.</summary>
        private protected SlotListEngine<TEntry> CreateEngine<TEntry>(GameObject headerPrefab)
            where TEntry : class
        {
            SlotListEngine<TEntry> created = new(this, this, this, settings, slotPrefab, headerPrefab, injectedInstantiator ?? ToolInjectionServices.PrefabInstantiator);
            created.BentoSource = ResolveBentoLayout();
            return created;
        }

        private BentoLayout ResolveBentoLayout()
        {
            if (bentoLayout != null)
                return bentoLayout;
            return bentoLayoutJson != null ? BentoLayout.FromJson(bentoLayoutJson.text) : null;
        }

        /// <summary>
        /// The bento layout (with Settings.bento on): where each item's tile goes. Set it before Initialize, or later
        /// to rebuild with the new layout. It replaces the Bento Layout JSON field. Null uses 1x1 tiles.
        /// </summary>
        public void SetBentoLayout(BentoLayout layout)
        {
            bentoLayout = layout;
            engine?.SetBentoLayout(layout ?? ResolveBentoLayout());
        }

        /// <summary>An item's tile in a bento list: its first column and row, and its span. A 0x0 tile outside bento.</summary>
        public BentoTile GetTile(int index) => engine?.QueryTile(index) ?? default;

        bool ISlotListHooks.IsHeader(int index) => IsHeaderEntry(index);
        void ISlotListHooks.BindItem(SlotHandle slot, int index) => BindEntry(slot, index);
        bool ISlotListHooks.SupportsPaging() => SupportsPagingEntries();
        bool ISlotListHooks.SupportsLooping() => SupportsLooping();
        void ISlotListHooks.OnPageRequested()
        {
            OnPaginationThresholdReached();
            PageRequested?.Invoke();
        }

        void ISlotListHooks.OnPageLoaded(int itemCount) => PageLoaded?.Invoke(itemCount);
        void ISlotListHooks.OnReachedEnd() => ReachedEnd?.Invoke();

        void ISlotListHooks.OnPageFailed(Exception exception)
        {
            if (PageFailed == null)
                Debug.LogWarning($"[SlotList] A page failed to load: {exception.Message}");

            PageFailed?.Invoke(exception);
        }

        void ISlotListHooks.OnPullRefreshRequested() => OnPullToRefreshDetected();
        void ISlotListHooks.OnReady() => OnInitialized?.Invoke();
        void ISlotListHooks.OnItemExpanded(int index) => ItemExpanded?.Invoke(index);
        void ISlotListHooks.OnItemCollapsed(int index) => ItemCollapsed?.Invoke(index);
        void ISlotListHooks.OnRebuilt() => Rebuilt?.Invoke();
        void ISlotListHooks.OnContentSizeChanged(float size) => ContentSizeChanged?.Invoke(size);
        void ISlotListHooks.OnSnapped(int index) => SnappedTo?.Invoke(index);

        /// <summary>
        /// The engine moved the content to keep the shown items still. ScrollRect remembers where a drag started and
        /// reads speed from the last position, so restart the drag from the pointer's current position and take the
        /// new position as the last one: no jump on the next drag event, and no speed from our shift.
        /// </summary>
        void ISlotListHooks.OnContentShifted()
        {
            if (isLeftDragging && IsActive())
                base.OnBeginDrag(dragRestart);

            UpdatePrevData();
        }

        // ---------- API ----------

        /// <summary>
        /// Measures an item again, for slots measured with layout: at once if it's on screen, otherwise when it's shown.
        /// Content changes the slot shows itself are noticed without this; call it when the size depends on something else.
        /// </summary>
        public void InvalidateSize(int index) => engine?.InvalidateSize(index);

        /// <summary>Where the item's line starts along the scroll axis, from the start of Content (padding included). -1 before initialization or for a bad index.</summary>
        public float GetItemOffset(int index) => engine?.QueryItemOffset(index) ?? -1f;

        /// <summary>The size of the item's line along the scroll axis (in a grid, its tallest slot). -1 before initialization or for a bad index.</summary>
        public float GetItemSize(int index) => engine?.QueryItemSize(index) ?? -1f;

        /// <summary>The viewport's size along the scroll axis. 0 before initialization.</summary>
        public float ViewportSize => engine?.ViewportSize ?? 0f;

        /// <summary>Content's size along the scroll axis, which the list sets. 0 before initialization.</summary>
        public float ContentSize => engine?.ContentSize ?? 0f;

        /// <summary>How many items are expanded right now.</summary>
        public int ExpandedCount => engine?.ExpandedCount ?? 0;

        /// <summary>
        /// Measures every item again, for slots measured with layout, keeping the first item on screen still.
        /// Called for you when the list's width changes and, with Unity Localization installed, when the language changes.
        /// </summary>
        public void InvalidateAllSizes() => engine?.InvalidateAllSizes();

        /// <summary>Rebuilds the list from the current data source.</summary>
        public void Refresh()
        {
            if (engine == null)
            {
                Debug.LogWarning("[SlotList] Refresh aborted - not initialized");
                return;
            }

            PrepareRefresh();
            engine.Refresh();
        }

        /// <summary>Clears displayed slots and resets the scroll position.</summary>
        public void Clear() => engine?.Clear();

        /// <summary>Checks whether an item is visible.</summary>
        public bool IsItemVisible(int index) => engine?.IsItemVisible(index) ?? false;

        /// <summary>
        /// Scrolls to an item: at once, or animated over <paramref name="duration"/> seconds (a drag cancels it).
        /// <paramref name="offset"/> leaves that many pixels between the item and the edge it's aligned to.
        /// </summary>
        public void ScrollToItem(int index, ScrollAlign align = ScrollAlign.Start, float duration = 0f, float offset = 0f)
            => engine?.ScrollToItem(index, align, duration, offset);

        /// <summary>Scrolls to the start of the list, at once or animated over <paramref name="duration"/> seconds.</summary>
        public void ScrollToStart(float duration = 0f) => engine?.ScrollToStart(duration);

        /// <summary>Scrolls to the end of the list, at once or animated over <paramref name="duration"/> seconds.</summary>
        public void ScrollToEnd(float duration = 0f) => engine?.ScrollToEnd(duration);

        /// <summary>Stops any scroll movement, including a scroll animation or a snap.</summary>
        public void ForceStopScrolling() => engine?.ForceStopScrolling();

        /// <summary>Checks whether a slot is expanded.</summary>
        public bool IsExpanded(int index) => engine?.IsExpanded(index) ?? false;

        /// <summary>Collapses an expanded slot.</summary>
        public void Collapse(int index) => engine?.Collapse(index);

        /// <summary>Resizes an expanded slot. Not supported in grids, ignored for slots measured with layout.</summary>
        public void SetExpandedSize(int index, float newSize = -1) => engine?.SetExpandedSize(index, newSize);

        /// <summary>Expands a slot. Ignored in grids. The size is used for fixed-size slots only.</summary>
        public void Expand(int index, float newSize = -1) => engine?.Expand(index, newSize);

        // ---------- Unity callbacks ----------

        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            RememberLeftDrag(eventData);
            engine?.OnDragBegin();
        }

        public override void OnDrag(PointerEventData eventData)
        {
            RememberLeftDrag(eventData);
            base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                isLeftDragging = false;
            base.OnEndDrag(eventData);
            engine?.OnDragEnd();
        }

        private void RememberLeftDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            dragRestart ??= new PointerEventData(EventSystem.current);
            dragRestart.button = PointerEventData.InputButton.Left;
            dragRestart.position = eventData.position;
            dragRestart.pointerPressRaycast = eventData.pointerPressRaycast;
            isLeftDragging = true;
        }

        /// <summary>Applies the sizes measured slots reported during this frame's layout pass, then moves scroll animations on.</summary>
        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (engine == null)
                return;

            engine.ApplyPendingResizes();
            engine.Tick(frameDeltaTime?.Invoke() ?? Time.unscaledDeltaTime);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            engine?.OnEnabled();
            ListenToLocale(true);
        }

        protected override void OnDisable()
        {
            ListenToLocale(false);
            base.OnDisable();
            engine?.OnDisabled();
        }

        /// <summary>With Unity Localization installed, a language change invalidates every size. Play mode only.</summary>
        private void ListenToLocale(bool listen)
        {
#if SLOTLIST_LOCALIZATION
            if (listen == isListeningToLocale || (listen && (!Application.isPlaying || !LocalizationSettings.HasSettings)))
                return;

            if (listen)
                LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            else
                LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            isListeningToLocale = listen;
#endif
        }

#if SLOTLIST_LOCALIZATION
        private void OnLocaleChanged(Locale locale) => InvalidateAllSizes();
#endif

        protected override void OnDestroy()
        {
            engine?.Dispose();
            base.OnDestroy();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            engine?.OnViewportResized();
        }
    }
}
