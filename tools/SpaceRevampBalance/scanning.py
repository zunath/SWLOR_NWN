"""Space revamp scanning and exploration sites (plan section 24, W12a): the Probe Launcher, hidden signatures, the
Scanner tab's information thresholds, Jump to Signature, Sensor Sweep, site content and site rewards.

usage: python scanning.py            (writes the generated C# scanning tuning)
       python scanning.py --check    (exit 1 when the generated file is out of date)
       python scanning.py --sim      (prints the exploration payout against space combat per tier, a player slicer's
                                      and a droid-run explorer's; exit 1 when one leaves the 100-140% band, or 120%
                                      with --store-price)

Every scanning number lives here. catalog.py reads the Probe Launcher steps from it, and bible_tabs.py calls
scanning_tab(Sheet) to write the Scanning (Revamp) tab. The space slicing numbers (data sites' data cans, locked caches,
wreck data cores, space lockboxes) are slicing.py's; this model counts them in each site's value and time.

Sources:
- Probe Launcher: the Ship Modules (Revamp) tab's draft (Mid, Piloting at the Sensors seat, toggle, 10 s cycle,
  4 capacitor, scan strength 10 + 2 x rank).
- Hull scan bonuses: the Starships (Revamp) tab (Explorer +25% scan strength, Chiss +15%, the Chiss Hyperion's
  signature +25% and the tier shown from 25%).
- Space combat credits per hour: trade.combat_credits_per_hour (SpaceLootTableDefinition with the "SPACE_BASIC_Tn,
  60, 3" loot local of the basic ships, 60 kills per hour, items at the 40% store markdown).
- Kill values of site ships: the LOOT_TABLE_n locals of each creature blueprint (Module/utc), valued the same way.
"""
import json
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import droids  # noqa: E402
import slicing  # noqa: E402
import trade  # noqa: E402
import wildspace  # noqa: E402

REPO = Path(__file__).resolve().parents[2]

# ---------------------------------------------------------------------------------------------------------------
# Probe Launcher (Mid slot, Piloting; Espionage at the Sensors seat; toggle)
# ---------------------------------------------------------------------------------------------------------------
PROBE_CYCLE_SECONDS = 10.0
PROBE_CAPACITOR = 4
PROBE_STRENGTH_BASE = 10
PROBE_STRENGTH_PER_RANK = 2


def probe_strength(rank):
    return PROBE_STRENGTH_BASE + PROBE_STRENGTH_PER_RANK * rank


# Espionage rank of whoever runs the launcher (the pilot, or the crew member at the Sensors seat): +1% scan strength
# per rank, +50% at rank 50 (plan section 17, "Espionage (scan strength ...)"). A droid at the Sensors seat has no
# Espionage; its Survey Protocol (plan section 27) is a hook that adds nothing yet.
ESPIONAGE_PERCENT_PER_RANK = 1
ESPIONAGE_MAXIMUM_RANK = 50

# Hull scan bonuses (Starships (Revamp)).
EXPLORER_SCAN_PERCENT = 25
CHISS_SCAN_PERCENT = 15
HYPERION_TAG = "sd_chisshyperion"
HYPERION_SCAN_PERCENT = 25

# A signature needs this much signal per tier of its area, times its type's difficulty. A probe at the tier's matched
# rank (10 x tier - 5, strength 20 x tier) with no Espionage resolves a difficulty-1.0 signature in 12 cycles (2 min).
SIGNAL_PER_TIER = 240
DIFFICULTY = {"Rift": 1.5, "CombatSite": 0.8, "DataSite": 1.2, "CacheSite": 1.0, "GasSite": 0.8, "Derelict": 1.2}
TYPE_NAMES = {"Rift": "Rift", "CombatSite": "Combat Site", "DataSite": "Data Site", "CacheSite": "Cache Site",
              "GasSite": "Gas Site", "Derelict": "Derelict"}
TYPE_IDS = {"Rift": 1, "CombatSite": 2, "DataSite": 3, "CacheSite": 4, "GasSite": 5, "Derelict": 6}

# Information grows with strength (plan section 24): the Scanner tab shows the type at 25%, the tier and the rough
# location (the area's sector) at 50%, the threat and loot class at 75%, the exact location and Jump at 100%.
INFO_TYPE = 25
INFO_TIER = 50
INFO_THREAT = 75
INFO_LOCATED = 100
SECTOR_GRID = 3  # rough location: the area's 3 x 3 sectors (north-west ... south-east)


def signal_required(kind, tier):
    return SIGNAL_PER_TIER * tier * DIFFICULTY[kind]


def signal_per_cycle(rank, espionage=0, hull_percent=0, environment_percent=0, survey_percent=0, effectiveness=1.0):
    bonus = min(espionage, ESPIONAGE_MAXIMUM_RANK) * ESPIONAGE_PERCENT_PER_RANK + hull_percent + environment_percent + survey_percent
    return probe_strength(rank) * max(0.0, 1 + bonus / 100) * effectiveness


def cycles_to_resolve(kind, tier, rank, espionage=0, hull_percent=0):
    return math.ceil(signal_required(kind, tier) / signal_per_cycle(rank, espionage, hull_percent))


# ---------------------------------------------------------------------------------------------------------------
# Jump to Signature and Sensor Sweep
# ---------------------------------------------------------------------------------------------------------------
JUMP_SPOOL_SECONDS = 5.0
JUMP_ARRIVAL_METERS = 20  # the ship drops out this far from the site's center, toward where it came from

SWEEP_RANGE_BASE = 100       # meters; a 32 x 32 orbit is 320 m across
SWEEP_RANGE_PER_RANK = 2     # Mk I 100 m, Mk X 190 m
SWEEP_COOLDOWN_SECONDS = 60
SWEEP_CAPACITOR = 10
SWEEP_RESULT_SECONDS = 60    # the Scanner tab keeps the list this long
SWEEP_MAXIMUM_SHIPS = 20


