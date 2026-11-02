using UnityEngine;

namespace Tools.RecyclableScroller.Scripts
{
    public enum PaginationSignal
    {
        None,
        Trigger,
        Reset
    }

    public class PaginationBoundaryDetector
    {
        private readonly float triggerNormalized;
        private readonly float resetNormalized;

        private bool hasTriggered;

        public PaginationBoundaryDetector(float triggerNormalized, float resetNormalized)
        {
            this.triggerNormalized = triggerNormalized;
            this.resetNormalized = resetNormalized;
        }

        public void ResetTriggerFlag()
            => hasTriggered = false;

        public PaginationSignal Evaluate(float scrollPosition, float contentSize, float viewportSize)
        {
            float scrollableDistance = Mathf.Max(0f, contentSize - viewportSize);

            if (scrollableDistance <= 0f)
                return PaginationSignal.None;

            float normalizedPosition = Mathf.Abs(scrollPosition) / scrollableDistance;

            if (normalizedPosition >= triggerNormalized && !hasTriggered)
            {
                hasTriggered = true;
                return PaginationSignal.Trigger;
            }

            if (normalizedPosition < resetNormalized && hasTriggered)
            {
                hasTriggered = false;
                return PaginationSignal.Reset;
            }

            return PaginationSignal.None;
        }
    }
}

