using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UIScroller.Editor
{
    /// <summary>The span list, the layout check, the live preview, drawing and saving.</summary>
    public partial class BentoLayoutWindow
    {
        private void DrawSpans()
        {
            for (int i = 0; i < data.spans.Length; i++)
            {
                BentoSpanData span = data.spans[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i}{(i == data.repeatFrom ? " ↻" : string.Empty)}", GUILayout.Width(36f));
                EditorGUI.BeginChangeCheck();
                EditorGUIUtility.labelWidth = 14f;
                span.w = Mathf.Clamp(EditorGUILayout.IntField("W", Mathf.Max(1, span.w), GUILayout.Width(60f)), 1, Mathf.Max(1, data.columns));
                span.h = Mathf.Max(1, EditorGUILayout.IntField("H", Mathf.Max(1, span.h), GUILayout.Width(60f)));
                EditorGUIUtility.labelWidth = 0f;
                if (EditorGUI.EndChangeCheck())
                    dirty = true;

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(24f)) && i > 0)
                    SwapSpans(i, i - 1);
                if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(24f)) && i < data.spans.Length - 1)
                    SwapSpans(i, i + 1);
                bool remove = GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(24f));
                EditorGUILayout.EndHorizontal();
                if (remove)
                {
                    List<BentoSpanData> spans = new(data.spans);
                    spans.RemoveAt(i);
                    data.spans = spans.ToArray();
                    data.repeatFrom = Mathf.Clamp(data.repeatFrom, 0, Mathf.Max(0, spans.Count - 1));
                    dirty = true;
                    break;
                }
            }

            if (GUILayout.Button("Add Span"))
            {
                List<BentoSpanData> spans = new(data.spans) { new BentoSpanData { w = 1, h = 1 } };
                data.spans = spans.ToArray();
                dirty = true;
            }
        }

        private void SwapSpans(int a, int b)
        {
            (data.spans[a], data.spans[b]) = (data.spans[b], data.spans[a]);
            dirty = true;
        }

        // ---------- Checks and preview ----------

        /// <summary>The layout as it will be saved: only the sequence of the current mode.</summary>
        private BentoLayoutData BuildData()
            => new()
            {
                columns = data.columns,
                repeatFrom = data.repeatFrom,
                patterns = mode == 0 ? data.patterns : null,
                spans = mode == 1 ? data.spans : null,
            };

        private static void DrawErrors(BentoLayout layout)
        {
            if (!layout.IsValid)
                EditorGUILayout.HelpBox("The list would place items as 1x1 tiles:\n- " + string.Join("\n- ", layout.Errors), MessageType.Error);
        }

        private void DrawPreview(BentoLayout layout)
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            previewItems = EditorGUILayout.IntSlider("Items", previewItems, 1, MAX_PREVIEW_ITEMS);
            if (!layout.IsValid)
                return;

            int columns = layout.Columns > 0 ? layout.Columns : data.columns;
            BentoArranger arranger = layout.CreateArranger(columns);
            arranger.Reset();
            List<BentoTile> tiles = new(previewItems);
            int rows = 0;
            for (int i = 0; i < previewItems; i++)
            {
                BentoTile tile = arranger.Next();
                if (tile.Row >= PREVIEW_MAX_ROWS)
                    break;

                tiles.Add(tile);
                rows = Mathf.Max(rows, tile.Row + tile.Rows);
            }

            Rect grid = GridRect(columns, Mathf.Max(1, rows));
            DrawEmptyCells(grid, columns, Mathf.Max(1, rows));
            for (int i = 0; i < tiles.Count; i++)
                DrawTile(grid, tiles[i].Column, tiles[i].Row, tiles[i].Columns, tiles[i].Rows, i, i.ToString());
        }

        // ---------- Drawing ----------

        private static Rect GridRect(int columns, int rows)
        {
            Rect rect = GUILayoutUtility.GetRect(columns * CELL + GRID_PADDING * 2f, rows * CELL + GRID_PADDING * 2f, GUILayout.ExpandWidth(false));
            return new Rect(rect.x + GRID_PADDING, rect.y + GRID_PADDING, columns * CELL, rows * CELL);
        }

        private static void DrawEmptyCells(Rect grid, int columns, int rows)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    Rect cell = CellsRect(grid, column, row, 1, 1);
                    EditorGUI.DrawRect(cell, GRID_LINE);
                    EditorGUI.DrawRect(new Rect(cell.x + 1f, cell.y + 1f, cell.width - 2f, cell.height - 2f), EMPTY_CELL);
                }
            }
        }

        private void DrawTile(Rect grid, int column, int row, int width, int height, int colorIndex, string label)
        {
            Rect rect = CellsRect(grid, column, row, width, height);
            Rect inner = new(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
            EditorGUI.DrawRect(inner, TileColor(colorIndex));
            GUI.Label(inner, label, tileLabel);
        }

        private static Rect CellsRect(Rect grid, int column, int row, int width, int height)
            => new(grid.x + column * CELL, grid.y + row * CELL, width * CELL, height * CELL);

        private static Vector2Int CellAt(Rect grid, Vector2 position, int columns, int rows)
        {
            int column = Mathf.Clamp(Mathf.FloorToInt((position.x - grid.x) / CELL), 0, columns - 1);
            int row = Mathf.Clamp(Mathf.FloorToInt((position.y - grid.y) / CELL), 0, rows - 1);
            return new Vector2Int(column, row);
        }

        private static Color TileColor(int index)
        {
            const float GOLDEN_RATIO = 0.618034f;
            float hue = index * GOLDEN_RATIO % 1f;
            return Color.HSVToRGB(hue, 0.45f, 0.85f);
        }

        // ---------- Saving ----------

        private void Save()
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                return;

            File.WriteAllText(path, JsonUtility.ToJson(BuildData(), true));
            AssetDatabase.ImportAsset(path);
            asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            dirty = false;
        }
    }
}
