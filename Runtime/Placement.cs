using UnityEngine;

namespace SlotList
{
    /// <summary>One item's place in a line: its index, cached size, expansion flag and the slot showing it (if any).</summary>
    public class Placement
    {
        /// <summary>The pooled slot showing this item, or null while it's off screen.</summary>
        public SlotHandle Handle { get; set; }

        /// <summary>The slot's RectTransform, or null.</summary>
        public RectTransform Slot => Handle?.Rect;

        public float Size { get; private set; }
        public bool WasExpanded { get; set; }

        /// <summary>True when the slot was just taken from the pool and still needs binding.</summary>
        public bool NeedsBind { get; set; }
        public int DataIndex { get; set; }

        /// <summary>Where the slot was last placed: line offset, line size, slots in the line and cross size. NaN when never placed.</summary>
        internal float PlacedOffset = float.NaN;
        internal float PlacedSize;
        internal int PlacedCount;
        internal float PlacedCross;

        /// <summary>True when the slot already sits where these values put it (P3: no need to place it again).</summary>
        internal bool IsPlacedAt(float offset, float size, int count, float cross)
            => PlacedOffset == offset && PlacedSize == size && PlacedCount == count && PlacedCross == cross;

        internal void MarkPlaced(float offset, float size, int count, float cross)
        {
            PlacedOffset = offset;
            PlacedSize = size;
            PlacedCount = count;
            PlacedCross = cross;
        }

        internal void ForgetPlacement()
            => PlacedOffset = float.NaN;

        public Placement(int dataIndex)
        {
            DataIndex = dataIndex;
        }

        /// <summary>Makes a reused placement new again, for item <paramref name="dataIndex"/>. It must hold no slot.</summary>
        internal void Reset(int dataIndex)
        {
            DataIndex = dataIndex;
            Size = 0f;
            WasExpanded = false;
            NeedsBind = false;
            Handle = null;
            ForgetPlacement();
        }

        public void CacheSize(float size)
        {
            Size = size;
        }

        public override string ToString()
            => $"DataIndex:{DataIndex} Size:{Size}";
    }
}
