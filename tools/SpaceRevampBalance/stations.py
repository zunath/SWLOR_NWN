"""Space revamp player stations (plan section 25, W13a and W13b): anchor sites, founding, upkeep, the deck, docking,
exchanges, the passenger shuttle and the station-only buildings (the Shipyard, the Asteroid Drill and the Jump Beacon).

usage: python stations.py            (writes the generated C# station tuning)
       python stations.py --check    (exit 1 when the generated file is out of date)

Every station number lives here. bible_tabs.py calls stations_tab(Sheet) to write the Player Stations (Revamp) tab.
Draft numbers, tune in testing.

Sources:
- A station is a player city in space (plan section 25), so founding and upkeep mirror the planet city:
  CityLayoutDefinition (initial price 100,000, 5,000 a day, 75 structures, 21 buildings), Property.ProcessUpkeep (7 days
  of the daily price, -5% per Upkeep perk level, +10,000 a week per Medical Center, Starport and Cantina level above 1),
  Property._citizensRequired (5 / 10 / 15 / 20 / 25 citizens for levels 1-5), the 18 hour citizen grace period and the
  30 day disrepair deadline, PlaceCityHallDialog (City Management I, not a citizen elsewhere).
- Docking: ShipAccessRules (plan section 7): any player starport takes Large hulls, Capital hulls need starport level 2.
- Exchanges: W7c (plan section 13): a player starport trades through the NPC exchange of the system it sits in.
- The shuttle: GalaxyMap's orbital hop (60 s, 25 credits, every 60 s) for a station and the NPC dock of its orbit.
"""
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import droids  # noqa: E402
import trade  # noqa: E402
import wildspace  # noqa: E402

REPO = Path(__file__).resolve().parents[2]

# ---------------------------------------------------------------------------------------------------------------
# Founding and upkeep (mirrors the planet city)
# ---------------------------------------------------------------------------------------------------------------
INITIAL_PRICE = 100000
PRICE_PER_DAY = 5000
STRUCTURE_LIMIT = 75
BUILDING_LIMIT = 21
UPGRADE_PRICE_PER_LEVEL = 10000          # a week, per Medical Center, Starport and Cantina level above 1
UPKEEP_PERK_REDUCTION = 0.05             # per Upkeep perk level
CITIZENS_REQUIRED = [5, 10, 15, 20, 25]  # city levels 1-5
GRACE_HOURS = 18
DISREPAIR_DAYS = 30
ELECTION_DAYS = 21
MINIMUM_CITY_MANAGEMENT = 1

# The Station Core takes the place of the City Hall item (plan section 25). Founding also builds the station's first
# Starport, because docking goes only through a Starport and a station has no other way in, so the Core's recipe is the
# City Hall's and the Starport's together, at the Starport's level (decision).
CORE_RECIPE_LEVEL = 50
CORE_COMPONENTS = [("const_parts", 3), ("pow_supp_unit", 2), ("r_const_parts", 4), ("r_pow_supp_unit", 2)]
CORE_ITEM_RESREF = "structure_5018"
CORE_ITEM_NAME = "Station Core"

# The Core is used in space within this range of a free anchor site.
FOUNDING_RANGE = 40.0

# ---------------------------------------------------------------------------------------------------------------
# Anchor sites
# ---------------------------------------------------------------------------------------------------------------
# Two per orbit with an NPC dock and one or two per Wild Space system (plan sections 23 and 25). Each is a waypoint
# tagged STATION_ANCHOR_<ID> in its area: the point ships launch to. The station exterior stands EXTERIOR_OFFSET metres
# from it toward the middle of the area. Anchors and exteriors sit at least 40 m from every placeable and waypoint the
# area places (StationTests checks it).
# The Mid Rim systems have no dock and the W6c zones are Small/Medium only, so neither has anchors (decision).
# (id, area, x, y, home planet)
ORBIT_ANCHORS = [
    ("Viscara1", "viscaraorbit", 150.5, 60.5, "Viscara"),
    ("Viscara2", "viscaraorbit", 60.5, 230.5, "CZ220"),
    ("Tatooine1", "tatooineorbit", 160.5, 60.5, "Tatooine"),
    ("Tatooine2", "tatooineorbit", 120.5, 180.5, "Tatooine"),
    ("MonCala1", "moncalaorbit", 180.5, 140.5, "MonCala"),
    ("MonCala2", "moncalaorbit", 220.5, 200.5, "MonCala"),
    ("Hutlar1", "hutlar_orbit", 60.5, 80.5, "Hutlar"),
    ("Hutlar2", "hutlar_orbit", 240.5, 250.5, "Hutlar"),
    ("Dathomir1", "space_dathomir", 60.5, 150.5, "Dathomir"),
    ("Dathomir2", "space_dathomir", 150.5, 250.5, "Dathomir"),
    ("Korriban1", "space_korriban", 180.5, 60.5, "Korriban"),
    ("Korriban2", "space_korriban", 160.5, 250.5, "Korriban"),
    ("Dantooine1", "dantooineorbit", 200.5, 90.5, "Dantooine"),
    ("Dantooine2", "dantooineorbit", 60.5, 90.5, "Dantooine"),
    ("SmugglersMoon1", "narshadorbit", 240.5, 120.5, "SmugglersMoon"),
    ("SmugglersMoon2", "narshadorbit", 80.5, 110.5, "SmugglersMoon"),
]
# (id, area, x, y)
WILD_ANCHORS = [
    ("VeshPulsar1", "space_wspulsar", 60.5, 150.5),
    ("VeshPulsar2", "space_wspulsar", 150.5, 250.5),
    ("TheMaw1", "space_wsblkhole", 160.5, 250.5),
    ("IonVeil1", "space_wsionstorm", 180.5, 60.5),
    ("IonVeil2", "space_wsionstorm", 160.5, 250.5),
    ("KorvaxMagnetar1", "space_wsmagnetar", 60.5, 80.5),
    ("EmberGiant1", "space_wsredgiant", 180.5, 60.5),
    ("EmberGiant2", "space_wsredgiant", 160.5, 250.5),
    ("ShipGraveyard1", "space_wsgraveyrd", 160.5, 250.5),
]
ANCHOR_TAG_PREFIX = "STATION_ANCHOR_"
ANCHOR_Z = 3.0
EXTERIOR_OFFSET = 30.0
EXTERIOR_RESREF = "_mdrn_pl_spcs003"     # "Space Station - Civilian 3", a space-station placeable the orbits use (W11g)
EXTERIOR_TAG = "station_exterior"
# The exterior grows with the city level (plan section 25: "scaled by city level").
EXTERIOR_SCALE_BY_LEVEL = [1.2, 1.4, 1.6, 1.8, 2.0]

