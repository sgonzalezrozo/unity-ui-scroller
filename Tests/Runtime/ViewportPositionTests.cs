using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// Slots that implement IViewportPositionListener are told where they sit in the viewport whenever the list moves:
    /// 0 at the centre, -0.5 and +0.5 at the edges. GetViewportPosition answers the same for any shown item.
    /// </summary>
    public class ViewportPositionTests
    {
        private const float TOLERANCE = 0.001f;
        private ScrollerRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private float Told(int item)
            => rig.ItemFor(item).GetComponent<ViewportRecorder>().Position;

        [UnityTest]
        public IEnumerator ShownSlots_AreToldTheirPosition_AndFollowTheScroll()
        {
            // Items of 100 px in an 800 px viewport.
            rig = ScrollerRig.Create(new RigOptions { ViewportRecorder = true });
            yield return rig.Initialize(50);

            Assert.AreEqual((50f - 400f) / 800f, Told(0), TOLERANCE, "item 0 near the start edge");
            Assert.AreEqual((350f - 400f) / 800f, Told(3), TOLERANCE);

            yield return rig.ScrollToOffset(1000f);
            Assert.AreEqual((1450f - 1400f) / 800f, Told(14), TOLERANCE, "item 14 just past the centre");
            Assert.AreEqual(Told(14), rig.List.GetViewportPosition(14), TOLERANCE);
            Assert.IsNaN(rig.List.GetViewportPosition(0), "off screen");
        }

        [UnityTest]
        public IEnumerator ALoopingCarousel_TellsItsSlots_AcrossTheLoop()
        {
            rig = ScrollerRig.Create(new RigOptions { ViewportRecorder = true });
            rig.List.Settings.loop = true;
            yield return rig.Initialize(20);

            rig.List.ScrollToItem(0, ScrollAlign.Center);
            yield return ScrollerRig.Frames(2);

            Assert.AreEqual(0f, rig.List.GetViewportPosition(0), TOLERANCE, "item 0 centred");
            Assert.AreEqual(0f, Told(0), TOLERANCE);
            Assert.AreEqual(100f / 800f, rig.List.GetViewportPosition(1), TOLERANCE, "the next one, one item further");
        }

        [UnityTest]
        public IEnumerator ListsWhoseSlotsDontListen_DoNoWork()
        {
            rig = ScrollerRig.Create(new RigOptions());
            yield return rig.Initialize(50);
            yield return rig.ScrollToOffset(500f);

            Assert.AreEqual((550f - 900f) / 800f, rig.List.GetViewportPosition(5), TOLERANCE, "the query still works");
        }
    }
}
