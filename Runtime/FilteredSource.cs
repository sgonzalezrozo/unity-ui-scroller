using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UIScroller
{
    /// <summary>
    /// Shows the items of another source that match a filter. The matching positions are worked out once and cached;
    /// call <see cref="Invalidate"/> after the underlying source changes.
    /// </summary>
    public class FilteredSource<T> : IItemSource<T> where T : class
    {
        private readonly IItemSource<T> sourceDataSource;
        private readonly List<int> matches = new();
        private readonly List<T> filteredItems = new();
        private readonly ReadOnlyCollection<T> filteredView;
        private Func<T, bool> filterPredicate;
        private bool matchesValid;
        private bool filteredItemsValid;

        public FilteredSource(IItemSource<T> source)
            : this(source, null)
        {
        }

        public FilteredSource(IItemSource<T> source, Func<T, bool> predicate)
        {
            sourceDataSource = source ?? throw new ArgumentNullException(nameof(source));
            filterPredicate = predicate;
            filteredView = filteredItems.AsReadOnly();
        }

        public int ItemCount
            => filterPredicate is null ? sourceDataSource.ItemCount : Matches().Count;

        public T GetItem(int index)
        {
            if (filterPredicate is null)
                return sourceDataSource.GetItem(index);

            List<int> positions = Matches();
            if (index < 0 || index >= positions.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Filtered index {index} is out of range (count: {positions.Count}).");

            return sourceDataSource.GetItem(positions[index]);
        }

        /// <summary>
        /// The matching items, as a read-only view that this source reuses: it allocates nothing, and the next call after
        /// a change (SetPredicate, ClearPredicate, Invalidate) updates the same view.
        /// </summary>
        public IReadOnlyList<T> GetAllItems()
        {
            if (filterPredicate is null)
                return sourceDataSource.GetAllItems();

            if (!filteredItemsValid)
            {
                List<int> positions = Matches();
                filteredItems.Clear();
                for (int i = 0; i < positions.Count; i++)
                    filteredItems.Add(sourceDataSource.GetItem(positions[i]));
                filteredItemsValid = true;
            }

            return filteredView;
        }

        public void SetPredicate(Func<T, bool> newPredicate)
        {
            filterPredicate = newPredicate;
            Invalidate();
        }

        public void ClearPredicate()
        {
            filterPredicate = null;
            Invalidate();
        }

        /// <summary>Forgets the cached matches. Call it after items are added, removed or changed in the underlying source.</summary>
        public void Invalidate()
        {
            matchesValid = false;
            filteredItemsValid = false;
        }

        private List<int> Matches()
        {
            if (matchesValid)
                return matches;

            matches.Clear();
            int count = sourceDataSource.ItemCount;
            for (int i = 0; i < count; i++)
            {
                if (filterPredicate(sourceDataSource.GetItem(i)))
                    matches.Add(i);
            }

            matchesValid = true;
            return matches;
        }
    }
}
