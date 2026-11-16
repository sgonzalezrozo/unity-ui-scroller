using System;
using System.Collections.Generic;

namespace SlotList
{
    /// <summary>What an entry of a <see cref="SectionedSource{TItem, THeader}"/> is.</summary>
    public enum EntryKind
    {
        Header,
        Item
    }

    /// <summary>
    /// A list made of sections, each a header followed by the items of its own source. A SectionedSlotList shows it as
    /// one flat list of entries: header, items, header, items...
    /// Empty sections are skipped unless <see cref="SkipEmptySections"/> is false. Inner sources don't announce their
    /// changes: after changing one, call the list's Refresh (or <see cref="Recount"/>).
    /// </summary>
    public sealed class SectionedSource<TItem, THeader>
        where TItem : class
        where THeader : class
    {
        private readonly struct Section
        {
            public readonly THeader Header;
            public readonly IItemSource<TItem> Items;

            public Section(THeader header, IItemSource<TItem> items)
            {
                Header = header;
                Items = items;
            }
        }

        private readonly List<Section> sections = new();
        private int[] itemCounts = Array.Empty<int>();
        private readonly List<int> shownSections = new();
        private readonly List<int> headerIndexes = new();
        private int[] shownPositionOf = Array.Empty<int>();
        private bool skipEmptySections = true;

        /// <summary>Raised after a section is added, inserted or removed, after a clear, and when Recount finds a change.</summary>
        public event Action Changed;

        /// <summary>When true (the default), a section with no items shows nothing, not even its header.</summary>
        public bool SkipEmptySections
        {
            get => skipEmptySections;
            set
            {
                if (skipEmptySections == value)
                    return;

                skipEmptySections = value;
                RebuildAndNotify();
            }
        }

        /// <summary>The number of sections, including empty ones that are skipped.</summary>
        public int SectionCount => sections.Count;

        /// <summary>The number of entries: headers plus items of the sections shown.</summary>
        public int Count { get; private set; }

        public void AddSection(THeader header, IItemSource<TItem> items)
            => InsertSection(sections.Count, header, items);

        public void InsertSection(int sectionIndex, THeader header, IItemSource<TItem> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (sectionIndex < 0 || sectionIndex > sections.Count)
                throw new ArgumentOutOfRangeException(nameof(sectionIndex));

            sections.Insert(sectionIndex, new Section(header, items));
            RebuildAndNotify();
        }

        public void RemoveSection(int sectionIndex)
        {
            CheckSection(sectionIndex);
            sections.RemoveAt(sectionIndex);
            RebuildAndNotify();
        }

        public void ClearSections()
        {
            sections.Clear();
            RebuildAndNotify();
        }

        /// <summary>Picks up item count changes in the inner sources. Raises Changed only if a count changed.</summary>
        public void Recount()
        {
            for (int s = 0; s < sections.Count; s++)
            {
                if (sections[s].Items.ItemCount != itemCounts[s])
                {
                    RebuildAndNotify();
                    return;
                }
            }
        }

        public EntryKind KindAt(int flatIndex)
            => Locate(flatIndex).indexInSection < 0 ? EntryKind.Header : EntryKind.Item;

        /// <summary>The header of the section this entry belongs to (the entry itself when it's a header).</summary>
        public THeader HeaderAt(int flatIndex)
            => sections[Locate(flatIndex).section].Header;

        /// <summary>The item at this entry. Throws InvalidOperationException for a header.</summary>
        public TItem ItemAt(int flatIndex)
        {
            (int section, int indexInSection) = Locate(flatIndex);
            if (indexInSection < 0)
                throw new InvalidOperationException($"Entry {flatIndex} is the header of section {section}, not an item.");

            return sections[section].Items.GetItem(indexInSection);
        }

        /// <summary>The section an entry belongs to and its index in that section, or -1 when the entry is the header.</summary>
        public (int section, int indexInSection) Locate(int flatIndex)
        {
            if (flatIndex < 0 || flatIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(flatIndex));

            // The last shown section whose header is at or before the entry.
            int low = 0;
            int high = headerIndexes.Count - 1;
            while (low < high)
            {
                int middle = low + (high - low + 1) / 2;
                if (headerIndexes[middle] <= flatIndex)
                    low = middle;
                else
                    high = middle - 1;
            }

            return (shownSections[low], flatIndex - headerIndexes[low] - 1);
        }

        /// <summary>The entry of an item (or of the header, with -1), or -1 when the section is skipped.</summary>
        public int FlatIndexOf(int sectionIndex, int indexInSection)
        {
            CheckSection(sectionIndex);
            if (indexInSection < -1 || indexInSection >= itemCounts[sectionIndex])
                throw new ArgumentOutOfRangeException(nameof(indexInSection));

            int shown = shownPositionOf[sectionIndex];
            return shown < 0 ? -1 : headerIndexes[shown] + 1 + indexInSection;
        }

        private void CheckSection(int sectionIndex)
        {
            if (sectionIndex < 0 || sectionIndex >= sections.Count)
                throw new ArgumentOutOfRangeException(nameof(sectionIndex));
        }

        private void RebuildAndNotify()
        {
            Rebuild();
            Changed?.Invoke();
        }

        /// <summary>
        /// Caches each section's item count and the entry of each shown section's header (a prefix sum). The buffers are
        /// reused, so a Refresh after an item count changed allocates nothing.
        /// </summary>
        private void Rebuild()
        {
            int count = sections.Count;
            if (itemCounts.Length < count)
            {
                itemCounts = new int[Math.Max(count, itemCounts.Length * 2)];
                shownPositionOf = new int[itemCounts.Length];
            }

            shownSections.Clear();
            headerIndexes.Clear();
            int flat = 0;

            for (int s = 0; s < count; s++)
            {
                itemCounts[s] = sections[s].Items.ItemCount;
                if (skipEmptySections && itemCounts[s] == 0)
                {
                    shownPositionOf[s] = -1;
                    continue;
                }

                shownPositionOf[s] = shownSections.Count;
                shownSections.Add(s);
                headerIndexes.Add(flat);
                flat += 1 + itemCounts[s];
            }

            Count = flat;
        }
    }
}
