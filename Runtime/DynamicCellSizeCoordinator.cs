using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tools.RecyclableScroller.Scripts
{
    public class DynamicCellSizeCoordinator<T> where T : class
    {
        private const float DEFAULT_CELL_FALLBACK_SIZE = 100f;

        private readonly Dictionary<int, float> calculatedCellSizes = new();
        private readonly Dictionary<int, float?> cachedExpandedSizes = new();

        private readonly MonoBehaviour host;
        private readonly int cellsPerFrame;
        private readonly float estimatedCellHeight;
        private readonly Action recalculateContentSize;
        private readonly Action rebuildVisibleCells;
        private readonly Action markPositionsDirty;

        private ICellSizeCalculator<T> cellSizeCalculator;
        private IRecyclableDataSource<T> dataSource;
        private Coroutine sizeCalculationRoutine;
        private bool isDynamicSizing;

        public bool IsDynamicSizing => isDynamicSizing;
        public int CalculatedCount => calculatedCellSizes.Count;

        public DynamicCellSizeCoordinator(
            MonoBehaviour host,
            int cellsPerFrame,
            float estimatedCellHeight,
            Action recalculateContentSize,
            Action rebuildVisibleCells,
            Action markPositionsDirty)
        {
            this.host = host;
            this.cellsPerFrame = cellsPerFrame;
            this.estimatedCellHeight = estimatedCellHeight;
            this.recalculateContentSize = recalculateContentSize;
            this.rebuildVisibleCells = rebuildVisibleCells;
            this.markPositionsDirty = markPositionsDirty;
        }

        public void ConfigureCalculator(ICellSizeCalculator<T> calculator, IRecyclableDataSource<T> source, GameObject cellPrefab, float containerWidth)
        {
            cellSizeCalculator = calculator;
            isDynamicSizing = calculator != null;
            dataSource = source;

            calculatedCellSizes.Clear();
            cachedExpandedSizes.Clear();

            if (!isDynamicSizing)
                return;

            if (cellPrefab != null)
                cellSizeCalculator.SetItemPrefab(cellPrefab);

            if (containerWidth > 0)
                cellSizeCalculator.SetContainerWidth(containerWidth);
        }

        public void UpdateDataSource(IRecyclableDataSource<T> source)
        {
            dataSource = source;
            calculatedCellSizes.Clear();
            cachedExpandedSizes.Clear();
        }

        public void PreCalculateAllCellSizes()
        {
            if (!isDynamicSizing || dataSource == null)
                return;

            int itemCount = dataSource.ItemCount;
            for (int i = 0; i < itemCount; i++)
            {
                T item = dataSource.GetItem(i);
                if (item == null)
                    continue;

                calculatedCellSizes[i] = cellSizeCalculator.CalculateCellSize(item, i);
                cachedExpandedSizes[i] = cellSizeCalculator.CalculateExpandedSize(item, i);
            }
        }

        public void StartBatchedCalculation(CellItem[] cellItems, CellLine[] cellLines, Func<int, bool> isIndexExpanded, int visibleCellCount)
        {
            if (!isDynamicSizing || dataSource == null)
                return;

            if (sizeCalculationRoutine != null)
                host.StopCoroutine(sizeCalculationRoutine);

            sizeCalculationRoutine = host.StartCoroutine(CalculateInBatches(cellItems, cellLines, isIndexExpanded, visibleCellCount));
        }

        public void ClearCaches()
        {
            calculatedCellSizes.Clear();
            cachedExpandedSizes.Clear();
        }

        public float GetCellSize(int index, GameObject cellPrefab)
        {
            if (isDynamicSizing && calculatedCellSizes.TryGetValue(index, out float cachedSize))
                return cachedSize;

            if (isDynamicSizing)
                return estimatedCellHeight;

            IRecyclableCell cellComponent = cellPrefab != null ? cellPrefab.GetComponent<IRecyclableCell>() : null;
            return cellComponent?.DefaultCellSize ?? DEFAULT_CELL_FALLBACK_SIZE;
        }

        public float GetExpandedSize(int index, float fallbackEstimate)
        {
            if (cachedExpandedSizes.TryGetValue(index, out float? cachedSize) && cachedSize.HasValue)
                return cachedSize.Value;

            if (cellSizeCalculator != null && dataSource != null && index < dataSource.ItemCount)
            {
                T item = dataSource.GetItem(index);
                if (item != null)
                {
                    float? calculated = cellSizeCalculator.CalculateExpandedSize(item, index);
                    if (calculated.HasValue)
                        return calculated.Value;
                }
            }

            return fallbackEstimate;
        }

        public void OnCellBoundIfDynamic(int index, CellItem[] cellItems, CellLine[] cellLines, Func<int, bool> isIndexExpanded)
        {
            if (!isDynamicSizing || calculatedCellSizes.ContainsKey(index))
                return;

            CalculateAndCache(index, cellItems, cellLines, isIndexExpanded);
        }

        public void OnExpandableBound(int index, T item)
        {
            if (!isDynamicSizing || cellSizeCalculator == null || cachedExpandedSizes.ContainsKey(index))
                return;

            cachedExpandedSizes[index] = cellSizeCalculator.CalculateExpandedSize(item, index);
        }

        public void OnCellCollapsed(int index, CellItem[] cellItems)
        {
            if (!isDynamicSizing || cellSizeCalculator == null || dataSource == null)
                return;

            if (index < 0 || index >= dataSource.ItemCount)
                return;

            T item = dataSource.GetItem(index);
            if (item == null)
                return;

            float collapsedSize = cellSizeCalculator.CalculateCellSize(item, index);
            calculatedCellSizes[index] = collapsedSize;

            if (cellItems != null && index < cellItems.Length && cellItems[index] != null)
                cellItems[index].CacheSize(collapsedSize);
        }

        public void OnCellExpanded(int index, float newSize)
        {
            if (!isDynamicSizing || cellSizeCalculator == null)
                return;

            if (cachedExpandedSizes.ContainsKey(index))
                cachedExpandedSizes[index] = newSize;
        }

        public void RebuildAllCalculatedSizes(CellItem[] cellItems, CellLine[] cellLines)
        {
            if (!isDynamicSizing || cellSizeCalculator == null || dataSource == null)
                return;

            int itemCount = dataSource.ItemCount;
            if (itemCount == 0 || cellItems == null || cellLines == null)
                return;

            for (int i = 0; i < itemCount; i++)
            {
                CalculateAndCache(i, cellItems, cellLines, null);

                if (i < cellLines.Length && cellItems[i] != null)
                {
                    float size = calculatedCellSizes.TryGetValue(i, out float s) ? s : estimatedCellHeight;
                    cellLines[i].CacheSize(size);
                }
            }
        }

        private IEnumerator CalculateInBatches(CellItem[] cellItems, CellLine[] cellLines, Func<int, bool> isIndexExpanded, int visibleCellCount)
        {
            int itemCount = dataSource.ItemCount;

            for (int i = 0; i < visibleCellCount && i < itemCount; i++)
                CalculateAndCache(i, cellItems, cellLines, isIndexExpanded);

            recalculateContentSize();
            markPositionsDirty();
            rebuildVisibleCells();

            yield return null;

            int processed = visibleCellCount;

            while (processed < itemCount)
            {
                int batch = Mathf.Min(cellsPerFrame, itemCount - processed);
                bool anyCalculated = ProcessBatch(cellItems, cellLines, isIndexExpanded, processed, batch);

                processed += batch;

                if (anyCalculated)
                {
                    recalculateContentSize();
                    markPositionsDirty();
                }

                yield return null;
            }

            recalculateContentSize();
            markPositionsDirty();
            rebuildVisibleCells();

            sizeCalculationRoutine = null;
        }

        private bool ProcessBatch(CellItem[] cellItems, CellLine[] cellLines, Func<int, bool> isIndexExpanded, int startIndex, int batch)
        {
            bool anyCalculated = false;

            for (int i = 0; i < batch; i++)
            {
                int index = startIndex + i;
                if (calculatedCellSizes.ContainsKey(index))
                    continue;

                CalculateAndCache(index, cellItems, cellLines, isIndexExpanded);
                anyCalculated = true;
            }

            return anyCalculated;
        }

        private void CalculateAndCache(int index, CellItem[] cellItems, CellLine[] cellLines, Func<int, bool> isIndexExpanded)
        {
            if (cellSizeCalculator == null || dataSource == null || index >= dataSource.ItemCount)
                return;

            T item = dataSource.GetItem(index);
            if (item == null)
                return;

            float calculatedSize = cellSizeCalculator.CalculateCellSize(item, index);
            calculatedCellSizes[index] = calculatedSize;

            if (cellItems != null && index < cellItems.Length)
                cellItems[index].CacheSize(calculatedSize);

            UpdateLineSizeForCell(index, calculatedSize, cellLines, isIndexExpanded);

            if (cachedExpandedSizes.ContainsKey(index))
                return;

            cachedExpandedSizes[index] = cellSizeCalculator.CalculateExpandedSize(item, index);
        }

        private static void UpdateLineSizeForCell(int cellIndex, float cellSize, CellLine[] cellLines, Func<int, bool> isIndexExpanded)
        {
            if (cellLines == null || cellIndex >= cellLines.Length)
                return;

            if (isIndexExpanded != null && isIndexExpanded(cellIndex))
                return;

            cellLines[cellIndex].CacheSize(cellSize);
        }
    }
}

