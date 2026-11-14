using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SlotList
{
    /// <summary>
    /// Paging: when the source is an IPagedSource, the engine asks for the next page once fewer than
    /// PAGING_THRESHOLD_ITEMS items are left after the last visible one, appends pages without a rebuild,
    /// and cancels a load in flight when the source changes or the list is destroyed.
    /// </summary>
    internal sealed partial class SlotListEngine<T>
    {
        private const int PAGING_THRESHOLD_ITEMS = 10;

        private IPagedSource<T> pagedSource;
        private CancellationTokenSource pagingCancellation;

        private void AttachSource(IItemSource<T> source)
        {
            if (ReferenceEquals(source, Source) && (pagedSource != null || source is not IPagedSource<T>))
                return;

            DetachSource();
            Source = source;
            pagedSource = source as IPagedSource<T>;

            if (pagedSource == null)
                return;

            pagedSource.ItemsAppended += OnItemsAppended;
            pagedSource.Cleared += OnSourceCleared;
            pagedSource.LoadFailed += OnPageFailed;
        }

        private void DetachSource()
        {
            if (pagingCancellation != null)
            {
                pagingCancellation.Cancel();
                pagingCancellation.Dispose();
                pagingCancellation = null;
            }

            if (pagedSource == null)
                return;

            pagedSource.ItemsAppended -= OnItemsAppended;
            pagedSource.Cleared -= OnSourceCleared;
            pagedSource.LoadFailed -= OnPageFailed;
            pagedSource = null;
        }

        private void RequestPageIfNeeded()
        {
            if (!isInitialized || pagedSource == null || HasReachedLastPage || !pagedSource.HasMore || pagedSource.IsLoading || !hooks.SupportsPaging())
                return;

            int itemsAfterLastVisible = ItemCount - (VisibleEndIndex + 1);
            if (itemsAfterLastVisible >= PAGING_THRESHOLD_ITEMS)
                return;

            hooks.OnPageRequested();
            _ = LoadNextPage(pagedSource);
        }

        private async Task LoadNextPage(IPagedSource<T> source)
        {
            pagingCancellation ??= new CancellationTokenSource();

            try
            {
                await source.LoadNextAsync(pagingCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (source != pagedSource || !isInitialized || source.HasMore || source.IsLoading || HasReachedLastPage)
                return;

            HasReachedLastPage = true;
            hooks.OnReachedEnd();
        }

        private void OnItemsAppended(int start, int count)
        {
            if (!isInitialized)
                return;

            if (start != layout.Placements.Length)
            {
                Refresh();
                hooks.OnPageLoaded(count);
                return;
            }

            layout.ExtendPlacements(ItemCount);
            BuildLines(start);
            RecalculateContentSize();
            layout.RecomputeOffsets();
            hooks.OnPageLoaded(count);

            visibleRange.ResetState();
            RebuildVisibleItems();
        }

        private void OnSourceCleared()
        {
            HasReachedLastPage = !hooks.SupportsPaging();
            if (!isInitialized)
                return;

            components.Content.anchoredPosition = Vector2.zero;
            Refresh();
        }

        private void OnPageFailed(Exception exception)
            => hooks.OnPageFailed(exception);
    }
}
