"""Space revamp active mining rate (W11e): ore and credits an hour of a mining laser pilot, from the code's own numbers.

usage: python mining.py           (writes the asteroid loot tables into HarvestingLootTableDefinition.cs)
       python mining.py --check   (exit 1 when those tables differ from ASTEROID_TABLES)
       python mining.py --sim     (prints ore and credits per hour by area against the tier's combat rate)

mining.py owns the sixteen asteroid loot tables (ASTEROID_* for mining lasers, ASTEROID_*_STRIP for strip miners) and
rewrites their blocks in place; the rest of HarvestingLootTableDefinition.cs is hand-written. bible_tabs.py's income
summary reads rows(). stations.py's Asteroid Drill values the same tables.

Sources (the code, not assumptions):
- Mining Laser: catalog.py FAMILIES (6 s cycle, 15 m range, mines asteroids up to tier rank / 10 + 1), a High module.
  Revamp lasers carry no Module Bonus property and no hull sets the legacy Industrial bonus, so a cycle's ore is
  SpaceCombatMath.MiningYield(ExtractionBaseAmount(0, 0), Prospector) = 1 + Prospector (+1 / +2 / +3), and it uses
  MiningUnitsPerCycle = 1 + ore / 2 of the asteroid's units (ShipModuleActions.MineAsteroid).
- Rapid Extraction I / II shorten the cycle 10 / 20% (SpaceCombatMath.MiningCycleSeconds).
- An asteroid holds 1d4 + 2 units (ShipModuleActions.RollAsteroidUnits). The extraction hold is per ship, so every
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
PROSPECTOR = [0, 1, 2, 3]
RAPID_EXTRACTION = [0, 10, 20]
WILD_SPACE_TIER = 5.5

# Balance pass (W11e): the asteroid loot tables, at the 40% store price, paid a matched miner 91-142% of combat in
# tiers 1-3 but 274-397% at tiers 4-5, 494% in Wild Space and 635% in the Korriban Debris Field, and strip miners
# 3-17x, mostly from the metal and gem by-products (a diamond is 1,300 credits beside 38 for Gostian ore) and the
# Ardanium rocks. The ore value was the lever (cycle time and yields are untouched): from tier 3 the ore rows carry
# the weight, so a by-product is 0.3-1.2% of picks instead of 6-9%; an Ardanium rock gives Ardanium one pick in nine
# (Gostian otherwise); strip miners use the laser tables (theirs were 4-5x richer in by-products; their edge is the
# Large hull and one cycle a rock). Tiers 1-2 keep their tables. The Korriban Debris Field's spawn table mixes
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
ASTEROID_TABLES.update({f"{k}_STRIP": list(v) for k, v in list(ASTEROID_TABLES.items())})
LOOT_FILE = REPO / "SWLOR.Game.Server" / "Feature" / "LootTableDefinition" / "HarvestingLootTableDefinition.cs"
STRIP_CYCLE_SECONDS = 10.0
STRIP_HULL = "Industrial"
STRIP_MINIMUM_TIER = 3

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
    """[(frequency, ore value per unit, bonus loot value per cycle)] of the asteroids a matched laser can mine, and the
    area's node count of every kind (gas clouds and higher-tier rocks take nodes too)."""
    loc = area_locals(area)
    rows = SPAWNS.get(loc.get("RESOURCE_SPAWN_TABLE_ID", ""), [])
    out, total = [], sum(w for _, w in rows)
    for resref, weight in rows:
        p = placeable_locals(resref)
        table = p.get("ASTEROID_LOOT_TABLE_ID")
        if not table or int(p.get("ASTEROID_TIER", 1)) > laser_tier(area_tier(area)):
            continue
        bonus = 0.0
        for key, value in p.items():
            if key.startswith("LOOT_TABLE_"):
                name, chance, attempts = [x.strip() for x in str(value).split(",")]
                rows_b = ENZYMES.get(name) or HARVEST.get(name) or []
                if rows_b:
                    bonus += int(chance) / 100 * int(attempts) * table_value(rows_b)
        out.append((weight, table_value(HARVEST[table]), bonus))
    nodes = int(loc.get("RESOURCE_SPAWN_COUNT", OPEN_ORBIT_NODES))
    return out, nodes, total


def asteroid_haul(ore_per_cycle, lasers=LASERS):
    """Average (ore, laser cycles, cycle batches) per asteroid: every laser on the same rock."""
    use = 1 + ore_per_cycle // 2
    ore = cycles = batches = 0.0
    for units in ASTEROID_UNITS:
        k = math.ceil(units / use)
        ore += k * ore_per_cycle
        cycles += k
        batches += math.ceil(k / lasers)
    n = len(ASTEROID_UNITS)
    return ore / n, cycles / n, batches / n


def travel_seconds(area):
    rows, nodes, total = asteroids(area)
    minable = nodes * sum(w for w, _, _ in rows) / total * AVAILABLE_SHARE
    distance = 0.5 * AREA_METERS / math.sqrt(minable)
    speed = PC_RUN_METERS_PER_SECOND * m.ROLES[hull(area_tier(area))]["speed"]
    return max(0.0, distance - LASER_RANGE) / speed + RETARGET_SECONDS


