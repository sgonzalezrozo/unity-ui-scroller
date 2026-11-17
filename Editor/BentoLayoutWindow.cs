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
    public partial class BentoLayoutWindow : EditorWindow
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
    }
}
