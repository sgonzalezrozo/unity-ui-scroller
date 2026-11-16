# UI Scroller

**A recycling (virtualized) scroll view for Unity uGUI: lists, grids and bento layouts of any length, zero allocations while scrolling, built and measured on low-end phones.**

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Tested on Unity 6000.3](https://img.shields.io/badge/Unity-6000.3%20tested-black?logo=unity)](#compatibility)
[![Version 1.0.0-preview.1](https://img.shields.io/badge/version-1.0.0--preview.1-blue.svg)](CHANGELOG.md)

UI Scroller extends Unity's `ScrollRect`. You give it a source of items and a slot prefab; it creates only the slots that fit on screen, plus a small buffer, and moves and rebinds them as you scroll. Its cost depends on what's on screen, never on how many items there are: 300 or 100,000 items cost the same.

<!-- Demo GIF: Documentation~/images/demo.gif -->

## Contents

- [At a glance](#at-a-glance) · [Is it right for your project?](#is-it-right-for-your-project) · [Features](#features) · [Compatibility](#compatibility)
- [Installation](#installation) · [Quick start](#quick-start)
- **Guide:** [Data and updates](#data-and-updates) · [Item sizes](#item-sizes) · [Layouts](#layouts) · [Sections and headers](#sections-and-headers) · [Expandable slots](#expandable-slots) · [Scrolling](#scrolling) · [Paging and pull to refresh](#paging-and-pull-to-refresh) · [Filtering](#filtering)
- **Reference:** [Performance](#performance) · [Inspector](#inspector) · [API](#api) · [Dependency injection](#dependency-injection)
- [Limitations and roadmap](#limitations-and-roadmap) · [Troubleshooting](#troubleshooting) · [FAQ](#faq) · [Project status](#project-status)

## At a glance

- **Set up in seconds.** Add your list component to a sized UI object: it builds the Viewport (with a `RectMask2D` and its own `Canvas`) and Content. The inspector shows only what a list needs: direction, layout, spacing and padding.
- **Sizes the way you build slots:**
  - a fixed size from the slot;
  - the slot's own Unity layout, measured once and cached, with no `ContentSizeFitter`;
  - or both: fixed when closed, measured when expanded.

  Late text (localization, downloaded content) is followed without code, and what's on screen never jumps, even mid-drag.
- **Every layout a game screen needs:** vertical and horizontal lists in both directions, fixed and automatic grids, **bento** (tiles of different sizes, described in JSON), **sections with headers** of any height, expandable slots, looping carousels with snapping.
- **Data that arrives in pages:** the list asks for the next page by itself, appends it without a rebuild, and cancels loads it no longer needs.
- **Built for phones:**
  - scrolling allocates nothing;
  - pooled slots stay active out of view, so reusing one doesn't wake its graphics;
  - scrolling rebatches only the list, not the rest of the screen.

Measured on a low-end phone (Galaxy A02s) and a mid-range one (Pixel 5), with a real list (TMP text, 100 px rows):

| | Galaxy A02s (60 Hz) | Pixel 5 (90 Hz) |
|---|---|---|
| The list's own work per frame, scrolling 100,000 items | 0.28 ms | 0.12 ms |
| Whole frame CPU while scrolling, the app included | 7.6 of 16.7 ms | 2.9 of 11.1 ms |
| Fast fling (16,000 px/s), whole frame | 8.3 ms | 4.0 ms |
| Memory allocated while scrolling | 0 B | 0 B |
| Opening slots while scrolling (TMP text, up to six sentences), slowest frame | 25 ms, no missed frame at 4 opens a second | 8.9 ms |
| Slowdown or memory growth over 2 minutes | none | none |

Every run is in [`Documentation~/benchmarks.md`](Documentation~/benchmarks.md). A fully automated bench on more devices (iPhone, WebGL) is in progress: see [`Documentation~/performance-plan.md`](Documentation~/performance-plan.md).

## Is it right for your project?

**UI Scroller fits if you:**
- build your UI with uGUI and show long lists, grids or store-style layouts;
- target phones, where GC spikes and canvas rebuilds hurt;
- want measured sizes, sections, paging, expanding and bento in one component, with tests behind them.

**Look elsewhere if you:**
- build your UI with UI Toolkit: its `ListView` already virtualizes ([comparison](#scrollrect-or-listview));
- have a short list (a few dozen items) that doesn't need virtualization;
- need features from [Limitations and roadmap](#limitations-and-roadmap) that aren't there yet, such as nested scrolling, several slot prefabs per list, sticky headers or gamepad navigation.

**Adopting it, honestly:**
- it's a preview, by a single author, not yet used in a shipped game;
- it's covered by about 470 automated tests (EditMode and PlayMode) that run in batch mode, and by a benchmark measured on Android phones;
- it's MIT licensed, with a [changelog](CHANGELOG.md) that marks every breaking change.

See [Project status](#project-status).

## Features

| Area | What you get |
|---|---|
| Recycling | Only visible slots exist; a small buffer of pooled slots that stay active out of view; the first screen shows in the first frame; the pool warms up two slots per frame |
| Layouts | Lists, fixed grids, auto grids (columns from the width), **bento** (spans or patterns in JSON), in all four directions |
| Sizes | Fixed (`DefaultExtent`) or measured with Unity's layout, cached per item, followed when content changes late |
| Sections | `SectionedScroller`: a header per section, headers of any height, section spacing, works with grids and bento |
| Expanding | Accordion slots, one or several open, fixed or measured expanded size, nothing above moves |
| Scrolling | `ScrollToItem` with alignment, animation and offset; snapping to items or pages; looping carousels |
| Data | `ListSource`, `FilteredSource`, `PagedSource` (async pages with cancellation), `SectionedSource` |
| Events and queries | Expanded, collapsed, rebuilt, Content size changed, snapped, paging events; item offsets, sizes, tiles, visibility |
| Editor | A minimal inspector with labels that follow the direction; layout components on Content are refused, with a fix button |

## Compatibility

| Requirement | Supported |
|---|---|
| Unity | Tested on Unity 6 (`6000.3.6f1` and `6000.3.16f1`). The package declares `2022.3` as its minimum, but older versions aren't tested yet. |
| Scripting backend | Mono (Editor) and IL2CPP (Android, 32-bit ARMv7 and 64-bit ARM64) |
| Platforms | macOS Editor; Android, tested on a Samsung Galaxy A02s and a Google Pixel 5. iOS and WebGL runs are planned (see the performance plan); desktop players aren't tested yet. |
| UI framework | uGUI only |
| Enter Play Mode with domain reload disabled | Supported by design: the runtime keeps no static state between plays (its only static field is an internal switch for tests). Not yet checked by hand. |
| Dependencies | [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) and uGUI. The Quick Start and Extras samples use TextMeshPro; the Examples sample doesn't. |

## Installation

Git must be installed on your machine.

### 1. Install Tools Core

UI Scroller needs [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), the shared code that defines the injection interfaces. Unity doesn't install Git dependencies by itself, so add it first:

1. In Unity, open **Window > Package Manager**.
2. Select **+ > Add package from git URL**.
3. Enter:

```text
https://github.com/sgonzalezrozo/unity-tools-core.git
```

### 2. Install UI Scroller

Same steps, with:

```text
https://github.com/sgonzalezrozo/unity-ui-scroller.git
```

To pin a release, add its tag:

```text
https://github.com/sgonzalezrozo/unity-ui-scroller.git#v1.0.0-preview.1
```

The package itself doesn't need TextMeshPro. The Quick Start and Extras samples use it: it's part of uGUI in Unity 6, and on Unity 2022.3 you install it from the Package Manager.

### Samples

Import them from the package's page in the Package Manager (**Samples** tab):

| Sample | What's in it |
|---|---|
| **Examples** | Seven scenes that build their UI in code: add one to an empty GameObject in an empty scene and press Play. `GridExample`, `BentoExample`, `BentoSectionsExample`, `SectionsExample`, `ExpandableExample`, `CarouselExample` (loop and snap) and `PagingExample` (paging and pull to refresh). No TextMeshPro needed. |
| **Quick Start** | The scripts for the quick start below. |
| **Extras** | A pull-to-refresh indicator and an empty-state panel. |

## Quick start

### 1. Your data and your slot

Items are classes. The slot receives its item through `Data`:

```csharp
using UIScroller;
using TMPro;
using UnityEngine;

public class InventoryItem
{
    public string Name;
}

public class InventorySlot : MonoBehaviour, ISlot<InventoryItem>
{
    [SerializeField] private TMP_Text label;

    private InventoryItem data;

    public float DefaultExtent => 80f; // a fixed height; -1 measures it with layout

    public InventoryItem Data
    {
        get => data;
        set
        {
            data = value;
            label.text = value.Name; // reset every visual here: slots are reused
        }
    }
}
```

Save the slot as a prefab, with a `RectTransform` on its root.

### 2. A list class

`Scroller<T>` is generic, and Unity can't add an open generic component, so create a concrete class:

```csharp
using UIScroller;

public class InventoryList : Scroller<InventoryItem>
{
}
```

### 3. The scene

1. Under your Canvas, create an empty UI GameObject with the size and anchors the list should have on screen.
2. Add `InventoryList`. It builds a stretched **Viewport** and a **Content** inside it, and wires them up. The Viewport gets:
   - a `RectMask2D`, which clips and culls slots outside it;
   - a nested `Canvas`, so scrolling rebuilds only the list's batches;
   - a `GraphicRaycaster`, so buttons in slots get clicks.

   A list set up another way gets the Canvas and the raycaster when it starts.
3. Assign the slot prefab to **Slot Prefab**, and pick the **Direction** and **Layout**.

Don't start from Unity's **UI > Scroll View**: it adds scrollbars, a stencil `Mask` and an `Image` on the Viewport that this list doesn't use.

**Content must have no layout components** (`ContentSizeFitter`, `LayoutGroup`, `LayoutElement`, `AspectRatioFitter`), and the Viewport no `LayoutGroup`. The list sets Content's size itself. The editor removes such a component as soon as you add one, the inspector shows an error with a **Remove Layout Components** button, and `Initialize` refuses to start and names the component in the way.

<!-- Screenshot: Documentation~/images/hierarchy-and-inspector.png -->

### 4. Start it

```csharp
using System.Collections.Generic;
using UIScroller;
using UnityEngine;

public class InventoryScreen : MonoBehaviour
{
    [SerializeField] private InventoryList list;

    private void Start()
    {
        List<InventoryItem> items = new();
        for (int i = 0; i < 10_000; i++)
            items.Add(new InventoryItem { Name = $"Item {i}" });

        list.Initialize(new ListSource<InventoryItem>(items));
    }
}
```

`OnInitialized` fires inside `Initialize`, with the first screen already bound: subscribe before calling it.

## Data and updates

A list reads its items from a source (`IItemSource<T>`). Change the source, then call `Refresh()`:

```csharp
source.AddItem(new InventoryItem { Name = "New item" });
list.Refresh();
```

- `ListSource<T>` also has `AddItems`, `InsertItem`, `RemoveItem`, `RemoveItemAt`, `SetItems`, `ReplaceItems` and `Clear`.
- `SetSource(newSource)` switches to another source. It also works before `Initialize` (it calls it).
- `Refresh()` with the same item count rebinds the slots in place, keeps expanded items and allocates nothing.
- To build the source elsewhere (a screen's Model, for example), implement `IItemSourceProvider<T>` and pass its `GetSource()`.

## Item sizes

Each slot decides how big it is through `DefaultExtent` (the height in a vertical list, the width in a horizontal one):

- **A positive number is a fixed size.** Nothing is measured. This is the fastest option.
- **0 or less (for example `-1`) measures the slot with Unity's layout.** Text, localized strings and images decide each item's size.

### Building a slot that sizes itself

- On the prefab's root, add a `VerticalLayoutGroup` (a `HorizontalLayoutGroup` in a horizontal list) with **Control Child Size > Height** on (**Width** in a horizontal list) and **Child Force Expand > Height** off.
- Inside it, use text with wrapping on, images with a `LayoutElement` or an `AspectRatioFitter`, and nested layout groups.
- **Don't put a `ContentSizeFitter` on the root.** The list sets the root's size, and a fitter would fight it. Fitters on children are fine.
- The list always sets the width: the full width in a list, the column width in a grid. That's what makes text wrap correctly.

If the root reports no preferred size (no layout group), the slot keeps the prefab's size and the list logs one warning.

### How measuring works

- A slot is measured when it's first shown. Items not measured yet use the average of the measured ones.
- **Sizes are cached:** an item scrolled back into view reuses its size, with no layout rebuild. `Refresh()` measures the items on screen again.
- **Late changes are followed.** A string that arrives late, an image that finishes loading, or a child turned on or off resizes the slot one frame later. The list adds a `SlotSizeWatcher` to measured slots by itself.
- **What you're looking at stays still.** Items measured for the first time while you scroll up grow upwards, and an item whose content changed keeps its start and pushes the ones after it. This also holds mid-drag, with no jump and no extra speed.
- In a grid, a row is as tall as its tallest slot.

When a size depends on something the slot can't notice, ask for a new measurement:

```csharp
list.InvalidateSize(index); // this item
list.InvalidateAllSizes();  // every item
```

Items on screen are measured at once, the others when they're shown. `InvalidateAllSizes()` is called for you when the list's width changes and, with Unity Localization installed, when the language changes. Both do nothing for fixed sizes.

## Layouts

### Lists and directions

**Direction** is Top To Bottom, Bottom To Top, Left To Right or Right To Left. Bottom To Top and Right To Left start from the opposite end, as in a list whose newest item sits at the bottom. Every layout works in every direction.

### Grids

Set **Layout** to:
- **Grid** for a fixed number of columns (rows in a horizontal list);
- **Auto Grid** for as many as the width allows, from a target slot size, between a minimum and a maximum.

Slots stretch to fill the line. The space between slots on a line is **Column Spacing** (**Row Spacing** in a horizontal list).

From code, before `Initialize`: `list.Settings.slotsPerLine = 3` for a fixed grid, or `list.Settings.autoSlotsPerLine = true` with `targetSlotSize` (and optionally `minSlotsPerLine` and `maxSlotsPerLine`).

### Bento

Bento places tiles of different sizes on a grid of cells: a big featured tile, a strip of small ones, a tall one beside two stacked. Set **Layout** to **Bento** and give the list a **Layout JSON** (a TextAsset), or set it from code. Items don't choose their size: the layout does.

A layout is either:
- **spans**, a sequence of tile sizes packed in order: each tile goes in the first gap where it fits, without going back to fill earlier holes;
- or **patterns**, blocks of cells drawn tile by tile.

Both repeat from `repeatFrom` after their last entry.

```json
{
  "columns": 4,
  "repeatFrom": 1,
  "patterns": [
    { "rows": 2, "tiles": [ {"c":0,"r":0,"w":2,"h":2}, {"c":2,"r":0,"w":2,"h":1}, {"c":2,"r":1,"w":1,"h":1}, {"c":3,"r":1,"w":1,"h":1} ] },
    { "rows": 1, "tiles": [ {"c":0,"r":0,"w":1,"h":1}, {"c":1,"r":0,"w":1,"h":1}, {"c":2,"r":0,"w":1,"h":1}, {"c":3,"r":0,"w":1,"h":1} ] }
  ]
}
```

```json
{ "columns": 4, "repeatFrom": 1, "spans": [ {"w":2,"h":2}, {"w":1,"h":1} ] }
```

```csharp
list.Settings.bento = true;
list.Settings.cellAspect = 1f;                    // row height / column width
list.SetBentoLayout(BentoLayout.FromJson(json));  // or BentoLayout.FromSpans / FromPatterns
list.Initialize(source);
```

- Columns come from the layout. The column width comes from the list's width and **Column Spacing**, and the row height from **Cell Aspect**. Tiles stretch to their cells.
- **Every layout is checked.** A broken one logs its errors (the inspector lists them too) and places items as 1x1 tiles. `layout.Errors` has them in code.
- Paging works: new pages continue the layout without moving what's on screen.
- `SetBentoLayout` after `Initialize` rebuilds with the new layout. `GetTile(index)` tells you where an item sits.
- Another JSON library can fill `BentoLayoutData` for `BentoLayout.FromData`.
- Tiles don't expand, and a bento list doesn't loop.

## Sections and headers

For a list split into sections, each with a header, use `SectionedScroller<TItem, THeader>` with a `SectionedSource<TItem, THeader>`. Each section is a header plus its own source of items:

```csharp
public class InventorySection
{
    public string Title;
}

// The header slot, on the Header Prefab. -1 measures it, so each header can be as tall as its own content.
public class InventoryHeaderSlot : MonoBehaviour, ISlot<InventorySection>
{
    [SerializeField] private TMP_Text title;

    private InventorySection data;

    public float DefaultExtent => -1f;

    public InventorySection Data
    {
        get => data;
        set
        {
            data = value;
            title.text = value.Title;
        }
    }
}

public class InventorySectionsList : SectionedScroller<InventoryItem, InventorySection>
{
}
```

Assign **Slot Prefab** and **Header Prefab**, then build the sections:

```csharp
SectionedSource<InventoryItem, InventorySection> source = new();
source.AddSection(new InventorySection { Title = "Weapons" }, new ListSource<InventoryItem>(weapons));
source.AddSection(new InventorySection { Title = "Potions" }, new ListSource<InventoryItem>(potions));

list.SetSource(source); // starts the list the first time, replaces the sections after that
```

- **Headers** follow the same size rule as slots: a positive `DefaultExtent` is fixed, 0 or less is measured. Measured headers can each have a different height.
- **Section Spacing** adds space before every header except the first.
- **In a grid or bento,** a header takes a whole row and each section starts a new row. In bento, the layout starts again in each section, and a tall tile never reaches into the next section.
- **Empty sections are skipped**, header included. Set `source.SkipEmptySections = false` to show the header of an empty section.
- `InsertSection`, `RemoveSection` and `ClearSections` change the sections. After changing a section's own source, call `list.Refresh()`.
- `list.ScrollToSection(index, duration)` goes to a section's header (or the next section shown, if that one is empty).
- **Indexes in the list's API count headers.** `source.FlatIndexOf(section, indexInSection)` gives an item's index, `source.FlatIndexOf(section, -1)` its header's, and `source.Locate(index)` goes the other way.
- `ISectionedSourceProvider<TItem, THeader>` builds the source elsewhere: call `list.SetSource(provider)`.
- A sectioned list doesn't page.

## Expandable slots

Implement `IExpandableSlot<T>`. The list sets `Owner` and `ItemIndex` every time it binds the slot, and `IsExpanded` after `Data`:

```csharp
public class ExpandableInventorySlot : MonoBehaviour, IExpandableSlot<InventoryItem>
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject details;

    private InventoryItem data;
    private bool isExpanded;

    public float DefaultExtent => 80f;
    public IScroller Owner { get; set; }
    public int ItemIndex { get; set; }

    public InventoryItem Data
    {
        get => data;
        set
        {
            data = value;
            label.text = value.Name;
        }
    }

    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            isExpanded = value;
            details.SetActive(value);
        }
    }

    // Hook this up to the slot's button.
    public void Toggle()
    {
        if (IsExpanded)
            Owner.Collapse(ItemIndex);
        else
            Owner.Expand(ItemIndex);
    }

    // Called when the list closes this slot because another one opened.
    public void OnCollapsed()
        => IsExpanded = false;
}
```

- **Fixed-size slots** (`DefaultExtent` above 0), the usual case: closed, the slot keeps its fixed size and nothing is measured while scrolling.
  - `Expand(index)` without a size measures the open slot in Unity's own layout pass, so it grows on the frame after the tap. It follows its details while open, including late text. The root needs a layout group (or a `LayoutElement`) that reports the open height; without one it opens to twice its size.
  - `Expand(index, 240f)` opens it to a fixed size. `SetExpandedSize(index, size)` resizes an open slot.
- **Measured slots** (`DefaultExtent` of 0 or less) size themselves: `Expand` and `Collapse` only change the state, and the slot is measured with its details on or off. A size passed to `Expand` is ignored, with a warning.
- Opening or closing never moves what's on screen above the item.
- **Allow Multiple Expanded** keeps more than one open.
- Lists only: grids and bento don't expand, because opening one slot would change the others on its row.

## Scrolling

### To an item

```csharp
list.ScrollToItem(42);                                // jump, item at the start
list.ScrollToItem(42, ScrollAlign.Center, 0.4f);      // animate for 0.4 s, item in the middle
list.ScrollToItem(42, ScrollAlign.Nearest);           // move as little as possible; not at all if it's visible
list.ScrollToItem(42, ScrollAlign.Start, 0.3f, 16f);  // leave 16 px before it
```

`ScrollToStart(duration)` and `ScrollToEnd(duration)` do the same for the ends. The animation eases out, follows items measured on the way, and stops when the user drags or you call `ForceStopScrolling()`. `IsScrolling` is true while it runs.

### Snapping

- **Snap: Item** settles on the nearest item once a drag ends and the list slows down, lined up with the viewport's start, centre or end (**Snap Align**).
- **Snap: Page** moves one viewport at a time, like a pager: a flick goes to the next or previous page, a slow release to the nearest one.

`SnappedTo(index)` tells you where it landed.

### Looping lists (carousels)

Tick **Loop** and the list goes round forever. Indexes stay real (`0` to `count - 1`) everywhere in the API, and `ScrollToItem` takes the shortest way round. For a carousel, combine it with **Snap: Item** and **Snap Align: Center**:

```csharp
carousel.SnappedTo += index => Debug.Log($"Showing {index}");
carousel.ScrollToItem(3, ScrollAlign.Center, 0.3f);
```

You never see the loop jump, and drags and flings carry on across it.
- **A short list** lays out a few copies of its items and jumps back by whole copies near either end.
- **A long one** lays out about ten screens of entries mapped to items through an offset, so the scroll position stays small and movement and inertia stay exact. In a long loop, `ScrollToItem` to an item far away jumps there instead of animating across thousands of items.

A looping list is a plain list: no grid, bento, sections, paged source or expanding (it logs an error and doesn't loop), and no pull to refresh, because it never reaches its start.

## Paging and pull to refresh

### Paging

Implement `IPageLoader<T>` and wrap it in a `PagedSource<T>`. Pages start at 0. Throw to report a failure:

```csharp
public class InventoryPageLoader : IPageLoader<InventoryItem>
{
    private const int PageSize = 50;
    private const int PageCount = 10;

    public Task<PageResult<InventoryItem>> LoadAsync(int page, CancellationToken cancellation)
    {
        List<InventoryItem> items = new(PageSize);
        for (int i = 0; i < PageSize; i++)
            items.Add(new InventoryItem { Name = $"Item {page * PageSize + i}" });

        return Task.FromResult(new PageResult<InventoryItem>(items, page + 1 < PageCount));
    }
}
```

```csharp
list.SetSource(new PagedSource<InventoryItem>(new InventoryPageLoader()));

list.PageLoaded += count => Debug.Log($"{count} more items");
list.PageFailed += exception => Debug.LogWarning(exception.Message);
list.ReachedEnd += () => Debug.Log("No more pages");
```

- The list loads the first page by itself, then the next one whenever fewer than 10 items are left after the last visible one.
- New pages are appended without a rebuild: the scroll position and expanded items stay. A page that lands while the list bounces back from its end stops the bounce instead of drifting it back.
- Only one page loads at a time. Changing the source or destroying the list cancels a load in flight: the loader receives a cancelled token.
- `source.Reset()` starts again from page 0, for example on pull to refresh.

### Pull to refresh

Override `OnPullToRefreshDetected()`. It runs when the user releases a drag more than 75 px past the start of the list:

```csharp
public class RefreshingList : Scroller<InventoryItem>
{
    protected override void OnPullToRefreshDetected()
    {
        if (DataSource is IPagedSource<InventoryItem> paged)
            paged.Reset();
    }
}
```

The **Extras** sample has a `PullIndicator` for a status label and spinner, which you drive yourself, and an `EmptyStatePanel` that shows a message when the list is empty.

## Filtering

Wrap a source in a `FilteredSource<T>`:

```csharp
FilteredSource<InventoryItem> filtered = new(items, item => item.Name.Contains("Sword"));
list.Initialize(filtered);

filtered.SetPredicate(item => item.Name.StartsWith("A"));
list.Refresh();
```

The matching items are worked out once and cached. `SetPredicate` and `ClearPredicate` work them out again; call `Invalidate()` after the underlying source changes.

## Performance

**What the tests guarantee** (they run on every verify):
- scrolling 100,000 items allocates 0 bytes;
- `Refresh()` with the same count allocates 0 bytes and rebinds slots in place;
- scrolling checks only the slots on screen, and places only the slot that came in;
- no slot ever leaves Content while scrolling;
- rebuilds and new pages only create what's new.

**How it stays cheap:**
- **The Viewport is a nested Canvas,** so moving slots rebatches only the list, not the rest of the screen.
- **Pooled slots stay active,** parked out of view, where the mask culls them. Reusing a slot doesn't run its `OnEnable` or wake its graphics: on a low-end phone, that cut the list's own cost by about three quarters.
- **Sizes are cached,** and an opened slot is measured in Unity's own layout pass, so TMP lays its text out once.
- **Scrolls under half a pixel** skip the visible-range work.
- **The first screen** shows in the first frame. The pool warms up two slots per frame, and disabling and enabling a list rebinds nothing.

**Tips:**
- Refresh the slot from its `Data` setter, not from `OnEnable` (a reused slot was never disabled).
- Turn off **Raycast Target** on graphics that don't need clicks.
- Use a fixed `DefaultExtent` when every slot has the same size.
- Keep measured slots shallow. Nested layout groups make each first measurement slower.
- With long TMP text that appears when a slot opens, pre-fill the font asset or use a static atlas.

Numbers per device, and how to run the bench, are in [`Documentation~/benchmarks.md`](Documentation~/benchmarks.md). The zero-GC audit and the automated device bench are planned in [`Documentation~/performance-plan.md`](Documentation~/performance-plan.md). Measure at your own list sizes on your target devices.

## Inspector

Labels follow the direction: "Padding Top" in a top-to-bottom list, "Padding Left" in a left-to-right one. The `ScrollRect` settings underneath are hidden and driven by these. Settings are read-only while playing.

| Setting | Purpose |
|---|---|
| Slot Prefab | The prefab used for slots |
| Header Prefab | The prefab used for headers (sectioned lists only) |
| Direction | Top to bottom, bottom to top, left to right or right to left |
| Layout | List, Grid (a fixed count per line), Auto Grid (as many as fit) or Bento (tiles from a JSON layout) |
| Columns / Rows, Target size, Min, Max | Grid settings, shown for the chosen layout |
| Layout JSON, Cell Aspect | Bento settings: the layout, and row height divided by column width. Errors in the JSON are shown under the settings |
| Row Spacing / Column Spacing | Space between lines, and between slots on a line |
| Section Spacing | Space before every header except the first (sectioned lists only) |
| Padding (start and end) | Space before the first line and after the last |
| Loop | Goes round forever, like a carousel (lists only) |
| Snap, Snap Align | After a drag, settle on the nearest item, or move one page at a time |
| Allow Multiple Expanded | Keeps more than one slot open (lists only) |
| Reset Scroll On Enable | Goes back to the start when the list is enabled again |
| Override Pool Buffer, Pool Buffer | How many extra slots to keep ready beyond the visible ones |

## API

### Methods and properties

| API | Purpose |
|---|---|
| `Initialize(source)` | Starts the list with a source |
| `SetSource(source)` | Replaces the source and refreshes (starts the list if needed) |
| `Refresh()` | Rebuilds from the current source |
| `Clear()` | Clears the shown slots and resets the scroll position |
| `ScrollToItem(index, align = Start, duration = 0, offset = 0)` | Scrolls to an item, at once or animated |
| `ScrollToStart(duration = 0)`, `ScrollToEnd(duration = 0)` | Scrolls to an end |
| `ForceStopScrolling()` | Stops any movement, including an animation or a snap |
| `Expand(index, size = -1)`, `Collapse(index)`, `SetExpandedSize(index, size = -1)` | Opens, closes or resizes a slot |
| `IsExpanded(index)`, `ExpandedCount` | Expansion state |
| `InvalidateSize(index)`, `InvalidateAllSizes()` | Measures again (measured slots) |
| `IsItemVisible(index)`, `VisibleStartIndex`, `VisibleEndIndex`, `IsFirstItemVisible`, `IsLastItemVisible` | What's on screen |
| `GetItemCount()`, `IsInitialized`, `IsScrolling`, `IsDragging` | State |
| `GetItemOffset(index)`, `GetItemSize(index)` | Where an item starts along the scroll axis, and its size (its line's in a grid, its tile's in bento); -1 if unknown |
| `ViewportSize`, `ContentSize` | Sizes along the scroll axis |
| `SetBentoLayout(layout)`, `GetTile(index)` | Bento: the layout, and an item's tile (counted from its section in a sectioned list) |
| `Settings`, `SlotPrefab` | Settings and prefab, read before `Initialize` |

`SectionedScroller<TItem, THeader>` has the same API, plus `SetSource(SectionedSource)`, `SetSource(ISectionedSourceProvider)`, `ScrollToSection(section, duration = 0)`, `Source` and `HeaderPrefab`. It has no `Initialize`: `SetSource` starts it.

### Overridable on your list class

| Member | Purpose |
|---|---|
| `BindItem(SlotHandle slot, int index)` | Shows an item in a slot (the default sets `Data` and `IsExpanded`) |
| `OnPullToRefreshDetected()` | The user pulled past the start |
| `OnPaginationThresholdReached()`, `SupportsPagination()` | Paging hooks |
| `DataSource`, `HasReachedLastPage` | The current source, and whether paging reached its end |

### Events

All are read-only notifications.

| Event | Raised when |
|---|---|
| `OnInitialized` | Initialization finished, inside `Initialize` (subscribe first) |
| `ItemExpanded(int)`, `ItemCollapsed(int)` | An item opened or closed (a closed one is raised before the next one's open) |
| `Rebuilt` | The list rebuilt its lines (`Refresh`, `SetSource`, a change of columns) |
| `ContentSizeChanged(float)` | The list changed Content's size (measuring, expanding, pages, Refresh), not when scrolling |
| `SnappedTo(int)` | The list settled on an item after a drag |
| `PageRequested`, `PageLoaded(int)`, `PageFailed(Exception)`, `ReachedEnd` | Paging |

### Bento types

| Type | Purpose |
|---|---|
| `BentoLayout` | `FromJson`, `FromData`, `FromSpans`, `FromPatterns`, `Uniform`; `Columns`, `IsValid`, `Errors`, `MaxRowSpan` |
| `BentoSpan`, `BentoTile`, `BentoPattern` | A tile's size, a tile's place, a block of tiles |
| `BentoLayoutData` and friends | The JSON form, for `JsonUtility` or any other JSON library |

## Dependency injection

The list works without a container, using `Instantiate`. With [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc), its instantiator can be injected. The `UIScroller` assembly exposes its internals to `Assembly-CSharp`, so Fast IoC `0.4.0` and later assign the instantiator directly, without reflection, including on subclasses in your project.

## ScrollRect or ListView?

| | UI Scroller | [UI Toolkit `ListView`](https://docs.unity3d.com/ScriptReference/UIElements.ListView.html) |
|---|---|---|
| UI framework | uGUI | UI Toolkit |
| Recycles rows | Yes | Yes |
| Item sizes | Fixed or measured per item | Fixed or dynamic height |
| Grids and bento | Built in | Not built in |
| Sections with headers | Built in | Build it yourself |
| Expandable slots | Built in | Build it yourself |
| Paging | Built in | Build it yourself |
| Package to install | Yes | No, it's part of Unity |

If your UI is in UI Toolkit, start with `ListView`.

## Limitations and roadmap

| Limitation | Status |
|---|---|
| Nested scrolling (a horizontal carousel inside a vertical list): the inner list takes every drag that starts on it | Planned for 1.0: [`nested-scrolling-plan.md`](Documentation~/nested-scrolling-plan.md) |
| One slot prefab per list (plus a header prefab in sectioned lists) | Slot types (several prefabs, one pool each) are planned after 1.0. Measured sizes and expanding already cover slots of different heights |
| No sticky headers | Planned as a separate component in the Extras sample, built on the public queries |
| Scrollbars aren't set up by default | Assigning one to the hidden `ScrollRect` fields (Debug inspector) is planned to be tested and documented |
| Snapping runs after a drag, not after the mouse wheel | Planned |
| No gamepad or keyboard navigation | When a console project needs it |
| Items must be reference types | By design |
| A looping list can't be a grid, bento, sectioned, paged or expandable | By design |
| Grids and bento don't expand | By design: opening one slot would change its whole row |
| A sectioned list doesn't page | By design |

## Troubleshooting

| Problem | Try this |
|---|---|
| `Content can't have layout components` | Remove the components it names from Content (and any `LayoutGroup` from the Viewport), or press **Remove Layout Components** in the inspector |
| `Slot does not implement ISlot` | Implement `ISlot<T>` (or `IExpandableSlot<T>`) on a component of the prefab's root |
| `Refresh aborted - not initialized` | Call `Initialize` (or `SetSource`) first |
| A recycled slot shows old content | Reset every visual when `Data` is set. Don't rely on `OnEnable`: a reused slot was never disabled |
| New items don't appear | Call `Refresh()` after changing the source |
| `Cannot initialize inactive GameObject` | Activate the list before calling `Initialize` |
| `Header Prefab is not assigned` | Assign a Header Prefab before the first `SetSource` |
| `Expand` does nothing | Expanding works in lists only, not in grids, bento or looping lists |
| Measured slots all keep the prefab's size, with a warning | Add a layout group to the slot's root (see [Item sizes](#item-sizes)) |
| Measured slots are too tall or collapse | Remove the `ContentSizeFitter` from the slot's root, and turn off **Child Force Expand > Height** |
| The bento list shows 1x1 tiles | The layout has errors: the inspector and the console list them |
| New pages don't appear | Check that the source is a `PagedSource<T>` (or another `IPagedSource<T>`), and subscribe to `PageFailed` to see load errors |

## FAQ

### How do I show thousands of items in Unity UI?

Use a recycling scroll view: subclass `Scroller<T>`, implement `ISlot<T>` on the slot prefab, and call `Initialize(new ListSource<T>(items))`. Only the visible slots exist. See [Quick start](#quick-start).

### Does it allocate while scrolling?

No. Tests check that scrolling 100,000 items and refreshing with the same count allocate 0 bytes, and the device runs show 0 B per frame. See [Performance](#performance).

### Does it support grids, bento and variable sizes?

Yes: **Grid**, **Auto Grid** and **Bento** in the inspector's Layout, and `DefaultExtent` of `-1` to size slots with Unity's layout. See [Layouts](#layouts) and [Item sizes](#item-sizes).

### Does it support infinite scroll?

Yes, with `PagedSource<T>` and an `IPageLoader<T>`. See [Paging](#paging).

### Does it work with IL2CPP and mobile?

Yes on Android: it's tested with IL2CPP (32-bit and 64-bit) on a Galaxy A02s and a Pixel 5. iOS and WebGL runs are planned. See [Compatibility](#compatibility).

### Why doesn't my slot update when I scroll?

Slots are reused: reset every visual in the `Data` setter, not only the ones that change.

### Is it production-ready?

Not yet. It's a preview (`1.0.0-preview.1`) by a single author, not yet used in a shipped game, so expect rough edges and possibly API changes before `1.0.0`. Bug reports are very welcome.

### Does it work with AI coding agents?

Yes. Copy [`Documentation~/agent-rules.md`](Documentation~/agent-rules.md) into your project's agent rules (`AGENTS.md`, `CLAUDE.md`, Cursor rules) so agents use the real API instead of guessing.

### Is it free for commercial use?

Yes, it's MIT licensed.

## Project status

This is a preview, version `1.0.0-preview.1`. Its API and behaviour may still change before `1.0.0`. It isn't used in a shipped game yet.

- Until `1.0.0`, a new preview can include breaking changes. From `1.0.0` on, only major versions will. Breaking changes are marked **Breaking** in the [changelog](CHANGELOG.md).
- Planned work: [Limitations and roadmap](#limitations-and-roadmap), the plans in [`Documentation~`](Documentation~), and the [issues](https://github.com/sgonzalezrozo/unity-ui-scroller/issues).

### Why I built it

I wrote UI Scroller after trying to fix other open-source scrollers I found on GitHub. I spent weeks on small issues before deciding to write my own and calculate its boundaries my way. After three very long days of personal time I had a first working version, and it has grown from there. I built and refined it in personal projects. In November 2026 I extracted it into this package, documented it and covered it with tests. Earlier history lives in a private repository.

## Contributing

Bug reports and pull requests are welcome. Please open an issue before starting a large change. See the [contributing guide](.github/CONTRIBUTING.md) and the [security policy](.github/SECURITY.md).

## Related packages

| Package | Purpose |
|---|---|
| [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc) | Code-generated dependency injection |
| [Screen System](https://github.com/sgonzalezrozo/unity-screen-system) | Declarative UI navigation |
| [UI Tweens](https://github.com/sgonzalezrozo/unity-ui-tweens) | ECS-based UI animation |
| [Enhanced Console](https://github.com/sgonzalezrozo/unity-enhanced-console) | Unity Editor log triage |
| [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) | Shared injection code, required by this package |

## License

Released under the [MIT License](LICENSE).
