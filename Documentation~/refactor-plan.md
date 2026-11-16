# Refactor plan: from Recyclable Scroller to Slot List

> **Renamed:** Slot List is now **UI Scroller** (`com.sgonzalezrozo.ui-scroller`, namespace `UIScroller`, `SlotList<T>` → `Scroller<T>`, `SectionedSlotList` → `SectionedScroller`, `SlotListBase` → `ScrollerBase`). This document keeps the old names as a record of how the work was done.

A step-by-step plan for turning `com.sgonzalezrozo.recyclable-scroller` (0.3.0) into a new, renamed, uGUI-only recycling scroller. Work through the phases in order, and tick the boxes as you go.

**Progress:** see [`refactor-status.md`](refactor-status.md) for what each phase did, what changed from this plan, what was skipped and what's left. The checkboxes below were only ticked from Phase 8 on.

**Package code and the architecture rules:** the scroller is a tool, not feature code, so the strict MVC rules from `unity-architecture-agents` don't bind its internals. It *could* follow them, but forcing MVC onto a layout and recycling engine isn't the best fit. The engine follows this plan's structure instead (Phase 5), while public names are chosen so the client code that uses it can follow the rules (Phase 12.4).

**Rules for every phase**

- One branch and one pull request per phase (for example `refactor/p1-remove-uitoolkit`). Merge to `main` only when that phase's **Done when** list passes.
- Test-driven: follow *How we work* below. Tests come first, `scripts/verify.sh` (EditMode + PlayMode) must be green before every merge, and only "feel" items are checked by hand.
- After each phase, open the test project, wait for it to compile, and run the manual part of the behaviour checklist from Phase 0.
- Commit often inside a phase. A commit that doesn't compile is fine on the branch, but not when you merge.
- Unity rule for renames: **rename the `.cs` file and its `.meta` together, and keep the `.meta`'s GUID**. Prefabs and scenes refer to scripts by GUID, so they keep working. A MonoBehaviour's class name must match its file name.

**What we know from using it (keep these behaviours)**

1. It works and is easy to use. Don't make the simple case harder: an item class, a cell prefab, a one-line subclass, then `Initialize`.
2. In grid mode, cells are scaled so the configured number of cells fills the row. Keep this.
3. The Header Prefab is optional and only used with sections ("multi-source").
4. Nested scrollers are out of scope. They're handled by a separate MIT plugin, so we only check that the two work together.
5. The size calculator is the hardest part to use, because you can't predict text size ahead of time, especially with localization. Phase 8 replaces it with Unity's own layout system on real instances.
6. MyTools (`/Users/sgrozo/Documents/Personal/MyTools`) loads this package from disk, so every change shows up there straight away. It's the test project for every phase.

---

## How we work: test-driven

Every phase follows test-driven development (TDD). No production code changes without a test that asks for the change, except pure deletions and renames, which the existing suite protects.

### The loop
1. **Red:** write one small test for the next behaviour. Run it and **watch it fail for the reason you expect**. A test that passes straight away tests nothing new: fix the test.
2. **Green:** write the least code that makes it pass. Don't add anything the test doesn't ask for (this matches `ARCH-10` and `ARCH-11` in the AI rules).
3. **Refactor:** clean up the code and the test with every test green. Run the whole suite.
4. Commit. One commit per red-green-refactor cycle is a good rhythm (`test:` and `feat:`/`fix:` together in one commit is fine).

### Three kinds of tests, used at different times

| Kind | When | What it pins | Where |
|---|---|---|---|
| **Characterization tests** | Phase 0, before touching anything | What the code does **today**, bugs included, so cleanup, renames and splits can't change behaviour by accident | EditMode + PlayMode |
| **Bug tests** | Phase 6 | The **correct** behaviour for each known bug. Written in Phase 0 as `[Ignore("Bug 6.x")]`, switched on and made green in Phase 6 | EditMode or PlayMode |
| **Design tests (TDD proper)** | Phases 5, 7–11 | The behaviour of new code, written **before** the code exists. They define the API | Mostly EditMode, against the plain C# engine |

### Test assemblies (inside the package)

| Folder | Assembly | Platform | Contents |
|---|---|---|---|
| `Tests/Editor/` | `RecyclableScroller.Tests.Editor` (→ `SlotList.Tests.Editor` in Phase 4) | Editor only | Pure logic: layout maths, data sources, detectors, engine. No frames, fast |
| `Tests/Runtime/` | `RecyclableScroller.Tests.PlayMode` (→ `SlotList.Tests.PlayMode`) | Any (PlayMode) | The component on a real Canvas: binding, recycling, scrolling, layout sizing, allocations |
| `Tests/Runtime/Support/` | (same assembly) | | Test helpers shared by PlayMode tests |

- Each asmdef references the runtime assembly, `UnityEngine.TestRunner`, `UnityEditor.TestRunner` (Editor only) and `nunit.framework.dll` (`overrideReferences` + `precompiledReferences`), with `defineConstraints: ["UNITY_INCLUDE_TESTS"]`.
- MyTools lists the package in `testables` (done), so verify runs these tests.
- Tests that need MyTools-only code (the sections code from the other project, until Phase 9) live in `MyTools/Assets/SlotListTests/Tests/` with their own test asmdef.

### Test helpers (write these in Phase 0)
- **`ScrollerRig`** builds everything in code, with no scene or prefab assets: a Canvas (Screen Space Overlay, `CanvasScaler` with a fixed reference resolution), a scroller of a fixed size (for example 400 × 800), Viewport with `RectMask2D`, Content, and a cell template. The template is an inactive GameObject used as the prefab, since `Instantiate` works on any GameObject. Options: direction, grid count, spacing, padding, item count, cell size, header template. It sets `inertia = false` and `movementType = Clamped` so tests are deterministic. `Dispose()` destroys everything.
- **`RecordingCell`** is the test cell. It records every bind (`List<(int index, TestItem item)>`) and exposes its current item, so tests can ask "which items are on screen and where".
- **`TestItem`** is a tiny class with `Id` and `Text`.
- **Rig methods:**
  - `ScrollTo(float offset)` sets the content position directly and waits one frame.
  - `BoundIndices()` returns the indices of active cells, sorted.
  - `CellAt(int index)` returns the RectTransform showing that item, or null.
  - `ActiveCellCount` and `PooledCellCount`.
- **`FakePageLoader`** (Phase 7): configurable page size, total, delay (`Task.Delay` or a manual `TaskCompletionSource`), failure on page N, and a log of requested pages.
- **`ManualClock`** (Phases 8 and 11) for budgets per frame and animations, so tests step frames by hand instead of waiting for real time.

### Naming and structure
- File per class under test: `LinePositionCacheTests.cs`.
- Test names say behaviour: `Offsets_IncludePaddingAndSpacing`, `Scroll_PastFirstScreen_RecyclesTopCells`, `Bug_6_1_InitializeWithEmptySource_IsReady`.
- Arrange, Act, Assert, with one behaviour per test. No logic (loops, ifs) in tests beyond building data.
- PlayMode tests are `[UnityTest] IEnumerator` with explicit `yield return null` frames. Never `WaitForSeconds` with real time.

### Verify runs both suites
- [ ] **Phase 0 task:** extend `MyTools/scripts/verify.sh` to run **EditMode, then PlayMode** (`-testPlatform PlayMode`), each with its own results file, and fail if either fails. It's slower (a second Unity launch, roughly 30–60 s more), so allow `VERIFY_EDITMODE_ONLY=1` for quick loops. Then propose the same change upstream to the rules repo's template.
- The Stop hook then refuses to let an agent finish while any test is red.

### Coverage targets
- Every public method of the engine and data sources has EditMode tests.
- Every row of the Phase 0 behaviour checklist that can be automated has a PlayMode test. Only "feel" items (smoothness, how a fling looks) stay manual in the MyTools scenes.

---

## Phase 0: Safety net

There are no tests, and Phases 1–5 move and rename almost every file. Record how things behave today before changing anything.

### 0.1 Baseline tag
- [ ] `git tag v0.3.0-baseline && git push origin v0.3.0-baseline`

### 0.2 Test project: MyTools
The test project is **MyTools** (`/Users/sgrozo/Documents/Personal/MyTools`, Unity 6000.3.6f1). It already loads the package from disk, so every change to the package shows up there straight away:
```json
"com.sgonzalezrozo.recyclable-scroller": "file:../../Packages/unity-recyclable-scroller"
```

What MyTools uses today. These files break when names change, so update them in each phase that renames something:

| File in `MyTools/Assets` | Uses |
|---|---|
| `Client UGui/Shop/Scripts/ShopScrollerRecyclableScrollRect.cs` | `RecyclableScrollRect<EnemyData>` |
| `Client UGui/Shop/Scripts/ShopScrollerInstantiator.cs` | `IScrollSource<T>`, `ListDataSource<T>`, `IsInitialized` / `Initialize` / `SetDataSource` |
| `Client UGui/SomeFeature/EnemyModel.cs` | `IRecyclableCell<EnemyData>` |
| `FastIoC/Generated/GeneratedMonoInjectors.cs` | Writes `target.injectedInstantiator` directly. Regenerate after renaming that field. |

