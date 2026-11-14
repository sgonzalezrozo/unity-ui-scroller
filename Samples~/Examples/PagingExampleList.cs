namespace SlotList.Samples
{
    /// <summary>The Paging example's list. Pull to refresh starts the paged source again from page 0.</summary>
    public class PagingExampleList : SlotList<ExampleItem>
    {
        protected override void OnPullToRefreshDetected()
        {
            if (DataSource is IPagedSource<ExampleItem> paged)
                paged.Reset();
        }
    }
}
