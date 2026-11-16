using NUnit.Framework;

namespace UIScroller.Tests.Editor
{
    /// <summary>The scroll animation's progress: eased, finished after its duration, cancellable.</summary>
    public class ScrollAnimationTests
    {
        [Test]
        public void Progress_EasesOut_AndReachesOneAfterTheDuration()
        {
            ScrollAnimation animation = new();
            animation.Start(100f, 0.5f);

            float half = animation.Advance(0.25f);
            Assert.Greater(half, 0.5f, "ease-out: past half way at half time");
            Assert.Less(half, 1f);
            Assert.IsTrue(animation.IsRunning);

            Assert.AreEqual(1f, animation.Advance(0.25f));
            Assert.IsFalse(animation.IsRunning);
        }

        [Test]
        public void Progress_NeverPassesOne()
        {
            ScrollAnimation animation = new();
            animation.Start(0f, 0.2f);

            Assert.AreEqual(1f, animation.Advance(5f));
        }

        [Test]
        public void Position_GoesFromTheStartToTheTarget()
        {
            ScrollAnimation animation = new();
            animation.Start(100f, 1f);

            Assert.AreEqual(100f, animation.PositionAt(0f, 500f));
            Assert.AreEqual(500f, animation.PositionAt(1f, 500f));
        }

        [Test]
        public void Shift_MovesTheStartToo()
        {
            ScrollAnimation animation = new();
            animation.Start(100f, 1f);

            animation.Shift(-50f);

            Assert.AreEqual(50f, animation.PositionAt(0f, 500f));
        }

        [Test]
        public void Cancel_StopsIt()
        {
            ScrollAnimation animation = new();
            animation.Start(0f, 1f);

            animation.Cancel();

            Assert.IsFalse(animation.IsRunning);
        }
    }
}
