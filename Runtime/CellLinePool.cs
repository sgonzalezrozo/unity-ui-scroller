using System.Collections.Generic;
using ToolsCore;
using UnityEngine;

namespace RecyclableScroller.Runtime
{
    public class CellLinePool
    {
        private readonly Queue<CellLine> availableLines;
        private readonly List<CellLine> activeLines;
        private readonly uint gridCount;
        private readonly CellPool cellPool;

        public int AvailableCount => availableLines.Count;
        public int ActiveCount => activeLines.Count;
        public int TotalCount => activeLines.Count + availableLines.Count;

        public CellLinePool(GameObject prefab, RectTransform content, int poolSize, uint gridCount, IPrefabInstantiator instantiator)
        {
            this.gridCount = gridCount;
            availableLines = new();
            activeLines = new();

            int totalCellsNeeded = poolSize * (int)gridCount;
            cellPool = new CellPool(prefab, content, totalCellsNeeded, instantiator);

            PrewarmPool(poolSize);
        }

        private void PrewarmPool(int poolSize)
        {
            for (int i = 0; i < poolSize; i++)
            {
                CellLine line = new(i, (int)gridCount);
                availableLines.Enqueue(line);
            }
        }

        public CellLine GetLine()
        {
            CellLine line;

            if (availableLines.Count > 0)
                line = availableLines.Dequeue();
            else
            {
                int newIndex = activeLines.Count + availableLines.Count;
                line = new(newIndex, (int)gridCount);
            }

            for (int i = 0; i < line.Cells.Length; i++)
            {
                RectTransform cell = cellPool.GetCell();
                line.Cells[i].Cell = cell;
                cell.gameObject.SetActive(true);
            }

            activeLines.Add(line);
            return line;
        }

        public void ReturnLine(CellLine line)
        {
            if (!activeLines.Remove(line))
                return;

            for (int i = 0; i < line.Cells.Length; i++)
            {
                if (line.Cells[i].Cell == null)
                    continue;

                cellPool.ReturnCell(line.Cells[i].Cell);
                line.Cells[i].Cell = null;
            }

            availableLines.Enqueue(line);
        }

        public void ReturnAllLines()
        {
            List<CellLine> linesCopy = new(activeLines);

            for (int i = 0; i < linesCopy.Count; i++)
                ReturnLine(linesCopy[i]);
        }

        public void Cleanup()
        {
            ReturnAllLines();
            cellPool.Cleanup();
            availableLines.Clear();
            activeLines.Clear();
        }
    }
}

