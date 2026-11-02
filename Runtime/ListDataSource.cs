using System;
using System.Collections.Generic;

namespace Tools.RecyclableScroller.Scripts
{
    public class ListDataSource<T> : IRecyclableDataSource<T>, IRecyclableDataSource where T : class
    {
        private readonly List<T> items;

        public int ItemCount => items.Count;

        public ListDataSource(IEnumerable<T> initialItems = null)
        {
            items = new();
            if (initialItems != null)
                items.AddRange(initialItems);
        }

        public T GetItem(int index)
        {
            if (index < 0 || index >= items.Count)
                throw new IndexOutOfRangeException($"Index {index} out of range");

            return items[index];
        }

        public IReadOnlyList<T> GetAllItems()
            => items.AsReadOnly();

        object IRecyclableDataSource.GetItem(int index)
            => GetItem(index);

        IReadOnlyList<object> IRecyclableDataSource.GetAllItems()
        {
            List<object> objectItems = new(items.Count);
            for (int i = 0; i < items.Count; i++)
                objectItems.Add(items[i]);
            return objectItems.AsReadOnly();
        }

        public void AddItem(T item)
        {
            if (item == null)
            {
                UnityEngine.Debug.LogWarning($"[ListDataSource] Attempted to add null item, skipping");
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
                UnityEngine.Debug.LogWarning($"[ListDataSource] Filtered out {nullCount} null items when adding to datasource");
        }

        public void InsertItem(int index, T item)
        {
            if (item == null)
            {
                UnityEngine.Debug.LogWarning($"[ListDataSource] Attempted to insert null item at index {index}, skipping");
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
                UnityEngine.Debug.LogWarning($"[ListDataSource] Filtered out {nullCount} null items when setting datasource");
        }

        public void ReplaceItems(IEnumerable<T> newItems)
            => SetItems(newItems);
    }
}
