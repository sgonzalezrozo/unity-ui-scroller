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
                item = lineBuffer[line][0]?.DataIndex ?? 0;
            }

            int startLine = line;
            BeginBuild(startLine);

            while (item < itemCount)
            {
                if (isHeader(item) || item >= placementCount)
                {
                    // As wide as an item line, so a line is reused whether it holds a header or items (no allocation).
                    Line headerLine = TakeLine(line, slotsPerLine);
                    headerLine.StartsSection = item < placementCount;
                    BuildHeaderLine(headerLine, item, getItemSize);
                    item++;
                }
                else
                {
                    BuildGridLine(TakeLine(line, slotsPerLine), slotsPerLine, itemCount, ref item, isHeader, getItemSize, getLineSizeForFirstItem);
                }

                line++;
            }

            EndBuild(startLine, line);
        }

        /// <summary>Call before lines from <paramref name="startLine"/> on are rebuilt: their sizes leave the running total.</summary>
        private void BeginBuild(int startLine)
        {
            sizeBeforeBuild = SumSizes(startLine, lineCount);
            sectionStartsBeforeBuild = CountSectionStarts(startLine, lineCount);
            placeholderLinesBeforeBuild = CountPlaceholderLines(startLine, lineCount);
            buildingLines = true;
        }

        /// <summary>Call after the lines from <paramref name="startLine"/> to <paramref name="newLineCount"/> were built.</summary>
        private void EndBuild(int startLine, int newLineCount)
        {
            buildingLines = false;
            lineCount = newLineCount;
            if (startLine <= 0)
            {
                // A full build adds everything up again, so rounding never builds up. Built lines have their own sizes:
                // the next placeholder visits every line once to find the ones that can follow it.
                lineSizeSum = SumSizes(0, lineCount);
                sectionStarts = CountSectionStarts(0, lineCount);
                placeholderLineCount = CountPlaceholderLines(0, lineCount);
                ClearTrackedLines();
                needsFullPlaceholderPass = true;
            }
            else
            {
                lineSizeSum += SumSizes(startLine, lineCount) - sizeBeforeBuild;
                sectionStarts += CountSectionStarts(startLine, lineCount) - sectionStartsBeforeBuild;
                placeholderLineCount -= placeholderLinesBeforeBuild;
                // Appended lines hold unmeasured items with their own sizes: the next placeholder visits only them.
                for (int i = startLine; i < lineCount; i++)
                    Track(lineBuffer[i]);
            }

            if (ShouldShrink(lineBuffer.Length, lineCount))
                Array.Resize(ref lineBuffer, KeptCapacity(lineCount));
            offsetsValidCount = Math.Min(offsetsValidCount, lineCount);
            MarkDirtyFrom(Math.Max(0, startLine));
        }

        private double SumSizes(int from, int to)
        {
            double sum = 0;
            for (int i = Math.Max(0, from); i < to && i < lineBuffer.Length; i++)
            {
                if (lineBuffer[i] != null && !lineBuffer[i].UsesPlaceholder)
                    sum += lineBuffer[i].OwnSize;
            }

            return sum;
        }

        private int CountPlaceholderLines(int from, int to)
        {
            int count = 0;
            for (int i = Math.Max(0, from); i < to && i < lineBuffer.Length; i++)
            {
                if (lineBuffer[i] != null && lineBuffer[i].UsesPlaceholder)
                    count++;
            }

            return count;
        }

        private bool IsInUse(Line line)
        {
            int index = line.LineIndex;
            return index >= 0 && index < lineCount && lineBuffer[index] == line;
        }

        /// <summary>A line that followed the placeholder got a size of its own (an item in it was measured or opened).</summary>
        internal void OnLineLeftPlaceholder(Line line, float newSize)
        {
            if (buildingLines || !IsInUse(line))
                return;

            placeholderLineCount--;
            lineSizeSum += newSize;
            MarkDirtyFrom(line.LineIndex + 1);
            Track(line);
        }

        private void Track(Line line)
        {
            if (line == null || line.Tracked || line.UsesPlaceholder)
                return;

            line.Tracked = true;
            trackedLines.Add(line);
        }

        private void ClearTrackedLines()
        {
            foreach (Line line in trackedLines)
                line.Tracked = false;
            trackedLines.Clear();
        }

        /// <summary>Before a placeholder line gets its own size, its unmeasured items take the current placeholder.</summary>
        private void RefreshPlaceholderItems(Line line)
        {
            if (!line.UsesPlaceholder)
                return;

            for (int slot = 0; slot < line.SlotCount; slot++)
            {
                Placement placement = line[slot];
                if (placement != null && !measured[placement.DataIndex])
                    placement.CacheSize(unmeasuredSize);
            }
        }

        private int CountSectionStarts(int from, int to)
        {
            int count = 0;
            for (int i = Math.Max(1, from); i < to && i < lineBuffer.Length; i++)
            {
                if (lineBuffer[i] != null && lineBuffer[i].StartsSection)
                    count++;
            }

            return count;
        }

        /// <summary>A line in use changed size: the total follows, and the offsets after it are computed again when asked for.</summary>
        internal void OnLineResized(Line line, float previousSize, float newSize)
        {
            if (buildingLines)
                return;

            int index = line.LineIndex;
            if (index < 0 || index >= lineCount || lineBuffer[index] != line)
                return;

            lineSizeSum += newSize - previousSize;
            MarkDirtyFrom(index + 1);
        }

        /// <summary>Line <paramref name="lineIndex"/> with room for <paramref name="slots"/>, reused when it has the same room.</summary>
        private Line TakeLine(int lineIndex, int slots)
        {
            if (lineIndex >= lineBuffer.Length)
                Array.Resize(ref lineBuffer, Math.Max(lineIndex + 1, lineBuffer.Length * 2));

            Line line = lineBuffer[lineIndex];
            if (line != null && line.SlotCount == slots)
            {
                line.Reset(lineIndex);
                return line;
            }

            line = new Line(lineIndex, slots) { Owner = this };
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

        private void BuildHeaderLine(Line line, int itemIndex, Func<int, float> getItemSize)
        {
            if (itemIndex >= placementCount)
                return;

            Placement placement = placementBuffer[itemIndex];
            line[0] = placement;
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
                line[slot] = placement;
                itemToLine[itemIndex] = line.LineIndex;
                placement.CacheSize(getItemSize(itemIndex));
                itemIndex++;
            }

            // The first item decides the line size (it carries an expanded size), and other items can only make it taller.
            int firstItem = line[0]?.DataIndex ?? 0;
            float lineSize = getLineSizeForFirstItem(firstItem);
            for (int slot = 1; slot < line.SlotCount; slot++)
            {
                if (line[slot] != null)
                    lineSize = Mathf.Max(lineSize, line[slot].Size);
            }

            line.CacheSize(lineSize);
        }
    }
}
