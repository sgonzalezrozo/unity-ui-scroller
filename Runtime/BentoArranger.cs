using System.Collections.Generic;
using UnityEngine;

namespace SlotList
{
    /// <summary>Hands out the tiles of a bento layout one item at a time, in item order. Stateful, so pages can continue it.</summary>
    internal abstract class BentoArranger
    {
        public abstract BentoTile Next();

        /// <summary>Starts again from the first item.</summary>
        public abstract void Reset();
    }

    /// <summary>
    /// Packs a sequence of spans in order: each tile goes in the first gap where it fits, at or after the previous
    /// tile's start (row by row, left to right). It never goes back to fill an earlier hole, so item order is reading
    /// order and each row holds a run of consecutive items. Keeps a bit mask per open row (columns are at most 64).
    /// </summary>
    internal sealed class SpanArranger : BentoArranger
    {
        private readonly int columns;
        private readonly BentoSpan[] spans;
        private readonly int repeatFrom;
        private readonly List<ulong> rowMasks = new();

        private int sequence;
        private int row;
        private int column;
        private int firstMaskRow;

        public SpanArranger(int columns, BentoSpan[] spans, int repeatFrom)
        {
            this.columns = Mathf.Clamp(columns, 1, BentoLayout.MAX_COLUMNS);
            this.spans = spans;
            this.repeatFrom = repeatFrom;
        }

        public override void Reset()
        {
            sequence = 0;
            row = 0;
            column = 0;
            firstMaskRow = 0;
            rowMasks.Clear();
        }

        public override BentoTile Next()
        {
            BentoSpan span = spans[sequence];
            sequence++;
            if (sequence >= spans.Length)
                sequence = repeatFrom;

            int width = Mathf.Clamp(span.Columns, 1, columns);
            int height = Mathf.Max(1, span.Rows);
            ulong bits = Bits(width);

            for (int r = row; ; r++)
            {
                int lastColumn = columns - width;
                for (int c = r == row ? column : 0; c <= lastColumn; c++)
                {
                    if (!Fits(r, height, bits << c))
                        continue;

                    Mark(r, height, bits << c);
                    row = r;
                    column = c;
                    DropRowsAbove(r);
                    return new BentoTile(c, r, width, height);
                }
            }
        }

        private static ulong Bits(int width)
            => width >= BentoLayout.MAX_COLUMNS ? ulong.MaxValue : (1UL << width) - 1UL;

        private bool Fits(int startRow, int height, ulong bits)
        {
            for (int r = startRow; r < startRow + height; r++)
            {
                if ((MaskOf(r) & bits) != 0UL)
                    return false;
            }

            return true;
        }

        private ulong MaskOf(int r)
        {
            int index = r - firstMaskRow;
            return index >= 0 && index < rowMasks.Count ? rowMasks[index] : 0UL;
        }

        private void Mark(int startRow, int height, ulong bits)
        {
            for (int r = startRow; r < startRow + height; r++)
            {
                int index = r - firstMaskRow;
                while (rowMasks.Count <= index)
                    rowMasks.Add(0UL);
                rowMasks[index] |= bits;
            }
        }

        // Rows above the current tile are never looked at again.
        private void DropRowsAbove(int r)
        {
            int drop = Mathf.Min(r - firstMaskRow, rowMasks.Count);
            if (drop <= 0)
                return;

            rowMasks.RemoveRange(0, drop);
            firstMaskRow += drop;
        }
    }

    /// <summary>Fills patterns in sequence: each pattern's tiles in reading order, then the next pattern below it.</summary>
    internal sealed class PatternArranger : BentoArranger
    {
        private readonly BentoPattern[] patterns;
        private readonly BentoTile[][] sortedTiles;
        private readonly int repeatFrom;

        private int sequence;
        private int tileIndex;
        private int firstRow;

        public PatternArranger(BentoPattern[] patterns, BentoTile[][] sortedTiles, int repeatFrom)
        {
            this.patterns = patterns;
            this.sortedTiles = sortedTiles;
            this.repeatFrom = repeatFrom;
        }

        public override void Reset()
        {
            sequence = 0;
            tileIndex = 0;
            firstRow = 0;
        }

        public override BentoTile Next()
        {
            BentoTile[] tiles = sortedTiles[sequence];
            BentoTile local = tiles[tileIndex];
            BentoTile tile = new(local.Column, firstRow + local.Row, local.Columns, local.Rows);

            tileIndex++;
            if (tileIndex >= tiles.Length)
            {
                tileIndex = 0;
                firstRow += patterns[sequence].Rows;
                sequence++;
                if (sequence >= patterns.Length)
                    sequence = repeatFrom;
            }

            return tile;
        }
    }
}
