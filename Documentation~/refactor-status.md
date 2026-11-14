# Refactor status: Recyclable Scroller → Slot List

Where the refactor stands against [`refactor-plan.md`](refactor-plan.md): what each phase did, what changed from the plan, what was skipped, and what's left. Updated 2026-11-13, after Phase 9 step 2.

## Summary

- **Phases 0 to 8 are done. Phase 9 is done in code**; its manual `Sections` scene is missing. Phase 10 is done and measured on two phones. Phases 11 and 12 haven't started.
- **243 tests pass** in batch mode (138 EditMode, 105 PlayMode), through `MyTools/scripts/verify.sh`. No test is ignored.
- **Nothing is merged or pushed.** Every phase has its own branch, but each branch starts from the previous one instead of from `main`. `refactor/p9-sections` holds all the work. `main` is still at 0.3.0 plus the plan (`79b4127`).
- The package is `com.sgonzalezrozo.slot-list` `1.0.0-preview.1`, namespace `SlotList`. The folder and the GitHub repository still use the old name.
- MyTools (the test project) compiles against it, including the Shop scroller and Fast IoC's generated code.

| Phase | Status | Branch (last commit) | Tests at the end |
|---|---|---|---|
| 0 Safety net | Done, except the manual checklist file | `refactor/p0-safety-net` (`7feb0ca`) | 104 (96 + 8 ignored bug tests) |
| 1 Remove UI Toolkit | Done | `refactor/p1-remove-uitoolkit` (`14ec9ab`) | 104 |
| 2 Remove the duplicate architecture | Done | `refactor/p2-remove-duplicates` (`135db66`) | 104 |
| 3 Remove unused code | Done | `refactor/p3-remove-unused` (`da709ff`) | 104 |
| 4 Rename | Done, renamed twice (Reel, then Slot List) | `refactor/p4-rename` (`dbdef92`), `refactor/p4b-slotlist` (`87d47da`) | 104 |
| 5 Split the god classes | Done, with a simpler structure than planned | `refactor/p5-split` (`5726db9`) | 142 |
| 6 Fix the known bugs | Done; two rows dropped or reversed by the owner | `refactor/p6-bugs` (`52027fc`) | 164 |
| 7 Pagination | Done; footer and top loading left out | `refactor/p7-paging` (`ad8a94e`) | 176 |
| 8 Layout sizing | Done; measuring ahead and the scenes left out | `refactor/p8-layout-sizing` (`3aa980e`) | 219 |
| 9 Sections | Code and docs done; scene missing | `refactor/p9-sections` (`c7a184e` + docs) | 243 |
| 10 Performance | Not started (P1 and P5 done in Phase 5) | | |
| 11 New features | Not started | | |
| 12 Tests, samples, docs, release | Not started (README kept up to date as we went) | | |

## Owner decisions along the way

These override the plan where they differ.

- **Names:** every type renamed, internal ones included. First to **Reel**, then to **Slot List**, with cells called **slots**. `FormerlySerializedAs` is never used: the owner re-attaches serialized references by hand.
- **UI Toolkit:** removed entirely.
- **Pagination:** kept. The footer and loading at the top wait until asked for.
- **Inspector:** minimal. No ScrollRect section, no Advanced foldout. The list's settings drive the ScrollRect, which stays hidden. No new concepts the original didn't have.
- **Cross-axis padding:** removed again after it was added (bug 6.7). It was a new concept, and the original never had it.
- **Sizing:** no sizing mode setting. `DefaultExtent > 0` on the slot is a fixed size; `<= 0` (for example `-1`) is measured with Unity's layout.
- **Headers:** only in `SectionedSlotList<TItem, THeader>`. `SlotList<T>` lost Header Prefab and `IsHeader`.
- **Sections code:** the owner chose not to share the old `CompositeDataSource`. Phase 9 was built from the plan's API.
- **Out of scope:** nested scrollers (handled by an MIT plugin).
- **Wanted later:** a looping carousel (Phase 11).

## Phase by phase

### Phase 0: Safety net

