using System;
using System.Collections.Generic;

namespace SlotList
{
    /// <summary>
    /// Shows the items of another source that match a filter. The matching positions are worked out once and cached;
    /// call <see cref="Invalidate"/> after the underlying source changes.
    /// </summary>
    public class FilteredSource<T> : IItemSource<T> where T : class
    {
        private readonly IItemSource<T> sourceDataSource;
        private readonly List<int> matchBuffer = new();
        private Func<T, bool> filterPredicate;
        private int[] matches;

        public FilteredSource(IItemSource<T> source)
            : this(source, null)
        {
        }

        public FilteredSource(IItemSource<T> source, Func<T, bool> predicate)
        {
            sourceDataSource = source ?? throw new ArgumentNullException(nameof(source));
            filterPredicate = predicate;
        }

        public int ItemCount
            => filterPredicate is null ? sourceDataSource.ItemCount : Matches().Length;

        public T GetItem(int index)
        {
            if (filterPredicate is null)
                return sourceDataSource.GetItem(index);

            int[] positions = Matches();
            if (index < 0 || index >= positions.Length)
                throw new ArgumentOutOfRangeException(nameof(index), $"Filtered index {index} is out of range (count: {positions.Length}).");

            return sourceDataSource.GetItem(positions[index]);
        }

        public IReadOnlyList<T> GetAllItems()
        {
            if (filterPredicate is null)
                return sourceDataSource.GetAllItems();

            int[] positions = Matches();
            List<T> filteredItems = new(positions.Length);
            for (int i = 0; i < positions.Length; i++)
                filteredItems.Add(sourceDataSource.GetItem(positions[i]));

            return filteredItems.AsReadOnly();
        }

        public void SetPredicate(Func<T, bool> newPredicate)
        {
            filterPredicate = newPredicate;
            matches = null;
        }

        public void ClearPredicate()
        {
            filterPredicate = null;
            matches = null;
        }

        /// <summary>Forgets the cached matches. Call it after items are added, removed or changed in the underlying source.</summary>
        public void Invalidate()
            => matches = null;

        private int[] Matches()
        {
            if (matches != null)
                return matches;

            matchBuffer.Clear();
            int count = sourceDataSource.ItemCount;
            for (int i = 0; i < count; i++)
            {
                if (filterPredicate(sourceDataSource.GetItem(i)))
                    matchBuffer.Add(i);
            }

            matches = matchBuffer.ToArray();
            return matches;
        }
    }
}
