"""Space revamp catalog: every new ship module and ship hull, generated from model.py.

usage: python catalog.py            (writes the generated files listed in WRITES)
       python catalog.py --check    (exit 1 when a generated file is out of date)

Outputs:
- the C# catalog data the module, ship and recipe definitions read
- one Module/uti blueprint per module item and ship deed
"""
import copy
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import model as m  # noqa: E402
import npc_catalog  # noqa: E402
import trade  # noqa: E402

ROOT = m.REPO
ROMAN = ["I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X"]
CAPITAL_STEPS = [40, 45]
SKILL = {"Piloting": "SkillType.Piloting", "Gathering": "SkillType.Gathering"}
RECIPE_TYPE_START = 7000

# Existing artwork each family reuses (ShipItemIconBindings.csv role): BaseItem, inventory model, action icon.
ART = {
    "combat_laser": (513, 239, "iit_smcmblaser"), "ion_cannon": (513, 230, "iit_smioncann"),
    "beam_cannon": (513, 250, "iit_smbeamcann"), "missile_launcher": (513, 228, "iit_smmsllaunch"),
    "bomb_launcher": (513, 227, "iit_smbomblaunch"), "aux_blaster": (513, 225, "iit_smauxblast"),
    "turbolaser": (513, 217, "iit_smturbolas"), "quad_laser": (516, 252, "iit_smquadlaser"),
    "storm_cannon": (516, 185, "iit_smstormcann"), "laser_battery": (522, 243, "iit_smlasbattery"),
    "concussion_launcher": (513, 251, "iit_smconclaunch"), "mining_laser": (513, 229, "iit_smminelaser"),
    "strip_miner": (513, 244, "iit_smstripminer"), "shield_booster": (513, 224, "iit_smshldboost"),
    "shield_repairer": (513, 223, "iit_smshldrepair"), "hull_repairer": (513, 232, "iit_smhullrepair"),
    "skirmisher_config": (522, 253, "iit_smskirmcfg"), "capacitor_booster": (513, 248, "iit_smcapboost"),
    "hypermatter_injector": (513, 245, "iit_smhminject"), "targeting_system": (513, 220, "iit_smtgtsystem"),
    "hull_booster": (513, 234, "iit_smhullboost"), "mirrored_plating": (522, 244, "iit_smmirrarmor"),
    "ion_shielding": (522, 245, "iit_smionarmor"), "reactive_armor": (522, 247, "iit_smreactarmor"),
    "durasteel_plating": (522, 246, "iit_smdurarmor"), "fuel_tank": (516, 193, "iit_smfueltank"),
    "evasion_booster": (513, 237, "iit_smevaboost"), "thermal_amplifier": (513, 219, "iit_smthermamp"),
    "em_amplifier": (513, 238, "iit_smemamp"), "explosive_amplifier": (513, 236, "iit_smexpamp"),
    "bulwark_generator": (513, 249, "iit_smbulwark"), "repair_field": (513, 226, "iit_smrepfield"),
    "ewar": (513, 246, "iit_smewar"), "advanced_thrusters": (513, 241, "iit_smadvthrust"),
    "redundant_shields": (516, 251, "iit_smredshld"), "reinforced_plating": (516, 253, "iit_smreinplate"),
    "targeting_array": (513, 242, "iit_smtgtarray"), "weapons_computer": (513, 240, "iit_smwpncomp"),
    "power_diverter": (513, 243, "iit_smpowdivert"),
    # New artwork for the salvage update (W6a), in the ESS9 bank.
    "salvager": (523, 226, "iit_smsalvager"), "gas_harvester": (523, 227, "iit_smgasharvest"),
    # The trade risk update (W7b), in the ESS9 bank.
    "smuggling_compartment": (523, 228, "iit_smsmuggle"),
}

W = m.WEAPONS
CM = m.T["capital_weapon_mult"]


def weapon_stats(name, dmg_mult=1.0, **override):
    w = {**W[name], **override}
    def f(r):
        mult = w["dmg"] * (CM if w.get("capital") else 1.0) * dmg_mult
        return dict(WeaponFamily=w["family"], DamageType=w["type"], Dmg=max(1, round(m.GROUND_DMG[r] * mult)),
                    Attack=2 * r, Accuracy=2 * r, ShieldBypass=w.get("bypass", False), IgnoreSizePenalty=w.get("ignore_size", False),
                    ShieldDamageMultiplier=w["shield"], FixedSize="Capital" if w.get("capital") else w.get("fixed_size"))
    return f


