using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlotList
{
    /// <summary>Item and header sizes: fixed or measured with layout, placeholders, invalidation, watcher reports and anchoring.</summary>
    internal sealed partial class SlotListEngine<T>
    {
        // ---------- Sizes ----------

        /// <summary>
        /// A fixed size from the prefab's DefaultExtent, or, for a prefab measured with layout (DefaultExtent of 0 or less),
        /// the item's measured size, or the average of the measured items while it isn't measured yet.
        /// </summary>
        private float GetItemSize(int index)
        {
            if (IsHeaderEntry(index))
            {
                if (!isHeaderLayoutSized)
                    return headerExtent;
                return layout.IsMeasured(index) ? layout.Placements[index].Size : headerPlaceholderSize;
            }

            if (!isLayoutSized)
                // A fixed-size item has a measured size only while it's expanded without a given size.
                return layout.IsMeasured(index) ? layout.Placements[index].Size : GetDefaultItemSizeFromPrefab();

            return layout.IsMeasured(index) ? layout.Placements[index].Size : layout.AverageMeasuredSize(placeholderSize);
        }

        private void WarnAboutMissingLayout()
        {
            if (hasWarnedAboutMissingLayout)
                return;

            hasWarnedAboutMissingLayout = true;
            Debug.LogWarning($"[SlotList] '{slotPrefab.name}' returns a DefaultExtent of 0 or less (measure with layout), " +
                             "but its root reports no preferred size, so its slots keep the prefab's size. " +
                             "Add a VerticalLayoutGroup (Control Child Size > Height) or a LayoutElement to the root, " +
                             "or return a positive DefaultExtent for a fixed size.");
        }

        /// <summary>A DefaultExtent of 0 or less means "measure with layout". The prefab's own size is the first placeholder.</summary>
        private void ReadSizingFromPrefab()
        {
            ReadSizing(slotPrefab, out isLayoutSized, out _, out placeholderSize);
            slotRootHasLayout = slotPrefab.GetComponent<LayoutGroup>() != null || slotPrefab.GetComponent<LayoutElement>() != null;
            if (headerPrefab != null)
                ReadSizing(headerPrefab, out isHeaderLayoutSized, out headerExtent, out headerPlaceholderSize);
        }

        private void ReadSizing(GameObject prefab, out bool layoutSized, out float extent, out float placeholder)
        {
            ISlot slot = prefab.GetComponent<ISlot>();
            extent = slot?.DefaultExtent ?? DEFAULT_ITEM_SIZE;
            layoutSized = slot != null && extent <= 0f;

            RectTransform rect = prefab.GetComponent<RectTransform>();
            float prefabExtent = rect == null ? 0f : settings.IsVertical ? rect.rect.height : rect.rect.width;
            placeholder = prefabExtent > 0f ? prefabExtent : DEFAULT_ITEM_SIZE;
        }

        /// <summary>True when the entry is a header (only a sectioned list has headers).</summary>
        private bool IsHeaderEntry(int index)
            => headerPrefab != null && hooks.IsHeader(index);

        /// <summary>True when this entry's kind (header or item) is measured with layout.</summary>
        private bool IsLayoutSizedEntry(int index)
            => IsHeaderEntry(index) ? isHeaderLayoutSized : isLayoutSized;

        private bool MeasuresAny => isLayoutSized || isHeaderLayoutSized;

        /// <summary>Only measured items count in the average, and only unmeasured items take it: headers keep their own placeholder.</summary>
        private bool CountsInAverage(int index)
            => !IsExpanded(index) && !IsHeaderEntry(index);

        /// <summary>Gives unmeasured items the average of the measured ones. Headers and fixed sizes are left alone.</summary>
        /// <remarks>
        /// Each pass visits every item, so it only runs when the average moved by more than 0.5% (at least half a
        /// pixel) since the last one. Placeholders are estimates anyway, and the average settles after a few screens.
        /// </remarks>
        private void UpdatePlaceholders()
        {
            if (!isLayoutSized)
                return;

            float average = layout.AverageMeasuredSize(placeholderSize);
            float threshold = Mathf.Max(PLACEHOLDER_MIN_CHANGE, appliedPlaceholder * PLACEHOLDER_RELATIVE_CHANGE);
            if (!float.IsNaN(appliedPlaceholder) && Mathf.Abs(average - appliedPlaceholder) < threshold)
                return;

            appliedPlaceholder = average;
            Work.PlaceholderPasses++;
            layout.SetUnmeasuredSize(average, headerPrefab != null ? isHeaderEntryFunc : null);
        }

        bool ISlotCallbacks.NeedsMeasuring(int index)
            => (IsLayoutSizedEntry(index) || expansion.IsMeasuredExpanded(index)) && !layout.IsMeasured(index);

        /// <summary>Measures a shown slot with Unity's layout when the prefab is layout-sized and the slot was just bound or never measured.</summary>
        void ISlotCallbacks.OnSlotShown(SlotHandle slot, int index, bool justBound)
        {
            if (slot == null || index < 0 || index >= layout.Placements.Length)
                return;

            bool measuredWhileExpanded = expansion.IsMeasuredExpanded(index);
            if (!IsLayoutSizedEntry(index) && !measuredWhileExpanded)
            {
                // A fixed size: a watcher left from an item measured before must not report for this one.
                slot.SizeWatcher?.StopWatching();
                return;
            }

            SlotSizeWatcher watcher = WatchSize(slot, index);

            bool isMeasured = layout.IsMeasured(index);
            if (isMeasured && !justBound)
                return;

            // A measured item rebound from the pool keeps its cached size: no layout rebuild. If its content changed
            // while it was off screen, the watcher sees the new size after this frame's layout pass and resizes it.
            // Refresh (data changed) and invalidated items are measured at once.
            if (isMeasured && !forceRebindAll && !remeasuring.Contains(index))
            {
                watcher.MarkReported(layout.Placements[index].Size);
                return;
            }

            // A fixed-size slot opened without a size: let Unity's own layout pass measure it this frame, and take the
            // size from the watcher's report (applied next frame). TMP lays the text out once instead of twice, and
            // text that arrives late (localization) is followed the same way. The slot grows one frame later.
            if (measuredWhileExpanded && slotRootHasLayout)
            {
                watcher.MarkReported(-1f);
                LayoutRebuilder.MarkLayoutForRebuild(slot.Rect);
                pendingResizes.Remove(index);
                return;
            }

            float size;
            Work.Measurements++;
            using (SlotListMarkers.Measure.Auto())
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(slot.Rect);
                size = LayoutUtility.GetPreferredSize(slot.Rect, settings.IsVertical ? 1 : 0);
            }

            // A root with no layout reports 0: keep the prefab's size instead of collapsing the slot.
            if (size <= 0f && measuredWhileExpanded)
            {
                // A fixed-size slot with no layout to measure expands to twice its size, as before.
                size = GetDefaultItemSizeFromPrefab() * EXPANDED_SIZE_FALLBACK_MULTIPLIER;
            }
            else if (size <= 0f)
            {
                WarnAboutMissingLayout();
                size = IsHeaderEntry(index) ? headerPlaceholderSize : placeholderSize;
            }

            watcher.MarkReported(size);
            pendingResizes.Remove(index);

            if (isMeasured && Mathf.Abs(size - layout.Placements[index].Size) < SIZE_TOLERANCE)
                return;

            layout.SetItemSize(index, size, CountsInAverage(index));
            RememberLoopSize(index, size);
            sizesChanged = true;
            // A first measurement grows upwards above the anchor; a remeasured item (invalidated) keeps its start.
            if (!isMeasured && !remeasuring.Contains(index))
                newlyMeasuredLines.Add(layout.LineOf(index));
        }

        /// <summary>Measures an item again: at once if it's on screen, otherwise when it's shown. Fixed sizes ignore it.</summary>
        /// <summary>In a looping list, takes a real index and measures every copy again.</summary>
        public void InvalidateSize(int index)
        {
            if (!isInitialized || !isLooping)
            {
                InvalidateEntrySize(index);
                return;
            }

            if (!IsValidPublicIndex(index))
                return;

            if (isLoopWindow)
            {
                InvalidateLoopWindowEntries(index);
                return;
            }

            for (int copy = 0; copy < loopCopies; copy++)
                InvalidateEntrySize(index + copy * RealCount);
        }

        private void InvalidateEntrySize(int index)
        {
            if (!isInitialized || index < 0 || index >= layout.Placements.Length)
                return;
            if (!IsLayoutSizedEntry(index) && !expansion.IsMeasuredExpanded(index))
                return;

            layout.ForgetSize(index);
            if (layout.Placements[index]?.Handle != null)
                remeasuring.Add(index);

            RemeasureShownItems();
        }

        /// <summary>
        /// Measures every item again (a language or width change): the items on screen at once, keeping the first one
        /// still, and the others when they're shown. Until then they take the average of the new measurements.
        /// </summary>
        public void InvalidateAllSizes()
        {
            if (!isInitialized || !MeasuresAny)
                return;

            layout.ForgetAllSizes();
            ForgetAllLoopSizes();
            appliedPlaceholder = float.NaN; // every item is unmeasured now: the next pass must run
            pendingResizes.Clear();

            for (int line = FirstVisibleLine; line >= 0 && line <= LastVisibleLine && line < layout.Lines.Length; line++)
            {
                Line shownLine = layout.Lines[line];
                for (int slot = 0; slot < shownLine.SlotCount; slot++)
                {
                    Placement placement = shownLine[slot];
                    if (placement?.Handle != null)
                        remeasuring.Add(placement.DataIndex);
                }
            }

            RemeasureShownItems();
        }

        private void RemeasureShownItems()
        {
            visibleRange.ResetState();
            RebuildVisibleItems();
            remeasuring.Clear();
        }

        /// <summary>Gives a layout-measured slot its size watcher (once) and points it at the item it now shows.</summary>
        private SlotSizeWatcher WatchSize(SlotHandle slot, int index)
        {
            SlotSizeWatcher watcher = slot.SizeWatcher;
            if (watcher == null)
            {
                watcher = slot.GameObject.GetComponent<SlotSizeWatcher>();
                if (watcher == null)
                    watcher = slot.GameObject.AddComponent<SlotSizeWatcher>();
                slot.SizeWatcher = watcher;
            }

            watcher.Watch(this, index, settings.IsVertical);
            return watcher;
        }

        /// <summary>Called during Unity's layout pass: only remembered here, applied by <see cref="ApplyPendingResizes"/>.</summary>
        void ISlotSizeListener.OnSlotSizeReported(int index, float size)
        {
            if (size > 0f && (MeasuresAny || expansion.IsMeasuredExpanded(index)))
                pendingResizes[index] = size;
        }

        /// <summary>
        /// Resizes the slots whose watchers reported a new size since the last call, and moves the items after them.
        /// Runs after Unity's layout pass (SlotList.LateUpdate), never inside it.
        /// </summary>
        public void ApplyPendingResizes()
        {
            if (pendingResizes.Count == 0)
                return;

            using var _ = SlotListMarkers.ApplySizes.Auto();

            // A re-measured item keeps its start: anchor on the first shown line, measured before any change.
            layout.RecomputeOffsets();
            int anchorLine = FirstVisibleLine;
            float anchorOffset = layout.OffsetOf(anchorLine);
            bool changed = false;
            foreach (KeyValuePair<int, float> resize in pendingResizes)
            {
                int index = resize.Key;
                bool stillShown = index >= 0 && index < layout.Placements.Length && layout.Placements[index]?.Handle != null;
                // An opened fixed-size slot gets its first measurement here, from Unity's layout pass.
                bool firstMeasureOfOpenSlot = expansion.IsMeasuredExpanded(index) && !layout.IsMeasured(index);
                if (!isInitialized || !stillShown || (!layout.IsMeasured(index) && !firstMeasureOfOpenSlot))
                    continue;

                if (!firstMeasureOfOpenSlot && Mathf.Abs(resize.Value - layout.Placements[index].Size) < SIZE_TOLERANCE)
                    continue;

                layout.SetItemSize(index, resize.Value, CountsInAverage(index));
                RememberLoopSize(index, resize.Value);
                changed = true;
            }

            pendingResizes.Clear();
            if (!changed)
                return;

            UpdatePlaceholders();
            RecalculateContentSize();
            KeepLineStill(anchorLine, anchorOffset);
            visibleRange.ResetState();
            RebuildVisibleItems();
        }

        /// <summary>
        /// The line to keep still after this pass measured items: the first shown line that wasn't measured for the
        /// first time, so newly measured items above it grow upwards. If every shown line is new (a jump), the first one.
        /// </summary>
        private int PickAnchorLine()
        {
            for (int line = FirstVisibleLine; line >= 0 && line <= LastVisibleLine; line++)
            {
                if (!newlyMeasuredLines.Contains(line))
                    return line;
            }

            return FirstVisibleLine;
        }

        /// <summary>
        /// Scrolls by however much <paramref name="line"/> moved since it started at <paramref name="offsetBefore"/>,
        /// so size changes above it don't move what's on screen.
        /// </summary>
        private void KeepLineStill(int line, float offsetBefore)
        {
            if (line < 0 || line >= layout.Lines.Length)
                return;

            layout.RecomputeOffsets();
            float shift = layout.OffsetOf(line) - offsetBefore;
            if (Mathf.Abs(shift) < 0.01f)
                return;

            viewportMetrics.SetScrollDistance(Mathf.Max(0f, viewportMetrics.GetScrollDistance() + shift));
            hooks.OnContentShifted();
        }
    }
}
