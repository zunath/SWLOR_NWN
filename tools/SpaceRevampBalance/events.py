"""Space revamp events (plan sections 30 and 31): weekly incursions and Rift Runs.

usage: python events.py            (writes the generated C# tuning and the Rift Key blueprints)
       python events.py --check    (exit 1 when a generated file is out of date)
       python events.py --sim      (prints the incursion and Rift Run payouts against each tier's combat rate)

Every incursion and Rift Run number lives here. checks.py section O runs the wave, blockade, command and Rift Run room
compositions through the simulator, catalog.py puts the Rift Keys in the aces' tables (rewards.ACE_LOOT_WEIGHTS) and
bible_tabs.py calls events_tab(Sheet) to write the Events (Revamp) tab. Draft numbers (plan section 17, "Activities":
incursion schedule and rewards, Rift Run timers and loot); tune in testing.

Sources:
- Combat rate: trade.combat_credits_per_hour (tiers 1-6: the basic ships' loot, 60 kills an hour, drops at the 40% store
  markdown) and wildspace.combat_rate (tier 5.5, the Rift Raiders and their stolen cargo).
- Earning targets (W15b direction): incursion contribution loot and the region reward bring an active participant to
  about 120-140% of the tier's combat rate, and a Rift Run pays about 130% of its tier's combat rate per minute of run,
  because the key is consumed. The kills' own loot counts in both (the scanning.py convention: a reward tops up what the
  ships already drop), and rare-flagged drops (schematics, Rift Keys) are left out, as in every tier baseline.
- Regions: an orbit with a dock and its starlane neighbours (trade.LANES, trade.ORBIT_TIERS).
- Ships: the W5b role ships of each tier, the Rift Raiders and the Sith and Mandalorian capitals (npc_catalog.py).
"""
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import trade  # noqa: E402
import wildspace  # noqa: E402

REPO = Path(__file__).resolve().parents[2]

# Time per kill: one normal fight plus finding the next one, the trade baseline's 60 kills an hour.
KILL_MINUTES = 60 / trade.KILLS_PER_HOUR

# ---------------------------------------------------------------------------------------------------------------
# Incursions (plan section 30)
# ---------------------------------------------------------------------------------------------------------------
# Once a week, at a fixed slot players can plan for, a Sith or Mandalorian armada invades one region for up to 48 hours.
# The schedule is persisted (the next start time), so a restart neither skips nor repeats a week; an incursion running
# at a restart resumes with its progress and contributions, and its ships respawn.
START_WEEKDAY = "Saturday"
START_HOUR_UTC = 18
DURATION_HOURS = 48
TICK_SECONDS = 5

# (enum name, display name, its capital at tier 6, the maker whose Ship Schematics a cleared region can give).
# The tier 6 capitals are the existing faction capitals with the same FACTION_CAPITAL_SCALE (2.0): the Inquisitor
# Assault Cruiser and the Jahavey'ir Cruiser. Below tier 6 the command ship is the tier's capital flying that capital's
# hull appearance, so its fight stays in its tier's band.
INVADERS = [
    ("Sith", "Sith Armada", "sithcap3", "Sith"),
    ("Mandalorian", "Mandalorian Armada", "mandocap1", "Mandalorian"),
]

# A region is an orbit with a dock and its starlane neighbours. The centre's tier is the region's tier (the command
# ship's area and the region reward); every area spawns its own tier's waves, command group and blockades, so each fight
# stays in its band.
CENTERS = list(dict.fromkeys(ex[3] for ex in trade.EXCHANGES))
REGION_NAMES = {"viscaraorbit": "Viscara", "moncalaorbit": "Mon Cala", "hutlar_orbit": "Hutlar", "tatooineorbit": "Tatooine",
                "narshadorbit": "Smuggler's Moon", "space_dathomir": "Dathomir", "dantooineorbit": "Dantooine",
                "space_korriban": "Korriban"}


def region_areas(center):
    return [center] + list(trade.LANES[center])


# Each area of the region holds WAVES_PER_AREA waves, one at a time: the next arrives WAVE_DELAY_SECONDS after the last
# is destroyed. After the last wave the area's command group arrives (a capital and two gunships); destroying it
# liberates the area. Each planet dock of the region holds a blockade (two gunships and a defense platform) until it is
# destroyed. The region is cleared when every area is liberated and every blockade broken.
WAVES_PER_AREA = 5
WAVE_DELAY_SECONDS = 60
WAVE_RING_METERS = 10
BLOCKADE_RING_METERS = 15
BLOCKADE_HULLS = ["gunship", "gunship", "platform"]
COMMAND_ESCORTS = ["gunship", "gunship"]


def wave_hulls(tier):
    """The W12a combat site squad of the tier (scanning.combat_squad)."""
    return ["fighter", "fighter", "interceptor"] + (["bomber"] if tier >= 3 else []) + (["gunship"] if tier >= 5 else [])


