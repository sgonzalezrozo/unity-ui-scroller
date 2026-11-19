using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// Starting a list before it can show anything: a viewport with no size yet (a parent not laid out), and a list whose
    /// GameObject is still inactive. Nothing is shown at the wrong size, and no call is lost.
    /// </summary>
    public class StartupTests
    {
        private ScrollerRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        [UnityTest]
        public IEnumerator AViewportWithNoWidth_ShowsNoSlots_UntilItHasOne([Values(false, true)] bool measured)
        {
            rig = ScrollerRig.Create(new RigOptions { ViewportSize = new Vector2(0f, 800f), ItemSize = measured ? -1f : 100f, LayoutContent = measured });
            rig.Scroller.Initialize(new ListSource<TestItem>(ScrollerRig.MakeItems(20)));
            yield return ScrollerRig.Frames(2);

            Assert.IsTrue(rig.List.IsInitialized, "the list is initialized, waiting for a size");
            CollectionAssert.IsEmpty(rig.BoundIds(), "no slot shown at a zero width");

            rig.ScrollerRect.sizeDelta = new Vector2(400f, 800f);
            yield return ScrollerRig.Frames(2);

            CollectionAssert.IsNotEmpty(rig.BoundIds());
            RectTransform first = rig.ItemFor(0);
            Assert.IsNotNull(first);
            Assert.AreEqual(400f, first.rect.width, 0.5f, "the first slot is as wide as the viewport");
        }

        [UnityTest]
        public IEnumerator AViewportWithANegativeWidth_ShowsNoSlots_UntilItHasOne()
        {
            // A stretched list with 40 px margins inside a parent that has no width yet is -80 px wide.
            rig = ScrollerRig.Create(new RigOptions { ViewportSize = new Vector2(-80f, 800f), ItemSize = -1f, LayoutContent = true });
            rig.Scroller.Initialize(new ListSource<TestItem>(ScrollerRig.MakeItems(20)));
            yield return ScrollerRig.Frames(2);

            CollectionAssert.IsEmpty(rig.BoundIds());

            rig.ScrollerRect.sizeDelta = new Vector2(400f, 800f);
            yield return ScrollerRig.Frames(2);

            Assert.AreEqual(400f, rig.ItemFor(0).rect.width, 0.5f);
        }

        [UnityTest]
        public IEnumerator Initialize_OnAnInactiveList_StartsWhenItIsEnabled_WithoutAWarning()
        {
            rig = ScrollerRig.Create();
            rig.ScrollerRect.gameObject.SetActive(false);

            rig.Scroller.Initialize(new ListSource<TestItem>(ScrollerRig.MakeItems(20)));
            Assert.IsFalse(rig.List.IsInitialized);

            rig.ScrollerRect.gameObject.SetActive(true);
            yield return ScrollerRig.Frames(2);

            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(rig.List.IsInitialized);
            CollectionAssert.IsNotEmpty(rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator SetSource_TwiceWhileInactive_StartsWithTheLastSource()
        {
            rig = ScrollerRig.Create();
            rig.ScrollerRect.gameObject.SetActive(false);

            rig.Scroller.SetSource(new ListSource<TestItem>(ScrollerRig.MakeItems(5)));
            rig.Scroller.SetSource(new ListSource<TestItem>(ScrollerRig.MakeItems(5, 100)));

            rig.ScrollerRect.gameObject.SetActive(true);
            yield return ScrollerRig.Frames(2);

            Assert.AreEqual(100, rig.BoundIds()[0]);
        }

        [UnityTest]
        public IEnumerator SetSource_OnAnInactiveSectionedList_StartsWhenItIsEnabled()
        {
            rig = ScrollerRig.Create(new RigOptions { Sectioned = true });
            rig.ScrollerRect.gameObject.SetActive(false);

            rig.Sectioned.SetSource(ScrollerRig.MakeSections(3, 3));
            Assert.IsFalse(rig.List.IsInitialized);

            rig.ScrollerRect.gameObject.SetActive(true);
            yield return ScrollerRig.Frames(2);

            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(rig.List.IsInitialized);
        }
    }
}
