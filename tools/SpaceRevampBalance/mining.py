"""Space revamp active mining rate (W11e): ore and credits an hour of a mining laser pilot, from the code's own numbers.

usage: python mining.py           (writes the asteroid loot tables and SpaceMiningTuning.cs)
       python mining.py --check   (exit 1 when either differs from this file)
       python mining.py --sim     (prints ore and credits per hour by area against the tier's combat rate)

mining.py owns the sixteen asteroid loot tables (ASTEROID_* for mining lasers, ASTEROID_*_STRIP for strip miners) and
rewrites their blocks in place (the rest of HarvestingLootTableDefinition.cs is hand-written), and generates
SpaceMiningTuning.cs (the asteroid size by tier). bible_tabs.py's income summary reads rows(); stations.py's Asteroid
Drill values the same tables and agents.py's mining quotas use open_orbit_ore_per_hour().

Sources (the code, not assumptions):
- Mining Laser: catalog.py FAMILIES (6 s cycle, 15 m range, mines asteroids up to tier rank / 10 + 1), a High module.
  Revamp lasers carry no Module Bonus property and no hull sets the legacy Industrial bonus, so a cycle's amount is
  ExtractionBaseAmount(0, 0) = 1 and it uses MiningUnitsPerCycle(1) = 1 of the asteroid's units. Prospector I-III add
  5 / 10 / 15% to the ore delivered, the fraction carried on the ship (SpaceCombatMath.MiningYield; W11e, was a flat
  +1 / +2 / +3 ore, which quadrupled a 1-ore cycle).
- Rapid Extraction I / II shorten the cycle 10 / 20% (SpaceCombatMath.MiningCycleSeconds).
- An asteroid holds 1d4 + 2 units at its tier's ASTEROID_UNIT_PERCENT (SpaceCombatMath.AsteroidUnits). The extraction hold is per ship, so every
  laser of one ship works the same asteroid at once; a cycle that ends after the rock is gone yields nothing.
- Each laser cycle also rolls the asteroid's LOOT_TABLE_n locals (Loot.SpawnLoot; LYASE_TYPE_1 at 11%).
- Deep Core I / II (SpaceRareMetalChance 10 / 20), the mining module's RareFind trait (one Extraction Head per laser or
  strip miner: Precision or Czerka-pattern +5 at best) and a bay droid's Prospecting Protocol I / II (+5 / +10) add
  weight to the asteroid table's rare row on every ore pick, as Treasure Hunter does on the ground
  (LootTable.GetRandomItemWithRareBonus; review fix 2026-10-30, was a separate percent roll).
- A droid in a Mining Barge's or Industrial's bay adds BayYieldPercent to the ore delivered, rounded half up
  (DroidIndustryRules.ApplyPercent: nothing on a 1-ore cycle).
- Ore value: the asteroid's loot table at the 40% NPC store price (stations.py's drill uses the same valuation).
- Travel: an area is 32 x 32 tiles (320 m square) with RESOURCE_SPAWN_COUNT nodes (50 in an open orbit, 75 in a
  zone; Spawn.CalculateResourceSpawnCount), respawning 5 minutes after they are mined out (Spawn.DefaultRespawnMinutes).
  The pilot flies to the nearest minable asteroid: half of the area's minable asteroids are taken as available (the
  rest mined out or claimed by others), the mean nearest-neighbour distance of that density, less the laser's range,
  at the PC run rate (creaturespeed.2da, 4 m/s) times the hull's speed, plus RETARGET_SECONDS to target and toggle.
- Strip Miner (Large and Capital hulls; the Industrial's gate is 25, so tier 3 up): one 10 s cycle strips the whole
  asteroid for what a laser would pull from it (SpaceCombatMath.StripMinerYield); the ship is rooted, one rock a cycle.
- The matched pilot: rank 10 x tier - 5 (T1 rank 5 flies a Fighter, below the Mining Barge's Piloting gate of 10;
  tier 2 up a Mining Barge), three High slots of Mining Lasers, no perks unless stated. Wild Space is tier 5 content.
"""
import json
import math
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import droids  # noqa: E402
import model as m  # noqa: E402
import scanning  # noqa: E402
import trade  # noqa: E402
import wildspace  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
CYCLE_SECONDS = 6.0
LASER_RANGE = 15
LASERS = 3
ASTEROID_UNITS = [3, 4, 5, 6]
AREA_METERS = 320
OPEN_ORBIT_NODES = 50
AVAILABLE_SHARE = 0.5
PC_RUN_METERS_PER_SECOND = 4.0
RETARGET_SECONDS = 4.0
STORE_BUY_RATE = 0.40
PROSPECTOR_PERCENT = [0, 5, 10, 15]     # Prospector I-III: percent on each cycle's ore, remainder carried (W11e)
PROSPECTOR = [0, 1, 2, 3]               # ranks
RAPID_EXTRACTION = [0, 10, 20]
WILD_SPACE_TIER = 5.5

