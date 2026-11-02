using System;
using UnityEngine;

namespace Tools.RecyclableScroller.Scripts
{
    public class ExpansionOperationHandler
    {
        private readonly CellExpansionController expansion;
        private readonly GridCountResolver gridResolver;
        private readonly ScrollStateManager scrollStateManager;
        private readonly Func<int, float> getDefaultSize;
        private readonly Func<int, float> getExpandedSize;
        private readonly Action<int> onCellCollapsed;
        private readonly Action<int, float> onCellExpanded;
        private readonly Action recalculateContentSize;
        private readonly Action markPositionsDirty;
        private readonly Action rebuildVisibleCells;

        public ExpansionOperationHandler(
            CellExpansionController expansion,
            GridCountResolver gridResolver,
            ScrollStateManager scrollStateManager,
            Func<int, float> getDefaultSize,
            Func<int, float> getExpandedSize,
            Action<int> onCellCollapsed,
            Action<int, float> onCellExpanded,
            Action recalculateContentSize,
            Action markPositionsDirty,
            Action rebuildVisibleCells)
        {
            this.expansion = expansion;
            this.gridResolver = gridResolver;
            this.scrollStateManager = scrollStateManager;
            this.getDefaultSize = getDefaultSize;
            this.getExpandedSize = getExpandedSize;
            this.onCellCollapsed = onCellCollapsed;
            this.onCellExpanded = onCellExpanded;
            this.recalculateContentSize = recalculateContentSize;
            this.markPositionsDirty = markPositionsDirty;
            this.rebuildVisibleCells = rebuildVisibleCells;
        }

        public bool IsIndexExpanded(int index)
            => expansion.IsExpanded(index);

        public void CellWantToCollapse(int index, CellLine[] cellLines, CellItem[] cellItems)
        {
            if (!IsValidIndex(index, cellItems))
                return;

            if (!expansion.IsExpanded(index))
                return;

            expansion.Collapse(index, gridResolver.Current, cellLines, cellItems, getDefaultSize, onCellCollapsed);
            ApplySideEffects();
        }

        public void UpdateExpandedCellSize(int index, float newSize, CellLine[] cellLines, CellItem[] cellItems)
        {
            if (!IsValidIndex(index, cellItems))
                return;

            if (!expansion.IsExpanded(index))
            {
                Debug.LogWarning($"[RecyclableScrollRect] Cannot update size - cell {index} is not expanded");
                return;
            }

            if (gridResolver.Current > 1)
            {
                Debug.LogWarning($"[RecyclableScrollRect] UpdateExpandedCellSize not supported with gridCount > 1");
                return;
            }

            if (newSize < 0)
                newSize = getExpandedSize(index);

            expansion.UpdateExpandedSize(index, newSize, gridResolver.Current, cellLines, onCellExpanded);
            ApplySideEffects();
        }

        public void ExpandCell(int index, float newSize, CellLine[] cellLines, CellItem[] cellItems, bool allowMultiple)
        {
            if (cellItems == null || index < -1 || index >= cellItems.Length)
                return;

            if (gridResolver.Current > 1)
                return;

            if (newSize < 0)
                newSize = getExpandedSize(index);

            expansion.Expand(index, newSize, gridResolver.Current, cellLines, cellItems, allowMultiple, getDefaultSize);
            ApplySideEffects();
        }

        private static bool IsValidIndex(int index, CellItem[] cellItems)
            => cellItems != null && index >= 0 && index < cellItems.Length;

        private void ApplySideEffects()
        {
            recalculateContentSize();
            markPositionsDirty();
            scrollStateManager.ResetState();
            rebuildVisibleCells();
        }
    }
}

