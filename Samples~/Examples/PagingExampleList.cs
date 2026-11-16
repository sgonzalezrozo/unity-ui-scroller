namespace UIScroller.Samples
{
    /// <summary>The Paging example's list. Pull to refresh starts the paged source again from page 0.</summary>
    public class PagingExampleList : Scroller<ExampleItem>
    {
        protected override void OnPullToRefreshDetected()
        {
            if (DataSource is IPagedSource<ExampleItem> paged)
                paged.Reset();
        }
    }
}
