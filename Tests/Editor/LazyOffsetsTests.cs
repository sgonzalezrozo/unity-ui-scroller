using System;
using NUnit.Framework;

namespace UIScroller.Tests.Editor
{
    /// <summary>
    /// Offsets are computed on demand and the total extent is a running sum: every answer must match adding the lines
    /// up from the start, and a size change near the screen must not compute the offsets of every line after it.
    /// </summary>
    public class LazyOffsetsTests
    {
        private const float PADDING_START = 12f;
        private const float PADDING_END = 20f;
        private const float LINE_SPACING = 6f;
        private const float SECTION_SPACING = 30f;
        private const float TOLERANCE = 0.01f;

        private static readonly Func<int, bool> NO_HEADERS = _ => false;
        private static readonly Func<int, float> SIZE_100 = _ => 100f;
        private static readonly Func<int, bool> EVERY_TENTH_IS_HEADER = i => i % 10 == 0;

        private static LayoutModel Build(int count, int slotsPerLine, Func<int, bool> isHeader = null)
        {
            LayoutModel layout = new() { Work = new WorkCounters() };
            layout.SetSpacing(PADDING_START, PADDING_END, LINE_SPACING, SECTION_SPACING);
            layout.CreatePlacements(count);
            layout.BuildLines(count, slotsPerLine, isHeader ?? NO_HEADERS, SIZE_100, SIZE_100);
            return layout;
        }

        /// <summary>Offsets and total worked out the slow way, from the first line.</summary>
        private static void AssertMatchesFullSum(LayoutModel layout)
        {
            ListView<Line> lines = layout.Lines;
            float position = PADDING_START;
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0 && lines[i].StartsSection)
                    position += SECTION_SPACING;

                Assert.AreEqual(position, layout.OffsetOf(i), TOLERANCE, $"offset of line {i}");
                position += lines[i].Size + LINE_SPACING;
            }

            float total = lines.Length == 0 ? 0f : position - LINE_SPACING + PADDING_END;
            Assert.AreEqual(total, layout.TotalExtent, TOLERANCE, "total extent");
        }

        [Test]
        public void SizeChanges_KeepOffsetsAndTotalEqualToTheFullSum()
        {
            LayoutModel layout = Build(500, 1, EVERY_TENTH_IS_HEADER);
            Random random = new(7);

            for (int step = 0; step < 200; step++)
                layout.SetItemSize(random.Next(500), 40f + random.Next(200));

            AssertMatchesFullSum(layout);
        }

        [Test]
        public void GridSizeChanges_KeepOffsetsAndTotalEqualToTheFullSum()
        {
            LayoutModel layout = Build(1_000, 4);
            Random random = new(11);

            for (int step = 0; step < 300; step++)
                layout.SetItemSize(random.Next(1_000), 40f + random.Next(200));
            layout.SetUnmeasuredSize(77f);

            AssertMatchesFullSum(layout);
        }

        [Test]
        public void TotalExtent_FollowsSizeChangesWithoutComputingOffsets()
        {
            LayoutModel layout = Build(100_000, 1);
            float before = layout.TotalExtent;
            layout.Work.Reset();

            layout.SetItemSize(50_000, 250f);

            Assert.AreEqual(before + 150f, layout.TotalExtent, TOLERANCE);
            Assert.AreEqual(0, layout.Work.OffsetsComputed);
        }

        [Test]
        public void MeasuringAnItemNearTheScreen_ComputesOnlyUpToTheScreen()
        {
            LayoutModel layout = Build(100_000, 1);
            layout.OffsetOf(30);
            layout.Work.Reset();

            // An item on screen is measured, then the screen's lines are asked for: the 99,970 lines after them wait.
            layout.SetItemSize(10, 180f);
            float offset = layout.OffsetOf(30);

            Assert.LessOrEqual(layout.Work.OffsetsComputed, 30);
            Assert.AreEqual(PADDING_START + 30 * (100f + LINE_SPACING) + 80f, offset, TOLERANCE);
        }

        [Test]
        public void VisibleRangeSearches_ComputeOnlyAsFarAsTheScreen()
        {
            LayoutModel layout = Build(100_000, 1);
            layout.Work.Reset();
            float viewportStart = 2_000f;
            float viewportEnd = 3_000f;

            int first = layout.FirstLineEndingAfter(viewportStart);
            int afterLast = layout.FirstLineStartingAtOrAfter(viewportEnd, first);

            Assert.AreEqual(18, first);
            Assert.AreEqual(29, afterLast);
            Assert.LessOrEqual(layout.Work.OffsetsComputed, 32);
        }

        [Test]
        public void LineAtDistance_MatchesTheFullSum()
        {
            LayoutModel layout = Build(300, 1, EVERY_TENTH_IS_HEADER);
            layout.SetItemSize(5, 260f);

            Assert.AreEqual(0, layout.LineAtDistance(0f), "before the first line");
            for (float distance = PADDING_START; distance < layout.TotalExtent; distance += 37f)
            {
                int line = layout.LineAtDistance(distance);
                Assert.LessOrEqual(layout.OffsetOf(line), distance + TOLERANCE, $"distance {distance}");
                if (line + 1 < layout.Lines.Length)
                    Assert.Greater(layout.OffsetOf(line + 1), distance, $"distance {distance}");
            }
        }

        [Test]
        public void AppendedLines_KeepTheTotalEqualToTheFullSum()
        {
            LayoutModel layout = Build(100, 3);
            layout.SetItemSize(4, 150f);
            layout.OffsetOf(20);

            layout.ExtendPlacements(250);
            layout.BuildLines(250, 3, NO_HEADERS, SIZE_100, SIZE_100, 100);

            AssertMatchesFullSum(layout);
        }

        [Test]
        public void RebuildingWithFewerItems_KeepsTheTotalEqualToTheFullSum()
        {
            LayoutModel layout = Build(400, 1, EVERY_TENTH_IS_HEADER);
            layout.SetItemSize(250, 300f);
            layout.OffsetOf(399);

            layout.CreatePlacements(120);
            layout.BuildLines(120, 1, EVERY_TENTH_IS_HEADER, SIZE_100, SIZE_100);

            AssertMatchesFullSum(layout);
        }

        [Test]
        public void ChangingSpacing_KeepsTheTotalEqualToTheFullSum()
        {
            LayoutModel layout = Build(50, 1, EVERY_TENTH_IS_HEADER);
            layout.OffsetOf(49);

            layout.SetSpacing(PADDING_START, PADDING_END, LINE_SPACING, SECTION_SPACING);
            layout.SetItemSize(3, 10f);

            AssertMatchesFullSum(layout);
        }
    }
}
