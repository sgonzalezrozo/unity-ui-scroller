using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// The bento layout in a real list: where tiles go in every direction, spacing and cell aspect, tall tiles that
    /// start above the screen, recycling, the guards (expand, loop, bad layouts), queries, width changes and paging.
    /// Rig: 400 x 800 viewport unless a test says otherwise, so 4 columns make 100 px square cells.
    /// </summary>
    public class BentoTests
    {
        private static readonly BentoSpan Big = new(2, 2);
        private ScrollerRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private static BentoLayout BigFirst(int columns = 4)
            => BentoLayout.FromSpans(columns, new[] { Big, BentoSpan.One }, repeatFrom: 1);

        private IEnumerator Start(BentoLayout layout, int count = 50, RigOptions options = null)
        {
            options ??= new RigOptions();
            options.Bento = true;
            options.BentoLayout = layout;
            rig = ScrollerRig.Create(options);
            yield return rig.Initialize(count);
        }

        private void AssertTile(int id, float x, float y, float width, float height)
        {
            RectTransform slot = rig.ItemFor(id);
            Assert.IsNotNull(slot, $"item {id} has no slot");
            Assert.AreEqual(x, slot.anchoredPosition.x, 0.01f, $"item {id} x");
            Assert.AreEqual(y, slot.anchoredPosition.y, 0.01f, $"item {id} y");
            Assert.AreEqual(width, slot.sizeDelta.x, 0.01f, $"item {id} width");
            Assert.AreEqual(height, slot.sizeDelta.y, 0.01f, $"item {id} height");
        }

        // ---------- Where tiles go ----------

        [UnityTest]
        public IEnumerator Uniform_FourColumns_MakesSquareCells()
        {
            yield return Start(BentoLayout.Uniform(4));

            AssertTile(0, 0f, 0f, 100f, 100f);
            AssertTile(5, 100f, -100f, 100f, 100f);
            Assert.AreEqual(1300f, rig.List.ContentSize, 0.01f, "13 rows of 100");
        }

        [UnityTest]
        public IEnumerator ABigTile_SpansItsColumnsAndRows_AndSmallOnesFillAroundIt()
        {
            yield return Start(BigFirst());

            AssertTile(0, 0f, 0f, 200f, 200f);
            AssertTile(1, 200f, 0f, 100f, 100f);
            AssertTile(2, 300f, 0f, 100f, 100f);
            AssertTile(3, 200f, -100f, 100f, 100f);
            AssertTile(4, 300f, -100f, 100f, 100f);
            AssertTile(5, 0f, -200f, 100f, 100f);
        }

        [UnityTest]
        public IEnumerator Spacing_IsBetweenCells_AndInsideBigTiles()
        {
            yield return Start(BigFirst(), options: new RigOptions { SlotSpacing = 10f, LineSpacing = 10f });

            // Cells: (400 - 3 x 10) / 4 = 92.5 across, and rows as tall.
            AssertTile(0, 0f, 0f, 195f, 195f);
            AssertTile(1, 205f, 0f, 92.5f, 92.5f);
            AssertTile(3, 205f, -102.5f, 92.5f, 92.5f);
            AssertTile(5, 0f, -205f, 92.5f, 92.5f);
        }

        [UnityTest]
        public IEnumerator CellAspect_SetsTheRowHeight()
        {
            yield return Start(BentoLayout.Uniform(4), options: new RigOptions { CellAspect = 0.5f });

            AssertTile(4, 0f, -50f, 100f, 50f);
        }

        [UnityTest]
        public IEnumerator LeftToRight_ColumnsGoDown_AndRowsGoAcross()
        {
            yield return Start(BigFirst(), options: new RigOptions { Direction = RigDirection.LeftToRight, ViewportSize = new Vector2(800f, 400f) });

            AssertTile(0, 0f, 0f, 200f, 200f);
            AssertTile(1, 0f, -200f, 100f, 100f);
            AssertTile(3, 100f, -200f, 100f, 100f);
        }

        [UnityTest]
        public IEnumerator RightToLeft_StartsAtTheRight()
        {
            yield return Start(BigFirst(), options: new RigOptions { Direction = RigDirection.RightToLeft, ViewportSize = new Vector2(800f, 400f) });

            AssertTile(0, 0f, 0f, 200f, 200f);
            AssertTile(3, -100f, -200f, 100f, 100f);
        }

        [UnityTest]
        public IEnumerator BottomToTop_StartsAtTheBottom()
        {
            yield return Start(BigFirst(), options: new RigOptions { Direction = RigDirection.BottomToTop });

            AssertTile(0, 0f, 0f, 200f, 200f);
            AssertTile(3, 200f, 100f, 100f, 100f);
        }

        // ---------- Scrolling ----------

        [UnityTest]
        public IEnumerator ATallTile_ThatStartsAboveTheScreen_KeepsItsSlot_UntilItsEndLeaves()
        {
            // 2 columns of 200 px, rows of 200. Item 0 is 1 x 3: rows 0 to 2, so 0 to 600 px.
            BentoLayout layout = BentoLayout.FromSpans(2, new[] { new BentoSpan(1, 3), BentoSpan.One }, repeatFrom: 1);
            yield return Start(layout, 40, new RigOptions { ViewportSize = new Vector2(400f, 300f) });

            yield return rig.ScrollToOffset(450f);
            Assert.IsNotNull(rig.ItemFor(0), "the tall tile still reaches the screen");
            Assert.IsTrue(rig.List.IsItemVisible(0));
            Assert.IsNotNull(rig.ItemFor(3), "row 2, beside it");

            yield return rig.ScrollToOffset(650f);
            Assert.IsNull(rig.ItemFor(0), "its end left the screen");
        }

        [UnityTest]
        public IEnumerator VisibleStartIndex_SkipsShortTilesInTheRowsKeptAboveTheScreen()
        {
            // As above: item 0 is 1 x 3 (0 to 600 px), then 1 x 1 tiles; rows 1 and 2 hold items 2 and 3 beside it.
            // The list keeps two rows above the screen for tall tiles, but items 2 and 3 there are off screen.
            BentoLayout layout = BentoLayout.FromSpans(2, new[] { new BentoSpan(1, 3), BentoSpan.One }, repeatFrom: 1);
            yield return Start(layout, 40, new RigOptions { ViewportSize = new Vector2(400f, 300f) });

            yield return rig.ScrollToOffset(450f);
            Assert.AreEqual(0, rig.List.VisibleStartIndex, "the tall tile still reaches the screen");
            Assert.IsFalse(rig.List.IsItemVisible(2), "row 1 ends at 400 px");

            yield return rig.ScrollToOffset(650f);
            Assert.AreEqual(4, rig.List.VisibleStartIndex, "row 3 is the first on screen");
            Assert.IsFalse(rig.List.IsItemVisible(3), "row 2 ends at 600 px");
            Assert.IsTrue(rig.List.IsItemVisible(4));
        }

        [UnityTest]
        public IEnumerator ScrollingFar_RecyclesSlots_AndShowsTheRightItems()
        {
            yield return Start(BentoLayout.Uniform(4), 2000);

            yield return rig.ScrollToOffset(20000f);

            List<int> ids = rig.BoundIds();
            Assert.AreEqual(800, ids[0], "row 200 is at the top");
            CollectionAssert.Contains(ids, 831);
            Assert.AreEqual(800, rig.List.VisibleStartIndex);
            AssertTile(805, 100f, -20100f, 100f, 100f);
            Assert.Less(rig.CountItemInstances(), 70, "slots are reused");
        }

        [UnityTest]
        public IEnumerator ScrollToItem_CentresABigTile_ByItsOwnSize()
        {
            // Every 5 items: a 2 x 2 tile and four 1 x 1 around it, 2 rows. Item 50 starts row 20 (2000 px) and is 200 tall.
            BentoLayout layout = BentoLayout.FromSpans(4, new[] { Big, BentoSpan.One, BentoSpan.One, BentoSpan.One, BentoSpan.One });
            yield return Start(layout, 200);

            rig.List.ScrollToItem(50, ScrollAlign.Center);
            yield return null;

            Assert.AreEqual(1700f, rig.ScrollDistance, 0.01f);
            Assert.IsNotNull(rig.ItemFor(50));
        }

        // ---------- Queries ----------

        [UnityTest]
        public IEnumerator Queries_GiveTheTile_ItsSize_AndItsRowOffset()
        {
            yield return Start(BigFirst());

            Assert.AreEqual(new BentoTile(0, 0, 2, 2), rig.List.GetTile(0));
            Assert.AreEqual(new BentoTile(2, 1, 1, 1), rig.List.GetTile(3));
            Assert.AreEqual(200f, rig.List.GetItemSize(0), 0.01f);
            Assert.AreEqual(100f, rig.List.GetItemSize(3), 0.01f);
            Assert.AreEqual(100f, rig.List.GetItemOffset(3), 0.01f);
        }

        // ---------- Guards ----------

        [UnityTest]
        public IEnumerator Expand_IsIgnored_WithAWarning()
        {
            yield return Start(BigFirst());

            using LogCapture log = new();
            rig.List.Expand(0);
            log.AssertLogged(LogType.Warning, "bento tiles don't expand");

            Assert.IsFalse(rig.List.IsExpanded(0));
            AssertTile(0, 0f, 0f, 200f, 200f);
        }

        [UnityTest]
        public IEnumerator ABrokenLayout_LogsItsErrors_AndUsesOneByOneTilesOverSlotsPerLine()
        {
            rig = ScrollerRig.Create(new RigOptions { Bento = true, SlotsPerLine = 4, BentoLayout = BentoLayout.FromJson("{ columns: ") });

            using LogCapture log = new();
            yield return rig.Initialize(20);
            log.AssertLogged(LogType.Error, "the bento layout has errors");

            AssertTile(1, 100f, 0f, 100f, 100f);
            AssertTile(4, 0f, -100f, 100f, 100f);
        }

        [UnityTest]
        public IEnumerator WithoutALayout_TilesAreOneByOne_OverSlotsPerLine()
        {
            yield return Start(null, options: new RigOptions { SlotsPerLine = 2 });

            AssertTile(1, 200f, 0f, 200f, 200f);
            AssertTile(2, 0f, -200f, 200f, 200f);
        }

        // ---------- Sections ----------

        private IEnumerator StartSections(BentoLayout layout, SectionedSource<TestItem, TestHeader> source, RigOptions options = null)
        {
            options ??= new RigOptions();
            options.Bento = true;
            options.Sectioned = true;
            options.BentoLayout = layout;
            rig = ScrollerRig.Create(options);
            yield return rig.InitializeSections(source);
            yield return ScrollerRig.Frames(2);
        }

        private static SectionedSource<TestItem, TestHeader> TwoSections(int first, int second, float firstHeader = ScrollerRig.HEADER_BODY_HEIGHT, float secondHeader = ScrollerRig.HEADER_BODY_HEIGHT)
        {
            SectionedSource<TestItem, TestHeader> source = new();
            source.AddSection(new TestHeader("S0", firstHeader), new ListSource<TestItem>(ScrollerRig.MakeItems(first, 0)));
            source.AddSection(new TestHeader("S1", secondHeader), new ListSource<TestItem>(ScrollerRig.MakeItems(second, 100)));
            return source;
        }

        private void AssertHeader(string name, float x, float y, float width, float height)
        {
            RectTransform header = rig.HeaderFor(name);
            Assert.IsNotNull(header, $"header {name} has no slot");
            Assert.AreEqual(x, header.anchoredPosition.x, 0.5f, $"{name} x");
            Assert.AreEqual(y, header.anchoredPosition.y, 0.5f, $"{name} y");
            Assert.AreEqual(width, header.sizeDelta.x, 0.5f, $"{name} width");
            Assert.AreEqual(height, header.sizeDelta.y, 0.5f, $"{name} height");
        }

        [UnityTest]
        public IEnumerator Sections_AHeaderTakesAWholeRow_AndItsItemsStartBelowIt()
        {
            // 2 columns of 200, rows of 100 (aspect 0.5), 50 px headers.
            yield return StartSections(BentoLayout.Uniform(2), TwoSections(3, 2), new RigOptions { CellAspect = 0.5f });

            AssertHeader("S0", 0f, 0f, 400f, 50f);
            AssertTile(0, 0f, -50f, 200f, 100f);
            AssertTile(1, 200f, -50f, 200f, 100f);
            AssertTile(2, 0f, -150f, 200f, 100f);
            AssertHeader("S1", 0f, -250f, 400f, 50f);
            AssertTile(100, 0f, -300f, 200f, 100f);
            AssertTile(101, 200f, -300f, 200f, 100f);
            Assert.AreEqual(400f, rig.List.ContentSize, 0.5f);
        }

        [UnityTest]
        public IEnumerator Sections_TheLayoutStartsAgainInEachSection()
        {
            yield return StartSections(BigFirst(2), TwoSections(2, 2), new RigOptions { CellAspect = 0.5f });

            AssertTile(0, 0f, -50f, 400f, 200f);
            AssertTile(1, 0f, -250f, 200f, 100f);
            AssertHeader("S1", 0f, -350f, 400f, 50f);
            AssertTile(100, 0f, -400f, 400f, 200f);
            Assert.AreEqual(new BentoTile(0, 0, 2, 2), rig.List.GetTile(rig.Sectioned.Source.FlatIndexOf(1, 0)), "tiles count rows from their section");
        }

        [UnityTest]
        public IEnumerator Sections_MeasuredHeaders_EachTakeTheirOwnHeight()
        {
            yield return StartSections(BentoLayout.Uniform(2), TwoSections(3, 2, firstHeader: 40f, secondHeader: 120f), new RigOptions { CellAspect = 0.5f, HeaderSize = -1f });

            AssertHeader("S0", 0f, 0f, 400f, 40f);
            AssertTile(0, 0f, -40f, 200f, 100f);
            AssertHeader("S1", 0f, -240f, 400f, 120f);
            AssertTile(100, 0f, -360f, 200f, 100f);
        }

        [UnityTest]
        public IEnumerator Sections_ATallTile_NeverReachesIntoTheNextSection()
        {
            BentoLayout layout = BentoLayout.FromSpans(2, new[] { new BentoSpan(1, 3), BentoSpan.One }, repeatFrom: 1);
            yield return StartSections(layout, TwoSections(1, 2), new RigOptions { CellAspect = 0.5f });

            AssertTile(0, 0f, -50f, 200f, 300f);
            AssertHeader("S1", 0f, -350f, 400f, 50f);
        }

        [UnityTest]
        public IEnumerator Sections_ScrollToSection_GoesToItsHeader()
        {
            // 10 items in 2 columns: 5 rows of 200 under a 50 px header.
            SectionedSource<TestItem, TestHeader> source = RigSections(10, 10, 10);
            yield return StartSections(BentoLayout.Uniform(2), source);

            rig.Sectioned.ScrollToSection(1);
            yield return null;

            Assert.AreEqual(1050f, rig.ScrollDistance, 0.5f);
            Assert.IsNotNull(rig.HeaderFor("S1"));
        }

        [UnityTest]
        public IEnumerator Sections_LeftToRight_AHeaderIsAWholeColumn()
        {
            yield return StartSections(BentoLayout.Uniform(2), TwoSections(3, 2),
                new RigOptions { CellAspect = 0.5f, Direction = RigDirection.LeftToRight, ViewportSize = new Vector2(800f, 400f) });

            AssertHeader("S0", 0f, 0f, 50f, 400f);
            AssertTile(0, 50f, 0f, 100f, 200f);
            AssertTile(1, 50f, -200f, 100f, 200f);
            AssertTile(2, 150f, 0f, 100f, 200f);
            AssertHeader("S1", 250f, 0f, 50f, 400f);
        }

        private static SectionedSource<TestItem, TestHeader> RigSections(params int[] counts)
            => ScrollerRig.MakeSections(counts);

        // ---------- Changes ----------

        [UnityTest]
        public IEnumerator SetBentoLayout_AfterInitialize_RebuildsWithTheNewTiles()
        {
            yield return Start(BentoLayout.Uniform(4));

            rig.List.SetBentoLayout(BigFirst());
            yield return null;

            AssertTile(0, 0f, 0f, 200f, 200f);
            AssertTile(1, 200f, 0f, 100f, 100f);
        }

        [UnityTest]
        public IEnumerator AWiderList_MakesBiggerCells()
        {
            yield return Start(BentoLayout.Uniform(4));

            rig.ScrollerRect.sizeDelta = new Vector2(800f, 800f);
            yield return ScrollerRig.Frames(3);

            AssertTile(5, 200f, -200f, 200f, 200f);
        }

        [UnityTest]
        public IEnumerator BottomToTop_Paged_LoadsTheFirstPage_AndMoreNearTheEnd(
            [Values(false, true)] bool bento)
        {
            rig = ScrollerRig.Create(new RigOptions
            {
                Direction = RigDirection.BottomToTop, Bento = bento, BentoLayout = bento ? BigFirst(2) : null, CellAspect = 0.5f, LineSpacing = 20f
            });
            FakePageLoader loader = new() { PageSize = 30, Total = 153 };
            yield return rig.Initialize(new PagedSource<TestItem>(loader));
            yield return ScrollerRig.WaitUntil(() => rig.List.GetItemCount() >= 30, "first page");
            yield return null;

            Assert.IsNotNull(rig.ItemFor(0), "the first item is shown");
            Assert.Greater(rig.BoundIds().Count, 2);

            yield return rig.ScrollToOffset(rig.List.ContentSize - 800f);
            yield return ScrollerRig.WaitUntil(() => rig.List.GetItemCount() >= 60, "second page");
        }

        [UnityTest]
        public IEnumerator AnAppendedPage_ContinuesTheLayout_WithoutMovingTheTilesAbove()
        {
            rig = ScrollerRig.Create(new RigOptions { Bento = true, BentoLayout = BigFirst() });
            FakePageLoader loader = new() { PageSize = 10, Total = 30 };
            yield return rig.Initialize(new PagedSource<TestItem>(loader));
            yield return ScrollerRig.WaitUntil(() => rig.List.GetItemCount() >= 10, "first page");

            RectTransform first = rig.ItemFor(0);
            Vector2 before = first.anchoredPosition;

            // Ten items fill under 800 px, so the list asks for the next page by itself.
            yield return ScrollerRig.WaitUntil(() => rig.List.GetItemCount() >= 20, "second page");
            yield return null;

            // Items 5 to 8 fill row 2, item 9 starts row 3, and the page goes on from there.
            Assert.AreEqual(new BentoTile(0, 0, 2, 2), rig.List.GetTile(0));
            Assert.AreEqual(new BentoTile(1, 3, 1, 1), rig.List.GetTile(10));
            AssertTile(10, 100f, -300f, 100f, 100f);
            Assert.AreSame(first, rig.ItemFor(0), "the slot on screen stayed");
            Assert.AreEqual(before, rig.ItemFor(0).anchoredPosition);
        }
    }
}
