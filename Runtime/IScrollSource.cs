namespace RecyclableScroller.Runtime
{
    public interface IScrollSource<T> where T : class
    {
        IRecyclableDataSource<T> GetDataSource();
    }
}

