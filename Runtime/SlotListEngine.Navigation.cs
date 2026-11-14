using UnityEngine;

namespace SlotList
{
    /// <summary>Scrolling to items and ends, with or without an animation, and snapping after a drag.</summary>
    internal sealed partial class SlotListEngine<T>
    {
        private const float SNAP_DURATION = 0.25f;
        private const float SNAP_MAX_SPEED = 600f;
        private const float PAGE_FLICK_SPEED = 400f;
        private const float ARRIVED_TOLERANCE = 0.5f;

        private enum TargetKind
        {
            Item,
            Distance,
            End
        }

        private readonly ScrollAnimation scrollAnimation = new();
        private TargetKind targetKind;
        private int targetIndex;
        private ScrollAlign targetAlign;
        private float targetOffset;
        private float targetDistance;
        private bool isSnapScroll;
        private bool snapPending;
        private float dragStartDistance;

        public bool IsAnimatingScroll => scrollAnimation.IsRunning;

        // ---------- Scroll to ----------

        /// <summary>In a looping list, takes a real index and goes the shortest way round.</summary>
        public void ScrollToItem(int index, ScrollAlign align, float duration, float offset)
        {
            if (!isInitialized || !IsValidPublicIndex(index))
                return;

            targetKind = TargetKind.Item;
            targetAlign = align;
            targetOffset = offset;
            targetIndex = isLooping ? ShortestCopyTo(index) : index;
            if (isLoopWindow && LineIndexOf(targetIndex) < 0)
            {
                // Too far for the window: map the window around the item, then aim at it there.
                CentreLoopWindowOn(index, 0f);
                targetIndex = ToNearestCopy(index);
            }

            if (LineIndexOf(targetIndex) < 0)
                return;

            BeginScroll(duration, snap: false);
        }

