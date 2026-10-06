"""Project ship vendor, resale and ore bids from the approved balance model."""
import argparse,json,math,re
from pathlib import Path
from ShipEquipmentResources import equipment_recipes,INPUT_RESREFS,module_resref,MATERIALS
ROOT=Path(__file__).resolve().parents[1]
def generate():
    s=json.loads((ROOT/"design/space/space-balance.json").read_text(encoding="utf-8"))
    prices={r["metal"]:r["metal_value"] for r in s["resources"]}
    prices.update(elec_ruined=13,elec_recover=26,prec_assembly=72)
    modules={m["id"]:m for m in s["modules"]};standard={r["id"]:r["reference_cost"] for r in s["recipes"]}
    variants={module_resref(v["design"],v["calibration"]):v for v in s["module_variants"]}
    hulls={r["id"]:r for r in s["hull_recipes"]};src=(ROOT/"SWLOR.Game.Server/Feature/ShipDefinition/PlayerShipDefinition.cs").read_text(encoding="utf-8")
    refs={re.search(r'\.ItemResref\("([^"\n]+)"\)',b).group(1):k for k,b in re.findall(r'_builder.Create\("([^"\n]+)"\)(.*?);',src,re.S)}
    inputs={INPUT_RESREFS[r["name"]]:r for r in s["craft_inputs"] if r["name"] in INPUT_RESREFS}
    rows=[]
    for r in equipment_recipes(s):
        material=sum(prices[c]*q for c,q in r["components"].items())/r["quantity"]
        ref=math.ceil(material);name=r["name"];vendor=False;ammo=False;base=material
        if r["resref"] in variants:
            v=variants[r["resref"]];m=modules[v["design"]];ref=m["value"];base=standard[v["design"]]
            name=m["name"]+("" if v["calibration"]=="Standard" else " ("+v["calibration"]+")")
            vendor=v["calibration"]=="Standard" and v["engineering"]<=20
        elif r["resref"] in refs:
            h=hulls[refs[r["resref"]]];ref=h["hull_reference"];name=h["name"]+" Deed"
        elif r["resref"] in inputs:
            i=inputs[r["resref"]];ref=i["npc_reference"] or math.ceil(i["reference_unit_cost"]);name=i["name"];vendor=i["npc_reference"]>0;ammo=True
        elif r["category"]=="ShipModule":ref=350;name=next(c["name"] for c in s["configurations"] if c["resref"]==r["resref"])
        rows.append(dict(resref=r["resref"],name=name,reference=ref,resale=math.floor(min(.25*ref,.75*base)),vendor=vendor,ammunition=ammo,engineering=r["level"]))
    return dict(items=rows,ore_bids={r["ore"]:r["bid"] for r in s["resources"]},legacy_refunds=[dict(resref=r["resref"],fraction=next((m["reclaim_fraction"] for m in s["legacy_modules"] if m["resref"]==r["resref"]),0),components=dict(r["components"])) for r in s["legacy_recipes"]])
def main():
    a=argparse.ArgumentParser();a.add_argument("--check",action="store_true");args=a.parse_args()
    p=ROOT/"SWLOR.Game.Server/Data/SpaceEconomy.json";text=json.dumps(generate(),indent=2)+"\n"
    if args.check:
        if not p.exists() or p.read_text(encoding="utf-8")!=text:raise SystemExit("Stale space economy data")
    else:p.write_text(text,encoding="utf-8",newline="")
    print("Space economy data matches the approved recipe, reference and ore values.")
if __name__=="__main__":main()
