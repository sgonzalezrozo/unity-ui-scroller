using UnityEngine;

namespace UIScroller
{
    internal class PoolSizeEstimator
    {
        private readonly LayoutModel layout;

        public PoolSizeEstimator(LayoutModel layout)
        {
            this.layout = layout;
        }

        public int CalculateVisibleCount(
            float viewportSize,
            float paddingStart,
            float paddingEnd,
            float itemSpacing,
            System.Func<int, float> getItemSize)
        {
            if (layout.Placements.Length == 0)
                return 1;

            float averageItemSize = CalculateAverageItemSize(getItemSize);
            float availableSpace = CalculateAvailableSpace(viewportSize, paddingStart, paddingEnd);
            float itemWithSpacing = averageItemSize + itemSpacing;

            if (itemWithSpacing <= 0f)
                return 1;

            int visibleCount = Mathf.CeilToInt(availableSpace / itemWithSpacing);
            visibleCount = Mathf.Max(visibleCount, 1);

            return visibleCount;
        }

        private float CalculateAverageItemSize(System.Func<int, float> getItemSize)
        {
            float sum = 0f;
            int sampleSize = Mathf.Min(layout.Placements.Length, 5);

            for (int i = 0; i < sampleSize; i++)
                sum += getItemSize(i);

            return sampleSize > 0 ? sum / sampleSize : 0f;
        }

        private static float CalculateAvailableSpace(float viewportSize, float paddingStart, float paddingEnd)
        {
            float available = viewportSize - paddingStart - paddingEnd;
            return Mathf.Max(0f, available);
        }
    }
}