        /// <summary>Of the copies around the screen, the one whose aligned position is the shortest scroll away.</summary>
        private int ShortestCopyTo(int realIndex)
        {
            int nearest = ToNearestCopy(realIndex);
            float current = viewportMetrics.GetScrollDistance();
            int best = nearest;
            float bestDistance = float.MaxValue;
            for (int candidate = nearest - RealCount; candidate <= nearest + RealCount; candidate += RealCount)
            {
                if (LineIndexOf(candidate) < 0)
                    continue;

                targetIndex = candidate;
                float distance = Mathf.Abs(CurrentTarget() - current);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>A looping list has no start: it goes to the first item.</summary>
        public void ScrollToStart(float duration)
        {
            if (isLooping)
                ScrollToItem(0, ScrollAlign.Start, duration, 0f);
            else
                ScrollToDistance(0f, duration, snap: false);
        }

        /// <summary>A looping list has no end: it goes to the last item.</summary>
        public void ScrollToEnd(float duration)
        {
            if (!isInitialized)
                return;

            if (isLooping)
            {
                ScrollToItem(RealCount - 1, ScrollAlign.End, duration, 0f);
                return;
            }

            targetKind = TargetKind.End;
            BeginScroll(duration, snap: false);
        }

        public void ForceStopScrolling()
        {
            if (!isInitialized)
                return;

            CancelScrollAnimation();
            mover.ForceStop();
            dragState.ForceStopScrolling();
        }

        private void ScrollToDistance(float distance, float duration, bool snap)
        {
            if (!isInitialized)
                return;

            targetKind = TargetKind.Distance;
            targetDistance = distance;
            BeginScroll(duration, snap);
        }

        private void BeginScroll(float duration, bool snap)
        {
            mover.ForceStop();
            snapPending = false;
            isSnapScroll = snap;

            if (duration > 0f)
            {
                scrollAnimation.Start(viewportMetrics.GetScrollDistance(), duration);
                return;
            }

            scrollAnimation.Cancel();
            MoveTo(CurrentTarget());
            FinishScroll();
        }

        private void CancelScrollAnimation()
        {
            scrollAnimation.Cancel();
            snapPending = false;
            isSnapScroll = false;
        }

        /// <summary>Read every frame, so an item's target follows sizes measured on the way.</summary>
        private float CurrentTarget()
        {
            float content = layout.TotalExtent;
            switch (targetKind)
            {
                case TargetKind.End:
                    return ScrollTargets.MaxDistance(viewportSize, content);
                case TargetKind.Distance:
                    return ScrollTargets.Clamp(targetDistance, viewportSize, content);
            }

            int line = LineIndexOf(targetIndex);
            if (line < 0)
                return viewportMetrics.GetScrollDistance();

            layout.RecomputeOffsets();
            return ScrollTargets.For(layout.OffsetOf(line), layout.Lines[line].Size, viewportSize, content, viewportMetrics.GetScrollDistance(), targetAlign, targetOffset);
        }

        private void MoveTo(float distance)
        {
            viewportMetrics.SetScrollDistance(distance);
            RebuildVisibleItems();
        }

        private void FinishScroll()
        {
            if (!isSnapScroll)
                return;

            isSnapScroll = false;
            int index = targetKind == TargetKind.Item ? targetIndex : ItemAtDistance(viewportMetrics.GetScrollDistance());
            if (index >= 0)
                hooks.OnSnapped(ToReal(index));
        }

        // ---------- Every frame ----------

        /// <summary>Moves a running animation on, and starts a snap once a released list slows down. Called from LateUpdate.</summary>
        public void Tick(float deltaTime)
        {
            if (!isInitialized)
                return;

            if (scrollAnimation.IsRunning)
            {
                float progress = scrollAnimation.Advance(deltaTime);
                if (targetKind == TargetKind.Item && LineIndexOf(targetIndex) < 0)
                {
                    CancelScrollAnimation();
                    return;
                }

                MoveTo(scrollAnimation.PositionAt(progress, CurrentTarget()));
                if (!scrollAnimation.IsRunning)
                {
                    // Sizes measured on the last frame can move the target: land exactly on it.
                    MoveTo(CurrentTarget());
                    FinishScroll();
                }

                return;
            }

            if (snapPending)
                TrySnapToNearestItem();
        }

        // ---------- Snapping ----------

        private void BeginSnapAfterDrag()
        {
            switch (settings.snap)
            {
                case SnapMode.Item:
                    snapPending = true;
                    TrySnapToNearestItem();
                    break;
                case SnapMode.Page:
                    SnapToPage();
                    break;
            }
        }

        /// <summary>Waits until the list is back inside its content and slower than the snap speed, then settles on the nearest item.</summary>
        private void TrySnapToNearestItem()
        {
            float current = viewportMetrics.GetScrollDistance();
            float max = ScrollTargets.MaxDistance(viewportSize, layout.TotalExtent);
            bool outOfBounds = current < -ARRIVED_TOLERANCE || current > max + ARRIVED_TOLERANCE;
            if (outOfBounds || Mathf.Abs(viewportMetrics.ToScrollSpeed(components.ScrollRect.velocity)) > SNAP_MAX_SPEED)
                return;

            snapPending = false;
            int index = NearestSnapItem(current);
            if (index < 0)
                return;

            targetKind = TargetKind.Item;
            targetIndex = index;
            targetAlign = ToScrollAlign(settings.snapAlign);
            targetOffset = 0f;
            bool arrived = Mathf.Abs(CurrentTarget() - current) <= ARRIVED_TOLERANCE;
            BeginScroll(arrived ? 0f : SNAP_DURATION, snap: true);
        }

        /// <summary>A flick moves one page from where the drag started; a slow release settles on the nearest page.</summary>
        private void SnapToPage()
        {
            float page = viewportSize;
            if (page <= 0f)
                return;

            float speed = viewportMetrics.ToScrollSpeed(components.ScrollRect.velocity);
            float current = viewportMetrics.GetScrollDistance();
            float pageIndex = Mathf.Abs(speed) > PAGE_FLICK_SPEED
                ? Mathf.Round(dragStartDistance / page) + Mathf.Sign(speed)
                : Mathf.Round(current / page);

            float target = ScrollTargets.Clamp(pageIndex * page, viewportSize, layout.TotalExtent);
            ScrollToDistance(target, Mathf.Abs(target - current) <= ARRIVED_TOLERANCE ? 0f : SNAP_DURATION, snap: true);
        }

        /// <summary>The shown item whose aligned position is closest to where the list is now.</summary>
        private int NearestSnapItem(float current)
        {
            int first = FirstVisibleLine;
            int last = LastVisibleLine;
            if (first < 0 || last < 0)
                return -1;

            layout.RecomputeOffsets();
            ScrollAlign align = ToScrollAlign(settings.snapAlign);
            float content = layout.TotalExtent;
            int bestLine = -1;
            float bestDistance = float.MaxValue;
            for (int line = first; line <= last && line < layout.Lines.Length; line++)
            {
                float target = ScrollTargets.For(layout.OffsetOf(line), layout.Lines[line].Size, viewportSize, content, current, align, 0f);
                float distance = Mathf.Abs(target - current);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestLine = line;
                }
            }

            return FirstItemIn(bestLine);
        }

        /// <summary>The first item whose line reaches past <paramref name="distance"/>.</summary>
        private int ItemAtDistance(float distance)
        {
            layout.RecomputeOffsets();
            int first = Mathf.Max(0, FirstVisibleLine);
            for (int line = first; line < layout.Lines.Length; line++)
            {
                if (layout.OffsetOf(line) + layout.Lines[line].Size > distance + ARRIVED_TOLERANCE)
                    return FirstItemIn(line);
            }

            return VisibleStartIndex;
        }

        private static ScrollAlign ToScrollAlign(SnapAlign align) => align switch
        {
            SnapAlign.Center => ScrollAlign.Center,
            SnapAlign.End => ScrollAlign.End,
            _ => ScrollAlign.Start
        };
    }
}
