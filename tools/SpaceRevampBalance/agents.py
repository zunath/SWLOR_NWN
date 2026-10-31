"""Space revamp faction agents (plan section 29, W15a): where the agents stand, the missions they offer, what the
missions pay, and the faction token stores.

usage: python agents.py            (writes the generated C# tuning and the test reference)
       python agents.py --check    (exit 1 when a generated file is out of date)
       python agents.py --sim      (prints the agent mission payout against space combat per tier)

Every agent number lives here. bible_tabs.py calls agents_tab(Sheet) to write the Faction Agents (Revamp) tab.

Sources:
- Placement: plan section 29's table, mapped to the standing factions W7c uses (trade.py EXCHANGE_FACTION). The table
  lists Korriban first for the Sith Military, but Dathomir (tier 5) is the lower tier, so it is the Sith's first dock.
- Dock tiers: trade.py EXCHANGES (Viscara and CZ-220 1, Mon Cala 2, Hutlar 3, Tatooine and Smuggler's Moon 4,
  Dathomir and Dantooine 5, Korriban 6).
- Mission orbits: the orbits and zones whose spawn tables fly the mission tier's faction ships
  (SpaceSpawnDefinition: Smuggler's Moon flies tier 1, Mid Rim 1 tier 5, Mid Rim 2 tier 6), with trade.py's lane graph.
- Space combat credits per hour: scanning.combat_rate (trade.py's baseline with the space lockboxes normal kills drop).
- Kill, site, slicing and raider values: scanning.py and slicing.py, valued the same way they are there.
- Ore: the asteroid tables' tier ores (HarvestingLootTableDefinition), mined at mining.open_orbit_ore_per_hour() and given
  up at the 40% store markdown.
- Store prices: rewards.DESIGN_SCHEMATIC_COST and SHIP_SCHEMATIC_COST, the item blueprints' Cost.
"""
import json
import math
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import rewards as rw  # noqa: E402
import scanning  # noqa: E402
import mining  # noqa: E402
import slicing  # noqa: E402
import trade  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
TIERS = [1, 2, 3, 4, 5, 6]

# ---------------------------------------------------------------------------------------------------------------
# Agents (plan section 29): two per faction at different docks, no dock with more than two, the first at the lower tier
# ---------------------------------------------------------------------------------------------------------------
# Dock: (landing area resref = the dock id Trade uses, exchange, dock name). The agent stands beside the dock's Dock
# Master in the same area.
DOCKS = {
    "velesinterior": ("Viscara", "Veles Starport (Viscara)"),
    "czs220_hangar": ("CZ220", "CZ-220 Hangar"),
    "moncaladaccityex": ("MonCala", "Dac City Starport (Mon Cala)"),
    "hutlar_outpost": ("Hutlar", "Hutlar Outpost"),
    "tat_anc_astropor": ("Tatooine", "Anchorhead Spaceport (Tatooine)"),
    "pw_ar_nsshipyard": ("SmugglersMoon", "Smuggler's Moon Landing"),
    "spacenarshaddung": ("SmugglersMoon", "Smuggler's Moon Station"),
    "dath_landingpad": ("Dathomir", "Dathomir Landing Pad"),
    "dan_centcolony": ("Dantooine", "Dantooine Central Colony"),
    "korribanlandingp": ("Korriban", "Korriban Landing Platform"),
}

FACTION_NAMES = {"RepublicMilitary": "Republic Military", "Czerka": "Czerka Corporation", "Smuggler": "Smugglers",
                 "HuttCartel": "Hutt Cartel", "Mandalorian": "Mandalorian", "SithMilitary": "Sith Military"}
# The ship maker whose Design Schematics, Ship Schematics and manufacturer component each faction's store sells.
FACTION_MAKER = {"RepublicMilitary": "Republic", "Czerka": "Czerka", "Smuggler": "Corellian", "HuttCartel": "Hutt",
                 "Mandalorian": "Mandalorian", "SithMilitary": "Sith"}
# Makers with no faction of their own whose manufacturer component the black-market factions' stores also sell (W11o,
# Appendix D: the Chiss Sensor Array comes from the black markets at Smuggler's Moon, Tatooine and Hutlar). The Smugglers
# and the Hutt Cartel have the agents at Smuggler's Moon and Tatooine; Hutlar's agents are Czerka and Mandalorian. Same
# category, price and standing gate as the faction maker's own component. The Rift Raider part stays Wild Space only.
BLACK_MARKET_MAKERS = {"Smuggler": ["Chiss"], "HuttCartel": ["Chiss"]}

MISSION_TYPES = ["Kill", "SealedCourier", "MiningQuota", "ConvoyEscort", "ScanDown", "DataHeist"]
MISSION_TYPE_IDS = {k: i for i, k in enumerate(MISSION_TYPES, 1)}
MISSION_NAMES = {"Kill": "Kill", "SealedCourier": "Sealed courier", "MiningQuota": "Mining quota",
                 "ConvoyEscort": "Convoy escort", "ScanDown": "Scan-down", "DataHeist": "Data heist"}

# Each faction's mission weights on its board (kill, courier, mining, convoy, scan-down, heist): a flavor, not a rule.
WEIGHTS = {
    "RepublicMilitary": [3, 1, 1, 3, 2, 1],
    "Czerka": [1, 2, 3, 2, 2, 1],
    "Smuggler": [1, 3, 1, 1, 2, 3],
    "HuttCartel": [2, 3, 1, 1, 1, 2],
    "Mandalorian": [4, 1, 1, 2, 2, 1],
    "SithMilitary": [3, 1, 1, 1, 2, 3],
}

