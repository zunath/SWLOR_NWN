"""Space revamp component layer (plan section 14, Appendix D): component families, their recipes, the traits
components carry and the salvage odds. catalog.py turns this into C#, item blueprints and recipes; bible_tabs.py
writes the Ship Components (Revamp) tab from it. Draft numbers, tune in testing.
"""

TIERS = [1, 2, 3, 4, 5]

# Tier material sets, as the existing Engineering and Smithery recipes use them.
REFINED = {1: "ref_tilarium", 2: "ref_currian", 3: "ref_idailia", 4: "ref_barinium", 5: "ref_gostian"}
ELECTRONICS = {1: "elec_ruined", 2: "elec_flawed", 3: "elec_good", 4: "elec_imperfect", 5: "elec_high"}
FIBERPLAST = {1: "fiberp_ruined", 2: "fiberp_flawed", 3: "fiberp_good", 4: "fiberp_imperfect", 5: "fiberp_high"}
METAL = {1: "aluminum", 2: "steel", 3: "obsidian", 4: "crystal", 5: "diamond"}
MINERAL = {1: "quadrenium", 2: "vintrium", 3: "ionite", 4: "katrium", 5: "zinsiam"}
GEM = {1: "jade", 2: "agate", 3: "citrine", 4: "ruby", 5: "emerald"}
HERB = {1: "herb_v", 2: "herb_m", 3: "herb_c", 4: "herb_x", 5: "herb_t"}
DURELIUM = "ore_durelium"
TIBANNA = "gas_tibanna"


def _durelium_or_metal(t):
    return DURELIUM if t <= 2 else METAL[t]


def _tibanna_or(other):
    return lambda t: TIBANNA if t in (3, 4) else other[t]


# key, enum, name, resref short, skill, ESS9 model, base material(t), signature material(t), description.
# Each component costs two material units (one base, one signature), so it stands in for two units of the
# module or hull bill it joins and the total material count stays where it was.
FAMILIES = [
    ("hframe", "HullFrame", "Hull Frame", "Smithery", 229, REFINED.get, _durelium_or_metal,
     "The ribbed metal skeleton of a hull or a plating module. Durelium in the lower tiers."),
    ("reactor", "ShipReactor", "Ship Reactor", "Engineering", 230, ELECTRONICS.get, _tibanna_or(REFINED),
     "A compact starship reactor core that powers a hull and feeds capacitor modules. Tibanna Gas in tiers 3 and 4."),
    ("thruster", "ThrusterAssembly", "Thruster Assembly", "Smithery", 231, REFINED.get, FIBERPLAST.get,
     "An engine nozzle with its mount and fuel lines, for hulls and thruster modules."),
    ("emitter", "EnergyEmitter", "Energy Emitter", "Engineering", 232, ELECTRONICS.get, _tibanna_or(MINERAL),
     "The focusing assembly of an energy weapon or damage amplifier. Tibanna Gas in tiers 3 and 4."),
    ("ordnance", "OrdnanceAssembly", "Ordnance Assembly", "Smithery", 233, REFINED.get, METAL.get,
     "A launcher breech and feed for missiles, torpedoes and explosive amplifiers."),
    ("shieldprj", "ShieldProjector", "Shield Projector", "Engineering", 234, ELECTRONICS.get, MINERAL.get,
     "A shield emitter dish for shield boosters, repairers and hardeners."),
    ("sensor", "SensorSuite", "Sensor Suite", "Engineering", 235, ELECTRONICS.get, GEM.get,
     "A sensor pod for targeting computers and electronic warfare."),
    ("extract", "ExtractionHead", "Extraction Head", "Engineering", 236, ELECTRONICS.get, METAL.get,
     "A cutting and focusing head for mining lasers, salvagers and gas harvesters."),
    ("lifesup", "LifeSupportSystem", "Life Support System", "Agriculture", 237, FIBERPLAST.get, HERB.get,
     "A hydroponic air and water system for freighters, cruisers and command ships."),
]
SALVAGE_FAMILIES = [f[1] for f in FAMILIES if f[1] != "LifeSupportSystem"]  # Appendix D: life support has no salvage


def resref(short, tier):
    return f"sc_{short}_{tier}"


def recipe_level(tier):
    return max(2, 10 * (tier - 1))