def sweep_range(rank):
    return SWEEP_RANGE_BASE + SWEEP_RANGE_PER_RANK * rank


# ---------------------------------------------------------------------------------------------------------------
# Signatures: where, how many, how long
# ---------------------------------------------------------------------------------------------------------------
# The space orbits and the three Small/Medium zones (trade.ORBIT_TIERS). Zones hold one more signature than an orbit,
# as they pay better than open orbit (plan section 12). The Wild Space systems (W12c, wildspace.py) are tier 5 content at
# tier 5.5 signal, with an orbit's site signatures; their rifts come from the rift network on top of these.
ZONE_AREAS = {"space_viscbelt", "space_midrimneb", "space_korrdebris"}
ORBIT_SIGNATURES = 3
ZONE_SIGNATURES = 4
AREAS = [(resref, tier, ZONE_SIGNATURES if resref in ZONE_AREAS else ORBIT_SIGNATURES, trade.ORBIT_NAMES[resref])
         for resref, tier in trade.ORBIT_TIERS.items()]
WILD_SPACE_AREAS = [(resref, wildspace.CONTENT_TIER, wildspace.SITE_SIGNATURES, name) for resref, name, _, _, _ in wildspace.SYSTEMS]

HIDDEN_LIFETIME_SECONDS = 3600   # an unresolved signature fades and a new one appears elsewhere
SITE_LIFETIME_SECONDS = 1200     # a revealed site despawns after this long, completed or not
RESPAWN_SECONDS = 300            # a new signature appears this long after one is completed or fades
SITE_CHECK_SECONDS = 6           # how often sites are checked for completion and timers

# Type weights. Gas sites need a tier 3 Gas Harvester cloud (the gas clouds are tier 3 and 4), so they spawn from
# tier 3. Data sites spawn with the Data Analyzer (W12b, slicing.py). Rifts weigh 0: the rift network (W12c, wildspace.py)
# places them in Wild Space and the known orbits, so they never take a site signature's place.
WEIGHTS = {"CombatSite": 35, "CacheSite": 25, "GasSite": 20, "Derelict": 20, "DataSite": slicing.DATA_SITE_WEIGHT, "Rift": 0}
GAS_MINIMUM_TIER = 3


def weights(tier):
    return {k: (0 if k == "GasSite" and tier < GAS_MINIMUM_TIER else v) for k, v in WEIGHTS.items()}


# ---------------------------------------------------------------------------------------------------------------
# Site content
# ---------------------------------------------------------------------------------------------------------------
def combat_squad(tier):
    """An NPC squad of the area's tier (the W5b role ships, SpaceSquadCatalog's t<tier><hull> tags)."""
    hulls = ["fighter", "fighter", "interceptor"] + (["bomber"] if tier >= 3 else []) + (["gunship"] if tier >= 5 else [])
    return [f"t{tier}{h}" for h in hulls]


# Wild Space sites: a tier 5 squad's hulls flown by Rift Raiders, and caches that pull from the Rift Raiders' elite table.
WILD_SPACE_SQUAD = ["rrfighter", "rrfighter", "rrinterceptor", "rrbomber", "rrgunship"]
WILD_SPACE_CACHE_TABLE = "SPACE_ELITE_WS"


def derelict_defenses(tier):
    """The wreck's defenses: the Korriban Debris Field's derelict turrets from tier 5, the tier's defense platform below."""
    return ["t5derturret1", "t5derturret2"] if tier >= 5 else [f"t{tier}platform"]


GAS_SITE_CLOUDS = 5
CACHE_RESREF = "spc_sig_cache"
WRECK_SIZE = "Large"
WRECK_DIFFICULTY = "Elite"


def gas_cloud_resref(tier):
    return "spc_gascloud_t3" if tier <= 3 else "spc_gascloud_t4"


# Threat and loot class shown at 75%.
THREAT = {"CombatSite": "Hostile squad", "Derelict": "Automated defenses", "GasSite": "None", "CacheSite": "None",
          "DataSite": "None", "Rift": "Unknown"}
LOOT_CLASS = {"CombatSite": "Credits and salvage", "Derelict": "Salvage and credits", "GasSite": "Tibanna Gas and credits",
              "CacheSite": "Credits and supplies", "DataSite": "Data", "Rift": "Passage"}

# ---------------------------------------------------------------------------------------------------------------
# Site rewards: a site cache paid when a site is completed (a cache site is the cache itself)
# ---------------------------------------------------------------------------------------------------------------
# Exploration pays EXPLORE_PREMIUM x the tier's space combat credits per hour. Every site's cache covers the time the
# site takes plus its share of the scanning, travel and jump overhead at that rate, less what the site already pays
# (the kills' own loot, the salvage and the gas), so each site type pays the same per hour and the mix of types does not
# move the result. The premium sits in the 100-130% band: exploring needs a Mid slot for the launcher, scanning time
# with nothing to shoot, and the Espionage investment, so it pays a little more than flying the same tier's ships down,
# without beating combat by so much that combat pilots stop fighting.
EXPLORE_PREMIUM = 1.15
KILL_MINUTES = 60 / trade.KILLS_PER_HOUR      # one normal fight, as the trade combat baseline counts it
PLATFORM_MINUTES = 2.0                         # an elite defense platform or derelict turret
JUMP_MINUTES = 0.25                            # the 5 s spool and lining up on the site
TRAVEL_MINUTES = 2.0                           # one starlane to the next system
CACHE_MINUTES = 0.5
GAS_CLOUD_MINUTES = 0.5                        # about 3 cycles of 6 s and the flight to the next cloud
GAS_CLOUD_UNITS = 4.5                          # 1d4 + 2 units
SALVAGE_CYCLES = 8                             # Large wreck 6 + Elite 2 (ShipSalvageRules.WreckCycles)
SALVAGE_SCRAP_PER_CYCLE = 1.5                  # 1 + Industrial / 4 + module bonus / 6 for a typical fit
SALVAGE_MINUTES = SALVAGE_CYCLES * 6 / 60 + 0.25
CACHE_ITEM_PULLS = 1                           # pulls from the tier's SPACE_ELITE table, on top of the credits
MARKDOWN = 0.40
CAN_TRAVEL_MINUTES = 0.25                      # flying between a data site's cans
LOCKED_CACHE_PERCENT = slicing.LOCKED_CACHE_PERCENT


