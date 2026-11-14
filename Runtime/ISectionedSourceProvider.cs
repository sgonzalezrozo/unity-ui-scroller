namespace SlotList
{
    /// <summary>Something that builds the sectioned source a SectionedSlotList shows (for example a screen's Model).</summary>
    public interface ISectionedSourceProvider<TItem, THeader>
        where TItem : class
        where THeader : class
    {
        SectionedSource<TItem, THeader> GetSource();
    }
}
