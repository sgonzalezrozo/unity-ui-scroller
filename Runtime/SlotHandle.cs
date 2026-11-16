using UnityEngine;

namespace UIScroller
{
    /// <summary>The kind of slot a placement needs. Each kind has its own prefab and pool queue.</summary>
    public enum SlotKind
    {
        Item,
        Header
    }

    /// <summary>A pooled slot instance with its components looked up once, when it was created.</summary>
    public sealed class SlotHandle
    {
        public RectTransform Rect { get; }
        public SlotKind Kind { get; }

        /// <summary>The slot component (implements ISlot<T> or IExpandableSlot<T>), or null.</summary>
        public ISlot Slot { get; }

        public IExpandableSlot Expandable { get; }
        public IAppearAnimation Appear { get; }

        /// <summary>The slot's GameObject, looked up once (reading Rect.gameObject is a native call).</summary>
        public GameObject GameObject { get; }

        // Where the list last placed the slot: line offset, length along the line, cells in the line and cross size.
        // NaN until placed. Only shown items have a slot, so this costs nothing per item.
        private float placedOffset = float.NaN;
        private float placedLength;
        private int placedCells;
        private float placedCross;

        /// <summary>True when the slot already sits where these values put it (P3: no need to place it again).</summary>
        internal bool IsPlacedAt(float offset, float length, int cells, float cross)
            => placedOffset == offset && placedLength == length && placedCells == cells && placedCross == cross;

        internal void MarkPlaced(float offset, float length, int cells, float cross)
        {
            placedOffset = offset;
            placedLength = length;
            placedCells = cells;
            placedCross = cross;
        }

        /// <summary>Forgets where the slot was placed: it's back in the pool, or about to show another entry.</summary>
        internal void ForgetPlacement()
            => placedOffset = float.NaN;

        /// <summary>True once the list gave the slot its pivot and anchors, which never change for a list.</summary>
        internal bool IsAnchored;

        /// <summary>The sizeDelta the list last gave the slot; NaN until it's placed. Unchanged sizes aren't written again.</summary>
        internal Vector2 PlacedSize = new(float.NaN, float.NaN);

        /// <summary>The size watcher of a slot measured with layout, added the first time it's shown. Null for fixed sizes.</summary>
        internal SlotSizeWatcher SizeWatcher { get; set; }

        public SlotHandle(RectTransform rect, SlotKind kind)
        {
            Rect = rect;
            GameObject = rect.gameObject;
            Kind = kind;
            Slot = rect.GetComponent<ISlot>();
            Expandable = Slot as IExpandableSlot;
            Appear = rect.GetComponent<IAppearAnimation>();
        }
    }
}
