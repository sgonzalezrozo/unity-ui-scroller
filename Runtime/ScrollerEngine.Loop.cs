using UnityEngine;

namespace UIScroller
{
    /// <summary>
    /// The looping list (Settings.loop). Layout and slots use virtual entries; the public API uses real items.
    /// A short list lays out a few copies of its items and re-centres by whole copies. A long one would make those
    /// copies millions of pixels tall, where positions only move in quarter-pixel steps, so it lays out a short window
    /// of entries instead, mapped to real items through an index offset. Near either end of the window it moves the
    /// view back to the middle and shifts the offset by as many entries, so the same items stay on screen.
    /// </summary>
    internal sealed partial class ScrollerEngine<T>
    {
        // Four copies longer than this use a window instead, so scroll positions stay small.
        private const float MAX_COPIES_EXTENT = 20_000f;
        private const float WINDOW_SCREENS = 10f;
        private const int MIN_WINDOW_ENTRIES = 32;

        private bool isLooping;
        private bool isLoopWindow;
        private int loopCopies = 1;
        private int loopEntries;
        private int loopIndexOffset;
        private int layoutRealCount;
        private bool hasWarnedAboutLoop;
        private int loopAnchorItem = -1;
        private float loopAnchorInset;

        // Measured sizes per real item, for a window whose entries show other items after each re-centre.
        private float[] loopRealSizes = System.Array.Empty<float>();
        private bool[] loopRealMeasured = System.Array.Empty<bool>();

        private int RealCount => Source?.ItemCount ?? 0;

        int IScrollerEngine.ItemCount => RealCount;

        /// <summary>Works out whether this list loops, with copies or a window, and how many entries it lays out.</summary>
        private void UpdateLooping()
        {
            isLooping = settings.loop && CanLoop();
            layoutRealCount = RealCount;
            if (!isLooping)
            {
                isLoopWindow = false;
                loopCopies = 1;
                loopEntries = RealCount;
                loopIndexOffset = 0;
                return;
            }

            float itemExtent = EstimatedLoopItemExtent();
            float period = RealCount * itemExtent;
            int copies = LoopIndex.Copies(viewportSize, period);
            isLoopWindow = period * copies > MAX_COPIES_EXTENT;
            if (isLoopWindow)
            {
                loopCopies = 1;
                loopEntries = Mathf.Max(MIN_WINDOW_ENTRIES, Mathf.CeilToInt(WINDOW_SCREENS * Mathf.Max(viewportSize, 1f) / itemExtent));
                EnsureLoopSizeCache();
            }
            else
            {
                loopCopies = copies;
                loopEntries = RealCount * copies;
                loopIndexOffset = 0;
            }
        }

        private bool CanLoop()
        {
            string reason = null;
            if (isBento)
                reason = "a bento layout";
            else if (settings.slotsPerLine != 1 || settings.autoSlotsPerLine)
                reason = "a grid";
            else if (headerPrefab != null || !hooks.SupportsLooping())
                reason = "sections";
            else if (hooks.SupportsPaging())
                reason = "a paged source";

            if (reason == null)
                return true;

            if (!hasWarnedAboutLoop)
            {
                hasWarnedAboutLoop = true;
                Debug.LogError($"[UIScroller] {host.gameObject.name}: Loop doesn't work with {reason}. The list won't loop.", host);
            }

            return false;
        }

        private float EstimatedLoopItemExtent()
        {
            float itemSize = isLayoutSized ? placeholderSize : GetDefaultItemSizeFromPrefab();
            return Mathf.Max(1f, itemSize) + settings.lineSpacing;
        }

        /// <summary>The real item a layout entry shows. Unchanged when the list doesn't loop.</summary>
        private int ToReal(int index)
            => isLooping && index >= 0 ? LoopIndex.Real(index + loopIndexOffset, RealCount) : index;

