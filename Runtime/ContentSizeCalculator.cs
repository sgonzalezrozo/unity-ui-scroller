using UnityEngine;

namespace Tools.RecyclableScroller.Scripts
{
    public class ContentSizeCalculator
    {
        private float contentSize;
        private float lastRebuildContentSize;
        private const float REBUILD_THRESHOLD = 10f;

        public float GetContentSize()
            => contentSize;

        public float GetLastRebuildContentSize()
            => lastRebuildContentSize;

        public void SetLastRebuildContentSize(float size)
            => lastRebuildContentSize = size;

        public bool NeedsRebuild()
            => Mathf.Abs(lastRebuildContentSize - contentSize) > REBUILD_THRESHOLD;

        public float RecalculateContentSize(
            CellItem[] cellItems,
            float paddingStart,
            float paddingEnd,
            float cellSpacing,
            GameObject cellPrefab,
            System.Func<int, float> getCellSizeDelegate)
        {
            if (cellItems == null || cellItems.Length == 0)
                return 0;

            float cellsSize = 0f;
            float spacingTotal = 0f;

            for (int i = 0; i < cellItems.Length; i++)
            {
                if (!cellItems[i].HasCachedSize)
                {
                    float cellSize = getCellSizeDelegate(i);
                    cellItems[i].CacheSize(cellSize);
                }

                cellsSize += cellItems[i].Size;

                if (i < cellItems.Length - 1)
                    spacingTotal += cellSpacing;
            }

            contentSize = paddingStart + cellsSize + spacingTotal + paddingEnd;

            if (!(contentSize <= 0) || cellItems.Length <= 0)
                return contentSize;

            if (cellPrefab == null)
            {
                Debug.LogError("[ContentSizeCalculator] cellPrefab is NULL - cannot calculate default size!");
                return 0;
            }

            IRecyclableCell cellComponent = cellPrefab.GetComponent<IRecyclableCell>();
            if (cellComponent == null)
            {
                Debug.LogError($"[ContentSizeCalculator] cellPrefab '{cellPrefab.name}' has no IRecyclableCell component!");
                return 0;
            }

            float defaultSize = cellComponent.DefaultCellSize;
            if (defaultSize <= 0)
            {
                Debug.LogError($"[ContentSizeCalculator] DefaultCellSize is {defaultSize} - cannot calculate content size!");
                return 0;
            }

            contentSize = paddingStart + (cellItems.Length * defaultSize) + ((cellItems.Length - 1) * cellSpacing) + paddingEnd;
            return contentSize;
        }

        public void UpdateContentRectSize(RectTransform contentTransform, float newSize, bool isVertical)
        {
            Vector2 contentSizeDelta = contentTransform.sizeDelta;
            if (isVertical)
                contentSizeDelta.y = newSize;
            else
                contentSizeDelta.x = newSize;

            contentTransform.sizeDelta = contentSizeDelta;
        }
    }
}

