# Changelog

All notable changes to this package are documented in this file.

## [Unreleased]

### Changed

- **Breaking:** the package is renamed **UI Scroller** (`com.sgonzalezrozo.ui-scroller`). The namespace is `UIScroller`, and the assemblies are `UIScroller`, `UIScroller.Editor` and the two test assemblies. `SlotList<T>` is now `Scroller<T>`, `SectionedSlotList` is `SectionedScroller`, `SlotListBase` is `ScrollerBase`, `ISlotList` is `IScroller`, `SlotListSettings` is `ScrollerSettings`, `SlotListDirection` is `ScrollerDirection`, the profiler markers are `UI Scroller.*` and the localization define is `UISCROLLER_LOCALIZATION`. `ISlot<T>`, `IExpandableSlot<T>`, **Slot Prefab** and every serialized field keep their names, so scenes and prefabs keep their values.
- **Zero GC in every case tested.** A 56-case test matrix (each layout × scroll, fling, scroll to, snap, refresh, expand, width change, enable and disable, filter) checks that nothing allocates once warmed up. Fixed on the way:
  - enabling a list no longer allocates: the locale listener subscribes once for the list's lifetime;
  - a width change no longer starts a coroutine: the re-placement and the pool warm-up run from `LateUpdate`;
  - `Refresh()` in sectioned lists, bento and sectioned grids reuses its buffers and lines;
  - `FilteredSource` keeps its matches in a reused list.
