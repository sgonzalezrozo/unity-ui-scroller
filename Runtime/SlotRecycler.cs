using System.Collections.Generic;
using UnityEngine;

namespace SlotList
{
    /// <summary>
    /// Shows the visible lines: returns slots of lines that left the screen, takes slots for lines that entered,
    /// places them and binds them. Lines come from the LayoutModel.
    /// </summary>
    internal class SlotRecycler
    {
        private readonly SlotPool slotPool;
        private readonly IAxisLayout direction;
        private readonly RectTransform viewport;
        private readonly RectTransform content;
        private readonly LayoutModel layout;
        private readonly ExpansionState expansion;
        private readonly ISlotCallbacks callbacks;
        private readonly SlotListSettings settings;
        private readonly WorkCounters work;

        // The placements holding a slot. Checking these, not every line, keeps recycling proportional to the screen (P2).
        private readonly List<Placement> shown = new();
        private float crossSize;

        /// <summary>The columns of a bento layout, or 0. In bento, a slot is placed from its tile instead of its index in the line.</summary>
        public int BentoColumns { get; set; }

        /// <summary>Decides which newly shown slots play their appear animation.</summary>
        public AppearAnimationPolicy AppearPolicy { get; } = new();

        public SlotRecycler(SlotPool slotPool, ScrollRectBinding binding, LayoutModel layout, ExpansionState expansion, ISlotCallbacks callbacks, SlotListSettings settings, WorkCounters work)
        {
            this.work = work;
            this.slotPool = slotPool;
            direction = binding.Direction;
            viewport = binding.Viewport;
            content = binding.Content;
            this.layout = layout;
            this.expansion = expansion;
            this.callbacks = callbacks;
            this.settings = settings;
        }

        // ---------- Returning slots ----------

        /// <summary>Returns the slots of every line outside [first, last] to the pool. Only shown slots are checked.</summary>
        public void RecycleOutOfViewLines(int firstLine, int lastLine)
        {
            using var _ = SlotListMarkers.Recycle.Auto();
            int itemCount = callbacks.ItemCount;

            for (int i = shown.Count - 1; i >= 0; i--)
            {
                work.SlotsChecked++;
                Placement placement = shown[i];
                if (placement.Handle != null && placement.DataIndex < itemCount)
                {
                    int line = layout.LineOf(placement.DataIndex);
                    if (line >= firstLine && line <= lastLine)
                        continue;
                }

                Release(placement);
                RemoveShownAt(i);
            }
        }

        /// <summary>Returns every shown slot to the pool.</summary>
        public void CleanupActiveLines()
        {
            for (int i = shown.Count - 1; i >= 0; i--)
                Release(shown[i]);

            shown.Clear();
        }

        private void Release(Placement placement)
        {
            if (placement.Handle != null && placement.Slot != null)
                slotPool.ReturnSlot(placement.Handle);

            placement.Handle = null;
            placement.ForgetPlacement();
        }

        // Order doesn't matter: swap with the last one so removing is O(1).
        private void RemoveShownAt(int index)
        {
            int last = shown.Count - 1;
            shown[index] = shown[last];
            shown.RemoveAt(last);
        }

        // ---------- Showing slots ----------

        public void ShowLines(int firstLine, int lastLine, bool forceRebindAll)
        {
            ListView<Line> lines = layout.Lines;
            lastLine = Mathf.Min(lastLine, lines.Length - 1);
            crossSize = settings.IsVertical ? viewport.rect.width : viewport.rect.height;

            for (int lineIndex = firstLine; lineIndex <= lastLine; lineIndex++)
                ShowLine(lineIndex, firstLine, lastLine, lines[lineIndex], forceRebindAll);

            if (!AppearPolicy.HasMarkedOnce)
                AppearPolicy.NotifyFirstVisibleLinesDisplayed();
        }

        private void ShowLine(int lineIndex, int firstVisibleLine, int lastVisibleLine, Line line, bool forceRebindAll)
        {
            if (!TakeMissingSlots(line))
                return;

            float lineOffset = layout.OffsetOf(lineIndex);
            int itemCount = callbacks.ItemCount;

            for (int slotIndex = 0; slotIndex < line.Placements.Length; slotIndex++)
                ShowPlacement(line, slotIndex, lineIndex, lineOffset, itemCount, forceRebindAll, firstVisibleLine, lastVisibleLine);
        }

