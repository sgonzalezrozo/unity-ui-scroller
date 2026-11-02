using UnityEngine;

namespace Tools.RecyclableScroller.Core.Scripts.Interfaces
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

