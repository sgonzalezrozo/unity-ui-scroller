namespace SlotList
{
    /// <summary>The overridable parts of a SlotList that its engine calls.</summary>
    internal interface ISlotListHooks
    {
        bool IsHeader(int index);
        void BindItem(SlotHandle slot, int index);
        bool SupportsPaging();
        bool SupportsLooping();
        void OnPageRequested();
        void OnPageLoaded(int itemCount);
        void OnPageFailed(System.Exception exception);
        void OnReachedEnd();
        void OnPullRefreshRequested();
        void OnReady();

        /// <summary>The engine moved the content to keep the shown items still. A drag in progress must restart from here.</summary>
        void OnContentShifted();

        void OnItemExpanded(int index);
        void OnItemCollapsed(int index);
        void OnRebuilt();
        void OnContentSizeChanged(float size);
        void OnSnapped(int index);
    }

    /// <summary>What SlotRecycler needs from the engine to show items.</summary>
    internal interface ISlotCallbacks
    {
        int ItemCount { get; }
        bool IsHeader(int index);
        void BindItem(SlotHandle slot, int index);
        void SetupExpandable(SlotHandle slot, int index);

        /// <summary>Called after a slot is shown. <paramref name="justBound"/> is true when it was bound in this pass.</summary>
        void OnSlotShown(SlotHandle slot, int index, bool justBound);

        /// <summary>True when a shown entry still needs measuring (never measured, or invalidated): its slot must be reported as shown even if it didn't move.</summary>
        bool NeedsMeasuring(int index);
    }

    /// <summary>Where a SlotSizeWatcher reports a slot whose preferred size changed. Called during Unity's layout pass.</summary>
    internal interface ISlotSizeListener
    {
        void OnSlotSizeReported(int index, float size);
    }

    /// <summary>Item sizes and expansion notifications ExpansionState needs.</summary>
    internal interface IItemSizes
    {
        float GetDefaultSize(int index);
        float GetExpandedSize(int index);
        void OnCollapsed(int index);
        void OnExpanded(int index, float newSize);
    }
}
