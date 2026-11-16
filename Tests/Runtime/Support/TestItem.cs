namespace UIScroller.Tests.PlayMode
{
    /// <summary>The data item used by the PlayMode tests. Id equals the item's index unless a test says otherwise.</summary>
    public class TestItem
    {
        public int Id;
        public string Text;

        /// <summary>The height a layout-sized test slot gives its content (RigOptions.LayoutContent).</summary>
        public float Height = 100f;

        public TestItem(int id)
        {
            Id = id;
            Text = $"Item {id}";
        }

        public override string ToString()
            => Text;
    }
}
