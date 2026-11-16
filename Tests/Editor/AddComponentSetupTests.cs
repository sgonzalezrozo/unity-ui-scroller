using System.Reflection;
using NUnit.Framework;
using UIScroller.Tests.PlayMode;
using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Tests.Editor
{
    /// <summary>
    /// Adding a list to a GameObject builds what it needs: a stretched Viewport with a RectMask2D, a stretched Content
    /// inside it, both referenced by the (hidden) ScrollRect fields, no scrollbars, and the scroll axis from the settings.
    /// The developer only sizes and anchors the GameObject itself.
    /// </summary>
    public class AddComponentSetupTests
    {
        private GameObject go;
        private TestScroller list;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("AddComponentSetup", typeof(RectTransform));
            list = go.AddComponent<TestScroller>();
        }

        [TearDown]
        public void TearDown()
            => Object.DestroyImmediate(go);

        /// <summary>What Unity runs when the component is added in the editor.</summary>
        private void RunAddComponentSetup()
        {
            MethodInfo reset = typeof(ScrollerBase).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(reset, "ScrollerBase.Reset not found");
            reset.Invoke(list, null);
        }

        private static void AssertStretched(RectTransform rect)
        {
            Assert.AreEqual(Vector2.zero, rect.anchorMin);
            Assert.AreEqual(Vector2.one, rect.anchorMax);
            Assert.AreEqual(Vector2.zero, rect.offsetMin);
            Assert.AreEqual(Vector2.zero, rect.offsetMax);
        }

        [Test]
        public void CreatesAStretchedViewport_WithARectMask2D_ReferencedByTheScrollRect()
        {
            RunAddComponentSetup();

            Transform viewport = go.transform.Find("Viewport");
            Assert.IsNotNull(viewport);
            Assert.AreSame(viewport, list.viewport);
            AssertStretched(list.viewport);
            Assert.IsTrue(viewport.GetComponent<RectMask2D>() != null, "The Viewport needs a RectMask2D.");
        }

        [Test]
        public void TheViewport_IsANestedCanvas_WithItsOwnRaycaster()
        {
            RunAddComponentSetup();

            Assert.IsTrue(list.viewport.GetComponent<Canvas>() != null, "Scrolling must only rebuild the list's own batches.");
            Assert.IsTrue(list.viewport.GetComponent<GraphicRaycaster>() != null, "Slots in a nested Canvas need its own raycaster for clicks.");
        }

        [Test]
        public void CreatesAContentInsideTheViewport_ReferencedByTheScrollRect()
        {
            RunAddComponentSetup();

            Transform content = list.viewport.Find("Content");
            Assert.IsNotNull(content);
            Assert.AreSame(content, list.content);
        }

        [Test]
        public void HasNoScrollbars()
        {
            RunAddComponentSetup();

            Assert.IsNull(list.horizontalScrollbar);
            Assert.IsNull(list.verticalScrollbar);
        }

        [Test]
        public void ScrollAxis_ComesFromTheSettings()
        {
            list.Settings.direction = ScrollerDirection.LeftToRight;

            RunAddComponentSetup();

            Assert.IsTrue(list.horizontal);
            Assert.IsFalse(list.vertical);
        }

        [Test]
        public void TheRoot_GetsAnInvisibleRaycastTarget_SoDragsOnEmptySpaceScroll()
        {
            RunAddComponentSetup();

            Image image = go.GetComponent<Image>();
            Assert.IsTrue(image != null);
            Assert.IsTrue(image.raycastTarget);
            Assert.AreEqual(0f, image.color.a);
        }

        [Test]
        public void RunningItTwice_CreatesNothingNew()
        {
            RunAddComponentSetup();
            RunAddComponentSetup();

            Assert.AreEqual(1, go.transform.childCount);
            Assert.AreEqual(1, list.viewport.childCount);
            Assert.AreEqual(1, go.GetComponents<Image>().Length);
        }
    }
}
