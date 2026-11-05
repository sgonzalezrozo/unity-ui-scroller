# Recyclable Scroller

**Virtualized scrolling for large uGUI lists and grids, built to keep UI responsive under load.**

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Tested on Unity 6000.3](https://img.shields.io/badge/Unity-6000.3%20tested-black?logo=unity)](#compatibility)
[![Version 0.3.0](https://img.shields.io/badge/version-0.3.0-blue.svg)](CHANGELOG.md)

Recyclable Scroller reuses a pool of cells instead of creating one object per item. It extends Unity's `ScrollRect` and supports vertical and horizontal lists, grids, headers, dynamic sizing, expandable cells and pagination.

<!-- Demo GIF: Documentation~/images/demo.gif -->

## Why I built it

I wrote Recyclable Scroller after trying to fix other open-source scrollers I found on GitHub. I spent weeks on minor issues until I decided I could just make my own, calculating its boundaries my way. It seemed like a worthy challenge.

After three very long days of personal time, I had a first working version. It has since evolved into a robust recycling scroller.

I built and refined it on my own time in personal projects. It isn't used in production yet. In November 2026 I extracted it into this standalone package and documented it. Earlier history lives in a private repository.

## Is it right for your project?

**Use Recyclable Scroller if you:**

- Display large lists or grids in uGUI
- Need responsive UI on mobile hardware
- Want pagination and expandable cells in one component

**Consider something else if you:**

- Have a short list that doesn't need virtualization
- Build your UI with UI Toolkit. Its built-in `ListView` already virtualizes lists, and this package only includes UI Toolkit adapters, not a finished component.

## Features

- Cell recycling and pooling
- Vertical, horizontal and reversed layouts
- Fixed or dynamic grid columns
- Dynamic cell sizing with `ICellSizeCalculator<T>`
- Expandable cells
- Optional headers
- List, filtered and paginated data sources
- Scroll-to-item, start and end
- Item visibility checks

## Compatibility

| Requirement | Supported |
|---|---|
| Unity | Tested on Unity 6 (`6000.3.16f1`). The package declares `2022.3` as its minimum, but older versions aren't tested yet. |
| Scripting backend | Not verified yet |
| Platforms | Not verified yet |
| UI frameworks | uGUI. UI Toolkit has adapters only. |
| Enter Play Mode with domain reload disabled | Not verified yet. The package has no static reset for this mode, so keep domain reload on until you've tested it. |
| Dependencies | [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), uGUI, TextMeshPro |

## Installation

Git must be installed on your machine.

### 1. Install Tools Core

This package requires [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), the shared code that defines the injection interfaces used by Fast IoC, Screen System and Recyclable Scroller. Unity doesn't install Git dependencies automatically, so add it first.

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

TextMeshPro is included with uGUI in Unity 6. On Unity 2022.3, install it from the Package Manager if your project doesn't have it.

## Quick start

### 1. Create your data and cell

Items must be classes. The cell receives its item through `Data`:

```csharp
using TMPro;
using RecyclableScroller.Runtime;
using UnityEngine;

public class InventoryItem
{
    public string Name;
}

public class InventoryCell : MonoBehaviour, IRecyclableCell<InventoryItem>
{
    [SerializeField] private TMP_Text label;

    private InventoryItem data;

    public float DefaultCellSize => 80f;

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

Save the cell as a prefab, with a `RectTransform` on its root.

### 2. Create a scroller class

`RecyclableScrollRect<T>` is abstract, and Unity can't add an open generic component, so create a concrete class:

```csharp
using RecyclableScroller.Runtime;

public class InventoryScroller : RecyclableScrollRect<InventoryItem>
{
}
```

### 3. Set up the scene

1. Create a UI **Scroll View**.
2. Remove its `ScrollRect` component and add `InventoryScroller`.
3. Assign the same **Viewport** and **Content** references.
4. Assign your cell prefab to **Cell Prefab**.
5. Remove any `LayoutGroup` or `ContentSizeFitter` from **Content**.

<!-- Screenshot: Documentation~/images/hierarchy-and-inspector.png -->

### 4. Initialize it

```csharp
using System.Collections.Generic;
using RecyclableScroller.Runtime;
using UnityEngine;

public class InventoryScreen : MonoBehaviour
{
    [SerializeField] private InventoryScroller scroller;

    private void Start()
    {
        List<InventoryItem> items = new();
        for (int i = 0; i < 10000; i++)
            items.Add(new InventoryItem { Name = $"Item {i}" });

        scroller.Initialize(new ListDataSource<InventoryItem>(items));
    }
}
```

## Update data

Change the data source, then call `Refresh()`:

```csharp
source.AddItem(new InventoryItem { Name = "New item" });
scroller.Refresh();
```

`ListDataSource<T>` also has `AddItems`, `InsertItem`, `RemoveItem`, `RemoveItemAt`, `SetItems`, `ReplaceItems` and `Clear`. To switch to a different source, call `SetDataSource(newSource)`.

## Dynamic sizing

Implement `ICellSizeCalculator<T>` when cell sizes vary:

```csharp
using RecyclableScroller.Runtime;
using UnityEngine;

public class InventorySizeCalculator : ICellSizeCalculator<InventoryItem>
{
    public float CalculateCellSize(InventoryItem item, int index)
    {
        return item.Name.Length > 20 ? 120f : 80f;
    }

    public void SetItemPrefab(GameObject prefab)
    {
    }
}
```

```csharp
scroller.Initialize(source, new InventorySizeCalculator());
```

Use a size calculator when an item's size is known from its data. Use expandable cells when users can open and close a cell.

## Expandable cells

Implement `IExpandableRecyclableCell<T>` on the cell. The scroller sets `ScrollRect` and `CellDataIndex` each time it binds the cell:

```csharp
using RecyclableScroller.Runtime;
using TMPro;
using UnityEngine;

public class ExpandableInventoryCell : MonoBehaviour, IExpandableRecyclableCell<InventoryItem>
{
    private const float ExpandedSize = 240f;

    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject details;

    private InventoryItem data;
    private bool isExpanded;

    public float DefaultCellSize => 80f;
    public IRecyclableScrollRect ScrollRect { get; set; }
    public int CellDataIndex { get; set; }

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

    // Hook this up to the cell's button.
    public void Toggle()
    {
        if (IsExpanded)
            CollapseCell();
        else
            ScrollRect.ExpandCell(CellDataIndex, ExpandedSize);
    }

    public void CollapseCell()
    {
        ScrollRect.CellWantToCollapse(CellDataIndex);
    }
}
```

- Without a size, `ExpandCell(index)` uses the size calculator's expanded size, or twice the estimated cell size.
- To resize an open cell, call `UpdateExpandedCellSize(index, newSize)`.
- Turn on **Allow Multiple Expanded Cells** to keep more than one open.
- Expansion only works in single-column lists. It's ignored when **Grid Count** is above 1.

## Grids

Set **Grid Count** to the number of cells per line. To fit as many columns as the width allows, turn on **Use Dynamic Grid Count**, then set the cell size and the minimum and maximum counts. Set **Grid Cell Spacing** for the space between cells on a line.

## Headers

Assign a **Header Prefab**, then override `IsHeader` in your scroller class:

```csharp
using RecyclableScroller.Runtime;

public class SectionedScroller : RecyclableScrollRect<InventoryItem>
{
    protected override bool IsHeader(int dataIndex)
    {
        return DataSource.GetItem(dataIndex) is InventorySectionItem;
    }
}
```

`InventorySectionItem` here is your own subclass of `InventoryItem`. Headers are items in your data source, so the header prefab receives its data through `IRecyclableCell<T>`, like any other cell.

## Reversed lists

Turn on **Reverse** to start from the opposite end, as in a chat list. It works with vertical and horizontal lists.

## Filtered data

Wrap a source in `FilteredDataSource<T>`:

```csharp
ListDataSource<InventoryItem> items = new(allItems);
FilteredDataSource<InventoryItem> filtered = new(items, item => item.Name.Contains("Sword"));

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
using RecyclableScroller.Runtime;

public class RefreshingScroller : RecyclableScrollRect<InventoryItem>
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

The package also includes a `PullToRefreshUI` component for a status label and spinner, which you drive yourself.

## Pagination

Implement `IPaginatedDataProvider<T>` and wrap it in `PaginatedDataSource<T>`. Pages start at `0`:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using RecyclableScroller.Runtime;

public class InventoryPageProvider : IPaginatedDataProvider<InventoryItem>
{
    private const int PageSize = 50;
    private const int PageCount = 10;

    public Task<PaginationResult<InventoryItem>> LoadPageAsync(int page)
    {
        List<InventoryItem> items = new(PageSize);
        for (int i = 0; i < PageSize; i++)
            items.Add(new InventoryItem { Name = $"Item {page * PageSize + i}" });

        return Task.FromResult(new PaginationResult<InventoryItem>
        {
            Success = true,
            Items = items,
            CurrentPage = page,
            HasMore = page + 1 < PageCount
        });
    }
}
```

Load the first page, then refresh the scroller whenever a page arrives:

```csharp
using System;
using RecyclableScroller.Runtime;
using UnityEngine;

public class PaginatedInventoryScreen : MonoBehaviour
{
    [SerializeField] private InventoryScroller scroller;

    private PaginatedDataSource<InventoryItem> source;

    private async void Start()
    {
        try
        {
            source = new PaginatedDataSource<InventoryItem>(new InventoryPageProvider());

            await source.LoadNextPageAsync();

            scroller.Initialize(source);
            source.OnDataChanged = scroller.Refresh;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
```

The next page starts loading automatically when a cell within 10 items of the end is bound. To load at a different point, call `LoadNextPageAsync()` yourself from an `OnPaginationThresholdReached()` override, which runs at 96% of the scroll range.

## API reference

| API | Purpose |
|---|---|
| `Initialize(source)` | Starts the scroller with a data source |
| `Initialize(source, calculator)` | Starts the scroller with a data source and size calculator |
| `SetDataSource(source)` | Replaces the data source and refreshes |
| `Refresh()` | Rebuilds the list from the current data source |
| `Clear()` | Clears displayed cells and resets the scroll position |
| `ScrollToItem(int index)` | Jumps to an item |
| `ScrollToStart()` | Jumps to the start |
| `ScrollToEnd()` | Jumps to the end |
| `ExpandCell(int index, float newSize = -1)` | Expands a cell |
| `CellWantToCollapse(int index)` | Collapses a cell |
| `UpdateExpandedCellSize(int index, float newSize = -1)` | Resizes an expanded cell |
| `IsIndexExpanded(int index)` | Checks whether a cell is expanded |
| `IsItemVisible(int index)` | Checks whether an item is visible |
| `GetItemCount()` | Gets the item count |
| `IsInitialized` | Checks whether initialization has finished |
| `VisibleStartIndex`, `VisibleEndIndex` | Get the visible range |

## Inspector settings

| Setting | Purpose |
|---|---|
| Cell Prefab, Header Prefab | Prefabs used for cells and headers |
| Line Spacing, Grid Cell Spacing | Spacing between lines and grid cells |
| Padding Start, Padding End | Space before and after the content |
| Is Vertical, Reverse | Scroll direction and order |
| Grid Count | Number of cells per line |
| Use Dynamic Grid Count | Calculates grid columns from available space |
| Allow Multiple Expanded Cells | Keeps more than one cell open |
| Cells Per Frame | How many cells are created per frame |

## Dependency injection

The scroller works without a container, using standard `Instantiate`. With [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc), its instantiator can be injected. See [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) for details.

The `RecyclableScroller` and `RecyclableScroller.UGUI` assemblies expose their internals to `Assembly-CSharp`, so Fast IoC `0.4.0` and later assign `injectedInstantiator` directly, without reflection. This also covers subclasses in your project, such as `MyScroller : RecyclableScrollRect<T>`.

## Recommended patterns

- Keep cell sizes deterministic where possible.
- Reset all visual state when `Data` is set, because cells are reused.
- Use a data source instead of changing the list without calling `Refresh()`.
- Use the built-in `ListDataSource<T>` for simple in-memory lists.

## Performance

Benchmarks aren't published yet. Measure memory, frame time and startup at your own list sizes on your target device.

## ScrollRect or ListView?

| | Recyclable Scroller | [UI Toolkit `ListView`](https://docs.unity3d.com/ScriptReference/UIElements.ListView.html) |
|---|---|---|
| UI framework | uGUI | UI Toolkit |
| Recycles rows | Yes | Yes |
| Item sizes | Fixed or calculated per item | Fixed or dynamic height |
| Grids | Built in | Not built in |
| Expandable cells | Built in | Build it yourself |
| Pagination | Built in | Build it yourself |
| Package to install | Yes | No, it's part of Unity |

Use this package if your UI is in uGUI. If your UI is in UI Toolkit, start with `ListView`.

## Code layout

| Folder | Status |
|---|---|
| `Runtime` | The code the examples use, in the `RecyclableScroller.Runtime` namespace |
| `Core`, `UGUI`, `UIToolkit`, `Abstractions` | A framework-independent layout in progress. `Core` and `UGUI` still overlap with `Runtime`. |

Depend on `RecyclableScroller.Runtime` for now, because the other namespaces may change.

## Troubleshooting

| Problem | Try this |
|---|---|
| `Cell does not implement IRecyclableCell` | Implement `IRecyclableCell<T>` on the prefab's root component |
| `Refresh aborted - not initialized` | Call `Initialize` first |
| A recycled cell shows old content | Reset every visual field when `Data` is set |
| New items don't appear | Call `Refresh()` after changing the data source |
| `Cannot initialize inactive GameObject` | Activate the scroller before calling `Initialize` |
| `ExpandCell` does nothing | Set **Grid Count** to 1, because expansion doesn't work in grids |
| New pages don't appear | Assign `OnDataChanged` so the scroller refreshes after each page |

## Limitations

- Items must be reference types.
- Expandable cells don't work in grids.
- `FilteredDataSource<T>` walks the source list on each item lookup.
- There's no finished UI Toolkit component yet.
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
