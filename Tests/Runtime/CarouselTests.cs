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

        /// <summary>
        /// Like Unity's Input System UI module: one event object per pointer, reused for every mouse button. After the
        /// left button's drag event it's processed again for the middle button, so by LateUpdate it says Middle.
        /// </summary>
        private IEnumerator InputSystemLikeDrag(float pixelsPerFrame, int frames, int framesAfter = 60)
        {
            PointerEventData shared = new(null) { button = PointerEventData.InputButton.Left, position = new Vector2(200f, 350f) };
            rig.List.OnInitializePotentialDrag(shared);
            rig.List.OnBeginDrag(shared);
            shared.button = PointerEventData.InputButton.Middle;
            for (int i = 0; i < frames; i++)
            {
                shared.button = PointerEventData.InputButton.Left;
                shared.position += new Vector2(0f, pixelsPerFrame);
                rig.List.OnDrag(shared);
                shared.button = PointerEventData.InputButton.Middle;
                yield return null;
                RecordFrame();
            }

            shared.button = PointerEventData.InputButton.Left;
            rig.List.OnEndDrag(shared);
            for (int i = 0; i < framesAfter; i++)
            {
                yield return null;
                RecordFrame();
            }
        }

        [UnityTest]
        public IEnumerator TenThousand_InputSystemDrag_AcrossALoopJump_KeepsANormalSpeed_AndNeverJumps()
        {
            yield return StartTenThousandWithInertia();

            yield return InputSystemLikeDrag(-40f, 20, framesAfter: 0);
            float speedAtRelease = Mathf.Abs(rig.List.velocity.y);
            rig.List.OnEndDrag(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
            for (int i = 0; i < 60; i++)
            {
                yield return null;
                RecordFrame();
            }

            Assert.Less(speedAtRelease, 100_000f, "a 40 px-per-frame drag can't release at millions of px/s");
            Assert.LessOrEqual(largestJump, MAX_ITEMS_PER_FRAME, "the top item jumped in one frame");
        }

        // ---------- Inertia matches a plain ScrollRect ----------

        /// <summary>A plain Unity ScrollRect next to the list: same size, settings, content height and position.</summary>
        private UnityEngine.UI.ScrollRect BuildTwinScrollRect()
        {
            GameObject twinGo = new("TwinScrollRect", typeof(RectTransform));
            RectTransform twinRect = (RectTransform)twinGo.transform;
            twinRect.SetParent(rig.ScrollerRect.parent, false);
            twinRect.sizeDelta = rig.ScrollerRect.sizeDelta;
            twinRect.anchoredPosition = rig.ScrollerRect.anchoredPosition + new Vector2(1000f, 0f);

            RectTransform viewport = (RectTransform)new GameObject("Viewport", typeof(RectTransform)).transform;
            viewport.SetParent(twinRect, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();

            RectTransform content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(viewport, false);
            content.anchorMin = rig.Content.anchorMin;
            content.anchorMax = rig.Content.anchorMax;
            content.pivot = rig.Content.pivot;
            content.sizeDelta = rig.Content.sizeDelta;
            content.anchoredPosition = rig.Content.anchoredPosition;

            UnityEngine.UI.ScrollRect twin = twinGo.AddComponent<UnityEngine.UI.ScrollRect>();
            twin.viewport = viewport;
            twin.content = content;
            twin.horizontal = false;
            twin.vertical = true;
            twin.inertia = rig.List.inertia;
            twin.movementType = rig.List.movementType;
            twin.decelerationRate = rig.List.decelerationRate;
            twin.elasticity = rig.List.elasticity;
            return twin;
        }

        /// <summary>Drags both with the same pointer moves in the same frames, then lets both coast. Returns (list, twin) travel after release.</summary>
        private IEnumerator DragBoth(UnityEngine.UI.ScrollRect twin, float pixelsPerFrame, int frames, int coastFrames, float[] result)
        {
            PointerEventData listPointer = new(null) { button = PointerEventData.InputButton.Left, position = new Vector2(200f, 350f) };
            PointerEventData twinPointer = new(null) { button = PointerEventData.InputButton.Left, position = new Vector2(200f, 350f) };
            rig.List.OnInitializePotentialDrag(listPointer);
            twin.OnInitializePotentialDrag(twinPointer);
            rig.List.OnBeginDrag(listPointer);
            twin.OnBeginDrag(twinPointer);
            for (int i = 0; i < frames; i++)
            {
                listPointer.position += new Vector2(0f, pixelsPerFrame);
                twinPointer.position += new Vector2(0f, pixelsPerFrame);
                rig.List.OnDrag(listPointer);
                twin.OnDrag(twinPointer);
                yield return null;
            }

            result[0] = rig.List.velocity.y;
            result[1] = twin.velocity.y;
            rig.List.OnEndDrag(listPointer);
            twin.OnEndDrag(twinPointer);

            // Travel in items, unwrapped: the list may loop round, the twin just moves.
            float listTravel = 0f;
            float lastDistance = rig.ScrollDistance;
            float twinStart = twin.content.anchoredPosition.y;
            for (int i = 0; i < coastFrames; i++)
            {
                yield return null;
                float now = rig.ScrollDistance;
                float step = now - lastDistance;
                float period = TEN_THOUSAND * 120f;
                if (Mathf.Abs(step) > period * 0.5f)
                    step -= Mathf.Sign(step) * period;
                listTravel += step;
                lastDistance = now;
            }

            result[2] = listTravel;
            result[3] = twin.content.anchoredPosition.y - twinStart;
        }

        [UnityTest]
        public IEnumerator LoopingList_ReleasesAndCoastsLikeAPlainScrollRect([Values(-40f, 40f, -120f)] float pixelsPerFrame)
        {
            yield return StartTenThousandWithInertia();
            UnityEngine.UI.ScrollRect twin = BuildTwinScrollRect();
            yield return null;
            float[] result = new float[4];

            yield return DragBoth(twin, pixelsPerFrame, 15, 40, result);

            // A big loop scrolls a short window, so its position stays small and moves in exact steps: it coasts as far.
            Debug.Log($"[Inertia] drag {pixelsPerFrame}/frame: release speed list {result[0]:F0} vs plain {result[1]:F0}; coasting list {result[2]:F0} px vs plain {result[3]:F0} px");
            Assert.AreEqual(result[1], result[0], Mathf.Abs(result[1]) * 0.01f + 1f, "release speed");
            Assert.AreEqual(result[3], result[2], Mathf.Abs(result[3]) * 0.03f + 1f, "distance coasted after release");
            Object.Destroy(twin.gameObject);
        }

        // ---------- Big loops: a short window, so positions stay small ----------

        private const float SMALL_POSITION = 20_000f;

        [UnityTest]
        public IEnumerator TenThousand_ScrollPositionAndContentStaySmall_WhileFlicking()
        {
            yield return StartTenThousandWithInertia();
            float largest = Mathf.Max(rig.ScrollDistance, rig.List.ContentSize);

            for (int flick = 0; flick < 8; flick++)
            {
                yield return FingerDrag(flick % 2 == 0 ? -150f : 150f, 5, 20);
                largest = Mathf.Max(largest, Mathf.Abs(rig.ScrollDistance));
            }

            Assert.Less(rig.List.ContentSize, SMALL_POSITION, "only a short window of entries is laid out");
            Assert.Less(largest, SMALL_POSITION, "the scroll position stays small, so it moves in exact steps");
            Assert.LessOrEqual(largestJump, MAX_ITEMS_PER_FRAME);
        }

        [UnityTest]
        public IEnumerator TenThousand_StartsOnTheFirstItem_WithTheLastJustAbove()
        {
            yield return StartTenThousandWithInertia();
            rig.List.inertia = false;

            Assert.AreEqual(0, rig.List.VisibleStartIndex);
            yield return Drag(-240f);

            List<int> shown = OnScreenTopToBottom();
            CollectionAssert.AreEqual(new[] { TEN_THOUSAND - 2, TEN_THOUSAND - 1, 0 }, shown.GetRange(0, 3));
        }

        [UnityTest]
        public IEnumerator TenThousand_ScrollingFarEachWay_ShowsTheRightItemsInOrder([Values(1f, -1f)] float direction)
        {
            yield return StartTenThousandWithInertia();
            rig.List.inertia = false;

            // 300 items each way, in steps: many re-centres of the window.
            for (int i = 0; i < 300; i++)
                yield return rig.ScrollToOffset(rig.ScrollDistance + 120f * direction);

            int expected = LoopIndex.Real(Mathf.RoundToInt(300 * direction), TEN_THOUSAND);
            Assert.AreEqual(expected, rig.List.VisibleStartIndex);
            List<int> shown = OnScreenTopToBottom();
            for (int k = 1; k < shown.Count; k++)
                Assert.AreEqual(LoopIndex.Real(shown[k - 1] + 1, TEN_THOUSAND), shown[k], "consecutive items on screen");
        }

        [UnityTest]
        public IEnumerator TenThousand_ScrollToAFarItem_ShowsIt()
        {
            yield return StartTenThousandWithInertia();

            rig.List.ScrollToItem(5000);
            yield return SlotListRig.Frames(2);

            Assert.AreEqual(5000, rig.List.VisibleStartIndex);
            Assert.IsTrue(rig.List.IsItemVisible(5000));
            Assert.Less(Mathf.Abs(rig.ScrollDistance), SMALL_POSITION);
        }

        [UnityTest]
        public IEnumerator TenThousandMeasured_SizesFollowTheRealItems_AcrossRecentres()
        {
            List<TestItem> items = SlotListRig.MakeItems(TEN_THOUSAND);
            for (int i = 0; i < items.Count; i++)
                items[i].Height = i % 3 == 0 ? 200f : 100f;
            rig = SlotListRig.Create(new RigOptions { ViewportSize = new Vector2(400f, 700f), ItemSize = -1f, LayoutContent = true });
            rig.List.inertia = false;
            rig.List.Settings.loop = true;
            yield return rig.Initialize(new ListSource<TestItem>(items));
            yield return SlotListRig.Frames(2);

            for (int i = 0; i < 200; i++)
                yield return rig.ScrollToOffset(rig.ScrollDistance + 90f);
            for (int i = 0; i < 260; i++)
                yield return rig.ScrollToOffset(rig.ScrollDistance - 90f);
            yield return SlotListRig.Frames(3);

            foreach (int id in rig.BoundIds())
                Assert.AreEqual(id % 3 == 0 ? 200f : 100f, rig.ItemFor(id).sizeDelta.y, 0.5f, $"item {id} has its own size");
            List<int> shown = OnScreenTopToBottom();
            for (int k = 1; k < shown.Count; k++)
            {
                RectTransform previous = rig.ItemFor(shown[k - 1]);
                float expectedTop = -previous.anchoredPosition.y + previous.sizeDelta.y;
                Assert.AreEqual(expectedTop, -rig.ItemFor(shown[k]).anchoredPosition.y, 0.5f, $"item {shown[k]} follows item {shown[k - 1]}");
            }
        }
    }
}
