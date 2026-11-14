namespace SlotList
{
    public interface IItemSourceProvider<T> where T : class
    {
        IItemSource<T> GetSource();
    }
}

