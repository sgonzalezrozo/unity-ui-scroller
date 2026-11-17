using System.Collections.Generic;
using UnityEngine;

namespace UIScroller
{
    /// <summary>
    /// Looping bento lists (a carousel of tiles of different sizes). The layout starts again with every copy of the
    /// items, so each copy is the same block of rows and jumping by whole copies is invisible. A copy whose last row
    /// isn't full keeps its holes; an item count that fills whole patterns leaves none. Copies only: a bento loop too
    /// long for a few copies doesn't loop.
    /// </summary>
    internal sealed partial class ScrollerEngine<T>
    {
        // One copy's tiles, rows counted from the copy's first row.
        private readonly List<BentoTile> loopCopyTiles = new();
        private int loopCopyRows = 1;
        private bool hasWarnedAboutLongBentoLoop;

        /// <summary>Lines in one copy: a row of cells in bento, an item otherwise.</summary>
        private int LoopLinesPerCopy => isBento ? loopCopyRows : RealCount;

        /// <summary>Arranges one copy and works out how many copies to lay out. False when the loop would be too long.</summary>
        private bool PrepareBentoLoop(out int copies)
        {
            loopCopyTiles.Clear();
            bentoTiles.Clear();
            bentoArranger.Reset();
            int rows = 1;
            for (int i = 0; i < RealCount; i++)
            {
                BentoTile tile = bentoArranger.Next();
                loopCopyTiles.Add(tile);
                rows = Mathf.Max(rows, tile.Row + tile.Rows);
            }

            // The tiles are kept in loopCopyTiles: the arranger starts again for anything that arranges after this.
            bentoArranger.Reset();
            loopCopyRows = rows;
            float period = rows * (BentoRowSize() + settings.lineSpacing);
            copies = LoopIndex.Copies(viewportSize, period);
            if (period * copies <= MAX_COPIES_EXTENT)
                return true;

            if (!hasWarnedAboutLongBentoLoop)
            {
                hasWarnedAboutLongBentoLoop = true;
                Debug.LogWarning($"[UIScroller] {host.gameObject.name}: this bento loop is too long to lay out as copies ({RealCount} items), so the list won't loop.", host);
            }

            return false;
        }

        /// <summary>Every entry's tile: its item's tile in the first copy, moved down by whole copies.</summary>
        private void FillBentoLoopTiles()
        {
            bentoTiles.Clear();
            int count = RealCount;
            for (int entry = 0; entry < ItemCount; entry++)
            {
                BentoTile tile = loopCopyTiles[entry % count];
                bentoTiles.Add(new BentoTile(tile.Column, tile.Row + entry / count * loopCopyRows, tile.Columns, tile.Rows));
            }
        }

        /// <summary>The first entry in <paramref name="line"/> or after it (bento rows that only tall tiles cross hold none), or -1.</summary>
        private int FirstEntryFrom(int line)
        {
            for (int l = Mathf.Max(0, line); l < layout.Lines.Length; l++)
            {
                int entry = FirstItemIn(l);
                if (entry >= 0)
                    return entry;
            }

            return -1;
        }

        /// <summary>The last entry in <paramref name="line"/> or before it, or -1.</summary>
        private int LastEntryUpTo(int line)
        {
            for (int l = Mathf.Min(line, layout.Lines.Length - 1); l >= 0; l--)
            {
                int entry = LastItemIn(l);
                if (entry >= 0)
                    return entry;
            }

            return -1;
        }
    }
}
