using UnityEngine;

namespace SlotList.Tests.PlayMode
{
    /// <summary>A header slot that allocates nothing when bound. Used by the allocation tests (RigOptions.QuietSlots).</summary>
    public class QuietHeaderSlot : MonoBehaviour, ISlot<TestHeader>
    {
        public float Size = 50f;
        public int BindCount;

        private TestHeader data;

        public float DefaultExtent => Size;

        public TestHeader Data
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
