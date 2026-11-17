using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// Nested scrolling: a drag or wheel mostly across a list's axis goes to the nearest parent that handles it, handed
    /// over once through pointerDrag; a drag along the axis, a diagonal one, or one with no such parent stays with the list.
    /// </summary>
    public class NestedScrollingTests
    {
        private const float STEP = 120f;
        private ScrollerRig inner;
        private ScrollerRig outer;
        private GameObject spyObject;

        [TearDown]
        public void TearDown()
        {
            inner?.Dispose();
            outer?.Dispose();
            if (spyObject != null)
                Object.DestroyImmediate(spyObject);
            inner = null;
            outer = null;
        }

        /// <summary>A horizontal list inside a spy parent.</summary>
        private IEnumerator StartInsideSpy(RigDirection direction = RigDirection.LeftToRight)
        {
            inner = ScrollerRig.Create(new RigOptions { Direction = direction, ViewportSize = new Vector2(800f, 400f) });
            spyObject = new GameObject("SpyParent", typeof(RectTransform), typeof(SpyDragParent));
            spyObject.transform.SetParent(inner.ScrollerRect.parent, false);
            inner.ScrollerRect.SetParent(spyObject.transform, false);
            yield return inner.Initialize(50);
        }

        private SpyDragParent Spy => spyObject.GetComponent<SpyDragParent>();

        private static PointerEventData Press(Vector2 movement)
            => new(null)
            {
                button = PointerEventData.InputButton.Left,
                pressPosition = new Vector2(400f, 200f),
                position = new Vector2(400f, 200f) + movement,
            };

        /// <summary>What the input module does: send begin drag to the drag target, then send drag to whatever pointerDrag is now.</summary>
        private static void BeginAndDrag(ScrollerBase list, PointerEventData pointer)
        {
            pointer.pointerDrag = list.gameObject;
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
        }

        [UnityTest]
        public IEnumerator ADragAcrossTheAxis_GoesToTheParent_ForTheWholeGesture()
        {
            yield return StartInsideSpy();
            PointerEventData pointer = Press(new Vector2(10f, STEP));

            BeginAndDrag(inner.List, pointer);

            Assert.AreEqual(spyObject, pointer.pointerDrag, "the input module now sends the gesture to the parent");
            Assert.AreEqual(1, Spy.BeginDrags);
            Assert.AreEqual(1, Spy.Drags);
            Assert.AreEqual(1, Spy.EndDrags);
            Assert.IsFalse(inner.List.IsDragging, "the inner list never started a drag");
        }

        [UnityTest]
        public IEnumerator ADragAlongTheAxis_StaysWithTheList([Values(RigDirection.LeftToRight, RigDirection.RightToLeft)] RigDirection direction)
        {
            yield return StartInsideSpy(direction);
            PointerEventData pointer = Press(new Vector2(-STEP, 10f));
            pointer.pointerDrag = inner.List.gameObject;

            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.beginDragHandler);

            Assert.AreEqual(inner.List.gameObject, pointer.pointerDrag);
            Assert.AreEqual(0, Spy.BeginDrags);
            Assert.IsTrue(inner.List.IsDragging);
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
        }

        [UnityTest]
        public IEnumerator ADiagonalDrag_StaysWithTheList()
        {
            yield return StartInsideSpy();
            PointerEventData pointer = Press(new Vector2(STEP, STEP));
            pointer.pointerDrag = inner.List.gameObject;

            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.beginDragHandler);

            Assert.AreEqual(0, Spy.BeginDrags);
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
        }

        [UnityTest]
        public IEnumerator WithoutAParentThatHandlesDrags_EveryDragStaysWithTheList()
        {
            inner = ScrollerRig.Create(new RigOptions { Direction = RigDirection.LeftToRight, ViewportSize = new Vector2(800f, 400f) });
            yield return inner.Initialize(50);
            PointerEventData pointer = Press(new Vector2(0f, STEP));
            pointer.pointerDrag = inner.List.gameObject;

            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.beginDragHandler);

            Assert.AreEqual(inner.List.gameObject, pointer.pointerDrag);
            Assert.IsTrue(inner.List.IsDragging);
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
        }

        [UnityTest]
        public IEnumerator TurnedOff_EveryDragStaysWithTheList()
        {
            yield return StartInsideSpy();
            inner.List.Settings.passDragsToParent = false;
            PointerEventData pointer = Press(new Vector2(0f, STEP));
            pointer.pointerDrag = inner.List.gameObject;

            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.beginDragHandler);

            Assert.AreEqual(0, Spy.BeginDrags);
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
        }

        [UnityTest]
        public IEnumerator TouchingTheInnerList_AlsoTellsTheParent_SoItsFlingStops()
        {
            yield return StartInsideSpy();

            ExecuteEvents.Execute(inner.List.gameObject, Press(Vector2.zero), ExecuteEvents.initializePotentialDrag);

            Assert.AreEqual(1, Spy.PotentialDrags);
        }

        [UnityTest]
        public IEnumerator AWheelAcrossTheAxis_ScrollsTheParent_AndAWheelAlongItScrollsTheList()
        {
            yield return StartInsideSpy();
            float before = inner.ScrollDistance;

            ExecuteEvents.Execute(inner.List.gameObject, new PointerEventData(null) { scrollDelta = new Vector2(0f, -1f) }, ExecuteEvents.scrollHandler);
            Assert.AreEqual(1, Spy.Scrolls, "a vertical wheel over a horizontal list");
            Assert.AreEqual(before, inner.ScrollDistance, 0.01f);

            ExecuteEvents.Execute(inner.List.gameObject, new PointerEventData(null) { scrollDelta = new Vector2(-1f, 0f) }, ExecuteEvents.scrollHandler);
            yield return null;
            Assert.AreEqual(1, Spy.Scrolls, "a horizontal wheel stays with the list");
            Assert.AreNotEqual(before, inner.ScrollDistance, "the list moved");
        }

        [UnityTest]
        public IEnumerator ACarouselInsideAVerticalList_ADragAcrossItMovesTheOuterList()
        {
            outer = ScrollerRig.Create();
            yield return outer.Initialize(50);
            inner = ScrollerRig.Create(new RigOptions { Direction = RigDirection.LeftToRight, ViewportSize = new Vector2(400f, 150f) });
            inner.ScrollerRect.SetParent(outer.Content, false);
            yield return inner.Initialize(30);

            // An upward drag on the carousel: the outer list scrolls towards its end.
            PointerEventData pointer = Press(new Vector2(5f, STEP));
            pointer.pointerDrag = inner.List.gameObject;
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.beginDragHandler);
            Assert.AreEqual(outer.List.gameObject, pointer.pointerDrag);

            pointer.position += new Vector2(0f, STEP);
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
            yield return null;

            Assert.Greater(outer.ScrollDistance, 50f, "the outer list moved");
            Assert.AreEqual(0f, inner.ScrollDistance, 0.01f, "the carousel didn't");
        }
    }
}