def combat_rate(tier):
    """Space combat credits per hour of the tier: trade.py's baseline with the space lockboxes normal kills drop."""
    return slicing.combat_with_lockboxes(tier)


def slicing_tier(tier):
    return slicing.target_tier(tier)


def lockbox_kill_value(tier):
    """What a site ship's space lockbox drop adds to its kill, where slicing lockboxes pays (slicing.py)."""
    return slicing.lockbox_per_kill(tier) if slicing.lockboxes_pay(tier) else 0.0


def lockbox_kill_minutes(tier):
    return slicing.lockbox_minutes_per_kill() if slicing.lockboxes_pay(tier) else 0.0


_UTC = {}


def kill_value(tag):
    """One kill's expected credits: each LOOT_TABLE_n local (chance, attempts) of the blueprint, rare tables left out."""
    if tag not in _UTC:
        data = json.loads((REPO / "Module" / "utc" / f"{tag}.utc.json").read_text(encoding="latin-1"))
        tables = trade.loot_tables()
        value = 0.0
        for var in data.get("VarTable", {}).get("value", []):
            if not var["Name"]["value"].startswith("LOOT_TABLE_"):
                continue
            name, chance, attempts = [x.strip() for x in str(var["Value"]["value"]).split(",")]
            rows = tables.get(name, [])
            if rows and not all(r[3] for r in rows) and not name.endswith("_RARES"):
                value += int(chance) / 100 * int(attempts) * trade.expected_value(rows)
        _UTC[tag] = value
    return _UTC[tag]


def elite_pull_value(tier):
    return trade.expected_value(trade.loot_tables()[f"SPACE_ELITE_T{tier}"])


def scrap_value(tier):
    return trade.item_cost(f"ship_scrap_{min(5, tier)}") * MARKDOWN


def site_minutes(kind, tier, crew=None):
    """A site's minutes; crew (a droid-run explorer's crew bonus percent, see DROID_CREWS) swaps the player's slicing
    boards for the droid's cooldown."""
    if crew is not None:
        return droid_site_minutes(kind, tier)
    if kind == "CombatSite":
        return len(combat_squad(tier)) * (KILL_MINUTES + lockbox_kill_minutes(tier))
    if kind == "Derelict":
        return (len(derelict_defenses(tier)) * (PLATFORM_MINUTES + lockbox_kill_minutes(tier)) + SALVAGE_MINUTES
                + slicing.SLICE_MINUTES)
    if kind == "GasSite":
        return GAS_SITE_CLOUDS * GAS_CLOUD_MINUTES
    if kind == "DataSite":
        return slicing.DATA_SITE_CANS * (slicing.SLICE_MINUTES + CAN_TRAVEL_MINUTES)
    if kind == "LockedCache":
        return CACHE_MINUTES + slicing.SLICE_MINUTES
    return CACHE_MINUTES


def own_value(kind, tier, crew=None):
    """What a site pays before its cache: the ships' own loot, the wreck's scrap, the clouds' gas. A droid-run
    explorer's slicing targets pay at the droid's chance instead of the player's (slicing.SLICE_SUCCESS)."""
    if crew is not None and kind in SLICED_KINDS:
        return own_value(kind, tier) - SLICED_KINDS[kind](tier) * (1 - droid_chance(crew) / slicing.SLICE_SUCCESS)
    if kind == "CombatSite":
        return sum(kill_value(t) + lockbox_kill_value(tier) for t in combat_squad(tier))
    if kind == "Derelict":
        return (sum(kill_value(t) + lockbox_kill_value(tier) for t in derelict_defenses(tier))
                + SALVAGE_CYCLES * SALVAGE_SCRAP_PER_CYCLE * scrap_value(tier)
                + slicing.expected_value("WreckDataCore", slicing_tier(tier)))
    if kind == "GasSite":
        return GAS_SITE_CLOUDS * GAS_CLOUD_UNITS * trade.item_cost("gas_tibanna") * MARKDOWN
    if kind == "DataSite":
        return slicing.DATA_SITE_CANS * slicing.expected_value("DataCan", slicing_tier(tier))
    if kind == "LockedCache":
        return slicing.expected_value("LockedCache", slicing_tier(tier))
    return 0.0


def area_signatures(tier):
    """The signatures of a typical area of the tier (orbits; zones have one more, which only lowers the overhead)."""
    return ORBIT_SIGNATURES


def scan_minutes(tier, espionage=0, crew=None):
    """Resolving every signature in an area at once: the slowest type in play, with a matched-rank probe (run by a
    droid at the Sensors seat when crew is given: its effectiveness and Survey Protocol II, no Espionage)."""
    rank = 10 * min(tier, 5) - 5
    kinds = [k for k, w in weights(tier).items() if w > 0]
    if crew is None:
        return max(cycles_to_resolve(k, tier, rank, espionage) for k in kinds) * PROBE_CYCLE_SECONDS / 60
    signal = signal_per_cycle(rank, survey_percent=DROID_SURVEY_PERCENT, effectiveness=droid_effectiveness(crew))
    return max(math.ceil(signal_required(k, tier) / signal) for k in kinds) * PROBE_CYCLE_SECONDS / 60