# (enum, name, faction, dock, appearance donor blueprint, greeting). Order: by faction, first dock first.
AGENTS = [
    ("RepublicVeles", "Lieutenant Corin Valse", "RepublicMilitary", "velesinterior", "republicdeskw003",
     "Republic Fleet Command needs pilots it can count on. Prove you are one, and the Fleet remembers."),
    ("RepublicDantooine", "Commander Neela Tarsk", "RepublicMilitary", "dan_centcolony", "frepsoldier",
     "The Outer Rim lanes are ours only as long as someone flies them. Are you that someone?"),
    ("CzerkaCZ220", "Requisitions Officer Mira Kell", "Czerka", "czs220_hangar", "femaleczerkaempl",
     "Czerka pays for results, pilot. Quotas met, cargo delivered, invoices settled."),
    ("CzerkaHutlar", "Contracts Manager Dorn Halvek", "Czerka", "hutlar_outpost", "maleczerkaemploy",
     "Hutlar is a long way from head office. Out here a contractor with a ship is worth three with clipboards."),
    ("SmugglerMonCala", "Teeso Vrill", "Smuggler", "moncaladaccityex", "npc_rodiansmugg",
     "You did not see me, I did not see you. Now, about that job."),
    ("SmugglerStation", "Kesha Doon", "Smuggler", "spacenarshaddung", "npc_rodianfem01",
     "Everything on this station has a price, pilot. Some prices are paid in favors."),
    ("HuttTatooine", "Bargo Ruul", "HuttCartel", "tat_anc_astropor", "malebh2",
     "The Cartel rewards loyalty. It also remembers everything else."),
    ("HuttSmugglersMoon", "Vex Marrock", "HuttCartel", "pw_ar_nsshipyard", "human_bh01",
     "The Hutts own this moon, pilot. Work for them and it is a very friendly moon."),
    ("MandalorianHutlar", "Kast Varad", "Mandalorian", "hutlar_outpost", "sol_mandotrooper",
     "Your ship is your armor out there. Show me you know how to wear it."),
    ("MandalorianDathomir", "Kaldren Varos", "Mandalorian", "dath_landingpad", "sol_mandotrooper",
     "The clans hunt the strongest prey in this sector. Hunt with us."),
    ("SithDathomir", "Lieutenant Vael Moraq", "SithMilitary", "dath_landingpad", "g_sithtroop",
     "The Empire has use for capable pilots. It has no use for anything else."),
    ("SithKorriban", "Captain Seris Drask", "SithMilitary", "korribanlandingp", "g_elite_sith",
     "Korriban's skies belong to the Empire. Keep them that way and you will be rewarded."),
]
AGENT_IDS = {a[0]: i for i, a in enumerate(AGENTS, 1)}
MAXIMUM_AGENTS_PER_DOCK = 2


def dock_tier(dock):
    return trade.exchange(DOCKS[dock][0])[5]


def dock_orbit(dock):
    return trade.exchange(DOCKS[dock][0])[3]


# ---------------------------------------------------------------------------------------------------------------
# The board: a small rotating list of offers per agent, the same for every player, replaced at 00:00 UTC
# ---------------------------------------------------------------------------------------------------------------
STANDARD_OFFERS = 2           # at the dock's tier, open from Neutral standing
PRIORITY_OFFERS = 1           # one tier higher (at most 6), open from Trusted
PRIORITY_STANDING = 250       # Trusted (trade.py STANDING_TIERS)
INTEL_STANDING = 0            # an intel lead is open to Neutral pilots who hold agent intel
MAXIMUM_ACTIVE = 3            # missions held at once, across every agent
# A mission taken from a board cannot be taken again; the board changes every day, so this is the daily cooldown.

STANDING_REWARD = {"Standard": 15, "Priority": 20, "Intel": 25}  # freight deliveries give 10 (W7c)
INTEL_TOKEN_MULTIPLIER = 2

# ---------------------------------------------------------------------------------------------------------------
# Payout: each mission pays its time at AGENT_PREMIUM x the tier's space combat credits per hour, less what the activity
# already pays (kill loot, site caches, slicing rewards), plus the value of ore the player gives up.
# ---------------------------------------------------------------------------------------------------------------
AGENT_PREMIUM = 1.08          # inside the plan's 100-115% for agent missions
MINIMUM_SHARE = 0.25          # a mission's own credits never fall below this share of its time's target
BASE_MINUTES = 3.0            # talking to the agent, launching and docking again; assumed
HOP_MINUTES = trade.HOP_MINUTES  # one starlane, each way
KILL_MINUTES = {"Invalid": 1.0, "Fighter": 1.2, "Interceptor": 1.5, "Gunship": 4.0}  # per ship, hunting one role
KILL_COUNT = {"Invalid": 10, "Fighter": 8, "Interceptor": 6, "Gunship": 3}           # "Invalid" = any ship of the tier
KILL_ROLES = list(KILL_COUNT)
ORE = {1: "ore_tilarium", 2: "ore_currian", 3: "ore_idailia", 4: "ore_barinium", 5: "ore_gostian", 6: "ore_gostian"}
ORE_QUANTITY = {1: 60, 2: 60, 3: 60, 4: 60, 5: 60, 6: 60}
# Ore a minute mining the quota's tier rocks (mining.py; W11e, was an assumed 360 ore an hour at every tier). Tier 6
# hands in Gostian, a tier 5 rock.
ORE_PER_MINUTE = {t: mining.open_orbit_ore_per_hour(min(5, t)) / 60 for t in range(1, 7)}
COURIER_VOLUME = {1: 10, 2: 10, 3: 20, 4: 20, 5: 40, 6: 40}
COURIER_STOP_MINUTES = trade.STOP_MINUTES
CONVOY_FLIGHT_MINUTES = 4.0   # the hauler's flight to its starlane; assumed
CONVOY_RAIDERS = {1: 2, 2: 2, 3: 3, 4: 3, 5: 4, 6: 4}
CONVOY_RAIDER_DELAY_SECONDS = 45
CONVOY_SPAWN_DELAY_SECONDS = 10
CONVOY_SPAWN_METERS = 30
SCAN_DOWN_SITES = ["CombatSite", "CacheSite", "Derelict"]
TOKEN_SHARE = 0.20            # tokens add about a fifth of the credits on top
TOKEN_VALUE = 100             # credits a token buys at its faction store (a Design Schematic: 2,000 credits, 20 tokens)
MARKDOWN = 0.40


def round25(x):
    return int(25 * math.floor(x / 25 + 0.5))


def rate_per_minute(tier):
    return AGENT_PREMIUM * scanning.combat_rate(tier) / 60


def kill_value(tier):
    """One kill's credits, as the combat baseline counts them (60 kills an hour)."""
    return scanning.combat_rate(tier) / trade.KILLS_PER_HOUR


