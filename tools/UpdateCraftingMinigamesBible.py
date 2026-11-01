"""Record the crafting-revamp minigames, shared craft rules and perk lines in the Design Bible's System Changes & Migrations tab.

Adds one numbered entry for each of: the Fabricator, the Kitchen, the Line (fishing), Prospecting (harvesting), the Search board
(scavenging), the research Experiment, the rules every item craft ends under (enhancement thresholds, food duration, failure,
quick craft), and the crafting perk lines and styles. Every number in the text is read from the game's source files (the rules
classes, the perk definitions and SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv), so the Bible cannot drift from
the code. The entries for the Circuit, the Plasma Forge, the Reaction, the rank unlocks and the progression pace are written by
their own scripts and are not repeated here.

Edits only the target cells of design/bible/SWLOR Design Bible.xlsx at the zip/XML level and copies every other workbook entry
byte-for-byte. Safe to rerun: an entry is found by its opening words and rewritten in place; a new one goes on the first row
after the last numbered entry. After a workbook merge conflict take the base workbook and run it again.

  python tools/UpdateCraftingMinigamesBible.py [--workbook PATH] [--check] [--show]
"""
from __future__ import annotations

import argparse
import ast
import csv
import hashlib
import operator
import os
import re
import sys
import tempfile
import zipfile
from fractions import Fraction
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import UpdateCraftingProgressionBible as Bible  # noqa: E402  (shared zip/XML helpers)

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_WORKBOOK = Bible.DEFAULT_WORKBOOK
SHEET = "System Changes & Migrations"
FIRST_ENTRY_ROW = 3

SERVER = Path("SWLOR.Game.Server")
CRAFT = SERVER / "Service" / "CraftService"
MINIGAME = CRAFT / "Minigame"
PERKS = SERVER / "Feature" / "PerkDefinition"

# Every source file the entries read, relative to the repository root, by the name the entry builders use.
SOURCE_FILES = {
    "fabricator": MINIGAME / "Fabricator" / "FabricatorRules.cs",
    "fabricator_heads": MINIGAME / "Fabricator" / "FabricatorHead.cs",
    "kitchen": MINIGAME / "Kitchen" / "KitchenTuning.cs",
    "kitchen_axis": MINIGAME / "Kitchen" / "KitchenAxis.cs",
    "kitchen_heat": MINIGAME / "Kitchen" / "KitchenHeat.cs",
    "line": MINIGAME / "Line" / "LineRules.cs",
    "quick_reel": SERVER / "Service" / "FishingService" / "FishingQuickReel.cs",
    "prospecting": MINIGAME / "Prospecting" / "ProspectingRules.cs",
    "prospecting_lifecycle": SERVER / "Service" / "ProspectingService" / "ProspectingLifecycle.cs",
    "milestones": CRAFT / "CraftRankMilestones.cs",
    "search": MINIGAME / "Search" / "SearchRules.cs",
    "search_board": MINIGAME / "Search" / "SearchBoard.cs",
    "quick_search": SERVER / "Service" / "ScavengeService" / "ScavengeQuickSearch.cs",
    "experiment": MINIGAME / "Experiment" / "ExperimentRules.cs",
    "research_grants": CRAFT / "Research" / "ResearchGrantRules.cs",
    "research_outcome": CRAFT / "Research" / "ResearchOutcomeRules.cs",
    "outcome": MINIGAME / "CraftOutcomeRules.cs",
    "quick_craft": MINIGAME / "QuickCraftRules.cs",
    "quick_craft_batch": MINIGAME / "QuickCraftBatch.cs",
    "perk_manifest": SERVER / "Readmes" / "CombatUpgradeBiblePerkManifest.csv",
    "perks_view": SERVER / "Feature" / "GuiDefinition" / "ViewModel" / "PerksViewModel.cs",
    "perks_engineering": PERKS / "EngineeringPerkDefinition.cs",
    "perks_builder": PERKS / "FabricationBuilderPerkDefinition.cs",
    "perks_researcher": PERKS / "FabricationResearcherPerkDefinition.cs",
    "perks_smithery": PERKS / "SmitheryPerkDefinition.cs",
    "perks_chef": PERKS / "AgricultureChefPerkDefinition.cs",
    "perks_angler": PERKS / "AgricultureAnglerPerkDefinition.cs",
    "perks_gathering": PERKS / "GatheringPerkDefinition.cs",
    "forge_perk_rules": MINIGAME / "Forge" / "ForgePerkRules.cs",
    "gathering_styles": MINIGAME / "Gathering" / "GatheringStyles.cs",
}

# --------------------------------------------------------------------------------------------------------------------------
# Reading the source files
# --------------------------------------------------------------------------------------------------------------------------

_OPERATORS = {
    ast.Add: operator.add,
    ast.Sub: operator.sub,
    ast.Mult: operator.mul,
}


