"""Keep the crafting tabs of the Design Bible free of the perks that rank milestones replaced, and keep Treasure Hunter
and Creditfinder on the General tab.

Edits only the target worksheets of design/bible/SWLOR Design Bible.xlsx at the zip/XML level and copies every other
workbook entry byte-for-byte. Safe to rerun, also after the perk-line tools (UpdateEngineeringPerkBible.py,
UpdateFabricationPerkBible.py, UpdateGatheringPerkBible.py, UpdateAgriculturePerkBible.py) have added their lines: those
tools own the tab's Total SP and total rows, so this tool leaves them alone.

  python tools/UpdateCraftingProgressionBible.py [--workbook PATH]

Changes:
  - Gathering, Fabrication and Engineering: the rows of the retired perks (Harvesting, Refining, Refinery Management,
    Scavenging, Hard Look, Research, Scientific Networking, Research Projects, Droid Assembly) and of the perks that moved
    to General (Treasure Hunter, Creditfinder) are removed, with the blank and Total rows that only served them. The rows
    below move up and the tab's formulas follow them.
  - SP Price cells stored as text become numbers on the Engineering, Fabrication, Agriculture and Gathering tabs.
  - Agriculture: while the tab still has the old Farming rows (row 9 reads Farming), their total and Total SP are written
    too. Once UpdateAgriculturePerkBible.py has replaced them with the Farmer line and the crop table, it owns the tab and
    this tool leaves the rows, totals and Total SP alone. AgriculturePerkBibleTests runs both tools to check they agree.
  - General: Treasure Hunter I-III and Creditfinder I-III with their Armor requirements.
"""
from __future__ import annotations

import argparse
import hashlib
import os
import re
import tempfile
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET
from xml.sax.saxutils import escape, unescape

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_WORKBOOK = ROOT / "design" / "bible" / "SWLOR Design Bible.xlsx"
NS = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"

ROW_RE = r'<row r="{n}"(?P<attrs>(?: [^>]*?)?)(?:/>|>(?P<body>.*?)</row>)'
CELL_RE = re.compile(r'<c [^>]*?/>|<c [^>]*?>.*?</c>', re.S)

# Perks that no longer belong on these tabs: retired for rank milestones, or moved to the General tab.
REMOVED_PERKS = {
    "Gathering": {"Harvesting", "Refining", "Refinery Management", "Scavenging", "Hard Look", "Treasure Hunter", "Creditfinder"},
    "Fabrication": {"Research", "Scientific Networking", "Research Projects"},
    "Engineering": {"Droid Assembly"},
}
FIRST_DATA_ROW = 9
RANK_SUFFIX = re.compile(r" (I|II|III|IV|V|VI)$")
# Column A labels the perk-line tools write. Once a tab has them, that tool owns the tab's Total SP (D4).
LINE_LABELS = {
    "Engineering": {"Overclocker", "Technician"},
    "Fabrication": {"Builder", "Researcher"},
    "Gathering": {"Prospector", "Treasure Seeker"},
    "Agriculture": {"Chef", "Angler"},
}
TREASURE_HUNTER_DESCRIPTION = ("Rare items are more likely to be chosen when loot is rolled, and rare loot tables gain up to "
                               "+{n}0% drop chance.")
GENERAL_ROWS = [
    ("Treasure Hunter I", 3, "Armor 20", TREASURE_HUNTER_DESCRIPTION.format(n=1)),
    ("Treasure Hunter II", 3, "Armor 40", TREASURE_HUNTER_DESCRIPTION.format(n=2)),
    ("Treasure Hunter III", 4, "Armor 50", TREASURE_HUNTER_DESCRIPTION.format(n=3)),
    ("Creditfinder I", 3, "Armor 15", "Increases the amount of credits found by 20%."),
    ("Creditfinder II", 3, "Armor 30", "Increases the amount of credits found by 40%."),
    ("Creditfinder III", 4, "Armor 45", "Increases the amount of credits found by 60%."),
]
GENERAL_FIRST_ROW = 19
SHARED_STRINGS = {672: "General", 692: "All", 706: "Trait", 623: "None", 695: "-", 698: "Implemented"}
GENERAL_NOTE = "Moved from the Gathering tab. Requires Armor instead of Gathering; previous owners were refunded and buy it again."


def col_index(letters: str) -> int:
    value = 0
    for ch in letters:
        value = value * 26 + ord(ch) - 64
    return value


def find_row(xml: str, number: int):
    return re.search(ROW_RE.format(n=number), xml, re.S)


def split_cells(body: str | None) -> list[str]:
    return CELL_RE.findall(body or "")


def cell_column(cell: str) -> str:
    return re.match(r'<c r="([A-Z]+)\d+"', cell).group(1)


