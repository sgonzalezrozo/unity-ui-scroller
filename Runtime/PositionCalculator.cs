using RecyclableScroller.Runtime.ScrollStrategies;
using UnityEngine;

namespace RecyclableScroller.Runtime
{
    public class PositionCalculator
    {
        private float[] cachedPositions;
        private bool needsRecalc = true;
        private IScrollStrategy scrollStrategy;

        public void SetScrollStrategy(IScrollStrategy strategy)
            => scrollStrategy = strategy;

        public bool NeedsRecalculation()
            => needsRecalc;

        public void MarkForRecalculation()
            => needsRecalc = true;

        public void RecalculateCachedPositions(CellItem[] cellItems, float paddingStart, float cellSpacing)
        {
            if (cachedPositions == null || cachedPositions.Length != cellItems.Length)
                cachedPositions = new float[cellItems.Length];

            scrollStrategy?.RecalculateCachedPositions(cellItems, cachedPositions, paddingStart, cellSpacing);
            needsRecalc = false;
        }

        public float GetCachedPosition(int index)
        {
            if (cachedPositions == null || index >= cachedPositions.Length)
                return 0f;

            return cachedPositions[index];
        }

        public int FindStartIndex(float scrollPosition, CellItem[] cellItems)
        {
            if (scrollStrategy != null && cachedPositions != null)
                return scrollStrategy.FindStartIndex(scrollPosition, cellItems, cachedPositions);

            return 0;
        }

        public int FindEndIndex(float scrollPosition, float viewportSize, CellItem[] cellItems, int startIndex)
        {
            if (scrollStrategy != null && cachedPositions != null)
                return scrollStrategy.FindEndIndex(scrollPosition, viewportSize, cellItems, cachedPositions, startIndex);

            return startIndex;
        }

        public void PositionCell(
            RectTransform cell,
            int index,
            CellItem[] cellItems,
            float viewportSize,
            float paddingStart,
            float paddingEnd,
            RectTransform viewport)
        {
            float position = GetCachedPosition(index);

            scrollStrategy?.PositionCell(cell, index, position, cellItems[index].Size, viewportSize, paddingStart, paddingEnd, viewport);
        }
    }
}

