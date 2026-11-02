using UnityEngine;

namespace Tools.RecyclableScroller.Scripts
{
    public class CellAnimationMarker
    {
        private int nonAnimatingStartLine = -1;
        private int nonAnimatingEndLine = -1;
        private bool hasMarkedOnce;

        public bool HasMarkedOnce => hasMarkedOnce;

        public void ConfigureNonAnimatingRange(int startLine, int endLine)
        {
            nonAnimatingStartLine = startLine;
            nonAnimatingEndLine = endLine;
        }

        public void NotifyFirstVisibleLinesDisplayed()
            => hasMarkedOnce = true;

        public void Mark(RectTransform cell, int lineIndex, CellItem cellItem, int visibleStartLine, int visibleEndLine)
        {
            if (cell == null || cellItem == null)
                return;

            cellItem.CacheComponentReferences(cell);

            bool isVisible = (lineIndex >= visibleStartLine && lineIndex <= visibleEndLine);

            if (IsInNonAnimatingRange(lineIndex))
                isVisible = false;

            if (hasMarkedOnce && isVisible)
                return;

            cellItem.SetAnimationMarker(isVisible);
        }

        private bool IsInNonAnimatingRange(int lineIndex)
            => nonAnimatingStartLine >= 0 && nonAnimatingEndLine >= 0
               && lineIndex >= nonAnimatingStartLine && lineIndex <= nonAnimatingEndLine;
    }
}

