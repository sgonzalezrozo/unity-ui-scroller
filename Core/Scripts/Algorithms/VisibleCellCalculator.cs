using Tools.RecyclableScroller.Core.Scripts.Pooling;
using UnityEngine;

namespace Tools.RecyclableScroller.Core.Scripts.Algorithms
{
    internal class VisibleCellCalculator
    {
        private readonly CellItem[] cellItems;

        public VisibleCellCalculator(CellItem[] cellItems)
        {
            this.cellItems = cellItems;
        }

        public int CalculateVisibleCellCount(
            float viewportSize,
            float paddingStart,
            float paddingEnd,
            float cellSpacing,
            System.Func<int, float> getCellSize)
        {
            if (cellItems == null || cellItems.Length == 0)
                return 1;

            float averageCellSize = CalculateAverageCellSize(getCellSize);
            float availableSpace = CalculateAvailableSpace(viewportSize, paddingStart, paddingEnd);
            float cellWithSpacing = averageCellSize + cellSpacing;

            if (cellWithSpacing <= 0f)
                return 1;

            int visibleCount = Mathf.CeilToInt(availableSpace / cellWithSpacing);
            visibleCount = Mathf.Max(visibleCount, 1);

            return visibleCount;
        }

        private float CalculateAverageCellSize(System.Func<int, float> getCellSize)
        {
            float sum = 0f;
            int sampleSize = Mathf.Min(cellItems.Length, 5);

            for (int i = 0; i < sampleSize; i++)
                sum += getCellSize(i);

            return sampleSize > 0 ? sum / sampleSize : 0f;
        }

        private static float CalculateAvailableSpace(float viewportSize, float paddingStart, float paddingEnd)
        {
            float available = viewportSize - paddingStart - paddingEnd;
            return Mathf.Max(0f, available);
        }
    }
}

