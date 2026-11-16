namespace UIScroller
{
    public class DragState
    {
        private readonly float movingThreshold;

        private bool isScrolling;
        private bool isDragging;

        public bool IsScrolling => isScrolling;
        public bool IsDragging => isDragging;

        public DragState(float movingThreshold)
        {
            this.movingThreshold = movingThreshold;
        }

        public void HandleScrollTick(float velocityMagnitude)
            => isScrolling = velocityMagnitude > movingThreshold;

        public void HandleDragBegin()
            => isDragging = true;

        public void HandleDragEnd()
            => isDragging = false;

        public void ForceStopScrolling()
            => isScrolling = false;
    }
}