# Balance pass (W11e): the asteroid loot tables, at the 40% store price, paid a matched miner 91-142% of combat in
# tiers 1-3 but 274-397% at tiers 4-5, 494% in Wild Space and 635% in the Korriban Debris Field, and strip miners
# 3-17x, mostly from the metal and gem by-products (a diamond is 1,300 credits beside 38 for Gostian ore) and the
# Ardanium rocks. The ore value was the first lever (cycle time untouched; the rock sizes below came second): from tier 3 the ore rows carry
# the weight, so a by-product is 0.3-1.2% of picks instead of 6-9%; an Ardanium rock gives Ardanium one pick in nine
# (Gostian otherwise); strip tables were 4-5x richer in by-products and now follow the laser tables (STRIP_* below).
# Tiers 1-2 keep their tables. The Korriban Debris Field's spawn table mixes
# Idailia, Barinium and Gostian rocks (SpaceResourceSpawnDefinition, edited by hand in this pass), since Gostian ore
# alone at a zone's density paid 148% of tier 5 combat. Ore item values stay those of the matching ground ores, and
# rare rows stay rare (Deep Core picks them).
# (resref, weight, rare) per table, before RARE_SCALE below; every *_STRIP table repeats its laser table.
# Rare rows (review fix 2026-10-30): tiers 1-4 flag their metal by-product (aluminum, steel, obsidian, crystal), worth more
# than the ore; their mineral (quadrenium ... katrium, 2 credits) was the rare row, so Deep Core lowered income there.
# Tier 5 rocks keep Zinsiam (1,200 credits).
ASTEROID_BASE = {
    "ASTEROID_TILARIUM": [("ore_tilarium", 30, False), ("ore_durelium", 4, False), ("aluminum", 2, True), ("quadrenium", 1, False)],
    "ASTEROID_CURRIAN": [("ore_currian", 30, False), ("ore_durelium", 4, False), ("steel", 2, True), ("vintrium", 1, False)],
    "ASTEROID_DURELIUM_1": [("ore_durelium", 30, False), ("ore_tilarium", 10, False), ("aluminum", 2, True), ("quadrenium", 1, False)],
    "ASTEROID_DURELIUM_2": [("ore_durelium", 30, False), ("ore_currian", 10, False), ("steel", 2, True), ("vintrium", 1, False)],
    "ASTEROID_IDAILIA": [("ore_idailia", 240, False), ("obsidian", 2, True), ("ionite", 1, False)],
    "ASTEROID_BARINIUM": [("ore_barinium", 480, False), ("crystal", 2, True), ("katrium", 1, False)],
    "ASTEROID_GOSTIAN": [("ore_gostian", 960, False), ("diamond", 2, False), ("zinsiam", 1, True)],
    "ASTEROID_ARDANIUM": [("ore_arda", 32, False), ("ore_gostian", 256, False), ("zinsiam", 1, True)],
}
# Strip tables: the laser table, with the by-product and rare rows of tier 3-5 rocks at 3x (a stripped rock takes a
# 10 s cycle and a slow hull's travel whatever its size, so this keeps strip mining level with three lasers: 78-107%).
STRIP_BYPRODUCT_FACTOR = 3
STRIP_RICH_TABLES = ["ASTEROID_IDAILIA", "ASTEROID_BARINIUM", "ASTEROID_GOSTIAN", "ASTEROID_ARDANIUM"]
ASTEROID_BASE.update({f"{k}_STRIP": [(r, w if r.startswith("ore_") or k not in STRIP_RICH_TABLES else w * STRIP_BYPRODUCT_FACTOR,
                                       rare) for r, w, rare in v] for k, v in list(ASTEROID_BASE.items())})

