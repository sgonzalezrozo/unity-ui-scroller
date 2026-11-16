using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UIScroller
{
    /// <summary>One page of items, and whether more pages follow.</summary>
    public readonly struct PageResult<T>
    {
        public readonly IReadOnlyList<T> Items;
        public readonly bool HasMore;

        public PageResult(IReadOnlyList<T> items, bool hasMore)
        {
            Items = items ?? Array.Empty<T>();
            HasMore = hasMore;
        }
    }

    /// <summary>
    /// Loads pages for a PagedSource. Pages are numbered from 0. Throw to report a failure;
    /// honour the cancellation token when the load can be cancelled.
    /// </summary>
    public interface IPageLoader<T> where T : class
    {
        Task<PageResult<T>> LoadAsync(int page, CancellationToken cancellation);
    }

    /// <summary>A source that grows one page at a time. A scroller asks for the next page when the user gets near the end.</summary>
    public interface IPagedSource<T> : IItemSource<T> where T : class
    {
        bool HasMore { get; }
        bool IsLoading { get; }

        /// <summary>Loads the next page. Does nothing while a page is loading or when there are no more pages.</summary>
        Task LoadNextAsync(CancellationToken cancellation = default);

        /// <summary>Removes every item and starts again from page 0. A page that is loading is discarded.</summary>
        void Reset();

        /// <summary>Raised after a page is added: the index of its first item and how many items it added.</summary>
        event Action<int, int> ItemsAppended;

        event Action Cleared;
        event Action<Exception> LoadFailed;
    }
}
