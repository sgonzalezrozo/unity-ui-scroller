# Extras sample

Optional UI pieces that sit next to a scroller. They use TextMeshPro.

| Component | What it does |
|---|---|
| `PullIndicator` | A status label and spinner for pull to refresh. Call `SetPullState` from your scroller's `OnPullToRefreshDetected` override and when loading finishes. |
| `EmptyStatePanel` | Shows a message when the data source is empty. Call `SetSource(source)` once and `OnDataSourceChanged()` after each change. |
