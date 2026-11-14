"""Space revamp derelict boarding (plan section 28, W15c): boardable and contested derelicts, the derelict interiors,
contested waves, reinforcements and droid salvage teams.

usage: python derelicts.py            (writes the generated C# derelict tuning)
       python derelicts.py --check    (exit 1 when the generated file is out of date)
       python derelicts.py --sim      (prints boarding, contested and salvage team payouts against space combat)

Every derelict boarding number lives here. bible_tabs.py calls derelicts_tab(Sheet) to write the Derelict Boarding
(Revamp) tab. Draft numbers; tune in testing.

Sources:
- Derelict sites: scanning.py (derelict weight 20, a Large Elite wreck with defenses, the site lifetime and the combat
  squads the contested waves reuse). Wild Space derelicts are Wild Space's derelict sites (wildspace.py, tier 5.5).
- Ground enemies: existing Normal creatures of the matching ground level band (the Bible's World NPCs tab: tier 1 is
  levels 1-10, tier 2 11-20, tier 3 21-30, tier 4 31-40, tier 5 41-50, tier 5.5 51+), with their own LOOT_TABLE_n
  locals (Module/utc) and the ground loot tables (Feature/LootTableDefinition).
- Slicing terminals and lockboxes: the ground's slice_term_1-5 placeables and lockbox_t1-t5 items, unchanged.
- Rates: no ground dungeon rate exists in these tools, so the space combat rate of the tier (scanning.combat_rate,
  trade.py's baseline with space lockboxes; Wild Space: wildspace.combat_rate) is the proxy the targets use.
- Droid salvage teams: droids.py's SalvageTeamSpeedPercent and SalvageTeamYieldPercent hooks (+0.3% and +0.25% per
  Industry).
"""
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import droids  # noqa: E402
import scanning  # noqa: E402
import trade  # noqa: E402
import wildspace  # noqa: E402

REPO = Path(__file__).resolve().parents[2]

# Content tiers: the six space tiers and Wild Space (tier 5.5), index 7. Space tier 6 (Korriban) and Wild Space use the
# ground's tier 5.5 roster; slicing terminals and lockboxes stop at tier 5, as space slicing does.
TIERS = [1, 2, 3, 4, 5, 6, 7]
WILD_SPACE_INDEX = 7
TIER_LABELS = {1: "1", 2: "2", 3: "3", 4: "4", 5: "5", 6: "6", 7: "5.5 (Wild Space)"}


def slicing_tier(tier):
    return min(tier, 5)


# ---------------------------------------------------------------------------------------------------------------
# Finding: a share of derelict signatures can be boarded, and a share of those is contested
# ---------------------------------------------------------------------------------------------------------------
BOARDABLE_PERCENT = 50   # of derelict signatures (scan sites and Wild Space); faction agent leads never are
CONTESTED_PERCENT = 40   # of boardable derelicts

# ---------------------------------------------------------------------------------------------------------------
# Boarding (the W8c docking flow: /board on the targeted derelict wreck)
# ---------------------------------------------------------------------------------------------------------------
DOCK_RANGE = 30          # ShipDockingRules.DockRange: the command range, inside weapon range
DOCK_CAPACITY = 2        # ships of one party docked at a derelict at once
BOARDING_HOLD_MINUTES = 40  # a boarded site stays open while boarded, at most this long after the first ship docked