        private void ShowPlacement(
            Line line,
            int slotIndex,
            int lineIndex,
            float lineOffset,
            int itemCount,
            bool forceRebindAll,
            int firstVisibleLine,
            int lastVisibleLine)
        {
            Placement placement = line.Placements[slotIndex];
            if (placement?.Handle == null || placement.Slot == null)
                return;

            SlotHandle handle = placement.Handle;
            int itemIndex = placement.DataIndex;

            // A bento tile is a slot at its column in a line of BentoColumns cells, as long as its own span, stretched across its columns.
            bool isBento = BentoColumns > 0;
            float mainSize = isBento ? placement.Size : line.Size;
            int indexInLine = isBento ? placement.Tile.Column : slotIndex;
            // A header's line has room for a whole row of slots, but the header is one slot across it.
            int slotsInLine = isBento ? BentoColumns : line.StartsSection ? 1 : line.Placements.Length;

            // P3: a slot that already sits there keeps its RectTransform untouched (each write can dirty the canvas).
            if (forceRebindAll || placement.NeedsBind || !placement.IsPlacedAt(lineOffset, mainSize, slotsInLine, crossSize))
            {
                direction.PositionInLine(handle.Rect, lineOffset, mainSize, indexInLine, slotsInLine, viewport, content, settings.slotSpacing);
                if (isBento)
                    StretchAcrossColumns(handle.Rect, placement.Tile.Columns);
                placement.MarkPlaced(lineOffset, mainSize, slotsInLine, crossSize);
                work.SlotsPlaced++;
            }

            if (itemIndex >= itemCount)
            {
                handle.GameObject.SetActive(false);
                return;
            }

            bool isExpanded = expansion.IsExpanded(itemIndex);
            bool expansionChanged = placement.WasExpanded != isExpanded;
            if (expansionChanged)
                placement.WasExpanded = isExpanded;

            if (!placement.NeedsBind && !expansionChanged && !forceRebindAll)
            {
                handle.GameObject.SetActive(true);
                callbacks.OnSlotShown(handle, itemIndex, false);
                return;
            }

            placement.NeedsBind = false;
            using (SlotListMarkers.Bind.Auto())
            {
                AppearPolicy.Mark(handle, lineIndex, firstVisibleLine, lastVisibleLine);
                callbacks.SetupExpandable(handle, itemIndex);
                callbacks.BindItem(handle, itemIndex);
                handle.GameObject.SetActive(true);
            }
            callbacks.OnSlotShown(handle, itemIndex, true);
        }

        private void StretchAcrossColumns(RectTransform slot, int span)
        {
            float cell = (crossSize - settings.slotSpacing * (BentoColumns - 1)) / BentoColumns;
            float size = span * cell + (span - 1) * settings.slotSpacing;
            Vector2 sizeDelta = slot.sizeDelta;
            if (settings.IsVertical)
                sizeDelta.x = size;
            else
                sizeDelta.y = size;
            slot.sizeDelta = sizeDelta;
        }

        /// <summary>
        /// Gives every placement of the line that has no slot, or a slot of the wrong kind, one of the right kind, and marks it for binding.
        /// A line already on screen can gain placements when a page fills its empty slots. False if the pool failed.
        /// </summary>
        private bool TakeMissingSlots(Line line)
        {
            using var _ = SlotListMarkers.Acquire.Auto();
            int itemCount = callbacks.ItemCount;

            for (int i = 0; i < line.Placements.Length; i++)
            {
                Placement placement = line.Placements[i];
                if (placement == null || placement.DataIndex < 0 || placement.DataIndex >= itemCount)
                    continue;

                SlotKind kind = callbacks.IsHeader(placement.DataIndex) ? SlotKind.Header : SlotKind.Item;
                if (placement.Slot != null)
                {
                    // Kept through a refresh, but the entry changed kind (a section's header moved): swap the slot.
                    if (placement.Handle.Kind == kind)
                        continue;

                    slotPool.ReturnSlot(placement.Handle);
                    placement.Handle = null;
                    placement.ForgetPlacement();
                }
                else
                {
                    shown.Add(placement);
                }

                SlotHandle handle = slotPool.GetSlot(kind);
                if (handle == null)
                {
                    shown.Remove(placement);
                    return false;
                }

                placement.Handle = handle;
                placement.NeedsBind = true;
            }

            return true;
        }
    }
}
