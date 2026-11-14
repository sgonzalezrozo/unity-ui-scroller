using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using SlotList.Tests.PlayMode;

namespace SlotList.Tests.Editor
{
    /// <summary>Design tests for the Phase 7 PagedSource: pages are loaded on request only, with events and cancellation.</summary>
    public class PagedSourceTests
    {
        private SynchronizationContext previousContext;
        private FakePageLoader loader;
        private PagedSource<TestItem> source;
        private readonly List<(int start, int count)> appended = new();
        private readonly List<Exception> failures = new();
        private int clearedCalls;

        [SetUp]
        public void SetUp()
        {
            // Run await continuations inline so the tests don't need frames.
            previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);

            appended.Clear();
            failures.Clear();
            clearedCalls = 0;
            loader = new FakePageLoader();
            source = new PagedSource<TestItem>(loader);
            source.ItemsAppended += (start, count) => appended.Add((start, count));
            source.LoadFailed += failures.Add;
            source.Cleared += () => clearedCalls++;
        }

        [TearDown]
        public void TearDown()
            => SynchronizationContext.SetSynchronizationContext(previousContext);

        [Test]
        public void LoadNext_AppendsThePage_AndSaysWhere()
        {
            source.LoadNextAsync().Wait();
            source.LoadNextAsync().Wait();

            Assert.AreEqual(40, source.ItemCount);
            Assert.AreEqual(new[] { (0, 20), (20, 20) }, appended.ToArray());
            Assert.AreEqual(new[] { 0, 1 }, loader.Requested.ToArray());
            Assert.AreEqual(25, source.GetItem(25).Id);
        }

        [Test]
        public void LoadNext_WhileLoading_DoesNothing()
        {
            loader.Hold = true;

            source.LoadNextAsync();
            source.LoadNextAsync();

            Assert.IsTrue(source.IsLoading);
            Assert.AreEqual(new[] { 0 }, loader.Requested.ToArray());

            loader.Release();
            Assert.IsFalse(source.IsLoading);
            Assert.AreEqual(20, source.ItemCount);
        }

        [Test]
        public void HasMore_TurnsFalseAtTheEnd_AndNoMorePagesAreRequested()
        {
            for (int i = 0; i < 5; i++)
                source.LoadNextAsync().Wait();

            Assert.IsFalse(source.HasMore);
            Assert.AreEqual(60, source.ItemCount);
            Assert.AreEqual(new[] { 0, 1, 2 }, loader.Requested.ToArray());
        }

        [Test]
        public void AFailedPage_RaisesLoadFailed_AndKeepsTheItems()
        {
            loader.FailOnPage = 1;
            source.LoadNextAsync().Wait();

            source.LoadNextAsync().Wait();

            Assert.AreEqual(1, failures.Count);
            Assert.AreEqual(20, source.ItemCount);
            Assert.IsFalse(source.IsLoading);
            Assert.IsTrue(source.HasMore);
        }

        [Test]
        public void AFailedPage_CanBeRetried()
        {
            loader.FailOnPage = 0;
            source.LoadNextAsync().Wait();
            loader.FailOnPage = -1;

            source.LoadNextAsync().Wait();

            Assert.AreEqual(20, source.ItemCount);
            Assert.AreEqual(new[] { 0, 0 }, loader.Requested.ToArray());
        }

        [Test]
        public void Reset_ClearsAndStartsAgainFromPageZero()
        {
            source.LoadNextAsync().Wait();
            source.LoadNextAsync().Wait();

            source.Reset();
            source.LoadNextAsync().Wait();

            Assert.AreEqual(1, clearedCalls);
            Assert.AreEqual(20, source.ItemCount);
            Assert.AreEqual(new[] { 0, 1, 0 }, loader.Requested.ToArray());
        }

        [Test]
        public void Reset_DuringALoad_DiscardsThatPage()
        {
            loader.Hold = true;
            source.LoadNextAsync();

            source.Reset();
            loader.Release();

            Assert.AreEqual(0, source.ItemCount);
            Assert.IsEmpty(appended);
            Assert.IsFalse(source.IsLoading);
        }

        [Test]
        public void Cancelling_DropsThePage_AndPassesTheTokenOn()
        {
            loader.Hold = true;
            using CancellationTokenSource cancellation = new();
            source.LoadNextAsync(cancellation.Token);

            cancellation.Cancel();
            loader.Release();

            Assert.IsTrue(loader.LastToken.IsCancellationRequested);
            Assert.AreEqual(0, source.ItemCount);
            Assert.IsEmpty(appended);
            Assert.IsFalse(source.IsLoading);
        }

        [Test]
        public void GetItem_NeverStartsALoad()
        {
            source.LoadNextAsync().Wait();

            for (int i = 0; i < 20; i++)
                source.GetItem(i);

            Assert.AreEqual(new[] { 0 }, loader.Requested.ToArray());
        }

        [Test]
        public void GetItem_OutOfRange_Throws()
            => Assert.Throws<ArgumentOutOfRangeException>(() => source.GetItem(0));

        [Test]
        public void ANullLoader_Throws()
            => Assert.Throws<ArgumentNullException>(() => new PagedSource<TestItem>(null));
    }
}
