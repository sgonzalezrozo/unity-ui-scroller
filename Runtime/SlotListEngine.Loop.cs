using UnityEngine;

namespace SlotList
{
    /// <summary>
    /// The looping list (Settings.loop): a few copies of the items laid out one after another, re-centred by whole
    /// copies before the scroll reaches an end. Layout and slots use virtual indices; the public API uses real ones.
    /// </summary>
    internal sealed partial class SlotListEngine<T>
    {
        private bool isLooping;
        private int loopCopies = 1;
        private bool hasWarnedAboutLoop;
        private int loopAnchorItem = -1;
        private float loopAnchorInset;

        private int RealCount => Source?.ItemCount ?? 0;

        int ISlotListEngine.ItemCount => RealCount;

        /// <summary>Works out whether this list loops, and how many copies it needs for the current viewport and sizes.</summary>
        private void UpdateLooping()
        {
            isLooping = settings.loop && CanLoop();
            loopCopies = isLooping ? LoopIndex.Copies(viewportSize, EstimatedLoopPeriod()) : 1;
        }

        private bool CanLoop()
        {
            string reason = null;
            if (settings.slotsPerLine != 1 || settings.autoSlotsPerLine)
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
                Debug.LogError($"[SlotList] {host.gameObject.name}: Loop doesn't work with {reason}. The list won't loop.", host);
            }

            return false;
        }

        private float EstimatedLoopPeriod()
        {
            float itemSize = isLayoutSized ? placeholderSize : GetDefaultItemSizeFromPrefab();
            return RealCount * (Mathf.Max(1f, itemSize) + settings.lineSpacing);
        }

        /// <summary>The real item a layout index shows. Unchanged when the list doesn't loop.</summary>
        private int ToReal(int index)
            => isLooping && index >= 0 ? LoopIndex.Real(index, RealCount) : index;

        /// <summary>The copy of a real item closest to what's on screen, or the index itself when the list doesn't loop.</summary>
        private int ToNearestCopy(int realIndex)
        {
            if (!isLooping)
                return realIndex;

            int first = FirstItemIn(FirstVisibleLine);
            int last = LastItemIn(LastVisibleLine);
            int around = first >= 0 && last >= 0 ? (first + last) / 2 : RealCount;
            return LoopIndex.NearestCopy(realIndex, RealCount, around);
        }

        private bool IsValidPublicIndex(int index)
            => index >= 0 && index < (isLooping ? RealCount : layout.Placements.Length);

        private bool RejectInLoop()
        {
            if (!isLooping)
                return false;

            Debug.LogWarning($"[SlotList] {host.gameObject.name}: a looping list doesn't expand items.", host);
            return true;
        }

        /// <summary>
        /// Remembers the real item at the top, and how far into it the list is scrolled, before a rebuild that can
        /// change the number of items or copies. Uses the old layout, since the source may already have changed.
        /// </summary>
        private void RememberLoopAnchor()
        {
            loopAnchorItem = -1;
            int oldCount = layout.Placements.Length / Mathf.Max(1, loopCopies);
            int firstLine = FirstVisibleLine;
            if (!isLooping || oldCount == 0 || firstLine < 0 || firstLine >= layout.Lines.Length)
                return;

            layout.RecomputeOffsets();
            loopAnchorItem = LoopIndex.Real(FirstItemIn(firstLine), oldCount);
            loopAnchorInset = viewportMetrics.GetScrollDistance() - layout.OffsetOf(firstLine);
        }

        /// <summary>Puts the remembered item back at the top, in the second copy, after the layout was rebuilt.</summary>
        private void RestoreLoopAnchor()
        {
            int item = loopAnchorItem;
            loopAnchorItem = -1;
            if (!isLooping || item < 0 || RealCount == 0)
                return;

            int line = LineIndexOf(Mathf.Min(item, RealCount - 1) + RealCount);
            if (line < 0)
                return;

            layout.RecomputeOffsets();
            viewportMetrics.SetScrollDistance(layout.OffsetOf(line) + loopAnchorInset);
            hooks.OnContentShifted();
        }

        /// <summary>
        /// Jumps by whole copies when the scroll gets within one copy of either end. The same items end up in the same
        /// place, so nothing visible changes; a drag, an animation and a snap carry on from the new position.
        /// </summary>
        private void KeepInsideLoop()
        {
            int count = RealCount;
            if (!isLooping || count == 0 || layout.Lines.Length < count * LoopIndex.MIN_COPIES)
                return;

            layout.RecomputeOffsets();
            float start = layout.OffsetOf(0);
            float period = layout.OffsetOf(count) - start;
            float distance = viewportMetrics.GetScrollDistance();
            float shift = LoopIndex.Recentre(distance, viewportSize, start, period, loopCopies);
            if (shift == 0f)
                return;

            int copies = Mathf.RoundToInt(shift / period);
            viewportMetrics.SetScrollDistance(distance + shift);
            scrollAnimation.Shift(shift);
            dragStartDistance += shift;
            if (targetKind == TargetKind.Item)
                targetIndex += copies * count;
            else if (targetKind == TargetKind.Distance)
                targetDistance += shift;

            hooks.OnContentShifted();
        }
    }
}
