#!/usr/bin/env python3
"""Build the standalone worksheet XML payload for the Design Bible Slicing tab."""

from __future__ import annotations

import argparse
import html
import json
from pathlib import Path

import GenerateSlicingContent as content


ROOT = Path(__file__).resolve().parents[1]
NS = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
REL_NS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
HEADER_STYLE = 369
BODY_STYLE = 370
COLUMNS = "ABCDEFGH"


def localized(field: dict) -> str:
    value = field.get("value", {})
    if isinstance(value, dict):
        return next((str(text) for text in value.values() if text), "")
    return str(value or "")


def description(resref: str) -> str:
    data = json.loads((content.UTI / f"{resref}.uti.json").read_text(encoding="utf-8"))
    return localized(data["Description"])


def direct_rewards() -> list[list[object]]:
    rows: list[list[object]] = []
    for resref, name, tier, source, _template, exceptional in content.WEAPONS:
        armor = content.level_for_tier(tier, exceptional)
        rows.append([source.title(), tier, "Named Item", "Weapon", name, resref,
                     f"Exceptional; Armor {armor}" if exceptional else f"Armor {armor}", description(resref)])
    for resref, name, tier, source, _template, _identity, exceptional in content.GEAR:
        armor = content.level_for_tier(tier, exceptional)
        rows.append([source.title(), tier, "Named Item", "Equipment", name, resref,
                     f"Exceptional; Armor {armor}" if exceptional else f"Armor {armor}", description(resref)])

    for index, (resref, name, _recipe_id) in enumerate(content.SCHEMATICS):
        if index < 10:
            source, tier = "Lockbox", index // 2 + 1
        else:
            source, tier = "Terminal", (index - 10) // 3 + 1
        rows.append([source, tier, "Schematic", "Unlock", name, resref, "Rare instructions", description(resref)])

    for index, (note_id, beast_name) in enumerate(content.FIELD_NOTES):
        if index < 5:
            source, tier = "Lockbox", index + 1
        else:
            source, tier = "Terminal", (index - 5) // 2 + 1
        resref = f"fnote_{note_id}"
        rows.append([source, tier, "Field Note", "Mutation", f"Field Note: {beast_name}", resref,
                     "Unique Slicing source", description(resref)])

    for index, (resref, name, _tool_type, tier, _effect) in enumerate(content.TOOLS):
        source = "Lockbox" if index < 5 else "Terminal"
        rows.append([source, tier, "Tool", "Consumable", name, resref, "One tool per attempt", description(resref)])

    source_order = {"Lockbox": 0, "Terminal": 1}
    category_order = {"Named Item": 0, "Schematic": 1, "Field Note": 2, "Tool": 3}
    return sorted(rows, key=lambda row: (source_order[row[0]], row[1], category_order[row[2]], row[4]))


def craft_outputs() -> list[list[object]]:
    rows: list[list[object]] = []
    for resref, name, tier, _template, _identity in content.SMITH_OUTPUTS:
        rows.append(["Smithery", tier, name, resref, "Armor", 1, "Fixed stats; Armor requirement; one armor enhancement slot", description(resref)])
    for resref, name, tier in content.FUSES:
        rows.append(["Engineering", tier, name, resref, "Tool", 1, "+1 trace on the first move; consumed when primed", description(resref)])
    for resref, name, tier, _subtype in content.FOODS:
        rows.append(["Agriculture", tier, name, resref, "Food", 1, "One food enhancement slot; derived stats only", description(resref)])
    for resref, name, tier in content.CONCENTRATES:
        rows.append(["Espionage", tier, name, resref, "Poison", 1, f"One vial, 10 charges, +{tier * 10}% Poison Bonus potency", description(resref)])
    for structure_id, name, _placeable, _appearance in content.STRUCTURES:
        resref = f"structure_{structure_id:04d}"
        rows.append(["Fabrication", structure_id - 430, name, resref, "Structure", 1,
                     "+1 property item storage; one structure enhancement slot", description(resref)])
    return sorted(rows, key=lambda row: (row[1], row[0]))


