using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotList
{
    /// <summary>
    /// Where every item sits: one Placement per item, Lines of placements (a header takes a whole line),
    /// line offsets along the scroll axis, the total extent, and which line each item is in.
    /// Arrays keep spare capacity and Placement and Line objects are reused, so rebuilds and appended pages only
    /// create what's new (P11).
    /// </summary>
    public sealed class LayoutModel
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
        private int firstDirtyLine;
        private float paddingStart;
        private float paddingEnd;
        private float lineSpacing;

        /// <summary>Counts created placements and lines, for tests. Optional.</summary>
        internal WorkCounters Work;

        // Buffers shrink back when the item count falls under 1/SHRINK_FACTOR of what they hold, and above this many
        // entries: small changes and pages keep reusing them, a big drop gives the memory back.
        private const int SHRINK_FACTOR = 2;
        private const int MIN_KEPT = 256;

        internal int PlacementCapacity => placementBuffer.Length;
        internal int LineCapacity => lineBuffer.Length;
        internal int OffsetCapacity => offsets.Length;

        public ListView<Placement> Placements => new(placementBuffer, placementCount);
        public ListView<Line> Lines => new(lineBuffer, lineCount);

        public bool HasOffsets => lineCount > 0 && offsets.Length >= lineCount;

        /// <summary>Padding plus every line's current size plus the spacing between lines.</summary>
        public float TotalExtent
        {
            get
            {
                if (lineCount == 0)
                    return 0f;

                // P12: read it from the offsets instead of adding up every line.
                RecomputeOffsets();
                int last = lineCount - 1;
                return offsets[last] + lineBuffer[last].Size + paddingEnd;
            }
        }

        public void SetSpacing(float paddingStartValue, float paddingEndValue, float lineSpacingValue)
        {
            paddingStart = paddingStartValue;
            paddingEnd = paddingEndValue;
            lineSpacing = lineSpacingValue;
            firstDirtyLine = 0;
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
            int keep = Math.Max(itemCount, MIN_KEPT);
            if (placementBuffer.Length <= keep * SHRINK_FACTOR)
                return;

            Array.Resize(ref placementBuffer, keep);
            Array.Resize(ref itemToLine, keep);
            Array.Resize(ref measured, keep);
            Array.Resize(ref inAverage, keep);
        }

        private static bool ShouldShrink(int capacity, int count)
            => capacity > Math.Max(count, MIN_KEPT) * SHRINK_FACTOR;

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

        /// <summary>
        /// Groups the placements into lines of <paramref name="slotsPerLine"/>. A header takes a whole line,
        /// and the item after it starts a new line. Item sizes are cached on the placements, line sizes on the lines.
        /// With <paramref name="fromItem"/> above 0 (items appended), the lines before the one holding the item
        /// before it are kept as they are.
        /// </summary>
        public void BuildLines(int itemCount, int slotsPerLine, Func<int, bool> isHeader, Func<int, float> getItemSize, Func<int, float> getLineSizeForFirstItem, int fromItem = 0)
        {
            int line = 0;
            int item = 0;
            if (fromItem > 0 && fromItem <= placementCount && LineOf(fromItem - 1) >= 0)
            {
                line = LineOf(fromItem - 1);
                item = lineBuffer[line].Placements[0]?.DataIndex ?? 0;
            }

            int startLine = line;

            while (item < itemCount)
            {
                if (isHeader(item) || item >= placementCount)
                {
                    BuildHeaderLine(TakeLine(line, 1), item, getItemSize);
                    item++;
                }
                else
                {
                    BuildGridLine(TakeLine(line, slotsPerLine), slotsPerLine, itemCount, ref item, isHeader, getItemSize, getLineSizeForFirstItem);
                }

                line++;
            }

            lineCount = line;
            if (ShouldShrink(lineBuffer.Length, lineCount))
                Array.Resize(ref lineBuffer, Math.Max(lineCount, MIN_KEPT));
            offsetsValidCount = Math.Min(offsetsValidCount, startLine);
            MarkDirtyFrom(startLine);
        }

        /// <summary>Line <paramref name="lineIndex"/> with room for <paramref name="slots"/>, reused when it has the same room.</summary>
        private Line TakeLine(int lineIndex, int slots)
        {
            if (lineIndex >= lineBuffer.Length)
                Array.Resize(ref lineBuffer, Math.Max(lineIndex + 1, lineBuffer.Length * 2));

            Line line = lineBuffer[lineIndex];
            if (line != null && line.Placements.Length == slots)
            {
                line.Reset(lineIndex);
                return line;
            }

            line = new Line(lineIndex, slots);
            lineBuffer[lineIndex] = line;
            if (Work != null)
                Work.LinesCreated++;
            return line;
        }

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
            foreach (Placement placement in Lines[line].Placements)
            {
                if (placement != null && placement.Size > lineSize)
                    lineSize = placement.Size;
            }

            Lines[line].CacheSize(lineSize);
            MarkDirtyFrom(line);
        }

        /// <summary>
        /// Gives every item not measured yet this placeholder size, and resizes the lines that hold them.
        /// Items for which <paramref name="skip"/> is true (headers) keep their size.
        /// </summary>
        public void SetUnmeasuredSize(float size, Func<int, bool> skip = null)
        {
            int firstChanged = -1;
            for (int lineIndex = 0; lineIndex < Lines.Length; lineIndex++)
            {
                Line line = Lines[lineIndex];
                bool changed = false;
                float lineSize = 0f;

                foreach (Placement placement in line.Placements)
                {
                    if (placement == null)
                        continue;

                    if (!measured[placement.DataIndex] && !Mathf.Approximately(placement.Size, size) && (skip == null || !skip(placement.DataIndex)))
                    {
                        placement.CacheSize(size);
                        changed = true;
                    }

                    lineSize = Mathf.Max(lineSize, placement.Size);
                }

                if (!changed)
                    continue;

                line.CacheSize(lineSize);
                if (firstChanged < 0)
                    firstChanged = lineIndex;
            }

            if (firstChanged >= 0)
                MarkDirtyFrom(firstChanged);
        }

        /// <summary>Marks an item to be measured again. Its current size stays as the placeholder until then.</summary>
        public void ForgetSize(int itemIndex)
        {
            if (!IsMeasured(itemIndex))
                return;

            RemoveFromAverage(itemIndex);
            measured[itemIndex] = false;
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

            Lines[line].CacheSize(size);
            MarkDirtyFrom(line);
        }

        /// <summary>Caches an item's own size on its placement.</summary>
        public void CacheItemSize(int itemIndex, float size)
        {
            if (itemIndex >= 0 && itemIndex < Placements.Length && Placements[itemIndex] != null)
                Placements[itemIndex].CacheSize(size);
        }

        /// <summary>Recomputes the line offsets from the lowest line that changed since the last call.</summary>
        public void RecomputeOffsets()
        {
            if (lineCount == 0)
                return;

            int start = firstDirtyLine;
            if (ShouldShrink(offsets.Length, lineCount))
                Array.Resize(ref offsets, Math.Max(lineCount, MIN_KEPT));
            if (offsets.Length < lineCount)
            {
                Array.Resize(ref offsets, Math.Max(lineCount, offsets.Length * 2));
                start = Math.Min(start, offsetsValidCount);
            }

            start = Math.Min(start, offsetsValidCount);
            if (start >= lineCount)
                return;

            float position = start == 0 ? paddingStart : offsets[start - 1] + lineBuffer[start - 1].Size + lineSpacing;
            for (int i = start; i < lineCount; i++)
            {
                offsets[i] = position;
                position += lineBuffer[i].Size + lineSpacing;
            }

            offsetsValidCount = lineCount;
            firstDirtyLine = lineCount;
        }

        // Offsets below this many lines hold correct values (lines past it were added since the last recompute).
        private int offsetsValidCount;

        private void MarkDirtyFrom(int line)
        {
            if (line < firstDirtyLine)
                firstDirtyLine = line;
        }

        /// <summary>The start of a line along the scroll axis, or 0 when out of range.</summary>
        public int LineAtDistance(float distance)
        {
            if (lineCount == 0)
                return -1;

            RecomputeOffsets();
            int low = 0;
            int high = lineCount - 1;
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

        public float OffsetOf(int lineIndex)
            => lineIndex >= 0 && lineIndex < lineCount && lineIndex < offsets.Length ? offsets[lineIndex] : 0f;

        private void BuildHeaderLine(Line line, int itemIndex, Func<int, float> getItemSize)
        {
            if (itemIndex >= placementCount)
                return;

            Placement placement = placementBuffer[itemIndex];
            line.Placements[0] = placement;
            itemToLine[itemIndex] = line.LineIndex;

            float itemSize = getItemSize(itemIndex);
            placement.CacheSize(itemSize);
            line.CacheSize(itemSize);
        }

        private void BuildGridLine(
            Line line,
            int slotsPerLine,
            int itemCount,
            ref int itemIndex,
            Func<int, bool> isHeader,
            Func<int, float> getItemSize,
            Func<int, float> getLineSizeForFirstItem)
        {
            for (int slot = 0; slot < slotsPerLine && itemIndex < itemCount && itemIndex < placementCount; slot++)
            {
                if (isHeader(itemIndex))
                    break;

                Placement placement = placementBuffer[itemIndex];
                line.Placements[slot] = placement;
                itemToLine[itemIndex] = line.LineIndex;
                placement.CacheSize(getItemSize(itemIndex));
                itemIndex++;
            }

            // The first item decides the line size (it carries an expanded size), and other items can only make it taller.
            int firstItem = line.Placements[0]?.DataIndex ?? 0;
            float lineSize = getLineSizeForFirstItem(firstItem);
            for (int slot = 1; slot < line.Placements.Length; slot++)
            {
                if (line.Placements[slot] != null)
                    lineSize = Mathf.Max(lineSize, line.Placements[slot].Size);
            }

            line.CacheSize(lineSize);
        }
    }
}
