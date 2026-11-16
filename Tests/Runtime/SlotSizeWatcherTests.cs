using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// Phase 8, step 4: a measured slot that changes size after it was bound (a late localized string, an image
    /// that finished loading) is resized and the slots after it move. The list applies it after Unity's layout pass.
    /// Canvas.ForceUpdateCanvases runs that layout pass here, because batch mode never renders.
    /// </summary>
    public class SlotSizeWatcherTests
    {
        private ScrollerRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private IEnumerator StartLayoutSized(float height)
        {
            rig = ScrollerRig.Create(new RigOptions { ItemSize = -1f, LayoutContent = true });
            List<TestItem> items = ScrollerRig.MakeItems(20);
            foreach (TestItem item in items)
                item.Height = height;
            yield return rig.Initialize(new ListSource<TestItem>(items));
            yield return null;
        }

        private static IEnumerator LayoutPassThenFrames(int frames)
        {
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < frames; i++)
                yield return null;
        }

        [UnityTest]
        public IEnumerator AMeasuredSlot_GetsAWatcher()
        {
            yield return StartLayoutSized(50f);

            Assert.IsNotNull(rig.ItemFor(0).GetComponent<SlotSizeWatcher>());
        }

        [UnityTest]
        public IEnumerator AFixedSizeSlot_GetsNoWatcher()
        {
            rig = ScrollerRig.Create(new RigOptions { ItemSize = 100f, LayoutContent = true });
            yield return rig.Initialize(20);

            Assert.IsNull(rig.ItemFor(0).GetComponent<SlotSizeWatcher>());
        }

        [UnityTest]
        public IEnumerator ContentThatGrowsAfterBinding_ResizesTheSlot_AndMovesTheOthers()
        {
            yield return StartLayoutSized(50f);

            rig.ItemFor(0).GetComponent<RecordingSlot>().Body.preferredHeight = 300f;
            yield return LayoutPassThenFrames(2);

            Assert.AreEqual(300f, rig.ItemFor(0).sizeDelta.y, 0.01f);
            Assert.AreEqual(-300f, rig.ItemFor(1).anchoredPosition.y, 0.01f);
            // Items not measured yet take the new average, so the content grows by at least the 250 px the slot grew.
            Assert.GreaterOrEqual(rig.ContentSize, 20 * 50f + 250f);
        }

        [UnityTest]
        public IEnumerator ContentThatShrinks_ResizesTheSlot()
        {
            yield return StartLayoutSized(200f);

            rig.ItemFor(1).GetComponent<RecordingSlot>().Body.preferredHeight = 40f;
            yield return LayoutPassThenFrames(2);

            Assert.AreEqual(40f, rig.ItemFor(1).sizeDelta.y, 0.01f);
            Assert.AreEqual(-240f, rig.ItemFor(2).anchoredPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator AResize_SettlesWithoutLoopsOrErrors()
        {
            yield return StartLayoutSized(50f);
            rig.ItemFor(0).GetComponent<RecordingSlot>().Body.preferredHeight = 300f;
            yield return LayoutPassThenFrames(2);
            float contentAfterResize = rig.ContentSize;

            yield return LayoutPassThenFrames(2);
            yield return LayoutPassThenFrames(2);

            Assert.AreEqual(contentAfterResize, rig.ContentSize, 0.01f);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
