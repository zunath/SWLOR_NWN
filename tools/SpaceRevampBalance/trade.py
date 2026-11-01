"""Space revamp trade core (plan section 13, W7a): commodities, exchanges, pricing, flooding and recovery.

usage: python trade.py            (writes the generated C# trade catalog and tuning)
       python trade.py --check    (exit 1 when a generated file is out of date)
       python trade.py --sim      (prints the round-trip simulation and the space combat comparison)

Every trade number lives here. bible_tabs.py calls trade_tab(Sheet) to write the Trade (Revamp) tab.

Sources:
- Base prices: the Cost of a comparable existing item blueprint (Module/uti), named per commodity in SOURCE. NPC stores
  buy items back at 40% of Cost (every Module/utm store but a few has MarkDown 40), so one commodity unit trades near
  the value of one comparable item. Bulk foods and water are a crate of 5 (x5), because one jar of honey (5) or one
  bottle of water (13) is not freight.
- Routes: the starlane graph in Module/git (tele_obj placeables with a DESTINATION landing, read the way
  ShipAccessRulesTests reads them) and the orbit tiers in SpaceSpawnDefinition (t1-t6 spawns per orbit).
- Space combat credits per hour: SpaceLootTableDefinition (gold and item drops) with the "SPACE_BASIC_Tn, 60, 3"
  LOOT_TABLE local every t*fighter / t*bomber creature carries (Module/utc), items sold at the 40% store markdown.
"""
import json
import math
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]

# ---------------------------------------------------------------------------------------------------------------
# Commodities
# ---------------------------------------------------------------------------------------------------------------
# id, enum name, display name, base price, volume per unit, banned by (lawful authorities), description.
COMMODITIES = [
    (1, "VelesHoney", "Veles Honey", 25, 1, [], "Wild honey from the Veles groves, packed in sealed jars."),
    (2, "ViscaranCrystals", "Viscaran Crystals", 40, 1, [], "Raw power crystals cut from Viscara's hills."),
    (3, "KinrathSilk", "Kinrath Silk", 125, 1, [], "Spun kinrath silk, prized by Mon Cala tailors."),
    (4, "PurifiedWater", "Purified Water", 65, 2, [], "Drums of filtered drinking water."),
    (5, "MedicalSupplies", "Medical Supplies", 55, 1, [], "Kolto packs, bacta gel and field dressings."),
    (6, "ReactorCores", "Reactor Cores", 175, 2, [], "Shielded Czerka reactor cores for ships and settlements."),
    (7, "CzerkaMachineParts", "Czerka Machine Parts", 63, 2, [], "Crated bearings, servos and pumps from Czerka foundries."),
    (8, "Grain", "Grain", 60, 2, [], "Sacks of Dantooine grain."),
    (9, "AraciaTimber", "Aracia Timber", 80, 2, [], "Seasoned Aracia logs from Hutlar."),
    (10, "Seafood", "Seafood", 40, 1, [], "Chilled catch from the Mon Cala seas."),
    (11, "HullSections", "Hull Sections", 75, 3, [], "Prefabricated hull plates from the Mon Cala yards."),
    (12, "NeonSalt", "Neon Salt", 50, 1, [], "Glowing mineral salt from the Smuggler's Moon flats."),
    (13, "ByyskHides", "Byysk Hides", 39, 1, [], "Tanned hides of Hutlar's byysk."),
    (14, "BanthaHides", "Bantha Hides", 50, 1, [], "Sun-cured bantha hides from Tatooine."),
    (15, "BeastHides", "Beast Hides", 69, 1, [], "Thick hides of Dathomir's great beasts."),
    (16, "JawaSalvage", "Jawa Salvage", 50, 2, [], "Sorted electronics salvage traded by Jawa clans."),
    (17, "Moonspice", "Moonspice", 250, 2, ["Republic", "Czerka", "Sith"], "Refined spice from the Smuggler's Moon labs."),
    (18, "EncryptedDataChips", "Encrypted Data Chips", 151, 1, ["Republic", "Czerka"], "Sliced data chips of unknown origin."),
    (19, "SithRelics", "Sith Relics", 782, 6, ["Republic"], "Artifacts taken from the tombs of Korriban."),
    (20, "WeaponsCrates", "Weapons Crates", 376, 3, ["Republic"], "Sealed crates of Czerka rifles and blasters."),
    (21, "AbdamarylliaExtract", "Abdamaryllia Extract", 250, 2, [], "Distilled extract of Korriban's abdamaryllia flower."),
    (22, "WitchHerbs", "Witch Herbs", 42, 1, [], "Dried herbs gathered by the witches of Dathomir."),
    (23, "BanthaMilk", "Bantha Milk", 65, 2, [], "Chilled churns of Dantooine bantha milk."),
    (24, "StationFuel", "Station Fuel", 250, 2, [], "Refined hyperfuel cells that power station Jump Beacons."),
]

# Comparable item blueprint and its Cost for each base price.
SOURCE = {
    "VelesHoney": "v_honey (Viscaran Honey) 5, crate of 5",
    "ViscaranCrystals": "power_crystal_go (Good Quality Power Crystal) 40",
    "KinrathSilk": "kinrath_silk (Kinrath Silk) 125",
    "PurifiedWater": "water_1 (Purified Water) 13, crate of 5",
    "MedicalSupplies": "m_enhancer3 (Medical Enhancer III) 55",
    "ReactorCores": "reactor_core (Salvaged Reactor Core) 175",
    "CzerkaMachineParts": "scrap_metal (Scrap Metal) 63",
    "Grain": "flour (Flour) 12, crate of 5",
    "AraciaTimber": "aracia_wood (Aracia Wood) 80",
    "Seafood": "blindfish and the other catch fish 40",
    "HullSections": "hull_plating (Hull Plating) 75",
    "NeonSalt": "no comparable item; between fine_wood 40 and bread_flour 63",
    "ByyskHides": "lth_good (Good Quality Leather) 39",
    "BanthaHides": "lth_premium (Premium Leather) 50",
    "BeastHides": "lth_flawless (Flawless Leather) 69",
    "JawaSalvage": "elec_imperfect (Imperfect Electronics) 50",
    "Moonspice": "allspice_1 (Allspice I) 250",
    "EncryptedDataChips": "night_datapada (Datapad A) 151",
    "SithRelics": "the tier 5 capstone relics (Anchor Relic and others) 782",
    "WeaponsCrates": "byysk_rifle and the other tier 3 Byysk weapons 376",
    "AbdamarylliaExtract": "abdamaryllia (Abdamaryllia) 250",
    "WitchHerbs": "herb_question (Questionable Herb) 42",
    "BanthaMilk": "milk_1 (Fresh Kath Hound Milk) 13, crate of 5",
    "StationFuel": "reactor_core (Salvaged Reactor Core) 175 + gas_tibanna (Tibanna Gas) 27, refined: x1.25, rounded to 250",
}

AUTHORITIES = ["None", "Republic", "Czerka", "Sith"]

# Holding goods (W14b, plan section 26): refined goods that only Planetary Holdings make, in a Processing Plant from the
# holding's own output and a good delivered from another planet (holdings.py holds every chain). No exchange sells them:
# exchanges buy them, as importers where listed below and as neutral markets elsewhere, priced by starlane hops from the
# docks whose holdings refine them (HOLDING_GOOD_SOURCES). The greedy trader of --sim cannot buy them, so they change
# no trade outcome. Ids start at 25, after Station Fuel (W13b, a CZ-220 export).
HOLDING_GOODS = [
    (25, "RationPacks", "Ration Packs", 150, 2, [], "Sealed crates of field rations packed by planetary holdings."),
    (26, "FocusingLenses", "Focusing Lenses", 200, 1, [], "Precision lenses ground from Viscaran crystals."),
    (27, "PrefabShelters", "Prefab Shelters", 270, 4, [], "Flat-packed shelters of timber and salvaged plate."),
    (28, "Glitterstim", "Glitterstim", 600, 1, ["Republic", "Czerka", "Sith"], "Refined glitterstim spice, cut with abdamaryllia."),
    (29, "SithHolocrons", "Sith Holocrons", 1300, 2, ["Republic"], "Restored holocrons from the Korriban digs."),
    (30, "AgedBanthaCheese", "Aged Bantha Cheese", 215, 2, [], "Wheels of salted, aged bantha milk cheese."),
]
COMMODITIES += HOLDING_GOODS
SOURCE.update({
    "RationPacks": "refined: Grain + Purified Water x1.20 (and the seafood and salt recipes, x1.18-1.22)",
    "FocusingLenses": "refined: 5 Viscaran Crystals + 2 Czerka Machine Parts = 2 lenses (x1.23), or 3 parts + 4 crystals (x1.15)",
    "PrefabShelters": "refined: 2 Aracia Timber + 1 Czerka Machine Parts (x1.21) or 3 Jawa Salvage + 1 Aracia Timber (x1.17)",
    "Glitterstim": "refined: Moonspice + Abdamaryllia Extract, x1.20",
    "SithHolocrons": "refined: Sith Relics + 2 Encrypted Data Chips, x1.20",
    "AgedBanthaCheese": "refined: 2 Bantha Milk + 1 Neon Salt, x1.19",
})

# Eshan goods (the Eshan merge, master #2344): Eshan's export, after the holding goods so no id moves. Eshan is a cold
# Echani farming world; its rice is a good no other dock makes (Dantooine already exports Grain). Only one: a second
# Eshan export (Dire Wolf Pelts, esh_wolf_pelt 175, was tried) puts the Hutt Grand Barge's every-good trade with Jump
# Beacons at 204-210% of T3 combat, over beacon_problems' 200% cap, with every import pair tried.
ESHAN_GOODS = [
    (31, "EshanRice", "Eshan Rice", 63, 2, [], "Sacks of white rice from the Eshan paddies."),
]
COMMODITIES += ESHAN_GOODS
SOURCE.update({
    "EshanRice": "esh_rice (Eshan White Rice) 63",
})
# The docks whose holdings refine each holding good (holdings.py checks them against its chains) and the docks that
# import it (pays well).
HOLDING_GOOD_SOURCES = {
    "RationPacks": ["MonCala", "Tatooine", "SmugglersMoon", "Dantooine", "Eshan"],
    "FocusingLenses": ["Viscara", "CZ220"],
    "PrefabShelters": ["Hutlar", "Tatooine"],
    "Glitterstim": ["SmugglersMoon"],
    "SithHolocrons": ["Korriban"],
    "AgedBanthaCheese": ["Dantooine"],
}
HOLDING_GOOD_CONSUMERS = {
    "RationPacks": ["Hutlar", "Korriban", "Dathomir"],
    "FocusingLenses": ["MonCala", "Hutlar", "Korriban"],
    "PrefabShelters": ["Dantooine", "Dathomir", "Korriban"],
    "Glitterstim": ["Hutlar", "Tatooine", "Dathomir"],
    "SithHolocrons": ["SmugglersMoon", "Hutlar"],
    "AgedBanthaCheese": ["Viscara", "MonCala", "Tatooine"],
}


def is_holding_good(name):
    return name in HOLDING_GOOD_SOURCES


def makes(ex, name):
    """An exchange makes a good it exports, or a holding good its holdings refine."""
    return name in ex[6] or ex[0] in HOLDING_GOOD_SOURCES.get(name, [])


def needs(ex, name):
    return name in ex[7] or ex[0] in HOLDING_GOOD_CONSUMERS.get(name, [])



