using UnityEngine;

namespace RecyclableScroller.Runtime
{
    public interface ICellSizeCalculator<T> where T : class
    {
        float CalculateCellSize(T item, int index);

        float? CalculateExpandedSize(T item, int index)
            => null;

        void SetItemPrefab(GameObject prefab);

        void SetContainerWidth(float width)
        {
        }
    }
}

