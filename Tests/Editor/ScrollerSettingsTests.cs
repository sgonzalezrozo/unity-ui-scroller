using NUnit.Framework;

namespace UIScroller.Tests.Editor
{
    /// <summary>Design tests for ScrollerSettings (Phase 5, step 1). Defaults match the old serialized field defaults.</summary>
    public class ScrollerSettingsTests
    {
        [Test]
        public void Defaults_MatchTheOldFieldDefaults()
        {
            ScrollerSettings settings = new();

            Assert.AreEqual(ScrollerDirection.TopToBottom, settings.direction);
            Assert.AreEqual(0f, settings.lineSpacing);
            Assert.AreEqual(0f, settings.slotSpacing);
            Assert.AreEqual(0f, settings.paddingStart);
            Assert.AreEqual(0f, settings.paddingEnd);
            Assert.AreEqual(1u, settings.slotsPerLine);
            Assert.IsFalse(settings.autoSlotsPerLine);
            Assert.AreEqual(240f, settings.targetSlotSize);
            Assert.AreEqual(1u, settings.minSlotsPerLine);
            Assert.AreEqual(8u, settings.maxSlotsPerLine);
            Assert.IsFalse(settings.allowMultipleExpanded);
            Assert.IsFalse(settings.resetScrollOnEnable);
            Assert.IsFalse(settings.overridePoolBuffer);
            Assert.AreEqual(1.4f, settings.poolBuffer);
        }

        [TestCase(ScrollerDirection.TopToBottom, true, false)]
        [TestCase(ScrollerDirection.BottomToTop, true, true)]
        [TestCase(ScrollerDirection.LeftToRight, false, false)]
        [TestCase(ScrollerDirection.RightToLeft, false, true)]
        public void Direction_GivesAxisAndReverse(ScrollerDirection direction, bool vertical, bool reversed)
        {
            ScrollerSettings settings = new() { direction = direction };

            Assert.AreEqual(vertical, settings.IsVertical);
            Assert.AreEqual(reversed, settings.IsReversed);
        }

        [Test]
        public void EffectivePoolBuffer_IsTheDefaultUnlessOverridden()
        {
            ScrollerSettings settings = new() { poolBuffer = 2.5f };
            Assert.AreEqual(1.4f, settings.EffectivePoolBuffer);

            settings.overridePoolBuffer = true;
            Assert.AreEqual(2.5f, settings.EffectivePoolBuffer);
        }
    }
}
