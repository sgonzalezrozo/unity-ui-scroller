using UnityEngine;

namespace UIScroller
{
    /// <summary>The scroll distance that shows an item with an alignment, clamped to the content. Distances count from the list's start.</summary>
    internal static class ScrollTargets
    {
        public static float For(float itemStart, float itemSize, float viewportSize, float contentSize, float current, ScrollAlign align, float offset)
        {
            float target = align switch
            {
                ScrollAlign.Center => itemStart + (itemSize - viewportSize) * 0.5f - offset,
                ScrollAlign.End => itemStart + itemSize - viewportSize + offset,
                ScrollAlign.Nearest => Nearest(itemStart, itemSize, viewportSize, current, offset),
                _ => itemStart - offset
            };

            return Clamp(target, viewportSize, contentSize);
        }

        public static float Clamp(float distance, float viewportSize, float contentSize)
            => Mathf.Clamp(distance, 0f, MaxDistance(viewportSize, contentSize));

        public static float MaxDistance(float viewportSize, float contentSize)
            => Mathf.Max(0f, contentSize - viewportSize);

        private static float Nearest(float itemStart, float itemSize, float viewportSize, float current, float offset)
        {
            float itemEnd = itemStart + itemSize;
            if (itemStart >= current && itemEnd <= current + viewportSize)
                return current;

            if (itemStart < current || itemSize >= viewportSize)
                return itemStart - offset;

            return itemEnd - viewportSize + offset;
        }
    }
}
