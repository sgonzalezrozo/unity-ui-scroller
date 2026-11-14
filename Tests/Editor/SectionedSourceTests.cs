using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SlotList.Tests.PlayMode;

namespace SlotList.Tests.Editor
{
    /// <summary>
    /// Phase 9, step 1: SectionedSource flattens sections into one list of entries, each section a header followed by
    /// its items. Empty sections are skipped unless SkipEmptySections is false. Inner sources don't announce changes,
    /// so Recount (called by the list's Refresh) picks them up.
    /// </summary>
    public class SectionedSourceTests
    {
        private sealed class Header
        {
            public string Name;
        }

        private static ListSource<TestItem> Items(int count, int firstId = 0)
            => new(SlotListRig.MakeItems(count, firstId));

        /// <summary>Sections named A, B, C... with these item counts. Item ids start at 100 × section.</summary>
        private static SectionedSource<TestItem, Header> Build(params int[] counts)
        {
            SectionedSource<TestItem, Header> source = new();
            for (int s = 0; s < counts.Length; s++)
                source.AddSection(new Header { Name = ((char)('A' + s)).ToString() }, Items(counts[s], 100 * s));
            return source;
        }

        private static string Describe(SectionedSource<TestItem, Header> source)
        {
            List<string> entries = new();
            for (int i = 0; i < source.Count; i++)
                entries.Add(source.KindAt(i) == EntryKind.Header ? source.HeaderAt(i).Name : source.ItemAt(i).Id.ToString());
            return string.Join(" ", entries);
        }

        [Test]
        public void Entries_AreEachHeaderFollowedByItsItems()
        {
            SectionedSource<TestItem, Header> source = Build(2, 3);

            Assert.AreEqual(2, source.SectionCount);
            Assert.AreEqual(7, source.Count);
            Assert.AreEqual("A 0 1 B 100 101 102", Describe(source));
        }

        [Test]
        public void LocateAndFlatIndexOf_RoundTrip_ForEveryEntry()
        {
            SectionedSource<TestItem, Header> source = Build(2, 0, 3, 1);

            for (int flat = 0; flat < source.Count; flat++)
            {
                (int section, int indexInSection) = source.Locate(flat);
                Assert.AreEqual(flat, source.FlatIndexOf(section, indexInSection), $"flat {flat}");
                Assert.AreEqual(indexInSection < 0, source.KindAt(flat) == EntryKind.Header, $"flat {flat}");
            }
        }

        [Test]
        public void Locate_GivesTheSectionAndTheIndexInIt_OrMinusOneForAHeader()
        {
            SectionedSource<TestItem, Header> source = Build(2, 3);

            Assert.AreEqual((0, -1), source.Locate(0));
            Assert.AreEqual((0, 1), source.Locate(2));
            Assert.AreEqual((1, -1), source.Locate(3));
            Assert.AreEqual((1, 2), source.Locate(6));
        }

        [Test]
        public void HeaderAt_AnItem_GivesItsSectionsHeader()
        {
            SectionedSource<TestItem, Header> source = Build(2, 3);

            Assert.AreEqual("B", source.HeaderAt(5).Name);
        }

        [Test]
        public void EmptySections_AreSkipped_ByDefault()
        {
            SectionedSource<TestItem, Header> source = Build(1, 0, 1);

            Assert.AreEqual("A 0 C 200", Describe(source));
            Assert.AreEqual(3, source.SectionCount, "a skipped section still counts as a section");
            Assert.AreEqual(-1, source.FlatIndexOf(1, -1));
            Assert.AreEqual((2, -1), source.Locate(2));
        }

        [Test]
        public void EmptySections_ShowTheirHeader_WhenNotSkipped()
        {
            SectionedSource<TestItem, Header> source = Build(1, 0, 1);

            source.SkipEmptySections = false;

            Assert.AreEqual("A 0 B C 200", Describe(source));
        }

        [Test]
        public void InsertSection_And_RemoveSection_MoveTheOthers()
        {
            SectionedSource<TestItem, Header> source = Build(1, 1);

            source.InsertSection(1, new Header { Name = "X" }, Items(2, 50));
            Assert.AreEqual("A 0 X 50 51 B 100", Describe(source));

            source.RemoveSection(0);
            Assert.AreEqual("X 50 51 B 100", Describe(source));
            Assert.AreEqual(2, source.SectionCount);
        }

        [Test]
        public void ClearSections_LeavesNothing()
        {
            SectionedSource<TestItem, Header> source = Build(2, 3);

            source.ClearSections();

            Assert.AreEqual(0, source.Count);
            Assert.AreEqual(0, source.SectionCount);
        }

        [Test]
        public void EveryChange_RaisesChanged()
        {
            SectionedSource<TestItem, Header> source = Build(1);
            int changes = 0;
            source.Changed += () => changes++;

            source.AddSection(new Header(), Items(1));
            source.InsertSection(0, new Header(), Items(1));
            source.RemoveSection(0);
            source.SkipEmptySections = false;
            source.ClearSections();

            Assert.AreEqual(5, changes);
        }

        [Test]
        public void Recount_PicksUpAnInnerSourceChange()
        {
            ListSource<TestItem> first = Items(2);
            SectionedSource<TestItem, Header> source = new();
            source.AddSection(new Header { Name = "A" }, first);
            source.AddSection(new Header { Name = "B" }, Items(1, 100));
            int changes = 0;
            source.Changed += () => changes++;

            first.AddItem(new TestItem(2));
            source.Recount();

            Assert.AreEqual("A 0 1 2 B 100", Describe(source));
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void Recount_WithoutChanges_DoesNotRaiseChanged()
        {
            SectionedSource<TestItem, Header> source = Build(2, 3);
            int changes = 0;
            source.Changed += () => changes++;

            source.Recount();

            Assert.AreEqual(0, changes);
        }

        [Test]
        public void Locate_OnAThousandSections_IsRightAtTheEdges()
        {
            int[] counts = Enumerable.Range(0, 1000).Select(s => s % 7).ToArray();
            SectionedSource<TestItem, Header> source = Build(counts);

            // Flat indexes walked in order must visit the shown sections and items in order.
            int flat = 0;
            for (int s = 0; s < counts.Length; s++)
            {
                if (counts[s] == 0)
                    continue;

                Assert.AreEqual((s, -1), source.Locate(flat++), $"header of {s}");
                for (int i = 0; i < counts[s]; i++)
                    Assert.AreEqual((s, i), source.Locate(flat++), $"item {i} of {s}");
            }

            Assert.AreEqual(source.Count, flat);
        }

        [Test]
        public void BadIndexes_Throw()
        {
            SectionedSource<TestItem, Header> source = Build(2);

            Assert.Throws<ArgumentOutOfRangeException>(() => source.KindAt(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Locate(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.FlatIndexOf(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.FlatIndexOf(0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.RemoveSection(1));
            Assert.Throws<InvalidOperationException>(() => source.ItemAt(0), "entry 0 is a header");
        }

        [Test]
        public void ANullItemSource_Throws()
            => Assert.Throws<ArgumentNullException>(() => new SectionedSource<TestItem, Header>().AddSection(new Header(), null));
    }
}
