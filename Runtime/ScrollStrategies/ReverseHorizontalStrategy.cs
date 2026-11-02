using UnityEngine;

namespace RecyclableScroller.Runtime.ScrollStrategies
{
    public class ReverseHorizontalStrategy : IScrollStrategy
    {
        public void RecalculateCachedPositions(CellItem[] cellItems, float[] cachedPositions, float paddingStart, float cellSpacing)
        {
            float position = 0;
            for (int i = cellItems.Length - 1; i >= 0; i--)
            {
                cachedPositions[i] = position;
                position += cellItems[i].Size + cellSpacing;
            }
        }

        public int FindStartIndex(float scrollPosition, CellItem[] cellItems, float[] cachedPositions)
        {
            if (cachedPositions.Length == 0)
                return cellItems.Length - 1;

            int left = 0;
            int right = cachedPositions.Length - 1;
            int result = cellItems.Length - 1;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                float cellStart = cachedPositions[mid];
                float cellEnd = cellStart + cellItems[mid].Size;

                if (cellStart < scrollPosition && cellEnd > 0)
                {
                    result = mid;
                    left = mid + 1;
                }
                else if (cellEnd <= 0)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            return Mathf.Max(0, result);
        }

        public int FindEndIndex(float scrollPosition, float viewportSize, CellItem[] cellItems, float[] cachedPositions, int startIndex)
        {
            if (cachedPositions.Length == 0)
                return 0;

            int result = startIndex;
            for (int i = startIndex; i >= 0; i--)
            {
                if (cachedPositions[i] < scrollPosition)
                {
                    result = i;
                    break;
                }
            }

            return result;
        }

        public void PositionCell(RectTransform cell, int index, float position, float cellSize, float viewportSize, float paddingStart, float paddingEnd, RectTransform viewport)
        {
            float cellHeight = viewport != null ? viewport.rect.height - paddingStart - paddingEnd : 0;
            if (cellHeight <= 0 && viewport != null)
                cellHeight = viewport.rect.height;

            cell.anchoredPosition = new Vector2(-position, -paddingStart);
            cell.sizeDelta = new Vector2(cellSize, cellHeight);
        }
    }
}
