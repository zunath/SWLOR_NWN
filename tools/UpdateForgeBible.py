"""Record the Plasma Forge's Smithery XP multiplier in the Design Bible.

The multiplier is `ForgeRules.XpMultiplier` in code. This writes it as an entry on the "System Changes & Migrations" tab: the
row whose text starts with the marker below is rewritten, or a new numbered row is added after the last one. Only that sheet's
XML changes and every other workbook entry is copied byte for byte. Safe to rerun, and after a workbook merge conflict take the
base workbook and run it again.

  python tools/UpdateForgeBible.py [--workbook PATH] [--check]
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

from UpdateCraftingProgressionBible import (  # noqa: E402
    DEFAULT_WORKBOOK,
    Sheet,
    cell_style,
    entry_hashes,
    num_cell,
    read_strings,
    resolve_sheets,
    text_cell,
)

ROOT = Path(__file__).resolve().parents[1]
RULES = ROOT / "SWLOR.Game.Server" / "Service" / "CraftService" / "Minigame" / "Forge" / "ForgeRules.cs"
SHEET = "System Changes & Migrations"
MARKER = "Smithery recipes are crafted in the Plasma Forge"
TEMPLATE_ROW = 13


def read_multiplier() -> str:
    match = re.search(r"XpMultiplier\s*=\s*(\d+(?:\.\d+)?)\s*;", RULES.read_text(encoding="utf-8"))
    if not match:
        raise SystemExit(f"XpMultiplier was not found in {RULES}.")
    return match.group(1)


def entry_text(multiplier: str) -> str:
    return (
        f"{MARKER}, and the base XP of every Smithery craft is multiplied by {multiplier}, a failed craft's included. The Forge's"
        " crafts take 10 to 20 actions where the old window's early crafts took about 6, so without the multiplier Smithery"
        " would pay less XP per hour at low ranks than today. The factor is ForgeRules.XpMultiplier and applies to Smithery only.")


def update_sheet(sheet: Sheet, multiplier: str) -> None:
    text = entry_text(multiplier)
    template = sheet.row_cells(TEMPLATE_ROW)
    number_style, text_style = cell_style(template["A"]), cell_style(template["B"])

    existing = None
    last_number = 0
    last_text_row = TEMPLATE_ROW
    for row in range(TEMPLATE_ROW, sheet.last_data_row() + 1):
        cells = sheet.row_cells(row)
        body = sheet.text_of(cells["B"]) if "B" in cells else ""
        if not body.strip():
            continue

        last_text_row = row
        if body.startswith(MARKER):
            existing = row
        if "A" in cells:
            try:
                last_number = max(last_number, int(float(sheet.text_of(cells["A"]))))
            except ValueError:
                pass

    if existing is not None:
        number = int(float(sheet.text_of(sheet.row_cells(existing)["A"])))
        row = existing
    else:
        number = last_number + 1
        row = last_text_row + 1

    cells = {
        "A": num_cell(f"A{row}", number_style, f"{number}.0"),
        "B": text_cell(f"B{row}", text_style, text),
    }
    sheet.write_row(row, cells, after=last_text_row)


def update_workbook(path: Path, check: bool) -> bool:
    multiplier = read_multiplier()
    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}
        paths = resolve_sheets(source)
        strings = read_strings(source)

    entry = paths[SHEET]
    sheet = Sheet(SHEET, original[entry].decode("utf-8"), strings)
    update_sheet(sheet, multiplier)
    updated = sheet.xml.encode("utf-8")
    if updated == original[entry]:
        print("The workbook already records the Smithery XP multiplier.")
        return False
    if check:
        print("The workbook would change.")
        return True

    handle, temp_name = tempfile.mkstemp(suffix=".xlsx", dir=str(path.parent))
    os.close(handle)
    try:
        with zipfile.ZipFile(temp_name, "w", zipfile.ZIP_DEFLATED) as target:
            for info in infos:
                clone = zipfile.ZipInfo(info.filename, info.date_time)
                clone.compress_type = zipfile.ZIP_DEFLATED
                clone.external_attr = info.external_attr
                target.writestr(clone, updated if info.filename == entry else original[info.filename])
        with zipfile.ZipFile(temp_name) as written:
            assert written.testzip() is None
            before = {name: hashlib.sha256(data).hexdigest() for name, data in original.items() if name != entry}
            after = entry_hashes(written, {entry})
            if before != after:
                raise RuntimeError("Untouched workbook entries changed.")
        os.replace(temp_name, path)
    finally:
        if os.path.exists(temp_name):
            os.remove(temp_name)

    print(f"Recorded the Smithery XP multiplier {multiplier} on the {SHEET} tab.")
    return True


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    parser.add_argument("--check", action="store_true", help="change nothing; exit 1 when the workbook would change")
    args = parser.parse_args()
    changed = update_workbook(args.workbook, args.check)
    sys.exit(1 if args.check and changed else 0)


if __name__ == "__main__":
    main()
