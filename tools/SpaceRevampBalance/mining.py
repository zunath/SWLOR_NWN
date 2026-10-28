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
# (resref, weight, rare) per table; every *_STRIP table repeats its laser table.
ASTEROID_TABLES = {
    "ASTEROID_TILARIUM": [("ore_tilarium", 30, False), ("ore_durelium", 4, False), ("aluminum", 2, False), ("quadrenium", 1, True)],
    "ASTEROID_CURRIAN": [("ore_currian", 30, False), ("ore_durelium", 4, False), ("steel", 2, False), ("vintrium", 1, True)],
    "ASTEROID_DURELIUM_1": [("ore_durelium", 30, False), ("ore_tilarium", 10, False), ("aluminum", 2, False), ("quadrenium", 1, True)],
    "ASTEROID_DURELIUM_2": [("ore_durelium", 30, False), ("ore_currian", 10, False), ("steel", 2, False), ("vintrium", 1, True)],
    "ASTEROID_IDAILIA": [("ore_idailia", 240, False), ("obsidian", 2, False), ("ionite", 1, True)],
    "ASTEROID_BARINIUM": [("ore_barinium", 480, False), ("crystal", 2, False), ("katrium", 1, True)],
    "ASTEROID_GOSTIAN": [("ore_gostian", 960, False), ("diamond", 2, False), ("zinsiam", 1, True)],
    "ASTEROID_ARDANIUM": [("ore_arda", 32, False), ("ore_gostian", 256, False), ("zinsiam", 1, True)],
}
# Strip tables: the laser table, with the by-product and rare rows of tier 3-5 rocks at 3x (a stripped rock takes a
# 10 s cycle and a slow hull's travel whatever its size, so this keeps strip mining level with three lasers: 78-107%).
STRIP_BYPRODUCT_FACTOR = 3
STRIP_RICH_TABLES = ["ASTEROID_IDAILIA", "ASTEROID_BARINIUM", "ASTEROID_GOSTIAN", "ASTEROID_ARDANIUM"]
ASTEROID_TABLES.update({f"{k}_STRIP": [(r, w if r.startswith("ore_") or k not in STRIP_RICH_TABLES else w * STRIP_BYPRODUCT_FACTOR,
                                         rare) for r, w, rare in v] for k, v in list(ASTEROID_TABLES.items())})
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


def table_value(rows):
    total = sum(w for _, w in rows)
    return sum(w / total * trade.item_cost(r) * STORE_BUY_RATE for r, w in rows)


def area_tier(area):
    return WILD_SPACE_TIER if area in wildspace.SYSTEM_NAMES else trade.ORBIT_TIERS[area]


def laser_tier(tier):
    rank = 10 * min(5, math.floor(tier)) - 5
    return rank // 10 + 1


def hull(tier):
    return "Fighter" if tier < 2 else "Mining Barge"


def asteroids(area):
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
        out.append((weight, table_value(HARVEST[table]), bonus, tier))
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


def mining_rate(area, prospector=0, rapid=0, bay_percent=0):
    """(ore an hour, credits an hour) of a matched pilot mining the area's asteroids with three lasers."""
    rows, _nodes, _total = asteroids(area)
    if not rows:
        return 0.0, 0.0
    weight = sum(r[0] for r in rows)
    travel = travel_seconds(area)
    factor = prospector_factor(prospector) * bay_factor(prospector, bay_percent)
    ore = value = seconds = 0.0
    for w, unit, bonus, tier in rows:
        o, cycles, batches = asteroid_haul(tier)
        share = w / weight
        ore += share * o * factor
        value += share * (o * factor * unit + cycles * bonus)
        seconds += share * (batches * CYCLE_SECONDS * (100 - rapid) / 100 + travel)
    return ore * 3600 / seconds, value * 3600 / seconds


def strip_rate(area, prospector=0, rapid=0):
    """Credits an hour of a matched Industrial stripping the area's asteroids (tier 3 up), or 0 below that: one rock a
    10 s cycle for every unit it holds (SpaceCombatMath.StripMinerYield at 1 ore a cycle)."""
    if area_tier(area) < STRIP_MINIMUM_TIER:
        return 0.0
    rows, _nodes, _total = asteroids(area)
    loc = area_locals(area)
    strip = {}
    for resref, _w in SPAWNS.get(loc.get("RESOURCE_SPAWN_TABLE_ID", ""), []):
        p = placeable_locals(resref)
        if p.get("STRIPMINE_LOOT_TABLE_ID"):
            strip[(p.get("ASTEROID_LOOT_TABLE_ID"))] = table_value(HARVEST[p["STRIPMINE_LOOT_TABLE_ID"]])
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


def rows():
    out = []
    for area in AREAS:
        if not asteroids(area)[0]:
            continue
        ore, credits = mining_rate(area)
        _, perks = mining_rate(area, PROSPECTOR[-1], RAPID_EXTRACTION[-1])
        perk_strip = strip_rate(area, PROSPECTOR[-1], RAPID_EXTRACTION[-1])
        _, best = mining_rate(area, PROSPECTOR[-1], RAPID_EXTRACTION[-1], BEST_BAY_PERCENT)
        out.append(dict(area=area, tier=area_tier(area), ore=ore, credits=credits, perks=perks, best=best,
                        strip=strip_rate(area), perk_strip=perk_strip, combat=combat_rate(area),
                        travel=travel_seconds(area)))
    return out


def report():
    print("Active mining, a matched pilot with three Mining Lasers (Fighter at T1, Mining Barge from T2), against the "
          "tier's combat rate. 'perks': Prospector III and Rapid Extraction II; 'best': plus a best bay droid "
          f"(+{BEST_BAY_PERCENT}%).")
    for r in sorted(rows(), key=lambda r: (r["tier"], r["area"])):
        c = r["combat"]
        print(f"  T{r['tier']:<4} {r['area']:18s} {r['ore']:5.0f} ore/h {r['credits']:7.0f}/h ({100 * r['credits'] / c:4.0f}%), "
              f"perks {r['perks']:7.0f}/h ({100 * r['perks'] / c:4.0f}%), best {r['best']:7.0f}/h ({100 * r['best'] / c:4.0f}%); "
              f"strip {100 * r['strip'] / c:4.0f}% (perks {100 * r['perk_strip'] / c:4.0f}%); travel {r['travel']:.1f} s")


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
        report()
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
