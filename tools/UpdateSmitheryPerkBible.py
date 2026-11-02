"""Write the Smithery perk lines (Forgemaster and Artisan) into the Design Bible's Smithery tab and the perk manifest.

Reads every magnitude from SWLOR.Game.Server/Service/CraftService/Minigame/Forge/ForgePerkRules.cs and ForgeRules.cs, so the Bible cannot
drift from the code, and edits only the target cells of design/bible/SWLOR Design Bible.xlsx at the zip/XML level, copying every other
workbook entry byte-for-byte. Safe to rerun: the perk rows go on the rows under the header, the Total row follows them, and a rerun
writes the same cells. After a merge conflict on the workbook, take the base's workbook and run this again. The Smithery rows of
SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv are rewritten from the same rows.

  python tools/UpdateSmitheryPerkBible.py [--workbook PATH] [--manifest PATH] [--rules DIR]

The descriptions are the ones SmitheryPerkDefinition.cs prints; SmitheryPerkBibleTests compares them.
"""
from __future__ import annotations

import argparse
import csv
import hashlib
import io
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
DEFAULT_MANIFEST = ROOT / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv"
DEFAULT_RULES = ROOT / "SWLOR.Game.Server" / "Service" / "CraftService" / "Minigame" / "Forge"
SHEET = "Smithery"
TEMPLATE_SHEET = "Engineering"  # the Smithery tab has no perk row of its own to take the cell styles from
TEMPLATE_ROW = 9
FIRST_PERK_ROW = 9
DESCRIPTION_MARKER = "Two perk lines"
STYLE_PREFIX = "Plasma Forge style, picked in the Plasma Forge window with one style active at a time. "


def read_constants(directory: Path) -> dict[str, float]:
    values: dict[str, float] = {}
    for name in ("ForgePerkRules.cs", "ForgeRules.cs"):
        source = (directory / name).read_text(encoding="utf-8")
        for match in re.finditer(r"public const (?:int|double) (\w+) = (-?[0-9.]+);", source):
            number = float(match.group(2))
            values[match.group(1)] = int(number) if number == int(number) else number
    return values


def point(n: int) -> str:
    return "percentage point" if n == 1 else "percentage points"


def number(value: float) -> str:
    """The way C# prints a value with the format 0.#, so 1.5 stays 1.5 and 2.0 becomes 2."""
    text = f"{value:.1f}"
    return text[:-2] if text.endswith(".0") else text


def describe_style(shift: int, press: int, hone: int) -> str:
    """The style in words, as ForgeStyles.Describe prints it: the range move first, then the Hydro-Press and Vibro-Hone changes, leaving out zeros."""
    changes = []
    if press != 0:
        changes.append(f"Hydro-Press gains {abs(press)}% {'more' if press > 0 else 'less'} Shape")
    if hone != 0:
        changes.append(f"Vibro-Hone gains {abs(hone)}% {'more' if hone > 0 else 'less'} Finish")
    sentences = []
    if shift != 0:
        sentences.append(f"The working range moves {'up' if shift > 0 else 'down'} {abs(shift)} Thermal.")
    if changes:
        sentences.append(" and ".join(changes) + ".")
    return " ".join(sentences)


def style_text(k: dict[str, float]) -> dict[str, str]:
    return {
        "Hot Forging": describe_style(k["HotForgingRangeShift"], k["HotForgingPressPercent"], k["HotForgingHonePercent"]),
        "Cold Working": describe_style(k["ColdWorkingRangeShift"], k["ColdWorkingPressPercent"], k["ColdWorkingHonePercent"]),
    }


