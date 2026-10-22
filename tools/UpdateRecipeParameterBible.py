"""Write crafting minigame parameter columns onto the Design Bible recipe tabs.

The data file comes from `dotnet run --project tools/SWLOR.RecipeParameters -- export <data.json>`,
which reads the parameter values compiled into the recipe definitions. For each recipe tab this script
appends one column per parameter header after the tab's last populated header and fills the cell of every
recipe row from the data. Existing columns are never renamed or moved, headers that already exist are
reused, and every other part of the workbook is copied unchanged. Running it again with the same data
leaves the workbook byte-for-byte alone.

It also records the new columns in tools/CombatUpgradeBibleWorkbookLayout.json, because
tools/FormatCombatUpgradeBibleWorkbook.ps1 rewrites every tab's column widths from that file.
"""
from __future__ import annotations

import argparse
import io
import json
import os
import re
import sys
import tempfile
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET
from xml.sax.saxutils import escape, unescape

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_WORKBOOK = ROOT / "design" / "bible" / "SWLOR Design Bible.xlsx"
DEFAULT_LAYOUT = ROOT / "tools" / "CombatUpgradeBibleWorkbookLayout.json"
RECIPE_TABS = ("Smithery Recipes", "Engineering Recipes", "Cooking Recipes", "Fabrication Recipes")
ENUM_HEADER = "Recipe Enum"
STYLE_REFERENCE_HEADER = "Skill Level"

MAIN_NS = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
REL_ID = "{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id"
PACKAGE_REL = "{http://schemas.openxmlformats.org/package/2006/relationships}Relationship"

_ROW = re.compile(r"<row\b[^>]*?(?:/>|>.*?</row>)", re.S)
_CELL = re.compile(r"<c\b(?P<attrs>[^>]*?)(?:\s*/>|>(?P<body>.*?)</c>)", re.S)
_INLINE_TEXT = re.compile(r"<t\b[^>]*>(.*?)</t>", re.S)
_VALUE = re.compile(r"<v>(.*?)</v>", re.S)
_RANGE = re.compile(r"^(\$?)([A-Z]+)(\$?)(\d+):(\$?)([A-Z]+)(\$?)(\d+)$")


class BibleParameterError(Exception):
    pass


def column_index(letters: str) -> int:
    index = 0
    for char in letters:
        index = index * 26 + ord(char) - 64
    return index


def column_letters(index: int) -> str:
    letters = ""
    while index > 0:
        index, remainder = divmod(index - 1, 26)
        letters = chr(65 + remainder) + letters
    return letters


def normalize(text: str) -> str:
    return re.sub(r"\s+", " ", text or "").strip()


def attribute(attrs: str, name: str):
    match = re.search(r'(?<![\w:])' + re.escape(name) + r'="([^"]*)"', attrs)
    return match.group(1) if match else None


class Cell:
    def __init__(self, match: re.Match, shared: list[str]):
        self.start, self.end = match.span()
        self.xml = match.group(0)
        attrs = match.group("attrs")
        self.ref = attribute(attrs, "r") or ""
        self.column = column_index(re.sub(r"\d", "", self.ref))
        self.style = attribute(attrs, "s")
        kind = attribute(attrs, "t")
        body = match.group("body") or ""
        if kind == "s":
            value = _VALUE.search(body)
            self.text = shared[int(value.group(1))] if value else ""
        elif kind == "inlineStr":
            self.text = "".join(unescape(part, {"&quot;": '"', "&apos;": "'"}) for part in _INLINE_TEXT.findall(body))
        else:
            value = _VALUE.search(body)
            self.text = unescape(value.group(1), {"&quot;": '"', "&apos;": "'"}) if value else ""

    @property
    def is_blank(self) -> bool:
        return self.text == ""


def read_entries(source: Path | bytes):
    handle = source if isinstance(source, Path) else io.BytesIO(source)
    with zipfile.ZipFile(handle) as archive:
        infos = archive.infolist()
        return infos, {info.filename: archive.read(info.filename) for info in infos}


def shared_strings(entries: dict[str, bytes]) -> list[str]:
    if "xl/sharedStrings.xml" not in entries:
        return []
    root = ET.fromstring(entries["xl/sharedStrings.xml"])
    return ["".join(item.itertext()) for item in root]