# Module family -> component family (Appendix D's inputs read for the module ladders).
MODULE_COMPONENT = {
    "LaserCannon": "EnergyEmitter", "IonCannon": "EnergyEmitter", "BeamCannon": "EnergyEmitter",
    "PointDefenseTurret": "EnergyEmitter", "Turbolaser": "EnergyEmitter", "QuadLaser": "EnergyEmitter",
    "StormCannon": "EnergyEmitter", "LaserBattery": "EnergyEmitter", "ThermalAmplifier": "EnergyEmitter",
    "EMAmplifier": "EnergyEmitter", "WeaponsComputer": "EnergyEmitter",
    "MissileLauncher": "OrdnanceAssembly", "TorpedoLauncher": "OrdnanceAssembly",
    "AssaultConcussionMissile": "OrdnanceAssembly", "ExplosiveAmplifier": "OrdnanceAssembly",
    "ShieldBooster": "ShieldProjector", "ShieldRepairer": "ShieldProjector", "ShieldHardener": "ShieldProjector",
    "BulwarkShieldGenerator": "ShieldProjector", "RepairField": "ShieldProjector", "RedundantShieldGenerator": "ShieldProjector",
    "CapacitorBattery": "ShipReactor", "HypermatterInjector": "ShipReactor", "CapacitorRegulator": "ShipReactor",
    "PowerDiverter": "ShipReactor",
    "HullPlating": "HullFrame", "HullRepairer": "HullFrame", "ThermalPlating": "HullFrame", "EMPlating": "HullFrame",
    "ExplosivePlating": "HullFrame", "AdaptivePlating": "HullFrame", "ReinforcedPlating": "HullFrame",
    "Thrusters": "ThrusterAssembly", "AdvancedThrusters": "ThrusterAssembly",
    "TargetingComputer": "SensorSuite", "TargetingArray": "SensorSuite", "EWarArray": "SensorSuite",
    "MiningLaser": "ExtractionHead", "StripMiner": "ExtractionHead", "Salvager": "ExtractionHead",
    "GasHarvester": "ExtractionHead",
}

# Hull role -> its role components (Appendix D "Used by ships"). Every hull also takes Hull Frames, a Ship Reactor and
# a Thruster Assembly (two for Interceptors and Blockade Runners).
ROLE_COMPONENTS = {
    "Fighter": ["EnergyEmitter"], "Interceptor": ["SensorSuite"], "Bomber": ["OrdnanceAssembly"],
    "Explorer": ["SensorSuite"], "Gunship": ["ShieldProjector"], "Logistics": ["ShieldProjector"],
    "Mining Barge": ["ExtractionHead"], "Light Freighter": ["LifeSupportSystem"],
    "Blockade Runner": ["LifeSupportSystem"], "Frigate": ["EnergyEmitter"], "Industrial": ["ExtractionHead"],
    "Heavy Freighter": ["LifeSupportSystem"], "Cruiser": ["EnergyEmitter", "LifeSupportSystem"],
    "Command Ship": ["SensorSuite", "LifeSupportSystem"],
}
TWO_THRUSTERS = {"Interceptor", "Blockade Runner"}
HULL_FRAMES = {"Small": 1, "Medium": 2, "Large": 3, "Capital": 4}

# Trait stats. Percent stats change the module's own numbers; flat stats add to the ship (Evasion, Speed percent
# points), to the module's bonus (Yield, in Module Bonus points) or to the rare-find roll (RareFind, points).
PERCENT_STATS = ["Damage", "Accuracy", "CapacitorCost", "Hull", "Shield", "Repair", "Capacitor", "CapacitorRegen",
                 "Defense", "Resistance"]
FLAT_STATS = ["Evasion", "Speed", "Yield", "RareFind"]
STAT_TEXT = {"Damage": "DMG", "Accuracy": "Accuracy", "CapacitorCost": "capacitor use", "Hull": "hull",
             "Shield": "shield", "Repair": "repair amount", "Capacitor": "capacitor", "CapacitorRegen": "capacitor regen",
             "Defense": "Defense", "Resistance": "resistances", "Evasion": "Evasion", "Speed": "speed",
             "Yield": "Module Bonus", "RareFind": "rare-find chance"}

