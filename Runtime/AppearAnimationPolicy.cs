namespace UIScroller
{
    public class AppearAnimationPolicy
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

        public void Mark(SlotHandle handle, int lineIndex, int visibleStartLine, int visibleEndLine)
        {
            if (handle == null)
                return;

            bool isVisible = (lineIndex >= visibleStartLine && lineIndex <= visibleEndLine);

            if (IsInNonAnimatingRange(lineIndex))
                isVisible = false;

            if (hasMarkedOnce && isVisible)
                return;

            if (handle.Appear != null)
                handle.Appear.ShouldPlay = isVisible;
        }

        private bool IsInNonAnimatingRange(int lineIndex)
            => nonAnimatingStartLine >= 0 && nonAnimatingEndLine >= 0
               && lineIndex >= nonAnimatingStartLine && lineIndex <= nonAnimatingEndLine;
    }
}

