using System;
using System.Collections.Generic;

namespace Tools.RecyclableScroller.Core.Scripts.Pooling
{
    public class CellLineBuilder
    {
        public CellLine[] Build(
            CellItem[] cellItems,
            int itemCount,
            int gridCount,
            Func<int, bool> isHeader,
            Func<int, float> getCellSize,
            Func<int, float> getLineSizeForFirstCell)
        {
            List<CellLine> lines = new();
            int lineIndex = 0;

            for (int i = 0; i < itemCount; )
            {
                if (isHeader(i) || i >= cellItems.Length)
                {
                    lines.Add(BuildHeaderLine(lineIndex++, cellItems, i, getCellSize));
                    i++;
                    continue;
                }

                lines.Add(BuildGridLine(lineIndex++, cellItems, gridCount, itemCount, ref i, isHeader, getCellSize, getLineSizeForFirstCell));
            }

            return lines.ToArray();
        }

        private static CellLine BuildHeaderLine(int lineIndex, CellItem[] cellItems, int itemIndex, Func<int, float> getCellSize)
        {
            CellLine line = new(lineIndex, 1);
            if (itemIndex >= cellItems.Length)
                return line;

            line.Cells[0] = cellItems[itemIndex];
            float cellSize = getCellSize(itemIndex);
            cellItems[itemIndex].CacheSize(cellSize);
            line.CacheSize(cellSize);
            return line;
        }

        private static CellLine BuildGridLine(
            int lineIndex,
            CellItem[] cellItems,
            int gridCount,
            int itemCount,
            ref int itemIndex,
            Func<int, bool> isHeader,
            Func<int, float> getCellSize,
            Func<int, float> getLineSizeForFirstCell)
        {
            CellLine line = new(lineIndex, gridCount);

            for (int clearIndex = 0; clearIndex < gridCount; clearIndex++)
                line.Cells[clearIndex] = null;

            for (int j = 0; j < gridCount && itemIndex < itemCount && itemIndex < cellItems.Length; j++)
            {
                if (isHeader(itemIndex))
                    break;

                line.Cells[j] = cellItems[itemIndex];
                float cellSize = getCellSize(itemIndex);
                cellItems[itemIndex].CacheSize(cellSize);
                itemIndex++;
            }

            int firstDataIndex = line.Cells[0]?.DataIndex ?? 0;
            float lineSize = getLineSizeForFirstCell(firstDataIndex);
            line.CacheSize(lineSize);
            return line;
        }
    }
}

