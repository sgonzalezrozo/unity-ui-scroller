using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UIScroller.Editor
{
    /// <summary>
    /// Builds the uGUI objects of the prefabs the New List wizard creates: rects, images, labels, buttons and layout
    /// groups. Labels are TextMeshPro when it's installed and chosen, Unity's built-in Text otherwise.
    /// </summary>
    internal static class ListUIKit
    {
        public static readonly Color ItemColor = Color.white;
        public static readonly Color HeaderColor = new(0.9f, 0.91f, 0.93f);
        public static readonly Color TextColor = new(0.12f, 0.13f, 0.16f);
        public static readonly Color MutedText = new(0.4f, 0.43f, 0.48f);

        private const string BUILT_IN_FONT = "LegacyRuntime.ttf";
        private const string TMP_TYPE = "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        private static Font font;

        /// <summary>The scene new roots go to while the prefabs are built (a preview scene, so the open scene stays clean).</summary>
        public static UnityEngine.SceneManagement.Scene TargetScene;

        /// <summary>Labels are TextMeshPro while this is true.</summary>
        public static bool UseTextMeshPro;

        /// <summary>TextMeshPro's label component, or null when it isn't installed.</summary>
        public static Type TextMeshProType => Type.GetType(TMP_TYPE);

        // ---------- Rects ----------

        public static GameObject NewRoot(string name)
        {
            GameObject root = new(name, typeof(RectTransform));
            if (TargetScene.IsValid())
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, TargetScene);

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
                root.layer = uiLayer;
            return root;
        }

        public static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return rect;
        }

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

        // ---------- Graphics ----------

        public static Image AddImage(GameObject go, Color color, bool raycast = false)
        {
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>A label stretched over its parent. <paramref name="top"/> aligns it to the top left instead of the middle left.</summary>
        public static Graphic NewLabel(string name, Transform parent, string content, int size, Color color, bool bold = false, bool top = false, bool grows = false)
        {
            RectTransform rect = Stretch(NewRect(name, parent));
            Type tmp = UseTextMeshPro ? TextMeshProType : null;
            if (tmp != null)
            {
                Graphic label = (Graphic)rect.gameObject.AddComponent(tmp);
                SetProperty(label, "text", content);
                SetProperty(label, "fontSize", (float)size);
                SetEnum(label, "alignment", top ? "TopLeft" : "Left");
                if (bold)
                    SetEnum(label, "fontStyle", "Bold");
                label.color = color;
                label.raycastTarget = false;
                return label;
            }

            Text text = rect.gameObject.AddComponent<Text>();
            text.font = BuiltInFont();
            text.text = content;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = top ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = grows ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
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

        // ---------- Lists ----------

        /// <summary>
        /// A root object with the list component, stretched to fill whatever it's placed under. Adding the component in the
        /// editor builds its Viewport and Content; they're built here if that didn't run.
        /// </summary>
        public static ScrollerBase NewList(string name, Type listType)
        {
            GameObject root = NewRoot(name);
            Stretch((RectTransform)root.transform);
            ScrollerBase list = (ScrollerBase)root.AddComponent(listType);
            if (list.viewport == null || list.content == null)
                typeof(ScrollerBase).GetMethod("SetupScrollRectStructure", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(list, null);
            return list;
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

        private static void SetProperty(object target, string name, object value)
            => target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.SetValue(target, value);

        private static void SetEnum(object target, string name, string value)
        {
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.PropertyType.IsEnum && Enum.IsDefined(property.PropertyType, value))
                property.SetValue(target, Enum.Parse(property.PropertyType, value));
        }

        private static Font BuiltInFont()
        {
            if (font == null)
                font = Resources.GetBuiltinResource<Font>(BUILT_IN_FONT);
            return font;
        }
    }
}
