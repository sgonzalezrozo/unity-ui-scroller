using System;

namespace SlotList
{
    /// <summary>A bento tile's size in cells: how many columns wide and how many rows tall.</summary>
    public readonly struct BentoSpan : IEquatable<BentoSpan>
    {
        public static readonly BentoSpan One = new(1, 1);

        public readonly int Columns;
        public readonly int Rows;

        public BentoSpan(int columns, int rows)
        {
            Columns = columns;
            Rows = rows;
        }

        public bool Equals(BentoSpan other) => Columns == other.Columns && Rows == other.Rows;
        public override bool Equals(object obj) => obj is BentoSpan other && Equals(other);
        public override int GetHashCode() => Columns * 397 ^ Rows;
        public override string ToString() => $"{Columns}x{Rows}";
    }

    /// <summary>Where an item sits in a bento layout: its first column and row, and its span in cells.</summary>
    public readonly struct BentoTile : IEquatable<BentoTile>
    {
        public readonly int Column;
        public readonly int Row;
        public readonly int Columns;
        public readonly int Rows;

        public BentoTile(int column, int row, int columns, int rows)
        {
            Column = column;
            Row = row;
            Columns = columns;
            Rows = rows;
        }

        public BentoSpan Span => new(Columns, Rows);

        public bool Equals(BentoTile other)
            => Column == other.Column && Row == other.Row && Columns == other.Columns && Rows == other.Rows;

        public override bool Equals(object obj) => obj is BentoTile other && Equals(other);
        public override int GetHashCode() => ((Column * 397 ^ Row) * 397 ^ Columns) * 397 ^ Rows;
        public override string ToString() => $"({Column},{Row} {Columns}x{Rows})";
    }
}