def command_tag(tier, invader):
    capital = next(c for n, _, c, _ in INVADERS if n == invader)
    return capital if tier >= 6 else f"t{tier}capital"


# The region's exchanges (those whose dock launches into a region area) price every good at this multiple while the
# incursion lasts: supplies run short. Buying and selling both move, so hauling goods in pays and hoarding there costs.
PRICE_MULTIPLIER = 1.25

# Contribution. A destroyed incursion ship is worth points by difficulty (fight length against a Normal: the plan's T5
# platform takes 90 s and a capital about 340 s against a fighter's 36 s); they are split between the player ships that
# damaged it by damage dealt, and the killing blow adds KILL_BONUS of its value. Repairs on an ally in an incursion area
# count REPAIR_PER_POOL points per tier fighter's hull + shield restored. A ship's points are shared evenly by its pilot
# and its station crew (plan section 10, crew rewards).
KILL_EQUIVALENTS = {"Normal": 1.0, "Elite": 2.5, "Boss": 9.0}
KILL_BONUS = 0.10
REPAIR_PER_POOL = 0.5

# Payout when the region is cleared, per tier the points were earned at (each area's own tier): credits per point, one
# pull from the tier's SPACE_ELITE table per POINTS_PER_PULL points (at most MAX_PULLS), points paid up to
# MAX_PAID_POINTS (about ten hours of fighting). The region reward goes to every player with MIN_REGION_POINTS points:
# REGION_HOURS of the region tier's combat rate in credits, REGION_PULLS pulls and a SCHEMATIC_PERCENT chance at a Ship
# Schematic of the invader's maker (rare, not counted). Players offline at the clear are paid at their next login.
POINTS_PER_PULL = 20
MAX_PULLS = 20
MAX_PAID_POINTS = 600
MIN_REGION_POINTS = 5
REGION_HOURS = 0.4
REGION_PULLS = 2
SCHEMATIC_PERCENT = 10

# The model: an active participant fights ACTIVE_HOURS of the incursion at the baseline kill rate, the killing blow half
# the time (a pair splits the blows). Contribution credits fill what the kills' loot, the pulls and the region reward's
# share leave of TARGET.
TARGET = 1.30
ACTIVE_HOURS = 4
POINTS_PER_HOUR = trade.KILLS_PER_HOUR * (1 + KILL_BONUS / 2)

TIERS = [1, 2, 3, 4, 5, 6]


