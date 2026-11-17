using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// Loop with a paged source: the list asks for the first page only, loops over it as the whole list, and loads no
    /// more pages, whatever the scroll. A warning says so once.
    /// </summary>
    public class LoopPagedTests
    {
        private const int PAGE_SIZE = 20;
        private const int TOTAL = 100;
        private ScrollerRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private IEnumerator Start(FakePageLoader loader, bool bento)
        {
            rig = ScrollerRig.Create(new RigOptions
            {
                Bento = bento,
                BentoLayout = bento ? BentoLayout.FromSpans(4, new[] { new BentoSpan(2, 2), BentoSpan.One, BentoSpan.One, BentoSpan.One, BentoSpan.One }) : null,
            });
            rig.List.Settings.loop = true;
            LogAssert.Expect(LogType.Warning, new Regex("Loop turns paging off"));
            yield return rig.Initialize(new PagedSource<TestItem>(loader));
            yield return ScrollerRig.WaitUntil(HasItems, "the first page never arrived");
        }

        private bool HasItems() => rig.List.GetItemCount() > 0;

        [UnityTest]
        public IEnumerator TheFirstPage_LoopsAsTheWholeList_AndNoOtherPageLoads([Values(false, true)] bool bento)
        {
            FakePageLoader loader = new() { PageSize = PAGE_SIZE, Total = TOTAL };
            yield return Start(loader, bento);

            Assert.AreEqual(PAGE_SIZE, rig.List.GetItemCount(), "the first page is the whole list");

            // Far in both directions: the list goes round its 20 items and never asks for page 1.
            for (int i = 0; i < 40; i++)
                yield return rig.ScrollToOffset(rig.ScrollDistance + 150f);
            for (int i = 0; i < 80; i++)
                yield return rig.ScrollToOffset(rig.ScrollDistance - 150f);

            CollectionAssert.AreEqual(new[] { 0 }, loader.Requested);
            Assert.That(rig.List.VisibleStartIndex, Is.InRange(0, PAGE_SIZE - 1));
            foreach (int id in rig.BoundIds())
                Assert.That(id, Is.InRange(0, PAGE_SIZE - 1));
        }

        [UnityTest]
        public IEnumerator ScrollToItem_GoesRoundToAnyItemOfTheFirstPage()
        {
            FakePageLoader loader = new() { PageSize = PAGE_SIZE, Total = TOTAL };
            yield return Start(loader, false);

            rig.List.ScrollToItem(PAGE_SIZE - 1, ScrollAlign.Start);
            yield return ScrollerRig.Frames(2);

            Assert.IsTrue(rig.List.IsItemVisible(PAGE_SIZE - 1));
            CollectionAssert.AreEqual(new[] { 0 }, loader.Requested);
        }
    }
}
