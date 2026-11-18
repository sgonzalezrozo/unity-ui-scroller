using System.Collections.Generic;
using UnityEngine;

namespace UIScroller.Samples
{
    /// <summary>
    /// A vertical list of shelves, each a horizontal carousel that snaps to its cards. Drag sideways on a shelf to move
    /// it, up or down anywhere to move the page: a drag across a carousel's axis goes to the list around it. Cards shrink
    /// as they move away from the shelf's centre (IViewportPositionListener). Add it to an empty GameObject and press Play.
    /// </summary>
    public class NestedExample : MonoBehaviour
    {
        private const int SHELF_COUNT = 40;
        private const int CARDS_PER_SHELF = 15;
        private const float SHELF_SPACING = 16f;

        private void Start()
        {
            RectTransform body = ExampleUI.CreateScreen("Nested", out UnityEngine.UI.Text status);
            status.text = "Drag sideways on a shelf, or up and down";

            Transform templates = ExampleUI.CreateTemplateRoot();
            CardSlot card = CardSlot.CreateTemplate(templates);
            ShelfSlot shelf = ShelfSlot.CreateTemplate(templates, card.gameObject);

            NestedExampleList list = ExampleUI.CreateList<NestedExampleList>(body, shelf.gameObject);
            list.Settings.lineSpacing = SHELF_SPACING;

            List<Shelf> shelves = new(SHELF_COUNT);
            for (int s = 0; s < SHELF_COUNT; s++)
            {
                List<ExampleItem> cards = new(CARDS_PER_SHELF);
                for (int c = 0; c < CARDS_PER_SHELF; c++)
                    cards.Add(new ExampleItem(s * CARDS_PER_SHELF + c));
                shelves.Add(new Shelf($"Shelf {s}", new ListSource<ExampleItem>(cards)));
            }

            list.Initialize(new ListSource<Shelf>(shelves));
        }
    }

    /// <summary>A shelf: a title and its own source of cards.</summary>
    public class Shelf
    {
        public readonly string Title;
        public readonly ListSource<ExampleItem> Cards;

        public Shelf(string title, ListSource<ExampleItem> cards)
        {
            Title = title;
            Cards = cards;
        }
    }
}
