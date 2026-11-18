# Troubleshooting & Limitations

[← Back to README](../README.md)

- [Troubleshooting](#troubleshooting)
- [Limitations and roadmap](#limitations-and-roadmap)

---

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
| A slot with a layout group doesn't grow with its content | `DefaultExtent` returns a positive number, which is a fixed size. Return `-1` (see [Item sizes](data-and-sizes.md#item-sizes)) |
| Measured slots all keep the prefab's size, with a warning | Add a layout group to the slot's root with **Control Child Size > Height** on (see [Item sizes](data-and-sizes.md#item-sizes)) |
| Measured slots are too tall or collapse | Remove the `ContentSizeFitter` from the slot's root, and turn off **Child Force Expand > Height** |
| The bento list shows 1x1 tiles | The layout has errors: the inspector and the console list them |
| New pages don't appear | Check that the source is a `PagedSource<T>` (or another `IPagedSource<T>`), and subscribe to `PageFailed` to see load errors |

## Limitations and roadmap

| Limitation | Status |
|---|---|
| Nested lists on the same axis (a vertical list inside a vertical one) don't hand the drag over at their edges | When a screen needs it |
| One slot prefab per list (plus a header prefab in sectioned lists) | Slot types (several prefabs, one pool each) are planned for a later version. Measured sizes and expanding already cover slots of different heights |
| No sticky headers | Planned as a separate component in the Extras sample, built on the public queries |
| Items must be reference types | By design |
| A looping list can't be a grid, sectioned or expandable; with a paged source it loops over the first page only | By design |
| A bento carousel repeats its layout as a block per round: when the item count doesn't fill whole patterns, each round ends with holes | By design: it keeps the loop seamless. Use an item count that fills the patterns |
| **Snap: Page** moves one viewport at a time, not one bento pattern or round | By design |
| A bento loop can't be very long: it's laid out as a few copies of its items | Longer ones log a warning and don't loop |
| Grids and bento don't expand | By design: opening one slot would change its whole row |
| A sectioned list doesn't page | By design |

Planned work is also tracked in [issues](https://github.com/sgonzalezrozo/unity-ui-scroller/issues).
