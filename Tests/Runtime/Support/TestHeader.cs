namespace SlotList.Tests.PlayMode
{
    /// <summary>The header type used by the sections tests.</summary>
    public class TestHeader
    {
        public string Name;

        /// <summary>The height a measured header slot gives its body (RigOptions.HeaderSize of 0 or less).</summary>
        public float Height = SlotListRig.HEADER_BODY_HEIGHT;

        public TestHeader(string name, float height = SlotListRig.HEADER_BODY_HEIGHT)
        {
            Name = name;
            Height = height;
        }

        public override string ToString()
            => Name;
    }
}
