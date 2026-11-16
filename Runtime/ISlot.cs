using ToolsCore;

namespace UIScroller
{
    public interface ISlot
    {
        float DefaultExtent { get; }
    }

    public interface ISlot<T> : ISlot, IInstantiable where T : class
    {
        T Data { get; set; }
    }

    public interface IExpandableSlot<T> : IExpandableSlot, IInstantiable where T : class
    {
        T Data { get; set; }
        bool IsExpanded { get; set; }
    }

    public interface IExpandableSlot : ISlot
    {
        IScroller Owner { get; set; }
        int ItemIndex { get; set; }
        void OnCollapsed();
    }
}