# Rare-metal bonus (review fix 2026-10-30). Deep Core was a separate percent roll for the rare row: the best legal stack
# (Deep Core II 20 + a +5 RareFind Extraction Head + Prospecting Protocol II 10 = 35 points) turned 35% of picks into
# Zinsiam at tier 5 (1,200 credits beside 38 for Gostian ore: 7-10x a tier 5 table's pick value) and into 2-credit
# minerals at tiers 1-4 (Deep Core lowered income). The points now add weight to the rare row, as Treasure Hunter's do on
# the ground, and each table's weights are scaled by RARE_SCALE so the best stack adds at most RARE_FIND_TARGET of the
# table's value at every tier (strip tables STRIP_RARE_FIND_TARGET; on a laser table Deep Core II alone adds about 3.4%,
# I 1.7%, and the best stack about a third more of the rare metal) without lifting the best pilot build past the 140% band. Scaling keeps every row's share, so the no-perk value
# and the Asteroid Drill do not move.
DEEP_CORE = [0, 10, 20]                 # SpaceRareMetalChance by rank (GatheringPerkDefinition)
PROSPECTING = [0, 5, 10]                # Prospecting Protocol rare-find points by rank (droids.PROTOCOLS)
BEST_TRAIT_RARE_FIND = 5                # Precision or Czerka-pattern: a mining module takes one Extraction Head
RARE_FIND_TARGET = 0.06
# Strip tables already carry 3x the rare row at tiers 3-5, and Prospector III with Rapid Extraction II puts a Tatooine
# strip miner at 136% before any rare metal, so a strip table's best stack adds at most 2.5%.
STRIP_RARE_FIND_TARGET = 0.025
BEST_RARE_POINTS = DEEP_CORE[-1] + BEST_TRAIT_RARE_FIND + PROSPECTING[-1]


def _rare_scale(rows, target):
    """The smallest whole factor on the table's weights at which BEST_RARE_POINTS add at most the target share."""
    weight = sum(w for _, w, _ in rows)
    mean = sum(w * trade.item_cost(r) for r, w, _ in rows) / weight
    rare = [(r, w) for r, w, x in rows if x]
    rare_value = sum(w * trade.item_cost(r) for r, w in rare) / sum(w for _, w in rare)
    needed = BEST_RARE_POINTS * (rare_value - mean) / (target * mean) - BEST_RARE_POINTS
    return max(1, math.ceil(needed / weight))


RARE_SCALE = {k: _rare_scale(v, STRIP_RARE_FIND_TARGET if k.endswith("_STRIP") else RARE_FIND_TARGET)
              for k, v in ASTEROID_BASE.items()}
ASTEROID_TABLES = {k: [(r, w * RARE_SCALE[k], rare) for r, w, rare in v] for k, v in ASTEROID_BASE.items()}
LOOT_FILE = REPO / "SWLOR.Game.Server" / "Feature" / "LootTableDefinition" / "HarvestingLootTableDefinition.cs"
STRIP_CYCLE_SECONDS = 10.0
STRIP_HULL = "Industrial"
STRIP_MINIMUM_TIER = 3
# Asteroid size by asteroid tier, percent of the 1d4 + 2 roll, rounded half up (W11e; SpaceMiningTuning). Ore item values
# match the ground ores and climb faster than the tier combat rates (Gostian 38 credits a unit at tier 5 beside Tilarium 8
# at tier 1, against combat 9,347 and 4,055 an hour), so the higher rocks hold fewer units: the yield an hour, not the
# value, comes down. With the tables above a no-perk miner earns 88-102% of tier 1-5 combat (Mid Rim Nebula 74%, a gas
# zone; Wild Space 78%, its combat pays 17% more; tier 6 64%, it has no tier 6 ore).
ASTEROID_UNIT_PERCENT = {1: 85, 2: 100, 3: 70, 4: 60, 5: 55}
TUNING_FILE = REPO / "SWLOR.Game.Server" / "Service" / "SpaceService" / "SpaceMiningTuning.cs"

# Areas with asteroids: orbits and zones by trade.py's tier, Wild Space systems at tier 5.5.
AREAS = [a for a in trade.ORBIT_TIERS] + list(wildspace.SYSTEM_NAMES)


def _cs(*parts):
    return (REPO / "SWLOR.Game.Server" / Path(*parts)).read_text(encoding="utf-8")


