"""Draws the UI Scroller Inspector, as Unity shows it, for each example in examples.md.

Run from anywhere:  python3 generate.py
It rewrites the .svg files next to it. The rows follow Editor/ScrollerEditor.cs (DrawnEntries and LabelFor):
change both together. Settings an example changes get a numbered badge, matching the steps in examples.md.
"""
import pathlib
import re
from html import escape

OUT = pathlib.Path(__file__).parent

# Unity dark theme, at 1x.
BG, TITLE_BG, LINE = "#383838", "#3e3e3e", "#232323"
TEXT, MUTED = "#d2d2d2", "#9a9a9a"
FIELD, FIELD_BORDER = "#2a2a2a", "#212121"
POPUP, BUTTON = "#515151", "#585858"
ACCENT = "#f2a33a"
FONT = "Inter, -apple-system, 'Segoe UI', Helvetica, Arial, sans-serif"

W, GUTTER = 524, 30            # Inspector width, and a right gutter for the badges
LABEL_X, INDENT, FIELD_X = 17, 15, 216
ROW, FIELD_H, HEADER_GAP = 20, 16, 8

DEFAULTS = dict(direction="TopToBottom", lineSpacing=0, slotSpacing=0, sectionSpacing=0, headerContentSpacing=0,
                paddingStart=0, paddingEnd=0, slotsPerLine=1, autoSlotsPerLine=False, targetSlotSize=240,
                minSlotsPerLine=1, maxSlotsPerLine=8, allowMultipleExpanded=False, bento=False, cellAspect=1,
                loop=False, snap="None", snapAlign="Start", resetScrollOnEnable=False, overridePoolBuffer=False,
                poolBuffer=1.4, bentoLayoutJson=None)


def nicify(name):
    spaced = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", " ", name)
    return spaced[:1].upper() + spaced[1:]


def layout_mode(s):
    if s["bento"]:
        return "Bento"
    if s["autoSlotsPerLine"]:
        return "AutoGrid"
    return "Grid" if s["slotsPerLine"] > 1 else "List"


def label_for(path, direction):
    vertical = direction in ("TopToBottom", "BottomToTop")
    start = {"TopToBottom": "Top", "BottomToTop": "Bottom", "LeftToRight": "Left"}.get(direction, "Right")
    end = {"TopToBottom": "Bottom", "BottomToTop": "Top", "LeftToRight": "Right"}.get(direction, "Left")
    lines, slots = ("Row", "Column") if vertical else ("Column", "Row")
    return {
        "paddingStart": "Padding " + start, "paddingEnd": "Padding " + end,
        "lineSpacing": lines + " Spacing", "slotSpacing": slots + " Spacing", "slotsPerLine": slots + "s",
        "targetSlotSize": "Target Column Width" if vertical else "Target Row Height",
        "minSlotsPerLine": "Min " + slots + "s", "maxSlotsPerLine": "Max " + slots + "s",
        "bentoLayoutJson": "Layout JSON", "sectionSpacing": "Section Spacing",
        "headerContentSpacing": "Header To Content Spacing",
    }.get(path, nicify(path))


def entries(s, sectioned):
    """The Inspector's rows, in order, as ScrollerEditor.DrawnEntries draws them."""
    e = [("header", "Slots"), "slotPrefab"]
    if sectioned:
        e.append("headerPrefab")
    e += [("header", "Layout"), "direction", "layoutMode"]
    mode = layout_mode(s)
    e += {"Grid": ["slotsPerLine", "slotSpacing"],
          "AutoGrid": ["targetSlotSize", "minSlotsPerLine", "maxSlotsPerLine", "slotSpacing"],
          "Bento": ["bentoLayoutJson", "cellAspect", "slotSpacing"]}.get(mode, [])
    e += [("header", "Spacing & Padding"), "lineSpacing"]
    if sectioned:
        e += ["sectionSpacing", "headerContentSpacing"]
    e += ["paddingStart", "paddingEnd", ("header", "Behaviour")]
    can_loop = mode in ("List", "Bento") and not sectioned
    if can_loop:
        e.append("loop")
    e.append("snap")
    if s["snap"] == "Item":
        e.append("snapAlign")
    if mode != "Bento" and (not can_loop or not s["loop"]):
        e.append("allowMultipleExpanded")
    e += ["resetScrollOnEnable", "overridePoolBuffer"]
    if s["overridePoolBuffer"]:
        e.append("poolBuffer")
    return e