def sheet_paths(entries: dict[str, bytes]) -> dict[str, str]:
    workbook = ET.fromstring(entries["xl/workbook.xml"])
    relationships = {item.get("Id"): item.get("Target") for item in ET.fromstring(entries["xl/_rels/workbook.xml.rels"]).iter(PACKAGE_REL)}
    paths = {}
    for sheet in workbook.findall("s:sheets/s:sheet", MAIN_NS):
        target = relationships[sheet.get(REL_ID)]
        paths[sheet.get("name")] = target.lstrip("/") if target.startswith("/") else "xl/" + target
    return paths


def format_number(value) -> str:
    if isinstance(value, float) and value == int(value):
        return str(int(value))
    return repr(value) if isinstance(value, float) else str(value)


def cell_xml(ref: str, style, value) -> str:
    style_attr = f' s="{style}"' if style is not None else ""
    if value is None or value == "":
        return f'<c r="{ref}"{style_attr} />'
    if isinstance(value, (int, float)) and not isinstance(value, bool):
        return f'<c r="{ref}"{style_attr}><v>{format_number(value)}</v></c>'
    text = str(value)
    preserve = ' xml:space="preserve"' if text != text.strip() else ""
    return f'<c r="{ref}"{style_attr} t="inlineStr"><is><t{preserve}>{escape(text)}</t></is></c>'


def widen_range(reference: str, last_column: int):
    """Returns the reference with its end column raised to last_column, or None when it already reaches it."""
    match = _RANGE.match(reference)
    if not match or column_index(match.group(6)) >= last_column:
        return None
    return f"{match.group(1)}{match.group(2)}{match.group(3)}{match.group(4)}:{match.group(5)}{column_letters(last_column)}{match.group(7)}{match.group(8)}"


def column_width(header: str) -> float:
    return round(max(14.0, min(30.0, len(header) * 0.95 + 2)), 2)


