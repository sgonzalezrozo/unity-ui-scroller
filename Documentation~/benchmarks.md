# UI Scroller benchmarks

How the list performs, measured with the bench runner in MyTools (`Assets/ScrollerTests/Bench`).

## Device bench, 0.5.0 (2026-11-17)

The current numbers. Five phones, development players (IL2CPP, Unity 6000.3.6f1), each running the same 45 cases by itself: every layout and direction, sections, loop, snapping, expanding, paging, filtering, data changes, width changes, disable and enable, source and layout swaps, opening and closing, a 60 s soak, and two controls. Each case scrolls at 4,000 px/s (or flings, or repeats its operation) for 5 s on a Screen Space Overlay canvas with 200 static images around the list. Slots have an Image and a legacy Text label. Development players run the profiler markers, so times are higher than in a release build, by the same amount for every case.

**Built and run from MyTools:** Tools > UI Scroller > Build Device Bench (Android installs and starts by itself; iOS writes an Xcode project). The report is written to `persistentDataPath/UIScrollerBench.md`, logged and shown on screen.

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

Full reports per device are kept with the bench in MyTools (`BenchResults/2026-11-17-final`).

## Earlier runs (before 0.5.0)

The sections below are the first measurements, from the end of the refactor (Phase 10). Kept for the record; the numbers above replace them.

### Method

