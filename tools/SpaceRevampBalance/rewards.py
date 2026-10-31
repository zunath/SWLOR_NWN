"""Space revamp rewards (plan sections 7, 8 and 14, Appendices C and D): the named rank-50 modules, Hull Designs and
their Design Schematics, Ship Schematics, ammo variants, the Refit fee, the salvage rare-find rewards and the research
hook for prototype tech. catalog.py turns this into C# and item blueprints; bible_tabs.py writes the Rewards (Revamp)
tab and bible_recipes.py the recipe rows. Draft numbers, tune in testing.
"""
import components as comp

# W9b's RecipeType block (named modules, rare components, ammo variants), after the component recipes (7358-7487)
# and the Probe Launcher (W12a, 7488-7497).
RECIPE_START = 7498

# ---------------------------------------------------------------------------------------------------------------
# Named rank-50 modules (plan sections 8 and 14): one per main family, the ground Chiro pattern (level-52 recipe
# unlocked by a Module Schematic, 2 enhancement slots, research cost +20%). Each number is the family's rank-45 number
# plus NAMED_STEP_SHARE of its rank 40 -> 45 step (at least +1 where the step grows it; numbers the ladder does not grow,
# and penalties, stay as they are), Attack and Accuracy included. A full rank step is worth about 1.3x kill speed
# (checks.py section M), so the share keeps a full named fit within 1.15x of a rank-45 fit. Each takes its family's
# tier 5 component twice, a Thorilide Core and an Aurodium Plating on top of twice the rank-45 bill's refined ore and
# electronics.
# ---------------------------------------------------------------------------------------------------------------
NAMED_RANK = 50
NAMED_STEP_SHARE = 0.3
# A named fit's kill-speed gain over a rank-45 fit may be at most this (or the rank step, if smaller).
NAMED_GAIN_CAP = 1.15
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


def named_value(rank45, rank40):
    """
    A named number: the rank-45 number plus NAMED_STEP_SHARE of its rank 40 -> 45 step, at least +1 (+0.01 for a
    fraction) when the step grows it. Penalties, flags and numbers the ladder does not grow stay at the rank-45 value.
    """
    if isinstance(rank45, bool) or not isinstance(rank45, (int, float)) or rank45 <= 0:
        return rank45
    step = rank45 - rank40
    if step <= 0:
        return rank45
    if isinstance(rank45, float) and not float(rank45).is_integer():
        return round(rank45 + max(0.01, step * NAMED_STEP_SHARE), 2)
    return int(rank45) + max(1, int(step * NAMED_STEP_SHARE + 0.5))


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
# The direct ship transfer fee by hull size (section 17 "Ship transfer: fee by hull size"; the gaps pass W11d draft): the
# same starport paperwork scale as the Refit fee. A market sale pays the market tax instead.
TRANSFER_FEE = dict(REFIT_FEE)
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