class Source:
    """One C# source file: constants, array initializers and anything else an entry needs, each read or an error."""

    def __init__(self, path: Path):
        self.path = path
        self.text = path.read_text(encoding="utf-8-sig")

    def __contains__(self, fragment: str) -> bool:
        return fragment in self.text

    def const(self, name: str):
        """The value of `const <type> name = <expression>;`. An integer stays an integer; `/` on integers divides as C# does."""
        match = re.search(rf"\bconst\s+(?:double|int|float|long)\s+{re.escape(name)}\s*=\s*([^;]+);", self.text)
        if not match:
            raise ValueError(f"{self.path.name} has no numeric constant '{name}'")
        return self._evaluate(match.group(1))

    def find(self, pattern: str, what: str) -> re.Match:
        match = re.search(pattern, self.text, re.S)
        if not match:
            raise ValueError(f"{self.path.name}: could not find {what} (pattern {pattern!r})")
        return match

    def find_int(self, pattern: str, what: str) -> int:
        return int(self.find(pattern, what).group(1))

    def int_array(self, name: str) -> list[int]:
        body = self.find(rf"\b{re.escape(name)}\s*=\s*(?:new\[\]\s*)?\{{([^}}]*)\}}\s*;", f"the array {name}").group(1)
        return [int(value) for value in re.findall(r"\d+", body)]

    def steps(self, name: str) -> list[tuple[int, int]]:
        body = self.find(rf"\b{re.escape(name)}\s*=\s*\{{(.*?)\}}\s*;", f"the steps {name}").group(1)
        pairs = [(int(rank), int(value)) for rank, value in re.findall(r"\((\d+),\s*(\d+)\)", body)]
        if not pairs:
            raise ValueError(f"{self.path.name}: {name} holds no (rank, value) steps")
        return pairs

    def enum_names(self, enum: str) -> list[str]:
        body = self.find(rf"\benum\s+{re.escape(enum)}\s*\{{([^}}]*)\}}", f"the enum {enum}").group(1)
        body = re.sub(r"//[^\n]*", "", body)
        return [re.match(r"\s*([A-Za-z]\w*)", item).group(1) for item in body.split(",") if item.strip()]

    def _evaluate(self, expression: str):
        cleaned = re.sub(r"(?<=[0-9.])[fFdDmM]\b", "", expression.strip())
        return self._walk(ast.parse(cleaned, mode="eval").body)

    def _walk(self, node):
        if isinstance(node, ast.Constant) and isinstance(node.value, (int, float)) and not isinstance(node.value, bool):
            return node.value
        if isinstance(node, ast.Name):
            return self.const(node.id)
        if isinstance(node, ast.UnaryOp) and isinstance(node.op, ast.USub):
            return -self._walk(node.operand)
        if isinstance(node, ast.BinOp):
            left, right = self._walk(node.left), self._walk(node.right)
            if isinstance(node.op, ast.Div):
                return left // right if isinstance(left, int) and isinstance(right, int) else left / right
            if type(node.op) in _OPERATORS:
                return _OPERATORS[type(node.op)](left, right)
        raise ValueError(f"{self.path.name}: cannot evaluate {ast.dump(node)}")


def load_sources(root: Path) -> dict[str, Source]:
    return {key: Source(root / relative) for key, relative in SOURCE_FILES.items() if key != "perk_manifest"}