def recipe_documentation_payload() -> dict[str, object]:
    tier_materials = [
        ("fiberp_ruined", "lth_ruined", "elec_ruined"),
        ("fiberp_flawed", "lth_flawed", "elec_flawed"),
        ("fiberp_imperfect", "lth_imperfect", "elec_imperfect"),
        ("fiberp_high", "lth_high", "elec_high"),
        ("fiberp_perfect", "lth_perfect", "elec_perfect"),
    ]
    smith_enums = ["StitchplateLockGloves", "FalseFaceFieldVisor", "QuietstepReinforcedBoots",
                   "DeadDropArmoredCloak", "BlacksiteBreachHarness"]
    smith_categories = ["Glove", "Helmet", "Boots", "Cloak", "Breastplate"]
    cooking_enums = ["QuietwatchJerky", "DustveilTravelCakes", "TombwalkerBroth",
                     "SnowblindHuntersStew", "NightMarchReserve"]
    cooking_components = [("herb_v", "wild_meat"), ("herb_m", "raivor_meat"),
                          ("herb_c", "byysk_meat"), ("herb_t", "sanddemon_meat"),
                          ("herb_x", "wild_innards")]
    fuse_enums = ["CopperTraceFuse", "BraidedTraceFuse", "PhaseTraceFuse", "CryoTraceFuse", "NullTraceFuse"]
    concentrate_enums = ["WhisperthornConcentrate", "GlassfangConcentrate", "TombsporeConcentrate",
                         "RimevenomConcentrate", "NightrootConcentrate"]
    concentrate_components = [("kath_blood", "herb_v"), ("raivor_blood", "herb_m"),
                              ("byysk_meat", "herb_c"), ("sanddemon_meat", "herb_t"),
                              ("wild_innards", "herb_x")]
    structure_enums = ["RustlineDataTerminal", "CipherfileCabinet", "ListeningPostMonitor",
                       "GhostChannelConsole", "BlacksiteAnalysisStation"]

    def standard_cells(skill: str, enum_name: str, category: str, name: str, level: int,
                       resref: str, enhancement: str, slots: int, components: list[tuple[str, int]]) -> dict[str, object]:
        cells: dict[str, object] = {
            "A": skill, "B": "Yes", "C": "Slicing Rewards", "D": enum_name, "E": category,
            "F": 0, "G": name, "H": level, "I": 1, "J": resref,
            "K": enhancement, "L": slots,
        }
        for index, (component_resref, quantity) in enumerate(components):
            cells[chr(ord("M") + index * 2)] = component_resref
            cells[chr(ord("N") + index * 2)] = quantity
        cells["AC"] = 0
        return cells

    sheets: dict[str, list[dict[str, object]]] = {
        "xl/worksheets/sheet37.xml": [],
        "xl/worksheets/sheet38.xml": [],
        "xl/worksheets/sheet39.xml": [],
        "xl/worksheets/sheet40.xml": [],
    }
    for index, (resref, name, _tier, _template, _identity) in enumerate(content.SMITH_OUTPUTS):
        fiber, leather, _electronics = tier_materials[index]
        sheets["xl/worksheets/sheet37.xml"].append({
            "row": 726 + index, "style": 6,
            "cells": standard_cells("Smithery", smith_enums[index], smith_categories[index], name,
                                    index * 10 + 5, resref, "Armor", 1, [(fiber, 3), (leather, 2)]),
        })

    for index, (resref, name, _tier, _subtype) in enumerate(content.FOODS):
        herb, hunted = cooking_components[index]
        cells = standard_cells("Agriculture", cooking_enums[index], "Food", name, index * 10 + 5,
                               resref, "Food", 1, [(herb, 2), (hunted, 2)])
        cells["AD"] = cells.pop("AC")
        sheets["xl/worksheets/sheet39.xml"].append({"row": 512 + index, "style": 14, "cells": cells})

    for index, (resref, name, _tier) in enumerate(content.FUSES):
        fiber, _leather, electronics = tier_materials[index]
        sheets["xl/worksheets/sheet38.xml"].append({
            "row": 1815 + index, "style": 6,
            "cells": standard_cells("Engineering", fuse_enums[index], "Tool", name, index * 10 + 5,
                                    resref, "N/A", 0, [(electronics, 3), (fiber, 2)]),
        })
    for index, (resref, name, _tier) in enumerate(content.CONCENTRATES):
        creature, herb = concentrate_components[index]
        sheets["xl/worksheets/sheet38.xml"].append({
            "row": 1820 + index, "style": 6,
            "cells": standard_cells("Espionage", concentrate_enums[index], "Poison", name, index * 10 + 6,
                                    resref, "N/A", 0, [(creature, 1), (herb, 2)]),
        })

    for index, (structure_id, name, _placeable, _appearance) in enumerate(content.STRUCTURES):
        fiber, _leather, electronics = tier_materials[index]
        cells = {
            "A": "Fabrication", "B": "Yes", "C": "Slicing Rewards", "D": structure_enums[index],
            "E": "Electronics", "F": 0, "G": name, "H": index * 10 + 5, "I": 1,
            "J": f"structure_{structure_id:04d}", "K": 1, "L": "Structure", "M": 1,
            "N": electronics, "O": 4, "P": fiber, "Q": 3, "AD": 0,
        }
        sheets["xl/worksheets/sheet40.xml"].append({"row": 558 + index, "style": 6, "cells": cells})

    return {
        "worksheets": sheets,
        "espionageDescriptions": {
            "G18": "Can slice tier 1 lockboxes and terminals.",
            "G22": "Can slice tier 2 lockboxes and terminals.",
            "G25": "Can slice tier 3 lockboxes and terminals. Grants +1 trace during slicing.",
            "G30": "Can slice tier 4 lockboxes and terminals. Grants +2 trace during slicing.",
            "G32": "Can slice tier 5 lockboxes and terminals. Grants +3 trace during slicing.",
        },
    }