INDENTED = {"slotsPerLine", "slotSpacing", "targetSlotSize", "minSlotsPerLine", "maxSlotsPerLine", "poolBuffer",
            "bentoLayoutJson", "cellAspect"}
BOOLS = {"loop", "allowMultipleExpanded", "resetScrollOnEnable", "overridePoolBuffer"}
ENUMS = {"direction", "snap", "snapAlign", "layoutMode"}


def num(v):
    return f"{v:g}" if isinstance(v, float) else str(v)


class Svg:
    def __init__(self):
        self.parts, self.y = [], 0

    def add(self, s):
        self.parts.append(s)

    def text(self, x, y, s, color=TEXT, weight=400, anchor="start", size=12):
        self.add(f'<text x="{x}" y="{y}" fill="{color}" font-size="{size}" font-weight="{weight}" '
                 f'text-anchor="{anchor}">{escape(s)}</text>')

    def rect(self, x, y, w, h, fill, stroke=None, r=3):
        st = f' stroke="{stroke}"' if stroke else ""
        self.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}"{st}/>')


def icon(svg, kind, x, y):
    if kind == "prefab":   # the blue prefab cube
        svg.add(f'<path d="M{x+6} {y} l6 3 v7 l-6 3 l-6 -3 v-7 z" fill="#5fa8e8"/>'
                f'<path d="M{x} {y+3} l6 3 l6 -3 M{x+6} {y+6} v7" stroke="#2f6aa0" fill="none" stroke-width="1"/>')
    elif kind == "text":   # a text asset page
        svg.add(f'<rect x="{x+1}" y="{y}" width="10" height="13" rx="1" fill="#e6e6e6"/>'
                f'<path d="M{x+3} {y+4} h6 M{x+3} {y+7} h6 M{x+3} {y+10} h4" stroke="#777" stroke-width="1"/>')


def field_row(svg, label, kind, value, y, indent, extra=None):
    svg.text(LABEL_X + (INDENT if indent else 0), y + 13, label)
    fx, fw = FIELD_X, W - FIELD_X - 4
    if kind == "bool":
        svg.rect(fx, y + 2, 14, 14, FIELD, FIELD_BORDER, 2)
        if value:
            svg.add(f'<path d="M{fx+3} {y+9} l3 3 l6 -7" stroke="{TEXT}" stroke-width="1.8" fill="none"/>')
    elif kind == "enum":
        svg.rect(fx, y + 1, fw, FIELD_H, POPUP, FIELD_BORDER)
        svg.text(fx + 5, y + 13, value)
        svg.add(f'<path d="M{fx+fw-14} {y+7} h8 l-4 5 z" fill="{TEXT}"/>')
    elif kind == "object":
        svg.rect(fx, y + 1, fw, FIELD_H, FIELD, FIELD_BORDER)
        if value:
            icon(svg, extra or "prefab", fx + 4, y + 3)
            svg.text(fx + 20, y + 13, value)
        else:
            svg.text(fx + 5, y + 13, "None (" + ("Text Asset" if extra == "text" else "Game Object") + ")", MUTED)
        svg.add(f'<circle cx="{fx+fw-9}" cy="{y+9}" r="4.5" stroke="{TEXT}" fill="none"/>'
                f'<circle cx="{fx+fw-9}" cy="{y+9}" r="1.6" fill="{TEXT}"/>')
    elif kind == "slider":
        sw = fw - 60
        svg.rect(fx, y + 8, sw, 3, "#5e5e5e", None, 1)
        t = (value - 1.4) / (3.0 - 1.4)
        svg.add(f'<circle cx="{fx + t * sw}" cy="{y+9.5}" r="5" fill="#9c9c9c"/>')
        svg.rect(fx + sw + 8, y + 1, 52, FIELD_H, FIELD, FIELD_BORDER)
        svg.text(fx + sw + 13, y + 13, num(value))
    else:
        svg.rect(fx, y + 1, fw, FIELD_H, FIELD, FIELD_BORDER)
        svg.text(fx + 5, y + 13, num(value))