        /// <summary>The entry showing a real item closest to what's on screen, or the index itself when the list doesn't loop.</summary>
        private int ToNearestCopy(int realIndex)
        {
            if (!isLooping)
                return realIndex;

            int first = FirstItemIn(FirstVisibleLine);
            int last = LastItemIn(LastVisibleLine);
            int around = first >= 0 && last >= 0 ? (first + last) / 2 : loopEntries / 2;
            return LoopIndex.NearestCopy(LoopIndex.Real(realIndex - loopIndexOffset, RealCount), RealCount, around);
        }

        private bool IsValidPublicIndex(int index)
            => index >= 0 && index < (isLooping ? RealCount : layout.Placements.Length);

        private bool RejectInLoop()
        {
            if (!isLooping)
                return false;

            Debug.LogWarning($"[UIScroller] {host.gameObject.name}: a looping list doesn't expand items.", host);
            return true;
        }

        /// <summary>
        /// Remembers the real item at the top, and how far into it the list is scrolled, before a rebuild that can
        /// change the number of items or entries. Uses the old layout, since the source may already have changed.
        /// </summary>
        private void RememberLoopAnchor()
        {
            loopAnchorItem = -1;
            int firstLine = FirstVisibleLine;
            if (!isLooping || layoutRealCount == 0 || firstLine < 0 || firstLine >= layout.Lines.Length)
                return;

            layout.RecomputeOffsets();
            loopAnchorItem = LoopIndex.Real(FirstItemIn(firstLine) + loopIndexOffset, layoutRealCount);
            loopAnchorInset = viewportMetrics.GetScrollDistance() - layout.OffsetOf(firstLine);
        }

        /// <summary>Puts the remembered item back at the top after the layout was rebuilt: in the second copy, or the window's middle.</summary>
        private void RestoreLoopAnchor()
        {
            int item = loopAnchorItem;
            loopAnchorItem = -1;
            if (!isLooping || item < 0 || RealCount == 0)
                return;

            item = Mathf.Min(item, RealCount - 1);
            if (isLoopWindow)
            {
                CentreLoopWindowOn(item, loopAnchorInset);
                return;
            }

            int line = LineIndexOf(item + RealCount);
            if (line < 0)
                return;

            layout.RecomputeOffsets();
            viewportMetrics.SetScrollDistance(layout.OffsetOf(line) + loopAnchorInset);
            hooks.OnContentShifted();
        }

        /// <summary>
        /// Keeps the view away from the ends: by whole copies, or by re-centring the window. The same items end up in
        /// the same place, so nothing visible changes; a drag, an animation and a snap carry on from the new position.
        /// </summary>
        private void KeepInsideLoop()
        {
            if (!isLooping || RealCount == 0)
                return;

            if (isLoopWindow)
            {
                KeepInsideLoopWindow();
                return;
            }

            int count = RealCount;
            if (layout.Lines.Length < count * LoopIndex.MIN_COPIES)
                return;

            layout.RecomputeOffsets();
            float start = layout.OffsetOf(0);
            float period = layout.OffsetOf(count) - start;
            float distance = viewportMetrics.GetScrollDistance();
            float shift = LoopIndex.Recentre(distance, viewportSize, start, period, loopCopies);
            if (shift == 0f)
                return;

            ApplyLoopShift(shift, Mathf.RoundToInt(shift / period) * count);
        }

        /// <summary>When the view is in the outer quarter of the window, puts its top item back in the window's middle.</summary>
        private void KeepInsideLoopWindow()
        {
            int lines = layout.Lines.Length;
            if (lines == 0)
                return;

            layout.RecomputeOffsets();
            float distance = viewportMetrics.GetScrollDistance();
            float start = layout.OffsetOf(0);
            float extent = layout.TotalExtent;
            float low = start + extent * 0.25f;
            float high = start + extent * 0.75f - viewportSize;
            if (distance >= low && distance <= high)
                return;

            int topLine = layout.LineAtDistance(Mathf.Clamp(distance, 0f, Mathf.Max(0f, extent - 1f)));
            if (topLine < 0)
                return;

            int topEntry = FirstItemIn(topLine);
            float inset = distance - layout.OffsetOf(topLine);
            CentreLoopWindowOn(ToReal(topEntry), inset, topEntry);
        }

