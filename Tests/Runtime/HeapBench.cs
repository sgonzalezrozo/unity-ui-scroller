using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// Heap bench for the performance plan (P5, candidate C7). [Explicit]: run it with -testFilter HeapBench.
    /// With the items alive in both cases, it measures what the list adds to the managed heap (bytes per item) and how
    /// much longer a full GC collection takes while the list is alive (the GC walks every Placement and Line object).
    /// </summary>
    [Explicit]
    public class HeapBench
    {
        public enum Case
        {
            List,
            Grid4,
            Bento
        }

        private const int COLLECTIONS = 15;

        [UnityTest]
        public IEnumerator Bench([Values(Case.List, Case.Grid4, Case.Bento)] Case benchCase, [Values(100_000, 1_000_000)] int count)
        {
            List<TestItem> items = ScrollerRig.MakeItems(count);
            ListSource<TestItem> source = new(items);
            yield return null;

            long heapWithoutList = SettledHeap();
            double collectWithoutList = MedianCollectMs();

            RigOptions options = new() { QuietSlots = true };
            if (benchCase == Case.Grid4)
                options.SlotsPerLine = 4;
            if (benchCase == Case.Bento)
            {
                options.Bento = true;
                options.BentoLayout = BentoLayout.FromSpans(4, new[] { new BentoSpan(2, 2), BentoSpan.One }, repeatFrom: 1);
            }

            ScrollerRig rig = ScrollerRig.Create(options);
            yield return rig.Initialize(source);
            yield return ScrollerRig.Frames(3);

            long heapWithList = SettledHeap();
            double collectWithList = MedianCollectMs();
            int lines = rig.List.Work == null ? -1 : LineCount(rig);

            double bytesPerItem = (heapWithList - heapWithoutList) / (double)count;
            Debug.Log($"HEAPBENCH {benchCase} {count}: list adds {(heapWithList - heapWithoutList) / 1048576.0:F1} MB ({bytesPerItem:F0} B/item, {lines} lines); " +
                      $"full GC {collectWithoutList:F2} ms without the list, {collectWithList:F2} ms with it (+{collectWithList - collectWithoutList:F2} ms)");

            rig.Dispose();
            GC.KeepAlive(items);
            Assert.Pass();
        }

        private static int LineCount(ScrollerRig rig)
        {
            System.Reflection.FieldInfo engineField = typeof(ScrollerBase).GetField("engine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            object engine = engineField.GetValue(rig.List);
            System.Reflection.FieldInfo layoutField = engine.GetType().GetField("layout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            LayoutModel layout = (LayoutModel)layoutField.GetValue(engine);
            return layout.Lines.Length;
        }

        private static long SettledHeap()
        {
            for (int i = 0; i < 3; i++)
                GC.Collect();
            return Profiler.GetMonoUsedSizeLong();
        }

        private static double MedianCollectMs()
        {
            GC.Collect();
            List<double> times = new();
            Stopwatch stopwatch = new();
            for (int i = 0; i < COLLECTIONS; i++)
            {
                stopwatch.Restart();
                GC.Collect();
                stopwatch.Stop();
                times.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            times.Sort();
            return times[times.Count / 2];
        }
    }
}
