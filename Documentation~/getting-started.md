# Getting Started

[← Back to README](../README.md)

- [Requirements](#requirements)
- [Installation](#installation)
- [Samples](#samples)
- [Quick start](#quick-start)
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

    // 80 is a fixed height: the slot is always 80 px and its layout group is never measured.
    // Return -1 (0 or less) to size each slot from its layout instead. See "Item sizes".
    public float DefaultExtent => 80f;

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

> **Slots with a layout group:** if the slot's root has a `VerticalLayoutGroup` and should grow with its text, return `-1` from `DefaultExtent`. Any positive value is a fixed size: the slot keeps it and doesn't grow, even with a layout group. See [Building a slot that sizes itself](data-and-sizes.md#building-a-slot-that-sizes-itself).

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

   A list set up another way gets the Canvas and the raycaster when it starts. A list added from code at runtime builds nothing: create a Viewport (with a `RectMask2D`) and a Content, and assign `viewport` and `content` before `Initialize`, as `ExampleUI` in the Examples sample does. Without Content, `Initialize` logs an error and doesn't start.
3. Assign the slot prefab to **Slot Prefab**, and pick the **Direction** and **Layout**.

Don't start from Unity's **UI > Scroll View**: it adds scrollbars, a stencil `Mask` and an `Image` on the Viewport that this list doesn't use.

**Content must have no layout components** (`ContentSizeFitter`, `LayoutGroup`, `LayoutElement`, `AspectRatioFitter`), and the Viewport no `LayoutGroup`. The list sets Content's size itself. The editor removes such a component as soon as you add one, the inspector shows an error with a **Remove Layout Components** button, and `Initialize` refuses to start and names the component in the way.

<!-- Screenshot: images/hierarchy-and-inspector.png -->

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

## Next steps

- [Data & Sizes](data-and-sizes.md): updating items, fixed and measured sizes, filtering
- [Layouts](layouts.md): grids, bento, sections and expandable slots
- [Scrolling](scrolling.md): scroll to items, snapping, carousels, paging
