using NUnit.Framework;
using SlotList.Tests.PlayMode;
using UnityEngine;

namespace SlotList.Tests.Editor
{
    /// <summary>The list's settings set up its ScrollRect in the editor too, so nobody edits the ScrollRect by hand.</summary>
    public class SettingsDriveScrollRectTests
    {
        private GameObject go;
        private TestSlotList list;
        private RectTransform content;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("SettingsDriveScrollRect", typeof(RectTransform));
            list = go.AddComponent<TestSlotList>();
            content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(go.transform, false);
            list.content = content;
        }

        [TearDown]
        public void TearDown()
            => Object.DestroyImmediate(go);

        [Test]
        public void Vertical_ScrollsVertically_WithContentAnchoredAtTheTop()
        {
            list.horizontal = true;
            list.vertical = false;
            list.Settings.direction = SlotListDirection.TopToBottom;

            list.ApplySettingsToScrollRect();

            Assert.IsTrue(list.vertical);
            Assert.IsFalse(list.horizontal);
            Assert.AreEqual(new Vector2(0.5f, 1f), content.pivot);
        }

        [Test]
        public void BottomToTop_AnchorsContentAtTheBottom()
        {
            list.Settings.direction = SlotListDirection.BottomToTop;

            list.ApplySettingsToScrollRect();

            Assert.IsTrue(list.vertical);
            Assert.AreEqual(new Vector2(0.5f, 0f), content.pivot);
        }

        [Test]
        public void Horizontal_ScrollsHorizontally_WithContentAnchoredAtTheLeft()
        {
            list.Settings.direction = SlotListDirection.LeftToRight;

            list.ApplySettingsToScrollRect();

            Assert.IsTrue(list.horizontal);
            Assert.IsFalse(list.vertical);
            Assert.AreEqual(new Vector2(0f, 0.5f), content.pivot);
        }

        [Test]
        public void RightToLeft_AnchorsContentAtTheRight()
        {
            list.Settings.direction = SlotListDirection.RightToLeft;

            list.ApplySettingsToScrollRect();

            Assert.IsTrue(list.horizontal);
            Assert.AreEqual(new Vector2(1f, 0.5f), content.pivot);
        }

        [Test]
        public void WithoutContent_OnlySetsTheAxis()
        {
            list.content = null;
            list.Settings.direction = SlotListDirection.LeftToRight;

            Assert.DoesNotThrow(() => list.ApplySettingsToScrollRect());
            Assert.IsTrue(list.horizontal);
        }
    }
}
