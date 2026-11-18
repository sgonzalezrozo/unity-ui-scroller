using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// Design tests for the ScrollerBase / ScrollerEngine split (Phase 5, step 6): the view's hooks stay in the loop,
    /// and calls before Initialize are safe.
    /// </summary>
    public class ScrollerEngineTests
    {
        private ScrollerRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        [UnityTest]
        public IEnumerator BindItemOverride_RunsForEveryBoundSlot()
        {
            rig = ScrollerRig.Create();
            yield return rig.Initialize(100);

            Assert.AreEqual(8, rig.Scroller.BindCalls);
            Assert.AreEqual(Enumerable.Range(0, 8).ToList(), rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator OnInitialized_FiresOnce()
        {
            rig = ScrollerRig.Create();
            int calls = 0;
            rig.Scroller.OnInitialized += () => calls++;

            yield return rig.Initialize(100);
            yield return ScrollerRig.Frames(3);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void BeforeInitialize_ReportsAnEmptyList()
        {
            rig = ScrollerRig.Create();

            Assert.IsFalse(rig.Scroller.IsInitialized);
            Assert.AreEqual(0, rig.Scroller.GetItemCount());
            Assert.AreEqual(-1, rig.Scroller.VisibleStartIndex);
            Assert.AreEqual(-1, rig.Scroller.VisibleEndIndex);
            Assert.IsFalse(rig.Scroller.IsItemVisible(0));
            Assert.IsFalse(rig.Scroller.IsExpanded(0));
        }

        [Test]
        public void Bug_6_16_DraggingBeforeInitialize_DoesNotThrow()
        {
            rig = ScrollerRig.Create();
            PointerEventData pointer = new(null) { button = PointerEventData.InputButton.Left };

            Assert.DoesNotThrow(() => rig.Scroller.OnBeginDrag(pointer));
            Assert.DoesNotThrow(() => rig.Scroller.OnEndDrag(pointer));
        }

        [Test]
        public void Bug_6_14_MissingContent_LogsAnError_AndDoesNotInitialize()
        {
            rig = ScrollerRig.Create();
            rig.Scroller.content = null;
            using LogCapture log = new();

            Assert.DoesNotThrow(() => rig.Scroller.Initialize(new ListSource<TestItem>(ScrollerRig.MakeItems(10))));
            log.AssertLogged(LogType.Error, "Content");
            Assert.IsFalse(rig.Scroller.IsInitialized);
        }

        [Test]
        public void ContentWithAContentSizeFitter_LogsAnError_AndDoesNotInitialize()
        {
            rig = ScrollerRig.Create();
            rig.Content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            using LogCapture log = new();

            rig.Scroller.Initialize(new ListSource<TestItem>(ScrollerRig.MakeItems(10)));
            log.AssertLogged(LogType.Error, "ContentSizeFitter on Content");

            Assert.IsFalse(rig.Scroller.IsInitialized);
        }

        [Test]
        public void CallsBeforeInitialize_DoNothing()
        {
            rig = ScrollerRig.Create();

            Assert.DoesNotThrow(() =>
            {
                rig.Scroller.ScrollToItem(3);
                rig.Scroller.ScrollToStart();
                rig.Scroller.ScrollToEnd();
                rig.Scroller.ForceStopScrolling();
                rig.Scroller.Expand(1);
                rig.Scroller.Collapse(1);
                rig.Scroller.Clear();
            });
        }
    }
}
