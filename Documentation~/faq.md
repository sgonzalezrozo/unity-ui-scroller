# FAQ

[← Back to README](../README.md)

**About the project**
- [Is it right for my project?](#is-it-right-for-my-project)
- [Is it production-ready?](#is-it-production-ready)
- [Why was it built?](#why-was-it-built)
- [Is it free for commercial use?](#is-it-free-for-commercial-use)

**Choosing a scroll view**
- [ScrollRect or ListView?](#scrollrect-or-listview)
- [How do I show thousands of items in Unity UI?](#how-do-i-show-thousands-of-items-in-unity-ui)

**Features**
- [Does it allocate while scrolling?](#does-it-allocate-while-scrolling)
- [Does it support grids, bento and variable sizes?](#does-it-support-grids-bento-and-variable-sizes)
- [Does it support infinite scroll?](#does-it-support-infinite-scroll)
- [Does it work with IL2CPP and mobile?](#does-it-work-with-il2cpp-and-mobile)
- [Why doesn't my slot update when I scroll?](#why-doesnt-my-slot-update-when-i-scroll)
- [Does it work with AI coding agents?](#does-it-work-with-ai-coding-agents)

---

## About the project

### Is it right for my project?

**UI Scroller fits if you:**
- build your UI with uGUI and show long lists, grids or store-style layouts;
- target phones, where GC spikes and canvas rebuilds hurt;
- want measured sizes, sections, paging, expanding and bento in one component, with tests behind them.

**Look elsewhere if you:**
- build your UI with UI Toolkit: its `ListView` already virtualizes ([comparison](#scrollrect-or-listview));
- have a short list (a few dozen items) that doesn't need virtualization;
- need features from [Limitations and roadmap](troubleshooting.md#limitations-and-roadmap) that aren't there yet, such as several slot prefabs per list or sticky headers.

**Adopting it, honestly:**
- it's an early release (0.7.2), by a single author, not yet used in a shipped game;
- it's covered by about 520 automated tests (EditMode and PlayMode) that run in batch mode, and by a 45-case benchmark measured on five phones and in WebGL;
- it's MIT licensed, with a [changelog](../CHANGELOG.md) that marks every breaking change.

### Is it production-ready?

It's an early release (`0.7.2`): tested in depth (about 520 automated tests, a 45-case bench on five phones and in WebGL) but by a single author and not yet used in a shipped game, so the API may still change before `1.0.0`. Bug reports are very welcome. See [Project Status](project-status.md).

### Why was it built?

I wrote UI Scroller after trying to fix other open-source scrollers I found on GitHub. I spent weeks on small issues before deciding to write my own and calculate its boundaries my way. After three very long days of personal time I had a first working version, and it has grown from there. I built and refined it in personal projects. In November 2026 I extracted it into this package, documented it and covered it with tests. Earlier history lives in a private repository.

### Is it free for commercial use?

Yes, it's MIT licensed.

## Choosing a scroll view

### ScrollRect or ListView?

| | UI Scroller | [UI Toolkit `ListView`](https://docs.unity3d.com/ScriptReference/UIElements.ListView.html) |
|---|---|---|
| UI framework | uGUI | UI Toolkit |
| Recycles rows | Yes | Yes |
| Item sizes | Fixed or measured per item | Fixed or dynamic height |
| Grids and bento | Built in | Not built in |
| Sections with headers | Built in | Build it yourself |
| Expandable slots | Built in | Build it yourself |
| Paging | Built in | Build it yourself |
| Package to install | Yes | No, it's part of Unity |

If your UI is in UI Toolkit, start with `ListView`.

### How do I show thousands of items in Unity UI?

Use a recycling scroll view: subclass `Scroller<T>`, implement `ISlot<T>` on the slot prefab, and call `Initialize(new ListSource<T>(items))`. Only the visible slots exist. See [Simple list with fixed-size slots](examples.md#simple-list-with-fixed-size-slots).

## Features

### Does it allocate while scrolling?

No. Tests check that scrolling 100,000 items and refreshing with the same count allocate 0 bytes, and the device runs show 0 B per frame. See [Performance](performance.md).

### Does it support grids, bento and variable sizes?

Yes: **Grid**, **Auto Grid** and **Bento** in the inspector's Layout, and `DefaultExtent` of `-1` to size slots with Unity's layout. See [Layouts](layouts.md) and [Item sizes](data-and-sizes.md#item-sizes).

### Does it support infinite scroll?

Yes, with `PagedSource<T>` and an `IPageLoader<T>`. See [Paging](scrolling.md#paging).

### Does it work with IL2CPP and mobile?

Yes: it's tested with IL2CPP on Android (Galaxy A02s, Pixel 2 XL, Pixel 5), iOS (iPhone 13 mini, iPhone Air) and WebGL, with a 45-case bench on each. See [Requirements](getting-started.md#requirements) and [`benchmarks.md`](benchmarks.md).

### Why doesn't my slot update when I scroll?

Slots are reused: reset every visual in the `Data` setter, not only the ones that change.

### Does it work with AI coding agents?

Yes. See [AI Coding Agents](ai-agents.md).
