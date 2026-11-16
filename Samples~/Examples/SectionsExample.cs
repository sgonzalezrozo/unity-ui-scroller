using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Samples
{
    /// <summary>
    /// A list split into sections, each with a header. An empty section is skipped, header included.
    /// Add it to an empty GameObject in an empty scene and press Play.
    /// </summary>
    public class SectionsExample : MonoBehaviour
    {
        private const float SPACING = 8f;
        private static readonly string[] SectionNames = { "Weapons", "Armour", "Empty (skipped)", "Potions", "Scrolls", "Keys" };
        private static readonly int[] SectionSizes = { 12, 8, 0, 20, 5, 3 };

        private void Start()
        {
            RectTransform body = ExampleUI.CreateScreen("Sections", out Text status);
            status.text = $"{SectionNames.Length} sections";

            Transform templates = ExampleUI.CreateTemplateRoot();
            RowSlot rowTemplate = RowSlot.CreateTemplate(templates);
            HeaderSlot headerTemplate = HeaderSlot.CreateTemplate(templates);

            SectionsExampleList list = ExampleUI.CreateList<SectionsExampleList>(body, rowTemplate.gameObject);
            list.HeaderPrefab = headerTemplate.gameObject;
            list.Settings.lineSpacing = SPACING;

            SectionedSource<ExampleItem, ExampleHeader> source = new();
            int nextId = 0;
            for (int section = 0; section < SectionNames.Length; section++)
            {
                List<ExampleItem> items = new(SectionSizes[section]);
                for (int i = 0; i < SectionSizes[section]; i++)
                    items.Add(new ExampleItem(nextId++));

                source.AddSection(new ExampleHeader(SectionNames[section]), new ListSource<ExampleItem>(items));
            }

            list.SetSource(source);
        }
    }
}
