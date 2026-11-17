using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIScroller
{
    /// <summary>Line offsets, computed on demand up to the line asked for, and the searches the visible range uses.</summary>
    public sealed partial class LayoutModel
    {
        /// <summary>
        /// Readies the offset buffer for the current line count. Offsets themselves are computed on demand, from the
        /// lowest line that changed, up to the line asked for (<see cref="OffsetOf"/>, <see cref="LineAtDistance"/>).
        /// </summary>
        public void RecomputeOffsets()
            => EnsureOffsetCapacity();

        // Offsets below this many lines hold correct values. A size change lowers it; asking for an offset raises it.
        private int offsetsValidCount;

        // While frozen, size changes don't make computed offsets stale yet: they're applied on unfreeze.
        private bool offsetsFrozen;
        private int dirtyWhileFrozen = int.MaxValue;

        private void MarkDirtyFrom(int line)
        {
            if (offsetsFrozen)
            {
                dirtyWhileFrozen = Math.Min(dirtyWhileFrozen, line);
                return;
            }

            if (line < offsetsValidCount)
                offsetsValidCount = Math.Max(0, line);
        }

        /// <summary>
        /// Keeps the computed offsets as they are until <see cref="UnfreezeOffsets"/>, whatever sizes change meanwhile:
        /// a visibility pass places all its lines at the offsets it started with, as the engine's anchoring expects.
        /// Lines not computed yet are still computed when asked for.
        /// </summary>
        public void FreezeOffsets()
            => offsetsFrozen = true;

        public void UnfreezeOffsets()
        {
            offsetsFrozen = false;
            if (dirtyWhileFrozen != int.MaxValue)
                MarkDirtyFrom(dirtyWhileFrozen);
            dirtyWhileFrozen = int.MaxValue;
        }

        private void EnsureOffsetCapacity()
        {
            if (ShouldShrink(offsets.Length, lineCount))
            {
                Array.Resize(ref offsets, KeptCapacity(lineCount));
                offsetsValidCount = Math.Min(offsetsValidCount, offsets.Length);
            }

            if (offsets.Length < lineCount)
                Array.Resize(ref offsets, Math.Max(lineCount, offsets.Length * 2));
        }

        /// <summary>Computes the offsets up to and including <paramref name="lineIndex"/>.</summary>
        private void EnsureOffsets(int lineIndex)
        {
            if (lineIndex < offsetsValidCount)
                return;

            EnsureOffsetCapacity();
            int last = Math.Min(lineIndex, lineCount - 1);
            while (offsetsValidCount <= last)
                ComputeNextOffset();
        }

        /// <summary>Computes the offset of the first line not computed yet.</summary>
        private void ComputeNextOffset()
        {
            int i = offsetsValidCount;
            float position = i == 0 ? paddingStart : offsets[i - 1] + lineBuffer[i - 1].Size + lineSpacing;
            if (i > 0 && lineBuffer[i].StartsSection)
                position += sectionSpacing;

            offsets[i] = position;
            offsetsValidCount = i + 1;
            if (Work != null)
                Work.OffsetsComputed++;
        }

        /// <summary>Computes offsets until the last computed line ends past <paramref name="distance"/>, or all of them.</summary>
        private void EnsureOffsetsEndingPast(float distance)
        {
            EnsureOffsetCapacity();
            while (offsetsValidCount < lineCount)
            {
                int last = offsetsValidCount - 1;
                if (last >= 0 && offsets[last] + lineBuffer[last].Size > distance)
                    return;

                ComputeNextOffset();
            }
        }

        /// <summary>Computes offsets until the last computed line starts at or past <paramref name="distance"/> (past it when not <paramref name="atOrPast"/>), or all of them.</summary>
        private void EnsureOffsetsStartingPast(float distance, bool atOrPast)
        {
            EnsureOffsetCapacity();
            while (offsetsValidCount < lineCount)
            {
                int last = offsetsValidCount - 1;
                if (last >= 0 && (atOrPast ? offsets[last] >= distance : offsets[last] > distance))
                    return;

                ComputeNextOffset();
            }
        }

        /// <summary>The last line that starts at or before <paramref name="distance"/> along the scroll axis, or -1 when there are no lines.</summary>
        public int LineAtDistance(float distance)
        {
            if (lineCount == 0)
                return -1;

            EnsureOffsetsStartingPast(distance, false);
            int low = 0;
            int high = offsetsValidCount - 1;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                if (offsets[mid] <= distance)
                    low = mid;
                else
                    high = mid - 1;
            }

            return low;
        }

        /// <summary>The first line whose end is past <paramref name="distance"/>, or the last line.</summary>
        public int FirstLineEndingAfter(float distance)
        {
            if (lineCount == 0)
                return 0;

            EnsureOffsetsEndingPast(distance);
            int low = 0;
            int high = offsetsValidCount;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (offsets[middle] + lineBuffer[middle].Size > distance)
                    high = middle;
                else
                    low = middle + 1;
            }

            return low < lineCount ? low : lineCount - 1;
        }

        /// <summary>The first line from <paramref name="fromLine"/> on that starts at or past <paramref name="distance"/>, or the line count when none does.</summary>
        public int FirstLineStartingAtOrAfter(float distance, int fromLine)
        {
            if (lineCount == 0)
                return 0;

            int low = Math.Max(0, fromLine);
            EnsureOffsets(Math.Min(low, lineCount - 1));
            EnsureOffsetsStartingPast(distance, true);
            int high = offsetsValidCount;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (offsets[middle] >= distance)
                    high = middle;
                else
                    low = middle + 1;
            }

            return low;
        }

        /// <summary>
        /// The line's start as last computed, without bringing it up to date: where its slots were placed before sizes
        /// changed. Used to keep a shown line still while items above it are measured.
        /// </summary>
        public float PlacedOffsetOf(int lineIndex)
            => lineIndex >= 0 && lineIndex < lineCount && lineIndex < offsets.Length ? offsets[lineIndex] : 0f;

        /// <summary>The start of a line along the scroll axis, or 0 when out of range.</summary>
        public float OffsetOf(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= lineCount)
                return 0f;

            EnsureOffsets(lineIndex);
            return offsets[lineIndex];
        }
    }
}
