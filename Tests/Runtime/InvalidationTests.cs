using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// Phase 8, step 7: InvalidateSize and InvalidateAllSizes remeasure the items on screen at once and the others when
    /// they're shown, keeping the first shown item still. A width change invalidates every size (text rewraps).
    /// The tests change a slot's content without a layout pass, so only the invalidation can notice it.
    /// </summary>
    public class InvalidationTests
    {
        private SlotListRig rig;
        private List<TestItem> items;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private IEnumerator Start(float height, int count = 100, float itemSize = -1f)
        {
            rig = SlotListRig.Create(new RigOptions { ItemSize = itemSize, LayoutContent = true });
            items = SlotListRig.MakeItems(count);
            foreach (TestItem item in items)
                item.Height = height;
            yield return rig.Initialize(new ListSource<TestItem>(items));
            yield return null;
        }

        /// <summary>Changes the item's height in the data and in the slot on screen, without a layout pass.</summary>
        private void ChangeContent(int id, float height)
        {
            items[id].Height = height;
            RectTransform slot = rig.ItemFor(id);
            if (slot != null)
                slot.GetComponent<RecordingSlot>().Body.preferredHeight = height;
        }

        private void ChangeAllContent(float height)
        {
            for (int i = 0; i < items.Count; i++)
                ChangeContent(i, height);
        }

        private float OnScreen(int id) => -rig.ItemFor(id).anchoredPosition.y - rig.Content.anchoredPosition.y;

        [UnityTest]
        public IEnumerator InvalidateSize_RemeasuresAShownItem_AtOnce()
        {
            yield return Start(50f);
            ChangeContent(2, 300f);

            rig.Scroller.InvalidateSize(2);

            Assert.AreEqual(300f, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(-400f, rig.ItemFor(3).anchoredPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator InvalidateSize_OfTheFirstShownItem_KeepsItsStart()
        {
            yield return Start(100f);
            yield return rig.ScrollToOffset(3050f);
            int first = rig.BoundIds()[0];
            int next = rig.BoundIds()[1];
            float firstBefore = OnScreen(first);
            float nextBefore = OnScreen(next);
            ChangeContent(first, 300f);

            rig.Scroller.InvalidateSize(first);

            Assert.AreEqual(firstBefore, OnScreen(first), 0.5f);
            Assert.AreEqual(nextBefore + 200f, OnScreen(next), 0.5f);
        }

        [UnityTest]
        public IEnumerator InvalidateAllSizes_RemeasuresShownItems_AndTheOthersTakeTheirAverage()
        {
            yield return Start(50f);
            ChangeAllContent(100f);

            rig.Scroller.InvalidateAllSizes();

            Assert.AreEqual(100f, rig.ItemFor(0).sizeDelta.y, 0.01f);
            Assert.AreEqual(-100f, rig.ItemFor(1).anchoredPosition.y, 0.01f);
            Assert.AreEqual(100 * 100f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator InvalidateAllSizes_KeepsTheFirstShownItemStill()
        {
            // Row 30 starts exactly at the viewport's start: it keeps that start while shrinking, so it stays on screen.
            yield return Start(100f);
            yield return rig.ScrollToOffset(3000f);
            int first = rig.BoundIds()[0];
            float before = OnScreen(first);
            ChangeAllContent(40f);

            rig.Scroller.InvalidateAllSizes();

            Assert.AreEqual(before, OnScreen(first), 0.5f);
            Assert.AreEqual(40f, rig.ItemFor(first).sizeDelta.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator AnInvalidatedItemOffScreen_IsMeasuredWhenShown()
        {
            yield return Start(50f);
            ChangeContent(60, 250f);

            rig.Scroller.InvalidateAllSizes();
            rig.Scroller.ScrollToItem(60);
            yield return SlotListRig.Frames(2);

            Assert.AreEqual(250f, rig.ItemFor(60).sizeDelta.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator AWidthChange_RemeasuresTheShownItems()
        {
            yield return Start(50f);
            ChangeAllContent(120f);

            rig.ScrollerRect.sizeDelta = new Vector2(300f, 800f);
            yield return SlotListRig.Frames(3);

            Assert.AreEqual(300f, rig.ItemFor(0).sizeDelta.x, 0.01f);
            Assert.AreEqual(120f, rig.ItemFor(0).sizeDelta.y, 0.01f);
            Assert.AreEqual(-120f, rig.ItemFor(1).anchoredPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator FixedSizes_IgnoreInvalidation()
        {
            yield return Start(300f, itemSize: 100f);

            rig.Scroller.InvalidateSize(2);
            rig.Scroller.InvalidateAllSizes();

            Assert.AreEqual(100f, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(100 * 100f, rig.ContentSize, 0.01f);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