**Done:**
- Tag `v0.3.0-baseline`.
- Test assemblies `Tests/Editor` and `Tests/Runtime`, the rig that builds a list on a Canvas in code (`SlotListRig`, was `ScrollerRig`), recording slots and `TestItem`.
- 66 EditMode and 30 PlayMode characterization tests pinning the original behaviour, plus 8 known bugs as ignored tests. Each bug test was run once and failed.
- `verify.sh` in MyTools runs EditMode and PlayMode in batch mode, and the Stop hook runs it when an agent finishes.
- One production fix, needed for the PlayMode suite to run at all (bug 6.15): the first build waited for `WaitForEndOfFrame`, which never resumes in batch mode.

**Skipped or changed:**
- `Documentation~/behaviour-checklist.md` (0.4) was never written. Manual checks were done in `Bootgame` and in the playground, and described in the conversation.
- Copying the sections code into MyTools (0.2) never happened: the code wasn't available. The sections characterization test (0.7) didn't exist, so Phase 9 had nothing to port.
- MyTools didn't follow the package branches. It's on `refactor/p8-layout-sizing` with uncommitted changes (see *Housekeeping*).
- The plan's checkboxes for Phases 0 to 7 were never ticked, even though the work is done. This document and the "What Phase N applied" notes are the record.

### Phase 1: Remove UI Toolkit

**Done:** `UIToolkit/` deleted. Nothing referenced it. Docs cleaned.

**Changed:** nothing.

### Phase 2: Remove the duplicate architecture

**Done:**
- The direction classes moved into `Runtime/Directions`, without the two methods that tied them to `Core`.
- `Core`, `Abstractions` and `UGUI` deleted. The asmdefs were fixed.

**Changed:** the plan's 14 manual scenes became `Bootgame` plus scenes added when needed, since the PlayMode suite already covers what they were meant to check.

### Phase 3: Remove unused code

**Done:**
- Ten unused classes and the unused members in 3.2 removed.
- Editor comments translated, and the pool help box fixed (140% default, 1.4 to 3.0 range).
- `PullToRefreshUI` and `EmptyStateDisplay` moved to a new `Samples~/Extras` (as `PullIndicator` and `EmptyStatePanel`). The runtime no longer references TextMeshPro.

**Changed:**
- `IScrollSource` was kept, as the plan said. It's now `IItemSourceProvider<T>`.
- Bug 6.16 was found here (dragging before `Initialize` threw) and fixed in Phase 5.

### Phase 4: Rename

**Done:**
- Every type renamed, **including the internal classes the plan had left for Phase 5**, so Phase 5 only had to merge and split.
- Members that used the old vocabulary renamed. `SetSource` added; it works before initialization.
- No "cell" is left in code, tests, samples or docs.

**Changed from the plan:**
- **Renamed twice.** The first pass used **Reel** (`com.sgonzalezrozo.reel`, `ReelView`). The owner didn't like it, so a second pass made it **Slot List**: `SlotList<T>`, `ISlot<T>`, `IExpandableSlot<T>`, `SlotPool`, `slotPrefab`. The internal position record is `Placement`.
- `IExpandableSlot.Owner` replaced `ScrollRect`.
- Kept on purpose:
  - `injectedInstantiator`, because Fast IoC's generated code writes to it.
  - The MyTools class names (`ShopScrollerRecyclableScrollRect`).
- Neutral names were kept (`ScrollToItem`, `ItemCount`, `IsInitialized`, `VisibleStartIndex`...). The plan's `FirstVisibleIndex` / `LastVisibleIndex` were never applied: bug 6.13 fixed what those properties return instead of renaming them.

**Not done:** renaming the GitHub repository and the folder (`unity-recyclable-scroller`). The README links still point to the old repository.

### Phase 5: Split the god classes

**Done:**
- **`SlotListSettings`** holds every serialized setting. `SlotListDirection` replaced the two booleans.
- **`LayoutModel`** merges lane building, offsets and total extent, with an item-to-lane map that's correct with headers.
- **`VisibleRangeFinder`** uses a binary search (perf item P1).
- **`SlotPool`** keeps one queue per `SlotKind`, so headers no longer end up in the item queue. **`SlotHandle`** caches components once per slot (perf item P5). `SlotRecycler` merges the two old recycler and displayer classes.
- **`ExpansionState`** merges the old controller and service.
- **`SlotListEngine<T>`** (internal, partial files) holds the work. `SlotList<T>` became a thin component, talking to the engine through small internal interfaces instead of stored delegates.
- Bug 6.16 fixed.

