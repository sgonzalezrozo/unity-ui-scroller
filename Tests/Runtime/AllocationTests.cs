using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// The zero-GC matrix (performance plan, P1): every layout × every operation a running screen repeats, checked to
    /// allocate nothing once warmed up. Slots are quiet (they allocate nothing when bound), so anything caught is the
    /// list's. Operations that span frames (flings, animations, snaps) run the list's LateUpdate several times inside
    /// one check, with a fixed frame time. Tests ignored with "P3" allocate today and are fixed in P3.
    /// </summary>
    public class AllocationTests
    {
        public enum Kind
        {
            List,
            Grid,
            AutoGrid,
            BentoSpans,
            BentoPatterns,
            Sections,
            SectionsGrid,
            SectionsBento,
            LoopShort,
            LoopLong,
            Expandable
        }

        private const int COUNT = 10_000;
        private const float FRAME = 1f / 60f;
        private const int WARM_UPS = 2;

        private static readonly BentoSpan Big = new(2, 2);
        private static readonly MethodInfo LateUpdateMethod = typeof(SlotListBase).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

        private SlotListRig rig;
        private Action lateUpdate;
        private ListSource<TestItem> source;
        private SectionedSource<TestItem, TestHeader> sections;
        private ListSource<TestItem> firstSection;
        private PointerEventData pointer;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
            source = null;
            sections = null;
            firstSection = null;
        }

        // ---------- Setup ----------

        private static bool IsSectioned(Kind kind) => kind is Kind.Sections or Kind.SectionsGrid or Kind.SectionsBento;

        private IEnumerator Build(Kind kind)
        {
            RigOptions options = new() { QuietSlots = true, Sectioned = IsSectioned(kind), Expandable = kind == Kind.Expandable };
            switch (kind)
            {
                case Kind.Grid:
                case Kind.SectionsGrid:
                    options.SlotsPerLine = 4;
                    break;
                case Kind.BentoSpans:
                    options.Bento = true;
                    options.BentoLayout = BentoLayout.FromSpans(4, new[] { Big, BentoSpan.One }, repeatFrom: 1);
                    break;
                case Kind.BentoPatterns:
                case Kind.SectionsBento:
                    options.Bento = true;
                    options.BentoLayout = BentoLayout.FromPatterns(4, new[]
                    {
                        new BentoPattern(2, new[] { new BentoTile(0, 0, 2, 2), new BentoTile(2, 0, 2, 1), new BentoTile(2, 1, 1, 1), new BentoTile(3, 1, 1, 1) }),
                        new BentoPattern(1, new[] { new BentoTile(0, 0, 1, 1), new BentoTile(1, 0, 1, 1), new BentoTile(2, 0, 1, 1), new BentoTile(3, 0, 1, 1) })
                    }, repeatFrom: 1);
                    break;
            }

            rig = SlotListRig.Create(options);
            rig.List.frameDeltaTime = FixedFrame;
            rig.List.inertia = true;
            rig.List.movementType = ScrollRect.MovementType.Elastic;
            SlotListSettings settings = rig.List.Settings;
            if (kind == Kind.AutoGrid)
            {
                settings.autoSlotsPerLine = true;
                settings.targetSlotSize = 100f;
            }

            settings.loop = kind is Kind.LoopShort or Kind.LoopLong;
            if (IsSectioned(kind))
            {
                settings.sectionSpacing = 20f;
                sections = new SectionedSource<TestItem, TestHeader>();
                for (int s = 0; s < 50; s++)
                {
                    ListSource<TestItem> items = new(SlotListRig.MakeItems(COUNT / 50, 1000 * s));
                    firstSection ??= items;
                    sections.AddSection(new TestHeader("S" + s), items);
                }
                yield return rig.InitializeSections(sections);
            }
            else
            {
                source = new ListSource<TestItem>(SlotListRig.MakeItems(kind == Kind.LoopShort ? 20 : COUNT));
                yield return rig.Initialize(source);
            }

            lateUpdate = (Action)Delegate.CreateDelegate(typeof(Action), rig.List, LateUpdateMethod);
            pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left, position = new Vector2(200f, 400f) };
            yield return SlotListRig.Frames(3);
        }

        private static float FixedFrame() => FRAME;

        /// <summary>Runs the operation a few times to warm caches and pools, then checks that it allocates nothing.</summary>
        private static void AssertZeroGC(Action operation)
        {
            for (int i = 0; i < WARM_UPS; i++)
                operation();

            TestDelegate check = new(operation);
            Assert.That(check, Is.Not.AllocatingGCMemory());
        }

        private void ScrollNow(float distance)
        {
            Vector2 position = rig.Content.anchoredPosition;
            position.y = distance;
            rig.Content.anchoredPosition = position;
            rig.List.onValueChanged.Invoke(Vector2.zero);
        }

        private void Frames(int count)
        {
            for (int i = 0; i < count; i++)
                lateUpdate();
        }

        private int FarItem() => rig.List.GetItemCount() / 2;

        // ---------- Operations ----------

        [UnityTest]
        public IEnumerator Scroll(
            [Values(Kind.List, Kind.Grid, Kind.AutoGrid, Kind.BentoSpans, Kind.BentoPatterns, Kind.Sections, Kind.SectionsGrid, Kind.SectionsBento, Kind.LoopShort, Kind.LoopLong, Kind.Expandable)] Kind kind)
        {
            yield return Build(kind);

            AssertZeroGC(ScrollThereAndBack);
        }

        private void ScrollThereAndBack()
        {
            for (int step = 0; step < 120; step++)
                ScrollNow(step * 37f);
            for (int step = 120; step >= 0; step--)
                ScrollNow(step * 37f);
        }

        [UnityTest]
        public IEnumerator Fling(
            [Values(Kind.List, Kind.Grid, Kind.BentoPatterns, Kind.SectionsBento, Kind.LoopShort, Kind.LoopLong)] Kind kind)
        {
            yield return Build(kind);
            ScrollNow(2000f);

            AssertZeroGC(FlingBothWays);
        }

        private void FlingBothWays()
        {
            rig.List.velocity = new Vector2(0f, 4000f);
            Frames(40);
            rig.List.velocity = new Vector2(0f, -4000f);
            Frames(40);
        }

        [UnityTest]
        public IEnumerator ScrollToItem_AtOnce(
            [Values(Kind.List, Kind.Grid, Kind.BentoSpans, Kind.Sections, Kind.LoopShort, Kind.LoopLong)] Kind kind)
        {
            yield return Build(kind);

            AssertZeroGC(JumpFarAndBack);
        }

        private void JumpFarAndBack()
        {
            rig.List.ScrollToItem(FarItem(), ScrollAlign.Center);
            rig.List.ScrollToItem(1);
        }

        [UnityTest]
        public IEnumerator ScrollToItem_Animated(
            [Values(Kind.List, Kind.BentoPatterns, Kind.Sections, Kind.LoopLong)] Kind kind)
        {
            yield return Build(kind);

            AssertZeroGC(AnimateThereAndBack);
        }

        private void AnimateThereAndBack()
        {
            rig.List.ScrollToItem(40, ScrollAlign.Center, 0.2f);
            Frames(15);
            rig.List.ScrollToItem(1, ScrollAlign.Start, 0.2f);
            Frames(15);
        }

        [UnityTest]
        public IEnumerator ScrollToSection([Values(Kind.Sections, Kind.SectionsBento)] Kind kind)
        {
            yield return Build(kind);

            AssertZeroGC(JumpBetweenSections);
        }

        private void JumpBetweenSections()
        {
            rig.Sectioned.ScrollToSection(30);
            rig.Sectioned.ScrollToSection(2, 0.2f);
            Frames(15);
        }

        [UnityTest]
        public IEnumerator Snap([Values(SnapMode.Item, SnapMode.Page)] SnapMode snap, [Values(Kind.List, Kind.LoopShort)] Kind kind)
        {
            yield return Build(kind);
            rig.List.Settings.snap = snap;
            rig.List.Settings.snapAlign = SnapAlign.Center;
            ScrollNow(1033f);

            AssertZeroGC(ReleaseAndSettle);
        }

        private void ReleaseAndSettle()
        {
            rig.List.OnBeginDrag(pointer);
            rig.List.OnEndDrag(pointer);
            rig.List.velocity = new Vector2(0f, 300f);
            Frames(40);
        }

        [UnityTest]
        public IEnumerator Refresh_SameCount(
            [Values(Kind.List, Kind.Grid, Kind.BentoSpans, Kind.Sections, Kind.SectionsBento, Kind.LoopShort, Kind.Expandable)] Kind kind)
        {
            yield return Build(kind);

            AssertZeroGC(rig.List.Refresh);
        }

        [UnityTest]
        public IEnumerator Refresh_AddAndRemoveAnItem([Values(Kind.List, Kind.Grid)] Kind kind)
            => AddAndRemoveAnItem(kind);

        [UnityTest]
        [Ignore("P3: a bento row creates a new Line when the number of items starting in it changes")]
        public IEnumerator Refresh_AddAndRemoveAnItem_Bento()
            => AddAndRemoveAnItem(Kind.BentoSpans);

        private IEnumerator AddAndRemoveAnItem(Kind kind)
        {
            yield return Build(kind);
            TestItem extra = new(-1);

            AssertZeroGC(AddRefreshRemoveRefresh);

            void AddRefreshRemoveRefresh()
            {
                source.AddItem(extra);
                rig.List.Refresh();
                source.RemoveItemAt(source.ItemCount - 1);
                rig.List.Refresh();
            }
        }

        [UnityTest]
        [Ignore("P3: SectionedSource.Recount allocates two arrays (ToArray) on every Refresh after a section changes")]
        public IEnumerator Refresh_ASectionGrows([Values(Kind.Sections, Kind.SectionsBento)] Kind kind)
        {
            yield return Build(kind);
            TestItem extra = new(-1);
            ListSource<TestItem> first = firstSection;

            AssertZeroGC(AddRefreshRemoveRefresh);

            void AddRefreshRemoveRefresh()
            {
                first.AddItem(extra);
                rig.List.Refresh();
                first.RemoveItemAt(first.ItemCount - 1);
                rig.List.Refresh();
            }
        }

        [UnityTest]
        public IEnumerator ExpandAndCollapse()
        {
            yield return Build(Kind.Expandable);
            rig.List.Settings.allowMultipleExpanded = false;

            AssertZeroGC(OpenTwoAndClose);
        }

        private void OpenTwoAndClose()
        {
            rig.List.Expand(2, 250f);
            rig.List.Expand(4, 250f);
            rig.List.Collapse(4);
        }

        [UnityTest]
        [Ignore("P3: OnViewportResized starts a coroutine on every width change")]
        public IEnumerator WidthChange([Values(Kind.List, Kind.AutoGrid, Kind.BentoPatterns)] Kind kind)
        {
            yield return Build(kind);

            AssertZeroGC(Resize);
        }

        private void Resize()
        {
            rig.ScrollerRect.sizeDelta = new Vector2(480f, 800f);
            rig.ScrollerRect.sizeDelta = new Vector2(400f, 800f);
        }

        [UnityTest]
        [Ignore("P3: enabling allocates; suspected: the locale listener's delegate (Unity Localization installed)")]
        public IEnumerator DisableAndEnable([Values(Kind.List, Kind.BentoPatterns, Kind.Sections)] Kind kind)
        {
            yield return Build(kind);

            AssertZeroGC(ToggleActive);
        }

        private void ToggleActive()
        {
            rig.List.gameObject.SetActive(false);
            rig.List.gameObject.SetActive(true);
        }

        [UnityTest]
        [Ignore("P3: FilteredSource allocates its match array (ToArray) on every predicate change")]
        public IEnumerator FilterChange()
        {
            rig = SlotListRig.Create(new RigOptions { QuietSlots = true });
            ListSource<TestItem> all = new(SlotListRig.MakeItems(COUNT));
            Func<TestItem, bool> even = IsEven;
            Func<TestItem, bool> odd = IsOdd;
            FilteredSource<TestItem> filtered = new(all, even);
            yield return rig.Initialize(filtered);

            AssertZeroGC(SwitchFilter);

            void SwitchFilter()
            {
                filtered.SetPredicate(odd);
                rig.List.Refresh();
                filtered.SetPredicate(even);
                rig.List.Refresh();
            }
        }

        private static bool IsEven(TestItem item) => item.Id % 2 == 0;
        private static bool IsOdd(TestItem item) => item.Id % 2 == 1;

        [UnityTest]
        [Ignore("P3: GetAllItems allocates an AsReadOnly wrapper on every call")]
        public IEnumerator GetAllItems_FromEverySource()
        {
            ListSource<TestItem> list = new(SlotListRig.MakeItems(10));
            FilteredSource<TestItem> filtered = new(list);
            Func<TestItem, bool> all = IsEven;
            filtered.SetPredicate(all);
            yield return null;

            AssertZeroGC(ReadAll);

            void ReadAll()
            {
                IReadOnlyList<TestItem> a = list.GetAllItems();
                IReadOnlyList<TestItem> b = filtered.GetAllItems();
                if (a.Count + b.Count < 0)
                    Debug.Log("unreachable");
            }
        }
    }
}
