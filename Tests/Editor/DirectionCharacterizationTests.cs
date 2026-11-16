using NUnit.Framework;
using UIScroller;
using UnityEngine;

namespace UIScroller.Tests.Editor
{
    /// <summary>Pins how each direction places slots and converts positions. Grid slots are scaled to fill the row.</summary>
    public class DirectionCharacterizationTests
    {
        private GameObject root;
        private RectTransform viewport;
        private RectTransform content;
        private RectTransform slot;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("DirectionTestRoot", typeof(RectTransform));
            viewport = NewRect("Viewport", root.transform);
            viewport.sizeDelta = new Vector2(400f, 800f);
            content = NewRect("Content", viewport);
            slot = NewRect("Slot", content);
        }

        [TearDown]
        public void TearDown()
            => Object.DestroyImmediate(root);

        [Test]
        public void Factory_PicksTheClassForEachDirection()
        {
            Assert.IsInstanceOf<TopToBottomLayout>(AxisLayouts.CreateDirection(true, false));
            Assert.IsInstanceOf<BottomToTopLayout>(AxisLayouts.CreateDirection(true, true));
            Assert.IsInstanceOf<LeftToRightLayout>(AxisLayouts.CreateDirection(false, false));
            Assert.IsInstanceOf<RightToLeftLayout>(AxisLayouts.CreateDirection(false, true));
        }

        [Test]
        public void TopToBottom_PlacesItemsDownwards_AndScalesGridItemsToFillTheRow()
        {
            IAxisLayout direction = new TopToBottomLayout();

            direction.PositionInLine(slot, 250f, 100f, 1, 3, viewport, content, 10f);

            float width = (400f - 20f) / 3f;
            AssertVector(new Vector2(width + 10f, -250f), slot.anchoredPosition);
            AssertVector(new Vector2(width, 100f), slot.sizeDelta);
            AssertVector(new Vector2(0f, 1f), slot.pivot);
        }

        [Test]
        public void BottomToTop_PlacesItemsUpwards()
        {
            IAxisLayout direction = new BottomToTopLayout();

            direction.PositionInLine(slot, 250f, 100f, 0, 1, viewport, content, 0f);

            AssertVector(new Vector2(0f, 250f), slot.anchoredPosition);
            AssertVector(new Vector2(400f, 100f), slot.sizeDelta);
            AssertVector(new Vector2(0f, 0f), slot.pivot);
        }

        [Test]
        public void LeftToRight_PlacesItemsRightwards_AndSplitsTheHeightInAGrid()
        {
            IAxisLayout direction = new LeftToRightLayout();

            direction.PositionInLine(slot, 250f, 100f, 1, 2, viewport, content, 0f);

            AssertVector(new Vector2(250f, -400f), slot.anchoredPosition);
            AssertVector(new Vector2(100f, 400f), slot.sizeDelta);
            AssertVector(new Vector2(0f, 1f), slot.pivot);
        }

        [Test]
        public void RightToLeft_PlacesItemsLeftwards()
        {
            IAxisLayout direction = new RightToLeftLayout();

            direction.PositionInLine(slot, 250f, 100f, 0, 1, viewport, content, 0f);

            AssertVector(new Vector2(-250f, 0f), slot.anchoredPosition);
            AssertVector(new Vector2(100f, 800f), slot.sizeDelta);
            AssertVector(new Vector2(1f, 1f), slot.pivot);
        }





        [TestCase(true, false, 0.75f, 1f)]
        [TestCase(true, true, 0.25f, 0f)]
        [TestCase(false, false, 0.25f, 0f)]
        [TestCase(false, true, 0.75f, 1f)]
        public void NormalizedScrollPosition_PutsTheTargetAtTheLayoutStart(bool vertical, bool reverse, float expected, float whenItFits)
        {
            // ScrollRect's normalized 1 is the top and 0 the left, so top-to-bottom and right-to-left invert (bug 6.2, fixed).
            IAxisLayout direction = AxisLayouts.CreateDirection(vertical, reverse);

            Assert.AreEqual(expected, direction.CalculateNormalizedScrollPosition(250f, 1800f, 800f), 0.0001f);
            Assert.AreEqual(whenItFits, direction.CalculateNormalizedScrollPosition(250f, 500f, 800f));
        }

        [TestCase(true, false, 0f, 300f, -300f)]
        [TestCase(true, true, 0f, -300f, 300f)]
        [TestCase(false, false, -300f, 0f, -300f)]
        [TestCase(false, true, 300f, 0f, -300f)]
        public void ScrollPosition_IsReadFromTheContentPosition(bool vertical, bool reverse, float x, float y, float expected)
        {
            IAxisLayout direction = AxisLayouts.CreateDirection(vertical, reverse);
            content.anchoredPosition = new Vector2(x, y);

            Assert.AreEqual(expected, direction.GetScrollPosition(content));
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void AssertVector(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f, "x");
            Assert.AreEqual(expected.y, actual.y, 0.001f, "y");
        }
    }
}