# ---------------------------------------------------------------------------------------------------------------
# Aces (W9c, plan sections 11 and 14): one named rare-spawn pilot per orbit (the ten orbit areas) and one per W6c zone,
# built like a ground named rare elite (skills/swlor-rare-spawn-generation): an Elite of its area's top faction level
# (tier x 10, the level of the tier's Gunship and Defense Platform) in a role ship, converted by model.npc_ship like every
# space NPC (npc_catalog.py), so it takes the Elite hull class (Medium) and the Elite HP and damage curves. Each flies a
# Hull Design (an Appendix C launch skin) no other creature uses, and its unique loot table (SPACE_ACE_<KEY>, IsRare,
# every entry rare) holds that design's Design Schematic, one named module's Module Schematic, its trophy, a component of
# its tier with a rare trait, and its maker's manufacturer component. Its wreck carries a data core (the SPACE_ACE
# creature local the space slicing update reads).
# ---------------------------------------------------------------------------------------------------------------
ACE_LEVEL_PER_TIER = 10
ACE_DIFFICULTY = "Elite"
# The ground rare share: a weight-1 rare beside about 120 weight of normal spawns is 0.8% of spawn rolls (Viscara's
# Harrek Voss beside the Mandalorian Raiders' 120). An open orbit's table weighs 75,648, so an ace takes ACE_SPAWN_WEIGHT
# (0.79%); a two-tier zone weighs about twice that and doubles the weight.
ACE_SPAWN_WEIGHT = 600
ACE_SPAWN_SHARE = (0.007, 0.009)
# The ace's own table: one guaranteed roll and one more at ACE_SECOND_ROLL percent, beside the tier's elite table.
ACE_SECOND_ROLL = 25
ACE_ELITE_TABLE = "SPACE_ELITE_T{tier}, 66, 5"
# rift_key: the Rift Key of the ace's tier (W15b, events.key_for_tier; tier 6 aces drop the tier 5.5 key).
ACE_LOOT_WEIGHTS = dict(design=30, module=10, trophy=20, rare_component=25, maker_component=15, rift_key=15)
ACE_ITEM_TAGS = {"Fighter": "NPC_Fighter", "Sniper": "NPC_Fighter", "Interceptor": "NPC_Interceptor",
                 "Electronic Warfare": "NPC_Interceptor", "Bomber": "NPC_Bomber", "Minelayer": "NPC_Bomber"}
FACTION_NAMES = {1: "Septra Khanate", 2: "Krakenborne", 3: "Snowdrift Syndicate", 4: "Clan Zel Arun", 5: "Red Hand",
                 6: "Imperial"}

# Trophies: one structure item per ace (structure_04NN, the property structure system's resref pattern, model 202 like
# every structure item), placing an existing placeable no other structure uses. Named for the piece, not the pilot.
TROPHY_START = 437
TROPHY_COST = 500

