"""Space revamp rewards (plan sections 7, 8 and 14, Appendices C and D): the named rank-50 modules, Hull Designs and
their Design Schematics, Ship Schematics, ammo variants, the Refit fee, the salvage rare-find rewards and the research
hook for prototype tech. catalog.py turns this into C# and item blueprints; bible_tabs.py writes the Rewards (Revamp)
tab and bible_recipes.py the recipe rows. Draft numbers, tune in testing.
"""
import components as comp

# W9b's RecipeType block (named modules, rare components, ammo variants), after the component recipes (7358-7487).
RECIPE_START = 7488

# ---------------------------------------------------------------------------------------------------------------
# Named rank-50 modules (plan sections 8 and 14): one per main family, the ground Chiro pattern (level-52 recipe
# unlocked by a Module Schematic, 2 enhancement slots, research cost +20%). Their numbers are the family's rank-45
# numbers +10% (penalties stay as they are), with Attack and Accuracy 2 x 50. Each takes its family's tier 5 component
# twice, a Thorilide Core and an Aurodium Plating on top of twice the rank-45 bill's refined ore and electronics.
# ---------------------------------------------------------------------------------------------------------------
NAMED_RANK = 50
NAMED_MULT = 1.1
NAMED_RECIPE_LEVEL = 52
NAMED_ENHANCEMENT_SLOTS = 2
NAMED_RESEARCH_COST_MODIFIER = 0.2
NAMED_COST = 4000
# family key, item name, hotbar short name (14 characters at most)
NAMED = [
    ("LaserCannon", "Sunfire Laser Cannon", "Sunfire"),
    ("IonCannon", "Stormcaller Ion Cannon", "Stormcaller"),
    ("BeamCannon", "Searing Lance Beam Cannon", "Searing Lance"),
    ("MissileLauncher", "Hailstorm Missile Launcher", "Hailstorm"),
    ("TorpedoLauncher", "Doomhammer Torpedo Launcher", "Doomhammer"),
    ("PointDefenseTurret", "Aegis Point-Defense Turret", "Aegis PD"),
    ("ShieldBooster", "Bastion Shield Booster", "Bastion"),
    ("ShieldRepairer", "Lifeline Shield Repairer", "Lifeline"),
    ("ShieldHardener", "Ironwall Shield Hardener", "Ironwall"),
    ("CapacitorBattery", "Wellspring Capacitor Battery", "Wellspring"),
    ("TargetingComputer", "Hawkeye Targeting Computer", "Hawkeye"),
    ("HullPlating", "Monolith Hull Plating", "Monolith"),
    ("HullRepairer", "Phoenix Hull Repairer", "Phoenix"),
    ("AdaptivePlating", "Chameleon Adaptive Plating", "Chameleon"),
    ("CapacitorRegulator", "Dynamo Capacitor Regulator", "Dynamo"),
    ("Thrusters", "Comet Thrusters", "Comet"),
]
NAMED_EXTRA = [(comp.rare_resref("thorcore"), 1), (comp.rare_resref("aurplate"), 1)]


def named_value(value):
    """A rank-45 number made named: positive values +10% (at least +1), penalties and zero unchanged."""
    if isinstance(value, bool) or not isinstance(value, (int, float)) or value <= 0:
        return value
    if isinstance(value, float) and not float(value).is_integer():
        return round(value * NAMED_MULT, 2)
    return max(int(value) + 1, int(value * NAMED_MULT + 0.5))


def unlock_resref(tag_prefix):
    """The Module Schematic of a named module: recipe_<family short>50."""
    return f"recipe_{tag_prefix[3:]}50"


# Where the Module Schematics drop for now (aces, Wild Space and slicing add more): the T5 and T6 boss tables at weight 1
# each, with the tables' other entries multiplied by this so their own odds stay as they were.
NAMED_LOOT_TABLES = {"SPACE_BOSS_T5": 150, "SPACE_BOSS_T6": 10}