class Sheet:
    def __init__(self) -> None:
        self.rows: list[str] = []
        self.merges: list[str] = []
        self.row_number = 0

    @staticmethod
    def _cell(address: str, value: object, style: int) -> str:
        if isinstance(value, (int, float)) and not isinstance(value, bool):
            return f'<c r="{address}" s="{style}"><v>{value}</v></c>'
        text = html.escape(str(value), quote=False)
        return f'<c r="{address}" s="{style}" t="inlineStr"><is><t xml:space="preserve">{text}</t></is></c>'

    def add_row(self, values: list[object], style: int = BODY_STYLE) -> int:
        if len(values) > len(COLUMNS):
            raise ValueError(f"Worksheet row has {len(values)} values but only {len(COLUMNS)} columns: {values}")
        self.row_number += 1
        cells = "".join(self._cell(f"{COLUMNS[index]}{self.row_number}", value, style)
                        for index, value in enumerate(values) if value != "")
        self.rows.append(f'<row r="{self.row_number}">{cells}</row>')
        return self.row_number

    def blank(self) -> None:
        self.row_number += 1
        self.rows.append(f'<row r="{self.row_number}"/>')

    def merged(self, text: str, style: int = HEADER_STYLE) -> None:
        row = self.add_row([text], style)
        self.merges.append(f"A{row}:H{row}")

    def table(self, headers: list[object], rows: list[list[object]]) -> None:
        self.add_row(headers, HEADER_STYLE)
        for row in rows:
            self.add_row(row)

    def xml(self) -> str:
        merge_xml = ""
        if self.merges:
            merge_xml = f'<mergeCells count="{len(self.merges)}">' + "".join(
                f'<mergeCell ref="{reference}"/>' for reference in self.merges) + "</mergeCells>"
        return (
            '<?xml version="1.0" encoding="utf-8" standalone="yes"?>'
            f'<worksheet xmlns="{NS}" xmlns:r="{REL_NS}">'
            '<sheetPr><pageSetUpPr/></sheetPr>'
            '<dimension ref="A1:H{last}"/>'
            '<sheetViews><sheetView showGridLines="0" workbookViewId="0">'
            '<pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/>'
            '</sheetView></sheetViews>'
            '<sheetFormatPr defaultColWidth="12.63" defaultRowHeight="15"/>'
            '<cols>'
            '<col min="1" max="1" width="17" customWidth="1"/>'
            '<col min="2" max="2" width="22" customWidth="1"/>'
            '<col min="3" max="3" width="24" customWidth="1"/>'
            '<col min="4" max="4" width="16" customWidth="1"/>'
            '<col min="5" max="5" width="34" customWidth="1"/>'
            '<col min="6" max="6" width="20" customWidth="1"/>'
            '<col min="7" max="7" width="25" customWidth="1"/>'
            '<col min="8" max="8" width="80" customWidth="1"/>'
            '</cols>'
            '<sheetData>{rows}</sheetData>{merges}'
            '<pageMargins left="0.3" right="0.3" top="0.5" bottom="0.5" header="0.2" footer="0.2"/>'
            '</worksheet>'
        ).format(last=self.row_number, rows="".join(self.rows), merges=merge_xml)


