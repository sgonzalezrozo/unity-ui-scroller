using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UIScroller.Editor
{
    /// <summary>
    /// Window > UI Scroller > Bento Layout Editor (also the Edit Layout button next to a list's Layout JSON): edits a bento
    /// layout JSON visually. Patterns are drawn on a grid (drag over cells to make a tile, right-click a tile to remove
    /// it), spans are a list of sizes, and a preview shows how the first items are placed. Saves back to the same JSON.
    /// </summary>
    public class BentoLayoutWindow : EditorWindow
    {
        private const string MENU = "Window/UI Scroller/Bento Layout Editor";
        private const float CELL = 30f;
        private const float GRID_PADDING = 4f;
        private const int DEFAULT_COLUMNS = 4;
        private const int MAX_EDITOR_COLUMNS = 12;
        private const int DEFAULT_PREVIEW_ITEMS = 24;
        private const int MAX_PREVIEW_ITEMS = 200;
        private const int PREVIEW_MAX_ROWS = 60;
        private static readonly string[] MODES = { "Patterns", "Spans" };
        private static readonly Color EMPTY_CELL = new(0.5f, 0.5f, 0.5f, 0.15f);
        private static readonly Color GRID_LINE = new(0f, 0f, 0f, 0.25f);
        private static readonly Color DRAG_COLOR = new(1f, 1f, 1f, 0.35f);

        [SerializeField] private TextAsset asset;
        [SerializeField] private int mode;
        [SerializeField] private int previewItems = DEFAULT_PREVIEW_ITEMS;

        private BentoLayoutData data;
        private bool dirty;
        private Vector2 scroll;
        private int dragPattern = -1;
        private Vector2Int dragStart;
        private Vector2Int dragEnd;
        private GUIStyle tileLabel;

        [MenuItem(MENU)]
        private static void OpenEmpty()
            => Open(null);

        /// <summary>Opens the editor on a layout JSON (or with nothing loaded).</summary>
        public static void Open(TextAsset layoutJson)
        {
            BentoLayoutWindow window = GetWindow<BentoLayoutWindow>();
            window.titleContent = new GUIContent("Bento Layout");
            window.Load(layoutJson);
            window.Show();
        }

        /// <summary>Asks where to save a new layout JSON, writes a starting layout there and returns the asset (or null).</summary>
        public static TextAsset CreateLayoutAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("New Bento Layout", "BentoLayout", "json", "Where to save the bento layout JSON.");
            if (string.IsNullOrEmpty(path))
                return null;

            File.WriteAllText(path, JsonUtility.ToJson(StartingLayout(), true));
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }

        internal static BentoLayoutData StartingLayout()
        {
            BentoPatternData featured = new()
            {
                rows = 2,
                tiles = new[]
                {
                    new BentoTileData { c = 0, r = 0, w = 2, h = 2 },
                    new BentoTileData { c = 2, r = 0, w = 2, h = 1 },
                    new BentoTileData { c = 2, r = 1, w = 1, h = 1 },
                    new BentoTileData { c = 3, r = 1, w = 1, h = 1 },
                },
            };
            BentoPatternData strip = new()
            {
                rows = 1,
                tiles = new[]
                {
                    new BentoTileData { c = 0, r = 0, w = 1, h = 1 },
                    new BentoTileData { c = 1, r = 0, w = 1, h = 1 },
                    new BentoTileData { c = 2, r = 0, w = 1, h = 1 },
                    new BentoTileData { c = 3, r = 0, w = 1, h = 1 },
                },
            };
            return new BentoLayoutData { columns = DEFAULT_COLUMNS, repeatFrom = 1, patterns = new[] { featured, strip } };
        }

        private void Load(TextAsset layoutJson)
        {
            asset = layoutJson;
            data = null;
            dirty = false;
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
                data = JsonUtility.FromJson<BentoLayoutData>(asset.text);

            data ??= new BentoLayoutData { columns = DEFAULT_COLUMNS };
            data.spans ??= new BentoSpanData[0];
            data.patterns ??= new BentoPatternData[0];
            mode = data.patterns.Length == 0 && data.spans.Length > 0 ? 1 : 0;
        }

        private void OnEnable()
        {
            if (data == null)
                Load(asset);
        }

        private void OnGUI()
        {
            tileLabel ??= new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            DrawToolbar();
            if (data == null)
                return;

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawSettings();
            EditorGUILayout.Space(8);
            if (mode == 0)
                DrawPatterns();
            else
                DrawSpans();

            EditorGUILayout.Space(8);
            BentoLayout layout = BentoLayout.FromData(BuildData());
            DrawErrors(layout);
            DrawPreview(layout);
            EditorGUILayout.EndScrollView();
        }

        // ---------- Toolbar and settings ----------

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            TextAsset picked = (TextAsset)EditorGUILayout.ObjectField(asset, typeof(TextAsset), false, GUILayout.MinWidth(160f));
            if (picked != asset && ConfirmDiscard())
                Load(picked);

            if (GUILayout.Button("New…", EditorStyles.toolbarButton, GUILayout.Width(50f)) && ConfirmDiscard())
            {
                TextAsset created = CreateLayoutAsset();
                if (created != null)
                    Load(created);
            }

            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(asset == null || !dirty);
            if (GUILayout.Button(dirty ? "Save*" : "Save", EditorStyles.toolbarButton, GUILayout.Width(55f)))
                Save();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (asset == null)
                EditorGUILayout.HelpBox("Pick a layout JSON, or make one with New. Changes are kept here until you save to a file.", MessageType.Info);
        }

        private bool ConfirmDiscard()
            => !dirty || EditorUtility.DisplayDialog("Bento Layout", "Discard the unsaved changes?", "Discard", "Cancel");

        private void DrawSettings()
        {
            EditorGUI.BeginChangeCheck();
            data.columns = EditorGUILayout.IntSlider("Columns", Mathf.Max(1, data.columns), 1, MAX_EDITOR_COLUMNS);
            int sequenceLength = mode == 0 ? data.patterns.Length : data.spans.Length;
            data.repeatFrom = EditorGUILayout.IntSlider(new GUIContent("Repeat From", "After the last entry, the sequence starts again from this one."), data.repeatFrom, 0, Mathf.Max(0, sequenceLength - 1));
            int newMode = GUILayout.Toolbar(mode, MODES);
            if (EditorGUI.EndChangeCheck())
            {
                mode = newMode;
                dirty = true;
            }

            EditorGUILayout.LabelField(mode == 0
                ? "Patterns: blocks of rows filled one after another. Drag over cells to make a tile; right-click a tile to remove it."
                : "Spans: tile sizes packed in order, each in the first gap where it fits.", EditorStyles.wordWrappedMiniLabel);
        }

        // ---------- Patterns ----------

        private void DrawPatterns()
        {
            for (int i = 0; i < data.patterns.Length; i++)
            {
                BentoPatternData pattern = data.patterns[i];
                pattern.tiles ??= new BentoTileData[0];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Pattern {i}{(i == data.repeatFrom ? "  (repeats from here)" : string.Empty)}", EditorStyles.boldLabel);
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(24f)) && i > 0)
                    SwapPatterns(i, i - 1);
                if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(24f)) && i < data.patterns.Length - 1)
                    SwapPatterns(i, i + 1);
                if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(24f)))
                {
                    RemovePattern(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                pattern.rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", Mathf.Max(1, pattern.rows)));
                if (EditorGUI.EndChangeCheck())
                {
                    RemoveTilesOutside(pattern);
                    dirty = true;
                }

                DrawPatternGrid(i, pattern);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Pattern"))
            {
                List<BentoPatternData> patterns = new(data.patterns) { new BentoPatternData { rows = 1, tiles = new BentoTileData[0] } };
                data.patterns = patterns.ToArray();
                dirty = true;
            }
        }

        private void DrawPatternGrid(int patternIndex, BentoPatternData pattern)
        {
            Rect grid = GridRect(data.columns, pattern.rows);
            DrawEmptyCells(grid, data.columns, pattern.rows);
            for (int t = 0; t < pattern.tiles.Length; t++)
            {
                BentoTileData tile = pattern.tiles[t];
                DrawTile(grid, tile.c, tile.r, tile.w, tile.h, t, t.ToString());
            }

            if (dragPattern == patternIndex)
            {
                RectInt selection = Selection();
                EditorGUI.DrawRect(CellsRect(grid, selection.x, selection.y, selection.width, selection.height), DRAG_COLOR);
            }

            HandleGridInput(grid, patternIndex, pattern);
        }

        private void HandleGridInput(Rect grid, int patternIndex, BentoPatternData pattern)
        {
            Event current = Event.current;
            if (!grid.Contains(current.mousePosition) && current.type != EventType.MouseUp && current.type != EventType.MouseDrag)
                return;

            Vector2Int cell = CellAt(grid, current.mousePosition, data.columns, pattern.rows);
            switch (current.type)
            {
                case EventType.MouseDown when current.button == 0:
                    dragPattern = patternIndex;
                    dragStart = cell;
                    dragEnd = cell;
                    current.Use();
                    break;
                case EventType.MouseDown when current.button == 1:
                    RemoveTileAt(pattern, cell);
                    current.Use();
                    break;
                case EventType.MouseDrag when dragPattern == patternIndex:
                    dragEnd = cell;
                    current.Use();
                    Repaint();
                    break;
                case EventType.MouseUp when dragPattern == patternIndex:
                    AddTile(pattern, Selection());
                    dragPattern = -1;
                    current.Use();
                    break;
            }
        }

        private RectInt Selection()
        {
            int minColumn = Mathf.Min(dragStart.x, dragEnd.x);
            int minRow = Mathf.Min(dragStart.y, dragEnd.y);
            return new RectInt(minColumn, minRow, Mathf.Abs(dragEnd.x - dragStart.x) + 1, Mathf.Abs(dragEnd.y - dragStart.y) + 1);
        }

        /// <summary>Adds a tile over the selected cells, replacing any tile it overlaps.</summary>
        private void AddTile(BentoPatternData pattern, RectInt cells)
        {
            List<BentoTileData> tiles = new();
            foreach (BentoTileData tile in pattern.tiles)
            {
                if (!Overlaps(tile, cells))
                    tiles.Add(tile);
            }

            tiles.Add(new BentoTileData { c = cells.x, r = cells.y, w = cells.width, h = cells.height });
            tiles.Sort(CompareReadingOrder);
            pattern.tiles = tiles.ToArray();
            dirty = true;
        }

        private void RemoveTileAt(BentoPatternData pattern, Vector2Int cell)
        {
            List<BentoTileData> tiles = new();
            foreach (BentoTileData tile in pattern.tiles)
            {
                if (!Overlaps(tile, new RectInt(cell.x, cell.y, 1, 1)))
                    tiles.Add(tile);
            }

            if (tiles.Count == pattern.tiles.Length)
                return;

            pattern.tiles = tiles.ToArray();
            dirty = true;
        }

        private void RemoveTilesOutside(BentoPatternData pattern)
        {
            List<BentoTileData> tiles = new();
            foreach (BentoTileData tile in pattern.tiles)
            {
                if (tile.c + tile.w <= data.columns && tile.r + tile.h <= pattern.rows)
                    tiles.Add(tile);
            }

            pattern.tiles = tiles.ToArray();
        }

        private static bool Overlaps(BentoTileData tile, RectInt cells)
            => tile.c < cells.xMax && tile.c + tile.w > cells.x && tile.r < cells.yMax && tile.r + tile.h > cells.y;

        private static int CompareReadingOrder(BentoTileData a, BentoTileData b)
            => a.r != b.r ? a.r.CompareTo(b.r) : a.c.CompareTo(b.c);

        private void SwapPatterns(int a, int b)
        {
            (data.patterns[a], data.patterns[b]) = (data.patterns[b], data.patterns[a]);
            dirty = true;
        }

        private void RemovePattern(int index)
        {
            List<BentoPatternData> patterns = new(data.patterns);
            patterns.RemoveAt(index);
            data.patterns = patterns.ToArray();
            data.repeatFrom = Mathf.Clamp(data.repeatFrom, 0, Mathf.Max(0, patterns.Count - 1));
            dirty = true;
        }

        // ---------- Spans ----------

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
