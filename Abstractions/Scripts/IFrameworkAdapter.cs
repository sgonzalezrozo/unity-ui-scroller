namespace Tools.RecyclableScroller.Abstractions.Scripts
{
    /// <summary>
    /// Container for all framework-specific implementations.
    /// Used to inject all required providers at initialization.
    /// Allows RecyclableScrollerCore to be completely UI-framework agnostic.
    /// </summary>
    public interface IFrameworkAdapter
    {
        IViewportProvider ViewportProvider { get; }
        ICellContainer CellContainer { get; }
        IInputHandler InputHandler { get; }
        IAsyncScheduler AsyncScheduler { get; }
        IRebindTracker RebindTracker { get; }
    }
}

