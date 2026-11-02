using Tools.RecyclableScroller.Core.Scripts.Interfaces;
using Tools.RecyclableScroller.Core.Scripts.Pooling;
using UnityEngine;

namespace Tools.RecyclableScroller.Core.Scripts.Directions
{
    /// <summary>
    /// Base class for scroll direction strategies.
    /// Provides common functionality for all direction implementations.
    /// </summary>
    public abstract class ScrollDirectionBase : IScrollDirection
    {
        public abstract float GetViewportDimension(RectTransform viewport);
        public abstract float GetContentDimension(RectTransform content);
        public abstract float GetScrollPosition(RectTransform content);
        public abstract void ConfigureContentTransform(RectTransform content);
        public abstract float CalculateCachedPositions(
            CellItem[] cellItems,
            float[] cachedPositions,
            float paddingStart,
            float paddingEnd,
            float cellSpacing);

        public abstract void PositionCell(
            RectTransform cell,
            float cellPosition,
            float cellSize,
            RectTransform viewport,
            RectTransform content);

        public abstract void PositionCellInLine(
            RectTransform cell,
            float linePosition,
            float lineSize,
            int cellIndexInLine,
            int totalCellsInLine,
            RectTransform viewport,
            RectTransform content,
            float gridCellSpacing);

        public abstract float CalculateNormalizedScrollPosition(
            float targetPosition,
            float contentSize,
            float viewportSize);

        protected void SetContentTransformPivotAndAnchor(
            RectTransform content,
            Vector2 pivot,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            content.pivot = pivot;
            content.anchorMin = anchorMin;
            content.anchorMax = anchorMax;
        }

        protected RectTransform GetViewportOrParent(RectTransform content)
        {
            return content.parent as RectTransform ?? content;
        }
    }
}