# key, tag, pilot name, short name (achievement), area resref and text, spawn table, tier, role, Hull Design appearance,
# manufacturer, named module family, rare component (family, trait), trophy (name, placeable, text), lore.
ACES = [
    dict(key="Viscara", tag="ace_viscara", name="Vesh Talloran", short="Talloran", area="viscaraorbit",
         area_text="Viscara orbit", table="SPACE_VISCARA_ORBIT", tier=1, role="Interceptor", design=10144,
         maker="Corellian", named="Thrusters", rare=("ThrusterAssembly", "CorellianModified"),
         trophy=("Mounted Thruster Bell", "_mdrn_pl_shipp10",
                 "A scorched thruster bell cut from a wrecked interceptor and set on a display stand."),
         lore="The Septra Khanate's fastest pilot runs down stragglers over Viscara in a stolen Condor, its engines tuned "
              "well past their safety limits."),
    dict(key="SmugglersMoon", tag="ace_smuggler", name="Grizzik the Collector", short="Grizzik", area="narshadorbit",
         area_text="Smuggler's Moon orbit", table="SPACE_NARSHADDAA_ORBIT", tier=1, role="Fighter", design=10190,
         maker="Hutt", named="CapacitorBattery", rare=("ShipReactor", "HuttRigged"),
         trophy=("Salvaged Engine Coil", "_mdrn_pl_shipp04",
                 "A heavy engine coil pulled from a bounty hunter's fighter, still marked with Hutt cartel sigils."),
         lore="A Rodian bounty hunter on the Hutt payroll who collects debts, cargo and starfighters from anyone who "
              "lingers near Smuggler's Moon."),
    dict(key="MonCala", tag="ace_moncala", name="Captain Ilsa Morrow", short="Morrow", area="moncalaorbit",
         area_text="Mon Cala orbit", table="SPACE_MONCALA_ORBIT", tier=2, role="Bomber", design=2005,
         maker="Republic", named="TorpedoLauncher", rare=("OrdnanceAssembly", "SithTuned"),
         trophy=("Scorched Thruster Nozzle", "_mdrn_pl_shipp11",
                 "A blackened thruster nozzle from a torpedo bomber, mounted as a wall piece."),
         lore="A Krakenborne raider who flies a captured Republic bomber and sinks freighters off Mon Cala with its own "
              "torpedoes."),
    dict(key="ViscaraBelt", tag="ace_visbelt", name="Rook Varrus", short="Varrus", area="space_viscbelt",
         area_text="the Viscara Asteroid Belt", table="SPACE_VISCARA_BELT", tier=2, role="Sniper", design=10200,
         maker="Czerka", named="BeamCannon", rare=("ExtractionHead", "CzerkaPattern"),
         trophy=("Wrecked Avionics Rack", "_mdrn_pl_debri05",
                 "A rack of burned-out avionics salvaged from a sniper's cockpit, kept as a curiosity."),
         lore="A Krakenborne marksman who hides among the Durelium rocks and picks off mining barges from extreme range."),
    dict(key="Hutlar", tag="ace_hutlar", name="Kael Dunmor", short="Dunmor", area="hutlar_orbit",
         area_text="Hutlar orbit", table="SPACE_HUTLAR_ORBIT", tier=3, role="Interceptor", design=10191,
         maker="Chiss", named="TargetingComputer", rare=("SensorSuite", "ChissCalibrated"),
         trophy=("Turbine Housing Trophy", "_mdrn_pl_shipp12",
                 "A frost-scarred turbine housing from a Chiss-built interceptor, polished for display."),
         lore="The Snowdrift Syndicate's best pilot, called Frostbite, hunts over Hutlar's ice in a Chiss Hyperion he "
              "never explains how he got."),
    dict(key="Tatooine", tag="ace_tatooine", name="Torvak Zel", short="Torvak", area="tatooineorbit",
         area_text="Tatooine orbit", table="SPACE_TATOOINE_ORBIT", tier=4, role="Fighter", design=10156,
         maker="Mandalorian", named="HullPlating", rare=("HullFrame", "MandalorianForged"),
         trophy=("Captured Hyperdrive Core", "_mdrn_pl_engineh",
                 "An opened hyperdrive core from a Mandalorian war droid, its casing dented by cannon fire."),
         lore="A champion of Clan Zel Arun who flies a Basilisk war droid over Tatooine and challenges any pilot worth "
              "the fight."),
    dict(key="MidRimNebula", tag="ace_midrimneb", name="Siv Orrin", short="Orrin", area="space_midrimneb",
         area_text="the Mid Rim Nebula", table="SPACE_MIDRIM_NEBULA", tier=4, role="Electronic Warfare", design=10088,
         maker="Mandalorian", named="AdaptivePlating", rare=("SensorSuite", "ChissCalibrated"),
         trophy=("Wrecked Sensor Array", "_mdrn_pl_debri06",
                 "The twisted remains of a jamming array, salvaged from a ship that hid in a nebula's gas."),
         lore="A Clan Zel Arun scout who blinds convoys inside the Tibanna clouds before the clan's gunships close in."),
    dict(key="Dathomir", tag="ace_dathomir", name="Vexa Thorne", short="Thorne", area="space_dathomir",
         area_text="Dathomir orbit", table="SPACE_DATHOMIR_ORBIT", tier=5, role="Fighter", design=2008,
         maker="Sith", named="LaserCannon", rare=("EnergyEmitter", "SithTuned"),
         trophy=("Wrecked Weapon Mount", "_mdrn_pl_debri07",
                 "A laser cannon mount torn from a Sith fighter's wing, its emitter cracked and cold."),
         lore="The Red Hand's deadliest fighter pilot, who flies a Sith fighter paid for in blood over Dathomir."),
    dict(key="Dantooine", tag="ace_dantooine", name="Mako Drell", short="Drell", area="dantooineorbit",
         area_text="Dantooine orbit", table="SPACE_DANTOOINE_ORBIT", tier=5, role="Sniper", design=2003,
         maker="Republic", named="ShieldBooster", rare=("ShieldProjector", "RepublicGrade"),
         trophy=("Burned-Out Power Generator", "_mdrn_pl_genert2",
                 "A ship's power generator, burned out from the inside, kept as a reminder of a hard fight."),
         lore="A Red Hand sniper who stalks the traffic over Dantooine in a modified Republic scout."),
    dict(key="MidRim1", tag="ace_midrim1", name="Harl Quessen", short="Quessen", area="space_midrim1",
         area_text="the Mid Rim nebula", table="SPACE_MIDRIM_1", tier=5, role="Interceptor", design=10087,
         maker="Corellian", named="IonCannon", rare=("ThrusterAssembly", "CorellianModified"),
         trophy=("Shattered Engine Turbine", "_mdrn_pl_engine1",
                 "A shattered engine turbine from a stripped-down interceptor, mounted on a stand."),
         lore="A Red Hand interceptor pilot who waits in the Mid Rim nebula for ships running the lane to Viscara."),
    dict(key="KorribanDebris", tag="ace_korrdebris", name="Scrapper VX-9", short="Scrapper", area="space_korrdebris",
         area_text="the Korriban Debris Field", table="SPACE_KORRIBAN_DEBRIS", tier=5, role="Minelayer", design=10216,
         maker="Hutt", named="HullRepairer", rare=("ShipReactor", "HuttRigged"),
         trophy=("Mounted Engine Control Block", "_mdrn_pl_engctr1",
                 "An engine control block salvaged from a droid gunship, wired to blink on command."),
         lore="A salvage droid that rebuilt itself into a gunship and now seeds the debris field with mines."),
    dict(key="MidRim2", tag="ace_midrim2", name="Commander Raine Kardos", short="Kardos", area="space_midrim2",
         area_text="the Outer Rim approach", table="SPACE_MIDRIM_2", tier=6, role="Fighter", design=10090,
         maker="Sith", named="MissileLauncher", rare=("EnergyEmitter", "SithTuned"),
         trophy=("Captured Flight Recorder", "_mdrn_pl_holocon",
                 "An Imperial flight recorder that still projects the last seconds of its fighter's final sortie."),
         lore="An Imperial squadron commander who guards the approach to Korriban and has never lost a duel."),
    dict(key="Korriban", tag="ace_korriban", name="Lord Malakor Venn", short="Malakor", area="space_korriban",
         area_text="Korriban orbit", table="SPACE_KORRIBAN_ORBIT", tier=6, role="Bomber", design=2007,
         maker="Sith", named="ShieldHardener", rare=("OrdnanceAssembly", "SithTuned"),
         trophy=("Holo Star Chart", "_mdrn_pl_holod04",
                 "A holographic chart of the Sith worlds, taken from an Imperial bomber's navigation computer."),
         lore="A Sith Lord who flies his own bomber over Korriban and hunts the ships that dare come close."),
]

