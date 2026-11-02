using UnityEngine;

namespace Tools.RecyclableScroller.Scripts
{
    public class VisibleLineFinder
    {
        private readonly bool isVertical;
        private readonly bool reverse;

        public VisibleLineFinder(bool isVertical, bool reverse)
        {
            this.isVertical = isVertical;
            this.reverse = reverse;
        }

        public int FindStartLine(float scrollPosition, Vector2 contentAnchoredPosition, CellLine[] cellLines, LinePositionCache positions)
        {
            if (cellLines == null || cellLines.Length == 0 || !positions.HasPositions())
                return 0;

            if (IsAtScrollStart(contentAnchoredPosition))
                return 0;

            float viewportStart = Mathf.Abs(scrollPosition);

            for (int i = 0; i < cellLines.Length; i++)
            {
                float lineStart = positions.GetPosition(i);
                float lineEnd = lineStart + cellLines[i].Size;

                if (lineEnd > viewportStart)
                    return i;
            }

            return cellLines.Length - 1;
        }

        public int FindEndLine(float scrollPosition, float viewportSize, int startLineIndex, CellLine[] cellLines, LinePositionCache positions)
        {
            if (cellLines == null || cellLines.Length == 0 || !positions.HasPositions())
                return 0;

            float viewportEnd = Mathf.Abs(scrollPosition) + viewportSize;

            for (int i = startLineIndex; i < cellLines.Length; i++)
            {
                float lineStart = positions.GetPosition(i);

                if (lineStart < viewportEnd)
                    continue;

                return Mathf.Max(startLineIndex, i - 1);
            }

            return cellLines.Length - 1;
        }

        private bool IsAtScrollStart(Vector2 contentAnchoredPosition)
        {
            if (reverse)
            {
                if (isVertical && contentAnchoredPosition.y >= 0)
                    return true;
                if (!isVertical && contentAnchoredPosition.x <= 0)
                    return true;
                return false;
            }

            if (isVertical && contentAnchoredPosition.y <= 0)
                return true;
            if (!isVertical && contentAnchoredPosition.x >= 0)
                return true;

            return false;
        }
    }
}