**Changed from the plan:**
- **No folders.** The plan's `Engine/`, `Layout/`, `Sizing/`, `Sources/`, `Interaction/` and `Views/` folders were not created. `Runtime/` is flat (42 files), with only `Directions/` as a subfolder.
- **`SlotListSettings` is a class**, not a struct.
- **No `RebuildLevel` enum.** The plan wanted one shared `Apply(RebuildLevel)`. The engine has separate paths instead (refresh, structure rebuild, visible-only rebuild, append). Bug 6.10 was fixed without it.
- `LaneBuilder` and `DeferredWork` aren't separate classes: they're inside `LayoutModel` and the engine's `Rebuild` partial.
- `PagingTrigger` was removed in Phase 7, not moved.
- **File sizes have grown past the plan's limit of about 300 lines** with Phases 8 and 9: `SlotListEngine.cs` has 526, `SlotListBase.cs` 425 (about 110 of them editor setup code), `LayoutModel.cs` 332 and `SlotListEngine.Rebuild.cs` 313. They need another split (see *Still to do*).

### Phase 6: Fix the known bugs

**Done:** bugs 6.1, 6.2, 6.3, 6.4, 6.6, 6.9 (in Phase 7), 6.10, 6.11, 6.12, 6.13, 6.14, 6.15 (in Phase 0) and 6.16 (in Phase 5). Every bug test was un-ignored, seen to fail and then fixed.

**Changed from the plan:**
- **6.5 was reversed by the owner.** The plan showed ScrollRect's own settings in a foldout, and that was built. The owner found setup tedious and switching to a grid almost impossible, so the inspector was redone:
  - It's minimal: prefabs, Direction, a Layout dropdown (List / Grid / Auto Grid) with its fields underneath, spacing and padding labelled for the direction, and the behaviour toggles.
  - ScrollRect's settings are hidden; the direction sets the ScrollRect's axis and Content's anchors, in the editor too.
  - Two IMGUI problems were fixed along the way: focus was lost when a row was inserted above the field being edited, and choosing List was undone.
- **6.7 (cross-axis padding) was built and then removed** by the owner: a new concept the original never had.
- 6.8 wasn't needed: the size calculator was deleted in Phase 8.

### Phase 7: Pagination

**Done:**
- `IPageLoader<T>.LoadAsync(page, ct)` returns `PageResult<T>(items, hasMore)` and throws on failure.
- `IPagedSource<T>` and a rewritten `PagedSource<T>`, with events, cancellation and `Reset`.
- **The list loads pages by itself**: the first page of an empty source, then the next one when fewer than 10 items are left after the last visible one. Pages are appended without a rebuild, keeping the scroll position and expanded items. One load at a time; changing the source or destroying the list cancels a load in flight.
- Events `PageRequested`, `PageLoaded`, `PageFailed` and `ReachedEnd`. Bug 6.9 fixed.

**Changed from the plan:**
- The threshold is a **constant (10)**, not an inspector setting, to keep the inspector minimal.
- No `PagingTrigger` class: the check lives in the engine's `Paging` partial.
- No `PullRefreshRequested` event: pull to refresh still calls the `OnPullToRefreshDetected()` override.

**Skipped (until the owner asks):**
- The optional footer (Loading, Failed with Retry, End), because it adds a Footer Prefab to the inspector. The PlayMode test for it was not written.
- Loading at the top, for chat lists.
- The `Gestures` manual scene.

### Phase 8: Layout sizing

