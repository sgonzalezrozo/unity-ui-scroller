using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UIScroller
{
    public class ListSource<T> : IItemSource<T> where T : class
    {
        private readonly List<T> items;
        private readonly ReadOnlyCollection<T> readOnlyItems;

        public int ItemCount => items.Count;

        public ListSource(IEnumerable<T> initialItems = null)
        {
            items = new();
            readOnlyItems = items.AsReadOnly();
            if (initialItems != null)
                items.AddRange(initialItems);
        }

        public T GetItem(int index)
        {
            if (index < 0 || index >= items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range (count: {items.Count}).");

            return items[index];
        }

        /// <summary>A read-only view of the items (the same object every call, so it allocates nothing).</summary>
        public IReadOnlyList<T> GetAllItems()
            => readOnlyItems;

        public void AddItem(T item)
        {
            if (item == null)
            {
                UnityEngine.Debug.LogWarning($"[ListSource] Attempted to add null item, skipping");
                return;
            }
            items.Add(item);
        }

        public void AddItems(IEnumerable<T> newItems)
        {
            if (newItems == null)
                return;

            int nullCount = 0;
            foreach (T item in newItems)
            {
                if (item == null)
                {
                    nullCount++;
                    continue;
                }
                items.Add(item);
            }

            if (nullCount > 0)
                UnityEngine.Debug.LogWarning($"[ListSource] Filtered out {nullCount} null items when adding to datasource");
        }

        public void InsertItem(int index, T item)
        {
            if (item == null)
            {
                UnityEngine.Debug.LogWarning($"[ListSource] Attempted to insert null item at index {index}, skipping");
                return;
            }
            items.Insert(index, item);
        }

        public void RemoveItem(T item)
            => items.Remove(item);

        public void RemoveItemAt(int index)
        {
            if (index >= 0 && index < items.Count)
                items.RemoveAt(index);
        }

        public void Clear()
            => items.Clear();

        public void SetItems(IEnumerable<T> newItems)
        {
            items.Clear();

            if (newItems == null)
                return;

            int nullCount = 0;
            foreach (T item in newItems)
            {
                if (item == null)
                {
                    nullCount++;
                    continue;
                }
                items.Add(item);
            }

            if (nullCount > 0)
                UnityEngine.Debug.LogWarning($"[ListSource] Filtered out {nullCount} null items when setting datasource");
        }

        public void ReplaceItems(IEnumerable<T> newItems)
            => SetItems(newItems);
    }
}
