"""Record the Engineering Circuit's XP multipliers and enhancement rule in the Design Bible's System Changes & Migrations tab.

Reads the values from SWLOR.Game.Server/Service/CraftService/Minigame/Circuit/CircuitRules.cs, so the Bible cannot drift from
the code, and edits only the target cells of design/bible/SWLOR Design Bible.xlsx at the zip/XML level, copying every other
workbook entry byte-for-byte. Safe to rerun: the entry is found by its opening words and rewritten in place; a new one goes on
the first row after the last numbered entry.

  python tools/UpdateCircuitBible.py [--workbook PATH] [--rules PATH]
"""
from __future__ import annotations

import argparse
import hashlib
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
DEFAULT_RULES = ROOT / "SWLOR.Game.Server" / "Service" / "CraftService" / "Minigame" / "Circuit" / "CircuitRules.cs"
SHEET = "System Changes & Migrations"
FIRST_ENTRY_ROW = 3
ENTRY_OPENING = "Engineering crafts use the Circuit minigame."

XP_BANDS = (
    ("XpMultiplierLevels1To10", "1-10"),
    ("XpMultiplierLevels11To20", "11-20"),
    ("XpMultiplierLevels21To30", "21-30"),
    ("XpMultiplierLevels31To40", "31-40"),
    ("XpMultiplierLevels41AndUp", "41 and up"),
)


def read_constant(source: str, name: str) -> float:
    match = re.search(rf"public const double {name} = ([0-9.]+);", source)
    if not match:
        raise ValueError(f"CircuitRules.cs has no 'public const double {name}'")
    return float(match.group(1))


def format_number(value: float) -> str:
    return f"{value:.2f}".rstrip("0").rstrip(".") if value != int(value) else str(int(value))


def entry_text(source: str) -> str:
    multipliers = [(label, read_constant(source, name)) for name, label in XP_BANDS]
    paid = ", ".join(f"{format_number(value)} at recipe levels {label}" for label, value in multipliers if value > 1.0)
    unchanged = [label for label, value in multipliers if value == 1.0]
    tail = f" and unchanged at {unchanged[0]}" if len(unchanged) == 1 else (
        f" and unchanged at {', '.join(unchanged[:-1])} and {unchanged[-1]}" if unchanged else "")
    cap = read_constant(source, "MaxEnhancementAssemblyShare")
    return (
        f"{ENTRY_OPENING} Every craft's base XP, for a finished item and a failure alike, is multiplied by {paid}{tail}, so XP"
        " per hour is at least the old crafting window's in every recipe band, gear tier and enhancement count. Enhancements"
        " add their progress penalty to the Assembly an item needs, at most"
        f" {format_number(cap * 100)}% of the Assembly the recipe itself needs.")


def entry_row(sheet: Bible.Sheet) -> tuple[int, int]:
    """The row to write and the entry number: the existing Circuit entry's, or the first row after the last numbered entry."""
    last_numbered, last_number = FIRST_ENTRY_ROW - 1, 0
    for number in range(FIRST_ENTRY_ROW, sheet.last_data_row() + 1):
        cells = sheet.row_cells(number)
        if "A" not in cells or not sheet.text_of(cells["A"]).strip():
            continue
        if "B" in cells and sheet.text_of(cells["B"]).startswith(ENTRY_OPENING):
            return number, int(float(sheet.text_of(cells["A"])))
        last_numbered, last_number = number, int(float(sheet.text_of(cells["A"])))
    return last_numbered + 1, last_number + 1


def update_sheet(sheet: Bible.Sheet, text: str) -> None:
    row, number = entry_row(sheet)
    template = sheet.row_cells(FIRST_ENTRY_ROW)
    cells = {
        "A": Bible.num_cell(f"A{row}", Bible.cell_style(template["A"]), f"{number}.0"),
        "B": Bible.text_cell(f"B{row}", Bible.cell_style(template["B"]), text),
    }
    sheet.write_row(row, cells, after=row - 1)


def update_workbook(path: Path, text: str) -> None:
    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}
        paths = Bible.resolve_sheets(source)
        strings = Bible.read_strings(source)

    entry = paths[SHEET]
    sheet = Bible.Sheet(SHEET, original[entry].decode("utf-8"), strings)
    update_sheet(sheet, text)
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
                                   ", ".join(sorted(k for k in before if before[k] != after.get(k))))
        os.replace(temp_name, path)
    finally:
        if os.path.exists(temp_name):
            os.remove(temp_name)
    print(f"Updated {SHEET} in {path.name}; every other entry is byte-identical.")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    parser.add_argument("--rules", type=Path, default=DEFAULT_RULES)
    args = parser.parse_args()
    update_workbook(args.workbook, entry_text(args.rules.read_text(encoding="utf-8")))


if __name__ == "__main__":
    main()