- **Scrolling costs 20 to 32 % less CPU** (the list's own work, Editor, 100,000 items): list −20 %, 4-column grid −28 %, bento −32 %, sections −20 %. A slot already on screen and unchanged is no longer walked on every range change, and slots are placed with fewer RectTransform writes (pivot and anchors once, size only when it changed, no viewport read per slot). Details in `Documentation~/performance-plan.md`.
- **The list uses 56 to 73 % less memory per item** (100,000 items, Editor): a plain list 134 → 54 bytes per item, a 4-column grid 67 → 18, bento 88 → 39. What only shown items need moved to the slot handle, bento tiles are kept once, and a one-slot line has no array. Scrolling CPU is unchanged.
- **Breaking:** `Line.Placements` is replaced by an indexer (`line[slot]`) and `SlotCount`. `Line` is part of the public `LayoutModel`, which the list uses internally; code outside the package rarely touches it.
- `GetAllItems()` on `ListSource`, `PagedSource` and `FilteredSource` returns the same read-only view on every call. `FilteredSource`'s view is updated in place after the filter or the source changes.

- **Measured lists cost the same at any length.** Lines made only of unmeasured items share one placeholder size, and line offsets are computed on demand up to the line asked for. A 100,000-item measured list took up to 46 ms per scroll event on a Galaxy A02s and now takes up to 3.5 ms, as a 1,000-item one does.
- **Rebuilds are faster:** fixed-size lists read the slot prefab's size once instead of calling `GetComponent` for every item. On a Galaxy A02s at 10,000 items, `Refresh()` with a new count went from up to 75 ms to 21 ms.
- **No allocations when switching between sources of similar size:** buffers shrink only past four times the item count. The first swap used to drop and recreate thousands of lines.

### Added

- **Nested scrolling:** a drag or mouse wheel mostly across a list's axis goes to the nearest parent that handles it, so a horizontal carousel inside a vertical list scrolls the page from anywhere. The gesture is handed over once, when it starts; touching the inner list also stops the outer list's fling. On by default; `Settings.passDragsToParent` turns it off (code only). The Examples sample has a `NestedExample`.
- **Viewport position for carousel effects:** slots that implement `IViewportPositionListener` are told where they sit in the viewport whenever the list moves (0 at the centre, ±0.5 at the edges), and `GetViewportPosition(index)` answers the same for any shown item.
- **Bento Layout Editor** (Window > UI Scroller, or **Edit Layout** / **New Layout** next to **Layout JSON**): patterns drawn on a grid, spans as a list, a live preview of the first items and the layout's errors. Saves the same JSON.
- **Section Spacing** (`Settings.sectionSpacing`): extra space before every section's header except the first, in sectioned lists, grids and bento. Shown in the inspector for sectioned lists only.
- Snapping (item or page) also runs after the mouse wheel, once it stops turning.
- Profiler markers `UI Scroller.Initialize` and `UI Scroller.Instantiate`.

### Fixed

- A page that lands while the list bounces back from its end no longer makes it drift towards the start: the new items end the overscroll, and ScrollRect turned the elastic pull into inertia. The bounce now stops and the list stays where it is. Easy to see in a bottom-to-top list.
- Bento: `VisibleStartIndex` and `IsItemVisible` count a tile only when it reaches the screen (the list keeps a few rows above it for tall tiles), and item snapping settles on the nearest tile start, by the tile's own size, instead of doing nothing on rows only tall tiles cross.
- In the editor, a destroyed list (with its source and items) is no longer kept alive by a pending `OnValidate` call in Play mode.
- A list without Content says why it won't start: a list added from code must create its Viewport and Content.

### Added

- **Bento layout**: `Settings.bento` places tiles of different sizes on a grid of cells. A `BentoLayout`, written in JSON or built in code, gives each item its tile: a span sequence packed in order, or patterns filled in sequence, both repeating from `repeatFrom`. Bad layouts log their errors and fall back to 1x1 tiles. New `SetBentoLayout`, `GetTile` and `Settings.cellAspect`. Tiles don't expand and aren't measured, and a bento list doesn't loop. List, Grid and Auto Grid don't change. Bento works in a `SectionedScroller` too: each header takes a whole row, as tall as the header itself (fixed, or measured so each header can differ), and the layout starts again in each section; a tall tile never reaches into the next section. The Examples sample has a `BentoExample` and a `BentoSectionsExample`. In the inspector, **Layout: Bento** shows a **Layout JSON** field (a TextAsset) and **Cell Aspect**, and lists any errors in the JSON.

## [1.0.0-preview.1] - Unreleased

### Added

- An **Examples** sample: grid, sections, expandable rows, a looping carousel and paging with pull to refresh. Each builds its UI in code, so it runs from an empty scene, and none needs TextMeshPro.

### Fixed

- `autoSlotsPerLine` set from code with `slotsPerLine` left at 1 made a plain list. Auto Grid is now a grid whatever the fixed count, as the inspector already showed.

### Changed

- `ScrollToItem(index, align, duration, offset)` takes an alignment (`ScrollAlign`: `Start`, `Center`, `End`, `Nearest`), an optional animation and a gap from the aligned edge. `ScrollToStart`, `ScrollToEnd` and `ScrollToSection` take an optional duration. A drag or `ForceStopScrolling()` cancels the animation, and `IsScrolling` is true while it runs. Existing calls keep jumping to the start of the item.
- Snapping: **Snap** (`None`, `Item`, `Page`) and **Snap Align** (`Start`, `Center`, `End`) settle the list on an item, or move it a page at a time, after a drag. New `SnappedTo(int index)` event.
- A long looping list (a few copies over 20,000 px, for example 10,000 items) lays out about ten screens of entries mapped to items through an offset, instead of four full copies. Its scroll position stays within a few thousand pixels, so movement and inertia match a plain ScrollRect exactly (at millions of pixels, positions only moved in quarter-pixel steps), and it no longer lays out tens of thousands of entries.
- A loop jump during a real drag no longer builds up a huge speed: the drag restart used the Input System's shared event, which said Middle by then.
- Looping lists: **Loop** makes a list go round forever, like a carousel. The API keeps real indexes and `ScrollToItem` takes the shortest way round. Not for grids, sections, paged sources or expanding items (an error is logged and the list doesn't loop).

- The Viewport is a nested `Canvas` with its own `GraphicRaycaster`, added when the component is added or when the list starts. Moving slots rebuilds only the list's batches, not the batches of everything else on the screen. The nested Canvas copies the outer Canvas's shader channels (TextMeshPro needs them).
- **Breaking:** Slots waiting in the pool stay active, parked across the scroll axis where the Viewport's `RectMask2D` culls them, instead of being switched off. Reusing a slot no longer wakes its graphics and scripts, which cut the list's own cost by about three quarters on a low-end phone. A reused slot's `OnEnable` and `OnDisable` don't run: refresh slots from the `Data` setter.
- After a big drop in item count (under half of what it holds), the layout gives its extra records back instead of keeping them at the largest count it ever had.
- A fixed-size slot opened without a size is measured in Unity's own layout pass instead of with an extra layout rebuild: TMP lays its text out once instead of twice, and the slot grows on the frame after the tap. Text that arrives late is followed the same way.
- New `ContentSizeChanged(float)` event and `ExpandedCount` property.
- A fixed-size slot (`DefaultExtent` above 0) expanded without a size is now measured while it's expanded: it grows to its content, follows it while open, and goes back to its fixed size when it collapses. Only expanded items are measured, so scrolling costs the same as before. `Expand(index, size)` still gives a fixed expanded size. A slot whose root has no layout still expands to twice its size.
- Measured sizes are cached: an item scrolled back into view reuses its size instead of being measured again. A change made while it was off screen is caught by the size watcher one frame after it's shown, or at once after `InvalidateSize(index)`. `Refresh()` still measures the items on screen.
- Rebuilds and appended pages reuse placements and lines and only create what's new; appending a page lays out only the new lines. Content's size is read from the offsets instead of adding up every line.
- The first screen is bound and placed inside `Initialize`, so it shows in the first rendered frame, and **`OnInitialized` now fires inside `Initialize`** (it used to fire a frame later): subscribe before calling it.
- The pool warms up two slots per frame instead of all at once, so starting a list causes no frame spike.
- Disabling a list keeps its slots where they are; enabling it again rebinds and moves nothing unless the viewport or the scroll position changed. Call `Refresh()` after changing data while it was disabled, as always.
- Recycling checks only the slots on screen, a slot that didn't move isn't placed again, and a scroll under half a pixel skips the visible-range work.
- New read-only extension points: the events `ItemExpanded`, `ItemCollapsed` and `Rebuilt`, and `GetItemOffset(index)`, `GetItemSize(index)`, `ViewportSize` and `ContentSize`. They replace what the removed protected hooks (`OnCellExpanded`, `OnCellCollapsed`, `OnBefore/AfterRebuildStructures`, `GetViewportSize`, `cellItems`, `cellLines`) were used to read.
- **Breaking:** A row (vertical) or column (horizontal) of slots is a **line**: `Line`, `slotsPerLine`, `autoSlotsPerLine`, `minSlotsPerLine`, `maxSlotsPerLine` and `lineSpacing` (the 0.3.0 name). Re-check slots per line and line spacing on existing lists.
- **Breaking:** The package is now **UI Scroller** (`com.sgonzalezrozo.ui-scroller`). Every type is renamed, the namespace `RecyclableScroller.Runtime` is now `UIScroller`, and the assemblies are `UIScroller`, `UIScroller.Editor`, `UIScroller.Tests.Editor` and `UIScroller.Tests.PlayMode`.
- **Breaking:** Serialized fields on the scroller are renamed, with no automatic migration. Reassign **Slot Prefab** and check the spacing and grid settings on existing scrollers.
- `SetSource(source)` replaces `SetDataSource` and also works before initialization (it calls `Initialize`).
- **Breaking:** `Scroller<T>` is a thin component over an internal engine. Subclasses keep `BindItem` (now takes a `SlotHandle`), `SupportsPagination`, `OnPullToRefreshDetected`, `OnPaginationThresholdReached`, `DataSource` (read-only) and `HasReachedLastPage`. The other protected members (`InitializeBase`, `InitializePlacements`, `GetItemSize`, `GetDefaultItemSize`, `GetExpandedSizeForItem`, `OnItemCollapsed`, `OnItemExpanded`, `OnBeforeRebuildStructures`, `OnAfterRebuildStructures`, `RecalculateContentSize`, `RebuildVisibleItems`, `GetViewportSize`, `CalculateVisibleCountForViewport`) and the public `scrollStateManager` field are gone.
- Dragging a list before `Initialize` no longer throws.
- `Initialize` works with an empty source; items added later appear after `Refresh()`.
- `ScrollToItem` puts the item at the start in top-to-bottom and right-to-left lists (it used to land on a mirrored position).
- `Expand(index)` without a size uses the item's own size x 2 when there's no size calculator, instead of `estimatedItemSize` x 2 (0 after the old inspector's Single mode).
- Resizing the list across its scroll axis re-places the visible slots, so they take the new width.
- `Refresh()` with the same item count keeps expanded items; `SetSource` with a new source still clears them.
- **Breaking:** `VisibleStartIndex` and `VisibleEndIndex` are item indexes. In a grid they used to be line indexes.
- `FilteredSource` works out the matching items once and caches them, instead of scanning the whole source on every `GetItem`. Call the new `Invalidate()` after the underlying source changes.
- Expanding in a grid logs a warning once, instead of being ignored silently.
- **Breaking:** Every source throws `ArgumentOutOfRangeException` for a bad index (`ListSource` and `FilteredSource` threw `IndexOutOfRangeException`, `PagedSource` returned null).
- A ScrollRect without Content logs an error and leaves the list uninitialized, instead of throwing from `Initialize`.
- **Breaking:** The size calculator is removed (`IItemSizer<T>`, `ItemSizerBase<T>`, `Initialize(source, calculator)`), with the hidden `estimatedItemSize` and `measureBudgetPerFrame` settings. Item sizes come from the slot prefab (see Phase 8 below).
- A slot whose `DefaultExtent` is 0 or less is measured with Unity's layout when it's shown: text, localization and images decide its size. Items not measured yet use the average of the measured ones, and a grid row is as tall as its tallest slot. A positive `DefaultExtent` is still a fixed size and nothing is measured.
- A measured slot whose root reports no preferred size (no layout group) keeps the prefab's size and logs one warning, instead of collapsing to 0.
- A measured slot that changes size after it was shown (a late string, a loaded image) is resized one frame later, and the items after it move. The list adds a `SlotSizeWatcher` to measured slots by itself; fixed-size slots don't get one.
- When sizes change, what's on screen stays still: items measured for the first time while scrolling up grow upwards, and an item whose content changed keeps its start and pushes the items after it. A shift made this way during a drag causes no jump on the next drag event and adds no scroll speed.
- New `InvalidateSize(index)` and `InvalidateAllSizes()` measure items again: those on screen at once (the first one stays still), the others when they're shown. A width change invalidates every size, and with Unity Localization installed a language change does too (no hard dependency: the `UISCROLLER_LOCALIZATION` define is set by the asmdef only when the package is present). Fixed sizes ignore both.
- Expanding a slot measured with layout only sets `IsExpanded`: the slot shows its details and is measured, and the size watcher keeps resizing it while expanded. The size given to `Expand(index, size)` and `SetExpandedSize` is ignored there, with a warning. Expanding or collapsing never moves what's on screen, and expanded items don't count in the average used for unmeasured items. Fixed sizes keep the explicit expanded size.
- New **sections with headers**: `SectionedScroller<TItem, THeader>` shows a `SectionedSource<TItem, THeader>` (`AddSection`, `InsertSection`, `RemoveSection`, `ClearSections`, `KindAt`, `HeaderAt`, `ItemAt`, `Locate`, `FlatIndexOf`, `Changed`). Headers use their own Header Prefab (`ISlot<THeader>`) and pool, take a whole row in a grid, and follow the same fixed or measured size rule as slots. Empty sections are skipped unless `SkipEmptySections` is false. `SetSource` starts the list or replaces its sections, `Refresh()` picks up changes inside a section, and `ScrollToSection` jumps to a header. `ISectionedSourceProvider<TItem, THeader>` builds a source elsewhere.
- **Breaking:** `Scroller<T>` no longer has a Header Prefab or an `IsHeader` override (headers there never got a slot). Use `SectionedScroller` for headers.
- `ScrollerBase` now holds what both lists share (settings, Slot Prefab, events and the API). Existing scenes keep their serialized values. You can't derive from it directly; derive from `Scroller<T>` or `SectionedScroller<TItem, THeader>`.
- **Breaking:** Paging is redesigned. `IPageLoader<T>.LoadAsync(page, cancellationToken)` returns a `PageResult<T>(items, hasMore)` and throws on failure. `PagedSource<T>` implements the new `IPagedSource<T>`: `LoadNextAsync(cancellationToken)`, `Reset()`, `HasMore`, `IsLoading` and the events `ItemsAppended`, `Cleared` and `LoadFailed` (replacing `OnDataChanged`, `OnLoadingStarted` and `OnLoadingFinished`). `GetItem` no longer starts loads.
- The list loads pages by itself: the first page of an empty paged source, then the next one when fewer than 10 items are left after the last visible one. Pages are appended without a rebuild (no `Refresh()` needed), keeping the scroll position and expanded items. Only one page loads at a time, and changing the source or destroying the list cancels a load in flight.
- New events on the list: `PageRequested`, `PageLoaded`, `PageFailed` and `ReachedEnd`. `HasReachedLastPage` turns true at the end. The 96% scroll trigger is gone; `OnPaginationThresholdReached()` is still called when a page is requested.
- The inspector is simpler: Slot and Header prefabs, Direction, a **Layout** choice (List / Grid / Auto Grid, with its fields underneath), spacing and padding labelled for the direction (rows, columns, top, bottom…), and a few behaviour toggles. ScrollRect's own settings are hidden: the list's direction now sets the ScrollRect's scroll axis and Content's anchors, also in the editor. The old "Scroll Type" guess, which could overwrite `estimatedItemSize`, is gone.

| Old | New |
|---|---|
| `RecyclableScrollRect<T>`, `RecyclableScrollRectBase` | `Scroller<T>`, `ScrollerBase` |
| `IRecyclableScrollRect` | `IScroller` |
| `IRecyclableCell`, `IRecyclableCell<T>` | `ISlot`, `ISlot<T>` |
| `IExpandableRecyclableCellBase`, `IExpandableRecyclableCell<T>` | `IExpandableSlot`, `IExpandableSlot<T>` |
| `IAnimationMarker` | `IAppearAnimation` |
| `IRecyclableDataSource<T>`, `IScrollSource<T>` | `IItemSource<T>`, `IItemSourceProvider<T>` |
| `ListDataSource<T>`, `FilteredDataSource<T>`, `PaginatedDataSource<T>` | `ListSource<T>`, `FilteredSource<T>`, `PagedSource<T>` |
| `IPaginatedDataProvider<T>`, `PaginationResult<T>` | `IPageLoader<T>`, `PageResult<T>` |
| `IScrollDirection` and the four directions | `IAxisLayout`, `TopToBottomLayout`, `BottomToTopLayout`, `LeftToRightLayout`, `RightToLeftLayout` |
| `CellItem`, `CellLine`, `CellPool` | `Placement`, `Line`, `SlotPool` |
| `CellPrefab`, `ExpandCell`, `CellWantToCollapse`, `UpdateExpandedCellSize`, `IsIndexExpanded` | `SlotPrefab`, `Expand`, `Collapse`, `SetExpandedSize`, `IsExpanded` |
| `DefaultCellSize`, `CellDataIndex`, `ScrollRect` (on expandable slots), `CollapseCell()` | `DefaultExtent`, `ItemIndex`, `Owner`, `OnCollapsed()` |
| `ShouldAnimate`, `CalculateCellSize`, `GetDataSource()` | `ShouldPlay`, `CalculateSize`, `GetSource()` |
| Serialized `cellPrefab`, `lineSpacing`, `gridCellSpacing`, `gridCount` | `slotPrefab`, `lineSpacing`, `slotSpacing`, `slotsPerLine` |

### Removed

- The unfinished UI Toolkit adapters (`RecyclableScroller.UIToolkit`). The package is uGUI only.
- The unfinished second layout: the `RecyclableScroller.Core`, `RecyclableScroller.Abstractions` and `RecyclableScroller.UGUI` assemblies, including `RecyclableScrollerCore<T>` and `RecyclableScrollRectGUI<T>`. Everything you use is in `RecyclableScroller.Runtime`.

### Changed

- **Breaking:** `PullToRefreshUI` and `EmptyStateDisplay` moved out of the runtime into the new **Extras** sample, renamed `PullIndicator` and `EmptyStatePanel`. The runtime assembly no longer references TextMeshPro.
- **Breaking:** Removed the non-generic `IRecyclableDataSource` interface. Use `IRecyclableDataSource<T>`.
- Removed unused classes and members (see the refactor plan, Phase 3).
- **Breaking:** The direction classes (`IScrollDirection` and the four directions) moved from `RecyclableScroller.Core` to `RecyclableScroller.Runtime`. They lost `CalculateCachedPositions` and `PositionCell`, which nothing called.

### Fixed

- The scroller now initializes in batch mode. Its first build waited for `WaitForEndOfFrame`, which never resumes there. It now waits one frame.

### Tests

- 243 tests (138 EditMode, 105 PlayMode), run in batch mode: the original behaviour pinned before the refactor, a test for each known bug (all fixed now), and design tests written before each new part.

### Documentation

- An FAQ, rules for AI coding agents in `Documentation~/agent-rules.md`, `llms.txt` and more package keywords.

### Documentation

- Said that the package isn't used in production or in a shipped game yet, and described Tools Core as a required dependency.

### Changed

- The runtime assemblies expose their internals to `Assembly-CSharp`, so Fast IoC `0.4.0` assigns injected fields directly, without reflection.

## [0.3.0] - 2026-11-03

### Fixed

- `PaginatedDataSource.Reset()` no longer skips page 0 on the next load.

### Changed

- Updated to Tools Core `0.3.0`, which removes `IMaskSource`.

### Documentation

- Added recipes for expandable cells, pagination, grids, headers, reversed lists, filtered data and pull to refresh.
- Added a `ListView` comparison and a description of the code layout.
- Added contributing, security and issue template files, and a Quick Start sample.
- Removed internal development notes from code comments, and added XML documentation to the main scroller APIs.

## [0.2.0] - Unreleased

### Changed

- **Breaking:** Renamed the namespaces from `Tools.RecyclableScroller.*` to `RecyclableScroller.*`. Main types are now in `RecyclableScroller.Runtime`.
- **Breaking:** Renamed the assemblies to `RecyclableScroller`, `RecyclableScroller.Core`, `RecyclableScroller.Abstractions`, `RecyclableScroller.UGUI`, `RecyclableScroller.UIToolkit` and `RecyclableScroller.Editor`.
- **Breaking:** Renamed the `GUI` module to `UGUI`.

## [0.1.1] - Unreleased

- Updated to Tools Core `0.2.0`.

## [0.1.0] - Unreleased

First pre-release for private testing.
