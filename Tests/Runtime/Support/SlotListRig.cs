using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SlotList;
using ToolsCore;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SlotList.Tests.PlayMode
{
    public enum RigDirection
    {
        TopToBottom,
        BottomToTop,
        LeftToRight,
        RightToLeft
    }

    /// <summary>Settings for a <see cref="SlotListRig"/>. Defaults: vertical list, 400 x 800 viewport, 100 px slots.</summary>
    public class RigOptions
    {
        public RigDirection Direction = RigDirection.TopToBottom;
        public Vector2 ViewportSize = new(400f, 800f);
        public float ItemSize = 100f;
        public bool Expandable;
        public uint SlotsPerLine = 1;
        public float LineSpacing;
        public float SlotSpacing;
        public float PaddingStart;
        public float PaddingEnd;
        public bool AllowMultipleExpanded;
        /// <summary>Builds a TestSectionedList (rig.Sectioned) with a header template, instead of a TestSlotList.</summary>
        public bool Sectioned;

        /// <summary>The header template's DefaultExtent. 0 or less measures it with layout: a 60 px body.</summary>
        public float HeaderSize = 50f;

        /// <summary>The slot gets a VerticalLayoutGroup and a body whose height is the item's Height. Combine with ItemSize = -1 to measure with layout.</summary>
        public bool LayoutContent;

        /// <summary>The slot is a QuietSlot (allocates nothing when bound) instead of a RecordingSlot. Every slot also gets a ParentWatch.</summary>
        public bool QuietSlots;
    }

    /// <summary>
    /// Builds a scroller in code on a world-space Canvas, with no scene or prefab assets.
    /// Inertia is off and movement is clamped, so results repeat. Dispose destroys everything.
    /// </summary>
    public sealed class SlotListRig : IDisposable
    {
        private const int MAX_WAIT_FRAMES = 30;

        public readonly RigOptions Options;
        /// <summary>The height of an expandable slot's details part (LayoutContent).</summary>
        public const float DETAILS_HEIGHT = 200f;

        /// <summary>The list, whatever its kind.</summary>
        public readonly SlotListBase List;

        /// <summary>The plain list, or null when Options.Sectioned.</summary>
        public readonly TestSlotList Scroller;

        /// <summary>The sectioned list, or null unless Options.Sectioned.</summary>
        public readonly TestSectionedList Sectioned;

        public const float HEADER_BODY_HEIGHT = 60f;
        public readonly RectTransform ScrollerRect;
        public readonly RectTransform Viewport;
        public readonly RectTransform Content;
        public readonly GameObject SlotTemplate;
        public readonly GameObject HeaderTemplate;

        private readonly GameObject canvasRoot;
        private readonly GameObject templateRoot;
        private bool initializedFired;

        public bool IsVertical => Options.Direction is RigDirection.TopToBottom or RigDirection.BottomToTop;

        private SlotListRig(RigOptions options)
        {
            Options = options;
            ToolInjectionServices.PrefabInstantiator = null;

            canvasRoot = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)canvasRoot.transform).sizeDelta = new Vector2(2000f, 2000f);

            templateRoot = new GameObject("TestTemplates");
            templateRoot.SetActive(false);
            SlotTemplate = CreateItemTemplate("SlotTemplate", options.ItemSize, options.Expandable);
            if (options.LayoutContent)
                AddLayoutContent(SlotTemplate);
            if (options.Sectioned)
                HeaderTemplate = CreateHeaderTemplate(options.HeaderSize);

            GameObject scrollerGo = new("TestSlotList", typeof(RectTransform));
            ScrollerRect = (RectTransform)scrollerGo.transform;
            ScrollerRect.SetParent(canvasRoot.transform, false);
            ScrollerRect.sizeDelta = options.ViewportSize;

            Viewport = CreateChild("Viewport", ScrollerRect);
            Viewport.anchorMin = Vector2.zero;
            Viewport.anchorMax = Vector2.one;
            Viewport.offsetMin = Vector2.zero;
            Viewport.offsetMax = Vector2.zero;
            Viewport.gameObject.AddComponent<RectMask2D>();

            Content = CreateChild("Content", Viewport);

            if (options.Sectioned)
            {
                Sectioned = scrollerGo.AddComponent<TestSectionedList>();
                Sectioned.HeaderPrefab = HeaderTemplate;
                List = Sectioned;
            }
            else
            {
                Scroller = scrollerGo.AddComponent<TestSlotList>();
                List = Scroller;
            }

            List.viewport = Viewport;
            List.content = Content;
            List.inertia = false;
            List.movementType = ScrollRect.MovementType.Clamped;
            List.OnInitialized += () => initializedFired = true;

            List.SlotPrefab = SlotTemplate;
            SlotListSettings settings = List.Settings;
            settings.direction = options.Direction switch
            {
                RigDirection.BottomToTop => SlotListDirection.BottomToTop,
                RigDirection.LeftToRight => SlotListDirection.LeftToRight,
                RigDirection.RightToLeft => SlotListDirection.RightToLeft,
                _ => SlotListDirection.TopToBottom
            };
            settings.slotsPerLine = options.SlotsPerLine;
            settings.lineSpacing = options.LineSpacing;
            settings.slotSpacing = options.SlotSpacing;
            settings.paddingStart = options.PaddingStart;
            settings.paddingEnd = options.PaddingEnd;
            settings.allowMultipleExpanded = options.AllowMultipleExpanded;
        }

        public static SlotListRig Create(RigOptions options = null)
            => new(options ?? new RigOptions());

        public static List<TestItem> MakeItems(int count, int firstId = 0)
        {
            List<TestItem> items = new(count);
            for (int i = 0; i < count; i++)
                items.Add(new TestItem(firstId + i));
            return items;
        }

        /// <summary>Calls Initialize and waits until OnInitialized fires.</summary>
        public IEnumerator Initialize(IItemSource<TestItem> source)
        {
            Scroller.Initialize(source);
            yield return WaitUntil(() => initializedFired, "OnInitialized never fired");
        }

        /// <summary>Calls SetSource on the sectioned list and waits until OnInitialized fires.</summary>
        public IEnumerator InitializeSections(SectionedSource<TestItem, TestHeader> source)
        {
            Sectioned.SetSource(source);
            yield return WaitUntil(() => initializedFired, "OnInitialized never fired");
        }

        /// <summary>Sections named S0, S1... with these item counts. Item ids start at 100 × section.</summary>
        public static SectionedSource<TestItem, TestHeader> MakeSections(params int[] counts)
        {
            SectionedSource<TestItem, TestHeader> source = new();
            for (int s = 0; s < counts.Length; s++)
                source.AddSection(new TestHeader("S" + s), new ListSource<TestItem>(MakeItems(counts[s], 100 * s)));
            return source;
        }

        /// <summary>The active header slot showing the header with this name, or null.</summary>
        public RectTransform HeaderFor(string name)
        {
            foreach (Transform child in Content)
            {
                RecordingHeaderSlot header = child.GetComponent<RecordingHeaderSlot>();
                if (child.gameObject.activeSelf && !SlotPool.IsParked((RectTransform)child) && header != null && header.Data != null && header.Data.Name == name)
                    return (RectTransform)child;
            }

            return null;
        }

        /// <summary>Every header slot instance the list created, active or pooled. The template doesn't count.</summary>
        public int CountHeaderInstances()
        {
            int count = 0;
            foreach (RecordingHeaderSlot slot in Object.FindObjectsByType<RecordingHeaderSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!IsTemplate(slot.gameObject))
                    count++;
            }

            return count;
        }

        /// <summary>Initializes with a ListSource of <paramref name="count"/> items.</summary>
        public IEnumerator Initialize(int count)
            => Initialize(new ListSource<TestItem>(MakeItems(count)));

        /// <summary>Moves the content so <paramref name="offset"/> pixels of the list are above the viewport start, then waits two frames.</summary>
        public IEnumerator ScrollToOffset(float offset)
        {
            Vector2 position = Content.anchoredPosition;
            switch (Options.Direction)
            {
                case RigDirection.TopToBottom:
                    position.y = offset;
                    break;
                case RigDirection.BottomToTop:
                    position.y = -offset;
                    break;
                case RigDirection.LeftToRight:
                    position.x = -offset;
                    break;
                case RigDirection.RightToLeft:
                    position.x = offset;
                    break;
            }

            Content.anchoredPosition = position;
            yield return null;
            yield return null;
        }

        public static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        public static IEnumerator WaitUntil(Func<bool> condition, string failure)
        {
            for (int i = 0; i < MAX_WAIT_FRAMES; i++)
            {
                if (condition())
                    yield break;
                yield return null;
            }

            Assert.Fail($"{failure} (waited {MAX_WAIT_FRAMES} frames)");
        }

        /// <summary>Ids of the items shown by active slots under Content, sorted.</summary>
        public List<int> BoundIds()
        {
            List<int> ids = new();
            foreach (Transform child in Content)
            {
                if (!child.gameObject.activeSelf || SlotPool.IsParked((RectTransform)child))
                    continue;

                TestItem item = CurrentItem(child.gameObject);
                if (item != null)
                    ids.Add(item.Id);
            }

            ids.Sort();
            return ids;
        }

        /// <summary>The RectTransform under Content that is active and shows the item with this id, or null.</summary>
        public RectTransform ItemFor(int id)
        {
            foreach (Transform child in Content)
            {
                if (!child.gameObject.activeSelf || SlotPool.IsParked((RectTransform)child))
                    continue;

                TestItem item = CurrentItem(child.gameObject);
                if (item != null && item.Id == id)
                    return (RectTransform)child;
            }

            return null;
        }

        /// <summary>Every slot instance the scroller created, active or pooled, in any scene. The templates don't count.</summary>
        public int CountItemInstances()
        {
            int count = 0;
            foreach (RecordingSlot slot in Object.FindObjectsByType<RecordingSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!IsTemplate(slot.gameObject))
                    count++;
            }

            foreach (ExpandableRecordingSlot slot in Object.FindObjectsByType<ExpandableRecordingSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!IsTemplate(slot.gameObject))
                    count++;
            }

            foreach (QuietSlot slot in Object.FindObjectsByType<QuietSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!IsTemplate(slot.gameObject))
                    count++;
            }

            return count;
        }

        private bool IsTemplate(GameObject go)
            => go == SlotTemplate || go == HeaderTemplate;

        public float ContentSize
            => IsVertical ? Content.rect.height : Content.rect.width;

        public void SetField(string name, object value)
        {
            for (Type type = List.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (field == null)
                    continue;

                field.SetValue(List, value);
                return;
            }

            throw new MissingFieldException(List.GetType().Name, name);
        }

        public void Dispose()
        {
            if (List != null)
                Object.DestroyImmediate(List.gameObject);

            Object.DestroyImmediate(canvasRoot);
            Object.DestroyImmediate(templateRoot);
        }

        private static TestItem CurrentItem(GameObject go)
        {
            RecordingSlot slot = go.GetComponent<RecordingSlot>();
            if (slot != null)
                return slot.Data;

            ExpandableRecordingSlot expandable = go.GetComponent<ExpandableRecordingSlot>();
            if (expandable != null)
                return expandable.Data;

            QuietSlot quiet = go.GetComponent<QuietSlot>();
            return quiet != null ? quiet.Data : null;
        }

        private GameObject CreateItemTemplate(string name, float size, bool expandable)
        {
            GameObject template = new(name, typeof(RectTransform));
            template.transform.SetParent(templateRoot.transform, false);
            float templateSize = size > 0f ? size : 100f;
            ((RectTransform)template.transform).sizeDelta = new Vector2(templateSize, templateSize);

            if (expandable)
                template.AddComponent<ExpandableRecordingSlot>().Size = size;
            else if (Options.QuietSlots)
                template.AddComponent<QuietSlot>().Size = size;
            else
                template.AddComponent<RecordingSlot>().Size = size;

            template.AddComponent<ParentWatch>();

            return template;
        }

        private GameObject CreateHeaderTemplate(float size)
        {
            GameObject template = new("HeaderTemplate", typeof(RectTransform));
            template.transform.SetParent(templateRoot.transform, false);
            float templateSize = size > 0f ? size : 100f;
            ((RectTransform)template.transform).sizeDelta = new Vector2(templateSize, templateSize);
            template.AddComponent<RecordingHeaderSlot>().Size = size;

            if (size <= 0f)
            {
                VerticalLayoutGroup group = template.AddComponent<VerticalLayoutGroup>();
                group.childControlHeight = true;
                group.childControlWidth = true;
                group.childForceExpandHeight = false;
                group.childForceExpandWidth = true;
                CreateChild("Body", template.transform).gameObject.AddComponent<LayoutElement>().preferredHeight = HEADER_BODY_HEIGHT;
            }

            return template;
        }

        private static void AddLayoutContent(GameObject template)
        {
            VerticalLayoutGroup group = template.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;

            RectTransform body = CreateChild("Body", template.transform);
            LayoutElement element = body.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 100f;

            ExpandableRecordingSlot expandable = template.GetComponent<ExpandableRecordingSlot>();
            if (expandable == null)
            {
                template.GetComponent<RecordingSlot>().Body = element;
                return;
            }

            // An expandable slot also gets a 200 px details part, shown only while expanded.
            RectTransform details = CreateChild("Details", template.transform);
            expandable.Body = element;
            expandable.Details = details.gameObject.AddComponent<LayoutElement>();
            expandable.Details.preferredHeight = DETAILS_HEIGHT;
            details.gameObject.SetActive(false);
        }

        private static RectTransform CreateChild(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
