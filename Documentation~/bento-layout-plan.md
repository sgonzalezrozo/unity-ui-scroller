# Bento layout plan

A new **Bento** layout for Slot List: an asymmetric grid where each item is a tile covering one or more cells (1×1, 2×1, 1×2, 2×2, and so on), and only the tiles on screen have slots.

Status: in progress on `feat/bento-layout`. Target: **1.1.0**, after the 1.0.0 release.

## Progress

- **B1 done:** `BentoSpan`, `BentoTile`, `BentoPattern`, `BentoLayout` (JSON through `JsonUtility`, `BentoLayoutData`, or code), validation with a 1x1 fallback, the ordered packer and the pattern arranger. 29 EditMode tests.
- **B2 done:** rows of cells are lines (`LayoutModel.BuildBentoLines`), tiles are placed through `PositionInLine` plus a cross-axis stretch, the visible range reaches up by the tallest tile, and the pool is sized from rows × columns. Also done early: the expand and loop guards (from B3), `GetTile`, tile sizes in `GetItemSize` and `ScrollToItem` (from B4), width changes, `SetBentoLayout` after `Initialize`, and appended pages (from B5). 18 PlayMode tests; all 447 tests pass.
- **Sample done:** `BentoExample` in the Examples sample: patterns and spans written in JSON, switched at runtime with `SetBentoLayout`. Smoke-tested in Play mode.
- **Inspector done:** **Layout: Bento** with **Layout JSON** (a TextAsset), **Cell Aspect** and the column spacing underneath; Allow Multiple Expanded and Loop hidden; JSON errors and sections shown as an error box. 6 EditMode tests.
- **Fix found while testing:** a page that lands during the bounce at the end no longer drifts the list back towards the start (both directions; it showed most bottom to top). 459 tests pass.
- **Merged into `main`** (2026-11-14).
- **Sections done:** bento in a `SectionedSlotList`. A header is a line of its own across every column, as tall as the header (fixed, or measured per header); each section's rows start below it and the layout starts again (decision: restart per section, one layout for all sections). A section ends after its last row, so a tall tile never crosses a header. `BentoSectionsExample` with measured headers of different heights. Also confirmed by tests: headers of different heights in a sectioned list and grid (no bento) already worked.
- **Left:** item snapping to tile starts, `VisibleStartIndex` ignoring tiles in the extra rows above the screen, the pattern asset with a grid editor, README and agent rules.

## Decisions (B0, 2026-11-14)

1. **A, B and C, all defined in JSON.** The list is given a layout: a span sequence (A) or patterns (B), written in JSON (C) or built in code from the same model. **Items don't decide their own size**: there's no `IBentoItem`, and no per-item override in the first version.
2. **Ordered packing.**
3. **Row height from Cell Aspect** (a list setting, so the JSON only describes the distribution).
4. **Incomplete patterns leave their cells empty.**
5. **A Layout mode on `SlotList<T>`**, not a new class.
6. Slot types (alternative E): not decided; not part of this work.

## Ground rules

1. **It's a separate feature.** List, Grid and Auto Grid keep working exactly as today. Their code paths don't change, except where the engine picks a layout, and all existing tests must pass untouched at every step.
2. **No expanding.** As in Grid, opening one tile would change how every other tile in the same rows looks. `Expand` on a bento list logs a warning once and does nothing, as Grid does today.
3. **Fixed sizes.** A tile's size comes from its cells, never from measuring. A measured slot (`DefaultExtent` of 0 or less) isn't measured in a bento list; that logs a warning once.
4. **Test-first**, as in the rest of the refactor. Pure logic is covered by EditMode tests, the list by PlayMode tests through `SlotListRig`.

## Vocabulary

| Term | Meaning |
|---|---|
| **Columns** | Cells across the list: across the width in a vertical list, the height in a horizontal one. |
| **Cell** | One column wide and one row tall. The column width comes from the list's width, the column count and the slot spacing, exactly as Grid stretches slots today. |
| **Row** | One cell tall along the scroll axis. Its height is the cell width × **Cell Aspect** (default 1, square cells). Each row is a `Line` in the existing layout model. |
| **Span** | A tile's size in cells: `columns × rows`. |
| **Tile** | Where an item sits: start column, start row, and span. |
| **Arrangement** | What turns items into tiles. The alternatives are below. |

## How the distribution is defined: alternatives

Every alternative ends up producing the same thing: a `BentoTile` for each item index. The engine only ever sees tiles. That keeps the alternatives independent of each other and of the engine, so we can ship one and add others later without touching the list.

### A. Spans from code or data, packed automatically

Each item says how big it is, and a packer places it in the first gap that fits.

