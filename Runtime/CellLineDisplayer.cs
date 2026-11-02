using System;
using RecyclableScroller.Core.Interfaces;
using UnityEngine;

namespace RecyclableScroller.Runtime
{
    public class CellLineDisplayer
    {
        private readonly CellPool cellPool;
        private readonly IScrollDirection direction;
        private readonly LinePositionCache positionCache;
        private readonly CellExpansionController expansion;
        private readonly CellAnimationMarker animationMarker;
        private readonly Action<RectTransform, int> bindCell;
        private readonly Func<int, bool> isHeader;
        private readonly Func<int> getDataSourceItemCount;
        private readonly Action<RectTransform, int> setupExpandableCell;
        private readonly RectTransform viewport;
        private readonly RectTransform content;
        private readonly float gridCellSpacing;

        public CellLineDisplayer(
            CellPool cellPool,
            IScrollDirection direction,
            LinePositionCache positionCache,
            CellExpansionController expansion,
            CellAnimationMarker animationMarker,
            Action<RectTransform, int> bindCell,
            Func<int, bool> isHeader,
            Func<int> getDataSourceItemCount,
            Action<RectTransform, int> setupExpandableCell,
            RectTransform viewport,
            RectTransform content,
            float gridCellSpacing)
        {
            this.cellPool = cellPool;
            this.direction = direction;
            this.positionCache = positionCache;
            this.expansion = expansion;
            this.animationMarker = animationMarker;
            this.bindCell = bindCell;
            this.isHeader = isHeader;
            this.getDataSourceItemCount = getDataSourceItemCount;
            this.setupExpandableCell = setupExpandableCell;
            this.viewport = viewport;
            this.content = content;
            this.gridCellSpacing = gridCellSpacing;
        }

        public void ShowLines(int startLineIndex, int endLineIndex, CellLine[] cellLines, bool forceRebindAll)
        {
            endLineIndex = Mathf.Min(endLineIndex, cellLines.Length - 1);

            for (int lineIdx = startLineIndex; lineIdx <= endLineIndex; lineIdx++)
                SetupAndDisplayLine(lineIdx, startLineIndex, endLineIndex, cellLines, forceRebindAll);
        }

        public void CleanupActiveLines(CellLine[] cellLines)
        {
            if (cellLines == null || cellPool == null)
                return;

            for (int i = 0; i < cellLines.Length; i++)
                CleanupLine(cellLines[i]);
        }

        private void CleanupLine(CellLine line)
        {
            if (line == null || line.Cells == null)
                return;

            for (int j = 0; j < line.Cells.Length; j++)
                CleanupCellSlot(line.Cells[j]);
        }

        private void CleanupCellSlot(CellItem cellItem)
        {
            if (cellItem == null || cellItem.Cell == null)
                return;

            if (cellItem.Cell.gameObject == null)
            {
                cellItem.Cell = null;
                return;
            }

            cellItem.ClearComponentCache();
            cellPool.ReturnCell(cellItem.Cell);
            cellItem.Cell = null;
        }

        private void SetupAndDisplayLine(int lineIndex, int visibleStartLine, int visibleEndLine, CellLine[] cellLines, bool forceRebindAll)
        {
            CellLine line = cellLines[lineIndex];
            bool wasAlreadyActive = HasActiveCells(line);
            CellLine activeLine = GetOrActivateLine(lineIndex, cellLines);

            if (activeLine == null)
                return;

            float linePosition = positionCache.GetPosition(lineIndex);
            float lineSize = line.Size;
            int itemCount = getDataSourceItemCount();

            for (int cellIdx = 0; cellIdx < line.Cells.Length; cellIdx++)
                DisplayCellInLine(line, activeLine, cellIdx, lineIndex, linePosition, lineSize, itemCount, wasAlreadyActive, forceRebindAll, visibleStartLine, visibleEndLine);
        }

        private void DisplayCellInLine(
            CellLine line,
            CellLine activeLine,
            int cellIdx,
            int lineIndex,
            float linePosition,
            float lineSize,
            int itemCount,
            bool wasAlreadyActive,
            bool forceRebindAll,
            int visibleStartLine,
            int visibleEndLine)
        {
            if (line.Cells[cellIdx] == null || activeLine.Cells[cellIdx] == null)
                return;

            int cellDataIndex = line.Cells[cellIdx].DataIndex;
            RectTransform cell = activeLine.Cells[cellIdx].Cell;

            if (cell == null)
                return;

            PositionCellInLine(cell, linePosition, lineSize, cellIdx, line.Cells.Length);

            if (cellDataIndex >= itemCount)
            {
                cell.gameObject.SetActive(false);
                return;
            }

            bool isCurrentlyExpanded = expansion.IsExpanded(cellDataIndex);
            bool wasExpanded = line.Cells[cellIdx].WasExpanded;
            bool expansionStateChanged = wasExpanded != isCurrentlyExpanded;

            if (expansionStateChanged)
                line.Cells[cellIdx].WasExpanded = isCurrentlyExpanded;

            bool shouldBind = !wasAlreadyActive || expansionStateChanged || forceRebindAll;

            if (!shouldBind)
            {
                cell.gameObject.SetActive(true);
                return;
            }

            animationMarker.Mark(cell, lineIndex, line.Cells[cellIdx], visibleStartLine, visibleEndLine);
            setupExpandableCell(cell, cellDataIndex);
            bindCell(cell, cellDataIndex);
            cell.gameObject.SetActive(true);
        }

        private CellLine GetOrActivateLine(int lineIndex, CellLine[] cellLines)
        {
            CellLine line = cellLines[lineIndex];
            int itemCount = getDataSourceItemCount();

            if (HasActiveCells(line))
                return line;

            for (int i = 0; i < line.Cells.Length; i++)
            {
                if (line.Cells[i] == null)
                    continue;

                int dataIndex = line.Cells[i].DataIndex;

                if (dataIndex < 0 || dataIndex >= itemCount)
                    continue;

                bool cellIsHeader = isHeader(dataIndex);
                RectTransform cell = cellPool.GetCell(cellIsHeader);

                if (cell == null)
                    return null;

                line.Cells[i].Cell = cell;
            }

            return line;
        }

        private static bool HasActiveCells(CellLine line)
        {
            if (line?.Cells == null)
                return false;

            for (int i = 0; i < line.Cells.Length; i++)
            {
                if (line.Cells[i] != null && line.Cells[i].Cell)
                    return true;
            }

            return false;
        }

        private void PositionCellInLine(RectTransform cell, float linePosition, float lineSize, int cellIndexInLine, int totalCellsInLine)
        {
            direction.PositionCellInLine(
                cell,
                linePosition,
                lineSize,
                cellIndexInLine,
                totalCellsInLine,
                viewport,
                content,
                gridCellSpacing);
        }
    }
}