def item_name(resref):
    data = json.loads((REPO / "Module" / "uti" / f"{resref}.uti.json").read_text(encoding="latin-1"))
    return data["LocalizedName"]["value"]["0"]


def ore_value(tier):
    return trade.item_cost(ORE[tier]) * MARKDOWN


def raider_squad(tier):
    size = CONVOY_RAIDERS[tier]
    squad = [f"t{tier}pirint"] + ([f"t{tier}pirgun"] if size >= 4 else [])
    return squad + [f"t{tier}pirftr"] * (size - len(squad))


def raider_value(tier):
    return sum(scanning.kill_value(t) for t in raider_squad(tier))


def scan_signature_minutes(kind, tier):
    rank = 10 * min(tier, 5) - 5
    return scanning.cycles_to_resolve(kind, tier, rank) * scanning.PROBE_CYCLE_SECONDS / 60


def scan_down_minutes(kind, tier):
    return scan_signature_minutes(kind, tier) + scanning.JUMP_MINUTES + scanning.site_minutes(kind, tier)


def scan_down_value(kind, tier):
    return scanning.site_value(kind, tier)


def heist_guards(tier):
    return scanning.combat_squad(tier)


def heist_minutes(tier):
    return (scan_signature_minutes("DataSite", tier) + scanning.JUMP_MINUTES + len(heist_guards(tier)) * scanning.KILL_MINUTES
            + slicing.SLICE_MINUTES / slicing.SLICE_SUCCESS)


def heist_value(tier):
    return (sum(scanning.kill_value(t) for t in heist_guards(tier))
            + slicing.expected_value("DataCan", slicing.target_tier(tier)))


def travel_minutes(hops):
    return BASE_MINUTES + 2 * HOP_MINUTES * hops


def mission_minutes(kind, tier, hops=0, role="Invalid", site="CombatSite"):
    """The time a mission takes; a courier's hops are the jumps to its destination."""
    if kind == "Kill":
        return travel_minutes(hops) + KILL_COUNT[role] * KILL_MINUTES[role]
    if kind == "SealedCourier":
        return BASE_MINUTES + COURIER_STOP_MINUTES + HOP_MINUTES * max(1, hops)
    if kind == "MiningQuota":
        return travel_minutes(0) + ORE_QUANTITY[tier] / ORE_PER_MINUTE[tier]
    if kind == "ConvoyEscort":
        return travel_minutes(hops) + CONVOY_FLIGHT_MINUTES + CONVOY_RAIDERS[tier] * scanning.KILL_MINUTES
    if kind == "ScanDown":
        return travel_minutes(hops) + scan_down_minutes(site, tier)
    if kind == "DataHeist":
        return travel_minutes(hops) + heist_minutes(tier)
    raise ValueError(kind)


def own_value(kind, tier, role="Invalid", site="CombatSite"):
    """What the activity pays besides the agent: negative when the player gives something up (ore)."""
    if kind == "Kill":
        return KILL_COUNT[role] * kill_value(tier)
    if kind == "MiningQuota":
        return -ORE_QUANTITY[tier] * ore_value(tier)
    if kind == "ConvoyEscort":
        return raider_value(tier)
    if kind == "ScanDown":
        return scan_down_value(site, tier)
    if kind == "DataHeist":
        return heist_value(tier)
    return 0.0


def credits(kind, tier, hops=0, role="Invalid", site="CombatSite"):
    minutes = mission_minutes(kind, tier, hops, role, site)
    target = rate_per_minute(tier) * minutes
    return round25(max(target - own_value(kind, tier, role, site), MINIMUM_SHARE * target))


def tokens(mission_credits, intel=False):
    return max(1, int(math.floor(mission_credits * TOKEN_SHARE / TOKEN_VALUE + 0.5))) * (INTEL_TOKEN_MULTIPLIER if intel else 1)


# ---------------------------------------------------------------------------------------------------------------
# Mission orbits: where kill, convoy, scan-down and heist missions of a tier send the pilot
# ---------------------------------------------------------------------------------------------------------------
ORBITS = {
    1: ["viscaraorbit", "narshadorbit", "space_viscbelt"],
    2: ["moncalaorbit", "space_viscbelt"],
    3: ["hutlar_orbit", "space_midrimneb"],
    4: ["tatooineorbit", "space_midrimneb"],
    5: ["space_dathomir", "dantooineorbit", "space_midrim1", "space_korrdebris"],
    6: ["space_korriban", "space_midrim2"],
}
ORBIT_NAMES = dict(trade.ORBIT_NAMES)


def mission_orbits(dock, tier):
    """The tier's orbits nearest the dock: the closest and any one jump further out, with their jumps."""
    start = dock_orbit(dock)
    pairs = [(o, trade.hops(start, o)) for o in ORBITS[tier]]
    nearest = min(h for _, h in pairs)
    return [(o, h) for o, h in pairs if h <= nearest + 1]


def mission_tiers(dock):
    t = dock_tier(dock)
    return sorted({t, min(6, t + 1)})


def courier_destinations(dock):
    here = DOCKS[dock][0]
    return [(x[0], trade.hops(dock_orbit(dock), x[3])) for x in trade.EXCHANGES
            if x[0] != here and trade.hops(dock_orbit(dock), x[3]) >= 1]


# ---------------------------------------------------------------------------------------------------------------
# Faction token stores (one per faction, at both of its agents)
# ---------------------------------------------------------------------------------------------------------------
# Tokens are a currency per faction (CurrencyType 5-10, the Currencies window, existing coin icons iit_coin_001-006):
# earned only from that faction's agents, spent only at its store, never traded. Exchange value: TOKEN_VALUE credits.
_SHIPS = REPO / "SWLOR.Game.Server" / "Feature" / "ShipDefinition" / "RevampShipCatalog.cs"
_REWARDS = REPO / "SWLOR.Game.Server" / "Feature" / "ShipRewardDefinition" / "RevampRewardCatalog.cs"
_COMPONENTS = REPO / "SWLOR.Game.Server" / "Feature" / "ShipComponentDefinition" / "RevampComponentCatalog.cs"

