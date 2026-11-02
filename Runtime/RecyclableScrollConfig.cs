namespace Tools.RecyclableScroller.Scripts
{
    public class RecyclableScrollConfig
    {
        public float CellSpacing { get; set; }
        public float PaddingStart { get; set; }
        public float PaddingEnd { get; set; }
        public bool IsVertical { get; set; } = true;
        public bool IsReverse { get; set; }
        public uint GridCount { get; set; } = 1;
        public bool AllowMultipleExpandedCells { get; set; }
        public System.Action OnPullRefresh { get; set; }
        public System.Action OnPullLoadMore { get; set; }

        public RecyclableScrollConfig()
        {
            CellSpacing = 0f;
            PaddingStart = 0f;
            PaddingEnd = 0f;
            GridCount = 1;
            AllowMultipleExpandedCells = false;
        }
    }
}

