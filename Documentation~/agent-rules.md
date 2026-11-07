# Recyclable Scroller rules for AI coding agents

Copy this file into your project's agent rules (for example `AGENTS.md`, `CLAUDE.md` or `.cursor/rules/recyclable-scroller.md`).

## How lists work here

- Long uGUI lists and grids use **Recyclable Scroller** (`com.sgonzalezrozo.recyclable-scroller`, namespace `RecyclableScroller.Runtime`). Don't instantiate one cell per item, and don't use `LayoutGroup` or `ContentSizeFitter` on the scroller's **Content**.
- Items MUST be classes.

## Build a list

```csharp
using RecyclableScroller.Runtime;

// 1. The cell: a MonoBehaviour on the cell prefab.
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
            label.text = value.Name;   // reset every visual here: cells are reused
        }
    }
}

// 2. A concrete scroller. RecyclableScrollRect<T> is abstract and Unity can't add an open generic component.
public class InventoryScroller : RecyclableScrollRect<InventoryItem> { }

// 3. Start it.
scroller.Initialize(new ListDataSource<InventoryItem>(items));
```

The developer replaces a Scroll View's `ScrollRect` with the concrete scroller, assigns **Viewport**, **Content** and **Cell Prefab**. Never write the prefab or scene as text.

## Change data

- Change the data source, then call `scroller.Refresh()`. `ListDataSource<T>` has `AddItem`, `AddItems`, `InsertItem`, `RemoveItem`, `RemoveItemAt`, `SetItems`, `ReplaceItems` and `Clear`.
- Switch sources with `SetDataSource(source)`.
- Other members: `ScrollToItem(index)`, `ScrollToStart()`, `ScrollToEnd()`, `ExpandCell(index, newSize)`, `CellWantToCollapse(index)`, `IsItemVisible(index)`, `GetItemCount()`, `IsInitialized`, `VisibleStartIndex`, `VisibleEndIndex`.

## Don't

- Don't keep per-item state in a cell. Keep it in the item or a Model, because the cell will show another item later.
- Don't change the list without calling `Refresh()`.
- Don't invent members. If one isn't listed here or in the README, read the package source.
