using RecyclableScroller.Core.Pooling;
using UnityEngine;

namespace RecyclableScroller.Core.Interfaces
{
    public interface IScrollDirection
    {
        float GetViewportDimension(RectTransform viewport);
        float GetContentDimension(RectTransform content);
        float GetScrollPosition(RectTransform content);
        void ConfigureContentTransform(RectTransform content);
        float CalculateCachedPositions(
            CellItem[] cellItems,
            float[] cachedPositions,
            float paddingStart,
            float paddingEnd,
            float cellSpacing);

        void PositionCell(
            RectTransform cell,
            float cellPosition,
            float cellSize,
            RectTransform viewport,
            RectTransform content);

        void PositionCellInLine(
            RectTransform cell,
            float linePosition,
            float lineSize,
            int cellIndexInLine,
            int totalCellsInLine,
            RectTransform viewport,
            RectTransform content,
            float gridCellSpacing);

        float CalculateNormalizedScrollPosition(
            float targetPosition,
            float contentSize,
            float viewportSize);
    }
}

