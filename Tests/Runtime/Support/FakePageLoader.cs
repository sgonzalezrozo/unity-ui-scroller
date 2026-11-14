using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// Pages of TestItems with ids 0, 1, 2… Pages can be held open (Hold, then Release) to test loads in flight,
    /// and one page can fail. Records every requested page and the last cancellation token.
    /// </summary>
    public class FakePageLoader : IPageLoader<TestItem>
    {
        public int PageSize = 20;
        public int Total = 60;
        public int FailOnPage = -1;
        public bool Hold;

        public readonly List<int> Requested = new();
        public CancellationToken LastToken;

        private TaskCompletionSource<PageResult<TestItem>> held;
        private int heldPage;

        public Task<PageResult<TestItem>> LoadAsync(int page, CancellationToken cancellation)
        {
            Requested.Add(page);
            LastToken = cancellation;

            if (page == FailOnPage)
                return Task.FromException<PageResult<TestItem>>(new InvalidOperationException($"Page {page} failed"));

            if (!Hold)
                return Task.FromResult(Build(page));

            held = new TaskCompletionSource<PageResult<TestItem>>();
            heldPage = page;
            return held.Task;
        }

        /// <summary>Completes the held page.</summary>
        public void Release()
            => held.SetResult(Build(heldPage));

        private PageResult<TestItem> Build(int page)
        {
            int start = page * PageSize;
            int count = Math.Max(0, Math.Min(PageSize, Total - start));
            return new PageResult<TestItem>(SlotListRig.MakeItems(count, start), start + PageSize < Total);
        }
    }
}
