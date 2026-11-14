using System.Collections.Generic;
using UnityEngine;

namespace SlotList
{
    /// <summary>
    /// A ScrollRect that recycles slots and shows a <see cref="SectionedSource{TItem, THeader}"/>: each section is a header
    /// followed by its items. Headers use the Header Prefab (implementing ISlot&lt;THeader&gt;) and take a whole row in a
    /// grid; items use the Slot Prefab (ISlot&lt;TItem&gt; or IExpandableSlot&lt;TItem&gt;).
    /// Create a concrete subclass to add it to a GameObject. Indexes in the API (ScrollToItem, Expand...) are entry
    /// indexes: headers count. Use the source's FlatIndexOf to find an item's entry.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1)]
    public abstract class SectionedSlotList<TItem, THeader> : SlotListBase
        where TItem : class
        where THeader : class
    {
        [SerializeField] protected GameObject headerPrefab;

        private SlotListEngine<object> typedEngine;
        private SectionedSource<TItem, THeader> source;

        /// <summary>The prefab used for headers. Setting it only has an effect before the first SetSource.</summary>
        public GameObject HeaderPrefab
        {
            get => headerPrefab;
            set => headerPrefab = value;
        }

        /// <summary>The sections shown, or null before SetSource.</summary>
        public SectionedSource<TItem, THeader> Source => source;

        /// <summary>Shows these sections. Starts the list the first time; after that it replaces the sections and refreshes.</summary>
        public void SetSource(SectionedSource<TItem, THeader> newSource)
        {
            if (newSource == null)
            {
                Debug.LogError("[SlotList] Cannot set a null sectioned source");
                return;
            }

            if (headerPrefab == null)
            {
                Debug.LogError($"[SlotList] Header Prefab is not assigned to {name}. A sectioned list needs a header prefab implementing ISlot<{typeof(THeader).Name}>.");
                return;
            }

            newSource.Recount();
            source = newSource;
            SectionedEntries entries = new(newSource);

            if (typedEngine != null && typedEngine.IsInitialized)
            {
                typedEngine.ReplaceSource(entries);
                return;
            }

            if (!SourceGuard<object>.ValidateForInitialize(gameObject, entries))
                return;

            typedEngine = CreateEngine<object>(headerPrefab);
            engine = typedEngine;
            typedEngine.Initialize(entries);
        }

        /// <summary>Shows the sections the provider builds.</summary>
        public void SetSource(ISectionedSourceProvider<TItem, THeader> provider)
            => SetSource(provider?.GetSource());

        /// <summary>
        /// Scrolls a section's header to the start, at once or animated over <paramref name="duration"/> seconds.
        /// A skipped (empty) section goes to the next section shown.
        /// </summary>
        public void ScrollToSection(int sectionIndex, float duration = 0f)
        {
            if (source == null)
                return;

            for (int section = Mathf.Max(0, sectionIndex); section < source.SectionCount; section++)
            {
                int entry = source.FlatIndexOf(section, -1);
                if (entry < 0)
                    continue;

                ScrollToItem(entry, ScrollAlign.Start, duration);
                return;
            }
        }

        /// <summary>
        /// Shows an entry in a slot. The default sets a header slot's Data to the section's header, and an item slot's
        /// Data (and IsExpanded on expandable slots) to the item.
        /// </summary>
        protected virtual void BindItem(SlotHandle slot, int index)
        {
            if (slot == null || source == null)
                return;

            if (source.KindAt(index) == EntryKind.Header)
            {
                if (slot.Slot is ISlot<THeader> header)
                    header.Data = source.HeaderAt(index);
                else
                    Debug.LogError($"[SlotList] Entry {index} is a header, but the header slot does not implement ISlot<{typeof(THeader).Name}>");
                return;
            }

            TItem item = source.ItemAt(index);
            if (item == null)
            {
                slot.GameObject.SetActive(false);
                return;
            }

            if (slot.Slot is IExpandableSlot<TItem> expandable)
            {
                expandable.Data = item;
                expandable.IsExpanded = IsExpanded(index);
                return;
            }

            if (slot.Slot is ISlot<TItem> plain)
            {
                plain.Data = item;
                return;
            }

            Debug.LogError($"[SlotList] Entry {index} - the slot does not implement ISlot<{typeof(TItem).Name}> or IExpandableSlot<{typeof(TItem).Name}>");
        }

        private protected override bool IsHeaderEntry(int index)
            => source != null && index >= 0 && index < source.Count && source.KindAt(index) == EntryKind.Header;

        private protected override bool SupportsLooping() => false;

        private protected override void PrepareRefresh()
            => source?.Recount();

        private protected override void BindEntry(SlotHandle slot, int index) => BindItem(slot, index);

        /// <summary>The sections as the engine's flat list of entries: a header, then its items.</summary>
        private sealed class SectionedEntries : IItemSource<object>
        {
            private readonly SectionedSource<TItem, THeader> sections;

            public SectionedEntries(SectionedSource<TItem, THeader> sections)
            {
                this.sections = sections;
            }

            public int ItemCount => sections.Count;

            public object GetItem(int index)
                => sections.KindAt(index) == EntryKind.Header ? sections.HeaderAt(index) : sections.ItemAt(index);

            public IReadOnlyList<object> GetAllItems()
            {
                List<object> entries = new(sections.Count);
                for (int i = 0; i < sections.Count; i++)
                    entries.Add(GetItem(i));
                return entries;
            }
        }
    }
}
