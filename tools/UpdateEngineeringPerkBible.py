"""Write the Engineering perk lines (Overclocker and Technician) into the Design Bible's Engineering tab.

Reads every magnitude from SWLOR.Game.Server/Service/CraftService/Minigame/Circuit/CircuitPerkRules.cs and CircuitRules.cs, so the
Bible cannot drift from the code, and edits only the target cells of design/bible/SWLOR Design Bible.xlsx at the zip/XML level,
copying every other workbook entry byte-for-byte. Safe to rerun: the perk rows go on the rows after the retired Droid Assembly rows,
the Total row follows them, and a rerun writes the same cells. After a merge conflict on the workbook, take the base's workbook and
run this again.

  python tools/UpdateEngineeringPerkBible.py [--workbook PATH] [--rules DIR]

The descriptions are the ones EngineeringPerkDefinition.cs prints; EngineeringPerkBibleTests compares them.
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
DEFAULT_RULES = ROOT / "SWLOR.Game.Server" / "Service" / "CraftService" / "Minigame" / "Circuit"
SHEET = "Engineering"
FIRST_PERK_ROW = 14
TEMPLATE_ROW = 9
DESCRIPTION_MARKER = "Two perk lines"


def read_constants(directory: Path) -> dict[str, int]:
    values: dict[str, int] = {}
    for name in ("CircuitPerkRules.cs", "CircuitRules.cs"):
        source = (directory / name).read_text(encoding="utf-8")
        for match in re.finditer(r"public const (?:int|double) (\w+) = (-?[0-9.]+);", source):
            number = float(match.group(2))
            values[match.group(1)] = int(number) if number == int(number) else number
    return values


def point(n: int) -> str:
    return "percentage point" if n == 1 else "percentage points"


def style_text(k: dict[str, int]) -> dict[str, str]:
    return {
        "Overclock": (
            f"Wire and Tune add {k['OverclockExtraLoad']} extra Load, and each Load point adds "
            f"{k['OverclockLoadBoostPercent']} more {point(k['OverclockLoadBoostPercent'])} of output."),
        "Safe Mode": (
            f"Actions cannot overload while Load is no more than {k['SafeModeOverloadGrace']} over the line, and each Load point adds "
            f"{-k['SafeModeLoadBoostPercent']} fewer {point(-k['SafeModeLoadBoostPercent'])} of output."),
    }


def rows(k: dict[str, int]) -> list[dict]:
    """The twenty purchases, in the order of the Bible tab: the Overclocker line by rank, then the Technician line by rank."""
    styles = style_text(k)
    kept = k["OverloadIntegrityPercent"] * (100 - k["ShockAbsorbersReductionPercent"]) // 100
    style_prefix = "Circuit style, picked in the Engineering craft window with one style active at a time. "
    minimum = k["MinimumActionIntegrity"]

    def row(line, price, name, rank, kind, description, cp="-", durability="-", notes=""):
        return dict(line=line, price=price, name=name, rank=rank, kind=kind, description=description, cp=cp, durability=durability, notes=notes)

    return [
        row("Overclocker", 1, "Conductive Wiring I", 5, "Trait",
            f"In the Circuit, each Load point adds {k['ConductiveWiringLoadBoostRank1']} more {point(k['ConductiveWiringLoadBoostRank1'])} of output to Wire, Tune and Discharge.",
            notes="Stat CircuitLoadBoostPercent. Ranks replace earlier ranks."),
        row("Overclocker", 1, "Capacitor Bank", 10, "Trait",
            f"In the Circuit, Discharge bursts are {k['CapacitorBankDischargePercent']}% larger.",
            notes="Stat CircuitDischargePowerPercent. Applies to Discharge, Cascade Discharge and Surge."),
        row("Overclocker", 2, "Heat Sinks I", 15, "Trait",
            f"In the Circuit, the overload line rises by {k['HeatSinksLineRank1']}.",
            notes="Stat CircuitOverloadLine. The line never passes the highest Load, 10. Ranks replace earlier ranks."),
        row("Overclocker", 2, "Overclock", 20, "Style",
            f"{style_prefix}{styles['Overclock']}",
            notes="Style: the status effect Overclock carries CircuitExtraLoadPerBuildAction and CircuitLoadBoostPercent. Not exclusive with Safe Mode as a purchase; only one style is active."),
        row("Overclocker", 3, "Cascade Discharge", 25, "Active",
            f"Adds the Cascade Discharge action to the Circuit. It costs {k['CascadeDischargeCp']} CP and spends all Load as one burst into both Assembly and Calibration at {k['CascadeDischargeSharePercent']}% each. It never overloads.",
            cp=str(k["CascadeDischargeCp"]), durability="Yes",
            notes="Stat CircuitCascadeDischargeUnlocked. Costs the Integrity of a Discharge, reduced by Insulated Traces."),
        row("Overclocker", 3, "Overdrive Boost", 30, "Trait",
            f"In the Circuit, Boost adds {k['BoostLoad'] + k['OverdriveBoostExtraLoad']} Load instead of {k['BoostLoad']} and costs {k['OverdriveBoostCpReduction']} less CP.",
            notes="Stats CircuitBoostLoad and CircuitBoostCpReduction."),
        row("Overclocker", 3, "Heat Sinks II", 35, "Trait",
            f"In the Circuit, the overload line rises by {k['HeatSinksLineRank2']}.",
            notes="Replaces Heat Sinks I's +1."),
        row("Overclocker", 3, "Shock Absorbers", 40, "Trait",
            f"In the Circuit, an overload costs a third less Integrity ({k['OverloadIntegrityPercent']}% of the piece's maximum becomes {kept}%).",
            notes="Stat CircuitOverloadIntegrityReductionPercent."),
        row("Overclocker", 3, "Conductive Wiring II", 45, "Trait",
            f"In the Circuit, each Load point adds {k['ConductiveWiringLoadBoostRank2']} more {point(k['ConductiveWiringLoadBoostRank2'])} of output to Wire, Tune and Discharge.",
            notes="Replaces Conductive Wiring I's total."),
        row("Overclocker", 4, "Surge", 50, "Trait",
            "Once per Circuit craft, the first overload does not cost Integrity. It discharges all your Load at full strength into whichever of Assembly or Calibration is further from full (Assembly on a tie).",
            notes="Capstone, automatic. Stat CircuitSurgeUnlocked. No quest requirement."),
        row("Technician", 1, "Insulated Traces I", 5, "Trait",
            f"In the Circuit, Wire, Tune, Boost and Discharge cost {k['InsulatedTracesReductionRank1']} less Integrity, to a minimum of {minimum}. Ground and Patch are unchanged.",
            notes="Stat CircuitActionIntegrityReduction. Also applies to Micro-Tune and Cascade Discharge. Ranks replace earlier ranks."),
        row("Technician", 1, "Fine Calibration I", 10, "Trait",
            f"In the Circuit, Tune builds {k['FineCalibrationPercentRank1']}% more Calibration.",
            notes="Stat CircuitTunePowerPercent. Also raises Micro-Tune. Ranks replace earlier ranks."),
        row("Technician", 2, "Diagnostics", 15, "Trait",
            "In the Circuit, a Clean Signal is announced two actions ahead and stays announced until it applies.",
            notes="Stat CircuitForecastTurns. The signal's chance per action is unchanged."),
        row("Technician", 2, "Safe Mode", 20, "Style",
            f"{style_prefix}{styles['Safe Mode']}",
            notes="Style: the status effect Safe Mode carries CircuitOverloadGrace and CircuitLoadBoostPercent. Not exclusive with Overclock as a purchase; only one style is active."),
        row("Technician", 3, "Micro-Tune", 25, "Active",
            f"Adds the Micro-Tune action to the Circuit. It builds {k['MicroTuneSharePercent']}% of a Tune's Calibration, adds no Load and costs {k['MicroTuneCp']} CP and {k['MicroTuneIntegrity']} Integrity. It can overload only when Load is already over the line.",
            cp=str(k["MicroTuneCp"]), durability="Yes",
            notes="Stat CircuitMicroTuneUnlocked. A Clean Signal doubles a Tune, not a Micro-Tune."),
        row("Technician", 3, "Signal Lock", 30, "Trait",
            f"In the Circuit, a Clean Signal is announced on {k['SignalLockChancePercent']} percentage points more actions ({k['CleanSignalChancePercent']}% becomes {k['CleanSignalChancePercent'] + k['SignalLockChancePercent']}%).",
            notes="Stat CircuitSignalChancePercent."),
        row("Technician", 3, "Precision Patch", 35, "Trait",
            f"In the Circuit, Patch restores {k['PrecisionPatchExtraRestore']} more Integrity and costs {k['PrecisionPatchCpReduction']} less CP.",
            notes="Stats CircuitPatchRestore and CircuitPatchCpReduction."),
        row("Technician", 3, "Insulated Traces II", 40, "Trait",
            f"In the Circuit, Wire, Tune, Boost and Discharge cost {k['InsulatedTracesReductionRank2']} less Integrity, to a minimum of {minimum}. Ground and Patch are unchanged.",
            notes="Replaces Insulated Traces I's total."),
        row("Technician", 3, "Fine Calibration II", 45, "Trait",
            f"In the Circuit, Tune builds {k['FineCalibrationPercentRank2']}% more Calibration.",
            notes="Replaces Fine Calibration I's total."),
        row("Technician", 4, "Failsafe", 50, "Trait",
            "Once per Circuit craft, the first time Integrity would reach 0 it stays at 1 and the piece survives.",
            notes="Capstone, automatic. Stat CircuitFailsafeUnlocked. No quest requirement."),
    ]


def description_suffix(total: int) -> str:
    return (
        f" {DESCRIPTION_MARKER} of {total // 2} SP each (Overclocker and Technician) change the Circuit minigame: ten purchases at Engineering"
        " 5/10/15/20/25/30/35/40/45/50 priced 1/1/2/2/3/3/3/3/3/4 SP, a style at rank 20 and an automatic capstone at rank 50. Both styles can"
        " be owned and the craft window toggles between them, one at a time.")


def update_sheet(sheet: Bible.Sheet, k: dict[str, int]) -> int:
    template = sheet.row_cells(TEMPLATE_ROW)
    label = sheet_total_label(sheet)  # found first: before the first run, row 14 holds it and the first perk row replaces it
    purchases = rows(k)
    total = sum(purchase["price"] for purchase in purchases)

    def cell(col: str, number: int, value=None, kind="text"):
        style = Bible.cell_style(template[col])
        ref = f"{col}{number}"
        if value is None:
            return Bible.blank_cell(ref, style)
        if kind == "number":
            return Bible.num_cell(ref, style, value)
        return Bible.text_cell(ref, style, str(value))

    for offset, purchase in enumerate(purchases):
        number = FIRST_PERK_ROW + offset
        cells = {col: cell(col, number) for col in template}
        values = {
            "A": purchase["line"], "C": purchase["name"], "D": f"Engineering {purchase['rank']}", "E": "All", "F": purchase["kind"],
            "G": purchase["description"], "H": "None", "I": "None", "J": "None", "K": purchase["cp"], "L": purchase["durability"],
            "M": "Implemented", "O": purchase["notes"],
        }
        for col, value in values.items():
            cells[col] = cell(col, number, value)
        cells["B"] = cell("B", number, purchase["price"], "number")
        sheet.write_row(number, cells)

    total_row = FIRST_PERK_ROW + len(purchases)
    first, last = TEMPLATE_ROW, total_row - 1
    total_cells = {col: cell(col, total_row) for col in template}
    total_cells["A"] = re.sub(r'r="A\d+"', f'r="A{total_row}"', label, count=1)
    total_cells["B"] = Bible.num_cell(
        f"B{total_row}", Bible.cell_style(label), total, f'SUMIFS(B{first}:B{last},M{first}:M{last},"<>Retired")')
    sheet.write_row(total_row, total_cells)

    Bible.set_formula(sheet, "D4", f"SUM(B{total_row})", total)
    description = sheet.row_cells(3)["B"]
    text = sheet.text_of(description)
    if DESCRIPTION_MARKER in text:
        text = text[:text.index(DESCRIPTION_MARKER)].rstrip()
    sheet.set_cell(3, "B", Bible.text_cell("B3", Bible.cell_style(description), text + description_suffix(total)))
    return total


def sheet_total_label(sheet: Bible.Sheet) -> str:
    """The Total label cell, taken from wherever the workbook has it (row 14 before the first run)."""
    for number in range(FIRST_PERK_ROW, FIRST_PERK_ROW + 30):
        cell = sheet.row_cells(number).get("A")
        if cell is not None and sheet.text_of(cell) == "Total":
            return cell
    raise ValueError("The Engineering tab has no Total row to move.")


def update_workbook(path: Path, k: dict[str, int]) -> int:
    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}
        paths = Bible.resolve_sheets(source)
        strings = Bible.read_strings(source)

    entry = paths[SHEET]
    sheet = Bible.Sheet(SHEET, original[entry].decode("utf-8"), strings)
    total = update_sheet(sheet, k)
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
    parser.add_argument("--rules", type=Path, default=DEFAULT_RULES, help="the Circuit folder holding CircuitPerkRules.cs and CircuitRules.cs")
    args = parser.parse_args()
    total = update_workbook(args.workbook, read_constants(args.rules))
    print(f"Wrote the Engineering perk lines ({total} SP) into {args.workbook.name}; every other entry is byte-identical.")


if __name__ == "__main__":
    main()
