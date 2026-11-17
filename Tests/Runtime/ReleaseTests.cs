using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>
    /// A destroyed list must not keep its source or items alive: nothing static may hold the engine, its layout or the
    /// data. Each case builds a list, scrolls it, destroys it, collects garbage and checks the data is gone.
    /// </summary>
    public class ReleaseTests
    {
        public enum Kind
        {
            List,
            Grid,
            Bento,
            Sectioned,
            Loop,
            Paged
        }

        private const int COUNT = 5_000;
        private const int SECTIONS = 50;
        private const int PAGE_SIZE = 100;
        private const int CLEANUP_FRAMES = 3;
        private const float SCROLL_STEP = 3_000f;

        /// <summary>Holds the list and its data in fields, so dropping them leaves no reference on the test's own stack.</summary>
        private sealed class Subject
        {
            public ScrollerRig Rig;
            public object Source;
            public List<TestItem> Items;

            public WeakReference SourceReference;
            public WeakReference ItemsReference;
            public WeakReference ListReference;

            public void Release()
            {
                Rig.Dispose();
                Rig = null;
                Source = null;
                Items = null;
            }
        }

        [UnityTest]
        public IEnumerator DestroyedList_LeavesItsSourceAndItemsToTheGarbageCollector([Values] Kind kind)
        {
            Subject subject = new();
            yield return Build(subject, kind);
            yield return subject.Rig.ScrollToOffset(SCROLL_STEP);
            subject.Rig.List.ScrollToItem(COUNT / 2);
            yield return null;

            WeakReference sourceReference = subject.SourceReference;
            WeakReference itemsReference = subject.ItemsReference;
            WeakReference listReference = subject.ListReference;
            subject.Release();

            for (int i = 0; i < CLEANUP_FRAMES; i++)
                yield return null;

            // uGUI's update registries hold destroyed elements until the next canvas update, which batch-mode tests
            // without a camera may never run.
            Canvas.ForceUpdateCanvases();
            yield return null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(itemsReference.IsAlive, "the items are still referenced");
            Assert.IsFalse(sourceReference.IsAlive, "the source is still referenced");
            Assert.IsFalse(listReference.IsAlive, "the list's C# object is still referenced");
        }

        private sealed class PlainSubject
        {
            public GameObject Root;
            public WeakReference ScrollRectReference;
        }

        // Control: Unity's own ScrollRect, built and destroyed the same way. If this one stays alive, the test
        // environment keeps destroyed components, and the list check above can't tell anything.
        [UnityTest]
        public IEnumerator Control_DestroyedPlainScrollRect_IsCollected()
        {
            PlainSubject subject = new();
            BuildPlain(subject);
            yield return null;

            WeakReference reference = subject.ScrollRectReference;
            UnityEngine.Object.DestroyImmediate(subject.Root);
            subject.Root = null;

            for (int i = 0; i < CLEANUP_FRAMES; i++)
                yield return null;

            Canvas.ForceUpdateCanvases();
            yield return null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(reference.IsAlive, "a plain ScrollRect is still referenced");
        }

        private static void BuildPlain(PlainSubject subject)
        {
            subject.Root = new GameObject("PlainCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject scroll = new("Plain", typeof(RectTransform));
            scroll.transform.SetParent(subject.Root.transform, false);
            UnityEngine.UI.ScrollRect scrollRect = scroll.AddComponent<UnityEngine.UI.ScrollRect>();
            GameObject content = new("Content", typeof(RectTransform));
            content.transform.SetParent(scroll.transform, false);
            scrollRect.content = (RectTransform)content.transform;
            subject.ScrollRectReference = new WeakReference(scrollRect);
        }

        private static IEnumerator Build(Subject subject, Kind kind)
        {
            RigOptions options = new()
            {
                QuietSlots = true,
                Sectioned = kind == Kind.Sectioned,
                SlotsPerLine = kind == Kind.Grid ? 4u : 1u,
                Bento = kind == Kind.Bento,
                BentoLayout = kind == Kind.Bento ? BentoLayout.FromSpans(4, new[] { new BentoSpan(2, 2), BentoSpan.One }, 1) : null,
            };
            subject.Rig = ScrollerRig.Create(options);
            subject.ListReference = new WeakReference(subject.Rig.List);
            subject.Items = ScrollerRig.MakeItems(COUNT);
            subject.ItemsReference = new WeakReference(subject.Items);

            if (kind == Kind.Sectioned)
            {
                int[] counts = new int[SECTIONS];
                for (int i = 0; i < SECTIONS; i++)
                    counts[i] = COUNT / SECTIONS;
                SectionedSource<TestItem, TestHeader> sections = ScrollerRig.MakeSections(counts);
                subject.Source = sections;
                subject.SourceReference = new WeakReference(sections);
                // The sectioned source makes its own items; follow those instead.
                subject.Items = null;
                subject.ItemsReference = new WeakReference(sections.ItemAt(1));
                yield return subject.Rig.InitializeSections(sections);
                yield break;
            }

            if (kind == Kind.Loop)
                subject.Rig.List.Settings.loop = true;

            IItemSource<TestItem> source = kind == Kind.Paged
                ? new PagedSource<TestItem>(new FakePageLoader { Total = COUNT, PageSize = PAGE_SIZE })
                : new ListSource<TestItem>(subject.Items);
            subject.Source = source;
            subject.SourceReference = new WeakReference(source);
            if (kind == Kind.Paged)
            {
                // The page loader makes its own items: follow the source, which holds them.
                subject.Items = null;
                subject.ItemsReference = new WeakReference(source);
            }
            yield return subject.Rig.Initialize(source);
        }
    }
}
