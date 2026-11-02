using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecyclableScroller.Runtime
{
    public class CellExpansionController
    {
        private readonly HashSet<int> expandedIndices = new();
        private readonly Dictionary<int, float> expandedSizes = new();

        public int Count => expandedIndices.Count;
        public bool HasAny => expandedIndices.Count > 0;

        public bool IsExpanded(int index)
            => expandedIndices.Contains(index);

        public float GetSizeOrDefault(int index, float defaultSize)
            => expandedSizes.TryGetValue(index, out float size) ? size : defaultSize;

        public bool TryGetSize(int index, out float size)
            => expandedSizes.TryGetValue(index, out size);

        public void ClearAll()
        {
            expandedIndices.Clear();
            expandedSizes.Clear();
        }

        public void Collapse(
            int index,
            int gridCount,
            CellLine[] cellLines,
            CellItem[] cellItems,
            Func<int, float> getDefaultSize,
            Action<int> onCollapsedCallback)
        {
            if (!expandedIndices.Contains(index))
                return;

            int lineIndex = index / Mathf.Max(1, gridCount);

            expandedIndices.Remove(index);
            expandedSizes.Remove(index);

            onCollapsedCallback?.Invoke(index);

            float collapsedSize = getDefaultSize(index);
            if (index >= 0 && index < cellItems.Length && cellItems[index] != null)
                cellItems[index].CacheSize(collapsedSize);
            if (lineIndex >= 0 && lineIndex < cellLines.Length)
                cellLines[lineIndex].CacheSize(collapsedSize);
        }

        public void UpdateExpandedSize(
            int index,
            float newSize,
            int gridCount,
            CellLine[] cellLines,
            Action<int, float> onExpandedCallback)
        {
            if (!expandedIndices.Contains(index))
                return;

            int lineIndex = index / Mathf.Max(1, gridCount);
            expandedSizes[index] = newSize;

            if (lineIndex >= 0 && lineIndex < cellLines.Length)
                cellLines[lineIndex].CacheSize(newSize);

            onExpandedCallback?.Invoke(index, newSize);
        }

        public void Expand(
            int index,
            float newSize,
            int gridCount,
            CellLine[] cellLines,
            CellItem[] cellItems,
            bool allowMultiple,
            Func<int, float> getDefaultSize)
        {
            int lineIndex = index / Mathf.Max(1, gridCount);
            bool currentlyExpanded = expandedIndices.Contains(index);

            if (currentlyExpanded)
            {
                CollapseExpandedAtLine(index, lineIndex, cellLines, getDefaultSize);
                return;
            }

            if (!allowMultiple && expandedIndices.Count > 0)
                CollapseAllPreviouslyExpanded(gridCount, cellLines, cellItems, getDefaultSize);

            if (index < 0)
                return;

            if (lineIndex >= 0 && lineIndex < cellLines.Length)
                cellLines[lineIndex].CacheSize(newSize);
            expandedIndices.Add(index);
            expandedSizes[index] = newSize;
        }

        private void CollapseExpandedAtLine(int index, int lineIndex, CellLine[] cellLines, Func<int, float> getDefaultSize)
        {
            if (index == -1)
                return;

            float defaultSize = getDefaultSize(index);
            if (lineIndex >= 0 && lineIndex < cellLines.Length)
                cellLines[lineIndex].CacheSize(defaultSize);
            expandedIndices.Remove(index);
            expandedSizes.Remove(index);
        }

        private void CollapseAllPreviouslyExpanded(int gridCount, CellLine[] cellLines, CellItem[] cellItems, Func<int, float> getDefaultSize)
        {
            List<int> indicesToCollapse = new(expandedIndices);

            for (int i = 0; i < indicesToCollapse.Count; i++)
            {
                int expandedIndex = indicesToCollapse[i];
                int prevLineIndex = expandedIndex / Mathf.Max(1, gridCount);
                float previousDefault = getDefaultSize(expandedIndex);

                if (prevLineIndex >= 0 && prevLineIndex < cellLines.Length)
                    cellLines[prevLineIndex].CacheSize(previousDefault);

                if (expandedIndex >= 0 && expandedIndex < cellItems.Length && cellItems[expandedIndex] != null)
                {
                    IExpandableRecyclableCellBase previousExpandable = cellItems[expandedIndex].Cell != null
                        ? cellItems[expandedIndex].Cell.GetComponent<IExpandableRecyclableCellBase>()
                        : null;
                    previousExpandable?.CollapseCell();
                }

                expandedIndices.Remove(expandedIndex);
                expandedSizes.Remove(expandedIndex);
            }
        }
    }
}

