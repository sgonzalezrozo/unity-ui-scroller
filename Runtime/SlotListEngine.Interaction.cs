using UnityEngine;

namespace SlotList
{
    /// <summary>Scrolling and drags, source changes, visibility queries and expansion.</summary>
    internal sealed partial class SlotListEngine<T>
    {
        // ---------- Scrolling and drags ----------

        private void OnScroll(Vector2 position)
        {
            if (!isInitialized)
                return;

            using (SlotListMarkers.OnScroll.Auto())
            {
                dragState.HandleScrollTick(components.ScrollRect.velocity.magnitude);
                RebuildVisibleItems();
            }
        }

        public void OnDragBegin()
        {
            dragState?.HandleDragBegin();
            CancelScrollAnimation();
            if (isInitialized)
                dragStartDistance = viewportMetrics.GetScrollDistance();
        }

        public void OnDragEnd()
        {
            dragState?.HandleDragEnd();

            if (!isInitialized)
                return;

            BeginSnapAfterDrag();

            if (pullGesture.WasPullDetected(viewportMetrics.GetContentAnchoredPosition(), viewportMetrics.GetContentSize()))
                hooks.OnPullRefreshRequested();
        }

        // ---------- Source changes ----------

        /// <summary>Replaces the source of an initialized list and refreshes.</summary>
        public void ReplaceSource(IItemSource<T> newSource)
        {
            AttachSource(newSource);
            HasReachedLastPage = !hooks.SupportsPaging();
            expansion?.ClearAll();
            Refresh();
        }

        /// <summary>Returns every slot to the pool, forgets expansion and goes back to the start.</summary>
        public void Clear()
        {
            if (!isInitialized)
                return;

            CancelScrollAnimation();
            recycler.CleanupActiveLines();
            expansion.ClearAll();
            visibleRange.ResetState();
            components.Content.anchoredPosition = Vector2.zero;
        }

        // ---------- Visibility ----------

        public bool IsItemVisible(int index)
        {
            if (!isInitialized || !IsValidPublicIndex(index))
                return false;

            int firstLine = FirstVisibleLine;
            int lastLine = LastVisibleLine;
            if (firstLine < 0 || lastLine < 0)
                return false;

            ListView<Line> lines = layout.Lines;
            for (int lineIndex = firstLine; lineIndex <= lastLine && lineIndex < lines.Length; lineIndex++)
            {
                Line line = lines[lineIndex];
                for (int i = 0; i < line.SlotCount; i++)
                {
                    Placement placement = line[i];
                    if (placement != null && ToReal(placement.DataIndex) == index)
                        return true;
                }
            }

            return false;
        }

        // ---------- Read-only queries ----------

        /// <summary>Where the item's line starts, from the start of Content (padding included). -1 if unknown.</summary>
        public float QueryItemOffset(int index)
        {
            int line = IsValidPublicIndex(index) ? LineIndexOf(ToNearestCopy(index)) : -1;
            if (line < 0)
                return -1f;

            layout.RecomputeOffsets();
            return layout.OffsetOf(line);
        }

        /// <summary>The size of the item's line along the scroll axis (in a grid, its tallest slot). -1 if unknown.</summary>
        public float QueryItemSize(int index)
        {
            int line = IsValidPublicIndex(index) ? LineIndexOf(ToNearestCopy(index)) : -1;
            return line < 0 ? -1f : MainSizeOf(ToNearestCopy(index), line);
        }

        public float ViewportSize => isInitialized ? viewportMetrics.GetViewportSize() : 0f;

        public float ContentSize => isInitialized ? layout.TotalExtent : 0f;

        public int ExpandedCount => expansion?.Count ?? 0;

        private int LineIndexOf(int index)
        {
            if (!isInitialized || index < 0 || index >= layout.Placements.Length)
                return -1;

            int line = layout.LineOf(index);
            return line < layout.Lines.Length ? line : -1;
        }

        // ---------- Expansion ----------

        public bool IsExpanded(int index)
            => expansion?.IsExpanded(index) ?? false;

        public void Expand(int index, float newSize)
        {
            if (!isInitialized || RejectInLoop() || RejectInBento() || !expansion.Expand(index, newSize, settings.allowMultipleExpanded, gridResolver.Current))
                return;

            ApplyExpansionChange();
            RaiseExpansionEvents();
        }

        public void Collapse(int index)
        {
            if (!isInitialized || isLooping || !expansion.Collapse(index))
                return;

            ApplyExpansionChange();
            RaiseExpansionEvents();
        }

        /// <summary>
        /// Tells the list which items the last Expand or Collapse opened or closed, collapsed ones first. Copied first,
        /// so a listener that expands another item doesn't change the list being read.
        /// </summary>
        private void RaiseExpansionEvents()
        {
            changedBuffer.Clear();
            changedBuffer.AddRange(expansion.Changed);

            foreach (int index in changedBuffer)
            {
                if (!expansion.IsExpanded(index))
                    hooks.OnItemCollapsed(index);
            }

            foreach (int index in changedBuffer)
            {
                if (expansion.IsExpanded(index))
                    hooks.OnItemExpanded(index);
            }
        }

        public void SetExpandedSize(int index, float newSize)
        {
            if (isInitialized && !isLooping && !RejectInBento() && expansion.SetExpandedSize(index, newSize, gridResolver.Current))
                ApplyExpansionChange();
        }

        /// <summary>
        /// Re-places the slots after an expansion change. Slots measured with layout are measured again: those on screen
        /// keep their start, those off screen are measured when shown.
        /// </summary>
        private void ApplyExpansionChange()
        {
            foreach (int index in expansion.Changed)
            {
                // Measured slots, and fixed slots expanded without a size, are measured again in their new state.
                if (!isLayoutSized && !expansion.IsMeasuredExpanded(index))
                    continue;

                layout.ForgetSize(index);
                if (index >= 0 && index < layout.Placements.Length && layout.Placements[index]?.Handle != null)
                    remeasuring.Add(index);
            }

            RecalculateContentSize();
            MarkPositionsDirty();
            visibleRange.ResetState();
            RebuildVisibleItems();
            remeasuring.Clear();
        }
    }
}
