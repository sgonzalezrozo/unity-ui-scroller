using System;
using NUnit.Framework;

namespace UIScroller.Tests.Editor
{
    /// <summary>
    /// Design tests for LayoutModel (Phase 5, step 2): placements, lines, offsets, total extent and the item-to-line map.
    /// They include the cases of the old LineBuilder, LineOffsets and ContentExtentCalculator tests.
    /// </summary>
    public class LayoutModelTests
    {
        private static readonly Func<int, bool> NO_HEADERS = _ => false;
        private static readonly Func<int, float> SIZE_100 = _ => 100f;

        private static LayoutModel Build(int count, int slotsPerLine, Func<int, bool> isHeader = null, Func<int, float> itemSize = null, Func<int, float> lineSize = null)
        {
            LayoutModel layout = new();
            layout.CreatePlacements(count);
            layout.BuildLines(count, slotsPerLine, isHeader ?? NO_HEADERS, itemSize ?? SIZE_100, lineSize ?? itemSize ?? SIZE_100);
            return layout;
        }

        private static int[] ItemsIn(Line line)
        {
            int[] result = new int[line.SlotCount];
            for (int i = 0; i < line.SlotCount; i++)
                result[i] = line[i]?.DataIndex ?? -1;
            return result;
        }

        // ---------- Buffers give memory back when the item count drops a lot ----------

        private static void Rebuild(LayoutModel layout, int count)
        {
            layout.CreatePlacements(count);
            layout.BuildLines(count, 1, NO_HEADERS, SIZE_100, SIZE_100);
            layout.RecomputeOffsets();
        }

        [Test]
        public void DroppingFrom100000To300_GivesTheExtraRecordsBack()
        {
            LayoutModel layout = Build(100_000, 1);
            layout.RecomputeOffsets();

            Rebuild(layout, 300);

            Assert.LessOrEqual(layout.PlacementCapacity, 600);
            Assert.LessOrEqual(layout.LineCapacity, 600);
            Assert.LessOrEqual(layout.OffsetCapacity, 600);
            Assert.AreEqual(300, layout.Placements.Length);
            Assert.AreEqual(29_900f, layout.OffsetOf(299), 0.01f);
        }

        [Test]
        public void DroppingALittle_KeepsTheRecords_SoNothingIsCreatedAgain()
        {
            LayoutModel layout = Build(10_000, 1);
            WorkCounters work = new();
            layout.Work = work;

            Rebuild(layout, 9_000);
            Rebuild(layout, 10_000);

            Assert.AreEqual(0, work.PlacementsCreated);
            Assert.AreEqual(0, work.LinesCreated);
        }

        // ---------- Placements and lines ----------

        [Test]
        public void CreatePlacements_MakesOnePerItem_WithItsIndex()
        {
            LayoutModel layout = new();
            layout.CreatePlacements(3);

            Assert.AreEqual(3, layout.Placements.Length);
            Assert.AreEqual(2, layout.Placements[2].DataIndex);
        }