```csharp
// On the item...
public class ShopItem : IBentoItem
{
    public BentoSpan Span => IsFeatured ? new BentoSpan(2, 2) : BentoSpan.One;
}

// ...or on the list, without touching the item type.
public class ShopList : SlotList<ShopItem>
{
    protected override BentoSpan GetTileSpan(int index)
        => DataSource.GetItem(index).IsFeatured ? new BentoSpan(2, 2) : BentoSpan.One;
}
```

- **Good for:** content-driven emphasis (featured, promoted or new items bigger), any item count, and paging (new tiles go after the old ones, and nothing already placed moves).
- **Costs:** the look isn't designed tile by tile. Ordered packing can leave holes (see *Packing order*).
- **Effort:** small: a pure packer with EditMode tests.

### B. Predefined patterns (templates)

A designer draws one or more blocks of cells, and the items fill the blocks in order. Patterns repeat: in a sequence (A, B, A, B…), or a fixed intro followed by a repeating tail.

```text
Pattern "Hero", 4 columns × 2 rows:      Pattern "Strip", 4 × 1:
┌───────┬───────┐                        ┌───┬───┬───┬───┐
│       │   1   │                        │ 0 │ 1 │ 2 │ 3 │
│   0   ├───┬───┤                        └───┴───┴───┴───┘
│       │ 2 │ 3 │
└───────┴───┴───┘
```

Authored as a `BentoPattern` ScriptableObject (a small grid editor in the inspector) or built in code. The list gets a **Patterns** field, shown only in Bento mode.

- **Good for:** the classic designed "bento" look, with no holes and a predictable rhythm. Designers own it without touching code.
- **Costs:** the item doesn't choose its size. A featured item only gets the big tile if it lands on that position, unless it's combined with A (see *Combining*). The pattern must be designed for the column count.
- **Effort:** small. Resolving a pattern is a lookup, with no packing. The inspector grid editor is the largest part.

### C. JSON (server-driven layout)

The arrangement arrives from a backend or a remote config, so live ops can change the layout without an app update. JSON is a **format**, not a different model: it describes patterns (B), spans (A) or explicit positions (D).

```json
{
  "columns": 4,
  "patterns": [
    { "rows": 2, "tiles": [ {"c":0,"r":0,"w":2,"h":2}, {"c":2,"r":0,"w":2,"h":1}, {"c":2,"r":1,"w":1,"h":1}, {"c":3,"r":1,"w":1,"h":1} ] },
    { "rows": 1, "tiles": [ {"c":0,"r":0,"w":1,"h":1}, {"c":1,"r":0,"w":1,"h":1}, {"c":2,"r":0,"w":1,"h":1}, {"c":3,"r":0,"w":1,"h":1} ] }
  ],
  "repeatFrom": 1
}
```

- The runtime gets `[Serializable]` data classes that Unity's `JsonUtility` can fill, plus `BentoPattern.FromData(...)`. The package adds no JSON library and no network code; fetching is the project's job.
- Every input is **validated**: tiles outside the columns, overlapping tiles, empty patterns, spans under 1. Bad data logs one clear error and falls back to 1×1 tiles, so a broken config never breaks the screen.
- **Good for:** live ops and A/B tests of layouts.
- **Costs:** validation and error reporting matter more, because the data isn't checked by a designer in the editor.
- **Effort:** small on top of B (and A): the data classes, validation and a sample.

### D. Explicit positions per item

Each item carries its exact tile (column, row, span), computed elsewhere: a CMS, a level editor, or a server.

- **Good for:** full control, and layouts that can't be expressed as patterns.
- **Costs:** brittle. Inserting or removing an item means recomputing positions, paging needs coordinated positions, and overlaps have to be validated. It's rarely what you want for a scrolling list.
- **Effort:** small (validation only). Suggested as **later**, only if a real need shows up.

### E. From the slot itself (the item's view decides)

The tile size comes from the slot that shows the item. For example, a `HeroTile` prefab is 2×2 and a `SmallTile` prefab is 1×1, and the item's type picks the prefab.

- **Good for:** cases where a different size also means a different look (a big card with a video next to small icons). Size and visuals stay together in the prefab.
- **Costs:** a list has **one slot prefab** today (plus a header prefab in a sectioned list). This needs **slot types**: several prefabs per list, each with its own pool, chosen per item. That's a feature of its own, useful beyond bento (feeds with ads, mixed rows). Reading the size from the slot's *measured* layout isn't viable: a cell's width depends on the column count, so a measured size would have to be rounded to cells, which is circular and would jitter.
- **Effort:** medium to large, mostly for slot types. Suggested as a **separate feature** after this one. Once it exists, E is just "the slot type declares its span", which feeds alternative A.

### Combining

Since everything becomes tiles, combinations are cheap:

