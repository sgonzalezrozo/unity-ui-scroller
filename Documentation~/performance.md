# Performance

[← Back to README](../README.md)

- [Device results](#device-results)
- [What the tests guarantee](#what-the-tests-guarantee)
- [How it stays cheap](#how-it-stays-cheap)
- [Tips](#tips)

---

## Device results

Measured on five phones, from a 2021 low-end Android to a 120 Hz iPhone, and in a browser (WebGL), scrolling 100,000 items (development players, every case in [`benchmarks.md`](benchmarks.md)):

| | Galaxy A02s | Pixel 2 XL | Pixel 5 | iPhone 13 mini | iPhone Air | WebGL (Chrome) |
|---|---|---|---|---|---|---|
| The list's own work per scroll event | 0.19 ms | 0.20 ms | 0.10 ms | 0.25 ms | 0.07 ms | 0.07 ms |
| …with slots measured by layout (text of any length) | 0.82 ms | 0.74 ms | 0.33 ms | 0.81 ms | 0.22 ms | 0.17 ms |
| Whole frame CPU, app included, of the frame budget | 8.0 of 16.7 ms | 5.2 of 16.7 ms | 3.5 of 11.1 ms | 4.6 of 16.7 ms | 2.0 of 8.3 ms | 1.6 of 16.7 ms |
| Missed frames scrolling, flinging, snapping, paging | ≤ 1.4 % | ≤ 0.3 % | 0 % | ≤ 0.3 % | ≤ 0.3 % | ≤ 0.3 % |

- **Allocates nothing while scrolling**: tests check it on every run, and on the phones every case allocates what the same screen with no list does.
- **Leaves nothing behind**: on all five phones, every list's items and slots were collected after it was destroyed.
- **Long lists cost the same as short ones**: a measured list of 100,000 items scrolls as cheaply as one of 1,000.

Numbers per device, what allocated and how it stopped, and what each optimization measured are in [`benchmarks.md`](benchmarks.md).

## What the tests guarantee

They run on every verify:
- scrolling 100,000 items allocates 0 bytes;
- `Refresh()` with the same count allocates 0 bytes and rebinds slots in place;
- scrolling checks only the slots on screen, and places only the slot that came in;
- no slot ever leaves Content while scrolling;
- rebuilds and new pages only create what's new.

## How it stays cheap

- **The Viewport is a nested Canvas,** so moving slots rebatches only the list, not the rest of the screen.
- **Pooled slots stay active,** parked out of view, where the mask culls them. Reusing a slot doesn't run its `OnEnable` or wake its graphics: on a low-end phone, that cut the list's own cost by about three quarters.
- **Sizes are cached,** and an opened slot is measured in Unity's own layout pass, so TMP lays its text out once.
- **Scrolls under half a pixel** skip the visible-range work.
- **The first screen** shows in the first frame. The pool warms up two slots per frame, and disabling and enabling a list rebinds nothing.

**What still costs a frame:** opening a list creates the slots that fill the screen (42–94 ms in the bench, for a list opened every 0.25 s), and a `Refresh()` with a new count rebuilds the whole layout (about 3 ms at 10,000 items on a low-end phone). Open lists before they're shown, and batch data changes into one `Refresh()`.

## Tips

- Refresh the slot from its `Data` setter, not from `OnEnable` (a reused slot was never disabled).
- Turn off **Raycast Target** on graphics that don't need clicks.
- Use a fixed `DefaultExtent` when every slot has the same size.
- Keep measured slots shallow. Nested layout groups make each first measurement slower.
- With long TMP text that appears when a slot opens, pre-fill the font asset or use a static atlas.