# ---------------------------------------------------------------------------------------------------------------
# Hull Designs (plan section 7, Appendix C): every ship's base model is its Standard design; the launch skins are
# designs unlocked by a Design Schematic. A starport Refit swaps a ship to another design unlocked for it.
# ---------------------------------------------------------------------------------------------------------------
LAUNCH_SKINS = {
    "Striker": [10190, 10193, 10194],
    "Republic Striker": [10072, 10073, 10199, 10200],
    "Davaab": [10105],
    "Sith Fighter": [2008, 10089, 10090, 10091, 10210, 10211],
    "Cutlass": [10204],
    "Condor": [10153, 10144],
    "Republic Scout": [10069, 10070, 10205, 2001, 2003],
    "Sith Scout": [10087, 10088],
    "Republic Bomber": [10059, 10060, 10104],
    "Sith Bomber": [10078, 10079, 10106, 10241, 2007],
    "Chela Bomber": [2005, 10237],
    "Sith Infiltrator": [10084, 10085, 10064, 10065, 10066, 10067],
    "Chiss Hyperion": [10191, 10192],
    "Raider Prowler": [10114, 10221, 10222],
    "Republic Gunship": [10062, 10063, 10148, 10056],
    "Sith Gunship": [10081, 10082, 10163],
    "Hutt Gunship": [10216],
    "Basilisk War Droid": [10156],
    "Czerka Repair Tender": [10226],
    "Chiss Caretaker": [10102],
    "Jedi Transport": [10157, 10101],
    "Y8 Miner": [10151, 10227],
    "Gas Skimmer": [10201],
    "XS Freighter": [10054, 10206],
    "Lambda Shuttle": [10240],
    "Pirate Freighter": [10147, 10160],
    "Barracuda": [10164],
    "Republic Hammerhead": [10134, 10075],
    "Sith Hammerhead": [10137],
    "Mandalorian Raider": [10224],
    "Raider Marauder": [10195, 10133],
    "Heavy Lifter": [10132, 10229],
    "Czerka Salvager": [10198, 10131],
    "Freight Train": [2174, 10187],
    "Republic Quartermaster": [10150],
    "Hutt Grand Barge": [10212],
    "Neutral Thranta": [10217],
    "Republic Thranta": [10117, 10122, 2170],
    "Sith Thranta": [10126, 10238, 10239],
    "Terminus Corsair": [10142, 10093, 10118, 2169],
    "Crusader Corvette": [10116, 10115],
    "Republic Centurion": [10129, 10124, 10125, 10143, 10127, 10141],
}
DESIGN_LETTERS = "ABCDEFGH"
STANDARD_DESIGN = "Standard"
# The Refit fee by hull size (section 17 "Refit: fee"): 1,000 credits times the size's repair multiplier.
REFIT_FEE = {"Small": 1000, "Medium": 1500, "Large": 2500, "Capital": 4000}
DESIGN_SCHEMATIC_COST = 2000
SHIP_SCHEMATIC_COST = 10000

# Makers whose schematics come only from Wild Space (W12c): no salvage source now.
WILD_SPACE_MAKERS = {"Rift Raider"}

# ---------------------------------------------------------------------------------------------------------------
# Salvage rare finds (W6a): the percent of an elite or boss wreck's rare finds that are a reward instead of the tier
# table's item. A schematic is for a ship of the wreck's tier or lower (a ship's tier comes from its Piloting gate).
# ---------------------------------------------------------------------------------------------------------------
RARE_FIND_REWARDS = {
    "Elite": dict(design=20, ship=5, torpedo=0),
    "Boss": dict(design=25, ship=10, torpedo=15),
}
RARE_TORPEDO_QUANTITY = 2

# ---------------------------------------------------------------------------------------------------------------
# Ammo variants (plan sections 8 and 14). A launcher fires the variant its pilot loads (use the ammo item; use it
# again to go back to plain ammo), else plain ammo, else any variant it carries. Missile variants are crafted at every
# tier beside the plain missile recipe, with one signature material per tier; the rare torpedoes only come from boss
# wreck salvage.
# ---------------------------------------------------------------------------------------------------------------
PLAIN_MISSILE = {
    1: dict(level=5, quantity=5, components=[("ref_tilarium", 1), ("elec_ruined", 1)]),
    2: dict(level=15, quantity=15, components=[("ref_currian", 5), ("elec_flawed", 1)]),
    3: dict(level=25, quantity=25, components=[("ref_idailia", 7), ("elec_good", 2)]),
    4: dict(level=35, quantity=35, components=[("ref_barinium", 9), ("elec_imperfect", 2)]),
    5: dict(level=45, quantity=45, components=[("ref_gostian", 11), ("elec_high", 3)]),
}