# Templates: existing starship interiors reused unchanged and dressed as wrecks when instanced (decision: no new areas).
# W11o added four more hull layouts, the roomy ones (30 or more placeables 10 m or more from the entrance, so the
# placement fallback beside placeables has room); the cramped Small layouts (Panther, Hound, Condor, Striker, Fighter,
# Basilisk, the Light Freighter) stay out.
TEMPLATES = [
    ("ship_merchant_z", "Derelict Merchant Freighter"),
    ("ship_throne_z", "Derelict Throne Yacht"),
    ("ship_consul_z", "Derelict Consular Cruiser"),
    ("ship_mule_z", "Derelict Mule Hauler"),
    ("ship_falchion_z", "Derelict Falchion Runner"),
    ("ship_saber_z", "Derelict Saber Prospector"),
    ("ship_corv_v", "Derelict Corvette"),
]
DRESSING = ["swd2_debr001", "swd3_dbpil02", "swd_jkpl001", "swd_jkpl002", "zep_smokesm"]
DRESSING_COUNT = 10
INTERIOR_ENEMIES = 8
INTERIOR_TERMINALS = 2
ENTRANCE_CLEAR_METERS = 8      # enemies, terminals and the strongbox spawn at least this far from the airlock
STRONGBOX_RESREF = "spc_sig_cache"  # the cargo crate placeable the site caches use
STRONGBOX_LOCKBOXES = 1        # ground lockboxes of the slicing tier in the strongbox, one more per wave cleared

# Ground enemies by content tier (Normal creatures of the band, their own loot): CZ-220's mynocks and malfunctioning droids;
# Mandalorian salvagers and Vellen flesheaters; the Abandoned Station's reanimated crew and colicoids; Nar Shaddaa's
# scavenger droids and pirates; the Hutlar Droid Foundry's droids (tier 5 the intake and assembly lines, 5.5 the core).
ROSTERS = {
    1: ["mynock", "malspiderdroid", "malsecdroid", "czcryo_mynock"],
    2: ["man_ranger_1", "man_hunter", "man_warrior_1", "v_flesheater"],
    3: ["abs_ztech", "abs_zresearch", "abs_colicoid", "abs_colstalk", "abs_zsubject"],
    4: ["nar_scavenger", "nar_pirate", "nar_thief", "nar_redblade"],
    5: ["fdy_hauler", "fdy_sentry", "fdy_welder", "fdy_linedef", "fdy_riveter", "fdy_reclaim"],
    6: ["fdy_wardroid", "fdy_siege", "fdy_hunter"],
    7: ["fdy_wardroid", "fdy_siege", "fdy_hunter"],
}

# ---------------------------------------------------------------------------------------------------------------
# Contested derelicts: waves outside, the loot step-up, reinforcements inside
# ---------------------------------------------------------------------------------------------------------------
WAVE_FIRST_SECONDS = 30        # after the first ship docks
WAVE_GAP_SECONDS = 30          # after a wave is cleared
MAXIMUM_WAVES = 3
WAVE_RING_METERS = 25          # the wave arrives on a ring around the derelict
CONTESTED_PREMIUM = 1.30       # a mixed group's rate per player on a contested derelict, of the tier's combat rate
WAVE_STEP_MINIMUM = 5          # every wave cleared raises the strongbox at least this much (percent); W11e: was 10, which
                               # held the T3-T4 groups at 138-142% of combat, above the 100-140% band (now 131-134%)
WAVE_STEP_MAXIMUM = 60
DEFENDER_SHARE_PERCENT = 50    # of the strongbox (with its step-up) each party defender within range gets when it opens
DEFENSE_RADIUS = 60            # a player ship this close to the derelict defends it
REINFORCEMENT_COUNT = 2        # enemy boarders per breach, from the tier's roster
REINFORCEMENT_SECONDS = 60     # at most one breach this often while a wave finds no defender
MAXIMUM_REINFORCEMENTS = 6     # per boarding

# ---------------------------------------------------------------------------------------------------------------
# Droid salvage teams (no boarding: the ship holds beside the derelict and its posted droids strip it)
# ---------------------------------------------------------------------------------------------------------------
SALVAGE_BASE_SECONDS = 480         # the timer of a team with no Industry; Industry shortens it (SalvageTeamSpeedPercent)
SALVAGE_TETHER_METERS = DEFENSE_RADIUS  # the ship must stay this close or the team comes back with nothing
SALVAGE_INDUSTRY_CAP = droids.MAXIMUM_INDUSTRY  # a team's Industry: its posted droids' summed, capped at one best droid
SALVAGE_REFERENCE_INDUSTRY = 80    # a tier 4 all-industrial droid, the model's reference team
SALVAGE_SHARE = 0.66               # of the boarding rate, at the reference team (the 60-70% target)
SALVAGE_SCRAP = 6                  # Ship Scrap of the tier, raised by the team's yield percent
SALVAGE_ITEM_PULLS = 1             # pulls from the site's elite table (SignatureSiteRules.CacheLootTable)

