using Tools.RecyclableScroller.Core.Scripts.Pooling;
using UnityEngine;

namespace Tools.RecyclableScroller.Core.Scripts.Directions
{
    public class HorizontalLeftToRightDirection : ScrollDirectionBase
    {
        public override float GetViewportDimension(RectTransform viewport)
            => viewport.rect.width;

        public override float GetContentDimension(RectTransform content)
            => content.rect.width;

        public override float GetScrollPosition(RectTransform content)
            => content.anchoredPosition.x;

        public override void ConfigureContentTransform(RectTransform content)
        {
            SetContentTransformPivotAndAnchor(
                content,
                pivot: new Vector2(0f, 0.5f),
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 1f));
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
            cell.anchoredPosition = new Vector2(cellPosition, 0);
            float height = viewport != null ? viewport.rect.height : content.rect.height;
            cell.sizeDelta = new Vector2(cellSize, height);
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
            float totalHeight = viewport != null ? viewport.rect.height : content.rect.height;
            float totalSpacing = gridCellSpacing * (totalCellsInLine - 1);
            float cellHeight = (totalHeight - totalSpacing) / totalCellsInLine;
            float yPosition = -cellIndexInLine * (cellHeight + gridCellSpacing);

            cell.anchoredPosition = new Vector2(linePosition, yPosition);
            cell.sizeDelta = new Vector2(lineSize, cellHeight);
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

