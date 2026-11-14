using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SlotList.Editor
{
    /// <summary>How the list arranges its slots, as chosen in the inspector.</summary>
    public enum SlotListLayoutMode
    {
        List,
        Grid,
        AutoGrid,
        Bento
    }

    /// <summary>
    /// The SlotList inspector. It shows only what setting up a list needs, with labels that follow the direction
    /// (rows and columns, top, bottom, left and right). ScrollRect's own fields and the size estimates stay hidden.
    /// Rows that appear for a choice are drawn below that choice, so editing it never moves the field in use.
    /// </summary>
    [CustomEditor(typeof(SlotListBase), true)]
    public class SlotListEditor : UnityEditor.Editor
    {
        /// <summary>The entry for the Layout dropdown in <see cref="DrawnEntries"/>.</summary>
        public const string LAYOUT_MODE = "#layoutMode";

        /// <summary>The list's Bento Layout JSON field.</summary>
        public const string BENTO_JSON = "bentoLayoutJson";

        private const string HEADER = "#header:";
        private const string S = "settings.";
        private const int MIN_GRID_SLOTS = 2;

        private static readonly HashSet<string> INDENTED = new()
        {
            S + "slotsPerLine", S + "slotSpacing", S + "targetSlotSize", S + "minSlotsPerLine", S + "maxSlotsPerLine", S + "poolBuffer",
            BENTO_JSON, S + "cellAspect"
        };

        /// <summary>What the inspector draws, in order: property paths, headers ("#header:…") and the Layout dropdown.</summary>
        public static IReadOnlyList<string> DrawnEntries(SerializedObject serializedObject)
        {
            List<string> entries = new()
            {
                HEADER + "Slots", "slotPrefab"
            };

            // Only a sectioned list has a header prefab.
            if (serializedObject.FindProperty("headerPrefab") != null)
                entries.Add("headerPrefab");

            entries.Add(HEADER + "Layout");
            entries.Add(S + "direction");
            entries.Add(LAYOUT_MODE);

            switch (GetLayoutMode(serializedObject))
            {
                case SlotListLayoutMode.Grid:
                    entries.Add(S + "slotsPerLine");
                    entries.Add(S + "slotSpacing");
                    break;
                case SlotListLayoutMode.AutoGrid:
                    entries.Add(S + "targetSlotSize");
                    entries.Add(S + "minSlotsPerLine");
                    entries.Add(S + "maxSlotsPerLine");
                    entries.Add(S + "slotSpacing");
                    break;
                case SlotListLayoutMode.Bento:
                    entries.Add(BENTO_JSON);
                    entries.Add(S + "cellAspect");
                    entries.Add(S + "slotSpacing");
                    break;
            }

            entries.Add(HEADER + "Spacing & Padding");
            entries.Add(S + "lineSpacing");
            if (serializedObject.FindProperty("headerPrefab") != null)
                entries.Add(S + "sectionSpacing");
            entries.Add(S + "paddingStart");
            entries.Add(S + "paddingEnd");

            entries.Add(HEADER + "Behaviour");

            // A looping list is a plain list: no grid, no sections.
            bool canLoop = GetLayoutMode(serializedObject) == SlotListLayoutMode.List && serializedObject.FindProperty("headerPrefab") == null;
            if (canLoop)
                entries.Add(S + "loop");

            entries.Add(S + "snap");
            if (serializedObject.FindProperty(S + "snap").enumValueIndex == (int)SnapMode.Item)
                entries.Add(S + "snapAlign");

            // Bento tiles never expand; a looping list doesn't either.
            bool isBento = GetLayoutMode(serializedObject) == SlotListLayoutMode.Bento;
            if (!isBento && (!canLoop || !serializedObject.FindProperty(S + "loop").boolValue))
                entries.Add(S + "allowMultipleExpanded");
            entries.Add(S + "resetScrollOnEnable");
            entries.Add(S + "overridePoolBuffer");
            if (serializedObject.FindProperty(S + "overridePoolBuffer").boolValue)
                entries.Add(S + "poolBuffer");

            return entries;
        }

        public static SlotListLayoutMode GetLayoutMode(SerializedObject serializedObject)
        {
            if (serializedObject.FindProperty(S + "bento").boolValue)
                return SlotListLayoutMode.Bento;

            if (serializedObject.FindProperty(S + "autoSlotsPerLine").boolValue)
                return SlotListLayoutMode.AutoGrid;

            return serializedObject.FindProperty(S + "slotsPerLine").uintValue > 1 ? SlotListLayoutMode.Grid : SlotListLayoutMode.List;
        }

        /// <summary>Writes the values a Layout choice implies. Grids keep their slot count if it's already at least 2.</summary>
        public static void ApplyLayoutMode(SerializedObject serializedObject, SlotListLayoutMode mode)
        {
            SerializedProperty slotsPerLine = serializedObject.FindProperty(S + "slotsPerLine");
            SerializedProperty auto = serializedObject.FindProperty(S + "autoSlotsPerLine");

            auto.boolValue = mode == SlotListLayoutMode.AutoGrid;
            serializedObject.FindProperty(S + "bento").boolValue = mode == SlotListLayoutMode.Bento;

            // Bento takes its columns from its layout; Slots Per Line is only its fallback, so it's left alone.
            if (mode == SlotListLayoutMode.Bento)
                return;

            if (mode == SlotListLayoutMode.List)
                slotsPerLine.uintValue = 1;
            else if (slotsPerLine.uintValue < MIN_GRID_SLOTS)
                slotsPerLine.uintValue = MIN_GRID_SLOTS;
        }

        /// <summary>
        /// What's wrong with a bento list's setup: the errors in its Layout JSON. Empty outside bento, and for a list
        /// with no JSON (1x1 tiles).
        /// </summary>
        public static IReadOnlyList<string> BentoProblems(SerializedObject serializedObject)
        {
            List<string> problems = new();
            if (GetLayoutMode(serializedObject) != SlotListLayoutMode.Bento)
                return problems;

            SerializedProperty json = serializedObject.FindProperty(BENTO_JSON);
            if (json?.objectReferenceValue is TextAsset asset)
            {
                foreach (string error in BentoLayout.FromJson(asset.text).Errors)
                    problems.Add($"Layout JSON: {error} Items are placed as 1x1 tiles until it's fixed.");
            }

            return problems;
        }

        /// <summary>The label for a setting, in the words of the chosen direction.</summary>
        public static string LabelFor(string path, SlotListDirection direction)
        {
            bool vertical = direction is SlotListDirection.TopToBottom or SlotListDirection.BottomToTop;
            string start = direction switch
            {
                SlotListDirection.TopToBottom => "Top",
                SlotListDirection.BottomToTop => "Bottom",
                SlotListDirection.LeftToRight => "Left",
                _ => "Right"
            };
            string end = direction switch
            {
                SlotListDirection.TopToBottom => "Bottom",
                SlotListDirection.BottomToTop => "Top",
                SlotListDirection.LeftToRight => "Right",
                _ => "Left"
            };
            string lines = vertical ? "Row" : "Column";
            string slots = vertical ? "Column" : "Row";

            return path switch
            {
                S + "paddingStart" => "Padding " + start,
                S + "paddingEnd" => "Padding " + end,
                S + "lineSpacing" => lines + " Spacing",
                S + "slotSpacing" => slots + " Spacing",
                S + "slotsPerLine" => slots + "s",
                S + "targetSlotSize" => vertical ? "Target Column Width" : "Target Row Height",
                S + "minSlotsPerLine" => "Min " + slots + "s",
                S + "maxSlotsPerLine" => "Max " + slots + "s",
                BENTO_JSON => "Layout JSON",
                S + "sectionSpacing" => "Section Spacing",
                _ => null
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            SlotListDirection direction = (SlotListDirection)serializedObject.FindProperty(S + "direction").enumValueIndex;

            foreach (string entry in DrawnEntries(serializedObject))
            {
                if (entry.StartsWith(HEADER))
                {
                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField(entry.Substring(HEADER.Length), EditorStyles.boldLabel);
                }
                else if (entry == LAYOUT_MODE)
                {
                    DrawLayoutMode();
                }
                else
                {
                    DrawProperty(entry, direction);
                }
            }

            EditorGUI.EndDisabledGroup();

            DrawBentoProblems();
            DrawContentLayoutError();

            if (Application.isPlaying)
                EditorGUILayout.HelpBox("Settings are read-only while playing.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBentoProblems()
        {
            if (serializedObject.isEditingMultipleObjects)
                return;

            IReadOnlyList<string> problems = BentoProblems(serializedObject);
            if (problems.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n", problems), MessageType.Error);
        }

        /// <summary>An error, with a fix button, when a layout component would fight the list over Content's size.</summary>
        private void DrawContentLayoutError()
        {
            if (serializedObject.isEditingMultipleObjects || target is not SlotListBase list)
                return;

            List<Component> forbidden = ContentLayoutRules.FindForbidden(list.content, list.viewport);
            if (forbidden.Count == 0)
                return;

            EditorGUILayout.HelpBox(ContentLayoutRules.Describe(forbidden), MessageType.Error);
            if (!Application.isPlaying && GUILayout.Button("Remove Layout Components"))
            {
                // Fitters and groups first: Unity won't remove a component another one requires.
                foreach (Component component in forbidden)
                {
                    if (component != null && component is not LayoutElement)
                        Undo.DestroyObjectImmediate(component);
                }

                foreach (Component component in forbidden)
                {
                    if (component != null)
                        Undo.DestroyObjectImmediate(component);
                }
            }
        }

        private void DrawLayoutMode()
        {
            SlotListLayoutMode current = GetLayoutMode(serializedObject);
            SlotListLayoutMode chosen = (SlotListLayoutMode)EditorGUILayout.EnumPopup(
                new GUIContent("Layout", "List: one slot per row (or column). Grid: a fixed number. Auto Grid: as many as fit. Bento: tiles of different sizes, placed by a JSON layout."),
                current);

            if (chosen == current)
                return;

            ApplyLayoutMode(serializedObject, chosen);

            // The rows below were worked out for the old layout. Save and start the draw pass again,
            // so a field from the old layout can't write its value back over the new one.
            serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        private void DrawProperty(string path, SlotListDirection direction)
        {
            SerializedProperty property = serializedObject.FindProperty(path);
            if (property == null)
                return;

            string label = LabelFor(path, direction);
            GUIContent content = label != null ? new GUIContent(label, property.tooltip) : null;

            bool indent = INDENTED.Contains(path);
            if (indent)
                EditorGUI.indentLevel++;

            if (path == S + "slotsPerLine")
            {
                // Only write when the user changed it: an unconditional write could undo another field's change.
                EditorGUI.BeginChangeCheck();
                int value = EditorGUILayout.IntField(content, (int)property.uintValue);
                if (EditorGUI.EndChangeCheck())
                    property.uintValue = (uint)Mathf.Max(MIN_GRID_SLOTS, value);
            }
            else if (content != null)
            {
                EditorGUILayout.PropertyField(property, content, true);
            }
            else
            {
                EditorGUILayout.PropertyField(property, true);
            }

            if (indent)
                EditorGUI.indentLevel--;
        }
    }
}
