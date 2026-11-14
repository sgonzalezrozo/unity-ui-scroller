using UnityEngine;

namespace SlotList
{
    public class VisibleRangeFinder
    {
        private readonly bool isVertical;
        private readonly bool reverse;

        public VisibleRangeFinder(bool isVertical, bool reverse)
        {
            this.isVertical = isVertical;
            this.reverse = reverse;
        }

        /// <summary>The first line whose end is past the viewport start. Binary search over the line offsets.</summary>
        public int FindFirstLine(float scrollPosition, Vector2 contentAnchoredPosition, LayoutModel layout)
        {
            ListView<Line> lines = layout.Lines;
            if (lines.Length == 0 || !layout.HasOffsets)
                return 0;

            if (IsAtScrollStart(contentAnchoredPosition))
                return 0;

            float viewportStart = Mathf.Abs(scrollPosition);
            int low = 0;
            int high = lines.Length;

            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (layout.OffsetOf(middle) + lines[middle].Size > viewportStart)
                    high = middle;
                else
                    low = middle + 1;
            }

            return low < lines.Length ? low : lines.Length - 1;
        }

        /// <summary>The last line that starts before the viewport end, never before <paramref name="startLineIndex"/>.</summary>
        public int FindLastLine(float scrollPosition, float viewportSize, int startLineIndex, LayoutModel layout)
        {
            ListView<Line> lines = layout.Lines;
            if (lines.Length == 0 || !layout.HasOffsets)
                return 0;

            float viewportEnd = Mathf.Abs(scrollPosition) + viewportSize;
            int low = Mathf.Max(0, startLineIndex);
            int high = lines.Length;

            // First line at or after startLineIndex that starts at or past the viewport end.
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (layout.OffsetOf(middle) >= viewportEnd)
                    high = middle;
                else
                    low = middle + 1;
            }

            return low < lines.Length ? Mathf.Max(startLineIndex, low - 1) : lines.Length - 1;
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

