# Quick Start sample

The scripts for the README's quick start: a list of 10,000 items. The slot uses TextMeshPro.

1. Create a prefab with a `RectTransform`, a `TMP_Text` child and `SampleSlot` on its root. Assign the label.
2. Under your Canvas, create an empty UI GameObject with the size and anchors the list should have, and add `SampleScroller`. It builds its **Viewport** and **Content**.
3. Assign the prefab as **Slot Prefab**.
4. Add `SampleScreen` to a GameObject and assign the list.
5. Press Play and scroll the list.

For grids, sections, expandable rows, carousels and paging, import the **Examples** sample: each one builds its own UI in code.
