using NUnit.Framework;

namespace UIScroller.Tests.Editor
{
    /// <summary>The looping list's index and re-centring maths.</summary>
    public class LoopIndexTests
    {
        [TestCase(0, 0)]
        [TestCase(4, 4)]
        [TestCase(5, 0)]
        [TestCase(13, 3)]
        [TestCase(-1, 4)]
        [TestCase(-6, 4)]
        public void Real_WrapsVirtualIndices_IncludingNegativeOnes(int virtualIndex, int expected)
            => Assert.AreEqual(expected, LoopIndex.Real(virtualIndex, 5));

        [Test]
        public void Real_WithNoItems_IsMinusOne()
            => Assert.AreEqual(-1, LoopIndex.Real(3, 0));

        [TestCase(1, 12, 11)]
        [TestCase(4, 11, 9)]
        [TestCase(0, 14, 15)]
        [TestCase(2, 12, 12)]
        public void NearestCopy_TakesTheShortestWayRound(int real, int around, int expected)
            => Assert.AreEqual(expected, LoopIndex.NearestCopy(real, 5, around));

        [Test]
        public void Copies_AreAtLeastFour_AndCoverTheViewport()
        {
            Assert.AreEqual(4, LoopIndex.Copies(800f, 2000f));
            Assert.AreEqual(6, LoopIndex.Copies(800f, 300f));
            Assert.AreEqual(4, LoopIndex.Copies(800f, 0f));
        }

        // One copy is 1000 px, 5 copies (5000 px), viewport 800: the viewport stays within [1000, 3200].
        [TestCase(2000f, 0f)]
        [TestCase(1000f, 0f)]
        [TestCase(3200f, 0f)]
        [TestCase(999f, 1000f)]
        [TestCase(10f, 1000f)]
        [TestCase(3201f, -1000f)]
        [TestCase(4200f, -1000f)]
        public void Recentre_JumpsByWholeCopies_OnlyNearTheEnds(float distance, float expected)
            => Assert.AreEqual(expected, LoopIndex.Recentre(distance, 800f, 0f, 1000f, 5));

        [Test]
        public void Recentre_AfterAJump_IsInsideAndShowsTheSameItems()
        {
            const float period = 1000f;
            for (float distance = 0f; distance <= 4200f; distance += 37f)
            {
                float shift = LoopIndex.Recentre(distance, 800f, 0f, period, 5);
                float after = distance + shift;

                Assert.AreEqual(0f, LoopIndex.Recentre(after, 800f, 0f, period, 5), "inside after one jump, from " + distance);
                Assert.AreEqual(0f, shift % period, 0.001f, "a whole number of copies, from " + distance);
            }
        }

        [Test]
        public void Recentre_CountsFromWhereTheFirstCopyStarts()
            => Assert.AreEqual(1000f, LoopIndex.Recentre(1010f, 800f, 20f, 1000f, 5));
    }
}
