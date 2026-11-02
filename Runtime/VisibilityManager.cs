using UnityEngine;

namespace RecyclableScroller.Runtime
{
    public class VisibilityManager
    {
        private int visibleStartIndex = -1;
        private int visibleEndIndex = -1;

        public bool HasVisibilityChanged(int newStartIndex, int newEndIndex)
        {
            return newStartIndex != visibleStartIndex || newEndIndex != visibleEndIndex;
        }

        public void UpdateVisibleRange(int startIndex, int endIndex)
        {
            visibleStartIndex = startIndex;
            visibleEndIndex = endIndex;
        }

        public (int, int) GetVisibleRange()
        {
            return (visibleStartIndex, visibleEndIndex);
        }

        public void RecycleOutOfViewCells(CellItem[] cellItems, CellPool cellPool, int newStartIndex, int newEndIndex)
        {
            newEndIndex = Mathf.Min(newEndIndex, cellItems.Length - 1);

            if (visibleStartIndex == -1)
                return;

            for (int i = visibleStartIndex; i <= visibleEndIndex; i++)
            {
                if (i >= newStartIndex && i <= newEndIndex)
                    continue;

                if (cellItems[i].Cell == null)
                    continue;

                cellPool.ReturnCell(cellItems[i].Cell);
                cellItems[i].Cell = null;
            }
        }

        public void RecycleAllCells(CellItem[] cellItems, CellPool cellPool)
        {
            cellPool.ReturnAllCells();
            for (int i = 0; i < cellItems.Length; i++)
                cellItems[i].Cell = null;

            visibleStartIndex = -1;
            visibleEndIndex = -1;
        }
    }
}