# ---------------------------------------------------------------------------------------------------------------
# Payout model
# ---------------------------------------------------------------------------------------------------------------
BOARDING_PREMIUM = 1.15        # of the tier's combat rate (proxy for the ground dungeon rate), inside 110-120%
DOCK_MINUTES = 0.5             # lining up, docking and walking aboard
GROUND_KILL_MINUTES = 0.75     # one Normal ground fight, assumed
WALK_MINUTES = 2.0             # moving through the hulk
STRONGBOX_MINUTES = 0.25
UNDOCK_MINUTES = 0.5
SPACE_KILL_MINUTES = scanning.KILL_MINUTES
MARKDOWN = 0.40
# Slicing terminals and the strongbox's lockboxes pay the ground's slicing rewards (named gear, schematics, tools): like
# rare drops in the tier baselines they are left out of the credit model, and so is the terminals' time (optional).


def combat_rate(tier):
    return wildspace.combat_rate()[2] if tier == WILD_SPACE_INDEX else scanning.combat_rate(tier)


def target_rate(tier):
    return BOARDING_PREMIUM * combat_rate(tier)


_GROUND_TABLES = None


def ground_loot_tables():
    """Every loot table in Feature/LootTableDefinition, parsed as trade.loot_tables parses the space ones."""
    global _GROUND_TABLES
    if _GROUND_TABLES is None:
        tables = {}
        for path in sorted((REPO / "SWLOR.Game.Server" / "Feature" / "LootTableDefinition").glob("*.cs")):
            text = path.read_text(encoding="utf-8")
            for block in re.split(r'_builder\.Create\(', text)[1:]:
                match = re.match(r'"([^"]+)"', block)
                if not match:
                    continue
                rows = []
                for m in re.finditer(r'\.Add(Item|Gold)\(([^)]*)\)', block.split(";")[0]):
                    args = [a.strip().strip('"') for a in m[2].split(",")]
                    named = {a.split(":")[0].strip(): a.split(":")[1].strip() for a in args if ":" in a}
                    args = [a for a in args if ":" not in a]
                    if m[1] == "Gold":
                        rows.append(("gold", int(args[1]), int(args[0]), False))
                    else:
                        quantity = int(named.get("maxQuantity", args[2] if len(args) > 2 else 1))
                        rare = named.get("isRare", args[3] if len(args) > 3 else "false") == "true"
                        rows.append((args[0], int(args[1]), quantity, rare))
                tables[match[1]] = rows
        _GROUND_TABLES = tables
    return _GROUND_TABLES


_KILLS = {}


def ground_kill_value(resref):
    """One ground kill's expected credits: each LOOT_TABLE_n local (table, chance, attempts), rare tables left out."""
    if resref not in _KILLS:
        data = json.loads((REPO / "Module" / "utc" / f"{resref}.utc.json").read_text(encoding="latin-1"))
        tables = ground_loot_tables()
        value = 0.0
        for var in data.get("VarTable", {}).get("value", []):
            if not var["Name"]["value"].startswith("LOOT_TABLE_"):
                continue
            name, chance, attempts = [x.strip() for x in str(var["Value"]["value"]).split(",")]
            rows = tables.get(name, [])
            if rows and not all(r[3] for r in rows) and not name.endswith("_RARES"):
                value += int(chance) / 100 * int(attempts) * trade.expected_value(rows)
        _KILLS[resref] = value
    return _KILLS[resref]


def roster_kill_value(tier):
    roster = ROSTERS[tier]
    return sum(ground_kill_value(r) for r in roster) / len(roster)


def board_minutes():
    return (DOCK_MINUTES + INTERIOR_ENEMIES * GROUND_KILL_MINUTES + WALK_MINUTES + STRONGBOX_MINUTES + UNDOCK_MINUTES)


def board_own_value(tier):
    return INTERIOR_ENEMIES * roster_kill_value(tier)