# Planet name of each PlanetType used as a home port.
PLANET_NAMES = {"Viscara": "Viscara", "CZ220": "CZ-220", "Tatooine": "Tatooine", "MonCala": "Mon Cala",
                "Hutlar": "Hutlar", "Dathomir": "Dathomir", "Korriban": "Korriban", "Dantooine": "Dantooine",
                "SmugglersMoon": "Smuggler's Moon"}


def exchange_for_planet(planet):
    for key, _, planets, _, _, _, _, _ in trade.EXCHANGES:
        if planet in planets:
            return key
    raise ValueError(planet)


def open_ports():
    """The exchanges no lawful authority runs: the open ports (plan section 13)."""
    return [(key, orbit, planets[0]) for key, _, planets, orbit, authority, _, _, _ in trade.EXCHANGES if authority == "None"]


def wild_space_port(area):
    """Wild Space has no NPC exchange. A station there trades through the open port nearest, by starlane hops, to the
    orbit its system was charted from (decision: no second market to flood, W7c). Ties keep the exchange list's order."""
    source = next(src for r, _, _, src, _ in wildspace.SYSTEMS if r == area)
    best = min(open_ports(), key=lambda p: trade.hops(source, p[1]))
    return best[0], best[2], trade.hops(source, best[1])


def anchors():
    rows = []
    for anchor_id, area, x, y, planet in ORBIT_ANCHORS:
        rows.append(dict(id=anchor_id, area=area, x=x, y=y, planet=planet, exchange=exchange_for_planet(planet),
                         wild=False, system=trade.ORBIT_NAMES[area], hops=0))
    for anchor_id, area, x, y in WILD_ANCHORS:
        exchange, planet, hops = wild_space_port(area)
        rows.append(dict(id=anchor_id, area=area, x=x, y=y, planet=planet, exchange=exchange, wild=True,
                         system=wildspace.SYSTEM_NAMES[area], hops=hops))
    return rows


def exterior_position(x, y):
    """The station exterior: EXTERIOR_OFFSET metres from the anchor toward the middle of the 320 m area."""
    cx, cy = 160.0, 160.0
    dx, dy = cx - x, cy - y
    length = (dx * dx + dy * dy) ** 0.5
    return x + dx / length * EXTERIOR_OFFSET, y + dy / length * EXTERIOR_OFFSET


# ---------------------------------------------------------------------------------------------------------------
# The deck
# ---------------------------------------------------------------------------------------------------------------
# The station's land is an instance of station_deck, a copy of Smuggler's Moon's Corporate District (pw_ar_narscorpd)
# with its NPCs, doors, transitions, triggers, sounds and working placeables removed and its static scenery kept.
# The civic buildings and the station-only buildings (W13b) snap to their slots (one of each per station); the Station
# Fuel slot is kept clear.
# Every other building and structure keeps DECK_SLOT_CLEARANCE metres from every slot. Slot points are the plazas the
# source area's NPCs stood on (unverified in a client).
# (slot, x, y, building)
DECK_SLOTS = [
    ("Core", 77.0, 44.0, "StationCore"),
    ("Starport", 128.0, 84.0, "Starport"),
    ("MedicalCenter", 35.0, 117.0, "MedicalCenter"),
    ("Bank", 95.0, 39.0, "Bank"),
    ("Cantina", 61.0, 66.0, "Cantina"),
    ("Shipyard", 98.0, 140.0, "Shipyard"),
    ("AsteroidDrill", 111.0, 50.0, "AsteroidDrill"),
    ("JumpBeacon", 34.0, 79.0, "JumpBeacon"),
    ("StationFuel", 83.0, 105.0, None),
]
DECK_SLOT_CLEARANCE = 10.0
DECK_ENTRANCE = (140.0, 85.0, 180.0)      # x, y, facing: beside the Starport slot
DECK_AREA = "station_deck"

