using System;
using System.Collections.Generic;

namespace SlotList
{
    /// <summary>
    /// The bento layout's lines: each row of cells is a line, every line is one row tall, and a line holds the items
    /// whose tile starts in that row (a row covered only by taller tiles has an empty line). An item's size is its
    /// tile's length along the scroll axis.
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
    }
}