def rows(k: dict[str, float]) -> list[dict]:
    """The twenty purchases, in the order of the Bible tab: the Forgemaster line by rank, then the Artisan line by rank."""
    styles = style_text(k)
    each = k["HammerRhythmPercent"]
    cap = each * k["HammerRhythmMaxStacks"]
    chance = k["StableContainmentChancePercent"]
    quench_usual = round(k["QuenchEdgeMultiplier"] * 100)
    hone_usual = round(k["HoneHotEdgeMultiplier"] * 100)
    focus_usual = k["StableContainmentBonusPercent"]

    def row(line, price, name, rank, kind, description, cp="-", durability="-", notes=""):
        return dict(line=line, price=price, name=name, rank=rank, kind=kind, description=description, cp=cp, durability=durability, notes=notes)

    return [
        row("Forgemaster", 1, "Plasma Efficiency", 5, "Trait",
            f"In the Plasma Forge, Ramp Plasma costs {k['PlasmaEfficiencyCpReduction']} less CP, to a minimum of 1.",
            notes="Stat ForgeRampCpReduction. Also lowers Surge Plasma's CP, which is 1.5 Ramp Plasmas."),
        row("Forgemaster", 1, "Heat Retention I", 10, "Trait",
            f"In the Plasma Forge, the piece loses {k['HeatRetentionRank1']} less Thermal after every action, to a minimum of 1 lost.",
            notes="Stat ForgeCoolingReduction. A recipe that cools 1 per action cannot cool less. Ranks replace earlier ranks."),
        row("Forgemaster", 2, "Overdrive Press", 15, "Active",
            f"Adds the Overdrive Press action to the Plasma Forge: a Hydro-Press with {100 + k['OverdrivePressPercent']}% of the Shape. "
            f"It costs {k['OverdrivePressCp']} CP and {k['OverdrivePressIntegrityMultiplier']} times a Hydro-Press's Integrity, and cannot be used twice in a row.",
            cp=str(k["OverdrivePressCp"]), durability="Yes",
            notes="Stat ForgeOverdrivePressPercent. Takes the style's Hydro-Press percent and Stable Containment; it is not a Hydro-Press for Hammer Rhythm, so it ends the count."),
        row("Forgemaster", 2, "Hot Forging", k["StyleRank"], "Style",
            f"{STYLE_PREFIX}{styles['Hot Forging']}",
            notes="Style: the status effect Hot Forging carries the stats its description names (ForgeRangeShift, ForgePressOutputPercent, ForgeHoneOutputPercent; a zero is not granted). Smithery is not split, so it is not exclusive with Cold Working as a purchase; only one style is active."),
        row("Forgemaster", 3, "Hammer Rhythm", 25, "Trait",
            f"In the Plasma Forge, a Hydro-Press made right after another gains {each}% more Shape, {each * 2}% after two in a row and {cap}% after three or more. "
            "Any other action starts the count again.",
            notes="Stat ForgePressStreakPercent, capped at 3 presses in a row by ForgePerkRules.HammerRhythmMaxStacks. The Red Line Press counts as a Hydro-Press. A preview does not count."),
        row("Forgemaster", 3, "Heat Retention II", 30, "Trait",
            f"In the Plasma Forge, the piece loses {k['HeatRetentionRank2']} less Thermal after every action, to a minimum of 1 lost.",
            notes="Replaces Heat Retention I's 1."),
        row("Forgemaster", 3, "Scorch Guard", 35, "Trait",
            f"In the Plasma Forge, a Hydro-Press made above the working range cracks {k['ScorchGuardReductionPercent']}% less Integrity.",
            notes="Stat ForgePressOverheatIntegrityReductionPercent. Also lowers an Overdrive Press above the range. A press inside or below the range is unchanged."),
        row("Forgemaster", 3, "Surge Plasma", 40, "Active",
            f"Adds the Surge Plasma action to the Plasma Forge: it raises Thermal by {k['SurgePlasmaThermal']} in one action. "
            f"It costs {number(k['SurgePlasmaCpMultiplier'])} times a Ramp Plasma's CP and {k['RampIntegrity']} Integrity.",
            cp="1.5x Ramp Plasma", durability="Yes",
            notes="Stat ForgeSurgePlasmaThermal. Thermal stops at 100. Refused on a set piece."),
        row("Forgemaster", 3, "Fast Rebond", 45, "Trait",
            f"In the Plasma Forge, Rebond costs {k['FastRebondThermalReduction']} less Thermal and {k['FastRebondCpReduction']} less CP, to a minimum of 1 CP.",
            notes="Stats ForgeRebondThermalReduction and ForgeRebondCpReduction. Rebond is still refused at full Integrity."),
        row("Forgemaster", 4, "Red Line", 50, "Capstone",
            f"Adds the Red Line Press to the Plasma Forge: once per craft, a Hydro-Press with {k['RedLineShapeMultiplier']} times the Shape that costs no Integrity. "
            "It can be used only while Thermal is above the working range.",
            notes="Capstone, an action the player chooses when to spend. Stat ForgeRedLineCharges. Takes the style's Hydro-Press percent, Hammer Rhythm and Stable Containment. No quest requirement."),
        row("Artisan", 1, "Steady Hands", 5, "Trait",
            f"In the Plasma Forge, Vibro-Hone costs {k['SteadyHandsIntegrityReduction']} less Integrity, to a minimum of 1.",
            notes="Stat ForgeHoneIntegrityReduction. Micro-Hone keeps its own cost."),
        row("Artisan", 1, "Clean Containment I", 10, "Trait",
            f"In the Plasma Forge, Stable Containment is announced on {k['CleanContainmentRank1']} {point(k['CleanContainmentRank1'])} more actions "
            f"({chance}% becomes {chance + k['CleanContainmentRank1']}%).",
            notes="Stat ForgeContainmentChancePercent. Ranks replace earlier ranks."),
        row("Artisan", 2, "Micro-Hone", 15, "Active",
            f"Adds the Micro-Hone action to the Plasma Forge: it raises Finish by {k['MicroHonePercent']}% of a Vibro-Hone's best result at any Thermal. "
            f"It costs {k['MicroHoneCp']} CP and {k['MicroHoneIntegrity']} Integrity.",
            cp=str(k["MicroHoneCp"]), durability="Yes",
            notes="Stat ForgeMicroHonePercent. Gives Finish only, also on a set piece (80% of its flat rate). Takes the style's Vibro-Hone percent and Stable Containment."),
        row("Artisan", 2, "Cold Working", k["StyleRank"], "Style",
            f"{STYLE_PREFIX}{styles['Cold Working']}",
            notes="Style: the status effect Cold Working carries the stats its description names (ForgeRangeShift, ForgePressOutputPercent, ForgeHoneOutputPercent; a zero is not granted). Smithery is not split, so it is not exclusive with Hot Forging as a purchase; only one style is active."),
        row("Artisan", 3, "Clean Containment II", 25, "Trait",
            f"In the Plasma Forge, Stable Containment is announced on {k['CleanContainmentRank2']} {point(k['CleanContainmentRank2'])} more actions "
            f"({chance}% becomes {chance + k['CleanContainmentRank2']}%).",
            notes="Replaces Clean Containment I's total."),
        row("Artisan", 3, "Steady Quench", 30, "Trait",
            f"In the Plasma Forge, Cryo-Quench keeps {quench_usual + k['SteadyQuenchEdgePercent']}% of its center bonus at the edge of the working range instead of {quench_usual}%.",
            notes="Stat ForgeQuenchEdgePercent. A quench outside the range is unchanged."),
        row("Artisan", 3, "Cool Hand", 35, "Trait",
            f"In the Plasma Forge, Vibro-Hone in the hot half of the working range falls to {hone_usual + k['CoolHandHotEdgePercent']}% of its best result at the hot edge instead of {hone_usual}%.",
            notes="Stat ForgeHoneHotEdgePercent."),
        row("Artisan", 3, "Temper Hold", 40, "Active",
            f"Adds the Temper Hold action to the Plasma Forge: for the next {k['TemperHoldActions']} actions the piece does not cool. It costs {k['TemperHoldCp']} CP.",
            cp=str(k["TemperHoldCp"]),
            notes="Stat ForgeTemperHoldActions. The Temper Hold action itself cools as usual. It costs no Integrity and cannot be used while a hold is running."),
        row("Artisan", 3, "Containment Focus", 45, "Trait",
            f"In the Plasma Forge, Stable Containment gives Vibro-Hone +{focus_usual + k['ContainmentFocusPercent']}% instead of +{focus_usual}%. Hydro-Press is unchanged.",
            notes="Stat ForgeContainmentHonePercent. Also raises Micro-Hone."),
        row("Artisan", 4, "Perfect Temper", 50, "Capstone",
            f"In the Plasma Forge, a Cryo-Quench made with Thermal within {k['PerfectTemperWindow']} of the center of the working range gives {k['PerfectTemperPercent']}% more Finish.",
            notes="Capstone, automatic. Stat ForgePerfectTemperPercent. Applies to the quench's Finish at its Thermal, so a quench 2 from the center gives 1.5 times the (slightly lower) Finish of that Thermal. No quest requirement."),
    ]


