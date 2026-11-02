"""Write the Gathering perk lines (Prospector and Treasure Seeker) into the Design Bible's Gathering tab.

Reads the rows from the JSON that the game's own perk definitions export
(`SWLOR.CraftSim gathering-perks --part bible --out design/testing/crafting-revamp/gathering-perk-bible-rows.json`), so the Bible cannot
drift from the code, and edits only the target cells of design/bible/SWLOR Design Bible.xlsx at the zip/XML level, copying every other
workbook entry byte-for-byte. Safe to rerun: the perk rows go on the rows after the retired Hard Look rows and a rerun writes the same
cells. After a merge conflict on the workbook, take the base's workbook and run this again.

  python tools/UpdateGatheringPerkBible.py [--workbook PATH] [--data PATH]

GatheringPerkBibleTests compares the descriptions with the code.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
import tempfile
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import UpdateCraftingProgressionBible as Bible  # noqa: E402  (shared zip/XML helpers)

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_WORKBOOK = ROOT / "design" / "bible" / "SWLOR Design Bible.xlsx"
DEFAULT_DATA = ROOT / "design" / "testing" / "crafting-revamp" / "gathering-perk-bible-rows.json"
SHEET = "Gathering"
FIRST_PERK_ROW = 47
TEMPLATE_ROW = 16
HEADER_ROW = 8
DESCRIPTION_MARKER = "Two perk lines"

# What each perk's notes say besides the stats the row grants (the export lists the stats).
NOTES = {
    "Efficient Strikes": "Prospecting only. It shortens the action and never changes what it does or costs: the session already claims nearly every seam its Stability allows, so a perk that saved Stability would lift it past the +15% item cap. Ranks replace earlier ranks.",
    "Bulk Refining": "Refining. The refinery adds it to the batch of 3 plus the Gathering rank's Refinery Management level; refining converts ore one for one, so it saves power cores (credits) and adds no supply. 12 items per core at most. Ranks replace earlier ranks.",
    "Rich Veins": "Prospecting only. It shortens an Extract of a rich seam, not of a gem pocket. Ranks replace earlier ranks.",
    "Strip Mining": "Style: the status effect Strip Mining carries the stats. Not exclusive with Careful Hands as a purchase; only one style is active, picked in the Prospecting window before the first action. It does nothing in Search.",
    "Core Sample": "Prospecting only. The window shows the hidden seams' count and kinds; a Heavy Strike lists its shatter chance only while a gem pocket is hidden.",
    "Mother Lode": "Capstone, automatic. The first Strike or Heavy Strike of a node takes half the time; a node remembers it was spent, so it is once per node and not once per session. It changes no yield: a skilled session already sits at the cap, and any perk that cut the session's actions would take it under the 3 to 8 a node should last. No quest requirement.",
    "Fine Trowel": "Search only. A Dig Deeper find rolls the table with the rare entries weighted x3 plus this. Ranks replace earlier ranks.",
    "Sharp Chisel": "Prospecting only. Gem pockets are about 2% of tier 1 nodes, so it rarely matters. Ranks replace earlier ranks.",
    "Steady Dig": "Search only. Percent off the risk of ruining the spot, rounded as the board rounds it.",
    "Careful Hands": "Style: the status effect Careful Hands carries the stats. Not exclusive with Strip Mining as a purchase; only one style is active, picked in the Search window before the first action. It does nothing in Prospecting.",
    "Keen Eye": "Search only. Raises the chance of every Search, ordinary and promising ground alike.",
    "Deep Scan": "Prospecting only. The extra seam is worth nothing at Gathering 50, where the rank already reveals every seam a node holds; the pocket order still helps there.",
    "Rich Pickings": "Search only. Raises the weight of the rare entries in an ordinary find's roll, not how often a Search finds.",
    "Jackpot": "Capstone, automatic. Search only: the first Dig Deeper find of each board rolls the rare entries with this much more weight. No quest requirement.",
}


def notes_for(row: dict) -> str:
    base = re.sub(r" (I|II|III)$", "", row["Name"])
    stats = ", ".join(stat.split(" ")[0] for stat in row["Stats"])
    prefix = f"Stat{'s' if len(row['Stats']) > 1 else ''} {stats}. " if stats else ""
    return prefix + NOTES[base]


def description_suffix(total: int) -> str:
    return (
        f" {DESCRIPTION_MARKER} of {total // 2} SP each (Prospector and Treasure Seeker) change Prospecting, the Search board and refining: ten"
        " purchases at Gathering 5/10/15/20/25/30/35/40/45/50 priced 1/1/2/2/3/3/3/3/3/4 SP, a style at rank 20 and an automatic capstone at rank 50."
        " Both styles can be owned and the Prospecting and Search windows toggle between them, one at a time. Neither line changes Quick Harvest or Quick Search.")


def update_sheet(sheet: Bible.Sheet, rows: list[dict]) -> int:
    template = sheet.row_cells(TEMPLATE_ROW)
    columns = sheet.header_columns(HEADER_ROW)
    status, notes_col = columns["Dev Status"], columns["Notes"]
    total = sum(row["Price"] for row in rows)

    def cell(col: str, number: int, value=None, kind="text"):
        style = Bible.cell_style(template[col])
        ref = f"{col}{number}"
        if value is None:
            return Bible.blank_cell(ref, style)
        if kind == "number":
            return Bible.num_cell(ref, style, value)
        return Bible.text_cell(ref, style, str(value))

    for offset, row in enumerate(rows):
        number = FIRST_PERK_ROW + offset
        cells = {col: cell(col, number) for col in template}
        values = {
            "A": row["Line"], "C": row["Name"], "D": f"Gathering {row['Rank']}", "E": "All", "F": row["Type"], "G": row["Description"],
            "H": "None", "I": "None", "J": "None", "K": "-", "L": "-", "M": "-", "N": "-", status: "Implemented", notes_col: notes_for(row),
        }
        for col, value in values.items():
            cells[col] = cell(col, number, value)
        cells["B"] = cell("B", number, row["Price"], "number")
        sheet.write_row(number, cells)

    last = FIRST_PERK_ROW + len(rows) - 1
    Bible.set_formula(
        sheet, "D4", f'SUMIFS(B9:B{last},{status}9:{status}{last},"<>Retired",{status}9:{status}{last},"<>Moved")', total)
    description = sheet.row_cells(3)["B"]
    text = sheet.text_of(description)
    if DESCRIPTION_MARKER in text:
        text = text[:text.index(DESCRIPTION_MARKER)].rstrip()
    sheet.set_cell(3, "B", Bible.text_cell("B3", Bible.cell_style(description), text + description_suffix(total)))
    return total


def update_workbook(path: Path, rows: list[dict]) -> int:
    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}
        paths = Bible.resolve_sheets(source)
        strings = Bible.read_strings(source)

    entry = paths[SHEET]
    sheet = Bible.Sheet(SHEET, original[entry].decode("utf-8"), strings)
    total = update_sheet(sheet, rows)
    changed = {entry: sheet.xml.encode("utf-8")}

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
            after = Bible.entry_hashes(written, set(changed))
            if before != after:
                raise RuntimeError("Untouched workbook entries changed: " +
                                   ", ".join(sorted(key for key in before if before[key] != after.get(key))))
        os.replace(temp_name, path)
    finally:
        if os.path.exists(temp_name):
            os.remove(temp_name)
    return total


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    parser.add_argument("--data", type=Path, default=DEFAULT_DATA, help="the JSON `gathering-perks --part bible` exports")
    args = parser.parse_args()
    rows = json.loads(args.data.read_text(encoding="utf-8"))["rows"]
    total = update_workbook(args.workbook, rows)
    print(f"Wrote the Gathering perk lines ({total} SP) into {args.workbook.name}; every other entry is byte-identical.")


if __name__ == "__main__":
    main()
