namespace SlotList.Tests.PlayMode
{
    /// <summary>The header type used by the sections tests.</summary>
    public class TestHeader
    {
        public string Name;

        public TestHeader(string name)
        {
            Name = name;
        }

        public override string ToString()
            => Name;
    }
}
