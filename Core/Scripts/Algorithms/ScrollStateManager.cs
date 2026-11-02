using UnityEngine;

namespace RecyclableScroller.Core.Algorithms
{
    public class ScrollStateManager
    {
        private float lastScrollPosition = -1f;
        private (int, int) visibleRange = (-1, -1);
        private const float SCROLL_POSITION_THRESHOLD = 0.01f;

        public int VisibleStartIndex => visibleRange.Item1;
        public int VisibleEndIndex => visibleRange.Item2;

        public bool HasScrollPositionChanged(float currentScrollPos)
            => Mathf.Abs(currentScrollPos - lastScrollPosition) > SCROLL_POSITION_THRESHOLD;

        public void UpdateLastScrollPosition(float scrollPos)
            => lastScrollPosition = scrollPos;

        public bool HasVisibilityRangeChanged(int newStart, int newEnd)
            => visibleRange.Item1 != newStart || visibleRange.Item2 != newEnd;

        public void UpdateVisibilityRange(int newStart, int newEnd)
            => visibleRange = (newStart, newEnd);

        public float GetScrollPositionDelta(float currentScrollPos)
            => currentScrollPos - lastScrollPosition;

        public void ResetState()
        {
            lastScrollPosition = -1f;
            visibleRange = (-1, -1);
        }
    }
}

