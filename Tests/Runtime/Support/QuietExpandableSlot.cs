using UnityEngine;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>An expandable slot that allocates nothing when bound or opened. Used by the allocation tests.</summary>
    public class QuietExpandableSlot : MonoBehaviour, IExpandableSlot<TestItem>
    {
        public float Size = 100f;
        public int BindCount;

        private TestItem data;

        public float DefaultExtent => Size;
        public IScroller Owner { get; set; }
        public int ItemIndex { get; set; }
        public bool IsExpanded { get; set; }

        public TestItem Data
        {
            get => data;
            set
            {
                data = value;
                BindCount++;
            }
        }

        public void OnCollapsed()
            => IsExpanded = false;
    }
}
