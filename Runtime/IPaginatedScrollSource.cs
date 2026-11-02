namespace RecyclableScroller.Runtime
{
    public interface IPaginatedScrollSource<T> : IScrollSource<T> where T : class
    {
        IPaginatedDataProvider<T> GetPaginationProvider();
        void OnPageLoaded(int page, int totalItems);
        void OnPaginationError(string error);
    }
}

