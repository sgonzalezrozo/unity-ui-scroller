namespace UIScroller
{
    public interface IScroller
    {
        bool IsInitialized { get; }
        bool IsExpanded(int index);
        void Collapse(int index);
        void Expand(int index, float newSize = -1);
        void SetExpandedSize(int index, float newSize = -1);
    }
}

