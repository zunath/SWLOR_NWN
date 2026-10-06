"""Stable, meaningful resource names for ship equipment and calibration recipes."""
BASE_RESREFS = {
    "tracking_laser":"trk_laser", "sustained_beam":"beam_emitter", "pulse_laser":"pulse_laser",
    "long_emitter":"long_emitter", "laser_battery":"laser_bank", "heavy_beam":"hvy_beam",
    "shield_breaker":"shield_break", "engine_disruptor":"eng_disrupt", "cap_disruptor":"cap_disrupt",
    "rapid_missile":"light_launch", "heavy_missile":"hvy_launch", "torpedo":"torp_launch", "bombardment":"bomb_launch",
    "shield_bank":"shield_bank", "recharge_array":"recharge_arr", "shield_booster":"shield_boost",
    "hull_repair":"hull_repair", "shield_repair":"shld_repair", "repair_projector":"repair_proj", "repair_field":"repair_field",
    "storage_bank":"storage_bank", "recovery_regulator":"cap_recovery", "fuel_injector":"fuel_inject", "transfer_projector":"cap_transfer",
    "survey_scanner":"survey_scan", "deep_scanner":"deep_scan", "tracking_computer":"track_comp", "precision_array":"prec_array",
    "interference_suite":"sensor_jam", "countermeasures":"decoy_suite", "precision_cutter":"prec_cutter", "bulk_extractor":"ore_extract",
    "deep_drill":"deep_drill", "compact_drill":"comp_drill", "strip_miner":"strip_miner", "recovery_arm":"recovery_arm",
    "electronics_kit":"elec_kit", "salvage_cutter":"salvage_cut", "cargo_hold":"cargo_hold", "ore_compressor":"ore_compress",
    "protected_hold":"secure_hold", "armor_plating":"armor_plate", "hull_plating":"hull_plate", "output_amplifier":"output_amp",
    "maneuver_jets":"maneuver_jet", "advanced_thrusters":"adv_thruster", "power_router":"power_router",
}
SUFFIXES = {"Standard":"", "Efficient":"_eff", "Extended":"_ext", "Rapid":"_rap", "Compact":"_cmp", "High Output":"_out", "Precision":"_pre"}
SHORT_NAMES = {
    "tracking_laser":"Tracking Laser", "sustained_beam":"Beam Emitter", "pulse_laser":"Pulse Laser", "long_emitter":"Long Emitter",
    "laser_battery":"Laser Battery", "heavy_beam":"Heavy Beam", "shield_breaker":"Shield Breaker", "engine_disruptor":"Engine Disrupt",
    "cap_disruptor":"Cap Disrupt", "rapid_missile":"Light Missile", "heavy_missile":"Heavy Missile", "torpedo":"Torpedo", "bombardment":"Bombardment",
    "shield_bank":"Shield Bank", "recharge_array":"Shield Regen", "shield_booster":"Shield Boost", "hull_repair":"Hull Repair",
    "shield_repair":"Shield Repair", "repair_projector":"Repair Beam", "repair_field":"Repair Field", "storage_bank":"Cap Storage",
    "recovery_regulator":"Cap Recovery", "fuel_injector":"Fuel Injector", "transfer_projector":"Cap Transfer", "survey_scanner":"Survey Scan",
    "deep_scanner":"Deep Scan", "tracking_computer":"Tracking Comp", "precision_array":"Precision", "interference_suite":"Interference",
    "countermeasures":"Decoys", "precision_cutter":"Mining Cutter", "bulk_extractor":"Bulk Extractor", "deep_drill":"Deep Drill",
    "compact_drill":"Compact Drill", "strip_miner":"Strip Miner", "recovery_arm":"Salvage Arm", "electronics_kit":"Recover Tech",
    "salvage_cutter":"Salvage Cutter", "cargo_hold":"Cargo Hold", "ore_compressor":"Ore Compressor", "protected_hold":"Secure Hold",
    "armor_plating":"Armor Plate", "hull_plating":"Hull Plate", "output_amplifier":"Weapon Boost", "maneuver_jets":"Maneuver Jets",
    "advanced_thrusters":"Thrusters", "power_router":"Power Router",
}
AMMUNITION = {"rapid_missile":"light_missile", "heavy_missile":"assault_missile", "torpedo":"ship_torpedo", "bombardment":"ship_bomb"}
INPUT_RESREFS = {"Light missiles":"light_missile", "Assault missiles":"assault_missile", "Torpedoes":"ship_torpedo", "Bombs":"ship_bomb",
    "Injector charges":"injector_charge", "Ore packing charges":"ore_pack_charge", "Precision assembly":"prec_assembly"}


def module_resref(design, calibration="Standard"):
    result = BASE_RESREFS[design] + SUFFIXES[calibration]
    if len(result)>16:raise ValueError("Ship resource exceeds NWN's 16-character limit: " + result)
    return result


