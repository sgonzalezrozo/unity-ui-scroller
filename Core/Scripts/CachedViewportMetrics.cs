using RecyclableScroller.Abstractions;
using UnityEngine;

namespace RecyclableScroller.Core
{
    /// <summary>
    /// Caches viewport metrics to reduce repeated framework queries.
    /// Automatically invalidates on viewport dimension changes.
    /// Significantly improves performance by avoiding repeated RectTransform.rect reads.
    /// </summary>
    public class CachedViewportMetrics
    {
        private readonly IViewportProvider provider;

        private float cachedViewportSize = -1f;
        private float cachedCrossAxisSize = -1f;
        private float cachedContentSize = -1f;

        public CachedViewportMetrics(IViewportProvider provider)
        {
            this.provider = provider;
            provider.OnViewportDimensionsChanged += Invalidate;
        }

        public void Invalidate()
        {
            cachedViewportSize = -1f;
            cachedCrossAxisSize = -1f;
            cachedContentSize = -1f;
        }

        public float GetViewportSize()
        {
            if (cachedViewportSize < 0)
                cachedViewportSize = provider.GetViewportSize();
            return cachedViewportSize;
        }

        public float GetCrossAxisViewportSize()
        {
            if (cachedCrossAxisSize < 0)
                cachedCrossAxisSize = provider.GetCrossAxisViewportSize();
            return cachedCrossAxisSize;
        }

        public float GetContentSize()
        {
            if (cachedContentSize < 0)
                cachedContentSize = provider.GetContentSize();
            return cachedContentSize;
        }

        public Vector2 GetContentAnchoredPosition() => provider.GetContentAnchoredPosition();

        public float GetNormalizedScrollPosition() => provider.GetNormalizedScrollPosition();

        public void Cleanup()
        {
            provider.OnViewportDimensionsChanged -= Invalidate;
        }
    }
}

