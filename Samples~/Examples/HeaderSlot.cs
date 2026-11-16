using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Samples
{
    /// <summary>A section header in the Sections example.</summary>
    public class HeaderSlot : MonoBehaviour, ISlot<ExampleHeader>
    {
        public const float HEIGHT = 80f;

        [SerializeField] private Text title;

        private ExampleHeader data;

        public float DefaultExtent => HEIGHT;

        public ExampleHeader Data
        {
            get => data;
            set
            {
                data = value;
                title.text = value.Title;
            }
        }

        public static HeaderSlot CreateTemplate(Transform templateRoot)
        {
            HeaderSlot slot = ExampleUI.CreateSlotTemplate<HeaderSlot>(templateRoot, ExampleUI.Background);
            slot.title = ExampleUI.CreateText("Title", slot.transform, ExampleUI.BODY_FONT_SIZE, TextAnchor.LowerLeft, ExampleUI.Accent);
            slot.title.fontStyle = FontStyle.Bold;
            return slot;
        }
    }
}
