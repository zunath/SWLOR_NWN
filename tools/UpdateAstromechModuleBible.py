"""Record the Astromech gear modules in the Design Bible.

Adds one "Engineering Recipes" row and one "Equipment - Armor" row per module (160 each), generated from the same data as
tools/GenerateAstromechModules.py: the recipe rows come from the generated AstromechModuleRecipes.cs, the equipment rows
copy the stats, level, slot and credits of the humanoid row each module copies and change the name, resref, required skill
(Chassis) and sources.

Edits only the two target tabs at the zip/XML level and copies every other workbook entry byte-for-byte. Safe to rerun:
rows are found by recipe enum and by resref and rewritten in place, new ones are appended.

  python tools/UpdateAstromechModuleBible.py [--workbook PATH]

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
import GenerateAstromechModules as modules  # noqa: E402
import UpdateCraftingProgressionBible as bible  # noqa: E402

ROOT = modules.ROOT
DEFAULT_WORKBOOK = ROOT / "design" / "bible" / "SWLOR Design Bible.xlsx"
RECIPE_TAB = "Engineering Recipes"
EQUIPMENT_TAB = "Equipment - Armor"
EQUIPMENT_COLUMNS = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W"]
COMPONENT_COLUMNS = ["M", "O", "Q", "S", "U", "W", "Y", "AA"]  # each is followed by its quantity column


def circuit_values(parameters: str) -> list[int]:
    return [int(v) for v in re.findall(r":\s*(\d+)", parameters)]


def code_recipes() -> dict:
    """The generated recipe definitions, by recipe enum name."""
    text = modules.RECIPES_CS.read_text(encoding="utf-8")
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
            "circuit": circuit_values(circuit.group(1)),
        }
    return found


def reref(cell: str, column: str, row: int) -> str:
    return re.sub(r'(<c r=")[A-Z]+\d+(")', lambda m: f"{m.group(1)}{column}{row}{m.group(2)}", cell, count=1)


def recipe_row(sheet, number: int, module, recipe: dict, style: str) -> dict:
    def text(col, value):
        return bible.text_cell(f"{col}{number}", style, value)

    def num(col, value):
        return f'<c r="{col}{number}" s="{style}" t="n"><v>{value}</v></c>'

    kind, slots = recipe["enhancement"]
    cells = {
        "A": text("A", "Engineering"),
        "D": text("D", module.enum),
        "E": text("E", "AstromechModule"),
        "G": text("G", module.name),
        "H": num("H", recipe["level"]),
        "I": num("I", recipe["quantity"]),
        "J": text("J", module.resref),
        "K": text("K", kind if int(slots) else "N/A"),
        "L": num("L", slots),
        "AC": num("AC", 0),
    }
    for index, (resref, quantity) in enumerate(recipe["components"]):
        cells[COMPONENT_COLUMNS[index]] = text(COMPONENT_COLUMNS[index], resref)
        quantity_col = bible_next_column(COMPONENT_COLUMNS[index])
        cells[quantity_col] = num(quantity_col, quantity)
    for col, value in zip(("AH", "AI", "AJ"), recipe["circuit"]):
        cells[col] = num(col, value)
    return cells


def bible_next_column(letters: str) -> str:
    index = bible.col_index(letters) + 1
    out = ""
    while index:
        index, rem = divmod(index - 1, 26)
        out = chr(65 + rem) + out
    return out


def column_index(sheet, column: str) -> dict:
    """Maps the text of one column to its row number, in a single pass over the sheet."""
    found = {}
    for match in re.finditer(r'<row r="(\d+)"[^>]*>(.*?)</row>', sheet.xml, re.S):
        if int(match.group(1)) < 2:
            continue
        cell = re.search(r'<c r="%s\d+"[^>]*?(?:/>|>.*?</c>)' % column, match.group(2), re.S)
        if cell:
            found.setdefault(sheet.text_of(cell.group(0)), int(match.group(1)))
    return found


def update_recipes(sheet) -> None:
    recipes = code_recipes()
    last = sheet.last_data_row()
    style = bible.cell_style(sheet.row_cells(last)["A"], "14")
    next_row = last + 1
    rows = column_index(sheet, "D")
    for module in modules.MODULES:
        existing = rows.get(module.enum)
        number = existing or next_row
        cells = recipe_row(sheet, number, module, recipes[module.enum], style)
        sheet.write_row(number, cells, after=None if existing else number - 1)
        if not existing:
            next_row += 1
    new_last = sheet.last_data_row()
    sheet.xml = re.sub(r'(<autoFilter ref="\$A\$1:\$[A-Z]+\$)\d+(")', lambda m: f"{m.group(1)}{new_last}{m.group(2)}", sheet.xml, count=1)


def update_equipment(sheet) -> None:
    last = sheet.last_data_row()
    next_row = last + 1
    rows = column_index(sheet, "B")
    for module in modules.MODULES:
        source_number = rows.get(module.source)
        if source_number is None:
            raise SystemExit(f"Equipment - Armor has no row for {module.source}")
        source = sheet.row_cells(source_number)
        existing = rows.get(module.resref)
        number = existing or next_row
        style = bible.cell_style(source["A"], "6")
        sold = module.tier == 0
        cells = {}
        for column in EQUIPMENT_COLUMNS:
            if column in source:
                cells[column] = reref(source[column], column, number)
        cells["A"] = bible.text_cell(f"A{number}", style, module.name)
        cells["B"] = bible.text_cell(f"B{number}", style, module.resref)
        cells["D"] = bible.text_cell(f"D{number}", style, "Chassis")
        cells["V"] = bible.text_cell(f"V{number}", style, "Crafting; Store" if sold else "Crafting")
        cells["W"] = bible.text_cell(
            f"W{number}", style, "Crafting: Astromech Module" + (" | Store: CZ-220 Merchant" if sold else ""))
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
        print("Astromech module rows are already in the workbook.")
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
