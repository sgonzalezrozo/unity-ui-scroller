using Unity.Profiling;

namespace UIScroller
{
    /// <summary>Profiler markers for the list's work, shown as "UI Scroller.*" in the Profiler. They cost nothing in a release build.</summary>
    internal static class ScrollerMarkers
    {
        public static readonly ProfilerMarker OnScroll = new("UI Scroller.OnScroll");
        public static readonly ProfilerMarker UpdateVisible = new("UI Scroller.UpdateVisible");
        public static readonly ProfilerMarker Recycle = new("UI Scroller.Recycle");
        public static readonly ProfilerMarker Acquire = new("UI Scroller.Acquire");
        public static readonly ProfilerMarker Bind = new("UI Scroller.Bind");
        public static readonly ProfilerMarker Measure = new("UI Scroller.Measure");
        public static readonly ProfilerMarker ApplySizes = new("UI Scroller.ApplySizes");
        public static readonly ProfilerMarker Rebuild = new("UI Scroller.Rebuild");
        public static readonly ProfilerMarker Initialize = new("UI Scroller.Initialize");
        public static readonly ProfilerMarker Instantiate = new("UI Scroller.Instantiate");
    }
}
