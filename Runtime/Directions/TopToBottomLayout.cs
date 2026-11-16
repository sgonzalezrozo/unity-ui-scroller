using UnityEngine;

namespace UIScroller
{
    public class TopToBottomLayout : AxisLayoutBase
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
            float totalWidth = viewport != null ? viewport.rect.width : content.rect.width;
            float totalSpacing = slotSpacing * (totalItemsInLine - 1);
            float itemWidth = (totalWidth - totalSpacing) / totalItemsInLine;
            float xPosition = itemIndexInLine * (itemWidth + slotSpacing);

            slot.anchoredPosition = new Vector2(xPosition, -linePosition);
            slot.sizeDelta = new Vector2(itemWidth, lineSize);
            slot.pivot = new Vector2(0f, 1f);
            slot.anchorMin = new Vector2(0f, 1f);
            slot.anchorMax = new Vector2(0f, 1f);
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