def _tibanna_or_metal(t):
    return comp.TIBANNA if t in (3, 4) else comp.METAL[t]


# dmg is a percent of the launcher's DMG; shield is the shield damage multiplier; riders: Slow (percent, seconds) on a
# landed hit, Burn (percent of the shot's DMG every BURN_TICK_SECONDS for the seconds). signature: the extra material
# of each tier's recipe, or None when the ammo is not crafted.
AMMO = [
    dict(key="IonMissile", resref="ship_missile_ion", name="Ion Missile", family="MissileLauncher", damage="EM",
         dmg=90, accuracy=0, shield=1.25, rider="Slow", rider_percent=25, rider_seconds=6, model=250,
         signature=comp.MINERAL.get,
         description="A missile with an ionized warhead: EM damage that hits shields 25% harder, and hull hits slow the "
                     "target by 25% for 6 seconds. 90% of the launcher's DMG."),
    dict(key="IncendiaryMissile", resref="ship_missile_inc", name="Incendiary Missile", family="MissileLauncher",
         damage="Thermal", dmg=70, accuracy=0, shield=1.0, rider="Burn", rider_percent=12, rider_seconds=6, model=251,
         signature=_tibanna_or_metal,
         description="A missile with an incendiary warhead: 70% of the launcher's DMG as thermal damage, then the target "
                     "burns for 12% of it every 2 seconds for 6 seconds."),
    dict(key="SeekerMissile", resref="ship_missile_skr", name="Seeker Missile", family="MissileLauncher",
         damage="Explosive", dmg=90, accuracy=20, shield=1.0, rider="None", rider_percent=0, rider_seconds=0, model=252,
         signature=comp.GEM.get,
         description="A missile with a seeker head: +20 Accuracy at 90% of the launcher's DMG."),
    dict(key="IonTorpedo", resref="proton_bomb_ion", name="Ion Torpedo", family="TorpedoLauncher", damage="EM",
         dmg=90, accuracy=0, shield=1.0, rider="Slow", rider_percent=50, rider_seconds=6, model=253, signature=None,
         description="A salvaged torpedo with an ion warhead: EM damage that bypasses shields, and the hit slows the "
                     "target by 50% for 6 seconds. 90% of the launcher's DMG."),
    dict(key="PlasmaTorpedo", resref="proton_bomb_pls", name="Plasma Torpedo", family="TorpedoLauncher",
         damage="Thermal", dmg=85, accuracy=0, shield=1.0, rider="Burn", rider_percent=15, rider_seconds=6, model=254,
         signature=None,
         description="A salvaged torpedo with a plasma warhead: 85% of the launcher's DMG as thermal damage that bypasses "
                     "shields, then the target burns for 15% of it every 2 seconds for 6 seconds."),
]
BURN_TICK_SECONDS = 2
AMMO_COST = {"MissileLauncher": 60, "TorpedoLauncher": 600}

# ---------------------------------------------------------------------------------------------------------------
# Research (plan sections 14 and 23): module recipes research into blueprints whose item bonuses are blueprint traits
# (components.py). Prototype Tech is an optional input: spending one when a job starts adds a guaranteed tier 3 trait
# when the job completes. Its sources come with Wild Space (W12c).
# ---------------------------------------------------------------------------------------------------------------
PROTOTYPE_TECH = "proto_tech"
PROTOTYPE_TECH_NAME = "Prototype Tech"
PROTOTYPE_TECH_MODEL = 249
PROTOTYPE_TECH_COST = 1000
PROTOTYPE_TECH_BONUS_TIER = 3
PROTOTYPE_TECH_DESCRIPTION = ("Stolen experimental ship parts. A research terminal can spend one on a module blueprint "
                              "for a guaranteed top-tier bonus, and an Engineer builds it into a Prototype Core.")