# ---------------------------------------------------------------------------------------------------------------
# Exchanges (plan section 13 dock table). Exports are produced (cheap), imports consumed (pays well).
# ---------------------------------------------------------------------------------------------------------------
# enum name, display name, planet types, orbit area, authority, orbit tier, produces, consumes.
EXCHANGES = [
    ("Viscara", "Viscara", ["Viscara"], "viscaraorbit", "Republic", 1,
     ["VelesHoney", "ViscaranCrystals", "KinrathSilk"], ["PurifiedWater", "MedicalSupplies"]),
    ("CZ220", "CZ-220", ["CZ220"], "viscaraorbit", "Czerka", 1,
     ["ReactorCores", "CzerkaMachineParts", "WeaponsCrates", "StationFuel"], ["Grain", "AraciaTimber"]),
    ("MonCala", "Mon Cala", ["MonCala"], "moncalaorbit", "Republic", 2,
     ["Seafood", "PurifiedWater", "HullSections", "MedicalSupplies"], ["KinrathSilk", "AraciaTimber", "NeonSalt"]),
    ("Hutlar", "Hutlar", ["Hutlar"], "hutlar_orbit", "None", 3,
     ["AraciaTimber", "ByyskHides"], ["MedicalSupplies", "ReactorCores"]),
    ("Tatooine", "Tatooine", ["Tatooine"], "tatooineorbit", "None", 4,
     ["JawaSalvage", "BanthaHides"], ["PurifiedWater", "Grain", "Seafood"]),
    ("SmugglersMoon", "Smuggler's Moon", ["SmugglersMoon", "SmugglersMoonStation"], "narshadorbit", "None", 4,
     ["Moonspice", "NeonSalt", "EncryptedDataChips"], ["SithRelics", "VelesHoney", "WeaponsCrates"]),
    ("Korriban", "Korriban", ["Korriban"], "space_korriban", "Sith", 6,
     ["SithRelics", "AbdamarylliaExtract"], ["PurifiedWater", "EncryptedDataChips"]),
    ("Dathomir", "Dathomir", ["Dathomir"], "space_dathomir", "None", 5,
     ["WitchHerbs", "BeastHides"], ["CzerkaMachineParts", "MedicalSupplies"]),
    ("Dantooine", "Dantooine", ["Dantooine"], "dantooineorbit", "Republic", 5,
     ["Grain", "BanthaMilk"], ["ByyskHides", "BanthaHides", "BeastHides", "AraciaTimber"]),
    ("Eshan", "Eshan", ["Eshan"], "eshanorbit", "None", 5,
     ["EshanRice"], ["KinrathSilk", "MedicalSupplies"]),
]
# Station Fuel (W13b, plan sections 25-26) is the Jump Beacon's fuel: a refined good worth more than its inputs (Reactor
# Cores and Tibanna Gas), made by Czerka at CZ-220 beside its reactor cores, and imported nowhere: stations burn it.
# Holdings' Processing Plants (W14b) add a second source later.
# Departures from the plan's dock table: Mon Cala also exports Medical Supplies (kolto) and CZ-220 Weapons Crates,
# because the table lists both as imports with no exporter. Smuggler's Moon Landing and Station share one exchange
# (they share an orbit). Smuggler's Moon has no NPC spawns yet; tier 4 is its lanes' tier (Hutlar 3, Tatooine 4).
# Eshan (master #2344) is an independent port like Dathomir: no lawful authority, no customs, the tier 5 roster in orbit.
# It imports Kinrath Silk (Echani tailoring) and Medical Supplies (a cold world): trade.py --sim keeps every band with
# them (Exalted legal 121%, beacon checks clear); Reactor Cores in their place put Exalted legal trade at 127-134%, over
# the 125% cap.

# Starlane graph (Module/git, 2026-10-31: the Eshan merge adds Dantooine - Eshan): every lane has one back. The three W6c zones are dead ends with no exchange,
# so they change no route length. The Mid Rim Nebula (T3-4) hangs off Hutlar (T3) since W11e, not Mid Rim 1 (T5 space).
LANES = {
    "viscaraorbit": ["moncalaorbit", "space_midrim1", "space_viscbelt"],
    "moncalaorbit": ["dantooineorbit", "hutlar_orbit", "viscaraorbit"],
    "dantooineorbit": ["eshanorbit", "moncalaorbit"],
    "eshanorbit": ["dantooineorbit"],
    "hutlar_orbit": ["moncalaorbit", "narshadorbit", "tatooineorbit", "space_midrimneb"],
    "narshadorbit": ["hutlar_orbit", "tatooineorbit"],
    "tatooineorbit": ["hutlar_orbit", "narshadorbit", "space_dathomir"],
    "space_dathomir": ["tatooineorbit"],
    "space_midrim1": ["space_midrim2", "viscaraorbit"],
    "space_midrim2": ["space_korriban", "space_midrim1"],
    "space_korriban": ["space_midrim2", "space_korrdebris"],
    "space_viscbelt": ["viscaraorbit"],
    "space_midrimneb": ["hutlar_orbit"],
    "space_korrdebris": ["space_korriban"],
}

# Lanes a key item opens (the placeable's KEY_ITEM_ID local): master #2586's Mon Cala - Smuggler's Moon lane needs the
# Smuggler's Moon Starlane Pass. Prices, routes and incursion regions follow LANES, the network every pilot can fly.
GATED_LANES = {
    "moncalaorbit": ["narshadorbit"],
    "narshadorbit": ["moncalaorbit"],
}

# ---------------------------------------------------------------------------------------------------------------
# Pricing tuning
# ---------------------------------------------------------------------------------------------------------------
PRODUCER_FACTOR = 0.85          # an exporter sells cheap
NEUTRAL_BASE = 0.90             # a dock that neither makes nor needs a good, in the producer's own orbit
NEUTRAL_PER_HOP = 0.04          # ... plus this per starlane hop from the nearest producer
NEUTRAL_MAX = 1.10
CONSUMER_BASE = 1.05            # an importer pays well
CONSUMER_PER_HOP = 0.04         # longer routes pay more (plan section 13)
CONSUMER_MAX = 1.40
RISK_PREMIUM = {"Legal": 1.00, "Restricted": 1.10, "Contraband": 1.20}  # on every non-producer price
SPREAD = 0.06                   # the exchange sells at mid + 6% and buys at mid - 6%
DEPTH_CREDITS = 250000          # market depth: one depth of pressure (units) is this many credits at base price
FLOOD_MIN, FLOOD_MAX = 0.35, 2.5  # the pressure multiplier stays inside these
RECOVERY_HALF_LIFE_HOURS = 1.0  # "both recover over a few hours": 94% back after 4 hours
SOCIAL_PER_POINT = 0.001        # SOC improves buy and sell prices slightly: 0.1% per point over 10
SOCIAL_BASE = 10
SOCIAL_MAX = 0.015              # 1.5% from SOC 25
XP_PER_PROFIT_CREDIT = 1.0      # Piloting XP for a profitable sale (plan section 4, XP rules)
XP_MAX_PER_SALE = 1200          # two same-level kills (Skill delta 0 = 600)
SPILL_SECONDS = 300             # cargo spill containers last 5 minutes (plan section 13)
SPILL_MAX_CONTAINERS = 6        # one container per commodity, the largest lots first; the rest merge into the last

# Route timing for the simulation (assumed; measure in a client): a starlane hop is launch-or-arrive, fly to the
# lane and jump; a stop is docking, walking to the Dock Master, trading and boarding.
HOP_MINUTES = 3.0
STOP_MINUTES = 2.0
SAME_ORBIT_MINUTES = 1.0

# ---------------------------------------------------------------------------------------------------------------
# Trade risk (W7b): black markets, customs, pirate ambushes, convoys, freight contracts, the HoloNet board, standing
# ---------------------------------------------------------------------------------------------------------------
# Black markets (plan section 13) pay a premium for restricted and contraband goods they do not make themselves.
BLACK_MARKETS = ["Hutlar", "Tatooine", "SmugglersMoon"]
BLACK_MARKET_PREMIUM = 1.10

# Orbit tiers: the t1-t6 spawns of each orbit in SpaceSpawnDefinition. Smuggler's Moon has no spawns: its lanes' tier
# (Hutlar 3, Tatooine 4) gives 4. The Mid Rim systems have none either: the plan's Mid Rim Nebula is T3-4, so Mid Rim 1
# (the Viscara side) is 3 and Mid Rim 2 (the Korriban side) is 4. The W6c zones take their higher faction tier.
ORBIT_TIERS = {"viscaraorbit": 1, "moncalaorbit": 2, "hutlar_orbit": 3, "tatooineorbit": 4, "narshadorbit": 4,
               "space_dathomir": 5, "dantooineorbit": 5, "eshanorbit": 5, "space_korriban": 6, "space_midrim1": 3, "space_midrim2": 4,
               "space_viscbelt": 2, "space_midrimneb": 4, "space_korrdebris": 5}
ORBIT_NAMES = {"viscaraorbit": "Viscara Orbit", "moncalaorbit": "Mon Cala Orbit", "hutlar_orbit": "Hutlar Orbit",
               "tatooineorbit": "Tatooine Orbit", "narshadorbit": "Smuggler's Moon Orbit", "space_dathomir": "Dathomir Orbit",
               "dantooineorbit": "Dantooine Orbit", "eshanorbit": "Eshan Orbit", "space_korriban": "Korriban Orbit", "space_midrim1": "Mid Rim 1",
               "space_midrim2": "Mid Rim 2", "space_viscbelt": "Viscara Asteroid Belt", "space_midrimneb": "Mid Rim Nebula",
               "space_korrdebris": "Korriban Debris Field"}

# Customs patrols fly the lawful orbits (plan section 13): the authorities of the lawful docks there. Viscara's orbit
# holds a Republic dock (Viscara) and a Czerka one (CZ-220), so either patrol may stop a ship there.
CUSTOMS = {"viscaraorbit": ["Republic", "Czerka"], "moncalaorbit": ["Republic"], "dantooineorbit": ["Republic"],
           "space_korriban": ["Sith"]}
# Scan chance per orbit entry, on the base value of the goods that orbit's patrol bans: base + per 1,000 credits,
# capped. A Smuggling Compartment cuts it by its percentage.
SCAN_BASE = 0.03
SCAN_PER_THOUSAND = 0.002
SCAN_MAX = 0.60
FINE_RATE = 0.50                # the fine is this share of the banned goods' base value, and the goods are confiscated
SCAN_DELAY_SECONDS = 10         # after the ship enters the orbit
CUSTOMS_ANSWER_SECONDS = 30     # no answer counts as running
PATROL_SECONDS = 300            # a patrol leaves after this long
# Smuggling Compartment (Low, Piloting): the Ship Modules (Revamp) tab's step, scan chance -(15 + rank / 3)% (Mk I 15%,
# Mk X 30%); only the best counts.
COMPARTMENT_BASE = 15
COMPARTMENT_RANKS_PER_PERCENT = 3
# Cargo Expander (Low, Piloting; the gaps pass W11d): the Ship Modules (Revamp) tab's draft, +(20 + rank / 3)% of the
# hull's cargo (Mk I 20%, Mk X 35%) and -5% speed. Both stack, like Thrusters' speed: each fitted expander adds its share
# of the hull's own cargo, and the speed cost makes a stack a real choice.
CARGO_EXPANDER_BASE = 20
CARGO_EXPANDER_RANKS_PER_PERCENT = 3
CARGO_EXPANDER_SPEED_PERCENT = -5

# Pirate ambushes (plan section 13): a ship carrying more than the threshold (base value of its commodities plus its
# sealed freight's declared value) may be ambushed when it enters an orbit or jumps. Chance and squad size grow with
# the value and the orbit tier; every squad leads with an interceptor.
AMBUSH_THRESHOLD = 25000
AMBUSH_BASE = 0.01
AMBUSH_PER_TEN_THOUSAND = 0.004
AMBUSH_MAX = 0.04
AMBUSH_TIER_STEP = 0.10         # chance x (0.8 + 0.1 x tier): T1 0.9 to T6 1.4
AMBUSH_DELAY_SECONDS = 20
AMBUSH_SQUAD_MIN = 2
AMBUSH_SQUAD_MAX = 5
AMBUSH_SQUAD_VALUE_STEP = 25000  # one more raider per this much value over the threshold; one more from tier 4
AMBUSH_SECONDS = 300            # raiders who lose their prey leave after this long
SEALED_VALUE_PER_VOLUME = 40    # what pirates think a sealed load is worth, per unit of volume
# Simulation only (assumed; measure in a client): an ambush costs this many minutes, and this share of ambushes ends
# with the freighter destroyed and its cargo lost.
AMBUSH_SIM_MINUTES = 2.0
AMBUSH_SIM_LOSS = 0.10

