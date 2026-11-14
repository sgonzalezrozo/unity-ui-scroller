using System;
using NUnit.Framework;
using SlotList;

namespace SlotList.Tests.Editor
{
    /// <summary>Characterization tests for the layout classes that remain. LineBuilder, LineOffsets and ContentExtentCalculator cases moved to LayoutModelTests.</summary>
    public class SlotsPerLineResolverTests
    {
        [Test]
        public void Fixed_ReturnsTheConfiguredCount()
            => Assert.AreEqual(3, new SlotsPerLineResolver(3, false, 100f, 10f, 1, 8).Calculate(1000f));

        [Test]
        public void Fixed_IsNeverBelowOne()
            => Assert.AreEqual(1, new SlotsPerLineResolver(0, false, 100f, 10f, 1, 8).Calculate(1000f));

        [Test]
        public void Dynamic_FitsAsManyItemsAsTheWidthAllows()
        {
            // (width + spacing) / (slot + spacing) = (450 + 10) / 110 = 4.18 -> 4
            Assert.AreEqual(4, new SlotsPerLineResolver(2, true, 100f, 10f, 1, 8).Calculate(450f));
        }

        [Test]
        public void Dynamic_IsClampedToMinAndMax()
        {
            Assert.AreEqual(3, new SlotsPerLineResolver(2, true, 100f, 10f, 3, 8).Calculate(150f));
            Assert.AreEqual(5, new SlotsPerLineResolver(2, true, 100f, 10f, 1, 5).Calculate(5000f));
        }

        [Test]
        public void Dynamic_WorksWhateverTheFixedCount()
        {
            // Auto Grid set from code, with slotsPerLine left at 1: the inspector shows Auto Grid, so it must be one.
            SlotsPerLineResolver resolver = new(1, true, 100f, 10f, 1, 8);

            Assert.IsTrue(resolver.IsDynamic);
            Assert.IsTrue(resolver.IsGridMode);
            Assert.AreEqual(4, resolver.Calculate(450f));
        }
    }

    public class VisibleRangeFinderTests
    {
        private static LayoutModel Layout(int count, float size)
        {
            LayoutModel layout = new();
            layout.CreatePlacements(count);
            layout.BuildLines(count, 1, _ => false, _ => size, _ => size);
            layout.RecomputeOffsets();
            return layout;
        }

        [Test]
        public void AtTheStart_ShowsTheFirstScreen()
        {
            LayoutModel layout = Layout(100, 100f);
            VisibleRangeFinder finder = new(true, false);

            int start = finder.FindFirstLine(0f, UnityEngine.Vector2.zero, layout);
            int end = finder.FindLastLine(0f, 800f, start, layout);

            Assert.AreEqual(0, start);
            Assert.AreEqual(7, end);
        }

        [Test]
        public void InTheMiddle_StartsAtThePartlyVisibleLine()
        {
            LayoutModel layout = Layout(100, 100f);
            VisibleRangeFinder finder = new(true, false);

            // Vertical top-to-bottom: content y = 5050 means 5050 px scrolled.
            int start = finder.FindFirstLine(-5050f, new UnityEngine.Vector2(0f, 5050f), layout);
            int end = finder.FindLastLine(-5050f, 800f, start, layout);

            Assert.AreEqual(50, start);
            Assert.AreEqual(58, end);
        }

        [Test]
        public void AtTheEnd_EndsAtTheLastLine()
        {
            LayoutModel layout = Layout(100, 100f);
            VisibleRangeFinder finder = new(true, false);

            int start = finder.FindFirstLine(-9200f, new UnityEngine.Vector2(0f, 9200f), layout);
            int end = finder.FindLastLine(-9200f, 800f, start, layout);

            Assert.AreEqual(92, start);
            Assert.AreEqual(99, end);
        }

        [Test]
        public void HundredThousandLines_FindsTheRightRangeInTheMiddleAndAtTheEnd()
        {
            LayoutModel layout = Layout(100000, 100f);
            VisibleRangeFinder finder = new(true, false);

            int middleStart = finder.FindFirstLine(-5000050f, new UnityEngine.Vector2(0f, 5000050f), layout);
            int middleEnd = finder.FindLastLine(-5000050f, 800f, middleStart, layout);
            int endStart = finder.FindFirstLine(-9999200f, new UnityEngine.Vector2(0f, 9999200f), layout);
            int endEnd = finder.FindLastLine(-9999200f, 800f, endStart, layout);

            Assert.AreEqual(50000, middleStart);
            Assert.AreEqual(50008, middleEnd);
            Assert.AreEqual(99992, endStart);
            Assert.AreEqual(99999, endEnd);
        }

        [Test]
        public void UnevenLines_FindTheLinesThatOverlapTheViewport()
        {
            // Sizes alternate 50, 150: line 2m starts at 200m, line 2m+1 at 200m + 50.
            LayoutModel layout = new();
            layout.CreatePlacements(100);
            layout.BuildLines(100, 1, _ => false, i => i % 2 == 0 ? 50f : 150f, i => i % 2 == 0 ? 50f : 150f);
            layout.RecomputeOffsets();
            VisibleRangeFinder finder = new(true, false);

            int start = finder.FindFirstLine(-1025f, new UnityEngine.Vector2(0f, 1025f), layout);
            int end = finder.FindLastLine(-1025f, 400f, start, layout);

            Assert.AreEqual(10, start);
            Assert.AreEqual(14, end);
        }

        [TestCase(true, false, 0f, -10f)]
        [TestCase(true, true, 0f, 10f)]
        [TestCase(false, false, 10f, 0f)]
        [TestCase(false, true, -10f, 0f)]
        public void AtOrBeforeTheScrollStart_ReturnsLineZero(bool vertical, bool reverse, float x, float y)
        {
            LayoutModel layout = Layout(100, 100f);
            VisibleRangeFinder finder = new(vertical, reverse);

            Assert.AreEqual(0, finder.FindFirstLine(5000f, new UnityEngine.Vector2(x, y), layout));
        }
    }
}
