using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UIScroller.Editor.ExampleUIKit;
using Object = UnityEngine.Object;

namespace UIScroller.Editor
{
    /// <summary>One example the Assets > UI Scroller menu creates: its scripts (a folder in Templates~) and how its prefabs are built.</summary>
    internal sealed class ExampleRecipe
    {
        public readonly string Id;
        public readonly string Label;
        public readonly string DefaultName;
        public readonly string Summary;
        public readonly Action<ExampleBuild> Build;

        public ExampleRecipe(string id, string label, string defaultName, string summary, Action<ExampleBuild> build)
        {
            Id = id;
            Label = label;
            DefaultName = defaultName;
            Summary = summary;
            Build = build;
        }
    }

    /// <summary>What a recipe builds with: the example's name and folder, its compiled scripts and prefab saving.</summary>
    internal sealed class ExampleBuild
    {
        public readonly string Name;
        public readonly string Folder;

        public ExampleBuild(string name, string folder)
        {
            Name = name;
            Folder = folder;
        }

        public string Title => ObjectNames.NicifyVariableName(Name);

        /// <summary>The generated MonoBehaviour <c>Name + suffix</c>, e.g. "InventorySlot".</summary>
        public Type Script(string suffix)
            => FindScript(Name + suffix) ?? throw new InvalidOperationException($"The script {Name}{suffix} isn't compiled yet.");

        public static Type FindScript(string className)
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (type.Name == className && string.IsNullOrEmpty(type.Namespace))
                    return type;
            }

