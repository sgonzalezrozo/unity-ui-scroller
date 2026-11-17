using UnityEngine;

namespace UIScroller
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

        /// <summary>The first line whose end is past the viewport start. Offsets are computed only as far as needed.</summary>
        public int FindFirstLine(float scrollPosition, Vector2 contentAnchoredPosition, LayoutModel layout)
        {
            ListView<Line> lines = layout.Lines;
            if (lines.Length == 0 || !layout.HasOffsets)
                return 0;

            if (IsAtScrollStart(contentAnchoredPosition))
                return 0;

            return layout.FirstLineEndingAfter(Mathf.Abs(scrollPosition));
        }

        /// <summary>The last line that starts before the viewport end, never before <paramref name="startLineIndex"/>.</summary>
        public int FindLastLine(float scrollPosition, float viewportSize, int startLineIndex, LayoutModel layout)
        {
            ListView<Line> lines = layout.Lines;
            if (lines.Length == 0 || !layout.HasOffsets)
                return 0;

            float viewportEnd = Mathf.Abs(scrollPosition) + viewportSize;

            // First line at or after startLineIndex that starts at or past the viewport end.
            int low = layout.FirstLineStartingAtOrAfter(viewportEnd, Mathf.Max(0, startLineIndex));
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

