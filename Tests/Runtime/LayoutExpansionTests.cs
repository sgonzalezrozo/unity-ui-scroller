using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// Phase 8, step 8: expanding a slot measured with layout only sets IsExpanded. The slot shows its details and is
    /// measured, so no size is passed or stored. The watcher keeps resizing it while expanded, and expanding or
    /// collapsing never moves what's on screen.
    /// Rig: 100 px bodies, a 200 px details part shown while expanded.
    /// </summary>
    public class LayoutExpansionTests
    {
        private const float EXPANDED = 100f + ScrollerRig.DETAILS_HEIGHT;

        private ScrollerRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private IEnumerator Start(bool allowMultiple = false)
        {
            rig = ScrollerRig.Create(new RigOptions { ItemSize = -1f, LayoutContent = true, Expandable = true, AllowMultipleExpanded = allowMultiple });
            List<TestItem> items = ScrollerRig.MakeItems(100);
            foreach (TestItem item in items)
                item.Height = 100f;
            yield return rig.Initialize(new ListSource<TestItem>(items));
            yield return null;
        }

        private float OnScreen(int id) => -rig.ItemFor(id).anchoredPosition.y - rig.Content.anchoredPosition.y;

        [UnityTest]
        public IEnumerator Expand_WithoutASize_GrowsToItsContent()
        {
            yield return Start();

            rig.Scroller.Expand(2);

            Assert.IsTrue(rig.Scroller.IsExpanded(2));
            Assert.AreEqual(EXPANDED, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(-(200f + EXPANDED), rig.ItemFor(3).anchoredPosition.y, 0.01f);
            Assert.AreEqual(100 * 100f + 200f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator Collapse_ShrinksBackToItsContent()
        {
            yield return Start();
            rig.Scroller.Expand(2);

            rig.Scroller.Collapse(2);

            Assert.IsFalse(rig.Scroller.IsExpanded(2));
            Assert.AreEqual(100f, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(-300f, rig.ItemFor(3).anchoredPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator InSingleMode_ExpandingAnother_CollapsesTheFirst()
        {
            yield return Start();
            rig.Scroller.Expand(1);

            rig.Scroller.Expand(3);

            Assert.AreEqual(100f, rig.ItemFor(1).sizeDelta.y, 0.01f);
            Assert.AreEqual(EXPANDED, rig.ItemFor(3).sizeDelta.y, 0.01f);
            Assert.AreEqual(-300f, rig.ItemFor(3).anchoredPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator AGivenSize_IsIgnored_AndWarnsOnce()
        {
            yield return Start(allowMultiple: true);
            LogAssert.Expect(LogType.Warning, new Regex("measured with layout"));

            rig.Scroller.Expand(2, 999f);
            rig.Scroller.Expand(4, 999f);

            Assert.AreEqual(EXPANDED, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(EXPANDED, rig.ItemFor(4).sizeDelta.y, 0.01f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SetExpandedSize_IsIgnored()
        {
            yield return Start();
            rig.Scroller.Expand(2);
            LogAssert.Expect(LogType.Warning, new Regex("measured with layout"));

            rig.Scroller.SetExpandedSize(2, 999f);

            Assert.AreEqual(EXPANDED, rig.ItemFor(2).sizeDelta.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator DetailsThatChangeSize_ResizeTheExpandedItem()
        {
            yield return Start();
            rig.Scroller.Expand(2);

            rig.ItemFor(2).GetComponent<ExpandableRecordingSlot>().Details.preferredHeight = 400f;
            Canvas.ForceUpdateCanvases();
            yield return ScrollerRig.Frames(2);

            Assert.AreEqual(500f, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(-700f, rig.ItemFor(3).anchoredPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator AnExpandedItem_ScrolledAwayAndBack_IsStillExpanded()
        {
            yield return Start();
            rig.Scroller.Expand(2);

            yield return rig.ScrollToOffset(3000f);
            yield return rig.ScrollToOffset(0f);

            Assert.IsTrue(rig.ItemFor(2).GetComponent<ExpandableRecordingSlot>().IsExpanded);
            Assert.AreEqual(EXPANDED, rig.ItemFor(2).sizeDelta.y, 0.01f);
            Assert.AreEqual(100 * 100f + 200f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator ExpandingTheFirstShownItem_KeepsItsStart()
        {
            yield return Start();
            yield return rig.ScrollToOffset(3000f);
            int first = rig.BoundIds()[0];
            float before = OnScreen(first);

            rig.Scroller.Expand(first);

            Assert.AreEqual(before, OnScreen(first), 0.5f);
            Assert.AreEqual(EXPANDED, rig.ItemFor(first).sizeDelta.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator CollapsingAnItemOffScreen_NeverMovesTheShownItems()
        {
            yield return Start();
            rig.Scroller.Expand(2);
            yield return rig.ScrollToOffset(1500f);

            int watched = rig.BoundIds()[1];
            float before = OnScreen(watched);
            rig.Scroller.Collapse(2);
            Assert.AreEqual(before, OnScreen(watched), 0.5f, "collapsing off screen");

            // Scrolling back up to it, it's measured collapsed and shrinks above the shown items.
            for (int step = 0; step < 15 && rig.Content.anchoredPosition.y > 0f; step++)
            {
                watched = rig.BoundIds()[1];
                before = OnScreen(watched);
                yield return rig.ScrollToOffset(Mathf.Max(0f, rig.Content.anchoredPosition.y - 100f));
                if (rig.Content.anchoredPosition.y > 0f)
                    Assert.AreEqual(before + 100f, OnScreen(watched), 0.5f, $"step {step}");
            }

            Assert.AreEqual(100f, rig.ItemFor(2).sizeDelta.y, 0.01f);
        }
    }
}
