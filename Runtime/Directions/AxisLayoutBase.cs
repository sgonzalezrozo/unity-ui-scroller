using UnityEngine;

namespace SlotList
{
    /// <summary>
    /// Base class for scroll direction strategies.
    /// Provides common functionality for all direction implementations.
    /// </summary>
    public abstract class AxisLayoutBase : IAxisLayout
    {
        public abstract float GetViewportDimension(RectTransform viewport);
        public abstract float GetContentDimension(RectTransform content);
        public abstract float GetScrollPosition(RectTransform content);
        public abstract void ConfigureContentTransform(RectTransform content);

        public abstract void PositionInLine(
            RectTransform slot,
            float linePosition,
            float lineSize,
            int itemIndexInLine,
            int totalItemsInLine,
            RectTransform viewport,
            RectTransform content,
            float slotSpacing);

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