def inspector(name, component, settings=None, slot=None, header=None, layout_json=None, marks=None, sectioned=False):
    """marks: {setting path or 'slotPrefab'...: badge number}."""
    s = dict(DEFAULTS, **(settings or {}))
    marks = marks or {}
    svg = Svg()
    rows = entries(s, sectioned)

    # Title bar
    svg.rect(0, 0, W, 21, TITLE_BG, None, 0)
    svg.add(f'<path d="M5 7 l4 6 l4 -6 z" fill="{MUTED}"/>')
    svg.rect(19, 3, 15, 15, "#e8e8e8", None, 2)
    svg.text(26.5, 15, "#", "#3b7d3b", 700, "middle", 12)
    svg.rect(40, 3, 15, 15, FIELD, FIELD_BORDER, 2)
    svg.add(f'<path d="M43 10.5 l3 3 l6 -7" stroke="{TEXT}" stroke-width="1.8" fill="none"/>')
    svg.text(61, 15, nicify(component) + " (Script)", TEXT, 600)
    svg.add(f'<line x1="0" y1="21" x2="{W}" y2="21" stroke="{LINE}"/>')

    y, highlights, help_after = 24, [], None
    for entry in rows:
        if isinstance(entry, tuple):
            y += HEADER_GAP
            svg.text(LABEL_X, y + 13, entry[1], TEXT, 700)
            y += ROW
            continue
        top = y
        if entry == "slotPrefab":
            field_row(svg, "Slot Prefab", "object", slot, y, False, "prefab")
        elif entry == "headerPrefab":
            field_row(svg, "Header Prefab", "object", header, y, False, "prefab")
        elif entry == "layoutMode":
            field_row(svg, "Layout", "enum", nicify(layout_mode(s)), y, False)
        elif entry == "bentoLayoutJson":
            field_row(svg, "Layout JSON", "object", layout_json, y, True, "text")
            y += ROW
            fx, fw = FIELD_X, W - FIELD_X - 4
            svg.rect(fx, y + 1, fw, FIELD_H, BUTTON, FIELD_BORDER)
            svg.text(fx + fw / 2, y + 13, "Edit Layout…" if layout_json else "New Layout…", TEXT, 400, "middle")
        else:
            value = s[entry]
            kind = "bool" if entry in BOOLS else "enum" if entry in ENUMS else "slider" if entry == "poolBuffer" else "num"
            if kind == "enum":
                value = nicify(value)
            field_row(svg, label_for(entry, s["direction"]), kind, value, y, entry in INDENTED)
        y += ROW
        if entry in marks:
            highlights.append((top, y, marks[entry]))
        if entry == "loop" and s["loop"]:
            # The Inspector's info box under Loop.
            svg.rect(LABEL_X - 13, y + 2, W - 8, 30, "#454545", "#2b2b2b")
            svg.add(f'<circle cx="{LABEL_X + 3}" cy="{y + 17}" r="7" fill="#7aa5d6"/>')
            svg.text(LABEL_X + 3, y + 21, "i", "#1f1f1f", 700, "middle", 11)
            svg.text(LABEL_X + 16, y + 14, "Loop overrides paging: with a paged source, the first page is", TEXT, 400, "start", 10.5)
            svg.text(LABEL_X + 16, y + 27, "the whole list and no more pages load.", TEXT, 400, "start", 10.5)
            y += 36

    height = y + 6
    for top, bottom, n in highlights:
        svg.add(f'<rect x="2" y="{top}" width="{W - 2}" height="{bottom - top}" rx="4" fill="none" '
                f'stroke="{ACCENT}" stroke-width="1.5"/>')
        cy = (top + bottom) / 2
        svg.add(f'<circle cx="{W + GUTTER / 2}" cy="{cy}" r="9" fill="{ACCENT}"/>')
        svg.text(W + GUTTER / 2, cy + 4, str(n), "#1f1f1f", 700, "middle", 11)

    total = W + GUTTER
    body = "\n".join(svg.parts)
    out = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{total}" height="{height}" viewBox="0 0 {total} {height}" '
           f'font-family="{FONT}">\n<rect width="{W}" height="{height}" fill="{BG}"/>\n{body}\n</svg>\n')
    (OUT / f"{name}.svg").write_text(out)
    return name


