using UnityEngine;

namespace RecyclableScroller.Runtime
{
    internal class VisibilityIndexFinder
    {
        private readonly CellItem[] cellItems;
        private readonly float[] cachedPositions;

        public VisibilityIndexFinder(CellItem[] cellItems, float[] cachedPositions)
        {
            this.cellItems = cellItems;
            this.cachedPositions = cachedPositions;
        }

        public int FindStartIndex(float scrollPos)
        {
            float absScrollPos = Mathf.Abs(scrollPos);

            if (IsInvalidState())
                return 0;

            return FindFirstVisibleIndex(absScrollPos);
        }

        public int FindEndIndex(float scrollPos, float viewportSize, int startIndex)
        {
            float absScrollPos = Mathf.Abs(scrollPos);
            float endPos = absScrollPos + viewportSize;

            return FindLastVisibleIndex(endPos, startIndex);
        }

        private bool IsInvalidState()
        {
            if (cachedPositions == null || cachedPositions.Length == 0)
                return true;

            return false;
        }

        private int FindFirstVisibleIndex(float absScrollPos)
        {
            for (int i = 0; i < cellItems.Length; i++)
            {
                float cellStart = cachedPositions[i];
                float cellEnd = cellStart + cellItems[i].Size;

                if (cellEnd >= absScrollPos)
                    return i;
            }

            return cellItems.Length - 1;
        }

        private int FindLastVisibleIndex(float endPos, int startIndex)
        {
            for (int i = startIndex; i < cellItems.Length; i++)
            {
                float cellStart = cachedPositions[i];

                if (cellStart >= endPos)
                {
                    int result = Mathf.Max(i - 1, startIndex);
                    return result;
                }
            }

            return cellItems.Length - 1;
        }
    }
}

