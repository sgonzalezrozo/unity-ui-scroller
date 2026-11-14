using NUnit.Framework;

namespace SlotList.Tests.Editor
{
    /// <summary>Design tests for SlotListSettings (Phase 5, step 1). Defaults match the old serialized field defaults.</summary>
    public class SlotListSettingsTests
    {
        [Test]
        public void Defaults_MatchTheOldFieldDefaults()
        {
            SlotListSettings settings = new();

            Assert.AreEqual(SlotListDirection.TopToBottom, settings.direction);
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

        [TestCase(SlotListDirection.TopToBottom, true, false)]
        [TestCase(SlotListDirection.BottomToTop, true, true)]
        [TestCase(SlotListDirection.LeftToRight, false, false)]
        [TestCase(SlotListDirection.RightToLeft, false, true)]
        public void Direction_GivesAxisAndReverse(SlotListDirection direction, bool vertical, bool reversed)
        {
            SlotListSettings settings = new() { direction = direction };

            Assert.AreEqual(vertical, settings.IsVertical);
            Assert.AreEqual(reversed, settings.IsReversed);
        }

        [Test]
        public void EffectivePoolBuffer_IsTheDefaultUnlessOverridden()
        {
            SlotListSettings settings = new() { poolBuffer = 2.5f };
            Assert.AreEqual(1.4f, settings.EffectivePoolBuffer);

            settings.overridePoolBuffer = true;
            Assert.AreEqual(2.5f, settings.EffectivePoolBuffer);
        }
    }
}
