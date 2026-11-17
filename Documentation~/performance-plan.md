# Performance and adoption plan

Two goals, from the point of view of a senior developer at a studio that needs a recycling scroll view and finds UI Scroller:

1. **An honest assessment:** what fits, what doesn't, which features are missing and what the risks of adopting it are.
2. **Performance that's zero GC and as cheap as it can be, and proven:** a baseline, measured improvements, and numbers anyone can reproduce.

Status: in progress. Branch: `perf/zero-gc-baseline`. Order: README rewrite first, then P1.

## Decisions (P0, 2026-11-16)

1. **Our own measurements, no extra packages.** The timing bench uses `ProfilerRecorder`, the `UI Scroller.*` markers and its own statistics (median, p95, max). No Performance Testing package.
2. **Fully automated runs on devices**, for the baseline (P2) and the final result (P7) only:
   - Samsung Galaxy A02s and Google Pixel 5 (Android, IL2CPP);
   - iPhone 13 mini and iPhone 17 Air (iOS, IL2CPP);
   - WebGL.

   A build starts the bench by itself, runs every case, and reports the results without anyone touching the device. The Editor is used in between.
3. **Comparisons:** Recyclable Scroller 0.3.0 and a plain `ScrollRect` with every item instantiated, where they can run the same case.
4. **C7 waits** for P5's measurement.
5. **The assessment is published** in `Documentation~`.
6. **A fuller README rewrite** comes first.

## P1 results: allocation baseline (2026-11-16)

`Tests/Runtime/AllocationTests.cs`: 54 layout × operation cases on `main` at `87c4f0b`, Editor (Unity 6000.3.6f1), quiet slots, two warm-up runs before each check. **43 allocate nothing, 11 allocate.** The 11 are marked `Ignore("P3: …")` until P3 fixes them.

**Zero today, in every layout tried** (list, grid, auto grid, bento spans and patterns, sections in list, grid and bento, short and long loops, expandable):
- scroll, fling (40 frames each way), `ScrollToItem` at once and animated, `ScrollToSection`;
- snap (item and page);
- `Refresh()` with the same count;
- `Refresh()` after adding and removing an item (list and grid);
- expand and collapse.

**Allocates today:**

| Case | Layouts | Cause (from the code; P3 confirms) |
|---|---|---|
| Disable and enable | List, bento, sections | Under investigation. Suspected: the locale listener subscribes a new delegate on every enable (Unity Localization is installed in MyTools) |
| Width change | List, auto grid, bento | `OnViewportResized` starts a coroutine |
| `Refresh()` after a section grows | Sections, sections bento | `SectionedSource.Recount` allocates two arrays |
| `Refresh()` after adding and removing an item | Bento | A bento row creates a new `Line` when the number of items starting in it changes (grid lines always have room for every column) |
| Filter change | — | `FilteredSource` allocates its match array |
| `GetAllItems()` | `ListSource`, `FilteredSource` | An `AsReadOnly` wrapper per call |

