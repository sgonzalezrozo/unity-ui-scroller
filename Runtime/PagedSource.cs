using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SlotList
{
    /// <summary>
    /// A source that loads its items page by page through an <see cref="IPageLoader{T}"/>. Pages only load when
    /// <see cref="LoadNextAsync"/> is called; a SlotList does that by itself near the end of the list.
    /// </summary>
    public sealed class PagedSource<T> : IPagedSource<T> where T : class
    {
        private readonly List<T> items = new();
        private readonly IPageLoader<T> loader;

        private int lastLoadedPage = -1;
        private int resetVersion;

        public int ItemCount => items.Count;
        public bool HasMore { get; private set; } = true;
        public bool IsLoading { get; private set; }

        /// <summary>The last page loaded, or -1.</summary>
        public int CurrentPage => lastLoadedPage;

        public event Action<int, int> ItemsAppended;
        public event Action Cleared;
        public event Action<Exception> LoadFailed;

        public PagedSource(IPageLoader<T> loader)
        {
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public T GetItem(int index)
        {
            if (index < 0 || index >= items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range (count: {items.Count}).");

            return items[index];
        }

        public IReadOnlyList<T> GetAllItems()
            => items.AsReadOnly();

        public async Task LoadNextAsync(CancellationToken cancellation = default)
        {
            if (IsLoading || !HasMore)
                return;

            IsLoading = true;
            int version = resetVersion;
            int page = lastLoadedPage + 1;
            PageResult<T> result;

            try
            {
                result = await loader.LoadAsync(page, cancellation);
            }
            catch (OperationCanceledException)
            {
                FinishIfCurrent(version);
                return;
            }
            catch (Exception exception)
            {
                if (FinishIfCurrent(version))
                    LoadFailed?.Invoke(exception);
                return;
            }

            if (!FinishIfCurrent(version) || cancellation.IsCancellationRequested)
                return;

            lastLoadedPage = page;
            HasMore = result.HasMore && result.Items.Count > 0;

            if (result.Items.Count == 0)
                return;

            int start = items.Count;
            items.AddRange(result.Items);
            ItemsAppended?.Invoke(start, result.Items.Count);
        }

        public void Reset()
        {
            resetVersion++;
            items.Clear();
            lastLoadedPage = -1;
            HasMore = true;
            IsLoading = false;
            Cleared?.Invoke();
        }

        /// <summary>Ends the load if no Reset happened since it started. False if the load is stale.</summary>
        private bool FinishIfCurrent(int version)
        {
            if (version != resetVersion)
                return false;

            IsLoading = false;
            return true;
        }
    }
}