def combat_rate(tier_tenths):
    if tier_tenths == wildspace.TIER_TENTHS:
        return wildspace.combat_rate()[2]
    return trade.combat_credits_per_hour(tier_tenths // 10)[1]


def elite_table(tier_tenths):
    return "SPACE_ELITE_WS" if tier_tenths == wildspace.TIER_TENTHS else f"SPACE_ELITE_T{tier_tenths // 10}"


_PULL = {}


def pull_value(table):
    if table not in _PULL:
        _PULL[table] = trade.expected_value(trade.loot_tables()[table])
    return _PULL[table]


def region_credits(tier):
    return int(round(REGION_HOURS * combat_rate(tier * 10), -1))


def incursion_hourly(tier):
    """Per hour of active fighting: kill loot (the combat rate), the pulls and the region reward's share."""
    rate = combat_rate(tier * 10)
    pulls = POINTS_PER_HOUR / POINTS_PER_PULL * pull_value(elite_table(tier * 10))
    region = (region_credits(tier) + REGION_PULLS * pull_value(elite_table(tier * 10))) / ACTIVE_HOURS
    return rate, pulls, region


def credits_per_point(tier):
    rate, pulls, region = incursion_hourly(tier)
    return round(max(0.0, (TARGET * rate - rate - pulls - region) / POINTS_PER_HOUR), 1)


def incursion_percent(tier, hours=ACTIVE_HOURS):
    rate, pulls, region = incursion_hourly(tier)
    return 100 * (rate + pulls + region * ACTIVE_HOURS / hours + credits_per_point(tier) * POINTS_PER_HOUR) / rate


def fighter_pools():
    """The fighter whose hull + shield is one repair "pool" at each tier (RevampNpcShipCatalog)."""
    text = (REPO / "SWLOR.Game.Server/Feature/SpaceObjectDefinition/RevampNpcShipCatalog.cs").read_text(encoding="utf-8")
    pools = {}
    for t in TIERS:
        m = re.search(r'CreatureTag = "t%dfighter".*?Hull = (\d+), Shield = (\d+)' % t, text)
        pools[t] = int(m[1]) + int(m[2])
    return pools


# ---------------------------------------------------------------------------------------------------------------
# Rift Runs (plan section 31)
# ---------------------------------------------------------------------------------------------------------------
# A Rift Key (tier 1-5 and 5.5) opens a private pocket for the user's ship and up to two party ships within JOIN_METERS.
# The pocket is an instance of one template area (space_riftpocket, copied from Wild Space's The Maw without its
# signatures, environment, spawns or anchor) with three chambers, each a waypoint far from the others. The key is spent
# when the pocket opens. Each chamber's waves come one at a time (the next ROOM_WAVE_DELAY_SECONDS after the last is
# destroyed); clearing a chamber pulls the party into the next after ROOM_DELAY_SECONDS. Clearing the third within
# TIMER_SECONDS pays; the party returns to where it opened the pocket RETURN_DELAY_SECONDS later. When the timer runs out
# the pocket collapses: every ship in it is destroyed, so everyone aboard ejects in escape pods with the normal XP debt
# (plan section 9). A restart empties the pockets: ships in one return to their dock at boot, with no debt and no payout.
TEMPLATE_RESREF = "space_riftpocket"
TEMPLATE_NAME = "Rift Pocket"
TEMPLATE_SOURCE = "space_wsblkhole"
ROOM_WAYPOINT_PREFIX = "RIFT_ROOM_"
ROOM_POINTS = [(60.0, 60.0, 3.0), (160.5, 250.5, 3.0), (225.0, 130.0, 3.0)]
TIMER_SECONDS = 900
MAX_SHIPS = 3
JOIN_METERS = 30
MAX_RUNS = 10
ROOM_WAVE_DELAY_SECONDS = 5
ROOM_DELAY_SECONDS = 8
RETURN_DELAY_SECONDS = 15
ENEMY_OFFSET_METERS = 30
ENEMY_RING_METERS = 8
WARNING_SECONDS = [300, 60, 30, 10]

# (tier tenths, resref, ModelPart1). The keycard models of existing miscellaneous small items, one per tier; tier 5.5
# keeps W12c's blueprint and model.
KEYS = [(10, "rift_key_t1", 187), (20, "rift_key_t2", 196), (30, "rift_key_t3", 239), (40, "rift_key_t4", 240),
        (50, "rift_key_t5", 171), (55, "rift_key_t55", 197)]
KEY_TEMPLATE = "rift_key_t55"


def key_label(tier_tenths):
    return "5.5" if tier_tenths % 10 else str(tier_tenths // 10)


def key_prefix(tier_tenths):
    """The creature tag prefix of the pocket's ships: the tier's role ships, or the Rift Raiders at 5.5."""
    return "rr" if tier_tenths == wildspace.TIER_TENTHS else f"t{tier_tenths // 10}"


def key_for_tier(tier):
    """The key a source of this tier gives: tiers 1-5 their own, tier 6 the tier 5.5 key."""
    return next(r for t, r, _ in KEYS if t == tier * 10) if tier <= 5 else KEYS[-1][1]


# Chambers: each wave is a pair the solo baseline wins (checks.py section O); each extra ship adds one ship to every wave
# of the chamber, in order. The tier 5.5 Rift Raiders (level 52) beat a named-fit rank 45 pilot two at a time (checks.py
# section O), so their chambers send them one at a time, and they fly no defense platform: their heart is a gunship and
# its logistics ship.
ROOMS = [
    ("Outer Drift", [["fighter", "interceptor"], ["fighter", "interceptor"]], ["fighter", "fighter"]),
    ("Shear Zone", [["sniper", "ewar"], ["fighter", "bomber"]], ["interceptor", "interceptor"]),
    ("Rift Heart", [["platform"]], ["gunship", "fighter"]),
]
RIFT_RAIDER_ROOMS = [[["fighter"], ["interceptor"], ["fighter"], ["interceptor"]],
                     [["sniper"], ["ewar"], ["fighter"], ["bomber"]],
                     [["gunship"], ["logistics"]]]
HULL_DIFFICULTY = {"platform": "Elite", "carrier": "Boss", "capital": "Boss"}

# Payout on finishing in time: credits x (MIN_MULTIPLIER + TIME_SLOPE x the fraction of the timer left) and RUN_PULLS
# pulls from the tier's elite table (SPACE_ELITE_WS at 5.5), one more with at least BONUS_PULL_FRACTION of the timer
# left, and a NEXT_KEY_PERCENT chance at the next tier's key (rare, not counted). A ship's credits are shared evenly by
# its pilot and its station crew; its pulls go to the pilot's hold.
MIN_MULTIPLIER = 0.75
TIME_SLOPE = 1.0
RUN_PULLS = 2
BONUS_PULL_FRACTION = 1 / 3
NEXT_KEY_PERCENT = 10
RUN_TARGET = 1.30

# Salvage: an elite or boss wreck's rare find can be the key of its tier (5.5 in Wild Space and for tier 6), after every
# other rare-find share. Aces: rewards.ACE_LOOT_WEIGHTS["rift_key"] in each ace's own table (the tier's key).
SALVAGE_KEY_PERCENT = {"Elite": 5, "Boss": 10}

# Key value: what the run's premium over combat is worth (the key is consumed), at the 40% store markdown.
KEY_PREMIUM_MARKDOWN = 0.40
EXISTING_KEY_COST = 2000  # rift_key_t55 (W12c)


def room_waves(tier_tenths, room, ships):
    """The waves of a chamber for a party of this many ships, as hull names."""
    _, waves, extras = ROOMS[room]
    if key_prefix(tier_tenths) == "rr":
        waves = RIFT_RAIDER_ROOMS[room]
    added = extras[:max(0, ships - 1)]
    return [list(w) + added for w in waves]


def kill_equivalents(hulls):
    return sum(KILL_EQUIVALENTS[HULL_DIFFICULTY.get(h, "Normal")] for h in hulls)


def solo_kills(tier_tenths):
    return sum(kill_equivalents(w) for r in range(len(ROOMS)) for w in room_waves(tier_tenths, r, 1))


def run_minutes(tier_tenths):
    """The modeled solo run: every kill at the baseline pace, plus the two chamber pulls and opening the pocket."""
    return solo_kills(tier_tenths) * KILL_MINUTES + (2 * ROOM_DELAY_SECONDS + 15) / 60


def multiplier(fraction):
    return MIN_MULTIPLIER + TIME_SLOPE * fraction


def run_pulls(fraction):
    return RUN_PULLS + (1 if fraction >= BONUS_PULL_FRACTION else 0)


def fraction_left(tier_tenths):
    return max(0.0, 1 - run_minutes(tier_tenths) * 60 / TIMER_SECONDS)


def run_base_credits(tier_tenths):
    """Credits at a multiplier of 1, so the modeled solo run lands on RUN_TARGET with its kills and pulls."""
    rate = combat_rate(tier_tenths) / 60
    minutes = run_minutes(tier_tenths)
    left = fraction_left(tier_tenths)
    kills = solo_kills(tier_tenths) * rate * KILL_MINUTES
    pulls = run_pulls(left) * pull_value(elite_table(tier_tenths))
    return int(round(max(0.0, (RUN_TARGET * rate * minutes - kills - pulls) / multiplier(left)), -1))


def run_value(tier_tenths, fraction):
    rate = combat_rate(tier_tenths) / 60
    return (solo_kills(tier_tenths) * rate * KILL_MINUTES + run_base_credits(tier_tenths) * multiplier(fraction)
            + run_pulls(fraction) * pull_value(elite_table(tier_tenths)))


def key_cost(tier_tenths):
    if tier_tenths == wildspace.TIER_TENTHS:
        return EXISTING_KEY_COST
    premium = (RUN_TARGET - 1) * combat_rate(tier_tenths) / 60 * run_minutes(tier_tenths)
    return int(round(premium / KEY_PREMIUM_MARKDOWN / 50) * 50)


def key_name(tier_tenths):
    return f"Rift Key (Tier {key_label(tier_tenths)})"


def key_description(tier_tenths):
    origin = ("cut from a Rift Raider jump computer. It holds the coordinates of an unstable tier 5.5 rift pocket deep in "
              "Wild Space" if tier_tenths == wildspace.TIER_TENTHS else
              f"pulled from a navigation core. It holds the coordinates of an unstable tier {key_label(tier_tenths)} rift pocket")
    return (f"A navigation key {origin}. Use it in space, out of combat, to open the pocket for your ship and up to "
            f"{MAX_SHIPS - 1} party ships within {JOIN_METERS} m. Clear its three chambers within {TIMER_SECONDS // 60} minutes: "
            "the faster you finish, the more it pays. If time runs out the pocket collapses and everyone aboard ejects in "
            "escape pods. The key is spent when the pocket opens.")


# ---------------------------------------------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------------------------------------------
def report():
    print(f"Incursions: {START_WEEKDAY} {START_HOUR_UTC:02d}:00 UTC weekly, up to {DURATION_HOURS} h; "
          f"{POINTS_PER_HOUR:.0f} points an hour, {ACTIVE_HOURS} active hours, target {TARGET:.0%}")
    print("  Tier   combat/h  contrib cr  pulls  region  total/h     %  cr/point  region cr")
    for t in TIERS:
        rate, pulls, region = incursion_hourly(t)
        credits = credits_per_point(t) * POINTS_PER_HOUR
        total = rate + pulls + region + credits
        print(f"  T{t}  {rate:9,.0f} {credits:11,.0f} {pulls:6,.0f} {region:7,.0f} {total:8,.0f} "
              f"{100 * total / rate:5.0f}%  {credits_per_point(t):8.1f} {region_credits(t):10,}")
    for hours in (2, 8):
        pct = [incursion_percent(t, hours) for t in TIERS]
        print(f"  with {hours} active hours instead of {ACTIVE_HOURS}: {min(pct):.0f}-{max(pct):.0f}% across the tiers")
    print(f"Rift Runs: {TIMER_SECONDS // 60} min timer, target {RUN_TARGET:.0%} per minute of run (solo model)")
    print("   Key  combat/min  kill eq  minutes  left  base cr  run value  per min     %   cost  0 left  half left")
    for tenths, resref, _ in KEYS:
        rate = combat_rate(tenths) / 60
        minutes = run_minutes(tenths)
        left = fraction_left(tenths)
        total = run_value(tenths, left)

        def pct(f):
            m = TIMER_SECONDS * (1 - f) / 60
            return 100 * run_value(tenths, f) / m / rate

        print(f"  {key_label(tenths):>4} {rate:10.1f} {solo_kills(tenths):8.1f} {minutes:8.1f} {left:5.0%} "
              f"{run_base_credits(tenths):8,} {total:10,.0f} {total / minutes:8.1f} {100 * total / minutes / rate:5.0f}% "
              f"{key_cost(tenths):6,} {pct(0.0):6.0f}% {pct(0.5):9.0f}%")


# ---------------------------------------------------------------------------------------------------------------
# Generated C# and blueprints
# ---------------------------------------------------------------------------------------------------------------
HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/events.py. Do not edit by hand: change events.py and regenerate.
// </auto-generated>
"""


def q(text):
    return json.dumps(text)


def cs_double(x):
    return repr(float(x))


def cs_float(x):
    return f"{float(x)!r}f"


def cs_strings(items):
    return "new[] { " + ", ".join(q(x) for x in items) + " }"


def utc_appearance(tag):
    utc = json.loads((REPO / "Module" / "utc" / f"{tag}.utc.json").read_text(encoding="utf-8"))
    return utc["Appearance_Type"]["value"]


def incursion_cs():
    pools = fighter_pools()
    invaders = [f"            new IncursionInvaderDetail {{ Invader = IncursionInvader.{n}, Name = {q(name)}, CapitalTag = {q(cap)}, "
                f"CapitalAppearance = {utc_appearance(cap)}, Maker = ShipManufacturer.{maker} }},"
                for n, name, cap, maker in INVADERS]
    regions = []
    for c in CENTERS:
        areas = ", ".join(f"new IncursionArea {{ Resref = {q(a)}, Name = {q(trade.ORBIT_NAMES[a])}, Tier = {trade.ORBIT_TIERS[a]} }}"
                          for a in region_areas(c))
        regions.append(f"            new IncursionRegion {{ Center = {q(c)}, Name = {q(REGION_NAMES[c])}, Tier = {trade.ORBIT_TIERS[c]}, "
                       f"Areas = new[] {{ {areas} }} }},")
    waves = ", ".join(cs_strings(wave_hulls(t)) for t in TIERS)
    lines = [HEADER.rstrip("\n"), "using System;", "using System.Collections.Generic;", "",
             "namespace SWLOR.Game.Server.Service.SpaceService", "{",
             "    /// <summary>",
             "    /// The incursion numbers (plan section 30): the weekly slot, the regions, what each area spawns, the price spike,",
             "    /// contribution and the payout.",
             "    /// </summary>",
             "    public static class IncursionTuning", "    {",
             f"        public const DayOfWeek StartDay = DayOfWeek.{START_WEEKDAY};",
             f"        public const int StartHourUtc = {START_HOUR_UTC};",
             f"        public const double DurationHours = {cs_double(DURATION_HOURS)};",
             f"        public const double TickSeconds = {cs_double(TICK_SECONDS)};",
             f"        public const int WavesPerArea = {WAVES_PER_AREA};",
             f"        public const double WaveDelaySeconds = {cs_double(WAVE_DELAY_SECONDS)};",
             f"        public const float WaveRingMeters = {cs_float(WAVE_RING_METERS)};",
             f"        public const float BlockadeRingMeters = {cs_float(BLOCKADE_RING_METERS)};",
             f"        public const double PriceMultiplier = {cs_double(PRICE_MULTIPLIER)};",
             f"        public const double KillBonus = {cs_double(KILL_BONUS)};",
             f"        public const double RepairPerPool = {cs_double(REPAIR_PER_POOL)};",
             f"        public const double PointsPerPull = {cs_double(POINTS_PER_PULL)};",
             f"        public const int MaximumPulls = {MAX_PULLS};",
             f"        public const double MaximumPaidPoints = {cs_double(MAX_PAID_POINTS)};",
             f"        public const double MinimumRegionPoints = {cs_double(MIN_REGION_POINTS)};",
             f"        public const int RegionPulls = {REGION_PULLS};",
             f"        public const int SchematicPercent = {SCHEMATIC_PERCENT};",
             "",
             "        /// <summary>Points for a destroyed incursion ship by its preset's difficulty.</summary>",
             "        public static readonly IReadOnlyDictionary<string, double> KillEquivalents = new Dictionary<string, double> { "
             + ", ".join(f"[{q(k)}] = {cs_double(v)}" for k, v in KILL_EQUIVALENTS.items()) + " };",
             "",
             "        /// <summary>Credits per contribution point, by tier (index 1-6).</summary>",
             "        public static readonly double[] CreditsPerPoint = { 0, " + ", ".join(cs_double(credits_per_point(t)) for t in TIERS) + " };",
             "",
             "        /// <summary>The region reward's credits, by the region's tier (index 1-6).</summary>",
             "        public static readonly int[] RegionCredits = { 0, " + ", ".join(str(region_credits(t)) for t in TIERS) + " };",
             "",
             "        /// <summary>A tier fighter's hull + shield: a repair that restores it counts RepairPerPool points (index 1-6).</summary>",
             "        public static readonly int[] FighterPool = { 0, " + ", ".join(str(pools[t]) for t in TIERS) + " };",
             "",
             "        /// <summary>The hulls of one wave, by tier (index 1-6): the combat site squad.</summary>",
             f"        public static readonly string[][] WaveHulls = {{ Array.Empty<string>(), {waves} }};",
             f"        public static readonly IReadOnlyList<string> BlockadeHulls = {cs_strings(BLOCKADE_HULLS)};",
             f"        public static readonly IReadOnlyList<string> CommandEscorts = {cs_strings(COMMAND_ESCORTS)};",
             "",
             "        public static readonly IReadOnlyList<IncursionInvaderDetail> Invaders = new List<IncursionInvaderDetail>", "        {",
             *invaders, "        };", "",
             "        /// <summary>Every region: an orbit with a dock and its starlane neighbours.</summary>",
             "        public static readonly IReadOnlyList<IncursionRegion> Regions = new List<IncursionRegion>", "        {",
             *regions, "        };", "    }", "}", ""]
    return "\n".join(lines)


def rift_cs():
    keys = [f"            new RiftKeyDetail {{ Resref = {q(resref)}, TierTenths = {tenths}, Name = {q(key_name(tenths))}, "
            f"CreaturePrefix = {q(key_prefix(tenths))}, LootTable = {q(elite_table(tenths))}, BaseCredits = {run_base_credits(tenths)} }},"
            for tenths, resref, _ in KEYS]
    rooms = [f"            new RiftRoom {{ Name = {q(name)}, Waves = new[] {{ {', '.join(cs_strings(x) for x in waves)} }}, "
             f"Extras = {cs_strings(extras)} }},"
             for name, waves, extras in ROOMS]
    points = ", ".join(f"({cs_float(x)}, {cs_float(y)}, {cs_float(z)})" for x, y, z in ROOM_POINTS)
    lines = [HEADER.rstrip("\n"), "using System.Collections.Generic;", "",
             "namespace SWLOR.Game.Server.Service.SpaceService", "{",
             "    /// <summary>",
             "    /// The Rift Run numbers (plan section 31): the keys, the pocket, its chambers and timer, and the payout.",
             "    /// </summary>",
             "    public static class RiftRunTuning", "    {",
             f"        public const string TemplateResref = {q(TEMPLATE_RESREF)};",
             f"        public const string RoomWaypointPrefix = {q(ROOM_WAYPOINT_PREFIX)};",
             f"        public const int TimerSeconds = {TIMER_SECONDS};",
             f"        public const int MaxShips = {MAX_SHIPS};",
             f"        public const float JoinMeters = {cs_float(JOIN_METERS)};",
             f"        public const int MaxRuns = {MAX_RUNS};",
             f"        public const double WaveDelaySeconds = {cs_double(ROOM_WAVE_DELAY_SECONDS)};",
             f"        public const double RoomDelaySeconds = {cs_double(ROOM_DELAY_SECONDS)};",
             f"        public const double ReturnDelaySeconds = {cs_double(RETURN_DELAY_SECONDS)};",
             f"        public const float EnemyOffsetMeters = {cs_float(ENEMY_OFFSET_METERS)};",
             f"        public const float EnemyRingMeters = {cs_float(ENEMY_RING_METERS)};",
             f"        public const double MinimumMultiplier = {cs_double(MIN_MULTIPLIER)};",
             f"        public const double TimeSlope = {cs_double(TIME_SLOPE)};",
             f"        public const int RunPulls = {RUN_PULLS};",
             f"        public const double BonusPullFraction = {cs_double(BONUS_PULL_FRACTION)};",
             f"        public const int NextKeyPercent = {NEXT_KEY_PERCENT};",
             "",
             "        /// <summary>Seconds left on the timer at which the party is warned.</summary>",
             "        public static readonly IReadOnlyList<int> WarningSeconds = new[] { " + ", ".join(str(x) for x in WARNING_SECONDS) + " };",
             "",
             "        /// <summary>Where each chamber's arrival point stands when the template has no waypoint for it.</summary>",
             f"        public static readonly IReadOnlyList<(float X, float Y, float Z)> RoomPoints = new[] {{ {points} }};",
             "",
             "        /// <summary>Every Rift Key, tier 1-5 and 5.5. Every Resref string literal here is an item resref.</summary>",
             "        public static readonly IReadOnlyList<RiftKeyDetail> Keys = new List<RiftKeyDetail>", "        {",
             *keys, "        };", "",
             "        public static readonly IReadOnlyList<RiftRoom> Rooms = new List<RiftRoom>", "        {",
             *rooms, "        };", "",
             "        /// <summary>The Rift Raiders' chambers (tier 5.5): one ship at a time, and no defense platform.</summary>",
             "        public static readonly IReadOnlyList<IReadOnlyList<IReadOnlyList<string>>> RiftRaiderRooms = new[] { "
             + ", ".join("new[] { " + ", ".join(cs_strings(w) for w in room) + " }" for room in RIFT_RAIDER_ROOMS) + " };",
             "",
             "        /// <summary>Percent of an elite or boss wreck's rare finds that are its tier's Rift Key, after every other share.</summary>",
             "        public static readonly IReadOnlyDictionary<WreckDifficulty, int> SalvageKeyPercent = new Dictionary<WreckDifficulty, int> { "
             + ", ".join(f"[WreckDifficulty.{k}] = {v}" for k, v in SALVAGE_KEY_PERCENT.items()) + " };",
             "    }", "}", ""]
    return "\n".join(lines)


def key_blueprints():
    template = json.loads((REPO / "Module" / "uti" / f"{KEY_TEMPLATE}.uti.json").read_text(encoding="utf-8"))
    activate = json.loads((REPO / "Module" / "uti" / "spc_lockbox_t1.uti.json").read_text(encoding="utf-8"))["PropertiesList"]["value"]
    out = {}
    for tenths, resref, model in KEYS:
        d = json.loads(json.dumps(template))
        d["Cost"]["value"] = key_cost(tenths)
        d["DescIdentified"]["value"] = {"0": key_description(tenths)}
        d["Description"]["value"] = {"0": key_description(tenths)}
        d["LocalizedName"]["value"] = {"0": key_name(tenths)}
        d["ModelPart1"]["value"] = model
        d["xModelPart1"]["value"] = model
        d["PropertiesList"]["value"] = json.loads(json.dumps(activate))
        d["Tag"]["value"] = resref
        d["TemplateResRef"]["value"] = resref
        out[f"Module/uti/{resref}.uti.json"] = json.dumps(d, indent=2, ensure_ascii=False) + "\n"
    return out


def writes():
    files = {
        "SWLOR.Game.Server/Service/SpaceService/IncursionTuning.cs": incursion_cs(),
        "SWLOR.Game.Server/Service/SpaceService/RiftRunTuning.cs": rift_cs(),
    }
    files.update(key_blueprints())
    return files


# ---------------------------------------------------------------------------------------------------------------
# Bible tab: bible_tabs.py calls events_tab(Sheet) to write the Events (Revamp) tab.
# ---------------------------------------------------------------------------------------------------------------
def events_tab(Sheet):
    s = Sheet("Events (Revamp)", [30, 18, 14, 14, 14, 14, 14, 70])
    s.title("Events (Revamp): weekly incursions and Rift Runs")
    s.note("Dev Status: Proposed (space revamp W15b draft). Generated by tools/SpaceRevampBalance/events.py; plan sections 30 and 31.")
    s.blank()
    s.title("Incursions: schedule and spawns")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Schedule", f"{START_WEEKDAY} {START_HOUR_UTC:02d}:00 UTC", "", "", "", "", "",
         "weekly; the next start is saved, so a restart neither skips nor repeats a week"],
        ["Duration", f"up to {DURATION_HOURS} h", "", "", "", "", "", "the armada withdraws with no reward when time runs out"],
        ["Invader", ", ".join(n for _, n, _, _ in INVADERS), "", "", "", "", "", "picked at random; the region is never last week's"],
        ["Waves", f"{WAVES_PER_AREA} per area", "", "", "", "", "",
         f"one at a time, the next {WAVE_DELAY_SECONDS} s after the last is destroyed; the tier's combat site squad"],
        ["Command group", "capital + " + " + ".join(COMMAND_ESCORTS), "", "", "", "", "",
         "after an area's last wave; destroying it liberates the area"],
        ["Blockades", " + ".join(BLOCKADE_HULLS), "", "", "", "", "",
         "one at every planet dock of the region; while it holds, the dock refuses docking and its exchange is closed"],
        ["Price spike", f"x{PRICE_MULTIPLIER:g}", "", "", "", "", "", "the region's exchanges, buying and selling, while the incursion lasts"],
        ["Command ship", "tier capital, invader hull", "", "", "", "", "",
         "tiers 1-5: the tier's capital with the Inquisitor or Jahavey'ir appearance; tier 6: the Sith or Mandalorian capital itself"],
    ])
    s.title("Regions (an orbit with a dock and its starlane neighbours; each area fights at its own tier)")
    s.table(["Region", "Tier", "", "", "", "", "", "Areas"],
            [[REGION_NAMES[c], trade.ORBIT_TIERS[c], "", "", "", "", "",
              ", ".join(f"{trade.ORBIT_NAMES[a]} (T{trade.ORBIT_TIERS[a]})" for a in region_areas(c))] for c in CENTERS])
    s.title("Contribution and payout")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Kill", ", ".join(f"{k} {v:g}" for k, v in KILL_EQUIVALENTS.items()), "", "", "", "", "",
         "points per destroyed ship by difficulty, split by damage dealt"],
        ["Killing blow", f"+{KILL_BONUS:.0%}", "", "", "", "", "", "of the ship's points"],
        ["Repairs", f"{REPAIR_PER_POOL:g} per fighter pool", "", "", "", "", "", "hull and shield restored to allies in an incursion area"],
        ["Crew", "shared evenly", "", "", "", "", "", "a ship's points go to its pilot and station crew"],
        ["Item pulls", f"1 per {POINTS_PER_PULL} points", "", "", "", "", "", f"from the tier's SPACE_ELITE table, at most {MAX_PULLS}"],
        ["Paid points", f"up to {MAX_PAID_POINTS}", "", "", "", "", "", "per tier, about ten hours of fighting"],
        ["Region reward", f"{MIN_REGION_POINTS} points", "", "", "", "", "",
         f"{REGION_HOURS:g} h of the region tier's combat rate, {REGION_PULLS} pulls and a {SCHEMATIC_PERCENT}% chance at the invader's Ship Schematic"],
        ["Offline", "next login", "", "", "", "", "", "players offline at the clear are paid when they log in"],
    ])
    rows = []
    for t in TIERS:
        rate, pulls, region = incursion_hourly(t)
        credits = credits_per_point(t) * POINTS_PER_HOUR
        total = rate + pulls + region + credits
        rows.append([f"T{t}", round(rate), credits_per_point(t), region_credits(t), round(credits + pulls + region), round(total),
                     f"{100 * total / rate:.0f}%", ""])
    s.table(["Tier", "Combat / h", "Credits / point", "Region credits", "Rewards / h", "Total / h", "Of combat", "Notes"], rows)
    s.note(f"Model: {POINTS_PER_HOUR:.0f} points an hour (60 kills, the killing blow half the time), {ACTIVE_HOURS} active hours "
           f"per incursion for the region reward's share, kill loot at the combat rate. Target {TARGET:.0%} (band 120-140%).")
    s.title("Rift Runs")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Pocket", TEMPLATE_RESREF, "", "", "", "", "",
         f"one template area (copied from {TEMPLATE_SOURCE}), an instance per run, at most {MAX_RUNS} at once"],
        ["Ships", f"1-{MAX_SHIPS}", "", "", "", "", "",
         f"the key user and party ships within {JOIN_METERS} m; the key is spent when the pocket opens"],
        ["Timer", f"{TIMER_SECONDS // 60} min", "", "", "", "", "", "warnings at " + ", ".join(f"{x} s" for x in WARNING_SECONDS)],
        ["Chambers", "3 in sequence", "", "", "", "", "",
         f"waves one at a time ({ROOM_WAVE_DELAY_SECONDS} s apart); a cleared chamber pulls the party on after {ROOM_DELAY_SECONDS} s"],
        ["Time out", "escape pods", "", "", "", "", "", "every ship in the pocket is destroyed: normal XP debt, cargo spills, repair bill"],
        ["Payout", f"x{MIN_MULTIPLIER:g} + {TIME_SLOPE:g} x time left", "", "", "", "", "",
         f"credits; {RUN_PULLS} elite pulls, +1 with a third of the timer left, {NEXT_KEY_PERCENT}% chance at the next key"],
        ["Salvage", ", ".join(f"{k} {v}%" for k, v in SALVAGE_KEY_PERCENT.items()), "", "", "", "", "",
         "of an elite or boss wreck's rare finds"],
    ])
    s.table(["Chamber", "Waves (solo)", "", "", "", "", "", "Each extra ship adds"],
            [[n, " / ".join("+".join(w) for w in waves), "", "", "", "", "", ", ".join(extras)] for n, waves, extras in ROOMS])
    rows = []
    for tenths, resref, model in KEYS:
        rate = combat_rate(tenths) / 60
        minutes = run_minutes(tenths)
        left = fraction_left(tenths)
        total = run_value(tenths, left)
        rows.append([f"{key_name(tenths)} ({resref})", key_cost(tenths), run_base_credits(tenths), round(minutes, 1),
                     f"{left:.0%}", round(total), f"{100 * total / minutes / rate:.0f}%", f"model {model}; ships {key_prefix(tenths)}*"])
    s.table(["Key", "Cost", "Base credits", "Modeled min", "Time left", "Run value", "Of combat / min", "Notes"], rows)
    s.note(f"Model: the solo run at the baseline kill pace plus the chamber pulls; the kills' loot counts. Target {RUN_TARGET:.0%} "
           "per minute of run. Key cost: the run's premium over combat at the 40% store markdown (tier 5.5 keeps its W12c cost).")
    return s


def main():
    if "--sim" in sys.argv:
        report()
        return
    check = "--check" in sys.argv
    stale = []
    for rel, text in writes().items():
        path = REPO / rel
        if path.exists() and path.read_text(encoding="utf-8") == text:
            continue
        stale.append(rel)
        if not check:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text, encoding="utf-8", newline="\r\n")
    print(f"{len(CENTERS)} regions, {len(KEYS)} keys, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
