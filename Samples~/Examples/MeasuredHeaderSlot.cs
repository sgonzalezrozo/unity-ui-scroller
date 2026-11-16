using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Samples
{
    /// <summary>
    /// A header measured with layout (DefaultExtent of 0 or less): a title and a wrapping description, stacked by a
    /// VerticalLayoutGroup, so each header is as tall as its own text. Used by the Bento Sections example.
    /// </summary>
    public class MeasuredHeaderSlot : MonoBehaviour, ISlot<ExampleHeader>
    {
        private const int PADDING = 16;
        private const float GAP = 6f;
        private const float MEASURED = -1f;

        [SerializeField] private Text title;
        [SerializeField] private Text description;

        private ExampleHeader data;

        public float DefaultExtent => MEASURED;

        public ExampleHeader Data
        {
            get => data;
            set
            {
                data = value;
                title.text = value.Title;
                description.text = value.Description ?? string.Empty;
                description.gameObject.SetActive(!string.IsNullOrEmpty(value.Description));
            }
        }

        public static MeasuredHeaderSlot CreateTemplate(Transform templateRoot)
        {
            MeasuredHeaderSlot slot = ExampleUI.CreateSlotTemplate<MeasuredHeaderSlot>(templateRoot, ExampleUI.Background);
            VerticalLayoutGroup group = slot.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(PADDING, PADDING, PADDING, PADDING);
            group.spacing = GAP;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            slot.title = ExampleUI.CreateText("Title", slot.transform, ExampleUI.BODY_FONT_SIZE, TextAnchor.LowerLeft, ExampleUI.Accent);
            slot.title.fontStyle = FontStyle.Bold;
            slot.description = ExampleUI.CreateText("Description", slot.transform, ExampleUI.SMALL_FONT_SIZE, TextAnchor.UpperLeft, ExampleUI.MutedText);
            slot.description.verticalOverflow = VerticalWrapMode.Overflow;
            return slot;
        }
    }
}