def cell_style(cell: str, default: str = "6") -> str:
    match = re.search(r' s="(\d+)"', cell.split(">", 1)[0])
    return match.group(1) if match else default


def num_cell(ref: str, style: str, value, formula: str | None = None) -> str:
    formula_xml = f"<f>{escape(formula)}</f>" if formula else ""
    return f'<c r="{ref}" s="{style}">{formula_xml}<v>{value}</v></c>'


def text_cell(ref: str, style: str, text: str) -> str:
    assert text.isascii(), f"Bible text must be ASCII: {text!r}"
    return f'<c r="{ref}" s="{style}" t="inlineStr"><is><t xml:space="preserve">{escape(text)}</t></is></c>'


def shared_cell(ref: str, style: str, index: int) -> str:
    return f'<c r="{ref}" s="{style}" t="s"><v>{index}</v></c>'


def blank_cell(ref: str, style: str) -> str:
    return f'<c r="{ref}" s="{style}" />'


class Sheet:
    def __init__(self, name: str, xml: str, strings: list[str]):
        self.name = name
        self.xml = xml
        self.strings = strings

    def text_of(self, cell: str) -> str:
        kind = re.search(r' t="(\w+)"', cell.split(">", 1)[0])
        kind = kind.group(1) if kind else None
        if kind == "s":
            return self.strings[int(re.search(r"<v>(\d+)</v>", cell).group(1))]
        if kind == "inlineStr":
            return unescape("".join(re.findall(r"<t[^>]*>(.*?)</t>", cell, re.S)))
        match = re.search(r"<v>(.*?)</v>", cell, re.S)
        return unescape(match.group(1)) if match else ""

    def row_cells(self, number: int) -> dict[str, str]:
        match = find_row(self.xml, number)
        return {cell_column(c): c for c in split_cells(match.group("body"))} if match else {}

    def write_row(self, number: int, cells: dict[str, str], after: int | None = None) -> None:
        ordered = "".join(cells[col] for col in sorted(cells, key=col_index))
        match = find_row(self.xml, number)
        if match:
            attrs = match.group("attrs") or ""
            new_row = f'<row r="{number}"{attrs}>{ordered}</row>'
            self.xml = self.xml[:match.start()] + new_row + self.xml[match.end():]
            return
        new_row = f'<row r="{number}">{ordered}</row>'
        anchor = after if after is not None else number - 1
        previous = None
        while anchor >= 1 and previous is None:
            previous = find_row(self.xml, anchor)
            anchor -= 1
        if previous is None:
            raise ValueError(f"No row before {number} to insert after in {self.name}")
        self.xml = self.xml[:previous.end()] + new_row + self.xml[previous.end():]

    def set_cell(self, number: int, col: str, cell: str) -> None:
        cells = self.row_cells(number)
        cells[col] = cell
        self.write_row(number, cells)

    def header_columns(self, number: int) -> dict[str, str]:
        return {" ".join(self.text_of(c).split()): col for col, c in self.row_cells(number).items()}

    def last_data_row(self) -> int:
        return max(int(n) for n in re.findall(r'<row r="(\d+)"', self.xml))


def numeric_sp_cells(sheet: Sheet, first_row: int, price_col: str = "B") -> None:
    """SP Price cells stored as text become numbers so the totals count them."""
    for number in range(first_row, sheet.last_data_row() + 1):
        cells = sheet.row_cells(number)
        cell = cells.get(price_col)
        if cell is None or ' t="s"' not in cell.split(">", 1)[0]:
            continue
        value = sheet.text_of(cell).strip()
        if re.fullmatch(r"\d+", value):
            sheet.set_cell(number, price_col, num_cell(f"{price_col}{number}", cell_style(cell), value))


def shift_refs(formula: str, removed: list[int]) -> str:
    """Moves the row numbers of a same-sheet formula or range up past the removed rows. A range keeps its surviving rows."""
    def above(row: int, inclusive: bool) -> int:
        return sum(1 for r in removed if r < row or (inclusive and r == row))

    def one(match: re.Match) -> str:
        col1, row1, col2, row2 = match.group(1), int(match.group(2)), match.group(3), match.group(4)
        if col2 is None:
            if row1 in removed:
                raise ValueError(f"{formula!r} points at removed row {row1}")
            return f"{col1}{row1 - above(row1, False)}"
        return f"{col1}{row1 - above(row1, False)}:{col2}{int(row2) - above(int(row2), True)}"

    return re.sub(r"(?<![A-Za-z!$])\$?([A-Z]{1,3})\$?(\d+)(?::\$?([A-Z]{1,3})\$?(\d+))?(?![\d(])", one, formula)


