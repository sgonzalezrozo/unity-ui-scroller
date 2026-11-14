using System.Collections;
using NUnit.Framework;
using SlotList;
using UnityEngine;
using UnityEngine.TestTools;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// The correct behaviour for each known bug in the refactor plan (Phase 6 table).
    /// Ignored until the phase that fixes it: remove [Ignore], watch it fail, then fix the code.
    /// </summary>
    public class KnownBugTests
    {
        private SlotListRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        [UnityTest]
        public IEnumerator Bug_6_1_EmptySource_IsInitialized_AndShowsItemsAddedLater()
        {
            rig = SlotListRig.Create();
            ListSource<TestItem> source = new();
            rig.Scroller.Initialize(source);
            yield return SlotListRig.Frames(3);

            Assert.IsTrue(rig.Scroller.IsInitialized);

            source.AddItems(SlotListRig.MakeItems(3));
            rig.Scroller.Refresh();
            yield return null;

            Assert.AreEqual(3, rig.BoundIds().Count);
        }

        [UnityTest]
        public IEnumerator Bug_6_2_ScrollToItem_PutsTheItemAtTheTop()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(1000);

            rig.Scroller.ScrollToItem(500);
            yield return null;

            Assert.AreEqual(500, rig.BoundIds()[0]);
        }

        [UnityTest]
        public IEnumerator Bug_6_2_ScrollToItem_RightToLeft_PutsTheItemAtTheStart()
        {
            rig = SlotListRig.Create(new RigOptions { Direction = RigDirection.RightToLeft });
            yield return rig.Initialize(1000);

            rig.Scroller.ScrollToItem(500);
            yield return null;

            Assert.AreEqual(500, rig.BoundIds()[0]);
        }

        [UnityTest]
        public IEnumerator Bug_6_3_ExpandWithoutASize_NeverCollapsesTheItemToZero()
        {
            rig = SlotListRig.Create(new RigOptions { Expandable = true });
            yield return rig.Initialize(20);

            rig.Scroller.Expand(2);
            yield return null;

            Assert.Greater(rig.ItemFor(2).sizeDelta.y, 100f);
        }

        [UnityTest]
        public IEnumerator Bug_6_6_Resize_UpdatesTheWidthOfVisibleItems()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(100);

            rig.ScrollerRect.sizeDelta = new Vector2(300f, 800f);
            yield return SlotListRig.Frames(3);

            Assert.AreEqual(300f, rig.ItemFor(0).sizeDelta.x, 0.01f);
        }

        [UnityTest]
        public IEnumerator Bug_6_10_Refresh_KeepsExpandedItems()
        {
            rig = SlotListRig.Create(new RigOptions { Expandable = true });
            yield return rig.Initialize(20);
            rig.Scroller.Expand(2, 300f);
            yield return null;

            rig.Scroller.Refresh();
            yield return null;

            Assert.IsTrue(rig.Scroller.IsExpanded(2));
        }

        [UnityTest]
        public IEnumerator Bug_6_13_VisibleIndices_AreItemIndices_InAGrid()
        {
            rig = SlotListRig.Create(new RigOptions { SlotsPerLine = 3 });
            yield return rig.Initialize(60);

            Assert.AreEqual(0, rig.Scroller.VisibleStartIndex);
            Assert.AreEqual(23, rig.Scroller.VisibleEndIndex);
        }
    }
}