# ---------------------------------------------------------------------------------------------------------------
# Passenger shuttle
# ---------------------------------------------------------------------------------------------------------------
# A station in an orbit is a quick orbital hop from its home port, like CZ-220 and Viscara (GalaxyMap).
ORBIT_HOP_FARE = 25
ORBIT_HOP_SECONDS = 60
ORBIT_HOP_PERIOD_SECONDS = 60
# A Wild Space station's shuttle runs to its exchange port through charted rift channels (decision): a full flight,
# priced like the longest interplanetary fares (12 credits per galaxy map unit, about 85 units).
WILD_SPACE_FARE = 1000
WILD_SPACE_SECONDS = 300
WILD_SPACE_PERIOD_SECONDS = 120


def weekly_upkeep(upkeep_perk=0, medical=1, starport=1, cantina=1, beacon=1):
    base = PRICE_PER_DAY * 7
    base -= int(base * UPKEEP_PERK_REDUCTION * upkeep_perk)
    return base + (medical - 1 + starport - 1 + cantina - 1 + beacon - 1) * UPGRADE_PRICE_PER_LEVEL


# ---------------------------------------------------------------------------------------------------------------
# Station-only buildings (W13b): the Shipyard, the Asteroid Drill and the Jump Beacon
# ---------------------------------------------------------------------------------------------------------------
# Each is a building without an interior: a console placeable that snaps to its reserved deck slot (one per station)
# and an existing scenery model beside it. Players use the console. The Station Fuel slot stays clear (decision):
# Station Fuel is cargo, burned from the jumping ship's own cargo bay, so it needs no building.
# Recipes follow the city buildings' (StructureRecipes): Fabrication, Reinforced Construction Parts and Reinforced
# Power Supply Units at levels 41-50; the Shipyard, the top building, matches the Lab (level 50, 8 Zinsiam).
# RecipeType continues the Fabrication structures after the Station Core (1524), far below W14a's 7700+.
# (structure, structure id, name, recipe id, recipe level, components, slot, console resref, model resref, description)
BUILDINGS = [
    ("Shipyard", 5019, "Shipyard", 1525, 50,
     [("r_const_parts", 4), ("r_pow_supp_unit", 3), ("zinsiam", 8)], "Shipyard", "engineering_term", "swd2_crane001",
     "A station shipyard. Deploy it on a player station's deck: Capital hulls can only be built at a Shipyard, and a "
     "droid assigned to it speeds up the work."),
    ("AsteroidDrill", 5020, "Asteroid Drill", 1526, 45,
     [("r_const_parts", 4), ("r_pow_supp_unit", 2)], "AsteroidDrill", "swlor_0034", "swd3_mindr01",
     "A station asteroid drill. Deploy it on a player station's deck: it extracts the anchor site's ore over time into "
     "the station's drill storage, and a droid assigned to it raises the output."),
    ("JumpBeacon", 5021, "Jump Beacon", 1527, 48,
     [("r_const_parts", 4), ("r_pow_supp_unit", 3)], "JumpBeacon", "swlor_0024", "_mdrn_pl_comtwr1",
     "A station jump beacon. Deploy it on a player station's deck: it jumps a ship docked at the station to another "
     "station's beacon for Station Fuel. Beacons never reach into or out of Wild Space."),
]
BUILDING_MODEL_OFFSET = 6.0              # the scenery model stands this far behind the console (+Y)

# Industry (plan section 27): an assigned droid's bonus is StationDroidRules' (droids.py numbers): the Shipyard builds
# +0.3% faster per Industry point and the Drill mines +0.5% more per point, raised by Efficiency Protocol (+40% at II).
MAX_INDUSTRY = droids.MAXIMUM_INDUSTRY
BEST_EFFICIENCY_PERCENT = max(droids.PROTOCOLS["EfficiencyProtocol"][2].values())

# Shipyard: Capital hull deeds are crafted only within this range of a Shipyard console that admits the crafter.
SHIPYARD_RANGE = 10.0
# The mayor's optional fee per Capital hull built, paid into the treasury (PropertyTaxType.Shipyard): a flat number of
# credits like the citizenship tax, with the same 0-50,000 range (decision).
SHIPYARD_FEE_MAX = 50000
# A droid shortens builds (StationDroidRules.ShipyardBuildSpeedPercent): Capital deed crafting progress per synthesis
# step rises by its percent. Crafting is the progress mini-game, so "faster" is fewer synthesis steps.