**Done:**
- **The size calculator is gone** (`IItemSizer<T>`, `ItemSizerBase<T>`, `SizeCoordinator`, `Initialize(source, calculator)`, and the hidden `estimatedItemSize` and `measureBudgetPerFrame` settings).
- **Sizes come from the slot's `DefaultExtent`:** above 0 it's a fixed size; 0 or less is measured on the real instance with `LayoutRebuilder.ForceRebuildLayoutImmediate` and `LayoutUtility.GetPreferredSize`, at the slot's real width.
- `LayoutModel` stores a size per item, the measured flags and the running average, and recomputes offsets only from the lowest changed lane.
- Items not measured yet use the **average of the measured ones**. A grid row is as tall as its tallest slot.
- A root with no layout group keeps the prefab's size and logs one warning. The owner found this case: slots collapsed to 0 before.
- **`SlotSizeWatcher`** (`ILayoutSelfController`), added by the list to measured slots only, catches size changes after binding: late strings, loaded images, children turned on or off.
- **Anchoring:** what's on screen stays still.
  - Items measured for the first time while scrolling up grow upwards.
  - An item whose content changed keeps its top.
  - During a drag, a shift made by code restarts the drag from the pointer's current position and causes no jump or extra speed.
- `InvalidateSize(i)` and `InvalidateAllSizes()`. A width change invalidates every size, and so does a language change when Unity Localization is installed (version define `SLOTLIST_LOCALIZATION`, no hard dependency).
- **Expandable measured slots size themselves.** `Expand` and `Collapse` only set `IsExpanded`. Expanded items stay out of the average. A size given to `Expand(index, size)` or `SetExpandedSize` is ignored for measured slots, with a warning; fixed-size slots keep the explicit size.
- README recipe for a slot that sizes itself; agent rules updated.

**Changed from the plan:**
- **No `SizingMode` setting** (`Auto` / `Fixed` / `Layout`, 8.1). The owner chose the `DefaultExtent` rule, with no inspector field.
- **No `estimatedItemSize` override.** The placeholder is the average, or the prefab's own size before anything is measured.
- **The watcher's changes apply one frame later**, in `SlotList.LateUpdate`, not in the same frame. Resizing during Unity's layout pass logs errors. The plan had already allowed this fallback.
- **Anchoring rule:** the anchor is the first visible lane not measured for the first time in that pass.
- **Drag correction:** `UpdatePrevData()` replaces the plan's "save and restore velocity around `base.LateUpdate`". It stops the speed jump at its source.
- **Tests:**
  - The PlayMode tests use a `LayoutElement` body, not real TMP text, so the test assembly has no TMP dependency.
  - The anchoring maths is tested in PlayMode, not EditMode.
  - The localization hook has no automated test: it needs real locales.
- No separate cache for the expanded size: a slot is measured again whenever it's rebound.
- Expanded items no longer count toward the average (found by a failing test).

**Skipped:**
- **Step 6, measuring ahead with a hidden copy** (8.3 B), marked optional. With it, the scrollbar and `ScrollToItem` would be accurate before items are seen.
- **The expand animation** (8.6), marked optional.
- **The six sizing scenes** S1 to S6 (8.8). Instead there's one playground in MyTools, `Assets/SlotListTests/Sizing/SizingPlayground.cs`: 500 rows of text, buttons to jump, grow and shrink rows, and an automatic mode that changes rows while you drag. The owner checked it by hand. It's not committed in MyTools, and there's no localized-text scene.

### Phase 9: Sections

**Done:**
- **`SectionedSource<TItem, THeader>`:**
  - Sections: `AddSection`, `InsertSection`, `RemoveSection` and `ClearSections`.
  - Reading entries: `Count`, `SectionCount`, `KindAt`, `HeaderAt`, `ItemAt`.
  - Converting indexes: `Locate` (a binary search over header positions) and `FlatIndexOf`.
  - `SkipEmptySections` (true by default), `Recount` and `Changed`.
  - Plus `EntryKind` and `ISectionedSourceProvider<TItem, THeader>`.
- **`SectionedSlotList<TItem, THeader>`:**
  - Header Prefab (`ISlot<THeader>`) with its own pool; item slots use `ISlot<TItem>` or `IExpandableSlot<TItem>`.
  - `SetSource` (starts or replaces, with no `IsInitialized` check), `ScrollToSection` (skips empty sections), and `Refresh()` recounts.
  - A header takes a whole row in a grid. Header sizes are fixed or measured like slots, and stay out of the item average.
  - A clear error when the Header Prefab is missing.