def strongbox_credits(tier):
    """The strongbox's base credits for each boarder: the boarding time at the target rate, less the kills' own loot."""
    credits = target_rate(tier) / 60 * board_minutes() - board_own_value(tier)
    return max(0, int(25 * round(credits / 25)))


def board_value(tier, waves_cleared=0):
    return board_own_value(tier) + strongbox_credits(tier) * step_multiplier(tier, waves_cleared)


def board_rate(tier):
    return board_value(tier) / board_minutes() * 60


# Contested: the waves' squads (scanning.combat_squad; Rift Raiders in Wild Space) and their kill values.
def wave_squad(tier):
    return scanning.WILD_SPACE_SQUAD if tier == WILD_SPACE_INDEX else scanning.combat_squad(tier)


def wave_kill_value(tier):
    space_tier = 5 if tier == WILD_SPACE_INDEX else tier
    return sum(scanning.kill_value(t) + scanning.lockbox_kill_value(space_tier) for t in wave_squad(tier))


def wave_minutes(tier):
    """One wave from its arrival to the last kill, for one defender."""
    return len(wave_squad(tier)) * SPACE_KILL_MINUTES


def waves_in(minutes, tier):
    """The waves one defender clears before the strongbox opens: the first after WAVE_FIRST_SECONDS, then a gap each."""
    elapsed, cleared = WAVE_FIRST_SECONDS / 60, 0
    while cleared < MAXIMUM_WAVES and elapsed + wave_minutes(tier) <= minutes:
        elapsed += wave_minutes(tier) + WAVE_GAP_SECONDS / 60
        cleared += 1
    return cleared


def expected_waves(tier):
    return waves_in(board_minutes(), tier)


def wave_step_percent(tier):
    """
    The strongbox step per wave cleared, solved so a mixed group of one boarder and one defender earns CONTESTED_PREMIUM
    of the tier's combat rate per player at the waves one defender clears in the boarding time. Rounded to 5.
    """
    waves = expected_waves(tier)
    box = strongbox_credits(tier)
    if waves == 0 or box == 0:
        return WAVE_STEP_MINIMUM
    group_value = 2 * CONTESTED_PREMIUM * combat_rate(tier) / 60 * board_minutes()
    needed = (group_value - board_own_value(tier) - waves * wave_kill_value(tier)) / (box * (1 + DEFENDER_SHARE_PERCENT / 100))
    step = int(5 * round((needed - 1) / waves * 100 / 5))
    return max(WAVE_STEP_MINIMUM, min(WAVE_STEP_MAXIMUM, step))


def step_multiplier(tier, waves_cleared):
    return 1 + wave_step_percent(tier) * min(waves_cleared, MAXIMUM_WAVES) / 100


def contested_rates(tier):
    """
    A mixed group of one boarder and one defender: each one's rate and the group's per player. The strongbox pays every
    party member there when it opens: the boarders in full and each defender within range DEFENDER_SHARE_PERCENT of it.
    """
    minutes = board_minutes()
    waves = expected_waves(tier)
    boarder = board_value(tier, waves) / minutes * 60
    share = strongbox_credits(tier) * step_multiplier(tier, waves) * DEFENDER_SHARE_PERCENT / 100
    defender = (share + waves * wave_kill_value(tier)) / minutes * 60
    return waves, boarder, defender, (boarder + defender) / 2


# Salvage teams: a solo pilot holds off the same waves for the team's timer.
def salvage_team_industry(industry):
    return max(0, min(SALVAGE_INDUSTRY_CAP, industry))


def salvage_speed_percent(industry):
    return round(droids.SALVAGE_TEAM_SPEED_PERCENT_PER_INDUSTRY * salvage_team_industry(industry))


def salvage_yield_percent(industry):
    return round(droids.SALVAGE_TEAM_YIELD_PERCENT_PER_INDUSTRY * salvage_team_industry(industry))


def salvage_seconds(industry):
    return round(SALVAGE_BASE_SECONDS / (1 + salvage_speed_percent(industry) / 100))


def salvage_minutes(industry):
    return DOCK_MINUTES + salvage_seconds(industry) / 60


