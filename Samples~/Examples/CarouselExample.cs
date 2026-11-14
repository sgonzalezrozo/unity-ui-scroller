using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlotList.Samples
{
    /// <summary>
    /// A horizontal carousel of 20 tiles that loops both ways and snaps the nearest tile to the centre.
    /// Add it to an empty GameObject in an empty scene and press Play.
    /// </summary>
    public class CarouselExample : MonoBehaviour
    {
        private const int ITEM_COUNT = 20;
        private const float SPACING = 24f;
        private const float CAROUSEL_HEIGHT = 320f;

        private Text status;

        private void Start()
        {
            RectTransform body = ExampleUI.CreateScreen("Carousel", out status);
            status.text = "Drag sideways";

            RectTransform strip = ExampleUI.CreateRect("Strip", body);
            strip.anchorMin = new Vector2(0f, 0.5f);
            strip.anchorMax = new Vector2(1f, 0.5f);
            strip.sizeDelta = new Vector2(0f, CAROUSEL_HEIGHT);

            TileSlot template = TileSlot.CreateTemplate(ExampleUI.CreateTemplateRoot());
            CarouselExampleList list = ExampleUI.CreateList<CarouselExampleList>(strip, template.gameObject);
            SlotListSettings settings = list.Settings;
            settings.direction = SlotListDirection.LeftToRight;
            settings.lineSpacing = SPACING;
            settings.loop = true;
            settings.snap = SnapMode.Item;
            settings.snapAlign = SnapAlign.Center;
            list.SnappedTo += OnSnapped;

            List<ExampleItem> items = new(ITEM_COUNT);
            for (int i = 0; i < ITEM_COUNT; i++)
                items.Add(new ExampleItem(i));

            list.Initialize(new ListSource<ExampleItem>(items));
        }

        private void OnSnapped(int index)
            => status.text = $"Showing {index}";
    }
}
