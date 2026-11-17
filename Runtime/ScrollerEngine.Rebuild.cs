using UnityEngine;

namespace UIScroller
{
    /// <summary>Rebuilds after data or size changes, deferred work (first build, re-enable, grid resize, pool warm-up) and lifecycle.</summary>
    internal sealed partial class ScrollerEngine<T>
    {
        private const float DYNAMIC_GRID_MIN_DELTA = 5f;
        private const float RESIZE_MIN_DELTA = 0.5f;

        private bool isRebuildingLayout;
        private float lastCrossAxisSize;
        // Deferred work, run from LateUpdate a frame later instead of in coroutines (a coroutine allocates when it starts).
        private bool gridUpdatePending;
        private int gridUpdateFrame;
        private bool poolWarmupPending;
        private int poolWarmupFrame;

        // ---------- Rebuilds ----------

        /// <summary>Rebuilds from the current source: everything if the count or slots per line changed, otherwise rebinds.</summary>
        public void Refresh()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[UIScroller] Refresh aborted - not initialized");
                return;
            }

            RereadSlotExtent();
            bool slotsPerLineChanged = ResolveSlotsPerLine();
            RememberLoopAnchor();
            UpdateLooping();
            forceRebindAll = true;

            if (ItemCount != layout.Placements.Length || slotsPerLineChanged)
                RebuildStructures();
            else
                RebuildForSameStructure(cleanDataIndices: true);