def salvage_scrap(industry):
    return round(SALVAGE_SCRAP * (1 + salvage_yield_percent(industry) / 100))


def space_tier(tier):
    return 5 if tier == WILD_SPACE_INDEX else tier


def salvage_items_value(tier, industry):
    elite = trade.expected_value(trade.loot_tables()[scanning.WILD_SPACE_CACHE_TABLE]) if tier == WILD_SPACE_INDEX \
        else scanning.elite_pull_value(tier)
    return salvage_scrap(industry) * scanning.scrap_value(space_tier(tier)) + SALVAGE_ITEM_PULLS * elite


def salvage_contested(tier, industry):
    """
    A contested salvage run: the team works only while no wave is out, so each wave pauses its timer. Returns the waves
    the pilot clears and the run's minutes.
    """
    work_left = salvage_seconds(industry) / 60
    elapsed, waves = DOCK_MINUTES, 0
    next_wave = WAVE_FIRST_SECONDS / 60
    worked = 0.0
    while worked < work_left:
        if waves < MAXIMUM_WAVES and worked >= next_wave - 1e-9:
            elapsed += wave_minutes(tier)
            waves += 1
            next_wave = worked + WAVE_GAP_SECONDS / 60
            continue
        step = min(work_left - worked, (next_wave - worked) if waves < MAXIMUM_WAVES else work_left)
        worked += step
        elapsed += step
    return waves, elapsed


def salvage_credits(tier):
    """The team's base credits: on an uncontested derelict the reference team pays SALVAGE_SHARE of the boarding rate."""
    minutes = salvage_minutes(SALVAGE_REFERENCE_INDUSTRY)
    target = SALVAGE_SHARE * board_rate(tier) / 60 * minutes
    rest = target - salvage_items_value(tier, SALVAGE_REFERENCE_INDUSTRY)
    base = rest / (1 + salvage_yield_percent(SALVAGE_REFERENCE_INDUSTRY) / 100)
    return max(0, int(25 * round(base / 25)))


def salvage_paid_credits(tier, industry):
    return round(salvage_credits(tier) * (1 + salvage_yield_percent(industry) / 100))


def salvage_rate(tier, industry, contested=False):
    """A solo pilot's rate: the team's haul, and on a contested derelict the waves the pilot holds off meanwhile."""
    value = salvage_paid_credits(tier, industry) + salvage_items_value(tier, industry)
    minutes = salvage_minutes(industry)
    if contested:
        waves, minutes = salvage_contested(tier, industry)
        value += waves * wave_kill_value(tier)
    return value / minutes * 60


def report():
    print(f"Derelict boarding: {board_minutes():.2f} min aboard ({INTERIOR_ENEMIES} enemies), target {BOARDING_PREMIUM:.0%} "
          "of the tier's space combat rate (proxy for the ground dungeon rate)")
    for t in TIERS:
        c = combat_rate(t)
        waves, boarder, defender, group = contested_rates(t)
        print(f"  T{TIER_LABELS[t]:16s} combat {c:7.0f}/h | board {board_rate(t):7.0f}/h ({100 * board_rate(t) / c:5.1f}%), "
              f"kills {board_own_value(t):6.0f} + strongbox {strongbox_credits(t):5d}")
        print(f"      contested: {waves} waves, boarder {boarder:7.0f}/h, defender {defender:7.0f}/h, group {group:7.0f}/h "
              f"({100 * group / c:5.1f}% of combat, {100 * group / board_rate(t):5.1f}% of boarding)")
        rows = []
        for industry in (0, SALVAGE_REFERENCE_INDUSTRY, 100):
            rows.append(f"I{industry} {salvage_seconds(industry)}s {salvage_rate(t, industry):6.0f}/h "
                        f"({100 * salvage_rate(t, industry) / board_rate(t):4.1f}%)")
        print(f"      salvage team (base {salvage_credits(t)} cr): " + ", ".join(rows))
        sc = salvage_rate(t, SALVAGE_REFERENCE_INDUSTRY, True)
        print(f"      contested salvage team: {sc:6.0f}/h ({100 * sc / group:4.1f}% of the contested group, "
              f"{salvage_contested(t, SALVAGE_REFERENCE_INDUSTRY)[0]} waves, {salvage_contested(t, SALVAGE_REFERENCE_INDUSTRY)[1]:.1f} min); "
              f"step {wave_step_percent(t)}% per wave")


