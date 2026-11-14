namespace SlotList
{
    public class VisibleRangeTracker
    {
        private (int, int) visibleRange = (-1, -1);

        public int VisibleStartIndex => visibleRange.Item1;
        public int VisibleEndIndex => visibleRange.Item2;

        public bool HasVisibilityRangeChanged(int newStart, int newEnd)
            => visibleRange.Item1 != newStart || visibleRange.Item2 != newEnd;

        public void UpdateVisibilityRange(int newStart, int newEnd)
            => visibleRange = (newStart, newEnd);

        public void ResetState()
            => visibleRange = (-1, -1);
    }
}

