using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Samples
{
    /// <summary>
    /// A shelf row with a title and a horizontal carousel. Shelves are pooled like any slot: a reused shelf gives its
    /// carousel the new shelf's cards and goes back to the first card.
    /// </summary>
    public class ShelfSlot : MonoBehaviour, ISlot<Shelf>
    {
        public const float HEIGHT = 300f;
        private const float TITLE_HEIGHT = 48f;
        private const float CARD_SPACING = 16f;
        private const float SIDE_PADDING = 24f;
        private const int TITLE_FONT_SIZE = 28;

        [SerializeField] private Text title;
        [SerializeField] private ShelfCarouselList carousel;

        private Shelf data;

        public float DefaultExtent => HEIGHT;

        public Shelf Data
        {
            get => data;
            set
            {
                data = value;
                title.text = value.Title;
                if (!carousel.IsInitialized)
                {
                    carousel.Initialize(value.Cards);
                    return;
                }

                carousel.SetSource(value.Cards);
                carousel.ScrollToStart();
            }
        }

        public static ShelfSlot CreateTemplate(Transform templateRoot, GameObject cardTemplate)
        {
            ShelfSlot slot = ExampleUI.CreateSlotTemplate<ShelfSlot>(templateRoot, ExampleUI.Background);
            slot.title = ExampleUI.CreateText("Title", slot.transform, TITLE_FONT_SIZE, TextAnchor.MiddleLeft, ExampleUI.TextColor);
            ExampleUI.AnchorTop(slot.title.rectTransform, 0f, TITLE_HEIGHT);
            slot.title.rectTransform.offsetMin = new Vector2(SIDE_PADDING, slot.title.rectTransform.offsetMin.y);

            RectTransform strip = ExampleUI.CreateRect("Strip", slot.transform);
            ExampleUI.Stretch(strip);
            strip.offsetMax = new Vector2(0f, -TITLE_HEIGHT);

            slot.carousel = ExampleUI.CreateList<ShelfCarouselList>(strip, cardTemplate);
            ScrollerSettings settings = slot.carousel.Settings;
            settings.direction = ScrollerDirection.LeftToRight;
            settings.lineSpacing = CARD_SPACING;
            settings.paddingStart = SIDE_PADDING;
            settings.paddingEnd = SIDE_PADDING;
            settings.snap = SnapMode.Item;
            settings.snapAlign = SnapAlign.Center;
            return slot;
        }
    }
}
