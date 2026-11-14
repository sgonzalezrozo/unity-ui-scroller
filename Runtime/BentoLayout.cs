using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotList
{
    /// <summary>
    /// How a bento list places its items: a sequence of spans packed in order, or a sequence of patterns, over a fixed
    /// number of columns. Build it from JSON (<see cref="FromJson"/>), from <see cref="BentoLayoutData"/>, or in code.
    /// Every input is checked: a layout with errors keeps them in <see cref="Errors"/> and places items as 1x1 tiles,
    /// so bad data never breaks the screen.
    /// </summary>
    public sealed class BentoLayout
    {
        public const int MAX_COLUMNS = 64;

        private static readonly BentoSpan[] OneByOne = { BentoSpan.One };

        private readonly List<string> errors = new();
        private BentoSpan[] spans;
        private BentoPattern[] patterns;
        private BentoTile[][] sortedTiles;
        private int repeatFrom;

        /// <summary>The number of columns, or 0 when the layout didn't give a valid one (the list then uses its own).</summary>
        public int Columns { get; private set; }

        /// <summary>The tallest tile, in rows.</summary>
        public int MaxRowSpan { get; private set; } = 1;

        public bool IsValid => errors.Count == 0;
        public IReadOnlyList<string> Errors => errors;

        private BentoLayout()
        {
        }

        /// <summary>A grid of 1x1 tiles.</summary>
        public static BentoLayout Uniform(int columns)
            => FromSpans(columns, null);

        /// <summary>Spans packed in order, repeating from <paramref name="repeatFrom"/> after the last one. No spans means 1x1 tiles.</summary>
        public static BentoLayout FromSpans(int columns, IReadOnlyList<BentoSpan> spans, int repeatFrom = 0)
        {
            BentoLayout layout = new();
            layout.SetColumns(columns);
            layout.SetSpans(spans, repeatFrom);
            return layout;
        }

        /// <summary>Patterns filled one after another, repeating from <paramref name="repeatFrom"/> after the last one.</summary>
        public static BentoLayout FromPatterns(int columns, IReadOnlyList<BentoPattern> patterns, int repeatFrom = 0)
        {
            BentoLayout layout = new();
            layout.SetColumns(columns);
            layout.SetPatterns(patterns, repeatFrom);
            return layout;
        }

        /// <summary>Reads a layout written as JSON. See <see cref="BentoLayoutData"/> for the format.</summary>
        public static BentoLayout FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Failed("The bento layout JSON is empty.");

            BentoLayoutData data;
            try
            {
                data = JsonUtility.FromJson<BentoLayoutData>(json);
            }
            catch (ArgumentException exception)
            {
                return Failed($"The bento layout isn't valid JSON: {exception.Message}");
            }

            return FromData(data);
        }

        /// <summary>A layout from its data form, however it was read.</summary>
        public static BentoLayout FromData(BentoLayoutData data)
        {
            if (data == null)
                return Failed("The bento layout data is missing.");

            bool hasSpans = data.spans != null && data.spans.Length > 0;
            bool hasPatterns = data.patterns != null && data.patterns.Length > 0;

            BentoLayout layout = new();
            layout.SetColumns(data.columns);
            if (hasSpans && hasPatterns)
            {
                layout.errors.Add("A bento layout uses either spans or patterns, not both.");
                return layout;
            }

            if (hasPatterns)
                layout.SetPatterns(ToPatterns(data.patterns), data.repeatFrom);
            else
                layout.SetSpans(hasSpans ? ToSpans(data.spans) : null, data.repeatFrom);

            return layout;
        }

        /// <summary>The arranger for this layout. A layout with errors arranges 1x1 tiles over its columns, or <paramref name="fallbackColumns"/>.</summary>
        internal BentoArranger CreateArranger(int fallbackColumns = 1)
        {
            if (!IsValid || (spans == null && patterns == null))
            {
                int columns = Columns > 0 ? Columns : Mathf.Clamp(fallbackColumns, 1, MAX_COLUMNS);
                return new SpanArranger(columns, OneByOne, 0);
            }

            if (patterns != null)
                return new PatternArranger(patterns, sortedTiles, repeatFrom);

            return new SpanArranger(Columns, spans, repeatFrom);
        }

        // ---------- Building and checking ----------

        private static BentoLayout Failed(string error)
        {
            BentoLayout layout = new();
            layout.errors.Add(error);
            return layout;
        }

        private void SetColumns(int columns)
        {
            if (columns >= 1 && columns <= MAX_COLUMNS)
            {
                Columns = columns;
                return;
            }

            errors.Add($"A bento layout's columns must be between 1 and {MAX_COLUMNS} (was {columns}).");
        }

        private void SetSpans(IReadOnlyList<BentoSpan> source, int repeat)
        {
            if (source == null || source.Count == 0)
            {
                spans = OneByOne;
                return;
            }

            if (!CheckRepeatFrom(repeat, source.Count))
                return;

            BentoSpan[] copy = new BentoSpan[source.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                BentoSpan span = source[i];
                if (span.Columns < 1 || span.Rows < 1)
                    errors.Add($"Bento spans[{i}] needs w and h of at least 1 (was {span}).");
                else if (Columns > 0 && span.Columns > Columns)
                    errors.Add($"Bento spans[{i}] is {span.Columns} columns wide, but the layout has {Columns} columns.");

                copy[i] = span;
                MaxRowSpan = Mathf.Max(MaxRowSpan, span.Rows);
            }

            spans = copy;
            repeatFrom = repeat;
        }

        private void SetPatterns(IReadOnlyList<BentoPattern> source, int repeat)
        {
            if (source == null || source.Count == 0)
            {
                errors.Add("A bento layout needs at least one pattern.");
                return;
            }

            if (!CheckRepeatFrom(repeat, source.Count))
                return;

            BentoPattern[] copy = new BentoPattern[source.Count];
            BentoTile[][] sorted = new BentoTile[source.Count][];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = source[i];
                sorted[i] = CheckPattern(i, source[i]);
            }

            patterns = copy;
            sortedTiles = sorted;
            repeatFrom = repeat;
        }

        /// <summary>Checks a pattern and returns its tiles in reading order.</summary>
        private BentoTile[] CheckPattern(int index, BentoPattern pattern)
        {
            BentoTile[] tiles = new BentoTile[pattern?.Tiles.Count ?? 0];
            if (pattern == null || pattern.Rows < 1)
            {
                errors.Add($"Bento patterns[{index}] needs rows of at least 1.");
                return tiles;
            }

            if (tiles.Length == 0)
            {
                errors.Add($"Bento patterns[{index}] has no tiles.");
                return tiles;
            }

            int columns = Columns > 0 ? Columns : MAX_COLUMNS;
            for (int t = 0; t < tiles.Length; t++)
            {
                BentoTile tile = pattern.Tiles[t];
                tiles[t] = tile;
                MaxRowSpan = Mathf.Max(MaxRowSpan, tile.Rows);

                if (tile.Columns < 1 || tile.Rows < 1)
                    errors.Add($"Bento patterns[{index}] tiles[{t}] needs w and h of at least 1 (was {tile}).");
                else if (tile.Column < 0 || tile.Row < 0 || tile.Column + tile.Columns > columns || tile.Row + tile.Rows > pattern.Rows)
                    errors.Add($"Bento patterns[{index}] tiles[{t}] {tile} is outside the pattern ({columns} columns x {pattern.Rows} rows).");
            }

            if (IsValid)
                CheckOverlaps(index, tiles);

            Array.Sort(tiles, ReadingOrder.Instance);
            return tiles;
        }

        private void CheckOverlaps(int index, BentoTile[] tiles)
        {
            for (int a = 0; a < tiles.Length; a++)
            for (int b = a + 1; b < tiles.Length; b++)
            {
                if (!Overlap(tiles[a], tiles[b]))
                    continue;

                errors.Add($"Bento patterns[{index}]: tiles[{a}] {tiles[a]} and tiles[{b}] {tiles[b]} overlap.");
                return;
            }
        }

        private static bool Overlap(BentoTile a, BentoTile b)
            => a.Column < b.Column + b.Columns && b.Column < a.Column + a.Columns
            && a.Row < b.Row + b.Rows && b.Row < a.Row + a.Rows;

        private bool CheckRepeatFrom(int repeat, int count)
        {
            if (repeat >= 0 && repeat < count)
                return true;

            errors.Add($"A bento layout's repeatFrom must be between 0 and {count - 1} (was {repeat}).");
            return false;
        }

        private static BentoSpan[] ToSpans(BentoSpanData[] data)
        {
            BentoSpan[] result = new BentoSpan[data.Length];
            for (int i = 0; i < data.Length; i++)
                result[i] = data[i] != null ? new BentoSpan(data[i].w, data[i].h) : default;
            return result;
        }

        private static BentoPattern[] ToPatterns(BentoPatternData[] data)
        {
            BentoPattern[] result = new BentoPattern[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                BentoPatternData pattern = data[i];
                int tileCount = pattern?.tiles?.Length ?? 0;
                BentoTile[] tiles = new BentoTile[tileCount];
                for (int t = 0; t < tileCount; t++)
                {
                    BentoTileData tile = pattern.tiles[t];
                    tiles[t] = tile != null ? new BentoTile(tile.c, tile.r, tile.w, tile.h) : default;
                }

                result[i] = new BentoPattern(pattern?.rows ?? 0, tiles);
            }

            return result;
        }

        /// <summary>Top row first, then left to right.</summary>
        private sealed class ReadingOrder : IComparer<BentoTile>
        {
            public static readonly ReadingOrder Instance = new();

            public int Compare(BentoTile a, BentoTile b)
                => a.Row != b.Row ? a.Row.CompareTo(b.Row) : a.Column.CompareTo(b.Column);
        }
    }
}
