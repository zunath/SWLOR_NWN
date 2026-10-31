"""Record Fabrication's Builder and Researcher perk lines in the Design Bible's Fabrication tab.

Reads the two lines from the perk definitions (FabricationBuilderPerkDefinition.cs and FabricationResearcherPerkDefinition.cs), so the
Bible's names, prices, requirements, descriptions and stats cannot drift from the code, and edits only the target cells of
design/bible/SWLOR Design Bible.xlsx at the zip/XML level, copying every other workbook entry byte-for-byte. Safe to rerun: the line
rows are rewritten in place from the first free row after the retired perks, and the totals and their cached values are rewritten.

  python tools/UpdateFabricationPerkBible.py [--workbook PATH]

Each line is ten purchases at Fabrication 5/10/15/20/25/30/35/40/45/50 for 1/1/2/2/3/3/3/3/3/4 SP (25 SP). The rank-20 purchase is a
choice of two styles, so a line lists eleven rows and its Total subtracts the alternative style. The tab's Total SP is the sum of the
two lines' totals and the retired perks' (which is 0).
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
PERK_DEFINITIONS = ROOT / "SWLOR.Game.Server" / "Feature" / "PerkDefinition"
SHEET = "Fabrication"

TEMPLATE_PERK_ROW = 9
RETIRED_TOTAL_ROW = 18
FIRST_LINE_ROW = 20

LINES = (
    ("Builder", "FabricationBuilderPerkDefinition.cs", "Fabricator"),
    ("Researcher", "FabricationResearcherPerkDefinition.cs", "research Experiment"),
)

RANKS = [5, 10, 15, 20, 20, 25, 30, 35, 40, 45, 50]
PRICES = [1, 1, 2, 2, 3, 3, 3, 3, 3, 4]
ROMAN = ["I", "II", "III", "IV", "V", "VI"]
ACTIVE_PERKS = {"DualFeed", "PeerReview", "Breakthrough"}
DESCRIPTION_MARKER = "The Builder (Fabricator) and Researcher"
DESCRIPTION_SUFFIX = (
    " The Builder (Fabricator) and Researcher (research Experiment) lines are two optional lines of 25 SP each at Fabrication"
    " 5/10/15/20/25/30/35/40/45/50, with a choice of two exclusive styles at rank 20 and a capstone at rank 50.")


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
                "rank": int(re.search(r"\.RequirementSkill\(SkillType\.Fabrication, (\d+)\)", level).group(1)),
                "description": re.search(r'\.Description\("((?:[^"\\]|\\.)*)"\)', level).group(1),
                "stats": [(stat, int(value)) for stat, value in re.findall(r"\.IncreasesStat\(StatType\.(\w+), (-?\d+)\)", level)],
                "excludes": excluded[0] if excluded else None,
                "order": len(purchases),
            })
    return sorted(purchases, key=lambda purchase: (purchase["rank"], purchase["order"]))


def bible_type(purchase: dict) -> str:
    """Capstone at rank 50, Trait otherwise: an action a perk adds to a craft window is a window action, not a combat ability."""
    return "Capstone" if purchase["rank"] == 50 else "Trait"


def note(purchase: dict, engine: str, names: dict[str, str]) -> str:
    stats = ", ".join(f"{stat} {value:+d}" for stat, value in purchase["stats"])
    parts = [f"Read by the {engine}. Stats: {stats}."]
    if purchase["perk"] in ACTIVE_PERKS:
        parts.append("Adds an action to the window (a window action, not a combat ability).")
    if purchase["level"] > 1:
        parts.append("Only this rank's stats apply, so it restates the earlier rank's.")
    if purchase["excludes"]:
        parts.append(f"Style, always active. Exclusive with {names[purchase['excludes']]} through the perk requirements; "
                     "switching costs a Perk Refund Token.")
    if purchase["rank"] == 50:
        parts.append("Capstone: no quest requirement.")
    return " ".join(parts)


def perk_cells(row: int, purchase: dict, line: str, engine: str, names: dict[str, str], styles: dict[str, str]) -> dict[str, str]:
    values = {
        "A": line,
        "C": purchase["name"],
        "D": f"Fabrication {purchase['rank']}",
        "E": "All",
        "F": bible_type(purchase),
        "G": purchase["description"],
        "H": "None",
        "I": "None",
        "J": "None",
        "K": "-",
        "L": "-",
        "M": "Implemented",
        "O": note(purchase, engine, names),
    }
    cells = {col: Bible.text_cell(f"{col}{row}", styles[col], text) for col, text in values.items()}
    cells["B"] = Bible.num_cell(f"B{row}", styles["B"], purchase["price"])
    cells["N"] = Bible.blank_cell(f"N{row}", styles["N"])
    return cells


def update_sheet(sheet: Bible.Sheet, lines: list[tuple[str, str, list[dict]]]) -> dict:
    styles = {col: Bible.cell_style(cell) for col, cell in sheet.row_cells(TEMPLATE_PERK_ROW).items()}
    total_styles = {col: Bible.cell_style(cell) for col, cell in sheet.row_cells(RETIRED_TOTAL_ROW).items()}
    names = {purchase["perk"]: purchase["name"] for _line, _engine, purchases in lines for purchase in purchases}

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
        (_, _), (alternative, alternative_row) = styles_seen
        total = sum(purchase["price"] for purchase in purchases) - alternative["price"]
        sheet.write_row(row, {
            "A": Bible.text_cell(f"A{row}", total_styles["A"], "Total"),
            "B": Bible.num_cell(f"B{row}", total_styles["B"], total, f"SUM(B{first}:B{last})-B{alternative_row}"),
            "C": Bible.blank_cell(f"C{row}", total_styles["C"]),
        }, after=row - 1)
        total_rows.append(row)
        totals.append(total)
        summary[line] = {"first": first, "last": last, "total_row": row, "sp": total}
        row += 2  # one blank row between the lines

    refs = ",".join([f"B{RETIRED_TOTAL_ROW}"] + [f"B{number}" for number in total_rows])
    grand = sum(totals)
    Bible.set_formula(sheet, "D4", f"SUM({refs})", grand)

    cell = sheet.row_cells(3)["B"]
    text = sheet.text_of(cell)
    if DESCRIPTION_MARKER in text:
        text = text[:text.index(DESCRIPTION_MARKER)].rstrip()
    sheet.set_cell(3, "B", Bible.text_cell("B3", Bible.cell_style(cell), text + DESCRIPTION_SUFFIX))
    summary["total_sp"] = grand
    return summary


def check(line: str, purchases: list[dict]) -> None:
    assert [p["rank"] for p in purchases] == RANKS, f"{line}: ranks {[p['rank'] for p in purchases]}"
    styles = [p for p in purchases if p["excludes"]]
    assert len(styles) == 2 and all(p["rank"] == 20 for p in styles), f"{line}: needs two rank-20 styles"
    assert [p["price"] for p in purchases if p is not styles[1]] == PRICES, f"{line}: prices"
    assert styles[0]["price"] == styles[1]["price"] and styles[0]["excludes"] == styles[1]["perk"], f"{line}: styles must mirror"


def update_workbook(path: Path) -> dict:
    lines = [(line, engine, parse_line(PERK_DEFINITIONS / file)) for line, file, engine in LINES]
    for line, _engine, purchases in lines:
        check(line, purchases)

    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}
        paths = Bible.resolve_sheets(source)
        strings = Bible.read_strings(source)

    entry = paths[SHEET]
    sheet = Bible.Sheet(SHEET, original[entry].decode("utf-8"), strings)
    summary = update_sheet(sheet, lines)
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