# NPC trade convoys: a hauler and escorts on the Neutral faction. The hauler's cargo is about this many base credits
# per orbit tier (about 12 same-tier kills of loot), in up to two legal goods the orbit's docks export, or any legal
# good in an orbit without a dock. Raiding a convoy costs lawful standing.
CONVOY_VALUE_PER_TIER = 800
CONVOY_ESCORTS = {1: 1, 2: 1, 3: 2, 4: 2, 5: 2, 6: 3}
CONVOY_LIFETIME_SECONDS = 900
CONVOY_SPAWN_WEIGHT = 480       # the carriers' weight: about 0.6% of an orbit's spawns

# Freight contracts: sealed loads from one exchange to another for a fixed fee (plan section 13, about 75% of the
# space combat rate). Offers change every hour. A late delivery still pays the fee but forfeits the deposit; an
# abandoned or destroyed load forfeits it with no fee.
FREIGHT_OFFERS_PER_EXCHANGE = 4
FREIGHT_OFFER_VOLUMES = [40, 100, 200, 400]
FREIGHT_FEE_BASE = 0.70         # credits per unit of volume ...
FREIGHT_FEE_PER_HOP = 1.05      # ... plus this per starlane hop
FREIGHT_DEPOSIT_RATE = 0.50     # of the fee
FREIGHT_DEADLINE_BASE_MINUTES = 20
FREIGHT_DEADLINE_PER_HOP_MINUTES = 10
FREIGHT_MAX_ACTIVE = 3
FREIGHT_OFFER_MINUTES = 60
FREIGHT_SIM_VOLUME = 300        # simulation: the sealed volume an XS Freighter (480) carries per run

# Lawful standing (the ground faction standing, -5000 to 5000). The lawful authorities' factions lose standing; a
# delivered freight contract gains standing with the destination exchange's faction.
STANDING = {"CaughtSmuggling": -25, "RanFromCustoms": -100, "AttackedLawfulShip": -75, "RaidedConvoy": -50,
            "DeliveredFreight": 10}
AUTHORITY_FACTION = {"Republic": "RepublicMilitary", "Czerka": "Czerka", "Sith": "SithMilitary"}
EXCHANGE_FACTION = {"Viscara": "RepublicMilitary", "CZ220": "Czerka", "MonCala": "RepublicMilitary", "Hutlar": "HuttCartel",
                    "Tatooine": "HuttCartel", "SmugglersMoon": "Smuggler", "Korriban": "SithMilitary", "Dathomir": "Invalid",
                    "Dantooine": "RepublicMilitary", "Eshan": "Invalid"}
CONVOY_AUTHORITY_DEFAULT = "Czerka"  # a convoy outside a lawful orbit flies for Czerka (industrial shipping)

# ---------------------------------------------------------------------------------------------------------------
# Standing and city tariffs (W7c)
# ---------------------------------------------------------------------------------------------------------------
# What lawful standing unlocks (plan section 13, "Factions"). Standing is the ground faction standing (-5000 to 5000)
# with an exchange's faction (its StandingFaction) or a customs patrol's authority. Tiers by minimum standing: a delivered
# freight contract gives 10, so Trusted takes 25 deliveries, Honored 75 and Exalted 150.
STANDING_HOSTILE = -500         # at or below this the authority's customs patrol attacks on sight (five runs from customs)
# name, minimum standing, customs scan chance cut (percent, multiplies with a Smuggling Compartment), price bonus
# (added to Social's, both sides of every trade at that faction's exchanges).
STANDING_TIERS = [("Trusted", 250, 8, 0.0025), ("Honored", 750, 16, 0.005), ("Exalted", 1500, 25, 0.0075)]
# Freight offers by volume (FREIGHT_OFFER_VOLUMES) need this standing with the offering exchange's faction. Dathomir has
# no faction and offers every load to everyone.
FREIGHT_OFFER_MIN_STANDING = [0, 0, 250, 1500]
# The fee per volume of each offer volume, as a share of the usual rate. The 400 volume load is a bulk load that pays
# less per unit, which holds the best-case Exalted freight (the three largest loads in a 700+ hold) inside the legal
# band of 100-125% of T3 combat. A volume that is not an offer volume pays the full rate.
FREIGHT_OFFER_FEE_FACTOR = [1.0, 1.0, 1.0, 0.65]
HOSTILE_PATROL_COOLDOWN_SECONDS = 300  # an attack-on-sight patrol comes at most once per authority in this time

# Hutt Cartel enforcers (plan section 11, W11b): tier 4 Neutral ships that patrol the Smuggler's Moon lanes, Smuggler's
# Moon and the two orbits its starlanes reach (Hutlar and Tatooine, both Hutt Cartel exchanges). Like a customs patrol,
# they attack on sight a pilot whose standing with the Hutt Cartel is Hostile (the W7c tiers), and every enforcer in the
# orbit turns on a pilot who fires on one, at the customs patrols' standing cost (AttackedLawfulShip). Each ship spawns at
# the W5b role ships' weight (Electronic Warfare, Sniper: 1920 of an orbit's 75,648, about 2.5%).
HUTT_ENFORCER_TIER = 4
HUTT_ENFORCER_TAGS = [f"t{HUTT_ENFORCER_TIER}huttenfgun", f"t{HUTT_ENFORCER_TIER}huttenfint"]
HUTT_ENFORCER_ORBITS = ["narshadorbit", "hutlar_orbit", "tatooineorbit"]
HUTT_ENFORCER_SPAWN_WEIGHT = 1920

# Player-city exchanges: a city starport trades through its planet's NPC exchange (same goods, prices and flooding),
# from this starport upgrade level, and the city adds a tariff on every buy (added to the price) and sale (taken from it).
CITY_EXCHANGE_STARPORT_LEVEL = 2
TARIFF_MAX_PERCENT = 10

# The HoloNet board shows every exchange's prices as of its last snapshot (plan section 13: an hour stale).
HOLONET_REFRESH_MINUTES = 60


def legality(banned):
    if not banned:
        return "Legal"
    return "Contraband" if len(banned) >= 3 else "Restricted"


def hops(a, b):
    if a == b:
        return 0
    seen, frontier, d = {a}, [a], 0
    while frontier:
        d += 1
        nxt = []
        for n in frontier:
            for x in LANES[n]:
                if x == b:
                    return d
                if x not in seen:
                    seen.add(x)
                    nxt.append(x)
        frontier = nxt
    raise ValueError(f"no route {a} -> {b}")


def commodity(name):
    return next(c for c in COMMODITIES if c[1] == name)


def exchange(name):
    return next(e for e in EXCHANGES if e[0] == name)


def trades(ex, com):
    """An exchange trades a good it makes or needs, and any other good its authority does not ban."""
    return makes(ex, com[1]) or needs(ex, com[1]) or ex[4] not in com[5]


def markets():
    """Every (exchange, commodity) market: role, price factor and depth in units."""
    out = []
    for ex in EXCHANGES:
        for com in COMMODITIES:
            if not trades(ex, com):
                continue
            producers = [e for e in EXCHANGES if makes(e, com[1])]
            nearest = min(hops(ex[3], p[3]) for p in producers)
            risk = RISK_PREMIUM[legality(com[5])]
            if ex[0] in BLACK_MARKETS and com[5]:
                risk *= BLACK_MARKET_PREMIUM
            if com[1] in ex[6]:
                role, factor = "Producer", PRODUCER_FACTOR
            elif makes(ex, com[1]):
                role, factor = "Neutral", NEUTRAL_BASE  # a holding good at a dock whose holdings make it: never sold
            elif needs(ex, com[1]):
                role, factor = "Consumer", min(CONSUMER_MAX, CONSUMER_BASE + CONSUMER_PER_HOP * nearest) * risk
            else:
                role, factor = "Neutral", min(NEUTRAL_MAX, NEUTRAL_BASE + NEUTRAL_PER_HOP * nearest) * risk
            depth = max(20, round(DEPTH_CREDITS / com[3]))
            out.append(dict(exchange=ex[0], commodity=com[1], role=role, factor=round(factor, 3), depth=depth,
                            base=com[3], volume=com[4]))
    return out


def check_orbit_rule():
    """No dock exports a good that a dock in the same orbit imports (plan section 13)."""
    bad = []
    for a in EXCHANGES:
        for b in EXCHANGES:
            if a is not b and a[3] == b[3]:
                bad += [(a[0], b[0], c) for c in a[6] if c in b[7]]
    return bad


# ---------------------------------------------------------------------------------------------------------------
# Price math (mirrors Service/TradeService/TradePricing.cs)
# ---------------------------------------------------------------------------------------------------------------
def pressure_multiplier(pressure, depth):
    return min(FLOOD_MAX, max(FLOOD_MIN, math.exp(-pressure / depth)))


def unit_mid(base, factor, pressure, depth):
    return base * factor * pressure_multiplier(pressure, depth)


def social_bonus(social):
    return min(SOCIAL_MAX, max(0, social - SOCIAL_BASE) * SOCIAL_PER_POINT)


def unit_buy(base, factor, pressure, depth, social, bonus=0.0):
    return unit_mid(base, factor, pressure, depth) * (1 + SPREAD) * (1 - social_bonus(social) - bonus)


def unit_sell(base, factor, pressure, depth, social, bonus=0.0):
    return unit_mid(base, factor, pressure, depth) * (1 - SPREAD) * (1 + social_bonus(social) + bonus)


def buy_cost(base, factor, pressure, depth, units, social=10, bonus=0.0):
    """Credits to buy units. Each unit is priced at the pressure halfway through it; buying lowers pressure."""
    return math.ceil(sum(unit_buy(base, factor, pressure - i - 0.5, depth, social, bonus) for i in range(units)))


def sell_revenue(base, factor, pressure, depth, units, social=10, bonus=0.0):
    return math.floor(sum(unit_sell(base, factor, pressure + i + 0.5, depth, social, bonus) for i in range(units)))


# ---------------------------------------------------------------------------------------------------------------
# Standing and tariffs (mirrors Service/TradeService/TradeStandingRules.cs and TradeTariffRules.cs)
# ---------------------------------------------------------------------------------------------------------------
def standing_tier(standing):
    """-1 Hostile, 0 Neutral, 1 Trusted, 2 Honored, 3 Exalted."""
    if standing <= STANDING_HOSTILE:
        return -1
    tier = 0
    for index, (_, minimum, _, _) in enumerate(STANDING_TIERS, 1):
        if standing >= minimum:
            tier = index
    return tier


def standing_scan_cut(tier):
    return 0 if tier <= 0 else STANDING_TIERS[tier - 1][2]


def standing_price_bonus(tier):
    return 0.0 if tier <= 0 else STANDING_TIERS[tier - 1][3]


def exchange_price_bonus(exchange_name, tier):
    """The price bonus at an exchange: its faction's standing tier (Dathomir has no faction)."""
    return 0.0 if EXCHANGE_FACTION[exchange_name] == "Invalid" else standing_price_bonus(tier)


def tariff(credits, percent):
    """The city's tariff on a buy or sale of this many credits: rounded up, never more than the credits."""
    percent = max(0, min(TARIFF_MAX_PERCENT, percent))
    return min(credits, math.ceil(credits * percent / 100))


def recover(pressure, hours):
    return pressure * 0.5 ** (hours / RECOVERY_HALF_LIFE_HOURS)


def trade_xp(profit):
    return 0 if profit <= 0 else min(XP_MAX_PER_SALE, int(profit * XP_PER_PROFIT_CREDIT))


# ---------------------------------------------------------------------------------------------------------------
# Space combat credits per hour, from today's loot tables
# ---------------------------------------------------------------------------------------------------------------
def item_cost(resref):
    path = REPO / "Module" / "uti" / f"{resref}.uti.json"
    if not path.exists():
        return 0
    return json.loads(path.read_text(encoding="latin-1")).get("Cost", {}).get("value", 0)


def loot_tables():
    text = (REPO / "SWLOR.Game.Server/Feature/LootTableDefinition/SpaceLootTableDefinition.cs").read_text(encoding="utf-8")
    tables = {}
    for block in re.split(r'_builder\.Create\(', text)[1:]:
        name = re.match(r'"([^"]+)"', block)[1]
        rows = []
        for m in re.finditer(r'\.Add(Item|Gold)\(([^)]*)\)', block.split(";")[0]):
            args = [a.strip().strip('"') for a in m[2].split(",")]
            if m[1] == "Gold":
                rows.append(("gold", int(args[1]), int(args[0]), False))
            else:
                rows.append((args[0], int(args[1]), int(args[2]) if len(args) > 2 else 1, len(args) > 3 and args[3] == "true"))
        tables[name] = rows
    return tables