def mining_rate(area, prospector=0, rapid=0, bay_percent=0):
    """(ore an hour, credits an hour) of a matched pilot mining the area's asteroids."""
    rows, _nodes, _total = asteroids(area)
    if not rows:
        return 0.0, 0.0
    weight = sum(w for w, _, _ in rows)
    unit = sum(w * v for w, v, _ in rows) / weight
    bonus = sum(w * b for w, _, b in rows) / weight
    per_cycle = 1 + prospector
    delivered = per_cycle if bay_percent <= 0 else max(per_cycle, math.floor(per_cycle * (1 + bay_percent / 100) + 0.5))
    ore, cycles, batches = asteroid_haul(per_cycle)
    ore *= delivered / per_cycle
    seconds = batches * CYCLE_SECONDS * (100 - rapid) / 100 + travel_seconds(area)
    per_hour = 3600 / seconds
    return ore * per_hour, (ore * unit + cycles * bonus) * per_hour


def open_orbit_ore_per_hour():
    """Ore an hour of a matched Mining Barge with three lasers in an open orbit of asteroids only (no perks)."""
    _ore, _cycles, batches = asteroid_haul(1)
    distance = 0.5 * AREA_METERS / math.sqrt(OPEN_ORBIT_NODES * AVAILABLE_SHARE)
    travel = max(0.0, distance - LASER_RANGE) / (PC_RUN_METERS_PER_SECOND * m.ROLES["Mining Barge"]["speed"]) + RETARGET_SECONDS
    return asteroid_haul(1)[0] * 3600 / (batches * CYCLE_SECONDS + travel)


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
        _, best = mining_rate(area, PROSPECTOR[-1], RAPID_EXTRACTION[-1], BEST_BAY_PERCENT)
        out.append(dict(area=area, tier=area_tier(area), ore=ore, credits=credits, perks=perks, best=best,
                        strip=strip_rate(area), combat=combat_rate(area), travel=travel_seconds(area)))
    return out


def report():
    print("Active mining, a matched pilot with three Mining Lasers (Fighter at T1, Mining Barge from T2), against the "
          "tier's combat rate. 'perks': Prospector III and Rapid Extraction II; 'best': plus a best bay droid "
          f"(+{BEST_BAY_PERCENT}%).")
    for r in sorted(rows(), key=lambda r: (r["tier"], r["area"])):
        c = r["combat"]
        print(f"  T{r['tier']:<4} {r['area']:18s} {r['ore']:5.0f} ore/h {r['credits']:7.0f}/h ({100 * r['credits'] / c:4.0f}%), "
              f"perks {r['perks']:7.0f}/h ({100 * r['perks'] / c:4.0f}%), best {r['best']:7.0f}/h ({100 * r['best'] / c:4.0f}%); "
              f"strip {100 * r['strip'] / c:4.0f}%; travel {r['travel']:.1f} s")


def strip_rate(area, prospector=0, rapid=0):
    """Credits an hour of a matched Industrial stripping the area's asteroids (tier 3 up), or 0 below that."""
    if area_tier(area) < STRIP_MINIMUM_TIER:
        return 0.0
    loc = area_locals(area)
    vals = []
    for resref, weight in SPAWNS.get(loc.get("RESOURCE_SPAWN_TABLE_ID", ""), []):
        p = placeable_locals(resref)
        table = p.get("STRIPMINE_LOOT_TABLE_ID")
        if table and int(p.get("ASTEROID_TIER", 1)) <= laser_tier(area_tier(area)):
            vals.append((weight, table_value(HARVEST[table])))
    if not vals:
        return 0.0
    unit = sum(w * v for w, v in vals) / sum(w for w, _ in vals)
    per_cycle = 1 + prospector
    use = 1 + per_cycle // 2
    ore = sum(math.ceil(u / use) * per_cycle for u in ASTEROID_UNITS) / len(ASTEROID_UNITS)
    rows_all, nodes, total = asteroids(area)
    minable = nodes * sum(w for w, _, _ in rows_all) / total * AVAILABLE_SHARE
    distance = 0.5 * AREA_METERS / math.sqrt(minable)
    speed = PC_RUN_METERS_PER_SECOND * m.ROLES[STRIP_HULL]["speed"]
    travel = max(0.0, distance - LASER_RANGE) / speed + RETARGET_SECONDS
    return ore * unit * 3600 / (STRIP_CYCLE_SECONDS * (100 - rapid) / 100 + travel)


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


def main():
    if "--sim" in sys.argv:
        report()
        return
    check = "--check" in sys.argv
    text = loot_file_text()
    stale = LOOT_FILE.read_bytes().decode("utf-8") != text
    if stale and not check:
        LOOT_FILE.write_bytes(text.encode("utf-8"))
    print(f"{len(ASTEROID_TABLES)} asteroid tables, {int(stale)} file(s) {'out of date' if check else 'written'}")
    if stale and check:
        sys.exit(1)


if __name__ == "__main__":
    main()