def action_metadata(module):
    mid=module["id"]
    result=dict(item_tag="fit_"+mid,short_name=SHORT_NAMES[mid],item_resref=module_resref(mid),action="Passive",
        shield_multiplier=1,hull_multiplier=1,hardness_limit=0,preparation_seconds=0,ammunition=None,
        working_speed_penalty=0,movement_lock=False)
    if len(result["short_name"])>14:raise ValueError("Module short name exceeds 14 characters: " + mid)
    if module["family"] in ("Thermal","Ion","Ordnance"):
        result["action"]="Weapon"
        result["ammunition"]=AMMUNITION.get(mid)
        if mid=="shield_breaker":result.update(shield_multiplier=1.3,hull_multiplier=.5)
        if mid=="torpedo":result["preparation_seconds"]=2
        if mid=="bombardment":result["preparation_seconds"]=3
    else:
        result["action"]={"shield_booster":"SelfShieldRepair", "hull_repair":"SelfHullRepair", "shield_repair":"ShieldRepair",
            "repair_projector":"HullRepair", "repair_field":"RepairField", "fuel_injector":"FuelInjection", "transfer_projector":"CapacitorTransfer",
            "survey_scanner":"Survey", "deep_scanner":"Survey", "interference_suite":"Interference", "countermeasures":"Countermeasures",
            "precision_cutter":"Extraction", "bulk_extractor":"Extraction", "deep_drill":"Extraction", "compact_drill":"Extraction", "strip_miner":"Extraction",
            "recovery_arm":"BulkSalvage", "electronics_kit":"IntactSalvage", "salvage_cutter":"BulkSalvage", "ore_compressor":"Compression"}.get(mid,"Passive")
        result["hardness_limit"]={"precision_cutter":45,"bulk_extractor":65,"deep_drill":90,"compact_drill":90,"strip_miner":75}.get(mid,0)
        result["working_speed_penalty"]=.5 if mid in ("deep_drill","compact_drill") else 0
        result["movement_lock"]=mid=="strip_miner"
    return result


DIMENSION_LABELS = {"Output":"output", "Tracking":"tracking", "Range":"range", "RecoveryFraction":"recovery efficiency", "ActivationCost":"capacitor cost", "CycleDuration":"cycle duration"}
DIMENSIONS = [("Output","out",128),("Tracking","track",129),("Range","range",130),("RecoveryFraction","recover",131),("ActivationCost","cost",132),("CycleDuration","cycle",133)]
MATERIALS = {"tilarium":"ref_tilarium","currian":"ref_currian","electronics":"elec_ruined","recovered":"elec_recover","precision":"prec_assembly"}


def pascal(text):
    import re
    return "".join(w[:1].upper()+w[1:] for w in re.findall(r"[A-Za-z0-9]+",text))


def quality_dimensions(module):
    dimensions={"Weapon output":"Output", "Recovery output":"Output", "Scanner resolution":"Output", "CAPACITY":"Output", "Tracking":"Tracking", "Range":"Range", "Usable recovery fraction":"RecoveryFraction", "Activation cost":"ActivationCost", "Cycle duration":"CycleDuration"}
    choices=module["quality_dimensions"].replace("Declared positive capacity/rating/handling amount","CAPACITY").split(" / ")
    return ", ".join(dimensions[c] for c in choices if c!="None") or "None"


def equipment_recipes(source):
    import re
    from pathlib import Path
    root=Path(__file__).resolve().parents[1]
    rows=[]
    def add(name,resref,data,category,dimensions="None",extra_recovered=0):
        rows.append(dict(name=name,resref=resref,level=data["engineering"],quantity=data.get("output",1),category=category,dimensions=dimensions,
            components={resref_:data.get(field,0)+(extra_recovered if field=="recovered" else 0) for field,resref_ in MATERIALS.items() if data.get(field,0)+(extra_recovered if field=="recovered" else 0)}))
    modules={m["id"]:m for m in source["modules"]};recipes={r["id"]:r for r in source["recipes"]}
    for variant in source["module_variants"]:
        mid=variant["design"]
        add("Assemble"+pascal(mid)+pascal(variant["calibration"]),module_resref(mid,variant["calibration"]),dict(recipes[mid],engineering=variant["engineering"]),"ShipModule",quality_dimensions(modules[mid]),int(variant["calibration"]!="Standard"))
    for config in source["configurations"]:
        add("Assemble"+pascal(config["id"]),config["resref"],next(r for r in source["craft_inputs"] if r["name"]=="Configuration"),"ShipModule","Output")
    hull_source=(root/"SWLOR.Game.Server/Feature/ShipDefinition/PlayerShipDefinition.cs").read_text(encoding="utf-8")
    hull_refs={key:re.search(r'\.ItemResref\("([^"\n]+)"\)',block).group(1) for key,block in re.findall(r'_builder.Create\("([^"\n]+)"\)(.*?);',hull_source,re.S)}
    for hull in source["hull_recipes"]:add("Construct"+pascal(hull["name"])+"Hull",hull_refs[hull["id"]],hull,"Starship")
    for data in source["craft_inputs"]:
        if data["name"] in INPUT_RESREFS:add("Manufacture"+pascal(data["name"]),INPUT_RESREFS[data["name"]],data,"StarshipAmmo")
        if data["name"].startswith("Refinement"):
            magnitude=int(data["name"].removeprefix("Refinement"))
            for dimension,short,subtype in DIMENSIONS:
                add("MakeShip"+dimension+"Refinement"+str(magnitude),f"tune_{short}{magnitude}",data,"ModuleEnhancement")
    return rows
