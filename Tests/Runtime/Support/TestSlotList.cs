using System;
using SlotList;

namespace SlotList.Tests.PlayMode
{
    /// <summary>The concrete scroller used by the PlayMode tests. Counts the overridable hooks.</summary>
    public class TestSlotList : SlotList<TestItem>
    {
        public int PaginationCalls;
        public int PullCalls;
        public int BindCalls;

        protected override void OnPaginationThresholdReached()
            => PaginationCalls++;

        protected override void OnPullToRefreshDetected()
            => PullCalls++;

        protected override void BindItem(SlotHandle slot, int index)
        {
            BindCalls++;
            base.BindItem(slot, index);
        }
    }
}
