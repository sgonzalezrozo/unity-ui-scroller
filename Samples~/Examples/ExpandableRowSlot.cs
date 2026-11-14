using UnityEngine;
using UnityEngine.UI;

namespace SlotList.Samples
{
    /// <summary>
    /// A row with a fixed height that opens to a fixed taller size, showing its description. The whole row is a
    /// button: tapping it asks the list to expand or collapse it.
    /// </summary>
    public class ExpandableRowSlot : MonoBehaviour, IExpandableSlot<ExampleItem>
    {
        public const float HEIGHT = RowSlot.HEIGHT;
        public const float EXPANDED_HEIGHT = 240f;

        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private Button button;

        private ExampleItem data;
        private bool isExpanded;

        public float DefaultExtent => HEIGHT;
        public ISlotList Owner { get; set; }
        public int ItemIndex { get; set; }

        public ExampleItem Data
        {
            get => data;
            set
            {
                data = value;
                title.text = value.Title;
                description.text = value.Description;
            }
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                isExpanded = value;
                description.gameObject.SetActive(value);
            }
        }

        public void OnCollapsed()
            => IsExpanded = false;

        private void Awake()
            => button.onClick.AddListener(Toggle);

        private void Toggle()
        {
            if (Owner == null)
                return;

            if (IsExpanded)
                Owner.Collapse(ItemIndex);
            else
                Owner.Expand(ItemIndex, EXPANDED_HEIGHT);
        }

        public static ExpandableRowSlot CreateTemplate(Transform templateRoot)
        {
            ExpandableRowSlot slot = ExampleUI.CreateSlotTemplate<ExpandableRowSlot>(templateRoot, ExampleUI.Panel);
            slot.button = slot.gameObject.AddComponent<Button>();

            slot.title = ExampleUI.CreateText("Title", slot.transform, ExampleUI.BODY_FONT_SIZE, TextAnchor.MiddleLeft, ExampleUI.TextColor);
            ExampleUI.AnchorTop((RectTransform)slot.title.transform, 0f, HEIGHT);

            slot.description = ExampleUI.CreateText("Description", slot.transform, ExampleUI.SMALL_FONT_SIZE, TextAnchor.UpperLeft, ExampleUI.MutedText);
            ((RectTransform)slot.description.transform).offsetMax = new Vector2(-ExampleUI.TEXT_INSET, -HEIGHT);
            slot.description.gameObject.SetActive(false);
            return slot;
        }
    }
}
