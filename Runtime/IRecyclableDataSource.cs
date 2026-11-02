using System.Collections.Generic;

namespace Tools.RecyclableScroller.Scripts
{
    public interface IRecyclableDataSource
    {
        int ItemCount { get; }
        object GetItem(int index);
        IReadOnlyList<object> GetAllItems();
    }

    public interface IRecyclableDataSource<T> where T : class
    {
        int ItemCount { get; }
        T GetItem(int index);
        IReadOnlyList<T> GetAllItems();
    }
}
