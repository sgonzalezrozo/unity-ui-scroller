using RecyclableScroller.Core.Interfaces;
using UnityEngine;

namespace RecyclableScroller.Core.Pooling
{
    public class CellItem
    {
        public RectTransform Cell { get; set; }
        public float Size { get; private set; }
        public bool HasCachedSize { get; private set; }
        public bool WasExpanded { get; set; }
        public int DataIndex { get; set; }

        private object cachedAnimationMarker;
        private object cachedExpandableCell;

        public CellItem(int dataIndex)
        {
            DataIndex = dataIndex;
        }

        public void CacheSize(float size)
        {
            Size = size;
            HasCachedSize = true;
        }

        public void CacheComponentReferences(RectTransform cell)
        {
            if (cell == null || cell.gameObject == null)
                return;

            GameObject cellGO = cell.gameObject;
            cachedAnimationMarker = cellGO.GetComponent<IAnimationMarker>();
            cachedExpandableCell = cellGO.GetComponent<IExpandableRecyclableCellBase>();
        }

        public void SetAnimationMarker(bool shouldAnimate)
        {
            if (cachedAnimationMarker != null && cachedAnimationMarker is IAnimationMarker marker)
                marker.ShouldAnimate = shouldAnimate;
        }

        public object GetExpandableCell()
            => cachedExpandableCell;

        public void ClearComponentCache()
        {
            cachedAnimationMarker = null;
            cachedExpandableCell = null;
        }

        public override string ToString()
            => $"DataIndex:{DataIndex} Size:{Size}";
    }
}