def hierarchy(name, rows, marks=None):
    """A Hierarchy panel. rows: (depth, label, kind) with kind 'canvas', 'list', 'rect' or 'go'."""
    marks = marks or {}
    w, row = 300, 18
    svg = Svg()
    svg.rect(0, 0, w, 21, TITLE_BG, None, 0)
    svg.text(10, 15, "Hierarchy", TEXT, 600)
    svg.add(f'<line x1="0" y1="21" x2="{w}" y2="21" stroke="{LINE}"/>')
    y = 24
    highlights = []
    for depth, label, kind in rows:
        x = 10 + depth * 14
        has_children = any(d == depth + 1 for d, _, _ in rows[rows.index((depth, label, kind)) + 1:
                                                                   rows.index((depth, label, kind)) + 2])
        if has_children:
            svg.add(f'<path d="M{x} {y+6} l7 0 l-3.5 5 z" fill="{MUTED}"/>')
        ix = x + 11
        color = {"canvas": "#7fb3e6", "list": "#9ad08a", "rect": "#b9b9b9", "go": "#b9b9b9"}[kind]
        svg.add(f'<rect x="{ix}" y="{y+3}" width="11" height="11" rx="2" fill="none" stroke="{color}" stroke-width="1.4"/>')
        svg.text(ix + 16, y + 13, label)
        if label in marks:
            highlights.append((y, y + row, marks[label]))
        y += row
    height = y + 6
    for top, bottom, n in highlights:
        svg.add(f'<rect x="2" y="{top}" width="{w - 4}" height="{bottom - top}" rx="4" fill="none" stroke="{ACCENT}" stroke-width="1.5"/>')
        svg.add(f'<circle cx="{w + 15}" cy="{(top + bottom) / 2}" r="9" fill="{ACCENT}"/>')
        svg.text(w + 15, (top + bottom) / 2 + 4, str(n), "#1f1f1f", 700, "middle", 11)
    total = w + 30
    out = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{total}" height="{height}" viewBox="0 0 {total} {height}" '
           f'font-family="{FONT}">\n<rect width="{w}" height="{height}" fill="{BG}"/>\n' + "\n".join(svg.parts) + "\n</svg>\n")
    (OUT / f"{name}.svg").write_text(out)


# ---------- The examples ----------

hierarchy("hierarchy-list", [
    (0, "Canvas", "canvas"), (1, "InventoryList", "list"), (2, "Viewport", "rect"), (3, "Content", "rect"),
    (0, "EventSystem", "go")], marks={"InventoryList": 1, "Viewport": 2, "Content": 3})

inspector("simple-list", "InventoryList", dict(lineSpacing=8, paddingStart=16, paddingEnd=16),
          slot="InventorySlot", marks={"slotPrefab": 1, "lineSpacing": 2, "paddingStart": 3, "paddingEnd": 3})

