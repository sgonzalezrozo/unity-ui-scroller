using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Tools.RecyclableScroller.Scripts
{
    public class PaginatedDataSource<T> : IRecyclableDataSource<T>, IRecyclableDataSource where T : class
    {
        private const int PRELOAD_THRESHOLD = 10;

        private readonly List<T> cachedItems;
        private readonly IPaginatedDataProvider<T> provider;

        private int currentPage = -1;
        private bool hasMorePages = true;
        private bool isLoading;

        public int ItemCount => cachedItems.Count;
        public bool IsLoading => isLoading;
        public bool HasMorePages => hasMorePages;
        public int CurrentPage => currentPage;

        public Action OnDataChanged;
        public Action OnLoadingStarted;
        public Action OnLoadingFinished;

        public PaginatedDataSource(IPaginatedDataProvider<T> provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            cachedItems = new();
        }

        public T GetItem(int index)
        {
            if (index < 0 || index >= cachedItems.Count)
            {
                Debug.LogWarning($"[PaginatedDataSource] Index {index} out of range (Count: {cachedItems.Count})");
                return null;
            }

            if (ShouldPreloadNextPage(index))
                _ = LoadNextPageAsync();

            return cachedItems[index];
        }

        public IReadOnlyList<T> GetAllItems()
            => cachedItems.AsReadOnly();

        object IRecyclableDataSource.GetItem(int index)
            => GetItem(index);

        IReadOnlyList<object> IRecyclableDataSource.GetAllItems()
        {
            List<object> objectItems = new(cachedItems.Count);
            for (int i = 0; i < cachedItems.Count; i++)
                objectItems.Add(cachedItems[i]);
            return objectItems.AsReadOnly();
        }

        public async Task<bool> LoadNextPageAsync()
        {
            if (isLoading)
                return false;

            if (!hasMorePages)
                return false;

            isLoading = true;
            OnLoadingStarted?.Invoke();

            try
            {
                int nextPage = currentPage + 1;

                PaginationResult<T> result = await provider.LoadPageAsync(nextPage);

                if (!result.Success)
                    return false;

                if (result.Items == null || result.Items.Count == 0)
                {
                    currentPage = nextPage;
                    hasMorePages = false;
                    return false;
                }

                cachedItems.AddRange(result.Items);
                currentPage = nextPage;
                hasMorePages = result.HasMore;


                OnDataChanged?.Invoke();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PaginatedDataSource] Exception loading page {currentPage}: {ex}");
                return false;
            }
            finally
            {
                isLoading = false;
                OnLoadingFinished?.Invoke();
            }
        }

        public void Reset()
        {
            cachedItems.Clear();
            currentPage = 0;
            hasMorePages = true;
            isLoading = false;
            OnDataChanged?.Invoke();
        }

        private bool ShouldPreloadNextPage(int accessedIndex)
        {
            if (!hasMorePages || isLoading)
                return false;

            int remainingItems = cachedItems.Count - accessedIndex;
            return remainingItems <= PRELOAD_THRESHOLD;
        }
    }
}

