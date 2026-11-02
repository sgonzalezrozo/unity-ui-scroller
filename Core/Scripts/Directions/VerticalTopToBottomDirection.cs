using RecyclableScroller.Core.Pooling;
using UnityEngine;

namespace RecyclableScroller.Core.Directions
{
    public class VerticalTopToBottomDirection : ScrollDirectionBase
    {
        public override float GetViewportDimension(RectTransform viewport)
            => viewport.rect.height;

        public override float GetContentDimension(RectTransform content)
            => content.rect.height;

        public override float GetScrollPosition(RectTransform content)
            => -content.anchoredPosition.y;

        public override void ConfigureContentTransform(RectTransform content)
        {
            SetContentTransformPivotAndAnchor(
                content,
                pivot: new Vector2(0.5f, 1f),
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f));
        }

        public override float CalculateCachedPositions(
            CellItem[] cellItems,
            float[] cachedPositions,
            float paddingStart,
            float paddingEnd,
            float cellSpacing)
        {
            float position = paddingStart;
            for (int i = 0; i < cellItems.Length; i++)
            {
                cachedPositions[i] = position;
                position += cellItems[i].Size + cellSpacing;
            }
            return position - cellSpacing + paddingEnd;
        }

        public override void PositionCell(
            RectTransform cell,
            float cellPosition,
            float cellSize,
            RectTransform viewport,
            RectTransform content)
        {
            cell.anchoredPosition = new Vector2(0, -cellPosition);
            float width = viewport != null ? viewport.rect.width : content.rect.width;
            cell.sizeDelta = new Vector2(width, cellSize);
        }

        public override void PositionCellInLine(
            RectTransform cell,
            float linePosition,
            float lineSize,
            int cellIndexInLine,
            int totalCellsInLine,
            RectTransform viewport,
            RectTransform content,
            float gridCellSpacing)
        {
            float totalWidth = viewport != null ? viewport.rect.width : content.rect.width;
            float totalSpacing = gridCellSpacing * (totalCellsInLine - 1);
            float cellWidth = (totalWidth - totalSpacing) / totalCellsInLine;
            float xPosition = cellIndexInLine * (cellWidth + gridCellSpacing);

            cell.anchoredPosition = new Vector2(xPosition, -linePosition);
            cell.sizeDelta = new Vector2(cellWidth, lineSize);
            cell.pivot = new Vector2(0f, 1f);
            cell.anchorMin = new Vector2(0f, 1f);
            cell.anchorMax = new Vector2(0f, 1f);
        }

        public override float CalculateNormalizedScrollPosition(
            float targetPosition,
            float contentSize,
            float viewportSize)
        {
            float maxScroll = contentSize - viewportSize;
            return Mathf.Max(0, maxScroll) > 0f
                ? targetPosition / Mathf.Max(0, maxScroll)
                : 0f;
        }
    }
}

