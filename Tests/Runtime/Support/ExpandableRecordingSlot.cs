using System.Collections.Generic;
using SlotList;
using UnityEngine;
using UnityEngine.UI;

namespace SlotList.Tests.PlayMode
{
    /// <summary>A test slot that can expand. It records binds and the expanded state the scroller gives it.</summary>
    public class ExpandableRecordingSlot : MonoBehaviour, IExpandableSlot<TestItem>
    {
        public float Size = 100f;
        public readonly List<TestItem> Binds = new();
        public int CollapseCalls;

        /// <summary>
        /// When above 0, the details appear this many frames after the slot opens, like text that arrives late
        /// (localization, a download). Closing hides them at once.
        /// </summary>
        public static int DetailsDelayFrames;

        /// <summary>When set (LayoutContent), binding gives this child the item's Height.</summary>
        public LayoutElement Body;

        /// <summary>When set (LayoutContent), the part shown only while expanded.</summary>
        public LayoutElement Details;

        private TestItem data;
        private bool isExpanded;

        public float DefaultExtent => Size;
        public ISlotList Owner { get; set; }
        public int ItemIndex { get; set; }
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                isExpanded = value;
                if (Details == null)
                    return;

                if (value && DetailsDelayFrames > 0 && isActiveAndEnabled)
                    StartCoroutine(ShowDetailsLater(DetailsDelayFrames));
                else
                    Details.gameObject.SetActive(value);
            }
        }

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

        private System.Collections.IEnumerator ShowDetailsLater(int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return null;

            if (isExpanded)
                Details.gameObject.SetActive(true);
        }

        public void OnCollapsed()
        {
            CollapseCalls++;
            IsExpanded = false;
        }
    }
}
