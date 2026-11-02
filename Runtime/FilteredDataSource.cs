using System;
using System.Collections.Generic;

namespace RecyclableScroller.Runtime
{
    public class FilteredDataSource<T> : IRecyclableDataSource<T>, IRecyclableDataSource where T : class
    {
        private readonly IRecyclableDataSource<T> sourceDataSource;
        private Func<T, bool> filterPredicate;
        private int cachedFilteredCount = -1;

        public int ItemCount
        {
            get
            {
                if (filterPredicate is null)
                    return sourceDataSource.ItemCount;

                if (cachedFilteredCount >= 0)
                    return cachedFilteredCount;

                cachedFilteredCount = CalculateFilteredCount();
                return cachedFilteredCount;
            }
        }

        private int CalculateFilteredCount()
        {
            int count = 0;
            IReadOnlyList<T> allItems = sourceDataSource.GetAllItems();
            for (int i = 0; i < allItems.Count; i++)
            {
                if (filterPredicate(allItems[i]))
                    count++;
            }
            return count;
        }

        public FilteredDataSource(IRecyclableDataSource<T> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            sourceDataSource = source;
            filterPredicate = null;
        }

        public FilteredDataSource(IRecyclableDataSource<T> source, Func<T, bool> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            sourceDataSource = source;
            filterPredicate = predicate;
        }

        public T GetItem(int index)
        {
            if (index < 0)
                throw new IndexOutOfRangeException($"Index {index} cannot be negative");

            if (filterPredicate is null)
                return sourceDataSource.GetItem(index);

            IReadOnlyList<T> allItems = sourceDataSource.GetAllItems();
            int filteredIndex = 0;

            for (int i = 0; i < allItems.Count; i++)
            {
                T item = allItems[i];
                if (filterPredicate(item))
                {
                    if (filteredIndex == index)
                        return item;
                    filteredIndex++;
                }
            }

            throw new IndexOutOfRangeException($"Filtered index {index} out of range (filtered count: {ItemCount})");
        }

        public IReadOnlyList<T> GetAllItems()
        {
            if (filterPredicate is null)
                return sourceDataSource.GetAllItems();

            List<T> filteredItems = new();
            IReadOnlyList<T> allItems = sourceDataSource.GetAllItems();

            for (int i = 0; i < allItems.Count; i++)
            {
                T item = allItems[i];
                if (filterPredicate(item))
                    filteredItems.Add(item);
            }

            return filteredItems.AsReadOnly();
        }

        public void SetPredicate(Func<T, bool> newPredicate)
        {
            filterPredicate = newPredicate;
            cachedFilteredCount = -1;
        }

        public void ClearPredicate()
        {
            filterPredicate = null;
            cachedFilteredCount = -1;
        }

        object IRecyclableDataSource.GetItem(int index)
            => GetItem(index);

        IReadOnlyList<object> IRecyclableDataSource.GetAllItems()
        {
            IReadOnlyList<T> filteredItems = GetAllItems();
            List<object> objectItems = new(filteredItems.Count);

            for (int i = 0; i < filteredItems.Count; i++)
                objectItems.Add(filteredItems[i]);

            return objectItems.AsReadOnly();
        }
    }
}