# id, enum, name, kind (Crafted / Common / Rare), families, effects. Crafted variants are trade-offs (section 14: no
# strict upgrade); salvaged common traits are one small bonus; rare traits are salvage-only and never crafted.
TRAITS = [
    (1, "Reinforced", "Reinforced", "Crafted", ["HullFrame"], [("Hull", 6), ("Resistance", 6), ("Speed", -2)]),
    (2, "Lightweight", "Lightweight", "Crafted", ["HullFrame"], [("Speed", 2), ("Evasion", 3), ("Hull", -5), ("Resistance", -5)]),
    (3, "Overclocked", "Overclocked", "Crafted", ["ShipReactor"], [("CapacitorRegen", 8), ("Capacitor", -5)]),
    (4, "DeepCell", "Deep-Cell", "Crafted", ["ShipReactor"], [("Capacitor", 8), ("CapacitorRegen", -5)]),
    (5, "Vectored", "Vectored", "Crafted", ["ThrusterAssembly"], [("Evasion", 3), ("Speed", -1)]),
    (6, "Boosted", "Boosted", "Crafted", ["ThrusterAssembly"], [("Speed", 2), ("Evasion", -2)]),
    (7, "Focused", "Focused", "Crafted", ["EnergyEmitter"], [("Accuracy", 6), ("Damage", -3)]),
    (8, "Overcharged", "Overcharged", "Crafted", ["EnergyEmitter"], [("Damage", 6), ("CapacitorCost", 25)]),
    (9, "Efficient", "Efficient", "Crafted", ["EnergyEmitter"], [("CapacitorCost", -25), ("Damage", -3)]),
    (10, "Guided", "Guided", "Crafted", ["OrdnanceAssembly"], [("Accuracy", 6), ("Damage", -3)]),
    (11, "HeavyPayload", "Heavy-Payload", "Crafted", ["OrdnanceAssembly"], [("Damage", 6), ("Accuracy", -4)]),
    (12, "Dense", "Dense", "Crafted", ["ShieldProjector"], [("Shield", 6), ("Repair", -4)]),
    (13, "Rapid", "Rapid", "Crafted", ["ShieldProjector"], [("Repair", 6), ("Shield", -4)]),
    (14, "Calibrated", "Calibrated", "Crafted", ["SensorSuite"], [("Accuracy", 6), ("Evasion", -2)]),
    (15, "LowPower", "Low-Power", "Crafted", ["SensorSuite"], [("CapacitorCost", -25), ("Accuracy", -3)]),
    (16, "Bulk", "Bulk", "Crafted", ["ExtractionHead"], [("Yield", 2), ("CapacitorCost", 25)]),
    (17, "Precision", "Precision", "Crafted", ["ExtractionHead"], [("RareFind", 5), ("Yield", -1)]),
    (21, "Tuned", "Tuned", "Common", ["EnergyEmitter", "OrdnanceAssembly"], [("Damage", 3)]),
    (22, "Aligned", "Aligned", "Common", ["EnergyEmitter", "OrdnanceAssembly", "SensorSuite"], [("Accuracy", 3)]),
    (23, "Insulated", "Insulated", "Common", ["EnergyEmitter", "ShipReactor", "ShieldProjector", "SensorSuite", "ExtractionHead"], [("CapacitorCost", -20)]),
    (24, "Braced", "Braced", "Common", ["HullFrame"], [("Hull", 3)]),
    (25, "Charged", "Charged", "Common", ["ShieldProjector"], [("Shield", 3)]),
    (26, "Responsive", "Responsive", "Common", ["ShieldProjector"], [("Repair", 3)]),
    (27, "Expanded", "Expanded", "Common", ["ShipReactor"], [("Capacitor", 3)]),
    (28, "CleanBurning", "Clean-Burning", "Common", ["ShipReactor"], [("CapacitorRegen", 3)]),
    (29, "Hardened", "Hardened", "Common", ["HullFrame"], [("Defense", 3)]),
    (30, "Shielded", "Shielded", "Common", ["HullFrame"], [("Resistance", 3)]),
    (31, "Nimble", "Nimble", "Common", ["HullFrame", "ThrusterAssembly", "SensorSuite"], [("Evasion", 2)]),
    (32, "Streamlined", "Streamlined", "Common", ["ThrusterAssembly"], [("Speed", 1)]),
    (33, "WideBore", "Wide-Bore", "Common", ["ExtractionHead"], [("Yield", 1)]),
    (34, "Keen", "Keen", "Common", ["ExtractionHead"], [("RareFind", 3)]),
    (41, "SithTuned", "Sith-tuned", "Rare", ["EnergyEmitter", "OrdnanceAssembly"], [("Damage", 5), ("Accuracy", 5)]),
    (42, "MandalorianForged", "Mandalorian-forged", "Rare", ["HullFrame"], [("Hull", 5), ("Resistance", 5), ("Defense", 5)]),
    (43, "RepublicGrade", "Republic-grade", "Rare", ["ShieldProjector"], [("Shield", 5), ("Repair", 5)]),
    (44, "CorellianModified", "Corellian-modified", "Rare", ["ThrusterAssembly"], [("Speed", 2), ("Evasion", 3)]),
    (45, "HuttRigged", "Hutt-rigged", "Rare", ["ShipReactor"], [("Capacitor", 5), ("CapacitorRegen", 5)]),
    (46, "ChissCalibrated", "Chiss-calibrated", "Rare", ["SensorSuite"], [("Accuracy", 5), ("CapacitorCost", -20)]),
    (47, "CzerkaPattern", "Czerka-pattern", "Rare", ["ExtractionHead"], [("Yield", 2), ("RareFind", 5)]),
]

# Salvage odds per wreck difficulty (plan sections 12 and 14 give none; draft): the chance a salvage cycle also yields
# one salvaged component of the wreck's tier (1-5), and the chance that component's trait is rare. Every salvaged
# component carries a trait. Trait Hunter adds its rank's points to the rare chance.
SALVAGED_COMPONENT_PERCENT = {"Normal": 10, "Elite": 20, "Boss": 30}
RARE_TRAIT_PERCENT = {"Normal": 5, "Elite": 10, "Boss": 20}
TRAIT_HUNTER = [10, 20]


def effect_text(effects):
    parts = []
    for stat, value in effects:
        sign = "+" if value > 0 else ""
        unit = "%" if stat in PERCENT_STATS or stat == "Speed" else ""
        parts.append(f"{sign}{value}{unit} {STAT_TEXT[stat]}")
    return ", ".join(parts)


def traits_for(family, kind):
    return [t for t in TRAITS if family in t[4] and t[3] == kind]


def components_for_recipe(family, tier):
    """The two materials of one component."""
    for f in FAMILIES:
        if f[1] == family:
            return [(f[5](tier), 1), (f[6](tier), 1)]
    raise KeyError(family)


def family(enum):
    return next(f for f in FAMILIES if f[1] == enum)