# Wild Space aces (W11o; plan section 23 and Appendix D's "Wild Space data sites and aces"): one per Wild Space system, an
# Elite of the Rift Raiders' level (wildspace.RIFT_RAIDER_LEVEL, 52) copied from the Rift Raider gunship, flying a Hull
# Design no other creature uses. The tier-indexed parts use the content tier 5 (rare component, legacy ship item), the
# tables are the Wild Space ones (SPACE_ELITE_WS and the tier 5.5 Rift Key), the maker is the Rift Raiders (their
# manufacturer component and wreck salvage), and the Module Schematic slot holds Prototype Tech (a Wild Space source)
# instead: 13 of the 16 named Module Schematics already belong to one ace each. Their achievements follow the trade ones.
WILD_ACE_LEVEL = 52
WILD_ACE_ELITE_TABLE = "SPACE_ELITE_WS, 66, 5"
WILD_ACES = [
    dict(key="VeshPulsar", tag="ace_wspulsar", name="Kessa Vane", short="Vane", area="space_wspulsar",
         area_text="the Vesh Pulsar", table="SPACE_WILD_SPACE_PULSAR", tier=5, role="Interceptor", design=10192,
         maker="Rift Raider", named=None, rare=("ThrusterAssembly", "CorellianModified"),
         trophy=("Pulsar-Scorched Hatch", "_mdrn_pl_shipp05",
                 "A hexagonal hatch cut from a raider interceptor, its paint burned away by a pulsar's sweep."),
         lore="A Rift Raider who times her strikes to the pulsar's beat and is gone before a pilot's sensors recover."),
    dict(key="TheMaw", tag="ace_wsblkhole", name="Old Mother Garrow", short="Garrow", area="space_wsblkhole",
         area_text="The Maw", table="SPACE_WILD_SPACE_BLACK_HOLE", tier=5, role="Bomber", design=10079,
         maker="Rift Raider", named=None, rare=("OrdnanceAssembly", "SithTuned"),
         trophy=("Gravity-Torn Turbine", "_mdrn_pl_engine2",
                 "A turbine bent out of true by the pull of a black hole, salvaged from a raider bomber."),
         lore="The oldest bomber pilot of the Rift Raiders, who herds ships toward the black hole's edge with her torpedoes."),
    dict(key="IonVeil", tag="ace_wsionstorm", name="Jex Turlo", short="Turlo", area="space_wsionstorm",
         area_text="the Ion Veil", table="SPACE_WILD_SPACE_ION_STORM", tier=5, role="Electronic Warfare", design=2001,
         maker="Rift Raider", named=None, rare=("SensorSuite", "ChissCalibrated"),
         trophy=("Ion-Fused Control Block", "_mdrn_pl_engine3",
                 "A control block whose tubes were fused solid by an ion storm, pulled from a jamming ship."),
         lore="A Rift Raider slicer who rides the ion storm in a stolen Republic scout and blinds whatever wanders in."),
    dict(key="KorvaxMagnetar", tag="ace_wsmagnetar", name="Brann Halvek", short="Halvek", area="space_wsmagnetar",
         area_text="the Korvax Magnetar", table="SPACE_WILD_SPACE_MAGNETAR", tier=5, role="Sniper", design=10210,
         maker="Rift Raider", named=None, rare=("EnergyEmitter", "SithTuned"),
         trophy=("Magnetized Hull Panel", "_mdrn_pl_debri09",
                 "A blue hull panel still faintly magnetized, cut from a sniper's ship near a magnetar."),
         lore="A Rift Raider marksman who lets the magnetar drag his targets' shields down before he fires."),
    dict(key="EmberGiant", tag="ace_wsredgiant", name="Captain Seris Kol", short="Kol", area="space_wsredgiant",
         area_text="the Ember Giant", table="SPACE_WILD_SPACE_RED_GIANT", tier=5, role="Fighter", design=10211,
         maker="Rift Raider", named=None, rare=("HullFrame", "MandalorianForged"),
         trophy=("Ember-Scarred Engine Control", "_mdrn_pl_engctr2",
                 "An engine control console scarred by a red giant's heat, taken from a raider fighter."),
         lore="A Rift Raider captain who duels in the red giant's glare, where only her own fighter's shields hold."),
    dict(key="ShipGraveyard", tag="ace_wsgraveyrd", name="Warlord Tasker Vohl", short="Vohl", area="space_wsgraveyrd",
         area_text="the Ship Graveyard", table="SPACE_WILD_SPACE_SHIP_GRAVEYARD", tier=5, role="Fighter", design=10195,
         maker="Rift Raider", named=None, rare=("ShieldProjector", "RepublicGrade"),
         trophy=("Graveyard Flagship Generator", "_mdrn_pl_genert3",
                 "A wrecked generator from a Rift Raider flagship, pulled from the ship graveyard as proof of the kill."),
         lore="The Rift Raider warlord who claims the Ship Graveyard and every wreck in it, flying the coalition's heaviest raider."),
]
for _ace in WILD_ACES:
    _ace["wild"] = True
