namespace UIScroller.Samples
{
    /// <summary>A section header in the Sections examples: a title and an optional description.</summary>
    public class ExampleHeader
    {
        public readonly string Title;
        public readonly string Description;

        public ExampleHeader(string title, string description = null)
        {
            Title = title;
            Description = description;
        }
    }
}
