"""Record the Astromech skills (Chassis, Utility Systems, Armaments) in the Design Bible.

Adds (or rewrites) one tab, "Astromech Skills": a header block with one row per skill, then a perk table with one row per
mirrored perk level. The rows come from the generated perk definitions
(SWLOR.Game.Server/Feature/PerkDefinition/UtilitySystems*PerkDefinition.cs and Armaments*PerkDefinition.cs); the mirror
mapping (which humanoid line and perk each one copies) comes from tools/AstromechSkillMirrors.json. Names, levels, SP
prices, required ranks and descriptions are read from the code, never retyped.

Edits the workbook at the zip/XML level only: the new sheet entry is added (or replaced) and xl/workbook.xml,
xl/_rels/workbook.xml.rels and [Content_Types].xml gain one entry each; every other entry keeps its exact bytes. Safe to
rerun: a second run produces a byte-identical workbook.

  python tools/UpdateAstromechSkillBible.py [--workbook PATH]

Run tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible afterwards.
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
from xml.sax.saxutils import escape

sys.path.insert(0, str(Path(__file__).resolve().parent))
import UpdateCraftingProgressionBible as bible  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_WORKBOOK = ROOT / "design" / "bible" / "SWLOR Design Bible.xlsx"
MIRRORS = ROOT / "tools" / "AstromechSkillMirrors.json"
PERK_DIR = ROOT / "SWLOR.Game.Server" / "Feature" / "PerkDefinition"
TAB = "Astromech Skills"

MAIN_NS = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
REL_NS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
SHEET_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"

# Existing perk-tab cell styles (xl/styles.xml cellXfs), reused so the tab adds no styles.
S_TITLE = "17"     # bold, centered, wrapped (the "Name" label on perk tabs)
S_BOLD = "29"      # bold, centered
S_HEADER = "30"    # bold, centered, wrapped (perk-table header cells)
S_CELL = "14"      # centered
S_WRAP = "16"      # centered, wrapped (perk-table Description cells)

COLUMNS = [  # (header, width)
    ("Skill", 16.0), ("Line", 14.0), ("Perk", 22.0), ("Level", 7.0), ("SP Price", 9.0), ("Required Rank", 10.5),
    ("Description", 92.25), ("Mirrors (source skill - source line - source perk)", 52.0), ("Source Tab", 13.0),
    ("Notes", 45.0),
]
ROMAN = ["I", "II", "III", "IV", "V", "VI", "VII", "VIII"]

CHASSIS_DESCRIPTION = "Ability of an astromech to withstand attacks through its frame, plating, and internal modules."
CHASSIS_HOW = ("Stands in for Armor wherever Armor rank is read for an astromech: defense, evasion, Armor-requirement perks and "
               "gear. It has no perk lines of its own.")


def unquote(text: str) -> str:
    return bytes(text, "utf-8").decode("unicode_escape")


def split_words(name: str) -> str:
    return re.sub(r"(?<=[a-z])(?=[A-Z])", " ", name)


def parse_definition(path: Path) -> list[dict]:
    """One dict per perk level in the file: category, perk enum, name, level, price, skill, rank, description, quest."""
    text = path.read_text(encoding="utf-8")
    perks = []
    pieces = re.split(r"_builder\.Create\(PerkCategoryType\.(\w+), PerkType\.(\w+)\)", text)
    for index in range(1, len(pieces), 3):
        category, perk_type, body = pieces[index], pieces[index + 1], pieces[index + 2]
        name = re.search(r'\.Name\("((?:[^"\\]|\\.)*)"\)', body).group(1)
        levels = body.split(".AddPerkLevel()")[1:]
        for number, level in enumerate(levels, 1):
            description = re.search(r'\.Description\("((?:[^"\\]|\\.)*)"\)', level)
            price = re.search(r"\.Price\((\d+)\)", level)
            skill = re.search(r"\.RequirementSkill\(SkillType\.(\w+), (\d+)\)", level)
            quest = re.search(r"\.RequirementQuest\((?:\w+\.)?(\w+?)(?:QuestId)?\)", level)
            if not (description and price and skill):
                raise SystemExit(f"{path.name}: {perk_type} level {number} is missing a description, price or skill requirement")
            perks.append({
                "category": category, "perk": perk_type, "name": unquote(name), "level": number, "levels": len(levels),
                "price": int(price.group(1)), "skill": skill.group(1), "rank": int(skill.group(2)),
                "description": unquote(description.group(1)), "quest": quest.group(1) if quest else None,
            })
    return perks


def mirror_rows() -> list[dict]:
    """The perk-table rows, in the order of the generated definition files, with the mirror mapping attached."""
    config = json.loads(MIRRORS.read_text(encoding="utf-8"))
    rows = []
    for mirror in config["mirrors"]:
        for line in mirror["lines"]:
            source_skill = line.get("source_skill_name", mirror["source_skill_name"])
            by_id = {data["id"]: (source_perk, data) for source_perk, data in line["perks"].items()}
            path = PERK_DIR / f"{line['category']}PerkDefinition.cs"
            levels = parse_definition(path)
            for level in levels:
                if level["category"] != line["category"] or level["perk"] not in by_id:
                    raise SystemExit(f"{path.name}: {level['perk']} is not in the mirror config for {line['category']}")
                source_perk, data = by_id[level["perk"]]
                if data["name"] != level["name"]:
                    raise SystemExit(f"{path.name}: perk {level['perk']} is named {level['name']!r}, config says {data['name']!r}")
                rows.append({
                    "skill": mirror["skill_name"], "skill_type": level["skill"], "line": line["name"],
                    "perk": level["name"], "level": level["level"], "levels": level["levels"],
                    "price": level["price"], "rank": level["rank"], "description": level["description"],
                    "mirrors": f"{source_skill} - {line['source_name']} - {data['source_name']}",
                    "source_tab": source_skill, "source_line": line["source_name"], "source_perk": data["source_name"],
                    "notes": "Also requires the capstone quest " + split_words(level["quest"]) if level["quest"] else "",
                })
            if not levels:
                raise SystemExit(f"{path.name} has no perk levels")
    return rows


def header_rows() -> list[dict]:
    config = json.loads(MIRRORS.read_text(encoding="utf-8"))
    by_skill = {m["skill"]: m for m in config["mirrors"]}
    utility, armaments = by_skill["UtilitySystems"], by_skill["Armaments"]

    def lines(mirror):
        return ", ".join(f"{line['name']} (mirror of {mirror['source_skill_name']} - {line['source_name']})"
                         for line in mirror["lines"])

    return [
        {"skill": "Chassis", "what": CHASSIS_DESCRIPTION, "how": CHASSIS_HOW},
        {"skill": utility["skill_name"], "what": utility["skill_description"],
         "how": f"Number-for-number copy of First Aid under new names. Lines: {lines(utility)}. Rank and XP are Utility Systems."},
        {"skill": armaments["skill_name"], "what": armaments["skill_description"],
         "how": "Tools act as a staff (Shock Prod) and a rifle (Blaster Mount) in combat; rank and XP are Armaments. "
                f"Lines: {lines(armaments)}."},
    ]


def source_checks(rows: list[dict], strings_workbook: Path) -> list[str]:
    """Compares each mirrored level with the source tab's row; returns a message per price or rank disagreement."""
    with zipfile.ZipFile(strings_workbook) as archive:
        paths = bible.resolve_sheets(archive)
        strings = bible.read_strings(archive)
        sheets = {}
        for tab in {row["source_tab"] for row in rows}:
            sheets[tab] = bible.Sheet(tab, archive.read(paths[tab]).decode("utf-8"), strings)
    index = {}
    for tab, sheet in sheets.items():
        for number in range(1, sheet.last_data_row() + 1):
            cells = sheet.row_cells(number)
            if not {"A", "B", "C", "D"} <= cells.keys():
                continue
            name = sheet.text_of(cells["C"]).strip()
            if name:
                index.setdefault((tab, name), []).append(
                    (sheet.text_of(cells["A"]).strip(), sheet.text_of(cells["B"]).strip(), sheet.text_of(cells["D"]).strip()))
    problems = []
    for row in rows:
        name = row["source_perk"] + (f" {ROMAN[row['level'] - 1]}" if row["levels"] > 1 else "")
        label = f"{row['skill']} / {row['perk']} {row['level']}"
        candidates = index.get((row["source_tab"], name), [])
        if len(candidates) != 1:
            problems.append(f"{label}: {len(candidates)} rows named '{name}' on tab {row['source_tab']}")
            continue
        line, price, requirement = candidates[0]
        row["source_line"] = line  # the tab's own line label (the Rifle tab calls the code's Pacification line Suppression)
        row["mirrors"] = f"{row['source_tab']} - {line} - {row['source_perk']}"
        if not price or float(price) != row["price"]:
            problems.append(f"{label}: SP price {row['price']} in code, {price} on {row['source_tab']}")
        source_rank = 0 if requirement in ("-", "") else (int(m.group(1)) if (m := re.search(r"(\d+)\s*$", requirement)) else None)
        if source_rank != row["rank"]:
            problems.append(f"{label}: required rank {row['rank']} in code, '{requirement}' on {row['source_tab']}")
    return problems