ACES = ACES + WILD_ACES


def ace_level(ace):
    return WILD_ACE_LEVEL if ace.get("wild") else ace["tier"] * ACE_LEVEL_PER_TIER


def ace_elite_table(ace):
    return WILD_ACE_ELITE_TABLE if ace.get("wild") else ACE_ELITE_TABLE.format(tier=ace["tier"])


def ace_loot_table(ace):
    return "SPACE_ACE_" + "".join(("_" + ch if ch.isupper() and i else ch) for i, ch in enumerate(ace["key"])).upper()


def trophy_resref(index):
    return f"structure_{TROPHY_START + index:04d}"


def trophy_enum(name):
    return "".join(ch for ch in name.title() if ch.isalnum())


# ---------------------------------------------------------------------------------------------------------------
# Space achievements (W9c, plan section 14), after the ground achievements (the last is 186). The notification window
# fits about 24 characters of name.
# ---------------------------------------------------------------------------------------------------------------
ACHIEVEMENT_START = 188
TIER_KILL_ACHIEVEMENTS = [
    (1, "SpaceKillTier1", "Khanate Hunter"),
    (2, "SpaceKillTier2", "Krakenborne Hunter"),
    (3, "SpaceKillTier3", "Syndicate Hunter"),
    (4, "SpaceKillTier4", "Clan Hunter"),
    (5, "SpaceKillTier5", "Red Hand Hunter"),
    (6, "SpaceKillTier6", "Imperial Hunter"),
]
# Credits earned from trade: commodity sale profit and freight contract fees.
TRADE_ACHIEVEMENTS = [(10000, "SpaceTradeEarnings1", "Trader"), (100000, "SpaceTradeEarnings2", "Merchant"),
                      (1000000, "SpaceTradeEarnings3", "Trade Baron")]
