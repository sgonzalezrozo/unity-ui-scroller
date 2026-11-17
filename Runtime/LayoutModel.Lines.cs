using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIScroller
{
    /// <summary>Building lines from placements, the running totals kept as lines change, and reusing Line objects.</summary>
    public sealed partial class LayoutModel
    {
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