- **Run it:** in MyTools, **Tools > UI Scroller > Run Benchmarks**. It opens a new empty scene, enters Play mode, runs every case, writes the results to `Logs/ScrollerBench.md` and the console, and leaves Play mode.
- **On a phone:** add `BenchRunner` to an empty scene with a camera, make an IL2CPP development build, and read the table from the device log (it's also written to `Application.persistentDataPath/ScrollerBench.md`).
- **Every case:** a Screen Space Overlay canvas (1080 × 1920 reference) with 200 static images around the list, so rebatching the whole screen would show. The list scrolls at 4,000 px/s for 5 s after 30 frames to settle.
- **Recorded per frame:**
  - frame time;
  - the markers `UI Scroller.OnScroll`, `UI Scroller.Bind` and `UI Scroller.Measure`;
  - `PostLateUpdate.PlayerUpdateCanvases` (layout and graphic rebuilds);
  - `Canvas.BuildBatch`;
  - GC bytes allocated in the frame.
- **Cases:**
  - lists of 1,000, 10,000 and 100,000 fixed-size items;
  - a 4-column grid of 10,000;
  - a list of 10,000 slots measured with layout, with text of varying length.

Timing is noisy, so the tests check work counts and allocations, never times (`PerformanceTests`, `LayoutSizingTests`). This file records times for reference.

## What Phase 10 changed

There's no timing from before Phase 10: the bench runner was written at the end. The tests record what changed, as counts:

| Item | Before | After |
|---|---|---|
| P6 | Each recycled slot moved to a container outside every canvas and back (100 parent changes over 5,000 px) | 0 |
| P7 | `Refresh()` returned every visible slot to the pool and took it back (16 parent changes) | 0; slots rebound in place |
| P2 | Each change of the visible range walked every line (100,000 per change) | Only the slots on screen (≤ 20) |
| P3 | Every visible slot re-placed on each range change | Only the slot that came in (1) |
| P4 | Every scroll event worked out the visible range | Skipped under 0.5 px |
| P18 | Moving slots rebuilt the batches of the whole screen | Only the list's (nested Canvas on the Viewport) |
| P10 | First screen bound a frame after `Initialize` (blank first frame) | Bound inside `Initialize` |
| P9 | Whole pool created in one frame | 2 slots per frame |
| P8 | Disable returned every slot; enable rebound them all | Nothing rebound or moved |
| P11 | Every rebuild and page created N placements and lines | Only the new ones; `Refresh()` with the same count allocates 0 B |
| P12 | Content size added up every line | Read from the offsets |
| Sizes | A measured item was measured again every time it scrolled back in | Cached; the watcher catches changes |
| Placeholders | Every measuring pass visited every item | Only while the average moves (≤ 10 passes over 30 screens of 100,000) |
| Allocations | — | Scrolling 100,000 items allocates 0 B (already true before Phase 10) |

## Results

Paste each run's table here, with the machine and the commit.

### Editor

**Bootgame Shop list** (EnemyItem prefab, FastIoC injection, EnemyView with TMP; fixed 70 px slots), MacBookPro16,1, Unity 6000.3.6f1, Game view 1023 × 1818, 2026-11-13 10:16. Cases ran in this order, 5 s each, scrolling back and forth at 4,000 px/s:

| Case | Frame ms (avg / max) | OnScroll ms (avg / max) | Bind ms (avg) | Measure ms (avg) | UpdateCanvases ms (avg / max) | BuildBatch ms (avg) | GC bytes / frame (avg / max) |
|---|---|---|---|---|---|---|---|
| Idle, not scrolling (10,000 enemies) | 2.56 / 39.13 | 0.000 / 0.000 | 0.000 | 0.000 | 0.053 / 0.104 | 0.000 | 14429 / 120547 |
| Shop, 10,000 enemies (its own source) | 3.37 / 41.34 | 0.041 / 0.680 | 0.019 | 0.000 | 0.151 / 1.739 | 0.011 | 14875 / 41383 |
| Shop, 300 enemies | 3.74 / 7.68 | 0.049 / 0.293 | 0.023 | 0.000 | 0.172 / 0.567 | 0.013 | 14929 / 42955 |
| Shop, 100,000 enemies | 4.28 / 8.91 | 0.061 / 0.273 | 0.029 | 0.000 | 0.203 / 0.579 | 0.015 | 15143 / 41339 |

Reading it:
- **Scrolling costs the list 0.04–0.06 ms per frame**, binding 0.02–0.03 ms, whatever the item count. Scrolling raises Unity's canvas update from 0.05 ms (idle) to 0.15–0.20 ms; the batch build stays near 0.01 ms. A 60 fps frame is 16.7 ms.
- **Item count barely matters, and the order of the cases matters more.** Every column creeps up from case to case (frame time 3.4, 3.7, 4.3 ms), including the 300-item case run between the other two. The Editor warming up or the heap growing is a likelier cause than the item count.
- **GC: the Editor allocates about 14.4 KB per frame on its own** (idle). Scrolling adds about 0.4–0.7 KB per frame. The tests show the list itself allocates 0 B while scrolling, so this is likely the slot (TMP text, `EnemyView`) or ScrollRect. A player build would show it without the Editor's noise.
- **The 40 ms spikes** happen in the first two cases only, in idle too, with the list's own maximum under 0.7 ms: an Editor hitch, not the list.
- **Measure is 0:** the Shop slots are fixed-size, so nothing is measured.

An earlier run the same day showed a 3.4 MB frame in every case. That was the bench itself (it listed every profiler stat when a case started); it now looks them up once and skips each case's first frame.

**Synthetic bench:** not run yet.

### Phone

**Samsung Galaxy A02s (SM-A025M)**, a low-end phone (MediaTek Helio P35, 60 Hz), development build, IL2CPP, 720 × 1600, 2026-11-13 11:24. Bootgame Shop list, same cases as the Editor run:

| Case | Frame ms (avg / max) | OnScroll ms (avg / max) | Bind ms (avg) | Measure ms (avg) | UpdateCanvases ms (avg / max) | BuildBatch ms (avg) | GC bytes / frame (avg / max) |
|---|---|---|---|---|---|---|---|
| Idle, not scrolling (10,000 enemies) | 16.67 / 16.90 | 0.000 / 0.000 | 0.000 | 0.000 | 0.473 / 1.253 | 0.000 | 476 / 27032 |
| Shop, 10,000 enemies (its own source) | 16.67 / 16.99 | 1.219 / 4.033 | 0.562 | 0.000 | 2.503 / 8.717 | 0.246 | 842 / 8492 |
| Shop, 300 enemies | 16.67 / 16.80 | 0.877 / 1.958 | 0.402 | 0.000 | 1.784 / 3.261 | 0.155 | 403 / 424 |
| Shop, 100,000 enemies | 16.67 / 16.82 | 1.075 / 2.132 | 0.496 | 0.000 | 2.141 / 3.811 | 0.183 | 403 / 424 |

Reading it:
- **A steady 60 fps.** Frame time is the 60 Hz display, and no frame went past 17 ms, not even at 100,000 items. The bench lifts Unity's 30 fps cap but can't go past the display's rate, so frame time shows "no missed frame", not the spare time.
- **What scrolling costs on this phone:** the list 0.9–1.2 ms (binding 0.4–0.6 ms of it), Unity's canvas update 1.8–2.5 ms (0.5 ms when idle), the batch build about 0.2 ms. About 3.5–4 ms of a 16.7 ms frame.
- **Item count doesn't matter; the first case is the slowest.** 300 items: 0.88 ms, 100,000: 1.08 ms, 10,000 (run first): 1.22 ms, with the highest maximums. The first case pays for warming up.
- **Scrolling allocates nothing extra.** 403 B per frame while scrolling, steady with 300 or 100,000 items, against 476 B idle. That's the app's own background allocation, not the list. The first case's 842 B average and 8 KB maximum belong to its warm-up too.
- **The canvas update is the biggest part.** It's mostly rebuilding the meshes of slots that just got a new enemy (TMP text, background). Fewer graphics per slot, or cheaper text, would lower it more than anything in the list.

**Google Pixel 5**, a mid-range phone (Snapdragon 765G), development build, IL2CPP, 1080 × 2340, 2026-11-13 11:27. It ran at 60 Hz (the display reported 60, not its 90 Hz mode). Numbers converted from the phone's Spanish number format:

| Case | Frame ms (avg / max) | OnScroll ms (avg / max) | Bind ms (avg) | Measure ms (avg) | UpdateCanvases ms (avg / max) | BuildBatch ms (avg) | GC bytes / frame (avg / max) |
|---|---|---|---|---|---|---|---|
| Idle, not scrolling (10,000 enemies) | 16.67 / 16.90 | 0.000 / 0.000 | 0.000 | 0.000 | 0.162 / 0.422 | 0.000 | 551 / 27216 |
| Shop, 10,000 enemies (its own source) | 16.67 / 17.14 | 0.434 / 1.727 | 0.196 | 0.000 | 0.846 / 2.443 | 0.122 | 1045 / 9000 |
| Shop, 300 enemies | 16.67 / 17.18 | 0.405 / 0.890 | 0.185 | 0.000 | 0.768 / 1.758 | 0.105 | 1419 / 251006 |
| Shop, 100,000 enemies | 16.67 / 16.80 | 0.447 / 1.404 | 0.199 | 0.000 | 0.927 / 2.376 | 0.193 | 581 / 624 |

Reading it:
- **A steady 60 fps here too**, never past 17.2 ms.
- **About 2.5× faster than the A02s:** the list 0.4–0.45 ms per frame (binding 0.2 ms), the canvas update 0.8–0.9 ms, the batch build 0.1–0.2 ms. About 1.4 ms of a 16.7 ms frame.
- **Item count doesn't matter:** 0.41 ms at 300 items, 0.45 ms at 100,000.
- **GC:** about 580 B per frame while scrolling at 100,000 items, close to idle (551 B). The 300-item case had one frame that allocated 245 KB (and a higher average because of it). It happened once, in one case and on one phone; the A02s didn't show it. Something other than the list most likely allocated it (TMP adding glyphs to a dynamic font atlas is the usual suspect). A Profiler capture would tell if it comes back.

**Samsung Galaxy A02s, stress run** (same build as the Pixel 5 stress run below: before the `EnemyView` fix, no missed-frame count), 60 Hz, 2026-11-13 11:46:

| Case | Frame ms (avg / max) | CPU main thread ms (avg / max) | OnScroll ms (avg / max) | Bind ms (avg) | UpdateCanvases ms (avg / max) | BuildBatch ms (avg) | GC bytes / frame (avg / max) | Heap MB (start → end) |
|---|---|---|---|---|---|---|---|---|
| Idle, not scrolling (10,000 enemies) | 16.67 / 16.97 | 5.119 / 7.773 | 0.000 / 0.000 | 0.000 | 0.491 / 0.787 | 0.000 | 296 / 296 | 6.0 → 6.1 |
| Shop, 10,000 enemies (its own source) | 16.67 / 16.87 | 8.149 / 11.816 | 1.194 / 3.728 | 0.543 | 2.491 / 4.282 | 0.249 | 842 / 8492 | 6.1 → 6.4 |
| Shop, 300 enemies | 16.73 / 33.45 | 6.950 / 12.175 | 1.009 / 2.106 | 0.464 | 1.890 / 3.970 | 0.174 | 403 / 552 | 6.4 → 6.0 |
| Shop, 100,000 enemies | 16.67 / 16.78 | 8.011 / 11.355 | 1.148 / 2.529 | 0.540 | 2.193 / 3.443 | 0.179 | 403 / 424 | 24.7 → 24.8 |
| Fast fling, 100,000 enemies at 16,000 px/s | 16.67 / 16.79 | 9.448 / 15.164 | 2.260 / 4.473 | 1.178 | 3.302 / 6.069 | 0.192 | 815 / 21168 | 32.6 → 32.9 |
| Soak, 10,000 enemies for 120 s | 16.67 / 17.06 | 8.003 / 13.508 | 1.153 / 4.288 | 0.539 | 2.331 / 5.152 | 0.199 | 413 / 8848 | 32.9 → 36.0 |

Reading it:
- **60 fps held**, with one missed refresh in the whole run (the 33 ms frame in the 300-item case).
- **Less spare time than the Pixel.** The app alone takes 5.1 ms of CPU idle. Scrolling brings it to 7–8 ms of the 16.7 ms frame, and a fast fling to 9.4 ms on average with peaks of 15.2 ms, close to the budget. On a low-end phone, a hard fling is the case to watch.
- **What the fling costs:** the list 2.3 ms (binding 1.2 ms of it) and the canvas update 3.3 ms, both about twice normal speed. Binding includes turning each reused slot back on (`SetActive(true)`), which wakes its TMP text and Image and runs `EnemyView.OnEnable`.
- **GC:** idle 296 B per frame; scrolling adds about 110–140 B per slot shown (the same `EnemyView` delegates, smaller than on the Pixel because the A02s runs the 32-bit build). The soak's heap growth (32.9 → 36.0 MB) matches the allocation rate: uncollected garbage.

**Google Pixel 5, stress run** (90 Hz, with CPU time, a fast fling and a 2-minute soak), 2026-11-13 11:42:

| Case | Frame ms (avg / max) | CPU main thread ms (avg / max) | OnScroll ms (avg / max) | Bind ms (avg) | UpdateCanvases ms (avg / max) | BuildBatch ms (avg) | GC bytes / frame (avg / max) | Heap MB (start → end) |
|---|---|---|---|---|---|---|---|---|
| Idle, not scrolling (10,000 enemies) | 11.13 / 22.27 | 2.265 / 3.407 | 0.000 / 0.000 | 0.000 | 0.228 / 0.430 | 0.000 | 368 / 368 | 7.6 → 7.8 |
| Shop, 10,000 enemies (its own source) | 11.13 / 22.46 | 3.520 / 6.964 | 0.472 / 1.103 | 0.212 | 0.860 / 1.774 | 0.081 | 819 / 9128 | 7.8 → 8.2 |
| Shop, 300 enemies | 11.13 / 22.34 | 3.439 / 5.663 | 0.476 / 1.043 | 0.220 | 0.835 / 1.977 | 0.053 | 511 / 752 | 8.3 → 8.5 |
| Shop, 100,000 enemies | 11.13 / 22.31 | 3.327 / 5.535 | 0.472 / 0.939 | 0.211 | 0.804 / 1.365 | 0.052 | 511 / 752 | 37.9 → 38.1 |
| Fast fling, 100,000 enemies at 16,000 px/s | 11.13 / 22.30 | 4.190 / 6.806 | 1.077 / 2.057 | 0.504 | 1.332 / 2.665 | 0.062 | 1039 / 22304 | 50.1 → 50.6 |
| Soak, 10,000 enemies for 120 s | 11.14 / 22.38 | 3.424 / 6.173 | 0.491 / 1.301 | 0.223 | 0.839 / 2.033 | 0.052 | 516 / 9248 | 50.6 → 56.7 |

Reading it:
- **90 fps, with room to spare.** The CPU main thread takes 3.3–3.5 ms of the 11.1 ms frame while scrolling (2.3 ms idle), 4.2 ms during a fast fling, and never more than 7 ms.
- **The 22 ms frames aren't CPU.** Every case has one, idle included, and the CPU never went past 7 ms: a display refresh was missed elsewhere (GPU or system). This run only had the maximum; the next one counts them.
- **The list:** 0.47 ms per frame at normal speed, the same for 300 or 100,000 items. A fast fling brings 4× as many slots into view per frame and costs 1.08 ms (binding 0.5 ms): it follows slots shown per frame, not items.
- **GC:** the bench's own share is gone (idle 368 B, was 551). Scrolling now shows its own share: about 250 B per slot that comes into view (140 B per frame at normal speed, 670 B in the fling). The list allocates nothing (tests). It was `EnemyView` subscribing with `+= UpdateUI`, which creates a new delegate every time a recycled slot is enabled and disabled. Fixed in MyTools by creating it once; not measured yet.
- **Heap:** 100,000 enemies hold about 30 MB (the data plus the list's placements). In the soak the heap went from 50.6 to 56.7 MB, about the allocation rate times 120 s: garbage the GC hadn't collected yet, not necessarily a leak. The next run records the heap after a full GC.

**After the `EnemyView` fix, both phones** (frames over budget counted, heap after a full GC), 2026-11-13 11:53 (A02s) and 11:56 (Pixel 5):

| Case | A02s CPU ms (avg / max) | A02s over budget | A02s GC B/frame | Pixel 5 CPU ms (avg / max) | Pixel 5 over budget | Pixel 5 GC B/frame |
|---|---|---|---|---|---|---|
| Idle, not scrolling (10,000 enemies) | 4.71 / 7.04 | 0 | 296 | 2.14 / 3.54 | 0 | 368 |
| Shop, 10,000 enemies (its own source, run first) | 8.06 / 11.92 | 0 | 735 | 3.36 / 5.62 | 0 | 677 |
| Shop, 300 enemies | 7.92 / 17.56 | 1 (0.3%) | 296 | 3.29 / 4.86 | 0 | 368 |
| Shop, 100,000 enemies | 7.75 / 11.70 | 0 | 296 | 3.50 / 6.09 | 0 | 368 |
| Fast fling, 100,000 enemies at 16,000 px/s | 9.39 / 13.69 | 0 | 404 | 4.06 / 7.01 | 0 | 469 |
| Soak, 10,000 enemies for 120 s | 8.04 / 15.59 | 0 | 305 | 3.37 / 8.88 | 0 | 373 |

The list's own numbers (OnScroll, Bind, canvas update) match the stress runs above; the full tables are in `MyTools/Logs/android-bench/`.

Reading it:
- **Scrolling allocates nothing extra any more:** with 300 or 100,000 items, GC per frame equals idle on both phones (296 and 368 B, the rest of the app). The first case's average still holds its warm-up, and the fling averages 100 B per frame more, from a few frames with an allocation.
- **Missed frames:** none on the Pixel 5 at 90 Hz in about 2.5 minutes (the 22 ms frames of the earlier run are gone with the bench's growing lists). One on the A02s at 60 Hz, in the whole run.
- **Heap after a full GC at the end of the soak:** 15.8 MB (A02s) and 23.8 MB (Pixel 5), against 6.0 and 7.9 MB when the run started, with the same 10,000 enemies shown. That's not a leak that grows: the list's layout keeps its buffers (Line and Placement objects) at the largest item count it ever had, 100,000 here, so they can be reused (P11). It's about 10 MB on the 32-bit phone. Trimming them when the item count drops a lot would give that memory back.

**Pool A vs B, and the layout trim, both phones**, 2026-11-13 12:14 (A02s) and 12:17 (Pixel 5). A switches returned slots off (`SetActive(false)`), B keeps them active, parked out of view:

| Case | A02s CPU ms | A02s OnScroll ms | A02s Bind ms | A02s canvas ms | Pixel 5 CPU ms | Pixel 5 OnScroll ms | Pixel 5 Bind ms | Pixel 5 over budget |
|---|---|---|---|---|---|---|---|---|
| Fast fling, pool A | 9.35 | 2.16 | 1.14 | 3.25 | 3.89 | 0.93 | 0.44 | 36 (8.0%) |
| Fast fling, pool B | **8.41** | **0.51** | **0.25** | 3.39 | **3.65** | **0.28** | **0.13** | **14 (3.1%)** |
| Normal speed, 100,000, pool A | 7.69 | 1.04 | 0.49 | 2.14 | 3.32 | 0.44 | 0.19 | 9 (2.0%) |
| Normal speed, 100,000, pool B | **7.57** | **0.34** | **0.13** | 2.21 | **3.21** | **0.16** | **0.05** | **6 (1.3%)** |

Reading it:
- **B cuts the list's own cost by about three quarters** on both phones (OnScroll 2.16 → 0.51 ms in the A02s fling, binding 1.14 → 0.25 ms): reusing a slot no longer wakes its TMP text, Image and scripts. The whole frame's CPU drops by about 1 ms in the A02s fling (9.35 → 8.41 ms). The canvas update barely changes (+0.1 ms: parked slots are still culled every frame), and the batch build halves. On the Pixel, B more than halved the missed frames in the fling.
- **B is now the default.**
- **The layout trim works:** after a full GC at the end of the soak the heap is 5.8 MB (A02s) and 7.5 MB (Pixel 5), back where the run started (6.0 and 7.7 MB). The previous build kept 15.8 and 23.8 MB.
- **The Pixel 5 missed more frames in this run in every case, idle included** (14 idle, 3.1%), with the CPU never past 7 ms of its 11.1 ms. The phone, warm after several runs, or the system dropped refreshes; the run before had none.
- **Scrolling still allocates nothing beyond idle** (296 and 368 B per frame).

**Opening enemies (dynamic sizes), both phones**, 2026-11-13 20:27 (A02s) and 20:31 (Pixel 5). Closed enemies are a fixed 100 px; open ones are measured with their TMP text (one to six sentences). Every enemy starts closed and the bench opens them while scrolling at 4,000 px/s, with several open allowed (as in the Shop):

| Case | A02s CPU ms (avg / max) | A02s over budget | A02s Measure ms | A02s canvas ms (avg / max) | A02s GC B/frame (avg / max) | Pixel 5 CPU ms (avg / max) | Pixel 5 Measure ms | Pixel 5 GC B/frame (avg / max) |
|---|---|---|---|---|---|---|---|---|
| Plain scrolling, 100,000 (for comparison) | 7.60 / 10.94 | 1 (0.3%) | 0.000 | 2.38 / 5.01 | 296 / 296 | 3.15 / 4.34 | 0.000 | 368 / 368 |
| Every 4th opens as it comes into view (32 opened) | 8.34 / **35.05** | 4 (1.4%) | **0.450** | 2.66 / 12.82 | **18,684 / 379,560** | 3.28 / **15.85** | **0.110** | **9,769 / 396,016** |
| Back up over the same open ones (cached) | 7.10 / 21.73 | 0 | **0.000** | 2.48 / 17.01 | 2,580 / 299,800 | 3.04 / 7.33 | **0.000** | 2,519 / 316,488 |
| The middle one opens 4×/s, all stay open (20 opened) | 7.51 / 32.77 | 3 (1.0%) | 0.229 | 2.60 / 15.84 | 4,989 / 462,856 | 3.29 / 12.30 | 0.080 | 5,809 / 483,920 |

Reading it:
- **Dynamic sizes work on the device.** The enemies really opened (the case names count them), and scrolling back over them measured nothing: their sizes came from the cache.
- **Opening one enemy is expensive the first time:** about 4 ms on the A02s and 1.5 ms on the Pixel 5 per opened enemy (Measure's average spread over the opens). That's one layout rebuild of the slot, which makes TMP lay out up to six sentences.
- **Frames that open an enemy can miss a refresh:** CPU peaks of 33–35 ms on the A02s and 12–16 ms on the Pixel 5 (over its 11.1 ms), with up to 4 missed frames per case on the A02s. The averages stay close to plain scrolling.
- **Opening allocates:** up to 380–480 KB in one frame and 5–19 KB per frame on average, against 0 extra for plain scrolling. Even the cached pass (no measuring) allocates up to 300 KB in a frame. That points to TMP rather than the list: a slot showing a longer text for the first time grows TMP's buffers, and new characters are added to a dynamic font atlas. The canvas update's peaks (13–17 ms on the A02s) also fit TMP rebuilding long texts.
- **Plain scrolling with Option B as the default:** the list costs 0.27 ms (A02s) and 0.11 ms (Pixel 5) per frame.
- **Heap:** generated enemies now carry their expanded texts (about 60 MB for 100,000). After a full GC at the end it's 16.4 MB (A02s) and 19.3 MB (Pixel 5) with the Shop's 10,000 enemies held, texts included.

**Opening enemies, measured in Unity's layout pass** (no extra layout rebuild), both phones, 2026-11-13 21:00 (A02s) and 21:04 (Pixel 5). Same cases as the run above, plus text arriving 0.2 s late:

| Case | A02s CPU max ms (before → now) | A02s over budget (before → now) | Pixel 5 CPU max ms (before → now) |
|---|---|---|---|
| Every 4th opens as it comes into view | 35.05 → **29.06** (43 opened, was 32) | 4 → **3** | 15.85 → **11.67** |
| Back up over the open ones (cached) | 21.73 → 21.16 | 0 → 0 | 7.33 → 7.62 |
| The middle one opens 4×/s, all stay open | 32.77 → **25.14** | 3 → **0** | 12.30 → **8.85** |
| Text arrives 0.2 s late, every 4th opens | new: 21.85 | new: 0 | new: 8.30 |

Reading it:
- **Opening is cheaper at its peak:** the slowest opening frame drops by 15–25% on the A02s and by about a quarter on the Pixel 5, now under the Pixel's 11.1 ms budget. The A02s missed no frame in the 4-per-second opening case (3 before). Averages stay within about 1 ms of plain scrolling.
- **Measure reads 0 now** because there's no separate measuring step: the work is part of Unity's canvas update (its average rose from 2.7 to 4.0 ms on the A02s in the first case, with more enemies opened).
- **Late text works on the device:** 43 and 44 enemies opened with their text arriving 0.2 s late, no missed frame on the A02s, and the list followed every late size change.
- **What's left in an opening frame is TMP:** the canvas update's peaks (16–20 ms on the A02s) and up to 380–480 KB allocated in one frame, also when nothing is measured. Pre-filling the TMP font asset (or a static atlas) is the next step on the project side.

### Summary

Final state (pooled slots kept active, open slots measured in Unity's layout pass), Bootgame Shop list:

| | MacBook Pro 2019, Editor | Pixel 5 (90 Hz) | Galaxy A02s (60 Hz) |
|---|---|---|---|
| List's own work, scrolling 100,000 | 0.04–0.06 ms (before Option B) | 0.12 ms | 0.28 ms |
| List's own work, fast fling | — | 0.25 ms | 0.42 ms |
| Whole frame CPU, scrolling | — | 2.9 of 11.1 ms | 7.6 of 16.7 ms |
| Whole frame CPU, fast fling | — | 4.0 ms | 8.3 ms |
| Unity canvas update, scrolling | 0.15–0.20 ms | 0.8 ms | 2.3 ms |
| Extra GC while scrolling | 0 B (tests) | 0 B | 0 B |
| Opening slots while scrolling, slowest frame | — | 8.9–11.7 ms | 25–29 ms (0–3 missed frames per case) |
| Heap after a full GC, after 100,000 items and back | — | back to start | back to start |
| Missed frames, plain scrolling | none from the list | 1–5% per case, idle too (the phone, not the list) | 0–1 per case |

What's left in an opening frame is TMP: laying out and building long text, growing its buffers (up to ~480 KB in a frame), and adding glyphs to a dynamic font atlas. That's project-side: pre-fill the font asset or use a static atlas.
