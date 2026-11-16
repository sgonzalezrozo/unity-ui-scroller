using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// Phase 10 regression tests: they hold the performance the list reaches. They check work that can be counted
    /// (parent changes, allocations), never timing, so they give the same result on every machine.
    /// </summary>
    public class PerformanceTests
    {
        private ScrollerRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
            SlotPool.KeepPooledSlotsActive = true;
        }

        private IEnumerator CreateQuietList(int count)
        {
            rig = ScrollerRig.Create(new RigOptions { QuietSlots = true });
            yield return rig.Initialize(count);
            yield return ScrollerRig.Frames(3);
        }

        /// <summary>Moves the content and runs the list's scroll handler at once, in this frame.</summary>
        private void ScrollNow(float offset)
        {
            Vector2 position = rig.Content.anchoredPosition;
            position.y = offset;
            rig.Content.anchoredPosition = position;
            rig.List.onValueChanged.Invoke(Vector2.zero);
        }

        // ---------- P6: slots stay under Content ----------

        [UnityTest]
        public IEnumerator Scrolling_NeverMovesASlotToAnotherParent()
        {
            yield return CreateQuietList(200);
            yield return rig.ScrollToOffset(3000f);
            yield return rig.ScrollToOffset(0f);
            ParentWatch.Changes = 0;

            for (float offset = 0f; offset <= 5000f; offset += 50f)
                yield return rig.ScrollToOffset(offset);

            Assert.AreEqual(0, ParentWatch.Changes, "A slot changed parent while scrolling, which rebuilds its graphics.");
        }

        // ---------- P7: Refresh keeps slots on their lines ----------

        [UnityTest]
        public IEnumerator Refresh_SameItemCount_KeepsEachItemInItsSlot_AndRebindsIt()
        {
            yield return CreateQuietList(50);
            List<int> shown = rig.BoundIds();
            Dictionary<int, RectTransform> slotBefore = new();
            foreach (int id in shown)
                slotBefore[id] = rig.ItemFor(id);
            int bindsBefore = BindCount(shown);
            ParentWatch.Changes = 0;

            rig.List.Refresh();
            yield return ScrollerRig.Frames(2);

            Assert.AreEqual(0, ParentWatch.Changes, "Refresh moved slots out of Content and back.");
            CollectionAssert.AreEqual(shown, rig.BoundIds());
            foreach (int id in shown)
                Assert.AreSame(slotBefore[id], rig.ItemFor(id), $"Item {id} moved to another slot.");
            Assert.AreEqual(bindsBefore + shown.Count, BindCount(shown), "Refresh must bind every shown item again.");
        }

        private int BindCount(List<int> ids)
        {
            int total = 0;
            foreach (int id in ids)
                total += rig.ItemFor(id).GetComponent<QuietSlot>().BindCount;
            return total;
        }

        // ---------- P2, P3, P4: work stays proportional to what's on screen ----------

        private WorkCounters Work => rig.List.Work;

        [UnityTest]
        public IEnumerator ScrollingOneLine_In100000Items_ChecksOnlyTheShownSlots()
        {
            yield return CreateQuietList(100_000);
            ScrollNow(50_000f);
            Work.Reset();

            ScrollNow(50_100f);

            Assert.LessOrEqual(Work.SlotsChecked, 20, "Deciding which slots left the screen must not walk every line.");
        }

        [UnityTest]
        public IEnumerator ScrollingOneLine_PlacesOnlyTheSlotThatCameIn()
        {
            yield return CreateQuietList(200);
            ScrollNow(1000f);
            Work.Reset();

            ScrollNow(1100f);

            Assert.AreEqual(1, Work.SlotsPlaced, "Slots that didn't move must not be placed again.");
        }

        [UnityTest]
        public IEnumerator Scrolling_KeepsEveryShownSlotWhereItBelongs()
        {
            yield return CreateQuietList(200);

            for (float offset = 0f; offset <= 3000f; offset += 70f)
                ScrollNow(offset);

            foreach (int id in rig.BoundIds())
                Assert.AreEqual(id * 100f, -rig.ItemFor(id).anchoredPosition.y, 0.01f, $"Item {id} is out of place.");
        }

        [UnityTest]
        public IEnumerator ASubPixelScroll_DoesNotWorkOutTheVisibleRange()
        {
            yield return CreateQuietList(200);
            ScrollNow(1000f);
            Work.Reset();

            ScrollNow(1000.2f);

            Assert.AreEqual(0, Work.RangeChecks);
        }

        [UnityTest]
        public IEnumerator ManySubPixelScrolls_StillAddUp()
        {
            yield return CreateQuietList(200);
            ScrollNow(1000f);
            CollectionAssert.Contains(rig.BoundIds(), 10);

            for (int i = 1; i <= 1000; i++)
                ScrollNow(1000f + i * 0.2f);

            // About 200 px of 0.2 px steps: item 10 scrolled off and item 19 came in (11 keeps a sliver on screen).
            CollectionAssert.DoesNotContain(rig.BoundIds(), 10, "Small steps must still add up and move the visible range.");
            CollectionAssert.Contains(rig.BoundIds(), 19);
        }

        // ---------- P8, P9, P10: no spikes when the list opens or comes back ----------

        [UnityTest]
        public IEnumerator Initialize_BindsTheFirstScreen_InTheSameFrame()
        {
            rig = ScrollerRig.Create(new RigOptions { QuietSlots = true });
            bool ready = false;
            rig.List.OnInitialized += () => ready = true;

            rig.Scroller.Initialize(new ListSource<TestItem>(ScrollerRig.MakeItems(50)));

            Assert.IsTrue(ready, "OnInitialized must fire inside Initialize.");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, rig.BoundIds(), "No blank first frame.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PoolWarmUp_CreatesAtMostTwoSlotsPerFrame()
        {
            rig = ScrollerRig.Create(new RigOptions { QuietSlots = true });
            rig.Scroller.Initialize(new ListSource<TestItem>(ScrollerRig.MakeItems(500)));
            int count = rig.CountItemInstances();

            for (int frame = 0; frame < 30; frame++)
            {
                yield return null;
                int now = rig.CountItemInstances();
                Assert.LessOrEqual(now - count, 2, $"Frame {frame} created {now - count} slots.");
                count = now;
            }

            Assert.Greater(count, rig.BoundIds().Count, "The warm-up must still fill the pool.");
        }

        [UnityTest]
        public IEnumerator DisableThenEnable_KeepsEverySlot_WithoutRebindingOrMovingIt()
        {
            yield return CreateQuietList(200);
            yield return rig.ScrollToOffset(3000f);
            List<int> shown = rig.BoundIds();
            Dictionary<int, RectTransform> slotBefore = new();
            foreach (int id in shown)
                slotBefore[id] = rig.ItemFor(id);
            int bindsBefore = BindCount(shown);
            ParentWatch.Changes = 0;

            rig.List.gameObject.SetActive(false);
            yield return null;
            rig.List.gameObject.SetActive(true);
            yield return ScrollerRig.Frames(2);

            CollectionAssert.AreEqual(shown, rig.BoundIds());
            foreach (int id in shown)
                Assert.AreSame(slotBefore[id], rig.ItemFor(id), $"Item {id} moved to another slot.");
            Assert.AreEqual(bindsBefore, BindCount(shown), "Nothing changed, so nothing is bound again.");
            Assert.AreEqual(0, ParentWatch.Changes);
        }

        // ---------- P6 option B: pooled slots stay active, parked out of view ----------

        [UnityTest]
        public IEnumerator WithPooledSlotsSwitchedOff_ScrollingReEnablesEachReusedSlot()
        {
            SlotPool.KeepPooledSlotsActive = false;
            yield return CreateQuietList(200);
            yield return rig.ScrollToOffset(3000f);
            yield return rig.ScrollToOffset(0f);
            ParentWatch.Enables = 0;

            for (float offset = 0f; offset <= 5000f; offset += 50f)
                ScrollNow(offset);

            Assert.Greater(ParentWatch.Enables, 0);
        }

        [UnityTest]
        public IEnumerator ByDefault_PooledSlotsStayActive_SoScrollingNeverReEnablesASlot()
        {
            yield return CreateQuietList(200);
            yield return rig.ScrollToOffset(3000f);
            yield return rig.ScrollToOffset(0f);
            ParentWatch.Enables = 0;

            for (float offset = 0f; offset <= 5000f; offset += 50f)
                ScrollNow(offset);

            Assert.AreEqual(0, ParentWatch.Enables, "Reused slots were already active.");
            Assert.AreEqual(0, ParentWatch.Changes);
        }

        [UnityTest]
        public IEnumerator ByDefault_ShownItemsSitWhereTheyBelong_AndTheRestWaitActiveOutOfView()
        {
            yield return CreateQuietList(200);

            for (float offset = 0f; offset <= 3000f; offset += 70f)
                ScrollNow(offset);
            yield return null;

            List<int> shown = rig.BoundIds();
            CollectionAssert.IsNotEmpty(shown);
            foreach (int id in shown)
                Assert.AreEqual(id * 100f, -rig.ItemFor(id).anchoredPosition.y, 0.01f, $"Item {id} is out of place.");

            int waiting = 0;
            foreach (Transform child in rig.Content)
            {
                Assert.IsTrue(child.gameObject.activeSelf, "Pooled slots stay active.");
                if (SlotPool.IsParked((RectTransform)child))
                    waiting++;
            }

            Assert.AreEqual(rig.Content.childCount - shown.Count, waiting, "Every slot not shown waits out of view.");
        }

        // ---------- P11: rebuilds reuse what they can ----------

        [UnityTest]
        public IEnumerator AddingItems_To10000_CreatesOnlyTheNewPlacementsAndLines()
        {
            rig = ScrollerRig.Create(new RigOptions { QuietSlots = true });
            ListSource<TestItem> source = new(ScrollerRig.MakeItems(10_000));
            yield return rig.Initialize(source);
            Work.Reset();

            source.AddItems(ScrollerRig.MakeItems(20, 10_000));
            rig.List.Refresh();

            Assert.AreEqual(20, Work.PlacementsCreated);
            Assert.LessOrEqual(Work.LinesCreated, 20);
            Assert.AreEqual(10_020, rig.List.GetItemCount());
            Assert.AreEqual(10_019 * 100f, rig.List.GetItemOffset(10_019), 0.01f);
        }

        [UnityTest]
        public IEnumerator AppendingAPage_To10000_CreatesOnlyTheNewPlacementsAndLines()
        {
            rig = ScrollerRig.Create(new RigOptions { QuietSlots = true });
            FakePageLoader loader = new() { PageSize = 10_000, Total = 10_020 };
            PagedSource<TestItem> source = new(loader);
            yield return rig.Initialize(source);
            Work.Reset();

            ScrollNow(10_000 * 100f - 800f);
            yield return ScrollerRig.WaitUntil(() => rig.List.GetItemCount() == 10_020, "The second page never arrived");

            Assert.AreEqual(20, Work.PlacementsCreated);
            Assert.LessOrEqual(Work.LinesCreated, 21);
            Assert.AreEqual(10_019 * 100f, rig.List.GetItemOffset(10_019), 0.01f);
        }

        [UnityTest]
        public IEnumerator Refresh_SameItemCount_AllocatesNothing()
        {
            yield return CreateQuietList(10_000);
            rig.List.Refresh();

            Assert.That(() => rig.List.Refresh(), Is.Not.AllocatingGCMemory());
        }

        [UnityTest]
        public IEnumerator Refresh_TwiceAfterGrowing_ReusesEveryPlacementAndLine()
        {
            rig = ScrollerRig.Create(new RigOptions { QuietSlots = true });
            ListSource<TestItem> source = new(ScrollerRig.MakeItems(1000));
            yield return rig.Initialize(source);
            source.RemoveItemAt(999);
            rig.List.Refresh();
            Work.Reset();

            source.AddItem(new TestItem(999));
            rig.List.Refresh();

            Assert.AreEqual(0, Work.PlacementsCreated, "The placement removed before is reused.");
            Assert.AreEqual(0, Work.LinesCreated);
        }

        // ---------- P11–P17: no allocations while scrolling ----------

        [UnityTest]
        public IEnumerator Scrolling_100000Items_AllocatesNothing()
        {
            yield return CreateQuietList(100_000);

            // Once through first: the pool creates the slots it needs and Unity warms its own caches.
            for (int step = 0; step < 120; step++)
                ScrollNow(step * 37f);
            ScrollNow(0f);

            // The check must be able to see an allocation, or passing proves nothing.
            Assert.That(() => new List<int>(16), Is.AllocatingGCMemory());

            Assert.That(() =>
            {
                for (int step = 0; step < 120; step++)
                    ScrollNow(step * 37f);
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