# Signature type, enum, name, description. The rift waits for Wild Space (W12c).
SITE_ACHIEVEMENTS = [
    ("CombatSite", "SpaceSiteCombat", "Signal Hunter", "Jumped to a combat site found by scanning."),
    ("DataSite", "SpaceSiteData", "Data Diver", "Jumped to a data site found by scanning."),
    ("CacheSite", "SpaceSiteCache", "Cache Finder", "Jumped to a cache site found by scanning."),
    ("GasSite", "SpaceSiteGas", "Cloud Chaser", "Jumped to a gas site found by scanning."),
    ("Derelict", "SpaceSiteDerelict", "Derelict Delver", "Jumped to a derelict found by scanning."),
    ("Rift", "SpaceSiteRift", "Rift Walker", "Jumped to a rift found by scanning."),
]


# W11o: the interiors that still had no exploration achievement (plan section 14) and the first crewed kill, appended
# after the trade milestones so no earlier id moves. Each interior's area carries EXPLORE_ACHIEVEMENT_ID (Module git
# VarTable), as the other starship interiors do; a crewed kill is a kill by a ship with a player crewmate at a station.
INTERIOR_ACHIEVEMENTS = [
    ("ship_fight_v", "ExploreStarshipFighter", "Explore Fighter Ship", "Explore a Fighter class starship."),
    ("ship_corv_v", "ExploreStarshipCorvette", "Explore Corvette Ship", "Explore a Corvette class starship."),
    ("ship_basi_v", "ExploreStarshipBasilisk", "Explore Basilisk Ship", "Explore a Basilisk class starship."),
]
CREWED_KILL_ACHIEVEMENT = ("SpaceCrewedKill", "Crewed Kill", "Destroyed a ship in space with a crewmate at a station.")

