"""Records the crafting progression pace decisions on the Design Bible's System Changes & Migrations tab.

The decisions are the named constants `CraftMinigameXp.CraftingPaceMultiplier`, `GuildTaskXp.XpPercent` and the percents in
`ResearchXpRules`; this script reads them from the source, so the Bible row and the code cannot drift apart. It adds one row after the
tab's last numbered row, or rewrites that row when it already exists (it is found by its opening words), edits only that row at the
zip/XML level, and copies every other part of the workbook unchanged. A second run changes nothing. After a workbook merge conflict,
take the base workbook and run this again, then run tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible.
"""
from __future__ import annotations

import argparse
import importlib.util
import re
import sys
from pathlib import Path
from xml.sax.saxutils import escape, unescape

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_WORKBOOK = ROOT / "design" / "bible" / "SWLOR Design Bible.xlsx"
SERVICE = ROOT / "SWLOR.Game.Server" / "Service"
PACE_SOURCE = SERVICE / "CraftService" / "Minigame" / "CraftMinigameXp.cs"
GUILD_SOURCE = SERVICE / "GuildTaskXp.cs"
RESEARCH_SOURCE = SERVICE / "CraftService" / "Research" / "ResearchXpRules.cs"
TAB = "System Changes & Migrations"
MARKER = "Crafting progression pace"
NUMBER_STYLE = "3"
TEXT_STYLE = "4"
LAST_NUMBERED_ROW = 21

_SPEC = importlib.util.spec_from_file_location("update_recipe_parameter_bible", ROOT / "tools" / "UpdateRecipeParameterBible.py")
_SHARED = importlib.util.module_from_spec(_SPEC)
sys.modules[_SPEC.name] = _SHARED
_SPEC.loader.exec_module(_SHARED)

_ROW = re.compile(r'<row r="(?P<number>\d+)"(?P<attributes>[^>]*?)(?:/>|>(?P<body>.*?)</row>)', re.S)
_CELL = re.compile(r'<c r="(?P<column>[A-Z]+)\d+"(?P<attributes>[^>]*?)(?:/>|>(?P<body>.*?)</c>)', re.S)
_NUMBER = re.compile(r"<v>([^<]*)</v>")
_TEXT = re.compile(r"<t\b[^>]*>(.*?)</t>", re.S)


class ProgressionPaceBibleError(Exception):
    pass


def read_constant(source: Path, name: str, kind: str) -> str:
    """Reads `public const <kind> <name> = <number>;` from a source file."""
    match = re.search(rf"public const {kind} {name} = ([0-9]+(?:\.[0-9]+)?);", source.read_text(encoding="utf-8"))
    if not match:
        raise ProgressionPaceBibleError(f"{source} has no {name} constant")
    return match.group(1)


def read_values(pace_source: Path = PACE_SOURCE, guild_source: Path = GUILD_SOURCE, research_source: Path = RESEARCH_SOURCE) -> dict[str, str]:
    return {
        "pace": read_constant(pace_source, "CraftingPaceMultiplier", "double"),
        "guild": read_constant(guild_source, "XpPercent", "int"),
        "experiment": read_constant(research_source, "ExperimentXpPercent", "int"),
        "completion": read_constant(research_source, "CompletionXpPercent", "int"),
        "level_bonus": read_constant(research_source, "LevelBonusPercentPerLevel", "int"),
    }


def row_text(values: dict[str, str]) -> str:
    return (
        f"{MARKER}: crafting XP in Smithery, Engineering, Fabrication and Agriculture's cooking is {values['pace']} times what the recipe "
        "and its minigame say, on top of each minigame's own XP multiplier. Gathering, Espionage, fishing, combat and Piloting XP are unchanged. "
        f"The Smithery, Engineering, Fabrication and Agriculture guilds give XP in their skill for every item handed in: {values['guild']}% of "
        "the XP chart's pay for the task's level against the player's rank, where the level is the recipe's for a crafted item and the fish's "
        "for a catch task. A hand-in five or more levels below the rank gives nothing, as crafting does and as quick craft opens. The Hunter's "
        "Guild gives none. Completed tasks of those four guilds return to the board at once, so a task can be taken again; guild point "
        "thresholds, points and credits per task and rank gates are unchanged. Research gives Fabrication XP: "
        f"{values['experiment']}% of the chart's XP for the experiment at full effort and {values['completion']}% for completing the job, each "
        f"raised {values['level_bonus']}% per research level reached, set so research XP per real hour stays below at-level crafting."
    )


