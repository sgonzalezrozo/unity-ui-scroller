using System.Collections.Generic;
using UnityEngine;

namespace SlotList.Samples
{
    /// <summary>
    /// A grid of 1,000 tiles. Auto Grid fits as many columns as the width allows from a target tile size.
    /// Add it to an empty GameObject in an empty scene and press Play.
    /// </summary>
    public class GridExample : MonoBehaviour
    {
        private const int ITEM_COUNT = 1000;
        private const float SPACING = 16f;

        private void Start()
        {
            RectTransform body = ExampleUI.CreateScreen("Grid", out UnityEngine.UI.Text status);
            status.text = $"{ITEM_COUNT} items";

            TileSlot template = TileSlot.CreateTemplate(ExampleUI.CreateTemplateRoot());
            GridExampleList list = ExampleUI.CreateList<GridExampleList>(body, template.gameObject);
            SlotListSettings settings = list.Settings;
            settings.autoSlotsPerLine = true;
            settings.targetSlotSize = TileSlot.SIZE;
            settings.minSlotsPerLine = 2;
            settings.lineSpacing = SPACING;
            settings.slotSpacing = SPACING;

            List<ExampleItem> items = new(ITEM_COUNT);
            for (int i = 0; i < ITEM_COUNT; i++)
                items.Add(new ExampleItem(i));

            list.Initialize(new ListSource<ExampleItem>(items));
        }
    }
}
