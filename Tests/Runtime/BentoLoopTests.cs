using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// Looping bento lists: the layout starts again with every copy of the items, so jumping by whole copies is
    /// invisible. Rows are 100 px (4 columns in a 400 px list). The layout is a 2-row featured block of 4 tiles, then
    /// rows of four 1 x 1 tiles; 40 items take 11 rows (1,100 px) per copy.
    /// </summary>
    public class BentoLoopTests
    {
        private const int COUNT = 40;
        private const float COPY = 1100f;
        private const float STEP = 100f;
        private const float TOLERANCE = 0.5f;
        private ScrollerRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private static BentoLayout Featured()
            => BentoLayout.FromPatterns(4, new[]
            {
                new BentoPattern(2, new[] { new BentoTile(0, 0, 2, 2), new BentoTile(2, 0, 2, 1), new BentoTile(2, 1, 1, 1), new BentoTile(3, 1, 1, 1) }),
                new BentoPattern(1, new[] { new BentoTile(0, 0, 1, 1), new BentoTile(1, 0, 1, 1), new BentoTile(2, 0, 1, 1), new BentoTile(3, 0, 1, 1) })
            }, repeatFrom: 1);

        private IEnumerator Start(RigDirection direction = RigDirection.TopToBottom, int count = COUNT)
        {
            bool vertical = direction is RigDirection.TopToBottom or RigDirection.BottomToTop;
            rig = ScrollerRig.Create(new RigOptions
            {
                Direction = direction,
                ViewportSize = vertical ? new Vector2(400f, 800f) : new Vector2(800f, 400f),
                Bento = true,
                BentoLayout = Featured(),
            });
            rig.List.Settings.loop = true;
            yield return rig.Initialize(count);
        }

        /// <summary>Where each shown item is along the scroll axis, in world space.</summary>
        private Dictionary<int, float> Positions()
        {
            Dictionary<int, float> positions = new();
            foreach (int id in rig.BoundIds())
            {
                Vector3 world = rig.ItemFor(id).position;
                positions[id] = rig.IsVertical ? world.y : world.x;
            }

            return positions;
        }

        private IEnumerator ScrollBy(float distance, int steps)
        {
            for (int i = 0; i < steps; i++)
                yield return rig.ScrollToOffset(rig.ScrollDistance + distance);
        }

        [UnityTest]
        public IEnumerator ScrollingOneCopyOn_ShowsTheSameTilesInTheSamePlaces(
            [Values(RigDirection.TopToBottom, RigDirection.BottomToTop, RigDirection.LeftToRight, RigDirection.RightToLeft)] RigDirection direction)
        {
            yield return Start(direction);
            yield return ScrollBy(STEP, 3);
            Dictionary<int, float> before = Positions();

            // One copy further on, through at least one jump back by whole copies.
            yield return ScrollBy(STEP, Mathf.RoundToInt(COPY / STEP));
            Dictionary<int, float> after = Positions();

            Assert.Greater(before.Count, 8);
            foreach (KeyValuePair<int, float> item in before)
            {
                Assert.IsTrue(after.ContainsKey(item.Key), $"item {item.Key} is shown again");
                Assert.AreEqual(item.Value, after[item.Key], TOLERANCE, $"item {item.Key} in the same place");
            }
        }

        [UnityTest]
        public IEnumerator ScrollingFarBothWays_KeepsShowingRealItems()
        {
            yield return Start();

            yield return ScrollBy(STEP * 3f, 30);
            Assert.That(rig.List.VisibleStartIndex, Is.InRange(0, COUNT - 1));
            CollectionAssert.IsNotEmpty(rig.BoundIds());

            yield return ScrollBy(-STEP * 3f, 60);
            Assert.That(rig.List.VisibleStartIndex, Is.InRange(0, COUNT - 1));
            foreach (int id in rig.BoundIds())
                Assert.That(id, Is.InRange(0, COUNT - 1));
        }

        [UnityTest]
        public IEnumerator ScrollToItem_ReachesAnyItem_AndSaysItIsVisible()
        {
            yield return Start();

            rig.List.ScrollToItem(37, ScrollAlign.Start);
            yield return ScrollerRig.Frames(2);
            Assert.IsTrue(rig.List.IsItemVisible(37));
            Assert.IsNotNull(rig.ItemFor(37));

            rig.List.ScrollToItem(2, ScrollAlign.Center);
            yield return ScrollerRig.Frames(2);
            Assert.IsTrue(rig.List.IsItemVisible(2));
        }

        [UnityTest]
        public IEnumerator EachCopyStartsTheLayoutAgain_SoTheFeaturedTileComesBack()
        {
            yield return Start();

            // Item 0 is the 2 x 2 featured tile in every copy.
            rig.List.ScrollToItem(0, ScrollAlign.Start);
            yield return ScrollerRig.Frames(2);
            RectTransform featured = rig.ItemFor(0);
            Assert.IsNotNull(featured);
            Assert.AreEqual(200f, featured.rect.height, TOLERANCE);
            Assert.AreEqual(200f, featured.rect.width, TOLERANCE);
        }

        private const float FRAME = 0.1f;
        private const float FAST_STEP = 330f;

        /// <summary>A drag that doesn't move the pointer, released with <paramref name="velocity"/>.</summary>
        private void DragAndRelease(Vector2 velocity)
        {
            PointerEventData pointer = new(null) { button = PointerEventData.InputButton.Left, position = new Vector2(200f, 400f) };
            rig.List.OnBeginDrag(pointer);
            rig.List.velocity = velocity;
            rig.List.OnEndDrag(pointer);
        }

        [UnityTest]
        public IEnumerator AFastMoveAcrossTheJump_ThenARelease_SnapsOnATileStart([Values(1f, -1f)] float direction)
        {
            yield return Start();
            rig.List.frameDeltaTime = FixedFrame;
            rig.List.Settings.snap = SnapMode.Item;
            List<int> snapped = new();
            rig.List.SnappedTo += snapped.Add;

            // Big steps, as a fling moves, through at least one jump by whole copies, ending between two rows.
            yield return ScrollBy(FAST_STEP * direction, 8);
            yield return rig.ScrollToOffset(rig.ScrollDistance + 40f);

            DragAndRelease(Vector2.zero);
            yield return ScrollerRig.Frames(10);

            Assert.AreEqual(1, snapped.Count, "settled once");
            Assert.That(snapped[0], Is.InRange(0, COUNT - 1));
            Assert.AreEqual(0f, Mathf.Repeat(rig.ScrollDistance + TOLERANCE, STEP), TOLERANCE * 2f, "on a row where a tile starts");
            Assert.IsTrue(rig.List.IsItemVisible(snapped[0]));
        }

        [UnityTest]
        public IEnumerator ADragThroughTheJump_MovesSmoothly()
        {
            yield return Start();
            PointerEventData pointer = new(null) { button = PointerEventData.InputButton.Left, position = new Vector2(200f, 400f), pressPosition = new Vector2(200f, 400f) };
            rig.List.OnBeginDrag(pointer);
            Dictionary<int, float> before = Positions();

            // Drag one copy towards the end in small steps: every step moves the content by the step, jump or not.
            for (int step = 0; step < Mathf.RoundToInt(COPY / STEP); step++)
            {
                float start = rig.ScrollDistance;
                pointer.position += new Vector2(0f, STEP);
                rig.List.OnDrag(pointer);
                yield return ScrollerRig.Frames(1);
                float moved = Mathf.Repeat(rig.ScrollDistance - start, COPY);
                Assert.AreEqual(STEP, moved, TOLERANCE, $"step {step}");
            }

            rig.List.OnEndDrag(pointer);
            Dictionary<int, float> after = Positions();
            foreach (KeyValuePair<int, float> item in before)
            {
                if (after.ContainsKey(item.Key))
                    Assert.AreEqual(item.Value, after[item.Key], TOLERANCE, $"item {item.Key} one copy later");
            }
        }

        private static float FixedFrame() => FRAME;

        [UnityTest]
        public IEnumerator ABentoLoopTooLongForCopies_WarnsAndDoesntLoop()
        {
            LogAssert.Expect(LogType.Warning, new Regex("too long"));
            yield return Start(count: 5_000);

            yield return rig.ScrollToOffset(0f);
            Assert.AreEqual(0, rig.List.VisibleStartIndex, "a plain bento list starts at its first item");
            Assert.AreEqual(0f, rig.ScrollDistance, 0.01f);
        }
    }
}
