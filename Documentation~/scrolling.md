# Scrolling

[← Back to README](../README.md)

- [To an item](#to-an-item)
- [Snapping](#snapping)
- [Looping lists (carousels)](#looping-lists-carousels)
- [Carousel effects](#carousel-effects)
- [Nested scrolling](#nested-scrolling)
- [Paging](#paging)
- [Pull to refresh](#pull-to-refresh)

---

## To an item

```csharp
list.ScrollToItem(42);                                // jump, item at the start
list.ScrollToItem(42, ScrollAlign.Center, 0.4f);      // animate for 0.4 s, item in the middle
list.ScrollToItem(42, ScrollAlign.Nearest);           // move as little as possible; not at all if it's visible
list.ScrollToItem(42, ScrollAlign.Start, 0.3f, 16f);  // leave 16 px before it
```

`ScrollToStart(duration)` and `ScrollToEnd(duration)` do the same for the ends. The animation eases out, follows items measured on the way, and stops when the user drags or you call `ForceStopScrolling()`. `IsScrolling` is true while it runs.

## Snapping

- **Snap: Item** settles on the nearest item once a drag ends and the list slows down, lined up with the viewport's start, centre or end (**Snap Align**).
- **Snap: Page** moves one viewport at a time, like a pager: a flick goes to the next or previous page, a slow release to the nearest one.

Snapping also runs after the mouse wheel, once it stops turning. `SnappedTo(index)` tells you where it landed.

## Looping lists (carousels)

Tick **Loop** and the list goes round forever. Indexes stay real (`0` to `count - 1`) everywhere in the API, and `ScrollToItem` takes the shortest way round. For a carousel, combine it with **Snap: Item** and **Snap Align: Center**:

```csharp
carousel.SnappedTo += index => Debug.Log($"Showing {index}");
carousel.ScrollToItem(3, ScrollAlign.Center, 0.3f);
```

You never see the loop jump, and drags and flings carry on across it.
- **A short list** lays out a few copies of its items and jumps back by whole copies near either end.
- **A long one** lays out about ten screens of entries mapped to items through an offset, so the scroll position stays small and movement and inertia stay exact. In a long loop, `ScrollToItem` to an item far away jumps there instead of animating across thousands of items.

**What can loop:**

| List | Loops |
|---|---|
| A list, any direction | Yes |
| Bento | Yes, as a block per round (below) |
| Grid, Auto Grid | No |
| A paged source | Yes: its first page is the whole list, and no more pages load (a log says so; the inspector notes it under **Loop**) |
| Sections, expanding slots | No (it logs an error and doesn't loop) |
| Pull to refresh | Never fires: a looping list never reaches its start |

### Bento carousels

Tick **Loop** on a bento list and its tiles go round too: a featured tile and its smaller neighbours, then the next row of offers, and back to the start. A typical store strip:

```csharp
ScrollerSettings settings = offers.Settings;
settings.direction = ScrollerDirection.LeftToRight;   // rows of cells run sideways
settings.bento = true;                                // Layout JSON: 2 rows across, a 2 x 2 featured tile, then 1 x 1 tiles
settings.loop = true;
settings.snap = SnapMode.Item;
settings.snapAlign = SnapAlign.Start;
offers.Initialize(new ListSource<Offer>(todaysOffers));
```

- **Each round is a block.** The layout starts again with every round of items, so the same tiles always come back in the same places and the jump is invisible.
- **Fill whole patterns for a seamless strip.** When the item count doesn't fill the layout's last pattern (or row of spans), that round's last row keeps its holes, right before the next round starts. With a 4-tile pattern that repeats, use a count that completes it.
- **Snap: Item** settles on the nearest tile start, by the tile's own size. **Snap: Page** moves one viewport at a time; pages don't line up with patterns or rounds.
- **Not too long.** A bento loop is laid out as a few copies of its items. One whose round is tens of thousands of pixels long logs a warning and scrolls as a plain bento list.

## Carousel effects

A slot that implements `IViewportPositionListener` is told where it sits in the viewport every time the list moves: 0 at the centre, -0.5 at the start edge, +0.5 at the end edge, from the slot's centre along the scroll axis. Use it to scale, fade or tilt the centred card:

```csharp
public class CardSlot : MonoBehaviour, ISlot<CardItem>, IViewportPositionListener
{
    [SerializeField] private RectTransform face;   // a child: the list sizes the slot's root

    public void OnViewportPosition(float position)
    {
        float scale = Mathf.Lerp(1f, 0.8f, Mathf.Clamp01(Mathf.Abs(position) * 2f));
        face.localScale = new Vector3(scale, scale, 1f);
    }
    // ...
}
```

`list.GetViewportPosition(index)` answers the same for any shown item (NaN when it isn't shown). Lists whose slots don't implement the interface do no extra work.

## Nested scrolling

Put a horizontal carousel inside a vertical list (or the other way round) and it just works: a drag mostly across the carousel's axis scrolls the list around it, a drag along it scrolls the carousel. The mouse wheel follows the same rule. The gesture is handed over once, when it starts, to the nearest parent that handles drags: a UI Scroller, a plain `ScrollRect` or your own handler. Touching the carousel also stops the outer list's fling.

- The carousel can live in a pooled slot of the outer list: give it the row's source in the slot's `Data` setter (`Initialize` the first time, `SetSource` after) and call `ScrollToStart()` for a new row. The `NestedExample` in the Examples sample does exactly that.
- Exactly diagonal drags stay with the list under the finger. A list with no such parent behaves as before.
- To turn it off for a list: `list.Settings.passDragsToParent = false`.
- A vertical list inside another vertical list doesn't hand over at its edges yet.

## Paging

Implement `IPageLoader<T>` and wrap it in a `PagedSource<T>`. Pages start at 0. Throw to report a failure:

```csharp
public class InventoryPageLoader : IPageLoader<InventoryItem>
{
    private const int PageSize = 50;
    private const int PageCount = 10;

    public Task<PageResult<InventoryItem>> LoadAsync(int page, CancellationToken cancellation)
    {
        List<InventoryItem> items = new(PageSize);
        for (int i = 0; i < PageSize; i++)
            items.Add(new InventoryItem { Name = $"Item {page * PageSize + i}" });

        return Task.FromResult(new PageResult<InventoryItem>(items, page + 1 < PageCount));
    }
}
```

```csharp
list.SetSource(new PagedSource<InventoryItem>(new InventoryPageLoader()));

list.PageLoaded += count => Debug.Log($"{count} more items");
list.PageFailed += exception => Debug.LogWarning(exception.Message);
list.ReachedEnd += () => Debug.Log("No more pages");
```

- The list loads the first page by itself, then the next one whenever fewer than 10 items are left after the last visible one.
- New pages are appended without a rebuild: the scroll position and expanded items stay. A page that lands while the list bounces back from its end stops the bounce instead of drifting it back.
- Only one page loads at a time. Changing the source or destroying the list cancels a load in flight: the loader receives a cancelled token.
- `source.Reset()` starts again from page 0, for example on pull to refresh.

## Pull to refresh

Override `OnPullToRefreshDetected()`. It runs when the user releases a drag more than 75 px past the start of the list:

```csharp
public class RefreshingList : Scroller<InventoryItem>
{
    protected override void OnPullToRefreshDetected()
    {
        if (DataSource is IPagedSource<InventoryItem> paged)
            paged.Reset();
    }
}
```

The **Extras** sample has a `PullIndicator` for a status label and spinner, which you drive yourself, and an `EmptyStatePanel` that shows a message when the list is empty.
