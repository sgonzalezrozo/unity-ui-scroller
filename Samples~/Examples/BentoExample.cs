using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlotList.Samples
{
    /// <summary>
    /// A bento list: tiles of different sizes on a 4-column grid, placed by a layout written in JSON. The button
    /// switches between a pattern layout and a span layout at runtime, keeping the same items.
    /// Add it to an empty GameObject in an empty scene and press Play.
    /// </summary>
    public class BentoExample : MonoBehaviour
    {
        private const int ITEM_COUNT = 300;
        private const float SPACING = 12f;

        // Patterns: a hero block (one 2x2 tile, one 2x1 and two 1x1), then a strip of four, then a mirrored block.
        // After the first round, the sequence repeats from the strip.
        private const string PATTERNS_JSON = @"{
            ""columns"": 4,
            ""repeatFrom"": 1,
            ""patterns"": [
                { ""rows"": 2, ""tiles"": [
                    { ""c"": 0, ""r"": 0, ""w"": 2, ""h"": 2 },
                    { ""c"": 2, ""r"": 0, ""w"": 2, ""h"": 1 },
                    { ""c"": 2, ""r"": 1, ""w"": 1, ""h"": 1 },
                    { ""c"": 3, ""r"": 1, ""w"": 1, ""h"": 1 } ] },
                { ""rows"": 1, ""tiles"": [
                    { ""c"": 0, ""r"": 0, ""w"": 1, ""h"": 1 },
                    { ""c"": 1, ""r"": 0, ""w"": 1, ""h"": 1 },
                    { ""c"": 2, ""r"": 0, ""w"": 1, ""h"": 1 },
                    { ""c"": 3, ""r"": 0, ""w"": 1, ""h"": 1 } ] },
                { ""rows"": 2, ""tiles"": [
                    { ""c"": 0, ""r"": 0, ""w"": 1, ""h"": 2 },
                    { ""c"": 1, ""r"": 0, ""w"": 1, ""h"": 1 },
                    { ""c"": 1, ""r"": 1, ""w"": 1, ""h"": 1 },
                    { ""c"": 2, ""r"": 0, ""w"": 2, ""h"": 2 } ] }
            ]
        }";

        // Spans: a sequence of tile sizes, packed in order. Each tile goes in the first gap where it fits.
        private const string SPANS_JSON = @"{
            ""columns"": 4,
            ""spans"": [
                { ""w"": 2, ""h"": 2 }, { ""w"": 1, ""h"": 1 }, { ""w"": 1, ""h"": 1 }, { ""w"": 1, ""h"": 1 },
                { ""w"": 1, ""h"": 1 }, { ""w"": 1, ""h"": 2 }, { ""w"": 3, ""h"": 1 }, { ""w"": 1, ""h"": 1 },
                { ""w"": 2, ""h"": 1 }, { ""w"": 2, ""h"": 1 }
            ]
        }";

        private const string PATTERNS_LABEL = "Showing patterns";
        private const string SPANS_LABEL = "Showing spans";

        private BentoExampleList list;
        private Text buttonLabel;
        private bool showingPatterns = true;

        private void Start()
        {
            RectTransform body = ExampleUI.CreateScreen("Bento", out Text status);
            status.text = string.Empty;

            Button button = ExampleUI.CreateTitleButton(body, PATTERNS_LABEL);
            buttonLabel = button.GetComponentInChildren<Text>();
            button.onClick.AddListener(SwitchLayout);

            TileSlot template = TileSlot.CreateTemplate(ExampleUI.CreateTemplateRoot());
            list = ExampleUI.CreateList<BentoExampleList>(body, template.gameObject);
            SlotListSettings settings = list.Settings;
            settings.bento = true;
            settings.cellAspect = 1f;
            settings.lineSpacing = SPACING;
            settings.slotSpacing = SPACING;
            list.SetBentoLayout(Parse(PATTERNS_JSON));

            List<ExampleItem> items = new(ITEM_COUNT);
            for (int i = 0; i < ITEM_COUNT; i++)
                items.Add(new ExampleItem(i));

            list.Initialize(new ListSource<ExampleItem>(items));
        }

        private void SwitchLayout()
        {
            showingPatterns = !showingPatterns;
            list.SetBentoLayout(Parse(showingPatterns ? PATTERNS_JSON : SPANS_JSON));
            buttonLabel.text = showingPatterns ? PATTERNS_LABEL : SPANS_LABEL;
        }

        private static BentoLayout Parse(string json)
        {
            BentoLayout layout = BentoLayout.FromJson(json);
            if (!layout.IsValid)
                Debug.LogError(string.Join("\n", layout.Errors));
            return layout;
        }
    }
}
