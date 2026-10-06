"""Project the approved space specification into embedded fitting definitions."""
import argparse
import json
from pathlib import Path
from UpdateSpaceDesignBible import operator_requirement

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "SWLOR.Game.Server/Data/ShipFitting.json"


def project(source):
    dimensions = {
        "Weapon output": "Output", "Recovery output": "Output",
        "Scanner resolution": "Output", "Declared positive capacity/rating/handling amount": "Output",
        "Tracking": "Tracking", "Range": "Range", "Usable recovery fraction": "RecoveryFraction",
        "Activation cost": "ActivationCost", "Cycle duration": "CycleDuration",
    }
    standard={v["design"]:v for v in source["module_variants"] if v["calibration"]=="Standard"}
    modules=[]
    for original in source["modules"]:
        module=dict(original)
        skill,rank=operator_requirement(module)
        module["operator_skill"]=skill.replace(" ", "")
        module["operator_rank"]=rank
        module["max_fitted"]=1 if module["id"]=="protected_hold" else 0
        module["recovery_fraction"]=standard[module["id"]]["recovery_fraction"]
        quality=module.pop("quality_dimensions")
        # The declared capacity/rating/handling label is one dimension, not three.
        choices=quality.replace("Declared positive capacity/rating/handling amount", "CAPACITY").split(" / ")
        module["quality_dimensions"]=", ".join(dimensions[choice.replace("CAPACITY", "Declared positive capacity/rating/handling amount")] for choice in choices if choice!="None") or "None"
        modules.append(module)
    multipliers={c["name"]:c["capacitor"] for c in source["calibrations"]}
    variants=[dict(v,capacitor_multiplier=multipliers[v["calibration"]]) for v in source["module_variants"]]
    return dict(hulls=source["hulls"],modules=modules,variants=variants,configurations=source["configurations"])


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