# Asteroid Drill: units of ore a day, rolled from the anchor area's asteroid tables in the shares they spawn
# (SpaceResourceSpawnDefinition, through each asteroid's ASTEROID_LOOT_TABLE_ID), so the drill mines the site's own ore
# at its system's tier. A droid adds StationDroidRules.DrillOutputPercent. Storage holds this many days of output; a
# full drill stops until someone collects.
DRILL_UNITS_PER_DAY = 150
DRILL_STORAGE_DAYS = 3
# The anchor areas' resource spawn tables (their spawn waypoint's SPAWN_TABLE local; StationTests checks the git).
AREA_RESOURCE_TABLES = {
    "viscaraorbit": "SPACE_RESOURCES_VISCARA_ORBIT", "tatooineorbit": "SPACE_RESOURCES_TATOOINE_ORBIT",
    "moncalaorbit": "SPACE_RESOURCES_MONCALA_ORBIT", "hutlar_orbit": "SPACE_RESOURCES_HUTLAR_ORBIT",
    "space_dathomir": "SPACE_RESOURCES_DATHOMIR_ORBIT", "space_korriban": "SPACE_RESOURCES_KORRIBAN_ORBIT",
    "dantooineorbit": "SPACE_RESOURCES_DATHOMIR_ORBIT", "narshadorbit": "SPACE_RESOURCES_VISCARA_ORBIT",
    "space_wspulsar": "SPACE_RESOURCES_KORRIBAN_DEBRIS", "space_wsblkhole": "SPACE_RESOURCES_KORRIBAN_DEBRIS",
    "space_wsionstorm": "SPACE_RESOURCES_KORRIBAN_DEBRIS", "space_wsmagnetar": "SPACE_RESOURCES_KORRIBAN_DEBRIS",
    "space_wsredgiant": "SPACE_RESOURCES_KORRIBAN_DEBRIS", "space_wsgraveyrd": "SPACE_RESOURCES_KORRIBAN_DEBRIS",
}
# Income check: an active miner's credits an hour at the system come from mining.py (three matched Mining Lasers, no
# perks; W11e replaced the assumed 360 ore an hour), ore valued at what an NPC store pays (40% of the item's Cost). A
# drill must stay under an hour of that a day, droid included.
STORE_BUY_RATE = 0.40
DRILL_MAX_MINING_HOURS = 1.0

# Jump Beacon: the beacon level (a city upgrade, 1-3, like the Starport's) caps the hull: both ends must take it.
BEACON_MAX_LEVEL = 3
BEACON_MAX_HULL = {1: "Medium", 2: "Large", 3: "Capital"}
# Station Fuel units a jump burns from the ship's cargo bay, by hull size, and the hulls that burn less (the Hutt Grand
# Barge's signature: 50% less fuel, rounded up).
JUMP_FUEL = {"Small": 1, "Medium": 2, "Large": 4, "Capital": 8}
JUMP_FUEL_DISCOUNT = {"sd_huttgrandbarg": 50}
# Who may jump to and from a beacon (the mayor's setting, default citizens). Allies are the citizens of the stations
# the mayor lists.
BEACON_ACCESS = ["Citizens", "Allies", "Anyone"]


