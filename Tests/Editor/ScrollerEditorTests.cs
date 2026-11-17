using System.Collections.Generic;
using NUnit.Framework;
using UIScroller.Editor;
using UIScroller.Tests.PlayMode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Tests.Editor
{
    /// <summary>
    /// The curated inspector: what it draws for the current values, in what order, and what the Layout choice writes.
    /// Rows that appear conditionally always come after the control that shows them, so editing that control never
    /// shifts the field being typed in (IMGUI loses focus when a row is inserted above it).
    /// </summary>
    public class ScrollerEditorTests
    {
        private GameObject go;
        private TestScroller list;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("InspectorTest", typeof(RectTransform));
            list = go.AddComponent<TestScroller>();
        }

        [TearDown]
        public void TearDown()
            => Object.DestroyImmediate(go);

        private SerializedObject Serialized()
            => new(list);

        private IReadOnlyList<string> Drawn()
            => ScrollerEditor.DrawnEntries(Serialized());

        private static int IndexOf(IReadOnlyList<string> entries, string entry)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == entry)
                    return i;
            }

            return -1;
        }

        // ---------- What is drawn by default ----------

        [Test]
        public void DrawsOnlyTheSetupFields()
        {
            IReadOnlyList<string> drawn = Drawn();

            CollectionAssert.IsSubsetOf(new[]
            {
                "slotPrefab", "settings.direction", ScrollerEditor.LAYOUT_MODE,
                "settings.lineSpacing", "settings.paddingStart", "settings.paddingEnd",
                "settings.allowMultipleExpanded", "settings.resetScrollOnEnable", "settings.overridePoolBuffer"
            }, drawn);
            CollectionAssert.DoesNotContain(drawn, "settings.slotSpacing");
        }

        [Test]
        public void SnapAlign_IsDrawnOnlyForItemSnapping()
        {
            CollectionAssert.Contains(Drawn(), "settings.snap");
            CollectionAssert.DoesNotContain(Drawn(), "settings.snapAlign");

            list.Settings.snap = SnapMode.Item;
            CollectionAssert.Contains(Drawn(), "settings.snapAlign");

            list.Settings.snap = SnapMode.Page;
            CollectionAssert.DoesNotContain(Drawn(), "settings.snapAlign");
        }

        [Test]
        public void Loop_IsDrawnForListsAndBento_NotGridsOrSections_AndHidesExpansion()
        {
            CollectionAssert.Contains(Drawn(), "settings.loop");

            list.Settings.loop = true;
            CollectionAssert.DoesNotContain(Drawn(), "settings.allowMultipleExpanded");

            list.Settings.slotsPerLine = 3;
            CollectionAssert.DoesNotContain(Drawn(), "settings.loop");
            CollectionAssert.Contains(Drawn(), "settings.allowMultipleExpanded");

            GameObject sectionedGo = new("SectionedLoopInspectorTest", typeof(RectTransform));
            try
            {
                TestSectionedList sectioned = sectionedGo.AddComponent<TestSectionedList>();
                CollectionAssert.DoesNotContain(ScrollerEditor.DrawnEntries(new SerializedObject(sectioned)), "settings.loop");
            }
            finally
            {
                Object.DestroyImmediate(sectionedGo);
            }
        }

        [Test]
        public void HeaderPrefab_IsDrawnOnlyForASectionedList()
        {
            CollectionAssert.DoesNotContain(Drawn(), "headerPrefab");

            GameObject sectionedGo = new("SectionedInspectorTest", typeof(RectTransform));
            try
            {
                TestSectionedList sectioned = sectionedGo.AddComponent<TestSectionedList>();
                IReadOnlyList<string> drawn = ScrollerEditor.DrawnEntries(new SerializedObject(sectioned));
                Assert.AreEqual(IndexOf(drawn, "slotPrefab") + 1, IndexOf(drawn, "headerPrefab"));
            }
            finally
            {
                Object.DestroyImmediate(sectionedGo);
            }
        }

        [Test]
        public void NeverDrawsScrollRectFieldsOrSizeEstimates()
        {
            list.movementType = ScrollRect.MovementType.Elastic;
            list.inertia = true;
            list.Settings.slotsPerLine = 3;
            list.Settings.overridePoolBuffer = true;

            IReadOnlyList<string> drawn = Drawn();

            foreach (string hidden in new[] { "m_Content", "m_Viewport", "m_MovementType", "m_Elasticity", "m_Inertia", "m_VerticalScrollbar" })
                CollectionAssert.DoesNotContain(drawn, hidden);
        }

        [TestCase(ScrollerDirection.TopToBottom, "Padding Top", "Padding Bottom", "Row Spacing", "Columns", "Column Spacing")]
        [TestCase(ScrollerDirection.BottomToTop, "Padding Bottom", "Padding Top", "Row Spacing", "Columns", "Column Spacing")]
        [TestCase(ScrollerDirection.LeftToRight, "Padding Left", "Padding Right", "Column Spacing", "Rows", "Row Spacing")]
        [TestCase(ScrollerDirection.RightToLeft, "Padding Right", "Padding Left", "Column Spacing", "Rows", "Row Spacing")]
        public void Labels_FollowTheDirection(ScrollerDirection direction, string start, string end, string lineSpacing, string slotsPerLine, string slotSpacing)
        {
            Assert.AreEqual(start, ScrollerEditor.LabelFor("settings.paddingStart", direction));
            Assert.AreEqual(end, ScrollerEditor.LabelFor("settings.paddingEnd", direction));
            Assert.AreEqual(lineSpacing, ScrollerEditor.LabelFor("settings.lineSpacing", direction));
            Assert.AreEqual(slotsPerLine, ScrollerEditor.LabelFor("settings.slotsPerLine", direction));
            Assert.AreEqual(slotSpacing, ScrollerEditor.LabelFor("settings.slotSpacing", direction));
        }

        // ---------- Layout ----------

        [Test]
        public void LayoutMode_IsReadFromTheValues()
        {
            Assert.AreEqual(ScrollerLayoutMode.List, ScrollerEditor.GetLayoutMode(Serialized()));

            list.Settings.slotsPerLine = 3;
            Assert.AreEqual(ScrollerLayoutMode.Grid, ScrollerEditor.GetLayoutMode(Serialized()));

            list.Settings.autoSlotsPerLine = true;
            Assert.AreEqual(ScrollerLayoutMode.AutoGrid, ScrollerEditor.GetLayoutMode(Serialized()));
        }

        [Test]
        public void ChoosingGrid_SetsTwoSlotsPerLine_AndShowsTheGridFieldsBelowTheChoice()
        {
            SerializedObject serialized = Serialized();

            ScrollerEditor.ApplyLayoutMode(serialized, ScrollerLayoutMode.Grid);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            IReadOnlyList<string> drawn = Drawn();

            Assert.AreEqual(2u, list.Settings.slotsPerLine);
            Assert.IsFalse(list.Settings.autoSlotsPerLine);
            Assert.Greater(IndexOf(drawn, "settings.slotsPerLine"), IndexOf(drawn, ScrollerEditor.LAYOUT_MODE));
            Assert.Greater(IndexOf(drawn, "settings.slotSpacing"), IndexOf(drawn, ScrollerEditor.LAYOUT_MODE));
            CollectionAssert.DoesNotContain(drawn, "settings.targetSlotSize");
        }

        [Test]
        public void ChoosingAutoGrid_TurnsItOn_AndShowsItsFieldsBelowTheChoice()
        {
            SerializedObject serialized = Serialized();

            ScrollerEditor.ApplyLayoutMode(serialized, ScrollerLayoutMode.AutoGrid);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            IReadOnlyList<string> drawn = Drawn();

            Assert.IsTrue(list.Settings.autoSlotsPerLine);
            Assert.GreaterOrEqual(list.Settings.slotsPerLine, 2u, "automatic grids need a grid count above 1 to be active");
            foreach (string field in new[] { "settings.targetSlotSize", "settings.minSlotsPerLine", "settings.maxSlotsPerLine", "settings.slotSpacing" })
                Assert.Greater(IndexOf(drawn, field), IndexOf(drawn, ScrollerEditor.LAYOUT_MODE), field);
            CollectionAssert.DoesNotContain(drawn, "settings.slotsPerLine");
        }

        // ---------- Sections ----------

        [Test]
        public void SectionSpacing_IsDrawnOnlyForASectionedList_AfterRowSpacing()
        {
            CollectionAssert.DoesNotContain(Drawn(), "settings.sectionSpacing");

            GameObject sectionedGo = new("SectionSpacingInspectorTest", typeof(RectTransform));
            try
            {
                TestSectionedList sectioned = sectionedGo.AddComponent<TestSectionedList>();
                IReadOnlyList<string> drawn = ScrollerEditor.DrawnEntries(new SerializedObject(sectioned));

                Assert.AreEqual(IndexOf(drawn, "settings.lineSpacing") + 1, IndexOf(drawn, "settings.sectionSpacing"));
                Assert.AreEqual("Section Spacing", ScrollerEditor.LabelFor("settings.sectionSpacing", ScrollerDirection.TopToBottom));
            }
            finally
            {
                Object.DestroyImmediate(sectionedGo);
            }
        }

        // ---------- Bento ----------

        [Test]
        public void LayoutMode_IsBento_WheneverBentoIsOn()
        {
            list.Settings.bento = true;
            Assert.AreEqual(ScrollerLayoutMode.Bento, ScrollerEditor.GetLayoutMode(Serialized()));

            list.Settings.slotsPerLine = 3;
            list.Settings.autoSlotsPerLine = true;
            Assert.AreEqual(ScrollerLayoutMode.Bento, ScrollerEditor.GetLayoutMode(Serialized()));
        }

        [Test]
        public void ChoosingBento_TurnsItOn_AndShowsItsFieldsBelowTheChoice()
        {
            list.Settings.autoSlotsPerLine = true;
            SerializedObject serialized = Serialized();

            ScrollerEditor.ApplyLayoutMode(serialized, ScrollerLayoutMode.Bento);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            IReadOnlyList<string> drawn = Drawn();

            Assert.IsTrue(list.Settings.bento);
            Assert.IsFalse(list.Settings.autoSlotsPerLine);
            foreach (string field in new[] { ScrollerEditor.BENTO_JSON, "settings.cellAspect", "settings.slotSpacing" })
                Assert.Greater(IndexOf(drawn, field), IndexOf(drawn, ScrollerEditor.LAYOUT_MODE), field);
            foreach (string hidden in new[] { "settings.slotsPerLine", "settings.targetSlotSize", "settings.allowMultipleExpanded" })
                CollectionAssert.DoesNotContain(drawn, hidden);
            CollectionAssert.Contains(drawn, "settings.loop", "a bento list can loop (a bento carousel)");
        }

        [Test]
        public void LeavingBento_TurnsItOff_AndHidesItsFields()
        {
            list.Settings.bento = true;
            SerializedObject serialized = Serialized();

            ScrollerEditor.ApplyLayoutMode(serialized, ScrollerLayoutMode.Grid);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsFalse(list.Settings.bento);
            CollectionAssert.DoesNotContain(Drawn(), ScrollerEditor.BENTO_JSON);
            CollectionAssert.DoesNotContain(Drawn(), "settings.cellAspect");
        }

        [Test]
        public void BentoProblems_ListTheJsonErrors_OnlyInBento()
        {
            TextAsset json = new("{ \"columns\": 0 }");
            try
            {
                SerializedObject serialized = Serialized();
                serialized.FindProperty(ScrollerEditor.BENTO_JSON).objectReferenceValue = json;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                CollectionAssert.IsEmpty(ScrollerEditor.BentoProblems(Serialized()), "not in bento");

                list.Settings.bento = true;
                IReadOnlyList<string> problems = ScrollerEditor.BentoProblems(Serialized());

                Assert.AreEqual(1, problems.Count);
                StringAssert.Contains("columns", problems[0]);
            }
            finally
            {
                Object.DestroyImmediate(json);
            }
        }

        [Test]
        public void BentoProblems_AreEmpty_ForAValidLayout_OrNone()
        {
            list.Settings.bento = true;
            CollectionAssert.IsEmpty(ScrollerEditor.BentoProblems(Serialized()), "no JSON: 1x1 tiles");

            TextAsset json = new("{ \"columns\": 2, \"spans\": [ {\"w\":2,\"h\":1}, {\"w\":1,\"h\":1} ] }");
            try
            {
                SerializedObject serialized = Serialized();
                serialized.FindProperty(ScrollerEditor.BENTO_JSON).objectReferenceValue = json;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                CollectionAssert.IsEmpty(ScrollerEditor.BentoProblems(Serialized()));
            }
            finally
            {
                Object.DestroyImmediate(json);
            }
        }

        [Test]
        public void BentoProblems_AreEmpty_ForASectionedList()
        {
            GameObject sectionedGo = new("SectionedBentoInspectorTest", typeof(RectTransform));
            try
            {
                TestSectionedList sectioned = sectionedGo.AddComponent<TestSectionedList>();
                sectioned.Settings.bento = true;

                CollectionAssert.IsEmpty(ScrollerEditor.BentoProblems(new SerializedObject(sectioned)));
            }
            finally
            {
                Object.DestroyImmediate(sectionedGo);
            }
        }

        [Test]
        public void ChoosingList_GoesBackToOneSlotPerLine()
        {
            list.Settings.slotsPerLine = 4;
            list.Settings.autoSlotsPerLine = true;
            SerializedObject serialized = Serialized();

            ScrollerEditor.ApplyLayoutMode(serialized, ScrollerLayoutMode.List);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(1u, list.Settings.slotsPerLine);
            Assert.IsFalse(list.Settings.autoSlotsPerLine);
        }

        [Test]
        public void ChoosingGrid_FromAutoGrid_KeepsTheSlotCount()
        {
            list.Settings.slotsPerLine = 4;
            list.Settings.autoSlotsPerLine = true;
            SerializedObject serialized = Serialized();

            ScrollerEditor.ApplyLayoutMode(serialized, ScrollerLayoutMode.Grid);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(4u, list.Settings.slotsPerLine);
            Assert.IsFalse(list.Settings.autoSlotsPerLine);
        }

        // ---------- Order and side effects ----------

        [Test]
        public void ConditionalRows_AlwaysComeAfterTheirControl()
        {
            list.Settings.slotsPerLine = 3;
            list.Settings.overridePoolBuffer = true;

            IReadOnlyList<string> drawn = Drawn();

            Assert.Greater(IndexOf(drawn, "settings.poolBuffer"), IndexOf(drawn, "settings.overridePoolBuffer"));
            Assert.Greater(IndexOf(drawn, "settings.slotSpacing"), IndexOf(drawn, ScrollerEditor.LAYOUT_MODE));
            Assert.Less(IndexOf(drawn, ScrollerEditor.LAYOUT_MODE), IndexOf(drawn, "settings.lineSpacing"));
        }

        [Test]
        public void PoolBuffer_OnlyWhenOverridden()
        {
            CollectionAssert.DoesNotContain(Drawn(), "settings.poolBuffer");

            list.Settings.overridePoolBuffer = true;

            CollectionAssert.Contains(Drawn(), "settings.poolBuffer");
        }

        [Test]
        public void Bug_6_4_DrawingNeverChangesTheSettings()
        {
            list.Settings.lineSpacing = 7f;
            list.Settings.slotsPerLine = 1;

            Drawn();
            ScrollerEditor.GetLayoutMode(Serialized());

            Assert.AreEqual(7f, list.Settings.lineSpacing);
            Assert.AreEqual(1u, list.Settings.slotsPerLine);
        }
    }
}
