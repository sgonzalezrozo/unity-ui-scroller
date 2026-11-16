using System;
using System.Collections.Generic;

namespace UIScroller
{
    /// <summary>
    /// A block of a bento layout: <see cref="Rows"/> rows of cells and the tiles drawn in them, with rows and columns
    /// counted from the block's top-left. Items fill the tiles in reading order (top row first, then left to right),
    /// whatever order they're listed in, and the next block starts below this one.
    /// </summary>
    public sealed class BentoPattern
    {
        public readonly int Rows;
        public readonly IReadOnlyList<BentoTile> Tiles;

        public BentoPattern(int rows, IReadOnlyList<BentoTile> tiles)
        {
            Rows = rows;
            BentoTile[] copy = new BentoTile[tiles?.Count ?? 0];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = tiles[i];
            Tiles = Array.AsReadOnly(copy);
        }
    }
}
