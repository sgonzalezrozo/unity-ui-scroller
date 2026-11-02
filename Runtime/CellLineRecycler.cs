namespace RecyclableScroller.Runtime
{
    public class CellLineRecycler
    {
        private readonly CellLine[] cellLines;
        private readonly CellPool cellPool;

        public CellLineRecycler(CellLine[] cellLines, CellPool cellPool)
        {
            this.cellLines = cellLines;
            this.cellPool = cellPool;
        }

        public void RecycleOutOfViewLines(int startLineIndex, int endLineIndex)
        {
            RecycleLinesBefore(startLineIndex);
            RecycleLinesAfter(endLineIndex);
        }

        private void RecycleLinesBefore(int startLineIndex)
        {
            for (int i = 0; i < startLineIndex; i++)
            {
                if (i >= cellLines.Length)
                    break;

                if (!HasActiveCells(cellLines[i]))
                    continue;

                ReturnLineCells(cellLines[i], i);
            }
        }

        private void RecycleLinesAfter(int endLineIndex)
        {
            for (int i = endLineIndex + 1; i < cellLines.Length; i++)
            {
                if (HasActiveCells(cellLines[i]))
                    ReturnLineCells(cellLines[i], i);
            }
        }

        private void ReturnLineCells(CellLine line, int lineIndex)
        {
            for (int i = 0; i < line.Cells.Length; i++)
            {
                if (line.Cells[i] == null || line.Cells[i].Cell == null)
                    continue;

                cellPool.ReturnCell(line.Cells[i].Cell);
                line.Cells[i].Cell = null;
            }
        }

        private bool HasActiveCells(CellLine line)
        {
            if (line?.Cells == null)
                return false;

            for (int i = 0; i < line.Cells.Length; i++)
            {
                if (line.Cells[i] != null && line.Cells[i].Cell)
                    return true;
            }

            return false;
        }
    }
}

