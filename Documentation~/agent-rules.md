# UI Scroller rules for AI coding agents

Copy this file into your project's agent rules (for example `AGENTS.md`, `CLAUDE.md` or `.cursor/rules/ui-scroller.md`).

## How lists work here

- Long uGUI lists and grids use **UI Scroller** (`com.sgonzalezrozo.ui-scroller`, namespace `UIScroller`). Don't instantiate one slot per item, and don't use `LayoutGroup` or `ContentSizeFitter` on the scroller's **Content**.
- Items MUST be classes.

## Build a list

```csharp
using UIScroller;

// 1. The slot: a MonoBehaviour on the slot prefab.
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
            label.text = value.Name;   // reset every visual here: slots are reused
        }
    }
}

// 2. A concrete scroller. Scroller<T> is abstract and Unity can't add an open generic component.
public class InventoryScroller : Scroller<InventoryItem> { }

// 3. Start it.
scroller.Initialize(new ListSource<InventoryItem>(items));
```

The developer adds the concrete scroller to an empty UI object sized and anchored for the list; it builds its Viewport and Content. Then they assign **Slot Prefab** and pick **Direction** and **Layout**. Don't start from Unity's Scroll View, and never write the prefab or scene as text.

## Slot sizes

- `DefaultExtent > 0` is a fixed size (fastest). `DefaultExtent <= 0` (use `-1`) measures each slot with Unity's layout: text, localization and images decide its size.
- A measured slot's root needs a `VerticalLayoutGroup` (Control Child Size > Height on, Child Force Expand > Height off) and NO `ContentSizeFitter`. The list sets the root's width and height.
- Content changes after binding are picked up automatically. Call `InvalidateSize(index)` or `InvalidateAllSizes()` only when a size depends on something the slot can't notice. Width and language changes already invalidate.
- Expandable measured slots: call `Expand(index)` and `Collapse(index)` without a size; the slot turns its details on or off in `IsExpanded` and is measured. Sizes passed to `Expand` or `SetExpandedSize` only apply to fixed-size slots.

## Layouts

- **Grid** (`settings.slotsPerLine`) or **Auto Grid** (`settings.autoSlotsPerLine`, `targetSlotSize`): slots stretch to fill the line.
- **Bento**: tiles of different sizes from a **Layout JSON** (TextAsset) or `SetBentoLayout(BentoLayout.FromJson(json))`; row height from `cellAspect`. The layout decides every tile's size, never the item or the slot. A broken layout logs errors and uses 1x1 tiles. `GetTile(index)` reads a placement.
- **Carousel**: `loop` with `snap = SnapMode.Item` and `snapAlign = SnapAlign.Center`; `SnappedTo` reports the index. **Pager**: `snap = SnapMode.Page`.
- Grids and bento don't expand. A looping list is a plain list: no grid, bento, sections, paged source or expanding. A sectioned list doesn't page.
- Set settings from code before `Initialize`.

## Sections with headers

- Use `SectionedScroller<TItem, THeader>` (assign Slot Prefab AND Header Prefab) with a `SectionedSource<TItem, THeader>`: `source.AddSection(header, new ListSource<TItem>(items))`, then `list.SetSource(source)`. Never check `IsInitialized` first.
- The header slot implements `ISlot<THeader>`; the item slot `ISlot<TItem>`. No shared base class. A plain `Scroller<T>` has NO headers and no `IsHeader` override.
- `sectionSpacing` adds space before every header except the first.
- Empty sections are skipped automatically. List indexes count headers: use `source.FlatIndexOf(section, i)`. `ScrollToSection(section)` jumps to a header.

## Change data

- Change the data source, then call `scroller.Refresh()`. `ListSource<T>` has `AddItem`, `AddItems`, `InsertItem`, `RemoveItem`, `RemoveItemAt`, `SetItems`, `ReplaceItems` and `Clear`.
- Switch sources with `SetSource(source)`. For endless feeds use `PagedSource<T>` with an `IPageLoader<T>`: it loads pages by itself, so don't call `Refresh()` for pages. `FilteredSource<T>` filters another source; call `Invalidate()` after that source changes.
- Other members: `ScrollToItem(index, align, duration, offset)`, `ScrollToStart(duration)`, `ScrollToEnd(duration)`, `ForceStopScrolling()`, `SetBentoLayout(layout)`, `GetTile(index)`, `Expand(index)`, `Collapse(index)`, `InvalidateSize(index)`, `InvalidateAllSizes()`, `IsItemVisible(index)`, `GetItemCount()`, `IsInitialized`, `VisibleStartIndex`, `VisibleEndIndex`.

## Don't

- Don't refresh a slot in `OnEnable`. Pooled slots stay active, so `OnEnable` doesn't run when a slot is reused. Refresh from the `Data` setter (or an event it raises).
- Don't keep per-item state in a slot. Keep it in the item or a Model, because the slot will show another item later.
- Don't change the list without calling `Refresh()`.
- Don't invent members. If one isn't listed here or in the README, read the package source.
