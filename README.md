# Slot List

*Formerly Recyclable Scroller (`com.sgonzalezrozo.recyclable-scroller`). Version 1.0 renames every type; see the changelog.*

**Virtualized scrolling for large uGUI lists and grids, built to keep UI responsive under load.**

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Tested on Unity 6000.3](https://img.shields.io/badge/Unity-6000.3%20tested-black?logo=unity)](#compatibility)
[![Version 1.0.0-preview.1](https://img.shields.io/badge/version-1.0.0--preview.1-blue.svg)](CHANGELOG.md)

Slot List is a free, open-source recycling (virtualized) scroll view for Unity 6 uGUI, for long lists and grids. It extends Unity's `ScrollRect`, creates only the slots that fit on screen and reuses them as you scroll, instead of one GameObject per item. It supports vertical and horizontal lists, grids, sections with headers, slot sizes measured with Unity's layout, expandable slots, filtering, pull to refresh and infinite scroll with pagination.

<!-- Demo GIF: Documentation~/images/demo.gif -->

## At a glance

Slot List shows lists of any length in uGUI with a handful of GameObjects. You give it a source of items and a slot prefab; it creates only the slots that fit on screen, plus a small buffer, and moves and rebinds them as you scroll. Its cost depends on what's on screen, never on how many items there are: 300 or 100,000 cost the same.

- **Set up in seconds:** add your list component to a sized UI object. It builds the Viewport (with a `RectMask2D` and its own `Canvas`) and Content. The inspector shows only direction, layout (List, Grid, Auto Grid), spacing and padding. Content can't have layout components; the editor and the list enforce it.
- **Sizes the way you build slots:**
  - a fixed size from the slot (`DefaultExtent`);
  - or the slot's own Unity layout, measured once and cached;
  - or both: fixed when closed, measured when expanded.

  The list reads the layout's preferred size itself, so slots need no `ContentSizeFitter`. Late text, such as localized strings or downloaded content, is followed without code.
- **Expandable slots**, one or several open at once. An opened slot is measured in Unity's own layout pass and grows on the next frame. What's on screen never jumps.
- **Sections with headers, grids, filtering, pull to refresh and paging** (the list loads pages by itself).
- **Read-only events and queries:** opened, closed, rebuilt, Content size changed, item offsets and sizes.
- **Built for phones.** Scrolling allocates nothing, and pooled slots stay active out of view, so reusing them is cheap. Scrolling only rebatches the list itself, not the rest of the screen.

Measured on a low-end phone (Galaxy A02s) and a mid-range one (Pixel 5) with a real list (TMP text, 100 px rows):

| | Galaxy A02s (60 Hz) | Pixel 5 (90 Hz) |
|---|---|---|
| The list's own work per frame, scrolling 100,000 items | 0.28 ms | 0.12 ms |
| Whole frame CPU while scrolling, the app included | 7.6 of 16.7 ms | 2.9 of 11.1 ms |
| Fast fling (16,000 px/s), whole frame | 8.3 ms | 4.0 ms |
| Extra memory allocated while scrolling | 0 B | 0 B |
| Opening slots while scrolling (TMP text, up to six sentences), slowest frame | 25 ms, no missed frame at 4 opens a second | 8.9 ms |
| Open slots scrolled back into view | cached, nothing measured | cached, nothing measured |
| Slowdown or memory growth over 2 minutes | none | none |

300 or 100,000 items cost the same. The details and every run are in [`Documentation~/benchmarks.md`](Documentation~/benchmarks.md).

## Why I built it

I wrote Slot List after trying to fix other open-source scrollers I found on GitHub. I spent weeks on minor issues until I decided I could just make my own, calculating its boundaries my way. It seemed like a worthy challenge.

After three very long days of personal time, I had a first working version. It has since evolved into a robust recycling scroller.

I built and refined it on my own time in personal projects. It isn't used in production yet. In November 2026 I extracted it into this standalone package and documented it. Earlier history lives in a private repository.

## Is it right for your project?

**Use Slot List if you:**

- Display large lists or grids in uGUI
- Need responsive UI on mobile hardware
- Want pagination and expandable slots in one component

**Consider something else if you:**

- Have a short list that doesn't need virtualization
- Build your UI with UI Toolkit. Its built-in `ListView` already virtualizes lists. This package is for uGUI only.

## Features

- Slot recycling and pooling
- Vertical, horizontal and reversed layouts
- Fixed or dynamic grid columns
- Fixed sizes, or sizes measured with Unity's layout (text, localization, images)
- Expandable slots
- Sections with headers (`SectionedSlotList`)
- List, filtered and paginated data sources
- Scroll-to-item, start and end
- Item visibility checks

## Compatibility

| Requirement | Supported |
|---|---|
| Unity | Tested on Unity 6 (`6000.3.16f1`). The package declares `2022.3` as its minimum, but older versions aren't tested yet. |
| Scripting backend | Not verified yet |
| Platforms | Not verified yet |
| UI frameworks | uGUI only |
| Enter Play Mode with domain reload disabled | Not verified yet. The package has no static reset for this mode, so keep domain reload on until you've tested it. |
| Dependencies | [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), uGUI. The samples also use TextMeshPro. |

## Installation

Git must be installed on your machine.

### 1. Install Tools Core

This package requires [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), the shared code that defines the injection interfaces used by Fast IoC, Screen System and Slot List. Unity doesn't install Git dependencies automatically, so add it first.

1. In Unity, open **Window > Package Manager**.
2. Select **+ > Add package from git URL**.
3. Enter:

```text
https://github.com/sgonzalezrozo/unity-tools-core.git
```

### 2. Install this package

1. In Unity, open **Window > Package Manager**.
2. Select **+ > Add package from git URL**.
3. Enter:

```text
https://github.com/sgonzalezrozo/unity-recyclable-scroller.git
```

To pin a release, add its tag:

```text
https://github.com/sgonzalezrozo/unity-recyclable-scroller.git#v0.3.0
```

The package itself doesn't need TextMeshPro. The samples use it: it's included with uGUI in Unity 6, and on Unity 2022.3 you install it from the Package Manager.

## Quick start

### 1. Create your data and slot

Items must be classes. The slot receives its item through `Data`:

```csharp
using TMPro;
using SlotList;
using UnityEngine;

public class InventoryItem
{
    public string Name;
}

public class InventorySlot : MonoBehaviour, ISlot<InventoryItem>
{
    [SerializeField] private TMP_Text label;

    private InventoryItem data;

    public float DefaultExtent => 80f;

    public InventoryItem Data
    {
        get => data;
        set
        {
            data = value;
            label.text = value.Name;
        }
    }
}
```

Save the slot as a prefab, with a `RectTransform` on its root.

### 2. Create a scroller class

`SlotList<T>` is abstract, and Unity can't add an open generic component, so create a concrete class:

```csharp
using SlotList;

public class InventoryScroller : SlotList<InventoryItem>
{
}
```

### 3. Set up the scene

1. Create an empty UI GameObject under your Canvas, and give it the size and anchors the list should have on screen.
2. Add `InventoryScroller`. It builds a stretched **Viewport** and a **Content** inside it, and wires them up. There are no scrollbars. The Viewport gets:
   - a `RectMask2D`, which clips and culls slots outside it;
   - a nested `Canvas`, so scrolling rebuilds only the list's batches, not the rest of the screen's;
   - a `GraphicRaycaster`, so buttons in slots get clicks.

   A list set up another way gets the Canvas and raycaster when it starts.
3. Assign your slot prefab to **Slot Prefab**, and pick the **Direction** and **Layout**.

Don't start from Unity's **UI > Scroll View**: it adds scrollbars, a stencil `Mask` and an `Image` on the Viewport that this list doesn't use.

**Content must have no layout components** (`ContentSizeFitter`, `LayoutGroup`, `LayoutElement`, `AspectRatioFitter`), and the Viewport no `LayoutGroup`. The list sets Content's size itself and changes it at runtime, when items are measured, expanded or collapsed. The editor removes such a component as soon as you add one, the inspector shows an error with a **Remove Layout Components** button, and `Initialize` refuses to start and logs which component is in the way.

<!-- Screenshot: Documentation~/images/hierarchy-and-inspector.png -->

### 4. Initialize it

```csharp
using System.Collections.Generic;
using SlotList;
using UnityEngine;

public class InventoryScreen : MonoBehaviour
{
    [SerializeField] private InventoryScroller scroller;

    private void Start()
    {
        List<InventoryItem> items = new();
        for (int i = 0; i < 10000; i++)
            items.Add(new InventoryItem { Name = $"Item {i}" });

        scroller.Initialize(new ListSource<InventoryItem>(items));
    }
}
```

## Update data

Change the data source, then call `Refresh()`:

```csharp
source.AddItem(new InventoryItem { Name = "New item" });
scroller.Refresh();
```

`ListSource<T>` also has `AddItems`, `InsertItem`, `RemoveItem`, `RemoveItemAt`, `SetItems`, `ReplaceItems` and `Clear`. To switch to a different source, call `SetSource(newSource)`.

## Item sizes

Each slot's script decides how big it is, through `DefaultExtent` (the height in a vertical list, the width in a horizontal one):

- **A positive number is a fixed size.** Every item uses it and nothing is measured. This is the fastest option.
- **0 or less (for example `-1`) means "measure with Unity's layout".** Text, localized strings and images decide each slot's size.

```csharp
public float DefaultExtent => -1f; // measured with layout
```

### Building a slot that sizes itself

- On the prefab's root, add a `VerticalLayoutGroup` (a `HorizontalLayoutGroup` for horizontal lists) with **Control Child Size > Height** on (**Width** for horizontal) and **Child Force Expand > Height** off. Add padding as you like, and a `LayoutElement` with a minimum height if you want one.
- Inside it, use text with word wrapping on, images with a `LayoutElement` or an `AspectRatioFitter`, and nested layout groups.
- **Don't put a `ContentSizeFitter` on the root.** The list sets the root's size, and a fitter would fight it. Fitters on children are fine.
- The list always sets the width: the full width in a list, the column width in a grid. That's what makes text wrap correctly.

If the root reports no preferred size (no layout group), the slot keeps the prefab's own size and the list logs one warning.

### How measuring works

- A slot is measured when it's shown. Items not measured yet use the average of the measured ones, so the scrollbar gets more accurate as you scroll.
- In a grid, a row is as tall as its tallest slot.
- **Sizes are cached.** An item is measured the first time it's shown; when it scrolls back in, it reuses that size without a layout rebuild. `Refresh()` measures the items on screen again.
- **If an item's content changed while it was off screen** (something unlocked, a new state), the watcher notices when it's shown and resizes it one frame later. To get the right size from the first frame, call `InvalidateSize(index)` when you change it.
- **Changes after binding are picked up for you.** A localized string that arrives late, an image that finishes loading or a child turned on or off resizes the slot one frame later. The list adds a `SlotSizeWatcher` to measured slots for this; you never add it yourself.
- **What you're looking at stays still.** Items measured for the first time while you scroll up grow upwards, and an item whose content changed keeps its top and pushes the ones below. This also holds mid-drag, without jumps or extra speed.

### Measuring again

When a size depends on something the slot can't notice by itself, ask for a new measurement:

```csharp
scroller.InvalidateSize(index); // this item
scroller.InvalidateAllSizes();  // every item
```

Items on screen are measured at once and the others when they're shown. `InvalidateAllSizes()` is called for you when the list's width changes (text rewraps) and, if Unity Localization is installed, when the language changes. Both do nothing for fixed sizes.

## Expandable slots

Implement `IExpandableSlot<T>` on the slot. The list sets `Owner` and `ItemIndex` each time it binds the slot, and sets `IsExpanded` after `Data`:

```csharp
using SlotList;
using TMPro;
using UnityEngine;

public class ExpandableInventorySlot : MonoBehaviour, IExpandableSlot<InventoryItem>
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject details;

    private InventoryItem data;
    private bool isExpanded;

    public float DefaultExtent => -1f; // measured: the details decide the expanded size
    public ISlotList Owner { get; set; }
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

    // Called when the list collapses this slot because another one expanded.
    public void OnCollapsed()
        => IsExpanded = false;
}
```

- **Measured slots** (`DefaultExtent` of 0 or less) size themselves: `Expand` and `Collapse` only change the state, and the slot is measured with its details on or off. If the details change size while open, the slot follows. A size given to `Expand(index, size)` or `SetExpandedSize` is ignored, with a warning.
- **Fixed-size slots** (`DefaultExtent` above 0), the usual case: collapsed, the slot keeps its fixed size and nothing is measured while scrolling. Expanded:
  - `Expand(index)` without a size: the expanded slot is measured with its details on, in Unity's own layout pass, so it grows on the frame after the tap (what's above it stays still). It follows its details if they change size while open, including text that arrives late (localization). Only that item is measured. Collapsing goes back to the fixed size. The root needs a layout group (or a `LayoutElement`) that reports the expanded height; without one it expands to twice its size.
  - `Expand(index, 240f)` with a size: a fixed expanded size. `SetExpandedSize(index, newSize)` resizes an open slot, and turns a measured one into a fixed one.
- Expanding or collapsing never moves what's on screen above the item.
- Turn on **Allow Multiple Expanded** to keep more than one open.
- Expansion only works in lists. It's ignored in a grid.

## Grids

In the inspector, set **Layout** to **Grid** and choose the number of columns (rows in a horizontal list), or to **Auto Grid** to fit as many as the width allows, between a minimum and a maximum, from a target slot size. Slots are stretched to fill the line. The spacing between slots on a line is the **Column Spacing** (**Row Spacing** in a horizontal list).

## Sections and headers

For a list split into sections, each with a header, use `SectionedSlotList<TItem, THeader>` and a `SectionedSource<TItem, THeader>`. Headers and items are different types, with their own prefabs:

```csharp
using SlotList;

public class InventorySection
{
    public string Title;
}

// The header slot: a MonoBehaviour on the Header Prefab.
public class InventoryHeaderSlot : MonoBehaviour, ISlot<InventorySection>
{
    [SerializeField] private TMP_Text title;

    private InventorySection data;

    public float DefaultExtent => 48f;

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

// The list.
public class InventorySectionsList : SectionedSlotList<InventoryItem, InventorySection>
{
}
```

Assign both **Slot Prefab** (implementing `ISlot<InventoryItem>`) and **Header Prefab** (implementing `ISlot<InventorySection>`), then build the sections:

```csharp
SectionedSource<InventoryItem, InventorySection> source = new();
source.AddSection(new InventorySection { Title = "Weapons" }, new ListSource<InventoryItem>(weapons));
source.AddSection(new InventorySection { Title = "Potions" }, new ListSource<InventoryItem>(potions));

list.SetSource(source); // starts the list the first time, replaces the sections after that
```

- **Empty sections are skipped**, header included. Set `source.SkipEmptySections = false` to show the header of an empty section.
- `InsertSection`, `RemoveSection` and `ClearSections` change the sections. After changing an inner source (adding to a section's `ListSource`, for example), call `list.Refresh()`, as with any list.
- `list.ScrollToSection(index)` jumps to a section's header (or the next section shown, if that one is empty).
- **A header takes a whole row in a grid**, and each section starts a new row.
- Headers follow the same size rule as slots: a positive `DefaultExtent` is fixed, 0 or less is measured with layout. Headers never change the average size used for items not measured yet.
- Indexes in the list's API (`ScrollToItem`, `Expand`, `IsItemVisible`, `VisibleStartIndex`...) count headers. Use `source.FlatIndexOf(section, indexInSection)` to find an item's index, and `source.Locate(index)` for the other way round.
- To build the source elsewhere (a screen's Model, for example), implement `ISectionedSourceProvider<TItem, THeader>` and call `list.SetSource(provider)`.
- `SectionedSlotList` doesn't page: a sectioned source isn't a paged source.

A plain `SlotList<T>` has no headers.

## Reversed lists

Turn on **Reverse** to start from the opposite end, as in a chat list. It works with vertical and horizontal lists.

## Filtered data

Wrap a source in `FilteredSource<T>`:

```csharp
ListSource<InventoryItem> items = new(allItems);
FilteredSource<InventoryItem> filtered = new(items, item => item.Name.Contains("Sword"));

scroller.Initialize(filtered);
```

To change the filter, update the predicate, then refresh:

```csharp
filtered.SetPredicate(item => item.Name.StartsWith("A"));
scroller.Refresh();

filtered.ClearPredicate();
scroller.Refresh();
```

Each `GetItem` call walks the source list, so filtering gets slower as the list grows. For very large lists, build a filtered list yourself and pass it to `SetItems`.

## Pull to refresh

Override `OnPullToRefreshDetected()`. It runs when the user releases a drag more than 75 pixels past the start of the list:

```csharp
using System.Threading.Tasks;
using SlotList;

public class RefreshingScroller : SlotList<InventoryItem>
{
    protected override async void OnPullToRefreshDetected()
    {
        await ReloadAsync();
        Refresh();
    }

    private Task ReloadAsync()
    {
        // Update your data source here.
        return Task.CompletedTask;
    }
}
```

The **Extras** sample has a `PullIndicator` component for a status label and spinner, which you drive yourself, and an `EmptyStatePanel` that shows a message when the list is empty.

## Pagination

Implement `IPageLoader<T>` and wrap it in `PagedSource<T>`. Pages start at `0`. Throw to report a failure:

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SlotList;

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

Give the list the source. It loads the first page by itself, and the next one whenever fewer than 10 items are left after the last visible one. New pages are added without a rebuild, so the scroll position and expanded items stay:

```csharp
scroller.SetSource(new PagedSource<InventoryItem>(new InventoryPageLoader()));

scroller.PageLoaded += count => Debug.Log($"{count} more items");
scroller.PageFailed += exception => Debug.LogWarning(exception.Message);
scroller.ReachedEnd += () => Debug.Log("No more pages");
```

Only one page loads at a time. Changing the source or destroying the list cancels a load in flight (the loader receives a cancelled token). To start again, for example on pull to refresh, call `source.Reset()`: the list goes back to the start and reloads page 0.

### Performance tips

- **Turn off Raycast Target** on graphics in the slot prefab that don't need clicks (backgrounds, icons, labels). Every raycast target is checked on every touch.
- **Keep slot hierarchies shallow** when they're measured with layout: nested layout groups make each measurement slower. Measured sizes are cached, so this mostly affects the first time an item is shown.
- Benchmarks: [`Documentation~/benchmarks.md`](Documentation~/benchmarks.md).

## API reference

| API | Purpose |
|---|---|
| `Initialize(source)` | Starts the scroller with a data source |
| `SetSource(source)` | Replaces the data source and refreshes |
| `Refresh()` | Rebuilds the list from the current data source |
| `Clear()` | Clears displayed slots and resets the scroll position |
| `ScrollToItem(int index)` | Jumps to an item |
| `ScrollToStart()` | Jumps to the start |
| `ScrollToEnd()` | Jumps to the end |
| `Expand(int index, float newSize = -1)` | Expands a slot (the size is used for fixed-size slots only) |
| `Collapse(int index)` | Collapses a slot |
| `SetExpandedSize(int index, float newSize = -1)` | Resizes an expanded slot |
| `InvalidateSize(int index)` | Measures an item again (measured slots) |
| `InvalidateAllSizes()` | Measures every item again (measured slots) |
| `IsExpanded(int index)` | Checks whether a slot is expanded |
| `IsItemVisible(int index)` | Checks whether an item is visible |
| `GetItemCount()` | Gets the item count |
| `IsInitialized` | Checks whether initialization has finished |
| `VisibleStartIndex`, `VisibleEndIndex` | Get the visible range |
| `GetItemOffset(int index)` | Where the item's line starts along the scroll axis, from the start of Content (-1 if unknown) |
| `GetItemSize(int index)` | The size of the item's line along the scroll axis (in a grid, its tallest slot; -1 if unknown) |
| `ViewportSize`, `ContentSize` | The viewport's and Content's size along the scroll axis |
| `ExpandedCount` | How many items are expanded |

Events, all read-only notifications:

| Event | Raised when |
|---|---|
| `ItemExpanded(int index)` | An item was expanded |
| `ItemCollapsed(int index)` | An item was collapsed, including one closed because another opened (raised before that one's `ItemExpanded`) |
| `Rebuilt` | The list rebuilt its lines: `Refresh`, `SetSource`, or a change of slots per line |
| `ContentSizeChanged(float size)` | The list changed Content's size along the scroll axis (items measured, opened or closed, pages, Refresh). Not raised by scrolling |
| `PageRequested`, `PageLoaded(int)`, `PageFailed(Exception)`, `ReachedEnd` | Paging (see Pagination) |
| `OnInitialized` | Initialization finished. It fires inside `Initialize` (or the first `SetSource`), with the first screen already bound, so subscribe before calling it. |

`SectionedSlotList<TItem, THeader>` has the same API, with these differences:

| API | Purpose |
|---|---|
| `SetSource(SectionedSource<TItem, THeader> source)` | Starts the list or replaces its sections |
| `SetSource(ISectionedSourceProvider<TItem, THeader> provider)` | The same, with the provider's source |
| `ScrollToSection(int section)` | Jumps to a section's header |
| `Source` | The sections shown |
| `HeaderPrefab` | The header prefab (set it before the first `SetSource`) |

It has no `Initialize`: `SetSource` starts it.

## Inspector settings

Labels follow the direction: "Padding Top" in a top-to-bottom list, "Padding Left" in a left-to-right one, and so on. The `ScrollRect` settings underneath are hidden and driven by these.

| Setting | Purpose |
|---|---|
| Slot Prefab | The prefab used for slots |
| Header Prefab | The prefab used for headers (sectioned lists only) |
| Direction | Top to bottom, bottom to top, left to right or right to left |
| Layout | List, Grid (a fixed count per line) or Auto Grid (as many as fit) |
| Columns / Rows, Target size, Min, Max | Grid settings, shown for the chosen layout |
| Row / Column Spacing | Space between lines, and between slots on a line in a grid |
| Padding (start and end) | Space before the first line and after the last |
| Allow Multiple Expanded | Keeps more than one slot open |
| Reset Scroll On Enable | Goes back to the start when the list is enabled again |
| Override Pool Buffer, Pool Buffer | How many extra slots to keep ready beyond the visible ones |

## Dependency injection

The scroller works without a container, using standard `Instantiate`. With [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc), its instantiator can be injected. See [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) for details.

The `SlotList` assembly exposes its internals to `Assembly-CSharp`, so Fast IoC `0.4.0` and later assign `injectedInstantiator` directly, without reflection. This also covers subclasses in your project, such as `MyScroller : SlotList<T>`.

## Recommended patterns

- Use a fixed `DefaultExtent` when every slot has the same size: nothing is measured.
- Reset all visual state when `Data` is set, because slots are reused.
- **Refresh the slot from the `Data` setter, not from `OnEnable`.** Slots waiting in the pool stay active (parked out of view, where the mask culls them), so a reused slot's `OnEnable` doesn't run again. Waking a slot's graphics on every reuse cost about three quarters of the list's time on a low-end phone. If a separate view component draws the slot, have the `Data` setter raise an event the view listens to.
- Use a data source instead of changing the list without calling `Refresh()`.
- Use the built-in `ListSource<T>` for simple in-memory lists.

## Performance

- Scrolling allocates nothing, even with 100,000 items, and its work depends on what's on screen, not on the item count.
- Moving slots rebuilds only the list's own batches: the Viewport is a nested Canvas.
- The first screen shows in the first frame. The pool warms up two slots per frame. Disabling and enabling a list rebinds nothing.
- Rebuilds and new pages only create what's new. Measured sizes are cached.

- Opening an expandable slot measures only that slot, in Unity's own layout pass, so its text is laid out once. It grows on the frame after the tap.

Tests hold these as counts and allocations (0 bytes while scrolling 100,000 items, no slot leaving Content, work that follows the slots on screen). On devices, see the table in [At a glance](#at-a-glance) and the full runs in [`Documentation~/benchmarks.md`](Documentation~/benchmarks.md):
- **Dynamic sizes:** opening an enemy with up to six sentences of TMP text peaked at 25–29 ms on the A02s and under 12 ms on the Pixel 5. Most of that is TMP laying out and building the text, and growing its buffers.
- **TMP tip:** if your slots show long or new text when they open, pre-fill your TMP font asset with the characters you use, or use a static atlas.

Measure at your own list sizes on your target device.

## ScrollRect or ListView?

| | Slot List | [UI Toolkit `ListView`](https://docs.unity3d.com/ScriptReference/UIElements.ListView.html) |
|---|---|---|
| UI framework | uGUI | UI Toolkit |
| Recycles rows | Yes | Yes |
| Item sizes | Fixed or calculated per item | Fixed or dynamic height |
| Grids | Built in | Not built in |
| Expandable slots | Built in | Build it yourself |
| Pagination | Built in | Build it yourself |
| Package to install | Yes | No, it's part of Unity |

Use this package if your UI is in uGUI. If your UI is in UI Toolkit, start with `ListView`.

## Code layout

| Folder | Assembly | Contents |
|---|---|---|
| `Runtime` | `SlotList` | Everything you use, in the `SlotList` namespace |
| `Editor` | `SlotList.Editor` | The inspector |
| `Tests` | `SlotList.Tests.Editor`, `SlotList.Tests.PlayMode` | EditMode and PlayMode tests. They run when the package is listed in `testables` |

## Use it with AI coding agents

Coding agents write C# files, and this package is newer than their training data. Give them its rules so they use the real API:

1. Copy [`Documentation~/agent-rules.md`](Documentation~/agent-rules.md) into your project's agent rules, for example `AGENTS.md`, `CLAUDE.md` or `.cursor/rules/slot-list.md`.
2. For a whole Unity architecture with a verify loop, add [Unity Architecture Agents](https://github.com/sgonzalezrozo/unity-architecture-agents). It detects this package and adapts its code.

There are no MCP tools for Slot List yet. The rules file works with any agent that reads project files.

## FAQ

### How do I show a list of thousands of items in Unity UI?

Use a virtualized scroll view. Subclass `SlotList<T>`, implement `ISlot<T>` on the slot prefab, then call `Initialize(new ListSource<T>(items))`. Only visible slots exist. See [Quick start](#quick-start).

### ScrollRect or UI Toolkit ListView?

UI Toolkit's `ListView` already virtualizes. Slot List is for uGUI projects. See [ScrollRect or ListView?](#scrollrect-or-listview).

### Does it support grids and variable slot sizes?

Yes. Choose **Grid** in the inspector's Layout for grids, and return `-1` from `DefaultExtent` to size slots with Unity's layout. See [Grids](#grids) and [Item sizes](#item-sizes).

### Does it support infinite scroll and pagination?

Yes, through a paginated data provider. See [Pagination](#pagination).

### Does it work with Unity 6?

Yes. It's tested on Unity `6000.3`. The package declares `2022.3` as its minimum, but older versions aren't tested yet.

### Does it work with IL2CPP and mobile?

Not verified yet. See [Compatibility](#compatibility).

### Why doesn't my slot update correctly when I scroll?

Slots are reused. Reset every visual in the `Data` setter, not only the ones that change.

### Does it work with AI coding agents?

It ships a rules file you copy into your project, so agents use its real API instead of guessing. It hasn't been tested with agents beyond the author's own projects. See [Use it with AI coding agents](#use-it-with-ai-coding-agents).

### How do I optimize a ScrollRect with many items?

Don't create one GameObject per item, and remove `LayoutGroup` and `ContentSizeFitter` from the content. A recycling scroll view keeps only the visible slots and reuses them. Benchmarks aren't published yet, so measure at your own list sizes. See [Performance](#performance).

### How do I install it?

In Unity, open **Window > Package Manager**, select **+ > Add package from git URL** and enter `https://github.com/sgonzalezrozo/unity-recyclable-scroller.git`. Install [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) first, because Unity doesn't install Git dependencies automatically. See [Installation](#installation).

### Is it production-ready? Does anyone use it?

Not yet. Slot List is a pre-release package (`0.x`) by a single author. It isn't used in a shipped game, and it has no other users yet, so expect rough edges and API changes before `1.0.0`. Bug reports are very welcome. See [Project status](#project-status).

### Is it free for commercial use?

Yes. It's MIT licensed.

## Troubleshooting

| Problem | Try this |
|---|---|
| `Content can't have layout components` | Remove the components it names from Content (and any `LayoutGroup` from the Viewport), or press **Remove Layout Components** in the list's inspector |
| `Slot does not implement ISlot` | Implement `ISlot<T>` on the prefab's root component |
| `Refresh aborted - not initialized` | Call `Initialize` first |
| A recycled slot shows old content | Reset every visual field when `Data` is set. Don't rely on `OnEnable`: a reused slot was never disabled |
| New items don't appear | Call `Refresh()` after changing the data source |
| `Cannot initialize inactive GameObject` | Activate the scroller before calling `Initialize` |
| `Header Prefab is not assigned` | Assign a Header Prefab to the sectioned list before calling `SetSource` |
| `Expand` does nothing | Set **Layout** to **List**, because expansion doesn't work in grids |
| Measured slots all keep the prefab's size, with a warning | Add a layout group to the slot's root (see [Item sizes](#item-sizes)) |
| Measured slots are too tall or collapse | Remove the `ContentSizeFitter` from the slot's root, and check **Child Force Expand > Height** is off |
| New pages don't appear | Check that the source is a `PagedSource<T>` (or another `IPagedSource<T>`), and subscribe to `PageFailed` to see load errors |

## Limitations

- Items must be reference types.
- Expandable slots don't work in grids.
- `FilteredSource<T>` walks the source list on each item lookup.
- `ScrollToItem` jumps to the item. It doesn't take alignment or animation options.

## Project status

This is a pre-release package at version `0.3.0`. Its API and behavior may change before `1.0.0`. Not yet used in a shipped game.

Until `1.0.0`, minor versions (`0.x.0`) can include breaking changes, and patch versions (`0.x.y`) don't. Breaking changes are marked **Breaking** in the [changelog](CHANGELOG.md).

There's no public roadmap yet. Known gaps are listed under [Limitations](#limitations), and planned work is tracked in [issues](https://github.com/sgonzalezrozo/unity-recyclable-scroller/issues).

## Contributing

Bug reports and pull requests are welcome. Please open an issue before starting a large change. See the [contributing guide](.github/CONTRIBUTING.md) and [security policy](.github/SECURITY.md).

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
