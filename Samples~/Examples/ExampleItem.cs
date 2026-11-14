using UnityEngine;

namespace SlotList.Samples
{
    /// <summary>The item every example shows: a number, a title, a description and a colour.</summary>
    public class ExampleItem
    {
        public readonly int Id;
        public readonly string Title;
        public readonly string Description;
        public readonly Color Color;

        public ExampleItem(int id)
        {
            Id = id;
            Title = $"Item {id}";
            Description = $"Details for item {id}. Tap again to close.";
            Color = ExampleUI.ColorFor(id);
        }
    }
}
