"""Space revamp kills per hour (review fix 2026-10-30): derives the combat baseline's kill rate per tier instead of
trade.KILLS_PER_HOUR's assumed 60, from the combat checks and the spawn density.

usage: python kills.py --sim   (prints the derived kills an hour per tier and the income bands that read the rate;
                                exit 1 when a tier differs from trade.KILLS_PER_HOUR by more than TOLERANCE)

Sources:
- Kill time: checks.py section A (a matched Fighter with 3 Laser Cannons against one same-level Normal of each role,
  150 runs). The mean times are read from the simulator work file checks.md (model.HERE) when it exists, otherwise
  from SECTION_A below (the 2026-10-28 run).
- What a combat pilot fights: the open orbits' creature spawn tables (SpaceSpawnDefinition), Normal hulls only
  (npc_catalog.HULLS; elite platforms, carriers, capitals and convoys left out), weighted by spawn frequency.
- Density: an orbit is 32 x 32 tiles (320 m square) with CREATURE_SPAWN_COUNT creatures, or 45 for that size
  (Spawn.CalculateCreatureSpawnCount), respawning 5 minutes after a kill (Spawn.DefaultRespawnMinutes). As mining.py
  does for asteroids, half of the Normal ships are taken as available (the rest dead and waiting, or another pilot's).
- Travel: the mean nearest-neighbour distance of that density, less the Laser Cannon's 40 m range, at the PC run rate
  (4 m/s) times the Fighter's speed; plus RETARGET_SECONDS to pick the next target and close in.
- Recovery: a fight ends with about 80% of hull and shield left (section A's "left"). The upper rate counts none; the
  lower rate waits for the shield to regenerate what the fight took (all of it taken as shield, which absorbs first;
  the Fighter's hull equals its shield and regenerates at model.py's rate), so the true rate lies between them.
- Not counted either way: looting the wreck, ships that aggro in pairs, and docking to repair hull.
"""
import math
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import mining  # noqa: E402
import model as m  # noqa: E402
import npc_catalog  # noqa: E402
import trade  # noqa: E402

TOLERANCE = 0.15
RETARGET_SECONDS = 4.0
AVAILABLE_SHARE = mining.AVAILABLE_SHARE
LASER_RANGE = 40
AREA_METERS = 320
DEFAULT_CREATURES = 45  # a 32 x 32 area (Spawn.CalculateCreatureSpawnCount)
TIER_LEVELS = {1: 5, 2: 15, 3: 25, 4: 35, 5: 45}
# checks.py section A, mean seconds to kill (2026-10-28, 150 runs), by tier and role.
SECTION_A = {
    1: {"Fighter": 13, "Interceptor": 12, "Bomber": 14, "Sniper": 11, "Electronic Warfare": 12, "Gunship": 15, "Logistics": 12, "Minelayer": 14},
    2: {"Fighter": 25, "Interceptor": 23, "Bomber": 24, "Sniper": 22, "Electronic Warfare": 22, "Gunship": 25, "Logistics": 24, "Minelayer": 23},
    3: {"Fighter": 34, "Interceptor": 32, "Bomber": 33, "Sniper": 30, "Electronic Warfare": 30, "Gunship": 35, "Logistics": 32, "Minelayer": 32},
    4: {"Fighter": 35, "Interceptor": 33, "Bomber": 36, "Sniper": 31, "Electronic Warfare": 32, "Gunship": 38, "Logistics": 34, "Minelayer": 32},
    5: {"Fighter": 35, "Interceptor": 33, "Bomber": 36, "Sniper": 31, "Electronic Warfare": 31, "Gunship": 37, "Logistics": 34, "Minelayer": 35},
}
# The open orbit of each tier the combat baseline (trade.combat_credits_per_hour) stands for.
ORBITS = {1: "viscaraorbit", 2: "moncalaorbit", 3: "hutlar_orbit", 4: "tatooineorbit", 5: "dantooineorbit"}
# Where the rate feeds an income comparison (trade.KILLS_PER_HOUR readers).
READERS = [
    ("Combat baseline (every '% of combat' band)", "trade.combat_credits_per_hour"),
    ("Exploring, slicing sites, derelicts (combat site kills, KILL_MINUTES)", "scanning.py, derelicts.py"),
    ("Space lockboxes on normal kills", "slicing.py lockboxes_pay, combat_with_lockboxes"),
    ("Incursions, Rift Runs (kill minutes, points an hour)", "events.py"),
    ("Faction agent missions (a kill's worth)", "agents.py"),
    ("Wild Space combat (loot and stolen cargo per kill)", "wildspace.py"),
]


# Section A's hull and shield left after the fight, in percent (2026-10-28).
SECTION_A_LEFT = {
    1: {"Fighter": 82, "Interceptor": 87, "Bomber": 80, "Sniper": 83, "Electronic Warfare": 79, "Gunship": 84, "Logistics": 83, "Minelayer": 86},
    2: {"Fighter": 80, "Interceptor": 86, "Bomber": 83, "Sniper": 80, "Electronic Warfare": 77, "Gunship": 83, "Logistics": 81, "Minelayer": 89},
    3: {"Fighter": 81, "Interceptor": 87, "Bomber": 84, "Sniper": 83, "Electronic Warfare": 77, "Gunship": 84, "Logistics": 82, "Minelayer": 88},
    4: {"Fighter": 81, "Interceptor": 84, "Bomber": 79, "Sniper": 80, "Electronic Warfare": 74, "Gunship": 84, "Logistics": 78, "Minelayer": 87},
    5: {"Fighter": 81, "Interceptor": 85, "Bomber": 80, "Sniper": 81, "Electronic Warfare": 75, "Gunship": 84, "Logistics": 79, "Minelayer": 86},
}