def col_letter(index: int) -> str:
    return chr(64 + index)


def text(ref: str, style: str, value: str) -> str:
    return bible.text_cell(ref, style, value)


def number(ref: str, style: str, value: int) -> str:
    return f'<c r="{ref}" s="{style}" t="n"><v>{value}</v></c>'


def sheet_xml(rows: list[dict], header: list[dict]) -> str:
    xml_rows = []

    def add(n: int, cells: list[str]) -> None:
        xml_rows.append(f'<row r="{n}">{"".join(cells)}</row>')

    add(1, [text("A1", S_TITLE, "Name"), text("B1", S_BOLD, TAB)])
    add(2, [text("A2", S_TITLE, "Description"),
            text("B2", S_BOLD, "Astromech-only skills. Perk lines are number-for-number mirrors of humanoid lines; see the Mirrors column.")])
    add(4, [text("A4", S_HEADER, "Skill"), text("G4", S_HEADER, "What it is"), text("H4", S_HEADER, "How it works")])
    n = 5
    for item in header:
        add(n, [text(f"A{n}", S_CELL, item["skill"]), text(f"G{n}", S_WRAP, item["what"]), text(f"H{n}", S_WRAP, item["how"])])
        n += 1
    n += 1
    add(n, [text(f"{col_letter(i)}{n}", S_HEADER, title) for i, (title, _) in enumerate(COLUMNS, 1)])
    for row in rows:
        n += 1
        cells = [
            text(f"A{n}", S_CELL, row["skill"]), text(f"B{n}", S_CELL, row["line"]), text(f"C{n}", S_CELL, row["perk"]),
            number(f"D{n}", S_CELL, row["level"]), number(f"E{n}", S_CELL, row["price"]), number(f"F{n}", S_CELL, row["rank"]),
            text(f"G{n}", S_WRAP, row["description"]), text(f"H{n}", S_WRAP, row["mirrors"]),
            text(f"I{n}", S_CELL, row["source_tab"]),
        ]
        if row["notes"]:
            cells.append(text(f"J{n}", S_WRAP, row["notes"]))
        add(n, cells)
    cols = "".join(f'<col min="{i}" max="{i}" width="{width}" customWidth="1" />' for i, (_, width) in enumerate(COLUMNS, 1))
    return (
        '<?xml version="1.0" encoding="utf-8" standalone="yes"?>'
        f'<worksheet xmlns="{MAIN_NS}" xmlns:r="{REL_NS}"><sheetPr><outlinePr summaryBelow="0" summaryRight="0" /></sheetPr>'
        '<sheetViews><sheetView workbookViewId="0" /></sheetViews>'
        '<sheetFormatPr defaultColWidth="12.63" defaultRowHeight="15.0" />'
        f"<cols>{cols}</cols><sheetData>{''.join(xml_rows)}</sheetData>"
        '<pageMargins bottom="1.0" footer="0.0" header="0.0" left="0.75" right="0.75" top="1.0" />'
        '<pageSetup orientation="landscape" /></worksheet>'
    )


