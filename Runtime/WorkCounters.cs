namespace UIScroller
{
    /// <summary>
    /// Counts the list's work, so tests can check it stays proportional to what's on screen, not to the item count.
    /// Plain increments: cheap enough to keep in release builds.
    /// </summary>
    internal sealed class WorkCounters
    {
        /// <summary>Shown slots checked when deciding which ones left the screen (P2).</summary>
        public int SlotsChecked;

        /// <summary>Slots given a new position and size (P3).</summary>
        public int SlotsPlaced;

        /// <summary>Scroll events that went on to work out the visible range (P4).</summary>
        public int RangeChecks;

        /// <summary>Placement objects created; rebuilds reuse the existing ones (P11).</summary>
        public int PlacementsCreated;

        /// <summary>Line objects created; rebuilds reuse the existing ones (P11).</summary>
        public int LinesCreated;

        /// <summary>Slots measured with ForceRebuildLayoutImmediate; cached sizes skip it.</summary>
        public int Measurements;

        /// <summary>Passes that gave every unmeasured item the new average size (each one visits every item).</summary>
        public int PlaceholderPasses;

        /// <summary>Line offsets computed; a size change near the screen computes only up to the screen.</summary>
        public int OffsetsComputed;

        public void Reset()
        {
            SlotsChecked = 0;
            SlotsPlaced = 0;
            RangeChecks = 0;
            PlacementsCreated = 0;
            LinesCreated = 0;
            Measurements = 0;
            OffsetsComputed = 0;
            PlaceholderPasses = 0;
        }
    }
}
