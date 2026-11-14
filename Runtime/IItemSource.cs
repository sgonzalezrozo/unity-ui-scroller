using System.Collections.Generic;

namespace SlotList
{
    public interface IItemSource<T> where T : class
    {
        int ItemCount { get; }
        T GetItem(int index);
        IReadOnlyList<T> GetAllItems();
    }
}
