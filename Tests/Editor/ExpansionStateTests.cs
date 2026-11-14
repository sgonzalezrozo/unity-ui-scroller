using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SlotList.Tests.Editor
{
    /// <summary>
    /// Design tests for ExpansionState (Phase 5, step 5). It merges the old expansion controller and its handler,
    /// and reaches lines and placements only through the LayoutModel. The old controller's cases are included.
    /// </summary>
    public class ExpansionStateTests
    {
        private LayoutModel layout;
        private ExpansionState expansion;
        private readonly List<int> collapsedCalls = new();
        private readonly List<(int index, float size)> expandedCalls = new();

        [SetUp]
        public void SetUp()
        {
            collapsedCalls.Clear();
            expandedCalls.Clear();
            layout = new LayoutModel();
            layout.CreatePlacements(5);
            layout.BuildLines(5, 1, _ => false, _ => 100f, _ => 100f);
            expansion = new ExpansionState(layout, new FakeSizes(collapsedCalls, expandedCalls));
        }

        /// <summary>Default size 100, expanded size 250, and records the collapse and expand notifications.</summary>
        private sealed class FakeSizes : IItemSizes
        {
            private readonly List<int> collapsed;
            private readonly List<(int index, float size)> expanded;

            public FakeSizes(List<int> collapsed, List<(int index, float size)> expanded)
            {
                this.collapsed = collapsed;
                this.expanded = expanded;
            }

            public float GetDefaultSize(int index) => 100f;
            public float GetExpandedSize(int index) => 250f;
            public void OnCollapsed(int index) => collapsed.Add(index);
            public void OnExpanded(int index, float newSize) => expanded.Add((index, newSize));
        }

        // ---------- Expand ----------

        [Test]
        public void Expand_StoresTheSize_AndResizesTheItemsLine()
        {
            bool changed = expansion.Expand(2, 300f, false, 1);

            Assert.IsTrue(changed);
            Assert.IsTrue(expansion.IsExpanded(2));
            Assert.AreEqual(300f, expansion.GetSizeOrDefault(2, 0f));
            Assert.AreEqual(300f, layout.Lines[2].Size);
        }

        [Test]
        public void Expand_WithoutASize_OnAFixedSlot_IsMeasuredWhileExpanded_AndKeepsItsSizeUntilThen()
        {
            float before = layout.Lines[2].Size;

            expansion.Expand(2, -1f, false, 1);

            Assert.IsTrue(expansion.IsExpanded(2));
            Assert.IsTrue(expansion.IsMeasuredExpanded(2), "The list measures it (default x 2 when it has no layout).");
            Assert.AreEqual(before, layout.Lines[2].Size);
        }

        [Test]
        public void Collapse_AfterAMeasuredExpansion_StopsMeasuringIt()
        {
            expansion.Expand(2, -1f, false, 1);

            expansion.Collapse(2);

            Assert.IsFalse(expansion.IsMeasuredExpanded(2));
        }

        [Test]
        public void ExpandingAgain_TogglesBackToTheDefaultSize_WithoutTheCollapseCallback()
        {
            expansion.Expand(2, 300f, false, 1);
            bool changed = expansion.Expand(2, 300f, false, 1);

            Assert.IsTrue(changed);
            Assert.IsFalse(expansion.IsExpanded(2));
            Assert.AreEqual(100f, layout.Lines[2].Size);
            Assert.IsEmpty(collapsedCalls);
        }

        [Test]
        public void SingleMode_CollapsesThePreviousItem()
        {
            expansion.Expand(1, 300f, false, 1);
            expansion.Expand(3, 250f, false, 1);

            Assert.IsFalse(expansion.IsExpanded(1));
            Assert.IsTrue(expansion.IsExpanded(3));
            Assert.AreEqual(100f, layout.Lines[1].Size);
            Assert.AreEqual(1, expansion.Count);
        }

        [Test]
        public void MultipleMode_KeepsBoth()
        {
            expansion.Expand(1, 300f, true, 1);
            expansion.Expand(3, 250f, true, 1);

            Assert.IsTrue(expansion.IsExpanded(1));
            Assert.IsTrue(expansion.IsExpanded(3));
        }

        [Test]
        public void ExpandMinusOne_CollapsesEverything_InSingleMode()
        {
            expansion.Expand(1, 300f, false, 1);

            bool changed = expansion.Expand(-1, 300f, false, 1);

            Assert.IsTrue(changed);
            Assert.IsFalse(expansion.HasAny);
            Assert.AreEqual(100f, layout.Lines[1].Size);
        }

        [Test]
        public void Expand_IsIgnoredInAGrid()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("grid"));

            bool changed = expansion.Expand(2, 300f, false, 2);

            Assert.IsFalse(changed);
            Assert.IsFalse(expansion.IsExpanded(2));
        }

        [Test]
        public void Bug_6_12_Expand_InAGrid_WarnsOnlyOnce()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("grid"));

            expansion.Expand(2, 300f, false, 2);
            expansion.Expand(3, 300f, false, 2);

            LogAssert.NoUnexpectedReceived();
        }

        [TestCase(-2)]
        [TestCase(5)]
        public void Expand_OutOfRange_DoesNothing(int index)
            => Assert.IsFalse(expansion.Expand(index, 300f, false, 1));

        // ---------- Collapse ----------

        [Test]
        public void Collapse_RestoresTheDefaultSize_OnTheLineAndThePlacement_AndCallsBack()
        {
            expansion.Expand(2, 300f, false, 1);

            bool changed = expansion.Collapse(2);

            Assert.IsTrue(changed);
            Assert.IsFalse(expansion.IsExpanded(2));
            Assert.AreEqual(100f, layout.Lines[2].Size);
            Assert.AreEqual(100f, layout.Placements[2].Size);
            Assert.AreEqual(new[] { 2 }, collapsedCalls.ToArray());
        }

        [Test]
        public void Collapse_WhenNotExpanded_DoesNothing()
        {
            Assert.IsFalse(expansion.Collapse(2));
            Assert.IsEmpty(collapsedCalls);
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void Collapse_OutOfRange_DoesNothing(int index)
            => Assert.IsFalse(expansion.Collapse(index));

        // ---------- SetExpandedSize ----------

        [Test]
        public void SetExpandedSize_ResizesAnExpandedItem_AndCallsBack()
        {
            expansion.Expand(2, 300f, false, 1);

            bool changed = expansion.SetExpandedSize(2, 400f, 1);

            Assert.IsTrue(changed);
            Assert.AreEqual(400f, layout.Lines[2].Size);
            Assert.AreEqual(new[] { (2, 400f) }, expandedCalls.ToArray());
        }

        [Test]
        public void SetExpandedSize_OnACollapsedItem_WarnsAndDoesNothing()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not expanded"));

            Assert.IsFalse(expansion.SetExpandedSize(2, 400f, 1));
            Assert.AreEqual(100f, layout.Lines[2].Size);
        }

        [Test]
        public void SetExpandedSize_InAGrid_WarnsAndDoesNothing()
        {
            expansion.Expand(2, 300f, false, 1);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not supported"));

            Assert.IsFalse(expansion.SetExpandedSize(2, 400f, 2));
        }

        [Test]
        public void ClearAll_ForgetsEveryExpandedItem()
        {
            expansion.Expand(1, 300f, true, 1);
            expansion.Expand(3, 300f, true, 1);

            expansion.ClearAll();

            Assert.IsFalse(expansion.HasAny);
        }
    }
}
