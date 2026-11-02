namespace Tools.RecyclableScroller.Scripts
{
    public enum ScrollTickSignal
    {
        None,
        RebuildVisible
    }

    public class ScrollActivityTracker
    {
        private readonly float movingThreshold;

        private bool isScrolling;
        private bool isDragging;
        private float dragMinScrollPosition;

        public bool IsScrolling => isScrolling;
        public bool IsDragging => isDragging;
        public float DragMinScrollPosition => dragMinScrollPosition;

        public ScrollActivityTracker(float movingThreshold)
        {
            this.movingThreshold = movingThreshold;
        }

        public ScrollTickSignal HandleScrollTick(float velocityMagnitude, float currentScrollPosition)
        {
            isScrolling = velocityMagnitude > movingThreshold;

            if (isDragging && currentScrollPosition < dragMinScrollPosition)
                dragMinScrollPosition = currentScrollPosition;

            return ScrollTickSignal.RebuildVisible;
        }

        public void HandleDragBegin(bool isInitialized, float scrollPosition)
        {
            isDragging = true;
            dragMinScrollPosition = isInitialized ? scrollPosition : 0f;
        }

        public void HandleDragEnd()
            => isDragging = false;

        public void ForceStopScrolling()
            => isScrolling = false;
    }
}

