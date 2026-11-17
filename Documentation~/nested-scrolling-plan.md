# Nested scrolling plan

Native nested scrolling for UI Scroller: a horizontal carousel inside a vertical list (or the other way round) where a drag across the inner list's axis moves the outer list instead. Today the inner list takes every drag that starts on it, so the outer list can't be scrolled from there. MyTools works around it with a third-party MIT plugin.

Status: proposal, not started. Target: **0.5.0**, the first release (it's the one gap that shows up in every F2P store and lobby).

## How others do it

The usual approach, and the one in [kwan3854/Nested-Scroll-Rect](https://github.com/kwan3854/Nested-Scroll-Rect) (MIT), is to **pass the gesture from the inner scroller to the outer one**. The plugin does it by relaying every event:

- A `ScrollRect` subclass overrides `OnInitializePotentialDrag`, `OnBeginDrag`, `OnDrag` and `OnEndDrag`.
- At `OnBeginDrag` it compares the drag's horizontal and vertical movement with its own axis. If the movement is mostly across its axis, it doesn't start its own drag and **forwards that event and the rest of the gesture** to the parent `ScrollRect`'s drag handlers.
- `OnInitializePotentialDrag` always goes to both, so touching the inner list also stops the outer list's fling, as touching it directly would.

Its limits, read from the code:
- the decision uses a single event's `delta`;
- the parent is found and cached once in `Awake`, so reparenting or a pooled slot breaks it;
- there's no mouse-wheel routing;
- there's no hand-off for same-axis nesting (a vertical list inside a vertical one);
- there are no settings.

## Ground rules

1. **Built into `ScrollerBase`**, not a separate component: every UI Scroller can be the inner list, and any parent that handles drags can be the outer one (a UI Scroller, a plain `ScrollRect`, or a custom handler).
2. **On by default and invisible** when there's nothing to hand to: a list without a scrollable parent behaves exactly as today. The inspector gets at most one toggle (see *Inspector*).
3. **The list's own features keep working.** The loop drag restart, snapping, pull to refresh, the drag state and scroll animations must ignore gestures that went to the parent.
4. **Test-first**, with the rig: synthetic drags on an inner list inside a spy parent and inside a real outer list.

## Design

### When a gesture goes to the parent

Decided once per gesture, in `OnBeginDrag`:

| Case | Goes to |
|---|---|
| Movement mostly along the list's axis | The list itself (as today) |
| Movement mostly across the list's axis, and a parent handles drags | The parent |
| Exactly diagonal (equal components) | The list itself |
| No parent handles drags | The list itself |

**Which movement:** `eventData.position - eventData.pressPosition` (the whole movement since the press), not the last event's `delta`. Unity only sends `OnBeginDrag` after the pointer has moved past the EventSystem's drag threshold, so that vector is long enough to read reliably. A single `delta` can be one noisy pixel.

**The parent** is looked up at each `OnBeginDrag`, starting from `transform.parent`: the first object above that handles `IBeginDragHandler` (`ExecuteEvents.GetEventHandler`). Nothing is cached in `Awake`, so it keeps working when a list moves, as an inner list in a pooled slot does.

### Handing the gesture over (once)

At `OnBeginDrag`, a gesture that goes to the parent is **handed over**, not relayed:

1. The list calls the parent handler's `OnBeginDrag` with the same `PointerEventData`, and skips its own (`base.OnBeginDrag`, `engine.OnDragBegin`, `RememberLeftDrag`).
2. It sets `eventData.pointerDrag` to the parent handler's GameObject.

From then on, the input module sends that gesture's `OnDrag` and `OnEndDrag` **straight to the parent**: both modules read `pointerDrag` every frame to choose the target (`PointerInputModule` in uGUI and `InputSystemUIInputModule`, checked in their source). The inner list is out of the loop until the finger lifts.

`OnInitializePotentialDrag` (the touch, before any movement) is passed to the parent as well as handled by the list, so touching a carousel stops the outer list's fling.

**Why hand over instead of relaying every event** (the plugin's way):
- **Cost:** one hierarchy walk and one call per gesture, then nothing. Relaying adds a call per drag event and a "this gesture is the parent's" flag on the inner list. Either is cheap next to what moving the outer list costs, but handing over is the minimum.
- **Pooled slots:** while the outer list scrolls, it recycles the slot that holds the carousel the finger started on. The carousel gets parked or rebound to another row mid-gesture. A relay keeps depending on that carousel for the rest of the gesture; after a hand-over, the carousel no longer takes part.
- **Several levels:** the parent's `OnBeginDrag` runs normally. If the parent is another UI Scroller, it makes the same decision and can hand the gesture further up.
- **Nothing to clean up:** the inner list keeps no per-gesture state. The input module drops `pointerDrag` when the finger lifts.

To check in N1: a test that drives a real EventSystem with both input modules (or, if that can't run in batch mode, a hand check on a phone with the Input System module) to confirm `OnEndDrag` reaches the parent after the hand-over.

### What the list's features do with a gesture handed over

| Feature | Behaviour |
|---|---|
| Loop drag restart (`dragRestart`, `isLeftDragging`) | Not set: the gesture isn't the list's drag |
| Snap after drag | Doesn't run (`engine.OnDragEnd` isn't called) |
| Pull to refresh | Not detected |
| `IsDragging`, `IsScrolling` | Stay false on the inner list |
| A running scroll animation | Keeps running: the user is moving the parent, not this list |
| Inner list's inertia | Stopped by `OnInitializePotentialDrag` on touch, as today |

### Mouse wheel (phase N2)

Unity sends the wheel to the innermost scroll handler under the pointer. A horizontal-only `ScrollRect` turns a vertical wheel into horizontal movement, so today a carousel eats the vertical wheel and the page doesn't scroll.

The rule: a wheel movement mostly **across** the list's axis goes to the parent, when there is one. A vertical wheel over a horizontal carousel scrolls the page; a horizontal wheel (or a trackpad swipe) scrolls the carousel. A list with no parent keeps Unity's behaviour.

### Same-axis hand-off at the edges (phase N3, optional)

For a vertical list inside a vertical one (a short inner list in a long page), "scroll chaining": when the inner list is at its edge and the drag keeps going outwards, the gesture goes to the parent.

This is harder than the cross-axis case:
- the decision can change mid-gesture, when the inner list reaches its edge;
- the inner list's elastic overscroll has to stay out of the way;
- a list whose content fits in its viewport (it can't scroll) should hand everything to the parent from the start.

Planned as a separate phase with its own setting, and only if a real screen needs it. The cross-axis case covers the common layouts.

## Inspector

One toggle under **Behaviour**: **Pass Drags To Parent** (`Settings.passDragsToParent`), on by default.
- It's needed only to opt out (for example, to keep the old behaviour while migrating from the plugin).
- It does nothing when the list has no parent that handles drags.

If N3 is done, a second toggle comes with it: **Chain At Edges**, off by default.

## Phases (test-first)

| # | Step | Tests | Done when |
|---|---|---|---|
| N0 | Settle the open decisions below | — | Written down here |
| N1 | Cross-axis hand-over: decision at begin, parent lookup, hand-over through `pointerDrag`, potential drag passed on, features ignore handed-over gestures | PlayMode: an inner horizontal list in a spy parent hands over a vertical drag and keeps a horizontal one; same in all four directions; no parent keeps every drag; diagonal stays; loop, snap and pull to refresh don't react to a handed-over drag; the next drag events reach the parent through `pointerDrag`; a real outer UI Scroller moves; two levels; touching the inner list stops the outer fling | A carousel inside a vertical list scrolls both ways |
| N2 | Mouse-wheel routing | PlayMode: a vertical wheel over a horizontal inner list scrolls the parent; a horizontal wheel scrolls the inner list; no parent keeps Unity's behaviour | Desktop and WebGL feel right |
| N3 | (Optional) Same-axis chaining at the edges, and non-scrollable inner lists | PlayMode: drag past the inner list's end moves the parent; a fitting inner list hands everything over | Only if a screen needs it |
| N4 | `NestedExample` in the Examples sample (a vertical list of rows, each a horizontal carousel with snapping), README section, CHANGELOG, agent rules; replace the plugin in MyTools | Smoke test of the sample; full verify | Released |

Existing tests must pass at every step: a list with no scrollable parent behaves exactly as today.

## Open decisions

1. **On by default?** Recommended: yes. It only acts when a parent handles drags, and in those layouts today's behaviour is the bug.
2. **Mouse wheel routing (N2):** yes, with the rule above? Recommended: yes, in the same release.
3. **Same-axis chaining (N3):** do it now, later, or not at all? Recommended: later, only when a screen needs it.
4. **An angle bias:** should a drag need to be clearly across the axis (for example, more than 60° from the list's axis) before it goes to the parent, instead of the plain "larger component wins"? Recommended: start with the plain comparison, which is what the plugin does and what users expect, and add a bias only if testing on a phone shows diagonal flicks going the wrong way.
