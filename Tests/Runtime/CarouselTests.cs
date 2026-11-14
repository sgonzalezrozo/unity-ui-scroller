using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// The looping list (Settings.loop). Rig: 400 x 800 viewport, 100 px items, 5 items by default, so one copy is
    /// 500 px. Animations run on a fixed 0.1 s per frame.
    /// </summary>
    public class CarouselTests
    {
        private const int COUNT = 5;
        private const float ITEM = 100f;

        private SlotListRig rig;
        private readonly List<int> snapped = new();

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
            snapped.Clear();
        }

        private IEnumerator Start(int count = COUNT, RigOptions options = null, SnapMode snap = SnapMode.None, SnapAlign align = SnapAlign.Start)
        {
            rig = SlotListRig.Create(options);
            rig.List.frameDeltaTime = () => 0.1f;
            rig.List.Settings.loop = true;
            rig.List.Settings.snap = snap;
            rig.List.Settings.snapAlign = align;
            rig.List.SnappedTo += index => snapped.Add(index);
            yield return rig.Initialize(count);
        }

        private float MaxDistance => rig.List.ContentSize - rig.List.ViewportSize;

        [UnityTest]
        public IEnumerator StartsOnTheFirstItem_WithRealIndices_AndNoEnds()
        {
            yield return Start();

            Assert.AreEqual(0, rig.List.VisibleStartIndex);
            Assert.AreEqual(COUNT, rig.List.GetItemCount());
            Assert.IsFalse(rig.List.IsFirstItemVisible);
            Assert.IsFalse(rig.List.IsLastItemVisible);
            Assert.Greater(rig.ScrollDistance, 0f, "there's room to scroll back");
            Assert.IsNotNull(rig.ItemFor(0));
            Assert.IsNotNull(rig.ItemFor(4));
        }

        [UnityTest]
        public IEnumerator ScrollingThreeRoundsEachWay_NeverReachesAnEnd_AndShowsTheRightItems([Values(1f, -1f)] float direction)
        {
            yield return Start();
            float moved = 0f;
            const float step = 50f;

            for (int i = 0; i < 3 * COUNT * ITEM / step; i++)
            {
                yield return rig.ScrollToOffset(rig.ScrollDistance + step * direction);
                moved += step * direction;

                Assert.Greater(rig.ScrollDistance, 0f, "step " + i);
                Assert.Less(rig.ScrollDistance, MaxDistance, "step " + i);

                int expectedFirst = LoopIndex.Real(Mathf.FloorToInt(moved / ITEM + 0.001f), COUNT);
                Assert.AreEqual(expectedFirst, rig.List.VisibleStartIndex, "step " + i);
                Assert.IsNotNull(rig.ItemFor(expectedFirst), "a slot shows item " + expectedFirst + " at step " + i);
            }
        }

        [UnityTest]
        public IEnumerator Recentring_KeepsTheSameItemAtTheSamePlace()
        {
            yield return Start();
            float before = rig.ScrollDistance;

            // Back past the start of the first copy: the list jumps a whole copy forward.
            yield return rig.ScrollToOffset(before - 30f);
            Assert.AreEqual(before - 30f + COUNT * ITEM, rig.ScrollDistance, 0.01f, "jumped one copy");
            Assert.AreEqual(4, rig.List.VisibleStartIndex);

            yield return rig.ScrollToOffset(rig.ScrollDistance + 30f);
            Assert.AreEqual(0, rig.List.VisibleStartIndex);
            Assert.AreEqual(0f, Mathf.Repeat(rig.ScrollDistance, COUNT * ITEM), 0.01f, "item 0 back at the top");
        }

        [UnityTest]
        public IEnumerator ScrollToItem_TakesTheShortestWayRound()
        {
            yield return Start();

            // From item 0, item 4 is one item back, not four forward.
            rig.List.ScrollToItem(4, ScrollAlign.Start, 0.5f);
            yield return null;

            Assert.AreEqual(4, rig.List.VisibleStartIndex);

            yield return SlotListRig.Frames(6);
            Assert.AreEqual(4, rig.List.VisibleStartIndex);
            Assert.AreEqual(0f, Mathf.Repeat(rig.ScrollDistance, ITEM), 0.01f);
            Assert.IsTrue(rig.List.IsItemVisible(4));
        }

        [UnityTest]
        public IEnumerator ScrollToItem_AtOnce_AlignsTheItem()
        {
            yield return Start();

            rig.List.ScrollToItem(2, ScrollAlign.Center);

            // Item 2's middle at the viewport's middle: 350 px of items above it, so item 3 (two back) is cut at the top.
            Assert.AreEqual(50f, Mathf.Repeat(rig.ScrollDistance, ITEM), 0.01f);
            Assert.AreEqual(3, rig.List.VisibleStartIndex);
            Assert.IsTrue(rig.List.IsItemVisible(2));
        }

        [UnityTest]
        public IEnumerator SnapCenter_SettlesOnARealItem_AcrossARecentre()
        {
            yield return Start(snap: SnapMode.Item, align: SnapAlign.Center);
            yield return rig.ScrollToOffset(rig.ScrollDistance - 140f);

            PointerEventData pointer = new(null) { button = PointerEventData.InputButton.Left, position = new Vector2(200f, 400f) };
            rig.List.OnBeginDrag(pointer);
            rig.List.OnEndDrag(pointer);
            yield return SlotListRig.Frames(5);

            Assert.AreEqual(1, snapped.Count);
            Assert.That(snapped[0], Is.InRange(0, COUNT - 1));
            Assert.AreEqual(50f, Mathf.Repeat(rig.ScrollDistance, ITEM), 0.01f, "an item's middle is at the viewport's middle");
        }

        [UnityTest]
        public IEnumerator Refresh_WithMoreItems_KeepsLooping()
        {
            ListSource<TestItem> source = new(SlotListRig.MakeItems(COUNT));
            rig = SlotListRig.Create();
            rig.List.Settings.loop = true;
            yield return rig.Initialize(source);

            source.AddItems(SlotListRig.MakeItems(3, COUNT));
            rig.List.Refresh();
            yield return null;

            Assert.AreEqual(8, rig.List.GetItemCount());
            Assert.AreEqual(0, rig.List.VisibleStartIndex, "the same item stays at the top");
            for (int i = 0; i < 20; i++)
                yield return rig.ScrollToOffset(rig.ScrollDistance + 100f);

            Assert.Greater(rig.ScrollDistance, 0f);
            Assert.Less(rig.ScrollDistance, MaxDistance);
            Assert.AreEqual(LoopIndex.Real(20, 8), rig.List.VisibleStartIndex);
        }

        [UnityTest]
        public IEnumerator Expand_IsIgnored_WithAWarning()
        {
            yield return Start(options: new RigOptions { Expandable = true });

            LogAssert.Expect(LogType.Warning, new Regex("doesn't expand items"));
            rig.List.Expand(1, 250f);

            Assert.IsFalse(rig.List.IsExpanded(1));
        }

        [UnityTest]
        public IEnumerator Grid_IsRejected_WithAnError()
        {
            LogAssert.Expect(LogType.Error, new Regex("Loop doesn't work with a grid"));
            yield return Start(count: 10, options: new RigOptions { SlotsPerLine = 2 });

            Assert.IsTrue(rig.List.IsFirstItemVisible, "a plain grid, with a start");
            Assert.AreEqual(500f, rig.List.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator Sections_AreRejected_WithAnError()
        {
            rig = SlotListRig.Create(new RigOptions { Sectioned = true });
            rig.List.Settings.loop = true;

            LogAssert.Expect(LogType.Error, new Regex("Loop doesn't work with sections"));
            yield return rig.InitializeSections(SlotListRig.MakeSections(3, 3));

            Assert.IsTrue(rig.List.IsFirstItemVisible);
        }

        // ---------- The owner's case: 20 items, 7 on screen, scrolled by dragging ----------

        private const int TWENTY = 20;

        private IEnumerator StartTwentyWithSevenVisible()
        {
            rig = SlotListRig.Create(new RigOptions { ViewportSize = new Vector2(400f, 700f) });
            rig.List.inertia = false;
            rig.List.Settings.loop = true;
            yield return rig.Initialize(TWENTY);
            yield return null;
        }

        /// <summary>The items whose slot is inside the viewport, from top to bottom (0-based ids).</summary>
        private List<int> OnScreenTopToBottom()
        {
            List<(float top, int id)> shown = new();
            foreach (int id in rig.BoundIds())
            {
                float top = -rig.ItemFor(id).anchoredPosition.y - rig.ScrollDistance;
                if (top > -ITEM + 0.5f && top < rig.Viewport.rect.height - 0.5f)
                    shown.Add((top, id));
            }

            shown.Sort((a, b) => a.top.CompareTo(b.top));
            List<int> ids = new();
            foreach ((float _, int id) in shown)
                ids.Add(id);
            return ids;
        }

        /// <summary>Drags by <paramref name="pixels"/> in steps (positive scrolls forward, negative toward the start).</summary>
        private IEnumerator Drag(float pixels)
        {
            const float step = 50f;
            PointerEventData pointer = new(null) { button = PointerEventData.InputButton.Left, position = new Vector2(200f, 350f) };
            rig.List.OnBeginDrag(pointer);
            int steps = Mathf.RoundToInt(Mathf.Abs(pixels) / step);
            for (int i = 0; i < steps; i++)
            {
                // Moving the pointer up scrolls a top-to-bottom list forward.
                pointer.position += new Vector2(0f, step * Mathf.Sign(pixels));
                rig.List.OnDrag(pointer);
                yield return null;
            }

            rig.List.OnEndDrag(pointer);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwentyItems_StartsWithTheFirstSeven()
        {
            yield return StartTwentyWithSevenVisible();

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6 }, OnScreenTopToBottom());
        }

        [UnityTest]
        public IEnumerator TwentyItems_DraggingTowardTheStart_ShowsTheLastItems()
        {
            yield return StartTwentyWithSevenVisible();

            yield return Drag(-300f);

            CollectionAssert.AreEqual(new[] { 17, 18, 19, 0, 1, 2, 3 }, OnScreenTopToBottom());
        }

        [UnityTest]
        public IEnumerator TwentyItems_DraggingTowardTheStart_GoesRoundAgainAndAgain()
        {
            yield return StartTwentyWithSevenVisible();

            yield return Drag(-TWENTY * ITEM * 2f - 300f);

            CollectionAssert.AreEqual(new[] { 17, 18, 19, 0, 1, 2, 3 }, OnScreenTopToBottom(), "two full rounds back, then three items more");
            Assert.Greater(rig.ScrollDistance, 0f, "never reached an end");
        }

        [UnityTest]
        public IEnumerator TwentyItems_DraggingForwardPastTheLast_ComesBackToTheFirst()
        {
            yield return StartTwentyWithSevenVisible();

            yield return Drag(TWENTY * ITEM - 300f);
            CollectionAssert.AreEqual(new[] { 17, 18, 19, 0, 1, 2, 3 }, OnScreenTopToBottom(), "item 20 is followed by item 1");

            yield return Drag(TWENTY * ITEM * 2f);
            CollectionAssert.AreEqual(new[] { 17, 18, 19, 0, 1, 2, 3 }, OnScreenTopToBottom(), "two more rounds forward");
            Assert.Less(rig.ScrollDistance, rig.List.ContentSize - rig.List.ViewportSize, "never reached an end");
        }

        // ---------- Dragging with inertia across a loop jump (10,000 items, the Shop's settings) ----------

        private const int TEN_THOUSAND = 10_000;
        private const int MAX_ITEMS_PER_FRAME = 60;

        private int largestJump;
        private int previousTop = -1;

        private IEnumerator StartTenThousandWithInertia()
        {
            rig = SlotListRig.Create(new RigOptions { ViewportSize = new Vector2(400f, 700f), LineSpacing = 20f });
            rig.List.inertia = true;
            rig.List.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
            rig.List.Settings.loop = true;
            yield return rig.Initialize(TEN_THOUSAND);
            yield return null;
            largestJump = 0;
            previousTop = rig.List.VisibleStartIndex;
        }

        /// <summary>How far the top item moved since the last frame, the shortest way round the loop.</summary>
        private void RecordFrame()
        {
            int top = rig.List.VisibleStartIndex;
            int moved = Mathf.Abs(top - previousTop);
            moved = Mathf.Min(moved, TEN_THOUSAND - moved);
            largestJump = Mathf.Max(largestJump, moved);
            previousTop = top;
        }

        /// <summary>A finger drag: one pointer move per frame, then release and let inertia run.</summary>
        private IEnumerator FingerDrag(float pixelsPerFrame, int frames, int framesAfter = 90)
        {
            PointerEventData pointer = new(null) { button = PointerEventData.InputButton.Left, position = new Vector2(200f, 350f) };
            rig.List.OnInitializePotentialDrag(pointer);
            rig.List.OnBeginDrag(pointer);
            for (int i = 0; i < frames; i++)
            {
                pointer.position += new Vector2(0f, pixelsPerFrame);
                rig.List.OnDrag(pointer);
                yield return null;
                RecordFrame();
            }

            rig.List.OnEndDrag(pointer);
            for (int i = 0; i < framesAfter; i++)
            {
                yield return null;
                RecordFrame();
            }
        }

        [UnityTest]
        public IEnumerator TenThousand_DragTowardTheStart_WithInertia_NeverJumps()
        {
            yield return StartTenThousandWithInertia();

            yield return FingerDrag(-40f, 20);

            Assert.LessOrEqual(largestJump, MAX_ITEMS_PER_FRAME, "the top item jumped in one frame");
        }

        [UnityTest]
        public IEnumerator TenThousand_FastFlicksBothWays_WithInertia_NeverJump()
        {
            yield return StartTenThousandWithInertia();

            for (int flick = 0; flick < 6; flick++)
                yield return FingerDrag(flick % 2 == 0 ? -120f : 120f, 6, 30);

            Assert.LessOrEqual(largestJump, MAX_ITEMS_PER_FRAME, "the top item jumped in one frame");
        }

        [UnityTest]
        public IEnumerator TenThousand_ManyFlicksTowardTheStart_WithInertia_NeverJump()
        {
            yield return StartTenThousandWithInertia();

            for (int flick = 0; flick < 8; flick++)
                yield return FingerDrag(-150f, 5, 20);

            Assert.LessOrEqual(largestJump, MAX_ITEMS_PER_FRAME, "the top item jumped in one frame");
        }
    }
}