STORE_GATES = {"Ammo": "Neutral", "DesignSchematic": "Trusted", "ManufacturerComponent": "Honored", "RiftKey": "Honored",
               "ShipSchematic": "Exalted"}
STANDING_TIER_IDS = {"Neutral": 0, "Trusted": 1, "Honored": 2, "Exalted": 3}
AMMO = ["ship_missile_ion", "ship_missile_inc", "ship_missile_skr"]
AMMO_QUANTITY = 10
RIFT_KEY = "rift_key_t55"
# Rift Keys of tiers 1-5 (W15b, events.KEYS): each store sells the keys up to its faction's highest dock tier (at most 5)
# beside the tier 5.5 key, priced by each key's Cost and gated by standing that rises with the key's tier.
RIFT_KEY_GATES = {10: "Neutral", 20: "Trusted", 30: "Trusted", 40: "Honored", 50: "Honored", 55: "Honored"}
STORE_CATEGORIES = ["Ammo", "DesignSchematic", "ManufacturerComponent", "RiftKey", "ShipSchematic"]
STORE_CATEGORY_IDS = {k: i for i, k in enumerate(STORE_CATEGORIES, 1)}


def price(credit_value):
    return max(1, int(math.floor(credit_value / TOKEN_VALUE + 0.5)))


def ship_makers():
    text = _SHIPS.read_text(encoding="utf-8")
    return {m.group(1): (m.group(2), m.group(3) == "true")
            for m in re.finditer(r'Tag = "(sd_\w+)", Name = "[^"]*",.*?Manufacturer = ShipManufacturer\.(\w+), Advanced = (\w+)', text)}


def design_rows():
    text = _REWARDS.read_text(encoding="utf-8")
    return [dict(ShipTag=m.group(1), ShipName=m.group(2), Name=m.group(3), Resref=m.group(4), WildSpaceOnly=m.group(5) == "true")
            for m in re.finditer(r'new ShipHullDesign \{ ShipTag = "(\w+)", ShipName = "([^"]*)", Appearance = \(AppearanceType\)\d+, '
                                 r'Name = "([^"]*)", Resref = "(\w+)", Tier = \d+, WildSpaceOnly = (\w+)', text)]


def schematic_rows():
    text = _REWARDS.read_text(encoding="utf-8")
    return [dict(ShipTag=m.group(1), ShipName=m.group(2), Resref=m.group(3), WildSpaceOnly=m.group(4) == "true")
            for m in re.finditer(r'new ShipSchematic \{ ShipTag = "(\w+)", ShipName = "([^"]*)", RecipeType = RecipeType\.\w+, '
                                 r'Resref = "(\w+)", Tier = \d+, WildSpaceOnly = (\w+)', text)]


def maker_component(maker):
    text = _COMPONENTS.read_text(encoding="utf-8")
    m = re.search(r'Manufacturer = ShipManufacturer\.' + maker + r', Name = "([^"]*)", Family = ShipComponentFamily\.\w+, '
                  r'Resref = "(\w+)", Trait = ShipComponentTraitType\.(\w+)', text)
    return dict(Name=m.group(1), Resref=m.group(2), Trait=m.group(3))


def component_value():
    """A manufacturer component's worth: its tier 5 item at full Cost (it cannot be crafted) plus a rare trait's premium."""
    return 2 * trade.item_cost("sc_shieldprj_5")


def store_prices():
    return {
        "Ammo": price(sum(trade.item_cost(r) for r in AMMO) / len(AMMO) * AMMO_QUANTITY),
        "DesignSchematic": price(rw.DESIGN_SCHEMATIC_COST),
        "ManufacturerComponent": price(component_value()),
        "RiftKey": price(trade.item_cost(RIFT_KEY)),
        "ShipSchematic": price(rw.SHIP_SCHEMATIC_COST),
    }


def faction_key_tier(faction):
    """The highest Rift Key tier (1-5) the faction's store sells: its highest agent dock's tier."""
    return min(5, max(dock_tier(dock) for _, _, f, dock, _, _ in AGENTS if f == faction))


def rift_keys(faction):
    """The faction store's Rift Keys in tier order: tiers 1 up to its dock tier, then tier 5.5."""
    import events
    top = faction_key_tier(faction) * 10
    return [(t, r) for t, r, _ in events.KEYS if t <= top] + [(t, r) for t, r, _ in events.KEYS if t == 55]


def rift_key_price(resref):
    return price(trade.item_cost(resref))


def store(faction):
    """The faction store's items: (category, resref, name, quantity, trait)."""
    maker = FACTION_MAKER[faction]
    makers = ship_makers()
    rows = [("Ammo", r, None, AMMO_QUANTITY, None) for r in AMMO]
    rows += [("DesignSchematic", d["Resref"], f"Design Schematic: {d['ShipName']} {d['Name']}", 1, None)
             for d in design_rows() if makers.get(d["ShipTag"], ("", False))[0] == maker and not d["WildSpaceOnly"]]
    for m in [maker] + BLACK_MARKET_MAKERS.get(faction, []):
        comp = maker_component(m)
        rows.append(("ManufacturerComponent", comp["Resref"], comp["Name"], 1, comp["Trait"]))
    rows += [("RiftKey", r, None, 1, None) for _, r in rift_keys(faction)]
    rows += [("ShipSchematic", s["Resref"], f"Ship Schematic: {s['ShipName']}", 1, None)
             for s in schematic_rows() if makers.get(s["ShipTag"], ("", False))[0] == maker and not s["WildSpaceOnly"]]
    return rows


# ---------------------------------------------------------------------------------------------------------------
# Checks and the simulation
# ---------------------------------------------------------------------------------------------------------------
def check_placement():
    problems = []
    per_dock = {}
    for enum, _, faction, dock, _, _ in AGENTS:
        per_dock.setdefault(dock, []).append(enum)
    for dock, agents in per_dock.items():
        if len(agents) > MAXIMUM_AGENTS_PER_DOCK:
            problems.append(f"{dock} hosts {len(agents)} agents")
    for faction in FACTION_NAMES:
        mine = [a for a in AGENTS if a[2] == faction]
        if len(mine) != 2 or mine[0][3] == mine[1][3]:
            problems.append(f"{faction} needs two agents at different docks")
        elif dock_tier(mine[0][3]) > dock_tier(mine[1][3]):
            problems.append(f"{faction}'s first agent is not at the lower-tier dock")
    return problems