**AI agent setup in MyTools (done on 2026-11-11):**
- `.ai` submodule = `unity-architecture-agents` `v0.2.0`. Root `CLAUDE.md` loads both `AGENTS.md` (MyTools' own rules) and `.ai/AGENTS.md`.
- `scripts/verify.sh` (batch compile + EditMode tests + Fast IoC generation check) and the Claude Code Stop hook `.claude/hooks/verify-on-stop.sh`. The baseline verify passed on 2026-11-11 (1 test, about 30 s).
- The hook also watches every local `file:` package in `Packages/manifest.json`, including this one, but ignores `~` folders such as `Documentation~`. Editing the package now triggers verify when an agent finishes.
- `Packages/manifest.json` → `"testables": ["com.sgonzalezrozo.recyclable-scroller"]`, so the package's tests (Phase 0.5) run in verify.
- **Start AI sessions for this refactor in MyTools** and add the package folder to the session. Claude only loads hooks and `CLAUDE.md` from the folder a session starts in.
- **Close MyTools in Unity** while an agent works, or verify reports "couldn't run" and the change goes unchecked.

- [ ] Work in MyTools on a branch that matches the package branch. While a package branch is checked out, MyTools sees that code.
- [ ] Put all test scenes and scripts in **`MyTools/Assets/SlotListTests/`** so they're separate from the shop feature.
- [ ] MyTools already has `com.unity.localization` 1.5.9. Use it in the sizing scenes (Phase 8).
- [ ] Copy the sections ("multi-source") code from the other project into `Assets/SlotListTests/Sections/`: `CompositeDataSource<T, U>`, `IScrollMultiSource<T, U>`, the scroller subclass that uses them, and the header and item cells. This is the only working copy of the header feature, so we have to keep it running.

### 0.3 Manual scenes (only what tests can't judge)
The PlayMode suite covers what the 14 per-feature scenes were meant to check. Manual scenes are only for **feel** (fling, inertia, edges, no jitter), **real input** (mouse or touch drag, pull gesture, window resize) and **looks** (real prefabs on a real canvas). Build them in the Editor, never as scene text.

| Scene | Status | Checks |
|---|---|---|
| `Assets/Scenes/Bootgame/Bootgame.unity` | **Exists.** The shop scroller with 25 enemy items, a real prefab, Fast IoC and Screen System | After every phase: open, scroll, fling, hit both edges |
| `SlotListTests/Grid` | Add when Phase 6.6/6.7 starts | Grid fill and window resize |
| `SlotListTests/Gestures` | Add when Phase 7 starts | Expand and collapse, pull to refresh with a real drag |
| `SlotListTests/Sections` | Add with the sections code (Phase 9, or earlier when the code arrives) | Headers in a list and a grid |
| Phase 8 sizing scenes (8.8) | Add in Phase 8 | Text and localization: nothing visibly jumps |

### 0.4 Behaviour checklist
- [ ] One line per manual scene in `Documentation~/behaviour-checklist.md`: what "correct" looks like. Start with `Bootgame`.

### 0.5 Test infrastructure
- [ ] Create `Tests/Editor/` and `Tests/Runtime/` with the asmdefs from *How we work*.
- [ ] Write the helpers: `ScrollerRig`, `RecordingCell`, `TestItem`.
- [ ] Extend `MyTools/scripts/verify.sh` to run PlayMode too (see *Verify runs both suites*).
- [ ] One smoke test per assembly, run through `scripts/verify.sh`, so you know the plumbing works.

### 0.6 Characterization tests: EditMode (pure logic, today's classes)
Pin what the code does now. If a result looks wrong, still assert today's value, and add a `// Bug 6.x` comment plus an `[Ignore]` bug test (0.8).

- [ ] `CellLineBuilderTests`: a 1-column list gives one line per item. A 3-column grid with 10 items gives 4 lines, and the last has 1 filled slot. A header breaks a row (the item after a header starts a new line). Line sizes come from the first slot.
- [ ] `LinePositionCacheTests`: offsets = paddingStart + Σ(size + spacing). Out-of-range indices return 0.
- [ ] `LineContentSizeCalculatorTests`: total = padding + sizes + spacing between lines (not after the last).
- [ ] `GridCountResolverTests`: fixed count. Automatic count from width, spacing, target size, clamped to min/max. Count is never below 1.
- [ ] `VisibleLineFinderTests`: start and end lines at the top, in the middle, at the end, and for reversed vertical and horizontal.
- [ ] `CellExpansionControllerTests`: expand, collapse, toggle by expanding again, only one at a time when multiple isn't allowed, and the line size after expand and collapse.
- [ ] `ListDataSourceTests`: every mutator. Nulls are skipped. `GetItem` out of range throws.
- [ ] `FilteredDataSourceTests`: count and items with and without a filter, and after changing the filter.
- [ ] `PaginatedDataSourceTests` (async, with a fake provider): loads page 0 then 1. No parallel loads. Stops at `HasMore == false`. `Reset` reloads page 0. A failed load leaves the state unchanged. Today's preload from `GetItem` within 10 items of the end is pinned too (it changes in Phase 7).
- [ ] `PaginationBoundaryDetectorTests`: triggers at 96%, doesn't trigger again until it drops below 95% or `ResetTriggerFlag()`.
- [ ] `PullToRefreshDetectorTests`: a pull past 75 px in each of the 4 directions.
- [ ] Direction classes: `CalculateNormalizedScrollPosition` and `PositionCellInLine` for each of the 4 directions (EditMode can create RectTransforms). Pin the grid fill: 3 columns of width (viewport − 2 × spacing) / 3.

### 0.7 Characterization tests: PlayMode (the component through its public API)
Use `ScrollerRig`. These tests only use public API, so after Phase 4's renames they keep working unchanged. **They're the main safety net for Phases 1–5.**

- [ ] `Initialize_BindsFirstScreen`: 1,000 items of 100 px in an 800 px viewport binds items 0..N (pin N as today's value).
- [ ] `Initialize_PoolSize_IsBounded`: total cells ≤ ceil(visible × 1.4) × gridCount. Never one per item.
- [ ] `Scroll_ToMiddle_BindsExpectedRange` and `Scroll_ToEnd_BindsLastItems`.
- [ ] `Scroll_RecyclesCells`: scrolling 10 screens down creates no new cells after warm-up.
- [ ] `Cell_IsPlacedAtLineOffset`: anchoredPosition and size of a bound cell match the layout maths.
- [ ] Grid: a 3-column grid fills the width, and the lines and slots of bound items are right.
- [ ] Horizontal, reversed vertical and reversed horizontal: first-screen binding and placement.
- [ ] `Refresh_AfterAdd_ShowsNewItem`, `Refresh_AfterRemove`, `SetDataSource_ReplacesItems`.
- [ ] `Expand_ChangesContentSize_AndMovesFollowingCells`, `Collapse_RestoresIt`, and only one expanded when multiple isn't allowed.
- [ ] `ScrollToStart`, `ScrollToEnd`, and `ScrollToItem` (pin today's result, even if it's the mirrored bug 6.2).
- [ ] `IsItemVisible` and `VisibleStartIndex`/`VisibleEndIndex` after scrolling.
- [ ] `DisableEnable_RebindsVisibleItems`.
- [ ] Pagination: reaching the threshold calls the override, and paging adds items after `Refresh()`.
- [ ] Pull to refresh: a drag released past 75 px calls the override (simulate with `OnBeginDrag`/`OnEndDrag` and a moved content position).
- [ ] **Sections (in MyTools, `Assets/SlotListTests/Tests/`)**: the copied `CompositeDataSource` scroller binds header cells to headers and item cells to items, and headers take a full line in a grid.

### 0.8 Known bugs as ignored tests
For each bug in the Phase 6 table, write the test for the **correct** behaviour now and mark it `[Ignore("Bug 6.x: <one line>")]`. Phase 6 removes the `[Ignore]` and makes it green.
- [ ] 6.1 empty source, 6.2 scroll-to direction (this also confirms whether the bug is real), 6.3 expand fallback, 6.6 resize, 6.7 cross padding, 6.10 refresh keeps expansion, 6.13 item-index API.
- [ ] Headers with the package's own `IsHeader` override + Header Prefab (not through multi-source), pinned as broken (`[Ignore("Phase 9")]`).

**Done when:** the tag is pushed, `Bootgame` is checked by hand, the checklist is written, every automatable checklist row has a characterization test, all characterization tests pass, the bug tests exist and are ignored, and `scripts/verify.sh` runs both suites green.

---

## Phase 1: Remove UI Toolkit

**Tests first:** no new tests. These are deletions, and the Phase 0 suite protects them. Run the full suite (`scripts/verify.sh`) after every deletion. A red test means the deleted code was in use: restore it and investigate.

- [ ] Delete `UIToolkit/` and `UIToolkit.meta`.
- [ ] Check that nothing else refers to it: `grep -rn "UIToolkit" --include='*.cs' --include='*.asmdef' .` should return nothing.
- [ ] Docs: remove the UI Toolkit lines from `README.md` (the *Is it right for your project?* section, the compatibility table, the code layout section), `llms.txt`, `Documentation~/agent-rules.md`, and the `package.json` description if mentioned.
- [ ] `CHANGELOG.md` → `Unreleased` → `Removed`: "The unfinished UI Toolkit adapters."

**Done when:** the project compiles, the full test suite is green, and the checklist passes.

---

## Phase 2: Remove the duplicate architecture

**Tests first:** no new tests. These are deletions, and the Phase 0 suite protects them. Run the full suite (`scripts/verify.sh`) after every deletion. A red test means the deleted code was in use: restore it and investigate.

`Runtime/` is the version that runs. `Core/`, `Abstractions/` and `UGUI/` are an unfinished second version (`RecyclableScrollerCore<T>` is a stub). `Runtime` only borrows the direction classes from `Core`.

### 2.1 Move what Runtime needs out of Core
- [ ] Move these into `Runtime/Directions/`, keeping their `.meta` files, and change their namespace to `RecyclableScroller.Runtime`:
  - `Core/Scripts/Interfaces/IScrollDirection.cs`
  - `Core/Scripts/Directions/ScrollDirectionBase.cs`
  - `Core/Scripts/Directions/ScrollDirectionFactory.cs`
  - `Core/Scripts/Directions/VerticalTopToBottomDirection.cs`
  - `Core/Scripts/Directions/VerticalBottomToTopDirection.cs`
  - `Core/Scripts/Directions/HorizontalLeftToRightDirection.cs`
  - `Core/Scripts/Directions/HorizontalRightToLeftDirection.cs`
- [ ] `IScrollDirection` and the direction classes refer to `Core.Pooling.CellItem` in `CalculateCachedPositions` and `PositionCell`. Nothing in `Runtime` calls those two methods, so **delete both methods** from the interface and every direction class. What's left: `GetViewportDimension`, `GetContentDimension`, `GetScrollPosition`, `ConfigureContentTransform`, `PositionCellInLine`, `CalculateNormalizedScrollPosition`.
- [ ] Remove `using RecyclableScroller.Core.*` from `Runtime/ScrollComponentBinder.cs`, `Runtime/ViewportMetrics.cs`, `Runtime/ScrollNavigator.cs` and `Runtime/CellLineDisplayer.cs`.

### 2.2 Delete the duplicate stack
- [ ] Delete `Core/`, `Core.meta`, `Abstractions/`, `Abstractions.meta`, `UGUI/` and `UGUI.meta`.

### 2.3 Fix the assemblies
- [ ] `Runtime/RecyclableScroller.asmdef` references: remove `RecyclableScroller.Core` and `RecyclableScroller.Abstractions`. Keep `UnityEngine.UI` and `ToolsCore`. Keep `Unity.TextMeshPro` until Phase 3.4.
- [ ] `Editor/RecyclableScroller.Editor.asmdef`: references only `RecyclableScroller`. No change.
- [ ] `README.md` code layout section: describe the single runtime assembly and the editor assembly.

**Done when:** `grep -rn "RecyclableScroller.Core\|RecyclableScroller.Abstractions\|RecyclableScroller.UGUI" .` returns nothing (CHANGELOG history aside), the project compiles, the full test suite is green, and the checklist passes.

---

## Phase 3: Remove unused code

**Tests first:** no new tests. These are deletions, and the Phase 0 suite protects them. Run the full suite (`scripts/verify.sh`) after every deletion. A red test means the deleted code was in use: restore it and investigate.

- Deleting a member that a characterization test touches (for example `ScrollTickSignal`): change the test to assert the same behaviour through what remains, in a separate commit **before** the deletion.

Check each item with your IDE's **Find Usages** before deleting it. A full search also catches `[SerializeField]` names and the inspector's `FindProperty("…")` strings.

### 3.1 Unused classes
- [ ] `Runtime/PositionCalculator.cs`
- [ ] `Runtime/ScrollStrategies/` (all 5 files and the folder `.meta`)
- [ ] `Runtime/VisibilityManager.cs`
- [ ] `Runtime/VisibilityIndexFinder.cs`
- [ ] `Runtime/CellRecycler.cs`
- [ ] `Runtime/CellLinePool.cs`
- [ ] `Runtime/ContentSizeCalculator.cs`
- [ ] `Runtime/ScrollInteractionHandler.cs`
- [ ] `Runtime/RecyclableScrollConfig.cs`
- [ ] `Runtime/IPaginatedScrollSource.cs`
- [ ] `Runtime/IScrollSource.cs`: **keep it**. MyTools uses it (`ShopScrollerInstantiator`), and the sections code uses the same provider pattern. It gets renamed in Phase 4.

### 3.2 Unused members
- [ ] `CellLine.LineTransform`
- [ ] `CellLine` constructor: stop creating `gridCount` `CellItem`s. `CellLineBuilder` overwrites them straight away. Leave the slots null.
- [ ] `CellItem.HasCachedSize` (check usages first)
- [ ] `CellPool.MarkAllPoolItemsAsNonAnimating` and `MarkCellAsNonAnimating`
- [ ] `CellPool.Cleanup` (it just calls `ReturnAllCells`)
- [ ] `ScrollActivityTracker.DragMinScrollPosition` and the code that tracks it
- [ ] `ScrollTickSignal`: it always returns `RebuildVisible`. Remove the enum and make `HandleScrollTick` return nothing.
- [ ] `PaginationSignal.Reset`: nothing reads it.
- [ ] `LinePositionCache.IsDirty`, `MarkDirty`, `isDirty`
- [ ] `ScrollStateManager.HasScrollPositionChanged`, `UpdateLastScrollPosition`, `GetScrollPositionDelta`, `lastScrollPosition`
- [ ] `RecyclableScrollRect.GetEstimatedHeaderCount`
- [ ] `RecyclableScrollRect.CellPrefab` setter: changing it after initialization does nothing. Make it read-only, or document that it only applies before `Initialize`.
- [ ] The non-generic `IRecyclableDataSource` and its explicit implementations in the three data sources. Only `EmptyStateDisplay` uses it, and that moves out in 3.4.

### 3.3 Noise
- [ ] Remove `Debug.Log` and `Debug.LogWarning` calls that only trace normal behaviour (for example in `CellPool.CleanDestroyedCells`). Keep logs for real misuse, such as a missing cell prefab.
- [ ] Translate or delete the Spanish comments in `Editor/RecyclableScrollRectBaseEditor.cs`.
- [ ] Fix the pool help box. It says "Default: 120%", but the default is 140%, the slider runs 1.4–2.5, and the field's range is 1.4–3.0. Pick one range.

### 3.4 Move optional UI out of the runtime assembly
- [ ] Move `PullToRefreshUI.cs` and `EmptyStateDisplay.cs` into a new sample `Samples~/Extras/`, and add it to `package.json` `samples`.
- [ ] Remove `Unity.TextMeshPro` from the runtime asmdef. The sample uses TMP. The runtime doesn't.

**Done when:** it compiles, the full test suite is green, the checklist passes, and the runtime no longer references TMP.

---

## Phase 4: Rename (public surface and folders)

**Tests first:** rename the tests together with the code (IDE rename updates both). Never change a test's assertions in this phase, only names. If an assertion has to change, the rename changed behaviour, and that's a bug. Rename the test assemblies and helpers too (`SlotList.Tests.Editor`, `SlotList.Tests.PlayMode`, `SlotListRig`).

Do the renames in this order. Each step should compile before the next one.

1. Package, assemblies and namespace
2. Public types (interfaces, data sources, the component)
3. Public members and serialized fields
4. Internal helpers that **survive Phase 5** (the rest get their names in Phase 5)

The full name map is in **Appendix A**. The product name used throughout is **Slot List**: a reel of items that keeps turning, which also fits the carousel loop coming later. If you'd prefer another name, swap `SlotList` everywhere. The rest of the map doesn't depend on it.

### 4.1 Package and assemblies
- [ ] `package.json`:
  - `name`: `com.sgonzalezrozo.slot-list`
  - `displayName`: `SlotList`
  - `version`: `1.0.0-preview.1`
  - `description`: rewrite it, with no UI Toolkit mention
  - Update `keywords` and `repository.url` once the repository is renamed
- [ ] Rename `Runtime/RecyclableScroller.asmdef` to `Runtime/Slot List.asmdef` with `"name": "Slot List"` and `"rootNamespace": "Slot List"`.
- [ ] Rename `Editor/RecyclableScroller.Editor.asmdef` to `Editor/SlotList.Editor.asmdef`, referencing `SlotList`.
- [ ] Rename the test asmdef to `SlotList.Tests.Editor`.
- [ ] Namespaces: `RecyclableScroller.Runtime` → `SlotList`, `RecyclableScroller.Editor` → `SlotList.Editor`.
- [ ] Keep `Runtime/AssemblyInfo.cs` (`InternalsVisibleTo("Assembly-CSharp")`), because Fast IoC's generated injectors need it. After renaming `injectedInstantiator` (Appendix A), MyTools won't compile until you **regenerate the Fast IoC injectors** (`MyTools/Assets/FastIoC/Generated/GeneratedMonoInjectors.cs`). If the generator needs a compiling project first, delete that one line by hand, let it compile, then regenerate.
- [ ] Update MyTools in the same sitting:
  - `manifest.json`: `"com.sgonzalezrozo.slot-list": "file:../../Packages/unity-slot-list"` (or the folder's current name), and rename the `testables` entry to `com.sgonzalezrozo.slot-list`. The Stop hook finds local packages from the manifest, so it needs no change.
  - `ShopScrollerRecyclableScrollRect` → derives from `SlotList<EnemyData>`. Renaming the class is optional (it was kept in Phase 4).
  - `ShopScrollerInstantiator` → `IItemSourceProvider<EnemyData>`, `ListSource<EnemyData>`, and the Initialize/SetDataSource branch becomes `SetSource`.
  - `EnemyModel` → `ISlot<EnemyData>`.
  - Open the Shop scene and check there are no "Missing script" warnings.
- [ ] Rename the GitHub repository to `unity-slot-list` (GitHub redirects the old URL), or create a new one. The old 0.3.0 package stays as it is.

### 4.2 Public types
- [ ] Rename every type marked **Phase 4** in Appendix A. For each one, rename the file and keep its `.meta`.
- [ ] Rename the sample types in `Samples~/QuickStart` (`SampleScroller` → `SampleSlotList`, and so on).

### 4.3 Public members and serialized fields
- [ ] Rename the members in **Appendix A.3**.
- [ ] Serialized fields (**Appendix A.4**): it's a new package, so old prefabs don't need to load. Rename without `[FormerlySerializedAs]`, and **update every `FindProperty("…")` string** in the inspector.
- [ ] `isVertical` + `reverse` become one serialized enum `SlotListDirection { TopToBottom, BottomToTop, LeftToRight, RightToLeft }`. The inspector already shows them as a single dropdown.
- [ ] Add `SetSource(source)` now as a simple pass-through: `if (!IsReady) Initialize(source); else SetDataSource(source);`. Callers like MyTools can switch to it right away. Phase 6.1 fixes it properly so it also works with an empty source.

### 4.4 Docs pass
- [ ] Search for the old names: `grep -rni "recyclable\|CellWantToCollapse\|IRecyclable" --include='*.cs' --include='*.md' --include='*.json' --include='*.txt' .` should only find CHANGELOG history and the migration note.
- [ ] Update README, `llms.txt`, `agent-rules.md` and `Samples~/QuickStart/README.md`.

**Done when:** it compiles, the full test suite is green with unchanged assertions, the test project uses `com.sgonzalezrozo.slot-list`, the checklist passes, and the search above is clean.

### 4.5 What Phase 4 applied (2026-11-12)
- Product **Slot List**, namespace `SlotList`, package `com.sgonzalezrozo.slot-list` `1.0.0-preview.1`. The folder and GitHub repository keep their old names until you rename the repository.
- **Every type is renamed now, including the internal classes Appendix A had left for Phase 5**, so Phase 5 merges and splits without renaming anything: `LinePositionCache` → `LaneOffsets`, `LineContentSizeCalculator` → `ContentExtentCalculator`, `CellLineRecycler` → `LaneRecycler`, `CellLineDisplayer` → `LaneDisplayer`, `CellExpansionController` → `ExpansionState`, `ExpansionOperationHandler` → `ExpansionService`, `ScrollStateManager` → `VisibleRangeTracker`, `VisibilityUpdater` → `VisibilityPass`, `RebuildCoordinator` → `Rebuilder`, `ScrollComponentBinder` → `ScrollRectBinding`, `DynamicCellSizeCoordinator` → `SizeCoordinator`, and `ICellSizeCalculator`/`BaseCellSizeCalculator` → `IItemSizer`/`ItemSizerBase` (still deleted in Phase 8).
- **Members:** only the ones using the old vocabulary are renamed (Appendix A.3 rows for `CellPrefab`, `ExpandCell`, `CellWantToCollapse`, `UpdateExpandedCellSize`, `IsIndexExpanded`, `DefaultCellSize`, `CellDataIndex`, `ScrollRect`, `CollapseCell`, `ShouldAnimate`, `CalculateCellSize`, `SetDataSource`, `GetDataSource`), plus internal ones like `Lane.Cells` → `Slots`, `Placement.Cell` → `View` and `SlotPool.GetCell` → `GetView`. Neutral names (`GetItem`, `ItemCount`, `ScrollToItem`, `AddItem`, `IsInitialized`, `OnInitialized`, `VisibleStartIndex`…) stay as they are and change in the phase that changes their behaviour, if at all. Private locals that still say "cell" go away when Phase 5 rewrites those classes.
- **Kept on purpose:** `injectedInstantiator` (Fast IoC's generated code writes to it), the MyTools class names (`ShopScrollerRecyclableScrollRect`, `RecyclableAnimationView`), and the `isVertical`/`reverse` fields (the `SlotListDirection` enum comes with `SlotListSettings` in Phase 5).
- **No `[FormerlySerializedAs]`** (your call). Renamed serialized fields reset in existing scenes: reassign Item Prefab and check spacing and grid settings on the `Bootgame` shop scroller.

### 4.6 Second rename (2026-11-12): Reel becomes Slot List
- Product **Slot List**: package `com.sgonzalezrozo.slot-list`, namespace and assemblies `SlotList`, `SlotList.Editor`, `SlotList.Tests.Editor`, `SlotList.Tests.PlayMode`.
- The component is `SlotList<T>` (with `SlotListBase` and `ISlotList`). Cells are **slots**: `ISlot<T>`, `IExpandableSlot<T>`, `SlotPool`, and the serialized prefab field is `slotPrefab` ("Slot Prefab").
- The internal position record that Phase 4 called `Slot` is now `Placement` (`Lane.Placements`, `Placement.Slot` is the RectTransform shown there).
- Grid names (`slotsPerLane`, `slotSpacing`, `SlotsPerLaneResolver`) were already right and stay.
- `IExpandableSlot.Owner` replaces `ScrollRect` (listed in Phase 4's changelog but only applied here).
- Phase 5 names: `SlotListSettings`, `SlotListEngine`, `SlotListDirection` (settings belong to the whole list, not one slot).

---

## Phase 5: Split up the god classes

**Tests first:**
- The PlayMode characterization tests (0.7) don't change: they use the public API. They must be green after every step.
- Each new engine class is built by TDD **before** the old code is switched over to it:
  1. Write the new class's EditMode tests from the target API in 5.1 (red).
  2. Implement it (green).
  3. Switch `SlotList`/`SlotListEngine` over to it, with the PlayMode tests still green.
  4. Delete the old class and move or delete its characterization tests. Keep any case that the new tests don't cover yet.
- Test lists to write first:
  - `LayoutModelTests`:
    - `LaneOf`/`SlotOf` for a list, a grid, headers in a list, headers in a grid, and an empty source.
    - Offsets and `TotalExtent`.
    - Changing one item's size updates later offsets only.
    - Append N items without a full rebuild (the arrays grow, and earlier offsets don't change).
  - `VisibleRangeFinderTests`: the same cases as the old `VisibleLineFinder`, plus 100,000 lanes (binary search gives correct bounds at the start, middle and end).
  - `ViewPoolTests`: one queue per view kind, and a returned header goes back to the header queue (the bug from Phase 0.8).
  - `ExpansionStateTests`: the same behaviour as `CellExpansionController`, without touching lanes.
  - `SlotListEngineTests` (EditMode, with a fake view provider): `SetSource` → `Full`, scroll → `VisibleOnly`, size change → `Positions`, count change → `Structure`.

**Goal:** a thin MonoBehaviour (`SlotList<T>`) that owns a plain C# engine (`SlotListEngine`) made of a few objects with clear jobs. No stored delegates and no 25-argument constructors.

### 5.1 Target structure

```
Runtime/
  SlotList.cs                 MonoBehaviour: serialized settings, Unity callbacks, public API
  SlotListBase.cs             Non-generic base (editor setup, custom inspector target)
  SlotListSettings.cs             [Serializable] struct with every serialized setting
  Engine/
    SlotListEngine.cs             Owns the objects below and runs Rebuild / Refresh / scroll updates
    LayoutModel.cs            Slots, lanes, item↔lane mapping, lane offsets, total extent
    LaneBuilder.cs            Builds lanes from the item count, slots per lane and section breaks
    SlotsPerLaneResolver.cs   Fixed or automatic slots per lane (was GridCountResolver)
    VisibleRangeFinder.cs     Binary search for the first and last visible lane
    SlotRecycler.cs           Takes and returns views, places and binds them (was LineRecycler + LineDisplayer)
    SlotPool.cs               One pool per view kind (item, header, footer)
    ExpansionState.cs         Which items are expanded and their sizes (was ExpansionController + OperationHandler)
    DeferredWork.cs           Coroutines: first build, re-enable, resize, warm-up
  Layout/
    IAxisLayout.cs            Was IScrollDirection
    TopToBottomLayout.cs  BottomToTopLayout.cs  LeftToRightLayout.cs  RightToLeftLayout.cs
    AxisLayouts.cs            Factory
    AxisMetrics.cs            Viewport and content sizes and scroll offset (was ViewportMetrics)
  Sizing/                     Phase 8: SlotSizeWatcher, SlotMeasurer, size storage in LayoutModel
  Sources/                    Data sources (Phase 7 and 9 add to this)
  Interaction/
    ScrollMover.cs            Scroll-to, start, end, stop (was ScrollNavigator)
    DragState.cs              Was ScrollActivityTracker
    PullGesture.cs            Was PullToRefreshDetector
    PagingTrigger.cs          Was PaginationBoundaryDetector
  Views/
    ISlot.cs  IExpandableSlot.cs  IAppearAnimation.cs
```

### 5.2 Steps (each one compiles and passes the checklist)
1. [ ] **`SlotListSettings`**: move every `[SerializeField]` from `SlotList<T>` into one `[Serializable] struct SlotListSettings`, held by a single `[SerializeField] SlotListSettings settings`. Update the inspector to use `settings.xxx` paths.
2. [ ] **`LayoutModel`**: merge `CellLineBuilder` (now `LaneBuilder`), `LinePositionCache` and `LineContentSizeCalculator` into one object that owns `Placement[]`, `Lane[]`, `float[] laneOffsets`, `int[] itemToLane` and `TotalExtent`. Add `LaneOf(int itemIndex)` and `SlotOf(int itemIndex)`.
   - **This fixes the item-index vs line-index bug.** Every `index / gridCount` and every `cellLines[itemIndex]` must go through `itemToLane[itemIndex]`. Search for them: `grep -rn "/ gridResolver.Current\|/ Mathf.Max(1, gridCount)\|cellLines\[index\]\|cellLines\[cellIndex\]\|cellLines\[i\]" Runtime`.
3. [ ] **`VisibleRangeFinder`**: a binary search over `laneOffsets` replaces the line-by-line loop. Keep the "at scroll start → 0" check.
4. [ ] **`SlotRecycler` + `SlotPool`**: merge `CellLineRecycler`, `CellLineDisplayer` and `CellPool`. Key the pool by **view kind** (`Item`, `Header`, later `Footer`), with one queue per kind. A view must go back to its own kind's queue (today headers end up in the item queue). Cache components once per view instance in a small `SlotHandle` class (`RectTransform`, `ISlot<T>`, `IExpandableSlot`, `IAppearAnimation`) instead of calling `GetComponent` on every bind.
5. [ ] **`ExpansionState`**: merge `CellExpansionController` and `ExpansionOperationHandler`. It only stores state (`HashSet<int>`, `Dictionary<int,float>`) and tells the engine to rebuild. It shouldn't touch lanes directly.
6. [ ] **`SlotListEngine`**: move the orchestration out of `RecyclableScrollRect` and `RebuildCoordinator`. It has a small set of methods, each changing state and then calling one shared `Apply(RebuildLevel level)`:
   - `SetSource(source)`, `Refresh()`, `OnScrolled()`, `OnViewportResized()`, `Expand/Collapse(i)`, `ScrollTo(i)`
   - `RebuildLevel` is one of `VisibleOnly` (scroll), `Positions` (sizes changed), `Structure` (count or slots per lane changed), or `Full` (source replaced).
7. [ ] **`SlotList<T>`**: what's left is the serialized `settings`, the item and header prefabs, the Unity callbacks (`OnEnable`, `OnDisable`, `OnDestroy`, `OnRectTransformDimensionsChange`, `OnBeginDrag`, `OnEndDrag`) forwarding to the engine, the public API forwarding to the engine, and the events. **Target: under 250 lines.**
8. [ ] Delete `RebuildCoordinator`, `RebuildCoroutineScheduler` (now `DeferredWork`), `VisibilityUpdater`, `ScrollStateManager` (now a `VisibleRange` struct inside the engine), `ScrollComponentBinder` (now a method on `SlotList`) and every `cached*` delegate field.
9. [ ] Move the Phase 0 tests to `LayoutModel`, `SlotsPerLaneResolver` and `VisibleRangeFinder`, and add tests for `itemToLane` with headers and grids.

**Done when:**
- No file in `Runtime/` is over about 300 lines.
- No constructor takes more than 6 arguments.
- No `Func<>` or `Action<>` fields are used for internal wiring.
- Every new engine class has EditMode tests written before it, the PlayMode characterization suite is unchanged and green, and the checklist passes.

---

## Phase 6: Fix the known bugs

**Tests first:** each bug already has an ignored test from Phase 0.8. For each row: remove `[Ignore]`, run it and see it **fail**, fix it, and see it pass. If a characterization test asserted the buggy value, update that assertion in the same commit and say so in the message. Bugs without a test yet (6.4, 6.5, 6.8, 6.11, 6.12, 6.14) get one before the fix. Inspector rows (6.4, 6.5) get an EditMode test that builds a `SerializedObject` and checks which properties are drawn, or a written manual check if that's not practical.

| # | Bug | Where (old name) | Fix |
|---|---|---|---|
| 6.1 | **Fixed in Phase 6 (06adf8c).** Can't start with an empty source: `InitializeBase` returns early with 0 items, so `IsInitialized` stays false and `Refresh()` is refused. Pagination always starts empty. | `InitializeBase` | Initialization must succeed with 0 items. Add `SetSource(source)`, which works before or after initialization, replacing the "Initialize or Refresh" pattern every caller writes today (see Phase 9). |
| 6.2 | **Fixed in Phase 6 (06adf8c).** **Confirmed by tests:** `ScrollToItem(500)` shows 492 in a top-to-bottom list and 496 in a right-to-left list. `ScrollToItem` mirrors the position in normal vertical and reversed horizontal lists. `verticalNormalizedPosition` 1 is the top. | `CalculateNormalizedScrollPosition` in the direction classes | Un-ignore `Bug_6_2_*`. Return `1 - t` where the axis's normalized 1 is the start. |
| 6.3 | **Fixed in Phase 6 (06adf8c).** **Confirmed by tests:** `ExpandCell(i)` with no size expands to 0. The inspector's **Single** mode sets `estimatedCellHeight = 0`, and the fallback is `estimatedCellHeight × 2`. | `GetExpandedSizeForCell`, editor `ApplyScrollType` | The inspector must never write the size estimate. Fall back to the prefab's default size × 2. |
| 6.4 | **Fixed in Phase 6 (d78c4d6).** The inspector guesses the scroll type from `estimatedCellHeight > 0`, which defaults to 100. | Editor | Remove "Scroll Type". Show grid fields when slots per lane > 1 or automatic, and sizing fields from the sizing mode (Phase 8). |
| 6.5 | **Fixed in Phase 6 (d78c4d6).** The inspector hides `ScrollRect`'s own settings (movement type, elasticity, inertia, deceleration, sensitivity, scrollbars), Header Prefab and the automatic grid fields. | Editor | Draw a "Scroll behaviour" foldout with the base `ScrollRect` properties, and show Header Prefab and the automatic grid fields. |
| 6.6 | **Fixed in Phase 6 (06adf8c).** Only cross-axis resizes are handled, and only for automatic grids. | `OnRectTransformDimensionsChange` | Any viewport size change → `OnViewportResized()`: recompute slots per lane, re-place visible views, recompute the visible range. |
| 6.7 | **Dropped (owner's decision, 2026-11-12).** Padding across the scroll axis was a new concept the original never had; it only complicates setup. Removed again. Original note: There's no cross-axis padding. Grid cells fill the full viewport width. | `PositionCellInLine` | Add `paddingCrossStart` and `paddingCrossEnd`. Cells still **scale to fill the slots per lane inside the padding**. |
| 6.8 | **Not needed.** The size calculator always gets the width, even for horizontal lists. | `GetContainerWidth` | Not needed: the calculator is deleted in Phase 8. Skip this row. |
| 6.9 | **Fixed in Phase 7.** `HasReachedLastPage` turns true when the paged source has no more pages. `hasReachedLastPage` is never set for paginated sources. | `RecyclableScrollRect` | Replaced in Phase 7. |
| 6.10 | **Fixed in Phase 6 (06adf8c).** `Refresh()` clears expanded items and resets the visible range even when nothing structural changed. | `RebuildCoordinator` | `RebuildLevel` from 5.2.6. Keep expansion state unless the source is replaced. |
| 6.11 | **Fixed in Phase 6 (7a655b0).** `FilteredDataSource.GetItem` scans the whole source on every call. | `FilteredDataSource` | Cache `int[] matchingIndices`, rebuilt when the filter changes or on `Invalidate()`. |
| 6.12 | **Fixed in Phase 6 (7a655b0).** Expansion is quietly ignored in grids. | `ExpansionOperationHandler.ExpandCell` | Log a warning once, or support it, since an expanded item makes its whole lane taller. |
| 6.13 | **Fixed in Phase 6 (06adf8c).** `VisibleStartIndex` and `VisibleEndIndex` are lane indexes, but `IsItemVisible` takes an item index. | Public API | Public API uses item indexes only: `FirstVisibleIndex` and `LastVisibleIndex` (Appendix A). |
| 6.14 | **Fixed in Phase 6 (7a655b0).** Errors are handled inconsistently: some calls throw, some log, some return null. | Data sources, `TryBind` | Misuse (bad setup) logs an error once and disables the view. Bad indexes throw `ArgumentOutOfRangeException` everywhere. |
| 6.15 | **Fixed in Phase 0.** The first build and the pool warm-up waited for `WaitForEndOfFrame`, which never resumes in batch mode (headless builds, servers, CI, `verify.sh`), so the scroller never initialized there. It also resumes after rendering, so the first frame was blank anyway. | `RebuildCoroutineScheduler` | Both now wait one frame (`yield return null`). Regression test: `Bug_6_15_Initialize_ShowsTheFirstScreenWithinTwoFrames`. This was the one production change in Phase 0, needed for the PlayMode suite to run at all. |
| 6.16 | **Fixed in Phase 5, step 6.** Dragging a list before `Initialize` threw a `NullReferenceException`. The engine only exists after `Initialize`, and `SlotList` forwards drags with `engine?.`. | `SlotList.OnBeginDrag`, `OnEndDrag` | Test: `SlotListEngineTests.Bug_6_16_DraggingBeforeInitialize_DoesNotThrow`. |

**Done when:** no `[Ignore("Bug 6.x")]` is left, every row has a passing test (or a documented manual check for the inspector), and the full suite is green.

---

## Phase 7: Pagination redesign

**Tests first** (EditMode with `FakePageLoader`, then PlayMode with the rig), written before `PagedSource` and `PagingTrigger` exist:
- `PagedSource`:
  - `LoadNext` appends and raises `ItemsAppended(start, count)`.
  - A second call while loading does nothing.
  - `HasMore` becomes false at the end.
  - A failure raises `LoadFailed` and keeps the items.
  - `Reset` clears and raises `Cleared`.
  - Cancelling drops the result.
  - `GetItem` never starts a load.
- `PagingTrigger`: requests when `items after last visible < threshold`, and not when `!HasMore` or `IsLoading`.
- PlayMode:
  - Scrolling to the end loads the next page with no `Refresh()` call.
  - The scroll position and expansion are kept after an append.
  - The footer shows Loading, Failed (with Retry) and End.
  - Changing the source mid-load discards the old page.

### 7.1 API
```csharp
public interface IPageLoader<T>
{
    Task<PageResult<T>> LoadAsync(int page, CancellationToken cancellation);
}

public readonly struct PageResult<T>
{
    public readonly IReadOnlyList<T> Items;
    public readonly bool HasMore;
    // A failed load throws. Cancellation throws OperationCanceledException.
}

public interface IPagedSource<T> : IItemSource<T>
{
    bool HasMore { get; }
    bool IsLoading { get; }
    Task LoadNextAsync(CancellationToken cancellation);
    void Reset();
    event Action<int, int> ItemsAppended;   // (startIndex, count)
    event Action Cleared;
    event Action<Exception> LoadFailed;
}

public sealed class PagedSource<T> : IPagedSource<T> { … }
```

### 7.2 Behaviour
- [ ] **The view decides when to load, not `GetItem`.** Remove the preload from `GetItem`. After each visibility update, `PagingTrigger` checks `items after last visible < settings.pagingThresholdItems` (default 10, shown in the inspector) and calls `LoadNextAsync` if `HasMore && !IsLoading`. Drop the 96%/95% normalized trigger.
- [ ] **New pages are added at the end without a full rebuild.** On `ItemsAppended`, the engine adds slots and lanes, extends `laneOffsets`, updates the content size, and **keeps** the scroll position, expansion state and bound views (`RebuildLevel.Positions` for the new lanes only).
- [ ] **Loading on the main thread.** Continue on Unity's synchronization context. If the loader resumes elsewhere, marshal back before changing the list.
- [ ] **Cancellation.** The view owns a `CancellationTokenSource`, cancelled on `SetSource`, `Reset`, `OnDestroy` and `OnDisable` (decide which, and document it).
- [ ] **Optional footer view.** An optional `footerPrefab` implementing `IPagingFooter` with `SetState(PagingState.Loading | Failed | End)`, from the `Footer` pool. When it's assigned, the layout adds one footer lane while `IsLoading`, after a failure, or at the end (each configurable). `Failed` exposes `Retry()`.
- [ ] **Events on the view:** `PageRequested`, `PageLoaded(int count)`, `PageFailed(Exception)` and `ReachedEnd`.
- [ ] **Pull to refresh with paging:** `PullRefreshRequested` event. The common handler is `source.Reset(); await source.LoadNextAsync(ct);`.
- [ ] **Optional: loading at the top** (prepending pages). Add `HasMoreBefore`, `LoadPreviousAsync` and `ItemsPrepended`. On prepend, shift the scroll offset by the added extent so the visible items don't move. This can come after 1.0.

### 7.3 Tests
- [ ] A fake loader with a configurable delay, page size, failure on page N, and a total count.
- [ ] Only one load at a time even when scrolling fast. The view stops at `HasMore == false`. A failed load shows the footer and `Retry` works. Changing the source mid-load discards the old result.

**Done when:** every test above was written first and passes, the `Gestures` scene and `Bootgame` work with no manual `Refresh()` calls, and the full suite is green.

### 7.4 What Phase 7 applied (2026-11-12)
- Done: `IPageLoader<T>.LoadAsync(page, ct)` with `PageResult<T>(items, hasMore)`, `IPagedSource<T>`, the rewritten `PagedSource<T>`, the engine asking for pages (fewer than 10 items left, a constant rather than a setting), appending without a rebuild, one load at a time, cancellation, `Reset` handling, and the `PageRequested`, `PageLoaded`, `PageFailed` and `ReachedEnd` events. Bug 6.9 is fixed.
- **Not done, on purpose:** the optional footer (loading, error, end), because it adds a Footer Prefab field to the inspector, and loading at the top (prepending pages). Both wait until the owner asks for them.

---

## Phase 8: Let Unity's layout system size the items

**Tests first:**
- **EditMode** (pure maths, before any Unity layout is involved):
  - `LayoutModel` sizes: an unmeasured item uses the running average, and the average updates as items are measured.
  - The lane size is the largest slot.
  - Batched updates recompute only from the lowest changed lane.
- **EditMode, anchoring maths:** given an anchor item and offset, size changes above the anchor shift the content by exactly their sum. Changes below shift nothing.
- **PlayMode with real TMP text** (the rig gets a text cell template: `VerticalLayoutGroup` + `TMP_Text`):
  - `Auto` picks `Layout` for a root with a `LayoutGroup`, and `Fixed` otherwise.
  - A bound cell's height equals `LayoutUtility.GetPreferredHeight` at the slot width.
  - Changing the text after binding resizes the cell through `SlotSizeWatcher`, within the same frame (or the next one, whichever the check in 8.4 settles on), and moves the following cells.
  - The watcher doesn't loop: one text change gives exactly one size report.
  - Narrowing the viewport rewraps text and invalidates the cached sizes.
  - Scrolling up through unmeasured items keeps the anchor item's screen position within 0.5 px.
  - Anchoring during a simulated drag gives no jump on the next drag event, and keeps the velocity.
  - Measure ahead fills the cache within ⌈N / measureAheadPerFrame⌉ frames.
- **PlayMode localization (MyTools test asmdef, since it needs `com.unity.localization`):** switching locale invalidates and remeasures visible cells, and the anchor stays put.

### 8.0 Why the size calculator is the wrong approach
- To size an item ahead of time, you have to predict how TextMeshPro will wrap text with a given font, size, width and language. That's guesswork outside of TMP itself.
- The prefab can't help, because it's an asset, not an instance: its layout never runs, so its preferred size is meaningless.
- Localization makes it worse: different string lengths and fonts per language, line breaks, and strings that load asynchronously and arrive after binding.

**New rule:** the scroller never calculates content size. Item views lay themselves out with normal uGUI layout (`LayoutGroup`, `LayoutElement`, `TMP_Text`, `AspectRatioFitter`, and so on), and the scroller **reads the resulting preferred size from a real, bound instance**, then reacts whenever that size changes.

### 8.1 Sizing modes (`settings.sizing`)

| Mode | When to use | How the size is found |
|---|---|---|
| `Fixed` | All items the same size | The prefab root's `RectTransform` size along the scroll axis, read once. Fastest. The view no longer needs `DefaultCellSize`. |
| `Layout` | Content varies: text, localized text, images, optional sections | Unity layout on real instances (8.2–8.5). No code to write. |
| `Auto` (default) | | `Layout` if the prefab root has a component that implements `ILayoutElement` (any `LayoutGroup`, `LayoutElement`, or a text component on the root), otherwise `Fixed`. Show the chosen mode in the inspector. |

- [ ] Delete `ICellSizeCalculator<T>`, `BaseCellSizeCalculator<T>` and `DynamicCellSizeCoordinator<T>`. A hand-written sizer can come back later as an advanced option if a real need appears. Don't build it now.

### 8.2 How to build a prefab that sizes itself (document this in the README)
- Root: a `VerticalLayoutGroup` (for vertical lists) with **Control Child Size → Height** ticked and **Child Force Expand → Height** unticked, plus padding. Optional `LayoutElement.minHeight`.
- Children: `TMP_Text` (with word wrapping on), images with a `LayoutElement` or `AspectRatioFitter`, and nested groups.
- **No `ContentSizeFitter` on the root.** The scroller sets the root's size itself, and a fitter would fight it. Fitters on children are fine.
- Width is always set by the scroller: the full width in a list, the slot width in a grid. That's what makes text wrap correctly.
- Horizontal lists work the same way with a `HorizontalLayoutGroup` and **Control Child Size → Width**.

### 8.3 Measuring on real instances
Two ways to get a size. Use both.

**A. Measure the visible view as it's bound (the main path)**
1. Take a view from the pool and set its cross-axis size (width).
2. `Bind(item, index)`.
3. If this item's size isn't cached: `LayoutRebuilder.ForceRebuildLayoutImmediate(viewRect)`, then `LayoutUtility.GetPreferredSize(viewRect, axis)`.
4. Cache the size, apply it to the view's `sizeDelta`, and update the layout model (8.5).

**B. Measure ahead with a hidden copy (optional, keeps the scrollbar and `ScrollTo` accurate)**
- Keep **one measuring instance per view kind**: a real instance of the prefab, active, parented under Content so it uses the same canvas scale and fonts, and hidden. Hide it with a `CanvasGroup` (alpha 0, no raycasts, not interactable) and by placing it outside the viewport, so `RectMask2D` culls it.
- Each frame, measure up to `settings.measureAheadPerFrame` items (default 4, 0 turns it off), starting next to the visible range and moving outwards: bind to the measurer, force a layout rebuild, read the preferred size, cache it.
- Text components on the measurer need a font atlas for each glyph. With TMP dynamic fonts, measuring ahead also warms the atlas, which is fine.

**Placeholders for unmeasured items:** use the **average of the sizes measured so far** (falling back to the prefab's rect size before anything is measured) instead of a fixed estimate. The total gets more accurate as you scroll. `settings.estimatedItemSize` remains as an optional manual override.

### 8.4 Reacting to size changes after binding (localization, async text, images)
Add a component **`SlotSizeWatcher`** to the view root. The scroller adds it automatically in `Layout` mode if it's missing.
- It implements `ILayoutSelfController`. Unity calls `SetLayoutHorizontal()` and `SetLayoutVertical()` on it whenever the view's layout is dirty and rebuilt: text changed, a localized string arrived, a child was turned on or off, an image size changed.
- In those calls it reads `LayoutUtility.GetPreferredSize(rect, axis)`, and if it differs from the last reported size (by more than 0.5 px) it tells the scroller: `owner.OnViewSizeChanged(itemIndex, newSize)`.
- This replaces the manual `UpdateExpandedCellSize`. Expanding and collapsing become "turn the details child on or off". The watcher reports the new size, and the scroller animates or applies it.
- This runs during the canvas's layout pass, before rendering. Updating positions (`anchoredPosition`) and Content's size at that point should show in the same frame. **Check this in MyTools**: if a one-frame lag shows, queue the change and apply it in the scroller's `LateUpdate` instead.
- Watch for loops: the scroller must set the root's size **without** marking the root's layout dirty again. Set `sizeDelta` only when the value actually changes, and ignore the watcher callback caused by our own `sizeDelta` write.

**Localization (MyTools has `com.unity.localization` 1.5.9)**
- A string that arrives after binding is handled by the watcher (above).
- Changing language makes every cached size wrong. Add `InvalidateAllSizes()`, which clears the cache, keeps the current position anchored, and remeasures visible items at once and the rest lazily.
- Optional automatic hook: add a `versionDefines` entry to the runtime asmdef (`com.unity.localization` → `SLOTLIST_LOCALIZATION`). Under that define, `SlotList` subscribes to `LocalizationSettings.SelectedLocaleChanged` and calls `InvalidateAllSizes()`. No hard dependency.
- Right-to-left languages only change text alignment inside the view. Nothing changes in the scroller.

**Width changes** (rotation, resizing, a different slot count in a grid) also invalidate every cached size, because the text rewraps. `OnViewportResized()` (Phase 6.6) calls `InvalidateAllSizes()` when the cross extent changes.

### 8.5 Keeping the view still when sizes change
Sizes now change all the time (measuring, localization, images), so the layout model and scroll position have to handle it.
- **Size storage:** `float[] itemSize` plus a "measured" bit per item. Lane size = the largest size among its slots (a grid lane is as tall as its tallest item).
- **Batch updates per frame:** collect changed items, keep the **lowest changed lane**, and once per frame recompute `laneOffsets` from that lane onwards and update Content's size. If profiling shows this is slow for very long lists, switch `laneOffsets` to a Fenwick tree (O(log n) per update) behind the same `LayoutModel` API.
- **Anchoring:** before applying changes, remember the **anchor**: the first visible item and its distance from the viewport's top edge. After applying, move the content so the anchor is in the same place again. Items above the screen growing or shrinking then never move what you're looking at. The same code handles pages added at the top (Phase 7) and `InvalidateAllSizes()`.
- **Anchoring while dragging or flinging:** `ScrollRect` remembers where the drag started and calculates speed from the last position, so moving the content from code mid-drag causes a jump on the next drag event and a speed spike. The scroller already derives from `ScrollRect`, so:
  - After shifting the content while dragging, start the drag again from the current pointer position: keep the last `PointerEventData` from `OnDrag` and call `base.OnBeginDrag(lastEvent)`.
  - Override `LateUpdate`: save `velocity` before the shift, call `base.LateUpdate()`, then restore the saved `velocity`, so inertia isn't affected by our shift.
  - **Check both in MyTools** while flinging up through items that haven't been measured yet.

### 8.6 Expandable items with Layout mode
- `IExpandableSlot.IsExpanded` turns the details part on or off. The watcher reports the new size, so the scroller doesn't need a size passed in.
- `Expand(i)` and `Collapse(i)` set the state, rebind or update that view, and let the watcher drive the size change.
- The expanded size is cached separately from the collapsed size (`itemSize` + `expandedSize`), so scrolling away and back doesn't remeasure.
- Optional animation: the scroller animates from the old size to the new one over `settings.expandDuration`, while the view's content is already laid out at its final size and the mask clips it.

### 8.7 Steps
1. [x] ~~Add `SizingMode`~~ Replaced by the `DefaultExtent` rule: > 0 is fixed, <= 0 is measured with layout. No inspector field. Was: Add `SizingMode` (`Auto`, `Fixed`, `Layout`) to `SlotListSettings` and to the inspector, showing which mode `Auto` picked.
2. [x] Store sizes in the `LayoutModel` (`itemSize`, measured flags, running average) with batched offset updates per frame.
3. [x] Measure on bind (8.3 A).
4. [x] Add `SlotSizeWatcher` (8.4) and loop protection. Reports are applied in `SlotList.LateUpdate`, one frame later, because resizing inside Unity's layout rebuild logs errors.
5. [x] Anchoring (8.5), including the drag and velocity fixes. The anchor is the first shown lane not measured for the first time in that pass; after a shift the drag restarts with `base.OnBeginDrag(lastEvent)` and `UpdatePrevData()` replaces the velocity save and restore.
6. [ ] Measure ahead with a hidden copy (8.3 B).
7. [x] `InvalidateSize(i)`, `InvalidateAllSizes()`, invalidation on width change, and the optional localization hook (define `SLOTLIST_LOCALIZATION`, not `REEL_LOCALIZATION`).
8. [x] Expandable items driven by the watcher (8.6). `SetExpandedSize` and the size given to `Expand` stay for fixed sizes only; with layout they're ignored with a warning. No separate expanded-size cache: the slot is remeasured when rebound. No animation yet.
9. [x] Delete the old calculator types (done in step 1) and update the README with the prefab recipe from 8.2. Step 6 (measure ahead with a hidden copy) is left optional and not done.

### 8.8 Test scenes in MyTools (`Assets/SlotListTests/Sizing/`)
- [ ] `S1_TextLengths`: 10,000 items with random text from 1 to 12 lines. Fling both ways: nothing visible jumps, and the scrollbar settles.
- [ ] `S2_Localized`: the same list using `LocalizeStringEvent`, plus a button that cycles 3 locales, one with much longer strings. Visible items resize in the same frame and the anchor item stays put.
- [ ] `S3_AsyncContent`: images that load after 0.2–1 s and change size through `AspectRatioFitter`. The watcher picks it up.
- [ ] `S4_GridText`: a 3-column grid with mixed text. The lane height is the tallest item, and resizing the window rewraps.
- [ ] `S5_Expandable`: expand and collapse with a details child, with no sizes in code.
- [ ] `S6_ScrollToUnmeasured`: `ScrollTo(9000)` on a fresh list lands on item 9000 once its neighbours are measured.

**Done when:** every test above was written first and passes, all six scenes work with **no sizing code**, flinging through `S1` has no visible jumps (manual check), and the full suite is green.

---

## Phase 9: Sections ("multi-source") in the package

**Tests first:**
- ~~Port the MyTools sections characterization tests (0.7) to the package's `SectionedSource` API **before** writing it.~~ Not possible: the sections code never reached MyTools. Tests were written from this plan's API instead.
- `SectionedSourceTests` (EditMode):
  - `Count`, `KindAt`, `HeaderAt`, `ItemAt`, `Locate` and `FlatIndexOf` round-trip for every flat index.
  - Empty sections are skipped (and kept when `SkipEmptySections = false`).
  - Insert and remove a section.
  - `Changed` is raised.
  - An inner source change updates `Count`.
  - `Locate` on 1,000 sections stays correct at the edges.
- PlayMode:
  - Headers bind with `ISlot<THeader>`, items with `ISlot<TItem>`.
  - Headers take a full lane in a grid.
  - Header views come only from the header pool.
  - `SetSource` before and after initialization.
  - `ScrollToSection`.
- Un-ignore the `[Ignore("Phase 9")]` header test from 0.8, or delete it if `IsHeader` is gone, saying so in the commit.

Move the pattern from the other project (`CompositeDataSource<T, U>` + `IScrollMultiSource<T, U>`) into the package, so headers work without copying code between projects.

### 9.1 How it's used today (for reference)
```csharp
public interface IScrollMultiSource<T, U> where T : class where U : class
{
    CompositeDataSource<T, U> GetDataSource();
}

CompositeDataSource<EventMainItemData, EventHeaderData> source = new();
foreach (SectionItems section in sections)
{
    if (section.Items.Count == 0)
        continue;

    source.AddSource(new EventHeaderData { Section = section.Section },
                     new ListDataSource<EventMainItemData>(section.Items));
}

// Every caller writes this:
if (!scroll.IsInitialized) scroll.Initialize(source);
else scroll.Refresh();
```

### 9.2 New API
```csharp
public interface ISectionedSourceProvider<TItem, THeader>
{
    SectionedSource<TItem, THeader> GetSource();
}

public sealed class SectionedSource<TItem, THeader>
{
    public bool SkipEmptySections { get; set; } = true;   // replaces the manual "Count == 0 → continue"

    public int SectionCount { get; }
    public int Count { get; }                              // headers + items, flattened

    public void AddSection(THeader header, IItemSource<TItem> items);
    public void InsertSection(int sectionIndex, THeader header, IItemSource<TItem> items);
    public void RemoveSection(int sectionIndex);
    public void ClearSections();

    public EntryKind KindAt(int flatIndex);                // Header or Item
    public THeader HeaderAt(int flatIndex);
    public TItem ItemAt(int flatIndex);
    public (int section, int indexInSection) Locate(int flatIndex);
    public int FlatIndexOf(int section, int indexInSection);

    public event Action Changed;
}

public abstract class SectionedSlotList<TItem, THeader> : SlotListBase
{
    // itemPrefab: ISlot<TItem>, headerPrefab: ISlot<THeader>
    public void SetSource(SectionedSource<TItem, THeader> source);   // works before or after initialization
    public void ScrollToSection(int section);
    // the rest of the SlotList API
}
```

Usage after the refactor:
```csharp
SectionedSource<EventMainItemData, EventHeaderData> source = new();
foreach (SectionItems section in sections)
    source.AddSection(new EventHeaderData { Section = section.Section },
                      new ListSource<EventMainItemData>(section.Items));

scroll.SetSource(source);   // no IsInitialized check
```

### 9.3 How it works inside
- [x] `SectionedSource` keeps a prefix-sum array `sectionStart[s]` (flat index of each header), rebuilt on `Changed`. `Locate` uses a binary search over it, O(log sections).
- [x] The engine works on **entries**, each with a **view kind** (`Header`, `Item`, later `Footer`). `LaneBuilder` already breaks a lane at a header. Keep that: a header takes a whole lane, and a section's first item starts a new lane.
- [x] `SlotRecycler` gets the view kind for each entry and takes from that kind's pool (from 5.2.4). The header prefab is **only required** with `SectionedSlotList`. `SlotList<T>` has no header field. That matches "optional, only used with sections".
- [x] Binding: header views implement `ISlot<THeader>`, item views implement `ISlot<TItem>`. The two item types no longer need a shared base class.
- [x] Changes to an inner source (`IItemSource<TItem>`) must update the flattened count. **Done with `Recount()`**, which the list's `Refresh()` calls: sources don't raise change events, so the rule stays "change the data, then Refresh()". No `NotifySectionChanged`.
- [ ] Optional: sticky headers. **Not done.** Keep the current section's header pinned to the top edge by drawing a copy of the header view over the viewport. This can come after 1.0.

### 9.4 Migration notes for the other project
- `CompositeDataSource<T, U>` → `SectionedSource<TItem, THeader>`, and `AddSource(header, source)` → `AddSection(header, source)`. Note the type order: **item first, header second**.
- `ListDataSource<T>` → `ListSource<T>`.
- `IScrollMultiSource<T, U>` → `ISectionedSourceProvider<TItem, THeader>`, and `GetDataSource()` → `GetSource()`.
- The scroller subclass → derive from `SectionedSlotList<TItem, THeader>`, and assign both Slot Prefab and Header Prefab.
- `Initialize` / `Refresh` branches → `SetSource(source)`. Calling it again replaces the sections.
- The manual `if (section.Items.Count == 0) continue;` → drop it: empty sections are skipped (`SkipEmptySections`, true by default).
- Header slots implement `ISlot<THeader>` and item slots `ISlot<TItem>` (or `IExpandableSlot<TItem>`). If the old cells shared a base class only so both could live in one source, that base class can go.
- An `IsHeader` override on the scroller, if any, goes away: the source knows which entries are headers.
- List indexes (`ScrollToItem`, `Expand`, `IsItemVisible`) count headers. Convert with `source.FlatIndexOf(section, indexInSection)`, or use `ScrollToSection(section)`.

### 9.5 What Phase 9 applied (2026-11-12)
- **Step 1 (640f9ea):** `SectionedSource<TItem, THeader>`, `EntryKind` and `ISectionedSourceProvider`. 14 EditMode tests.
- **Step 2 (c7a184e):** `SectionedSlotList<TItem, THeader>` with its Header Prefab, header pool, header sizing (fixed or measured, kept out of the item average), `SetSource`, `ScrollToSection` and binding. 10 PlayMode tests and an inspector test.
  - `SlotListBase` now holds what both lists share (settings, prefab, events, API, Unity callbacks) and talks to the engine through the internal `ISlotListEngine`. Its constructor is `private protected`, so only the two lists in the package derive from it. No new public base type was added.
  - `SlotList<T>` lost Header Prefab and `IsHeader` (owner's decision). The ignored `Phase9_IsHeader…` test was removed.
- **Not done:** the `Sections` scene in MyTools, sticky headers, and the ports of MyTools sections tests (MyTools never had the sections code, and the owner chose not to share it: the tests were written from this plan's API).

**Done when:** the ported tests and the new ones pass, the `Sections` scene runs with the package's `SectionedSource` (headers in a list and in a grid, empty sections skipped), and MyTools no longer needs its copied sections code.

---

## Phase 10: Performance

**Tests first:** performance work is TDD with **regression tests** that hold the numbers you reach:
- PlayMode **allocation tests**: wrap 120 frames of programmatic scrolling in `Recorder`/`ProfilerRecorder` for `GC.Alloc` (or `GC.GetAllocatedBytesForCurrentThread()` around the scroller's update), and assert 0 B. Write it first: it fails today, and it's the target for P11–P17.
- PlayMode **structure tests** for the recycling fixes:
  - Returning a view doesn't change its parent (P6).
  - `Refresh()` on an unchanged structure doesn't return views to the pool (P7).
  - Disable then enable doesn't rebind when the data didn't change (P8).
  - Warm-up creates at most `warmupPerFrame` views per frame (P9).
  - A visible view is bound and placed in the first rendered frame (P10).
- **Work-count tests** with counters in the engine (internal and test-only, exposed to the test assembly with `InternalsVisibleTo`):
  - A range change touches O(visible) lanes, not O(all) (P2).
  - Already-placed views aren't re-placed (P3).
  - Sub-pixel scrolls don't run the visibility update (P4).
- Optional timing benchmarks with `com.unity.test-framework.performance` (`Measure.Frames()`), recorded but **not** used as pass/fail in verify, since timing is noisy. Pass/fail tests check allocations and work counts, which are deterministic.

Phase 5 fixes some of these in passing (binary search, cached components). This phase goes through the rest, **measuring before and after each change**.

### 10.1 Measure first
- [ ] Add profiler markers (`Unity.Profiling.ProfilerMarker`): `Slot List.OnScroll`, `Slot List.UpdateVisible`, `Slot List.Recycle`, `Slot List.Acquire`, `Slot List.Bind`, `Slot List.Measure`, `Slot List.ApplySizes`, `Slot List.Rebuild`.
- [ ] Benchmark scenes in `MyTools/Assets/SlotListTests/Bench/`: 1,000, 10,000 and 100,000 items, in a list, a 4-column grid, and Layout sizing with text. A script flings at a fixed speed for 10 s.
- [ ] Record per frame: CPU time of the `Slot List.*` markers, `Canvas.SendWillRenderCanvases` (layout and graphic rebuilds), `Canvas.BuildBatch`, and GC bytes allocated. Use `ProfilerRecorder` in the bench script, and optionally `com.unity.test-framework.performance` to automate it.
- [ ] Record on a real phone too (IL2CPP, development build). The Editor hides most UI costs.
- [ ] Write the baseline into `Documentation~/benchmarks.md` **before** starting 10.2.

### 10.2 What the current code costs, and the fix

**Scrolling (runs every frame while moving)**

| # | Today | Cost | Fix |
|---|---|---|---|
| P1 | `VisibleLineFinder` checks lines one by one, twice per scroll event | O(lines) per frame | Binary search over `laneOffsets` (Phase 5.2.3) |
| P2 | `CellLineRecycler.RecycleOutOfViewLines` walks **every line** before the visible start and after the visible end whenever the range changes | O(lines) per range change, which is most frames during a fling | Remember the previous range. Only release lanes in `old − new` and only acquire lanes in `new − old`. O(visible) |
| P3 | `CellLineDisplayer.ShowLines` re-places every visible cell (anchoredPosition, sizeDelta, pivot, anchors) on every range change, even cells that didn't move | Many RectTransform writes, each of which can dirty the canvas | Set pivot and anchors once when a view is created. Set position and size only for newly shown views, or when that lane's offset or size changed (keep a `laneVersion` number) |
| P4 | `ScrollActivityTracker` always asks for a rebuild | Visibility work even when nothing changed | Skip unless the offset moved ≥ 0.5 px or a rebuild is pending |
| P5 | 4–5 `GetComponent` calls per bind (`IExpandableRecyclableCell<T>`, `IRecyclableCell<T>`, `CacheComponentReferences` ×2 in `Mark` and in `SetupExpandableCell`) | Native calls on the hot path | `SlotHandle` caches them once per instance (Phase 5.2.4) |

**Recycling (taking and returning views)**

| # | Today | Cost | Fix |
|---|---|---|---|
| P6 | A returned cell is `SetActive(false)` and **re-parented to a `DontDestroyOnLoad` object outside every canvas**, then re-parented back on reuse | Changing canvas re-registers every Graphic, rebuilds its mesh and material, rebuilds layout and dirties both canvases. Probably the biggest cost today | Keep pooled views **under Content**. Option A: `SetActive(false)` only. Option B: keep them active, set the `CanvasGroup` alpha to 0 with no raycasts, and move them far outside the viewport. `RectMask2D` culls them, so they cost nothing to draw and nothing to wake up. Benchmark A vs B. B usually wins for heavy views. |
| P7 | `Refresh()` on an unchanged structure returns **every** visible view to the pool and takes it back | Double re-parenting of every visible view | Keep views attached to their lanes and just rebind them |
| P8 | `OnDisable` returns all views, and `OnEnable` takes and rebinds all of them | A spike every time a screen opens | Keep views in place while disabled. On enable, only rebind if the data version changed |
| P9 | Pool warm-up creates every pooled view in one frame (`PrewarmPoolDeferred`) | A frame spike after `Initialize` | Spread over frames (`settings.warmupPerFrame`, default 2), or warm up while idle |
| P10 | The first build waited for `WaitForEndOfFrame`, which runs **after** rendering. Phase 0 changed it to wait one frame (bug 6.15) | One blank frame when the screen opens | Build in the same frame: in `Start` after `Canvas.ForceUpdateCanvases()`, or on `Canvas.willRenderCanvases` |

**Rebuilds and memory**

| # | Today | Cost | Fix |
|---|---|---|---|
| P11 | Every structural rebuild allocates `CellItem[N]` (N objects), a `List<CellLine>` + `ToArray()`, new `CellLine` objects, and `gridCount` extra `CellItem`s per line that are thrown away | O(N) objects and GC on every count change, which includes **every page loaded** | Arrays of plain values that grow but never shrink: `float[] itemSize`, `int[] itemToLane`, `int[] laneFirstItem`, `byte[] laneSlotCount`, `float[] laneOffsets`. Append a page by growing the arrays, not rebuilding |
| P12 | `LineContentSizeCalculator` sums every line on every content-size update, called on each expand and every frame during batched sizing | O(lines) repeatedly | `TotalExtent = laneOffsets[last] + lastLaneSize + paddingEnd` |
| P13 | `LinePositionCache.Rebuild` recomputes all offsets on any size change | O(lines) per change | Recompute from the lowest changed lane, once per frame (Phase 8.5). Use a Fenwick tree only if the benchmark asks for it |
| P14 | The old sizer measured everything synchronously at startup and again on every rebuild | O(N) calculator calls in one frame | Gone with Phase 8 (lazy, budgeted) |
| P15 | `FilteredDataSource.GetItem` scans the whole source per call, and `GetAllItems()` allocates a `ReadOnlyCollection` wrapper per call | O(N) and an allocation **per bind** | Cached index map (Phase 6.11). Remove `GetAllItems` from the hot path |
| P16 | `CellExpansionController.CollapseAllPreviouslyExpanded` allocates a `List<int>` on each expand | Small GC | Reuse one list field |
| P17 | Log strings are built in methods that run often (`CleanDestroyedCells` and others) | Allocation and log cost | Removed in Phase 3.3. Guard any remaining ones with `[Conditional("REEL_DEBUG")]` |

**Canvas and rendering (setup advice the scroller can apply or warn about)**

| # | Topic | Fix |
|---|---|---|
| P18 | Moving views dirties the **whole screen's canvas**, so every other UI element on that canvas gets rebatched while scrolling | Put a nested `Canvas` (+ `GraphicRaycaster` if needed) on the **Viewport**. The scroller can add it automatically (`settings.isolateCanvas`, default on) or warn in the inspector |
| P19 | Many cells have `raycastTarget` on graphics that don't need it | Inspector helper: "Turn off Raycast Target on non-interactive graphics in Item Prefab", plus a README note |
| P20 | Layout groups inside views rebuild whenever a view is activated or rebound | With cached sizes (Phase 8), skip `ForceRebuildLayoutImmediate` for items that are already measured. Recommend shallow layout hierarchies in the README |
| P21 | Heavy binds (images, icons) during fast flings | Expose `IsMoving` and the current speed, and add an optional `ISlot.BindDeferred(item)` called once scrolling slows down, so views can postpone image loads |
| P22 | Float precision past about 10⁷ px (100,000 × 100 px) causes jitter | Only if the bench shows it: periodically re-centre Content and shift all offsets |

### 10.3 Order
1. [ ] 10.1 baseline.
2. [ ] P6 and P7 (re-parenting): expected to be the biggest win. Benchmark A vs B.
3. [ ] P2, P3, P4 (scrolling hot path).
4. [ ] P18 (canvas isolation).
5. [ ] P8, P9, P10 (spikes on open, enable and warm-up).
6. [ ] P11–P17 (allocations).
7. [ ] P19–P22 (helpers and edge cases).
8. [ ] Re-run the benchmarks after each step and add a row to `benchmarks.md`.

**Done when:** the allocation test asserts 0 B for 100,000 items and passes, the structure and work-count tests pass, no frame spike when opening a screen or enabling the scroller, and before/after numbers in `benchmarks.md` for both the Editor and a phone.

---

## Phase 11: New features

**Tests first** for each feature before its code:
- `ScrollTo` alignment maths (EditMode): the target offset for Start, Center, End and Nearest, clamped to the content. Nearest doesn't move when the item is visible.
- The animation with `ManualClock` (PlayMode): it reaches the target after `duration`, and a drag cancels it.
- Snapping (PlayMode): after a released drag below the speed threshold, it ends exactly on an item (Start or Center), and `SnappedTo` is raised once.
- Carousel index maths (EditMode):
  - `virtual % count` mapping, including negative indices.
  - Re-centring keeps the same item at the same screen position.
  - `ScrollTo` takes the shortest way round.
  - It's rejected (error) with a grid or with sections.
- Carousel (PlayMode): scrolling 3 × count items in each direction never reaches an edge, and the bound items are always the right real indices.

### 11.1 Animated scroll-to and alignment (needed for 11.2)
- [ ] `ScrollTo(int index, ScrollAlign align = ScrollAlign.Start, float duration = 0f, float offset = 0f)` with `ScrollAlign { Start, Center, End, Nearest }`. `Nearest` doesn't move if the item is already visible.
- [ ] The animation runs in `LateUpdate` with an easing curve, and is cancelled when the user starts dragging.

### 11.2 Snapping
- [ ] `settings.snap`: `None | Item | Page`. After the drag ends and speed drops below a threshold, animate to the nearest item or page, with an alignment of start or centre.
- [ ] Events: `SnappedTo(int index)`.

### 11.3 Looping carousel
- [ ] `settings.loop = true`. Valid when slots per lane is 1 and there are no sections (log an error otherwise).
- [ ] Virtual indices: the engine lays out a long virtual range (for example `count × 1000`) and binds `virtual % count`. Public methods take and return real indices.
- [ ] Re-centring: when the scroll gets within one viewport of either end of the virtual range, jump the offset by a whole multiple of `count × lane size` in the same frame. You can't see the jump because the content is the same.
- [ ] Works with snapping (`Item`, `Center`) for a typical carousel, and with `ScrollTo` taking the shortest way round.
- [ ] Add a manual scene `SlotListTests/Carousel`.

### 11.4 Nested scrollers
- [ ] Out of scope (MIT plugin). Add a manual scene `SlotListTests/Nested` with a horizontal `SlotList` inside a vertical `SlotList`, using the plugin, and check that drags go to the right scroller.

---

## Phase 12: Tests, samples, docs, release

### 12.1 Tests
By now most tests exist (written first in each phase). This step fills the gaps:
- [ ] Edit-mode: `LayoutModel` (including batched size updates and anchoring maths), `LaneBuilder`, `SlotsPerLaneResolver`, `VisibleRangeFinder`, `SectionedSource`, `FilteredSource`, `PagedSource` (fake loader).
- [ ] Play-mode smoke tests: load each test scene, scroll programmatically, and check that bound views match `FirstVisibleIndex…LastVisibleIndex`.

### 12.2 Samples (`package.json` → `samples`)
- [ ] `QuickStart` (updated), `Grid`, `Sections`, `Paging`, `Expandable`, `LayoutSizedText` (including localized text), `Carousel`, `Extras` (pull indicator, empty state).

### 12.3 Docs
- [ ] Rewrite `README.md`: install, quick start, one recipe per sample, API reference, and inspector reference.
- [ ] `Documentation~/migration-from-recyclable-scroller.md`: a table with Appendix A's old → new names, plus the behaviour changes (`SetSource`, item-index API, paging events, sizing modes).
- [ ] Update `llms.txt` and `agent-rules.md`.
- [ ] `CHANGELOG.md`: a `1.0.0-preview.1` section.

### 12.4 Update the AI rules (`unity-architecture-agents`)
The rules repo describes the scroller's current API, so an agent working from it would write code against names that no longer exist. Update it after the refactor, once the API is stable, and release a new rules version:
- [ ] `architecture/OPTIONAL_PACKAGES.md`: capability `recyclableScroller` → new name (for example `reel`), detected from `com.sgonzalezrozo.slot-list`. Update the "List without…" fallback section's title and links.
- [ ] `architecture/AGENT_WORKFLOW.md`: the capabilities line.
- [ ] `architecture/UI_PATTERNS.md`: the list section (`LIST-*` rules). Use the new component, source, cell interface, `SetSource`, sections, Layout sizing (no size calculator), and paging events.
- [ ] `architecture/examples/03_FILTERED_LIST_INVENTORY.md` and the list part of `01_BACKEND_FEATURE_TROPHY_ROOM.md`.
- [ ] `AGENTS.md`: the API policy list of symbols, and the scope sentence that names Recyclable Scroller.
- [ ] `README.md`, `llms.txt` and `CHANGELOG.md`.
- [ ] **Resolve the naming clash with MVC first.** In the rules, the cell's entry point is a **Model** (`ItemCellModel : IRecyclableCell<T>`), and the analyzers decide the layer from the class name suffix (`*Model`, `*View`, `*Controller`). A cell interface named `ISlot<T>` (Appendix A) would have Models implementing a "View" interface. Pick the final cell interface name with that in mind, and update Appendix A and the rules together.
- [ ] Package events with parameters (`PageLoaded(int)`, `SnappedTo(int)`) don't break `UA0004`, because that rule only checks declarations in client code. Still, decide whether the public events should follow the rules' style (no parameters, the listener reads the state) for consistency.
- [ ] Bump the `.ai` submodule in MyTools and in the sample project.

### 12.5 Check before release
- [ ] IL2CPP build (Android or iOS) with the QuickStart and Sections samples.
- [ ] Play mode with domain reload turned off. Check that no static state survives (`WaitForEndOfFrame` cache is fine, nothing else is static).
- [ ] Update the README compatibility table with what you actually checked.
- [ ] Tag `v1.0.0-preview.1`.

---

## Appendix A: Name map

Vocabulary used everywhere:

| Concept | Name |
|---|---|
| The product, namespace and component | **Slot List** (`SlotList<T>`, namespace `SlotList`) |
| Your data object | **item** |
| The GameObject that shows an item | **slot** (`ISlot<T>`, pooled by `SlotPool`) |
| One row (vertical) or column (horizontal), holding one or more slots | **lane** |
| The record of one item's position in a lane | **placement** (`Placement`) |
| Number of slots in a lane (the grid count) | **slots per lane** |
| Size along the scroll direction | **extent** |
| Size across the scroll direction | **cross extent** |
| Where items come from | **source** |
| What reads an item's size from Unity layout | **watcher** (`SlotSizeWatcher`) and **measurer** (`SlotMeasurer`) |

### A.1 Package, assemblies, namespaces

| Old | New |
|---|---|
| `com.sgonzalezrozo.recyclable-scroller` | `com.sgonzalezrozo.slot-list` |
| Display name "Recyclable Scroller" | "Slot List" |
| Assembly `RecyclableScroller` | `SlotList` |
| Assembly `RecyclableScroller.Editor` | `SlotList.Editor` |
| Namespace `RecyclableScroller.Runtime` | `SlotList` |
| Namespace `RecyclableScroller.Editor` | `SlotList.Editor` |
| Repository `unity-recyclable-scroller` | `unity-slot-list` |

### A.2 Types

**Phase** says when to rename: in Phase 4 directly, or created under the new name in Phase 5 or later.

| Old | New | Phase | Notes |
|---|---|---|---|
| `RecyclableScrollRect<T>` | `SlotList<T>` | 4 | The component |
| `RecyclableScrollRectBase` | `SlotListBase` | 4 | |
| `IRecyclableScrollRect` | `ISlotList` | 4 | What expandable views talk to |
| `IRecyclableCell` | `ISlot` | 4 | |
| `IRecyclableCell<T>` | `ISlot<T>` | 4 | |
| `IExpandableRecyclableCellBase` | `IExpandableSlot` | 4 | |
| `IExpandableRecyclableCell<T>` | `IExpandableSlot<T>` | 4 | |
| `IAnimationMarker` | `IAppearAnimation` | 4 | |
| `IRecyclableDataSource<T>` | `IItemSource<T>` | 4 | |
| `ListDataSource<T>` | `ListSource<T>` | 4 | |
| `FilteredDataSource<T>` | `FilteredSource<T>` | 4 | |
| `PaginatedDataSource<T>` | `PagedSource<T>` | 4 | Redesigned in Phase 7 |
| `IPaginatedDataProvider<T>` | `IPageLoader<T>` | 4 | |
| `PaginationResult<T>` | `PageResult<T>` | 4 | |
| `IScrollSource<T>` | `IItemSourceProvider<T>` | 4 | |
| `CompositeDataSource<T, U>` (other project) | `SectionedSource<TItem, THeader>` | 9 | |
| `IScrollMultiSource<T, U>` (other project) | `ISectionedSourceProvider<TItem, THeader>` | 9 | |
| (scroller subclass for sections, other project) | `SectionedSlotList<TItem, THeader>` | 9 | |
| `ICellSizeCalculator<T>` | (deleted) | 8 | Unity layout replaces it. Leave the old name alone in Phase 4, since it's deleted later |
| `BaseCellSizeCalculator<T>` | (deleted) | 8 | |
| `DynamicCellSizeCoordinator<T>` | (deleted) | 8 | Replaced by size storage in `LayoutModel`, `SlotMeasurer` and `SlotSizeWatcher` |
| `IScrollDirection` | `IAxisLayout` | 4 | |
| `ScrollDirectionBase` | `AxisLayoutBase` | 4 | |
| `ScrollDirectionFactory` | `AxisLayouts` | 4 | |
| `VerticalTopToBottomDirection` | `TopToBottomLayout` | 4 | |
| `VerticalBottomToTopDirection` | `BottomToTopLayout` | 4 | |
| `HorizontalLeftToRightDirection` | `LeftToRightLayout` | 4 | |
| `HorizontalRightToLeftDirection` | `RightToLeftLayout` | 4 | |
| `ViewportMetrics` | `AxisMetrics` | 4 | |
| `GridCountResolver` | `SlotsPerLaneResolver` | 4 | |
| `ScrollNavigator` | `ScrollMover` | 4 | |
| `ScrollActivityTracker` | `DragState` | 4 | |
| `PullToRefreshDetector` | `PullGesture` | 4 | |
| `PaginationBoundaryDetector` | `PagingTrigger` | 4 | Rewritten in Phase 7 |
| `DataSourceGuard<T>` | (folded into `SlotList`) | 5 | |
| `CellItem` | `Placement` | 5 | |
| `CellLine` | `Lane` | 5 | |
| `CellLineBuilder` | `LaneBuilder` | 5 | |
| `LinePositionCache` | (part of `LayoutModel`) | 5 | |
| `LineContentSizeCalculator` | (part of `LayoutModel`) | 5 | |
| `VisibleLineFinder` | `VisibleRangeFinder` | 5 | |
| `VisibleCellCalculator` | `PoolSizeEstimator` | 5 | |
| `ScrollStateManager` | `VisibleRange` (struct) | 5 | |
| `VisibilityUpdater` | (part of `SlotListEngine`) | 5 | |
| `CellPool` | `SlotPool` | 5 | One pool per view kind |
| `CellLineRecycler` + `CellLineDisplayer` | `SlotRecycler` | 5 | |
| `CellExpansionController` + `ExpansionOperationHandler` | `ExpansionState` | 5 | |
| `RebuildCoordinator` | (part of `SlotListEngine`) | 5 | |
| `RebuildCoroutineScheduler` | `DeferredWork` | 5 | |
| `ScrollComponentBinder` | (method on `SlotList`) | 5 | |
| `CellAnimationMarker` | `AppearAnimationPolicy` | 5 | |
| `PullToRefreshUI` | `PullIndicator` | 3 | Moves to the `Extras` sample |
| `PullRefreshState` | `PullIndicatorState` | 3 | |
| `EmptyStateDisplay` | `EmptyStatePanel` | 3 | Moves to the `Extras` sample |
| `RecyclableScrollRectBaseEditor` | `SlotListEditor` | 4 | |
| (new) | `SlotListSettings`, `SlotListDirection`, `SlotListEngine`, `LayoutModel`, `SlotHandle`, `SlotKind`, `RebuildLevel`, `ScrollAlign`, `SizingMode`, `SlotSizeWatcher`, `SlotMeasurer`, `SnapMode`, `IPagingFooter`, `PagingState`, `EntryKind` | 5–11 | |
| Samples: `SampleScroller`, `SampleCell`, `SampleItem`, `SampleScreen` | `SampleSlotList`, `SampleSlot`, `SampleItem`, `SampleScreen` | 4 | |

### A.3 Public members

| Old | New | Notes |
|---|---|---|
| `Initialize(source)` / `Initialize(source, calculator)` | `SetSource(source)` | Works before or after initialization (Phase 6.1). No calculator argument: sizing is a setting (Phase 8). |
| `SetDataSource(source)` | `SetSource(source)` | |
| `Refresh()` | `Refresh()` | Same name, incremental behaviour (6.10) |
| `Clear()` | `ClearViews()` | It only clears the views, not the data |
| `OnInitialized` (Action field) | `Ready` (event) | |
| `IsInitialized` | `IsReady` | |
| `IsScrolling` / `IsDragging` | `IsMoving` / `IsDragging` | |
| `VisibleStartIndex` / `VisibleEndIndex` (lanes) | `FirstVisibleIndex` / `LastVisibleIndex` (items) | Item indexes (6.13) |
| `IsFirstItemVisible` / `IsLastItemVisible` | `IsAtStart` / `IsAtEnd` | |
| `GetItemCount()` | `ItemCount` (property) | |
| `CellPrefab` | `ItemPrefab` | |
| `IsItemVisible(i)` | `IsVisible(i)` | |
| `ScrollToItem(i)` | `ScrollTo(i, align, duration, offset)` | Phase 11.1 |
| `ScrollToStart()` / `ScrollToEnd()` | `ScrollToStart()` / `ScrollToEnd()` | Plus an optional `duration` |
| `ForceStopScrolling()` | `StopMotion()` | |
| `IsIndexExpanded(i)` | `IsExpanded(i)` | |
| `ExpandCell(i, size)` | `Expand(i, size = -1)` | |
| `CellWantToCollapse(i)` | `Collapse(i)` | |
| `UpdateExpandedCellSize(i, size)` | (removed) | The watcher reports size changes (Phase 8.6). Keep `SetExpandedSize` only as a manual override if needed |
| (new) | `InvalidateSize(i)` / `InvalidateAllSizes()` | Phase 8.4 |
| (new) | `Toggle(i)` | Expand or collapse |
| `OnPullToRefreshDetected()` (virtual) | `PullRefreshRequested` (event) | The virtual can stay as well |
| `OnPaginationThresholdReached()` (virtual) | `PageRequested` (event) | |
| `IsHeader(i)` (virtual) | (removed) | Replaced by `SectionedSource` |
| `SupportsPagination()` (virtual) | (removed) | `source is IPagedSource<T>` |
| `IRecyclableCell.DefaultCellSize` | (removed) | `Fixed` reads the prefab's rect, `Layout` measures |
| `IRecyclableCell<T>.Data { set; }` | `ISlot<T>.Bind(T item, int index)` | A method makes "rebind everything here" explicit. Add `Unbind()` (default no-op) for cancelling image loads and similar. |
| `IExpandableRecyclableCellBase.ScrollRect` | `IExpandableSlot.Owner` | |
| `IExpandableRecyclableCellBase.CellDataIndex` | `IExpandableSlot.ItemIndex` | |
| `IExpandableRecyclableCellBase.CollapseCell()` | `IExpandableSlot.OnCollapsed()` | Called by the view when another item expands |
| `IExpandableRecyclableCell<T>.IsExpanded` | `IExpandableSlot.IsExpanded` | |
| `IAnimationMarker.ShouldAnimate` | `IAppearAnimation.ShouldPlay` | |
| `IRecyclableDataSource<T>.ItemCount` | `IItemSource<T>.Count` | |
| `IRecyclableDataSource<T>.GetItem(i)` | `IItemSource<T>.this[i]` | |
| `IRecyclableDataSource<T>.GetAllItems()` | (removed) | Was only used by `FilteredDataSource`, which now keeps indices |
| `ListDataSource.AddItem/AddItems/InsertItem/RemoveItem/RemoveItemAt/SetItems/ReplaceItems/Clear` | `ListSource.Add/AddRange/Insert/Remove/RemoveAt/SetAll/SetAll/Clear` | Drop `ReplaceItems` (same as `SetItems`) |
| `FilteredDataSource.SetPredicate/ClearPredicate` | `FilteredSource.SetFilter/ClearFilter` | |
| `PaginatedDataSource.LoadNextPageAsync()` | `PagedSource.LoadNextAsync(ct)` | |
| `PaginatedDataSource.OnDataChanged/OnLoadingStarted/OnLoadingFinished` | `ItemsAppended/LoadStarted/LoadFinished` events | |
| `ICellSizeCalculator.*` | (removed) | See Phase 8 |
| `injectedInstantiator` (internal field) | `instantiator` | Regenerate Fast IoC injectors |

### A.4 Serialized fields (inside `SlotListSettings` after Phase 5)

| Old | New |
|---|---|
| `cellPrefab` | `itemPrefab` (on `SlotList`, not in settings) |
| `headerPrefab` | `headerPrefab` (only on `SectionedSlotList`) |
| (new) | `footerPrefab` (Phase 7) |
| `isVertical` + `reverse` | `direction` (`SlotListDirection`) |
| `lineSpacing` | `laneSpacing` |
| `gridCellSpacing` | `slotSpacing` |
| `paddingStart` / `paddingEnd` | `paddingStart` / `paddingEnd` |
| (new) | `paddingCrossStart` / `paddingCrossEnd` (6.7) |
| `gridCount` | `slotsPerLane` |
| `useDynamicGridCount` | `autoSlotsPerLane` |
| `dynamicGridCellSize` | `targetSlotSize` |
| `minDynamicGridCount` / `maxDynamicGridCount` | `minSlotsPerLane` / `maxSlotsPerLane` |
| `allowMultipleExpandedCells` | `allowMultipleExpanded` |
| `scrollToInitOnEnable` | `resetScrollOnEnable` |
| `overridePoolPercentage` + `poolPercentage` | `poolBuffer` (one float, default 1.4) |
| `estimatedCellHeight` | `estimatedItemSize` (optional override. The default is the running average) |
| `cellsPerFrame` | `measureAheadPerFrame` (default 4, 0 = off) |
| (new) | `sizing` (`SizingMode`: `Auto`/`Fixed`/`Layout`, Phase 8) |
| (new) | `expandDuration` (Phase 8.6) |
| (new) | `warmupPerFrame`, `isolateCanvas` (Phase 10) |
| (new) | `pagingThresholdItems` (Phase 7) |
| (new) | `snap`, `snapAlign`, `loop` (Phase 11) |

---

## Appendix B: Rename tips

- **IDE rename** (Rider or Visual Studio): rename the symbol. It renames the file too, and Unity keeps the `.meta` if the IDE moves both. Check `git status` shows `renamed:` and not `deleted:` + `new file:` for each `.meta`.
- **Shell rename**: `git mv Foo.cs Bar.cs && git mv Foo.cs.meta Bar.cs.meta`, then change the class name in the file.
- **Leftover strings:** search for the old name in quotes too, for `FindProperty`, `nameof` and log messages: `grep -rn '"cellPrefab"\|RecyclableScrollRect\]' .`
- **Prefabs in the test project:** after renaming MonoBehaviours, open each scene and check for "Missing script". If you see one, the `.meta` GUID changed. Restore the old `.meta` from git.
