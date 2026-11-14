using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// Phase 8, step 3: a slot whose DefaultExtent is 0 or less is measured with Unity's layout when it's bound.
    /// A positive DefaultExtent is a fixed size and nothing is measured.
    /// Rig: 400 x 800 viewport; the slot's body gets each item's Height through a VerticalLayoutGroup.
    /// </summary>
    public class LayoutSizingTests
    {
        private SlotListRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private static List<TestItem> Items(int count, params float[] heights)
        {
            List<TestItem> items = SlotListRig.MakeItems(count);
            for (int i = 0; i < items.Count; i++)
                items[i].Height = heights[i % heights.Length];
            return items;
        }

        private IEnumerator StartLayoutSized(List<TestItem> items, uint slotsPerLine = 1)
        {
            rig = SlotListRig.Create(new RigOptions { ItemSize = -1f, LayoutContent = true, SlotsPerLine = slotsPerLine });
            yield return rig.Initialize(new ListSource<TestItem>(items));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Slots_AreAsTallAsTheirContent_AndFollowEachOther()
        {
            yield return StartLayoutSized(Items(20, 50f, 150f, 250f));

            Assert.AreEqual(50f, rig.ItemFor(0).sizeDelta.y, 0.01f);
            Assert.AreEqual(150f, rig.ItemFor(1).sizeDelta.y, 0.01f);
            Assert.AreEqual(250f, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(-50f, rig.ItemFor(1).anchoredPosition.y, 0.01f);
            Assert.AreEqual(-200f, rig.ItemFor(2).anchoredPosition.y, 0.01f);
            Assert.AreEqual(-450f, rig.ItemFor(3).anchoredPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator UnmeasuredItems_UseTheAverageOfTheMeasuredOnes()
        {
            yield return StartLayoutSized(Items(100, 200f));

            Assert.AreEqual(Enumerable.Range(0, 4).ToList(), rig.BoundIds());
            Assert.AreEqual(100 * 200f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator InAGrid_EachLineIsAsTallAsItsTallestSlot()
        {
            yield return StartLayoutSized(Items(30, 50f, 150f, 100f), slotsPerLine: 3);

            Assert.AreEqual(150f, rig.ItemFor(0).sizeDelta.y, 0.01f);
            Assert.AreEqual(150f, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(-150f, rig.ItemFor(3).anchoredPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator Refresh_MeasuresChangedItemsAgain()
        {
            List<TestItem> items = Items(20, 50f);
            yield return StartLayoutSized(items);

            items[0].Height = 300f;
            rig.Scroller.Refresh();
            yield return null;

            Assert.AreEqual(300f, rig.ItemFor(0).sizeDelta.y, 0.01f);
            Assert.AreEqual(-300f, rig.ItemFor(1).anchoredPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator APositiveDefaultExtent_IsAFixedSize_EvenWithALayoutGroup()
        {
            rig = SlotListRig.Create(new RigOptions { ItemSize = 100f, LayoutContent = true });
            yield return rig.Initialize(new ListSource<TestItem>(Items(20, 300f)));
            yield return null;

            Assert.AreEqual(100f, rig.ItemFor(0).sizeDelta.y, 0.01f);
            Assert.AreEqual(-100f, rig.ItemFor(1).anchoredPosition.y, 0.01f);
            Assert.AreEqual(2000f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator APrefabWithoutALayout_KeepsItsOwnSize_AndWarnsOnce()
        {
            // DefaultExtent <= 0 but the root reports no preferred size (no layout group): measuring gives 0.
            rig = SlotListRig.Create(new RigOptions { ItemSize = -1f, LayoutContent = false });
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("preferred size"));

            yield return rig.Initialize(new ListSource<TestItem>(Items(20, 300f)));
            yield return null;

            Assert.AreEqual(100f, rig.ItemFor(0).sizeDelta.y, 0.01f);
            Assert.AreEqual(-100f, rig.ItemFor(1).anchoredPosition.y, 0.01f);
            Assert.AreEqual(2000f, rig.ContentSize, 0.01f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ScrollingDown_MeasuresTheNewlyShownItems()
        {
            yield return StartLayoutSized(Items(100, 200f, 100f));

            yield return rig.ScrollToOffset(3000f);

            int first = rig.BoundIds()[0];
            Assert.AreEqual(first % 2 == 0 ? 200f : 100f, rig.ItemFor(first).sizeDelta.y, 0.01f);
            Assert.AreEqual(100 * 150f, rig.ContentSize, 0.01f);
        }

        // ---------- Measured sizes are cached ----------

        [UnityTest]
        public IEnumerator AMeasuredItem_ScrolledBackIn_IsNotMeasuredAgain()
        {
            yield return StartLayoutSized(Items(200, 100f));
            yield return rig.ScrollToOffset(3000f);
            rig.List.Work.Reset();

            yield return rig.ScrollToOffset(0f);

            Assert.AreEqual(0, rig.List.Work.Measurements, "Items measured before keep their cached size.");
            Assert.AreEqual(100f, rig.ItemFor(2).sizeDelta.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator AnItemThatChangedOffScreen_IsResizedSoonAfterItIsShown()
        {
            List<TestItem> items = Items(200, 100f);
            yield return StartLayoutSized(items);
            yield return rig.ScrollToOffset(3000f);

            items[2].Height = 300f;
            yield return rig.ScrollToOffset(0f);
            yield return SlotListRig.Frames(3);

            Assert.AreEqual(300f, rig.ItemFor(2).sizeDelta.y, 0.01f, "The size watcher catches what the cache missed.");
            float top2 = -rig.ItemFor(2).anchoredPosition.y;
            Assert.AreEqual(top2 + 300f, -rig.ItemFor(3).anchoredPosition.y, 0.01f, "The items after it move down.");
        }

        [UnityTest]
        public IEnumerator InvalidateSize_OffScreen_MeasuresTheItemAsSoonAsItIsShown()
        {
            List<TestItem> items = Items(200, 100f);
            yield return StartLayoutSized(items);
            yield return rig.ScrollToOffset(3000f);
            items[2].Height = 300f;
            rig.List.InvalidateSize(2);
            rig.List.Work.Reset();

            yield return rig.ScrollToOffset(0f);

            Assert.GreaterOrEqual(rig.List.Work.Measurements, 1);
            Assert.AreEqual(300f, rig.ItemFor(2).sizeDelta.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator Scrolling100000MeasuredItems_UpdatesPlaceholdersOnlyWhileTheAverageMoves()
        {
            yield return StartLayoutSized(Items(100_000, 100f, 200f));
            rig.List.Work.Reset();

            for (int screen = 1; screen <= 30; screen++)
                yield return rig.ScrollToOffset(screen * 800f);

            // Each pass visits all 100,000 items; it should stop once the average of measured sizes settles near 150.
            Assert.LessOrEqual(rig.List.Work.PlaceholderPasses, 10);
        }
    }
}
