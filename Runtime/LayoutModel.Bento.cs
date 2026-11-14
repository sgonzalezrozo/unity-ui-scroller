using System;
using System.Collections.Generic;

namespace SlotList
{
    /// <summary>
    /// The bento layout's lines: each row of cells is a line, every line is one row tall, and a line holds the items
    /// whose tile starts in that row (a row covered only by taller tiles has an empty line). An item's size is its
    /// tile's length along the scroll axis. With sections, a header is a line of its own, as tall as the header,
    /// and each section's rows start below it.
    /// </summary>
    public sealed partial class LayoutModel
    {
        /// <summary>
        /// Builds the rows for <paramref name="tiles"/> (at least <paramref name="itemCount"/> of them, in item order,
        /// as the bento arrangers give them). With <paramref name="fromItem"/> above 0 (items appended), the rows
        /// before the one where item <paramref name="fromItem"/> - 1 starts are kept.
        /// </summary>
        public void BuildBentoLines(int itemCount, IReadOnlyList<BentoTile> tiles, float rowSize, int fromItem = 0)
        {
            int count = Math.Min(itemCount, placementCount);
            int item = 0;
            int startRow = 0;
            if (fromItem > 0 && fromItem <= count)
            {
                startRow = tiles[fromItem - 1].Row;
                item = fromItem - 1;
                while (item > 0 && tiles[item - 1].Row == startRow)
                    item--;
            }

            int rowEnd = 0;
            for (int i = 0; i < count; i++)
                rowEnd = Math.Max(rowEnd, tiles[i].Row + tiles[i].Rows);

            for (int row = startRow; row < rowEnd; row++)
            {
                int first = item;
                while (item < count && tiles[item].Row == row)
                    item++;

                Line line = TakeLine(row, item - first);
                for (int i = first; i < item; i++)
                {
                    Placement placement = placementBuffer[i];
                    BentoTile tile = tiles[i];
                    line.Placements[i - first] = placement;
                    itemToLine[i] = row;
                    placement.Tile = tile;
                    placement.CacheSize(tile.Rows * rowSize + (tile.Rows - 1) * lineSpacing);
                }

                line.CacheSize(rowSize);
            }

            lineCount = rowEnd;
            if (ShouldShrink(lineBuffer.Length, lineCount))
                Array.Resize(ref lineBuffer, Math.Max(lineCount, MIN_KEPT));
            offsetsValidCount = Math.Min(offsetsValidCount, startRow);
            MarkDirtyFrom(startRow);
        }

        /// <summary>
        /// Builds the lines of a bento list with sections. <paramref name="tiles"/> has an entry per item, header or
        /// not; item tiles count rows from the start of their section. A header gets a line sized by
        /// <paramref name="headerSize"/>, and the next section starts after the last row of the one before, so a tall
        /// tile never reaches into the next section.
        /// </summary>
        public void BuildBentoSectionLines(int itemCount, IReadOnlyList<BentoTile> tiles, float rowSize, Func<int, bool> isHeader, Func<int, float> headerSize)
        {
            int count = Math.Min(itemCount, placementCount);
            int line = 0;
            int item = 0;
            while (item < count)
            {
                if (isHeader(item))
                {
                    BuildBentoHeaderLine(TakeLine(line, 1), item, tiles[item], headerSize(item));
                    line++;
                    item++;
                    continue;
                }

                int sectionEnd = item;
                int rowEnd = 0;
                while (sectionEnd < count && !isHeader(sectionEnd))
                {
                    rowEnd = Math.Max(rowEnd, tiles[sectionEnd].Row + tiles[sectionEnd].Rows);
                    sectionEnd++;
                }

                for (int row = 0; row < rowEnd; row++)
                {
                    int first = item;
                    while (item < sectionEnd && tiles[item].Row == row)
                        item++;

                    Line rowLine = TakeLine(line + row, item - first);
                    for (int i = first; i < item; i++)
                        PlaceBentoItem(rowLine, i - first, i, tiles[i], rowSize, line + row);
                    rowLine.CacheSize(rowSize);
                }

                line += rowEnd;
            }

            lineCount = line;
            if (ShouldShrink(lineBuffer.Length, lineCount))
                Array.Resize(ref lineBuffer, Math.Max(lineCount, MIN_KEPT));
            offsetsValidCount = 0;
            MarkDirtyFrom(0);
        }

        private void BuildBentoHeaderLine(Line line, int itemIndex, BentoTile tile, float size)
        {
            Placement placement = placementBuffer[itemIndex];
            line.Placements[0] = placement;
            itemToLine[itemIndex] = line.LineIndex;
            placement.Tile = tile;
            placement.CacheSize(size);
            line.CacheSize(size);
        }

        private void PlaceBentoItem(Line line, int slot, int itemIndex, BentoTile tile, float rowSize, int lineIndex)
        {
            Placement placement = placementBuffer[itemIndex];
            line.Placements[slot] = placement;
            itemToLine[itemIndex] = lineIndex;
            placement.Tile = tile;
            placement.CacheSize(tile.Rows * rowSize + (tile.Rows - 1) * lineSpacing);
        }
    }
}