def overhead_minutes(tier, crew=None):
    return (scan_minutes(tier, crew=crew) + TRAVEL_MINUTES) / area_signatures(tier) + JUMP_MINUTES


def site_credits(kind, tier):
    """The credits in a site's cache, rounded to 25; its item pulls are counted against the target as well."""
    target = EXPLORE_PREMIUM * combat_rate(tier) / 60 * (site_minutes(kind, tier) + overhead_minutes(tier))
    with slicing.generated_values():  # the generated credits, also under --store-price
        credits = target - own_value(kind, tier) - CACHE_ITEM_PULLS * elite_pull_value(tier)
    return max(0, int(25 * round(credits / 25)))


def site_value(kind, tier, crew=None):
    return own_value(kind, tier, crew) + site_credits(kind, tier) + CACHE_ITEM_PULLS * elite_pull_value(tier)


def mix(tier):
    """The weighted site kinds of a tier, with LOCKED_CACHE_PERCENT of the cache sites counted as locked caches."""
    w = {k: v for k, v in weights(tier).items() if v}
    if "CacheSite" in w:
        cache = w.pop("CacheSite")
        w["CacheSite"] = cache * (100 - LOCKED_CACHE_PERCENT) / 100
        w["LockedCache"] = cache * LOCKED_CACHE_PERCENT / 100
    return w


def explore_rate(tier, crew=None):
    """Credits per hour exploring the tier's areas: the weighted site mix, each with its share of the overhead. With
    crew, a droid-run explorer: a slicing droid at the Sensors seat with that crew bonus percent (DROID_CREWS)."""
    w = mix(tier)
    total = sum(w.values())
    minutes = sum(w[k] / total * (site_minutes(k, tier, crew) + overhead_minutes(tier, crew)) for k in w)
    value = sum(w[k] / total * site_value(k, tier, crew) for k in w)
    return value / minutes * 60


# ---------------------------------------------------------------------------------------------------------------
# Droid-run explorer (review fix 2026-10-30). A droid at the Sensors seat slices with no mini-game
# (SpaceSlicing.StartDataAnalyzer): the pilot needs the Slicing rank a player would, the droid opens one target a press
# and then works it for DroidCrewRules.SlicingSeconds (120 s; review fix 2026-10-30, was 90, a player's board) before the next, at its seat
# effectiveness capped at DroidCrewRules.MaximumSlicingChance (85%, the player's assumed success); a miss loses the
# target as a player's failure does. Its 90 s run alongside the pilot's flying, so a single-target site (a locked
# cache, a derelict's data core) costs no slicing time and a data site's three cans cost two cooldowns, not three
# boards and the hops between them. The same droid runs the Probe Launcher at its effectiveness with Survey Protocol II
# (+40%) and no Espionage. The best industrial droid (Industry 100) works a support seat at 75%; Crew Morale (+10%),
# the captain's Leadership (+5% at 50) and the Czerka Armored Transport (+20% for droids) multiply it, capped at 105%.
# Sites regenerate RESPAWN_SECONDS after completion; the model's explorer moves on to the next system after an area's
# signatures, and an area's round (scan, sites, travel) is checked against the respawn below.
# ---------------------------------------------------------------------------------------------------------------
DROID_SLICE_MINUTES = 120 / 60         # DroidCrewRules.SlicingSeconds (was 90: 132-146% with the best droid and crew)
DROID_MAXIMUM_SLICING_CHANCE = 0.85    # DroidCrewRules.MaximumSlicingChance
DROID_SURVEY_PERCENT = 40              # Survey Protocol II (droids.PROTOCOLS)
MAXIMUM_SEAT_EFFECTIVENESS = 1.05      # ShipCrewSeats.MaximumSeatEffectiveness
DROID_CREWS = [("best droid, no crew bonus", 0), ("with Crew Morale", 10), ("Crew Morale and Leadership 50", 15),
               ("Czerka Armored Transport, Crew Morale, Leadership 50", 35)]
# The slicing a site holds: its targets' expected value at the player's success (slicing.expected_value).
SLICED_KINDS = {
    "DataSite": lambda t: slicing.DATA_SITE_CANS * slicing.expected_value("DataCan", slicing_tier(t)),
    "LockedCache": lambda t: slicing.expected_value("LockedCache", slicing_tier(t)),
    "Derelict": lambda t: slicing.expected_value("WreckDataCore", slicing_tier(t)),
}


def droid_effectiveness(crew):
    """The best industrial droid's Sensors seat effectiveness with the crew bonus percent (SpaceCrew.GetDroidEffectiveness)."""
    return min(MAXIMUM_SEAT_EFFECTIVENESS, droids.BEST_SUPPORT_EFFECTIVENESS * (1 + crew / 100))


def droid_chance(crew):
    """The droid's slicing chance (DroidCrewRules.SlicingSucceeds: whole percent, at most 85%)."""
    return min(DROID_MAXIMUM_SLICING_CHANCE, math.floor(droid_effectiveness(crew) * 100 + 0.5) / 100)


def droid_site_minutes(kind, tier):
    """A site's minutes for a droid-run explorer: the player's site with its boards taken out, a data site waiting on
    the droid between cans (its hops overlap the cooldown) and a derelict's core sliced while the wreck is salvaged."""
    if kind == "DataSite":
        return (slicing.DATA_SITE_CANS - 1) * max(DROID_SLICE_MINUTES, CAN_TRAVEL_MINUTES)
    if kind == "LockedCache":
        return CACHE_MINUTES
    if kind == "Derelict":
        return site_minutes(kind, tier) - slicing.SLICE_MINUTES
    return site_minutes(kind, tier)


