using System;
using Tools.RecyclableScroller.Abstractions.Scripts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tools.RecyclableScroller.UIToolkit.Scripts.Adapters
{
    /// <summary>
    /// Adapts UIToolkit ScrollView to IViewportProvider.
    /// Returns normalized (0-1) positions using UIToolkit's layout metrics.
    /// </summary>
    public class UIToolkitViewportProvider : IViewportProvider
    {
        private readonly ScrollView scrollView;
        private readonly bool isVertical;

        private float cachedViewportSize = -1f;
        private float cachedCrossAxisSize = -1f;

        public event Action OnViewportDimensionsChanged;

        public UIToolkitViewportProvider(ScrollView scrollView, bool isVertical)
        {
            this.scrollView = scrollView;
            this.isVertical = isVertical;

            if (scrollView != null)
            {
                scrollView.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    cachedViewportSize = -1f;
                    cachedCrossAxisSize = -1f;
                    OnViewportDimensionsChanged?.Invoke();
                });
            }
        }

        public float GetNormalizedScrollPosition()
        {
            if (scrollView == null)
                return 0f;

            Vector2 scrollOffset = scrollView.scrollOffset;
            float contentSize = GetContentSize();
            float viewportSize = GetViewportSize();

            if (viewportSize <= 0.001f || contentSize <= 0.001f)
                return 0f;

            float scrollPos = isVertical ? scrollOffset.y : scrollOffset.x;
            float maxScroll = contentSize - viewportSize;

            if (maxScroll <= 0.001f)
                return 0f;

            return Mathf.Clamp01(scrollPos / maxScroll);
        }

        public float GetViewportSize()
        {
            if (cachedViewportSize < 0)
            {
                if (scrollView == null || scrollView.contentContainer == null)
                    cachedViewportSize = 0f;
                else
                {
                    Rect rect = scrollView.contentContainer.layout;
                    cachedViewportSize = isVertical ? rect.height : rect.width;
                }
            }
            return cachedViewportSize;
        }

        public float GetCrossAxisViewportSize()
        {
            if (cachedCrossAxisSize < 0)
            {
                if (scrollView == null || scrollView.contentContainer == null)
                    cachedCrossAxisSize = 0f;
                else
                {
                    Rect rect = scrollView.contentContainer.layout;
                    cachedCrossAxisSize = isVertical ? rect.width : rect.height;
                }
            }
            return cachedCrossAxisSize;
        }

        public float GetContentSize()
        {
            if (scrollView == null || scrollView.contentContainer == null)
                return 0f;

            Rect rect = scrollView.contentContainer.layout;
            return isVertical ? rect.height : rect.width;
        }

        public Vector2 GetContentAnchoredPosition()
        {
            if (scrollView == null)
                return Vector2.zero;

            return scrollView.scrollOffset;
        }
    }
}

