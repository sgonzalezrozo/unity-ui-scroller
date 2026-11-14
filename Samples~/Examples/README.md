# Examples sample

Five small scenes that build all their UI in code, so there are no prefabs or scenes to set up. They use Unity's built-in font, not TextMeshPro.

To run one, create an empty scene, add an empty GameObject, add one of these components and press Play:

| Component | Shows |
|---|---|
| `GridExample` | 1,000 tiles in an **Auto Grid** that fits as many columns as the width allows |
| `SectionsExample` | A `SectionedSlotList` with headers; an empty section is skipped |
| `ExpandableExample` | Rows that open to a fixed taller size when tapped, closing the others |
| `CarouselExample` | A horizontal carousel of 20 tiles that **loops** both ways and **snaps** to the centre |
| `PagingExample` | A `PagedSource` that loads 5 pages as you scroll (the last has 3 items). Pull down past the top to start again |

`ExampleUI` is the code that builds the Canvas, the list (with its Viewport and Content) and the slot templates. In your own project you'd build those in the editor instead, as the **Quick Start** sample and the README describe.