def expected_value(rows):
    """One pull: weighted item, quantity 1..max; gold at face value, items at the 40% store markdown."""
    weight = sum(r[1] for r in rows if not r[3])
    value = 0.0
    for resref, w, qty, rare in rows:
        if rare:
            continue
        unit = 1 if resref == "gold" else item_cost(resref) * 0.40
        value += w / weight * (qty + 1) / 2 * unit
    return value


KILLS_PER_HOUR = 60  # a normal fight (plan section 19 targets, 20-50 s) plus finding the next one; assumed


def combat_credits_per_hour(tier):
    basic = loot_tables()[f"SPACE_BASIC_T{tier}"]
    per_kill = 3 * 0.60 * expected_value(basic)
    return per_kill, per_kill * KILLS_PER_HOUR


# ---------------------------------------------------------------------------------------------------------------
# Simulation: one greedy trader on fresh markets
# ---------------------------------------------------------------------------------------------------------------
def leg_minutes(a, b):
    h = hops(exchange(a)[3], exchange(b)[3])
    return STOP_MINUTES + (SAME_ORBIT_MINUTES if h == 0 else h * HOP_MINUTES)


def market(ex, com):
    return MARKET_INDEX.get((ex, com))


# Jump Beacons (W13b, plan section 25; review 2026-10-30): a player station's beacon jumps a ship docked at it, cargo and
# sealed freight aboard, to another station's beacon, whatever the starlane hops between them. Every orbit with an
# exchange has two station anchor sites (stations.ORBIT_ANCHORS), so the best case modelled is a beacon in every one of
# those orbits, open to anyone and able to take the hull. A beacon leg between exchanges in different orbits: trade at
# the dock, fly to the station in the same orbit, jump, and fly from the destination station to its dock:
# BEACON_LEG_MINUTES. It skips the orbits between, but launching from the dock and from the destination station still
# enters both end orbits, which roll customs and ambushes as usual (Trade.CheckOrbitRisks on entering an orbit). It
# burns Station Fuel (stations.jump_fuel, which scales with the cargo aboard), bought at CZ-220's exchange.
BEACON_LEG_MINUTES = STOP_MINUTES + 2 * SAME_ORBIT_MINUTES
# The trade sim's hulls (HULLS): size and hull tag for the beacon's fuel.
HULL_BEACON = {"Fighter": ("Small", ""), "Y8 Miner": ("Medium", ""), "Barracuda": ("Medium", ""),
               "XS Freighter": ("Medium", ""), "Heavy Lifter": ("Large", ""), "Republic Quartermaster": ("Large", ""),
               "Hutt Grand Barge": ("Large", "sd_huttgrandbarg")}


def _stations():
    import stations  # stations.py imports this module, so it is loaded on first use
    return stations


def station_fuel_price(social=10):
    """What a unit of Station Fuel costs at its cheapest exchange (CZ-220, the exporter), fresh market."""
    m = market("CZ220", "StationFuel")
    return unit_buy(m["base"], m["factor"], -0.5, m["depth"], social)


def beacon_orbits():
    return {a[1] for a in _stations().ORBIT_ANCHORS}


def basket_volume(basket):
    return sum(n * commodity(c)[4] for c, n in basket.items())


def beacon_hops(here, there):
    return hops(exchange(here)[3], exchange(there)[3])


def beacon_fuel_cost(size, hull_tag, here, there, basket, sealed_volume=0):
    """The credits of Station Fuel a jump with this cargo burns. Sealed freight counts as pirates value it."""
    value = sum(n * commodity(c)[3] for c, n in basket.items()) + sealed_volume * SEALED_VALUE_PER_VOLUME
    return _stations().jump_fuel(size, hull_tag, value, beacon_hops(here, there)) * station_fuel_price()


def beacon_unit_cost(size, hull_tag, here, there):
    """The fuel one more unit of each good adds to the jump, in credits (for choosing what to carry)."""
    st = _stations()
    share = (100 - st.JUMP_FUEL_DISCOUNT.get(hull_tag, 0)) / 100
    h = beacon_hops(here, there)
    price = station_fuel_price()
    return lambda c: price * share * st.jump_cargo_fuel(commodity(c)[3], h)


def beacon_leg_orbits(here, there):
    """The orbits a beacon leg enters: launching from the dock, then from the destination station."""
    return [exchange(here)[3], exchange(there)[3]]


def beacon_leg(here, there, size, hull_tag, basket, sealed_volume=0):
    """(minutes, fuel credits) of the leg by Jump Beacon, or None when both docks share an orbit or one has no anchor."""
    a, b = exchange(here)[3], exchange(there)[3]
    orbits = beacon_orbits()
    if a == b or a not in orbits or b not in orbits:
        return None
    return BEACON_LEG_MINUTES, beacon_fuel_cost(size, hull_tag, here, there, basket, sealed_volume)


def plan_leg(state, here, there, hold, social, goods, tier=0, unit_cost=None):
    """The basket that earns the most from here to there: add the best unit per volume until nothing pays. A beacon leg
    passes the fuel each unit adds to the jump (unit_cost, credits by good), which the unit must also pay for."""
    basket, used, profit = {}, 0, 0.0
    buy_bonus, sell_bonus = exchange_price_bonus(here, tier), exchange_price_bonus(there, tier)
    while True:
        best, best_gain = None, 0.0
        for c in goods:
            hm, tm = market(here, c), market(there, c)
            if not hm or not tm or used + hm["volume"] > hold:
                continue
            n = basket.get(c, 0)
            buy = unit_buy(hm["base"], hm["factor"], state[(here, c)] - n - 0.5, hm["depth"], social, buy_bonus)
            sell = unit_sell(tm["base"], tm["factor"], state[(there, c)] + n + 0.5, tm["depth"], social, sell_bonus)
            gain = (sell - buy - (unit_cost(c) if unit_cost else 0.0)) / hm["volume"]
            if gain > best_gain:
                best, best_gain = c, gain
        if best is None:
            return basket, profit
        basket[best] = basket.get(best, 0) + 1
        used += market(here, best)["volume"]
        profit += best_gain * market(here, best)["volume"] + (unit_cost(best) if unit_cost else 0.0)


