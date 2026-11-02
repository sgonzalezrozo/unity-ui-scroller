using ToolsCore;

namespace RecyclableScroller.Core.Interfaces
{
    public interface IRecyclableCell
    {
        float DefaultCellSize { get; }
    }

    public interface IRecyclableCell<T> : IRecyclableCell, IInstantiable where T : class
    {
        T Data { get; set; }
    }

    public interface IExpandableRecyclableCell<T> : IExpandableRecyclableCellBase, IInstantiable where T : class
    {
        T Data { get; set; }
        bool IsExpanded { get; set; }
    }

    public interface IExpandableRecyclableCellBase : IRecyclableCell
    {
        object ScrollRect { get; set; }
        int CellDataIndex { get; set; }
        void CollapseCell();
    }
}
