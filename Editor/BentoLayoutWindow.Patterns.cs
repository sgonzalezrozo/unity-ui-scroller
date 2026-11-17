using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UIScroller.Editor
{
    /// <summary>The pattern grids: drag over cells to make a tile, right-click a tile to remove it.</summary>
    public partial class BentoLayoutWindow
    {
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
    }
}