# ---------------------------------------------------------------------------------------------------------------
# Generated C#
# ---------------------------------------------------------------------------------------------------------------
HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/derelicts.py. Do not edit by hand: change derelicts.py and regenerate.
// </auto-generated>
"""


def cs_strings(items):
    return "new[] { " + ", ".join(json.dumps(x) for x in items) + " }"


def cs_ints(values):
    return "{ 0, " + ", ".join(str(v) for v in values) + " }"


def tuning_cs():
    lines = [HEADER.rstrip("\n"), "namespace SWLOR.Game.Server.Service.SpaceService", "{",
             "    /// <summary>",
             "    /// The derelict boarding numbers (plan section 28): which derelicts can be boarded or are contested, the derelict",
             "    /// interiors, contested waves and reinforcements, droid salvage teams and the rewards. Tier arrays are indexed by",
             "    /// content tier: 1-6 the space tiers, 7 Wild Space (tier 5.5); index 0 is unused.",
             "    /// </summary>",
             "    public static class DerelictTuning", "    {",
             f"        public const int BoardablePercent = {BOARDABLE_PERCENT};",
             f"        public const int ContestedPercent = {CONTESTED_PERCENT};",
             f"        public const int WildSpaceTierIndex = {WILD_SPACE_INDEX};",
             f"        public const int DockCapacity = {DOCK_CAPACITY};",
             f"        public const int BoardingHoldMinutes = {BOARDING_HOLD_MINUTES};",
             "",
             "        /// <summary>The interior templates (existing starship interiors) and the names their instances take.</summary>",
             f"        public static readonly string[] TemplateResrefs = {cs_strings([r for r, _ in TEMPLATES])};",
             f"        public static readonly string[] TemplateNames = {cs_strings([n for _, n in TEMPLATES])};",
             f"        public static readonly string[] DressingResrefs = {cs_strings(DRESSING)};",
             f"        public const int DressingCount = {DRESSING_COUNT};",
             f"        public const int InteriorEnemies = {INTERIOR_ENEMIES};",
             f"        public const int InteriorTerminals = {INTERIOR_TERMINALS};",
             f"        public const float EntranceClearMeters = {ENTRANCE_CLEAR_METERS}f;",
             f"        public const string StrongboxResref = {json.dumps(STRONGBOX_RESREF)};",
             f"        public const int StrongboxLockboxes = {STRONGBOX_LOCKBOXES};",
             "",
             "        /// <summary>The ground enemies of a derelict interior, by content tier.</summary>",
             "        public static readonly string[][] Rosters =",
             "        {",
             "            System.Array.Empty<string>(),"]
    lines += [f"            {cs_strings(ROSTERS[t])}," for t in TIERS]
    lines += ["        };", "",
              "        /// <summary>A strongbox's base credits for each boarder, by content tier.</summary>",
              f"        public static readonly int[] StrongboxCredits = {cs_ints(strongbox_credits(t) for t in TIERS)};",
              "",
              f"        public const int WaveFirstSeconds = {WAVE_FIRST_SECONDS};",
              f"        public const int WaveGapSeconds = {WAVE_GAP_SECONDS};",
              f"        public const int MaximumWaves = {MAXIMUM_WAVES};",
              f"        public const float WaveRingMeters = {WAVE_RING_METERS}f;",
              "        /// <summary>The strongbox's step-up per contested wave cleared before it opens (percent), by content tier.</summary>",
              f"        public static readonly int[] WaveStepPercent = {cs_ints(wave_step_percent(t) for t in TIERS)};",
              f"        public const int DefenderSharePercent = {DEFENDER_SHARE_PERCENT};",
              f"        public const float DefenseRadius = {DEFENSE_RADIUS}f;",
              f"        public const int ReinforcementCount = {REINFORCEMENT_COUNT};",
              f"        public const int ReinforcementSeconds = {REINFORCEMENT_SECONDS};",
              f"        public const int MaximumReinforcements = {MAXIMUM_REINFORCEMENTS};",
              "",
              f"        public const int SalvageBaseSeconds = {SALVAGE_BASE_SECONDS};",
              f"        public const float SalvageTetherMeters = {SALVAGE_TETHER_METERS}f;",
              f"        public const int SalvageIndustryCap = {SALVAGE_INDUSTRY_CAP};",
              f"        public const int SalvageScrap = {SALVAGE_SCRAP};",
              f"        public const int SalvageItemPulls = {SALVAGE_ITEM_PULLS};",
              "",
              "        /// <summary>A salvage team's base credits, by content tier; the team's yield percent raises them.</summary>",
              f"        public static readonly int[] SalvageCredits = {cs_ints(salvage_credits(t) for t in TIERS)};",
              "",
              "        // Payout model (derelicts.py --sim): credits per hour by content tier against the tier's space combat rate.",
              "        public static readonly int[] CombatCreditsPerHour = " + cs_ints(round(combat_rate(t)) for t in TIERS) + ";",
              "        public static readonly int[] BoardingCreditsPerHour = " + cs_ints(round(board_rate(t)) for t in TIERS) + ";",
              "        public static readonly int[] ContestedGroupCreditsPerHour = " + cs_ints(round(contested_rates(t)[3]) for t in TIERS) + ";",
              "        public static readonly int[] SalvageCreditsPerHour = " + cs_ints(round(salvage_rate(t, SALVAGE_REFERENCE_INDUSTRY)) for t in TIERS) + ";",
              f"        public const int SalvageReferenceIndustry = {SALVAGE_REFERENCE_INDUSTRY};",
              "    }", "}", ""]
    return "\n".join(lines)


WRITES = {"SWLOR.Game.Server/Service/SpaceService/DerelictTuning.cs": tuning_cs}


# ---------------------------------------------------------------------------------------------------------------
# Bible tab: bible_tabs.py calls derelicts_tab(Sheet) to write the Derelict Boarding (Revamp) tab.
# ---------------------------------------------------------------------------------------------------------------
def derelicts_tab(Sheet):
    s = Sheet("Derelict Boarding (Revamp)", [28, 16, 14, 14, 14, 14, 14, 60])
    s.title("Derelict Boarding (Revamp): boardable and contested derelicts, interiors, waves and droid salvage teams")
    s.note("Dev Status: Proposed (space revamp W15c draft). Generated by tools/SpaceRevampBalance/derelicts.py; plan section 28.")
    s.blank()
    s.title("Finding and boarding")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Boardable", f"{BOARDABLE_PERCENT}% of derelicts", "", "", "", "", "",
         "scan sites and Wild Space derelicts; shown on the Scanner tab from the threat level; agent leads never are"],
        ["Contested", f"{CONTESTED_PERCENT}% of boardable", "", "", "", "", "", "waves attack while the party is aboard"],
        ["Docking", "/board on the targeted derelict", "", "", "", "", "",
         f"Small or Medium hulls, within {DOCK_RANGE} m, out of combat, defenses cleared, the scanning party; "
         f"{DOCK_CAPACITY} ships of one party at once"],
        ["Safety", "the docked ship leaves space", "", "", "", "", "",
         "it cannot be attacked while docked; leaving through the airlock returns to it, undocking brings everyone back"],
        ["Hold", f"{BOARDING_HOLD_MINUTES} min", "", "", "", "", "", "a boarded site stays open while boarded, up to this long"],
    ])
    s.title("Interiors (existing starship interiors, dressed as wrecks when instanced)")
    s.table(["Template", "Name", "", "", "", "", "", "Contents"],
            [[r, n, "", "", "", "", "",
              f"{INTERIOR_ENEMIES} ground enemies, {INTERIOR_TERMINALS} slicing terminals, a strongbox, {DRESSING_COUNT} debris pieces"
              if i == 0 else ""] for i, (r, n) in enumerate(TEMPLATES)])
    s.table(["Content tier", "Ground enemies", "", "", "Terminal / lockbox tier", "Strongbox credits", "Kill loot / run", "Notes"],
            [[TIER_LABELS[t], ", ".join(ROSTERS[t]), "", "", slicing_tier(t), strongbox_credits(t), round(board_own_value(t)),
              "the strongbox opens once the interior is clear; each boarder inside gets the credits and the lockboxes"
              if t == 1 else ""] for t in TIERS])
    s.title("Contested derelicts and droid salvage teams")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Waves", f"{MAXIMUM_WAVES}, first after {WAVE_FIRST_SECONDS} s, then {WAVE_GAP_SECONDS} s after each clears", "", "", "", "", "",
         "the tier's combat-site squad (Rift Raiders in Wild Space) on a ring around the derelict"],
        ["Loot step", "strongbox credits +step% and +1 lockbox per wave cleared", "", "", "", "", "",
         "waves cleared before the strongbox opens; the step by tier is in the table below, solved so the mixed group earns "
         f"{CONTESTED_PREMIUM:.0%} of combat"],
        ["Defenders", f"{DEFENDER_SHARE_PERCENT}% of the strongbox", "", "", "", "", "",
         f"each party member flying within {DEFENSE_RADIUS} m when it opens; the boarders inside get it in full"],
        ["Reinforcements", f"{REINFORCEMENT_COUNT} boarders every {REINFORCEMENT_SECONDS} s, at most {MAXIMUM_REINFORCEMENTS}", "", "", "", "", "",
         f"while a wave is out and no player ship is within {DEFENSE_RADIUS} m of the derelict"],
        ["Salvage team timer", f"{SALVAGE_BASE_SECONDS} s / (1 + SalvageTeamSpeedPercent)", "", "", "", "", "",
         f"/salvage within {DOCK_RANGE} m; the ship stays in space within {SALVAGE_TETHER_METERS} m and draws the waves"],
        ["Salvage team yield", f"credits and {SALVAGE_SCRAP} Ship Scrap x (1 + SalvageTeamYieldPercent), {SALVAGE_ITEM_PULLS} elite pull", "", "", "", "", "",
         f"team Industry: the posted droids' summed, capped at {SALVAGE_INDUSTRY_CAP}"],
    ])
    s.title(f"Payout (per player; target {BOARDING_PREMIUM:.0%} of the tier's space combat rate, the ground dungeon proxy)")
    rows = []
    for t in TIERS:
        c = combat_rate(t)
        waves, _, _, group = contested_rates(t)
        sal = salvage_rate(t, SALVAGE_REFERENCE_INDUSTRY)
        rows.append([TIER_LABELS[t], round(c), round(board_rate(t)), f"{100 * board_rate(t) / c:.0f}%", round(group),
                     f"{100 * group / c:.0f}% ({waves} waves)", round(sal), f"{100 * sal / board_rate(t):.0f}% of boarding"])
    s.table(["Content tier", "Combat / h", "Boarding / h", "% of combat", "Contested group / h", "% of combat",
             f"Salvage team / h (Industry {SALVAGE_REFERENCE_INDUSTRY})", "Salvage vs boarding"], rows)
    s.note(f"Boarding: {board_minutes():g} minutes aboard ({INTERIOR_ENEMIES} ground fights of {GROUND_KILL_MINUTES:g} min, "
           f"{WALK_MINUTES:g} min of walking, docking and undocking). Slicing terminals and lockboxes pay the ground slicing "
           "rewards and are left out, as rare drops are. Contested: one boarder and one defender. Salvage: a solo pilot.")
    return s


def main():
    if "--sim" in sys.argv:
        report()
        return
    check = "--check" in sys.argv
    stale = []
    for rel, build in WRITES.items():
        text = build()
        path = REPO / rel
        if path.exists() and path.read_text(encoding="utf-8") == text:
            continue
        stale.append(rel)
        if not check:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text, encoding="utf-8", newline="\r\n")
    print(f"{len(TEMPLATES)} templates, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