- **Patterns with a span override (B + A):** a pattern position can be marked *flexible*, and an item that asks for a bigger span takes a big position. This is a possible later step; it isn't needed for the first version.
- **JSON for either (C over A, B or D).**

### Recommendation

| Phase | Alternatives | Why |
|---|---|---|
| First version | **A (spans, ordered packing)** and **B (patterns)** | Together they cover content-driven and designed layouts. Both are small pure code that's easy to test. |
| Same release, if time allows | **C (JSON)** for patterns and spans | Little extra work once A and B exist. |
| Later, on request | **D (explicit)**, **B + A (flexible positions)** | Wait for a real need. |
| Separate feature | **E**, through **slot types** | A bigger feature with value of its own. |

## Packing order (for spans)

| Option | Behaviour | Verdict |
|---|---|---|
| **Ordered** | Each tile goes in the first gap at or after the previous tile's position (row by row, left to right). Item order matches reading order. Holes can remain where a big tile didn't fit. | **Recommended** |
| **Dense** | Each tile goes in the first gap anywhere, so later small tiles fill earlier holes. | Not in the first version |

Why ordered: the layout model assumes each line holds a contiguous run of item indexes, and that line order follows item order. `VisibleStartIndex` and `VisibleEndIndex`, `ScrollToItem`, paging and snapping all rely on it. Dense packing breaks that (item 40 could sit above item 35), and new pages could fill holes above the screen. Ordered packing keeps all of it working, and reading order stays predictable for players. Holes are limited, because a gap is only skipped when the next tile is wider than what's left in the row. A designer who wants zero holes uses patterns.

How the packer works: it keeps an occupancy mask for the open rows, no more rows than the tallest span. For each item it scans from the previous tile's row and column for the first spot where the span fits, marks those cells, and moves on. That's O(columns × tallest span) per item and allocates nothing per item. It's incremental, so appending a page continues from the saved state instead of starting over.

Spans wider than the column count are clamped to it, with a warning once. Spans under 1 count as 1.

## How it fits the engine

The key idea: **each row of cells is a `Line`.** The layout model, offsets, visible-range search, recycling and content size all already work on lines, so the bento layout reuses them.

| Piece | Change |
|---|---|
| `LayoutModel` | New `BuildBentoLines(...)` next to `BuildLines`. Each row is a line, all lines are the same size (row height), and a line holds the items **starting** in that row. Rows covered only by a tall tile have an empty line. Tiles are stored per placement (start column, column span, row span). `BuildLines` doesn't change. |
| `Placement` | Internal fields for the tile. `IsPlacedAt` also compares the tile, so slots that didn't move still skip placement (P3). |
| Visible range | A tile can start above the screen and reach into it. After the binary search, the first visible line moves up by the tallest row span minus one, in bento mode only. Recycling already keeps any slot whose start line is in range. |
| Placing slots | A new internal placement step positions a slot from its tile: cross start = column × (cell + spacing), cross size = span × cell + (span − 1) × spacing, main start = row offset, main size = rows × row height + (rows − 1) × line spacing. It works for all four directions. `IAxisLayout` is public, so the new method goes in an internal helper rather than on the interface, to avoid breaking anyone who implements it. |
| Offsets and content size | No change: rows have equal sizes, spaced by **Line Spacing** (rows) and **Slot Spacing** (columns), as in Grid. |
| Item queries | `GetItemSize(index)` returns the tile's main size, `GetItemOffset(index)` its row offset. |
| `ScrollToItem` | Aligns using the tile's size instead of its line's size (Center and End on a 2×2 tile). |
| Snap | Item snapping lines up the nearest tile start. Page snapping doesn't change. |
| Pool size | Estimated from the visible rows plus the tallest span, × columns. |
| Paging | Appending continues the packer or pattern from its saved state. Only the new rows are built, as `BuildLines(fromItem)` does today. |

What stays out of bento mode, each with a clear message:

| Feature | In a bento list |
|---|---|
| Expand | Warning once, ignored (as in Grid) |
| Loop | Error once, doesn't loop (as with Grid) |
| Sections (`SectionedSlotList`) | Supported: a header takes a full-width row as tall as the header, and the layout starts again in each section. |
| Measured slots | Warning once, the tile size is used |
| Auto Grid columns | The first version has a fixed column count. Later, possibly: patterns per column count, chosen by width. |

## Settings and inspector

Kept minimal, and shown only when **Layout** is **Bento**:

| Field | Meaning |
|---|---|
| Layout: **Bento** | A fourth choice next to List, Grid and Auto Grid. Stored as its own flag, so the existing `slotsPerLine` and `autoSlotsPerLine` meanings don't change. |
| **Columns** | Cells across the list. Reuses `slotsPerLine`, so switching Grid ↔ Bento keeps the number. |
| **Cell Aspect** | Row height ÷ column width. 1 is square. |
| **Patterns** | Optional. Empty means spans from code (`IBentoItem` or `GetTileSpan`, 1×1 by default). |
| Line Spacing, Slot Spacing, Padding | As today. |

