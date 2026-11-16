using UnityEngine;

namespace UIScroller
{
    public class LeftToRightLayout : AxisLayoutBase
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

            slot.anchoredPosition = new Vector2(linePosition, yPosition);
            slot.sizeDelta = new Vector2(lineSize, itemHeight);
            slot.pivot = new Vector2(0f, 1f);
            slot.anchorMin = new Vector2(0f, 1f);
            slot.anchorMax = new Vector2(0f, 1f);
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

