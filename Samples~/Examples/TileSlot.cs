using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Samples
{
    /// <summary>A coloured square with the item's number. Used by the Grid and Carousel examples.</summary>
    public class TileSlot : MonoBehaviour, ISlot<ExampleItem>
    {
        public const float SIZE = 240f;
        private const int NUMBER_FONT_SIZE = 56;

        [SerializeField] private Text number;
        [SerializeField] private Image background;

        private ExampleItem data;

        public float DefaultExtent => SIZE;

        public ExampleItem Data
        {
            get => data;
            set
            {
                data = value;
                number.text = value.Id.ToString();
                background.color = value.Color;
            }
        }

        public static TileSlot CreateTemplate(Transform templateRoot)
        {
            TileSlot slot = ExampleUI.CreateSlotTemplate<TileSlot>(templateRoot, ExampleUI.Panel);
            slot.background = slot.GetComponent<Image>();
            slot.number = ExampleUI.CreateText("Number", slot.transform, NUMBER_FONT_SIZE, TextAnchor.MiddleCenter, ExampleUI.TextColor);
            return slot;
        }
    }
}
