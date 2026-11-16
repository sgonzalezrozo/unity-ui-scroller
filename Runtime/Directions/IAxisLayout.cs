using UnityEngine;

namespace UIScroller
{
    public interface IAxisLayout
    {
        float GetViewportDimension(RectTransform viewport);
        float GetContentDimension(RectTransform content);
        float GetScrollPosition(RectTransform content);
        void ConfigureContentTransform(RectTransform content);

        void PositionInLine(
            RectTransform slot,
            float linePosition,
            float lineSize,
            int itemIndexInLine,
            int totalItemsInLine,
            RectTransform viewport,
            RectTransform content,
            float slotSpacing);

        float CalculateNormalizedScrollPosition(
            float targetPosition,
            float contentSize,
            float viewportSize);
    }
}

