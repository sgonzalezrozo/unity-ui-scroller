using System.Collections.Generic;
using SlotList;
using UnityEngine;

namespace SlotList.Tests.PlayMode
{
    /// <summary>A header slot that records every header bound to it.</summary>
    public class RecordingHeaderSlot : MonoBehaviour, ISlot<TestHeader>
    {
        public float Size = 50f;
        public readonly List<TestHeader> Binds = new();

        private TestHeader data;

        public float DefaultExtent => Size;

        public TestHeader Data
        {
            get => data;
            set
            {
                data = value;
                Binds.Add(value);
            }
        }
    }
}
