"""Record Agriculture's Farmer, Chef and Angler perk lines and the crop table in the Design Bible's Agriculture tab.

Reads the three lines from the perk definitions (AgricultureFarmerPerkDefinition.cs, AgricultureChefPerkDefinition.cs and
AgricultureAnglerPerkDefinition.cs) and the crops from CropListDefinition.cs, so the Bible's names, prices, requirements,
descriptions, stats and crop data cannot drift from the code, and edits only the target cells of design/bible/SWLOR Design
Bible.xlsx at the zip/XML level, copying every other workbook entry byte-for-byte. Safe to rerun: the lines and the crop table are
rewritten in place from row 9, and the totals and their cached values are rewritten. After a merge conflict on the workbook, take
the base's workbook and run this again.

  python tools/UpdateAgriculturePerkBible.py [--workbook PATH]

Each line is ten purchases at Agriculture 5/10/15/20/25/30/35/40/45/50 for 1/1/2/2/3/3/3/3/3/4 SP (25 SP). The Farmer line
(Crop Management, Bountiful Harvest and Seed Saver) lists those ten rows. The Chef and Angler lines have a choice of two styles at
rank 20, so each lists eleven rows and its Total subtracts the alternative style. The tab's Total SP is the sum of the three lines'
totals. The crop table follows the lines: one row per crop with its tier, crop level, grow time, seed and harvest item.
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
PERK_DEFINITIONS = ROOT / "SWLOR.Game.Server" / "Feature" / "PerkDefinition"
CROP_DEFINITIONS = ROOT / "SWLOR.Game.Server" / "Feature" / "FarmingDefinition" / "CropListDefinition.cs"
MODULE_ITEMS = ROOT / "Module" / "uti"
SHEET = "Agriculture"

TEMPLATE_PERK_ROW = 9
FIRST_LINE_ROW = 9

# (line name, perk definition file, the engine that reads it, whether it has the rank-20 choice of two styles)
LINES = (
    ("Farmer", "AgricultureFarmerPerkDefinition.cs", "farming service", False),
    ("Chef", "AgricultureChefPerkDefinition.cs", "Kitchen", True),
    ("Angler", "AgricultureAnglerPerkDefinition.cs", "Line", True),
)

RANKS = [5, 10, 15, 20, 25, 30, 35, 40, 45, 50]
RANKS_WITH_STYLES = [5, 10, 15, 20, 20, 25, 30, 35, 40, 45, 50]
PRICES = [1, 1, 2, 2, 3, 3, 3, 3, 3, 4]
ROMAN = ["I", "II", "III", "IV", "V", "VI"]

# Perks that add an action to the window (a window action, not a combat ability), and the CP each rank of them costs.
ACTION_PERKS = {"KitchenReduce", "SignatureDish"}
ACTION_CP = {"KitchenReduce": ["12", "9"]}
# A perk the player switches on or off for each dish.
TOGGLE_PERKS = {"SeasonedPalate"}

# The description's last sentences, from the first marker found on, are replaced on every run.
DESCRIPTION_MARKERS = ("The Chef (Kitchen) and Angler", "The Farmer (crops), Chef (Kitchen) and Angler")
DESCRIPTION_SUFFIX = (
    " The Farmer (crops), Chef (Kitchen) and Angler (Line) lines are three optional lines of 25 SP each at Agriculture"
    " 5/10/15/20/25/30/35/40/45/50. Chef and Angler have a choice of two exclusive styles at rank 20 and a capstone at rank 50;"
    " Farmer has neither. The crop table below the lines lists the 26 crops of the farming system.")

# The Farmer line's notes, by perk: what the farming service reads from it.
FARMER_NOTES = {
    "CropManagement": "Read by the farming service: the perk level is the highest crop tier the planter can plant (crop tier N unlocks at crop level 5/15/25/35/45) and the highest livestock tier they can raise.",
    "BountifulHarvest": "Read by the farming service when the planter harvests a crop or butchers an animal: adds this many items to the yield.",
    "SeedSaver": "Read by the farming service when the planter harvests a crop: the chance the harvest also returns its seed.",
}

CROP_RE = re.compile(
    r'_builder\.Create\(CropType\.(\w+)\)\.Name\("([^"]+)"\)\.Tier\((\d)\)\.Seed\("(\w+)"\)\.Harvest\("(\w+)"\)')
TIER_LEVELS = [5, 15, 25, 35, 45]
TIER_GROW_HOURS = [12, 16, 20, 24, 24]


def parse_line(path: Path) -> list[dict]:
    """Every purchase of a perk definition in rank order: the perk, its level, price, rank, description and stats."""
    source = path.read_text(encoding="utf-8")
    purchases = []
    for block in source.split("_builder.Create(")[1:]:
        perk_type = re.search(r"PerkType\.(\w+)\)", block).group(1)
        name = re.search(r'\.Name\("([^"]+)"\)', block).group(1)
        levels = block.split(".AddPerkLevel()")[1:]
        for index, level in enumerate(levels, start=1):
            excluded = re.findall(r"\.RequirementCannotHavePerk\(PerkType\.(\w+)\)", level)
            purchases.append({
                "perk": perk_type,
                "name": name if len(levels) == 1 else f"{name} {ROMAN[index - 1]}",
                "level": index,
                "levels": len(levels),
                "price": int(re.search(r"\.Price\((\d+)\)", level).group(1)),
                "rank": int(re.search(r"\.RequirementSkill\(SkillType\.Agriculture, (\d+)\)", level).group(1)),
                "description": re.search(r'\.Description\("((?:[^"\\]|\\.)*)"\)', level).group(1),
                "stats": [(stat, int(value)) for stat, value in re.findall(r"\.IncreasesStat\(StatType\.(\w+), (-?\d+)\)", level)],
                "excludes": excluded[0] if excluded else None,
                "order": len(purchases),
            })
    return sorted(purchases, key=lambda purchase: (purchase["rank"], purchase["order"]))


def bible_type(purchase: dict, line: str) -> str:
    """
    Style for the two rank-20 choices, Capstone at rank 50, Active for an action a perk adds to a craft window, Trait otherwise. A
    per-dish switch (Seasoned Palate) is a Trait too: the Toggle type is a combat toggle that needs an animation plan row. Every
    Farmer purchase is a Trait: the line has no style choice and no capstone.
    """
    if line == "Farmer":
        return "Trait"
    if purchase["excludes"]:
        return "Style"
    if purchase["rank"] == 50:
        return "Capstone"
    if purchase["perk"] in ACTION_PERKS:
        return "Active"
    return "Trait"


def note(purchase: dict, engine: str, names: dict[str, str], line: str) -> str:
    if line == "Farmer":
        parts = [FARMER_NOTES[purchase["perk"]]]
        if purchase["level"] > 1:
            parts.append("Only this rank's value applies, so it restates the earlier rank.")
        return " ".join(parts)

    stats = ", ".join(f"{stat} {value:+d}" for stat, value in purchase["stats"])
    parts = [f"Read by the {engine}. Stats: {stats}."]
    if purchase["perk"] in ACTION_PERKS and purchase["level"] == 1:
        parts.append("Adds an action to the window (a window action, not a combat ability).")
    if purchase["perk"] in TOGGLE_PERKS and purchase["level"] == 1:
        parts.append("A switch in the Kitchen's setup view, chosen for each dish.")
    if purchase["level"] > 1:
        parts.append("Only this rank's stats apply, so it restates the earlier rank's.")
    if purchase["excludes"]:
        parts.append(f"Style, always active. Exclusive with {names[purchase['excludes']]} through the perk requirements; "
                     "switching costs a Perk Refund Token.")
    if purchase["rank"] == 50:
        parts.append("Capstone: no quest requirement.")
    return " ".join(parts)


def cp_cost(purchase: dict) -> str:
    costs = ACTION_CP.get(purchase["perk"])
    return costs[purchase["level"] - 1] if costs else "-"


def perk_cells(row: int, purchase: dict, line: str, engine: str, names: dict[str, str], styles: dict[str, str]) -> dict[str, str]:
    values = {
        "A": line,
        "C": purchase["name"],
        "D": f"Agriculture {purchase['rank']}",
        "E": "All",
        "F": bible_type(purchase, line),
        "G": purchase["description"],
        "H": "None",
        "I": "None",
        "J": "None",
        "K": cp_cost(purchase),
        "L": "-",
        "M": "Implemented",
        "O": note(purchase, engine, names, line),
    }
    # The template row has no Notes cell, so the notes take the description's style.
    cells = {col: Bible.text_cell(f"{col}{row}", styles.get(col, styles["G"]), text) for col, text in values.items()}
    cells["B"] = Bible.num_cell(f"B{row}", styles["B"], purchase["price"])
    cells["N"] = Bible.blank_cell(f"N{row}", styles["N"])
    return cells


def find_total_row(sheet: Bible.Sheet) -> int:
    """The first row whose Style cell reads Total: the style template for every line's total row."""
    for number in range(TEMPLATE_PERK_ROW, sheet.last_data_row() + 1):
        cell = sheet.row_cells(number).get("A")
        if cell is not None and sheet.text_of(cell).strip() == "Total":
            return number
    raise ValueError("The Agriculture tab has no Total row to take the totals' style from.")


