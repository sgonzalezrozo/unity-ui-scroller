using System.Collections.Generic;
using UnityEngine;

namespace UIScroller
{
    /// <summary>
    /// The bento layout (Settings.bento): tiles of different sizes on a grid of cells. A BentoLayout hands out each
    /// item's tile; rows of cells are the layout model's lines, so offsets, recycling and paging work as for a list.
    /// Columns come from the layout (or Slots Per Line when it gives none), the row height from the column width and
    /// Cell Aspect. Tiles never expand and item slots are never measured. With sections, each header takes a whole
    /// row as tall as the header (fixed or measured, like any header), and the layout starts again in each section.
    /// </summary>
    internal sealed partial class ScrollerEngine<T>
    {
        private readonly List<BentoTile> bentoTiles = new();
        private bool isBento;
        private int bentoColumns;
        private BentoLayout appliedBentoLayout;
        private BentoArranger bentoArranger;
        private bool hasReportedBentoErrors;
        private bool hasWarnedAboutBentoMeasuring;
        private bool hasWarnedAboutBentoExpand;

        /// <summary>The layout the list was given: in code, or from its JSON field. Null uses 1x1 tiles.</summary>
        public BentoLayout BentoSource { get; set; }

        /// <summary>Works out whether this list uses the bento layout, and starts its tiles again from the first item.</summary>
        private void UpdateBento()
        {
            isBento = settings.bento;
            bentoTiles.Clear();
            if (!isBento)
            {
                bentoArranger = null;
                return;
            }

            appliedBentoLayout = BentoSource ?? BentoLayout.Uniform(Mathf.Clamp((int)settings.slotsPerLine, 1, BentoLayout.MAX_COLUMNS));
            ReportBentoErrors(appliedBentoLayout);
            bentoColumns = appliedBentoLayout.Columns > 0
                ? appliedBentoLayout.Columns
                : Mathf.Clamp((int)settings.slotsPerLine, 1, BentoLayout.MAX_COLUMNS);
            bentoArranger = appliedBentoLayout.CreateArranger(bentoColumns);
            bentoArranger.Reset();

            if (isLayoutSized && !hasWarnedAboutBentoMeasuring)
            {
                hasWarnedAboutBentoMeasuring = true;
                Debug.LogWarning($"[UIScroller] {host.gameObject.name}: a bento tile's size comes from its cells, so '{slotPrefab.name}' isn't measured.", host);
            }

            isLayoutSized = false;
        }

        private void ReportBentoErrors(BentoLayout layout)
        {
            if (layout.IsValid || hasReportedBentoErrors)
                return;

            hasReportedBentoErrors = true;
            Debug.LogError($"[UIScroller] {host.gameObject.name}: the bento layout has errors, so items are placed as 1x1 tiles:\n- {string.Join("\n- ", layout.Errors)}", host);
        }

        /// <summary>Uses another layout: tiles are worked out again and the list rebuilt, at the same scroll position.</summary>
        public void SetBentoLayout(BentoLayout bentoLayout)
        {
            BentoSource = bentoLayout;
            hasReportedBentoErrors = false;
            if (!isInitialized)
                return;

            UpdateBento();
            forceRebindAll = true;
            RebuildStructures();
            forceRebindAll = false;
        }

        private void BuildBentoLines(int fromItem)
        {
            if (headerPrefab != null)
            {
                ArrangeSections();
                layout.BuildBentoSectionLines(ItemCount, bentoTiles, BentoRowSize(), bentoColumns, isHeaderFunc, itemSizeFunc);
            }
            else
            {
                while (bentoTiles.Count < ItemCount)
                    bentoTiles.Add(bentoArranger.Next());

                layout.BuildBentoLines(ItemCount, bentoTiles, BentoRowSize(), bentoColumns, fromItem);
            }

            if (recycler != null)
            {
                recycler.BentoColumns = bentoColumns;
                recycler.BentoTiles = bentoTiles;
            }
            if (visibilityPass != null)
                visibilityPass.ExtraLinesAbove = appliedBentoLayout.MaxRowSpan - 1;
        }

        /// <summary>Tiles for every entry: a header spans every column and starts the layout again for its section.</summary>
        private void ArrangeSections()
        {
            bentoTiles.Clear();
            bentoArranger.Reset();
            for (int i = 0; i < ItemCount; i++)
            {
                if (hooks.IsHeader(i))
                {
                    bentoArranger.Reset();
                    bentoTiles.Add(new BentoTile(0, 0, bentoColumns, 1));
                }
                else
                {
                    bentoTiles.Add(bentoArranger.Next());
                }
            }
        }

        private float BentoCellCross()
        {
            float cross = viewportMetrics.GetCrossAxisViewportSize();
            return Mathf.Max(1f, (cross - settings.slotSpacing * (bentoColumns - 1)) / bentoColumns);
        }

        private float BentoRowSize()
            => BentoCellCross() * (settings.cellAspect > 0f ? settings.cellAspect : 1f);

        /// <summary>Slots for the rows that fit on screen, plus the tallest tile, times the columns.</summary>
        private int BentoPoolSize()
        {
            float row = BentoRowSize() + settings.lineSpacing;
            int rows = Mathf.CeilToInt(viewportSize / Mathf.Max(1f, row)) + appliedBentoLayout.MaxRowSpan;
            return Mathf.CeilToInt(rows * settings.EffectivePoolBuffer) * bentoColumns;
        }

        /// <summary>The width changed: rows follow the new column width, so every tile is placed again.</summary>
        private void RebuildBentoForNewWidth()
        {
            BuildLines();
            RecalculateContentSize();
            layout.RecomputeOffsets();
            visibleRange.ResetState();
            RebuildVisibleItems();
        }

        private bool RejectInBento()
        {
            if (!isBento)
                return false;

            if (!hasWarnedAboutBentoExpand)
            {
                hasWarnedAboutBentoExpand = true;
                Debug.LogWarning($"[UIScroller] {host.gameObject.name}: bento tiles don't expand.", host);
            }

            return true;
        }

        /// <summary>An item's tile, or a 0x0 tile before initialization, outside bento, or for a bad index.</summary>
        public BentoTile QueryTile(int index)
            => isBento && index >= 0 && index < layout.Placements.Length && index < bentoTiles.Count ? bentoTiles[index] : default;

        /// <summary>The length of an item along the scroll axis: its tile in bento, its line otherwise.</summary>
        private float MainSizeOf(int index, int line)
            => isBento ? layout.Placements[index].Size : layout.Lines[line].Size;

        /// <summary>The first item shown in visible lines, skipping rows that hold no tile start.</summary>
        private int FirstItemInVisibleLines()
        {
            for (int line = FirstVisibleLine; line >= 0 && line <= LastVisibleLine; line++)
            {
                int item = FirstItemIn(line);
                if (item >= 0)
                    return item;
            }

            return -1;
        }

        private int LastItemInVisibleLines()
        {
            for (int line = LastVisibleLine; line >= 0 && line >= FirstVisibleLine; line--)
            {
                int item = LastItemIn(line);
                if (item >= 0)
                    return item;
            }

            return -1;
        }
    }
}