        [Test]
        public void List_HasOneLinePerItem()
        {
            LayoutModel layout = Build(5, 1);

            Assert.AreEqual(5, layout.Lines.Length);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i, layout.Lines[i].LineIndex);
                Assert.AreEqual(new[] { i }, ItemsIn(layout.Lines[i]));
                Assert.AreEqual(100f, layout.Lines[i].Size);
            }
        }

        [Test]
        public void Grid_FillsLines_AndLeavesTheLastOnePartial()
        {
            LayoutModel layout = Build(10, 3);

            Assert.AreEqual(4, layout.Lines.Length);
            Assert.AreEqual(new[] { 0, 1, 2 }, ItemsIn(layout.Lines[0]));
            Assert.AreEqual(new[] { 9, -1, -1 }, ItemsIn(layout.Lines[3]));
        }

        [Test]
        public void Header_TakesItsOwnLine_AndTheNextItemStartsANewLine()
        {
            LayoutModel layout = Build(7, 3, i => i == 0 || i == 4);

            // A header's line has room for a whole row (so lines are reused whether they hold a header or items),
            // with the header first, and is marked as starting a section.
            Assert.AreEqual(4, layout.Lines.Length);
            Assert.AreEqual(new[] { 0, -1, -1 }, ItemsIn(layout.Lines[0]));
            Assert.AreEqual(new[] { 1, 2, 3 }, ItemsIn(layout.Lines[1]));
            Assert.AreEqual(new[] { 4, -1, -1 }, ItemsIn(layout.Lines[2]));
            Assert.AreEqual(new[] { 5, 6, -1 }, ItemsIn(layout.Lines[3]));
            Assert.IsTrue(layout.Lines[0].StartsSection);
            Assert.IsFalse(layout.Lines[1].StartsSection);
            Assert.IsTrue(layout.Lines[2].StartsSection);
        }

        [Test]
        public void LineSize_ComesFromTheLineSizeFunctionForItsFirstItem()
        {
            LayoutModel layout = Build(6, 3, null, i => 10f * (i + 1), i => 1000f + i);

            Assert.AreEqual(1000f, layout.Lines[0].Size);
            Assert.AreEqual(1003f, layout.Lines[1].Size);
        }

        [Test]
        public void ItemSizes_AreCachedOnThePlacements()
        {
            LayoutModel layout = Build(3, 1, null, i => 10f * (i + 1), SIZE_100);

            Assert.AreEqual(10f, layout.Placements[0].Size);
            Assert.AreEqual(30f, layout.Placements[2].Size);
        }

        [Test]
        public void Empty_HasNoLines_AndZeroExtent()
        {
            LayoutModel layout = Build(0, 1);
            layout.SetSpacing(10f, 20f, 5f);
            layout.RecomputeOffsets();

            Assert.AreEqual(0, layout.Lines.Length);
            Assert.AreEqual(0f, layout.TotalExtent);
            Assert.IsFalse(layout.HasOffsets);
        }

        [Test]
        public void SectionSpacing_GoesBeforeEveryHeader_ButTheFirst()
        {
            // Entries: header 0, items 1 and 2, header 3, item 4, header 5. 100 px each, 5 between lines, 30 between sections.
            LayoutModel layout = Build(6, 1, i => i == 0 || i == 3 || i == 5);
            layout.SetSpacing(10f, 20f, 5f, 30f);
            layout.RecomputeOffsets();

            Assert.AreEqual(10f, layout.OffsetOf(0), "the first header starts at the padding");
            Assert.AreEqual(115f, layout.OffsetOf(1));
            Assert.AreEqual(220f, layout.OffsetOf(2));
            Assert.AreEqual(355f, layout.OffsetOf(3), "220 + 100 + 5 + 30");
            Assert.AreEqual(460f, layout.OffsetOf(4));
            Assert.AreEqual(595f, layout.OffsetOf(5));
            Assert.AreEqual(715f, layout.TotalExtent);
        }

        [Test]
        public void SectionSpacing_DoesNothing_WithoutHeaders()
        {
            LayoutModel layout = Build(3, 1);
            layout.SetSpacing(0f, 0f, 5f, 30f);
            layout.RecomputeOffsets();

            Assert.AreEqual(210f, layout.OffsetOf(2));
        }

        [Test]
        public void ExtendPlacements_KeepsTheExistingOnes_AndAddsTheRest()
        {
            LayoutModel layout = Build(3, 1);
            Placement first = layout.Placements[0];

            layout.ExtendPlacements(5);

            Assert.AreEqual(5, layout.Placements.Length);
            Assert.AreSame(first, layout.Placements[0]);
            Assert.AreEqual(4, layout.Placements[4].DataIndex);
        }

        // ---------- Per-item sizes (Phase 8, step 2) ----------

        [Test]
        public void SetItemSize_UpdatesTheItemAndItsLine_AndMarksItMeasured()
        {
            LayoutModel layout = Build(5, 1);
            Assert.IsFalse(layout.IsMeasured(2));

            layout.SetItemSize(2, 250f);

            Assert.AreEqual(250f, layout.Placements[2].Size);
            Assert.AreEqual(250f, layout.Lines[2].Size);
            Assert.IsTrue(layout.IsMeasured(2));
        }

        [Test]
        public void InAGrid_TheLineIsAsTallAsItsTallestItem()
        {
            LayoutModel layout = Build(6, 3);

            layout.SetItemSize(0, 50f);
            layout.SetItemSize(1, 120f);
            layout.SetItemSize(2, 80f);

            Assert.AreEqual(120f, layout.Lines[0].Size);

            layout.SetItemSize(1, 60f);
            Assert.AreEqual(80f, layout.Lines[0].Size, "the line shrinks to its tallest remaining item");
        }

        [Test]
        public void ChangingOneSize_MovesOnlyTheLinesAfterIt()
        {
            LayoutModel layout = Build(5, 1);
            layout.SetSpacing(0f, 0f, 10f);
            layout.RecomputeOffsets();

            layout.SetItemSize(2, 300f);
            layout.RecomputeOffsets();

            Assert.AreEqual(0f, layout.OffsetOf(0));
            Assert.AreEqual(110f, layout.OffsetOf(1));
            Assert.AreEqual(220f, layout.OffsetOf(2));
            Assert.AreEqual(530f, layout.OffsetOf(3));
            Assert.AreEqual(640f, layout.OffsetOf(4));
            Assert.AreEqual(700f + 4 * 10f, layout.TotalExtent);
        }

        [Test]
        public void AverageMeasuredSize_IsTheMeanOfMeasuredItems_OrTheFallback()
        {
            LayoutModel layout = Build(5, 1);
            Assert.AreEqual(77f, layout.AverageMeasuredSize(77f));

            layout.SetItemSize(0, 100f);
            layout.SetItemSize(1, 300f);

            Assert.AreEqual(200f, layout.AverageMeasuredSize(77f));

            layout.SetItemSize(1, 100f);
            Assert.AreEqual(100f, layout.AverageMeasuredSize(77f), "re-measuring an item replaces its old size in the average");
        }

        [Test]
        public void SetUnmeasuredSize_ResizesOnlyTheItemsNotMeasuredYet()
        {
            LayoutModel layout = Build(4, 1);
            layout.SetItemSize(1, 250f);

            layout.SetUnmeasuredSize(80f);

            Assert.AreEqual(80f, layout.Lines[0].Size);
            Assert.AreEqual(250f, layout.Lines[1].Size);
            Assert.AreEqual(80f, layout.Placements[3].Size);
            Assert.IsFalse(layout.IsMeasured(3));
        }

        [Test]
        public void CreatePlacements_ForgetsTheMeasurements()
        {
            LayoutModel layout = Build(3, 1);
            layout.SetItemSize(1, 250f);

            layout.CreatePlacements(3);

            Assert.IsFalse(layout.IsMeasured(1));
            Assert.AreEqual(5f, layout.AverageMeasuredSize(5f));
        }

        [Test]
        public void ForgetSize_KeepsTheSize_ButMarksItUnmeasured_AndLeavesTheAverage()
        {
            LayoutModel layout = Build(4, 1);
            layout.SetItemSize(0, 100f);
            layout.SetItemSize(1, 300f);

            layout.ForgetSize(1);

            Assert.IsFalse(layout.IsMeasured(1));
            Assert.AreEqual(300f, layout.Placements[1].Size, "the old size stays as the placeholder until it's measured again");
            Assert.AreEqual(100f, layout.AverageMeasuredSize(5f));
        }

        [Test]
        public void ASizeLeftOutOfTheAverage_IsStillMeasured_AndSized()
        {
            LayoutModel layout = Build(4, 1);
            layout.SetItemSize(0, 100f);
            layout.SetItemSize(1, 500f, countInAverage: false);

            Assert.IsTrue(layout.IsMeasured(1));
            Assert.AreEqual(500f, layout.Lines[1].Size);
            Assert.AreEqual(100f, layout.AverageMeasuredSize(5f));

            layout.SetItemSize(1, 100f);
            Assert.AreEqual(100f, layout.AverageMeasuredSize(5f), "measured again normally, it joins the average");
            layout.ForgetSize(1);
            Assert.AreEqual(100f, layout.AverageMeasuredSize(5f));
        }

        [Test]
        public void ForgetAllSizes_MarksEverythingUnmeasured()
        {
            LayoutModel layout = Build(4, 1);
            layout.SetItemSize(0, 100f);
            layout.SetItemSize(2, 300f);

            layout.ForgetAllSizes();

            Assert.IsFalse(layout.IsMeasured(0));
            Assert.IsFalse(layout.IsMeasured(2));
            Assert.AreEqual(5f, layout.AverageMeasuredSize(5f));
        }

        // ---------- Item to line ----------

        [Test]
        public void LineOf_InAList_IsTheItemIndex()
        {
            LayoutModel layout = Build(5, 1);

            Assert.AreEqual(3, layout.LineOf(3));
        }

        [Test]
        public void LineOf_InAGrid_IsTheRow()
        {
            LayoutModel layout = Build(10, 3);

            Assert.AreEqual(0, layout.LineOf(2));
            Assert.AreEqual(1, layout.LineOf(3));
            Assert.AreEqual(3, layout.LineOf(9));
        }

        [Test]
        public void LineOf_WithHeaders_FollowsTheRealLines()
        {
            // Lines: [0] [1 2 3] [4] [5 6 -]. Dividing by the slot count would say item 4 is in line 1.
            LayoutModel layout = Build(7, 3, i => i == 0 || i == 4);

            Assert.AreEqual(0, layout.LineOf(0));
            Assert.AreEqual(1, layout.LineOf(3));
            Assert.AreEqual(2, layout.LineOf(4));
            Assert.AreEqual(3, layout.LineOf(6));
        }

        [Test]
        public void LineOf_OutOfRange_IsMinusOne()
        {
            LayoutModel layout = Build(5, 1);

            Assert.AreEqual(-1, layout.LineOf(-1));
            Assert.AreEqual(-1, layout.LineOf(5));
        }

        // ---------- Offsets and extent ----------

        [Test]
        public void Offsets_IncludePaddingAndSpacing()
        {
            float[] sizes = { 100f, 50f, 200f };
            LayoutModel layout = Build(3, 1, null, i => sizes[i]);
            layout.SetSpacing(10f, 0f, 5f);

            layout.RecomputeOffsets();

            Assert.AreEqual(10f, layout.OffsetOf(0));
            Assert.AreEqual(115f, layout.OffsetOf(1));
            Assert.AreEqual(170f, layout.OffsetOf(2));
            Assert.IsTrue(layout.HasOffsets);
        }

        [Test]
        public void OffsetOf_OutOfRange_IsZero()
        {
            LayoutModel layout = Build(1, 1);
            layout.RecomputeOffsets();

            Assert.AreEqual(0f, layout.OffsetOf(-1));
            Assert.AreEqual(0f, layout.OffsetOf(1));
        }

        [Test]
        public void TotalExtent_IsPaddingPlusSizesPlusSpacingBetweenLines()
        {
            LayoutModel layout = Build(3, 1);
            layout.SetSpacing(10f, 20f, 5f);

            Assert.AreEqual(10f + 300f + 10f + 20f, layout.TotalExtent);
        }

        [Test]
        public void TotalExtent_FollowsLineSizeChanges()
        {
            LayoutModel layout = Build(3, 1);
            layout.Lines[1].CacheSize(300f);

            Assert.AreEqual(500f, layout.TotalExtent);
        }
    }
}