def area_round_minutes(tier, crew=None):
    """One pass over an area: its scan, each signature's site and jump, then the starlane on."""
    w = mix(tier)
    total = sum(w.values())
    site = sum(w[k] / total * site_minutes(k, tier, crew) for k in w)
    return scan_minutes(tier, crew=crew) + area_signatures(tier) * (site + JUMP_MINUTES) + TRAVEL_MINUTES


EXPLORE_BAND = (1.00, 1.40)
STORE_PRICE_BAND = (1.00, 1.20)  # --store-price: schematics and Prototype Tech at their store price (plan section 19)


TIERS = [1, 2, 3, 4, 5, 6]
KINDS = ["CombatSite", "CacheSite", "GasSite", "Derelict", "DataSite"]


def report():
    """Prints the payouts and returns False when the player's or a droid-run explorer's rate leaves EXPLORE_BAND."""
    ok = True
    band = STORE_PRICE_BAND if slicing.SCHEMATICS_AT_STORE_PRICE else EXPLORE_BAND
    t3 = combat_rate(3)
    print("Exploration against space combat (matched-rank probe, no Espionage):")
    for t in TIERS:
        e, c = explore_rate(t), combat_rate(t)
        print(f"  T{t}: explore {e:7.0f}/h, combat {c:7.0f}/h, {100 * e / c:5.1f}% of its tier, {100 * e / t3:5.1f}% of T3; "
              f"scan {scan_minutes(t):.1f} min, overhead {overhead_minutes(t):.2f} min/site")
        for k in KINDS + ["LockedCache"]:
            if mix(t).get(k):
                print(f"      {k:11s} {site_minutes(k, t):4.1f} min, own {own_value(k, t):6.0f}, cache {site_credits(k, t):5d} cr "
                      f"+ {CACHE_ITEM_PULLS} elite pull ({elite_pull_value(t):.0f})")
    print("Droid-run explorer (a slicing droid at the Sensors seat, Survey Protocol II) against the player slicer:")
    for t in TIERS:
        c, player = combat_rate(t), explore_rate(t)
        cells = []
        for name, crew in DROID_CREWS:
            rate = explore_rate(t, crew)
            cells.append(f"{name} {droid_chance(crew):.0%}: {100 * rate / c:5.1f}%")
            ok &= band[0] <= rate / c <= band[1]
        ok &= band[0] <= player / c <= band[1]
        print(f"  T{t}: player {100 * player / c:5.1f}%; " + "; ".join(cells))
        print(f"      an area's round: player {area_round_minutes(t):4.1f} min, best droid "
              f"{area_round_minutes(t, DROID_CREWS[-1][1]):4.1f} min (sites respawn {RESPAWN_SECONDS / 60:g} min after completion)")
    if not ok:
        print(f"  BAND FAIL: exploring outside {band[0]:.0%}-{band[1]:.0%} of the tier's combat")
    return ok


