using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UIScroller.Editor
{
    /// <summary>
    /// Builds the uGUI objects of the generated examples: rects, images, texts, buttons and layout groups, in one dark
    /// style. Texts use Unity's built-in font, so the examples need no TextMeshPro.
    /// </summary>
    internal static class ExampleUIKit
    {
        public const float HEADER_HEIGHT = 160f;
        public const float MARGIN = 32f;

        public static readonly Color Background = new(0.09f, 0.1f, 0.13f);
        public static readonly Color Panel = new(0.16f, 0.18f, 0.22f);
        public static readonly Color PanelLight = new(0.24f, 0.26f, 0.31f);
        public static readonly Color Accent = new(0.33f, 0.55f, 0.95f);
        public static readonly Color Gold = new(0.98f, 0.76f, 0.3f);
        public static readonly Color Online = new(0.3f, 0.8f, 0.45f);
        public static readonly Color TextColor = new(0.94f, 0.95f, 0.97f);
        public static readonly Color Muted = new(0.62f, 0.66f, 0.72f);
        public static readonly Color Shade = new(0f, 0f, 0f, 0.28f);

        private const string BUILT_IN_FONT = "LegacyRuntime.ttf";
        private static readonly Vector2 ReferenceResolution = new(1080f, 1920f);

        private static Font font;

        /// <summary>The scene new roots go to while an example is built (a preview scene, so the open scene stays clean).</summary>
        public static UnityEngine.SceneManagement.Scene TargetScene;

        private static void MoveToTargetScene(GameObject root)
        {
            if (TargetScene.IsValid())
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, TargetScene);
        }

        // ---------- Rects ----------

        public static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            if (parent != null)
            {
                rect.SetParent(parent, false);
                go.layer = parent.gameObject.layer;
            }

            return rect;
        }

        /// <summary>Anchors between <paramref name="anchorMin"/> and <paramref name="anchorMax"/>, inset by the offsets.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
            => Place(rect, Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(-right, -top));

        /// <summary>A fixed size at one anchor point: <paramref name="anchor"/> is both the anchor and the pivot.</summary>
        public static RectTransform Pin(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        // ---------- Graphics ----------

        public static Image AddImage(GameObject go, Color color, bool raycast = false)
        {
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Image NewImage(string name, Transform parent, Color color, bool raycast = false)
            => AddImage(NewRect(name, parent).gameObject, color, raycast);

        /// <summary>A text stretched over its parent, with the built-in font.</summary>
        public static Text NewText(string name, Transform parent, string content, int size, TextAnchor alignment, Color color, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = Stretch(NewRect(name, parent));
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = BuiltInFont();
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        /// <summary>A text that grows with its content, for slots measured with layout.</summary>
        public static Text FlowText(string name, Transform parent, string content, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            Text text = NewText(name, parent, content, size, TextAnchor.UpperLeft, color, style);
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>A Button on <paramref name="go"/>, using its Image as the target graphic (an invisible one if it has none).</summary>
        public static Button AddButton(GameObject go)
        {
            if (!go.TryGetComponent(out Image target))
                target = AddImage(go, Color.clear);
            target.raycastTarget = true;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = target;
            return button;
        }

        /// <summary>A coloured button with a centred label.</summary>
        public static Button LabelButton(string name, Transform parent, string label, Color color, int fontSize = 30)
        {
            Image image = NewImage(name, parent, color, true);
            NewText("Label", image.transform, label, fontSize, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            return AddButton(image.gameObject);
        }

        // ---------- Layout ----------

        public static VerticalLayoutGroup Vertical(GameObject go, int padding, float spacing)
        {
            VerticalLayoutGroup group = go.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.spacing = spacing;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return group;
        }

        public static HorizontalLayoutGroup Horizontal(GameObject go, float spacing)
        {
            HorizontalLayoutGroup group = go.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return group;
        }

        public static LayoutElement Layout(GameObject go, float width = -1f, float height = -1f, float flexibleWidth = -1f)
        {
            LayoutElement element = go.AddComponent<LayoutElement>();
            if (width >= 0f)
            {
                element.minWidth = width;
                element.preferredWidth = width;
            }

            if (height >= 0f)
            {
                element.minHeight = height;
                element.preferredHeight = height;
            }

            element.flexibleWidth = flexibleWidth;
            return element;
        }

        // ---------- Screens and lists ----------

        /// <summary>
        /// The example's root: a Canvas (so the prefab works on its own in an empty scene; nested under another Canvas it
        /// just follows that one), a background, the screen component, a title bar and the body for the list.
        /// </summary>
        public static RectTransform ScreenRoot(string name, Type screenType, string title, string subtitle, out Component screen, out Text status)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            MoveToTargetScene(root);
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
                root.layer = uiLayer;

            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            AddImage(root, Background);
            screen = root.AddComponent(screenType);

            RectTransform header = Place(NewRect("Header", root.transform), new Vector2(0f, 1f), Vector2.one, new Vector2(MARGIN, -HEADER_HEIGHT), new Vector2(-MARGIN, 0f));
            NewText("Title", header, title, 52, TextAnchor.MiddleLeft, TextColor, FontStyle.Bold);
            status = NewText("Status", header, subtitle, 28, TextAnchor.MiddleRight, Muted);

            return Stretch(NewRect("Body", root.transform), MARGIN, MARGIN, MARGIN, HEADER_HEIGHT);
        }

        /// <summary>
        /// A list of <paramref name="scrollerType"/> filling <paramref name="parent"/>, with its Viewport and Content. Set its
        /// Settings, then call <see cref="FinishList"/>.
        /// </summary>
        public static ScrollerBase NewList(Type scrollerType, RectTransform parent, GameObject slotPrefab)
        {
            RectTransform rect = Stretch(NewRect(scrollerType.Name, parent));
            ScrollerBase list = (ScrollerBase)rect.gameObject.AddComponent(scrollerType);

            // Adding the component in the editor builds the Viewport and Content (Reset). Build them if it didn't run.
            if (list.viewport == null || list.content == null)
                typeof(ScrollerBase).GetMethod("SetupScrollRectStructure", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(list, null);

            list.SlotPrefab = slotPrefab;
            return list;
        }

        /// <summary>Applies the list's settings to its ScrollRect and Content, as the Inspector does.</summary>
        public static void FinishList(ScrollerBase list)
            => list.ApplySettingsToScrollRect();

        /// <summary>A slot template's root: a panel-coloured Image with the slot component, and a Button if asked.</summary>
        public static GameObject SlotRoot(string name, Type slotType, Color color, out Component slot, bool button = false)
        {
            GameObject root = new(name, typeof(RectTransform));
            MoveToTargetScene(root);
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
                root.layer = uiLayer;

            AddImage(root, color, true);
            slot = root.AddComponent(slotType);
            if (button)
                AddButton(root);
            return root;
        }

        // ---------- Serialized fields ----------

        /// <summary>Sets a serialized field (private ones too) by name. Logs when the script has no such field.</summary>
        public static void Set(Object target, string field, Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[UI Scroller] {target.GetType().Name} has no serialized field '{field}'. Assign it by hand.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Font BuiltInFont()
        {
            if (font == null)
                font = Resources.GetBuiltinResource<Font>(BUILT_IN_FONT);
            return font;
        }
    }
}
