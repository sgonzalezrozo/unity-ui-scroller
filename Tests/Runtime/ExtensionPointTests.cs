using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// Read-only extension points: events for expand, collapse and rebuild, and queries for where an item is and how
    /// big it is. Rig: 400 x 800 viewport, 100 px expandable items, no padding or spacing.
    /// </summary>
    public class ExtensionPointTests
    {
        private ScrollerRig rig;
        private readonly List<string> events = new();

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
            events.Clear();
        }

        private IEnumerator Start(int count = 50)
        {
            rig = ScrollerRig.Create(new RigOptions { Expandable = true });
            rig.List.ItemExpanded += index => events.Add("expanded " + index);
            rig.List.ItemCollapsed += index => events.Add("collapsed " + index);
            rig.List.Rebuilt += () => events.Add("rebuilt");
            yield return rig.Initialize(count);
            events.Clear();
        }

        [UnityTest]
        public IEnumerator Expand_RaisesItemExpanded_Once()
        {
            yield return Start();

            rig.List.Expand(3, 250f);

            CollectionAssert.AreEqual(new[] { "expanded 3" }, events);
        }

        [UnityTest]
        public IEnumerator ExpandingAnother_CollapsesTheFirst_AndSaysSo()
        {
            yield return Start();
            rig.List.Expand(3, 250f);
            events.Clear();

            rig.List.Expand(5, 250f);

            CollectionAssert.AreEqual(new[] { "collapsed 3", "expanded 5" }, events);
        }

        [UnityTest]
        public IEnumerator Collapse_RaisesItemCollapsed()
        {
            yield return Start();
            rig.List.Expand(3, 250f);
            events.Clear();

            rig.List.Collapse(3);

            CollectionAssert.AreEqual(new[] { "collapsed 3" }, events);
        }

        [UnityTest]
        public IEnumerator SetExpandedSize_RaisesNoEvent()
        {
            yield return Start();
            rig.List.Expand(3, 250f);
            events.Clear();

            rig.List.SetExpandedSize(3, 300f);

            CollectionAssert.IsEmpty(events);
        }

        [UnityTest]
        public IEnumerator Refresh_RaisesRebuilt()
        {
            yield return Start();

            rig.List.Refresh();

            CollectionAssert.AreEqual(new[] { "rebuilt" }, events);
        }

        [UnityTest]
        public IEnumerator Queries_GiveEachItemsOffsetAndSize_AndTheSizes()
        {
            yield return Start();

            Assert.AreEqual(300f, rig.List.GetItemOffset(3), 0.01f);
            Assert.AreEqual(100f, rig.List.GetItemSize(3), 0.01f);
            Assert.AreEqual(800f, rig.List.ViewportSize, 0.01f);
            Assert.AreEqual(5000f, rig.List.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator Queries_FollowAnExpansion()
        {
            yield return Start();

            rig.List.Expand(3, 250f);

            Assert.AreEqual(250f, rig.List.GetItemSize(3), 0.01f);
            Assert.AreEqual(550f, rig.List.GetItemOffset(4), 0.01f);
            Assert.AreEqual(5150f, rig.List.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator Queries_ForABadIndex_OrBeforeInitialize_ReturnMinusOne()
        {
            rig = ScrollerRig.Create();
            Assert.AreEqual(-1f, rig.List.GetItemOffset(0));
            Assert.AreEqual(-1f, rig.List.GetItemSize(0));

            yield return rig.Initialize(5);

            Assert.AreEqual(-1f, rig.List.GetItemOffset(5));
            Assert.AreEqual(-1f, rig.List.GetItemSize(-1));
        }
    }
}
