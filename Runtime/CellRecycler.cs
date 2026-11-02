namespace Tools.RecyclableScroller.Scripts
{
    internal class CellRecycler
    {
        private readonly CellItem[] cellItems;
        private readonly CellPool cellPool;

        public CellRecycler(CellItem[] cellItems, CellPool cellPool)
        {
            this.cellItems = cellItems;
            this.cellPool = cellPool;
        }

        public void RecycleOutOfViewCells(int startIndex, int endIndex)
        {
            RecycleCellsBefore(startIndex);
            RecycleCellsAfter(endIndex);
        }

        private void RecycleCellsBefore(int startIndex)
        {
            for (int i = 0; i < startIndex; i++)
            {
                if (cellItems[i].Cell == null)
                    continue;

                cellPool.ReturnCell(cellItems[i].Cell);
                cellItems[i].Cell = null;
            }
        }

        private void RecycleCellsAfter(int endIndex)
        {
            for (int i = endIndex + 1; i < cellItems.Length; i++)
            {
                if (cellItems[i].Cell == null)
                    continue;

                cellPool.ReturnCell(cellItems[i].Cell);
                cellItems[i].Cell = null;
            }
        }
    }
}