# ---------------------------------------------------------------------------------------------------------------
# Generated C#
# ---------------------------------------------------------------------------------------------------------------
HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/scanning.py. Do not edit by hand: change scanning.py and regenerate.
// </auto-generated>
"""


def cs_double(x):
    return repr(float(x))


def cs_strings(items):
    return "new[] { " + ", ".join(json.dumps(x) for x in items) + " }"


def tuning_cs():
    lines = [HEADER.rstrip("\n"), "using System.Collections.Generic;", "",
             "namespace SWLOR.Game.Server.Service.SpaceService", "{",
             "    /// <summary>",
             "    /// The scanning numbers (plan section 24): signal, information thresholds, Jump to Signature, Sensor Sweep,",
             "    /// where signatures appear, site content and site rewards.",
             "    /// </summary>",
             "    public static class ScanningTuning", "    {",
             f"        public const double ProbeCycleSeconds = {cs_double(PROBE_CYCLE_SECONDS)};",
             f"        public const int ProbeStrengthBase = {PROBE_STRENGTH_BASE};",
             f"        public const int ProbeStrengthPerRank = {PROBE_STRENGTH_PER_RANK};",
             f"        public const int EspionagePercentPerRank = {ESPIONAGE_PERCENT_PER_RANK};",
             f"        public const int EspionageMaximumRank = {ESPIONAGE_MAXIMUM_RANK};",
             f"        public const int ExplorerScanPercent = {EXPLORER_SCAN_PERCENT};",
             f"        public const int ChissScanPercent = {CHISS_SCAN_PERCENT};",
             f"        public const string HyperionTag = {json.dumps(HYPERION_TAG)};",
             f"        public const int HyperionScanPercent = {HYPERION_SCAN_PERCENT};",
             f"        public const int SignalPerTier = {SIGNAL_PER_TIER};",
             "",
             "        public static readonly IReadOnlyDictionary<SignatureType, double> Difficulty = new Dictionary<SignatureType, double>",
             "        {"]
    lines += [f"            [SignatureType.{k}] = {cs_double(v)}," for k, v in DIFFICULTY.items()]
    lines += ["        };", "",
              "        public static readonly IReadOnlyDictionary<SignatureType, string> Names = new Dictionary<SignatureType, string>",
              "        {"]
    lines += [f"            [SignatureType.{k}] = {json.dumps(v)}," for k, v in TYPE_NAMES.items()]
    lines += ["        };", "",
              f"        public const int InfoTypePercent = {INFO_TYPE};",
              f"        public const int InfoTierPercent = {INFO_TIER};",
              f"        public const int InfoThreatPercent = {INFO_THREAT};",
              f"        public const int InfoLocatedPercent = {INFO_LOCATED};",
              f"        public const int SectorGrid = {SECTOR_GRID};",
              "",
              f"        public const double JumpSpoolSeconds = {cs_double(JUMP_SPOOL_SECONDS)};",
              f"        public const int JumpArrivalMeters = {JUMP_ARRIVAL_METERS};",
              f"        public const int SweepRangeBase = {SWEEP_RANGE_BASE};",
              f"        public const int SweepRangePerRank = {SWEEP_RANGE_PER_RANK};",
              f"        public const int SweepCooldownSeconds = {SWEEP_COOLDOWN_SECONDS};",
              f"        public const int SweepCapacitor = {SWEEP_CAPACITOR};",
              f"        public const int SweepResultSeconds = {SWEEP_RESULT_SECONDS};",
              f"        public const int SweepMaximumShips = {SWEEP_MAXIMUM_SHIPS};",
              "",
              f"        public const int HiddenLifetimeSeconds = {HIDDEN_LIFETIME_SECONDS};",
              f"        public const int SiteLifetimeSeconds = {SITE_LIFETIME_SECONDS};",
              f"        public const int RespawnSeconds = {RESPAWN_SECONDS};",
              f"        public const int SiteCheckSeconds = {SITE_CHECK_SECONDS};",
              f"        public const int GasMinimumTier = {GAS_MINIMUM_TIER};",
              "",
              "        /// <summary>The areas signatures appear in: the space orbits, the Small/Medium zones and Wild Space.</summary>",
              "        public static readonly IReadOnlyList<ScanAreaDetail> Areas = new List<ScanAreaDetail>",
              "        {"]
    lines += [f"            new ScanAreaDetail {{ AreaResref = {json.dumps(r)}, Tier = {t}, Signatures = {n} }},"
              for r, t, n, _ in AREAS]
    lines += [f"            new ScanAreaDetail {{ AreaResref = {json.dumps(r)}, Tier = {t}, Signatures = {n}, WildSpace = true }},"
              for r, t, n, _ in WILD_SPACE_AREAS]
    lines += ["        };", "",
              "        /// <summary>Type weights before the tier rules (gas sites from tier 3).</summary>",
              "        public static readonly IReadOnlyDictionary<SignatureType, int> Weights = new Dictionary<SignatureType, int>",
              "        {"]
    lines += [f"            [SignatureType.{k}] = {v}," for k, v in WEIGHTS.items()]
    lines += ["        };", "",
              "        public static readonly IReadOnlyDictionary<SignatureType, string> Threat = new Dictionary<SignatureType, string>",
              "        {"]
    lines += [f"            [SignatureType.{k}] = {json.dumps(v)}," for k, v in THREAT.items()]
    lines += ["        };", "",
              "        public static readonly IReadOnlyDictionary<SignatureType, string> LootClass = new Dictionary<SignatureType, string>",
              "        {"]
    lines += [f"            [SignatureType.{k}] = {json.dumps(v)}," for k, v in LOOT_CLASS.items()]
    lines += ["        };", "",
              f"        public const int GasSiteClouds = {GAS_SITE_CLOUDS};",
              f"        public const string CacheResref = {json.dumps(CACHE_RESREF)};",
              f"        public const int CacheItemPulls = {CACHE_ITEM_PULLS};",
              f"        public const ShipSize WreckSize = ShipSize.{WRECK_SIZE};",
              f"        public const WreckDifficulty WreckDifficultyLevel = WreckDifficulty.{WRECK_DIFFICULTY};",
              "",
              "        /// <summary>The squad of a combat site, by tier (index 1-6).</summary>",
              "        public static readonly string[][] CombatSquads =",
              "        {",
              "            System.Array.Empty<string>(),"]
    lines += [f"            {cs_strings(combat_squad(t))}," for t in TIERS]
    lines += ["        };", "",
              "        /// <summary>The squad of a combat site in Wild Space: Rift Raiders.</summary>",
              f"        public static readonly string[] WildSpaceCombatSquad = {cs_strings(WILD_SPACE_SQUAD)};",
              f"        public const string WildSpaceCacheTable = {json.dumps(WILD_SPACE_CACHE_TABLE)};",
              "",
              "        /// <summary>A derelict's defenses, by tier (index 1-6).</summary>",
              "        public static readonly string[][] DerelictDefenses =",
              "        {",
              "            System.Array.Empty<string>(),"]
    lines += [f"            {cs_strings(derelict_defenses(t))}," for t in TIERS]
    lines += ["        };", "",
              "        /// <summary>The gas cloud blueprint of a gas site, by tier (index 1-6).</summary>",
              "        public static readonly string[] GasClouds = { \"\", " + ", ".join(json.dumps(gas_cloud_resref(t)) for t in TIERS) + " };",
              "",
              "        /// <summary>The credits in a completed site's cache, by type and tier (index 1-6).</summary>",
              "        public static readonly IReadOnlyDictionary<SignatureType, int[]> SiteCredits = new Dictionary<SignatureType, int[]>",
              "        {"]
    lines += [f"            [SignatureType.{k}] = new[] {{ 0, " + ", ".join(str(site_credits(k, t)) for t in TIERS) + " },"
              for k in KINDS]
    lines += ["        };", "",
              "        // Payout model (scanning.py --sim): minutes and credits per site and the combat rate it is measured against.",
              f"        public const double ExplorePremium = {cs_double(EXPLORE_PREMIUM)};",
              "        public static readonly double[] CombatCreditsPerHour = { 0, " + ", ".join(cs_double(round(combat_rate(t), 2)) for t in TIERS) + " };",
              "        public static readonly double[] ElitePullCredits = { 0, " + ", ".join(cs_double(round(elite_pull_value(t), 2)) for t in TIERS) + " };",
              "        public static readonly double[] OverheadMinutes = { 0, " + ", ".join(cs_double(round(overhead_minutes(t), 4)) for t in TIERS) + " };",
              "        public static readonly IReadOnlyDictionary<SignatureType, double[]> SiteMinutes = new Dictionary<SignatureType, double[]>",
              "        {"]
    lines += [f"            [SignatureType.{k}] = new[] {{ 0.0, " + ", ".join(cs_double(round(site_minutes(k, t), 4)) for t in TIERS) + " },"
              for k in KINDS]
    lines += ["        };",
              "        public static readonly IReadOnlyDictionary<SignatureType, double[]> SiteOwnCredits = new Dictionary<SignatureType, double[]>",
              "        {"]
    lines += [f"            [SignatureType.{k}] = new[] {{ 0.0, " + ", ".join(cs_double(round(own_value(k, t), 2)) for t in TIERS) + " },"
              for k in KINDS]
    lines += ["        };", "",
              "        // Locked caches (space slicing, slicing.py): this share of cache sites must be sliced open; their credits, time and",
              "        // slicing reward replace a plain cache's.",
              f"        public const int LockedCachePercent = {LOCKED_CACHE_PERCENT};",
              "        public static readonly int[] LockedCacheCredits = { 0, " + ", ".join(str(site_credits("LockedCache", t)) for t in TIERS) + " };",
              "        public static readonly double[] LockedCacheMinutes = { 0.0, " + ", ".join(cs_double(round(site_minutes("LockedCache", t), 4)) for t in TIERS) + " };",
              "        public static readonly double[] LockedCacheOwnCredits = { 0.0, " + ", ".join(cs_double(round(own_value("LockedCache", t), 2)) for t in TIERS) + " };",
              "    }", "}", ""]
    return "\n".join(lines)


WRITES = {"SWLOR.Game.Server/Service/SpaceService/ScanningTuning.cs": tuning_cs}


# ---------------------------------------------------------------------------------------------------------------
# Bible tab: bible_tabs.py calls scanning_tab(Sheet) to write the Scanning (Revamp) tab.
# ---------------------------------------------------------------------------------------------------------------
def scanning_tab(Sheet):
    s = Sheet("Scanning (Revamp)", [26, 14, 12, 12, 12, 12, 12, 60])
    s.title("Scanning (Revamp): Probe Launcher, signatures, Jump to Signature, Sensor Sweep and exploration sites")
    s.note("Dev Status: Proposed (space revamp W12a draft). Generated by tools/SpaceRevampBalance/scanning.py; plan section 24.")
    s.blank()
    s.title("Probe Launcher (Mid slot; Piloting, Espionage at the Sensors seat; toggle)")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Cycle", f"{PROBE_CYCLE_SECONDS:g} s, {PROBE_CAPACITOR} capacitor", "", "", "", "", "",
         "each cycle adds signal to every hidden signature in the area"],
        ["Scan strength", f"{PROBE_STRENGTH_BASE} + {PROBE_STRENGTH_PER_RANK} x rank", "", "", "", "", "", "Mk I 10, Mk X 100 (Ship Modules (Revamp) utility steps)"],
        ["Espionage", f"+{ESPIONAGE_PERCENT_PER_RANK}% per rank", "", "", "", "", "",
         f"of whoever runs it (the pilot, or the Sensors crew member); +{ESPIONAGE_PERCENT_PER_RANK * ESPIONAGE_MAXIMUM_RANK}% at rank {ESPIONAGE_MAXIMUM_RANK}"],
        ["Hull bonus", f"Explorer +{EXPLORER_SCAN_PERCENT}%, Chiss +{CHISS_SCAN_PERCENT}%, Chiss Hyperion +{HYPERION_SCAN_PERCENT}%", "", "", "", "", "",
         "add together; the Hyperion also shows a signature's tier from 25%"],
        ["Environment", "Space Scan Strength stat (%)", "", "", "", "", "", "Wild Space environments set it (Ship Graveyard: weaker scanning)"],
        ["Droid crew", "station effectiveness", "", "", "", "", "", "a droid has no Espionage; Survey Protocol adds to it later (plan section 27)"],
        ["Signal per cycle", "strength x (1 + bonuses / 100) x station effectiveness", "", "", "", "", "", "a crew member works at the crew multiplier"],
        ["Signal to resolve", f"{SIGNAL_PER_TIER} x area tier x type difficulty", "", "", "", "", "",
         "a matched-rank probe (rank 10 x tier - 5) with no Espionage resolves difficulty 1.0 in 12 cycles"],
    ])
    s.title("Signature types")
    s.table(["Type", "Difficulty", "Weight", "From tier", "Threat (75%)", "", "", "Loot class (75%)"],
            [[TYPE_NAMES[k], DIFFICULTY[k], WEIGHTS[k], GAS_MINIMUM_TIER if k == "GasSite" else (1 if WEIGHTS[k] else "-"),
              THREAT[k], "", "", LOOT_CLASS[k] + ("" if WEIGHTS[k] else
                                                ("; spawns with the Data Analyzer (W12b)" if k == "DataSite" else "; Wild Space only (W12c)"))]
             for k in DIFFICULTY])
    s.title("Information on the Scanner tab")
    s.table(["Strength", "Shows", "", "", "", "", "", "Notes"], [
        [f"{INFO_TYPE}%", "type", "", "", "", "", "", "below it: Unknown signal"],
        [f"{INFO_TIER}%", "tier and rough location", "", "", "", "", "", f"the area's sector on a {SECTOR_GRID} x {SECTOR_GRID} grid"],
        [f"{INFO_THREAT}%", "threat and loot class", "", "", "", "", "", ""],
        [f"{INFO_LOCATED}%", "exact location; the site opens", "", "", "", "", "",
         "no object exists until the first party reaches 100%; strength is per party"],
    ])
    s.title("Jump to Signature and Sensor Sweep")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Jump spool", f"{JUMP_SPOOL_SECONDS:g} s", "", "", "", "", "",
         "anyone whose party holds 100% on the site; damage interrupts it, so does an Interdictor (no mechanic yet)"],
        ["Jump arrival", f"{JUMP_ARRIVAL_METERS} m from the site", "", "", "", "", "", "a micro-jump inside the area"],
        ["Sensor Sweep range", f"{SWEEP_RANGE_BASE} + {SWEEP_RANGE_PER_RANK} x rank m", "", "", "", "", "",
         f"Mk I {sweep_range(0)} m, Mk X {sweep_range(45)} m; lists every ship in range, cloaked ships included (plan section 32)"],
        ["Sensor Sweep cooldown", f"{SWEEP_COOLDOWN_SECONDS} s, {SWEEP_CAPACITOR} capacitor", "", "", "", "", "",
         f"a pulse of a fitted Probe Launcher; the list stays {SWEEP_RESULT_SECONDS} s, at most {SWEEP_MAXIMUM_SHIPS} ships"],
    ])
    s.title("Where signatures appear")
    s.table(["Area", "Tier", "Signatures", "", "", "", "", "Notes"],
            [[name, t, n, "", "", "", "", "Small/Medium zone: one more than an orbit" if r in ZONE_AREAS else ""] for r, t, n, name in AREAS])
    s.table(["Timer", "Seconds", "", "", "", "", "", "Notes"], [
        ["Hidden lifetime", HIDDEN_LIFETIME_SECONDS, "", "", "", "", "", "an unresolved signature fades and moves"],
        ["Site lifetime", SITE_LIFETIME_SECONDS, "", "", "", "", "", "a revealed site despawns, completed or not"],
        ["Respawn", RESPAWN_SECONDS, "", "", "", "", "", "after a site is completed or a signature fades"],
    ])
    s.title("Site content")
    s.table(["Tier", "Combat squad", "", "", "Derelict defenses", "Gas clouds", "", "Notes"],
            [[t, ", ".join(combat_squad(t)), "", "", ", ".join(derelict_defenses(t)),
              f"{GAS_SITE_CLOUDS} x {gas_cloud_resref(t)}" if t >= GAS_MINIMUM_TIER else "-", "",
              "derelict: a salvageable Large Elite wreck of the tier; cache sites: one site cache" if t == 1 else ""] for t in TIERS])
    s.title(f"Site rewards: the site cache (credits + {CACHE_ITEM_PULLS} pull from SPACE_ELITE_Tn) on completion")
    s.note(f"Each cache pays the site's time plus its share of scanning, travel and jump at {EXPLORE_PREMIUM:.0%} of the tier's "
           "space combat credits per hour, less what the site already pays (ship loot, scrap, gas). The premium is in the "
           "100-130% band: exploring costs a Mid slot, scanning time with nothing to shoot and Espionage ranks.")
    s.table(["Tier", "Combat site", "Cache site", "Gas site", "Derelict", "Explore / h", "Combat / h", "% of tier combat"],
            [[t, site_credits("CombatSite", t), site_credits("CacheSite", t), site_credits("GasSite", t) if t >= GAS_MINIMUM_TIER else "-",
              site_credits("Derelict", t), round(explore_rate(t)), round(combat_rate(t)), f"{100 * explore_rate(t) / combat_rate(t):.0f}%"]
             for t in TIERS])
    s.table(["Tier", "Data site", "Locked cache", "", "", "", "", "Notes (space slicing, slicing.py)"],
            [[t, site_credits("DataSite", t), site_credits("LockedCache", t), "", "", "", "",
              f"data site: {slicing.DATA_SITE_CANS} data cans, then its cache; {LOCKED_CACHE_PERCENT}% of cache sites are locked "
              "caches, sliced open for these credits, the elite pull and a slicing reward" if t == 1 else ""] for t in TIERS])
    s.table(["Model input", "Value", "", "", "", "", "", "Notes"], [
        ["Kill", f"{KILL_MINUTES:g} min", "", "", "", "", "", "the trade combat baseline's 60 kills per hour"],
        ["Platform or turret", f"{PLATFORM_MINUTES:g} min", "", "", "", "", "", ""],
        ["Salvage", f"{SALVAGE_MINUTES:g} min", "", "", "", "", "", f"{SALVAGE_CYCLES} cycles, {SALVAGE_SCRAP_PER_CYCLE} Ship Scrap each"],
        ["Gas cloud", f"{GAS_CLOUD_MINUTES:g} min", "", "", "", "", "", f"{GAS_CLOUD_UNITS} Tibanna Gas"],
        ["Cache", f"{CACHE_MINUTES:g} min", "", "", "", "", "", ""],
        ["Jump", f"{JUMP_MINUTES:g} min", "", "", "", "", "", "per site"],
        ["Travel", f"{TRAVEL_MINUTES:g} min", "", "", "", "", "", "per area, shared by its signatures with the scan"],
        ["Slicing", f"{slicing.SLICE_MINUTES:g} min", "", "", "", "", "",
         f"a data can, locked cache or data core board, {slicing.SLICE_SUCCESS:.0%} success; {CAN_TRAVEL_MINUTES:g} min between cans"],
        ["Combat / h", "with space lockboxes", "", "", "", "", "",
         f"normal kills drop a space lockbox {slicing.LOCKBOX_DROP_PERCENT['Normal']}% of the time; counted where slicing it pays"],
    ])
    return s


def main():
    if "--sim" in sys.argv:
        if not report():
            sys.exit(1)
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
    print(f"{len(AREAS)} areas, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
