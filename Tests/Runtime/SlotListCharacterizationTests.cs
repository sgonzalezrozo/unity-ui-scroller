using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SlotList;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// Characterization tests for the whole scroller through its public API.
    /// They pin today's behaviour (bugs included, marked "Bug 6.x") so Phases 1-5 can't change it by accident.
    /// Default rig: vertical list, 400 x 800 viewport, 100 px slots, no spacing or padding.
    /// </summary>
    public class SlotListCharacterizationTests
    {
        private SlotListRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private static List<int> Range(int first, int last)
            => Enumerable.Range(first, last - first + 1).ToList();

        // ---------- Binding and recycling ----------

        [UnityTest]
        public IEnumerator Initialize_BindsTheFirstScreen()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(1000);

            Assert.AreEqual(Range(0, 7), rig.BoundIds());
            Assert.IsTrue(rig.Scroller.IsInitialized);
            Assert.AreEqual(1000, rig.Scroller.GetItemCount());
        }

        [UnityTest]
        public IEnumerator Bug_6_15_Initialize_ShowsTheFirstScreenWithinTwoFrames()
        {
            // The first build used to wait for WaitForEndOfFrame, which never resumes in batch mode,
            // and resumes after rendering anyway. It now waits one frame.
            rig = SlotListRig.Create();
            rig.Scroller.Initialize(new ListSource<TestItem>(SlotListRig.MakeItems(1000)));

            yield return SlotListRig.Frames(2);

            Assert.AreEqual(Range(0, 7), rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator Initialize_SetsTheContentSize()
        {
            rig = SlotListRig.Create(new RigOptions { LineSpacing = 10f, PaddingStart = 20f, PaddingEnd = 30f });
            yield return rig.Initialize(10);

            Assert.AreEqual(20f + 1000f + 90f + 30f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator Initialize_CreatesABoundedPool_NotOneItemPerItem()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(1000);
            yield return SlotListRig.Frames(8);

            // 8 visible slots created for the first build, plus a pool of ceil(8 x 1.4) = 12 warmed up afterwards,
            // two per frame (Phase 10, P9), so it's done after 6 frames.
            Assert.AreEqual(20, rig.CountItemInstances());
        }

        [UnityTest]
        public IEnumerator Scroll_ToTheMiddle_BindsThatRange()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(1000);

            yield return rig.ScrollToOffset(5000f);

            Assert.AreEqual(Range(50, 57), rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator Scroll_HalfAItem_ShowsOneMorePartialItem()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(1000);

            yield return rig.ScrollToOffset(5050f);

            Assert.AreEqual(Range(50, 58), rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator Scroll_ToTheEnd_BindsTheLastItems()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(1000);

            yield return rig.ScrollToOffset(99200f);

            Assert.AreEqual(Range(992, 999), rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator Scroll_ManyScreens_ReusesItems()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(1000);
            yield return SlotListRig.Frames(8); // the pool warm-up (two slots per frame) is done
            int before = rig.CountItemInstances();

            for (int screen = 1; screen <= 10; screen++)
                yield return rig.ScrollToOffset(screen * 800f);

            Assert.AreEqual(before, rig.CountItemInstances());
        }

        [UnityTest]
        public IEnumerator BoundItems_SitAtTheirLineOffset()
        {
            rig = SlotListRig.Create(new RigOptions { LineSpacing = 10f, PaddingStart = 20f });
            yield return rig.Initialize(100);

            RectTransform slot = rig.ItemFor(3);

            Assert.IsNotNull(slot);
            Assert.AreEqual(new Vector2(0f, -(20f + 3 * 110f)), slot.anchoredPosition);
            Assert.AreEqual(new Vector2(400f, 100f), slot.sizeDelta);
        }

        // ---------- Grid ----------

        [UnityTest]
        public IEnumerator Grid_FillsTheRowWidth()
        {
            rig = SlotListRig.Create(new RigOptions { SlotsPerLine = 3, SlotSpacing = 10f });
            yield return rig.Initialize(60);

            float width = (400f - 20f) / 3f;
            RectTransform slot = rig.ItemFor(4);

            Assert.AreEqual(Range(0, 23), rig.BoundIds());
            Assert.AreEqual(width + 10f, slot.anchoredPosition.x, 0.01f);
            Assert.AreEqual(-100f, slot.anchoredPosition.y, 0.01f);
            Assert.AreEqual(width, slot.sizeDelta.x, 0.01f);
            Assert.AreEqual(20f * 100f, rig.ContentSize, 0.01f);
        }

        // ---------- Directions ----------

        [UnityTest]
        public IEnumerator Horizontal_BindsTheFirstScreen_AndPlacesItemsRightwards()
        {
            rig = SlotListRig.Create(new RigOptions { Direction = RigDirection.LeftToRight });
            yield return rig.Initialize(100);

            Assert.AreEqual(Range(0, 3), rig.BoundIds());
            Assert.AreEqual(new Vector2(200f, 0f), rig.ItemFor(2).anchoredPosition);
            Assert.AreEqual(new Vector2(100f, 800f), rig.ItemFor(2).sizeDelta);
        }

        [UnityTest]
        public IEnumerator Horizontal_Scroll_BindsThatRange()
        {
            rig = SlotListRig.Create(new RigOptions { Direction = RigDirection.LeftToRight });
            yield return rig.Initialize(100);

            yield return rig.ScrollToOffset(5000f);

            Assert.AreEqual(Range(50, 53), rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator BottomToTop_PlacesItemsUpwards()
        {
            rig = SlotListRig.Create(new RigOptions { Direction = RigDirection.BottomToTop });
            yield return rig.Initialize(100);

            Assert.AreEqual(Range(0, 7), rig.BoundIds());
            Assert.AreEqual(new Vector2(0f, 200f), rig.ItemFor(2).anchoredPosition);
        }

        [UnityTest]
        public IEnumerator RightToLeft_PlacesItemsLeftwards()
        {
            rig = SlotListRig.Create(new RigOptions { Direction = RigDirection.RightToLeft });
            yield return rig.Initialize(100);

            Assert.AreEqual(Range(0, 3), rig.BoundIds());
            Assert.AreEqual(new Vector2(-200f, 0f), rig.ItemFor(2).anchoredPosition);
        }

        // ---------- Data changes ----------

        [UnityTest]
        public IEnumerator Refresh_AfterAdd_ShowsTheNewItem()
        {
            rig = SlotListRig.Create();
            ListSource<TestItem> source = new(SlotListRig.MakeItems(5));
            yield return rig.Initialize(source);

            source.AddItem(new TestItem(5));
            rig.Scroller.Refresh();
            yield return null;

            Assert.AreEqual(Range(0, 5), rig.BoundIds());
            Assert.AreEqual(600f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator Refresh_AfterRemove_HidesTheItem()
        {
            rig = SlotListRig.Create();
            ListSource<TestItem> source = new(SlotListRig.MakeItems(5));
            yield return rig.Initialize(source);

            source.RemoveItemAt(0);
            rig.Scroller.Refresh();
            yield return null;

            Assert.AreEqual(Range(1, 4), rig.BoundIds());
            Assert.AreEqual(400f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator SetDataSource_ShowsTheNewItems()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(5);

            rig.Scroller.SetSource(new ListSource<TestItem>(SlotListRig.MakeItems(3, 100)));
            yield return null;

            Assert.AreEqual(Range(100, 102), rig.BoundIds());
        }

        // ---------- Expansion ----------

        [UnityTest]
        public IEnumerator Expand_GrowsTheContent_AndMovesFollowingItems()
        {
            rig = SlotListRig.Create(new RigOptions { Expandable = true });
            yield return rig.Initialize(20);

            rig.Scroller.Expand(2, 300f);
            yield return null;

            Assert.IsTrue(rig.Scroller.IsExpanded(2));
            Assert.AreEqual(2200f, rig.ContentSize, 0.01f);
            Assert.AreEqual(300f, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(-500f, rig.ItemFor(3).anchoredPosition.y, 0.01f);
            Assert.IsTrue(rig.ItemFor(2).GetComponent<ExpandableRecordingSlot>().IsExpanded);
        }

        [UnityTest]
        public IEnumerator Collapse_RestoresTheLayout()
        {
            rig = SlotListRig.Create(new RigOptions { Expandable = true });
            yield return rig.Initialize(20);
            rig.Scroller.Expand(2, 300f);
            yield return null;

            rig.Scroller.Collapse(2);
            yield return null;

            Assert.IsFalse(rig.Scroller.IsExpanded(2));
            Assert.AreEqual(2000f, rig.ContentSize, 0.01f);
            Assert.AreEqual(-300f, rig.ItemFor(3).anchoredPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator Expand_InSingleMode_CollapsesThePreviousItem()
        {
            rig = SlotListRig.Create(new RigOptions { Expandable = true });
            yield return rig.Initialize(20);

            rig.Scroller.Expand(1, 300f);
            yield return null;
            ExpandableRecordingSlot first = rig.ItemFor(1).GetComponent<ExpandableRecordingSlot>();
            rig.Scroller.Expand(4, 300f);
            yield return null;

            Assert.IsFalse(rig.Scroller.IsExpanded(1));
            Assert.IsTrue(rig.Scroller.IsExpanded(4));
            Assert.AreEqual(1, first.CollapseCalls);
            Assert.AreEqual(2200f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator Expand_InMultipleMode_KeepsBoth()
        {
            rig = SlotListRig.Create(new RigOptions { Expandable = true, AllowMultipleExpanded = true });
            yield return rig.Initialize(20);

            rig.Scroller.Expand(1, 300f);
            rig.Scroller.Expand(4, 300f);
            yield return null;

            Assert.IsTrue(rig.Scroller.IsExpanded(1));
            Assert.IsTrue(rig.Scroller.IsExpanded(4));
            Assert.AreEqual(2400f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator Expand_IsIgnoredInAGrid()
        {
            rig = SlotListRig.Create(new RigOptions { Expandable = true, SlotsPerLine = 2 });
            yield return rig.Initialize(20);

            rig.Scroller.Expand(2, 300f);
            yield return null;

            Assert.IsFalse(rig.Scroller.IsExpanded(2));
            Assert.AreEqual(1000f, rig.ContentSize, 0.01f);
        }

        // ---------- Navigation and visibility ----------

        [UnityTest]
        public IEnumerator ScrollToEnd_ThenScrollToStart()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(1000);

            rig.Scroller.ScrollToEnd();
            yield return null;
            Assert.AreEqual(Range(992, 999), rig.BoundIds());
            Assert.IsTrue(rig.Scroller.IsLastItemVisible);

            rig.Scroller.ScrollToStart();
            yield return null;
            Assert.AreEqual(Range(0, 7), rig.BoundIds());
            Assert.IsTrue(rig.Scroller.IsFirstItemVisible);
        }

        [UnityTest]
        public IEnumerator Visibility_ReportsLinesAndItems()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(1000);
            yield return rig.ScrollToOffset(5000f);

            Assert.AreEqual(50, rig.Scroller.VisibleStartIndex);
            Assert.AreEqual(57, rig.Scroller.VisibleEndIndex);
            Assert.IsTrue(rig.Scroller.IsItemVisible(55));
            Assert.IsFalse(rig.Scroller.IsItemVisible(3));
            Assert.IsFalse(rig.Scroller.IsFirstItemVisible);
        }

        [UnityTest]
        public IEnumerator Visibility_InAGrid_ReportsItemIndices()
        {
            // Bug 6.13 (fixed): VisibleEndIndex used to be the line index (7). It is now the last visible item (23).
            rig = SlotListRig.Create(new RigOptions { SlotsPerLine = 3 });
            yield return rig.Initialize(60);

            Assert.AreEqual(0, rig.Scroller.VisibleStartIndex);
            Assert.AreEqual(23, rig.Scroller.VisibleEndIndex);
            Assert.IsTrue(rig.Scroller.IsItemVisible(23));
        }

        [UnityTest]
        public IEnumerator DisableThenEnable_ShowsTheSameRangeAgain()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(1000);
            yield return rig.ScrollToOffset(5000f);

            rig.Scroller.gameObject.SetActive(false);
            yield return null;
            rig.Scroller.gameObject.SetActive(true);
            yield return SlotListRig.Frames(2);

            Assert.AreEqual(Range(50, 57), rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator Clear_HidesEverything_AndResetsThePosition()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(1000);
            yield return rig.ScrollToOffset(5000f);

            rig.Scroller.Clear();

            Assert.IsEmpty(rig.BoundIds());
            Assert.AreEqual(Vector2.zero, rig.Content.anchoredPosition);
        }

        // ---------- Pull to refresh (paging is covered by PagingTests) ----------

        [UnityTest]
        public IEnumerator PullToRefresh_PastTheThreshold_CallsTheHook()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(100);
            PointerEventData pointer = new(null) { button = PointerEventData.InputButton.Left };

            rig.Content.anchoredPosition = new Vector2(0f, -100f);
            rig.Scroller.OnEndDrag(pointer);
            Assert.AreEqual(1, rig.Scroller.PullCalls);

            rig.Content.anchoredPosition = new Vector2(0f, -50f);
            rig.Scroller.OnEndDrag(pointer);
            Assert.AreEqual(1, rig.Scroller.PullCalls);
        }
    }
}
