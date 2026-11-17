using UnityEngine;

namespace UIScroller
{
    /// <summary>Snapping after a drag or the mouse wheel: to the nearest item (or bento tile), or one page at a time.</summary>
    internal sealed partial class ScrollerEngine<T>
    {
        // The wheel sends bursts of small steps: snap once it has been still this long.
        private const float WHEEL_SETTLE_SECONDS = 0.15f;
        private bool wheelSnapPending;
        private float wheelIdleTime;

        /// <summary>The mouse wheel moved the list: with snapping on, settle once the wheel stops, as after a drag.</summary>
        public void OnWheelScrolled()
        {
            if (!isInitialized || settings.snap == SnapMode.None)
                return;

            CancelScrollAnimation();
            snapPending = false;
            wheelSnapPending = true;
            wheelIdleTime = 0f;
        }

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
            if (isBento)
                return NearestSnapTile(first, last, current, content, align);

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

        /// <summary>
        /// Bento: the tile whose aligned position is closest, each by its own size. Rows that only tall tiles cross hold
        /// no tile start and are skipped.
        /// </summary>
        private int NearestSnapTile(int first, int last, float current, float content, ScrollAlign align)
        {
            int bestItem = -1;
            float bestDistance = float.MaxValue;
            for (int line = first; line <= last && line < layout.Lines.Length; line++)
            {
                Line row = layout.Lines[line];
                float start = layout.OffsetOf(line);
                for (int slot = 0; slot < row.SlotCount; slot++)
                {
                    Placement placement = row[slot];
                    if (placement == null)
                        continue;

                    float target = ScrollTargets.For(start, placement.Size, viewportSize, content, current, align, 0f);
                    float distance = Mathf.Abs(target - current);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestItem = placement.DataIndex;
                    }
                }
            }

            return bestItem;
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
