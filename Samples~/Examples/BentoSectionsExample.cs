using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlotList.Samples
{
    /// <summary>
    /// Bento with sections: each section's header takes a whole row, as tall as its own text, and the bento layout
    /// starts again below it, so every section opens with its big tile. The layout is written in JSON.
    /// Add it to an empty GameObject in an empty scene and press Play.
    /// </summary>
    public class BentoSectionsExample : MonoBehaviour
    {
        private const float SPACING = 12f;

        // A big tile, then a strip of three, then a tall tile with two stacked beside it; then the strip and the tall block again.
        private const string LAYOUT_JSON = @"{
            ""columns"": 3,
            ""repeatFrom"": 1,
            ""patterns"": [
                { ""rows"": 2, ""tiles"": [
                    { ""c"": 0, ""r"": 0, ""w"": 2, ""h"": 2 },
                    { ""c"": 2, ""r"": 0, ""w"": 1, ""h"": 1 },
                    { ""c"": 2, ""r"": 1, ""w"": 1, ""h"": 1 } ] },
                { ""rows"": 1, ""tiles"": [
                    { ""c"": 0, ""r"": 0, ""w"": 1, ""h"": 1 },
                    { ""c"": 1, ""r"": 0, ""w"": 1, ""h"": 1 },
                    { ""c"": 2, ""r"": 0, ""w"": 1, ""h"": 1 } ] },
                { ""rows"": 2, ""tiles"": [
                    { ""c"": 0, ""r"": 0, ""w"": 1, ""h"": 2 },
                    { ""c"": 1, ""r"": 0, ""w"": 2, ""h"": 1 },
                    { ""c"": 1, ""r"": 1, ""w"": 2, ""h"": 1 } ] }
            ]
        }";

        private static readonly string[] Titles = { "Featured", "Weapons", "Armour", "Potions", "Pets" };

        private static readonly string[] Descriptions =
        {
            null,
            "Blades, bows and staffs.",
            "Heavy plates, light leathers, and everything in between. Better armour slows you down a little.",
            "One sip each.",
            "Companions that follow you around. Each one has its own trick, and some of them find items on their own while you explore."
        };

        private static readonly int[] Counts = { 3, 14, 9, 6, 11 };

        private void Start()
        {
            RectTransform body = ExampleUI.CreateScreen("Bento Sections", out Text status);
            status.text = $"{Titles.Length} sections";

            Transform templates = ExampleUI.CreateTemplateRoot();
            TileSlot tile = TileSlot.CreateTemplate(templates);
            MeasuredHeaderSlot header = MeasuredHeaderSlot.CreateTemplate(templates);

            BentoSectionsExampleList list = ExampleUI.CreateList<BentoSectionsExampleList>(body, tile.gameObject);
            list.HeaderPrefab = header.gameObject;
            SlotListSettings settings = list.Settings;
            settings.bento = true;
            settings.lineSpacing = SPACING;
            settings.slotSpacing = SPACING;

            BentoLayout layout = BentoLayout.FromJson(LAYOUT_JSON);
            if (!layout.IsValid)
                Debug.LogError(string.Join("\n", layout.Errors));
            list.SetBentoLayout(layout);

            SectionedSource<ExampleItem, ExampleHeader> source = new();
            int nextId = 0;
            for (int section = 0; section < Titles.Length; section++)
            {
                List<ExampleItem> items = new(Counts[section]);
                for (int i = 0; i < Counts[section]; i++)
                    items.Add(new ExampleItem(nextId++));

                source.AddSection(new ExampleHeader(Titles[section], Descriptions[section]), new ListSource<ExampleItem>(items));
            }

            list.SetSource(source);
        }
    }
}
