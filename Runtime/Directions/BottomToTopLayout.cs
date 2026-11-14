using UnityEngine;

namespace SlotList
{
    public class BottomToTopLayout : AxisLayoutBase
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
                pivot: new Vector2(0.5f, 0f),
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 0f));
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

            slot.anchoredPosition = new Vector2(xPosition, linePosition);
            slot.sizeDelta = new Vector2(itemWidth, lineSize);
            slot.pivot = new Vector2(0f, 0f);
            slot.anchorMin = new Vector2(0f, 0f);
            slot.anchorMax = new Vector2(0f, 0f);
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

