namespace UIScroller
{
    /// <summary>Where shown slots sit in the viewport, for carousel effects: told to slots that listen, and readable by index.</summary>
    internal sealed partial class ScrollerEngine<T>
    {
        private const float CENTRE = 0.5f;

        // Read once from the prefabs: lists whose slots don't listen skip the work entirely.
        private bool hasViewportListeners;

        /// <summary>Tells every shown slot that listens where it sits now. Runs after each visibility pass.</summary>
        private void NotifyViewportPositions()
        {
            if (!hasViewportListeners || !isInitialized || viewportSize <= 0f)
                return;

            int first = FirstVisibleLine;
            int last = LastVisibleLine;
            if (first < 0 || last < 0)
                return;

            float viewportCentre = viewportMetrics.GetScrollDistance() + viewportSize * CENTRE;
            for (int line = first; line <= last && line < layout.Lines.Length; line++)
            {
                Line row = layout.Lines[line];
                for (int slot = 0; slot < row.SlotCount; slot++)
                {
                    Placement placement = row[slot];
                    IViewportPositionListener listener = placement?.Handle?.ViewportListener;
                    if (listener != null)
                        listener.OnViewportPosition(PositionOf(line, placement, viewportCentre));
                }
            }
        }

        /// <summary>The item's position in the viewport (0 centre, ±0.5 the edges), or NaN when it isn't shown.</summary>
        public float QueryViewportPosition(int index)
        {
            if (!isInitialized || !IsValidPublicIndex(index) || viewportSize <= 0f)
                return float.NaN;

            int entry = ToNearestCopy(index);
            int line = LineIndexOf(entry);
            if (line < FirstVisibleLine || line > LastVisibleLine || line < 0)
                return float.NaN;

            Placement placement = layout.Placements[entry];
            float viewportCentre = viewportMetrics.GetScrollDistance() + viewportSize * CENTRE;
            return PositionOf(line, placement, viewportCentre);
        }

        private float PositionOf(int line, Placement placement, float viewportCentre)
        {
            float size = MainSizeOf(placement.DataIndex, line);
            float centre = layout.OffsetOf(line) + size * CENTRE;
            return (centre - viewportCentre) / viewportSize;
        }
    }
}