def build_sheet() -> Sheet:
    sheet = Sheet()
    sheet.merged("Slicing Minigame, World Terminals, and Reward Catalog")
    sheet.merged("Scope: loot acquisition only. Mandalorian Facility quest-line terminals and area security-system effects are explicitly out of scope.", BODY_STYLE)
    sheet.blank()

    sheet.merged("Core Implementation Rules")
    sheet.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Interaction model", "Turn-based", "", "", "", "", "", "No timers or heartbeat-driven puzzle state; the server reacts only to player actions."],
        ["Win condition", "Powered route", "", "", "", "", "", "Rotate and swap tiles until one continuous entry-to-core circuit is powered."],
        ["Rotate", "1 trace", "", "", "", "", "", "Clockwise rotation only."],
        ["Adjacent swap", "2 trace", "", "", "", "", "", "Entry and core tiles cannot be swapped."],
        ["Trace bonuses", "Ranks III/IV/V: +1/+2/+3", "", "", "", "", "", "Every 5 combined Lockpicking plus positive Perception modifier grants +1 trace, capped at +5."],
        ["Attempt commitment", "First board/tool action", "", "", "", "", "", "Cancel before commitment is free. Abort, death, disconnect, or area exit after commitment counts as failure."],
        ["Combat/range", "Validated on actions", "", "", "", "", "", "Starting does not force combat. Terminal actions validate interaction range."],
        ["Terminal ownership", "Shared node; 3-minute stale claim", "", "", "", "", "", "One active owner. A stale committed claim resolves as a failure before another player may claim it."],
        ["Persistence", "Seed/failures/integrity on object", "", "", "", "", "", "Transferred lockboxes preserve their seed and failure state; retries use the same authored board."],
        ["Success", "One weighted item + XP once", "", "", "", "", "", "Lockbox is consumed. Terminal burns out and respawns at a new valid walkmesh point in 45-75 minutes."],
        ["Failure", "No reward or XP", "", "", "", "", "", "First failed attempt is free of destruction risk; subsequent attempts become progressively dangerous."],
        ["Direct reward rules", "Fixed-stat items", "", "", "", "", "", "Armor skill sets equipment requirements. No raw attributes, random affixes, sets, or direct-reward enhancement slots."],
    ])
    sheet.blank()

    sheet.merged("Tier Board and Progression")
    sheet.table(["Tier", "Grid", "Extra Trace", "Authored Swaps", "Slicing Rank", "Armor Bands", "", "Notes"], [
        [1, "3 x 3", 4, 0, 1, "0 / 5", "", "Base trace equals known solution cost plus extra trace."],
        [2, "4 x 3", 3, 1, 2, "10 / 15", "", "Deterministic guaranteed-solvable route."],
        [3, "4 x 4", 3, 2, 3, "20 / 25", "", "No runtime solver."],
        [4, "5 x 4", 2, 3, 4, "30 / 35", "", "Rare exceptional named item uses the half-tier Armor band."],
        [5, "5 x 5", 2, 4, 5, "40 / 45", "", "Rare exceptional named item uses the half-tier Armor band."],
    ])
    sheet.blank()

    sheet.merged("Reward Weights and Failure Curve")
    sheet.table(["Band", "Weight / Chance", "", "", "", "", "", "Notes"], [
        ["Common/pre-existing item", "65%", "", "", "", "", "", "Tier-appropriate existing items may supplement the 100 new direct rewards."],
        ["Tool", "15%", "", "", "", "", "", "Five unique tools per source pool across tiers."],
        ["Named item", "12%", "", "", "", "", "", "Exceptional item is a 0.5% absolute sub-band, one per source/tier."],
        ["Schematic", "6%", "", "", "", "", "", "Lockbox and terminal schematic pools remain separate."],
        ["Field note", "2%", "", "", "", "", "", "Fifteen DiscoveryOnly mutation notes become Slicing rewards."],
        ["Legacy accessory", "2% absolute", "", "", "", "", "", "Lockbox named-item sub-band only; terminals never award legacy accessories."],
    ])
    sheet.table(["Failure Number", "Destruction Chance", "", "", "", "", "", "Integrity After Survival"], [
        [1, "0%", "", "", "", "", "", "100%"], [2, "10%", "", "", "", "", "", "90%"],
        [3, "25%", "", "", "", "", "", "75%"], [4, "50%", "", "", "", "", "", "50%"],
        [5, "100%", "", "", "", "", "", "Destroyed"],
    ])
    sheet.blank()

    sheet.merged("Shared World Terminal Nodes")
    area_rows = [[tier, area, f"SLICING_TERMINAL_T{tier}", 1, "45-75 minutes", "Random valid walkmesh", "", "One shared node per area"]
                 for area, tier in content.TERMINAL_AREAS.items()]
    sheet.table(["Tier", "Area Resref", "Spawn Table", "Node Count", "Respawn", "Placement", "", "Notes"], area_rows)
    sheet.blank()

    sheet.merged("100 New Direct Rewards")
    sheet.table(["Source", "Tier", "Pool Category", "Item Type", "Name", "Resref", "Requirement / Rarity", "Effect / Acquisition"], direct_rewards())
    sheet.blank()

    sheet.merged("Additional Craft Outputs (Beyond the 100 Direct Rewards)")
    sheet.table(["Craft", "Tier", "Name", "Resref", "Output Type", "Quantity", "Rules", "Description"], craft_outputs())
    sheet.blank()

    sheet.merged("Legacy Lockbox Accessories")
    legacy_rows = []
    for tier in range(1, 6):
        for slot in ("neck", "belt", "ring"):
            legacy_rows.append(["Lockbox", tier, "Legacy Accessory", slot.title(), f"Espionage {slot.title()} T{tier}",
                                f"espn_{slot}_{tier}", f"Armor {tier * 10 - 5}", "2% absolute named-item sub-band; fixed existing appearance and stats"])
    sheet.table(["Source", "Tier", "Pool Category", "Item Type", "Name", "Resref", "Requirement", "Acquisition"], legacy_rows)
    sheet.blank()

    sheet.merged("Agriculture and Poison Production Note")
    sheet.merged("Agriculture must remain an additional source for the herb components used by concentrated poison recipes. Each Espionage concentrate consumes tier-appropriate agricultural herbs alongside a creature material; agriculture is not replaced by Slicing.", BODY_STYLE)
    return sheet


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--recipes-output", type=Path)
    args = parser.parse_args()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    sheet = build_sheet()
    args.output.write_text(sheet.xml(), encoding="utf-8", newline="")
    if args.recipes_output:
        args.recipes_output.write_text(
            json.dumps(recipe_documentation_payload(), indent=2) + "\n", encoding="utf-8")
    print(f"Generated Slicing worksheet XML with {sheet.row_number} rows")


if __name__ == "__main__":
    main()
