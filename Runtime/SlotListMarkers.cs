using Unity.Profiling;

namespace SlotList
{
    /// <summary>Profiler markers for the list's work, shown as "Slot List.*" in the Profiler. They cost nothing in a release build.</summary>
    internal static class SlotListMarkers
    {
        public static readonly ProfilerMarker OnScroll = new("Slot List.OnScroll");
        public static readonly ProfilerMarker UpdateVisible = new("Slot List.UpdateVisible");
        public static readonly ProfilerMarker Recycle = new("Slot List.Recycle");
        public static readonly ProfilerMarker Acquire = new("Slot List.Acquire");
        public static readonly ProfilerMarker Bind = new("Slot List.Bind");
        public static readonly ProfilerMarker Measure = new("Slot List.Measure");
        public static readonly ProfilerMarker ApplySizes = new("Slot List.ApplySizes");
        public static readonly ProfilerMarker Rebuild = new("Slot List.Rebuild");
    }
}
