using System;

namespace SlotList
{
    /// <summary>
    /// The JSON form of a bento layout, as Unity's JsonUtility reads it. Give it either <see cref="spans"/> or
    /// <see cref="patterns"/>, or neither for a grid of 1x1 tiles. Another JSON library can fill these classes too;
    /// pass the result to <see cref="BentoLayout.FromData"/>.
    /// </summary>
    [Serializable]
    public class BentoLayoutData
    {
        public int columns;

        /// <summary>Where the sequence starts again after its last entry. 0 repeats all of it.</summary>
        public int repeatFrom;

        public BentoSpanData[] spans;
        public BentoPatternData[] patterns;
    }

    /// <summary>One entry of a span sequence: a tile <see cref="w"/> columns wide and <see cref="h"/> rows tall.</summary>
    [Serializable]
    public class BentoSpanData
    {
        public int w;
        public int h;
    }

    /// <summary>One pattern: <see cref="rows"/> rows of cells and the tiles in them.</summary>
    [Serializable]
    public class BentoPatternData
    {
        public int rows;
        public BentoTileData[] tiles;
    }

    /// <summary>A tile in a pattern: column <see cref="c"/>, row <see cref="r"/>, <see cref="w"/> columns wide and <see cref="h"/> rows tall.</summary>
    [Serializable]
    public class BentoTileData
    {
        public int c;
        public int r;
        public int w;
        public int h;
    }
}