def update_sheet(xml: str, tab: str, profiles: list[dict], recipes: dict, shared: list[str]):
    """Returns (new xml, {header: column}, new column indexes, rows changed, recipes written)."""
    sheet_data = re.search(r"<sheetData>(.*?)</sheetData>", xml, re.S)
    if not sheet_data:
        raise BibleParameterError(f"{tab} has no sheetData")
    base = sheet_data.start(1)
    row_matches = list(_ROW.finditer(sheet_data.group(1)))
    rows = {}
    for match in row_matches:
        number = attribute(match.group(0)[: match.group(0).index(">") + 1], "r")
        if number:
            rows[int(number)] = match

    def cells_of(match: re.Match) -> list[Cell]:
        text = match.group(0)
        if text.rstrip().endswith("/>") and "</row>" not in text:
            return []
        inner_start = text.index(">") + 1
        inner = text[inner_start: text.rindex("</row>")]
        return [Cell(cell, shared) for cell in _CELL.finditer(inner)]

    if 1 not in rows:
        raise BibleParameterError(f"{tab} has no header row")
    header_cells = cells_of(rows[1])
    headers = {normalize(cell.text): cell.column for cell in header_cells if not cell.is_blank}
    if ENUM_HEADER not in headers:
        raise BibleParameterError(f"{tab} has no '{ENUM_HEADER}' header")
    enum_column = headers[ENUM_HEADER]
    style_column = headers.get(STYLE_REFERENCE_HEADER)

    row_cells = {number: cells_of(match) for number, match in rows.items()}
    enums = {}
    for number, cells in row_cells.items():
        if number == 1:
            continue
        found = next((cell.text.strip() for cell in cells if cell.column == enum_column), "")
        if found:
            enums[number] = found

    on_tab = {name for name in enums.values() if name in recipes}
    wanted = []
    for profile in profiles:
        profile_headers = profile["headers"]
        for header in profile_headers:
            if not header.startswith(profile["key"] + " "):
                raise BibleParameterError(f"header '{header}' must start with its profile key '{profile['key']}'")
        if any(header in recipes[name] for name in on_tab for header in profile_headers):
            wanted.extend(header for header in profile_headers if header not in wanted)
    if not wanted:
        return xml, {}, [], 0, 0

    last_header = max(headers.values())
    assignment, new_columns = {}, []
    for header in wanted:
        if normalize(header) in headers:
            assignment[header] = headers[normalize(header)]
        else:
            last_header += 1
            assignment[header] = last_header
            new_columns.append(last_header)
            for number, cells in row_cells.items():
                if any(cell.column == last_header and not cell.is_blank for cell in cells):
                    raise BibleParameterError(f"{tab} column {column_letters(last_header)} holds data without a header")

    header_style = next((cell.style for cell in reversed(header_cells) if not cell.is_blank), None)
    edits = []  # (start, end, text) in sheet coordinates
    rows_changed = 0
    recipes_written = 0

    def row_edits(number: int, desired: dict[int, tuple], keep_style: bool) -> list[tuple]:
        """desired: column -> (value, style). Returns splice edits (start, end, text) relative to the row text."""
        text = rows[number].group(0)
        cells = row_cells[number]
        if not cells and text.rstrip().endswith("/>") and "</row>" not in text:
            parts = [
                cell_xml(f"{column_letters(column)}{number}", style, value)
                for column, (value, style) in sorted(desired.items())
                if value not in (None, "")
            ]
            if not parts:
                return []
            opening = text.rstrip()[:-2].rstrip() + ">"
            return [(0, len(text), opening + "".join(parts) + "</row>")]

        by_column = {cell.column: cell for cell in cells}
        inner_start = text.index(">") + 1
        local = []
        inserts = {}
        for column, (value, style) in sorted(desired.items()):
            existing = by_column.get(column)
            ref = f"{column_letters(column)}{number}"
            if existing is not None:
                if existing.is_blank and value in (None, ""):
                    continue
                use_style = existing.style if keep_style and existing.style is not None else style
                wanted_xml = cell_xml(ref, use_style, value)
                if existing.xml != wanted_xml:
                    local.append((inner_start + existing.start, inner_start + existing.end, wanted_xml))
            elif value not in (None, ""):
                following = next((cell for cell in cells if cell.column > column), None)
                position = inner_start + following.start if following else text.rindex("</row>")
                inserts.setdefault(position, []).append(cell_xml(ref, style, value))
        local.extend((position, position, "".join(parts)) for position, parts in inserts.items())
        return local

    def apply_row(number: int, local: list[tuple]):
        nonlocal rows_changed
        if not local:
            return
        match = rows[number]
        text = match.group(0)
        for start, end, part in sorted(local, reverse=True):
            text = text[:start] + part + text[end:]
        edits.append((base + match.start(), base + match.end(), text))
        rows_changed += 1

    # Header row: only columns whose header is not already there.
    header_desired = {}
    for header, column in assignment.items():
        existing = next((cell for cell in header_cells if cell.column == column), None)
        if existing is None or normalize(existing.text) != normalize(header):
            header_desired[column] = (header, header_style)
    apply_row(1, row_edits(1, header_desired, False))

    style_of_row = {}
    for number, cells in row_cells.items():
        if number == 1 or number not in enums:
            continue
        reference = next((cell for cell in cells if cell.column == style_column), None) if style_column else None
        populated = [cell for cell in cells if not cell.is_blank]
        style_of_row[number] = (reference.style if reference is not None else (populated[-1].style if populated else None))

    for number, name in sorted(enums.items()):
        data = recipes.get(name)
        desired = {}
        for header, column in assignment.items():
            value = data.get(header) if data else None
            desired[column] = (value, style_of_row.get(number))
        if data and any(header in data for header in assignment):
            recipes_written += 1
        apply_row(number, row_edits(number, desired, True))

    if not edits and not new_columns:
        return xml, assignment, [], 0, recipes_written

    result = xml
    for start, end, part in sorted(edits, reverse=True):
        result = result[:start] + part + result[end:]

    last_column = max(assignment.values())
    widths = {assignment[header]: column_width(header) for header in assignment}

    cols = re.search(r"<cols>(.*?)</cols>", result, re.S)
    if new_columns:
        covered = set()
        if cols:
            for item in re.finditer(r"<col\b[^>]*>", cols.group(1)):
                low, high = int(attribute(item.group(0), "min")), int(attribute(item.group(0), "max"))
                covered.update(range(low, high + 1))
        additions = "".join(f'<col min="{n}" max="{n}" width="{widths[n]}" customWidth="1" />' for n in new_columns if n not in covered)
        if additions and cols:
            result = result[: cols.end(1)] + additions + result[cols.end(1):]
        elif additions:
            result = result.replace("<sheetData>", f"<cols>{additions}</cols><sheetData>", 1)

    for tag in ("autoFilter", "dimension"):
        found = re.search(rf'<{tag}\b[^>]*?\bref="([^"]+)"', result)
        if found:
            widened = widen_range(found.group(1), last_column)
            if widened:
                result = result[: found.start(1)] + widened + result[found.end(1):]

    return result, assignment, new_columns, rows_changed, recipes_written