        /// <summary>
        /// Maps the window so <paramref name="realItem"/> is its middle entry, at <paramref name="inset"/> above the
        /// viewport's start. <paramref name="fromEntry"/> is the entry that showed it before (for moving a running
        /// scroll target), or -1.
        /// </summary>
        private void CentreLoopWindowOn(int realItem, float inset, int fromEntry = -1)
        {
            int middle = loopEntries / 2;
            loopIndexOffset = LoopIndex.Real(realItem - middle, RealCount);
            RefreshLoopWindowSizes();

            // Every shown entry now shows another item: give the slots back so the visible items are bound again.
            recycler.CleanupActiveLines();
            visibleRange.ResetState();

            layout.RecomputeOffsets();
            float before = viewportMetrics.GetScrollDistance();
            float after = layout.OffsetOf(LineIndexOf(middle)) + inset;
            int movedEntries = fromEntry >= 0 ? middle - fromEntry : 0;
            ApplyLoopShift(after - before, movedEntries);
        }

        /// <summary>Moves the view by <paramref name="shift"/> pixels, and anything aimed at an entry by <paramref name="entries"/>.</summary>
        private void ApplyLoopShift(float shift, int entries)
        {
            if (Mathf.Abs(shift) < 0.001f && entries == 0)
                return;

            float distance = viewportMetrics.GetScrollDistance();
            viewportMetrics.SetScrollDistance(distance + shift);
            scrollAnimation.Shift(shift);
            dragStartDistance += shift;
            if (targetKind == TargetKind.Item)
                targetIndex += entries;
            else if (targetKind == TargetKind.Distance)
                targetDistance += shift;

            hooks.OnContentShifted();
        }

        // ---------- Sizes per real item (window only) ----------

        private bool LoopCachesSizes => isLoopWindow && MeasuresAny;

        private void EnsureLoopSizeCache()
        {
            if (!MeasuresAny || loopRealSizes.Length == RealCount)
                return;

            // A new item count: sizes remembered by position no longer belong to the same items.
            loopRealSizes = new float[RealCount];
            loopRealMeasured = new bool[RealCount];
        }

        /// <summary>Remembers an entry's measured size for the real item it shows, so it survives re-centring the window.</summary>
        private void RememberLoopSize(int entry, float size)
        {
            if (!LoopCachesSizes)
                return;

            int real = ToReal(entry);
            if (real < 0 || real >= loopRealSizes.Length)
                return;

            loopRealSizes[real] = size;
            loopRealMeasured[real] = true;
        }

        private void ForgetLoopSize(int real)
        {
            if (LoopCachesSizes && real >= 0 && real < loopRealMeasured.Length)
                loopRealMeasured[real] = false;
        }

        private void ForgetAllLoopSizes()
        {
            if (LoopCachesSizes)
                System.Array.Clear(loopRealMeasured, 0, loopRealMeasured.Length);
        }

        /// <summary>After the window's mapping changed, gives each entry the size of the real item it now shows.</summary>
        private void RefreshLoopWindowSizes()
        {
            if (!LoopCachesSizes)
                return;

            float placeholder = layout.AverageMeasuredSize(placeholderSize);
            for (int entry = 0; entry < layout.Placements.Length; entry++)
            {
                int real = ToReal(entry);
                layout.ForgetSize(entry);
                if (real >= 0 && real < loopRealMeasured.Length && loopRealMeasured[real])
                    layout.SetItemSize(entry, loopRealSizes[real]);
                else
                    layout.CacheItemSize(entry, placeholder);

                layout.SetLineSizeOf(entry, layout.Placements[entry].Size);
            }
        }

        /// <summary>The window entries that show <paramref name="real"/>, for measuring it again.</summary>
        private void InvalidateLoopWindowEntries(int real)
        {
            ForgetLoopSize(real);
            int count = RealCount;
            for (int entry = LoopIndex.Real(real - loopIndexOffset, count); entry < layout.Placements.Length; entry += count)
                InvalidateEntrySize(entry);
        }
    }
}
