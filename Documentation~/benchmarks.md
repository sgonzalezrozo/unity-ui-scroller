# UI Scroller benchmarks

How the list performs on phones, and what the tests guarantee on every run.

## Device bench, 0.5.0 (2026-11-17)

The current numbers. Five phones, development players (IL2CPP, Unity 6000.3.6f1), each running the same 45 cases by itself: every layout and direction, sections, loop, snapping, expanding, paging, filtering, data changes, width changes, disable and enable, source and layout swaps, opening and closing, a 60 s soak, and two controls. Each case scrolls at 4,000 px/s (or flings, or repeats its operation) for 5 s on a Screen Space Overlay canvas with 200 static images around the list. Slots have an Image and a legacy Text label. Development players run the profiler markers, so times are higher than in a release build, by the same amount for every case.

### Scrolling 100,000 items

`OnScroll` is the list's own work per scroll event (average / slowest). CPU is the whole main thread per frame, the app and the 200 static images included (average / 95th percentile).

| Device | Frame budget | Plain list: OnScroll | Plain list: CPU | Measured text: OnScroll | Grid 4: OnScroll | Bento: OnScroll | Missed frames, normal use |
|---|---|---|---|---|---|---|---|
| Galaxy A02s (low end, 2021) | 16.7 ms (60 Hz) | 0.19 / 0.74 ms | 8.0 / 10.6 ms | 0.82 / 3.5 ms | 0.29 / 1.5 ms | 0.21 / 1.4 ms | ≤ 1.4 % |
| Pixel 2 XL (2017) | 16.7 ms (60 Hz) | 0.20 / 0.87 ms | 5.2 / 6.1 ms | 0.74 / 2.8 ms | 0.43 / 1.7 ms | 0.19 / 1.5 ms | ≤ 0.3 % |
| Pixel 5 | 11.1 ms (90 Hz) | 0.10 / 0.51 ms | 3.5 / 4.2 ms | 0.33 / 1.4 ms | 0.14 / 0.70 ms | 0.10 / 0.74 ms | 0 % |
| iPhone 13 mini | 16.7 ms (60 Hz) | 0.25 / 0.71 ms | 4.6 / 5.4 ms | 0.81 / 2.8 ms | 0.39 / 2.2 ms | 0.31 / 1.9 ms | ≤ 0.3 % |
| iPhone Air | 8.3 ms (120 Hz) | 0.07 / 0.35 ms | 2.0 / 2.5 ms | 0.22 / 1.4 ms | 0.13 / 0.65 ms | 0.08 / 0.87 ms | ≤ 0.3 % |

"Normal use" is every scroll, fling, scroll-to, snap, carousel, sections, paging, expanding and soak case. The same screen with no list costs 6.6 ms of CPU on the A02s and 0.8 ms on the iPhone Air, so most of the frame is the screen, not the list.

### Memory

