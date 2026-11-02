namespace Tools.RecyclableScroller.Scripts
{
    public interface IRecyclableScrollRect
    {
        bool IsInitialized { get; }
        bool IsIndexExpanded(int index);
        void CellWantToCollapse(int index);
        void ExpandCell(int index, float newSize = -1);
        void UpdateExpandedCellSize(int index, float newSize = -1);
    }
}

