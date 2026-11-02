using RecyclableScroller.Runtime;
using UnityEditor;
using UnityEngine;

namespace RecyclableScroller.Editor
{
    [CustomEditor(typeof(RecyclableScrollRectBase), true)]
    public class RecyclableScrollRectBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty cellPrefabProp;
        private SerializedProperty lineSpacingProp;
        private SerializedProperty gridCellSpacingProp;
        private SerializedProperty paddingStartProp;
        private SerializedProperty paddingEndProp;
        private SerializedProperty isVerticalProp;
        private SerializedProperty reverseProp;
        private SerializedProperty gridCountProp;
        private SerializedProperty allowMultipleExpandedCellsProp;
        private SerializedProperty scrollToInitOnEnableProp;
        private SerializedProperty overridePoolPercentageProp;
        private SerializedProperty poolPercentageProp;
        private SerializedProperty estimatedCellHeightProp;
        private SerializedProperty cellsPerFrameProp;

        private enum ScrollDirection
        {
            VerticalNormal,
            VerticalReverse,
            HorizontalNormal,
            HorizontalReverse
        }

        private enum ScrollType
        {
            Single,
            Grid,
            SingleDynamicSize
        }

        private void OnEnable()
        {
            cellPrefabProp = serializedObject.FindProperty("cellPrefab");
            lineSpacingProp = serializedObject.FindProperty("lineSpacing");
            gridCellSpacingProp = serializedObject.FindProperty("gridCellSpacing");
            paddingStartProp = serializedObject.FindProperty("paddingStart");
            paddingEndProp = serializedObject.FindProperty("paddingEnd");
            isVerticalProp = serializedObject.FindProperty("isVertical");
            reverseProp = serializedObject.FindProperty("reverse");
            gridCountProp = serializedObject.FindProperty("gridCount");
            allowMultipleExpandedCellsProp = serializedObject.FindProperty("allowMultipleExpandedCells");
            scrollToInitOnEnableProp = serializedObject.FindProperty("scrollToInitOnEnable");
            overridePoolPercentageProp = serializedObject.FindProperty("overridePoolPercentage");
            poolPercentageProp = serializedObject.FindProperty("poolPercentage");
            estimatedCellHeightProp = serializedObject.FindProperty("estimatedCellHeight");
            cellsPerFrameProp = serializedObject.FindProperty("cellsPerFrame");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool isRuntime = Application.isPlaying;

            EditorGUI.BeginDisabledGroup(isRuntime);

            EditorGUILayout.PropertyField(cellPrefabProp, new GUIContent("Cell Prefab"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Layout Settings", EditorStyles.boldLabel);

            ScrollDirection currentDirection = GetCurrentDirection();
            ScrollDirection newDirection = (ScrollDirection)EditorGUILayout.EnumPopup("Scroll Direction", currentDirection);
            ApplyDirection(newDirection);

            ScrollType currentScrollType = GetCurrentScrollType();
            ScrollType newScrollType = (ScrollType)EditorGUILayout.EnumPopup("Scroll Type", currentScrollType);

            if (currentScrollType != newScrollType)
                ApplyScrollType(newScrollType);

            if (newScrollType == ScrollType.SingleDynamicSize && estimatedCellHeightProp != null && cellsPerFrameProp != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    "SingleDynamicSize mode requires Initialize() to be called with an ICellSizeCalculator. " +
                    "Each cell's height is calculated based on its content.",
                    MessageType.Info);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(estimatedCellHeightProp, new GUIContent("Estimated Cell Height", "Used for initial pool sizing before actual sizes are calculated"));
                EditorGUILayout.PropertyField(cellsPerFrameProp, new GUIContent("Cells Per Frame", "Number of cells to calculate per frame during batch processing"));

                int allowMultipleIndex = allowMultipleExpandedCellsProp.boolValue ? 1 : 0;
                int newAllowMultipleIndex = EditorGUILayout.Popup("Allow Multiple Expanded", allowMultipleIndex, new[] { "False", "True" });
                allowMultipleExpandedCellsProp.boolValue = newAllowMultipleIndex == 1;
                EditorGUI.indentLevel--;
            }
            else if (newScrollType == ScrollType.Grid)
            {
                EditorGUI.indentLevel++;
                int newGridCount = EditorGUILayout.IntField("Grid Count", gridCountProp.intValue);
                if (newGridCount < 2)
                    newGridCount = 2;
                gridCountProp.intValue = newGridCount;

                EditorGUILayout.PropertyField(gridCellSpacingProp, new GUIContent("Grid Cell Spacing"));
                EditorGUI.indentLevel--;
            }
            else if (newScrollType == ScrollType.Single)
            {
                int allowMultipleIndex = allowMultipleExpandedCellsProp.boolValue ? 1 : 0;
                int newAllowMultipleIndex = EditorGUILayout.Popup("Allow Multiple Expanded", allowMultipleIndex, new[] { "False", "True" });
                allowMultipleExpandedCellsProp.boolValue = newAllowMultipleIndex == 1;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Spacing & Padding", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(lineSpacingProp, new GUIContent("Line Spacing"));
            EditorGUILayout.PropertyField(paddingStartProp, new GUIContent("Padding Start"));
            EditorGUILayout.PropertyField(paddingEndProp, new GUIContent("Padding End"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);

            int scrollToInitIndex = scrollToInitOnEnableProp.boolValue ? 1 : 0;
            int newScrollToInitIndex = EditorGUILayout.Popup("Scroll To Init On Enable", scrollToInitIndex, new[] { "False", "True" });
            scrollToInitOnEnableProp.boolValue = newScrollToInitIndex == 1;

            EditorGUILayout.Space(5);

            // Toggle para override pool percentage
            bool overridePool = overridePoolPercentageProp.boolValue;
            bool newOverridePool = EditorGUILayout.Toggle("Override Pool Percentage", overridePool);
            overridePoolPercentageProp.boolValue = newOverridePool;

            // Mostrar slider solo si override está activado
            if (newOverridePool)
            {
                EditorGUI.indentLevel++;
                float poolPercentage = poolPercentageProp.floatValue;
                float newPoolPercentage = EditorGUILayout.Slider("Pool Percentage", poolPercentage, 1.4f, 2.5f);
                poolPercentageProp.floatValue = newPoolPercentage;

                // Mostrar valor actual como porcentaje
                int percentageValue = Mathf.RoundToInt(newPoolPercentage * 100f);
                EditorGUILayout.HelpBox($"Current: {percentageValue}% (Default: 120% for 20% buffer)", MessageType.Info);
                EditorGUI.indentLevel--;
            }


            EditorGUI.EndDisabledGroup();


            if (isRuntime)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("Settings are read-only during runtime.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private ScrollDirection GetCurrentDirection()
        {
            bool isVertical = isVerticalProp.boolValue;
            bool isReverse = reverseProp.boolValue;

            if (isVertical && !isReverse)
                return ScrollDirection.VerticalNormal;
            if (isVertical && isReverse)
                return ScrollDirection.VerticalReverse;
            if (!isVertical && !isReverse)
                return ScrollDirection.HorizontalNormal;

            return ScrollDirection.HorizontalReverse;
        }

        private void ApplyDirection(ScrollDirection direction)
        {
            switch (direction)
            {
                case ScrollDirection.VerticalNormal:
                    isVerticalProp.boolValue = true;
                    reverseProp.boolValue = false;
                    break;
                case ScrollDirection.VerticalReverse:
                    isVerticalProp.boolValue = true;
                    reverseProp.boolValue = true;
                    break;
                case ScrollDirection.HorizontalNormal:
                    isVerticalProp.boolValue = false;
                    reverseProp.boolValue = false;
                    break;
                case ScrollDirection.HorizontalReverse:
                    isVerticalProp.boolValue = false;
                    reverseProp.boolValue = true;
                    break;
            }
        }

        private ScrollType GetCurrentScrollType()
        {
            int gridCount = gridCountProp.intValue;

            if (gridCount > 1)
                return ScrollType.Grid;

            if (estimatedCellHeightProp != null && estimatedCellHeightProp.floatValue > 0f)
                return ScrollType.SingleDynamicSize;

            return ScrollType.Single;
        }

        private void ApplyScrollType(ScrollType scrollType)
        {
            switch (scrollType)
            {
                case ScrollType.Single:
                    gridCountProp.intValue = 1;
                    if (estimatedCellHeightProp != null)
                        estimatedCellHeightProp.floatValue = 0f;
                    break;
                case ScrollType.Grid:
                    gridCountProp.intValue = 2;
                    if (estimatedCellHeightProp != null)
                        estimatedCellHeightProp.floatValue = 0f;
                    allowMultipleExpandedCellsProp.boolValue = false;
                    break;
                case ScrollType.SingleDynamicSize:
                    gridCountProp.intValue = 1;
                    if (estimatedCellHeightProp != null && estimatedCellHeightProp.floatValue == 0f)
                        estimatedCellHeightProp.floatValue = 100f;
                    break;
            }
        }
    }
}