def description_suffix(total: int, k: dict[str, float]) -> str:
    return (
        f" {DESCRIPTION_MARKER} of {total // 2} SP each (Forgemaster and Artisan) change the Plasma Forge minigame: ten purchases at Smithery"
        " 5/10/15/20/25/30/35/40/45/50 priced 1/1/2/2/3/3/3/3/3/4 SP, a style at rank 20 and a capstone at rank 50. Both styles can"
        " be owned and the Plasma Forge window toggles between them, one at a time.")


def update_sheet(sheet: Bible.Sheet, template_sheet: Bible.Sheet, k: dict[str, float]) -> int:
    template = template_sheet.row_cells(TEMPLATE_ROW)
    label = total_label(template_sheet)
    purchases = rows(k)
    total = sum(purchase["price"] for purchase in purchases)

    def cell(col: str, number_: int, value=None, kind="text"):
        style = Bible.cell_style(template[col])
        ref = f"{col}{number_}"
        if value is None:
            return Bible.blank_cell(ref, style)
        if kind == "number":
            return Bible.num_cell(ref, style, value)
        return Bible.text_cell(ref, style, str(value))

    for offset, purchase in enumerate(purchases):
        number_ = FIRST_PERK_ROW + offset
        cells = {col: cell(col, number_) for col in template}
        values = {
            "A": purchase["line"], "C": purchase["name"], "D": f"Smithery {purchase['rank']}", "E": "All", "F": purchase["kind"],
            "G": purchase["description"], "H": "None", "I": "None", "J": "None", "K": purchase["cp"], "L": purchase["durability"],
            "M": "Implemented", "O": purchase["notes"],
        }
        for col, value in values.items():
            cells[col] = cell(col, number_, value)
        cells["B"] = cell("B", number_, purchase["price"], "number")
        sheet.write_row(number_, cells, after=number_ - 1)

    total_row = FIRST_PERK_ROW + len(purchases)
    first, last = FIRST_PERK_ROW, total_row - 1
    total_cells = {col: cell(col, total_row) for col in template}
    total_cells["A"] = re.sub(r'r="A\d+"', f'r="A{total_row}"', label, count=1)
    total_cells["B"] = Bible.num_cell(
        f"B{total_row}", Bible.cell_style(label), total, f'SUMIFS(B{first}:B{last},M{first}:M{last},"<>Retired")')
    sheet.write_row(total_row, total_cells, after=total_row - 1)

    Bible.set_formula(sheet, "D4", f"SUM(B{total_row})", total)
    description = sheet.row_cells(3)["B"]
    text = sheet.text_of(description)
    if DESCRIPTION_MARKER in text:
        text = text[:text.index(DESCRIPTION_MARKER)].rstrip()
    sheet.set_cell(3, "B", Bible.text_cell("B3", Bible.cell_style(description), text + description_suffix(total, k)))
    return total


