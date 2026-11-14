# Slot List rules for AI coding agents

Copy this file into your project's agent rules (for example `AGENTS.md`, `CLAUDE.md` or `.cursor/rules/slot-list.md`).

## How lists work here

- Long uGUI lists and grids use **Slot List** (`com.sgonzalezrozo.slot-list`, namespace `SlotList`). Don't instantiate one slot per item, and don't use `LayoutGroup` or `ContentSizeFitter` on the scroller's **Content**.
- Items MUST be classes.

## Build a list

```csharp
using SlotList;

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

// 2. A concrete scroller. SlotList<T> is abstract and Unity can't add an open generic component.
public class InventoryScroller : SlotList<InventoryItem> { }

// 3. Start it.
scroller.Initialize(new ListSource<InventoryItem>(items));
```

The developer replaces a Scroll View's `ScrollRect` with the concrete scroller, assigns **Viewport**, **Content** and **Slot Prefab**. Never write the prefab or scene as text.

## Slot sizes

- `DefaultExtent > 0` is a fixed size (fastest). `DefaultExtent <= 0` (use `-1`) measures each slot with Unity's layout: text, localization and images decide its size.
- A measured slot's root needs a `VerticalLayoutGroup` (Control Child Size > Height on, Child Force Expand > Height off) and NO `ContentSizeFitter`. The list sets the root's width and height.
- Content changes after binding are picked up automatically. Call `InvalidateSize(index)` or `InvalidateAllSizes()` only when a size depends on something the slot can't notice. Width and language changes already invalidate.
- Expandable measured slots: call `Expand(index)` and `Collapse(index)` without a size; the slot turns its details on or off in `IsExpanded` and is measured. Sizes passed to `Expand` or `SetExpandedSize` only apply to fixed-size slots.

## Sections with headers

- Use `SectionedSlotList<TItem, THeader>` (assign Slot Prefab AND Header Prefab) with a `SectionedSource<TItem, THeader>`: `source.AddSection(header, new ListSource<TItem>(items))`, then `list.SetSource(source)`. Never check `IsInitialized` first.
- The header slot implements `ISlot<THeader>`; the item slot `ISlot<TItem>`. No shared base class. A plain `SlotList<T>` has NO headers and no `IsHeader` override.
- Empty sections are skipped automatically. List indexes count headers: use `source.FlatIndexOf(section, i)`. `ScrollToSection(section)` jumps to a header.

## Change data

- Change the data source, then call `scroller.Refresh()`. `ListSource<T>` has `AddItem`, `AddItems`, `InsertItem`, `RemoveItem`, `RemoveItemAt`, `SetItems`, `ReplaceItems` and `Clear`.
- Switch sources with `SetSource(source)`.
- Other members: `ScrollToItem(index, align, duration, offset)`, `ScrollToStart(duration)`, `ScrollToEnd(duration)`, `ForceStopScrolling()`, `Expand(index)`, `Collapse(index)`, `InvalidateSize(index)`, `InvalidateAllSizes()`, `IsItemVisible(index)`, `GetItemCount()`, `IsInitialized`, `VisibleStartIndex`, `VisibleEndIndex`.

## Don't

- Don't refresh a slot in `OnEnable`. Pooled slots stay active, so `OnEnable` doesn't run when a slot is reused. Refresh from the `Data` setter (or an event it raises).
- Don't keep per-item state in a slot. Keep it in the item or a Model, because the slot will show another item later.
- Don't change the list without calling `Refresh()`.
- Don't invent members. If one isn't listed here or in the README, read the package source.
