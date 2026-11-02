using Tools.RecyclableScroller.Core.Scripts.Pooling;

namespace Tools.RecyclableScroller.Core.Scripts.Algorithms
{
    public class LinePositionCache
    {
        private float[] cachedPositions;
        private bool isDirty = true;

        public bool IsDirty => isDirty;

        public void Rebuild(CellLine[] cellLines, float paddingStart, float lineSpacing)
        {
            if (cellLines == null || cellLines.Length == 0)
            {
                isDirty = false;
                return;
            }

            if (cachedPositions == null || cachedPositions.Length != cellLines.Length)
                cachedPositions = new float[cellLines.Length];

            float position = paddingStart;

            for (int i = 0; i < cellLines.Length; i++)
            {
                cachedPositions[i] = position;
                position += cellLines[i].Size + lineSpacing;
            }

            isDirty = false;
        }

        public float GetPosition(int lineIndex)
        {
            if (cachedPositions == null || lineIndex < 0 || lineIndex >= cachedPositions.Length)
                return 0f;

            return cachedPositions[lineIndex];
        }

        public bool HasPositions()
            => cachedPositions != null && cachedPositions.Length > 0;

        public void MarkDirty()
            => isDirty = true;
    }
}