def board_mix(kind_weights=None):
    return {k: 1.0 for k in MISSION_TYPES} if kind_weights is None else dict(zip(MISSION_TYPES, kind_weights))


def mission_rate(kind, tier, hops):
    """Credits per hour of one kind: its credits plus what the activity pays (less the ore given up), tokens left out."""
    if kind == "Kill":
        variants = [dict(role=r) for r in KILL_ROLES]
    elif kind == "ScanDown":
        variants = [dict(site=s) for s in SCAN_DOWN_SITES]
    else:
        variants = [{}]
    value = sum(credits(kind, tier, hops, **v) + own_value(kind, tier, **v) for v in variants) / len(variants)
    minutes = sum(mission_minutes(kind, tier, hops, **v) for v in variants) / len(variants)
    return value / minutes * 60, minutes


SIM_HOPS = 1  # a mission orbit one jump from the dock, a courier run of one jump (the nearest destinations)


def tier_rate(tier, hops=SIM_HOPS, kind_weights=None, with_tokens=False):
    w = board_mix(kind_weights)
    total = sum(w.values())
    minutes = 0.0
    value = 0.0
    for kind, weight in w.items():
        rate, m = mission_rate(kind, tier, hops)
        minutes += weight / total * m
        value += weight / total * rate * m / 60
        if with_tokens:
            value += weight / total * tokens(credits(kind, tier, hops)) * TOKEN_VALUE
    return value / minutes * 60


def report():
    print("Faction agent missions against space combat (credits and the activity's own value; tokens on top):")
    for t in TIERS:
        c = scanning.combat_rate(t)
        print(f"  T{t}: combat {c:7.0f}/h, agents {tier_rate(t):7.0f}/h = {100 * tier_rate(t) / c:5.1f}%, "
              f"with tokens at {TOKEN_VALUE} cr {100 * tier_rate(t, with_tokens=True) / c:5.1f}%; "
              f"0 jumps {100 * tier_rate(t, 0) / c:5.1f}%, 2 jumps {100 * tier_rate(t, 2) / c:5.1f}%")
        for kind in MISSION_TYPES:
            rate, minutes = mission_rate(kind, t, SIM_HOPS)
            cr = credits(kind, t, SIM_HOPS)
            print(f"      {kind:13s} {minutes:5.1f} min, {cr:5d} cr (+{tokens(cr)} tokens), {rate:7.0f}/h = {100 * rate / c:5.1f}%")
    print("Per faction board mix, one jump:")
    for faction, weights in WEIGHTS.items():
        print(f"  {faction:16s} " + ", ".join(f"T{t} {100 * tier_rate(t, kind_weights=weights) / scanning.combat_rate(t):5.1f}%"
                                              for t in TIERS))
    print("Store prices (tokens):", store_prices())
    problems = check_placement()
    print("Placement:", "ok" if not problems else problems)


