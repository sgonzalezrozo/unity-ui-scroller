namespace UIScroller
{
    /// <summary>Where ScrollToItem puts the item in the viewport. Nearest moves as little as possible, and not at all if the item is fully visible.</summary>
    public enum ScrollAlign
    {
        Start,
        Center,
        End,
        Nearest
    }
}