def simulate(hold, start="Viscara", hours=4, social=10, log=None, lawful=False, tier=0, beacon=None):
    """Credits earned in each hour by a trader who always flies the leg that pays most per minute. A lawful trader
    carries only legal goods. With beacon (hull size, hull tag) the trader may also take any leg by Jump Beacon
    (beacon_leg), paying its fuel."""
    goods = [c[1] for c in COMMODITIES if (not lawful or not c[5]) and not is_holding_good(c[1])]
    state = {(m["exchange"], m["commodity"]): 0.0 for m in MARKETS}
    here, clock = start, 0.0
    per_hour = [0.0] * hours
    while clock < hours * 60:
        options = []
        for ex in EXCHANGES:
            if ex[0] != here:
                basket, profit = plan_leg(state, here, ex[0], hold, social, goods, tier)
                options.append((profit / leg_minutes(here, ex[0]), ex[0], basket, leg_minutes(here, ex[0]), 0.0))
                if beacon and beacon_leg(here, ex[0], beacon[0], beacon[1], {}):
                    basket, profit = plan_leg(state, here, ex[0], hold, social, goods, tier,
                                              beacon_unit_cost(beacon[0], beacon[1], here, ex[0]))
                    minutes, fuel = beacon_leg(here, ex[0], beacon[0], beacon[1], basket)
                    options.append(((profit - fuel) / minutes, ex[0], basket, minutes, fuel))
        _, there, basket, minutes, fuel = max(options, key=lambda o: (o[0], o[1]))
        realized = -fuel
        for c, n in basket.items():
            hm = market(here, c)
            realized -= buy_cost(hm["base"], hm["factor"], state[(here, c)], hm["depth"], n, social, exchange_price_bonus(here, tier))
            state[(here, c)] -= n
        for k in state:
            state[k] = recover(state[k], minutes / 60)
        clock += minutes
        for c, n in basket.items():
            tm = market(there, c)
            realized += sell_revenue(tm["base"], tm["factor"], state[(there, c)], tm["depth"], n, social, exchange_price_bonus(there, tier))
            state[(there, c)] += n
        per_hour[min(hours - 1, int((clock - 1e-9) // 60))] += realized
        if log is not None:
            log.append((round(clock), here, there, realized, dict(basket)))
        here = there
    return per_hour


def same_exchange_round_trip(m, units, social=26):
    """Buying and selling straight back at one exchange always loses (the arbitrage bound)."""
    cost = buy_cost(m["base"], m["factor"], 0.0, m["depth"], units, social)
    back = sell_revenue(m["base"], m["factor"], -units, m["depth"], units, social)
    return back - cost


# ---------------------------------------------------------------------------------------------------------------
# Trade risk math (mirrors Service/TradeService/CustomsRules.cs, PirateAmbushRules.cs and FreightContractRules.cs)
# ---------------------------------------------------------------------------------------------------------------
def compartment_reduction(rank):
    return COMPARTMENT_BASE + rank // COMPARTMENT_RANKS_PER_PERCENT


def expander_percent(rank):
    """The cargo bonus (percent of the hull's cargo) of a Cargo Expander of this rank."""
    return CARGO_EXPANDER_BASE + rank // CARGO_EXPANDER_RANKS_PER_PERCENT


def expanded_hold(hold, ranks):
    """The cargo bay of a hull with these Cargo Expanders fitted (mirrors ShipCargoRules.GetCapacity)."""
    return hold + sum(hold * expander_percent(r) // 100 for r in ranks)


def banned_value(basket, authority):
    return sum(units * commodity(c)[3] for c, units in basket.items() if authority in commodity(c)[5])


def scan_chance(value, reduction_percent=0.0, standing_percent=0.0):
    """The scan chance after a Smuggling Compartment's cut and the patrol authority's standing cut (they multiply)."""
    if value <= 0:
        return 0.0
    chance = min(SCAN_MAX, SCAN_BASE + SCAN_PER_THOUSAND * value / 1000)
    return chance * (1 - min(100.0, reduction_percent) / 100) * (1 - min(100.0, standing_percent) / 100)


def fine(value):
    return math.ceil(value * FINE_RATE)


def ambush_chance(value, tier):
    if value <= AMBUSH_THRESHOLD:
        return 0.0
    chance = min(AMBUSH_MAX, AMBUSH_BASE + AMBUSH_PER_TEN_THOUSAND * (value - AMBUSH_THRESHOLD) / 10000)
    return chance * (0.8 + AMBUSH_TIER_STEP * tier)


def ambush_squad_size(value, tier):
    size = AMBUSH_SQUAD_MIN + int(max(0, value - AMBUSH_THRESHOLD) // AMBUSH_SQUAD_VALUE_STEP) + (1 if tier >= 4 else 0)
    return max(AMBUSH_SQUAD_MIN, min(AMBUSH_SQUAD_MAX, size))


def freight_fee_factor(volume):
    return FREIGHT_OFFER_FEE_FACTOR[FREIGHT_OFFER_VOLUMES.index(volume)] if volume in FREIGHT_OFFER_VOLUMES else 1.0


def freight_fee(volume, hop_count):
    return round(volume * (FREIGHT_FEE_BASE + FREIGHT_FEE_PER_HOP * max(1, hop_count)) * freight_fee_factor(volume))


def freight_deposit(fee):
    return math.ceil(fee * FREIGHT_DEPOSIT_RATE)


def freight_deadline_minutes(hop_count):
    return FREIGHT_DEADLINE_BASE_MINUTES + FREIGHT_DEADLINE_PER_HOP_MINUTES * max(1, hop_count)


def route(a, b):
    """The orbits a ship enters from launching at a to arriving at b: the launch orbit, then one per jump."""
    if a == b:
        return [a]
    prev, frontier = {a: None}, [a]
    while frontier and b not in prev:
        nxt = []
        for n in frontier:
            for x in LANES[n]:
                if x not in prev:
                    prev[x] = n
                    nxt.append(x)
        frontier = nxt
    path, node = [], b
    while node is not None:
        path.append(node)
        node = prev[node]
    return path[::-1]


def leg_risk(basket, revenue, cost, here, there, reduction=0.0, standing_cut=0.0, orbits=None):
    """Expected credits and extra minutes of one leg after customs and pirates, in expectation: each orbit entered may
    bring an ambush (some cargo lost) and a customs scan (the patrol's banned goods confiscated and fined). The orbits
    entered are the starlane route's unless given (a beacon leg's)."""
    left = {c: float(n) for c, n in basket.items()}
    fines, minutes = 0.0, 0.0
    for orbit in orbits or route(exchange(here)[3], exchange(there)[3]):
        value = sum(n * commodity(c)[3] for c, n in left.items())
        pa = ambush_chance(value, ORBIT_TIERS[orbit])
        minutes += pa * AMBUSH_SIM_MINUTES
        for c in left:
            left[c] *= 1 - pa * AMBUSH_SIM_LOSS
        authorities = CUSTOMS.get(orbit, [])
        for authority in authorities:
            caught = scan_chance(banned_value(left, authority), reduction, standing_cut) / len(authorities)
            fines += caught * fine(banned_value(left, authority))
            for c in left:
                if authority in commodity(c)[5]:
                    left[c] *= 1 - caught
    kept = sum(revenue[c] * left[c] / basket[c] for c in basket if basket[c])
    return kept - cost - fines, minutes


def trim_banned(basket, cap):
    """The basket with its banned goods scaled down to at most cap credits of base value."""
    value = sum(n * commodity(c)[3] for c, n in basket.items() if commodity(c)[5])
    if value <= cap:
        return dict(basket)
    scale = cap / value
    return {c: (int(n * scale) if commodity(c)[5] else n) for c, n in basket.items()}


BANNED_CAPS = [0, 2000, 5000, 10000, 20000, 40000, float("inf")]


def priced(state, here, there, basket, social, tier=0):
    cost, revenue = 0, {}
    buy_bonus, sell_bonus = exchange_price_bonus(here, tier), exchange_price_bonus(there, tier)
    for c, n in basket.items():
        if n <= 0:
            continue
        hm, tm = market(here, c), market(there, c)
        cost += buy_cost(hm["base"], hm["factor"], state[(here, c)], hm["depth"], n, social, buy_bonus)
        revenue[c] = sell_revenue(tm["base"], tm["factor"], state[(there, c)], tm["depth"], n, social, sell_bonus)
    return cost, revenue


def simulate_risk(hold, start="Viscara", hours=4, social=10, lawful=False, reduction=0.0, log=None, tier=0, beacon=None):
    """simulate() with customs and pirates in expectation. The trader weighs each leg's expected profit after risk and
    may carry fewer banned goods (BANNED_CAPS) than fit. With beacon (hull size, hull tag) a leg may go by Jump Beacon,
    paying its fuel, with the risks of the two orbits it launches into (beacon_leg_orbits)."""
    goods = [c[1] for c in COMMODITIES if (not lawful or not c[5]) and not is_holding_good(c[1])]
    state = {(m["exchange"], m["commodity"]): 0.0 for m in MARKETS}
    here, clock = start, 0.0
    per_hour = [0.0] * hours
    while clock < hours * 60:
        best = None
        for ex in EXCHANGES:
            if ex[0] == here:
                continue
            full, _ = plan_leg(state, here, ex[0], hold, social, goods, tier)
            tried = []
            for cap in ([0] if lawful else BANNED_CAPS):
                basket = {c: n for c, n in trim_banned(full, cap).items() if n > 0}
                if basket in tried:
                    continue
                tried.append(basket)
                cost, revenue = priced(state, here, ex[0], basket, social, tier)
                expected, extra = leg_risk(basket, revenue, cost, here, ex[0], reduction, standing_scan_cut(tier))
                minutes = leg_minutes(here, ex[0]) + extra
                if best is None or expected / minutes > best[0]:
                    best = (expected / minutes, ex[0], basket, expected, minutes)
            if not beacon or not beacon_leg(here, ex[0], beacon[0], beacon[1], {}):
                continue
            full, _ = plan_leg(state, here, ex[0], hold, social, goods, tier, beacon_unit_cost(beacon[0], beacon[1], here, ex[0]))
            tried = []
            for cap in ([0] if lawful else BANNED_CAPS):
                basket = {c: n for c, n in trim_banned(full, cap).items() if n > 0}
                if basket in tried:
                    continue
                tried.append(basket)
                cost, revenue = priced(state, here, ex[0], basket, social, tier)
                minutes, fuel = beacon_leg(here, ex[0], beacon[0], beacon[1], basket)
                expected, extra = leg_risk(basket, revenue, cost + fuel, here, ex[0], reduction, standing_scan_cut(tier),
                                           beacon_leg_orbits(here, ex[0]))
                minutes += extra
                if best is None or expected / minutes > best[0]:
                    best = (expected / minutes, ex[0], basket, expected, minutes)
        _, there, basket, expected, minutes = best
        for c, n in basket.items():
            state[(here, c)] -= n
        for k in state:
            state[k] = recover(state[k], minutes / 60)
        for c, n in basket.items():
            state[(there, c)] += n
        clock += minutes
        per_hour[min(hours - 1, int((clock - 1e-9) // 60))] += expected
        if log is not None:
            log.append((round(clock), here, there, round(expected), dict(basket)))
        here = there
    return per_hour


def freight_rate(volume=FREIGHT_SIM_VOLUME):
    """Credits per hour hauling sealed freight on each route length, without trading on the side."""
    out = {}
    for h in range(1, 6):
        minutes = STOP_MINUTES + h * HOP_MINUTES
        out[h] = freight_fee(volume, h) / minutes * 60
    return out


def freight_rate_beacon(volume, size, hull_tag=""):
    """freight_rate() for a pilot who may take each job by Jump Beacon (one jump from the pickup's orbit to the
    delivery's, the sealed load aboard), whichever pays more per minute."""
    out = {}
    for h in range(1, 6):
        fee = freight_fee(volume, h)
        here, there = next((a[0], b[0]) for a in EXCHANGES for b in EXCHANGES if beacon_hops(a[0], b[0]) == h)
        fuel = beacon_fuel_cost(size, hull_tag, here, there, {}, volume)
        out[h] = max(fee / (STOP_MINUTES + h * HOP_MINUTES), (fee - fuel) / BEACON_LEG_MINUTES) * 60
    return out


def beacon_outcome():
    """Beacon-assisted hauling against T3 combat, for each hull that carries FREIGHT_SIM_VOLUME: freight contracts with
    that volume aboard (average over 1-5 hop jobs and the best, 5-hop, job, with the fuel that job's jump burns), the
    greedy trader's 4-hour average with beacons (legal goods, and every good after risk)."""
    t3 = combat_credits_per_hour(3)[1]
    rows = []
    for name, hold in HULLS:
        if hold < FREIGHT_SIM_VOLUME:
            continue
        size, tag = HULL_BEACON[name]
        freight = freight_rate_beacon(FREIGHT_SIM_VOLUME, size, tag)
        legal = sum(simulate(hold, lawful=True, beacon=(size, tag))) / 4
        every = sum(simulate_risk(hold, beacon=(size, tag))) / 4
        rows.append(dict(Hull=name, Size=size,
                         Fuel=_stations().jump_fuel(size, tag, FREIGHT_SIM_VOLUME * SEALED_VALUE_PER_VOLUME, 5),
                         FreightPerHour=round(sum(freight.values()) / 5),
                         FreightPercent=round(100 * sum(freight.values()) / 5 / t3),
                         Best5HopPerHour=round(freight[5]), Best5HopPercent=round(100 * freight[5] / t3),
                         LegalPerHour=round(legal), LegalPercent=round(100 * legal / t3),
                         EveryPerHour=round(every), EveryPercent=round(100 * every / t3)))
    return dict(T3Combat=round(t3), FuelPrice=round(station_fuel_price()), Rows=rows)


BEACON_FREIGHT_MAX_PERCENT = 125   # freight by beacon stays inside the open-trading band (starlane freight: about 75%)
BEACON_LEGAL_MAX_PERCENT = 125
BEACON_EVERY_MAX_PERCENT = 200


def beacon_problems(outcome=None):
    """Beacon-assisted hauling outside the plan's trade bands (section 13)."""
    out = []
    for row in (outcome or beacon_outcome())["Rows"]:
        if row["Best5HopPercent"] > BEACON_FREIGHT_MAX_PERCENT:
            out.append(f"{row['Hull']}: a 5-hop freight job by beacon pays {row['Best5HopPercent']}% of T3 combat")
        if row["LegalPercent"] > BEACON_LEGAL_MAX_PERCENT:
            out.append(f"{row['Hull']}: legal trade with beacons pays {row['LegalPercent']}%")
        if row["EveryPercent"] > BEACON_EVERY_MAX_PERCENT:
            out.append(f"{row['Hull']}: every good with beacons pays {row['EveryPercent']}%")
    return out


def freight_volume_open(tier, hold):
    """The paying volume a pilot of this standing tier can have aboard: the three largest offers they may take at one
    exchange (one of each volume is offered), as many as fit the hold, each counted at its fee factor."""
    standing = 0 if tier <= 0 else STANDING_TIERS[tier - 1][1]
    allowed = sorted((v for v, need in zip(FREIGHT_OFFER_VOLUMES, FREIGHT_OFFER_MIN_STANDING) if need <= standing), reverse=True)
    total, used = 0.0, 0
    for volume in allowed[:FREIGHT_MAX_ACTIVE]:
        if used + volume <= hold:
            used += volume
            total += volume * freight_fee_factor(volume)
    return total


def freight_outcome():
    """Freight contract earnings per hour by standing tier, for each freighter (hold 480+), against T3 combat."""
    t3 = combat_credits_per_hour(3)[1]
    holds = [h for _, h in HULLS if h >= 480]
    out = {}
    for tier in range(0, len(STANDING_TIERS) + 1):
        rates = [sum(freight_rate(freight_volume_open(tier, hold)).values()) / 5 for hold in holds]
        average = sum(rates) / len(rates)
        out[tier] = dict(PerHour=round(average), Percent=round(100 * average / t3))
    return out


RISK_REFERENCE_CASES = [
    # (authority, cargo, compartment rank or None, orbit tier)
    ("Republic", {"EncryptedDataChips": 100}, None, 1),
    ("Republic", {"EncryptedDataChips": 300, "SithRelics": 20}, None, 2),
    ("Czerka", {"EncryptedDataChips": 300, "SithRelics": 20}, 20, 1),
    ("Sith", {"Moonspice": 60, "Grain": 100}, 45, 6),
    ("Republic", {"Grain": 400}, 0, 5),
]


def risk_reference():
    """Reference numbers the C# rules must reproduce (TradeRiskTests reads the generated fixture)."""
    cases = []
    for authority, cargo, rank, tier in RISK_REFERENCE_CASES:
        reduction = 0.0 if rank is None else compartment_reduction(rank)
        value = banned_value(cargo, authority)
        total = sum(n * commodity(c)[3] for c, n in cargo.items())
        cases.append(dict(Authority=authority, Cargo=cargo, CompartmentRank=-1 if rank is None else rank, Tier=tier,
                          BannedValue=value, ScanChance=round(scan_chance(value, reduction), 6), Fine=fine(value),
                          CargoValue=total, AmbushChance=round(ambush_chance(total, tier), 6),
                          SquadSize=ambush_squad_size(total, tier)))
    return cases


def risk_outcome():
    """The contraband and legal earning rates after risk against T3 space combat (plan section 13 targets)."""
    t3 = combat_credits_per_hour(3)[1]
    freighters = [h for h in HULLS if h[1] >= 480]

    def average(lawful, reduction):
        rates = [sum(simulate_risk(hold, lawful=lawful, reduction=reduction)) / 4 for _, hold in freighters]
        return sum(rates) / len(rates)

    contraband = average(False, 0.0)
    compartment = average(False, compartment_reduction(20))
    legal = average(True, 0.0)
    freight = freight_rate()
    return dict(T3Combat=round(t3), ContrabandPerHour=round(contraband), ContrabandPercent=round(100 * contraband / t3),
                CompartmentPerHour=round(compartment), CompartmentPercent=round(100 * compartment / t3),
                LegalPerHour=round(legal), LegalPercent=round(100 * legal / t3),
                FreightPerHour=round(sum(freight.values()) / len(freight)),
                FreightPercent=round(100 * sum(freight.values()) / len(freight) / t3))


def standing_outcome():
    """Legal and contraband earnings (freighters, 4-hour average, after risk) and freight contracts for a pilot with
    this tier of standing at every faction (the best case), against T3 combat. The price bonus applies at every exchange
    with a faction and the scan cut at every patrol."""
    t3 = combat_credits_per_hour(3)[1]
    freighters = [h for h in HULLS if h[1] >= 480]
    freight = freight_outcome()
    rows = []
    for tier in range(1, len(STANDING_TIERS) + 1):
        def average(lawful):
            rates = [sum(simulate_risk(hold, lawful=lawful, tier=tier)) / 4 for _, hold in freighters]
            return sum(rates) / len(rates)

        legal, contraband = average(True), average(False)
        rows.append(dict(Tier=STANDING_TIERS[tier - 1][0], LegalPerHour=round(legal), LegalPercent=round(100 * legal / t3),
                         ContrabandPerHour=round(contraband), ContrabandPercent=round(100 * contraband / t3),
                         FreightPerHour=freight[tier]["PerHour"], FreightPercent=freight[tier]["Percent"]))
    return dict(NeutralFreightPerHour=freight[0]["PerHour"], NeutralFreightPercent=freight[0]["Percent"], Tiers=rows)


def standing_reference():
    """Reference numbers the C# standing, price and tariff rules must reproduce."""
    standings = [-5000, -501, -500, -499, 0, 249, 250, 749, 750, 1499, 1500, 5000]
    tiers = [dict(Standing=x, Tier=standing_tier(x), ScanCut=standing_scan_cut(max(0, standing_tier(x))),
                  PriceBonus=standing_price_bonus(max(0, standing_tier(x)))) for x in standings]
    tariffs = [dict(Credits=c, Percent=pc, Tariff=tariff(c, pc))
               for c, pc in [(1000, 0), (1000, 5), (999, 5), (1, 10), (100000, 10), (100, 25), (3, -4), (0, 10)]]
    base, factor, depth = 100, 1.2, 2500
    prices = [dict(Social=social, Bonus=bonus, Units=units, Buy=buy_cost(base, factor, 0.0, depth, units, social, bonus),
                   Sell=sell_revenue(base, factor, 0.0, depth, units, social, bonus))
              for social, bonus, units in [(10, 0.0, 10), (26, 0.0, 10), (10, 0.015, 10), (26, 0.015, 40), (10, 0.01, 1)]]
    scan = [dict(Banned=value, Compartment=comp, Standing=cut, Chance=round(scan_chance(value, comp, cut), 6))
            for value, comp, cut in [(10000, 0, 0), (10000, 0, 25), (10000, 30, 25), (40000, 15, 8), (200000, 0, 16)]]
    return dict(Tiers=tiers, Tariffs=tariffs, Prices=prices, Scans=scan,
                FreightMinimumStandings=dict(zip(map(str, FREIGHT_OFFER_VOLUMES), FREIGHT_OFFER_MIN_STANDING)))


MARKETS = markets()
MARKET_INDEX = {(m["exchange"], m["commodity"]): m for m in MARKETS}
COMMODITY_NAMES = [c[1] for c in COMMODITIES]
HULLS = [("Fighter", 10), ("Y8 Miner", 188), ("Barracuda", 200), ("XS Freighter", 480), ("Heavy Lifter", 750),
         ("Republic Quartermaster", 1200), ("Hutt Grand Barge", 1980)]


def report():
    print("Orbit rule violations:", check_orbit_rule() or "none")
    print(f"{len(COMMODITIES)} commodities, {len(EXCHANGES)} exchanges, {len(MARKETS)} markets")
    print("\nSpace combat today (normal kills, 60 per hour):")
    for t in range(1, 7):
        per_kill, per_hour = combat_credits_per_hour(t)
        print(f"  T{t}: {per_kill:7.0f} per kill, {per_hour:8.0f} per hour")
    for lawful in (True, False):
        print(f"\nBest greedy loop from Viscara on fresh markets, {'legal goods only' if lawful else 'every good'} "
              "(SOC 10): credits in hours 1 / 2 / 3 / 4, and the 4-hour average")
        for name, hold in HULLS:
            hours = simulate(hold, lawful=lawful)
            print(f"  {name:24s} hold {hold:5d}: " + " / ".join(f"{h:6.0f}" for h in hours)
                  + f"   avg {sum(hours) / len(hours):6.0f}")
    worst = max(same_exchange_round_trip(m, 50) for m in MARKETS)
    print(f"\nBest same-exchange buy-then-sell of 50 units at SOC 26: {worst} credits (must be < 0)")
    log = []
    simulate(480, hours=1, log=log, lawful=True)
    print("\nXS Freighter, legal goods, hour 1 legs (minute, from, to, profit, basket):")
    for row in log:
        print("   ", row)
    print("\nAfter customs and pirates (expected loss), freighters (hold 480+), 4-hour averages, against T3 combat:")
    outcome = risk_outcome()
    print(f"  every good, no compartment: {outcome['ContrabandPerHour']} ({outcome['ContrabandPercent']}%; target 150-200%)")
    print(f"  every good, Smuggling Compartment Mk V: {outcome['CompartmentPerHour']} ({outcome['CompartmentPercent']}%)")
    print(f"  legal goods: {outcome['LegalPerHour']} ({outcome['LegalPercent']}%; target 100-125%)")
    print(f"  freight contracts, {FREIGHT_SIM_VOLUME} volume: {outcome['FreightPerHour']} ({outcome['FreightPercent']}%; target 75%)")
    for name, hold in HULLS:
        for lawful in (True, False):
            hours = simulate_risk(hold, lawful=lawful)
            print(f"  {name:24s} {'legal' if lawful else 'all  '}: " + " / ".join(f"{h:6.0f}" for h in hours)
                  + f"   avg {sum(hours) / len(hours):6.0f}")
    log = []
    simulate_risk(480, hours=2, log=log)
    print("\nXS Freighter, every good, after risk, hours 1-2 legs (minute, from, to, expected profit, basket):")
    for row in log:
        print("   ", row)
    print("\nStanding (W7c): best case, a pilot with this tier at every faction (price bonus at their exchanges, scan cut at "
          "every patrol), freighters, 4-hour averages, against T3 combat:")
    result = standing_outcome()
    print(f"  Neutral: freight contracts {result['NeutralFreightPerHour']} ({result['NeutralFreightPercent']}%; offers over "
          f"100 volume are locked)")
    for row in result["Tiers"]:
        print(f"  {row['Tier']:8s}: legal {row['LegalPerHour']} ({row['LegalPercent']}%; target 100-125% at neutral), "
              f"every good {row['ContrabandPerHour']} ({row['ContrabandPercent']}%; target 150-200% at neutral), "
              f"freight {row['FreightPerHour']} ({row['FreightPercent']}%)")
    t3 = combat_credits_per_hour(3)[1]
    print(f"\nCargo Expanders (W11d): legal goods, 4-hour averages without risk, against T3 combat ({t3:.0f}); "
          "the -5% speed per expander is not modelled (leg times stay fixed), so these overstate the gain:")
    for name, hold in HULLS:
        if hold < 100:
            continue
        for ranks in ((), (45,), (45, 45), (45, 45, 45, 45)):
            bay = expanded_hold(hold, ranks)
            hours = simulate(bay, lawful=True)
            avg = sum(hours) / len(hours)
            print(f"  {name:24s} {len(ranks)} x Mk X, hold {bay:5d}: avg {avg:6.0f} ({100 * avg / t3:3.0f}% of T3)")
    beacons = beacon_outcome()
    print(f"\nJump Beacons (best case: a beacon open to anyone in every orbit with an exchange; a beacon leg takes "
          f"{BEACON_LEG_MINUTES:.0f} minutes whatever the hops and rolls customs and ambushes only in its two end orbits; "
          f"Station Fuel at {beacons['FuelPrice']}): 4-hour averages against T3 combat ({beacons['T3Combat']}):")
    for row in beacons["Rows"]:
        print(f"  {row['Hull']:24s} {row['Size']:6s}: freight {FREIGHT_SIM_VOLUME} volume {row['FreightPerHour']} "
              f"({row['FreightPercent']}%; target 75%), best job (5 hops, {row['Fuel']} fuel) {row['Best5HopPerHour']} "
              f"({row['Best5HopPercent']}%), legal {row['LegalPerHour']} ({row['LegalPercent']}%; target 100-125%), "
              f"every good after risk {row['EveryPerHour']} ({row['EveryPercent']}%; target 150-200%)")
    print("  Checks: " + ("; ".join(beacon_problems(beacons)) or "all inside the trade bands"))
    print("\nCity tariff, one buy and one sale of 1,000 credits at 10%:",
          f"{tariff(1000, 10)} each, {tariff(1000, 10) * 2 / 2000:.0%} of the money changing hands")


# ---------------------------------------------------------------------------------------------------------------
# Generated C#
# ---------------------------------------------------------------------------------------------------------------
HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/trade.py. Do not edit by hand: change trade.py and regenerate.
// </auto-generated>
"""


def cs_string(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def cs_array(type_name, items):
    return f"new {type_name}[] {{ {items} }}" if items else f"System.Array.Empty<{type_name}>()"


def cs_double(x):
    text = repr(float(x))
    return text


def catalog_cs():
    lines = [HEADER.rstrip("\n"), "using System.Collections.Generic;", "using SWLOR.Game.Server.Enumeration;",
             "using SWLOR.Game.Server.Service.FactionService;", "using SWLOR.Game.Server.Service.TradeService;", "",
             "namespace SWLOR.Game.Server.Feature.TradeDefinition", "{",
             "    /// <summary>", "    /// The commodities and dock exchanges of the trade core (plan section 13).", "    /// </summary>",
             "    public static class TradeCatalog", "    {",
             "        public static readonly IReadOnlyList<CommodityDetail> Commodities = new List<CommodityDetail>", "        {"]
    for _, enum, name, base, vol, banned, desc in COMMODITIES:
        producers = ", ".join(f"TradeExchangeType.{e[0]}" for e in EXCHANGES if enum in e[6])
        consumers = ", ".join(f"TradeExchangeType.{e[0]}" for e in EXCHANGES if needs(e, enum))
        bans = ", ".join(f"TradeAuthority.{a}" for a in banned)
        sources = ", ".join(f"TradeExchangeType.{x}" for x in HOLDING_GOOD_SOURCES.get(enum, []))
        holding = f"HoldingSources = {cs_array('TradeExchangeType', sources)}, " if sources else ""
        lines.append(f"            new CommodityDetail {{ Type = CommodityType.{enum}, Name = {cs_string(name)}, BasePrice = {base}, "
                     f"Volume = {vol}, BannedBy = {cs_array('TradeAuthority', bans)}, "
                     f"Producers = {cs_array('TradeExchangeType', producers)}, Consumers = {cs_array('TradeExchangeType', consumers)}, "
                     f"{holding}Description = {cs_string(desc)} }},")
    lines += ["        };", "",
              "        public static readonly IReadOnlyList<TradeExchangeDetail> Exchanges = new List<TradeExchangeDetail>", "        {"]
    for enum, name, planets, orbit, authority, tier, produces, consumes in EXCHANGES:
        planet_list = ", ".join(f"PlanetType.{p}" for p in planets)
        black = "true" if enum in BLACK_MARKETS else "false"
        lines.append(f"            new TradeExchangeDetail {{ Type = TradeExchangeType.{enum}, Name = {cs_string(name)}, "
                     f"Authority = TradeAuthority.{authority}, OrbitTier = {tier}, PlanetTypes = new[] {{ {planet_list} }}, "
                     f"OrbitResref = {cs_string(orbit)}, IsBlackMarket = {black}, "
                     f"StandingFaction = FactionType.{EXCHANGE_FACTION[enum]}, "
                     "Markets = new List<TradeMarketDetail>")
        lines.append("            {")
        for m in MARKETS:
            if m["exchange"] == enum:
                lines.append(f"                new TradeMarketDetail {{ Commodity = CommodityType.{m['commodity']}, "
                             f"Role = CommodityRole.{m['role']}, PriceFactor = {cs_double(m['factor'])}, Depth = {m['depth']} }},")
        lines.append("            } },")
    lines += ["        };", "",
              "        /// <summary>The space systems: orbit tier for pirates and convoys, and the customs patrols of lawful orbits.</summary>",
              "        public static readonly IReadOnlyList<TradeOrbitDetail> Orbits = new List<TradeOrbitDetail>", "        {"]
    for orbit, tier in ORBIT_TIERS.items():
        patrols = ", ".join(f"TradeAuthority.{a}" for a in CUSTOMS.get(orbit, []))
        lines.append(f"            new TradeOrbitDetail {{ AreaResref = {cs_string(orbit)}, Name = {cs_string(ORBIT_NAMES[orbit])}, "
                     f"Tier = {tier}, CustomsAuthorities = {cs_array('TradeAuthority', patrols)} }},")
    lines += ["        };", "",
              "        /// <summary>Starlane hops between every pair of exchanges (freight contract fees and deadlines).</summary>",
              "        public static readonly IReadOnlyList<TradeRouteDetail> Routes = new List<TradeRouteDetail>", "        {"]
    for a in EXCHANGES:
        for b in EXCHANGES:
            if a is not b:
                lines.append(f"            new TradeRouteDetail {{ From = TradeExchangeType.{a[0]}, To = TradeExchangeType.{b[0]}, "
                             f"Hops = {hops(a[3], b[3])} }},")
    lines += ["        };", "    }", "}", ""]
    return "\n".join(lines)


def tuning_cs():
    return "\n".join([
        HEADER.rstrip("\n"), "namespace SWLOR.Game.Server.Service.TradeService", "{",
        "    /// <summary>",
        "    /// The trade numbers: spread, flooding, recovery, Social, XP and cargo spill (the trade core), and customs,",
        "    /// black markets, pirate ambushes, convoys, freight contracts, lawful standing and the HoloNet board (trade risk), and",
        "    /// what standing unlocks and the city exchange tariff (standing and tariffs).",
        "    /// </summary>",
        "    public static class TradeTuning", "    {",
        f"        public const double Spread = {cs_double(SPREAD)};",
        f"        public const double FloodMinimum = {cs_double(FLOOD_MIN)};",
        f"        public const double FloodMaximum = {cs_double(FLOOD_MAX)};",
        f"        public const double RecoveryHalfLifeHours = {cs_double(RECOVERY_HALF_LIFE_HOURS)};",
        f"        public const double SocialPerPoint = {cs_double(SOCIAL_PER_POINT)};",
        f"        public const int SocialBase = {SOCIAL_BASE};",
        f"        public const double SocialMaximum = {cs_double(SOCIAL_MAX)};",
        f"        public const double XpPerProfitCredit = {cs_double(XP_PER_PROFIT_CREDIT)};",
        f"        public const int XpMaximumPerSale = {XP_MAX_PER_SALE};",
        f"        public const int SpillSeconds = {SPILL_SECONDS};",
        f"        public const int SpillMaximumContainers = {SPILL_MAX_CONTAINERS};",
        "",
        f"        public const double BlackMarketPremium = {cs_double(BLACK_MARKET_PREMIUM)};",
        f"        public const double ScanBase = {cs_double(SCAN_BASE)};",
        f"        public const double ScanPerThousandCredits = {cs_double(SCAN_PER_THOUSAND)};",
        f"        public const double ScanMaximum = {cs_double(SCAN_MAX)};",
        f"        public const double FineRate = {cs_double(FINE_RATE)};",
        f"        public const int ScanDelaySeconds = {SCAN_DELAY_SECONDS};",
        f"        public const int CustomsAnswerSeconds = {CUSTOMS_ANSWER_SECONDS};",
        f"        public const int PatrolSeconds = {PATROL_SECONDS};",
        f"        public const int CompartmentBasePercent = {COMPARTMENT_BASE};",
        f"        public const int CompartmentRanksPerPercent = {COMPARTMENT_RANKS_PER_PERCENT};",
        f"        public const int CargoExpanderBasePercent = {CARGO_EXPANDER_BASE};",
        f"        public const int CargoExpanderRanksPerPercent = {CARGO_EXPANDER_RANKS_PER_PERCENT};",
        "",
        f"        public const int AmbushThreshold = {AMBUSH_THRESHOLD};",
        f"        public const double AmbushBase = {cs_double(AMBUSH_BASE)};",
        f"        public const double AmbushPerTenThousandCredits = {cs_double(AMBUSH_PER_TEN_THOUSAND)};",
        f"        public const double AmbushMaximum = {cs_double(AMBUSH_MAX)};",
        f"        public const double AmbushTierStep = {cs_double(AMBUSH_TIER_STEP)};",
        f"        public const int AmbushDelaySeconds = {AMBUSH_DELAY_SECONDS};",
        f"        public const int AmbushSquadMinimum = {AMBUSH_SQUAD_MIN};",
        f"        public const int AmbushSquadMaximum = {AMBUSH_SQUAD_MAX};",
        f"        public const int AmbushSquadValueStep = {AMBUSH_SQUAD_VALUE_STEP};",
        f"        public const int AmbushSeconds = {AMBUSH_SECONDS};",
        f"        public const int SealedValuePerVolume = {SEALED_VALUE_PER_VOLUME};",
        "",
        f"        public const int ConvoyValuePerTier = {CONVOY_VALUE_PER_TIER};",
        f"        public const int ConvoyLifetimeSeconds = {CONVOY_LIFETIME_SECONDS};",
        "        public static readonly int[] ConvoyEscortsByTier = { 0, " + ", ".join(str(CONVOY_ESCORTS[t]) for t in range(1, 7)) + " };",
        f"        public const TradeAuthority ConvoyAuthorityDefault = TradeAuthority.{CONVOY_AUTHORITY_DEFAULT};",
        "",
        f"        public const int FreightOffersPerExchange = {FREIGHT_OFFERS_PER_EXCHANGE};",
        "        public static readonly int[] FreightOfferVolumes = { " + ", ".join(str(v) for v in FREIGHT_OFFER_VOLUMES) + " };",
        f"        public const double FreightFeeBase = {cs_double(FREIGHT_FEE_BASE)};",
        f"        public const double FreightFeePerHop = {cs_double(FREIGHT_FEE_PER_HOP)};",
        "        public static readonly double[] FreightOfferFeeFactors = { " + ", ".join(cs_double(v) for v in FREIGHT_OFFER_FEE_FACTOR) + " };",
        f"        public const double FreightDepositRate = {cs_double(FREIGHT_DEPOSIT_RATE)};",
        f"        public const int FreightDeadlineBaseMinutes = {FREIGHT_DEADLINE_BASE_MINUTES};",
        f"        public const int FreightDeadlinePerHopMinutes = {FREIGHT_DEADLINE_PER_HOP_MINUTES};",
        f"        public const int FreightMaximumActive = {FREIGHT_MAX_ACTIVE};",
        f"        public const int FreightOfferMinutes = {FREIGHT_OFFER_MINUTES};",
        "",
        *[f"        public const int Standing{k} = {v};" for k, v in STANDING.items()],
        f"        public const int StandingHostile = {STANDING_HOSTILE};",
        "        public static readonly int[] StandingTierMinimums = { " + ", ".join(str(t[1]) for t in STANDING_TIERS) + " };",
        "        public static readonly int[] StandingScanCutPercents = { " + ", ".join(str(t[2]) for t in STANDING_TIERS) + " };",
        "        public static readonly double[] StandingPriceBonuses = { " + ", ".join(cs_double(t[3]) for t in STANDING_TIERS) + " };",
        "        public static readonly int[] FreightOfferMinimumStandings = { " + ", ".join(str(v) for v in FREIGHT_OFFER_MIN_STANDING) + " };",
        f"        public const int HostilePatrolCooldownSeconds = {HOSTILE_PATROL_COOLDOWN_SECONDS};",
        "",
        f"        public const int HuttEnforcerTier = {HUTT_ENFORCER_TIER};",
        "        public static readonly string[] HuttEnforcerTags = { " + ", ".join(f'"{t}"' for t in HUTT_ENFORCER_TAGS) + " };",
        "        public static readonly string[] HuttEnforcerOrbits = { " + ", ".join(f'"{o}"' for o in HUTT_ENFORCER_ORBITS) + " };",
        f"        public const int HuttEnforcerSpawnWeight = {HUTT_ENFORCER_SPAWN_WEIGHT};",
        "",
        f"        public const int CityExchangeStarportLevel = {CITY_EXCHANGE_STARPORT_LEVEL};",
        f"        public const int TariffMaximumPercent = {TARIFF_MAX_PERCENT};",
        "",
        f"        public const int HoloNetRefreshMinutes = {HOLONET_REFRESH_MINUTES};",
        "    }", "}", ""])


_OUTCOME = []
_STANDING_OUTCOME = []


def standing_outcome_cached():
    """standing_outcome(), worked out once per run."""
    if not _STANDING_OUTCOME:
        _STANDING_OUTCOME.append(standing_outcome())
    return _STANDING_OUTCOME[0]


def outcome():
    """risk_outcome(), worked out once per run (it takes about a minute)."""
    if not _OUTCOME:
        _OUTCOME.append(risk_outcome())
    return _OUTCOME[0]


def reference_json():
    """The C# rules' reference cases and the simulated outcome (SWLOR.Game.Server.Tests, TradeRiskTests)."""
    return json.dumps(dict(Cases=risk_reference(), Outcome=outcome(), StandingReference=standing_reference(),
                           StandingOutcome=standing_outcome_cached()), indent=2) + "\n"


WRITES = {
    "SWLOR.Game.Server/Feature/TradeDefinition/TradeCatalog.cs": catalog_cs,
    "SWLOR.Game.Server/Service/TradeService/TradeTuning.cs": tuning_cs,
    "SWLOR.Game.Server.Tests/Fixtures/Trade/TradeRiskReference.json": reference_json,
}


# ---------------------------------------------------------------------------------------------------------------
# Bible tab: bible_tabs.py calls trade_tab(Sheet) to write the Trade (Revamp) tab.
# ---------------------------------------------------------------------------------------------------------------
def trade_tab(Sheet):
    s = Sheet("Trade (Revamp)", [24, 12, 9, 9, 14, 24, 24, 60])
    s.title("Trade (Revamp): commodities, dock exchanges, pricing, flooding and recovery")
    s.note("Dev Status: Proposed (space revamp W7a and W7b drafts). Generated by tools/SpaceRevampBalance/trade.py.")
    s.blank()
    s.title("Commodities")
    s.table(["Commodity", "Legality", "Base", "Volume", "Banned by", "Produced at", "Consumed at", "Base price source"],
            [[name, legality(banned), base, vol, ", ".join(banned) or "-",
              ", ".join(e[1] for e in EXCHANGES if enum in e[6]) or
              "Holdings at " + ", ".join(exchange(x)[1] for x in HOLDING_GOOD_SOURCES.get(enum, [])),
              ", ".join(e[1] for e in EXCHANGES if needs(e, enum)),
              SOURCE[enum]] for _, enum, name, base, vol, banned, _ in COMMODITIES])
    s.title("Exchanges")
    s.table(["Exchange", "Authority", "Orbit tier", "", "Orbit", "Exports", "Imports", ""],
            [[e[1], e[4], e[5], "", e[3], ", ".join(commodity(c)[2] for c in e[6]),
              ", ".join(commodity(c)[2] for c in e[7]), ""] for e in EXCHANGES])
    s.title("Pricing rules")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Producer price", PRODUCER_FACTOR, "", "", "", "", "", "x base"],
        ["Neutral price", f"{NEUTRAL_BASE} + {NEUTRAL_PER_HOP} per hop", "", "", "", "", "",
         f"hops from the nearest producer, at most {NEUTRAL_MAX}"],
        ["Consumer price", f"{CONSUMER_BASE} + {CONSUMER_PER_HOP} per hop", "", "", "", "", "",
         f"at most {CONSUMER_MAX}; longer routes pay more"],
        ["Risk premium", ", ".join(f"{k} {v}" for k, v in RISK_PREMIUM.items()), "", "", "", "", "",
         "on neutral and consumer prices"],
        ["Spread", SPREAD, "", "", "", "", "", "the exchange sells at mid + spread and buys at mid - spread"],
        ["Depth", DEPTH_CREDITS, "", "", "", "", "", "credits at base price; depth in units = this / base"],
        ["Flooding", "exp(-pressure / depth)", "", "", "", "", "",
         f"clamped to {FLOOD_MIN}-{FLOOD_MAX}; selling adds pressure, buying removes it"],
        ["Recovery half-life (h)", RECOVERY_HALF_LIFE_HOURS, "", "", "", "", "", "pressure decays toward 0"],
        ["Social", f"{SOCIAL_PER_POINT * 100:g}% per point over {SOCIAL_BASE}", "", "", "", "", "", f"at most {SOCIAL_MAX * 100:g}%"],
        ["Trade XP", f"{XP_PER_PROFIT_CREDIT} Piloting XP per profit credit", "", "", "", "", "", f"at most {XP_MAX_PER_SALE} per sale"],
        ["Cargo spill", f"{SPILL_SECONDS} s", "", "", "", "", "", f"up to {SPILL_MAX_CONTAINERS} containers; anyone can scoop"],
    ])
    s.title("Trade risk (W7b)")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Black markets", ", ".join(exchange(e)[1] for e in BLACK_MARKETS), "", "", "", "", "",
         f"x{BLACK_MARKET_PREMIUM} on restricted and contraband goods they do not make"],
        ["Customs scan chance", f"{SCAN_BASE} + {SCAN_PER_THOUSAND} per 1,000 credits", "", "", "", "", "",
         f"per orbit entry, on the base value of the goods the patrol bans; at most {SCAN_MAX}; {SCAN_DELAY_SECONDS} s after entry"],
        ["Customs fine", f"{FINE_RATE} x banned base value", "", "", "", "", "",
         f"and the goods are confiscated; or run: the patrol turns hostile. {CUSTOMS_ANSWER_SECONDS} s to answer"],
        ["Smuggling Compartment", f"-({COMPARTMENT_BASE} + rank / {COMPARTMENT_RANKS_PER_PERCENT})% scan chance", "", "", "", "", "",
         "Low slot, Piloting (the Ship Modules tab's step); Mk I 15%, Mk X 30%; only the best counts"],
        ["Cargo Expander", f"+({CARGO_EXPANDER_BASE} + rank / {CARGO_EXPANDER_RANKS_PER_PERCENT})% of the hull's cargo", "", "", "", "", "",
         f"Low slot, Piloting (the Ship Modules tab's step); Mk I 20%, Mk X 35%; {CARGO_EXPANDER_SPEED_PERCENT}% speed each; both stack"],
        ["Pirate ambush chance", f"{AMBUSH_BASE} + {AMBUSH_PER_TEN_THOUSAND} per 10,000 over {AMBUSH_THRESHOLD}", "", "", "", "", "",
         f"per orbit entry, at most {AMBUSH_MAX}, x (0.8 + {AMBUSH_TIER_STEP} x orbit tier); cargo base value plus "
         f"{SEALED_VALUE_PER_VOLUME} per sealed volume"],
        ["Pirate squad", f"{AMBUSH_SQUAD_MIN} + 1 per {AMBUSH_SQUAD_VALUE_STEP} over the threshold, +1 from tier 4", "", "", "", "", "",
         f"{AMBUSH_SQUAD_MIN}-{AMBUSH_SQUAD_MAX} ships; an interceptor always, then fighters, a gunship from 4 ships"],
        ["Convoy cargo", f"{CONVOY_VALUE_PER_TIER} x orbit tier base credits", "", "", "", "", "",
         "1-2 legal exports of the orbit; escorts " + ", ".join(f"T{t} {n}" for t, n in CONVOY_ESCORTS.items())],
        ["Freight fee", f"volume x ({FREIGHT_FEE_BASE} + {FREIGHT_FEE_PER_HOP} x hops)", "", "", "", "", "",
         f"deposit {FREIGHT_DEPOSIT_RATE} x fee; deadline {FREIGHT_DEADLINE_BASE_MINUTES} + {FREIGHT_DEADLINE_PER_HOP_MINUTES} min per hop; "
         f"{FREIGHT_OFFERS_PER_EXCHANGE} offers per exchange per {FREIGHT_OFFER_MINUTES} min; at most {FREIGHT_MAX_ACTIVE} held"],
        ["Lawful standing", ", ".join(f"{k} {v}" for k, v in STANDING.items()), "", "", "", "", "",
         "ground faction standing (-5000 to 5000)"],
        ["HoloNet board", f"{HOLONET_REFRESH_MINUTES} min", "", "", "", "", "", "price snapshot age at most"],
    ])
    s.title("Standing and city tariffs (W7c)")
    s.table(["Rule", "Value", "", "", "", "", "", "Notes"], [
        ["Standing tiers", ", ".join(f"{n} {m}" for n, m, _, _ in STANDING_TIERS), "", "", "", "", "",
         f"minimum ground faction standing (-5000 to 5000) with the exchange's faction or the patrol's authority; Hostile at {STANDING_HOSTILE} or lower"],
        ["Customs scan cut", ", ".join(f"{n} -{c}%" for n, _, c, _ in STANDING_TIERS), "", "", "", "", "",
         "multiplies with a Smuggling Compartment's cut"],
        ["Price bonus", ", ".join(f"{n} +{b * 100:g}%" for n, _, _, b in STANDING_TIERS), "", "", "", "", "",
         "both sides of every trade at that faction's exchanges; adds to Social's (at most 1.5%)"],
        ["Freight offers", ", ".join(f"{v} vol: {m or 'open'}" for v, m in zip(FREIGHT_OFFER_VOLUMES, FREIGHT_OFFER_MIN_STANDING)), "", "", "", "", "",
         "minimum standing with the offering exchange's faction (Dathomir has none: open)"],
        ["Freight standing", STANDING["DeliveredFreight"], "", "", "", "", "", "per delivered contract (late or on time)"],
        ["Freight fee factor", ", ".join(f"{v} vol: x{f:g}" for v, f in zip(FREIGHT_OFFER_VOLUMES, FREIGHT_OFFER_FEE_FACTOR)), "", "", "", "", "",
         "share of the usual fee per volume; the 400 volume bulk load pays less per unit"],
        ["Hostile patrols", f"every {HOSTILE_PATROL_COOLDOWN_SECONDS} s at most", "", "", "", "", "",
         "a patrol of a hostile authority attacks on sight when the pilot enters its system"],
        ["Hutt enforcers", f"tier {HUTT_ENFORCER_TIER}, weight {HUTT_ENFORCER_SPAWN_WEIGHT} each", "", "", "", "", "",
         f"{', '.join(HUTT_ENFORCER_TAGS)} (Neutral) in {', '.join(HUTT_ENFORCER_ORBITS)}; attack pilots Hostile to the Hutt "
         "Cartel on sight; firing on one turns the orbit's enforcers and costs Hutt Cartel standing (AttackedLawfulShip)"],
        ["City exchange", f"starport level {CITY_EXCHANGE_STARPORT_LEVEL}", "", "", "", "", "",
         "uses the planet's NPC exchange (goods, prices, flooding, freight)"],
        ["City tariff", f"0-{TARIFF_MAX_PERCENT}%", "", "", "", "", "",
         "set by the mayor or council (Edit Taxes); added to buys, taken from sales, paid into the city treasury"],
    ])
    standing_result = standing_outcome_cached()
    neutral = outcome()
    s.title("Earnings with standing (best case: this tier at every faction; trade.py --sim)")
    s.table(["Standing", "Legal per hour", "% of T3", "Every good per hour", "% of T3", "Freight per hour", "% of T3", "Notes"],
            [["Neutral", neutral["LegalPerHour"], neutral["LegalPercent"], neutral["ContrabandPerHour"], neutral["ContrabandPercent"],
              standing_result["NeutralFreightPerHour"], standing_result["NeutralFreightPercent"], "offers over 100 volume locked"]] +
            [[row["Tier"], row["LegalPerHour"], row["LegalPercent"], row["ContrabandPerHour"], row["ContrabandPercent"],
              row["FreightPerHour"], row["FreightPercent"], ""] for row in standing_result["Tiers"]])
    s.title("Orbits")
    s.table(["Orbit", "Tier", "Customs", "", "", "", "", ""],
            [[ORBIT_NAMES[o], t, ", ".join(CUSTOMS.get(o, [])) or "-", "", "", "", "", ""] for o, t in ORBIT_TIERS.items()])
    result = outcome()
    s.title("Earnings after risk (trade.py --sim; freighters, 4-hour average, against T3 space combat)")
    s.table(["Activity", "Credits per hour", "% of T3", "", "", "", "", "Target"], [
        ["Contraband and legal goods", result["ContrabandPerHour"], result["ContrabandPercent"], "", "", "", "", "150-200%"],
        ["... with a Smuggling Compartment Mk V", result["CompartmentPerHour"], result["CompartmentPercent"], "", "", "", "", ""],
        ["Legal goods only", result["LegalPerHour"], result["LegalPercent"], "", "", "", "", "100-125%"],
        ["Freight contracts", result["FreightPerHour"], result["FreightPercent"], "", "", "", "", "about 75%"],
    ])
    s.title("Markets")
    s.table(["Exchange", "Commodity", "Role", "Factor", "Depth", "", "", ""],
            [[exchange(m["exchange"])[1], commodity(m["commodity"])[2], m["role"], m["factor"], m["depth"], "", "", ""]
             for m in MARKETS])
    return s


def module_lanes(gated=False):
    """The starlane graph as Module/git holds it (tele_obj placeables whose DESTINATION is a STARLANE_LANDING waypoint),
    read the way ShipAccessRulesTests reads it: area -> the areas its lanes land in. The open lanes, or with
    gated=True the lanes a key item opens (KEY_ITEM_ID)."""
    lanes, landings = [], {}
    for path in sorted((REPO / "Module" / "git").glob("*.git.json")):
        text = path.read_text(encoding="latin-1")
        if "STARLANE_LANDING" not in text:
            continue
        data, area = json.loads(text), path.name[:-len(".git.json")]
        for p in data["Placeable List"]["value"]:
            if p["TemplateResRef"]["value"] != "tele_obj":
                continue
            variables = {v["Name"]["value"]: v["Value"]["value"] for v in p.get("VarTable", {}).get("value", [])}
            dest = [variables["DESTINATION"]] if "DESTINATION" in variables else []
            if bool(variables.get("KEY_ITEM_ID", 0)) != gated:
                continue
            if dest and str(dest[0]).startswith("STARLANE_LANDING"):
                lanes.append((area, dest[0]))
        for w in data["WaypointList"]["value"]:
            if w["Tag"]["value"].startswith("STARLANE_LANDING"):
                landings[w["Tag"]["value"]] = area
    graph = {}
    for area, dest in lanes:
        graph.setdefault(area, set()).add(landings[dest])
    return graph


def check_lanes():
    """The differences between LANES and the module's starlanes (empty when they agree)."""
    out = []
    for name, table, module in (("LANES", LANES, module_lanes()), ("GATED_LANES", GATED_LANES, module_lanes(gated=True))):
        out += [f"{a}: {name} {sorted(table.get(a, []))}, module {sorted(module.get(a, set()))}"
                for a in sorted(set(table) | set(module)) if set(table.get(a, [])) != module.get(a, set())]
    return out


def main():
    if "--sim" in sys.argv:
        report()
        return
    if check_orbit_rule():
        sys.exit(f"orbit rule broken: {check_orbit_rule()}")
    if check_lanes():
        sys.exit("LANES differs from the Module/git starlanes: " + "; ".join(check_lanes()))
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
    print(f"{len(COMMODITIES)} commodities, {len(MARKETS)} markets, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
