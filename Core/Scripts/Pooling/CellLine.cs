using UnityEngine;

namespace RecyclableScroller.Core.Pooling
{
    public class CellLine
    {
        public RectTransform LineTransform { get; set; }
        public float Size { get; private set; }
        public bool HasCachedSize { get; private set; }
        public readonly int LineIndex;
        public readonly CellItem[] Cells;

        public CellLine(int lineIndex, int gridCount)
        {
            LineIndex = lineIndex;
            Cells = new CellItem[gridCount];

            for (int i = 0; i < gridCount; i++)
                Cells[i] = new CellItem(lineIndex * gridCount + i);
        }

        public void CacheSize(float size)
        {
            Size = size;
            HasCachedSize = true;
        }

        public override string ToString()
            => $"LineIndex:{LineIndex} Size:{Size} CellCount:{Cells.Length}";
    }
}