def item_name(resref: str) -> str:
    """The name of a module item. Module JSON is Windows-1252, and the Bible's text is ASCII."""
    document = json.loads((MODULE_ITEMS / f"{resref}.uti.json").read_text(encoding="latin-1"))
    name = document["LocalizedName"]["value"]["0"].strip()
    assert name.isascii(), f"Item {resref} has a non-ASCII name: {name!r}"
    return name


def parse_crops(path: Path) -> list[dict]:
    """The crops of CropListDefinition.cs in source order, each with the name of the item it yields."""
    source = path.read_text(encoding="utf-8")
    crops = []
    for type_name, name, tier, seed, harvest in CROP_RE.findall(source):
        tier = int(tier)
        crops.append({
            "type": type_name,
            "name": name,
            "tier": tier,
            "level": TIER_LEVELS[tier - 1],
            "hours": TIER_GROW_HOURS[tier - 1],
            "seed": seed,
            "harvest": harvest,
            "harvest_name": item_name(harvest),
        })
    return crops


def blank_row(sheet: Bible.Sheet, number: int, columns: str = "ABCDEFGKLMNO") -> None:
    """A row with nothing in it, as the tab keeps between its sections. The row's old content is replaced."""
    sheet.write_row(number, {col: Bible.blank_cell(f"{col}{number}", "6") for col in columns}, after=number - 1)


