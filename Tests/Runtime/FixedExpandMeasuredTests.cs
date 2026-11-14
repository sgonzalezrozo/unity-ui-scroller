using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// The usual slot: a fixed default size (DefaultExtent > 0) and an expanded size that follows its content.
    /// Expand(index) without a size measures only the expanded slot; Expand(index, size) keeps a fixed expanded size.
    /// Rig: fixed 100 px slots whose content is a 100 px body plus a 200 px details part shown while expanded.
    /// </summary>
    public class FixedExpandMeasuredTests
    {
        private const float EXPANDED = 100f + SlotListRig.DETAILS_HEIGHT;

        private SlotListRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
            ExpandableRecordingSlot.DetailsDelayFrames = 0;
        }

        /// <summary>An opened slot is measured in Unity's layout pass and grows on the next frame.</summary>
        private static IEnumerator SettleOpen() => SlotListRig.Frames(3);

        private IEnumerator Start(bool allowMultiple = false)
        {
            rig = SlotListRig.Create(new RigOptions { ItemSize = 100f, LayoutContent = true, Expandable = true, AllowMultipleExpanded = allowMultiple });
            List<TestItem> items = SlotListRig.MakeItems(100);
            foreach (TestItem item in items)
                item.Height = 100f;
            yield return rig.Initialize(new ListSource<TestItem>(items));
            yield return null;
        }

        private float Top(int id) => -rig.ItemFor(id).anchoredPosition.y;

        [UnityTest]
        public IEnumerator ScrollingCollapsedFixedSlots_MeasuresNothing()
        {
            yield return Start();
            rig.List.Work.Reset();

            yield return rig.ScrollToOffset(2000f);

            Assert.AreEqual(0, rig.List.Work.Measurements);
        }

        [UnityTest]
        public IEnumerator Expand_WithoutASize_MeasuresTheExpandedSlot()
        {
            yield return Start();

            rig.Scroller.Expand(2);
            yield return SettleOpen();

            Assert.AreEqual(EXPANDED, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(200f + EXPANDED, Top(3), 0.01f);
            Assert.AreEqual(100 * 100f + 200f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator Collapse_GoesBackToTheFixedSize()
        {
            yield return Start();
            rig.Scroller.Expand(2);
            yield return SettleOpen();

            rig.Scroller.Collapse(2);
            yield return null;

            Assert.AreEqual(100f, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(300f, Top(3), 0.01f);
            Assert.AreEqual(100 * 100f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator Expand_WithASize_KeepsThatFixedSize()
        {
            yield return Start();

            rig.Scroller.Expand(2, 250f);

            Assert.AreEqual(250f, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(450f, Top(3), 0.01f);
        }

        [UnityTest]
        public IEnumerator AnExpandedSlotWhoseContentGrows_IsResized()
        {
            yield return Start();
            rig.Scroller.Expand(2);
            yield return SettleOpen();

            rig.ItemFor(2).GetComponent<ExpandableRecordingSlot>().Details.preferredHeight = 400f;
            yield return SlotListRig.Frames(3);

            Assert.AreEqual(100f + 400f, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(200f + 500f, Top(3), 0.01f);
        }

        [UnityTest]
        public IEnumerator ExpandingAnother_CollapsesTheFirstBackToItsFixedSize()
        {
            yield return Start();
            rig.Scroller.Expand(2);
            yield return SettleOpen();

            rig.Scroller.Expand(5);
            yield return SettleOpen();

            Assert.AreEqual(100f, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(EXPANDED, rig.ItemFor(5).sizeDelta.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator ASlotReusedAfterShowingTheExpandedItem_StaysFixed()
        {
            yield return Start();
            rig.Scroller.Expand(2);
            yield return SettleOpen();

            yield return rig.ScrollToOffset(3000f);
            yield return SlotListRig.Frames(3);

            foreach (int id in rig.BoundIds())
                Assert.AreEqual(100f, rig.ItemFor(id).sizeDelta.y, 0.01f, $"Item {id} must keep the fixed size.");
        }

        [UnityTest]
        public IEnumerator TheExpandedItem_ScrolledAwayAndBack_KeepsItsMeasuredSize()
        {
            yield return Start();
            rig.Scroller.Expand(2);
            yield return SettleOpen();
            yield return rig.ScrollToOffset(3000f);
            rig.List.Work.Reset();

            yield return rig.ScrollToOffset(0f);
            yield return SettleOpen();

            Assert.AreEqual(EXPANDED, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(0, rig.List.Work.Measurements, "Its measured size is cached like any other.");
        }

        [UnityTest]
        public IEnumerator Expand_KeepsTheFixedSizeInTheFrameOfTheTap_AndGrowsOnTheNext()
        {
            yield return Start();

            rig.Scroller.Expand(2);

            Assert.AreEqual(100f, rig.ItemFor(2).sizeDelta.y, 0.01f, "Measured in Unity's layout pass, not at once.");
            Assert.AreEqual(0, rig.List.Work.Measurements, "No extra layout rebuild.");
            yield return SettleOpen();
            Assert.AreEqual(EXPANDED, rig.ItemFor(2).sizeDelta.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator ContentThatArrivesLate_IsFollowed_AndWhatIsAboveStaysStill()
        {
            ExpandableRecordingSlot.DetailsDelayFrames = 5;
            yield return Start();
            yield return rig.ScrollToOffset(150f);
            float topOf1 = Top(1) - rig.Content.anchoredPosition.y;

            rig.Scroller.Expand(2);
            yield return SettleOpen();
            Assert.AreEqual(100f, rig.ItemFor(2).sizeDelta.y, 0.01f, "Opened, but its details haven't arrived yet.");

            yield return SlotListRig.Frames(6);

            Assert.AreEqual(EXPANDED, rig.ItemFor(2).sizeDelta.y, 0.01f, "The late details resize it.");
            Assert.AreEqual(200f + EXPANDED, Top(3), 0.01f);
            Assert.AreEqual(topOf1, Top(1) - rig.Content.anchoredPosition.y, 0.5f, "Items above it don't move on screen.");
        }

        [UnityTest]
        public IEnumerator ContentSizeChanged_ReportsTheNewSize_AndExpandedCountFollows()
        {
            yield return Start(allowMultiple: true);
            List<float> sizes = new();
            rig.List.ContentSizeChanged += size => sizes.Add(size);

            rig.Scroller.Expand(2);
            rig.Scroller.Expand(5);
            yield return SettleOpen();

            Assert.AreEqual(2, rig.List.ExpandedCount);
            CollectionAssert.IsNotEmpty(sizes);
            Assert.AreEqual(100 * 100f + 2 * 200f, sizes[sizes.Count - 1], 0.01f);
            Assert.AreEqual(rig.ContentSize, sizes[sizes.Count - 1], 0.01f);

            rig.Scroller.Collapse(2);
            Assert.AreEqual(1, rig.List.ExpandedCount);
            Assert.AreEqual(100 * 100f + 200f, sizes[sizes.Count - 1], 0.01f);
        }

        [UnityTest]
        public IEnumerator Scrolling_DoesNotRaiseContentSizeChanged()
        {
            yield return Start();
            int raised = 0;
            rig.List.ContentSizeChanged += _ => raised++;

            yield return rig.ScrollToOffset(2000f);

            Assert.AreEqual(0, raised);
        }
    }
}
