using RecyclableScroller.Core.Interfaces;
using UnityEngine;

namespace RecyclableScroller.Runtime
{
    public class ViewportMetrics
    {
        private readonly IScrollDirection direction;
        private readonly RectTransform viewportTransform;
        private readonly RectTransform contentTransform;
        private readonly bool isVertical;

        public ViewportMetrics(IScrollDirection direction, RectTransform viewportTransform, RectTransform contentTransform, bool isVertical)
        {
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

        public Vector2 GetContentAnchoredPosition()
            => contentTransform.anchoredPosition;

        private RectTransform ResolveViewport()
            => viewportTransform ?? contentTransform?.parent as RectTransform;
    }
}

