# Migrating from Recyclable Scroller 0.3.0

Slot List `1.0.0-preview.1` is the next version of Recyclable Scroller `0.3.0`, under a new name. Every type, namespace, assembly and serialized field is renamed, and there's no automatic migration. Plan for one pass over your code and one over your scenes and prefabs.

## 1. Swap the package

In `Packages/manifest.json`, replace the old entry with the new one. The Git URL stays the same; the package name changes:

```json
"com.sgonzalezrozo.slot-list": "https://github.com/sgonzalezrozo/unity-recyclable-scroller.git#v1.0.0-preview.1"
```

Remove `com.sgonzalezrozo.recyclable-scroller`. Keep Tools Core (`com.sgonzalezrozo.core` `0.3.0`). If an assembly definition of yours referenced `RecyclableScroller` (or `.Core`, `.UGUI`, `.Abstractions`), reference `SlotList` instead.

## 2. Rename in code

Replace `using RecyclableScroller.Runtime;` with `using SlotList;`, then rename:

| 0.3.0 | Slot List |
|---|---|
| `RecyclableScrollRect<T>`, `RecyclableScrollRectBase` | `SlotList<T>`, `SlotListBase` |
| `IRecyclableScrollRect` | `ISlotList` |
| `IRecyclableCell`, `IRecyclableCell<T>` | `ISlot`, `ISlot<T>` |
| `IExpandableRecyclableCellBase`, `IExpandableRecyclableCell<T>` | `IExpandableSlot`, `IExpandableSlot<T>` |
| `IAnimationMarker` (`ShouldAnimate`) | `IAppearAnimation` (`ShouldPlay`) |
| `IRecyclableDataSource<T>`, `IScrollSource<T>` (`GetDataSource()`) | `IItemSource<T>`, `IItemSourceProvider<T>` (`GetSource()`) |
| `ListDataSource<T>`, `FilteredDataSource<T>`, `PaginatedDataSource<T>` | `ListSource<T>`, `FilteredSource<T>`, `PagedSource<T>` |
| `IPaginatedDataProvider<T>`, `PaginationResult<T>` | `IPageLoader<T>`, `PageResult<T>` (redesigned, see step 4) |
| `CellPrefab` | `SlotPrefab` |
| `ExpandCell`, `CellWantToCollapse`, `UpdateExpandedCellSize`, `IsIndexExpanded` | `Expand`, `Collapse`, `SetExpandedSize`, `IsExpanded` |
| `SetDataSource` | `SetSource` (also works before `Initialize`) |
| On a slot: `DefaultCellSize` | `DefaultExtent` |
| On an expandable slot: `CellDataIndex`, `ScrollRect`, `CollapseCell()` | `ItemIndex`, `Owner`, `OnCollapsed()` |
| `PullToRefreshUI`, `EmptyStateDisplay` | `PullIndicator`, `EmptyStatePanel`, in the **Extras** sample |

## 3. Subclasses of the scroller

`SlotList<T>` is a thin component over an internal engine, so most protected members are gone. What's left:

- `BindItem(SlotHandle slot, int index)`: it used to take the cell. `slot.Slot` is your `ISlot`, `slot.GameObject` its object.
- `OnPullToRefreshDetected()`, `OnPaginationThresholdReached()`, `SupportsPagination()`, `DataSource` (read-only) and `HasReachedLastPage`.

If you overrode a removed hook to read state, use the public replacement:

| Removed | Use |
|---|---|
| `OnCellExpanded`, `OnCellCollapsed` | the `ItemExpanded` and `ItemCollapsed` events |
| `OnBeforeRebuildStructures`, `OnAfterRebuildStructures` | the `Rebuilt` event |
| `GetViewportSize`, `cellItems`, `cellLines` | `ViewportSize`, `ContentSize`, `GetItemOffset(index)`, `GetItemSize(index)` |
| `RecalculateContentSize`, `RebuildVisibleItems` | `Refresh()` or `InvalidateSize(index)` |

## 4. Sizes, headers and paging

- **The size calculator is gone** (`IItemSizer<T>`, `ItemSizerBase<T>`, `Initialize(source, calculator)`). A positive `DefaultExtent` is a fixed size; 0 or less measures the slot with Unity's layout. See *Item sizes* in the README.
- **Headers** in a plain list (Header Prefab, `IsHeader`) are gone. Use `SectionedSlotList<TItem, THeader>` with a `SectionedSource<TItem, THeader>`.
- **Paging is redesigned.** Implement `IPageLoader<T>.LoadAsync(page, cancellationToken)`, return `new PageResult<T>(items, hasMore)` and throw on failure. Pages start at 0. The list loads the first page and the next ones by itself, and appends them without `Refresh()`. Listen to `PageRequested`, `PageLoaded`, `PageFailed` and `ReachedEnd` instead of the source's old events.
- `VisibleStartIndex` and `VisibleEndIndex` are item indexes; in a grid they used to be line indexes.
- Sources throw `ArgumentOutOfRangeException` for a bad index.

## 5. Scenes and prefabs

Your scroller subclasses keep their script (only their base class changed), but the serialized fields they inherit are renamed, so their values don't carry over. On each scroller:

1. Open it in the inspector after the code compiles.
2. Reassign **Slot Prefab** (and **Header Prefab** on a sectioned list).
3. Check **Direction**, **Layout** (List, Grid, Auto Grid), the spacing and the padding. The old `gridCount` and `gridCellSpacing` are now **slots per line** and **slot spacing**, and they aren't carried over.
4. Remove any `LayoutGroup`, `ContentSizeFitter`, `LayoutElement` or `AspectRatioFitter` from **Content**; the list refuses to start with one (the inspector has a **Remove Layout Components** button).
5. Remove the slot root's `ContentSizeFitter`, if it has one. A measured slot needs a layout group on its root instead.

## 6. Behaviour changes to check

- **Pooled slots stay active.** A slot waiting in the pool is moved out of view instead of switched off, so a reused slot's `OnEnable` and `OnDisable` don't run. Refresh every visual from the `Data` setter.
- **`OnInitialized` fires inside `Initialize`**, not a frame later. Subscribe before calling it.
- The Viewport becomes a nested `Canvas` with its own `GraphicRaycaster`. If you raycast against slots from your own code, include that raycaster.
- `ScrollToItem` takes an alignment, a duration and an offset. Existing calls still jump to the start of the item.

## New things you can use

Snapping (`Snap`, `Snap Align`, `SnappedTo`), looping carousels (**Loop**), animated `ScrollToItem`, measured sizes that follow late text, `InvalidateSize`, sections and the new paging. The **Examples** sample shows most of them.
