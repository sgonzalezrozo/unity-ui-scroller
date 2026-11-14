using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SlotList.Tests.Editor
{
    /// <summary>The list owns Content's size, so layout components that would drive it are refused.</summary>
    public class ContentLayoutRulesTests
    {
        private GameObject root;
        private RectTransform viewport;
        private RectTransform content;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Root", typeof(RectTransform));
            viewport = (RectTransform)new GameObject("Viewport", typeof(RectTransform)).transform;
            viewport.SetParent(root.transform, false);
            content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(viewport, false);
        }

        [TearDown]
        public void TearDown()
            => Object.DestroyImmediate(root);

        [Test]
        public void PlainContent_IsValid()
            => Assert.IsEmpty(ContentLayoutRules.FindForbidden(content, viewport));

        [Test]
        public void AnImageOnContent_IsAllowed()
        {
            content.gameObject.AddComponent<Image>();

            Assert.IsEmpty(ContentLayoutRules.FindForbidden(content, viewport));
        }

        [Test]
        public void AContentSizeFitter_IsRefused()
        {
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();

            CollectionAssert.AreEqual(new Component[] { fitter }, ContentLayoutRules.FindForbidden(content, viewport));
        }

        [Test]
        public void ALayoutGroupOrLayoutElementOnContent_IsRefused()
        {
            VerticalLayoutGroup group = content.gameObject.AddComponent<VerticalLayoutGroup>();
            LayoutElement element = content.gameObject.AddComponent<LayoutElement>();

            CollectionAssert.AreEquivalent(new Component[] { group, element }, ContentLayoutRules.FindForbidden(content, viewport));
        }

        [Test]
        public void ALayoutGroupOnTheViewport_IsRefused_BecauseItSizesContent()
        {
            HorizontalLayoutGroup group = viewport.gameObject.AddComponent<HorizontalLayoutGroup>();

            CollectionAssert.AreEqual(new Component[] { group }, ContentLayoutRules.FindForbidden(content, viewport));
        }

        [Test]
        public void TheMessage_NamesEachComponentAndWhereItIs()
        {
            content.gameObject.AddComponent<ContentSizeFitter>();

            string message = ContentLayoutRules.Describe(ContentLayoutRules.FindForbidden(content, viewport));

            StringAssert.Contains("ContentSizeFitter on Content", message);
        }
    }
}
