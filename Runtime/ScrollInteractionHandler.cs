using UnityEngine;

namespace RecyclableScroller.Runtime
{
    public class ScrollInteractionHandler
    {
        private float lastScrollPosition;
        private const float SCROLL_DELTA_THRESHOLD = 5f;

        public bool HasScrollThresholdMet(float currentScrollPosition)
        {
            if (Mathf.Abs(currentScrollPosition - lastScrollPosition) < SCROLL_DELTA_THRESHOLD)
                return false;

            lastScrollPosition = currentScrollPosition;
            return true;
        }

        public void UpdateLastScrollPosition(float scrollPosition)
        {
            lastScrollPosition = scrollPosition;
        }

        public float GetScrollPosition(RectTransform contentTransform, bool isVertical)
        {
            return isVertical
                ? contentTransform.anchoredPosition.y
                : -contentTransform.anchoredPosition.x;
        }

        public void CheckPullActions(
            RectTransform contentTransform,
            bool isVertical,
            bool reverse,
            float contentSize,
            float viewportSize,
            RecyclableScrollConfig config)
        {
            if (config == null)
                return;

            float scrollPosition = GetScrollPosition(contentTransform, isVertical);

            if (reverse)
            {
                if (scrollPosition <= 0 && config.OnPullRefresh != null)
                    config.OnPullRefresh.Invoke();

                if (scrollPosition >= contentSize - viewportSize && config.OnPullLoadMore != null)
                    config.OnPullLoadMore.Invoke();
            }
            else
            {
                if (scrollPosition <= 0 && config.OnPullRefresh != null)
                    config.OnPullRefresh.Invoke();

                if (scrollPosition >= contentSize - viewportSize && config.OnPullLoadMore != null)
                    config.OnPullLoadMore.Invoke();
            }
        }
    }
}

