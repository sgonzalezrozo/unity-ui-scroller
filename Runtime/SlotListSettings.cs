using System;
using UnityEngine;

namespace SlotList
{
    /// <summary>The direction items flow in. The first item is at the start of this direction.</summary>
    public enum SlotListDirection
    {
        TopToBottom,
        BottomToTop,
        LeftToRight,
        RightToLeft
    }

    /// <summary>What a list settles on after a drag. Page moves one viewport at a time, like a pager.</summary>
    public enum SnapMode
    {
        None,
        Item,
        Page
    }

    /// <summary>The part of the viewport snapped items line up with.</summary>
    public enum SnapAlign
    {
        Start,
        Center,
        End
    }

    /// <summary>Layout and behaviour settings for a whole slot list. Changes apply on the next Initialize.</summary>
    [Serializable]
    public class SlotListSettings
    {
        public const float DEFAULT_POOL_BUFFER = 1.4f;

        public SlotListDirection direction = SlotListDirection.TopToBottom;

        [Tooltip("Space between lines (rows in a vertical list, columns in a horizontal one).")]
        public float lineSpacing;

        [Tooltip("Space between slots in the same line.")]
        public float slotSpacing;

        [Tooltip("Extra space before each section's header, except the first. Sectioned lists only.")]
        public float sectionSpacing;

        public float paddingStart;
        public float paddingEnd;

        [Tooltip("Slots per line. 1 is a list, more is a grid. Slots scale to fill the line.")]
        public uint slotsPerLine = 1;

        [Tooltip("Work out the slots per line from the viewport width and Target Slot Size.")]
        public bool autoSlotsPerLine;

        public float targetSlotSize = 240f;
        public uint minSlotsPerLine = 1;
        public uint maxSlotsPerLine = 8;

        public bool allowMultipleExpanded;

        [Tooltip("Bento: tiles of different sizes on a grid of cells, placed by a Bento Layout (JSON or code). Lists only, no expanding.")]
        public bool bento;

        [Tooltip("Bento row height divided by column width. 1 makes square cells.")]
        public float cellAspect = 1f;

        [Tooltip("Loop round forever, like a carousel. Lists only: not grids, sections or paged sources. Items don't expand.")]
        public bool loop;

        [Tooltip("After a drag, settle on the nearest item, or move one page (viewport) at a time.")]
        public SnapMode snap = SnapMode.None;

        [Tooltip("The part of the viewport a snapped item lines up with. Pages always line up with the start.")]
        public SnapAlign snapAlign = SnapAlign.Start;

        [Tooltip("Go back to the start every time the list is enabled.")]
        public bool resetScrollOnEnable;

        public bool overridePoolBuffer;

        [Range(1.4f, 3.0f)]
        [Tooltip("How many slots to keep, as a multiple of the slots that fit on screen.")]
        public float poolBuffer = DEFAULT_POOL_BUFFER;

        public bool IsVertical
            => direction is SlotListDirection.TopToBottom or SlotListDirection.BottomToTop;

        public bool IsReversed
            => direction is SlotListDirection.BottomToTop or SlotListDirection.RightToLeft;

        public float EffectivePoolBuffer
            => overridePoolBuffer ? poolBuffer : DEFAULT_POOL_BUFFER;
    }
}
