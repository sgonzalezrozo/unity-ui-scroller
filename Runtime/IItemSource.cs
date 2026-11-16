using System.Collections.Generic;

namespace UIScroller
{
    public interface IItemSource<T> where T : class
    {
        int ItemCount { get; }
        T GetItem(int index);
        IReadOnlyList<T> GetAllItems();
    }
}
