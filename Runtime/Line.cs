using System;

namespace SlotList
{
    public class Line
    {
        public float Size { get; private set; }
        public int LineIndex { get; internal set; }
        public readonly Placement[] Placements;

        public Line(int lineIndex, int gridCount)
        {
            LineIndex = lineIndex;
            Placements = new Placement[gridCount];
        }

        /// <summary>Makes a reused line empty again, as line <paramref name="lineIndex"/>.</summary>
        internal void Reset(int lineIndex)
        {
            LineIndex = lineIndex;
            Size = 0f;
            Array.Clear(Placements, 0, Placements.Length);
        }

        public void CacheSize(float size)
        {
            Size = size;
        }

        public override string ToString()
            => $"LineIndex:{LineIndex} Size:{Size} ItemCount:{Placements.Length}";
    }
}

