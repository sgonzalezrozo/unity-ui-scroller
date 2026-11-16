using UnityEngine;

namespace UIScroller
{
    /// <summary>
    /// A ScrollRect that recycles slots, showing one item type. Create a concrete subclass to add it to a GameObject.
    /// For sections with headers, use <see cref="SectionedScroller{TItem, THeader}"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1)]
    public abstract class Scroller<T> : ScrollerBase
        where T : class
    {
        private ScrollerEngine<T> typedEngine;

        /// <summary>The current data source, or null before Initialize.</summary>
        protected IItemSource<T> DataSource => typedEngine?.Source;

        /// <summary>Starts the scroller with a data source.</summary>
        public void Initialize(IItemSource<T> source)
        {
            if (!SourceGuard<T>.ValidateForInitialize(gameObject, source))
                return;

            if (typedEngine == null || !typedEngine.IsInitialized)
            {
                typedEngine = CreateEngine<T>(null);
                engine = typedEngine;
            }

            typedEngine.Initialize(source);
        }

        /// <summary>Shows a data source. Before initialization it calls Initialize; after, it replaces the source and refreshes.</summary>
        public void SetSource(IItemSource<T> newSource)
        {
            if (!IsInitialized)
            {
                Initialize(newSource);
                return;
            }

            if (SourceGuard<T>.ValidateForSetDataSource(gameObject, newSource, true))
                typedEngine.ReplaceSource(newSource);
        }

        protected virtual bool SupportsPagination()
            => DataSource is IPagedSource<T>;

        /// <summary>Shows an item in a slot. The default sets Data (and IsExpanded on expandable slots).</summary>
        protected virtual void BindItem(SlotHandle slot, int index)
            => typedEngine?.BindDefault(slot, index);

        private protected override bool SupportsPagingEntries() => SupportsPagination();

        private protected override void BindEntry(SlotHandle slot, int index) => BindItem(slot, index);
    }
}
