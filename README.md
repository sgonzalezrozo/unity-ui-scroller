# Recyclable Scroller

**Virtualized scrolling for large uGUI lists and grids, built to keep UI responsive under load.**

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Recyclable Scroller reuses a pool of cells instead of creating one object per item. It extends Unity's `ScrollRect` and supports vertical and horizontal lists, grids, headers, dynamic sizing, expandable cells and pagination.

> **TODO:** Add a GIF of a very large list, ideally with the Profiler visible.

## Why I built it

Long lists are a common source of performance problems in mobile UI. Creating an object for every row costs memory and setup time, and on mobile games, smooth scrolling depends on keeping allocations under control.

> **TODO:** Replace this with your own story: which screen or project pushed you to build this, and what the existing options lacked.

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
| Unity | 2022.3 or newer. **TODO:** add tested versions. |
| Scripting backend | **TODO:** confirm Mono and IL2CPP |
| Platforms | **TODO:** list tested platforms |
| UI frameworks | uGUI. UI Toolkit has adapters only. |
| Enter Play Mode with domain reload disabled | **TODO:** confirm support |
| Dependencies | [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), uGUI, TextMeshPro |

## Installation

Git must be installed on your machine.

### 1. Install Tools Core

Unity doesn't install Git dependencies automatically, so add [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) first.

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
https://github.com/sgonzalezrozo/unity-recyclable-scroller.git#v0.2.0
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

> **TODO:** Add a screenshot, and confirm these scene steps in a clean project.

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

Implement `IExpandableRecyclableCell<T>` on the cell, then call:

```csharp
scroller.ExpandCell(index);
```

Pass a size to set it explicitly, such as `scroller.ExpandCell(index, 240f)`. Turn on **Allow Multiple Expanded Cells** to keep more than one open.

## Pagination

Implement `IPaginatedDataProvider<T>` and wrap it in `PaginatedDataSource<T>`. Subclasses can override `SupportsPagination()` and `OnPaginationThresholdReached()`.

> **TODO:** Add a tested pagination example.

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
| Cells Per Frame | How many cells are created per frame |

## Dependency injection

The scroller works without a container, using standard `Instantiate`. With [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc), its instantiator can be injected. See [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) for details.

## Recommended patterns

- Keep cell sizes deterministic where possible.
- Reset all visual state when `Data` is set, because cells are reused.
- Use a data source instead of changing the list without calling `Refresh()`.
- Use the built-in `ListDataSource<T>` for simple in-memory lists.

## Performance

> **TODO:** Add memory, frame time and initialization results at 1,000, 10,000 and 100,000 items. Include the device, Unity version, scripting backend, build type and a link to the benchmark project.

## Troubleshooting

| Problem | Try this |
|---|---|
| `Cell does not implement IRecyclableCell` | Implement `IRecyclableCell<T>` on the prefab's root component |
| `Refresh aborted - not initialized` | Call `Initialize` first |
| A recycled cell shows old content | Reset every visual field when `Data` is set |
| New items don't appear | Call `Refresh()` after changing the data source |

## Limitations

- Items must be reference types.
- There's no finished UI Toolkit component yet.
- `ScrollToItem` jumps to the item. It doesn't take alignment or animation options.

## Project status

This is a pre-release package at version `0.2.0`. Its API and behavior may change before `1.0.0`.

> **TODO:** Add a roadmap link and breaking-change policy.

## Contributing

Bug reports and pull requests are welcome. Please open an issue before starting a large change.

## Related packages

| Package | Purpose |
|---|---|
| [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc) | Code-generated dependency injection |
| [Screen System](https://github.com/sgonzalezrozo/unity-screen-system) | Declarative UI navigation |
| [UI Tweens](https://github.com/sgonzalezrozo/unity-ui-tweens) | ECS-based UI animation |
| [Enhanced Console](https://github.com/sgonzalezrozo/unity-enhanced-console) | Unity Editor log triage |
| [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) | Shared injection interfaces |

## License

Released under the [MIT License](LICENSE).
