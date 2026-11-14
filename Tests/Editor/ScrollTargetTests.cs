using NUnit.Framework;

namespace SlotList.Tests.Editor
{
    /// <summary>
    /// Where ScrollToItem puts the list for each alignment, as a scroll distance from the start, clamped to the content.
    /// Setup: 800 px viewport, 5000 px content (max distance 4200), 100 px items.
    /// </summary>
    public class ScrollTargetTests
    {
        private const float VIEWPORT = 800f;
        private const float CONTENT = 5000f;

        private static float Target(float itemStart, ScrollAlign align, float current = 0f, float offset = 0f, float itemSize = 100f, float content = CONTENT)
            => ScrollTargets.For(itemStart, itemSize, VIEWPORT, content, current, align, offset);

        [Test]
        public void Start_PutsTheItemAtTheViewportStart()
            => Assert.AreEqual(1000f, Target(1000f, ScrollAlign.Start));

        [Test]
        public void Center_PutsTheItemsMiddleAtTheViewportsMiddle()
            => Assert.AreEqual(650f, Target(1000f, ScrollAlign.Center));

        [Test]
        public void End_PutsTheItemAtTheViewportEnd()
            => Assert.AreEqual(300f, Target(1000f, ScrollAlign.End));

        [Test]
        public void Offset_LeavesAGapFromTheAlignedEdge()
        {
            Assert.AreEqual(980f, Target(1000f, ScrollAlign.Start, offset: 20f));
            Assert.AreEqual(320f, Target(1000f, ScrollAlign.End, offset: 20f));
            Assert.AreEqual(630f, Target(1000f, ScrollAlign.Center, offset: 20f));
        }

        [Test]
        public void Targets_AreClampedToTheContent()
        {
            Assert.AreEqual(0f, Target(100f, ScrollAlign.Center));
            Assert.AreEqual(0f, Target(0f, ScrollAlign.Start, offset: 50f));
            Assert.AreEqual(4200f, Target(4900f, ScrollAlign.Start));
        }

        [Test]
        public void ContentSmallerThanTheViewport_NeverMoves()
            => Assert.AreEqual(0f, Target(300f, ScrollAlign.Start, content: 500f));

        [Test]
        public void Nearest_DoesntMove_WhenTheItemIsFullyVisible()
            => Assert.AreEqual(950f, Target(1000f, ScrollAlign.Nearest, current: 950f));

        [Test]
        public void Nearest_AlignsToTheStart_WhenTheItemIsBefore()
            => Assert.AreEqual(1000f, Target(1000f, ScrollAlign.Nearest, current: 1050f));

        [Test]
        public void Nearest_AlignsToTheEnd_WhenTheItemIsAfter()
            => Assert.AreEqual(300f, Target(1000f, ScrollAlign.Nearest, current: 250f));

        [Test]
        public void Nearest_WithAnItemBiggerThanTheViewport_AlignsToTheStart()
            => Assert.AreEqual(1000f, Target(1000f, ScrollAlign.Nearest, current: 0f, itemSize: 1200f));

        [Test]
        public void Nearest_KeepsTheGap_WhenItMoves()
        {
            Assert.AreEqual(980f, Target(1000f, ScrollAlign.Nearest, current: 1050f, offset: 20f));
            Assert.AreEqual(320f, Target(1000f, ScrollAlign.Nearest, current: 250f, offset: 20f));
        }
    }
}
