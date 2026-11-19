"""Record the astromech loot and reward forms in the Design Bible.

Adds one equipment row per form in GenerateAstromechRewards.TWINS (looted forms plus the hand-listed quest-reward and
guild-store forms): "Equipment - Armor" for armor-slot modules and "Equipment - Weapons" for Shock Prod and Blaster Mount
tools. Each row copies the humanoid row the form copies (stats, level, slot, credits and the sources columns, since the form
drops wherever the piece drops) and changes the name, resref and required skill (Chassis, or Armaments with the tool's base
item). Source resrefs with no row on their tab are skipped and listed at the end.

Edits only the two target tabs at the zip/XML level and copies every other workbook entry byte-for-byte. Safe to rerun:
rows are found by resref and rewritten in place, new ones are appended.

  python tools/UpdateAstromechLootBible.py [--workbook PATH]

Run tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible afterwards.
"""
from __future__ import annotations

import argparse
import hashlib
import os
import sys
import tempfile
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import GenerateAstromechRewards as rewards  # noqa: E402
import UpdateAstromechModuleBible as modbible  # noqa: E402
import UpdateAstromechToolBible as toolbible  # noqa: E402
import UpdateCraftingProgressionBible as bible  # noqa: E402

DEFAULT_WORKBOOK = modbible.DEFAULT_WORKBOOK
ARMOR_TAB = modbible.EQUIPMENT_TAB
WEAPON_TAB = toolbible.EQUIPMENT_TAB


def tab_for(twin) -> str:
    return ARMOR_TAB if twin.weapon is None else WEAPON_TAB


def form_cells(source: dict, number: int, twin, style: str) -> dict:
    """The cells of a form's row: the source row's cells moved to `number`, with the form's own identity columns."""
    columns = modbible.EQUIPMENT_COLUMNS if twin.weapon is None else toolbible.EQUIPMENT_COLUMNS
    cells = {c: modbible.reref(source[c], c, number) for c in columns if c in source}
    cells["A"] = bible.text_cell(f"A{number}", style, twin.name)
    cells["B"] = bible.text_cell(f"B{number}", style, twin.resref)
    if twin.weapon is None:
        cells["D"] = bible.text_cell(f"D{number}", style, "Chassis")
    else:
        cells["C"] = bible.text_cell(f"C{number}", style, "Armaments")
        cells["D"] = bible.text_cell(f"D{number}", style, toolbible.BASE_ITEM_LABELS[twin.weapon[2]])
    return cells


def update_tab(sheet, twins: list, missing: list) -> None:
    next_row = sheet.last_data_row() + 1
    rows = modbible.column_index(sheet, "B")
    for twin in twins:
        source_number = rows.get(twin.source)
        if source_number is None:
            missing.append(twin.source)
            continue
        source = sheet.row_cells(source_number)
        existing = rows.get(twin.resref)
        number = existing or next_row
        cells = form_cells(source, number, twin, bible.cell_style(source["A"], "6"))
        sheet.write_row(number, cells, after=None if existing else number - 1)
        if not existing:
            next_row += 1


def update_workbook(path: Path) -> list:
    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}
        paths = bible.resolve_sheets(source)
        strings = bible.read_strings(source)

    changed, missing = {}, []
    for name in (ARMOR_TAB, WEAPON_TAB):
        entry = paths[name]
        sheet = bible.Sheet(name, original[entry].decode("utf-8"), strings)
        update_tab(sheet, [t for t in rewards.TWINS if tab_for(t) == name], missing)
        changed[entry] = sheet.xml.encode("utf-8")

    if all(original[entry] == data for entry, data in changed.items()):
        print("Astromech loot rows are already in the workbook.")
        return missing

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
            before = {n: hashlib.sha256(d).hexdigest() for n, d in original.items() if n not in changed}
            after = bible.entry_hashes(written, set(changed))
            if before != after:
                raise RuntimeError("Untouched workbook entries changed: " +
                                   ", ".join(sorted(k for k in before if before[k] != after.get(k))))
        os.replace(temp_name, path)
    finally:
        if os.path.exists(temp_name):
            os.remove(temp_name)
    print(f"Updated {len(changed)} worksheets in {path.name}; every other entry is byte-identical.")
    return missing


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    missing = update_workbook(parser.parse_args().workbook)
    if missing:
        print(f"{len(missing)} source resrefs have no row and were skipped: " + ", ".join(sorted(set(missing))))


if __name__ == "__main__":
    main()