def register_sheet(original: dict[str, bytes]) -> tuple[str, dict[str, bytes]]:
    """Returns the sheet entry path for TAB and the edited workbook.xml, rels and content types (new entries only if absent)."""
    workbook = original["xl/workbook.xml"].decode("utf-8")
    rels = original["xl/_rels/workbook.xml.rels"].decode("utf-8")
    types = original["[Content_Types].xml"].decode("utf-8")
    existing = re.search(r'<sheet name="%s" [^>]*r:id="(rId\d+)"' % re.escape(TAB), workbook)
    if existing:
        target = re.search(r'<Relationship Id="%s"[^>]*Target="([^"]+)"' % existing.group(1), rels) or \
            re.search(r'<Relationship [^>]*Target="([^"]+)"[^>]*Id="%s"' % existing.group(1), rels)
        return "xl/" + target.group(1), {}
    sheet_numbers = [int(m) for m in re.findall(r"^xl/worksheets/sheet(\d+)\.xml$", "\n".join(original), re.M)]
    entry_number = max(sheet_numbers) + 1
    sheet_id = max(int(m) for m in re.findall(r'<sheet [^>]*sheetId="(\d+)"', workbook)) + 1
    rel_id = "rId%d" % (max(int(m) for m in re.findall(r'Id="rId(\d+)"', rels)) + 1)
    entry = f"xl/worksheets/sheet{entry_number}.xml"
    workbook = workbook.replace("</sheets>", f'<sheet name="{TAB}" sheetId="{sheet_id}" r:id="{rel_id}"/></sheets>', 1)
    rels = rels.replace("</Relationships>",
                        f'<Relationship Id="{rel_id}" Type="{REL_NS}/worksheet" Target="worksheets/sheet{entry_number}.xml"/></Relationships>', 1)
    types = types.replace("</Types>", f'<Override PartName="/{entry}" ContentType="{SHEET_TYPE}"/></Types>', 1)
    return entry, {"xl/workbook.xml": workbook.encode("utf-8"), "xl/_rels/workbook.xml.rels": rels.encode("utf-8"),
                   "[Content_Types].xml": types.encode("utf-8")}


