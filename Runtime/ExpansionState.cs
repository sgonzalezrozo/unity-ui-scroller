using System.Collections.Generic;
using UnityEngine;

namespace SlotList
{
    /// <summary>
    /// Which items are expanded and how big they are. Expand, Collapse and SetExpandedSize resize the item's line
    /// through the LayoutModel and return true when the layout changed, so the list can re-place its slots.
    /// For slots measured with layout no size is stored: the slot shows its details and is measured again,
    /// so these only change the state and list the items that changed in <see cref="Changed"/>.
    /// A fixed-size slot expanded without a size is measured while it's expanded (<see cref="IsMeasuredExpanded"/>),
    /// and goes back to its fixed size when it collapses.
    /// </summary>
    internal sealed class ExpansionState
    {
        private readonly HashSet<int> expandedIndices = new();
        private readonly Dictionary<int, float> expandedSizes = new();
        private readonly HashSet<int> measuredExpanded = new();
        private readonly List<int> collapseBuffer = new();
        private readonly LayoutModel layout;
        private readonly IItemSizes sizes;
        private readonly bool sizesFromLayout;
        private bool hasWarnedAboutGrid;
        private bool hasWarnedAboutGivenSize;

        /// <summary>The items whose expanded state changed in the last Expand or Collapse.</summary>
        public List<int> Changed { get; } = new();

        public int Count => expandedIndices.Count;
        public bool HasAny => expandedIndices.Count > 0;

        public ExpansionState(LayoutModel layout, IItemSizes sizes, bool sizesFromLayout = false)
        {
            this.layout = layout;
            this.sizes = sizes;
            this.sizesFromLayout = sizesFromLayout;
        }

        public bool IsExpanded(int index)
            => expandedIndices.Contains(index);

        /// <summary>True for a fixed-size slot expanded without a size: the list measures it while it's expanded.</summary>
        public bool IsMeasuredExpanded(int index)
            => measuredExpanded.Contains(index);

        public float GetSizeOrDefault(int index, float defaultSize)
            => expandedSizes.TryGetValue(index, out float size) ? size : defaultSize;

        public void ClearAll()
        {
            expandedIndices.Clear();
            expandedSizes.Clear();
            measuredExpanded.Clear();
        }

        /// <summary>
        /// Expands an item, or collapses it if it's already expanded. In single mode the others collapse first.
        /// -1 only collapses the others. A negative size uses the expanded-size function. Ignored in grids.
        /// </summary>
        public bool Expand(int index, float newSize, bool allowMultiple, int slotsPerLine)
        {
            Changed.Clear();
            if (index < -1 || index >= layout.Placements.Length)
                return false;

            if (slotsPerLine > 1)
            {
                if (!hasWarnedAboutGrid)
                    Debug.LogWarning("[SlotList] Expand is ignored in a grid (more than one slot per line).");

                hasWarnedAboutGrid = true;
                return false;
            }

            if (sizesFromLayout)
                WarnAboutGivenSize(index, newSize);

            // A fixed-size slot expanded without a size follows its content while it's expanded.
            bool measureWhileExpanded = !sizesFromLayout && newSize < 0;

            if (index >= 0 && expandedIndices.Contains(index))
            {
                if (!sizesFromLayout)
                    RestoreFixedSize(index);
                Forget(index);
                Changed.Add(index);
                return true;
            }

            if (!allowMultiple && expandedIndices.Count > 0)
                CollapseAllOthers();

            if (index < 0)
                return true;

            expandedIndices.Add(index);
            Changed.Add(index);
            if (sizesFromLayout)
                return true;

            if (measureWhileExpanded)
            {
                // The list measures it now (on screen) or when it's shown; until then its line keeps the fixed size.
                measuredExpanded.Add(index);
                return true;
            }

            layout.SetLineSizeOf(index, newSize);
            expandedSizes[index] = newSize;
            return true;
        }

        public bool Collapse(int index)
        {
            Changed.Clear();
            if (index < 0 || index >= layout.Placements.Length || !expandedIndices.Contains(index))
                return false;

            if (!sizesFromLayout)
                RestoreFixedSize(index);
            Forget(index);
            Changed.Add(index);
            sizes.OnCollapsed(index);
            return true;
        }

        /// <summary>Resizes an expanded item. A negative size uses the expanded-size function. Not supported in grids.</summary>
        public bool SetExpandedSize(int index, float newSize, int slotsPerLine)
        {
            if (index < 0 || index >= layout.Placements.Length)
                return false;

            if (sizesFromLayout)
            {
                Debug.LogWarning("[SlotList] SetExpandedSize is ignored for slots measured with layout (DefaultExtent of 0 or less): " +
                                 "the expanded slot's own layout decides its size.");
                return false;
            }

            if (!expandedIndices.Contains(index))
            {
                Debug.LogWarning($"[SlotList] Cannot update size - slot {index} is not expanded");
                return false;
            }

            if (measuredExpanded.Contains(index))
            {
                if (newSize < 0)
                {
                    Debug.LogWarning($"[SlotList] Slot {index} was expanded without a size, so its content decides its size. Pass a size to fix it.");
                    return false;
                }

                // A given size turns it into a fixed expanded size.
                measuredExpanded.Remove(index);
                layout.ForgetSize(index);
            }

            if (slotsPerLine > 1)
            {
                Debug.LogWarning("[SlotList] SetExpandedSize is not supported with more than one slot per line");
                return false;
            }

            if (newSize < 0)
                newSize = sizes.GetExpandedSize(index);

            expandedSizes[index] = newSize;
            layout.SetLineSizeOf(index, newSize);
            sizes.OnExpanded(index, newSize);
            return true;
        }

        private void CollapseAllOthers()
        {
            collapseBuffer.Clear();
            collapseBuffer.AddRange(expandedIndices);

            for (int i = 0; i < collapseBuffer.Count; i++)
            {
                int expandedIndex = collapseBuffer[i];
                if (!sizesFromLayout)
                    RestoreFixedSize(expandedIndex);

                Placement placement = expandedIndex >= 0 && expandedIndex < layout.Placements.Length ? layout.Placements[expandedIndex] : null;
                if (placement?.Slot != null)
                    placement.Handle.Expandable?.OnCollapsed();

                Forget(expandedIndex);
                Changed.Add(expandedIndex);
            }
        }

        private void WarnAboutGivenSize(int index, float newSize)
        {
            if (index < 0 || newSize < 0f || hasWarnedAboutGivenSize)
                return;

            hasWarnedAboutGivenSize = true;
            Debug.LogWarning("[SlotList] The size given to Expand is ignored for slots measured with layout (DefaultExtent of 0 or less): " +
                             "the expanded slot's own layout decides its size.");
        }

        private void Forget(int index)
        {
            expandedIndices.Remove(index);
            expandedSizes.Remove(index);
            measuredExpanded.Remove(index);
        }

        /// <summary>Gives a fixed-size item its default size back, dropping a size measured while it was expanded.</summary>
        private void RestoreFixedSize(int index)
        {
            layout.ForgetSize(index);
            float defaultSize = sizes.GetDefaultSize(index);
            layout.CacheItemSize(index, defaultSize);
            layout.SetLineSizeOf(index, defaultSize);
        }
    }
}