Not covered yet: paging (an async page load, P2 measures it across frames), measured slots (their first measurement goes through Unity's layout), and section insert or remove (they also go through `Recount`).

## P3 results: zero GC (2026-11-16)

All 56 cases of the matrix now allocate nothing (P1's 54, plus bento patterns when an item is added and removed, and a sectioned grid when a section grows).

| Was allocating | Fix |
|---|---|
| Disable and enable | The locale listener subscribed on every enable. Adding to an event combines delegates, which allocates even with a cached handler. The list now subscribes once for its lifetime, unsubscribes in `OnDestroy`, and applies a change made while it was disabled when it's enabled again |
| Width change | `OnViewportResized` started a coroutine. The re-placement (and the pool warm-up) now run from `LateUpdate` a frame later, through a flag |
| `Refresh()` after a section grows | `SectionedSource` reuses its buffers instead of allocating arrays |
| `Refresh()` after an item is added or removed (bento, sectioned grid) | Every bento row, and every header line in a grid, has room for a whole row, so a line is reused whatever it holds. The recycler places a header as one slot across its line |
| Filter change | `FilteredSource` keeps its matches in a reused list |
| `GetAllItems()` | `ListSource`, `PagedSource` and `FilteredSource` return the same read-only view every call. `FilteredSource`'s view is updated in place after the filter or the source changes |

**Found by the device bench (2026-11-17):** a filter change that changes the count, `SetSource` between sources of different sizes and a grid scroll with graphics in the slots all allocate nothing once warmed up (tests added). Swapping bento layouts created a new arranger each time; arrangers are now cached per list (test added). Disabling and enabling a list whose slots have graphics allocates inside Unity: its graphic registry drops a canvas's set when the last graphic is disabled and makes a new one when one comes back. A plain Canvas with Images, no list, allocates the same; the list's own part is zero.

**Measuring note:** in the Editor (Mono), `GC.GetAllocatedBytesForCurrentThread()` reports 0 even when something allocates. The enable allocation was invisible to it and only showed in the profiler's `GC.Alloc` samples. The allocation tests use Unity's constraint (built on those samples), and P2's bench must use the `GC.Alloc` recorder too.

## P4 results: CPU (2026-11-16)

Measured with `Tests/Runtime/CpuBench.cs` (`[Explicit]`, run with `-testFilter CpuBench`): 100,000 items, 3,000 steps of 37 px forward and back, the scroll handler timed apart from `Canvas.ForceUpdateCanvases`, slots with an Image. Medians of 9 runs, in microseconds per scroll event, Editor (Unity 6000.3.6f1, MacBookPro16,1). Baseline `de0fe3c` and P4 run alternately, twice each, on the same machine state.

| Case | Baseline | P4 | Change |
|---|---|---|---|
| List | 3.30 / 3.58 | 2.70 / 2.80 | **−20 %** |
| Grid, 4 columns | 7.90 / 8.06 | 5.71 / 5.84 | **−28 %** |
| Bento patterns | 8.25 / 9.25 | 5.79 / 6.02 | **−32 %** |
| Sections | 3.82 / 3.96 | 3.06 / 3.21 | **−20 %** |
| Unity's canvas update (all cases) | 23–62 | the same | — |

**Kept:**

| # | Change | Measured on its own |
|---|---|---|
| C9 (new) | A slot already on screen, in the same place, showing the same item, is skipped: no `SetActive`, no shown callback. Before, every range change walked every visible slot (about 32 in a 4-column grid). A slot that still needs measuring (new or invalidated) is still reported | With C3: list −18 %, grid −25 to −30 %, bento −30 % |
| C3 | `SetActive(true)` only for an inactive slot; the slot's GameObject is cached | (with C9) |
| C1 | Slots are placed from the pass's cross size, not by reading `viewport.rect` per slot | With C2: grid −11 %, bento −7 %, list −5 % |
| C2 | Pivot and anchors are set once per slot; `sizeDelta` only when it changed | (with C1) |
| C10 | The content's position is read once per scroll event, not twice | No measurable change; kept because it's one line and removes a native call |

**Dropped:**
- **C4** (stepping the visible range from the last result instead of two binary searches): no measurable change. It added code, so it went.
- **C5 and C6**: covered by C9. The per-event checks are cheap once unchanged slots are skipped.

**Found while measuring:**
- **Unity's canvas update costs 6 to 9 times the list's own work**, and P4 doesn't change it. Most of it is the `RectMask2D` clipping and culling every child graphic each time the content moves, including the parked pool slots. Taking pooled slots out from under the mask was measured on phones in Phase 10 and lost (re-parenting and re-enabling cost more), so this stays. It's reported, not hidden: the device runs (P2, P7) show the whole frame.
- Editor timing is very sensitive to machine load: an IL2CPP build in the background tripled every number. The device runs must start from an idle device.

## P5 results: heap objects, candidate C7 (2026-11-16)

Measured with `Tests/Runtime/HeapBench.cs` (`[Explicit]`, run with `-testFilter HeapBench`). The items stay alive in both measurements, so the numbers are what the list itself adds. Editor, Unity 6000.3.6f1, medians of 15 full collections.

| Case | Memory the list adds | Per item | Heap objects (approx.) | Full GC without → with the list |
|---|---|---|---|---|
| List, 100,000 | 12.8 MB | 134 B | 300,000 (a Placement, a Line and its array per item) | 438 → 426 ms (noise) |
| Grid 4, 100,000 | 6.4 MB | 67 B | 150,000 | 440 → 433 ms (noise) |
| Bento, 100,000 | 8.3 MB | 88 B | 150,000 | 435 → 449 ms (+14, near noise) |
| List, 1,000,000 | 182.7 MB | 192 B | 3,000,000 | 465 → 521 ms (+56, +12 %) |
| Grid 4, 1,000,000 | 117.5 MB | 123 B | 1,500,000 | 471 → 488 ms (+17) |
| Bento, 1,000,000 | 133.5 MB | 140 B | 1,500,000 | 471 → 506 ms (+35) |

**Reading it:**
- **The Editor hides most of the GC cost.** A full collection walks the whole Editor heap (about 440 ms), so the list's share only shows at 1,000,000 items. A player's heap is much smaller, so the same objects are a larger share of each collection there. The device runs (P2) should include this bench to give the real figure.
- **Memory is the clearer number:** 67 to 134 bytes per item up to 100,000 items. That's several times the size of a typical item, almost all of it bookkeeping for items that aren't on screen. A plain list is the heaviest, with three objects per item, because every item is its own line.
- **At typical sizes it doesn't matter:** 10,000 items is about 1.3 MB and 30,000 objects.

**What C7 would give:** with per-item data in arrays (sizes, line per item, flags, bento tiles) and objects only for the slots on screen, memory would drop to roughly 10 to 25 bytes per item, and the list would add almost nothing for the GC to walk.

**What it costs:** `Placement` and `Line` are public types used throughout the layout model, the recycler, expansion, sizing, loop, bento and sections. It's the largest refactor since Phase 5, and every one of the 650 tests depends on it.

**Recommendation: no-go for the first release.**
- Revisit after the device runs (P2) show the GC cost on phones with real heaps.
- Or revisit when a project needs lists past about 100,000 items.
- If memory matters before then, a smaller step ("C7-lite") gets part of the gain without touching the public types:
  - one `Line` object per line only where a line holds several slots (grids, bento), with plain lists keeping their line data in arrays;
  - the "where it was placed" fields moved from every `Placement` to the slot handle, since only shown items need them.

## C7-lite results (2026-11-16)

Done instead of the full C7, after P5:
- **Where a slot was placed** (four fields used only to skip re-placing a slot) moved from every `Placement` to the slot handle. Only shown items have a slot.
- **Bento tiles** live in the engine's tile list only, not copied onto every `Placement`.
- **A line with room for one slot** (every line of a plain list or a sectioned list) keeps its placement in a field instead of a one-element array. `Line` now has an indexer (`line[slot]`) and `SlotCount` instead of the public `Placements` array.

**Memory the list adds** (`HeapBench`, Editor):

| Case | Before (P5) | C7-lite | Change |
|---|---|---|---|
| List, 100,000 | 12.8 MB (134 B/item) | 5.1 MB (54 B/item) | **−60 %** |
| Grid 4, 100,000 | 6.4 MB (67 B/item) | 1.7 MB (18 B/item) | **−73 %** |
| Bento, 100,000 | 8.3 MB (88 B/item) | 3.7 MB (39 B/item) | **−56 %** |
| List, 1,000,000 | 182.7 MB (192 B/item) | 115.6 MB (121 B/item) | −37 % |
| Grid 4, 1,000,000 | 117.5 MB (123 B/item) | 76.7 MB (80 B/item) | −35 % |
| Bento, 1,000,000 | 133.5 MB (140 B/item) | 92.7 MB (97 B/item) | −31 % |

A plain list also has one heap object less per item (2 instead of 3). The GC time is still within the Editor's noise at 100,000. At 1,000,000 the larger per-item figure comes from the buffers' doubling growth.

**CPU:** unchanged. P4 (`4e0c4f0`) and C7-lite were run alternately, twice each. The only difference was bento at +3 to 5 %, from reading tiles through an interface; with the concrete list it matches P4 (list 2.3, grid 4.9, bento 5.0 to 5.2, sections 2.7 µs per event).

## 1. Definitions

### Zero GC

**UI Scroller's own code allocates 0 bytes of managed memory in steady state**, for every operation a running screen does over and over:
- scrolling, flinging and recycling;
- binding (the list's side; what the slot's `Data` setter does is the game's);
- `Refresh()` with the same item count;
- expand and collapse;
- `ScrollToItem` (instant and animated), snapping, loop re-centring;
- a width change;
- appending a page (the list's side; the loader's `Task` is the game's);
- measuring sizes, and the size watcher;
- enable and disable.

**Allowed to allocate:**
- the first `Initialize`, and growing past the largest count seen (buffers grow, then are reused);
- creating pool slots;
- a new source with a larger count;
- error and warning messages;
- the game's own callbacks.

**Reported separately**, because the list can't remove them and a studio will want to know:
- Unity's UI work: canvas rebuilds, layout rebuilds when measuring, `ScrollRect`'s own `LateUpdate`;
- the async state machine of a page load.

### Very cheap

- **Per scrolled frame**, the list's own CPU time (`UI Scroller.*` markers, excluding the game's `Data` setter) stays flat whatever the item count (1,000 to 1,000,000) and under **0.05 ms on a Pixel 5**.
- **Native calls per recycled slot** (RectTransform writes, `SetActive`) are kept to the minimum that moves the slot.
- **Memory per item** is known and small, and so is the work the game's GC does because of the list (see *Heap objects*).

## 2. Assessment (to write once the numbers are in)

The written assessment comes last, so it can quote the measurements. These are the points it will cover, with what's known today.

### What fits

- One component on a UI object, a minimal inspector, Viewport and Content built for you, and Content layout components refused with a clear fix.
- Sizes are fixed or measured with layout, with no size calculator. Measured items stay still while scrolling up, and late text is followed.
- Lists, grids, auto grids, bento, sections with headers of any height, expandable slots, paging with cancellation, loop with snapping, all four directions.
- About 474 automated tests (EditMode and PlayMode) that run in batch mode, and a bench measured on two Android phones.
- MIT licence, a CHANGELOG, agent rules.

### What doesn't fit (the adoption risks a lead would raise)

| Risk | Today | What would fix it |
|---|---|---|
| Single author, preview version, not used in a shipped game | Stated in the README | The first release, then time and users |
| Installing it needs Tools Core first, as a Git URL (UPM doesn't resolve Git dependencies) | Two manual steps | Publish on OpenUPM (it resolves dependencies), or make Tools Core optional |
| Platforms: Editor and Android (IL2CPP) only | — | An iOS and a WebGL check |
| Items must be classes | `where T : class` everywhere | Probably never; a struct wrapped in a class covers it |
| No proof of performance a third party can reproduce | A bench in a private project | This plan |

### Missing features (from the earlier comparison, with decisions taken)

| Feature | Decision |
|---|---|
| Nested scrolling (a carousel inside a vertical list) | Planned for the first release: `nested-scrolling-plan.md` |
| Several prefabs per list (slot types) | After the first release |
| Sticky headers | Outside the core: a `StickyHeaderBar` in the Extras sample |
| Per-slot position for carousels | After the first release, as a read-only query |

## 3. Measuring

### Three kinds of measurement

| Kind | What it proves | Where it runs |
|---|---|---|
| **Allocation tests**: `Is.Not.AllocatingGCMemory()` around each operation | Zero GC, as a pass or fail that never drifts | `verify.sh`, every run |
| **Work counts** (`WorkCounters`): slots checked, placed, bound and measured, native writes | The list does work in proportion to the screen, not to the item count | `verify.sh`, every run |
| **Timing**: `ProfilerRecorder` on the `UI Scroller.*` markers, `PlayerUpdateCanvases` and `Canvas.BuildBatch`, plus GC bytes and count per frame | How fast it is | Opt-in (timing is noisy): the bench in the Editor, and automated builds on the A02s, Pixel 5, iPhone 13 mini, iPhone 17 Air and WebGL |

Timing stays out of the pass-or-fail tests. Times are recorded and compared, never asserted.

### Scenarios

Each layout is run with the operations that apply to it, at 1,000 and 100,000 items (1,000,000 for the plain list).

| Layouts | Operations |
|---|---|
| List, fixed size | Steady scroll at 4,000 px/s |
| List, measured with TMP text of varying length | Fling and decelerate |
| Grid, 4 columns, and Auto Grid | `ScrollToItem` far, instant and animated |
| Bento, spans and patterns | Snap (item and page) |
| Sections, list and bento, measured headers | Expand and collapse (list) |
| Loop, short (copies) and long (window) | `Refresh()` same count; `Refresh()` + 1 item |
| | Append a page; width change; disable and enable |

### Baselines to compare against

1. **Current `main`**: the baseline every improvement is measured against.
2. **Recyclable Scroller 0.3.0** (tag `v0.3.0`): shows what the refactor gained. Only the scenarios it supports: fixed list and grid, scroll, refresh.
3. **A plain `ScrollRect` with every item instantiated**, at 1,000 items: what a studio would write without a recycler, and why they need one.

### How results are kept

- One table per run in `benchmarks.md`, with the machine, Unity version, build type and commit.
- Medians and p95 over at least 3 runs after a warm-up. An improvement counts only if it beats the spread between runs.
- The raw results stay in the bench output (`ScrollerBench.md`), so a run can be checked later.

## 4. Allocation audit: candidates found by reading the code

| Where | What allocates | When | Plan |
|---|---|---|---|
| `SectionedSource.Recount` | Two `ToArray()` (shown sections, header indexes) | **Every `Refresh()` of a sectioned list**, and every section change | Reuse arrays with a count, as `LayoutModel` does |
| `FilteredSource.Invalidate` | `matchBuffer.ToArray()` | Every filter change | Reuse the buffer with a count |
| `ListSource`, `PagedSource`, `FilteredSource` `.GetAllItems()` | `AsReadOnly()` wrapper per call | Whenever it's called (check whether the engine calls it on a hot path) | Cache the wrapper |
| `OnViewportResized` (`StartCoroutine`) | A coroutine object | Every width change (rotation, resizable windows, safe-area changes) | A flag handled in `LateUpdate` instead of a coroutine |
| Pool warm-up (`StartCoroutine`) | A coroutine object | Once per `Initialize`; allowed, but the same flag approach removes it |
| `LoadNextPage` (async) | State machine, `Task` | Once per page | The list's side can't avoid the `Task` it awaits; measure and report |
| Measuring (`LayoutRebuilder.ForceRebuildLayoutImmediate`) | Unity internals (list pools) | When a measured item is shown for the first time | Measure; report as Unity's if it's theirs |

Each candidate gets a failing allocation test first, then the fix.

## 5. CPU candidates (hypotheses, each measured before it's kept)

| # | Idea | Why it might help | How it's measured |
|---|---|---|---|
| C1 | `PositionInLine` reads `viewport.rect` for every slot it places; the recycler already knows the cross size | `RectTransform.rect` is a native call | Native calls and marker time per recycled slot |
| C2 | Every placement writes `pivot`, `anchorMin`, `anchorMax`, `anchoredPosition` and `sizeDelta`. Pivot and anchors never change for a direction: set them once, when the slot is created | 3 fewer RectTransform writes per recycled slot, each one dirtying layout | Same |
| C3 | `SetActive(true)` on every show, even for a slot that's already active (pooled slots stay active) | A native call per shown slot | Same |
| C4 | The visible range is found with two binary searches on every check; scrolling moves it a line or two | Start from the previous range: O(1) in the usual case | Marker time at 1,000,000 items |
| C5 | `RequestPageIfNeeded`, `KeepInsideLoop` and similar checks run on every scroll event | Skip them when nothing that matters changed | Marker time |
| C6 | Fixed-size slots call `SizeWatcher?.StopWatching()` every time they're shown | Skip when nothing is being watched | Marker time |
| C7 | **Heap objects:** a `Placement` object per item and a `Line` object per line: 100,000 items means about 125,000 objects the game's GC walks on every collection, even though only about 20 are on screen | Keep per-item data in arrays (offsets, sizes, flags, tiles), with objects only for shown items | GC collection time with the list alive vs not (`GC.Collect` timed), memory per item; **a larger change: decided after measuring** |

C1 to C6 are small and local. C7 is the one with real potential for a studio with big lists and an incremental GC, but it touches the layout model everywhere. It goes ahead only if the measurement shows the GC walking these objects costs something worth the change.

## 6. Proving it

- **In the repository:**
  - the allocation matrix (every scenario × operation in section 3) as tests that run in `verify.sh`;
  - `benchmarks.md` with baseline vs after, in the Editor and on the five targets, with commits;
  - the comparison with 0.3.0 and with a plain `ScrollRect`.
- **In the README:** a short Performance section with the headline numbers (0 B per frame, ms per frame on a low-end phone, memory per item), and how to run the bench yourself.
- **The bench in the package**, as a sample (it lives in MyTools today), so a third party can run it in their own project and on their own devices.

## 7. Phases

| # | Step | Done when |
|---|---|---|
| P0 | Settle the open decisions below | Written here |
| P1 | Allocation matrix: tests for every scenario × operation. The ones that fail are the baseline's allocation list | The list of what allocates today, by test |
| P2 | Timing bench: extend the MyTools bench to the scenarios in section 3, with `ProfilerRecorder` and GC per frame, running by itself in a build and reporting its results; run it on `main` in the Editor and on the five targets; also run 0.3.0 and a plain `ScrollRect` | Baseline tables in `benchmarks.md` |
| P3 | Fix the allocations (section 4), each test-first | Every allocation test passes |
| P4 | CPU candidates C1 to C6, one at a time: measure, keep or drop, record | A before and after per candidate |
| P5 | Measure C7 (GC walk cost and memory per item); decide | A go or no-go with numbers |
| P6 | (If go) C7 | Same tests pass; the numbers show the gain |
| P7 | Move the bench into the package as a sample; README Performance section; write the assessment (section 2) | Published with the proof |

## 8. Decisions

Settled on 2026-11-16: see *Decisions* at the top.