def update_workbook_entries(entries: dict[str, bytes], data: dict, layout: dict | None):
    """Returns (changed entries, changed layout or None, report lines)."""
    shared = shared_strings(entries)
    paths = sheet_paths(entries)
    profiles, recipes = data["profiles"], data["recipes"]
    changed, report = {}, []
    filter_ranges = {}
    layout_after = json.loads(json.dumps(layout)) if layout is not None else None

    for tab in RECIPE_TABS:
        if tab not in paths:
            continue
        path = paths[tab]
        xml = entries[path].decode("utf-8")
        result, assignment, new_columns, rows_changed, written = update_sheet(xml, tab, profiles, recipes, shared)
        if not assignment:
            continue
        report.append(f"{tab}: {len(assignment)} parameter columns ({len(new_columns)} new), {written} recipe rows, {rows_changed} rows edited")
        if result != xml:
            changed[path] = result.encode("utf-8")
        filter_ranges[tab] = max(assignment.values())
        if layout_after is not None and tab in layout_after.get("columnsBySheet", {}):
            entries_for_tab = layout_after["columnsBySheet"][tab]
            for header, column in assignment.items():
                if not any(item["min"] <= column <= item["max"] for item in entries_for_tab):
                    entries_for_tab.append({"min": column, "max": column, "width": column_width(header)})
            entries_for_tab.sort(key=lambda item: item["min"])

    if filter_ranges:
        workbook = entries["xl/workbook.xml"].decode("utf-8")
        updated = workbook
        for tab, last_column in filter_ranges.items():
            pattern = re.compile(r"(<definedName\b[^>]*name=\"_xlnm\._FilterDatabase\"[^>]*>'" + re.escape(escape(tab)) + r"'!)([^<]+)(</definedName>)")
            updated = pattern.sub(lambda m: m.group(1) + (widen_range(m.group(2), last_column) or m.group(2)) + m.group(3), updated)
        if updated != workbook:
            changed["xl/workbook.xml"] = updated.encode("utf-8")

    return changed, (layout_after if layout_after != layout else None), report


def write_archive(path: Path, infos, entries: dict[str, bytes], changed: dict[str, bytes]):
    handle, temporary = tempfile.mkstemp(suffix=".xlsx", dir=path.parent)
    os.close(handle)
    try:
        with zipfile.ZipFile(temporary, "w") as target:
            for info in infos:
                copy = zipfile.ZipInfo(info.filename, info.date_time)
                copy.compress_type = info.compress_type
                copy.external_attr = info.external_attr
                copy.create_system = info.create_system
                target.writestr(copy, changed.get(info.filename, entries[info.filename]))
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary):
            os.remove(temporary)


def render_layout(layout: dict) -> str:
    return json.dumps(layout, separators=(",", ":"), ensure_ascii=False)


def update_files(workbook: Path, data: dict, layout_path: Path | None, check: bool = False) -> tuple[bool, list[str]]:
    infos, entries = read_entries(workbook)
    layout = None
    if layout_path is not None and layout_path.exists():
        raw = layout_path.read_text(encoding="utf-8")
        layout = json.loads(raw)
        if render_layout(layout) != raw.rstrip("\r\n"):
            raise BibleParameterError(f"{layout_path} does not round-trip through the JSON writer; refusing to rewrite it")
    changed, layout_after, report = update_workbook_entries(entries, data, layout)
    has_changes = bool(changed) or layout_after is not None
    if has_changes and not check:
        if changed:
            write_archive(workbook, infos, entries, changed)
        if layout_after is not None:
            layout_path.write_text(render_layout(layout_after), encoding="utf-8", newline="")
    return has_changes, report


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--data", required=True, type=Path, help="JSON written by the SWLOR.RecipeParameters export command")
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    parser.add_argument("--layout", type=Path, default=DEFAULT_LAYOUT, help="column width layout the Bible formatter applies; pass an empty path to skip")
    parser.add_argument("--check", action="store_true", help="change nothing; exit 1 when the workbook or layout would change")
    args = parser.parse_args(argv)

    data = json.loads(args.data.read_text(encoding="utf-8"))
    layout_path = args.layout if str(args.layout) not in ("", ".") else None
    try:
        has_changes, report = update_files(args.workbook, data, layout_path, args.check)
    except BibleParameterError as error:
        print(f"error: {error}", file=sys.stderr)
        return 2
    for line in report:
        print(line)
    print(("Would change" if args.check else "Changed") + f" the workbook: {'yes' if has_changes else 'no'}")
    return 1 if args.check and has_changes else 0


if __name__ == "__main__":
    sys.exit(main())
