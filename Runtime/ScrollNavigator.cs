using RecyclableScroller.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclableScroller.Runtime
{
    public class ScrollNavigator
    {
        private readonly ScrollRect scrollRect;
        private readonly IScrollDirection direction;
        private readonly bool isVertical;
        private readonly bool reverse;

        public ScrollNavigator(ScrollRect scrollRect, IScrollDirection direction, bool isVertical, bool reverse)
        {
            this.scrollRect = scrollRect;
            this.direction = direction;
            this.isVertical = isVertical;
            this.reverse = reverse;
        }

        public void ScrollToLinePosition(float linePosition, float contentSize, float viewportSize)
        {
            float normalizedPos = direction.CalculateNormalizedScrollPosition(linePosition, contentSize, viewportSize);
            normalizedPos = Mathf.Clamp01(normalizedPos);
            ApplyNormalizedPosition(normalizedPos);
        }

        public void ScrollToStart()
        {
            float target = isVertical
                ? (reverse ? 0f : 1f)
                : (reverse ? 1f : 0f);
            ApplyNormalizedPosition(target);
        }

        public void ScrollToEnd()
        {
            float target = isVertical
                ? (reverse ? 1f : 0f)
                : (reverse ? 0f : 1f);
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

