using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// ScrollToItem with alignment and animation, and snapping after a drag. Rig: 400 x 800 viewport, 100 px items,
    /// 50 items (5000 px, max distance 4200). Animations run on a fixed 0.1 s per frame.
    /// </summary>
    public class ScrollNavigationTests
    {
        private const float FRAME = 0.1f;

        private ScrollerRig rig;
        private readonly List<int> snapped = new();

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
            snapped.Clear();
        }

        private IEnumerator Start(RigOptions options = null, SnapMode snap = SnapMode.None, SnapAlign align = SnapAlign.Start)
        {
            rig = ScrollerRig.Create(options);
            rig.List.frameDeltaTime = () => FRAME;
            rig.List.Settings.snap = snap;
            rig.List.Settings.snapAlign = align;
            rig.List.SnappedTo += index => snapped.Add(index);
            yield return rig.Initialize(50);
        }

        private static PointerEventData Pointer()
            => new(null) { button = PointerEventData.InputButton.Left, position = new Vector2(200f, 400f) };

        /// <summary>A drag that doesn't move the pointer, released with <paramref name="speed"/> along the list.</summary>
        private void DragAndRelease(Vector2 velocity = default)
        {
            PointerEventData pointer = Pointer();
            rig.List.OnBeginDrag(pointer);
            rig.List.velocity = velocity;
            rig.List.OnEndDrag(pointer);
        }

        // ---------- ScrollToItem ----------

        [UnityTest]
        public IEnumerator ScrollToItem_AlignsTheItem_AtOnce()
        {
            yield return Start();

            rig.List.ScrollToItem(20);
            Assert.AreEqual(2000f, rig.ScrollDistance, 0.01f, "Start");

            rig.List.ScrollToItem(20, ScrollAlign.Center);
            Assert.AreEqual(1650f, rig.ScrollDistance, 0.01f, "Center");

            rig.List.ScrollToItem(20, ScrollAlign.End);
            Assert.AreEqual(1300f, rig.ScrollDistance, 0.01f, "End");

            rig.List.ScrollToItem(20, ScrollAlign.Start, offset: 30f);
            Assert.AreEqual(1970f, rig.ScrollDistance, 0.01f, "Start with a gap");
            Assert.IsTrue(rig.List.IsItemVisible(20));
        }

        [UnityTest]
        public IEnumerator ScrollToItem_Nearest_DoesntMove_WhenTheItemIsVisible()
        {
            yield return Start();
            yield return rig.ScrollToOffset(1000f);

            rig.List.ScrollToItem(14, ScrollAlign.Nearest);
            Assert.AreEqual(1000f, rig.ScrollDistance, 0.01f);

            rig.List.ScrollToItem(25, ScrollAlign.Nearest);
            Assert.AreEqual(1800f, rig.ScrollDistance, 0.01f, "item 25 ends at the viewport end");
        }

        [UnityTest]
        public IEnumerator ScrollToItem_RightToLeft_AlignsTheItemAtTheStart()
        {
            yield return Start(new RigOptions { Direction = RigDirection.RightToLeft, ViewportSize = new Vector2(800f, 400f) });

            rig.List.ScrollToItem(20, ScrollAlign.Center);

            Assert.AreEqual(1650f, rig.ScrollDistance, 0.01f);
            Assert.IsTrue(rig.List.IsItemVisible(20));
        }

        [UnityTest]
        public IEnumerator ScrollToItem_Animated_MovesOverTheDuration_AndLandsOnTheItem()
        {
            yield return Start();

            rig.List.ScrollToItem(30, ScrollAlign.Start, 0.5f);
            Assert.AreEqual(0f, rig.ScrollDistance, 0.01f, "nothing moves before the first frame");
            Assert.IsTrue(rig.List.IsScrolling);

            yield return null;
            float afterOneFrame = rig.ScrollDistance;
            Assert.Greater(afterOneFrame, 0f);
            Assert.Less(afterOneFrame, 3000f);

            yield return ScrollerRig.Frames(6);
            Assert.AreEqual(3000f, rig.ScrollDistance, 0.01f);
            Assert.IsFalse(rig.List.IsScrolling);
            Assert.IsTrue(rig.List.IsItemVisible(30));
        }

        [UnityTest]
        public IEnumerator ScrollToEnd_Animated_ReachesTheEnd_AndScrollToStartComesBack()
        {
            yield return Start();

            rig.List.ScrollToEnd(0.3f);
            yield return ScrollerRig.Frames(5);
            Assert.AreEqual(4200f, rig.ScrollDistance, 0.01f);
            Assert.IsTrue(rig.List.IsLastItemVisible);

            rig.List.ScrollToStart(0.3f);
            yield return ScrollerRig.Frames(5);
            Assert.AreEqual(0f, rig.ScrollDistance, 0.01f);
        }

        [UnityTest]
        public IEnumerator ADrag_CancelsTheAnimation()
        {
            yield return Start();
            rig.List.ScrollToItem(30, ScrollAlign.Start, 1f);
            yield return null;

            PointerEventData pointer = Pointer();
            rig.List.OnBeginDrag(pointer);
            float whenDragged = rig.ScrollDistance;
            yield return ScrollerRig.Frames(12);

            Assert.AreEqual(whenDragged, rig.ScrollDistance, 0.01f);
            rig.List.OnEndDrag(pointer);
        }

        [UnityTest]
        public IEnumerator ForceStopScrolling_CancelsTheAnimation()
        {
            yield return Start();
            rig.List.ScrollToItem(30, ScrollAlign.Start, 1f);
            yield return null;

            rig.List.ForceStopScrolling();
            float whenStopped = rig.ScrollDistance;
            yield return ScrollerRig.Frames(12);

            Assert.AreEqual(whenStopped, rig.ScrollDistance, 0.01f);
            Assert.IsFalse(rig.List.IsScrolling);
        }

        // ---------- Snapping ----------

        [UnityTest]
        public IEnumerator SnapItem_Start_SettlesOnTheNearestItem_AndSaysSoOnce()
        {
            yield return Start(snap: SnapMode.Item);
            yield return rig.ScrollToOffset(1240f);

            DragAndRelease();
            yield return ScrollerRig.Frames(5);

            Assert.AreEqual(1200f, rig.ScrollDistance, 0.01f);
            CollectionAssert.AreEqual(new[] { 12 }, snapped);
        }

        [UnityTest]
        public IEnumerator SnapItem_Center_CentresTheNearestItem()
        {
            yield return Start(snap: SnapMode.Item, align: SnapAlign.Center);
            yield return rig.ScrollToOffset(1290f);

            DragAndRelease();
            yield return ScrollerRig.Frames(5);

            Assert.AreEqual(1250f, rig.ScrollDistance, 0.01f);
            CollectionAssert.AreEqual(new[] { 16 }, snapped);
        }

        [UnityTest]
        public IEnumerator SnapItem_AlreadyAligned_SaysSoWithoutMoving()
        {
            yield return Start(snap: SnapMode.Item);
            yield return rig.ScrollToOffset(700f);

            DragAndRelease();

            Assert.AreEqual(700f, rig.ScrollDistance, 0.01f);
            CollectionAssert.AreEqual(new[] { 7 }, snapped);
        }

        [UnityTest]
        public IEnumerator SnapItem_WaitsForAFlingToSlowDown()
        {
            yield return Start(snap: SnapMode.Item);
            rig.List.frameDeltaTime = null;
            rig.List.inertia = true;
            rig.List.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
            yield return rig.ScrollToOffset(1000f);

            DragAndRelease(new Vector2(0f, 3000f));
            Assert.IsEmpty(snapped, "too fast to snap at release");

            // Inertia slows down in real time; batch mode runs frames much faster than 60 fps.
            float giveUpAt = Time.unscaledTime + 5f;
            while (snapped.Count == 0 && Time.unscaledTime < giveUpAt)
                yield return null;
            yield return ScrollerRig.Frames(30);

            Assert.AreEqual(1, snapped.Count);
            Assert.Greater(rig.ScrollDistance, 1000f);
            Assert.AreEqual(snapped[0] * 100f, rig.ScrollDistance, 0.01f);
        }

        [UnityTest]
        public IEnumerator SnapPage_SlowRelease_SettlesOnTheNearestPage()
        {
            yield return Start(snap: SnapMode.Page);
            yield return rig.ScrollToOffset(1000f);

            DragAndRelease();
            yield return ScrollerRig.Frames(5);

            Assert.AreEqual(800f, rig.ScrollDistance, 0.01f);
            CollectionAssert.AreEqual(new[] { 8 }, snapped);
        }

        [UnityTest]
        public IEnumerator SnapPage_Flick_MovesOnePageFromWhereTheDragStarted()
        {
            yield return Start(snap: SnapMode.Page);
            yield return rig.ScrollToOffset(1000f);

            DragAndRelease(new Vector2(0f, 1000f));
            yield return ScrollerRig.Frames(5);

            Assert.AreEqual(1600f, rig.ScrollDistance, 0.01f);
            CollectionAssert.AreEqual(new[] { 16 }, snapped);
        }

        [UnityTest]
        public IEnumerator SnapNone_DoesNothing()
        {
            yield return Start();
            yield return rig.ScrollToOffset(1240f);

            DragAndRelease();
            yield return ScrollerRig.Frames(5);

            Assert.AreEqual(1240f, rig.ScrollDistance, 0.01f);
            Assert.IsEmpty(snapped);
        }
    }
}
