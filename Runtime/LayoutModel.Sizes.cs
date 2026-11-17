using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIScroller
{
    /// <summary>Measured sizes, the average, and the placeholder that lines of unmeasured items share.</summary>
    public sealed partial class LayoutModel
    {
        /// <summary>
        /// Records an item's measured size: caches it on the placement, resizes the item's line to its tallest item,
        /// and marks the offsets after that line for the next <see cref="RecomputeOffsets"/>.
        /// With <paramref name="countInAverage"/> false (an expanded item) the size doesn't count in <see cref="AverageMeasuredSize"/>.
        /// </summary>
        public void SetItemSize(int itemIndex, float size, bool countInAverage = true)
        {
            if (itemIndex < 0 || itemIndex >= Placements.Length || Placements[itemIndex] == null)
                return;

            RemoveFromAverage(itemIndex);
            measured[itemIndex] = true;
            if (countInAverage)
            {
                inAverage[itemIndex] = true;
                measuredTotal += size;
                measuredCount++;
            }

            Placements[itemIndex].CacheSize(size);

            int line = LineOf(itemIndex);
            if (line < 0 || line >= Lines.Length)
                return;

            float lineSize = 0f;
            Line changedLine = Lines[line];
            RefreshPlaceholderItems(changedLine);
            for (int slot = 0; slot < changedLine.SlotCount; slot++)
            {
                Placement placement = changedLine[slot];
                if (placement != null && placement.Size > lineSize)
                    lineSize = placement.Size;
            }

            Lines[line].CacheSize(lineSize);
            MarkDirtyFrom(line);
        }

        /// <summary>
        /// Gives every item not measured yet this placeholder size, and resizes the lines that hold them.
        /// Items for which <paramref name="skip"/> is true (headers) keep their size. Lines made only of unmeasured
        /// items follow the placeholder, so after the first call only lines that mix measured and unmeasured items
        /// are visited: the cost doesn't grow with the item count.
        /// </summary>
        public void SetUnmeasuredSize(float size, Func<int, bool> skip = null)
        {
            float previous = unmeasuredSize;
            unmeasuredSize = size;

            if (needsFullPlaceholderPass)
            {
                ClearTrackedLines();
                firstPlaceholderLine = int.MaxValue;
                for (int i = 0; i < lineCount; i++)
                {
                    if (ApplyPlaceholder(lineBuffer[i], skip))
                        Track(lineBuffer[i]);
                }

                needsFullPlaceholderPass = false;
                return;
            }

            if (placeholderLineCount > 0 && previous != size)
                MarkDirtyFrom(firstPlaceholderLine == int.MaxValue ? 0 : firstPlaceholderLine);

            for (int i = trackedLines.Count - 1; i >= 0; i--)
            {
                Line line = trackedLines[i];
                if (ApplyPlaceholder(line, skip))
                    continue;

                line.Tracked = false;
                trackedLines[i] = trackedLines[trackedLines.Count - 1];
                trackedLines.RemoveAt(trackedLines.Count - 1);
            }
        }

        /// <summary>
        /// Brings one line up to the placeholder. A line of only unmeasured items starts following it; a line that
        /// mixes them gives its unmeasured items the placeholder and resizes. True if the line still needs visiting.
        /// </summary>
        private bool ApplyPlaceholder(Line line, Func<int, bool> skip)
        {
            if (line == null || line.UsesPlaceholder || !IsInUse(line))
                return false;

            if (Work != null)
                Work.PlaceholderLinesVisited++;

            bool anyUnmeasured = false;
            bool allUnmeasured = true;
            for (int slot = 0; slot < line.SlotCount; slot++)
            {
                Placement placement = line[slot];
                if (placement == null)
                    continue;

                if (measured[placement.DataIndex] || (skip != null && skip(placement.DataIndex)))
                    allUnmeasured = false;
                else
                    anyUnmeasured = true;
            }

            if (!anyUnmeasured)
                return false;

            if (allUnmeasured)
            {
                for (int slot = 0; slot < line.SlotCount; slot++)
                    line[slot]?.CacheSize(unmeasuredSize);

                lineSizeSum -= line.OwnSize;
                placeholderLineCount++;
                line.FollowPlaceholder();
                MarkDirtyFrom(line.LineIndex + 1);
                firstPlaceholderLine = Math.Min(firstPlaceholderLine, line.LineIndex);
                return false;
            }

            bool changed = false;
            float lineSize = 0f;
            for (int slot = 0; slot < line.SlotCount; slot++)
            {
                Placement placement = line[slot];
                if (placement == null)
                    continue;

                if (!measured[placement.DataIndex] && !Mathf.Approximately(placement.Size, unmeasuredSize) && (skip == null || !skip(placement.DataIndex)))
                {
                    placement.CacheSize(unmeasuredSize);
                    changed = true;
                }

                lineSize = Mathf.Max(lineSize, placement.Size);
            }

            if (changed)
                line.CacheSize(lineSize);
            return true;
        }

        /// <summary>Marks an item to be measured again. Its current size stays as the placeholder until then.</summary>
        public void ForgetSize(int itemIndex)
        {
            if (!IsMeasured(itemIndex))
                return;

            RemoveFromAverage(itemIndex);
            measured[itemIndex] = false;
            int line = LineOf(itemIndex);
            if (line >= 0 && line < lineCount)
                Track(lineBuffer[line]);
        }

        private void RemoveFromAverage(int itemIndex)
        {
            if (!inAverage[itemIndex])
                return;

            inAverage[itemIndex] = false;
            measuredTotal -= Placements[itemIndex].Size;
            measuredCount--;
        }

        /// <summary>Marks every item to be measured again, keeping the current sizes as placeholders.</summary>
        public void ForgetAllSizes()
        {
            Array.Clear(measured, 0, placementCount);
            Array.Clear(inAverage, 0, placementCount);
            measuredTotal = 0f;
            measuredCount = 0;
            needsFullPlaceholderPass = true;
        }

        public bool IsMeasured(int itemIndex)
            => itemIndex >= 0 && itemIndex < placementCount && measured[itemIndex];

        /// <summary>The mean size of the measured items, or <paramref name="fallback"/> when none is measured yet.</summary>
        public float AverageMeasuredSize(float fallback)
            => measuredCount > 0 ? measuredTotal / measuredCount : fallback;

        /// <summary>The line that holds this item, or -1.</summary>
        public int LineOf(int itemIndex)
            => itemIndex >= 0 && itemIndex < placementCount ? itemToLine[itemIndex] : -1;

        /// <summary>Sets the size of the line that holds this item. Does nothing if the item has no line.</summary>
        public void SetLineSizeOf(int itemIndex, float size)
        {
            int line = LineOf(itemIndex);
            if (line < 0 || line >= Lines.Length)
                return;

            RefreshPlaceholderItems(Lines[line]);
            Lines[line].CacheSize(size);
            MarkDirtyFrom(line);
        }

        /// <summary>Caches an item's own size on its placement.</summary>
        public void CacheItemSize(int itemIndex, float size)
        {
            if (itemIndex >= 0 && itemIndex < Placements.Length && Placements[itemIndex] != null)
                Placements[itemIndex].CacheSize(size);
        }
    }
}