def cell_text(cell_body: str) -> str:
    text = "".join(_TEXT.findall(cell_body or ""))
    return unescape(text, {"&quot;": '"', "&apos;": "'"})


def row_cells(body: str) -> dict[str, tuple[str, str]]:
    cells = {}
    for match in _CELL.finditer(body or ""):
        cells[match.group("column")] = (match.group("attributes"), match.group("body") or "")
    return cells


def populated(cells: dict[str, tuple[str, str]]) -> bool:
    return any("<v>" in body or "<t" in body for _, body in cells.values())


def build_row(number: int, ordinal: float, text: str) -> str:
    preserve = ' xml:space="preserve"' if text != text.strip() else ""
    return (
        f'<row r="{number}"><c r="A{number}" s="{NUMBER_STYLE}"><v>{ordinal}</v></c>'
        f'<c r="B{number}" s="{TEXT_STYLE}" t="inlineStr"><is><t{preserve}>{escape(text)}</t></is></c></row>'
    )


def update_sheet(xml: str, text: str) -> str:
    start = xml.index("<sheetData>") + len("<sheetData>")
    end = xml.index("</sheetData>")
    rows = list(_ROW.finditer(xml, start, end))

    last_populated = 0
    last_ordinal = 0.0
    for match in rows:
        cells = row_cells(match.group("body"))
        if "B" in cells and cell_text(cells["B"][1]).startswith(MARKER):
            number = int(match.group("number"))
            ordinal = _NUMBER.search(cells["A"][1]).group(1) if "A" in cells and _NUMBER.search(cells["A"][1]) else str(float(last_ordinal + 1))
            replacement = build_row(number, float(ordinal), text)
            return xml[: match.start()] + replacement + xml[match.end() :]

        if populated(cells):
            number = int(match.group("number"))
            if number > last_populated:
                last_populated = number
            if "A" in cells and _NUMBER.search(cells["A"][1]):
                last_ordinal = max(last_ordinal, float(_NUMBER.search(cells["A"][1]).group(1)))

    number = last_populated + 1
    if number > LAST_NUMBERED_ROW:
        raise ProgressionPaceBibleError("The numbered list has run into the rows below it; make room before adding a row")

    row = build_row(number, float(last_ordinal + 1), text)
    following = next((match for match in rows if int(match.group("number")) >= number), None)
    if following is None:
        return xml[:end] + row + xml[end:]

    if int(following.group("number")) == number:
        if populated(row_cells(following.group("body"))):
            raise ProgressionPaceBibleError(f"Row {number} is already in use")
        return xml[: following.start()] + row + xml[following.end() :]

    return xml[: following.start()] + row + xml[following.start() :]


def update_workbook(workbook: Path, check: bool = False) -> bool:
    _, entries = _SHARED.read_entries(workbook)
    paths = _SHARED.sheet_paths(entries)
    if TAB not in paths and TAB.replace("&", "&amp;") not in paths:
        raise ProgressionPaceBibleError(f"The workbook has no '{TAB}' tab")
    path = paths.get(TAB) or paths[TAB.replace("&", "&amp;")]

    xml = entries[path].decode("utf-8")
    updated = update_sheet(xml, row_text(read_values()))
    if updated == xml:
        return False

    if not check:
        _SHARED.write_archive(workbook, {path: updated.encode("utf-8")})

    return True


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    parser.add_argument("--check", action="store_true", help="change nothing; exit 1 when the workbook would change")
    args = parser.parse_args(argv)

    try:
        changed = update_workbook(args.workbook, args.check)
    except (ProgressionPaceBibleError, _SHARED.BibleParameterError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 2

    print(("Would change" if args.check else "Changed") + f" the workbook: {'yes' if changed else 'no'}")
    return 1 if args.check and changed else 0


if __name__ == "__main__":
    sys.exit(main())