def total_label(sheet: Bible.Sheet) -> str:
    """The Total label cell, taken from the template sheet."""
    for number_ in range(TEMPLATE_ROW, TEMPLATE_ROW + 60):
        cell = sheet.row_cells(number_).get("A")
        if cell is not None and sheet.text_of(cell) == "Total":
            return cell
    raise ValueError(f"The {TEMPLATE_SHEET} tab has no Total row to take the style from.")


def update_workbook(path: Path, k: dict[str, float]) -> int:
    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}
        paths = Bible.resolve_sheets(source)
        strings = Bible.read_strings(source)

    entry = paths[SHEET]
    sheet = Bible.Sheet(SHEET, original[entry].decode("utf-8"), strings)
    template_sheet = Bible.Sheet(TEMPLATE_SHEET, original[paths[TEMPLATE_SHEET]].decode("utf-8"), strings)
    total = update_sheet(sheet, template_sheet, k)
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


def update_manifest(path: Path, k: dict[str, float]) -> int:
    """Rewrites the Smithery rows of the perk manifest from the Bible rows; every other line stays as it was."""
    text = path.read_text(encoding="utf-8", newline="")
    newline = "\r\n" if "\r\n" in text else "\n"
    lines = text.split(newline)
    trailing = lines[-1] == ""
    if trailing:
        lines = lines[:-1]

    def quote(value) -> str:
        return '"' + str(value).replace('"', '""') + '"'

    block = []
    for offset, purchase in enumerate(rows(k)):
        fields = [
            "Smithery", FIRST_PERK_ROW + offset, purchase["line"], purchase["price"], purchase["name"], f"Smithery {purchase['rank']}", "All",
            purchase["kind"], purchase["description"], "None", "None", "None", "", "", "", "", "", "Implemented", "", purchase["notes"], "",
        ]
        block.append(",".join(quote(field) for field in fields))

    kept = [line for line in lines if not line.startswith('"Smithery",')]
    anchor = next(index for index, line in enumerate(kept) if line.startswith('"Engineering",'))
    result = kept[:anchor] + block + kept[anchor:]
    path.write_text(newline.join(result) + (newline if trailing else ""), encoding="utf-8", newline="")
    return len(block)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--rules", type=Path, default=DEFAULT_RULES, help="the Forge folder holding ForgePerkRules.cs and ForgeRules.cs")
    args = parser.parse_args()
    constants = read_constants(args.rules)
    total = update_workbook(args.workbook, constants)
    count = update_manifest(args.manifest, constants)
    print(f"Wrote the Smithery perk lines ({total} SP) into {args.workbook.name} and {count} rows into {args.manifest.name}; every other workbook entry is byte-identical.")


if __name__ == "__main__":
    main()