def crop_rows(sheet: Bible.Sheet, first_row: int, crops: list[dict], styles: dict[str, str], header_styles: dict[str, str]) -> dict:
    """Writes the crop table from first_row: a title row, a header row and one row per crop. Returns where it went."""
    title = first_row
    sheet.write_row(title, {
        "A": Bible.text_cell(f"A{title}", styles["TOTAL"], "Crops"),
        "C": Bible.text_cell(
            f"C{title}", styles["TOTAL"],
            "The 26 crops of the farming system, in five tiers. A tier's crops share a crop level and a grow time and need Crop "
            "Management of that tier to plant. Each yields an item harvesting already gives. Seeds are items named seed_ plus the crop."),
    }, after=title - 1)

    header = title + 1
    headings = {"A": "Table", "B": "Tier", "C": "Crop", "D": "Crop Level", "E": "Grow (h)", "F": "", "G": "Seed and Harvest Item"}
    sheet.write_row(header, {
        col: Bible.text_cell(f"{col}{header}", header_styles[col], text) for col, text in headings.items()
    }, after=header - 1)

    row = header + 1
    for crop in crops:
        sheet.write_row(row, {
            "A": Bible.text_cell(f"A{row}", styles["A"], "Crop"),
            "B": Bible.num_cell(f"B{row}", styles["B"], crop["tier"]),
            "C": Bible.text_cell(f"C{row}", styles["C"], crop["name"]),
            "D": Bible.num_cell(f"D{row}", styles["D"], crop["level"]),
            "E": Bible.num_cell(f"E{row}", styles["E"], crop["hours"]),
            "F": Bible.blank_cell(f"F{row}", styles["F"]),
            "G": Bible.text_cell(f"G{row}", styles["G"], f"Seed: {crop['seed']}. Harvest: {crop['harvest_name']} ({crop['harvest']})."),
        }, after=row - 1)
        row += 1

    return {"title": title, "header": header, "first": header + 1, "last": row - 1}


