using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// Design tests for Phase 7 paging through the list: the list asks for pages itself, appends them without a
    /// rebuild, keeps one load at a time, stops at the end and cancels loads it no longer needs.
    /// Default rig: 400 x 800, 100 px slots, 8 visible. Default loader: pages of 20, 60 items.
    /// </summary>
    public class PagingTests
    {
        private SlotListRig rig;
        private FakePageLoader loader;
        private PagedSource<TestItem> source;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
            loader = null;
            source = null;
        }

        private IEnumerator StartList(RigOptions options = null)
        {
            rig = SlotListRig.Create(options);
            loader ??= new FakePageLoader();
            source = new PagedSource<TestItem>(loader);
            yield return rig.Initialize(source);
        }

        private static List<int> Range(int first, int last)
            => Enumerable.Range(first, last - first + 1).ToList();

        [UnityTest]
        public IEnumerator AnEmptyPagedSource_LoadsAndShowsTheFirstPage_WithoutRefresh()
        {
            yield return StartList();

            Assert.AreEqual(new[] { 0 }, loader.Requested.ToArray());
            Assert.AreEqual(20, rig.Scroller.GetItemCount());
            Assert.AreEqual(Range(0, 7), rig.BoundIds());
            Assert.AreEqual(2000f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator ScrollingNearTheEnd_AppendsTheNextPage_AndKeepsThePosition()
        {
            yield return StartList();
            int pagesLoaded = 0;
            rig.Scroller.PageLoaded += _ => pagesLoaded++;

            yield return rig.ScrollToOffset(1200f);

            Assert.AreEqual(new[] { 0, 1 }, loader.Requested.ToArray());
            Assert.AreEqual(4000f, rig.ContentSize, 0.01f);
            Assert.AreEqual(Range(12, 19), rig.BoundIds());
            Assert.AreEqual(1, pagesLoaded);
        }

        [UnityTest]
        public IEnumerator RequestingAPage_RaisesPageRequested_AndCallsTheOverride()
        {
            yield return StartList();
            int requested = 0;
            rig.Scroller.PageRequested += () => requested++;

            yield return rig.ScrollToOffset(1200f);

            Assert.AreEqual(1, requested);
            Assert.AreEqual(2, rig.Scroller.PaginationCalls, "page 0 at start, page 1 near the end");
        }

        [UnityTest]
        public IEnumerator OnlyOnePageLoadsAtATime()
        {
            yield return StartList();
            loader.Hold = true;

            yield return rig.ScrollToOffset(1200f);
            yield return rig.ScrollToOffset(1100f);
            yield return rig.ScrollToOffset(1200f);

            Assert.AreEqual(new[] { 0, 1 }, loader.Requested.ToArray());

            loader.Release();
            yield return null;
            Assert.AreEqual(40, rig.Scroller.GetItemCount());
        }

        [UnityTest]
        public IEnumerator AtTheEnd_StopsAsking_AndRaisesReachedEndOnce()
        {
            yield return StartList();
            int reachedEnd = 0;
            rig.Scroller.ReachedEnd += () => reachedEnd++;

            for (int i = 0; i < 6; i++)
                yield return rig.ScrollToOffset(rig.ContentSize - 800f);

            Assert.AreEqual(new[] { 0, 1, 2 }, loader.Requested.ToArray());
            Assert.AreEqual(60, rig.Scroller.GetItemCount());
            Assert.AreEqual(1, reachedEnd);
            Assert.AreEqual(Range(52, 59), rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator AnAppendedPage_KeepsExpandedItems()
        {
            yield return StartList(new RigOptions { Expandable = true });
            rig.Scroller.Expand(2, 300f);
            yield return null;

            yield return rig.ScrollToOffset(1400f);

            Assert.IsTrue(rig.Scroller.IsExpanded(2));
            Assert.AreEqual(4000f + 200f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator ChangingTheSource_DuringALoad_CancelsIt_AndIgnoresTheOldPage()
        {
            yield return StartList();
            loader.Hold = true;
            yield return rig.ScrollToOffset(1200f);

            rig.Scroller.SetSource(new ListSource<TestItem>(SlotListRig.MakeItems(5, 100)));
            yield return null;
            loader.Release();
            yield return null;

            Assert.IsTrue(loader.LastToken.IsCancellationRequested);
            Assert.AreEqual(Range(100, 104), rig.BoundIds());
            Assert.AreEqual(5, rig.Scroller.GetItemCount());
        }

        [UnityTest]
        public IEnumerator AFailedPage_RaisesPageFailed_AndKeepsTheItems()
        {
            loader = new FakePageLoader { FailOnPage = 1 };
            yield return StartList();
            Exception failure = null;
            rig.Scroller.PageFailed += e => failure = e;

            yield return rig.ScrollToOffset(1200f);

            Assert.IsInstanceOf<InvalidOperationException>(failure);
            Assert.AreEqual(20, rig.Scroller.GetItemCount());
            Assert.IsFalse(source.IsLoading);
        }

        [UnityTest]
        public IEnumerator ResettingTheSource_ShowsTheFirstPageAgain_FromTheStart()
        {
            yield return StartList();
            yield return rig.ScrollToOffset(1200f);

            source.Reset();
            yield return null;
            yield return null;

            Assert.AreEqual(new[] { 0, 1, 0 }, loader.Requested.ToArray());
            Assert.AreEqual(20, rig.Scroller.GetItemCount());
            Assert.AreEqual(Range(0, 7), rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator InAGrid_AnAppendedPage_FillsThePartlyFilledLastLine()
        {
            loader = new FakePageLoader { PageSize = 10 };
            yield return StartList(new RigOptions { SlotsPerLine = 3 });

            RectTransform ten = rig.ItemFor(10);
            RectTransform eleven = rig.ItemFor(11);

            Assert.IsNotNull(ten);
            Assert.IsNotNull(eleven);
            Assert.AreEqual(-300f, ten.anchoredPosition.y, 0.01f);
            Assert.AreEqual(rig.ItemFor(9).anchoredPosition.y, ten.anchoredPosition.y, 0.01f);
        }

        // ---------- A page that lands during the bounce at the end ----------

        /// <summary>
        /// The list is pulled past the end and the elastic bounce is taking it back, at <paramref name="speed"/> px/s
        /// along the list (negative is towards the start), when the next page lands.
        /// </summary>
        private IEnumerator LandAPageWhileMoving(RigDirection direction, float overscroll, float speed)
        {
            rig = SlotListRig.Create(new RigOptions { Direction = direction });
            rig.List.inertia = true;
            rig.List.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
            loader = new FakePageLoader { PageSize = 20, Total = 60 };
            source = new PagedSource<TestItem>(loader);
            yield return rig.Initialize(source);
            loader.Hold = true;

            float max = rig.List.ContentSize - rig.List.ViewportSize;
            yield return rig.ScrollToOffset(max + overscroll);
            Assert.AreEqual(2, loader.Requested.Count, "the second page is loading");

            float towardsEnd = direction == RigDirection.TopToBottom ? 1f : -1f;
            rig.List.velocity = new Vector2(0f, speed * towardsEnd);
            loader.Release();
            yield return SlotListRig.WaitUntil(() => rig.List.GetItemCount() == 40, "second page");
        }

        [UnityTest]
        public IEnumerator APageThatLandsDuringTheBounceBack_StopsIt_SoTheListDoesNotDriftToTheStart(
            [Values(RigDirection.TopToBottom, RigDirection.BottomToTop)] RigDirection direction)
        {
            yield return LandAPageWhileMoving(direction, overscroll: 25f, speed: -245f);
            yield return null;

            Assert.AreEqual(0f, rig.List.velocity.y, 0.01f, "the bounce back is stopped");
        }

        [UnityTest]
        public IEnumerator APageThatLandsWhileMovingTowardsTheEnd_KeepsTheMovement(
            [Values(RigDirection.TopToBottom, RigDirection.BottomToTop)] RigDirection direction)
        {
            yield return LandAPageWhileMoving(direction, overscroll: 25f, speed: 600f);
            yield return null;

            float towardsEnd = direction == RigDirection.TopToBottom ? 1f : -1f;
            Assert.Greater(rig.List.velocity.y * towardsEnd, 100f, "still moving on into the new items");
        }
    }
}
