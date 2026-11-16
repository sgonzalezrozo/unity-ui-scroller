using System;

namespace UIScroller
{
    /// <summary>
    /// A row (vertical list) or column (horizontal list) of slots. A line with room for one slot, every line of a plain
    /// list, keeps its placement in a field instead of a one-element array: one object less per item.
    /// </summary>
    public class Line
    {
        private readonly Placement[] slots;
        private Placement single;

        public float Size { get; private set; }
        public int LineIndex { get; internal set; }

        /// <summary>How many slots the line has room for. Some can be empty (null), such as at the end of a grid.</summary>
        public int SlotCount { get; }

        /// <summary>A header's line: the section spacing goes before it, unless it's the first line.</summary>
        internal bool StartsSection;

        public Line(int lineIndex, int slotCount)
        {
            LineIndex = lineIndex;
            SlotCount = Math.Max(1, slotCount);
            if (SlotCount > 1)
                slots = new Placement[SlotCount];
        }

        /// <summary>The placement in a slot of the line, or null when that slot is empty.</summary>
        public Placement this[int slot]
        {
            get => slots == null ? (slot == 0 ? single : null) : slots[slot];
            internal set
            {
                if (slots == null)
                    single = value;
                else
                    slots[slot] = value;
            }
        }

        /// <summary>Makes a reused line empty again, as line <paramref name="lineIndex"/>.</summary>
        internal void Reset(int lineIndex)
        {
            LineIndex = lineIndex;
            Size = 0f;
            StartsSection = false;
            single = null;
            if (slots != null)
                Array.Clear(slots, 0, slots.Length);
        }

        public void CacheSize(float size)
        {
            Size = size;
        }

        public override string ToString()
            => $"LineIndex:{LineIndex} Size:{Size} SlotCount:{SlotCount}";
    }
}
