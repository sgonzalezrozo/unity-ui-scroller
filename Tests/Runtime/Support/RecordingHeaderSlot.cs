using System.Collections.Generic;
using SlotList;
using UnityEngine;
using UnityEngine.UI;

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

                // A measured header (HeaderSize of 0 or less) is as tall as its header says.
                Transform body = transform.Find("Body");
                if (body != null && value != null)
                    body.GetComponent<LayoutElement>().preferredHeight = value.Height;
            }
        }
    }
}