def _tables(text):
    tables = {}
    for block in re.split(r'_builder\.Create\(', text)[1:]:
        name = re.match(r'"([A-Z0-9_]+)"', block).group(1)
        tables[name] = [(r, int(w)) for r, w in re.findall(r'\.AddItem\("([^"]+)",\s*(\d+)', block)]
    return tables


HARVEST = _tables(_cs("Feature", "LootTableDefinition", "HarvestingLootTableDefinition.cs"))
HARVEST.update({k: [(r, w) for r, w, _rare in v] for k, v in ASTEROID_TABLES.items()})  # the model reads the owned tables
ENZYMES = _tables(_cs("Feature", "LootTableDefinition", "EnzymeLootTableDefinition.cs"))
SPAWNS = {}
for _block in re.split(r'_builder\.Create\(', _cs("Feature", "SpawnDefinition", "SpaceResourceSpawnDefinition.cs"))[1:]:
    SPAWNS[re.match(r'"([A-Z0-9_]+)"', _block).group(1)] = [
        (r, int(w)) for r, w in re.findall(r'AddSpawn\(ObjectType\.Placeable,\s*"([^"]+)"\)\s*\.WithFrequency\((\d+)\)', _block)]


def area_locals(area):
    data = json.loads((REPO / "Module" / "git" / f"{area}.git.json").read_text(encoding="latin-1"))
    return {v["Name"]["value"]: v["Value"]["value"] for v in data.get("VarTable", {}).get("value", [])}


def placeable_locals(resref):
    data = json.loads((REPO / "Module" / "utp" / f"{resref}.utp.json").read_text(encoding="latin-1"))
    return {v["Name"]["value"]: v["Value"]["value"] for v in data.get("VarTable", {}).get("value", [])}


def table_value(rows, rare_points=0, rare=()):
    """One pick's value at the store price; rare_points add to each rare row's weight (LootTable.GetRandomItemWithRareBonus)."""
    weighted = [(r, w + (rare_points if r in rare else 0)) for r, w in rows]
    total = sum(w for _, w in weighted)
    return sum(w / total * trade.item_cost(r) * STORE_BUY_RATE for r, w in weighted)


def owned_value(table, rare_points=0):
    """One ore pick from an owned asteroid table with the pilot's rare-metal points."""
    rows = ASTEROID_TABLES[table]
    return table_value([(r, w) for r, w, _ in rows], rare_points, {r for r, _, x in rows if x})


def rare_gain(table, rare_points):
    """The share the rare-metal points add to a table's pick value."""
    return owned_value(table, rare_points) / owned_value(table) - 1


def area_tier(area):
    return WILD_SPACE_TIER if area in wildspace.SYSTEM_NAMES else trade.ORBIT_TIERS[area]


def laser_tier(tier):
    rank = 10 * min(5, math.floor(tier)) - 5
    return rank // 10 + 1


def hull(tier):
    return "Fighter" if tier < 2 else "Mining Barge"


def asteroids(area, rare_points=0):
    """[(frequency, ore value per unit, bonus loot value per cycle, asteroid tier)] of the asteroids a matched laser can
    mine, the area's node count and the spawn table's total frequency (gas clouds and higher-tier rocks take nodes)."""
    loc = area_locals(area)
    rows = SPAWNS.get(loc.get("RESOURCE_SPAWN_TABLE_ID", ""), [])
    out, total = [], sum(w for _, w in rows)
    for resref, weight in rows:
        p = placeable_locals(resref)
        table = p.get("ASTEROID_LOOT_TABLE_ID")
        tier = int(p.get("ASTEROID_TIER", 1))
        if not table or tier > laser_tier(area_tier(area)):
            continue
        bonus = 0.0
        for key, value in p.items():
            if key.startswith("LOOT_TABLE_"):
                name, chance, attempts = [x.strip() for x in str(value).split(",")]
                rows_b = ENZYMES.get(name) or HARVEST.get(name) or []
                if rows_b:
                    bonus += int(chance) / 100 * int(attempts) * table_value(rows_b)
        unit = owned_value(table, rare_points) if table in ASTEROID_TABLES else table_value(HARVEST[table])
        out.append((weight, unit, bonus, tier))
    nodes = int(loc.get("RESOURCE_SPAWN_COUNT", OPEN_ORBIT_NODES))
    return out, nodes, total


