using UnityEngine;
using UnityEngine.UI;

namespace SlotList
{
    public class ScrollMover
    {
        private readonly ScrollRect scrollRect;
        private readonly bool isVertical;
        private readonly bool reverse;

        public ScrollMover(ScrollRect scrollRect, bool isVertical, bool reverse)
        {
            this.scrollRect = scrollRect;
            this.isVertical = isVertical;
            this.reverse = reverse;
        }

        public void ScrollToStart()
        {
            float target = isVertical
                ? (reverse ? 0f : 1f)
                : (reverse ? 1f : 0f);
            ApplyNormalizedPosition(target);
        }

        public void ForceStop()
            => scrollRect.velocity = Vector2.zero;

        public void ApplyNormalizedPosition(float normalizedPosition)
        {
            if (isVertical)
                scrollRect.verticalNormalizedPosition = normalizedPosition;
            else
                scrollRect.horizontalNormalizedPosition = normalizedPosition;
        }
    }
}

