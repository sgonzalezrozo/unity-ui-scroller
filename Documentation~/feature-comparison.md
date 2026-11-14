# Recyclable Scroller 0.3.0 → Slot List 1.0.0-preview.1: feature by feature

A guide to the refactor for the owner: for every feature of the original, what's the same, what changed in the implementation, what changed only in name, what's new and what was dropped. Then a mental model of the tool as it is now.

- **Compared:** `main` (Recyclable Scroller `0.3.0`, tag `v0.3.0-baseline`) and `refactor/p10-performance` (`dd61297`, 2026-11-13).
- **The short answer:** every feature of the original is still there, with the same concepts. One real capability was dropped: **a fixed size per item computed from data** (the size calculator). Other drops are protected hooks, unused code, and the unfinished UI Toolkit and Core layers. See [What was dropped](#what-was-dropped) for the full list.

**Legend:**
- **Same:** the concept and behaviour didn't change.
- **Renamed:** same code or behaviour under a new name.
- **Reimplemented:** same behaviour, new code.
- **Changed:** the behaviour differs. Usually a fix, sometimes an owner decision.
- **New:** didn't exist in 0.3.0.
- **Dropped:** gone.

---

## 1. Slots and lines (the core concept)

**Same concept.** An item is shown by a pooled GameObject. Items are grouped into lines along the scroll axis: one item per line in a list, several in a grid. Each line has an offset and a size, and the content is as long as all the lines plus spacing and padding.

| 0.3.0 | Now | Kind |
|---|---|---|
| Cell (the pooled GameObject) | **Slot** | Renamed |
| `IRecyclableCell<T>` (`DefaultCellSize`, `Data`) | `ISlot<T>` (`DefaultExtent`, `Data`) | Renamed. `DefaultExtent` also decides the sizing mode now (§4). |
| `CellLine` (a line of cells) | **`Line`** | Renamed. It was `Lane` for a while during the refactor; you chose *line*. |
| `CellItem` (one item's position, size and cell) | **`Placement`** | Renamed. Its `gridCount` throw-away items are gone. |
| `CellLineBuilder`, `LinePositionCache`, `LineContentSizeCalculator`, `ContentSizeCalculator` | **`LayoutModel`** (one class) | Reimplemented: one model holds placements, lines, offsets, item sizes and the total extent. |
| Grid count | **Slots per line** | Renamed (`gridCount` → `slotsPerLine`) |
| Size along the scroll axis | **Extent** | Renamed |

**New in the model:**
- An item-to-line map, correct with headers. The old code assumed `index / gridCount`, which broke with headers.
- Measured flags and a running average of the measured sizes (§4).

---

## 2. Working out what's visible, and recycling

**Same concept.** From the scroll position, find the first and last visible lines. Return the slots of lines that left that range to the pool, take slots for lines that entered it, place them and bind them. The pool is sized from how many lines fit in the viewport times a buffer (1.4 by default, 1.4 to 3.0 with the override).

| 0.3.0 | Now | Kind |
|---|---|---|
| `VisibleLineFinder` checks lines one by one, twice per scroll | `VisibleRangeFinder` does a **binary search** over line offsets | Reimplemented (P1) |
| `VisibilityUpdater`, `VisibilityManager`, `VisibilityIndexFinder`, `VisibleCellCalculator` | `VisibilityPass` | Reimplemented. It still returns early when the range didn't change. |
| `CellLineRecycler` + `CellLineDisplayer` + `CellRecycler` | **`SlotRecycler`** | Reimplemented, merged |
| `ScrollStateManager` (public field) | `VisibleRangeTracker` (internal) | Renamed. The public field is gone. |
| `LinePositionCache.Rebuild` recomputes every offset after any change | Offsets recomputed **from the lowest changed line** | Changed (faster) |
| `VisibleStartIndex` / `VisibleEndIndex` returned **line** indexes | They return **item** indexes, like `IsItemVisible` | Changed (bug 6.13). Same values in a plain list, different in a grid. |
| `ScrollActivityTracker` | `DragState` | Renamed |
| `ScrollNavigator`, `PositionCalculator` | `ScrollMover` | Reimplemented |
| `ViewportMetrics`, `CachedViewportMetrics` | `AxisMetrics` | Reimplemented. It also reads and sets the scroll distance, for anchoring. |

**New:**
- **Anchoring:** when sizes change, what's on screen stays still (§4).

**Phase 10 so far:**
- A recycled slot **no longer leaves Content** (P6). It used to be re-parented to a `DontDestroyOnLoad` object outside every canvas and back, which rebuilt its graphics each time.
- `Refresh()` **rebinds slots in place** instead of returning and taking back every visible slot (P7).
- Scrolling 100,000 items allocates **0 bytes**, which a test now checks.

**Later in Phase 10:**
- Done since: only the slots on screen are checked when lines leave (P2), slots that didn't move aren't placed again (P3), sub-pixel scrolls skip the range work (P4), and the Viewport is a nested Canvas (P18).

---

## 3. Pooling

| 0.3.0 | Now | Kind |
|---|---|---|
| `CellPool`: one queue for cells, a second for headers. A returned header could end up in the cell queue. | `SlotPool`: **one queue per `SlotKind`** (Item, Header) | Reimplemented, bug fixed |
| `GetComponent` 4–5 times per bind | `SlotHandle` caches the slot's components once | Reimplemented (P5) |
| Pool container outside the canvas | Slots wait **inactive under Content** | Changed (P6) |
| Warm-up a frame after `Initialize`, waiting for `WaitForEndOfFrame` | Warm-up a frame later with `yield return null` | Changed (bug 6.15). `WaitForEndOfFrame` never resumes in batch mode. |
| `CellLinePool` | Removed | Was unused |

---

## 4. Item sizes: fixed and dynamic

**Same idea:** a slot either has a fixed size or a dynamic one. **What decides it, and how the dynamic size is found, changed completely.**

| 0.3.0 | Now | Kind |
|---|---|---|
| Fixed: every cell is `DefaultCellSize` | Fixed: **`DefaultExtent > 0`** on the slot prefab | Renamed |
| Dynamic: `ICellSizeCalculator<T>.CalculateCellSize(item, index)` returns a size from the data, given with `Initialize(source, calculator)` | Dynamic: **`DefaultExtent <= 0`** (for example `-1`). The list measures the real slot with Unity's layout (`ForceRebuildLayoutImmediate` + `GetPreferredSize`) when it's shown. | **Changed.** The calculator is gone. |
| `DynamicCellSizeCoordinator` computed every size up front, `cellsPerFrame` at a time | Sizes are measured lazily, when a slot is shown | Reimplemented |
| `estimatedCellHeight` for unknown sizes | The **average of the measured items**, or the prefab's own size before anything is measured | Changed. The setting is gone. |
| `CalculateExpandedSize`, `SetContainerWidth` on the calculator | — | Dropped with the calculator |

**New:**
- **`SlotSizeWatcher`:** added to measured slots by itself. It catches size changes after binding (a late string, a loaded image, a child turned on) and applies them one frame later.
- **Anchoring:** items measured for the first time while scrolling up grow upwards. An item whose content changed keeps its start. A shift during a drag causes no jump and no extra speed.
- **`InvalidateSize(i)` and `InvalidateAllSizes()`.** A width change invalidates every size, and so does a language change with Unity Localization installed. The package doesn't depend on Localization: the `SLOTLIST_LOCALIZATION` define is set only when it's present.
- A grid row is as tall as its tallest slot.
- A root with no layout keeps the prefab's size and logs one warning, instead of collapsing to 0.

**Dropped:**
- **A different fixed size per item, computed from data, without layout.** `DefaultExtent` is read once from the prefab, so every fixed-size slot has the same size. Items that differ ("long names get 120, short get 80") now need a measured slot: a layout group or `LayoutElement` on the root, and `DefaultExtent <= 0`.
  - Measuring costs one layout rebuild per item the first time it's shown. The calculator cost nothing.
  - The scrollbar and `ScrollToItem` are only approximate until items are seen. The calculator knew every size up front.
  - Plan 8.3 B, "measure ahead with a hidden copy", was skipped. It would fix the accuracy.
  - **If you relied on this, say so.** An optional per-item size override could come back without adding anything to the inspector.

---

## 5. Expandable slots

**Same concept.** A slot implements the expandable interface. It asks its list to expand or collapse it, and the list resizes its line and moves the items after it. The list keeps one item expanded at a time, unless more are allowed. Grids still don't expand.

| 0.3.0 | Now | Kind |
|---|---|---|
| `IExpandableRecyclableCell<T>` / `IExpandableRecyclableCellBase` | `IExpandableSlot<T>` / `IExpandableSlot` | Renamed |
| `ScrollRect` (on the cell), `CellDataIndex`, `CollapseCell()` | `Owner`, `ItemIndex`, `OnCollapsed()` | Renamed |
| `IRecyclableScrollRect` | `ISlotList` | Renamed |
| `ExpandCell`, `CellWantToCollapse`, `UpdateExpandedCellSize`, `IsIndexExpanded` | `Expand`, `Collapse`, `SetExpandedSize`, `IsExpanded` | Renamed |
| **Allow Multiple Expanded Cells** | **Allow Multiple Expanded** | Renamed |
| `CellExpansionController` + `ExpansionOperationHandler` | `ExpansionState` | Reimplemented, merged |
| `ExpandCell(i)` without a size used `estimatedCellHeight × 2`, which was 0 after the inspector's Single mode | The item's own size × 2 | Changed (bug 6.3) |
| `Refresh()` collapsed everything | `Refresh()` keeps expanded items; `SetSource` clears them | Changed (bug 6.10) |
| Expanding in a grid was ignored silently | It logs one warning | Changed (bug 6.12) |

**New:**
- **Measured expandable slots size themselves.** `Expand` and `Collapse` only set `IsExpanded`: the slot shows its details and is measured again. A size passed to `Expand` is ignored, with a warning. Expanded items stay out of the average.
- Expanding or collapsing never moves what's on screen.

**Dropped:**
- The protected hooks `OnCellExpanded(index, size)`, `OnCellCollapsed(index)` and `GetExpandedSizeForCell(index)` on the scroller subclass. Today the slot itself reacts through `IsExpanded` and `OnCollapsed()`, and there's no list-level event. If you used them, they can come back as events.

---

## 6. List and grid

**Same.** A list has one slot per line. A grid has N slots per line, scaled to fill the width, which is your rule. An automatic grid fits as many columns as the width allows, between a minimum and a maximum.

| 0.3.0 | Now | Kind |
|---|---|---|
| `gridCount`, `useDynamicGridCount`, `dynamicGridCellSize`, `minDynamicGridCount`, `maxDynamicGridCount` | `slotsPerLine`, `autoSlotsPerLine`, `targetSlotSize`, `minSlotsPerLine`, `maxSlotsPerLine` | Renamed |
| `lineSpacing`, `gridCellSpacing` | `lineSpacing` (same name), `slotSpacing` | `gridCellSpacing` renamed |
| `GridCountResolver` | `SlotsPerLineResolver` | Renamed |
| The inspector guessed a "Scroll Type" from `estimatedCellHeight` | A **Layout** dropdown: List / Grid / Auto Grid, with its fields underneath | Changed (bugs 6.4, 6.5, then your minimal-inspector decision) |
| Only cross-axis resizes, and only in automatic grids, re-laid the list | Any viewport resize re-places the slots and recomputes the range | Changed (bug 6.6) |

**Not added:** cross-axis padding. It was built (6.7) and then removed at your request.

---

## 7. Direction and reverse

**Same four layouts:** top to bottom, bottom to top, left to right, right to left.

| 0.3.0 | Now | Kind |
|---|---|---|
| `isVertical` + `reverse` (two booleans) | `direction` (`SlotListDirection`, one dropdown) | Changed: same four options, one field |
| `IScrollDirection`, `ScrollDirectionBase`, `ScrollDirectionFactory`, `VerticalTopToBottomDirection`… | `IAxisLayout`, `AxisLayoutBase`, `AxisLayouts`, `TopToBottomLayout`… | Renamed |
| Four `ScrollStrategies` classes | Folded into the layouts | Reimplemented |
| `ScrollToItem` landed on a mirrored position in top-to-bottom and right-to-left lists | It puts the item at the start | Changed (bug 6.2) |
| The axis and Content's anchors were set from `isVertical` / `reverse` at runtime, when the list started (`ScrollComponentBinder`). In the editor the ScrollRect showed vertical until Play. | The direction sets the axis and Content's anchors at runtime **and** in the editor whenever you change it. ScrollRect's own settings are hidden. | Changed (same rule, applied earlier). Content's anchoring will follow your upcoming instructions. |

---

## 8. Data sources

| 0.3.0 | Now | Kind |
|---|---|---|
| `IRecyclableDataSource<T>` (`ItemCount`, `GetItem`, `GetAllItems`) | `IItemSource<T>` | Renamed |
| `ListDataSource<T>` (`AddItem(s)`, `InsertItem`, `RemoveItem(At)`, `Clear`, `SetItems`, `ReplaceItems`) | `ListSource<T>`, same methods | Renamed |
| `FilteredDataSource<T>` scanned the whole source on every `GetItem` | `FilteredSource<T>` caches the matches. Call **`Invalidate()`** after the underlying source changes. | Changed (bug 6.11) |
| `IScrollSource<T>.GetDataSource()` | `IItemSourceProvider<T>.GetSource()` | Renamed |
| Bad index: `IndexOutOfRangeException`, or null from the paged source | `ArgumentOutOfRangeException` everywhere | Changed (bug 6.14) |
| `DataSourceGuard<T>` | `SourceGuard<T>` | Renamed |
| `Initialize` with 0 items left the list uninitialized | It works; items added later appear after `Refresh()` | Changed (bug 6.1) |
| `SetDataSource(source)` after `Initialize` only | **`SetSource(source)`** works before or after | Changed |
| Non-generic `IRecyclableDataSource` | — | Dropped. Only `EmptyStateDisplay` used it. |

**The rule is the same:** change the data, then call `Refresh()`. Paged sources are the exception (§9).

---

## 9. Pagination

**Same concept.** A provider loads page N, a paged source accumulates the items, and the list asks for more near the end. **The API and the trigger were redesigned** (Phase 7).

| 0.3.0 | Now | Kind |
|---|---|---|
| `IPaginatedDataProvider<T>.LoadPageAsync(page)` → `PaginationResult<T>` (`Success`, `Items`, `HasMore`, `CurrentPage`) | `IPageLoader<T>.LoadAsync(page, cancellationToken)` → `PageResult<T>(items, hasMore)`. **It throws on failure** instead of returning `Success = false`. | Changed |
| `PaginatedDataSource<T>`: `LoadNextPageAsync`, `Reset`, `IsLoading`, `HasMorePages`, `CurrentPage`, `OnDataChanged`, `OnLoadingStarted`, `OnLoadingFinished` | `PagedSource<T>` (`IPagedSource<T>`): `LoadNextAsync(ct)`, `Reset`, `IsLoading`, `HasMore`, `CurrentPage`, events `ItemsAppended`, `Cleared`, `LoadFailed` | Changed |
| You loaded the first page yourself, and wired `OnDataChanged = scroller.Refresh` | **The list loads pages by itself**: the first page of an empty source, then the next one. Pages are **appended without a rebuild**, keeping the scroll position and expanded items. | Changed |
| Trigger: binding a cell within 10 of the end (in `GetItem`), plus a 96% scroll threshold calling `OnPaginationThresholdReached()` | Trigger: fewer than 10 items left after the last visible one. **`OnPaginationThresholdReached()` is kept** and runs when a page is requested. | Changed. The 96% trigger is gone. |
| `hasReachedLastPage` was never set for paged sources | `HasReachedLastPage` turns true at the end | Changed (bug 6.9) |
| `SupportsPagination()` override | Same | Same |

**New:**
- Events on the list: `PageRequested`, `PageLoaded(count)`, `PageFailed(exception)` and `ReachedEnd`.
- One load at a time. Changing the source or destroying the list cancels a load in flight.

**Dropped:**
- `IPaginatedScrollSource<T>`. Nothing used it.

**Left out until you ask:**
- A paging footer (Loading / Failed with Retry / End).
- Loading at the top (prepending pages).

---

## 10. Pull to refresh

**Same.** Releasing a drag more than **75 px** past the start calls the **`OnPullToRefreshDetected()`** override.

| 0.3.0 | Now | Kind |
|---|---|---|
| `PullToRefreshDetector` | `PullGesture` | Renamed |
| `PullToRefreshUI` in the runtime (TextMeshPro label and spinner) | `PullIndicator` in the **Extras** sample | Moved and renamed. The runtime no longer depends on TextMeshPro. |
| `RecyclableScrollConfig.OnPullRefresh` / `OnPullLoadMore` | — | Dropped: the config class was never used, so this was never a working feature |

---

## 11. Multi-source: sections with headers

**The concept is kept and now lives in the package.** It came from your other project (`CompositeDataSource<T, U>` + `IScrollMultiSource<T, U>` + a scroller subclass). That code wasn't shared, so Phase 9 **rebuilt it from the plan's API**. The concept is the same: one source per section, each with a header. The API may differ from what your project uses; plan 9.4 has migration notes.

| Other project | Now | Kind |
|---|---|---|
| `CompositeDataSource<T, U>` | `SectionedSource<TItem, THeader>` | Reimplemented. The type order is item first, header second. |
| `IScrollMultiSource<T, U>` | `ISectionedSourceProvider<TItem, THeader>` | Reimplemented |
| Your scroller subclass | `SectionedSlotList<TItem, THeader>` | Reimplemented |

**What it does:**
- **Sections:** `AddSection`, `InsertSection`, `RemoveSection`, `ClearSections`.
- **Reading entries:** `Count`, `SectionCount`, `KindAt`, `HeaderAt`, `ItemAt`.
- **Converting indexes:** `Locate` (a binary search) and `FlatIndexOf`.
- **Changes:** `Changed`. `Recount()` runs from `Refresh()`.
- **Empty sections** are skipped by default (`SkipEmptySections`).
- **Headers** have their own Header Prefab (`ISlot<THeader>`) and their own pool. A header takes a whole row in a grid, and follows the same fixed or measured size rule as slots.
- `ScrollToSection(i)`.

**The original header path:**
- In 0.3.0, `RecyclableScrollRect<T>` had a Header Prefab and an `IsHeader(index)` override, but a header entry never got the header prefab (bug).
- At your request, **headers exist only in `SectionedSlotList`**. `SlotList<T>` lost Header Prefab and `IsHeader`.

**Not checked yet:**
- Did your sectioned list in the other project page? `SectionedSlotList` doesn't.
- No manual Sections scene has been checked by hand.

---

## 12. Navigation and visibility API

| 0.3.0 | Now | Kind |
|---|---|---|
| `ScrollToItem`, `ScrollToStart`, `ScrollToEnd`, `ForceStopScrolling` | Same names | Same. `ScrollToItem` has the direction bug fixed. |
| `IsItemVisible`, `IsFirstItemVisible`, `IsLastItemVisible`, `IsScrolling`, `IsDragging`, `IsInitialized`, `GetItemCount` | Same names | Same |
| `VisibleStartIndex`, `VisibleEndIndex` | Same names, item indexes now | Changed (§2) |
| `OnInitialized` action | Same | Same |
| — | `ScrollToSection` (sectioned lists) | New |

**Added in Phase 11:** `ScrollToItem` with alignment (`ScrollAlign`) and animation, snapping (`Item`, `Page`) with `SnappedTo`, and looping lists (carousels).

---

## 13. Appear animation and empty state

| 0.3.0 | Now | Kind |
|---|---|---|
| `IAnimationMarker.ShouldAnimate` | `IAppearAnimation.ShouldPlay` | Renamed |
| `CellAnimationMarker` (the first screen doesn't animate) | `AppearAnimationPolicy` | Renamed |
| `EmptyStateDisplay` in the runtime | `EmptyStatePanel` in the Extras sample | Moved and renamed |

---

## 14. Inspector, setup and dependencies

| 0.3.0 | Now | Kind |
|---|---|---|
| Fields serialized directly on the scroller | `SlotListSettings` (a serializable class) | Reimplemented. Field names changed, with no `FormerlySerializedAs` (your decision): existing scenes need re-assigning. |
| `cellPrefab`, `headerPrefab` | `slotPrefab` (both lists), `headerPrefab` (sectioned only) | Renamed |
| `scrollToInitOnEnable` | `resetScrollOnEnable` | Renamed |
| `overridePoolPercentage`, `poolPercentage` | `overridePoolBuffer`, `poolBuffer` | Renamed |
| `estimatedCellHeight`, `cellsPerFrame` | — | Dropped with the calculator (§4) |
| Adding the component builds a stretched Viewport (with RectMask2D) and a stretched Content, references them in the hidden ScrollRect fields, adds an invisible raycast Image to the root and clears the scrollbars (`Reset`) | Same (`SlotListBase.Setup.cs`), now covered by `AddComponentSetupTests` | Same. Fixed: it always set the axis to vertical; it now reads the direction. |
| `injectedInstantiator` for Fast IoC, `InternalsVisibleTo("Assembly-CSharp")` | Same | Same (kept on purpose) |
| Content with a layout component: only a README line ("remove any LayoutGroup or ContentSizeFitter"), nothing enforced it | **Enforced:** the editor removes a layout component added to Content (or a `LayoutGroup` added to the Viewport), the inspector shows an error with a Remove button, and `Initialize` refuses to start with an error naming it | New |
| Depends on Tools Core, uGUI, TextMeshPro | Tools Core and uGUI. TextMeshPro only in the samples. | Changed |
| Assemblies `RecyclableScroller`, `.Core`, `.Abstractions`, `.UGUI`, `.UIToolkit`, `.Editor` | `SlotList`, `SlotList.Editor` | Changed: the unfinished layers were removed |

---

## 15. The scroller subclass

**Same way to use it:** subclass the generic component, because Unity can't add an open generic, and override a few hooks.

| 0.3.0 | Now | Kind |
|---|---|---|
| `RecyclableScrollRect<T>` : `RecyclableScrollRectBase` : `ScrollRect` | `SlotList<T>` / `SectionedSlotList<TItem, THeader>` : `SlotListBase` : `ScrollRect` | Renamed. You can't derive from `SlotListBase` directly. |
| `BindCell(RectTransform cell, int index)` | `BindItem(SlotHandle slot, int index)` | Changed: the argument type |
| `SupportsPagination`, `OnPullToRefreshDetected`, `OnPaginationThresholdReached` | Same | Same |
| `DataSource` (protected, settable) | `DataSource` (read-only) | Changed |
| `IsHeader`, `GetEstimatedHeaderCount` | — | Dropped (headers moved to §11) |
| `GetCellSize`, `GetDefaultCellSize`, `GetExpandedSizeForCell`, `OnCellExpanded`, `OnCellCollapsed`, `OnBeforeRebuildStructures`, `OnAfterRebuildStructures`, `InitializeCellItems`, `GetDataSourceItemCount`, `InitializeBase`, `RecalculateContentSize`, `RebuildVisibleCells`, `GetViewportSize`, `CalculateVisibleCountForViewport`, protected `cellItems` / `cellLines` | — | **Dropped.** The work lives in the internal `SlotListEngine<T>` now. |

---

## 16. Compared with Unity's Scroll View (GameObject > UI > Scroll View)

| | Unity's Scroll View | Slot List |
|---|---|---|
| How you start | Create the Scroll View, then fit it to your layout | Create an empty UI GameObject with the size and anchors you want, then add your list component |
| Root | `ScrollRect` + a visible, sliced background `Image` | Your list (a `ScrollRect` subclass) + an **invisible** raycast `Image`, so drags on empty space scroll |
| Viewport | `Image` + **`Mask`** (stencil), inset to leave room for the scrollbars | Stretched, with **`RectMask2D`** (rectangular clipping without stencil or an extra draw call; slots outside it are culled), a **nested `Canvas`** (scrolling rebuilds only the list's batches, not the screen's) and its own **`GraphicRaycaster`**. Better for mobile and web. |
| Content | Anchored to the top, a fixed starting height; you usually add a `VerticalLayoutGroup` + `ContentSizeFitter` | Created stretched, then anchored from the **Direction**. **No layout components allowed:** the list sets and changes Content's size itself (measuring, expanding, collapsing, paging). Enforced in three places (§14). |
| Scrollbars | Horizontal and vertical, with handles, referenced by the ScrollRect | None created or referenced (touch and web focus) |
| Scroll axis, movement | Set by hand on the ScrollRect | The axis follows the inspector's Direction; ScrollRect's own settings are hidden |
| Items | One GameObject per item, all alive | Only visible slots exist, reused from a pool |

---

## Issues found in the original and fixed

Everything below was in 0.3.0 and is fixed on this branch. Most have a test that failed before the fix. The numbered ones are rows of plan §6.

**Correctness**

| # | Issue in 0.3.0 | Fix | Test |
|---|---|---|---|
| 6.1 | `Initialize` with 0 items returned early: `IsInitialized` stayed false, `Refresh()` was refused, and a paged list could never start | Initialization works with 0 items; `SetSource` works before or after `Initialize` | Yes |
| 6.2 | `ScrollToItem(500)` showed item 492 (top to bottom) or 496 (right to left): the position was mirrored | The item lands at the start in every direction | Yes |
| 6.3 | `ExpandCell(i)` without a size expanded to **0** after the inspector's Single mode (`estimatedCellHeight × 2` with the estimate at 0) | Falls back to the item's own size × 2 | Yes |
| 6.6 | Resizing the list was only handled across the axis, and only for automatic grids | Any viewport resize re-places slots and recomputes the range | Yes |
| 6.9 | `hasReachedLastPage` was never set for paged sources | `HasReachedLastPage` turns true at the end | Yes |
| 6.10 | `Refresh()` collapsed every expanded item and reset the visible range even when nothing structural changed | `Refresh()` keeps expansion; only `SetSource` clears it | Yes |
| 6.12 | `ExpandCell` in a grid did nothing, silently | Logs one warning | Yes |
| 6.13 | `VisibleStartIndex` / `VisibleEndIndex` returned line indexes while `IsItemVisible` took item indexes, so they disagreed in grids | Item indexes everywhere | Yes |
| 6.14 | Error handling was inconsistent: bad indexes threw `IndexOutOfRangeException`, the paged source returned null, and a ScrollRect without Content threw from `Initialize` | `ArgumentOutOfRangeException` for bad indexes everywhere; bad setup logs an error and leaves the list uninitialized | Yes |
| 6.15 | The first build and the pool warm-up waited for `WaitForEndOfFrame`, which never resumes in batch mode (CI, headless), and resumes after rendering, so the first frame was blank | Waits one frame | Yes |
| 6.16 | Dragging a list before `Initialize` threw `NullReferenceException` | Ignored safely | Yes |
| — | A returned header slot could go back into the cell queue and be reused as a cell | One queue per slot kind (`SlotPool`) | Yes |
| — | The item-to-line mapping assumed `index / gridCount`, wrong as soon as a header took a line | An explicit item-to-line map in `LayoutModel` | Yes |
| — | `IsHeader` entries in `RecyclableScrollRect<T>` never received the Header Prefab | Headers moved to `SectionedSlotList`, with their own prefab and pool | Yes |
| — | `PaginatedDataSource.GetItem` started page loads as a side effect, and a failed page was only a `Success = false` flag nobody saw | Loads are started by the list; failures throw and raise `PageFailed` / `LoadFailed` (logged when nobody listens) | Yes |
| — | Adding the component always set the ScrollRect to vertical, whatever the direction | Reads the direction from the settings | Yes |

**Inspector**

| # | Issue in 0.3.0 | Fix |
|---|---|---|
| 6.4 | "Scroll Type" was guessed from `estimatedCellHeight > 0` (default 100), and choosing Single overwrote the estimate with 0 (the cause of 6.3) | Removed; a List / Grid / Auto Grid dropdown instead |
| 6.5 | Header Prefab and the automatic grid fields were hidden, so automatic grids couldn't be set up from the inspector | Everything the list uses is drawn; ScrollRect's own settings stay hidden on purpose (your decision) |
| — | The pool help box disagreed with the real default and range | Fixed (140% default, 1.4 to 3.0) |

**Performance**

| # | Issue in 0.3.0 | Fix |
|---|---|---|
| P1 | Finding the visible lines checked them one by one, twice per scroll event | Binary search |
| P5 | 4–5 `GetComponent` calls per bind | Cached once per slot (`SlotHandle`) |
| 6.11 / P15 | `FilteredDataSource.GetItem` scanned the whole source on every call | Matches cached; `Invalidate()` after source changes |
| P6 | A recycled cell was re-parented outside every canvas and back, rebuilding its graphics | Slots stay inactive under Content |
| P7 | `Refresh()` returned every visible cell to the pool and took it back | Slots are rebound in place |
| P13 | Every size change recomputed every line offset | Offsets recomputed from the lowest changed line |
| P14 | The size calculator computed every size up front | Sizes measured lazily (the calculator is gone) |
| — | Each `CellLine` created `gridCount` throw-away `CellItem`s | Removed |

**Not fixed, decided otherwise**
- 6.7 cross-axis padding: built, then removed at your request (it was a new concept, not a bug).
- 6.8 the calculator always got the width, even in horizontal lists: moot, the calculator is gone.

---

## What was dropped

| Dropped | Why | Replacement | Worth bringing back? |
|---|---|---|---|
| **Size calculator** (`ICellSizeCalculator<T>`, `Initialize(source, calculator)`): a different fixed size per item from data | Your top usability complaint. Replaced by layout measuring. | `DefaultExtent <= 0` plus a layout group | **Decide.** It's the only real capability lost. A per-item size override could return as an optional virtual. |
| Expansion hooks `OnCellExpanded`, `OnCellCollapsed`, `GetExpandedSizeForCell` | Engine extraction | The slot reacts to `IsExpanded` and `OnCollapsed()` | Only if you used them: they could return as events |
| Rebuild hooks `OnBefore/AfterRebuildStructures` and the other protected internals | Engine extraction | — | Probably not |
| The 96% scroll trigger for paging | Replaced by "10 items left" | `OnPaginationThresholdReached()` still runs | No |
| `estimatedCellHeight`, `cellsPerFrame` settings | Went with the calculator | The average of measured sizes | No |
| `IPaginatedScrollSource<T>`, `RecyclableScrollConfig`, `GetEstimatedHeaderCount`, `CellLinePool`, and ~10 other unused classes | Unused | — | No |
| Non-generic `IRecyclableDataSource` | Only `EmptyStateDisplay` used it | — | No |
| UI Toolkit adapters; the `Core`, `Abstractions` and `UGUI` layers | Unfinished duplicates | — | No (your decision) |
| Public `scrollStateManager` field | Exposed internals | `VisibleStartIndex` / `VisibleEndIndex` | No |

**Moved, not dropped:** `PullToRefreshUI` → `PullIndicator` and `EmptyStateDisplay` → `EmptyStatePanel`, both in the Extras sample.

---

## The tool as it is now

### The flow

```
Your code ──SetSource / Refresh──► SlotList<T> (a ScrollRect, thin)
                                     │ forwards everything
                                     ▼
                              SlotListEngine<T> (internal, partial files)
   IItemSource<T> ──count, items──►  LayoutModel: Placement per item → Lines → offsets → total extent
                                     │ on scroll
                                     ▼
                              VisibilityPass: binary search → first/last visible line
                                     ▼
                              SlotRecycler: give back slots of lines that left,
                                            take slots for lines that entered (SlotPool),
                                            place them (IAxisLayout), bind them (ISlot<T>.Data)
                                     ▼
                              measured slots: Measure → LayoutModel size → offsets → anchoring
                              SlotSizeWatcher reports later changes → applied in LateUpdate
```

### Five rules that explain most behaviour

1. **`DefaultExtent > 0` is a fixed size, `<= 0` is measured with layout.** It's read once from the prefab.
2. **Change the data, then `Refresh()`.** Paged sources append by themselves. Sectioned sources recount on `Refresh()`.
3. **Indexes are item indexes everywhere.** In a sectioned list they're flat entry indexes, counting headers. Use `Locate` / `FlatIndexOf` to convert.
4. **What's on screen stays still** when sizes change: anchoring.
5. **Slots are reused.** Reset every visual in the `Data` setter.

### Where things live (`Runtime/`)

| Area | Files |
|---|---|
| Components | `SlotListBase.cs` (shared API, events, Unity callbacks), `SlotListBase.Setup.cs` (settings drive the ScrollRect, editor setup), `SlotList.cs`, `SectionedSlotList.cs`, `SlotListSettings.cs` |
| Engine | `SlotListEngine.cs` (setup, binding), `.Sizing.cs` (sizes, measuring, anchoring), `.Rebuild.cs` (refresh, rebuilds, lifecycle), `.Interaction.cs` (scroll, drag, navigation), `.Paging.cs` |
| Layout and visibility | `LayoutModel.cs`, `Line.cs`, `Placement.cs`, `VisibilityPass.cs`, `VisibleRangeFinder.cs`, `VisibleRangeTracker.cs`, `SlotsPerLineResolver.cs`, `PoolSizeEstimator.cs`, `AxisMetrics.cs`, `Directions/` |
| Slots | `ISlot.cs`, `ISlotList.cs`, `SlotHandle.cs`, `SlotPool.cs`, `SlotRecycler.cs`, `SlotSizeWatcher.cs`, `ExpansionState.cs`, `AppearAnimationPolicy.cs`, `IAppearAnimation.cs` |
| Sources | `IItemSource.cs`, `IItemSourceProvider.cs`, `ListSource.cs`, `FilteredSource.cs`, `PagedSource.cs`, `IPageLoader.cs`, `SectionedSource.cs`, `ISectionedSourceProvider.cs`, `SourceGuard.cs` |
| Gestures and plumbing | `PullGesture.cs`, `DragState.cs`, `ScrollMover.cs`, `ScrollRectBinding.cs`, `EngineCallbacks.cs`, `ISlotListEngine.cs`, `SlotListMarkers.cs` |

0.3.0 had about 100 C# files in 6 assemblies, with two overlapping architectures. Slot List has 51 files in one runtime assembly.

### Tests

249 tests: 138 EditMode and 111 PlayMode, run by `MyTools/scripts/verify.sh`. They cover:
- the original behaviour, pinned in Phase 0;
- one test per fixed bug;
- design tests written before each new part;
- Phase 10 regression tests: parent changes, `Refresh` reuse and 0 B allocated while scrolling.

---

## Decisions on the open questions (2026-11-13)

1. **Sizes.** No size calculator. If the slot defines its size in code (`DefaultExtent > 0`), that size is used. If not, the list reads the size Unity's layout gives the slot after binding, and grows or shrinks Content to match. That's what Slot List does today. One small option stays open: read `DefaultExtent` from the bound slot instead of only from the prefab, so a slot could return a size that depends on its data. That needs no new API.
2. **Hooks.** None of the old ones were overridden. **Done:** read-only extension points replace them: the events `ItemExpanded`, `ItemCollapsed` and `Rebuilt`, and `GetItemOffset(index)`, `GetItemSize(index)`, `ViewportSize`, `ContentSize`. The internal steps (`InitializeCellItems`, `RebuildVisibleCells`…) stay out.
3. **Sections and paging.** Paging just appended items and grew Content. `SectionedSlotList` doesn't page, and that's fine for now.
4. **Line, not lane.** `Lane` is now `Line` everywhere: `Line`, `slotsPerLine`, `lineSpacing` (the original's name), `autoSlotsPerLine`, `minSlotsPerLine`, `maxSlotsPerLine`, `SlotsPerLineResolver`. Serialized fields renamed, so re-check slots per line and line spacing on existing lists.

## Loose ends

1. **Public surface wider than needed:** engine pieces such as `LayoutModel`, `Line`, `Placement`, `SlotPool`, `VisibleRangeFinder`, `VisibleRangeTracker`, `AxisMetrics`, `ScrollMover`, `DragState`, `PullGesture`, `AppearAnimationPolicy`, `SlotsPerLineResolver`, `ScrollRectBinding`, `SourceGuard<T>` and the direction layouts are still `public`, as they were in 0.3.0. `SlotHandle` has to stay public (`BindItem` takes it). The rest could become `internal` before 1.0.