# ---------------------------------------------------------------------------------------------------------------
# Space decor (W11o, plan section 14 "interior trophies and decor"): Fabrication furniture for homes, city buildings,
# stations and starship interiors. Each places an existing static placeable no other structure uses, and copies the
# bill, level and tier of the Electronics furniture recipe named last (FurnitureRecipes), so no number is new: one
# item storage slot and two Structure enhancement slots like every crafted furniture piece.
# StructureType after the station buildings (5018-5021), RecipeType after the capital parts (7793-7795).
# ---------------------------------------------------------------------------------------------------------------
DECOR_STRUCTURE_START = 5022
DECOR_RECIPE_START = 7796
DECOR_COST = 100   # like every crafted furniture item (structure_0162)
DECOR_STORAGE = 1
DECOR_ENHANCEMENT_SLOTS = 2
# enum, name, placeable, description, level, perk tier, components, the furniture recipe it copies
DECOR = [
    ("CaptainsHolotable", "Captain's Holotable", "swlor_1268",
     "A command holotable that projects star charts and fleet positions above the captain's console.",
     49, 5, [("ref_jasioclase", 5), ("elec_high", 5)], "ConsoleCircularCommand"),
    ("StarMapPedestal", "Star Map Pedestal", "_mdrn_pl_basestm",
     "A navigator's pedestal that holds the hyperspace routes a ship has charted.",
     40, 4, [("elec_imperfect", 6), ("ref_keromber", 3)], "HoloDisplay"),
    ("HoloStarGlobe", "Holo Star Globe", "_mdrn_pl_holgl7l",
     "A large holographic star, a favorite centerpiece of spacers' quarters.",
     45, 5, [("elec_imperfect", 3), ("ref_jasioclase", 2)], "HoloProjector2"),
]


def decor_rows():
    """Every space decor piece with its StructureType, RecipeType and item resref."""
    out = []
    for i, (enum, name, placeable, text, level, tier, components, copies) in enumerate(DECOR):
        sid = DECOR_STRUCTURE_START + i
        out.append(dict(Enum=enum, Name=name, Placeable=placeable, Text=text, Level=level, Tier=tier, Components=components,
                        Copies=copies, StructureId=sid, RecipeId=DECOR_RECIPE_START + i, Resref=f"structure_{sid}"))
    return out


def ace_achievement(ace):
    return f"SpaceAce{ace['key']}"


def achievements():
    """Every space achievement: (id, enum, name, description)."""
    out = []
    for tier, enum, name in TIER_KILL_ACHIEVEMENTS:
        faction = FACTION_NAMES[tier]
        article = "an" if faction[0] in "AEIOU" else "a"
        out.append((enum, name, f"Destroyed {article} {faction} ship (tier {tier}) in space."))
    out.append(("SpaceCapitalKill", "Capital Breaker", "Helped destroy a capital ship in space."))
    for ace in ACES:
        if not ace.get("wild"):
            out.append((ace_achievement(ace), f"{ace['short']} Downed", f"Helped destroy the ace {ace['name']} in {ace['area_text']}."))
    out.append(("SpaceNamedModule", "Named and Fitted", "Fitted a named rank-50 module to a ship."))
    for _site, enum, name, text in SITE_ACHIEVEMENTS:
        out.append((enum, name, text))
    out.append(("SpaceWildSpace", "Beyond the Map", "Entered Wild Space."))
    for credits, enum, name in TRADE_ACHIEVEMENTS:
        out.append((enum, name, f"Earned {credits:,} credits of trade profit and freight fees."))
    # W11o: the Wild Space aces, after the last W9c-W12c achievement so no existing id moves.
    for ace in WILD_ACES:
        out.append((ace_achievement(ace), f"{ace['short']} Downed", f"Helped destroy the ace {ace['name']} in {ace['area_text']}."))
    for _area, enum, name, text in INTERIOR_ACHIEVEMENTS:
        out.append((enum, name, text))
    out.append(CREWED_KILL_ACHIEVEMENT)
    return [(ACHIEVEMENT_START + i, enum, name, text) for i, (enum, name, text) in enumerate(out)]