From code: `list.Settings.bento = true; list.Settings.slotsPerLine = 4; list.Settings.cellAspect = 1f;` before `Initialize`, with `list.SetBentoPatterns(...)` or a `GetTileSpan` override.

## Public API (draft)

```csharp
public readonly struct BentoSpan { public readonly int Columns; public readonly int Rows; public static readonly BentoSpan One; }
public readonly struct BentoTile { public readonly int Column, Row, Columns, Rows; }

public interface IBentoItem { BentoSpan Span { get; } }

// SlotList<T>
protected virtual BentoSpan GetTileSpan(int index);    // default: IBentoItem's span, or 1×1
public void SetBentoPatterns(BentoPatternSequence patterns);
public BentoTile GetTile(int index);                    // read-only, for UI and tests

// Patterns
public class BentoPattern : ScriptableObject { int Columns; int Rows; BentoTile[] Tiles; }
public class BentoPatternSequence { BentoPattern[] Patterns; int RepeatFrom; }
[Serializable] public class BentoPatternData { ... }   // JsonUtility-friendly
public static BentoPatternSequence BentoPatternSequence.FromData(BentoLayoutData data);
```

Names are a draft: they'll get the same naming pass as the rest of the API before they ship.

## Incomplete patterns and holes

When the items run out in the middle of a pattern (the last page, or a short list):

| Option | Result | Verdict |
|---|---|---|
| **Leave the cells empty** | The pattern keeps its shape, with gaps at the end | Simple, and honest with patterns |
| **Fill with 1×1** | The remaining items pack as 1×1 in the pattern's rows | Fewer gaps, but the last block looks different |
| **Fall back to a tail pattern** | A designer-chosen pattern for the end | Most control, more to author |

Recommended: **leave empty** in the first version, with the other two as possible options later.

## Phases (test-first)

| # | Step | Tests | Done when |
|---|---|---|---|
| B0 | Settle the open decisions below | — | Decisions written down here |
| B1 | `BentoSpan`, `BentoTile`, the ordered packer, the pattern resolver | EditMode: 1×1 rows; mixed spans; holes; clamping; order is monotonic; incremental append equals a full build; patterns cycle and `RepeatFrom`; incomplete pattern | Pure code, no engine changes |
| B2 | `LayoutModel.BuildBentoLines`, tiles on placements, the widened visible range, the tile placement helper | PlayMode with `RigOptions.Bento`: tiles at the expected rects in all four directions; a tall tile that starts above the screen stays shown; slots recycle; no slot moves on a sub-row scroll | A code-only bento list scrolls correctly |
| B3 | Settings, the inspector **Bento** choice, and the guards (expand, loop, sections, measured slots) | EditMode editor tests; PlayMode: `Expand` is ignored with a warning; loop and sections log errors | Usable from the inspector |
| B4 | Queries and navigation: `GetTile`, `GetItemSize`, `GetItemOffset`, `ScrollToItem` with tile sizes, snapping | PlayMode: center a 2×2 tile; snap to tile starts | API complete |
| B5 | Paging | PlayMode: pages append without moving tiles on screen; a pattern continues across pages | Works with `PagedSource` |
| B6 | `BentoPattern` asset with an inspector grid editor; JSON data classes and validation | EditMode: valid and invalid JSON, overlaps, out of bounds | Designers and live ops can author layouts |
| B7 | `BentoExample` in the Examples sample (patterns and spans), README section, CHANGELOG, agent rules, a benchmark with 10,000 tiles | Smoke test of the sample; full verify | Documented and released as 1.1.0 |

At every step: the existing 400 tests pass, and Grid and List behaviour don't change. B1 to B3 are the minimum useful feature. B4 to B7 can ship in the same release or a follow-up.

## Open decisions

1. **Which alternatives in the first version?** Recommended: A (spans) and B (patterns), with C (JSON) if time allows.
2. **Packing order:** ordered (recommended) or dense.
3. **Row height:** Cell Aspect (recommended; rows follow the width, so tiles keep their shape on every screen) or a fixed row height in pixels.
4. **Incomplete patterns:** leave empty (recommended), fill with 1×1, or a tail pattern.
5. **A Layout mode on `SlotList<T>` (recommended) or a new `BentoSlotList<T>` class?** A mode matches how Grid works and needs no new component. A separate class isolates the code even more, but means another component to learn.
6. **Slot types** (alternative E, several prefabs per list): plan them as the next feature after bento, or not at all?
