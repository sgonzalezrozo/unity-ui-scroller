using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIScroller.Samples
{
    /// <summary>
    /// Builds the examples' UI in code, so each one runs from an empty scene: an EventSystem, a Canvas with a title,
    /// a list with its Viewport and Content, slot templates and labels. Uses Unity's built-in font, not TextMeshPro.
    /// </summary>
    public static class ExampleUI
    {
        public const float TITLE_HEIGHT = 120f;
        public const float MARGIN = 32f;
        public const int TITLE_FONT_SIZE = 44;
        public const int BODY_FONT_SIZE = 32;
        public const int SMALL_FONT_SIZE = 26;
        public const float TEXT_INSET = 24f;

        private const string BUILT_IN_FONT = "LegacyRuntime.ttf";
        private static readonly Vector2 ReferenceResolution = new(1080f, 1920f);
        private const float MATCH_WIDTH_OR_HEIGHT = 0.5f;

        public static readonly Color Background = new(0.11f, 0.12f, 0.15f);
        public static readonly Color Panel = new(0.18f, 0.2f, 0.24f);
        public static readonly Color Accent = new(0.33f, 0.55f, 0.95f);
        public static readonly Color TextColor = new(0.93f, 0.94f, 0.96f);
        public static readonly Color MutedText = new(0.65f, 0.68f, 0.74f);

        private static Font font;

        /// <summary>A full-screen Canvas with a title bar. Returns the area below the title, for the list.</summary>
        public static RectTransform CreateScreen(string title, out Text status)
        {
            EnsureEventSystem();

            GameObject canvasObject = new("Example Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = MATCH_WIDTH_OR_HEIGHT;

            RectTransform root = (RectTransform)canvasObject.transform;
            RectTransform background = CreateRect("Background", root);
            Stretch(background);
            background.gameObject.AddComponent<Image>().color = Background;

            Text titleText = CreateText("Title", background, TITLE_FONT_SIZE, TextAnchor.MiddleLeft, TextColor);
            AnchorTop((RectTransform)titleText.transform, 0f, TITLE_HEIGHT);
            titleText.text = title;

            status = CreateText("Status", background, SMALL_FONT_SIZE, TextAnchor.MiddleRight, MutedText);
            AnchorTop((RectTransform)status.transform, 0f, TITLE_HEIGHT);

            RectTransform body = CreateRect("Body", background);
            Stretch(body);
            body.offsetMin = new Vector2(MARGIN, MARGIN);
            body.offsetMax = new Vector2(-MARGIN, -TITLE_HEIGHT);
            return body;
        }

        /// <summary>
        /// A list filling <paramref name="parent"/>: the list object, a Viewport with a RectMask2D and an empty Content.
        /// Set its Settings before Initialize or SetSource.
        /// </summary>
        public static TList CreateList<TList>(RectTransform parent, GameObject slotTemplate) where TList : ScrollerBase
        {
            RectTransform listRect = CreateRect(typeof(TList).Name, parent);
            Stretch(listRect);

            // A transparent graphic, so a drag that starts between slots still reaches the list.
            Image hitArea = listRect.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;

            RectTransform viewport = CreateRect("Viewport", listRect);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = CreateRect("Content", viewport);

            TList list = listRect.gameObject.AddComponent<TList>();
            list.viewport = viewport;
            list.content = content;
            list.SlotPrefab = slotTemplate;
            return list;
        }

        /// <summary>An inactive object that holds the slot templates, so they never show or run.</summary>
        public static Transform CreateTemplateRoot()
        {
            GameObject templates = new("Slot Templates");
            templates.SetActive(false);
            return templates.transform;
        }

        /// <summary>A slot template: a panel-coloured root with <typeparamref name="TSlot"/> on it.</summary>
        public static TSlot CreateSlotTemplate<TSlot>(Transform templateRoot, Color color) where TSlot : Component
        {
            RectTransform rect = CreateRect(typeof(TSlot).Name, templateRoot);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect.gameObject.AddComponent<TSlot>();
        }

        /// <summary>A button at the top right of the title bar, over the status label.</summary>
        public static Button CreateTitleButton(RectTransform body, string label)
        {
            const float WIDTH = 420f;
            const float HEIGHT = 80f;

            RectTransform rect = CreateRect("Button", body.parent);
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.sizeDelta = new Vector2(WIDTH, HEIGHT);
            rect.anchoredPosition = new Vector2(-MARGIN, -(TITLE_HEIGHT - HEIGHT) / 2f);
            rect.gameObject.AddComponent<Image>().color = Accent;

            Text text = CreateText("Label", rect, SMALL_FONT_SIZE, TextAnchor.MiddleCenter, TextColor);
            text.text = label;
            return rect.gameObject.AddComponent<Button>();
        }

        public static Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect);
            rect.offsetMin = new Vector2(TEXT_INSET, 0f);
            rect.offsetMax = new Vector2(-TEXT_INSET, 0f);

            Text text = rect.gameObject.AddComponent<Text>();
            text.font = BuiltInFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Stretches across the parent's width, <paramref name="height"/> tall, <paramref name="top"/> below its top.</summary>
        public static void AnchorTop(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(rect.offsetMin.x, -top - height);
            rect.offsetMax = new Vector2(rect.offsetMax.x, -top);
        }

        public static Color ColorFor(int index)
        {
            const float GOLDEN_RATIO_STEP = 0.618034f;
            const float SATURATION = 0.45f;
            const float VALUE = 0.75f;
            return Color.HSVToRGB(index * GOLDEN_RATIO_STEP % 1f, SATURATION, VALUE);
        }

        private static Font BuiltInFont()
        {
            if (font == null)
                font = Resources.GetBuiltinResource<Font>(BUILT_IN_FONT);
            return font;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystem = new("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
