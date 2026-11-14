using UnityEngine;

namespace SlotList.Tests.PlayMode
{
    /// <summary>A test slot that allocates nothing when bound: it only counts binds. Used by the allocation tests.</summary>
    public class QuietSlot : MonoBehaviour, ISlot<TestItem>
    {
        public float Size = 100f;
        public int BindCount;

        private TestItem data;

        public float DefaultExtent => Size;

        public TestItem Data
        {
            get => data;
            set
            {
                data = value;
                BindCount++;
            }
        }
    }
}
