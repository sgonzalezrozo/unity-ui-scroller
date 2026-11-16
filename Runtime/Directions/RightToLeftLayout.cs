using UnityEngine;

namespace UIScroller
{
    public class RightToLeftLayout : AxisLayoutBase
    {
        public override float GetViewportDimension(RectTransform viewport)
            => viewport.rect.width;

        public override float GetContentDimension(RectTransform content)
            => content.rect.width;

        public override float GetScrollPosition(RectTransform content)
            => -content.anchoredPosition.x;

        public override void ConfigureContentTransform(RectTransform content)
        {
            SetContentTransformPivotAndAnchor(
                content,
                pivot: new Vector2(1f, 0.5f),
                anchorMin: new Vector2(1f, 0f),
                anchorMax: new Vector2(1f, 1f));
        }

        public override void PositionInLine(
            RectTransform slot,
            float linePosition,
            float lineSize,
            int itemIndexInLine,
            int totalItemsInLine,
            RectTransform viewport,
            RectTransform content,
            float slotSpacing)
        {
            float totalHeight = viewport != null ? viewport.rect.height : content.rect.height;
            float totalSpacing = slotSpacing * (totalItemsInLine - 1);
            float itemHeight = (totalHeight - totalSpacing) / totalItemsInLine;
            float yPosition = -itemIndexInLine * (itemHeight + slotSpacing);

            slot.anchoredPosition = new Vector2(-linePosition, yPosition);
            slot.sizeDelta = new Vector2(lineSize, itemHeight);
            slot.pivot = new Vector2(1f, 1f);
            slot.anchorMin = new Vector2(1f, 1f);
            slot.anchorMax = new Vector2(1f, 1f);
        }

        public override float CalculateNormalizedScrollPosition(
            float targetPosition,
            float contentSize,
            float viewportSize)
        {
            // The start of this layout is normalized 1 (top, or right), so the position is inverted.
            float maxScroll = contentSize - viewportSize;
            return maxScroll > 0f
                ? 1f - targetPosition / maxScroll
                : 1f;
        }
    }
}

