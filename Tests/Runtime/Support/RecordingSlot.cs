using System.Collections.Generic;
using UIScroller;
using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>A test slot that records every item bound to it.</summary>
    public class RecordingSlot : MonoBehaviour, ISlot<TestItem>
    {
        public float Size = 100f;
        public readonly List<TestItem> Binds = new();

        /// <summary>When set, binding gives this child the item's Height, so a layout group on the root sizes the slot.</summary>
        public LayoutElement Body;

        private TestItem data;

        public float DefaultExtent => Size;

        public TestItem Data
        {
            get => data;
            set
            {
                data = value;
                Binds.Add(value);
                if (Body != null && value != null)
                    Body.preferredHeight = value.Height;
            }
        }
    }
}
