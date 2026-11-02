using UnityEngine;

namespace Tools.RecyclableScroller.Scripts
{
    public abstract class BaseCellSizeCalculator<T> : ICellSizeCalculator<T> where T : class
    {
        protected float containerWidth;

        public abstract float CalculateCellSize(T item, int index);

        public virtual float? CalculateExpandedSize(T item, int index)
            => null;

        public virtual void SetItemPrefab(GameObject prefab)
        {
        }

        public void SetContainerWidth(float width)
            => containerWidth = width;
    }
}
