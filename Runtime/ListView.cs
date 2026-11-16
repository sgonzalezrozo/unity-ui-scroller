using System;

namespace UIScroller
{
    /// <summary>
    /// A read-only window on the first <see cref="Length"/> entries of a buffer that may be larger. Lets the layout
    /// keep spare capacity and reuse its arrays instead of allocating a new one on every rebuild (P11).
    /// </summary>
    public readonly struct ListView<T>
    {
        private readonly T[] items;

        public readonly int Length;

        internal ListView(T[] items, int length)
        {
            this.items = items ?? Array.Empty<T>();
            Length = length;
        }

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Length)
                    throw new IndexOutOfRangeException($"Index {index} is outside 0..{Length - 1}.");
                return items[index];
            }
        }

        public Enumerator GetEnumerator() => new(items, Length);

        /// <summary>Allocation-free enumerator for foreach.</summary>
        public struct Enumerator
        {
            private readonly T[] items;
            private readonly int length;
            private int index;

            internal Enumerator(T[] items, int length)
            {
                this.items = items;
                this.length = length;
                index = -1;
            }

            public T Current => items[index];

            public bool MoveNext() => ++index < length;
        }
    }
}
