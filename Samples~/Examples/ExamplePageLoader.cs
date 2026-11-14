using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SlotList.Samples
{
    /// <summary>Makes up pages of items after a short delay, as a server would: 5 pages of 30, the last with 3.</summary>
    public class ExamplePageLoader : IPageLoader<ExampleItem>
    {
        private const int PAGE_SIZE = 30;
        private const int PAGE_COUNT = 5;
        private const int LAST_PAGE_SIZE = 3;
        private const int DELAY_MILLISECONDS = 600;

        public async Task<PageResult<ExampleItem>> LoadAsync(int page, CancellationToken cancellation)
        {
            await Task.Delay(DELAY_MILLISECONDS, cancellation);

            bool isLast = page >= PAGE_COUNT - 1;
            int count = isLast ? LAST_PAGE_SIZE : PAGE_SIZE;
            List<ExampleItem> items = new(count);
            for (int i = 0; i < count; i++)
                items.Add(new ExampleItem(page * PAGE_SIZE + i));

            return new PageResult<ExampleItem>(items, !isLast);
        }
    }
}