def p(**kw):
    return lambda r: {k: (v(r) if callable(v) else v) for k, v in kw.items()}


HP = m.HP_STEP
REP = m.REPAIR_STEP
# key, name, short, tag prefix, row, skill, use, cycle, capacitor, range, art, capital, stats(r), description(r, s)
FAMILIES = [
    ("LaserCannon", "Laser Cannon", "Laser", "sm_laser", "High", "Piloting", "Toggle", 3.0, 3, 40, "combat_laser", False,
     weapon_stats("Laser Cannon"), lambda r, s: f"Fires {s['Dmg']} thermal DMG at your target every 3 seconds while on."),
    ("IonCannon", "Ion Cannon", "Ion", "sm_ion", "High", "Piloting", "Toggle", 3.0, 3, 40, "ion_cannon", False,
     weapon_stats("Ion Cannon"), lambda r, s: f"Fires {s['Dmg']} EM DMG every 3 seconds while on. Shields take 50% more; hull hits slow the target."),
    ("BeamCannon", "Beam Cannon", "Beam", "sm_beam", "High", "Piloting", "Toggle", 3.0, 5, 15, "beam_cannon", False,
     weapon_stats("Beam Cannon"), lambda r, s: f"Burns your target for {s['Dmg']} thermal DMG every 3 seconds while on. Short range."),
    ("MissileLauncher", "Missile Launcher", "Missile", "sm_missl", "High", "Piloting", "Toggle", 3.0, 2, 50, "missile_launcher", False,
     weapon_stats("Missile Launcher"), lambda r, s: f"Fires a missile for {s['Dmg']} explosive DMG every 3 seconds while on; +20% against a target with no shield. Uses missiles; damage scales with MGT and accuracy with PER."),
    ("TorpedoLauncher", "Torpedo Launcher", "Torpedo", "sm_torp", "High", "Piloting", "OnePress", 18.0, 10, 40, "bomb_launcher", False,
     weapon_stats("Torpedo Launcher"), lambda r, s: f"Launches a torpedo for {s['Dmg']} explosive DMG that bypasses shields and ignores size. Uses a torpedo; 18 second recast."),
    ("PointDefenseTurret", "Point-Defense Turret", "Point-Def", "sm_pdef", "High", "Piloting", "Toggle", 2.5, 2, 25, "aux_blaster", False,
     weapon_stats("Point-Defense Turret"), lambda r, s: f"Fires {s['Dmg']} thermal DMG every 2.5 seconds while on. Always fires as Light; +20% against Small hulls."),
    ("Turbolaser", "Turbolaser", "Turbo", "sm_turbo", "High", "Piloting", "Toggle", 4.0, 6, 50, "turbolaser", True,
     weapon_stats("Turbolaser"), lambda r, s: f"Capital weapon: {s['Dmg']} thermal DMG every 4 seconds while on."),
    ("QuadLaser", "Quad Laser", "Quad", "sm_quad", "High", "Piloting", "Toggle", 2.0, 3, 40, "quad_laser", True,
     weapon_stats("Quad Laser"), lambda r, s: f"Capital weapon: {s['Dmg']} thermal DMG every 2 seconds while on."),
    ("StormCannon", "Storm Cannon", "Storm", "sm_storm", "High", "Piloting", "Toggle", 4.0, 6, 40, "storm_cannon", True,
     weapon_stats("Storm Cannon"), lambda r, s: f"Capital ion cannon: {s['Dmg']} EM DMG every 4 seconds while on. Shields take 50% more; hull hits slow the target."),
    ("LaserBattery", "Laser Battery", "Battery", "sm_lbatt", "High", "Piloting", "OnePress", 24.0, 20, 40, "laser_battery", True,
     weapon_stats("Turbolaser", 2.0), lambda r, s: f"Capital volley: {s['Dmg']} thermal DMG to your target and up to 4 other enemies within 15 m. 24 second recast."),
    ("AssaultConcussionMissile", "Assault Concussion Missile", "ACM", "sm_acm", "High", "Piloting", "OnePress", 30.0, 15, 50, "concussion_launcher", True,
     weapon_stats("Turbolaser", 6.0, family="Launcher", type="Explosive", bypass=True), lambda r, s: f"Capital missile: {s['Dmg']} explosive DMG that bypasses shields. Uses a concussion missile; 30 second recast."),
    ("MiningLaser", "Mining Laser", "Mining", "sm_mine", "High", "Gathering", "Toggle", 6.0, 3, 15, "mining_laser", False,
     p(MiningTier=lambda r: r // 10 + 1), lambda r, s: f"Mines asteroids up to tier {s['MiningTier']} every 6 seconds while on."),
    ("StripMiner", "Strip Miner", "Strip", "sm_strip", "High", "Gathering", "Toggle", 10.0, 8, 15, "strip_miner", False,
     p(MiningTier=lambda r: r // 10 + 1), lambda r, s: f"Large and Capital hulls only. Strips a whole asteroid up to tier {s['MiningTier']} and roots the ship while it runs."),
    ("ShieldBooster", "Shield Booster", "Sh Booster", "sm_shbst", "Mid", "Piloting", "Passive", 0, 0, 0, "shield_booster", False,
     p(Shield=lambda r: HP[r]), lambda r, s: f"+{s['Shield']} max shield (scales with hull size)."),
    ("ShieldRepairer", "Shield Repairer", "Sh Repair", "sm_shrep", "Mid", "Piloting", "Toggle", 4.0, 4, 30, "shield_repairer", False,
     p(Repair=lambda r: REP[r]), lambda r, s: f"Restores {s['Repair']} shield to its locked target, or you, every 4 seconds while on (scales with hull size)."),
    ("ShieldHardener", "Shield Hardener", "Hardener", "sm_hard", "Mid", "Piloting", "Toggle", 4.0, 4, 0, "skirmisher_config", False,
     p(ResistAll=lambda r: 10 + r // 5), lambda r, s: f"+{s['ResistAll']} to all resistances while on."),
    ("CapacitorBattery", "Capacitor Battery", "Cap Battery", "sm_capb", "Mid", "Piloting", "Passive", 0, 0, 0, "capacitor_booster", False,
     p(MaxCapacitor=lambda r: m.CAP_STEP[r]), lambda r, s: f"+{s['MaxCapacitor']} max capacitor (scales with hull size)."),
    ("HypermatterInjector", "Hypermatter Injector", "Injector", "sm_hyper", "Mid", "Piloting", "OnePress", 30.0, 0, 0, "hypermatter_injector", False,
     p(CapacitorRestore=lambda r: 30 + r), lambda r, s: f"Restores {s['CapacitorRestore']} capacitor (scales with hull size). Uses a fuel capsule; 30 second recast."),
    ("TargetingComputer", "Targeting Computer", "Tgt Computer", "sm_tcomp", "Mid", "Piloting", "Passive", 0, 0, 0, "targeting_system", False,
     p(ModuleAccuracy=lambda r: 2 + round(0.4 * r)), lambda r, s: f"+{s['ModuleAccuracy']} Accuracy. Only your best Targeting Computer counts."),
    ("HullPlating", "Hull Plating", "Plating", "sm_plate", "Low", "Piloting", "Passive", 0, 0, 0, "hull_booster", False,
     p(Hull=lambda r: HP[r], ModuleDefense=lambda r: round(1.8 * r)),
     lambda r, s: f"+{s['Hull']} max hull (scales with hull size) and +{s['ModuleDefense']} Defense. Only your best plating's Defense counts."),
    ("HullRepairer", "Hull Repairer", "Hu Repair", "sm_hrep", "Low", "Piloting", "Toggle", 4.0, 4, 30, "hull_repairer", False,
     p(Repair=lambda r: max(1, round(REP[r] * 0.8))), lambda r, s: f"Restores {s['Repair']} hull to its locked target, or you, every 4 seconds while on (scales with hull size)."),
    ("ThermalPlating", "Thermal Plating", "Th Plating", "sm_resth", "Low", "Piloting", "Passive", 0, 0, 0, "mirrored_plating", False,
     p(ResistThermal=lambda r: 10 + r // 3), lambda r, s: f"+{s['ResistThermal']} Thermal resistance."),
    ("EMPlating", "EM Plating", "EM Plating", "sm_resem", "Low", "Piloting", "Passive", 0, 0, 0, "ion_shielding", False,
     p(ResistEM=lambda r: 10 + r // 3), lambda r, s: f"+{s['ResistEM']} EM resistance."),
    ("ExplosivePlating", "Explosive Plating", "Ex Plating", "sm_resex", "Low", "Piloting", "Passive", 0, 0, 0, "reactive_armor", False,
     p(ResistExplosive=lambda r: 10 + r // 3), lambda r, s: f"+{s['ResistExplosive']} Explosive resistance."),
    ("AdaptivePlating", "Adaptive Plating", "Ad Plating", "sm_resall", "Low", "Piloting", "Passive", 0, 0, 0, "durasteel_plating", False,
     p(ResistAll=lambda r: 6 + r // 6, ModuleEvasion=-4), lambda r, s: f"+{s['ResistAll']} to all resistances; -4 Evasion."),
    ("CapacitorRegulator", "Capacitor Regulator", "Cap Regulator", "sm_capr", "Low", "Piloting", "Passive", 0, 0, 0, "fuel_tank", False,
     p(CapacitorRegen=lambda r: m.CAP_REGEN_STEP[r]), lambda r, s: f"+{s['CapacitorRegen']} capacitor per second (scales with hull size)."),
    ("Thrusters", "Thrusters", "Thrusters", "sm_thrust", "Low", "Piloting", "Passive", 0, 0, 0, "evasion_booster", False,
     p(ModuleEvasion=lambda r: 2 * r, SpeedPercent=5), lambda r, s: f"+{s['ModuleEvasion']} Evasion and +5% speed. Only your best Thrusters' Evasion counts."),
    ("ThermalAmplifier", "Thermal Amplifier", "Th Amp", "sm_ampth", "Low", "Piloting", "Passive", 0, 0, 0, "thermal_amplifier", False,
     p(DamageThermal=lambda r: 5 + r // 9), lambda r, s: f"+{s['DamageThermal']}% thermal damage."),
    ("EMAmplifier", "EM Amplifier", "EM Amp", "sm_ampem", "Low", "Piloting", "Passive", 0, 0, 0, "em_amplifier", False,
     p(DamageEM=lambda r: 5 + r // 9), lambda r, s: f"+{s['DamageEM']}% EM damage."),
    ("ExplosiveAmplifier", "Explosive Amplifier", "Ex Amp", "sm_ampex", "Low", "Piloting", "Passive", 0, 0, 0, "explosive_amplifier", False,
     p(DamageExplosive=lambda r: 5 + r // 9), lambda r, s: f"+{s['DamageExplosive']}% explosive damage."),
    ("BulwarkShieldGenerator", "Bulwark Shield Generator", "Bulwark", "sm_bulw", "Mid", "Piloting", "Toggle", 4.0, 8, 30, "bulwark_generator", True,
     p(AuraShieldPercent=lambda r: 15 if r < 45 else 20), lambda r, s: f"Capital array: allies within 30 m get +{s['AuraShieldPercent']}% max shield while on."),
    ("RepairField", "Repair Field", "Rep Field", "sm_rfield", "Mid", "Piloting", "Toggle", 4.0, 10, 30, "repair_field", True,
     p(Repair=lambda r: round(REP[r] * 1.1)), lambda r, s: f"Capital array: allies within 30 m regain {s['Repair']} shield every 4 seconds while on; never the capital itself."),
    ("EWarArray", "E-War Array", "E-War", "sm_ewar", "Mid", "Piloting", "Toggle", 4.0, 8, 30, "ewar", True,
     p(AuraAccuracy=lambda r: -12 if r < 45 else -14), lambda r, s: f"Capital array: enemies within 30 m get {s['AuraAccuracy']} Accuracy while on."),
    ("AdvancedThrusters", "Advanced Thrusters", "Adv Thrusters", "sm_advthr", "Low", "Piloting", "Passive", 0, 0, 0, "advanced_thrusters", True,
     p(ModuleEvasion=lambda r: 2 * r, SpeedPercent=10), lambda r, s: f"Capital: +{s['ModuleEvasion']} Evasion and +10% speed. Counts as Thrusters."),
    ("RedundantShieldGenerator", "Redundant Shield Generator", "Red Shields", "sm_redsh", "Low", "Piloting", "Passive", 0, 0, 0, "redundant_shields", True,
     p(Shield=lambda r: round(HP[r] * 1.5)), lambda r, s: f"Capital: +{s['Shield']} max shield (scales with hull size)."),
    ("ReinforcedPlating", "Reinforced Plating", "Reinforced", "sm_reinf", "Low", "Piloting", "Passive", 0, 0, 0, "reinforced_plating", True,
     p(Hull=lambda r: round(HP[r] * 1.5), ModuleDefense=lambda r: round(1.8 * r)),
     lambda r, s: f"Capital: +{s['Hull']} max hull (scales with hull size) and +{s['ModuleDefense']} Defense. Counts as Hull Plating."),
    ("TargetingArray", "Targeting Array", "Tgt Array", "sm_tarr", "Low", "Piloting", "Passive", 0, 0, 0, "targeting_array", True,
     p(ModuleAccuracy=lambda r: 2 + round(0.45 * r)), lambda r, s: f"Capital: +{s['ModuleAccuracy']} Accuracy. Counts as a Targeting Computer."),
    ("WeaponsComputer", "Weapons Computer", "Wpn Computer", "sm_wcomp", "Low", "Piloting", "Passive", 0, 0, 0, "weapons_computer", True,
     p(DamageAll=lambda r: 8 if r < 45 else 10), lambda r, s: f"Capital: +{s['DamageAll']}% weapon damage."),
    ("PowerDiverter", "Power Diverter", "Pwr Diverter", "sm_pdiv", "Low", "Piloting", "Passive", 0, 0, 0, "power_diverter", True,
     p(CapacitorRegen=lambda r: round(m.CAP_REGEN_STEP[r] * 2, 2)), lambda r, s: f"Capital: +{s['CapacitorRegen']} capacitor per second (scales with hull size)."),
    ("Salvager", "Salvager", "Salvager", "sm_salv", "High", "Gathering", "Toggle", 6.0, 3, 15, "salvager", False,
     p(MiningTier=lambda r: r // 10 + 1),
     lambda r, s: f"Salvages wrecks up to tier {s['MiningTier']} every 6 seconds while on. Capital wrecks need a Large or Capital hull."),
    ("GasHarvester", "Gas Harvester", "Gas Harvest", "sm_gas", "High", "Gathering", "Toggle", 6.0, 3, 15, "gas_harvester", False,
     p(MiningTier=lambda r: r // 10 + 1), lambda r, s: f"Harvests Tibanna Gas from gas clouds up to tier {s['MiningTier']} every 6 seconds while on."),
    ("SmugglingCompartment", "Smuggling Compartment", "Smuggler", "sm_smug", "Low", "Piloting", "Passive", 0, 0, 0,
     "smuggling_compartment", False, p(ScanReduction=lambda r: trade.compartment_reduction(r)),
     lambda r, s: f"A shielded hidden hold: customs scans find your cargo {s['ScanReduction']}% less often. Only your best Smuggling Compartment counts."),
]

MATERIALS = {1: ("ref_tilarium", "elec_ruined"), 2: ("ref_currian", "elec_flawed"), 3: ("ref_idailia", "elec_good"),
             4: ("ref_barinium", "elec_imperfect"), 5: ("ref_gostian", "elec_high")}


def tier(rank):
    return min(5, rank // 10 + 1)


# Families added after the 50 hulls (the salvage update W6a, the trade risk update W7b). Their recipe IDs follow the
# hulls' so no earlier RecipeType value moves.
LATE_FAMILIES = {"Salvager", "GasHarvester", "SmugglingCompartment"}


def module_steps(late_recipe_start=None):
    steps, recipe = [], RECIPE_TYPE_START
    late_recipe = late_recipe_start
    for (key, name, short, prefix, row, skill, use, cycle, cap, rng, art, capital, stats, desc) in FAMILIES:
        for r in (CAPITAL_STEPS if capital else m.STEPS):
            mk = ROMAN[r // 5]
            s = stats(r)
            refined, elec = MATERIALS[tier(r)]
            heavy = 2 if capital else 1
            base, model_part, texture = ART[art]
            late = key in LATE_FAMILIES
            steps.append(dict(
                Family=key, Rank=r, Tag=f"{prefix}_{r:02d}", Name=f"{name} Mk {mk}", ShortName=f"{short} {mk}"[:14],
                Row=row, Skill=skill, Activation=use, CycleSeconds=cycle, CapacitorCost=cap, Range=rng, Capital=capital,
                Texture=texture, BaseItem=base, ModelPart=model_part, Description=desc(r, s), RecipeType=late_recipe if late else recipe,
                RecipeName=f"Ship{key}{r:02d}",
                RecipeLevel=max(2, min(50, r + (3 if capital else 0))),
                Components=[(refined, (3 if row == "High" else 2) * heavy), (elec, (2 if row == "High" else 1) * heavy)],
                Cost=100 + 30 * r + (2000 if capital else 0), **s))
            if late:
                late_recipe = None if late_recipe is None else late_recipe + 1
            else:
                recipe += 1
    return steps


ABBR = {"Republic": "rep", "Corellian": "cor", "Sith": "sith", "Czerka": "czk", "Mandalorian": "mando", "Hutt": "hutt",
        "Chiss": "chiss", "Rift Raider": "raid"}
LAYOUT = {"Fighter": "Striker", "Interceptor": "Condor", "Bomber": "Fighter", "Explorer": "Condor",
          "Gunship": "Hound", "Logistics": "Panther", "Mining Barge": "Saber", "Light Freighter": "Mule",
          "Blockade Runner": "Falchion", "Frigate": "Throne", "Industrial": "Merchant", "Heavy Freighter": "Merchant",
          "Cruiser": "Corvette", "Command Ship": "Corvette"}
STORE_SHIPS = {"Striker", "XS Freighter"}


def ship_tag(name):
    t = "sd_" + "".join(c for c in name.lower() if c.isalnum())
    return t[:16]


def ships(recipe_start):
    out, recipe = [], recipe_start
    tags = set()
    for i, (name, role, maker, adv, model_row, text, sig) in enumerate(m.SHIPS, 1):
        r = m.ROLES[role]
        h = m.hull_stats(role, maker, adv, sig)
        tag = ship_tag(name)
        assert tag not in tags, tag
        tags.add(tag)
        gate = m.GATE[r["size"]] + (10 if adv else 0)
        lvl = {"Small": 5, "Medium": 15, "Large": 30, "Capital": 45}[r["size"]] + (10 if adv else 0)
        refined, elec = MATERIALS[min(5, gate // 10 + 1)]
        qty = {"Small": 8, "Medium": 12, "Large": 18, "Capital": 30}[r["size"]]
        mk = m.MAKERS[maker]
        out.append(dict(
            Number=i, Tag=tag, Name=name, Size=r["size"], Role=role.replace(" ", ""), Manufacturer=maker.replace(" ", ""),
            Advanced=adv, PilotingRank=gate, Appearance=model_row, Layout=LAYOUT[role],
            High=h["H"], Mid=h["M"], Low=h["L"], StationHigh=r.get("stH", 0), StationMid=r.get("stM", 0), StationLow=r.get("stL", 0),
            Hull=r["hull"], Shield=r["shield"], Capacitor=r["cap"], CapacitorRegen=r["capr"], Speed=h["speed"],
            Attack=h["atk"], Accuracy=h["acc"], Defense=h["df"], Evasion=h["ev"], Cargo=h["cargo"], Interior=r["interior"],
            RepairMultiplier=r["repair"], ResistAll=h["res"], HullPercent=h["pct"]["hull"], ShieldPercent=h["pct"]["shield"],
            CapacitorPercent=h["pct"]["cap"], ShieldRegenPercent=h["pct"]["shieldregen"], DamagePercent=h["dmg"],
            ExplosivePercent=h["explosive"], CriticalBonus=h["crit"], BuiltInBonus=r["bonus"], Signature=text,
            Store=name in STORE_SHIPS, RecipeType=recipe, RecipeName="ShipDeed" + "".join(c for c in name.title() if c.isalnum()),
            RecipeLevel=min(50, lvl),
            Components=[(refined, qty), (elec, qty // 2)], Cost={"Small": 500, "Medium": 2500, "Large": 10000, "Capital": 30000}[r["size"]] * (2 if adv else 1),
            RepairCostMultiplier=r["repair"], Theme=mk["theme"]))
        recipe += 1
    return out


# ---------------------------------------------------------------------------------------------------------------
def cs(v):
    if isinstance(v, bool):
        return "true" if v else "false"
    if isinstance(v, (int, float)):
        return repr(v) if isinstance(v, int) else f"{v}"
    return json.dumps(v)


ENUMS = {"Row": "ShipModulePowerType.", "Activation": "ShipModuleActivation.", "Family": "ShipModuleFamily.",
         "WeaponFamily": "SpaceWeaponFamily.", "DamageType": "CombatDamageType.", "FixedSize": "ShipSize.",
         "Size": "ShipSize.", "Role": "ShipRole.", "Manufacturer": "ShipManufacturer.", "Layout": "PropertyLayoutType.",
         "Skill": "SkillType."}
SKIP = {"Components", "BaseItem", "ModelPart", "Cost", "BuiltInBonus", "Theme", "Number", "RepairCostMultiplier"}


def initializer(cls, d):
    parts = []
    for k, v in d.items():
        if k in SKIP or v is None:
            continue
        if k == "Activation" and v == "Passive":
            parts.append("Activation = ShipModuleActivation.Invalid")
        elif k in ENUMS:
            parts.append(f"{k} = {ENUMS[k]}{v}")
        elif k == "RecipeType":
            parts.append(f"RecipeType = RecipeType.{d['RecipeName']}")
        elif k == "RecipeName":
            continue
        elif k == "Appearance":
            parts.append(f"Appearance = (AppearanceType){v}")
        else:
            parts.append(f"{k} = {cs(v)}")
    comps = ", ".join(f"({json.dumps(c)}, {q})" for c, q in d["Components"])
    parts.append(f"Components = new[] {{ {comps} }}")
    return f"            new {cls} {{ " + ", ".join(parts) + " },"


HEADER = """// <auto-generated>
// Generated by tools/SpaceRevampBalance/catalog.py from the space revamp balance model. Do not edit by hand:
// change model.py or catalog.py and regenerate. The Design Bible's Revamp tabs come from the same model.
// </auto-generated>
"""


def module_catalog_cs(steps):
    rows = "\n".join(initializer("ShipModuleStep", s) for s in steps)
    return (HEADER + "using System.Collections.Generic;\nusing SWLOR.Game.Server.Service.CombatService;\n"
            "using SWLOR.Game.Server.Service.CraftService;\nusing SWLOR.Game.Server.Service.SkillService;\n"
            "using SWLOR.Game.Server.Service.SpaceService;\n\nnamespace SWLOR.Game.Server.Feature.ShipModuleDefinition\n{\n"
            "    public static class RevampModuleCatalog\n    {\n"
            "        public static readonly IReadOnlyList<ShipModuleStep> Steps = new List<ShipModuleStep>\n        {\n"
            f"{rows}\n        }};\n    }}\n}}\n")


def ship_catalog_cs(hulls):
    rows = "\n".join(initializer("ShipHullEntry", s) for s in hulls)
    return (HEADER + "using System.Collections.Generic;\nusing SWLOR.Game.Server.Service.CraftService;\n"
            "using SWLOR.Game.Server.Service.PropertyService;\nusing SWLOR.Game.Server.Service.SpaceService;\n"
            "using SWLOR.NWN.API.NWScript.Enum;\n\nnamespace SWLOR.Game.Server.Feature.ShipDefinition\n{\n"
            "    public static class RevampShipCatalog\n    {\n"
            "        public static readonly IReadOnlyList<ShipHullEntry> Hulls = new List<ShipHullEntry>\n        {\n"
            f"{rows}\n        }};\n    }}\n}}\n")


TEMPLATE_MODULE = ROOT / "Module" / "uti" / "com_laser_1.uti.json"
TEMPLATE_DEED = ROOT / "Module" / "uti" / "sdeed_striker.uti.json"
TEMPLATE_CAPDEED = ROOT / "Module" / "uti" / "capdeed_rthran.uti.json"
SKILL_IDS = {"Piloting": 7, "Gathering": 11}


def requires_skill(skill, rank):
    return {"__struct_id": 0, "ChanceAppear": {"type": "byte", "value": 100}, "CostTable": {"type": "byte", "value": 48},
            "CostValue": {"type": "word", "value": rank}, "Param1": {"type": "byte", "value": 255},
            "Param1Value": {"type": "byte", "value": 0}, "PropertyName": {"type": "word", "value": 131},
            "Subtype": {"type": "word", "value": SKILL_IDS[skill]}}


def uti(template, tag, name, base, model_part, cost, props, description=None):
    d = copy.deepcopy(template)
    d["Tag"]["value"] = tag
    d["TemplateResRef"]["value"] = tag
    d["LocalizedName"]["value"] = {"0": name}
    d["BaseItem"]["value"] = base
    d["Cost"]["value"] = cost
    d["AddCost"]["value"] = cost
    if model_part is not None:
        d["ModelPart1"]["value"] = model_part
        if "xModelPart1" in d:
            d["xModelPart1"]["value"] = model_part
    d["PropertiesList"]["value"] = props
    if description is not None:
        d["Description"]["value"] = {"0": description}
        d["DescIdentified"]["value"] = {"0": description}
    return json.dumps(d, indent=2, ensure_ascii=False) + "\n"


def blueprints(steps, hulls):
    out = {}
    tm = json.loads(TEMPLATE_MODULE.read_text(encoding="utf-8"))
    td = json.loads(TEMPLATE_DEED.read_text(encoding="utf-8"))
    tc = json.loads(TEMPLATE_CAPDEED.read_text(encoding="utf-8"))
    for s in steps:
        props = [requires_skill(s["Skill"], s["Rank"])] if s["Rank"] > 0 else []
        out[f"Module/uti/{s['Tag']}.uti.json"] = uti(tm, s["Tag"], s["Name"], s["BaseItem"], s["ModelPart"], s["Cost"], props)
    deed_text = "This deed can be used to register a starship in your name at any starport."
    for h in hulls:
        props = [requires_skill("Piloting", h["PilotingRank"])] if h["PilotingRank"] > 0 else []
        capital = h["Size"] == "Capital"
        t = tc if capital else td
        base = t["BaseItem"]["value"]
        part = t["ModelPart1"]["value"]
        out[f"Module/uti/{h['Tag']}.uti.json"] = uti(t, h["Tag"], f"Ship Deed: {h['Name']}", base, part, h["Cost"], props, deed_text)
    return out


RECIPE_ENUM = ROOT / "SWLOR.Game.Server" / "Service" / "CraftService" / "RecipeType.cs"
BEGIN = "        // BEGIN space revamp catalog (generated by tools/SpaceRevampBalance/catalog.py)"
END = "        // END space revamp catalog"


def recipe_enum(steps, hulls):
    text = RECIPE_ENUM.read_text(encoding="utf-8")
    block = "\n".join([BEGIN] + [f"        {x['RecipeName']} = {x['RecipeType']}," for x in sorted(steps + hulls, key=lambda x: x["RecipeType"])] + [END])
    if BEGIN in text:
        head, rest = text.split(BEGIN, 1)
        tail = rest.split(END, 1)[1]
        return head + block + tail
    # First run: insert the block before the enum's closing brace.
    closing = text.rindex("    }")
    return text[:closing] + block + "\n" + text[closing:]


CRLF_FILES = {"SWLOR.Game.Server/Service/CraftService/RecipeType.cs"}


def catalog():
    """Every module step and ship deed with its RecipeType: the earlier families, then the hulls, then LATE_FAMILIES."""
    early = [s for s in module_steps() if s["Family"] not in LATE_FAMILIES]
    hulls = ships(RECIPE_TYPE_START + len(early))
    return module_steps(RECIPE_TYPE_START + len(early) + len(hulls)), hulls


def writes():
    steps, hulls = catalog()
    files = {
        "SWLOR.Game.Server/Feature/ShipModuleDefinition/RevampModuleCatalog.cs": module_catalog_cs(steps),
        "SWLOR.Game.Server/Feature/ShipDefinition/RevampShipCatalog.cs": ship_catalog_cs(hulls),
    }
    files["SWLOR.Game.Server/Service/CraftService/RecipeType.cs"] = recipe_enum(steps, hulls)
    npc_source = (ROOT / "SWLOR.Game.Server/Feature/SpaceObjectDefinition/NPCSpaceObjectDefinition.cs").read_text(encoding="utf-8")
    npc_tags = re.findall(r'_builder\.Create\("([^"]+)"\)', npc_source)
    files["SWLOR.Game.Server/Feature/SpaceObjectDefinition/RevampNpcShipCatalog.cs"] = npc_catalog.catalog_cs(
        HEADER, npc_catalog.entries(ROOT, npc_tags))
    files.update(blueprints(steps, hulls))
    return files, steps, hulls


def main():
    files, steps, hulls = writes()
    check = "--check" in sys.argv
    out_root = Path(sys.argv[sys.argv.index("--out") + 1]) if "--out" in sys.argv else ROOT
    stale = []
    for rel, text in files.items():
        path = out_root / rel
        if path.exists() and path.read_text(encoding="utf-8") == text:
            continue
        stale.append(rel)
        if not check:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text, encoding="utf-8", newline="\r\n" if rel.startswith("Module/") or rel in CRLF_FILES else "\n")
    print(f"{len(steps)} module items, {len(hulls)} ship deeds, {len(stale)} file(s) {'out of date' if check else 'written'}")
    if check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
