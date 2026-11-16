using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIScroller
{
    /// <summary>
    /// Added by the list to slots measured with layout (DefaultExtent of 0 or less). Unity calls it whenever it rebuilds
    /// the slot's layout; when the slot's preferred size changed (a late string, a loaded image) it tells the list,
    /// which resizes the slot after the layout pass. You never add it yourself.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class SlotSizeWatcher : UIBehaviour, ILayoutSelfController
    {
        private const float TOLERANCE = 0.5f;

        private ISlotSizeListener listener;
        private bool isVertical;
        private int itemIndex = -1;
        private float lastReported = -1f;

        internal int ItemIndex => itemIndex;

        /// <summary>Points the watcher at the item its slot now shows. A new item forgets the last reported size.</summary>
        internal void Watch(ISlotSizeListener sizeListener, int index, bool vertical)
        {
            listener = sizeListener;
            isVertical = vertical;
            if (index == itemIndex)
                return;

            itemIndex = index;
            lastReported = -1f;
        }

        /// <summary>Stops reporting: the slot now shows an item with a fixed size.</summary>
        internal void StopWatching()
        {
            itemIndex = -1;
            lastReported = -1f;
        }

        /// <summary>Records a size the list measured itself, so the watcher doesn't report it again.</summary>
        internal void MarkReported(float size)
            => lastReported = size;

        public void SetLayoutHorizontal()
        {
            if (!isVertical)
                Report();
        }

        public void SetLayoutVertical()
        {
            if (isVertical)
                Report();
        }

        private void Report()
        {
            if (listener == null || itemIndex < 0)
                return;

            float size = LayoutUtility.GetPreferredSize((RectTransform)transform, isVertical ? 1 : 0);
            if (size <= 0f || Mathf.Abs(size - lastReported) < TOLERANCE)
                return;

            lastReported = size;
            listener.OnSlotSizeReported(itemIndex, size);
        }
    }
}
