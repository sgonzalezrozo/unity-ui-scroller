using UnityEngine;

namespace Tools.RecyclableScroller.Scripts.ScrollStrategies
{
    public class NormalVerticalStrategy : IScrollStrategy
    {
        public void RecalculateCachedPositions(CellItem[] cellItems, float[] cachedPositions, float paddingStart, float cellSpacing)
        {
            float position = paddingStart;
            for (int i = 0; i < cellItems.Length; i++)
            {
                cachedPositions[i] = position;
                position += cellItems[i].Size + cellSpacing;
            }
        }

        public int FindStartIndex(float scrollPosition, CellItem[] cellItems, float[] cachedPositions)
        {
            if (cachedPositions.Length == 0)
                return 0;

            int left = 0;
            int right = cachedPositions.Length - 1;
            int result = 0;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                float cellStart = cachedPositions[mid];
                float cellEnd = cellStart + cellItems[mid].Size;

                if (cellEnd > scrollPosition)
                {
                    result = mid;
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            return result;
        }

        public int FindEndIndex(float scrollPosition, float viewportSize, CellItem[] cellItems, float[] cachedPositions, int startIndex)
        {
            if (cachedPositions.Length == 0)
                return 0;

            float endScrollPosition = scrollPosition + viewportSize;
            int left = startIndex;
            int right = cachedPositions.Length - 1;
            int result = startIndex;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                float cellStart = cachedPositions[mid];

                if (cellStart < endScrollPosition)
                {
                    result = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return Mathf.Min(result, cachedPositions.Length - 1);
        }

        public void PositionCell(RectTransform cell, int index, float position, float cellSize, float viewportSize, float paddingStart, float paddingEnd, RectTransform viewport)
        {
            float cellWidth = viewport != null ? viewport.rect.width - paddingStart - paddingEnd : 0;
            if (cellWidth <= 0 && viewport != null)
                cellWidth = viewport.rect.width;

            cell.anchoredPosition = new Vector2(paddingStart, -position);
            cell.sizeDelta = new Vector2(cellWidth, cellSize);
        }
    }
}