def remove_rows(sheet: Sheet, removed: list[int]) -> None:
    """Deletes the rows and moves every row below them up, with their cell references, formulas and merged ranges."""
    removed = sorted(set(removed))
    if not removed:
        return

    def new_number(row: int) -> int:
        return row - sum(1 for r in removed if r < row)

    for number in removed:
        match = find_row(sheet.xml, number)
        if match:
            sheet.xml = sheet.xml[:match.start()] + sheet.xml[match.end():]

    head, data, tail = re.split(r"(<sheetData>.*</sheetData>)", sheet.xml, maxsplit=1, flags=re.S)
    data = re.sub(r'<row r="(\d+)"', lambda m: f'<row r="{new_number(int(m.group(1)))}"', data)
    data = re.sub(r'<c r="([A-Z]+)(\d+)"', lambda m: f'<c r="{m.group(1)}{new_number(int(m.group(2)))}"', data)
    data = re.sub(r"(<f(?: [^>]*)?>)(.*?)(</f>)",
                  lambda m: m.group(1) + escape(shift_refs(unescape(m.group(2)), removed)) + m.group(3), data, flags=re.S)
    tail = re.sub(r'(<mergeCell ref=")([^"]+)(")', lambda m: m.group(1) + shift_refs(m.group(2), removed) + m.group(3), tail)
    sheet.xml = head + data + tail


def removable_rows(sheet: Sheet) -> list[int]:
    """The rows of the removed perks, plus the Total and blank rows that would be left with nothing to serve."""
    perks = REMOVED_PERKS[sheet.name]
    last = sheet.last_data_row()
    names = {n: sheet.text_of(sheet.row_cells(n).get("C", "")).strip() for n in range(FIRST_DATA_ROW, last + 1)}
    labels = {n: sheet.text_of(sheet.row_cells(n).get("A", "")).strip() for n in range(FIRST_DATA_ROW, last + 1)}
    removed = {n for n, name in names.items() if RANK_SUFFIX.sub("", name) in perks}
    if not removed:
        return []

    # A Total row whose block (the rows above it, up to the previous blank row) was removed entirely.
    for n in range(FIRST_DATA_ROW, last + 1):
        if labels[n] != "Total":
            continue
        block, above = [], n - 1
        while above >= FIRST_DATA_ROW and (names[above] or labels[above]):
            block.append(above)
            above -= 1
        if block and all(r in removed for r in block):
            removed.add(n)

    # Blank rows left at the top of the table or doubled up once the perks are gone.
    previous_blank = True
    for n in range(FIRST_DATA_ROW, last + 1):
        if n in removed:
            continue
        blank = not names[n] and not labels[n]
        if blank and previous_blank:
            removed.add(n)
        previous_blank = blank
    return sorted(removed)


def has_lines(sheet: Sheet) -> bool:
    """True once a perk-line tool has written its lines to this tab."""
    labels = LINE_LABELS.get(sheet.name, set())
    return any(sheet.text_of(sheet.row_cells(number).get("A", "")).strip() in labels
               for number in range(9, sheet.last_data_row() + 1))


def set_formula(sheet: Sheet, ref: str, formula: str, value) -> None:
    number = int(re.search(r"\d+", ref).group(0))
    col = re.match(r"[A-Z]+", ref).group(0)
    cell = sheet.row_cells(number)[col]
    sheet.set_cell(number, col, num_cell(ref, cell_style(cell), value, formula))


def update_removed_perks(sheet: Sheet) -> None:
    """Removes the retired and moved perk rows. The perk-line tools then rewrite the tab's totals."""
    numeric_sp_cells(sheet, FIRST_DATA_ROW)
    remove_rows(sheet, removable_rows(sheet))


def update_agriculture(sheet: Sheet) -> None:
    numeric_sp_cells(sheet, 9)

    # UpdateAgriculturePerkBible.py replaces the Farming rows with the Farmer line, moves the Chef and Angler lines down, and
    # writes every total and Total SP. Rows 9-14 are then Farmer purchases, so the Farming totals below would overwrite them.
    if sheet.text_of(sheet.row_cells(9).get("A", "")).strip() != "Farming":
        return

    total = 0
    for number in range(9, 14):
        total += int(sheet.text_of(sheet.row_cells(number)["B"]).split(".")[0])
    set_formula(sheet, "B14", "SUM(B9:B13)", total)
    if not has_lines(sheet):  # the Chef and Angler totals join Total SP once their lines exist
        set_formula(sheet, "D4", "SUM(B14)", total)