# ---------------------------------------------------------------------------------------------------------------
# Generated C# and the test reference
# ---------------------------------------------------------------------------------------------------------------
HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/agents.py. Do not edit by hand: change agents.py and regenerate.
// </auto-generated>
"""


def cs_double(x):
    return repr(float(x))


def cs_tier_doubles(f):
    return "new double[] { 0.0, " + ", ".join(cs_double(f(t)) for t in TIERS) + " }"


def cs_tier_ints(d):
    return "new[] { 0, " + ", ".join(str(d[t]) for t in TIERS) + " }"


def cs_string(s):
    return json.dumps(s)


def tuning_cs():
    lines = [HEADER.rstrip("\n"),
             "using System.Collections.Generic;",
             "using SWLOR.Game.Server.Service.FactionService;",
             "using SWLOR.Game.Server.Service.SpaceService;",
             "using SWLOR.Game.Server.Service.TradeService;",
             "",
             "namespace SWLOR.Game.Server.Service.FactionAgentService",
             "{",
             "    /// <summary>",
             "    /// The faction agent numbers (plan section 29): the agents, their boards, mission sizes, payouts and the faction",
             "    /// token stores. Arrays by tier hold a zero at index 0.",
             "    /// </summary>",
             "    public static class FactionAgentTuning",
             "    {",
             f"        public const int StandardOffers = {STANDARD_OFFERS};",
             f"        public const int PriorityOffers = {PRIORITY_OFFERS};",
             f"        public const int PriorityStanding = {PRIORITY_STANDING};",
             f"        public const int IntelStanding = {INTEL_STANDING};",
             f"        public const int MaximumActive = {MAXIMUM_ACTIVE};",
             f"        public const int StandingStandard = {STANDING_REWARD['Standard']};",
             f"        public const int StandingPriority = {STANDING_REWARD['Priority']};",
             f"        public const int StandingIntel = {STANDING_REWARD['Intel']};",
             f"        public const int IntelTokenMultiplier = {INTEL_TOKEN_MULTIPLIER};",
             f"        public const double MinimumShare = {cs_double(MINIMUM_SHARE)};",
             f"        public const double BaseMinutes = {cs_double(BASE_MINUTES)};",
             f"        public const double HopMinutes = {cs_double(HOP_MINUTES)};",
             f"        public const double CourierStopMinutes = {cs_double(COURIER_STOP_MINUTES)};",
             f"        public static readonly double[] OrePerMinute = {cs_tier_doubles(ORE_PER_MINUTE.get)};",
             f"        public const double ConvoyFlightMinutes = {cs_double(CONVOY_FLIGHT_MINUTES)};",
             f"        public const double RaiderKillMinutes = {cs_double(scanning.KILL_MINUTES)};",
             f"        public const float ConvoySpawnDelaySeconds = {CONVOY_SPAWN_DELAY_SECONDS}f;",
             f"        public const float ConvoyRaiderDelaySeconds = {CONVOY_RAIDER_DELAY_SECONDS}f;",
             f"        public const float ConvoySpawnMeters = {CONVOY_SPAWN_METERS}f;",
             f"        public const double TokenShare = {cs_double(TOKEN_SHARE)};",
             f"        public const int TokenValue = {TOKEN_VALUE};",
             f"        public const int AmmoQuantity = {AMMO_QUANTITY};",
             f"        public const string RiftKeyResref = {cs_string(RIFT_KEY)};",
             "",
             "        /// <summary>The NPC faction flying each tier's ships (plan section 11's ladder).</summary>",
             "        public static readonly string[] TierFactionNames = { \"\", " + ", ".join(cs_string(rw.FACTION_NAMES[t]) for t in TIERS) + " };",
             "        /// <summary>Credits a minute of mission time pays at each tier: the tier's space combat rate times the agent premium.</summary>",
             f"        public static readonly double[] CreditsPerMinute = {cs_tier_doubles(rate_per_minute)};",
             "        /// <summary>One kill's credits at each tier, as the combat baseline counts them.</summary>",
             f"        public static readonly double[] KillValue = {cs_tier_doubles(kill_value)};",
             "        public static readonly IReadOnlyDictionary<ShipNpcRole, int> KillCount = new Dictionary<ShipNpcRole, int> { "
             + ", ".join(f"[ShipNpcRole.{r}] = {KILL_COUNT[r]}" for r in KILL_ROLES) + " };",
             "        public static readonly IReadOnlyDictionary<ShipNpcRole, double> KillMinutes = new Dictionary<ShipNpcRole, double> { "
             + ", ".join(f"[ShipNpcRole.{r}] = {cs_double(KILL_MINUTES[r])}" for r in KILL_ROLES) + " };",
             "        public static readonly string[] OreResref = { \"\", " + ", ".join(cs_string(ORE[t]) for t in TIERS) + " };",
             "        public static readonly string[] OreName = { \"\", " + ", ".join(cs_string(item_name(ORE[t])) for t in TIERS) + " };",
             f"        public static readonly int[] OreQuantity = {cs_tier_ints(ORE_QUANTITY)};",
             f"        public static readonly double[] OreValue = {cs_tier_doubles(ore_value)};",
             f"        public static readonly int[] CourierVolume = {cs_tier_ints(COURIER_VOLUME)};",
             f"        public static readonly int[] ConvoyRaiders = {cs_tier_ints(CONVOY_RAIDERS)};",
             f"        public static readonly double[] ConvoyRaiderValue = {cs_tier_doubles(raider_value)};",
             "        public static readonly SignatureType[] ScanDownSites = { " + ", ".join(f"SignatureType.{s}" for s in SCAN_DOWN_SITES) + " };",
             "        public static readonly IReadOnlyDictionary<SignatureType, double[]> ScanDownMinutes = new Dictionary<SignatureType, double[]> { "
             + ", ".join(f"[SignatureType.{s}] = {cs_tier_doubles(lambda t, s=s: scan_down_minutes(s, t))}" for s in SCAN_DOWN_SITES) + " };",
             "        public static readonly IReadOnlyDictionary<SignatureType, double[]> ScanDownValue = new Dictionary<SignatureType, double[]> { "
             + ", ".join(f"[SignatureType.{s}] = {cs_tier_doubles(lambda t, s=s: scan_down_value(s, t))}" for s in SCAN_DOWN_SITES) + " };",
             f"        public static readonly double[] HeistMinutes = {cs_tier_doubles(heist_minutes)};",
             f"        public static readonly double[] HeistValue = {cs_tier_doubles(heist_value)};",
             "",
             "        /// <summary>The orbits whose spawn tables fly each tier's faction ships.</summary>",
             "        public static readonly IReadOnlyDictionary<int, string[]> MissionOrbits = new Dictionary<int, string[]>",
             "        {"]
    for t in TIERS:
        lines.append(f"            [{t}] = new[] {{ " + ", ".join(cs_string(o) for o in ORBITS[t]) + " },")
    lines += ["        };",
              "",
              "        /// <summary>Token prices by store category, and the standing tier each needs.</summary>",
              "        public static readonly IReadOnlyDictionary<FactionTokenStoreCategory, int> StorePrices = new Dictionary<FactionTokenStoreCategory, int> { "
              + ", ".join(f"[FactionTokenStoreCategory.{k}] = {v}" for k, v in store_prices().items()) + " };",
              "        public static readonly IReadOnlyDictionary<FactionTokenStoreCategory, TradeStandingTier> StoreGates = new Dictionary<FactionTokenStoreCategory, TradeStandingTier> { "
              + ", ".join(f"[FactionTokenStoreCategory.{k}] = TradeStandingTier.{v}" for k, v in STORE_GATES.items()) + " };",
              "        public static readonly string[] StoreAmmo = { " + ", ".join(cs_string(r) for r in AMMO) + " };",
              "",
              "        /// <summary>The Rift Keys each faction's store sells, in tier order, with their token price and standing gate.</summary>",
              "        public static readonly IReadOnlyDictionary<FactionType, string[]> StoreRiftKeys = new Dictionary<FactionType, string[]> { "
              + ", ".join(f"[FactionType.{f}] = new[] {{ " + ", ".join(cs_string(r) for _, r in rift_keys(f)) + " }" for f in FACTION_NAMES) + " };",
              "        public static readonly IReadOnlyDictionary<string, int> RiftKeyPrices = new Dictionary<string, int> { "
              + ", ".join(f"[{cs_string(r)}] = {rift_key_price(r)}" for t, r in sorted({k for f in FACTION_NAMES for k in rift_keys(f)})) + " };",
              "        public static readonly IReadOnlyDictionary<string, TradeStandingTier> RiftKeyGates = new Dictionary<string, TradeStandingTier> { "
              + ", ".join(f"[{cs_string(r)}] = TradeStandingTier.{RIFT_KEY_GATES[t]}" for t, r in sorted({k for f in FACTION_NAMES for k in rift_keys(f)})) + " };",
              "",
              "        /// <summary>The faction whose store sells each ship maker's schematics and component.</summary>",
              "        public static readonly IReadOnlyDictionary<FactionType, ShipManufacturer> FactionMakers = new Dictionary<FactionType, ShipManufacturer> { "
              + ", ".join(f"[FactionType.{f}] = ShipManufacturer.{m}" for f, m in FACTION_MAKER.items()) + " };",
              "",
              "        /// <summary>The makers without a faction whose component a black-market faction's store also sells (W11o).</summary>",
              "        public static readonly IReadOnlyDictionary<FactionType, ShipManufacturer[]> BlackMarketMakers = new Dictionary<FactionType, ShipManufacturer[]> { "
              + ", ".join(f"[FactionType.{f}] = new[] {{ " + ", ".join(f"ShipManufacturer.{m}" for m in ms) + " }"
                          for f, ms in BLACK_MARKET_MAKERS.items()) + " };",
              "",
              "        public static readonly IReadOnlyList<FactionAgentDetail> Agents = new List<FactionAgentDetail>",
              "        {"]
    for enum, name, faction, dock, donor, greeting in AGENTS:
        exchange, dock_name = DOCKS[dock]
        orbit_rows = []
        for t in mission_tiers(dock):
            orbit_rows.append(f"[{t}] = new[] {{ " + ", ".join(f"new AgentMissionOrbit({cs_string(o)}, {h})" for o, h in mission_orbits(dock, t)) + " }")
        lines.append(
            f"            new FactionAgentDetail {{ Type = FactionAgentType.{enum}, Name = {cs_string(name)}, Faction = FactionType.{faction}, "
            f"DockId = {cs_string(dock)}, DockName = {cs_string(dock_name)}, Exchange = TradeExchangeType.{exchange}, DockTier = {dock_tier(dock)}, "
            f"Greeting = {cs_string(greeting)}, "
            "MissionWeights = new Dictionary<AgentMissionType, int> { "
            + ", ".join(f"[AgentMissionType.{k}] = {w}" for k, w in zip(MISSION_TYPES, WEIGHTS[faction])) + " }, "
            "Orbits = new Dictionary<int, AgentMissionOrbit[]> { " + ", ".join(orbit_rows) + " } },")
    lines += ["        };", "    }", "}", ""]
    return "\n".join(lines)


def reference_json():
    """Mission payouts the C# rules must match (FactionAgentTests)."""
    cases = []
    for t in TIERS:
        for role in KILL_ROLES:
            for hops in (0, 2):
                c = credits("Kill", t, hops, role=role)
                cases.append(dict(Type="Kill", Tier=t, Hops=hops, Role=role, Site="", Credits=c, Tokens=tokens(c)))
        for hops in (1, 3):
            c = credits("SealedCourier", t, hops)
            cases.append(dict(Type="SealedCourier", Tier=t, Hops=hops, Role="Invalid", Site="", Credits=c, Tokens=tokens(c)))
        c = credits("MiningQuota", t)
        cases.append(dict(Type="MiningQuota", Tier=t, Hops=0, Role="Invalid", Site="", Credits=c, Tokens=tokens(c)))
        c = credits("ConvoyEscort", t, 1)
        cases.append(dict(Type="ConvoyEscort", Tier=t, Hops=1, Role="Invalid", Site="", Credits=c, Tokens=tokens(c)))
        for site in SCAN_DOWN_SITES:
            c = credits("ScanDown", t, 1, site=site)
            cases.append(dict(Type="ScanDown", Tier=t, Hops=1, Role="Invalid", Site=site, Credits=c, Tokens=tokens(c)))
        c = credits("DataHeist", t, 1)
        cases.append(dict(Type="DataHeist", Tier=t, Hops=1, Role="Invalid", Site="", Credits=c, Tokens=tokens(c),
                          IntelTokens=tokens(c, True)))
    rates = {str(t): dict(Combat=round(scanning.combat_rate(t)), Agents=round(tier_rate(t)),
                          Percent=round(100 * tier_rate(t) / scanning.combat_rate(t), 1)) for t in TIERS}
    stores = {f: [dict(Category=c, Resref=r, Quantity=q) for c, r, _, q, _ in store(f)] for f in FACTION_NAMES}
    keys = {r: dict(Tokens=rift_key_price(r), MinimumTier=RIFT_KEY_GATES[t]) for f in FACTION_NAMES for t, r in rift_keys(f)}
    return json.dumps(dict(Cases=cases, Rates=rates, Stores=stores, StorePrices=store_prices(), RiftKeys=keys), indent=2) + "\n"