inspector("measured-list", "MessageList", dict(lineSpacing=12, paddingStart=16, paddingEnd=16),
          slot="MessageSlot", marks={"slotPrefab": 1, "lineSpacing": 2})

inspector("horizontal-list", "HeroList", dict(direction="LeftToRight", lineSpacing=16, paddingStart=24, paddingEnd=24),
          slot="HeroSlot", marks={"direction": 1, "lineSpacing": 2, "paddingStart": 3, "paddingEnd": 3})

inspector("grid", "CardGrid", dict(slotsPerLine=3, slotSpacing=12, lineSpacing=12, paddingStart=16, paddingEnd=16),
          slot="CardSlot", marks={"layoutMode": 1, "slotsPerLine": 2, "slotSpacing": 3, "lineSpacing": 3})

inspector("auto-grid", "CardGrid", dict(autoSlotsPerLine=True, slotsPerLine=2, targetSlotSize=200, minSlotsPerLine=2,
                                        maxSlotsPerLine=6, slotSpacing=12, lineSpacing=12, paddingStart=16, paddingEnd=16),
          slot="CardSlot", marks={"layoutMode": 1, "targetSlotSize": 2, "minSlotsPerLine": 3, "maxSlotsPerLine": 3})

inspector("bento", "ShopList", dict(bento=True, cellAspect=1, slotSpacing=10, lineSpacing=10, paddingStart=16, paddingEnd=16),
          slot="ShopTile", layout_json="ShopBentoLayout",
          marks={"layoutMode": 1, "bentoLayoutJson": 2, "cellAspect": 3, "slotSpacing": 4, "lineSpacing": 4})

inspector("sections", "InventorySectionsList", dict(lineSpacing=8, sectionSpacing=24, headerContentSpacing=8,
                                                     paddingStart=16, paddingEnd=16),
          slot="InventorySlot", header="InventoryHeaderSlot", sectioned=True,
          marks={"slotPrefab": 1, "headerPrefab": 1, "sectionSpacing": 2, "headerContentSpacing": 3})

inspector("bento-sections", "ShopSectionedScroller", dict(bento=True, cellAspect=0.5, slotSpacing=10, lineSpacing=20,
                                                          sectionSpacing=60, headerContentSpacing=40,
                                                          paddingStart=100, paddingEnd=100),
          slot="EnemyItem", header="ShopSectionHeader", layout_json="EnemyBentoLayout", sectioned=True,
          marks={"headerPrefab": 1, "layoutMode": 2, "sectionSpacing": 3, "headerContentSpacing": 3})

inspector("expandable", "QuestList", dict(lineSpacing=8, paddingStart=16, paddingEnd=16),
          slot="QuestSlot", marks={"slotPrefab": 1, "allowMultipleExpanded": 2})

inspector("paging", "FeedList", dict(lineSpacing=8, paddingStart=16, paddingEnd=16),
          slot="FeedSlot", marks={"slotPrefab": 1})

inspector("carousel", "CardCarousel", dict(direction="LeftToRight", lineSpacing=24, loop=True, snap="Item",
                                           snapAlign="Center"),
          slot="CarouselCardSlot", marks={"direction": 1, "lineSpacing": 2, "loop": 3, "snap": 4, "snapAlign": 5})

inspector("pager", "OnboardingPager", dict(direction="LeftToRight", snap="Page"),
          slot="OnboardingPageSlot", marks={"direction": 1, "snap": 2})

inspector("nested-outer", "ShelfList", dict(lineSpacing=24, paddingStart=16, paddingEnd=16),
          slot="ShelfSlot", marks={"slotPrefab": 1})

inspector("nested-inner", "ShelfCarousel", dict(direction="LeftToRight", lineSpacing=16, paddingStart=16, paddingEnd=16),
          slot="ProductCardSlot", marks={"direction": 1, "lineSpacing": 2})

print("Wrote", len(list(OUT.glob("*.svg"))), "images to", OUT)
