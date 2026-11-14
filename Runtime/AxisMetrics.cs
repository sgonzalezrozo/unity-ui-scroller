using UnityEngine;

namespace SlotList
{
    public class AxisMetrics
    {
        private readonly IAxisLayout direction;
        private readonly RectTransform viewportTransform;
        private readonly RectTransform contentTransform;
        private readonly bool isVertical;

        // Content moves +y (top to bottom), -y (bottom to top), -x (left to right) or +x (right to left) as the list scrolls on.
        private readonly float scrollSign;

        public AxisMetrics(IAxisLayout direction, RectTransform viewportTransform, RectTransform contentTransform, bool isVertical, bool isReversed)
        {
            scrollSign = isVertical != isReversed ? 1f : -1f;
            this.direction = direction;
            this.viewportTransform = viewportTransform;
            this.contentTransform = contentTransform;
            this.isVertical = isVertical;
        }

        public float GetViewportSize()
        {
            RectTransform viewport = ResolveViewport();
            return viewport == null ? 0f : direction.GetViewportDimension(viewport);
        }

        public float GetCrossAxisViewportSize()
        {
            RectTransform viewport = ResolveViewport();
            if (viewport == null)
                return 0f;

            return isVertical ? viewport.rect.width : viewport.rect.height;
        }

        public float GetContentSize()
            => isVertical ? contentTransform.rect.height : contentTransform.rect.width;

        public float GetScrollPosition()
            => direction.GetScrollPosition(contentTransform);

        /// <summary>How far the list is scrolled from its start, in pixels (0 at the start).</summary>
        public float GetScrollDistance()
            => (isVertical ? contentTransform.anchoredPosition.y : contentTransform.anchoredPosition.x) * scrollSign;

        /// <summary>Moves the content so the list is scrolled <paramref name="distance"/> pixels from its start.</summary>
        public void SetScrollDistance(float distance)
        {
            Vector2 position = contentTransform.anchoredPosition;
            if (isVertical)
                position.y = distance * scrollSign;
            else
                position.x = distance * scrollSign;
            contentTransform.anchoredPosition = position;
        }

        /// <summary>A ScrollRect velocity as pixels per second along the list (positive towards its end).</summary>
        public float ToScrollSpeed(Vector2 velocity)
            => (isVertical ? velocity.y : velocity.x) * scrollSign;

        public Vector2 GetContentAnchoredPosition()
            => contentTransform.anchoredPosition;

        private RectTransform ResolveViewport()
            => viewportTransform ?? contentTransform?.parent as RectTransform;
    }
}

