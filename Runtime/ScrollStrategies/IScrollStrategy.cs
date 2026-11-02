using UnityEngine;

namespace Tools.RecyclableScroller.Scripts.ScrollStrategies
{
    public interface IScrollStrategy
    {
        void RecalculateCachedPositions(CellItem[] cellItems, float[] cachedPositions, float paddingStart, float cellSpacing);
        int FindStartIndex(float scrollPosition, CellItem[] cellItems, float[] cachedPositions);
        int FindEndIndex(float scrollPosition, float viewportSize, CellItem[] cellItems, float[] cachedPositions, int startIndex);
        void PositionCell(RectTransform cell, int index, float position, float cellSize, float viewportSize, float paddingStart, float paddingEnd, RectTransform viewport);
    }
}

