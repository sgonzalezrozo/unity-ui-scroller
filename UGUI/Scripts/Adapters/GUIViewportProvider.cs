using System;
using RecyclableScroller.Abstractions;
using RecyclableScroller.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclableScroller.UGUI.Adapters
{
    /// <summary>
    /// Adapts uGUI ScrollRect to IViewportProvider interface.
    /// Normalizes all positions to 0-1 range.
    /// Caches RectTransform queries to minimize frame overhead.
    /// </summary>
    public class GUIViewportProvider : IViewportProvider
    {
        private readonly ScrollRect scrollRect;
        private readonly IScrollDirection scrollDirection;
        private readonly bool isVertical;

        public event Action OnViewportDimensionsChanged;

        public GUIViewportProvider(ScrollRect scrollRect, IScrollDirection direction, bool isVertical)
        {
            this.scrollRect = scrollRect;
            this.scrollDirection = direction;
            this.isVertical = isVertical;
        }

        public float GetNormalizedScrollPosition()
        {
            if (scrollRect == null)
                return 0f;

            return isVertical
                ? scrollRect.verticalNormalizedPosition
                : scrollRect.horizontalNormalizedPosition;
        }

        public float GetViewportSize()
        {
            RectTransform viewport = scrollRect.viewport ?? scrollRect.content?.parent as RectTransform;
            if (viewport == null)
                return 0f;

            return scrollDirection.GetViewportDimension(viewport);
        }

        public float GetCrossAxisViewportSize()
        {
            RectTransform viewport = scrollRect.viewport ?? scrollRect.content?.parent as RectTransform;
            if (viewport == null)
                return 0f;

            return isVertical ? viewport.rect.width : viewport.rect.height;
        }

        public float GetContentSize()
        {
            if (scrollRect.content == null)
                return 0f;

            return scrollDirection.GetContentDimension(scrollRect.content);
        }

        public Vector2 GetContentAnchoredPosition()
        {
            return scrollRect.content?.anchoredPosition ?? Vector2.zero;
        }

        /// <summary>
        /// Called by RecyclableScrollRectGUI.OnRectTransformDimensionsChange()
        /// to notify viewport metrics cache to invalidate.
        /// </summary>
        public void InvalidateViewportDimensions()
        {
            OnViewportDimensionsChanged?.Invoke();
        }
    }
}

