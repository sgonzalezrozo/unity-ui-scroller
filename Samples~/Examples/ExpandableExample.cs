using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlotList.Samples
{
    /// <summary>
    /// Rows that open to show details when tapped. Opening one closes the others, and nothing above it moves.
    /// Add it to an empty GameObject in an empty scene and press Play.
    /// </summary>
    public class ExpandableExample : MonoBehaviour
    {
        private const int ITEM_COUNT = 200;
        private const float SPACING = 8f;

        private Text status;

        private void Start()
        {
            RectTransform body = ExampleUI.CreateScreen("Expandable", out status);
            status.text = "Tap a row";

            ExpandableRowSlot template = ExpandableRowSlot.CreateTemplate(ExampleUI.CreateTemplateRoot());
            ExpandableExampleList list = ExampleUI.CreateList<ExpandableExampleList>(body, template.gameObject);
            list.Settings.lineSpacing = SPACING;
            list.ItemExpanded += OnItemExpanded;
            list.ItemCollapsed += OnItemCollapsed;

            List<ExampleItem> items = new(ITEM_COUNT);
            for (int i = 0; i < ITEM_COUNT; i++)
                items.Add(new ExampleItem(i));

            list.Initialize(new ListSource<ExampleItem>(items));
        }

        private void OnItemExpanded(int index)
            => status.text = $"Opened {index}";

        private void OnItemCollapsed(int index)
            => status.text = $"Closed {index}";
    }
}
