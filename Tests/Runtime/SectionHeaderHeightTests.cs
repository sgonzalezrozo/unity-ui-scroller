using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// Headers measured with layout, each as tall as its own content, in a sectioned list and a sectioned grid.
    /// Rig: 400 x 800 viewport, 100 px items. Sections: S0 (40 px header, 2 items), S1 (120 px, 2), S2 (80 px, 1).
    /// </summary>
    public class SectionHeaderHeightTests
    {
        private SlotListRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private static SectionedSource<TestItem, TestHeader> Sections()
        {
            SectionedSource<TestItem, TestHeader> source = new();
            source.AddSection(new TestHeader("S0", 40f), new ListSource<TestItem>(SlotListRig.MakeItems(2, 0)));
            source.AddSection(new TestHeader("S1", 120f), new ListSource<TestItem>(SlotListRig.MakeItems(2, 100)));
            source.AddSection(new TestHeader("S2", 80f), new ListSource<TestItem>(SlotListRig.MakeItems(1, 200)));
            return source;
        }

        private IEnumerator Start(uint slotsPerLine)
        {
            rig = SlotListRig.Create(new RigOptions { Sectioned = true, HeaderSize = -1f, SlotsPerLine = slotsPerLine });
            yield return rig.InitializeSections(Sections());
            yield return SlotListRig.Frames(2);
        }

        private static void AssertAt(RectTransform rect, float y, float height, string what)
        {
            Assert.IsNotNull(rect, $"{what} has no slot");
            Assert.AreEqual(y, rect.anchoredPosition.y, 0.5f, $"{what} y");
            Assert.AreEqual(height, rect.sizeDelta.y, 0.5f, $"{what} height");
        }

        // ---------- Section Spacing: 30 px before every header but the first. 50 px headers, 2 items per section ----------

        private IEnumerator StartSpaced(RigOptions options)
        {
            options.Sectioned = true;
            options.SectionSpacing = 30f;
            rig = SlotListRig.Create(options);
            yield return rig.InitializeSections(SlotListRig.MakeSections(2, 2));
            yield return SlotListRig.Frames(2);
        }

        [UnityTest]
        public IEnumerator SectionSpacing_InAList_GoesBeforeEachHeaderButTheFirst()
        {
            yield return StartSpaced(new RigOptions());

            AssertAt(rig.HeaderFor("S0"), 0f, 50f, "S0");
            AssertAt(rig.ItemFor(1), -150f, 100f, "item 1");
            AssertAt(rig.HeaderFor("S1"), -280f, 50f, "S1");
            AssertAt(rig.ItemFor(100), -330f, 100f, "item 100");
            Assert.AreEqual(530f, rig.List.ContentSize, 0.5f);
        }

        [UnityTest]
        public IEnumerator SectionSpacing_InAGrid()
        {
            yield return StartSpaced(new RigOptions { SlotsPerLine = 3 });

            AssertAt(rig.ItemFor(1), -50f, 100f, "item 1");
            AssertAt(rig.HeaderFor("S1"), -180f, 50f, "S1");
            AssertAt(rig.ItemFor(101), -230f, 100f, "item 101");
        }

        [UnityTest]
        public IEnumerator SectionSpacing_InBento()
        {
            yield return StartSpaced(new RigOptions { Bento = true, BentoLayout = BentoLayout.Uniform(2), CellAspect = 0.5f });

            AssertAt(rig.ItemFor(1), -50f, 100f, "item 1");
            AssertAt(rig.HeaderFor("S1"), -180f, 50f, "S1");
            AssertAt(rig.ItemFor(100), -230f, 100f, "item 100");
        }

        [UnityTest]
        public IEnumerator SectionSpacing_ScrollToSection_StopsAtTheHeader()
        {
            rig = SlotListRig.Create(new RigOptions { Sectioned = true, SectionSpacing = 30f });
            yield return rig.InitializeSections(SlotListRig.MakeSections(10, 10, 10));

            rig.Sectioned.ScrollToSection(1);
            yield return null;

            Assert.AreEqual(1080f, rig.ScrollDistance, 0.5f, "50 + 10 x 100, then the 30 px gap");
        }

        [UnityTest]
        public IEnumerator InAList_EachHeaderTakesItsOwnHeight()
        {
            yield return Start(1);

            AssertAt(rig.HeaderFor("S0"), 0f, 40f, "S0");
            AssertAt(rig.ItemFor(0), -40f, 100f, "item 0");
            AssertAt(rig.ItemFor(1), -140f, 100f, "item 1");
            AssertAt(rig.HeaderFor("S1"), -240f, 120f, "S1");
            AssertAt(rig.ItemFor(100), -360f, 100f, "item 100");
            AssertAt(rig.HeaderFor("S2"), -560f, 80f, "S2");
            AssertAt(rig.ItemFor(200), -640f, 100f, "item 200");
            Assert.AreEqual(740f, rig.List.ContentSize, 0.5f);
        }

        [UnityTest]
        public IEnumerator InAGrid_EachHeaderTakesItsOwnHeight_AcrossTheWholeRow()
        {
            yield return Start(3);

            AssertAt(rig.HeaderFor("S0"), 0f, 40f, "S0");
            Assert.AreEqual(400f, rig.HeaderFor("S0").sizeDelta.x, 0.5f, "a header spans the row");
            AssertAt(rig.ItemFor(1), -40f, 100f, "item 1");
            AssertAt(rig.HeaderFor("S1"), -140f, 120f, "S1");
            AssertAt(rig.ItemFor(101), -260f, 100f, "item 101");
            AssertAt(rig.HeaderFor("S2"), -360f, 80f, "S2");
            AssertAt(rig.ItemFor(200), -440f, 100f, "item 200");
        }
    }
}