def update_sheet(sheet: Bible.Sheet, lines: list[tuple[str, str, list[dict]]], crops: list[dict]) -> dict:
    styles = {col: Bible.cell_style(cell) for col, cell in sheet.row_cells(TEMPLATE_PERK_ROW).items()}
    total_styles = {col: Bible.cell_style(cell) for col, cell in sheet.row_cells(find_total_row(sheet)).items()}
    header_styles = {col: Bible.cell_style(cell) for col, cell in sheet.row_cells(8).items()}
    names = {purchase["perk"]: purchase["name"] for _line, _engine, purchases in lines for purchase in purchases}
    has_styles_by_line = {line: has_styles for line, _file, _engine, has_styles in LINES}

    row = FIRST_LINE_ROW
    total_rows, totals, summary = [], [], {}
    for line, engine, purchases in lines:
        first = row
        styles_seen = []
        for purchase in purchases:
            sheet.write_row(row, perk_cells(row, purchase, line, engine, names, styles), after=row - 1)
            if purchase["excludes"]:
                styles_seen.append((purchase, row))
            row += 1

        last = row - 1
        if has_styles_by_line[line]:
            (_, _), (alternative, alternative_row) = styles_seen
            total = sum(purchase["price"] for purchase in purchases) - alternative["price"]
            formula = f"SUM(B{first}:B{last})-B{alternative_row}"
        else:
            total = sum(purchase["price"] for purchase in purchases)
            formula = f"SUM(B{first}:B{last})"

        sheet.write_row(row, {
            "A": Bible.text_cell(f"A{row}", total_styles["A"], "Total"),
            "B": Bible.num_cell(f"B{row}", total_styles["B"], total, formula),
            "C": Bible.blank_cell(f"C{row}", total_styles.get("C", total_styles["B"])),
        }, after=row - 1)
        total_rows.append(row)
        totals.append(total)
        summary[line] = {"first": first, "last": last, "total_row": row, "sp": total}
        blank_row(sheet, row + 1)  # one blank row between the sections
        row += 2

    grand = sum(totals)
    refs = ",".join(f"B{number}" for number in total_rows)
    Bible.set_formula(sheet, "D4", f"SUM({refs})", grand)

    summary["crops"] = crop_rows(sheet, row, crops, {**styles, "TOTAL": total_styles["A"]}, header_styles)

    cell = sheet.row_cells(3)["B"]
    text = sheet.text_of(cell)
    cuts = [text.index(marker) for marker in DESCRIPTION_MARKERS if marker in text]
    if cuts:
        text = text[:min(cuts)].rstrip()
    sheet.set_cell(3, "B", Bible.text_cell("B3", Bible.cell_style(cell), text + DESCRIPTION_SUFFIX))
    summary["total_sp"] = grand
    return summary


def check(line: str, purchases: list[dict], has_styles: bool) -> None:
    styles = [p for p in purchases if p["excludes"]]
    if not has_styles:
        assert [p["rank"] for p in purchases] == RANKS, f"{line}: ranks {[p['rank'] for p in purchases]}"
        assert not styles, f"{line}: has no styles"
        assert [p["price"] for p in purchases] == PRICES, f"{line}: prices"
        return

    assert [p["rank"] for p in purchases] == RANKS_WITH_STYLES, f"{line}: ranks {[p['rank'] for p in purchases]}"
    assert len(styles) == 2 and all(p["rank"] == 20 for p in styles), f"{line}: needs two rank-20 styles"
    assert [p["price"] for p in purchases if p is not styles[1]] == PRICES, f"{line}: prices"
    assert styles[0]["price"] == styles[1]["price"] and styles[0]["excludes"] == styles[1]["perk"], f"{line}: styles must mirror"


def check_crops(crops: list[dict]) -> None:
    assert len(crops) == 26, f"{len(crops)} crops, expected 26"
    for tier, count in zip(range(1, 6), (5, 5, 5, 7, 4)):
        assert sum(1 for crop in crops if crop["tier"] == tier) == count, f"tier {tier} crops"


def update_workbook(path: Path) -> dict:
    lines = [(line, engine, parse_line(PERK_DEFINITIONS / file)) for line, file, engine, _has_styles in LINES]
    for (line, _engine, purchases), (_name, _file, _read_by, has_styles) in zip(lines, LINES):
        check(line, purchases, has_styles)
    crops = parse_crops(CROP_DEFINITIONS)
    check_crops(crops)

    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}
        paths = Bible.resolve_sheets(source)
        strings = Bible.read_strings(source)

    entry = paths[SHEET]
    sheet = Bible.Sheet(SHEET, original[entry].decode("utf-8"), strings)
    summary = update_sheet(sheet, lines, crops)
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
    print(f"Updated {SHEET} in {path.name}: {summary}; every other entry is byte-identical.")
    return summary


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    update_workbook(parser.parse_args().workbook)


if __name__ == "__main__":
    main()
