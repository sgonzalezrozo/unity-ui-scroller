using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UIScroller;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIScroller.Tests.Editor
{
    public class Item
    {
        public int Id;

        public Item(int id)
        {
            Id = id;
        }
    }

    public class ListSourceTests
    {
        [Test]
        public void Mutators_ChangeTheList()
        {
            ListSource<Item> source = new(new[] { new Item(0), new Item(1) });

            source.AddItem(new Item(2));
            source.AddItems(new[] { new Item(3), new Item(4) });
            source.InsertItem(0, new Item(-1));
            source.RemoveItemAt(1);
            source.RemoveItem(source.GetItem(1));

            // [0,1] +2 +3,4 -> insert -1 at 0 -> remove at 1 (0) -> remove item 1 = [-1, 2, 3, 4]
            Assert.AreEqual(4, source.ItemCount);
            Assert.AreEqual(-1, source.GetItem(0).Id);
            Assert.AreEqual(2, source.GetItem(1).Id);
        }

        [Test]
        public void Nulls_AreSkipped_WithAWarning()
        {
            ListSource<Item> source = new();

            source.AddItem(null);
            source.AddItems(new Item[] { null, new Item(1) });
            source.SetItems(new Item[] { new Item(5), null });

            Assert.AreEqual(1, source.ItemCount);
            Assert.AreEqual(5, source.GetItem(0).Id);
        }

        [Test]
        public void GetItem_OutOfRange_Throws()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new ListSource<Item>().GetItem(0));

        [Test]
        public void Clear_Empties()
        {
            ListSource<Item> source = new(new[] { new Item(0) });
            source.Clear();
            Assert.AreEqual(0, source.ItemCount);
        }
    }

    public class FilteredSourceTests
    {
        private static ListSource<Item> Source(int count)
        {
            List<Item> items = new();
            for (int i = 0; i < count; i++)
                items.Add(new Item(i));
            return new ListSource<Item>(items);
        }

        [Test]
        public void WithoutAFilter_PassesEverythingThrough()
        {
            FilteredSource<Item> filtered = new(Source(5));

            Assert.AreEqual(5, filtered.ItemCount);
            Assert.AreEqual(3, filtered.GetItem(3).Id);
        }

        [Test]
        public void WithAFilter_ShowsOnlyMatches()
        {
            FilteredSource<Item> filtered = new(Source(10), item => item.Id % 3 == 0);

            Assert.AreEqual(4, filtered.ItemCount);
            Assert.AreEqual(6, filtered.GetItem(2).Id);
        }

        [Test]
        public void ChangingTheFilter_UpdatesTheCount()
        {
            FilteredSource<Item> filtered = new(Source(10), item => item.Id < 2);
            Assert.AreEqual(2, filtered.ItemCount);

            filtered.SetPredicate(item => item.Id < 5);
            Assert.AreEqual(5, filtered.ItemCount);

            filtered.ClearPredicate();
            Assert.AreEqual(10, filtered.ItemCount);
        }

        [Test]
        public void FilteredIndexOutOfRange_Throws()
        {
            FilteredSource<Item> filtered = new(Source(10), item => item.Id < 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => filtered.GetItem(2));
        }

        [Test]
        public void NullSource_Throws()
            => Assert.Throws<ArgumentNullException>(() => new FilteredSource<Item>(null));

        [Test]
        public void Bug_6_11_GetItem_DoesNotRunTheFilterAgain_AfterTheFirstLookup()
        {
            int filterCalls = 0;
            FilteredSource<Item> filtered = new(Source(100), item => { filterCalls++; return item.Id % 2 == 0; });
            filtered.GetItem(0);
            int callsAfterFirstLookup = filterCalls;

            for (int i = 0; i < 50; i++)
                filtered.GetItem(i);

            Assert.AreEqual(100, callsAfterFirstLookup);
            Assert.AreEqual(callsAfterFirstLookup, filterCalls);
            Assert.AreEqual(98, filtered.GetItem(49).Id);
        }

        [Test]
        public void Bug_6_11_Invalidate_PicksUpChangesInTheSource()
        {
            ListSource<Item> source = Source(4);
            FilteredSource<Item> filtered = new(source, item => item.Id % 2 == 0);
            Assert.AreEqual(2, filtered.ItemCount);

            source.AddItem(new Item(4));
            filtered.Invalidate();

            Assert.AreEqual(3, filtered.ItemCount);
            Assert.AreEqual(4, filtered.GetItem(2).Id);
        }
    }

    public class PullGestureTests
    {
        [TestCase(true, false, 0f, -80f, true)]
        [TestCase(true, false, 0f, -70f, false)]
        [TestCase(false, false, 80f, 0f, true)]
        [TestCase(false, false, 70f, 0f, false)]
        public void Pull_PastSeventyFivePixels_IsDetected(bool vertical, bool reverse, float x, float y, bool expected)
            => Assert.AreEqual(expected, new PullGesture(75f, vertical, reverse).WasPullDetected(new Vector2(x, y), 1000f));

        [Test]
        public void Reversed_ComparesAgainstContentSizePlusThreshold()
        {
            Assert.IsTrue(new PullGesture(75f, true, true).WasPullDetected(new Vector2(0f, 1080f), 1000f));
            Assert.IsTrue(new PullGesture(75f, false, true).WasPullDetected(new Vector2(-1080f, 0f), 1000f));
        }
    }
}