def load_manifest(root: Path) -> list[dict[str, str]]:
    with (root / SOURCE_FILES["perk_manifest"]).open(encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


# --------------------------------------------------------------------------------------------------------------------------
# Words and numbers
# --------------------------------------------------------------------------------------------------------------------------

def number(value) -> str:
    """5 for 5.0, 2.4 for 2.40, 8.55 for 8.55."""
    if isinstance(value, float) and value == int(value):
        value = int(value)
    return str(value) if isinstance(value, int) else f"{value:.4f}".rstrip("0").rstrip(".")


def percent(fraction) -> str:
    return number(round(fraction * 100, 4)) + "%"


def join_and(items) -> str:
    items = [item for item in items if item]
    if len(items) <= 1:
        return "".join(items)
    return ", ".join(items[:-1]) + " and " + items[-1]


def join_or(items) -> str:
    items = [item for item in items if item]
    if len(items) <= 1:
        return "".join(items)
    return ", ".join(items[:-1]) + " or " + items[-1]


def counted(value, singular: str, plural: str | None = None) -> str:
    return f"{number(value)} {singular if value == 1 else (plural or singular + 's')}"


def charges(value: int) -> str:
    return "no harvester charge" if value == 0 else counted(value, "charge")


def share(denominator: int) -> str:
    return {2: "half", 4: "a quarter"}.get(denominator, f"1/{denominator}")


def multiplier_text(bands: list[tuple[str, float]], noun: str) -> str:
    """'2.4 at recipe levels 1-10, ... and unchanged at 41 and up': a multiplier of 1 is shown as unchanged."""
    paid = [f"{number(value)} at {noun} {label}" for label, value in bands if value != 1]
    unchanged = [label for label, value in bands if value == 1]
    if not paid:
        return "nothing: it is 1 in every band"
    if unchanged:
        paid.append(f"unchanged at {join_and(unchanged)}")
    return join_and(paid)


def read_bands(source: Source, specs) -> list[tuple[str, float]]:
    return [(label, float(source.const(name))) for name, label in specs]


def rank_schedule(steps: list[tuple[int, int]], format_value, skill: str = "Gathering") -> str:
    """'29% at Gathering 0, 32% at 10 and ...'; one step reads as the same at every rank."""
    steps = sorted(steps)
    if len(steps) == 1:
        return f"{format_value(steps[0][1])} at every {skill} rank"
    parts = [f"{format_value(value)} at {skill} {rank}" if index == 0 else f"{format_value(value)} at {rank}"
             for index, (rank, value) in enumerate(steps)]
    return join_and(parts)


def slash_list(values) -> str:
    return "/".join(number(value) for value in values)


# --------------------------------------------------------------------------------------------------------------------------
# The entries
# --------------------------------------------------------------------------------------------------------------------------

BAND_LABELS = ("1-10", "11-20", "21-30", "31-40", "41 and up")
XP_GATE = "so XP per hour is at least the old crafting window's in every recipe band, gear tier and enhancement count"

FABRICATOR_OPENING = "Fabrication crafts use the Fabricator minigame."
KITCHEN_OPENING = "Agriculture cooking uses the Kitchen minigame."
FISHING_OPENING = "Fishing now gives each bite a choice between Quick Reel and the Line fight."
PROSPECTING_OPENING = "Harvesting now opens a Prospecting session on the node."
SEARCH_OPENING = "Scavenging now gives each site a choice between Quick Search and the Search board."
RESEARCH_OPENING = "Research now starts every level with a short Experiment."
SHARED_OPENING = "Every item craft ends under the same rules, whichever minigame played it."
PERK_LINES_OPENING = "Each crafting skill now has two perk lines and a choice of styles."


def recipe_bands(source: Source, names) -> list[tuple[str, float]]:
    return [(label, float(source.const(name))) for label, name in zip(BAND_LABELS, names)]


def fabricator_entry(src: dict[str, Source]) -> str:
    rules, heads = src["fabricator"], src["fabricator_heads"]
    parts = heads.enum_names("SubAssembly")
    tools = heads.enum_names("FabricatorHead")
    if len(parts) != 3 or len(tools) != 3:
        raise ValueError("FabricatorHead.cs: the Fabricator is expected to have three sub-assemblies and three heads")
    bands = recipe_bands(rules, ("XpMultiplierLevels1To10", "XpMultiplierLevels11To20", "XpMultiplierLevels21To30",
                                 "XpMultiplierLevels31To40", "XpMultiplierLevels41Up"))
    worked = ", ".join(f"the {part} by the {tool}" for part, tool in zip(parts, tools))
    return (
        f"{FABRICATOR_OPENING} The piece is built as three sub-assemblies, {join_and([f"the {part}" for part in parts])}, each worked by its own tool head"
        f" ({worked}), and one head is loaded at a time. Run builds a sub-assembly, at"
        f" {percent(rules.const('OffHeadFactor'))} effect when another head is loaded; Precision Pass raises the quality of the"
        " loaded head's sub-assembly; Retool changes heads for CP; Service restores"
        f" {number(rules.const('ServiceRestore'))} Machine Condition for CP and unlocks at Fabrication rank"
        f" {number(rules.const('ServiceRank'))}; Assemble finishes the piece once all three are complete and caps its quality at"
        f" the weakest sub-assembly plus {number(rules.const('AssembleCapPoints'))} points. The piece breaks if Machine Condition"
        " reaches 0. Enhancements make the piece bigger: it needs more Runs and Precision Passes and starts with more Machine"
        " Condition. Every craft's base XP, for a finished item and a failure alike, is multiplied by"
        f" {multiplier_text(bands, 'recipe levels')}, {XP_GATE}.")


def kitchen_entry(src: dict[str, Source]) -> str:
    tuning = src["kitchen"]
    flavors = src["kitchen_axis"].enum_names("KitchenAxis")
    heats = src["kitchen_heat"].enum_names("KitchenHeat")
    bands = recipe_bands(tuning, ("XpMultiplierLevels1To10", "XpMultiplierLevels11To20", "XpMultiplierLevels21To30",
                                  "XpMultiplierLevels31To40", "XpMultiplierLevels41Up"))
    return (
        f"{KITCHEN_OPENING} The cook steers a dish's flavors, {join_and(flavors)}, into the recipe's target zone"
        f" while it cooks: Add puts ingredients in the pot, the heat ({join_or(heats)}) sets how fast Doneness builds and how far"
        " the flavor drifts, and Freshness falls every turn. The dish is finished when Doneness is full and ruined if Freshness"
        " reaches 0, and its quality is 100% inside the perfect distance of the target and falls to 0% at the outer distance."
        f" Enhancements add {percent(tuning.const('EnhancementDonenessPerPenalty'))} of their progress penalty to the Doneness the"
        " dish needs. Every craft's base XP, for a finished dish and a failure alike, is multiplied by"
        f" {multiplier_text(bands, 'recipe levels')}, {XP_GATE}.")


def fishing_entry(src: dict[str, Source]) -> str:
    line, reel = src["line"], src["quick_reel"]
    bands = read_bands(line, (
        ("XpMultiplierLevels0To10", "0-10"),
        ("XpMultiplierLevels11To20", "11-20"),
        ("XpMultiplierLevels21To30", "21-30"),
        ("XpMultiplierLevels31To40", "31-40"),
        ("XpMultiplierLevels41Up", "41 and up")))
    willpower = reel.const("ReferenceWillpowerScore")
    at_own_level = reel.const("BaseChance") + willpower * reel.const("ChancePerWillpowerPoint")
    turn_seconds = line.const("TurnSeconds")
    full_turns = line.const("XpFullTurns")
    return (
        f"{FISHING_OPENING} The bite spends the bait and one of the fishing point's bites and pays Agriculture XP at once, caught"
        " or not, exactly as the old catch roll did. Quick Reel, offered only before the angler's first move, lands the fish at"
        f" once by that old catch roll, which no longer reads Willpower and rolls as a score of {number(willpower)} would:"
        f" {number(at_own_level)}% at the fish's own level, {number(reel.const('ChancePerRank'))} points more for each Agriculture"
        f" rank above the fish's level and {number(reel.const('ChancePerRank'))} fewer for each below, kept between 0 and"
        f" {number(reel.const('MaximumChance'))}%. It pays no XP beyond the bite's. The Line fight lands the fish only if the angler"
        " reels it in (Distance to 0) before the line snaps (Tension at its maximum) or the fish escapes (Distance at its maximum):"
        " each turn the fish announces a move and the angler answers with Set Hook (first turn only), Reel, Give Line or Hold."
        f" The first move is free and each later one waits {number(turn_seconds)} seconds. When the fight ends on its own"
        " (landed, line snapped or fish escaped) it pays the rest of the XP, in proportion to how long it lasted up to"
        f" {number(turn_seconds * full_turns)} seconds ({number(full_turns)} turns), so a full fight pays the bite's XP times"
        f" {multiplier_text(bands, 'fish levels')}. A fight the angler leaves (closing the window, moving, putting the rod away,"
        " entering combat or dying) loses the fish and the rest of that XP.")


def prospecting_entry(src: dict[str, Source]) -> str:
    rules, lifecycle, milestones = src["prospecting"], src["prospecting_lifecycle"], src["milestones"]
    yield_ranks = milestones.int_array("ProspectingYieldRanks")
    survey_ranks = milestones.int_array("ProspectingSurveyRanks")

    def units(method: str) -> tuple[int, int]:
        body = rules.find(rf"{method}\(int yieldTier\).*?return new\[\]\s*\{{(.*?)\}};", f"the yield of {method}").group(1)
        counts = [int(value) for value in re.findall(r"\((\d+),", body)]
        return min(counts), max(counts)

    strike, heavy = units("StrikeYield"), units("HeavyStrikeYield")
    strike_shift, heavy_shift = rules.const("StrikeYieldShiftPercent"), rules.const("HeavyStrikeYieldShiftPercent")
    shift = (f"{number(strike_shift)} points" if strike_shift == heavy_shift
             else f"{number(strike_shift)} points of a Strike's chance and {number(heavy_shift)} of a Heavy Strike's")
    xp_bands = read_bands(rules, (
        ("XpMultiplierVeinLevel5", "5"),
        ("XpMultiplierVeinLevel15", "15"),
        ("XpMultiplierVeinLevel25", "25"),
        ("XpMultiplierVeinLevel35", "35"),
        ("XpMultiplierVeinLevel45", "45")))
    if all(value == 1 for _, value in xp_bands):
        xp = ("Each unit gathered pays the same Gathering XP a harvest always paid, by the vein's level against the rank (the"
              " session multiplier is 1 in every vein band), and Quick Harvest pays it too.")
    else:
        xp = ("Each unit gathered pays the Gathering XP a harvest always paid, by the vein's level against the rank, times"
              f" {multiplier_text(xp_bands, 'vein levels up to')}; Quick Harvest pays the old XP.")
    return (
        f"{PROSPECTING_OPENING} A node holds 1 to {number(rules.const('PoolDie'))} units of ore or wood (its Yield), a Stability of"
        f" {number(rules.const('MaxStability'))} and 1 to {number(rules.const('MaxSeams'))} hidden seams that each lock up one"
        " unit; when Stability reaches 0 the node collapses and what is left in it is lost. Survey reveals hidden seams"
        f" ({charges(rules.const('SurveyCharges'))}, {number(rules.const('SurveyStability'))} Stability,"
        f" {number(rules.const('SurveySeconds'))} seconds); Strike takes {strike[0]} or {strike[1]} units"
        f" ({charges(rules.const('StrikeCharges'))}, {number(rules.const('StrikeStability'))} Stability,"
        f" {number(rules.const('StrikeSeconds'))} seconds), the loose units first and breaking into a seam only when none is left;"
        f" Heavy Strike takes {heavy[0]} to {heavy[1]} units ({charges(rules.const('HeavyStrikeCharges'))},"
        f" {number(rules.const('HeavyStrikeStability'))} Stability, {number(rules.const('HeavyStrikeSeconds'))} seconds) and has a"
        f" {number(rules.const('ShatterPercent'))}% chance to shatter an unsurveyed gem pocket into plain ore; Extract opens a"
        f" found seam ({charges(rules.const('ExtractCharges'))}, {number(rules.const('ExtractStability'))} Stability,"
        f" {number(rules.const('ExtractSeconds'))} seconds): a rich seam gives its unit and a"
        f" {number(rules.const('RichSeamBonusPercent'))}% chance of one extra ore roll, and a gem pocket gives a gem in place of"
        " its unit; Leave stops and leaves the rest for the next harvester. Gathering rank replaces Might and Perception here: a"
        f" Strike and a Heavy Strike take more units at Gathering {join_and([number(rank) for rank in yield_ranks])} (each tier"
        f" moves {shift} of chance to the largest yield), and a Survey reveals one more seam at Gathering"
        f" {join_and([number(rank) for rank in survey_ranks])}. Everything gathered is kept however the session ends, only one"
        " harvester works a node at a time, and a window left idle for"
        f" {number(lifecycle.const('IdleSeconds'))} seconds lets the node go. {xp} Quick Harvest, offered only before the first"
        " action, takes everything the node has left with no seams, one unit and one charge every"
        f" {number(rules.const('HarvestSeconds'))} seconds, at the old yield and the old pace.")


def search_entry(src: dict[str, Source]) -> str:
    rules, board, milestones, quick = src["search"], src["search_board"], src["milestones"], src["quick_search"]
    searches = milestones.steps("SearchesSteps")
    promising_spots = milestones.steps("PromisingSpotSteps")
    promising_chance = milestones.steps("PromisingChanceSteps")
    if len(searches) != 1 or len(promising_spots) != 1:
        raise ValueError("CraftRankMilestones.cs: the Search text expects one search count and one promising-spot count")
    bands = [(label, float(rules.const(name))) for name, label in (
        ("XpMultiplierLevel1", "1"), ("XpMultiplierLevel2", "2"), ("XpMultiplierLevel3", "3"),
        ("XpMultiplierLevel4", "4"), ("XpMultiplierLevel5", "5 and up"))]
    xp_factors = ", ".join(number(value) for _, value in bands[:-1]) + f" and {number(bands[-1][1])}"
    xp_levels = ", ".join(label for label, _ in bands[:-1]) + f" and {bands[-1][0]}"
    miss = share(rules.find_int(r"deltaXp / (\d+)", "the XP of a missed attempt"))

    # Quick Search's second search: the old Hard Look level (the Gathering tier) times a fixed number of percent.
    per_level = quick.find_int(r"hardLookLevel \* (\d+)", "Quick Search's second-search chance per Hard Look level")
    milestones.find(r"Math\.Min\(MaxTier, Math\.Max\(0, rank\) / RanksPerTier \+ 1\)", "the tier formula")
    top_tier = milestones.const("MaxTier")
    top_rank = (top_tier - 1) * milestones.const("RanksPerTier")
    first_second, last_second = per_level, per_level * top_tier
    return (
        f"{SEARCH_OPENING} The board is {number(board.const('Spots'))} spots, {number(promising_spots[0][1])} of them promising"
        " and unseen: a Search finds something"
        f" {number(rules.const('OrdinaryChance'))}% of the time on ordinary ground and, on promising ground,"
        f" {rank_schedule(promising_chance, lambda v: number(v) + '%')}. Every action costs one of the board's"
        f" {number(searches[0][1])} searches (the same at every Gathering rank), and the board ends when they are spent. Search searches a spot once; Dig"
        " Deeper searches a searched spot again for a rare item"
        f" ({number(rules.const('DigFindChance'))}% to find) and risks ruining it ({number(rules.const('DigRiskBase'))}% on the"
        f" first dig, {number(rules.const('DigRiskStep'))} points more for each dig before, up to"
        f" {number(rules.const('DigRiskMaximum'))}%); Sift shows whether the neighbouring spots are promising and finds nothing."
        f" The first action is free and each later one waits {number(rules.const('ActionSeconds'))} seconds. Every action pays"
        f" Gathering XP: the site's XP when a Search or Dig Deeper finds something and {miss} of it when nothing is found (a Sift"
        f" always pays that), times {xp_factors} at site levels {xp_levels}. Quick Search, offered only before the first action,"
        " searches the whole site at once by the old rules: one search and a chance of a second of"
        f" {first_second}% at Gathering 0 rising {per_level} points every {number(milestones.const('RanksPerTier'))} ranks to"
        f" {last_second}% at Gathering {top_rank}, each search rolled against a DC of {number(quick.const('BaseDc'))} with"
        f" Perception no longer read (a modifier of {number(quick.const('ReferencePerceptionModifier'))}), the old XP, no Dig"
        f" Deeper, and {number(rules.const('QuickSearchSeconds'))} seconds for the visit.")


def research_entry(src: dict[str, Source]) -> str:
    rules, grants, outcome = src["experiment"], src["research_grants"], src["research_outcome"]
    random_levels = [int(value) for value in re.findall(r"\d+", grants.find(
        r"IsRandomLevel\(int level\)\s*=>\s*level is ([\d ]+(?:or [\d ]+)*);", "the random research levels").group(1))]
    weights = grants.int_array("StandardWeights")
    licensed = Fraction(weights[0], sum(weights))
    ceilings = grants.int_array("ExtraBonusCeilings")
    sides = grants.const("ExtraBonusSides")
    extra_low, extra_high = (ceilings[0] + 1) * 100 / sides, (ceilings[-1] + 1) * 100 / sides
    return (
        f"{RESEARCH_OPENING} The job's credits are spent when it starts and the job then runs in real time as before. The"
        f" experiment has {number(rules.const('Trials'))} trials. Test gathers evidence, with a"
        f" {number(rules.const('BaseTestChancePercent'))}% chance for a researcher at the lowest Fabrication rank that can"
        " research the recipe plus one point for each rank above its research tier, up to +"
        f"{number(rules.const('RankChanceBonusMaximum'))}, kept between {number(rules.const('MinimumTestChancePercent'))}% and"
        f" {number(rules.const('MaximumTestChancePercent'))}%; Refine Method costs {number(rules.const('RefineCp'))} Fabrication CP"
        f" and adds {number(rules.const('RefineChanceBonusPercent'))} points to the next Test; Analyze spends spare evidence to cut"
        f" the job's time, {number(rules.const('TimeCutPercentPerEvidence'))}% for each piece up to"
        f" {number(rules.const('TimeCutCapPercent'))}%; Conclude ends the experiment. A Test or an Analyze uses one trial. At"
        f" research levels {join_and([number(level) for level in random_levels])} the blueprint bonus is rolled when the"
        f" experiment opens: licensed runs about {licensed.numerator} times in {licensed.denominator} (up to"
        f" +{number(grants.const('LicensedRunsMaximum'))}), as before, and then the grant is fixed; otherwise it is a credit"
        " reduction or a time reduction that the evidence decides, the candidate with the most evidence winning and more evidence"
        f" paying more, up to {number(grants.const('ReductionMaximum'))}% at"
        f" {number(outcome.const('EvidenceForMaximum'))} pieces and never beyond the old d{number(grants.const('ReductionMaximum'))}."
        " With no evidence on either, the old roll decides when the job completes. At every other level the grant is fixed and"
        " all the evidence goes to a shorter job. Each level also has a small chance of an extra guaranteed bonus,"
        f" {number(extra_low)}% at level 1 rising to {number(extra_high)}% at level {len(ceilings)}, rolled once when the"
        " experiment opens and not changed by evidence. Closing the window, walking away or logging out ends the experiment with"
        " the evidence gathered so far, and a job with an experiment cannot be cancelled. The Fabrication XP research gives is in"
        " the crafting progression pace entry.")


def shared_entry(src: dict[str, Source]) -> str:
    outcome, quick, batch = src["outcome"], src["quick_craft"], src["quick_craft_batch"]
    thresholds = [outcome.const("FirstSlotQualityThresholdPercent"), outcome.const("SecondSlotQualityThresholdPercent"),
                  outcome.const("ThirdSlotQualityThresholdPercent")]
    food_seconds = outcome.const("FullQualityFoodDurationUnits") * outcome.const("FoodDurationSecondsPerUnit")
    minutes = food_seconds / 60
    return (
        f"{SHARED_OPENING} An enhancement transfers when the finished item's quality reaches its slot's threshold,"
        f" {number(thresholds[0])}% for the first slot, {number(thresholds[1])}% for the second and {number(thresholds[2])}% for the"
        " third and any later slot, instead of a chance rolled for each enhancement. A crafted food's bonus duration rises"
        f" evenly with its quality, from nothing at 0% to {counted(minutes, 'minute')} at 100%, where it used to pay only at full"
        f" quality. A failed craft, such as a piece that breaks, loses each component stack and each enhancement on a"
        f" {number(outcome.const('ChanceToLoseItemPercent'))}% roll and pays {percent(outcome.const('FailureXpFraction'))} of the XP a"
        " finished item at 0% quality would, with no first-craft bonus. Quick craft opens once the crafter's skill rank is at"
        f" least {number(quick.const('RankMargin'))} above the recipe's level and the recipe has been crafted successfully by hand once; one"
        f" batch makes up to {number(quick.const('MaxBatchSize'))} items, {number(batch.const('SecondsPerItem'))} seconds each,"
        " and makes only the base item, with no enhancements, no blueprint bonuses and no XP.")


# Each crafting skill's perk lines: the line, the perk definition file that holds it and the word that names its minigame there.
SKILL_LINES = (
    ("Engineering", (("Overclocker", "perks_engineering", "Circuit"), ("Technician", "perks_engineering", "Circuit"))),
    ("Fabrication", (("Builder", "perks_builder", "Fabricator"), ("Researcher", "perks_researcher", "Experiment"))),
    ("Smithery", (("Forgemaster", "perks_smithery", "Plasma Forge"), ("Artisan", "perks_smithery", "Plasma Forge"))),
    ("Agriculture", (("Chef", "perks_chef", "Kitchen"), ("Angler", "perks_angler", "Line"))),
    ("Gathering", (("Prospector", "perks_gathering", "Prospecting"), ("Treasure Seeker", "perks_gathering", "Search"))),
)

# The window a toggled style is picked in, and the text of the perk definition that proves it.
STYLE_WINDOWS = {
    "Engineering": ("the Engineering craft window", ("Engineering craft window",)),
    "Smithery": ("the Plasma Forge window", ("Plasma Forge window",)),
    "Gathering": ("the Prospecting or Search window", ('"Prospecting"', '"Search"')),
}


def implemented_lines(manifest: list[dict[str, str]], skill: str) -> list[str]:
    """The perk lines of a skill's Bible tab that still have implemented perks, in tab order."""
    lines: list[str] = []
    for row in manifest:
        if row["Tab"] == skill and row["DevStatus"] == "Implemented" and row["Style"] not in lines:
            lines.append(row["Style"])
    return lines


def line_purchases(manifest: list[dict[str, str]], skill: str, line: str) -> dict[int, float]:
    """The skill rank and SP price of each purchase of a line. The alternatives of a style share one purchase."""
    purchases: dict[int, float] = {}
    for row in manifest:
        if row["Tab"] != skill or row["Style"] != line or row["DevStatus"] != "Implemented":
            continue
        rank = int(re.search(r"(\d+)\s*$", row["SkillRequirements"]).group(1))
        price = float(row["Price"])
        if purchases.setdefault(rank, price) != price:
            raise ValueError(f"{skill} {line}: the purchases at rank {rank} have different SP prices")
    return purchases


def style_perks(src: dict[str, Source], key: str) -> list[dict]:
    """The style perks of a perk definition file: name, the rank they open at and how they are chosen."""
    styles = []
    for block in re.split(r"(?=_builder\.Create\()", src[key].text)[1:]:
        toggled = re.search(r"TriggerRefund\(\w+Styles\.RemoveUnowned\)", block)
        exclusive = re.search(r"RequirementCannotHavePerk\(PerkType\.(\w+)\)", block)
        if not toggled and not exclusive:
            continue
        name = re.search(r'\.Name\("([^"]+)"\)', block)
        requirement = re.search(r"RequirementSkill\(SkillType\.(\w+),\s*([\w.]+)\)", block)
        if not name or not requirement:
            raise ValueError(f"{src[key].path.name}: a style perk has no name or no skill requirement")
        rank = requirement.group(2)
        if not rank.isdigit():
            owner, constant = rank.split(".")
            holder = {"ForgePerkRules": src["forge_perk_rules"], "GatheringStyles": src["gathering_styles"]}.get(owner)
            if holder is None:
                raise ValueError(f"{src[key].path.name}: cannot read the style rank {rank}")
            rank = holder.const(constant)
        styles.append({"name": name.group(1), "rank": int(rank), "mode": "toggled" if toggled else "exclusive"})
    return styles


def perk_lines_entry(src: dict[str, Source], manifest: list[dict[str, str]]) -> str:
    schedules = set()
    skill_items = []
    modes = {}
    ranks = set()
    for skill, lines in SKILL_LINES:
        implemented = implemented_lines(manifest, skill)
        if implemented != [line for line, _, _ in lines]:
            raise ValueError(f"{skill}: the Bible manifest lists the lines {implemented}, the script expects"
                             f" {[line for line, _, _ in lines]}")
        for line, key, minigame in lines:
            if line not in src[key] or minigame not in src[key]:
                raise ValueError(f"{src[key].path.name} does not name the line '{line}' or its minigame '{minigame}'")
            schedules.add(tuple(sorted(line_purchases(manifest, skill, line).items())))

        by_line = {line: [] for line, _, _ in lines}
        skill_modes = set()
        for key in dict.fromkeys(key for _, key, _ in lines):
            for style in style_perks(src, key):
                row = next((r for r in manifest if r["Tab"] == skill and r["PerkName"] == style["name"]), None)
                if row is None or row["Style"] not in by_line:
                    raise ValueError(f"{skill}: the style '{style['name']}' is not a perk of the Bible manifest's lines")
                by_line[row["Style"]].append(style["name"])
                skill_modes.add(style["mode"])
                ranks.add(style["rank"])
        if len(skill_modes) != 1:
            raise ValueError(f"{skill}: its styles are chosen in more than one way: {by_line}")
        modes[skill] = skill_modes.pop()
        expected = 2 if modes[skill] == "exclusive" else 1
        if any(len(names) != expected for names in by_line.values()):
            raise ValueError(f"{skill}: expected {expected} style(s) in each line: {by_line}")
        if modes[skill] == "toggled":
            window, proofs = STYLE_WINDOWS[skill]
            if not all(proof in src[lines[0][1]] for proof in proofs):
                raise ValueError(f"{skill}: the perk definition no longer says the style is picked in {window}")
        skill_items.append((skill, lines, by_line))

    if len(schedules) != 1:
        raise ValueError("the crafting perk lines no longer share one rank and SP schedule")
    if len(ranks) != 1:
        raise ValueError(f"the crafting styles open at different ranks: {sorted(ranks)}")
    schedule = sorted(next(iter(schedules)))
    style_rank = next(iter(ranks))
    if style_rank not in [rank for rank, _ in schedule]:
        raise ValueError("the style rank is not one of the line's purchase ranks")

    def lines_of(lines) -> str:
        if len({minigame for _, _, minigame in lines}) == 1:
            return f"{join_and(line for line, _, _ in lines)} ({lines[0][2]})"
        return join_and(f"{line} ({minigame})" for line, _, minigame in lines)

    lines_text = "; ".join(f"{skill}: {lines_of(lines)}" for skill, lines, _ in skill_items)
    styles_text = "; ".join(
        f"{skill} has " + join_and(f"{join_and(by_line[line])} ({line})" for line, _, _ in lines)
        for skill, lines, by_line in skill_items)
    toggled = [skill for skill, mode in modes.items() if mode == "toggled"]
    exclusive = [skill for skill, mode in modes.items() if mode == "exclusive"]
    windows = [STYLE_WINDOWS[skill][0] for skill in toggled]
    hours = src["perks_view"].find_int(r"DatePerkRefundAvailable\s*=\s*DateTime\.UtcNow\.AddHours\((\d+)\)", "the refund delay")
    src["perks_view"].find(rf"You may only refund one perk per {hours} hour", "the refund text")
    return (
        f"{PERK_LINES_OPENING} {lines_text}. Each line has {len(schedule)} purchases at skill ranks"
        f" {slash_list(rank for rank, _ in schedule)} for {slash_list(price for _, price in schedule)} SP, and the style is the"
        f" rank {style_rank} purchase: {styles_text}. The styles of {join_and(toggled)} are separate purchases, so a"
        f" crafter may own both; one is active at a time and is picked in the craft window ({join_and(windows)}), the pick is"
        " saved and applied again after a death or a relog, and refunding the style or a full rebuild clears it. The styles of"
        f" {join_and(exclusive)} are a choice of two within a line: owning one blocks buying the other, the owned style is always"
        " active, and changing it means refunding it (one perk refund per"
        f" {'hour' if hours == 1 else counted(hours, 'hour')}, which spends a refund token) and buying the other.")


def build_entries(root: Path = ROOT) -> list[tuple[str, str]]:
    """Every entry as (opening words, full text), in the order they are numbered."""
    src = load_sources(root)
    manifest = load_manifest(root)
    entries = [
        (FABRICATOR_OPENING, fabricator_entry(src)),
        (KITCHEN_OPENING, kitchen_entry(src)),
        (FISHING_OPENING, fishing_entry(src)),
        (PROSPECTING_OPENING, prospecting_entry(src)),
        (SEARCH_OPENING, search_entry(src)),
        (RESEARCH_OPENING, research_entry(src)),
        (SHARED_OPENING, shared_entry(src)),
        (PERK_LINES_OPENING, perk_lines_entry(src, manifest)),
    ]
    for opening, text in entries:
        if not text.startswith(opening):
            raise ValueError(f"The entry '{opening}' does not start with its opening words")
        if not text.isascii():
            raise ValueError(f"The entry '{opening}' has a non-ASCII character")
    return entries


# --------------------------------------------------------------------------------------------------------------------------
# Writing the workbook
# --------------------------------------------------------------------------------------------------------------------------

def entry_row(sheet: Bible.Sheet, opening: str) -> tuple[int, int]:
    """The row to write and the entry number: the existing entry's, or the first row after the last numbered entry."""
    last_numbered, last_number = FIRST_ENTRY_ROW - 1, 0
    for row in range(FIRST_ENTRY_ROW, sheet.last_data_row() + 1):
        cells = sheet.row_cells(row)
        if "A" not in cells or not sheet.text_of(cells["A"]).strip():
            continue
        if "B" in cells and sheet.text_of(cells["B"]).startswith(opening):
            return row, int(float(sheet.text_of(cells["A"])))
        last_numbered, last_number = row, int(float(sheet.text_of(cells["A"])))
    return last_numbered + 1, last_number + 1


def update_sheet(sheet: Bible.Sheet, entries: list[tuple[str, str]]) -> None:
    template = sheet.row_cells(FIRST_ENTRY_ROW)
    for opening, text in entries:
        row, entry_number = entry_row(sheet, opening)
        cells = sheet.row_cells(row)  # an existing entry keeps whatever it holds beyond columns A and B
        cells["A"] = Bible.num_cell(f"A{row}", Bible.cell_style(template["A"]), f"{entry_number}.0")
        cells["B"] = Bible.text_cell(f"B{row}", Bible.cell_style(template["B"]), text)
        sheet.write_row(row, cells, after=row - 1)


def update_workbook(path: Path, entries: list[tuple[str, str]], check: bool = False) -> bool:
    """Writes the entries into the workbook. Returns True when the workbook changed (or, with `check`, would change)."""
    with zipfile.ZipFile(path) as source:
        infos = source.infolist()
        original = {info.filename: source.read(info.filename) for info in infos}
        paths = Bible.resolve_sheets(source)
        strings = Bible.read_strings(source)

    entry = paths[SHEET]
    sheet = Bible.Sheet(SHEET, original[entry].decode("utf-8"), strings)
    update_sheet(sheet, entries)
    updated = sheet.xml.encode("utf-8")
    if updated == original[entry]:
        print(f"The {SHEET} tab already records the crafting minigames.")
        return False
    if check:
        print("The workbook would change.")
        return True

    handle, temp_name = tempfile.mkstemp(suffix=".xlsx", dir=str(path.parent))
    os.close(handle)
    try:
        with zipfile.ZipFile(temp_name, "w", zipfile.ZIP_DEFLATED) as target:
            for info in infos:
                clone = zipfile.ZipInfo(info.filename, info.date_time)
                clone.compress_type = zipfile.ZIP_DEFLATED
                clone.external_attr = info.external_attr
                target.writestr(clone, updated if info.filename == entry else original[info.filename])
        with zipfile.ZipFile(temp_name) as written:
            assert written.testzip() is None
            before = {name: hashlib.sha256(data).hexdigest() for name, data in original.items() if name != entry}
            after = Bible.entry_hashes(written, {entry})
            if before != after:
                raise RuntimeError("Untouched workbook entries changed: " +
                                   ", ".join(sorted(k for k in before if before[k] != after.get(k))))
        os.replace(temp_name, path)
    finally:
        if os.path.exists(temp_name):
            os.remove(temp_name)

    print(f"Updated {len(entries)} entries on the {SHEET} tab of {path.name}; every other entry is byte-identical.")
    return True


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--workbook", type=Path, default=DEFAULT_WORKBOOK)
    parser.add_argument("--root", type=Path, default=ROOT, help="repository root to read the source files from")
    parser.add_argument("--check", action="store_true", help="change nothing; exit 1 when the workbook would change")
    parser.add_argument("--show", action="store_true", help="print the entries and change nothing")
    args = parser.parse_args()
    entries = build_entries(args.root)
    if args.show:
        for _, text in entries:
            print(f"{text}\n")
        return
    changed = update_workbook(args.workbook, entries, args.check)
    sys.exit(1 if args.check and changed else 0)


if __name__ == "__main__":
    main()
