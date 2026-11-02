using RecyclableScroller.Core.Interfaces;

namespace RecyclableScroller.Core.Directions
{
    /// <summary>
    /// Factory for creating scroll direction implementations.
    /// Returns the appropriate direction based on orientation and reverse flag.
    /// </summary>
    public static class ScrollDirectionFactory
    {
        public static IScrollDirection CreateDirection(bool isVertical, bool reverse)
        {
            if (isVertical)
            {
                return reverse
                    ? new VerticalBottomToTopDirection()
                    : new VerticalTopToBottomDirection();
            }
            else
            {
                return reverse
                    ? new HorizontalRightToLeftDirection()
                    : new HorizontalLeftToRightDirection();
            }
        }
    }
}


