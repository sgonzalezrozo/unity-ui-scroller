using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIScroller
{
    /// <summary>
    /// Where every item sits: one Placement per item, Lines of placements (a header takes a whole line),
    /// line offsets along the scroll axis, the total extent, and which line each item is in.
    /// Arrays keep spare capacity and Placement and Line objects are reused, so rebuilds and appended pages only
    /// create what's new (P11). Offsets are computed on demand, only as far as someone asks: a size change near the
    /// screen costs the lines up to the screen, not every line after it. The total extent is a running sum.
    /// </summary>
    public sealed partial class LayoutModel
    {
        private Placement[] placementBuffer = Array.Empty<Placement>();
        private int placementCount;
        private Line[] lineBuffer = Array.Empty<Line>();
        private int lineCount;

        private float[] offsets = Array.Empty<float>();
        private int[] itemToLine = Array.Empty<int>();
        private bool[] measured = Array.Empty<bool>();
        private bool[] inAverage = Array.Empty<bool>();
        private float measuredTotal;
        private int measuredCount;
        // Kept as lines change size, so the total extent never walks the lines (see OnLineResized).
        private double lineSizeSum;
        private int sectionStarts;
        private bool buildingLines;
        private double sizeBeforeBuild;
        private int sectionStartsBeforeBuild;
        private int placeholderLinesBeforeBuild;

        // Lines whose items are all unmeasured follow one shared placeholder size (Line.UsesPlaceholder), so a new
        // placeholder doesn't visit them. Lines with their own size that still hold unmeasured items are tracked.
        private float unmeasuredSize;
        private int placeholderLineCount;
        private int firstPlaceholderLine = int.MaxValue;
        private bool needsFullPlaceholderPass = true;
        private readonly List<Line> trackedLines = new();
        private float paddingStart;
        private float paddingEnd;
        private float lineSpacing;
        private float sectionSpacing;

        /// <summary>Counts created placements and lines, for tests. Optional.</summary>
        internal WorkCounters Work;

        // Buffers shrink back when the item count falls under 1/SHRINK_FACTOR of what they hold, and above this many
        // entries, keeping KEEP_FACTOR times the count: small changes, pages and swapping between sources of similar
        // sizes keep reusing them (a tighter rule dropped and recreated thousands of lines on every such swap), and a
        // big drop gives the memory back.
        private const int SHRINK_FACTOR = 4;
        private const int KEEP_FACTOR = 2;
        private const int MIN_KEPT = 256;

        internal int PlacementCapacity => placementBuffer.Length;
        internal int LineCapacity => lineBuffer.Length;
        internal int OffsetCapacity => offsets.Length;

        public ListView<Placement> Placements => new(placementBuffer, placementCount);
        public ListView<Line> Lines => new(lineBuffer, lineCount);

        public bool HasOffsets => lineCount > 0;

        /// <summary>The size every line made only of unmeasured items has: the last one given to <see cref="SetUnmeasuredSize"/>.</summary>
        internal float UnmeasuredSize => unmeasuredSize;

        /// <summary>Padding plus every line's current size plus the spacing between lines.</summary>
        public float TotalExtent
        {
            get
            {
                if (lineCount == 0)
                    return 0f;

                return (float)(paddingStart + lineSizeSum + placeholderLineCount * (double)unmeasuredSize + lineSpacing * (double)(lineCount - 1) + sectionSpacing * (double)sectionStarts + paddingEnd);
            }
        }

        /// <summary>Padding at both ends, the space between lines, and the extra space before every header but the first.</summary>
        public void SetSpacing(float paddingStartValue, float paddingEndValue, float lineSpacingValue, float sectionSpacingValue = 0f)
        {
            paddingStart = paddingStartValue;
            paddingEnd = paddingEndValue;
            lineSpacing = lineSpacingValue;
            sectionSpacing = sectionSpacingValue;
            offsetsValidCount = 0;
        }

        /// <summary>Sets up <paramref name="itemCount"/> fresh placements, reusing the existing objects. None may hold a slot.</summary>
        public void CreatePlacements(int itemCount)
        {
            ShrinkItemBuffers(itemCount);
            EnsureItemCapacity(itemCount);
            for (int i = 0; i < itemCount; i++)
            {
                if (placementBuffer[i] == null)
                {
                    placementBuffer[i] = new Placement(i);
                    if (Work != null)
                        Work.PlacementsCreated++;
                }
                else
                {
                    placementBuffer[i].Reset(i);
                }
            }

            placementCount = itemCount;
            Array.Clear(measured, 0, measured.Length);
            Array.Clear(inAverage, 0, inAverage.Length);
            measuredTotal = 0f;
            measuredCount = 0;
        }

        /// <summary>Grows the placements to <paramref name="itemCount"/>, keeping the existing ones (and the slots they hold).</summary>
        public void ExtendPlacements(int itemCount)
        {
            if (itemCount <= placementCount)
                return;

            EnsureItemCapacity(itemCount);
            for (int i = placementCount; i < itemCount; i++)
            {
                if (placementBuffer[i] == null)
                {
                    placementBuffer[i] = new Placement(i);
                    if (Work != null)
                        Work.PlacementsCreated++;
                }
                else
                {
                    placementBuffer[i].Reset(i);
                }

                measured[i] = false;
                inAverage[i] = false;
            }

            placementCount = itemCount;
        }

        /// <summary>After a big drop in item count, drops the records past it (their objects are garbage collected).</summary>
        private void ShrinkItemBuffers(int itemCount)
        {
            if (!ShouldShrink(placementBuffer.Length, itemCount))
                return;

            int keep = KeptCapacity(itemCount);
            Array.Resize(ref placementBuffer, keep);
            Array.Resize(ref itemToLine, keep);
            Array.Resize(ref measured, keep);
            Array.Resize(ref inAverage, keep);
        }

        private static bool ShouldShrink(int capacity, int count)
            => capacity > Math.Max(count, MIN_KEPT) * SHRINK_FACTOR;

        private static int KeptCapacity(int count)
            => Math.Max(count * KEEP_FACTOR, MIN_KEPT);

        private void EnsureItemCapacity(int itemCount)
        {
            if (itemCount <= placementBuffer.Length)
                return;

            int capacity = Math.Max(itemCount, placementBuffer.Length * 2);
            Array.Resize(ref placementBuffer, capacity);
            Array.Resize(ref itemToLine, capacity);
            Array.Resize(ref measured, capacity);
            Array.Resize(ref inAverage, capacity);
        }
    }
}
