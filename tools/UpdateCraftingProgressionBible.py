"""Record the crafting rank milestones, the retired perks and the Treasure Hunter / Creditfinder move in the Design Bible.

Edits only the target cells of design/bible/SWLOR Design Bible.xlsx at the zip/XML level and copies every other
workbook entry byte-for-byte. Safe to rerun: each run writes the same cells.

  python tools/UpdateCraftingProgressionBible.py [--workbook PATH]

Changes:
  - Gathering, Fabrication and Engineering: the retired perk rows stay as history with Dev Status "Retired" (or "Moved"
    for Treasure Hunter and Creditfinder) and a note naming the rank milestone that replaced them.
  - Total SP on those tabs counts only the purchasable rows, and SP Price cells stored as text become numbers on the
    Engineering, Fabrication, Agriculture and Gathering tabs.
  - General: Treasure Hunter I-III and Creditfinder I-III with their Armor requirements.
  - System Changes & Migrations: the refund and rebuild token entry.
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

# Perk rows by tab. Each entry: perk name prefix -> (rank requirement per rank, SP per rank, note per rank).
ROMAN = ["I", "II", "III", "IV", "V", "VI"]


def tier_notes(template: str, ranks: list[int]) -> list[str]:
    return [template.format(n=i + 1, r=r) for i, r in enumerate(ranks)]


GATHERING_RETIRED = {
    "Harvesting": ([1, 1, 2, 3, 4], tier_notes("Tier {n} harvesters unlock for every character at Gathering {r}.", [0, 10, 20, 30, 40])),
    "Refining": ([1, 1, 2, 3, 4], tier_notes("Tier {n} ores can be refined by every character at Gathering {r}.", [0, 10, 20, 30, 40])),
    "Refinery Management": ([1, 1, 2, 2, 3, 4], [
        f"Power cores refine {n} additional item{'s' if n > 1 else ''} for every character from Gathering {r} ({3 + n} items per core)."
        for n, r in zip(range(1, 7), [0, 10, 20, 30, 40, 45])]),
    "Scavenging": ([1, 1, 2, 3, 4], tier_notes("Tier {n} scavenge sites unlock for every character at Gathering {r}.", [0, 10, 20, 30, 40])),
    "Hard Look": ([1, 1, 2, 3, 3], [
        f"Counts as Hard Look {n} for every character from Gathering {r}: a {10 * n}% base chance of a second search until the Search board replaces it."
        for n, r in zip(range(1, 6), [0, 10, 20, 30, 40])]),
}
GATHERING_MOVED = {
    "Treasure Hunter": ([3, 3, 4], ["Armor 20", "Armor 40", "Armor 50"]),
    "Creditfinder": ([3, 3, 4], ["Armor 15", "Armor 30", "Armor 45"]),
}
FABRICATION_RETIRED = {
    "Research": ([2, 2, 3, 3, 4], tier_notes("Tier {n} blueprints can be researched by every character at Fabrication {r}.", [10, 20, 30, 40, 50])),
    "Scientific Networking": ([3, 4], [
        "New blueprints gain one more licensed run for every character at Fabrication 25.",
        "New blueprints gain another licensed run for every character at Fabrication 50."]),
    "Research Projects": ([2, 3], [
        "Every character can run one more concurrent research job at Fabrication 25 (2 in total).",
        "Every character can run another concurrent research job at Fabrication 50 (3 in total)."]),
}
ENGINEERING_RETIRED = {
    "Droid Assembly": ([1, 1, 2, 3, 4], tier_notes("Tier {n} droids unlock for every character at Engineering {r}.", [0, 10, 20, 30, 40])),
}

GATHERING_LAST_PERK_ROW = 45
GATHERING_DESCRIPTION = (
    " Harvester, ore, scavenge-site and Hard Look tiers unlock for every character at Gathering 0/10/20/30/40, and power cores"
    " refine one more item at Gathering 0/10/20/30/40/45, so the Harvesting, Refining, Refinery Management, Scavenging and Hard Look"
    " perks are retired. Treasure Hunter and Creditfinder moved to the General tab. Total SP counts only purchasable perks.")
FABRICATION_DESCRIPTION = (
    " Research tiers unlock for every character at Fabrication 10/20/30/40/50, and Fabrication 25 and 50 each add one concurrent"
    " research job and one licensed run on new blueprints, so the Research, Research Projects and Scientific Networking perks"
    " are retired. Total SP counts only purchasable perks.")
ENGINEERING_DESCRIPTION = (
    " Droid tiers unlock for every character at Engineering 0/10/20/30/40, so the Droid Assembly perk is retired."
    " Total SP counts only purchasable perks.")

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

SYSTEM_CHANGE_NUMBER = 12
SYSTEM_CHANGE_ROW = 14
SYSTEM_CHANGE_TEXT = (
    "Harvesting, Refining, Refinery Management, Scavenging, Hard Look, Research, Research Projects, Scientific Networking and"
    " Droid Assembly are no longer perks. Their tiers unlock for every character by skill rank: harvesters, ores, scavenge sites"
    " and droid tiers at ranks 0/10/20/30/40 of Gathering (Engineering for droids), power cores refine one more item at Gathering"
    " 0/10/20/30/40/45, research tiers at Fabrication 10/20/30/40/50, and Fabrication 25 and 50 each add one research job and one"
    " licensed run on new blueprints. Treasure Hunter and Creditfinder moved to General and require Armor (Treasure Hunter"
    " 20/40/50, Creditfinder 15/30/45). All SP spent on these perks is refunded automatically, and every character receives"
    " one optional Rebuild Token.")


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


def perk_rows(sheet: Sheet, first_row: int = 9):
    """(row number, perk name, rank index) for each ranked perk row."""
    for number in range(first_row, sheet.last_data_row() + 1):
        name = sheet.text_of(sheet.row_cells(number).get("C", "")).strip()
        match = re.fullmatch(r"(.+?) (I|II|III|IV|V|VI)", name)
        if match:
            yield number, match.group(1), ROMAN.index(match.group(2))


def mark_rows(sheet: Sheet, spec: dict, status: str, status_col: str, notes_col: str, moved: bool = False) -> int:
    marked = 0
    for number, perk, rank in perk_rows(sheet):
        if perk not in spec:
            continue
        prices, notes = spec[perk]
        cells = sheet.row_cells(number)
        sheet.set_cell(number, status_col, text_cell(f"{status_col}{number}", cell_style(cells[status_col]), status))
        refunded = f"Refunded {prices[rank]} SP."
        if moved:
            note = f"Moved to the General tab with the requirement {notes[rank]}. {refunded} Owners buy it again."
        else:
            note = f"Retired. {notes[rank]} {refunded}"
        style = cell_style(cells.get(notes_col, cells[status_col]))
        sheet.set_cell(number, notes_col, text_cell(f"{notes_col}{number}", style, note))
        marked += 1
    expected = sum(len(prices) for prices, _ in spec.values())
    assert marked == expected, f"{sheet.name}: marked {marked} perk rows, expected {expected}"
    return marked


def set_description(sheet: Sheet, suffix: str) -> None:
    cell = sheet.row_cells(3)["B"]
    text = sheet.text_of(cell)
    marker = suffix.split(".")[0].strip()
    if marker in text:
        text = text[:text.index(marker)].rstrip()
    sheet.set_cell(3, "B", text_cell("B3", cell_style(cell), text + suffix))


def set_formula(sheet: Sheet, ref: str, formula: str, value) -> None:
    number = int(re.search(r"\d+", ref).group(0))
    col = re.match(r"[A-Z]+", ref).group(0)
    cell = sheet.row_cells(number)[col]
    sheet.set_cell(number, col, num_cell(ref, cell_style(cell), value, formula))


def update_gathering(sheet: Sheet) -> None:
    numeric_sp_cells(sheet, 9)
    cols = sheet.header_columns(8)
    status, notes = cols["Dev Status"], cols["Notes"]
    mark_rows(sheet, GATHERING_RETIRED, "Retired", status, notes)
    mark_rows(sheet, GATHERING_MOVED, "Moved", status, notes, moved=True)
    last = GATHERING_LAST_PERK_ROW
    set_formula(sheet, "D4", f'SUMIFS(B9:B{last},{status}9:{status}{last},"<>Retired",{status}9:{status}{last},"<>Moved")', 0)
    set_description(sheet, GATHERING_DESCRIPTION)


def update_fabrication_like(sheet: Sheet, spec: dict, description: str, total_row: int, total_cached: int = 0) -> None:
    numeric_sp_cells(sheet, 9)
    cols = sheet.header_columns(8)
    status, notes = cols["Dev Status"], cols["Notes"]
    mark_rows(sheet, spec, "Retired", status, notes)
    first, last = 9, total_row - 1
    set_formula(sheet, f"B{total_row}", f'SUMIFS(B{first}:B{last},{status}{first}:{status}{last},"<>Retired")', total_cached)
    set_formula(sheet, "D4", f"SUM(B{total_row})", total_cached)
    set_description(sheet, description)


def update_agriculture(sheet: Sheet) -> None:
    numeric_sp_cells(sheet, 9)
    total = 0
    for number in range(9, 14):
        total += int(sheet.text_of(sheet.row_cells(number)["B"]).split(".")[0])
    set_formula(sheet, "B14", "SUM(B9:B13)", total)
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


def update_system_changes(sheet: Sheet) -> None:
    template_number, template_text = sheet.row_cells(13)["A"], sheet.row_cells(13)["B"]
    cells = {
        "A": num_cell(f"A{SYSTEM_CHANGE_ROW}", cell_style(template_number), f"{SYSTEM_CHANGE_NUMBER}.0"),
        "B": text_cell(f"B{SYSTEM_CHANGE_ROW}", cell_style(template_text), SYSTEM_CHANGE_TEXT),
    }
    sheet.write_row(SYSTEM_CHANGE_ROW, cells, after=13)


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
        "System Changes & Migrations": update_system_changes,
        "General": update_general,
        "Gathering": update_gathering,
        "Fabrication": lambda s: update_fabrication_like(s, FABRICATION_RETIRED, FABRICATION_DESCRIPTION, 18),
        "Engineering": lambda s: update_fabrication_like(s, ENGINEERING_RETIRED, ENGINEERING_DESCRIPTION, 14),
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
