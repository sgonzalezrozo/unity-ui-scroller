# Getting Started

[← Back to README](../README.md)

- [Requirements](#requirements)
- [Installation](#installation)
- [Samples](#samples)
- [Your first list](#your-first-list)
- [Next steps](#next-steps)

---

## Requirements

| Requirement | Supported |
|---|---|
| Unity | Tested on Unity 2022.3 LTS, Unity 6000.0 LTS and Unity 6000.3. |
| Scripting backend | Mono (Editor) and IL2CPP (Android 32-bit ARMv7 and 64-bit ARM64, iOS ARM64, WebGL) |
| Platforms | macOS Editor; Android (Galaxy A02s, Pixel 2 XL, Pixel 5), iOS (iPhone 13 mini, iPhone Air) and WebGL (Chrome on macOS), each running the full device bench. Desktop players aren't tested yet. |
| UI framework | uGUI only |
| Enter Play Mode with domain reload disabled | Supported and checked: the runtime keeps no static state between plays (its only static field is an internal switch for tests). |
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

The package itself doesn't need TextMeshPro. The Quick Start and Extras samples use it: it's part of uGUI in Unity 6, and on Unity 2022.3 you install it from the Package Manager.

## Samples

Import them from the package's page in the Package Manager (**Samples** tab):

| Sample | What's in it |
|---|---|
| **Examples** | Eight scenes that build their UI in code: add one to an empty GameObject in an empty scene and press Play. `GridExample`, `BentoExample`, `BentoSectionsExample`, `SectionsExample`, `ExpandableExample`, `CarouselExample` (loop and snap), `NestedExample` (carousels inside a vertical list) and `PagingExample` (paging and pull to refresh). No TextMeshPro needed. |
| **Quick Start** | The scripts for [Simple list with fixed-size slots](examples.md#simple-list-with-fixed-size-slots). |
| **Extras** | A pull-to-refresh indicator and an empty-state panel. |

## Create an example from the Assets menu

Right-click a folder in the Project window, open **UI Scroller** and pick an example. Type a name (for example `Inventory`) and click **Create**. A folder with that name gets the example's scripts and prefabs:

```
Inventory/
├── InventoryItem.cs       the data
├── InventorySlot.cs       shows one item; reused as you scroll
├── InventoryScroller.cs   the list class
├── InventoryScreen.cs     makes up items and gives them to the list
├── InventorySlot.prefab   the slot template
└── Inventory.prefab       a Canvas with the screen and the list
```

Drop `Inventory.prefab` in a scene, or under a Canvas, and press Play. The scripts are written first and the prefabs are built once Unity has compiled them. Labels use Unity's built-in `Text`; swap them for TextMeshPro when you make the example your own.

| Menu item | What it builds |
|---|---|
| **Basic List** | Rows with an avatar, a title, a subtitle and an unread badge |
| **Grid** | A shop grid that fits as many columns as the width allows |
| **Dynamic Size** | A feed where each post is as tall as its text, measured with layout |
| **Dynamic Size on Click** | Rows measured with layout that grow to show their answer when tapped |
| **Expandable Row** | Fixed-height rows with a hitbox that asks the list to open them to a taller size, with stat bars and a button inside |
| **Bento** | Tiles of different sizes from a bento layout JSON, created next to the scripts and set on the list |
| **Bento with Paging** | A bento list fed page by page by a page loader, with a loading status |
| **Grid Carousel** | A two-row grid that scrolls sideways a page at a time, with page buttons |
| **Looping Carousel** | A carousel that loops both ways and centres a card that scales and fades with its position |
| **Sections** | Contacts under letter headers, with a `SectionedScroller` |
| **Nested Shelves** | A vertical list of shelves, each a horizontal carousel of cards |

With Screen System installed, its **Screen System** menu sits right above this one.

## Your first list

A list takes four pieces:

1. An **item** class with your data.
2. A **slot** prefab whose component implements `ISlot<T>`: it shows one item and is reused as you scroll.
3. A **list** class, `public class InventoryList : Scroller<InventoryItem> { }`, added to a sized UI object under your Canvas. It builds its own Viewport and Content.
4. One call with your items: `list.Initialize(new ListSource<InventoryItem>(items))`.

[Simple list with fixed-size slots](examples.md#simple-list-with-fixed-size-slots) walks through each step, with the scripts, the prefab, the Hierarchy and the Inspector.

## Next steps

- [Examples](examples.md): step-by-step setups for measured slots, grids, bento, sections, tabs, paging, search, expandable rows, carousels and nested lists
- [Data & Sizes](data-and-sizes.md): sources, updates, fixed and measured sizes
- [Layouts](layouts.md) and [Scrolling](scrolling.md): every option, in detail