def section_a():
    """(mean kill seconds, hull and shield left percent) by tier and role: checks.md's section A when the simulator work
    file exists, else SECTION_A and SECTION_A_LEFT."""
    path = Path(m.HERE) / "checks.md"
    if not path.exists():
        return (SECTION_A, SECTION_A_LEFT), "SECTION_A (2026-10-28)"
    text = path.read_text(encoding="utf-8")
    block = text.split("## A.", 1)[1].split("\n## ", 1)[0]
    lines = [x for x in block.splitlines() if x.startswith("|")]
    head = [c.strip() for c in lines[0].strip("|").split("|")]
    out, left = {}, {}
    for line in lines[2:]:
        cells = [c.strip() for c in line.strip("|").split("|")]
        tier = int(re.match(r"T(\d)", cells[0]).group(1))
        parsed = {role: re.match(r"([\d.]+)s \((\d+)%\)", cells[head.index(role)]) for role in m.NORMAL_ROLES}
        out[tier] = {role: float(x.group(1)) for role, x in parsed.items()}
        left[tier] = {role: float(x.group(2)) for role, x in parsed.items()}
    return (out, left), str(path)


def spawn_mix(table):
    """{role: frequency} of the Normal hulls in a creature spawn table."""
    text = (mining.REPO / "SWLOR.Game.Server" / "Feature" / "SpawnDefinition" / "SpaceSpawnDefinition.cs").read_text(encoding="utf-8")
    block = text.split(f'_builder.Create("{table}"', 1)[1].split("_builder.Create(", 1)[0]
    mix = {}
    for tag, weight in re.findall(r'AddSpawn\(ObjectType\.Creature,\s*"t\d([a-z]+)"\)\s*\.WithFrequency\((\d+)\)', block):
        hull = npc_catalog.HULLS.get(npc_catalog.ALIASES.get(tag, tag))
        if hull and hull[1] == "Normal":
            mix[hull[0]] = mix.get(hull[0], 0) + int(weight)
    total_all = sum(int(w) for w in re.findall(r'AddSpawn\(ObjectType\.Creature,\s*"[^"]+"\)\s*\.WithFrequency\((\d+)\)', block))
    return mix, total_all


def derive(tier, data):
    times, left = data
    area = ORBITS[tier]
    loc = mining.area_locals(area)
    mix, total_all = spawn_mix(loc["CREATURE_SPAWN_TABLE_ID"])
    normal = sum(mix.values())
    kill = sum(w * times[tier][r] for r, w in mix.items()) / normal
    creatures = int(loc.get("CREATURE_SPAWN_COUNT", DEFAULT_CREATURES))
    available = creatures * normal / total_all * AVAILABLE_SHARE
    distance = 0.5 * AREA_METERS / math.sqrt(available)
    travel = max(0.0, distance - LASER_RANGE) / (mining.PC_RUN_METERS_PER_SECOND * m.ROLES["Fighter"]["speed"])
    seconds = kill + travel + RETARGET_SECONDS
    ship = m.player_ship("Fighter", TIER_LEVELS[tier], m.FIT["Fighter"])
    taken = sum(w * (100 - left[tier][r]) / 100 for r, w in mix.items()) / normal * (ship["maxHull"] + ship["maxShield"])
    recovery = max(0.0, taken / ship["shieldRegenPerSecond"] - travel - RETARGET_SECONDS)
    return dict(tier=tier, area=area, kill=kill, distance=distance, travel=travel, seconds=seconds, rate=3600 / seconds,
                recovery=recovery, low=3600 / (seconds + recovery), normal_share=normal / total_all, available=available)


def report():
    times, source = section_a()
    print(f"Kills an hour, derived (kill time from {source}; nearest Normal ship at half the orbit's density; "
          f"{RETARGET_SECONDS:g} s to retarget), against trade.KILLS_PER_HOUR = {trade.KILLS_PER_HOUR}:")
    ok = True
    for t in ORBITS:
        r = derive(t, times)
        off = r["rate"] / trade.KILLS_PER_HOUR - 1
        ok &= abs(off) <= TOLERANCE
        print(f"  T{t} {r['area']:14s} kill {r['kill']:4.1f} s, nearest ship {r['distance']:4.1f} m (travel {r['travel']:.1f} s), "
              f"{r['seconds']:4.1f} s a kill: {r['rate']:5.1f} an hour ({100 * off:+.0f}%); waiting out the shield "
              f"({r['recovery']:.0f} s more): {r['low']:4.1f} an hour ({100 * (r['low'] / trade.KILLS_PER_HOUR - 1):+.0f}%)")
    if not ok:
        print(f"  Outside +-{TOLERANCE:.0%} of {trade.KILLS_PER_HOUR}: the income bands below read the rate and would move "
              "(left at the assumed rate pending the lead's decision):")
        for name, where in READERS:
            print(f"    - {name}: {where}")
    return ok


if __name__ == "__main__":
    if "--sim" in sys.argv:
        sys.exit(0 if report() else 1)
    print(__doc__)
