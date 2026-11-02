namespace Tools.RecyclableScroller.Scripts
{
    public interface IScrollSource<T> where T : class
    {
        IRecyclableDataSource<T> GetDataSource();
    }
}