- **Allocations:** every case allocates what the same screen with no list allocates (about 1.8 KB per frame, the development player's own), except where it's expected: measuring text (Unity's text generation), adding items and the first use of a bigger bento layout (buffers grow once), opening a list (its slots), and enabling slots with graphics (Unity's graphic registry). The zero-GC test matrix checks the list's own part on every verify.
- **Leaks:** after each case its screen is destroyed and garbage is collected. On all five phones, every case's items and list were collected, and GameObjects returned to their starting count.
- **Per item** (Editor, 100,000 items): 54 bytes for a plain list, 18 for a 4-column grid, 39 for bento.

### What still costs a frame

| Case | Slowest frame | Why |
|---|---|---|
| Opening a list of 10,000 items (every 0.25 s, to stress it) | 42 ms (iPhone Air) to 94 ms (A02s) | Creating the slots that fill the screen. Open lists before they're shown, or keep the screen loaded |
| Refresh after adding an item, every 5 frames | 20 % missed at 120 Hz (iPhone Air), 12 % at 60 Hz (A02s) | A new count rebuilds the layout of all 10,000 items, about 3 ms on the A02s. Batch changes into one `Refresh()` |
| Width change on a measured list | 1.4 to 4.6 % missed | Unity lays out every shown text again at the new width |

## How it got here

The list's own work was measured in the Editor before and after each optimization, and a change was kept only when it beat the spread between runs.

### What zero GC means here

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

### Zero GC: what allocated, and how it stopped

`Tests/Runtime/AllocationTests.cs` checks every layout × every operation a running screen repeats (scroll, fling, scroll to, snap, refresh, expand, width change, enable and disable, filter, source and layout swaps) and fails if anything allocates once warmed up. These used to allocate:

| Was allocating | Fix |
|---|---|
| Disable and enable | The locale listener subscribed on every enable. Adding to an event combines delegates, which allocates even with a cached handler. The list now subscribes once for its lifetime, unsubscribes in `OnDestroy`, and applies a change made while it was disabled when it's enabled again |
| Width change | `OnViewportResized` started a coroutine. The re-placement (and the pool warm-up) now run from `LateUpdate` a frame later, through a flag |
| `Refresh()` after a section grows | `SectionedSource` reuses its buffers instead of allocating arrays |
| `Refresh()` after an item is added or removed (bento, sectioned grid) | Every bento row, and every header line in a grid, has room for a whole row, so a line is reused whatever it holds. The recycler places a header as one slot across its line |
| Filter change | `FilteredSource` keeps its matches in a reused list |
| `GetAllItems()` | `ListSource`, `PagedSource` and `FilteredSource` return the same read-only view every call. `FilteredSource`'s view is updated in place after the filter or the source changes |

**Found by the device bench:** a filter change that changes the count, `SetSource` between sources of different sizes and a grid scroll with graphics in the slots all allocate nothing once warmed up (tests added). Swapping bento layouts created a new arranger each time; arrangers are now cached per list (test added). Disabling and enabling a list whose slots have graphics allocates inside Unity: its graphic registry drops a canvas's set when the last graphic is disabled and makes a new one when one comes back. A plain Canvas with Images, no list, allocates the same; the list's own part is zero.

**Measuring note:** in the Editor (Mono), `GC.GetAllocatedBytesForCurrentThread()` reports 0 even when something allocates. The enable allocation was invisible to it and only showed in the profiler's `GC.Alloc` samples. The allocation tests use Unity's constraint (built on those samples), and the device bench uses the `GC.Alloc` recorder too.

### CPU: 20 to 32 % less per scroll event

Measured with `Tests/Runtime/CpuBench.cs` (`[Explicit]`, run with `-testFilter CpuBench`): 100,000 items, 3,000 steps of 37 px forward and back, the scroll handler timed apart from `Canvas.ForceUpdateCanvases`, slots with an Image. Medians of 9 runs, in microseconds per scroll event, Editor (Unity 6000.3.6f1, MacBookPro16,1). The baseline and the change ran alternately, twice each, on the same machine state.

| Case | Before | After | Change |
|---|---|---|---|
| List | 3.30 / 3.58 | 2.70 / 2.80 | **−20 %** |
| Grid, 4 columns | 7.90 / 8.06 | 5.71 / 5.84 | **−28 %** |
| Bento patterns | 8.25 / 9.25 | 5.79 / 6.02 | **−32 %** |
| Sections | 3.82 / 3.96 | 3.06 / 3.21 | **−20 %** |
| Unity's canvas update (all cases) | 23–62 | the same | — |

**What changed:**
- A slot already on screen, in the same place and showing the same item, is skipped: no `SetActive`, no callback. Before, every change of the visible range walked every visible slot (about 32 in a 4-column grid). This was most of the gain.
- `SetActive(true)` runs only for an inactive slot, and each slot's GameObject is cached.
- Slots are placed from the pass's cross size instead of reading the viewport per slot, pivot and anchors are set once per slot, and `sizeDelta` only when it changed.
- The content's position is read once per scroll event.
- Tried and dropped: stepping the visible range from the last result instead of searching (no measurable change).

**Found while measuring:**
- **Unity's canvas update costs 6 to 9 times the list's own work**, and this work doesn't change it. Most of it is the `RectMask2D` clipping and culling every child graphic each time the content moves, including the parked pool slots. Taking pooled slots out from under the mask was measured on phones and lost (re-parenting and re-enabling cost more), so this stays. It's reported, not hidden: the device bench shows the whole frame.
- Editor timing is very sensitive to machine load: an IL2CPP build in the background tripled every number. Device runs start from an idle device.

### Memory per item: 56 to 73 % less

What changed:
- **Where a slot was placed** (four fields used only to skip re-placing a slot) moved from every `Placement` to the slot handle. Only shown items have a slot.
- **Bento tiles** live in the engine's tile list only, not copied onto every `Placement`.
- **A line with room for one slot** (every line of a plain list or a sectioned list) keeps its placement in a field instead of a one-element array. `Line` now has an indexer (`line[slot]`) and `SlotCount` instead of the public `Placements` array.

**Memory the list adds** (`HeapBench`, Editor):

| Case | Before | After | Change |
|---|---|---|---|
| List, 100,000 | 12.8 MB (134 B/item) | 5.1 MB (54 B/item) | **−60 %** |
| Grid 4, 100,000 | 6.4 MB (67 B/item) | 1.7 MB (18 B/item) | **−73 %** |
| Bento, 100,000 | 8.3 MB (88 B/item) | 3.7 MB (39 B/item) | **−56 %** |
| List, 1,000,000 | 182.7 MB (192 B/item) | 115.6 MB (121 B/item) | −37 % |
| Grid 4, 1,000,000 | 117.5 MB (123 B/item) | 76.7 MB (80 B/item) | −35 % |
| Bento, 1,000,000 | 133.5 MB (140 B/item) | 92.7 MB (97 B/item) | −31 % |

A plain list also has one heap object less per item (2 instead of 3). The GC time is still within the Editor's noise at 100,000. At 1,000,000 the larger per-item figure comes from the buffers' doubling growth.

**CPU:** unchanged. Before and after ran alternately, twice each. The only difference was bento at +3 to 5 %, from reading tiles through an interface; with the concrete list it matches the earlier numbers (list 2.3, grid 4.9, bento 5.0 to 5.2, sections 2.7 µs per event).
