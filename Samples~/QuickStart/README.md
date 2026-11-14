# Quick Start sample

1. Create a prefab with a `RectTransform`, a `TMP_Text` child and `SampleSlot` on its root. Assign the label.
2. Create a UI **Scroll View**, remove its `ScrollRect` and add `SampleSlotList`.
3. Assign the same **Viewport** and **Content** references, then assign your slot as **Slot Prefab**.
4. Remove any `LayoutGroup` or `ContentSizeFitter` from **Content**.
5. Add `SampleScreen` to a GameObject and assign the scroller.
6. Press Play and scroll the list.
