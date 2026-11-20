"""Record the Astromech Armaments tools in the Design Bible.

Adds one "Engineering Recipes" row and one "Equipment - Weapons" row per tool (20 each), generated from the same data as
tools/GenerateAstromechTools.py: the recipe rows come from the generated AstromechToolRecipes.cs, the equipment rows copy the
stats, level, delay and credits of the humanoid staff or rifle row each tool copies and change the name, resref, weapon type
(Armaments), base item and sources.

Edits only the two target tabs at the zip/XML level and copies every other workbook entry byte-for-byte. Safe to rerun:
rows are found by recipe enum and by resref and rewritten in place, new ones are appended.

  python tools/UpdateAstromechToolBible.py [--workbook PATH]

Run tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible afterwards.
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
import GenerateAstromechTools as tools  # noqa: E402
import UpdateAstromechModuleBible as modbible  # noqa: E402
import UpdateCraftingProgressionBible as bible  # noqa: E402

ROOT = tools.ROOT
DEFAULT_WORKBOOK = modbible.DEFAULT_WORKBOOK
RECIPE_TAB = "Engineering Recipes"
EQUIPMENT_TAB = "Equipment - Weapons"
EQUIPMENT_COLUMNS = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P"]
BASE_ITEM_LABELS = {545: "shockprod", 546: "blastermount"}


def code_recipes() -> dict:
    """The generated recipe definitions, by recipe enum name."""
    text = tools.RECIPES_CS.read_text(encoding="utf-8")
    found = {}
    for m in re.finditer(r"_builder\.Create\(RecipeType\.(\w+), SkillType\.Engineering\)(.*?);", text, re.S):
        body = m.group(2)
        circuit = re.search(r"\.CircuitParameters\(([^)]*)\)", body)
        if not circuit:
            raise SystemExit(f"{m.group(1)} has no circuit parameters; run tools/SWLOR.RecipeParameters write Circuit first")
        enhancement = re.search(r"\.EnhancementSlots\(RecipeEnhancementType\.(\w+), (\d+)\)", body)
        found[m.group(1)] = {
            "level": int(re.search(r"\.Level\((\d+)\)", body).group(1)),
            "quantity": int(re.search(r"\.Quantity\((\d+)\)", body).group(1)),
            "enhancement": enhancement.groups() if enhancement else ("None", "0"),
            "components": re.findall(r'\.Component\("([^"]+)", (\d+)\)', body),
            "circuit": modbible.circuit_values(circuit.group(1)),
        }
    return found


def update_recipes(sheet) -> None:
    recipes = code_recipes()
    last = sheet.last_data_row()
    style = bible.cell_style(sheet.row_cells(last)["A"], "14")
    next_row = last + 1
    rows = modbible.column_index(sheet, "D")
    for tool in tools.TOOLS:
        existing = rows.get(tool.enum)
        number = existing or next_row
        cells = modbible.recipe_row(sheet, number, tool, recipes[tool.enum], style)
        cells["E"] = bible.text_cell(f"E{number}", style, "AstromechTool")
        sheet.write_row(number, cells, after=None if existing else number - 1)
        if not existing:
            next_row += 1
    new_last = sheet.last_data_row()
    sheet.xml = re.sub(r'(<autoFilter ref="\$A\$1:\$[A-Z]+\$)\d+(")', lambda m: f"{m.group(1)}{new_last}{m.group(2)}", sheet.xml, count=1)


def update_equipment(sheet) -> None:
    last = sheet.last_data_row()
    next_row = last + 1
    rows = modbible.column_index(sheet, "B")
    for tool in tools.TOOLS:
        source_number = rows.get(tool.source)
        if source_number is None:
            raise SystemExit(f"Equipment - Weapons has no row for {tool.source}")
        source = sheet.row_cells(source_number)
        existing = rows.get(tool.resref)
        number = existing or next_row
        style = bible.cell_style(source["A"], "6")
        sold = tool.tier == 0
        cells = {}
        for column in EQUIPMENT_COLUMNS:
            if column in source:
                cells[column] = modbible.reref(source[column], column, number)
        cells["A"] = bible.text_cell(f"A{number}", style, tool.name)
        cells["B"] = bible.text_cell(f"B{number}", style, tool.resref)
        cells["C"] = bible.text_cell(f"C{number}", style, "Armaments")
        cells["D"] = bible.text_cell(f"D{number}", style, BASE_ITEM_LABELS[tool.base_item])
        cells["O"] = bible.text_cell(f"O{number}", style, "Crafting; Store" if sold else "Crafting")
        cells["P"] = bible.text_cell(
            f"P{number}", style, "Crafting: Astromech Tool" + (" | Store: CZ-220 Merchant" if sold else ""))
        sheet.write_row(number, cells, after=None if existing else number - 1)
        if not existing:
            next_row += 1


def update_workbook(path: Path) -> None:
    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}
        paths = bible.resolve_sheets(source)
        strings = bible.read_strings(source)

    changed = {}
    for name, edit in ((RECIPE_TAB, update_recipes), (EQUIPMENT_TAB, update_equipment)):
        entry = paths[name]
        sheet = bible.Sheet(name, original[entry].decode("utf-8"), strings)
        edit(sheet)
        changed[entry] = sheet.xml.encode("utf-8")

    if all(original[entry] == data for entry, data in changed.items()):
        print("Astromech tool rows are already in the workbook.")
        return

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


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    update_workbook(parser.parse_args().workbook)


if __name__ == "__main__":
    main()
