namespace UIScroller
{
    /// <summary>
    /// Factory for creating scroll direction implementations.
    /// Returns the appropriate direction based on orientation and reverse flag.
    /// </summary>
    public static class AxisLayouts
    {
        public static IAxisLayout CreateDirection(bool isVertical, bool reverse)
        {
            if (isVertical)
            {
                return reverse
                    ? new BottomToTopLayout()
                    : new TopToBottomLayout();
            }
            else
            {
                return reverse
                    ? new RightToLeftLayout()
                    : new LeftToRightLayout();
            }
        }
    }
}