def asteroid_units(roll, tier):
    """An asteroid's units: the 1d4 + 2 roll at its tier's ASTEROID_UNIT_PERCENT, rounded half up, at least 1
    (SpaceCombatMath.AsteroidUnits)."""
    return max(1, (roll * ASTEROID_UNIT_PERCENT[tier] + 50) // 100)


def asteroid_haul(tier, lasers=LASERS):
    """Average (ore before Prospector, laser cycles, cycle batches) per asteroid of a tier: every laser on the same rock,
    1 ore and 1 unit a cycle (the base amount of a revamp laser)."""
    ore = cycles = batches = 0.0
    for roll in ASTEROID_UNITS:
        k = asteroid_units(roll, tier)
        ore += k
        cycles += k
        batches += math.ceil(k / lasers)
    n = len(ASTEROID_UNITS)
    return ore / n, cycles / n, batches / n


def prospector_factor(rank):
    """Prospector's percent on every cycle's ore, the remainder carried (SpaceCombatMath.MiningYield)."""
    return 1 + PROSPECTOR_PERCENT[rank] / 100


def has_bay(hull_name):
    """Whether the hull has a droid bay that adds to mining (droids.BAY_YIELD_ROLES): Mining Barge and Industrial."""
    return hull_name.replace(" ", "") in droids.BAY_YIELD_ROLES


def best_rare_points(hull_name):
    """Deep Core II and a +5 RareFind module, plus Prospecting Protocol II where the hull has a droid bay."""
    return DEEP_CORE[-1] + BEST_TRAIT_RARE_FIND + (PROSPECTING[-1] if has_bay(hull_name) else 0)


def bay_factor(rank, bay_percent):
    """The droid bay's rounded percent on a cycle's ore (DroidIndustryRules.ApplyPercent): with 1 ore a cycle it adds
    nothing, so only the cycles Prospector's carry lifts to 2 ore gain (2 x 1.35 rounds to 3)."""
    if bay_percent <= 0:
        return 1.0
    extra = PROSPECTOR_PERCENT[rank] / 100  # the share of cycles that deliver 2 ore
    two = max(2, math.floor(2 * (1 + bay_percent / 100) + 0.5))
    return ((1 - extra) * 1 + extra * two) / (1 + extra)


def travel_seconds(area, hull_name=None):
    rows, nodes, total = asteroids(area)
    minable = nodes * sum(r[0] for r in rows) / total * AVAILABLE_SHARE
    distance = 0.5 * AREA_METERS / math.sqrt(minable)
    speed = PC_RUN_METERS_PER_SECOND * m.ROLES[hull_name or hull(area_tier(area))]["speed"]
    return max(0.0, distance - LASER_RANGE) / speed + RETARGET_SECONDS


def mining_rate(area, prospector=0, rapid=0, bay_percent=0, rare_points=0):
    """(ore an hour, credits an hour) of a matched pilot mining the area's asteroids with three lasers. A bay droid's
    yield needs a hull with a droid bay (the tier 1 Fighter has none)."""
    rows, _nodes, _total = asteroids(area, rare_points)
    if not rows:
        return 0.0, 0.0
    weight = sum(r[0] for r in rows)
    travel = travel_seconds(area)
    if not has_bay(hull(area_tier(area))):
        bay_percent = 0
    factor = prospector_factor(prospector) * bay_factor(prospector, bay_percent)
    ore = value = seconds = 0.0
    for w, unit, bonus, tier in rows:
        o, cycles, batches = asteroid_haul(tier)
        share = w / weight
        ore += share * o * factor
        value += share * (o * factor * unit + cycles * bonus)
        seconds += share * (batches * CYCLE_SECONDS * (100 - rapid) / 100 + travel)
    return ore * 3600 / seconds, value * 3600 / seconds


def strip_rate(area, prospector=0, rapid=0, rare_points=0):
    """Credits an hour of a matched Industrial stripping the area's asteroids (tier 3 up), or 0 below that: one rock a
    10 s cycle for every unit it holds (SpaceCombatMath.StripMinerYield at 1 ore a cycle)."""
    if area_tier(area) < STRIP_MINIMUM_TIER:
        return 0.0
    rows, _nodes, _total = asteroids(area, rare_points)
    loc = area_locals(area)
    strip = {}
    for resref, _w in SPAWNS.get(loc.get("RESOURCE_SPAWN_TABLE_ID", ""), []):
        p = placeable_locals(resref)
        if p.get("STRIPMINE_LOOT_TABLE_ID"):
            strip[(p.get("ASTEROID_LOOT_TABLE_ID"))] = owned_value(p["STRIPMINE_LOOT_TABLE_ID"], rare_points)
    weight = sum(r[0] for r in rows)
    travel = travel_seconds(area, STRIP_HULL)
    seconds = STRIP_CYCLE_SECONDS * (100 - rapid) / 100 + travel
    value = 0.0
    for (w, unit, _bonus, tier), strip_unit in zip(rows, [strip.get(t) for t in _laser_tables(area)]):
        o = asteroid_haul(tier)[0] * prospector_factor(prospector)
        value += w / weight * o * (strip_unit if strip_unit is not None else unit)
    return value * 3600 / seconds


def _laser_tables(area):
    loc = area_locals(area)
    out = []
    for resref, _w in SPAWNS.get(loc.get("RESOURCE_SPAWN_TABLE_ID", ""), []):
        p = placeable_locals(resref)
        if p.get("ASTEROID_LOOT_TABLE_ID") and int(p.get("ASTEROID_TIER", 1)) <= laser_tier(area_tier(area)):
            out.append(p["ASTEROID_LOOT_TABLE_ID"])
    return out


def open_orbit_ore_per_hour(tier=3):
    """Ore an hour of a matched Mining Barge with three lasers in an open orbit of the tier's asteroids (no perks)."""
    o, _cycles, batches = asteroid_haul(tier)
    distance = 0.5 * AREA_METERS / math.sqrt(OPEN_ORBIT_NODES * AVAILABLE_SHARE)
    travel = max(0.0, distance - LASER_RANGE) / (PC_RUN_METERS_PER_SECOND * m.ROLES["Mining Barge"]["speed"]) + RETARGET_SECONDS
    return o * 3600 / (batches * CYCLE_SECONDS + travel)


def combat_rate(area):
    t = area_tier(area)
    return wildspace.combat_rate()[2] if t == WILD_SPACE_TIER else scanning.combat_rate(t)


BEST_BAY_PERCENT = round(droids.BAY_YIELD_PERCENT_PER_INDUSTRY * droids.MAXIMUM_INDUSTRY * 1.4)


BAND_MAXIMUM = 1.40  # plan section 19: mining with the perks at most 140% of the tier's combat


def rows():
    out = []
    p, r = PROSPECTOR[-1], RAPID_EXTRACTION[-1]
    for area in AREAS:
        if not asteroids(area)[0]:
            continue
        ore, credits = mining_rate(area)
        _, perks = mining_rate(area, p, r)
        points = best_rare_points(hull(area_tier(area)))
        _, rare = mining_rate(area, p, r, 0, points)
        _, best = mining_rate(area, p, r, BEST_BAY_PERCENT, points)
        out.append(dict(area=area, tier=area_tier(area), ore=ore, credits=credits, perks=perks, rare=rare, best=best,
                        rare_points=points, strip=strip_rate(area), perk_strip=strip_rate(area, p, r),
                        rare_strip=strip_rate(area, p, r, best_rare_points(STRIP_HULL)), combat=combat_rate(area),
                        travel=travel_seconds(area)))
    return out


def band_failures(table):
    """The pilot builds above BAND_MAXIMUM: Prospector III, Rapid Extraction II, Deep Core II, the best RareFind trait and
    (bay hulls) Prospecting Protocol II, lasers and strip. A bay droid's ore on top is reported, not held to the band."""
    return [(r["area"], key, r[key] / r["combat"]) for r in table for key in ("rare", "rare_strip")
            if r[key] > BAND_MAXIMUM * r["combat"]]


def report():
    """Prints the rates and returns False when a pilot build breaks the band."""
    print("Active mining, a matched pilot with three Mining Lasers (Fighter at T1, Mining Barge from T2), against the "
          "tier's combat rate. 'perks': Prospector III and Rapid Extraction II; 'rare': plus Deep Core II, a +5 RareFind "
          f"Extraction Head and (bay hulls) Prospecting Protocol II; 'best': plus a best bay droid's ore (+{BEST_BAY_PERCENT}%).")
    print("  Rare-metal weight scale (Deep Core II / best stack): " + ", ".join(
        f"{k[9:]} x{v} (+{100 * rare_gain(k, DEEP_CORE[-1]):.1f}% / +{100 * rare_gain(k, BEST_RARE_POINTS):.1f}%)"
        for k, v in RARE_SCALE.items() if not k.endswith("_STRIP")))
    table = rows()
    for r in sorted(table, key=lambda r: (r["tier"], r["area"])):
        c = r["combat"]
        print(f"  T{r['tier']:<4} {r['area']:18s} {r['ore']:5.0f} ore/h {r['credits']:7.0f}/h ({100 * r['credits'] / c:4.0f}%), "
              f"perks {100 * r['perks'] / c:4.0f}%, rare ({r['rare_points']} pts) {r['rare']:7.0f}/h ({100 * r['rare'] / c:4.0f}%), "
              f"best {100 * r['best'] / c:4.0f}%; strip {100 * r['strip'] / c:4.0f}% (perks {100 * r['perk_strip'] / c:4.0f}%, "
              f"rare {100 * r['rare_strip'] / c:4.0f}%); travel {r['travel']:.1f} s")
    failures = band_failures(table)
    for area, key, share in failures:
        print(f"  BAND FAIL: {area} {key} {100 * share:.0f}% > {100 * BAND_MAXIMUM:.0f}%")
    over = [r for r in table if r["best"] > BAND_MAXIMUM * r["combat"]]
    if over:
        print("  A best bay droid's ore puts these above 140% (plan: the droid adds on top of the band): "
              + ", ".join(f"{r['area']} {100 * r['best'] / r['combat']:.0f}%" for r in over))
    return not failures


def table_block(name):
    rows = ASTEROID_TABLES[name]
    lines = ['            _builder.Create("' + name + '")']
    for i, (resref, weight, rare) in enumerate(rows):
        end = ";" if i == len(rows) - 1 else ""
        lines.append('                .AddItem("' + resref + '", ' + str(weight) + (", isRare: true" if rare else "") + ")" + end)
    return "\n".join(lines)


def loot_file_text():
    """HarvestingLootTableDefinition.cs with every owned table's block written from ASTEROID_TABLES."""
    text = LOOT_FILE.read_bytes().decode("utf-8")
    crlf = "\r\n" in text
    text = text.replace("\r\n", "\n")
    for name in ASTEROID_TABLES:
        pattern = re.compile(r'            _builder\.Create\("' + name + r'"\)\n(?:                \.AddItem\([^\n]*\)\n)*'
                             r'                \.AddItem\([^\n]*\);')
        assert len(pattern.findall(text)) == 1, name
        text = pattern.sub(lambda _m, n=name: table_block(n), text)
    return text.replace("\n", "\r\n") if crlf else text


def tuning_text():
    values = ", ".join(str(ASTEROID_UNIT_PERCENT[t]) for t in sorted(ASTEROID_UNIT_PERCENT))
    return "\r\n".join([
        "// <auto-generated>",
        "// Generated by tools/SpaceRevampBalance/mining.py. Do not edit by hand: change mining.py and regenerate.",
        "// </auto-generated>",
        "using System;",
        "",
        "namespace SWLOR.Game.Server.Service.SpaceService",
        "{",
        "    /// <summary>The asteroid mining numbers (W11e balance pass): an asteroid's size by its tier.</summary>",
        "    public static class SpaceMiningTuning",
        "    {",
        "        /// <summary>Percent of the 1d4 + 2 unit roll an asteroid of each tier (index 1-5) holds.</summary>",
        f"        public static readonly int[] AsteroidUnitPercentByTier = {{ 100, {values} }};",
        "",
        "        /// <summary>An asteroid tier's unit percent; tiers outside 1-5 use the nearest one.</summary>",
        "        public static int AsteroidUnitPercent(int asteroidTier) =>",
        "            AsteroidUnitPercentByTier[Math.Clamp(asteroidTier, 1, AsteroidUnitPercentByTier.Length - 1)];",
        "    }",
        "}",
        "",
    ])


def main():
    if "--sim" in sys.argv:
        if not report():
            sys.exit(1)
        return
    check = "--check" in sys.argv
    stale = 0
    for path, text in ((LOOT_FILE, loot_file_text()), (TUNING_FILE, tuning_text())):
        if path.exists() and path.read_bytes().decode("utf-8") == text:
            continue
        stale += 1
        if not check:
            path.write_bytes(text.encode("utf-8"))
    print(f"{len(ASTEROID_TABLES)} asteroid tables, {stale} file(s) {'out of date' if check else 'written'}")
    if stale and check:
        sys.exit(1)


if __name__ == "__main__":
    main()
