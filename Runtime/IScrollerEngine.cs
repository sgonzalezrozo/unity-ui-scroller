namespace UIScroller
{
    /// <summary>What ScrollerBase calls on its engine, whatever the engine's entry type.</summary>
    internal interface IScrollerEngine
    {
        bool IsInitialized { get; }
        bool IsScrolling { get; }
        bool IsDragging { get; }
        int VisibleStartIndex { get; }
        int VisibleEndIndex { get; }
        bool IsFirstItemVisible { get; }
        bool IsLastItemVisible { get; }
        bool HasReachedLastPage { get; set; }
        int ItemCount { get; }
        WorkCounters Work { get; }

        void Refresh();
        void Clear();
        bool IsItemVisible(int index);
        void ScrollToItem(int index, ScrollAlign align, float duration, float offset);
        void ScrollToStart(float duration);
        void ScrollToEnd(float duration);
        void ForceStopScrolling();
        void Tick(float deltaTime);

        bool IsExpanded(int index);
        void Expand(int index, float newSize);
        void Collapse(int index);
        void SetExpandedSize(int index, float newSize);

        float QueryItemOffset(int index);
        float QueryItemSize(int index);
        BentoTile QueryTile(int index);
        void SetBentoLayout(BentoLayout layout);
        float ViewportSize { get; }
        float ContentSize { get; }
        int ExpandedCount { get; }

        void InvalidateSize(int index);
        void InvalidateAllSizes();
        void ApplyPendingResizes();
        void RunDeferredWork();

        void OnDragBegin();
        void OnDragEnd();
        void OnEnabled();
        void OnDisabled();
        void OnViewportResized();
        void Dispose();
    }
}