            forceRebindAll = false;
            loopAnchorItem = -1;
        }

        private void RebuildLayout()
        {
            if (isRebuildingLayout)
                return;

            isRebuildingLayout = true;
            try
            {
                lastCrossAxisSize = viewportMetrics.GetCrossAxisViewportSize();
                bool slotsPerLineChanged = ResolveSlotsPerLine();
                viewportSize = viewportMetrics.GetViewportSize();
                RememberLoopAnchor();
                UpdateLooping();
                forceRebindAll = true;

                if (ItemCount != layout.Placements.Length || slotsPerLineChanged)
                    RebuildStructures();
                else
                    RebuildForSameStructure(cleanDataIndices: false);

                forceRebindAll = false;
                loopAnchorItem = -1;
            }
            finally
            {
                isRebuildingLayout = false;
            }
        }

        /// <summary>Recomputes slots per line from the viewport. True if it changed.</summary>
        private bool ResolveSlotsPerLine()
        {
            float crossAxisSize = viewportMetrics.GetCrossAxisViewportSize();
            bool changed = gridResolver.Calculate(crossAxisSize) != gridResolver.Current;
            gridResolver.Resolve(crossAxisSize);
            return changed;
        }

        private void RebuildStructures()
        {
            using var _ = ScrollerMarkers.Rebuild.Auto();
            recycler.CleanupActiveLines();
            expansion.ClearAll();

            layout.CreatePlacements(ItemCount);
            BuildLines();

            RecalculateContentSize();
            layout.RecomputeOffsets();
            RestoreLoopAnchor();

            visibleRange.ResetState();
            RebuildVisibleItems();
            hooks.OnRebuilt();
        }

        private void RebuildForSameStructure(bool cleanDataIndices)
        {
            using var _ = ScrollerMarkers.Rebuild.Auto();
            bool hasExpanded = expansion.HasAny;

            visibleRange.ResetState();

            if (cleanDataIndices)
                ResetPlacementIndices();

            // With sections, a header can land where an item was (same count, new sections): line sizes change too.
            if (hasExpanded || headerPrefab != null)
            {
                BuildLines();
                RecalculateContentSize();
                layout.RecomputeOffsets();
            }
            else
            {
                RecalculateContentSize();
            }

            // Slots stay where they are: shown lines are rebound in place, lines that left the screen give theirs back.
            RebuildVisibleItems();
            hooks.OnRebuilt();
        }

        private void ResetPlacementIndices()
        {
            ListView<Placement> placements = layout.Placements;
            for (int i = 0; i < placements.Length; i++)
            {
                if (placements[i] != null)
                    placements[i].DataIndex = i;
            }
        }

        // ---------- Layout refresh (also used by background size work) ----------

        public void RecalculateContentSize()
        {
            if (!isInitialized)
                return;

            RectTransform content = components.Content;
            if (content != null)
            {
                Vector2 sizeDelta = content.sizeDelta;
                Vector2 newSizeDelta = sizeDelta;
                if (settings.IsVertical)
                    newSizeDelta.y = layout.TotalExtent;
                else
                    newSizeDelta.x = layout.TotalExtent;

                if (newSizeDelta != sizeDelta)
                    content.sizeDelta = newSizeDelta;

                float size = settings.IsVertical ? newSizeDelta.y : newSizeDelta.x;
                if (Mathf.Abs(size - lastContentSize) > 0.01f)
                {
                    lastContentSize = size;
                    hooks.OnContentSizeChanged(size);
                }
            }

            MarkPositionsDirty();
        }

        public void MarkPositionsDirty()
            => needsPositionRecalculation = true;

        public void RebuildVisibleItems()
        {
            if (!isInitialized)
                return;

            KeepInsideLoop();

            // Measuring slots can change sizes; place again until nothing changes (each item settles after one measure).
            for (int pass = 0; pass < MAX_MEASURE_PASSES; pass++)
            {
                sizesChanged = false;
                newlyMeasuredLines.Clear();
                visibilityPass.RebuildVisibleItems(ref needsPositionRecalculation, forceRebindAll && pass == 0);
                viewportSize = visibilityPass.LastViewportSize;

                if (!sizesChanged)
                    break;

                // The offsets still hold the values the items were placed with, before this pass measured them.
                int anchorLine = PickAnchorLine();
                float anchorOffset = layout.PlacedOffsetOf(anchorLine);

                UpdatePlaceholders();
                RecalculateContentSize();
                KeepLineStill(anchorLine, anchorOffset);
                visibleRange.ResetState();
            }

            RequestPageIfNeeded();
        }

        // ---------- Lifecycle ----------

        /// <summary>
        /// Slots stayed where they were while the list was disabled (P8), so coming back only checks the visible range:
        /// nothing is rebound or moved unless the viewport or the scroll position changed. Data changes still need Refresh().
        /// </summary>
        public void OnEnabled()
        {
            if (!isInitialized)
                return;

            if (settings.resetScrollOnEnable)
                mover.ScrollToStart();

            visibleRange.ResetState();
            RebuildVisibleItems();
        }

        /// <summary>Nothing to do: the slots are hidden with the list and stay attached to their lines (P8).</summary>
        public void OnDisabled()
        {
        }

        public void Dispose()
        {
            DetachSource();
            CancelPendingGridUpdate();

            if (components?.ScrollRect != null)
                components.ScrollRect.onValueChanged.RemoveListener(OnScroll);

            slotPool?.DestroyAllSlots();
        }

        /// <summary>
        /// After a cross-axis resize, re-places the visible slots on the next frame, and rebuilds the lines
        /// if an automatic slots-per-line count changed.
        /// </summary>
        public void OnViewportResized()
        {
            if (!isInitialized)
                return;

            float crossAxisSize = viewportMetrics.GetCrossAxisViewportSize();
            float threshold = gridResolver.IsDynamic ? DYNAMIC_GRID_MIN_DELTA : RESIZE_MIN_DELTA;
            if (Mathf.Abs(crossAxisSize - lastCrossAxisSize) < threshold)
                return;

            lastCrossAxisSize = crossAxisSize;

            gridUpdatePending = true;
            gridUpdateFrame = Time.frameCount;
        }

        // ---------- Deferred work ----------

        /// <summary>
        /// Binds and places the first screen inside Initialize, so it shows in the first rendered frame (P10). The
        /// canvases are laid out first, so the viewport has its real size even right after a screen was instantiated.
        /// </summary>
        private void BuildFirstScreen()
        {
            Canvas.ForceUpdateCanvases();
            viewportSize = viewportMetrics.GetViewportSize();

            RecalculateContentSize();
            components.Content.anchoredPosition = Vector2.zero;

            RebuildVisibleItems();
            hooks.OnReady();
        }

        /// <summary>
        /// Runs the work deferred to a later frame: the re-placement after a width change, and the pool warm-up (a few
        /// slots per frame). Called from LateUpdate; allocates nothing.
        /// </summary>
        public void RunDeferredWork()
        {
            int frame = Time.frameCount;
            if (gridUpdatePending && frame > gridUpdateFrame)
            {
                gridUpdatePending = false;
                if (isInitialized)
                    UpdateAfterResize();
            }

            if (poolWarmupPending && frame > poolWarmupFrame)
            {
                poolWarmupFrame = frame;
                if (isInitialized && slotPool != null)
                    slotPool.PrewarmSome(WARMUP_PER_FRAME);
                poolWarmupPending = isInitialized && slotPool != null && !slotPool.IsPrewarmed;
            }
        }

        private void UpdateAfterResize()
        {
            if (isBento)
            {
                RebuildBentoForNewWidth();
            }
            else if (gridResolver.IsDynamic && gridResolver.Calculate(viewportMetrics.GetCrossAxisViewportSize()) != gridResolver.Current)
            {
                RebuildLayout();
            }
            else if (MeasuresAny)
            {
                // Text rewraps at the new width, so every measured size is stale.
                InvalidateAllSizes();
            }
            else
            {
                visibleRange.ResetState();
                RebuildVisibleItems();
            }
        }

        private void ScheduleDeferredPoolWarmup()
        {
            if (slotPool == null || slotPool.IsPrewarmed)
                return;

            poolWarmupPending = true;
            poolWarmupFrame = Time.frameCount;
        }

        private void CancelPendingGridUpdate()
        {
            gridUpdatePending = false;
            poolWarmupPending = false;
        }
    }
}