def update_workbook(path: Path) -> list[str]:
    rows = mirror_rows()
    problems = source_checks(rows, path)  # also swaps in the source tab's own line labels
    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}

    entry, edits = register_sheet(original)
    edits[entry] = sheet_xml(rows, header_rows()).encode("utf-8")
    if all(original.get(name) == data for name, data in edits.items()):
        print("The Astromech Skills tab is already current.")
        return problems

    handle, temp_name = tempfile.mkstemp(suffix=".xlsx", dir=str(path.parent))
    os.close(handle)
    try:
        with zipfile.ZipFile(temp_name, "w", zipfile.ZIP_DEFLATED) as target:
            for info in infos:
                clone = zipfile.ZipInfo(info.filename, info.date_time)
                clone.compress_type = zipfile.ZIP_DEFLATED
                clone.external_attr = info.external_attr
                target.writestr(clone, edits.get(info.filename, original[info.filename]))
            if entry not in original:
                clone = zipfile.ZipInfo(entry, infos[-1].date_time)
                clone.compress_type = zipfile.ZIP_DEFLATED
                clone.external_attr = infos[-1].external_attr
                target.writestr(clone, edits[entry])
        with zipfile.ZipFile(temp_name) as written:
            assert written.testzip() is None
            before = {n: hashlib.sha256(d).hexdigest() for n, d in original.items() if n not in edits}
            after = bible.entry_hashes(written, set(edits))
            if before != after:
                raise RuntimeError("Untouched workbook entries changed: " +
                                   ", ".join(sorted(k for k in before if before[k] != after.get(k))))
        os.replace(temp_name, path)
    finally:
        if os.path.exists(temp_name):
            os.remove(temp_name)
    print(f"Wrote the {TAB} tab ({len(rows)} perk rows) in {path.name}; every other entry is byte-identical "
          f"except {', '.join(sorted(n for n in edits if n != entry)) or 'none'}.")
    return problems


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    problems = update_workbook(parser.parse_args().workbook)
    for problem in problems:
        print("MISMATCH (code value used):", problem)


if __name__ == "__main__":
    main()
