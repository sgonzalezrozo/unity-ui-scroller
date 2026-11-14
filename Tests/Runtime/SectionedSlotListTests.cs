using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// Phase 9, step 2: SectionedSlotList shows a SectionedSource. Headers come from the header prefab and bind with
    /// ISlot&lt;THeader&gt;, items with ISlot&lt;TItem&gt;; a header takes a whole row in a grid.
    /// Rig: 400 x 800 viewport, 100 px items, 50 px headers. Sections are S0, S1...; items of section s have ids 100 × s + i.
    /// </summary>
    public class SectionedSlotListTests
    {
        private SlotListRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        private IEnumerator Start(SectionedSource<TestItem, TestHeader> source, RigOptions options = null)
        {
            options ??= new RigOptions();
            options.Sectioned = true;
            rig = SlotListRig.Create(options);
            yield return rig.InitializeSections(source);
        }

        private static float Top(RectTransform slot) => -slot.anchoredPosition.y;

        [UnityTest]
        public IEnumerator HeadersAndItems_BindWithTheirOwnSlots_InOrder()
        {
            yield return Start(SlotListRig.MakeSections(3, 3));

            RectTransform headerA = rig.HeaderFor("S0");
            RectTransform headerB = rig.HeaderFor("S1");
            Assert.AreEqual("HeaderTemplate(Clone)", headerA.name);
            Assert.AreEqual(0f, Top(headerA), 0.01f);
            Assert.AreEqual(50f, headerA.sizeDelta.y, 0.01f);
            Assert.AreEqual(50f, Top(rig.ItemFor(0)), 0.01f);
            Assert.AreEqual(350f, Top(headerB), 0.01f);
            Assert.AreEqual(400f, Top(rig.ItemFor(100)), 0.01f);
            Assert.AreEqual(2 * 50f + 6 * 100f, rig.ContentSize, 0.01f);
        }

        [UnityTest]
        public IEnumerator NewSource_SameEntryCount_HeaderMoved_ShowsEachEntryWithTheRightSlot()
        {
            yield return Start(SlotListRig.MakeSections(3, 3));

            // Still 8 entries, so slots are kept through the refresh, but S1's header moves from entry 4 to entry 2.
            rig.Sectioned.SetSource(SlotListRig.MakeSections(1, 5));
            yield return SlotListRig.Frames(2);

            Assert.AreEqual(150f, Top(rig.HeaderFor("S1")), 0.01f);
            Assert.AreEqual(200f, Top(rig.ItemFor(100)), 0.01f);
            Assert.AreEqual(50f, Top(rig.ItemFor(0)), 0.01f);
            Assert.IsNull(rig.ItemFor(1), "Section 0 has one item now.");
            CollectionAssert.AreEqual(new[] { 0, 100, 101, 102, 103, 104 }, rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator EmptySections_AreSkipped()
        {
            yield return Start(SlotListRig.MakeSections(2, 0, 1));

            Assert.IsNull(rig.HeaderFor("S1"));
            Assert.AreEqual(250f, Top(rig.HeaderFor("S2")), 0.01f);
        }

        [UnityTest]
        public IEnumerator InAGrid_AHeaderTakesAWholeRow_AndASectionStartsANewRow()
        {
            yield return Start(SlotListRig.MakeSections(4, 2), new RigOptions { SlotsPerLine = 3 });

            RectTransform headerA = rig.HeaderFor("S0");
            Assert.AreEqual(400f, headerA.sizeDelta.x, 0.01f);
            Assert.AreEqual(50f, Top(rig.ItemFor(0)), 0.01f);
            Assert.AreEqual(50f, Top(rig.ItemFor(2)), 0.01f);
            Assert.Less(rig.ItemFor(0).sizeDelta.x, 140f);
            Assert.AreEqual(150f, Top(rig.ItemFor(3)), 0.01f);
            Assert.AreEqual(250f, Top(rig.HeaderFor("S1")), 0.01f);
            Assert.AreEqual(300f, Top(rig.ItemFor(100)), 0.01f);
        }

        [UnityTest]
        public IEnumerator Scrolling_RecyclesHeaders_ThroughTheirOwnPool()
        {
            int[] counts = new int[50];
            for (int i = 0; i < counts.Length; i++)
                counts[i] = 5;
            yield return Start(SlotListRig.MakeSections(counts));

            for (int offset = 0; offset <= 20000; offset += 700)
                yield return rig.ScrollToOffset(offset);

            // Each section is 50 + 5 × 100 = 550 px, so header s starts at 550 × s.
            int shownHeaders = 0;
            foreach (Transform child in rig.Content)
            {
                RecordingHeaderSlot header = child.GetComponent<RecordingHeaderSlot>();
                if (!child.gameObject.activeSelf || header == null)
                    continue;

                int section = int.Parse(header.Data.Name.Substring(1));
                Assert.AreEqual(550f * section, Top((RectTransform)child), 0.01f, header.Data.Name);
                shownHeaders++;
            }

            Assert.Greater(shownHeaders, 0);
            Assert.LessOrEqual(rig.CountHeaderInstances(), 4, "headers are reused, not created per section");
        }

        [UnityTest]
        public IEnumerator SetSource_AfterInitialization_ReplacesTheSections()
        {
            yield return Start(SlotListRig.MakeSections(3));

            SectionedSource<TestItem, TestHeader> other = new();
            other.AddSection(new TestHeader("Other"), new ListSource<TestItem>(SlotListRig.MakeItems(2, 900)));
            rig.Sectioned.SetSource(other);
            yield return null;

            Assert.IsNull(rig.HeaderFor("S0"));
            Assert.AreEqual(0f, Top(rig.HeaderFor("Other")), 0.01f);
            Assert.AreEqual(new List<int> { 900, 901 }, rig.BoundIds());
        }

        [UnityTest]
        public IEnumerator Refresh_PicksUpAChangeInASection()
        {
            ListSource<TestItem> first = new(SlotListRig.MakeItems(2));
            SectionedSource<TestItem, TestHeader> source = new();
            source.AddSection(new TestHeader("A"), first);
            source.AddSection(new TestHeader("B"), new ListSource<TestItem>(SlotListRig.MakeItems(1, 100)));
            yield return Start(source);

            first.AddItem(new TestItem(2));
            rig.Sectioned.Refresh();
            yield return null;

            Assert.AreEqual(250f, Top(rig.ItemFor(2)), 0.01f);
            Assert.AreEqual(350f, Top(rig.HeaderFor("B")), 0.01f);
        }

        [UnityTest]
        public IEnumerator ScrollToSection_PutsItsHeaderAtTheStart_OrTheNextShownOne()
        {
            int[] counts = new int[50];
            for (int i = 0; i < counts.Length; i++)
                counts[i] = i == 10 ? 0 : 5;
            yield return Start(SlotListRig.MakeSections(counts));

            rig.Sectioned.ScrollToSection(20);
            yield return SlotListRig.Frames(2);
            RectTransform header = rig.HeaderFor("S20");
            Assert.IsNotNull(header);
            Assert.AreEqual(0f, Top(header) - rig.Content.anchoredPosition.y, 0.5f);

            rig.Sectioned.ScrollToSection(10);
            yield return SlotListRig.Frames(2);
            Assert.AreEqual(0f, Top(rig.HeaderFor("S11")) - rig.Content.anchoredPosition.y, 0.5f, "S10 is empty and skipped");
        }

        [UnityTest]
        public IEnumerator ItemsInSections_CanExpand()
        {
            yield return Start(SlotListRig.MakeSections(3, 3), new RigOptions { Expandable = true });

            rig.Sectioned.Expand(2, 300f);
            yield return null;

            Assert.AreEqual(300f, rig.ItemFor(1).sizeDelta.y, 0.01f);
            Assert.AreEqual(550f, Top(rig.HeaderFor("S1")), 0.01f);
        }

        [UnityTest]
        public IEnumerator MeasuredHeaders_UseTheirOwnLayout_AndDontChangeTheItemAverage()
        {
            int[] counts = { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };
            yield return Start(SlotListRig.MakeSections(counts), new RigOptions { ItemSize = -1f, LayoutContent = true, HeaderSize = -1f });
            yield return null;

            Assert.AreEqual(SlotListRig.HEADER_BODY_HEIGHT, rig.HeaderFor("S0").sizeDelta.y, 0.01f);
            Assert.AreEqual(SlotListRig.HEADER_BODY_HEIGHT, Top(rig.ItemFor(0)), 0.01f);
            Assert.AreEqual(100f, rig.ItemFor(4).sizeDelta.y, 0.01f);

            // Unmeasured items take the items' average (100), never a header's size.
            yield return rig.ScrollToOffset(2000f);
            int first = rig.BoundIds()[0];
            Assert.AreEqual(100f, rig.ItemFor(first).sizeDelta.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator WithoutAHeaderPrefab_SetSource_LogsAnError_AndDoesNotStart()
        {
            rig = SlotListRig.Create(new RigOptions { Sectioned = true });
            rig.Sectioned.HeaderPrefab = null;
            LogAssert.Expect(LogType.Error, new Regex("Header Prefab"));

            rig.Sectioned.SetSource(SlotListRig.MakeSections(2));
            yield return SlotListRig.Frames(2);

            Assert.IsFalse(rig.Sectioned.IsInitialized);
        }
    }
}
