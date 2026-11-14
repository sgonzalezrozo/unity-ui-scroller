using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// The bento layout in a real list: where tiles go in every direction, spacing and cell aspect, tall tiles that
    /// start above the screen, recycling, the guards (expand, loop, bad layouts), queries, width changes and paging.
    /// Rig: 400 x 800 viewport unless a test says otherwise, so 4 columns make 100 px square cells.
    /// </summary>
    public class BentoTests
    {
        private static readonly BentoSpan Big = new(2, 2);
        private SlotListRig rig;

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
            rig = SlotListRig.Create(options);
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

            LogAssert.Expect(LogType.Warning, new Regex("bento tiles don't expand"));
            rig.List.Expand(0);

            Assert.IsFalse(rig.List.IsExpanded(0));
            AssertTile(0, 0f, 0f, 200f, 200f);
        }

        [UnityTest]
        public IEnumerator Loop_IsRefused_AndTheTilesStay()
        {
            rig = SlotListRig.Create(new RigOptions { Bento = true, BentoLayout = BigFirst() });
            rig.List.Settings.loop = true;

            LogAssert.Expect(LogType.Error, new Regex("Loop doesn't work with a bento layout"));
            yield return rig.Initialize(50);

            AssertTile(1, 200f, 0f, 100f, 100f);
        }

        [UnityTest]
        public IEnumerator ABrokenLayout_LogsItsErrors_AndUsesOneByOneTilesOverSlotsPerLine()
        {
            rig = SlotListRig.Create(new RigOptions { Bento = true, SlotsPerLine = 4, BentoLayout = BentoLayout.FromJson("{ columns: ") });

            LogAssert.Expect(LogType.Error, new Regex("the bento layout has errors"));
            yield return rig.Initialize(20);

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
            yield return SlotListRig.Frames(3);

            AssertTile(5, 200f, -200f, 200f, 200f);
        }

        [UnityTest]
        public IEnumerator AnAppendedPage_ContinuesTheLayout_WithoutMovingTheTilesAbove()
        {
            rig = SlotListRig.Create(new RigOptions { Bento = true, BentoLayout = BigFirst() });
            FakePageLoader loader = new() { PageSize = 10, Total = 30 };
            yield return rig.Initialize(new PagedSource<TestItem>(loader));
            yield return SlotListRig.WaitUntil(() => rig.List.GetItemCount() >= 10, "first page");

            RectTransform first = rig.ItemFor(0);
            Vector2 before = first.anchoredPosition;

            // Ten items fill under 800 px, so the list asks for the next page by itself.
            yield return SlotListRig.WaitUntil(() => rig.List.GetItemCount() >= 20, "second page");
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