WRITES = {
    "SWLOR.Game.Server/Service/FactionAgentService/FactionAgentTuning.cs": tuning_cs,
    "SWLOR.Game.Server.Tests/Fixtures/FactionAgents/FactionAgentReference.json": reference_json,
}


# ---------------------------------------------------------------------------------------------------------------
# Bible: the Faction Agents (Revamp) tab
# ---------------------------------------------------------------------------------------------------------------
def agents_tab(Sheet):
    s = Sheet("Faction Agents (Revamp)", [30, 16, 14, 12, 12, 12, 12, 60])
    s.title("Faction Agents (Revamp): agents, mission boards, payouts and faction token stores")
    s.note("Dev Status: Proposed (space revamp W15a draft). Generated by tools/SpaceRevampBalance/agents.py; plan section 29.")
    s.blank()
    s.title("Agents (two per faction at different docks, at most two per dock; the first at the lower-tier dock)")
    s.table(["Agent", "Faction", "Dock", "Dock tier", "Mission tiers", "", "", "Mission orbits (jumps)"],
            [[name, FACTION_NAMES[faction], DOCKS[dock][1], dock_tier(dock), ", ".join(str(t) for t in mission_tiers(dock)), "", "",
              "; ".join(f"T{t}: " + ", ".join(f"{ORBIT_NAMES.get(o, o)} ({h})" for o, h in mission_orbits(dock, t)) for t in mission_tiers(dock))]
             for _, name, faction, dock, _, _ in AGENTS])
    s.title("The board (the same for every pilot, replaced at 00:00 UTC; each offer can be taken once)")
    s.table(["Slot", "Tier", "Standing", "Standing reward", "", "", "", "Notes"], [
        ["Standard (x2)", "dock tier", "Neutral", STANDING_REWARD["Standard"], "", "", "", "a Hostile pilot gets no missions and no store"],
        ["Priority (x1)", "dock tier + 1 (at most 6)", f"Trusted ({PRIORITY_STANDING})", STANDING_REWARD["Priority"], "", "", "", "higher standing unlocks harder missions"],
        ["Intel lead", "dock tier + 1 (at most 6)", "Neutral", STANDING_REWARD["Intel"], "", "", "",
         f"a data heist; needs one agent intel (from data cans), spent on accepting; {INTEL_TOKEN_MULTIPLIER}x tokens"],
        ["Held at once", MAXIMUM_ACTIVE, "", "", "", "", "", "across every agent; missions pay the moment they are done, except the mining quota (handed in to the agent)"],
    ])
    s.title("Missions (runtime quests)")
    s.table(["Mission", "Size", "", "", "", "", "", "How it completes"], [
        ["Kill", ", ".join(f"{KILL_COUNT[r]} {'ships' if r == 'Invalid' else r.lower() + 's'}" for r in KILL_ROLES), "", "", "", "", "",
         "destroy them in the named orbit, at the mission tier; the killer's party in the area shares the credit"],
        ["Sealed courier", ", ".join(f"T{t} {COURIER_VOLUME[t]} vol" for t in TIERS), "", "", "", "", "",
         "W7b sealed freight loaded on accepting into a ship docked here; delivered through the destination's Freight tab before the freight deadline"],
        ["Mining quota", f"{ORE_QUANTITY[1]} ore", "", "", "", "", "", "the tier's ore (" + ", ".join(f"T{t} {ORE[t]}" for t in TIERS) + "), handed in to the agent"],
        ["Convoy escort", ", ".join(f"T{t} {CONVOY_RAIDERS[t]} raiders" for t in TIERS), "", "", "", "", "",
         f"a friendly W7b convoy appears {CONVOY_SPAWN_METERS} m away {CONVOY_SPAWN_DELAY_SECONDS} s after arriving; raiders hit it after {CONVOY_RAIDER_DELAY_SECONDS} s; it must reach its starlane"],
        ["Scan-down", ", ".join(SCAN_DOWN_SITES), "", "", "", "", "", "a signature placed for the pilot's party in the named orbit; open its site cache"],
        ["Data heist", "data site + guard squad", "", "", "", "", "", "slice the marked target data can (the Data Analyzer); a destroyed target is placed again"],
    ])
    s.title(f"Payout: mission time at {AGENT_PREMIUM:.0%} of the tier's space combat credits per hour, less what the activity pays")
    s.table(["Tier", "Kill (any)", "Courier (1 jump)", "Mining", "Convoy", "Scan-down (combat)", "Heist", "Agents / h vs combat"],
            [[t, credits("Kill", t, SIM_HOPS), credits("SealedCourier", t, 1), credits("MiningQuota", t), credits("ConvoyEscort", t, SIM_HOPS),
              credits("ScanDown", t, SIM_HOPS), credits("DataHeist", t, SIM_HOPS),
              f"{round(tier_rate(t))} vs {round(scanning.combat_rate(t))} ({100 * tier_rate(t) / scanning.combat_rate(t):.0f}%; "
              f"{100 * tier_rate(t, with_tokens=True) / scanning.combat_rate(t):.0f}% with tokens)"] for t in TIERS])
    s.table(["Model input", "Value", "", "", "", "", "", "Notes"], [
        ["Travel", f"{BASE_MINUTES:g} min + {2 * HOP_MINUTES:g} min a jump", "", "", "", "", "", "there and back to the agent's dock"],
        ["Kill time", ", ".join(f"{r if r != 'Invalid' else 'any'} {KILL_MINUTES[r]:g}" for r in KILL_ROLES) + " min", "", "", "", "", "",
         "hunting one role takes longer than any target"],
        ["Mining", "ore an hour (mining.py) " + ", ".join(f"T{t} {60 * ORE_PER_MINUTE[t]:.0f}" for t in TIERS), "", "", "", "", "", "the ore handed in is paid back at the 40% markdown"],
        ["Floor", f"{MINIMUM_SHARE:.0%}", "", "", "", "", "", "a mission's credits never drop below this share of its time's target"],
        ["Tokens", f"{TOKEN_SHARE:.0%} of the credits / {TOKEN_VALUE}", "", "", "", "", "", "at least 1; a token buys about 100 credits of store goods"],
    ])
    s.title("Faction token stores (at both of a faction's agents; tokens are per faction and cannot be traded)")
    prices = store_prices()
    s.table(["Category", "Tokens", "Standing", "", "", "", "", "Items"],
            [[c, prices[c], STORE_GATES[c], "", "", "", "",
              {"Ammo": f"Ion, Incendiary and Seeker Missiles, {AMMO_QUANTITY} each",
               "DesignSchematic": "the faction maker's Hull Designs (Wild Space only designs left out)",
               "ManufacturerComponent": "the faction maker's component (with its trait); the Smugglers and the Hutt "
                                        "Cartel also sell the Chiss Sensor Array (the black markets, W11o)",
               "RiftKey": "Rift Keys: tier 1 up to the faction's highest dock tier (at most 5), and tier 5.5",
               "ShipSchematic": "the faction maker's Advanced ships"}[c]] for c in STORE_CATEGORIES])
    s.table(["Rift Key", "Tokens", "Standing", "", "", "", "", "Sold by"],
            [[r, rift_key_price(r), RIFT_KEY_GATES[t], "", "", "", "",
              ", ".join(FACTION_NAMES[f] for f in FACTION_NAMES if (t, r) in rift_keys(f))]
             for t, r in sorted({k for f in FACTION_NAMES for k in rift_keys(f)})])
    s.table(["Faction", "Maker", "Designs", "Ship Schematics", "", "", "", "Manufacturer component"],
            [[FACTION_NAMES[f], FACTION_MAKER[f], sum(1 for x in store(f) if x[0] == "DesignSchematic"),
              sum(1 for x in store(f) if x[0] == "ShipSchematic"), "", "", "",
              ", ".join(x[2] for x in store(f) if x[0] == "ManufacturerComponent")]
             for f in FACTION_NAMES])
    return s


def main():
    if "--sim" in sys.argv:
        report()
        return
    problems = check_placement()
    if problems:
        print("Placement problems:", problems)
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
    print(f"{len(AGENTS)} agents, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
