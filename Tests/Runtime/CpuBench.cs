using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// CPU bench for the performance plan (P4). Not a pass or fail test: it's [Explicit], so verify skips it; run it with
    /// -testFilter CpuBench. Each case scrolls 3,000 steps of 37 px forward and back, synchronously, and times the list's
    /// own work (the scroll handler) apart from Unity's canvas update (Canvas.ForceUpdateCanvases after each step).
    /// Medians of 9 runs after a warm-up, in microseconds per scroll event. Slots have an Image, so the canvas has
    /// graphics to rebuild.
    /// </summary>
    [Explicit]
    public class CpuBench
    {
        public enum Case
        {
            List,
            Grid4,
            BentoPatterns,
            Sections
        }

        private const int COUNT = 100_000;
        private const int STEPS = 3000;
        private const float STEP = 37f;
        private const int RUNS = 9;

        [UnityTest]
        public IEnumerator Bench([Values(Case.List, Case.Grid4, Case.BentoPatterns, Case.Sections)] Case benchCase)
        {
            RigOptions options = new() { QuietSlots = true, SlotImage = true, Sectioned = benchCase == Case.Sections };
            if (benchCase == Case.Grid4)
                options.SlotsPerLine = 4;
            if (benchCase == Case.BentoPatterns)
            {
                options.Bento = true;
                options.BentoLayout = BentoLayout.FromPatterns(4, new[]
                {
                    new BentoPattern(2, new[] { new BentoTile(0, 0, 2, 2), new BentoTile(2, 0, 2, 1), new BentoTile(2, 1, 1, 1), new BentoTile(3, 1, 1, 1) }),
                    new BentoPattern(1, new[] { new BentoTile(0, 0, 1, 1), new BentoTile(1, 0, 1, 1), new BentoTile(2, 0, 1, 1), new BentoTile(3, 0, 1, 1) })
                }, repeatFrom: 1);
            }

            SlotListRig rig = SlotListRig.Create(options);
            if (benchCase == Case.Sections)
            {
                SectionedSource<TestItem, TestHeader> sections = new();
                for (int s = 0; s < 100; s++)
                    sections.AddSection(new TestHeader("S" + s), new ListSource<TestItem>(SlotListRig.MakeItems(COUNT / 100, 1000 * s)));
                yield return rig.InitializeSections(sections);
            }
            else
            {
                yield return rig.Initialize(COUNT);
            }

            yield return SlotListRig.Frames(3);

            Run(rig, out _, out _);
            List<double> listTimes = new();
            List<double> canvasTimes = new();
            for (int r = 0; r < RUNS; r++)
            {
                Run(rig, out double listUs, out double canvasUs);
                listTimes.Add(listUs);
                canvasTimes.Add(canvasUs);
                yield return null;
            }

            Debug.Log($"CPUBENCH {benchCase}: list {Median(listTimes):F3} us/event (min {Min(listTimes):F3}), canvas {Median(canvasTimes):F3} us/event (min {Min(canvasTimes):F3})");
            rig.Dispose();
            Assert.Pass();
        }

        private static void Run(SlotListRig rig, out double listUs, out double canvasUs)
        {
            Stopwatch list = new();
            Stopwatch canvas = new();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int step = 0; step < STEPS; step++)
                {
                    float distance = (pass == 0 ? step : STEPS - step) * STEP;
                    Vector2 position = rig.Content.anchoredPosition;
                    position.y = distance;
                    rig.Content.anchoredPosition = position;

                    list.Start();
                    rig.List.onValueChanged.Invoke(Vector2.zero);
                    list.Stop();

                    canvas.Start();
                    Canvas.ForceUpdateCanvases();
                    canvas.Stop();
                }
            }

            double events = STEPS * 2;
            listUs = list.Elapsed.TotalMilliseconds * 1000.0 / events;
            canvasUs = canvas.Elapsed.TotalMilliseconds * 1000.0 / events;
        }

        private static double Median(List<double> values)
        {
            values.Sort();
            return values[values.Count / 2];
        }

        private static double Min(List<double> values)
        {
            double min = double.MaxValue;
            foreach (double value in values)
                min = Math.Min(min, value);
            return min;
        }
    }
}