- **`SlotListBase`** now holds what both lists share and talks to the engine through the internal `ISlotListEngine`. Its constructor is `private protected`, so no new public base type was added and nobody outside the package can derive from it. Serialized field names didn't change, so existing scenes keep their values.
- **Breaking:** `SlotList<T>` lost Header Prefab and `IsHeader` (owner's decision). The ignored `Phase9_IsHeader…` test was removed.
- README "Sections and headers" section, API table, CHANGELOG, agent rules, `llms.txt`, and migration notes in plan 9.4.

**Changed from the plan:**
- Inner source changes are picked up by **`Recount()`**, called from `Refresh()`. There's no `NotifySectionChanged`, and no listening to inner sources, which raise no events. The rule stays "change the data, then `Refresh()`".
- `SectionedSlotList` derives from `SlotListBase`, as planned. To get there, all the shared code moved from `SlotList<T>` into `SlotListBase`.
- `SectionedSlotList` doesn't page.

**Skipped:**
- **The `Sections` manual scene** in MyTools (headers in a list and in a grid): it's part of the plan's "done when".
- Sticky headers (optional).
- Porting the old sections tests: they never existed in MyTools.

## Still to do

### To close Phase 9
- [ ] A sections playground in MyTools (headers in a list and a grid, empty sections, `ScrollToSection`), checked by hand.
- [ ] Optional: migrate the other project from `CompositeDataSource` using the notes in plan 9.4.

### Housekeeping (any time, before release)
- [ ] **Decide how to merge.** The plan said one pull request per phase into `main`, but the branches are stacked. Options: one pull request from `refactor/p9-sections` into `main`, or rebase the phases into separate pull requests. Nothing has been pushed.
- [ ] **MyTools:**
  - Commit the playground (`Assets/SlotListTests/`).
  - Commit or discard the other changes waiting there (`Bootgame.unity`, the Shop scripts and prefabs, the Fast IoC generated files, `packages-lock.json`).
  - Switch it to the branch that matches the package.
- [ ] **Split the large files again** (Phase 5's limit of about 300 lines):
  - `SlotListEngine.cs` (526 lines): move sizing and measuring into their own partial or class.
  - `SlotListBase.cs` (425 lines): move the editor setup code into a partial.
  - `LayoutModel.cs` (332 lines) and `SlotListEngine.Rebuild.cs` (313 lines).
- [ ] **Tick the plan's checkboxes** for Phases 0 to 7, or point them to this document.
- [ ] **README:**
  - The version badge says 0.3.0, but the package is `1.0.0-preview.1`.
  - The links point to the old repository name.
- [ ] Write `Documentation~/behaviour-checklist.md` (Phase 0.4), or drop it in favour of the playgrounds.
- [ ] Rename the GitHub repository and the folder to `unity-slot-list`, and update `manifest.json` in MyTools.

### Left out on purpose (until the owner asks)
- The paging footer (Loading / Failed with Retry / End) and loading at the top (chat lists). From Phase 7.
- Measuring ahead with a hidden copy, the expand animation, the localized-text scene and S1 to S6 as real scenes. From Phase 8.
- Sticky headers. From Phase 9.

### Phase 10: Performance (done, measured on two phones)
Branch `refactor/p10-performance`. Done, each with tests on counts or allocations (see `benchmarks.md` for the before/after table):
- Profiler markers `Slot List.*`.
- **P6** slots never leave Content. **P7** `Refresh()` rebinds in place.
- **P2** only shown slots are checked. **P3** slots that didn't move aren't placed. **P4** sub-pixel scrolls skip the range work.
- **P18** the Viewport is a nested Canvas with its own GraphicRaycaster (setup and Initialize).
- **P8** disable/enable keeps slots. **P9** warm-up two slots per frame. **P10** first screen bound inside `Initialize` (`OnInitialized` fires there now).
- **P11** reusable layout buffers: rebuilds and pages only create what's new; `Refresh()` same count allocates 0 B. **P12** content size from offsets.
- Measured sizes cached (the old P20); placeholder passes only while the average moves.
- Along the way (owner's requests): `Lane` → `Line`; read-only extension points; Content can't have layout components; the add-component setup has tests.

Not done:
- **Timings:** recorded in `benchmarks.md` for the Editor, a Galaxy A02s and a Pixel 5, with the Bootgame Shop list (fixed and expandable slots, 300 to 100,000 items, fling, 2-minute soak, late text). There's no timing baseline from before Phase 10; the bench came at the end.
- **Added after the first device runs:** layout buffers shrink after a big drop in item count; pooled slots stay active, parked out of view (the default, about three quarters off the list's cost); fixed slots measured while expanded; opened slots measured in Unity's layout pass; `ContentSizeChanged` and `ExpandedCount`; Content can't have layout components (enforced).
- **P19:** a README tip instead of an inspector helper (minimal inspector). **P21** (deferred binding during flings) waits for the owner. **P22** (float precision past ~10⁷ px) only if the bench shows jitter.
- Phase 10's tests for P2–P4 and P8–P11 were written with their fixes, not seen failing first (the placeholder test was checked by turning the fix off).

### Phase 11: New features (not started)
- `ScrollTo(index, align, duration, offset)` with `ScrollAlign` (Start, Center, End, Nearest) and an animation.
- Snapping (`None`, `Item`, `Page`) with a `SnappedTo` event.
- **The looping carousel** (wanted by the owner).
- Nested scrollers: only a manual check with the MIT plugin.

### Phase 12: Tests, samples, docs, release (not started)
- Samples: Grid, Sections, Chat, Expandable, LayoutSizedText, Carousel, and an update of QuickStart. Today only QuickStart and Extras exist.
- `Documentation~/migration-from-recyclable-scroller.md`. Today the CHANGELOG has the name table, and plan 9.4 has the sections notes.
- **Update the AI rules repository** (`unity-architecture-agents`): its docs still describe Recyclable Scroller's API and package name. Also settle the naming clash between `ISlot<T>` and MVC Models. Then bump the `.ai` submodule in MyTools.
- An IL2CPP build, Play mode with domain reload off, the compatibility table, and the `v1.0.0-preview.1` tag.

## Public API today

```csharp
// A plain list
public class InventoryList : SlotList<InventoryItem> { }
list.Initialize(new ListSource<InventoryItem>(items));   // or list.SetSource(...), before or after initializing

// A list with sections
public class EventsList : SectionedSlotList<EventItem, EventHeader> { }
SectionedSource<EventItem, EventHeader> source = new();
source.AddSection(header, new ListSource<EventItem>(items));
list.SetSource(source);

// Slots
public class InventorySlot : MonoBehaviour, ISlot<InventoryItem>
{
    public float DefaultExtent => -1f;   // > 0 fixed, <= 0 measured with layout
    public InventoryItem Data { get; set; }
}
```

- **Shared API** (both lists): `Refresh`, `Clear`, `ScrollToItem`, `ScrollToStart`, `ScrollToEnd`, `ForceStopScrolling`, `Expand`, `Collapse`, `SetExpandedSize`, `IsExpanded`, `IsItemVisible`, `InvalidateSize`, `InvalidateAllSizes`, `GetItemCount`, `IsInitialized`, `IsScrolling`, `IsDragging`, `VisibleStartIndex`, `VisibleEndIndex`, `IsFirstItemVisible`, `IsLastItemVisible`, `Settings`, `SlotPrefab`.
- **Events:** `OnInitialized`, `PageRequested`, `PageLoaded`, `PageFailed`, `ReachedEnd`.
- **Sources:** `ListSource<T>`, `FilteredSource<T>` (with `Invalidate()`), `PagedSource<T>` with `IPageLoader<T>`, `SectionedSource<TItem, THeader>`. Providers: `IItemSourceProvider<T>`, `ISectionedSourceProvider<TItem, THeader>`.
- **Overridable on `SlotList<T>`:** `BindItem`, `SupportsPagination`, `OnPaginationThresholdReached`, `OnPullToRefreshDetected`, `DataSource`, `HasReachedLastPage`.
- **Overridable on `SectionedSlotList`:** `BindItem`.
