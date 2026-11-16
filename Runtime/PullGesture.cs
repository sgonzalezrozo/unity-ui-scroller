using UnityEngine;

namespace UIScroller
{
    public class PullGesture
    {
        private readonly float pullThreshold;
        private readonly bool isVertical;
        private readonly bool reverse;

        public PullGesture(float pullThreshold, bool isVertical, bool reverse)
        {
            this.pullThreshold = pullThreshold;
            this.isVertical = isVertical;
            this.reverse = reverse;
        }

        public bool WasPullDetected(Vector2 contentAnchoredPosition, float contentSize)
        {
            if (isVertical)
                return CheckVertical(contentAnchoredPosition.y, contentSize);

            return CheckHorizontal(contentAnchoredPosition.x, contentSize);
        }

        private bool CheckVertical(float contentY, float contentSize)
        {
            if (reverse)
                return contentY > contentSize + pullThreshold;

            return contentY < -pullThreshold;
        }

        private bool CheckHorizontal(float contentX, float contentSize)
        {
            if (reverse)
                return contentX < -(contentSize + pullThreshold);

            return contentX > pullThreshold;
        }
    }
}