def jump_fuel(size, hull_tag=""):
    units = JUMP_FUEL[size]
    discount = JUMP_FUEL_DISCOUNT.get(hull_tag, 0)
    return max(1, -(-units * (100 - discount) // 100))


def _cs_source(*parts):
    return (REPO / "SWLOR.Game.Server" / Path(*parts)).read_text(encoding="utf-8")


def loot_table_rows():
    """HarvestingLootTableDefinition: table name to [(resref, weight)]."""
    text = _cs_source("Feature", "LootTableDefinition", "HarvestingLootTableDefinition.cs")
    tables = {}
    for block in re.split(r'_builder\.Create\(', text)[1:]:
        name = re.match(r'"([A-Z0-9_]+)"', block).group(1)
        tables[name] = [(r, int(w)) for r, w in re.findall(r'\.AddItem\("([^"]+)",\s*(\d+)', block)]
    return tables


def resource_spawn_rows():
    """SpaceResourceSpawnDefinition: table name to [(placeable resref, frequency)]."""
    text = _cs_source("Feature", "SpawnDefinition", "SpaceResourceSpawnDefinition.cs")
    tables = {}
    for block in re.split(r'_builder\.Create\(', text)[1:]:
        name = re.match(r'"([A-Z0-9_]+)"', block).group(1)
        tables[name] = [(r, int(w)) for r, w in
                        re.findall(r'AddSpawn\(ObjectType\.Placeable,\s*"([^"]+)"\)\s*\.WithFrequency\((\d+)\)', block)]
    return tables


def asteroid_table(resref):
    """The ASTEROID_LOOT_TABLE_ID local of an asteroid placeable, or None for anything else (gas clouds)."""
    data = json.loads((REPO / "Module" / "utp" / f"{resref}.utp.json").read_text(encoding="latin-1"))
    for var in data.get("VarTable", {}).get("value", []):
        if var["Name"]["value"] == "ASTEROID_LOOT_TABLE_ID":
            return var["Value"]["value"]
    return None


def drill_tables():
    """Each anchor area's drill tables: [(asteroid loot table, weight)] in the shares its asteroids spawn."""
    spawns = resource_spawn_rows()
    out = {}
    for area in sorted({a["area"] for a in anchors()}):
        rows = []
        for resref, frequency in spawns[AREA_RESOURCE_TABLES[area]]:
            table = asteroid_table(resref)
            if table:
                rows.append((table, frequency))
        out[area] = rows
    return out


def system_tier(area):
    return wildspace.CONTENT_TIER if area in wildspace.SYSTEM_NAMES else trade.ORBIT_TIERS[area]


def ore_value(area):
    """Credits an NPC store pays for one unit of the area's drill ore, on average."""
    tables = loot_table_rows()
    rows = drill_tables()[area]
    total_weight = sum(w for _, w in rows)
    value = 0.0
    for table, weight in rows:
        items = tables[table]
        item_weight = sum(w for _, w in items)
        value += weight / total_weight * sum(w / item_weight * trade.item_cost(r) for r, w in items)
    return value * STORE_BUY_RATE


def drill_droid_percent(industry=0, efficiency=0):
    return round(droids.DRILL_OUTPUT_PERCENT_PER_INDUSTRY * industry * (1 + efficiency / 100))


def drill_units_per_day(industry=0, efficiency=0):
    return DRILL_UNITS_PER_DAY * (1 + drill_droid_percent(industry, efficiency) / 100)


def miner_rate(area):
    """An active miner's credits an hour in the area (mining.py)."""
    import mining
    return mining.mining_rate(area)[1]


def drill_income():
    """Per anchor area: tier, credits a day (no droid, best droid), a miner's credits an hour, and the mining hours."""
    rows = []
    for area in sorted(drill_tables(), key=lambda a: (system_tier(a), a)):
        unit = ore_value(area)
        day, best = drill_units_per_day() * unit, drill_units_per_day(MAX_INDUSTRY, BEST_EFFICIENCY_PERCENT) * unit
        miner = miner_rate(area)
        rows.append(dict(area=area, tier=system_tier(area), unit=unit, day=day, best=best, miner=miner,
                         hours=day / miner, best_hours=best / miner))
    return rows


# ---------------------------------------------------------------------------------------------------------------
# Generated C#
# ---------------------------------------------------------------------------------------------------------------
HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/stations.py. Do not edit by hand: change stations.py and regenerate.
// </auto-generated>
"""


def f(value):
    return f"{value:.1f}f"


def tuning_cs():
    q = json.dumps
    anchor_lines = "\n".join(
        f"            new StationAnchor {{ Id = {q(a['id'])}, Tag = {q(ANCHOR_TAG_PREFIX + a['id'].upper())}, "
        f"AreaResref = {q(a['area'])}, SystemName = {q(a['system'])}, X = {f(a['x'])}, Y = {f(a['y'])}, "
        f"HomePlanet = PlanetType.{a['planet']}, Exchange = TradeExchangeType.{a['exchange']}, "
        f"IsWildSpace = {'true' if a['wild'] else 'false'} }},"
        for a in anchors())
    slot_lines = "\n".join(
        f"            [StationDeckSlotType.{s}] = new StationDeckSlot {{ Type = StationDeckSlotType.{s}, X = {f(x)}, Y = {f(y)}, "
        f"Building = StructureType.{b or 'Invalid'} }},"
        for s, x, y, b in DECK_SLOTS)
    components = ", ".join(f"({q(r)}, {n})" for r, n in CORE_COMPONENTS)
    scales = ", ".join(f"{s:.1f}f" for s in EXTERIOR_SCALE_BY_LEVEL)
    citizens = ", ".join(str(c) for c in CITIZENS_REQUIRED)
    ex, ey, efacing = DECK_ENTRANCE
    return (HEADER + "using System.Collections.Generic;\nusing SWLOR.Game.Server.Enumeration;\n"
            "using SWLOR.Game.Server.Service.CraftService;\nusing SWLOR.Game.Server.Service.PropertyService;\n"
            "using SWLOR.Game.Server.Service.SpaceService;\nusing SWLOR.Game.Server.Service.TradeService;\n\n"
            "namespace SWLOR.Game.Server.Service.StationService\n{\n"
            "    /// <summary>\n"
            "    /// The player station numbers (plan section 25): founding and upkeep (the planet city's), the anchor sites,\n"
            "    /// the station exterior, the deck slots, the passenger shuttle and the station-only buildings.\n"
            "    /// </summary>\n"
            "    public static class StationTuning\n    {\n"
            f"        public const int InitialPrice = {INITIAL_PRICE};\n"
            f"        public const int PricePerDay = {PRICE_PER_DAY};\n"
            f"        public const int StructureLimit = {STRUCTURE_LIMIT};\n"
            f"        public const int BuildingLimit = {BUILDING_LIMIT};\n"
            f"        public const int UpgradePricePerLevel = {UPGRADE_PRICE_PER_LEVEL};\n"
            f"        public const float UpkeepPerkReduction = {UPKEEP_PERK_REDUCTION}f;\n"
            f"        public static readonly int[] CitizensRequired = {{ {citizens} }};\n"
            f"        public const int GraceHours = {GRACE_HOURS};\n"
            f"        public const int DisrepairDays = {DISREPAIR_DAYS};\n"
            f"        public const int ElectionDays = {ELECTION_DAYS};\n"
            f"        public const int MinimumCityManagement = {MINIMUM_CITY_MANAGEMENT};\n\n"
            "        // The Station Core\n"
            f"        public const string CoreItemResref = {q(CORE_ITEM_RESREF)};\n"
            f"        public const int CoreRecipeLevel = {CORE_RECIPE_LEVEL};\n"
            f"        public static readonly (string Resref, int Quantity)[] CoreComponents = {{ {components} }};\n"
            f"        public const float FoundingRange = {f(FOUNDING_RANGE)};\n\n"
            "        // Anchor sites and the exterior\n"
            f"        public const string AnchorTagPrefix = {q(ANCHOR_TAG_PREFIX)};\n"
            f"        public const float ExteriorOffset = {f(EXTERIOR_OFFSET)};\n"
            "        /// <summary>\n"
            "        /// The exterior model: \"Space Station - Civilian 3\", one of the SWLOR space-station placeables the orbits use.\n"
            "        /// </summary>\n"
            f"        public const string ExteriorResref = {q(EXTERIOR_RESREF)};\n"
            "        /// <summary>The tag every station exterior model is spawned with.</summary>\n"
            f"        public const string ExteriorTag = {q(EXTERIOR_TAG)};\n"
            f"        public static readonly float[] ExteriorScaleByLevel = {{ {scales} }};\n\n"
            "        /// <summary>Two anchor sites per orbit with an NPC dock and one or two per Wild Space system.</summary>\n"
            "        public static readonly IReadOnlyList<StationAnchor> Anchors = new List<StationAnchor>\n        {\n"
            f"{anchor_lines}\n        }};\n\n"
            "        // The deck\n"
            f"        public const string DeckAreaResref = {q(DECK_AREA)};\n"
            f"        public const float DeckSlotClearance = {f(DECK_SLOT_CLEARANCE)};\n"
            f"        public const float DeckEntranceX = {f(ex)};\n"
            f"        public const float DeckEntranceY = {f(ey)};\n"
            f"        public const float DeckEntranceFacing = {f(efacing)};\n\n"
            "        /// <summary>The civic buildings' slots and the station-only slots kept clear for later buildings.</summary>\n"
            "        public static readonly IReadOnlyDictionary<StationDeckSlotType, StationDeckSlot> DeckSlots = "
            "new Dictionary<StationDeckSlotType, StationDeckSlot>\n        {\n"
            f"{slot_lines}\n        }};\n\n"
            "        // The passenger shuttle\n"
            f"        public const int OrbitHopFare = {ORBIT_HOP_FARE};\n"
            f"        public const int OrbitHopSeconds = {ORBIT_HOP_SECONDS};\n"
            f"        public const int OrbitHopPeriodSeconds = {ORBIT_HOP_PERIOD_SECONDS};\n"
            f"        public const int WildSpaceFare = {WILD_SPACE_FARE};\n"
            f"        public const int WildSpaceSeconds = {WILD_SPACE_SECONDS};\n"
            f"        public const int WildSpacePeriodSeconds = {WILD_SPACE_PERIOD_SECONDS};\n\n"
            + buildings_cs() +
            "    }\n}\n")


def buildings_cs():
    q = json.dumps
    building_lines = "\n".join(
        f"            new StationBuildingDetail {{ Structure = StructureType.{b}, Name = {q(name)}, "
        f"ItemResref = {q(f'structure_{sid}')}, Recipe = RecipeType.{b}, RecipeLevel = {level}, "
        f"Components = new[] {{ {', '.join(f'({q(r)}, {n})' for r, n in comps)} }}, Slot = StationDeckSlotType.{slot}, "
        f"ConsoleResref = {q(console)}, ModelResref = {q(model)} }},"
        for b, sid, name, rid, level, comps, slot, console, model, _ in BUILDINGS)
    table_lines = "\n".join(
        f"            [{q(area)}] = new[] {{ {', '.join(f'({q(t)}, {w})' for t, w in rows)} }},"
        for area, rows in drill_tables().items())
    hull_lines = ", ".join(f"[{lvl}] = ShipSize.{size}" for lvl, size in BEACON_MAX_HULL.items())
    fuel_lines = ", ".join(f"[ShipSize.{size}] = {n}" for size, n in JUMP_FUEL.items())
    discount_lines = ", ".join(f"[{q(tag)}] = {pct}" for tag, pct in JUMP_FUEL_DISCOUNT.items())
    return ("        // Station-only buildings: the Shipyard, the Asteroid Drill and the Jump Beacon\n"
            "        public static readonly IReadOnlyList<StationBuildingDetail> Buildings = new List<StationBuildingDetail>\n        {\n"
            f"{building_lines}\n        }};\n\n"
            f"        public const float BuildingModelOffset = {f(BUILDING_MODEL_OFFSET)};\n\n"
            "        // The Shipyard\n"
            f"        public const float ShipyardRange = {f(SHIPYARD_RANGE)};\n"
            f"        public const int ShipyardFeeMax = {SHIPYARD_FEE_MAX};\n\n"
            "        // The Asteroid Drill\n"
            f"        public const int DrillUnitsPerDay = {DRILL_UNITS_PER_DAY};\n"
            f"        public const int DrillStorageDays = {DRILL_STORAGE_DAYS};\n\n"
            "        /// <summary>Each anchor area's asteroid loot tables, weighted as its asteroids spawn.</summary>\n"
            "        public static readonly IReadOnlyDictionary<string, (string LootTable, int Weight)[]> DrillTables = "
            "new Dictionary<string, (string LootTable, int Weight)[]>\n        {\n"
            f"{table_lines}\n        }};\n\n"
            "        // The Jump Beacon\n"
            f"        public const int BeaconMaxLevel = {BEACON_MAX_LEVEL};\n"
            "        public static readonly IReadOnlyDictionary<int, ShipSize> BeaconMaxHull = "
            f"new Dictionary<int, ShipSize> {{ {hull_lines} }};\n"
            "        public static readonly IReadOnlyDictionary<ShipSize, int> JumpFuel = "
            f"new Dictionary<ShipSize, int> {{ {fuel_lines} }};\n"
            "        public static readonly IReadOnlyDictionary<string, int> JumpFuelDiscountPercent = "
            f"new Dictionary<string, int> {{ {discount_lines} }};\n")


WRITES = {"SWLOR.Game.Server/Service/StationService/StationTuning.cs": tuning_cs}


# ---------------------------------------------------------------------------------------------------------------
# Bible tab
# ---------------------------------------------------------------------------------------------------------------
def stations_tab(Sheet):
    s = Sheet("Player Stations (Revamp)", [26, 18, 16, 14, 14, 14, 14, 60])
    s.title("Player Stations (Revamp): anchor sites, founding, upkeep, the deck, docking, exchanges, the shuttle and the station-only buildings")
    s.note("Dev Status: Proposed (space revamp W13a and W13b drafts). Generated by tools/SpaceRevampBalance/stations.py; plan section 25.")
    s.blank()
    s.title("Founding and upkeep (the planet city's numbers)")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Station Core", CORE_ITEM_NAME, "", "", "", "", "", f"Fabrication level {CORE_RECIPE_LEVEL}: "
         + ", ".join(f"{n} {r}" for r, n in CORE_COMPONENTS) + " (the City Hall's and the Starport's recipes together)"],
        ["Founding price", INITIAL_PRICE, "", "", "", "", "", "credits, as a planet city"],
        ["Founding needs", f"City Management {MINIMUM_CITY_MANAGEMENT}", "", "", "", "", "",
         f"no other citizenship; within {FOUNDING_RANGE:.0f} m of a free anchor site in space"],
        ["Upkeep", f"{PRICE_PER_DAY} a day", "", "", "", "", "",
         f"charged weekly ({PRICE_PER_DAY * 7}), -{UPKEEP_PERK_REDUCTION:.0%} per Upkeep perk level, "
         f"+{UPGRADE_PRICE_PER_LEVEL} per Medical Center, Starport and Cantina level above 1"],
        ["Disrepair", f"{DISREPAIR_DAYS} days", "", "", "", "", "", "unpaid upkeep past this loses the station"],
        ["Citizens", " / ".join(str(c) for c in CITIZENS_REQUIRED), "", "", "", "", "",
         f"for levels 1-5; below {CITIZENS_REQUIRED[0]} for {GRACE_HOURS} h loses the station"],
        ["Limits", f"{BUILDING_LIMIT} buildings", f"{STRUCTURE_LIMIT} structures", "", "", "", "", "as a planet city"],
        ["Elections", f"every {ELECTION_DAYS} days", "", "", "", "", "", ""],
    ])
    s.table(["Upkeep perk", "Base week", "All upgrades level 1", "All level 3", "All level 5", "", "", ""],
            [[p, weekly_upkeep(p), weekly_upkeep(p), weekly_upkeep(p, 3, 3, 3), weekly_upkeep(p, 5, 5, 5), "", "", ""]
             for p in range(0, 4)])
    s.title("Anchor sites (one station per site)")
    s.table(["Anchor", "System", "Position", "Home port", "Exchange", "", "", "Notes"],
            [[a["id"], a["system"], f"{a['x']:.0f}, {a['y']:.0f}", PLANET_NAMES[a["planet"]], a["exchange"], "", "",
              f"Wild Space: nearest open port, {a['hops']} lanes from the charted orbit" if a["wild"] else "its orbit's NPC dock"]
             for a in anchors()])
    s.note(f"The station exterior ({EXTERIOR_RESREF}) stands {EXTERIOR_OFFSET:.0f} m from the anchor toward the area's middle, "
           "scaled " + " / ".join(f"{x:.1f}" for x in EXTERIOR_SCALE_BY_LEVEL) + " at city levels 1-5.")
    s.title("Deck slots (station_deck)")
    s.table(["Slot", "X", "Y", "Building", "", "", "", "Notes"],
            [[slot, x, y, b or "-", "", "", "", "snaps here, one per station" if b else "kept clear (Station Fuel is cargo)"]
             for slot, x, y, b in DECK_SLOTS])
    s.note(f"Other buildings and structures keep {DECK_SLOT_CLEARANCE:.0f} m from every slot.")
    s.title("Docking, exchanges and the shuttle")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Docking", "Large", "", "", "", "", "", "any station Starport; Capital hulls need starport level 2"],
        ["Exchange", "home port's", "", "", "", "", "", "same goods, prices and flooding; the mayor's tariff goes to the treasury"],
        ["Orbit shuttle", f"{ORBIT_HOP_FARE} cr", f"{ORBIT_HOP_SECONDS} s", f"every {ORBIT_HOP_PERIOD_SECONDS} s", "", "", "",
         "to and from the home port, plus the transport tax"],
        ["Wild Space shuttle", f"{WILD_SPACE_FARE} cr", f"{WILD_SPACE_SECONDS} s", f"every {WILD_SPACE_PERIOD_SECONDS} s", "", "", "",
         "to and from the nearest open port; passengers only"],
    ])
    s.title("Station-only buildings (W13b): consoles at their deck slots, one each per station")
    s.table(["Building", "Recipe", "Level", "Components", "Console", "Model", "", "Notes"],
            [[name, f"RecipeType {rid}", level, ", ".join(f"{n} {r}" for r, n in comps), console, model, "", f"structure_{sid}; {desc}"]
             for _, sid, name, rid, level, comps, _, console, model, desc in BUILDINGS])
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Shipyard gate", "Capital deeds", "", "", "", "", "", f"crafted only within {SHIPYARD_RANGE:.0f} m of a Shipyard console that admits the crafter"],
        ["Shipyard access", "Citizens / Anyone", "", "", "", "", "", "the mayor's choice (Edit Taxes permission), citizens by default"],
        ["Shipyard fee", f"0-{SHIPYARD_FEE_MAX}", "", "", "", "", "", "credits per Capital hull, paid into the treasury (PropertyTaxType.Shipyard 4, Manage City)"],
        ["Shipyard droid", f"+{droids.SHIPYARD_BUILD_SPEED_PERCENT_PER_INDUSTRY:g}% / Industry", f"+{droids.SHIPYARD_BUILD_SPEED_PERCENT_PER_INDUSTRY * MAX_INDUSTRY:.0f}% at {MAX_INDUSTRY}", "", "", "", "",
         "crafting progress per synthesis step on Capital deeds (StationDroidRules)"],
        ["Drill output", f"{DRILL_UNITS_PER_DAY} ore a day", f"{DRILL_STORAGE_DAYS} days stored", "", "", "", "",
         "rolled from the anchor area's asteroid tables in their spawn shares; stops while full"],
        ["Drill droid", f"+{droids.DRILL_OUTPUT_PERCENT_PER_INDUSTRY:g}% / Industry", f"+{drill_droid_percent(MAX_INDUSTRY)}% at {MAX_INDUSTRY}",
         f"+{drill_droid_percent(MAX_INDUSTRY, BEST_EFFICIENCY_PERCENT)}% with Efficiency", "", "", "", "output (StationDroidRules)"],
        ["Beacon level", "1-3", "", "", "", "", "", "a city upgrade (Manage City), +" + f"{UPGRADE_PRICE_PER_LEVEL} upkeep a week per level above 1; "
         + ", ".join(f"level {lvl} {size}" for lvl, size in BEACON_MAX_HULL.items()) + " hulls; both beacons must take the hull"],
        ["Jump fuel", " / ".join(f"{size} {n}" for size, n in JUMP_FUEL.items()), "", "", "", "", "",
         "Station Fuel units from the ship's cargo bay; the Hutt Grand Barge burns 50% less (rounded up)"],
        ["Beacon access", " / ".join(BEACON_ACCESS), "", "", "", "", "", "who may jump to and from it (Edit Taxes permission), citizens by default; allies are the citizens of the stations the mayor lists"],
        ["Wild Space", "never", "", "", "", "", "", "no beacon in Wild Space, and no jump into or out of it"],
    ])
    s.title(f"Asteroid Drill income against an active miner (mining.py: three matched Mining Lasers, ore at {STORE_BUY_RATE:.0%} of Cost)")
    s.table(["Anchor area", "Tier", "Credits / ore", "Drill / day", "Best droid / day", "Miner / hour", "Hours", "Notes"],
            [[r["area"], r["tier"], round(r["unit"], 1), round(r["day"]), round(r["best"]), round(r["miner"]),
              f"{r['hours']:.2f} / {r['best_hours']:.2f}", "mining hours a day without / with the best droid"]
             for r in drill_income()])
    return s


def check_anchors():
    """Every anchor is in a known area, inside the map, and clear of the area's placeables and waypoints."""
    problems = []
    for r in drill_income():
        if r["best_hours"] >= DRILL_MAX_MINING_HOURS:
            problems.append(f"{r['area']} drill pays {r['best_hours']:.2f} mining hours a day")
    seen = set()
    for a in anchors():
        if a["id"] in seen:
            problems.append(f"duplicate anchor {a['id']}")
        seen.add(a["id"])
        if not (30 <= a["x"] <= 290 and 30 <= a["y"] <= 290):
            problems.append(f"{a['id']} too close to the edge")
    return problems


def main():
    problems = check_anchors()
    if problems:
        print("\n".join(problems))
        sys.exit(1)
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
    print(f"{len(anchors())} anchors, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