            return null;
        }

        public TextAsset LayoutJson => AssetDatabase.LoadAssetAtPath<TextAsset>($"{Folder}/{Name}Layout.json");

        /// <summary>Saves <paramref name="root"/> as <c>Folder/fileName.prefab</c>, destroys the scene copy and returns the asset.</summary>
        public GameObject Save(GameObject root, string fileName)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{fileName}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }
    }

    /// <summary>The examples, in menu order, and how each one's slot and screen prefabs are put together.</summary>
    internal static class ScrollerExampleRecipes
    {
        public static readonly ExampleRecipe[] All =
        {
            new("List", "Basic List", "ContactList", "A list of rows with an avatar, a title, a subtitle and a badge.", BuildList),
            new("Grid", "Grid", "ShopGrid", "A shop grid that fits as many columns as the width allows.", BuildGrid),
            new("DynamicSize", "Dynamic Size", "PostFeed", "A feed where each post is as tall as its text (measured with layout).", BuildDynamicSize),
            new("ExpandOnClick", "Dynamic Size on Click", "FaqList", "Rows measured with layout that grow to show their answer when tapped.", BuildExpandOnClick),
            new("Expandable", "Expandable Row", "HeroList", "Fixed-height rows with a hitbox that asks the list to open them to a taller size.", BuildExpandable),
            new("Bento", "Bento", "StoreBento", "Tiles of different sizes placed by a bento layout JSON.", BuildBento),
            new("BentoPaging", "Bento with Paging", "PagedBento", "A bento list fed page by page by a page loader.", BuildBentoPaging),
            new("GridCarousel", "Grid Carousel", "ItemPager", "A two-row grid that scrolls sideways a page at a time, with page buttons.", BuildGridCarousel),
            new("LoopCarousel", "Looping Carousel", "WorldCarousel", "A carousel that loops both ways and centres a card that scales with its position.", BuildLoopCarousel),
            new("Sections", "Sections", "Contacts", "Contacts under letter headers, with a SectionedScroller.", BuildSections),
            new("Nested", "Nested Shelves", "StoreShelves", "A vertical list of shelves, each a horizontal carousel of cards.", BuildNested)
        };

        // ---------- Basic List ----------

        private static void BuildList(ExampleBuild b)
        {
            GameObject slot = b.Save(AvatarRow(b, "Slot"), $"{b.Name}Slot");
            ScrollerBase list = Screen(b, "1,000 rows", slot, out Component screen, out _);
            list.Settings.lineSpacing = 12f;
            Finish(b, screen, list);
        }

        private static GameObject AvatarRow(ExampleBuild b, string suffix)
        {
            GameObject root = SlotRoot($"{b.Name}{suffix}", b.Script(suffix), Panel, out Component slot, true);
            Image avatar = NewImage("Avatar", root.transform, Accent);
            Pin(avatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(96f, 96f));
            Text initial = NewText("Initial", avatar.transform, "A", 44, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);

            Text title = NewText("Title", root.transform, "Title", 36, TextAnchor.LowerLeft, TextColor, FontStyle.Bold);
            Place(title.rectTransform, new Vector2(0f, 0.5f), Vector2.one, new Vector2(152f, 4f), new Vector2(-140f, -16f));
            Text subtitle = NewText("Subtitle", root.transform, "Subtitle", 28, TextAnchor.UpperLeft, Muted);
            Place(subtitle.rectTransform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(152f, 16f), new Vector2(-140f, -4f));

            Image badge = NewImage("Badge", root.transform, Accent);
            Pin(badge.rectTransform, new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Vector2(80f, 56f));
            Text badgeLabel = NewText("Label", badge.transform, "3", 28, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);

            Set(slot, "avatar", avatar);
            Set(slot, "initial", initial);
            Set(slot, "title", title);
            Set(slot, "subtitle", subtitle);
            Set(slot, "badge", badge.gameObject);
            Set(slot, "badgeLabel", badgeLabel);
            Set(slot, "button", root.GetComponent<Button>());
            return root;
        }

        // ---------- Grid ----------

        private static void BuildGrid(ExampleBuild b)
        {
            GameObject slot = b.Save(ShopTile(b), $"{b.Name}Slot");
            ScrollerBase list = Screen(b, "1,000 items", slot, out Component screen, out _);
            ScrollerSettings settings = list.Settings;
            settings.autoSlotsPerLine = true;
            settings.targetSlotSize = 300f;
            settings.minSlotsPerLine = 2;
            settings.lineSpacing = 20f;
            settings.slotSpacing = 20f;
            Finish(b, screen, list);
        }

        private static GameObject ShopTile(ExampleBuild b)
        {
            GameObject root = SlotRoot($"{b.Name}Slot", b.Script("Slot"), Panel, out Component slot, true);
            Image art = NewImage("Art", root.transform, Accent);
            Place(art.rectTransform, new Vector2(0f, 0.3f), Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, -12f));
            Text icon = NewText("Icon", art.transform, "S", 96, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f), FontStyle.Bold);
            Image rarity = NewImage("Rarity", root.transform, Gold);
            Place(rarity.rectTransform, new Vector2(0f, 0.3f), new Vector2(1f, 0.3f), new Vector2(12f, -4f), new Vector2(-12f, 4f));

            Text itemName = NewText("Name", root.transform, "Item", 30, TextAnchor.MiddleLeft, TextColor, FontStyle.Bold);
            Place(itemName.rectTransform, new Vector2(0f, 0.14f), new Vector2(1f, 0.3f), new Vector2(20f, 0f), new Vector2(-20f, -6f));
            Text price = NewText("Price", root.transform, "100 G", 26, TextAnchor.MiddleLeft, Gold);
            Place(price.rectTransform, Vector2.zero, new Vector2(1f, 0.14f), new Vector2(20f, 8f), new Vector2(-20f, 0f));

            Set(slot, "art", art);
            Set(slot, "icon", icon);
            Set(slot, "rarityStrip", rarity);
            Set(slot, "itemName", itemName);
            Set(slot, "price", price);
            Set(slot, "button", root.GetComponent<Button>());
            return root;
        }

        // ---------- Dynamic Size ----------

        private static void BuildDynamicSize(ExampleBuild b)
        {
            GameObject root = SlotRoot($"{b.Name}Slot", b.Script("Slot"), Panel, out Component slot);
            Vertical(root, 28, 18f);

            HorizontalLayoutGroup header = Horizontal(NewRect("Header", root.transform).gameObject, 20f);
            Image avatar = NewImage("Avatar", header.transform, Accent);
            Layout(avatar.gameObject, 80f, 80f, 0f);
            Text initial = NewText("Initial", avatar.transform, "A", 36, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);

            VerticalLayoutGroup names = Vertical(NewRect("Names", header.transform).gameObject, 0, 4f);
            Layout(names.gameObject, flexibleWidth: 1f);
            Text author = FlowText("Author", names.transform, "Author", 32, TextColor, FontStyle.Bold);
            Text time = FlowText("Time", names.transform, "1 min ago", 24, Muted);

            Text body = FlowText("Body", root.transform, "Body", 30, TextColor);
            Image picture = NewImage("Picture", root.transform, PanelLight);
            Layout(picture.gameObject, height: 360f);
            Text likes = FlowText("Likes", root.transform, "♥ 0", 26, Muted);

            Set(slot, "avatar", avatar);
            Set(slot, "initial", initial);
            Set(slot, "author", author);
            Set(slot, "time", time);
            Set(slot, "body", body);
            Set(slot, "picture", picture);
            Set(slot, "likes", likes);

            GameObject slotPrefab = b.Save(root, $"{b.Name}Slot");
            ScrollerBase list = Screen(b, "Each post is as tall as its text", slotPrefab, out Component screen, out _);
            list.Settings.lineSpacing = 16f;
            Finish(b, screen, list);
        }

        // ---------- Dynamic Size on Click ----------

        private static void BuildExpandOnClick(ExampleBuild b)
        {
            GameObject root = SlotRoot($"{b.Name}Slot", b.Script("Slot"), Panel, out Component slot, true);
            Vertical(root, 28, 16f);

            HorizontalLayoutGroup header = Horizontal(NewRect("Header", root.transform).gameObject, 16f);
            Text question = FlowText("Question", header.transform, "Question", 32, TextColor, FontStyle.Bold);
            Layout(question.gameObject, flexibleWidth: 1f);
            Text chevron = NewText("Chevron", header.transform, "+", 44, TextAnchor.MiddleCenter, Accent, FontStyle.Bold);
            Layout(chevron.gameObject, 48f, 48f, 0f);

            Text answer = FlowText("Answer", root.transform, "Answer", 28, Muted);
            answer.gameObject.SetActive(false);

            Set(slot, "question", question);
            Set(slot, "chevron", chevron);
            Set(slot, "answer", answer);
            Set(slot, "hitbox", root.GetComponent<Button>());

            GameObject slotPrefab = b.Save(root, $"{b.Name}Slot");
            ScrollerBase list = Screen(b, "Tap a question", slotPrefab, out Component screen, out _);
            list.Settings.lineSpacing = 12f;
            Finish(b, screen, list);
        }

        // ---------- Expandable Row ----------

        private const float EXPANDABLE_HEIGHT = 170f;

        private static void BuildExpandable(ExampleBuild b)
        {
            GameObject root = SlotRoot($"{b.Name}Slot", b.Script("Slot"), Panel, out Component slot);
            RectTransform summary = Place(NewRect("Summary", root.transform), new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -EXPANDABLE_HEIGHT), Vector2.zero);

            Image portrait = NewImage("Portrait", summary, Accent);
            Pin(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(110f, 110f));
            Text initial = NewText("Initial", portrait.transform, "A", 48, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            Text heroName = NewText("Name", summary, "Hero", 36, TextAnchor.LowerLeft, TextColor, FontStyle.Bold);
            Place(heroName.rectTransform, new Vector2(0f, 0.5f), Vector2.one, new Vector2(166f, 4f), new Vector2(-220f, -24f));
            Text role = NewText("Role", summary, "Role", 28, TextAnchor.UpperLeft, Muted);
            Place(role.rectTransform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(166f, 24f), new Vector2(-220f, -4f));
            Text level = NewText("Level", summary, "Lv 1", 30, TextAnchor.MiddleRight, Gold, FontStyle.Bold);
            Pin(level.rectTransform, new Vector2(1f, 0.5f), new Vector2(-90f, 0f), new Vector2(130f, 60f));
            Text chevron = NewText("Chevron", summary, "▼", 28, TextAnchor.MiddleCenter, Muted);
            Pin(chevron.rectTransform, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(50f, 60f));

            // Over the summary only: taps on the details reach their own buttons.
            Image hitbox = NewImage("Hitbox", summary, Color.clear, true);
            Stretch(hitbox.rectTransform);
            Button hitboxButton = AddButton(hitbox.gameObject);

            RectTransform details = Place(NewRect("Details", root.transform), Vector2.zero, Vector2.one, new Vector2(28f, 28f), new Vector2(-28f, -EXPANDABLE_HEIGHT));
            RectTransform attack = StatBar(details, "Attack", 0, new Color(0.95f, 0.4f, 0.35f));
            RectTransform defense = StatBar(details, "Defense", 1, Accent);
            RectTransform speed = StatBar(details, "Speed", 2, Online);
            Button select = LabelButton("Select", details, "Select", Accent, 32);
            Place((RectTransform)select.transform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 90f));
            details.gameObject.SetActive(false);

            Set(slot, "portrait", portrait);
            Set(slot, "initial", initial);
            Set(slot, "heroName", heroName);
            Set(slot, "role", role);
            Set(slot, "level", level);
            Set(slot, "chevron", chevron);
            Set(slot, "hitbox", hitboxButton);
            Set(slot, "details", details.gameObject);
            Set(slot, "attackFill", attack);
            Set(slot, "defenseFill", defense);
            Set(slot, "speedFill", speed);
            Set(slot, "selectButton", select);

            GameObject slotPrefab = b.Save(root, $"{b.Name}Slot");
            ScrollerBase list = Screen(b, "Tap a row's summary", slotPrefab, out Component screen, out _);
            list.Settings.lineSpacing = 12f;
            Finish(b, screen, list);
        }

        /// <summary>A labelled bar in row <paramref name="row"/> of the details. Returns the fill, sized by its right anchor.</summary>
        private static RectTransform StatBar(RectTransform parent, string label, int row, Color color)
        {
            const float ROW = 80f;
            const float HEIGHT = 60f;
            RectTransform line = Place(NewRect(label, parent), new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -(row * ROW + HEIGHT)), new Vector2(0f, -row * ROW));
            Text text = NewText("Label", line, label, 28, TextAnchor.MiddleLeft, Muted);
            Place(text.rectTransform, Vector2.zero, new Vector2(0.3f, 1f), Vector2.zero, Vector2.zero);
            Image bar = NewImage("Bar", line, PanelLight);
            Place(bar.rectTransform, new Vector2(0.3f, 0.3f), new Vector2(1f, 0.7f), Vector2.zero, Vector2.zero);
            Image fill = NewImage("Fill", bar.transform, color);
            return Place(fill.rectTransform, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        }

        // ---------- Bento ----------

        private static void BuildBento(ExampleBuild b)
        {
            GameObject slot = b.Save(BentoTile(b), $"{b.Name}Slot");
            ScrollerBase list = Screen(b, "Layout: " + b.Name + "Layout.json", slot, out Component screen, out _);
            SetUpBento(b, list);
            Finish(b, screen, list);
        }

        private static void BuildBentoPaging(ExampleBuild b)
        {
            GameObject slot = b.Save(BentoTile(b), $"{b.Name}Slot");
            ScrollerBase list = Screen(b, "Loading…", slot, out Component screen, out Text status);
            SetUpBento(b, list);
            Set(screen, "status", status);
            Finish(b, screen, list);
        }

        private static void SetUpBento(ExampleBuild b, ScrollerBase list)
        {
            ScrollerSettings settings = list.Settings;
            settings.bento = true;
            settings.cellAspect = 1f;
            settings.lineSpacing = 14f;
            settings.slotSpacing = 14f;
            Set(list, "bentoLayoutJson", b.LayoutJson);
        }

        private static GameObject BentoTile(ExampleBuild b)
        {
            GameObject root = SlotRoot($"{b.Name}Slot", b.Script("Slot"), Accent, out Component slot, true);
            Image shade = NewImage("Shade", root.transform, Shade);
            Place(shade.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 110f));

            Text category = NewText("Category", root.transform, "FEATURED", 22, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.85f), FontStyle.Bold);
            Place(category.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(22f, -70f), new Vector2(-110f, -18f));
            Text value = NewText("Value", root.transform, "0", 52, TextAnchor.UpperRight, TextColor, FontStyle.Bold);
            Place(value.rectTransform, new Vector2(0.5f, 1f), Vector2.one, new Vector2(0f, -90f), new Vector2(-22f, -14f));
            Text title = NewText("Title", root.transform, "Title", 32, TextAnchor.LowerLeft, TextColor, FontStyle.Bold);
            Place(title.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(22f, 18f), new Vector2(-22f, 96f));

            Set(slot, "background", root.GetComponent<Image>());
            Set(slot, "category", category);
            Set(slot, "valueLabel", value);
            Set(slot, "title", title);
            Set(slot, "button", root.GetComponent<Button>());
            return root;
        }

        // ---------- Grid Carousel ----------

        private const float PAGER_HALF_HEIGHT = 340f;

        private static void BuildGridCarousel(ExampleBuild b)
        {
            GameObject slot = b.Save(ShopTile(b), $"{b.Name}Slot");
            RectTransform body = ScreenRoot(b.Name, b.Script("Screen"), b.Title, "Swipe a page at a time", out Component screen, out _);

            RectTransform strip = Place(NewRect("Strip", body), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -PAGER_HALF_HEIGHT), new Vector2(0f, PAGER_HALF_HEIGHT));
            ScrollerBase list = NewList(b.Script("Scroller"), strip, slot);
            ScrollerSettings settings = list.Settings;
            settings.direction = ScrollerDirection.LeftToRight;
            settings.slotsPerLine = 2;
            settings.lineSpacing = 20f;
            settings.slotSpacing = 20f;
            settings.snap = SnapMode.Page;
            settings.snapAlign = SnapAlign.Start;

            RectTransform pager = Place(NewRect("Pager", body), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -PAGER_HALF_HEIGHT - 130f), new Vector2(0f, -PAGER_HALF_HEIGHT - 30f));
            Text pageLabel = NewText("Page", pager, "Page 1 / 1", 34, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            Button previous = LabelButton("Previous", pager, "‹", PanelLight, 56);
            Pin((RectTransform)previous.transform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(140f, 100f));
            Button next = LabelButton("Next", pager, "›", PanelLight, 56);
            Pin((RectTransform)next.transform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(140f, 100f));

            Set(screen, "pageLabel", pageLabel);
            Set(screen, "previous", previous);
            Set(screen, "next", next);
            Finish(b, screen, list);
        }

        // ---------- Looping Carousel ----------

        private const float CAROUSEL_HALF_HEIGHT = 380f;

        private static void BuildLoopCarousel(ExampleBuild b)
        {
            GameObject root = SlotRoot($"{b.Name}Slot", b.Script("Slot"), Color.clear, out Component slot);
            RectTransform face = Stretch(NewRect("Face", root.transform), 12f, 0f, 12f, 0f);
            CanvasGroup faceGroup = face.gameObject.AddComponent<CanvasGroup>();
            Image art = AddImage(face.gameObject, Accent, true);
            Image shade = NewImage("Shade", face, Shade);
            Place(shade.rectTransform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 200f));
            Text title = NewText("Title", face, "World", 46, TextAnchor.LowerLeft, TextColor, FontStyle.Bold);
            Place(title.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(32f, 96f), new Vector2(-32f, 176f));
            Text subtitle = NewText("Subtitle", face, "World 1", 28, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.8f));
            Place(subtitle.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(32f, 36f), new Vector2(-32f, 92f));

            Set(slot, "face", face);
            Set(slot, "faceGroup", faceGroup);
            Set(slot, "art", art);
            Set(slot, "title", title);
            Set(slot, "subtitle", subtitle);
            GameObject slotPrefab = b.Save(root, $"{b.Name}Slot");

            RectTransform body = ScreenRoot(b.Name, b.Script("Screen"), b.Title, "Drag sideways: it loops", out Component screen, out _);
            RectTransform strip = Place(NewRect("Strip", body), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -CAROUSEL_HALF_HEIGHT), new Vector2(0f, CAROUSEL_HALF_HEIGHT));
            ScrollerBase list = NewList(b.Script("Scroller"), strip, slotPrefab);
            ScrollerSettings settings = list.Settings;
            settings.direction = ScrollerDirection.LeftToRight;
            settings.loop = true;
            settings.snap = SnapMode.Item;
            settings.snapAlign = SnapAlign.Center;

            Text caption = NewText("Caption", body, "", 40, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            Place(caption.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -CAROUSEL_HALF_HEIGHT - 120f), new Vector2(0f, -CAROUSEL_HALF_HEIGHT - 20f));
            Set(screen, "caption", caption);
            Finish(b, screen, list);
        }

        // ---------- Sections ----------

        private static void BuildSections(ExampleBuild b)
        {
            GameObject row = SlotRoot($"{b.Name}Slot", b.Script("Slot"), Panel, out Component slot, true);
            Image avatar = NewImage("Avatar", row.transform, Accent);
            Pin(avatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(84f, 84f));
            Text initials = NewText("Initials", avatar.transform, "AL", 30, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            Image dot = NewImage("Online", avatar.transform, Online);
            Pin(dot.rectTransform, new Vector2(1f, 0f), new Vector2(6f, -6f), new Vector2(26f, 26f));
            Text contactName = NewText("Name", row.transform, "Name", 32, TextAnchor.LowerLeft, TextColor, FontStyle.Bold);
            Place(contactName.rectTransform, new Vector2(0f, 0.5f), Vector2.one, new Vector2(140f, 2f), new Vector2(-28f, -14f));
            Text phone = NewText("Phone", row.transform, "+1 555", 26, TextAnchor.UpperLeft, Muted);
            Place(phone.rectTransform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(140f, 14f), new Vector2(-28f, -2f));
            Set(slot, "avatar", avatar);
            Set(slot, "initials", initials);
            Set(slot, "onlineDot", dot.gameObject);
            Set(slot, "contactName", contactName);
            Set(slot, "phone", phone);
            Set(slot, "button", row.GetComponent<Button>());
            GameObject rowPrefab = b.Save(row, $"{b.Name}Slot");

            GameObject header = SlotRoot($"{b.Name}HeaderSlot", b.Script("HeaderSlot"), Background, out Component headerSlot);
            Text letter = NewText("Letter", header.transform, "A", 40, TextAnchor.MiddleLeft, Accent, FontStyle.Bold);
            Stretch(letter.rectTransform, 12f, 0f, 12f, 0f);
            Text count = NewText("Count", header.transform, "0 contacts", 26, TextAnchor.MiddleRight, Muted);
            Stretch(count.rectTransform, 12f, 0f, 12f, 0f);
            Set(headerSlot, "letter", letter);
            Set(headerSlot, "count", count);
            GameObject headerPrefab = b.Save(header, $"{b.Name}HeaderSlot");

            ScrollerBase list = Screen(b, "Grouped by first letter", rowPrefab, out Component screen, out _);
            ScrollerSettings settings = list.Settings;
            settings.lineSpacing = 8f;
            settings.headerContentSpacing = 8f;
            settings.sectionSpacing = 24f;
            Set(list, "headerPrefab", headerPrefab);
            Finish(b, screen, list);
        }

        // ---------- Nested Shelves ----------

        private static void BuildNested(ExampleBuild b)
        {
            GameObject card = SlotRoot($"{b.Name}CardSlot", b.Script("CardSlot"), Panel, out Component cardSlot, true);
            Image art = NewImage("Art", card.transform, Accent);
            Place(art.rectTransform, new Vector2(0f, 0.32f), Vector2.one, Vector2.zero, Vector2.zero);
            Text initial = NewText("Initial", art.transform, "A", 96, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f), FontStyle.Bold);
            Text title = NewText("Title", card.transform, "Title", 28, TextAnchor.MiddleLeft, TextColor, FontStyle.Bold);
            Place(title.rectTransform, new Vector2(0f, 0.15f), new Vector2(1f, 0.32f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
            Text rating = NewText("Rating", card.transform, "★ 4.5", 24, TextAnchor.MiddleLeft, Gold);
            Place(rating.rectTransform, Vector2.zero, new Vector2(1f, 0.15f), new Vector2(16f, 6f), new Vector2(-16f, 0f));
            Set(cardSlot, "art", art);
            Set(cardSlot, "initial", initial);
            Set(cardSlot, "title", title);
            Set(cardSlot, "rating", rating);
            Set(cardSlot, "button", card.GetComponent<Button>());
            GameObject cardPrefab = b.Save(card, $"{b.Name}CardSlot");

            GameObject shelf = SlotRoot($"{b.Name}Slot", b.Script("Slot"), Background, out Component shelfSlot);
            Text shelfTitle = NewText("Title", shelf.transform, "Shelf", 36, TextAnchor.MiddleLeft, TextColor, FontStyle.Bold);
            Place(shelfTitle.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(8f, -72f), new Vector2(-200f, 0f));
            Button seeAll = LabelButton("SeeAll", shelf.transform, "See all", PanelLight, 26);
            Pin((RectTransform)seeAll.transform, Vector2.one, new Vector2(-8f, -8f), new Vector2(170f, 56f));
            RectTransform strip = Stretch(NewRect("Strip", shelf.transform), 0f, 0f, 0f, 84f);
            ScrollerBase carousel = NewList(b.Script("ShelfScroller"), strip, cardPrefab);
            ScrollerSettings carouselSettings = carousel.Settings;
            carouselSettings.direction = ScrollerDirection.LeftToRight;
            carouselSettings.lineSpacing = 20f;
            carouselSettings.snap = SnapMode.Item;
            carouselSettings.snapAlign = SnapAlign.Start;
            FinishList(carousel);
            Set(shelfSlot, "title", shelfTitle);
            Set(shelfSlot, "seeAll", seeAll);
            Set(shelfSlot, "carousel", carousel);
            GameObject shelfPrefab = b.Save(shelf, $"{b.Name}Slot");

            ScrollerBase list = Screen(b, "Drag a shelf sideways", shelfPrefab, out Component screen, out _);
            list.Settings.lineSpacing = 28f;
            Finish(b, screen, list);
        }

        // ---------- Shared ----------

        /// <summary>The screen root with a list filling its body.</summary>
        private static ScrollerBase Screen(ExampleBuild b, string subtitle, GameObject slotPrefab, out Component screen, out Text status)
        {
            RectTransform body = ScreenRoot(b.Name, b.Script("Screen"), b.Title, subtitle, out screen, out status);
            return NewList(b.Script("Scroller"), body, slotPrefab);
        }

        /// <summary>Applies the list's settings, links it to the screen script and saves the screen prefab.</summary>
        private static void Finish(ExampleBuild b, Component screen, ScrollerBase list)
        {
            FinishList(list);
            Set(screen, "scroller", list);
            GameObject prefab = b.Save(screen.gameObject, b.Name);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
    }
}
