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

    /// <summary>
    /// Optional, on a slot: told where it sits in the viewport every time the list moves, for carousel effects such as
    /// scaling or fading the centred card. 0 is the viewport's centre, -0.5 its start edge and +0.5 its end edge,
    /// measured from the slot's centre along the scroll axis; past ±0.5 the slot is partly off screen.
    /// </summary>
    public interface IViewportPositionListener
    {
        void OnViewportPosition(float position);
    }

    public interface IExpandableSlot : ISlot
    {
        IScroller Owner { get; set; }
        int ItemIndex { get; set; }
        void OnCollapsed();
    }
}
