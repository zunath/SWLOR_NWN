"""Project the approved space specification into embedded fitting definitions."""
import argparse
import json
from pathlib import Path
from UpdateSpaceDesignBible import operator_requirement
from ShipEquipmentResources import action_metadata, module_resref, quality_dimensions

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "SWLOR.Game.Server/Data/ShipFitting.json"


# Gameplay metadata is authored alongside the numerical projection, never parsed from prose.
# (stat, amount, scales with this module's declared output). Fractions are unit fractions.
PASSIVE_MODIFIERS = {
    "shield_bank": [("ShipShieldCapacity", 60, True), ("ShipCapacitorCapacity", -15, False)],
    "recharge_array": [("ShipShieldRecovery", .6, True), ("ShipShieldCapacity", -.10, False)],
    "storage_bank": [("ShipCapacitorCapacity", 50, True), ("ShipCapacitorRecovery", -.10, False)],
    "recovery_regulator": [("ShipCapacitorRecovery", .6, True), ("ShipCapacitorCapacity", -.10, False)],
    "deep_scanner": [("ShipSpeed", -.05, False)],
    "tracking_computer": [("ShipTracking", .15, True)],
    "precision_array": [("ShipAccuracy", .04, True), ("ShipWeaponOutput", -.08, False)],
    "cargo_hold": [("ShipCargoCapacity", .30, True), ("ShipShieldCapacity", -.10, False)],
    "protected_hold": [("ShipProtectedCargo", 60, False), ("ShipCargoCapacity", -.10, False)],
    "armor_plating": [("ShipHullResistance", 20, True), ("ShipSpeed", -.08, False)],
    "hull_plating": [("ShipHullCapacity", 60, True), ("ShipSpeed", -.05, False)],
    "output_amplifier": [("ShipWeaponOutput", .08, True), ("ShipWeaponCapacitorDemand", .10, False)],
    "maneuver_jets": [("ShipEvasion", .04, True), ("ShipCargoCapacity", -.10, False)],
    "advanced_thrusters": [("ShipSpeed", .10, True), ("ShipCapacitorCapacity", -.10, False)],
    "power_router": [("ShipShieldResistance", 15, True), ("ShipWeaponOutput", -.10, False), ("ShipCapacitorCapacity", -.10, False)],
}
CONFIGURATION_MODIFIERS = {
    "combat_conversion": [("ShipWeaponOutput", .05, False), ("ShipCargoCapacity", -.15, False)],
    "survey_conversion": [("ShipScannerResolution", 10, False), ("ShipWeaponOutput", -.10, False)],
    "industrial_conversion": [("ShipReserveRemoval", .10, False), ("ShipWeaponOutput", -.15, False), ("ShipSpeed", -.05, False)],
    "cargo_conversion": [("ShipCargoCapacity", .20, False), ("ShipShieldCapacity", -.15, False)],
    "support_conversion": [("ShipExternalRecoveryOutput", .10, False), ("ShipWeaponOutput", -.15, False)],
}
FRACTIONAL_CAPACITY = {"ShipShieldCapacity", "ShipCapacitorCapacity", "ShipCapacitorRecovery"}


def modifiers(rows):
    return [dict(stat=stat, amount=amount, scales_with_output=scale,
                 proportional=(stat in FRACTIONAL_CAPACITY and -1 < amount < 0))
            for stat,amount,scale in rows]


def project(source):
    standard={v["design"]:v for v in source["module_variants"] if v["calibration"]=="Standard"}
    modules=[]
    for original in source["modules"]:
        module=dict(original)
        module.update(action_metadata(module))
        skill,rank=operator_requirement(module)
        module["operator_skill"]=skill.replace(" ", "")
        module["operator_rank"]=rank
        module["max_fitted"]={"protected_hold":1,"ore_compressor":1,"recovery_regulator":2}.get(module["id"],0)
        module["modifiers"]=modifiers(PASSIVE_MODIFIERS.get(module["id"],[]))
        module["recovery_fraction"]=standard[module["id"]]["recovery_fraction"]
        module["quality_dimensions"]=quality_dimensions(original)
        modules.append(module)
    multipliers={c["name"]:c["capacitor"] for c in source["calibrations"]}
    variants=[dict(v,capacitor_multiplier=multipliers[v["calibration"]],item_resref=module_resref(v["design"],v["calibration"])) for v in source["module_variants"]]
    legacy=[]
    for original in source["legacy_modules"]:
        row=dict(original)
        blueprint=json.loads((ROOT/"Module/uti"/(row["resref"]+".uti.json")).read_text(encoding="utf-8"))
        row["item_tag"]=blueprint["Tag"]["value"]
        legacy.append(row)
    configurations=[dict(c,item_tag="fit_"+c["id"],modifiers=modifiers(CONFIGURATION_MODIFIERS[c["id"]])) for c in source["configurations"]]
    return dict(hulls=source["hulls"],modules=modules,variants=variants,configurations=configurations,legacy_modules=legacy)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check",action="store_true")
    args=parser.parse_args()
    source=json.loads((ROOT/"design/space/space-balance.json").read_text(encoding="utf-8"))
    text=json.dumps(project(source),indent=2,ensure_ascii=False)+"\n"
    if args.check:
        if TARGET.read_text(encoding="utf-8")!=text:raise SystemExit("Embedded fitting definitions differ from the approved specification")
    else:
        TARGET.parent.mkdir(parents=True,exist_ok=True)
        TARGET.write_text(text,encoding="utf-8")
    print(f"Verified {len(source['hulls'])} hulls, {len(source['modules'])} module designs, {len(source['module_variants'])} calibrated variants.")


if __name__=="__main__":main()