def update_general(sheet: Sheet) -> None:
    for index, expected in SHARED_STRINGS.items():
        assert sheet.strings[index] == expected, f"Shared string {index} is {sheet.strings[index]!r}, expected {expected!r}"
    template = sheet.row_cells(18)
    styles = {col: cell_style(cell) for col, cell in template.items()}
    first, last = GENERAL_FIRST_ROW, GENERAL_FIRST_ROW + len(GENERAL_ROWS) - 1
    for offset, (name, price, requirement, description) in enumerate(GENERAL_ROWS):
        n = first + offset
        cells = {
            "A": shared_cell(f"A{n}", styles["A"], 672),
            "B": num_cell(f"B{n}", styles["B"], price),
            "C": text_cell(f"C{n}", styles["C"], name),
            "D": text_cell(f"D{n}", styles["D"], requirement),
            "E": shared_cell(f"E{n}", styles["E"], 692),
            "F": shared_cell(f"F{n}", styles["F"], 706),
            "G": text_cell(f"G{n}", styles["G"], description),
            "H": shared_cell(f"H{n}", styles["H"], 623),
            "I": shared_cell(f"I{n}", styles["I"], 623),
            "J": shared_cell(f"J{n}", styles["J"], 623),
            "K": shared_cell(f"K{n}", styles["K"], 695),
            "L": shared_cell(f"L{n}", styles["L"], 695),
            "M": shared_cell(f"M{n}", styles["M"], 695),
            "N": shared_cell(f"N{n}", styles["N"], 695),
            "O": shared_cell(f"O{n}", styles["O"], 698),
            "P": blank_cell(f"P{n}", styles["P"]),
            "Q": text_cell(f"Q{n}", styles["Q"], GENERAL_NOTE),
        }
        existing = sheet.row_cells(n)
        existing.update(cells)
        sheet.write_row(n, existing)
    total = sum(int(sheet.text_of(sheet.row_cells(n)["B"]).split(".")[0]) for n in range(8, last + 1))
    set_formula(sheet, "D4", f"SUM(B8:B{last})", total)


def resolve_sheets(archive: zipfile.ZipFile) -> dict[str, str]:
    workbook = ET.fromstring(archive.read("xl/workbook.xml"))
    relationships = {r.get("Id"): r.get("Target") for r in ET.fromstring(archive.read("xl/_rels/workbook.xml.rels"))}
    paths = {}
    for sheet in workbook.findall("s:sheets/s:sheet", NS):
        target = relationships[sheet.get(f"{{{REL}}}id")]
        paths[sheet.get("name")] = target.lstrip("/") if target.startswith("/") else "xl/" + target
    return paths


def read_strings(archive: zipfile.ZipFile) -> list[str]:
    root = ET.fromstring(archive.read("xl/sharedStrings.xml"))
    return ["".join(t.text or "" for t in si.iter(f"{{{NS['s']}}}t")) for si in root.findall("s:si", NS)]


def entry_hashes(archive: zipfile.ZipFile, skip: set[str]) -> dict[str, str]:
    return {i.filename: hashlib.sha256(archive.read(i.filename)).hexdigest()
            for i in archive.infolist() if i.filename not in skip}


def update_workbook(path: Path) -> None:
    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}
        paths = resolve_sheets(source)
        strings = read_strings(source)

    edits = {
        "General": update_general,
        "Gathering": update_removed_perks,
        "Fabrication": update_removed_perks,
        "Engineering": update_removed_perks,
        "Agriculture": update_agriculture,
    }
    changed = {}
    for name, edit in edits.items():
        entry = paths[name]
        sheet = Sheet(name, original[entry].decode("utf-8"), strings)
        edit(sheet)
        changed[entry] = sheet.xml.encode("utf-8")

    handle, temp_name = tempfile.mkstemp(suffix=".xlsx", dir=str(path.parent))
    os.close(handle)
    try:
        with zipfile.ZipFile(temp_name, "w", zipfile.ZIP_DEFLATED) as target:
            for info in infos:
                clone = zipfile.ZipInfo(info.filename, info.date_time)
                clone.compress_type = zipfile.ZIP_DEFLATED
                clone.external_attr = info.external_attr
                target.writestr(clone, changed.get(info.filename, original[info.filename]))
        with zipfile.ZipFile(temp_name) as written:
            assert written.testzip() is None
            before = {name: hashlib.sha256(data).hexdigest() for name, data in original.items() if name not in changed}
            after = entry_hashes(written, set(changed))
            if before != after:
                raise RuntimeError("Untouched workbook entries changed: " +
                                   ", ".join(sorted(k for k in before if before[k] != after.get(k))))
        os.replace(temp_name, path)
    finally:
        if os.path.exists(temp_name):
            os.remove(temp_name)
    print(f"Updated {len(changed)} worksheets in {path.name}; every other entry is byte-identical.")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    update_workbook(parser.parse_args().workbook)


if __name__ == "__main__":
    main()
