using UnityEngine;

namespace Tools.RecyclableScroller.Scripts
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
            {
                int cellDataIndex = lineIndex * gridCount + i;
                Cells[i] = new CellItem(cellDataIndex);
            }
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

