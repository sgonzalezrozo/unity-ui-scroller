using ToolsCore;

namespace Tools.RecyclableScroller.Scripts
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
        IRecyclableScrollRect ScrollRect { get; set; }
        int CellDataIndex { get; set; }
        void CollapseCell();
    }
}
