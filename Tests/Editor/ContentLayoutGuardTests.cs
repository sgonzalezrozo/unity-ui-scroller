using System.Text.RegularExpressions;
using NUnit.Framework;
using UIScroller.Editor;
using UIScroller.Tests.PlayMode;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UIScroller.Tests.Editor
{
    /// <summary>Adding a layout component to a list's Content in the editor is undone straight away, with a warning.</summary>
    public class ContentLayoutGuardTests
    {
        private GameObject go;
        private TestScroller list;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("GuardedList", typeof(RectTransform));
            list = go.AddComponent<TestScroller>();
            RectTransform viewport = (RectTransform)new GameObject("Viewport", typeof(RectTransform)).transform;
            viewport.SetParent(go.transform, false);
            RectTransform content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(viewport, false);
            list.viewport = viewport;
            list.content = content;
        }

        [TearDown]
        public void TearDown()
            => Object.DestroyImmediate(go);

        [Test]
        public void LayoutComponentsOnContent_AreForbidden_ButAnImageIsNot()
        {
            GameObject content = list.content.gameObject;

            Assert.IsTrue(ContentLayoutGuard.IsForbiddenOn(content.AddComponent<ContentSizeFitter>(), list));
            Assert.IsTrue(ContentLayoutGuard.IsForbiddenOn(content.AddComponent<LayoutElement>(), list));
            Assert.IsFalse(ContentLayoutGuard.IsForbiddenOn(content.AddComponent<Image>(), list));
        }

        [Test]
        public void ALayoutGroupOnTheViewport_IsForbidden_ButNotElsewhere()
        {
            Assert.IsTrue(ContentLayoutGuard.IsForbiddenOn(list.viewport.gameObject.AddComponent<VerticalLayoutGroup>(), list));
            Assert.IsFalse(ContentLayoutGuard.IsForbiddenOn(go.AddComponent<ContentSizeFitter>(), list));
        }

        [Test]
        public void AddingAContentSizeFitterToContent_InTheEditor_WarnsThatItIsRemoved()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Removed ContentSizeFitter from Content"));

            ObjectFactory.AddComponent<ContentSizeFitter>(list.content.gameObject);
        }
    }
}
