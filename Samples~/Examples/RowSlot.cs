using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Samples
{
    /// <summary>A fixed-height row: a colour swatch and a title. Used by the Sections and Paging examples.</summary>
    public class RowSlot : MonoBehaviour, ISlot<ExampleItem>
    {
        public const float HEIGHT = 96f;
        private const float SWATCH_WIDTH = 12f;

        [SerializeField] private Text title;
        [SerializeField] private Image swatch;

        private ExampleItem data;

        public float DefaultExtent => HEIGHT;

        public ExampleItem Data
        {
            get => data;
            set
            {
                data = value;
                title.text = value.Title;
                swatch.color = value.Color;
            }
        }

        public static RowSlot CreateTemplate(Transform templateRoot)
        {
            RowSlot slot = ExampleUI.CreateSlotTemplate<RowSlot>(templateRoot, ExampleUI.Panel);
            slot.title = ExampleUI.CreateText("Title", slot.transform, ExampleUI.BODY_FONT_SIZE, TextAnchor.MiddleLeft, ExampleUI.TextColor);

            RectTransform swatchRect = ExampleUI.CreateRect("Swatch", slot.transform);
            swatchRect.anchorMin = Vector2.zero;
            swatchRect.anchorMax = new Vector2(0f, 1f);
            swatchRect.pivot = new Vector2(0f, 0.5f);
            swatchRect.sizeDelta = new Vector2(SWATCH_WIDTH, 0f);
            slot.swatch = swatchRect.gameObject.AddComponent<Image>();
            slot.swatch.raycastTarget = false;
            return slot;
        }
    }
}
