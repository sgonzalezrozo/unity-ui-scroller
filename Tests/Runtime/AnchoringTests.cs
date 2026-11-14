using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// Phase 8, step 5: when sizes change, what you're looking at stays still.
    /// Items measured for the first time (scrolling up into them) grow upwards; an item whose content changed keeps
    /// its start and pushes the items after it. Moving the content from code mid-drag causes no jump and no speed.
    /// Data: items 0-3 are 300 tall and the rest 100, so the average starts at 300 and drops once deep items are
    /// measured, which resizes every unmeasured item above the viewport.
    /// </summary>
    public class AnchoringTests
    {
        private const float DEEP_OFFSET = 15000f;

        private SlotListRig rig;
        private List<TestItem> items;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private IEnumerator Start(RigDirection direction, bool deep)
        {
            rig = SlotListRig.Create(new RigOptions { ItemSize = -1f, LayoutContent = true, Direction = direction });
            items = SlotListRig.MakeItems(200);
            for (int i = 0; i < items.Count; i++)
                items[i].Height = i < 4 ? 300f : 100f;

            yield return rig.Initialize(new ListSource<TestItem>(items));
            yield return null;

            if (!deep)
                yield break;

            yield return rig.ScrollToOffset(DEEP_OFFSET);
            yield return SlotListRig.Frames(2);
        }

        private bool IsTopToBottom => rig.Options.Direction == RigDirection.TopToBottom;

        /// <summary>How far the list is scrolled from its start.</summary>
        private float Distance => IsTopToBottom ? rig.Content.anchoredPosition.y : -rig.Content.anchoredPosition.y;

        /// <summary>Where an item starts, measured from the viewport's start edge.</summary>
        private float OnScreen(int id)
        {
            float itemOffset = IsTopToBottom ? -rig.ItemFor(id).anchoredPosition.y : rig.ItemFor(id).anchoredPosition.y;
            return itemOffset - Distance;
        }

        private static PointerEventData Pointer(float x, float y)
            => new(null) { button = PointerEventData.InputButton.Left, position = new Vector2(x, y) };

        private IEnumerator ResizeShownItem(int id, float height)
        {
            rig.ItemFor(id).GetComponent<RecordingSlot>().Body.preferredHeight = height;
            Canvas.ForceUpdateCanvases();
            yield return SlotListRig.Frames(3);
        }

        [UnityTest]
        public IEnumerator ScrollingUp_IntoUnmeasuredItems_MovesTheShownItemOnlyByTheScroll(
            [Values(RigDirection.TopToBottom, RigDirection.BottomToTop)] RigDirection direction)
        {
            yield return Start(direction, deep: true);
            float contentBefore = rig.ContentSize;

            // Step up a row at a time until unmeasured rows are reached; the shown item must move by the step only.
            for (int step = 0; step < 30; step++)
            {
                int watched = rig.BoundIds()[1];
                float before = OnScreen(watched);

                yield return rig.ScrollToOffset(Distance - 100f);

                Assert.AreEqual(before + 100f, OnScreen(watched), 0.5f, $"step {step}");
            }

            Assume.That(Mathf.Abs(rig.ContentSize - contentBefore), Is.GreaterThan(1f), "scrolling up should have measured new items");
        }

        [UnityTest]
        public IEnumerator AShownItemThatGrows_KeepsItsStart_AndPushesTheOthers()
        {
            yield return Start(RigDirection.TopToBottom, deep: true);
            int first = rig.BoundIds()[0];
            int next = rig.BoundIds()[1];
            float firstBefore = OnScreen(first);
            float nextBefore = OnScreen(next);

            yield return ResizeShownItem(first, 400f);

            Assert.AreEqual(firstBefore, OnScreen(first), 0.5f);
            Assert.AreEqual(nextBefore + 300f, OnScreen(next), 0.5f);
        }

        [UnityTest]
        public IEnumerator AtTheTop_AGrowingFirstItem_DoesNotScroll()
        {
            yield return Start(RigDirection.TopToBottom, deep: false);

            yield return ResizeShownItem(0, 600f);

            Assert.AreEqual(0f, Distance, 0.5f);
            Assert.AreEqual(0f, OnScreen(0), 0.5f);
        }

        [UnityTest]
        public IEnumerator AShiftDuringADrag_DoesNotJumpOnTheNextDragEvent()
        {
            yield return Start(RigDirection.TopToBottom, deep: true);
            PointerEventData pointer = Pointer(200f, 400f);
            rig.Scroller.OnBeginDrag(pointer);

            // Dragging down by 150 scrolls up into unmeasured items, and measuring them shifts the content.
            pointer.position = new Vector2(200f, 250f);
            rig.Scroller.OnDrag(pointer);
            float afterDragEvent = Distance;
            yield return SlotListRig.Frames(2);
            float afterShift = Distance;
            Assume.That(Mathf.Abs(afterShift - afterDragEvent), Is.GreaterThan(1f), "measuring should have shifted the content");

            rig.Scroller.OnDrag(pointer);

            Assert.AreEqual(afterShift, Distance, 0.5f);
            rig.Scroller.OnEndDrag(pointer);
        }

        [UnityTest]
        public IEnumerator AShiftAfterTheLayoutPass_DuringADrag_AddsNoSpeed()
        {
            yield return Start(RigDirection.TopToBottom, deep: true);
            PointerEventData pointer = Pointer(200f, 400f);
            rig.Scroller.OnBeginDrag(pointer);
            yield return SlotListRig.Frames(2);
            float before = Distance;

            // A much taller item raises the average, which resizes the unmeasured items above and shifts the content.
            yield return ResizeShownItem(rig.BoundIds()[1], 2000f);
            float afterShift = Distance;
            Assume.That(Mathf.Abs(afterShift - before), Is.GreaterThan(1f), "the new average should have shifted the content");

            Assert.Less(rig.Scroller.velocity.magnitude, 1f);
            rig.Scroller.OnDrag(pointer);
            Assert.AreEqual(afterShift, Distance, 0.5f);
            rig.Scroller.OnEndDrag(pointer);
        }
    }
}
