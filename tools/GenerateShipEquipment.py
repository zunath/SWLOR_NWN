"""Generate craftable ship equipment from the approved numerical specification."""
import argparse,copy,json,re,math,subprocess
from pathlib import Path
from GenerateShipFittingData import project
from ShipEquipmentResources import module_resref,INPUT_RESREFS,equipment_recipes,DIMENSIONS,DIMENSION_LABELS,MATERIALS,pascal
ROOT=Path(__file__).resolve().parents[1]
def variable(name,value):
    return {"__struct_id":0,"Name":{"type":"cexostring","value":name},"Type":{"type":"dword","value":3 if isinstance(value,str) else 1},"Value":{"type":"cexostring" if isinstance(value,str) else "int","value":value}}


def blueprint(template,resref,name,tag,value,description,variables=None):
    item=json.loads((ROOT/"Module/uti"/(template+".uti.json")).read_text(encoding="utf-8"))
    item["TemplateResRef"]["value"]=resref;item["Tag"]["value"]=tag
    item["LocalizedName"]["value"]={"0":name};item["Description"]["value"]={"0":description}
    item["DescIdentified"]["value"]={};item["Cost"]["value"]=value;item["AddCost"]["value"]=value
    item["PropertiesList"]["value"]=[]
    item["VarTable"]={"type":"list","value":[variable(k,v) for k,v in dict(variables or {},SHIP_EQUIPMENT_RESOURCE=1).items()]}
    return item


def item_property(kind,amount,subtype=0,cost_table=45):
    template=json.loads((ROOT/"Module/uti/men_mod1.uti.json").read_text())["PropertiesList"]["value"][0]
    row=copy.deepcopy(template);row["PropertyName"]["value"]=kind;row["CostValue"]["value"]=amount
    row["Subtype"]["value"]=subtype;row["CostTable"]["value"]=cost_table
    return row


def generate():
    source=json.loads((ROOT/"design/space/space-balance.json").read_text(encoding="utf-8"));runtime=project(source)
    modules={m["id"]:m for m in runtime["modules"]};recipes={r["id"]:r for r in source["recipes"]}
    variants={v["design"]:v for v in runtime["variants"] if v["calibration"]=="Standard"}
    outputs={};rows=equipment_recipes(source)
    def add_recipe(name,resref,data,category,dimensions="None",extra_recovered=0):
        row=next(r for r in rows if r["name"]==name)
        assert row["resref"]==resref and row["level"]==data["engineering"] and row["category"]==category and row["dimensions"]==dimensions
    for variant in runtime["variants"]:
        mid=variant["design"];module=modules[mid];recipe=recipes[mid];resref=variant["item_resref"]
        template=next((old["resref"] for old in source["legacy_modules"] if old["target"]==mid),"com_laser_b")
        title=module["name"]+("" if variant["calibration"]=="Standard" else " ("+variant["calibration"]+")")
        ceiling=math.floor(min(module["value"]*.25,recipe["reference_cost"]*.75))
        details=f"{module['effect']}\n{variant['calibration']} calibration. {module['slot']} slot; {module['mount']} mount; {variant['power']} fitting power. Operate: { {'ShipSystems':'Ship Systems','SpaceIndustry':'Space Industry'}.get(module['operator_skill'],module['operator_skill'])} {module['operator_rank']}. Engineering {variant['engineering']} to manufacture.\nOutput {variant['output']}; cycle {variant['cycle']}s; capacitor {variant['capacitor']}; range {variant['range']}m. Quality tunes one eligible property, up to 8%."
        outputs[resref]=blueprint(template,resref,title,module["item_tag"],module["value"],details,
            {"SHIP_FITTING":1,"SHIP_DESIGN":mid,"SHIP_CALIBRATION":variant["calibration"],"SHIP_CONDITION":100,"SHIP_QUALITY_DIM":0,"SHIP_QUALITY":0,"NPC_RESALE_LIMIT":ceiling})
        add_recipe("Assemble"+pascal(mid)+pascal(variant["calibration"]),resref,dict(recipe,engineering=variant["engineering"]),"ShipModule",module["quality_dimensions"],1 if variant["calibration"]!="Standard" else 0)
    for config in source["configurations"]:
        resref=config["resref"];data=next(r for r in source["craft_inputs"] if r["name"]=="Configuration")
        outputs[resref]=blueprint("config_fig1",resref,config["name"],"fit_"+config["id"],350,
            config["benefit"]+". "+config["drawback"]+". One configuration slot; 6 fitting power. One optional output refinement.",
            {"SHIP_FITTING":1,"SHIP_DESIGN":config["id"],"SHIP_CONDITION":100,"SHIP_QUALITY_DIM":0,"SHIP_QUALITY":0,"NPC_RESALE_LIMIT":87})
        add_recipe("Assemble"+pascal(config["id"]),resref,data,"ShipModule","Output")
    hull_source=(ROOT/"SWLOR.Game.Server/Feature/ShipDefinition/PlayerShipDefinition.cs").read_text(encoding="utf-8")
    hull_refs={key:re.search(r'\.ItemResref\("([^"\n]+)"\)',block).group(1) for key,block in re.findall(r'_builder.Create\("([^"\n]+)"\)(.*?);',hull_source,re.S)}
    for hull in source["hull_recipes"]:
        add_recipe("Construct"+pascal(hull["name"])+"Hull",hull_refs[hull["id"]],hull,"Starship")
    for data in source["craft_inputs"]:
        if data["name"] not in INPUT_RESREFS:continue
        resref=INPUT_RESREFS[data["name"]]
        template="ship_missile" if data["name"]!="Precision assembly" else "elec_ruined"
        value=data["npc_reference"] or math.ceil(data["reference_unit_cost"])
        outputs[resref]=blueprint(template,resref,data["name"],resref,value,
            "Ship equipment supply. Engineering "+str(data["engineering"])+" to manufacture.",
            {"NPC_RESALE_LIMIT":math.floor(min(value*.25,data["reference_unit_cost"]*.75))})
        add_recipe("Manufacture"+pascal(data["name"]),resref,data,"StarshipAmmo")
    outputs["elec_recover"]=blueprint("elec_ruined","elec_recover","Recovered Electronics","elec_recover",26,
        "Recovered intact electronics used in ship calibrations and precision assemblies.",{"NPC_RESALE_LIMIT":6})
    for data in source["craft_inputs"]:
        if not data["name"].startswith("Refinement"):continue
        magnitude=int(data["name"].removeprefix("Refinement"))
        for dimension,short,subtype in DIMENSIONS:
            resref=f"tune_{short}{magnitude}"
            outputs[resref]=blueprint("men_mod1",resref,f"Ship {DIMENSION_LABELS[dimension].title()} Refinement {magnitude}",resref,math.ceil(data["reference_unit_cost"]),
                f"Select one eligible {DIMENSION_LABELS[dimension]} refinement. Magnitude {magnitude} grants {magnitude*.08:g}% refinement after a successful crafting transfer. Adds {magnitude*40//100} progress requirement. No extra tuning slot.",{"NPC_RESALE_LIMIT":0})
            outputs[resref]["PropertiesList"]["value"]=[item_property(104,data["engineering"]),item_property(95,magnitude*40//100,cost_table=44),item_property(110,magnitude,subtype)]
            add_recipe("MakeShip"+dimension+"Refinement"+str(magnitude),resref,data,"ModuleEnhancement")
    if len(outputs)!=len(set(outputs)) or len(rows)!=len({r["name"] for r in rows}):raise ValueError("Duplicate ship resource or recipe.")
    if any(len(k)>16 for k in outputs):raise ValueError("Overlong equipment resref.")
    enum_path=ROOT/"SWLOR.Game.Server/Service/CraftService/RecipeType.cs";enum_source=enum_path.read_text(encoding="utf-8-sig")
    existing={name:int(number) for name,number in re.findall(r'^\s*(\w+)\s*=\s*(\d+)',enum_source,re.M)}
    next_id=max(existing.values())+1;additions=[]
    for row in rows:
        if row["name"] not in existing:
            existing[row["name"]]=next_id;additions.append(f"        {row['name']} = {next_id},");next_id+=1
    if additions:
        index=enum_source.rfind("    }");enum_source=enum_source[:index]+"\n        // Ship equipment recipes.\n"+"\n".join(additions)+"\n"+enum_source[index:]
    code=["// Generated by tools/GenerateShipEquipment.py from the approved ship specification.","using System.Collections.Generic;","using SWLOR.Game.Server.Service.CraftService;","using SWLOR.Game.Server.Service.PerkService;","using SWLOR.Game.Server.Service.SkillService;","using SWLOR.Game.Server.Service.SpaceService;","","namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition","{","    public sealed class ShipEquipmentRecipes : IRecipeListDefinition","    {","        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()","        {","            var builder = new RecipeBuilder();"]
    for row in rows:
        level=row["level"];license_rank=1 if level<10 else 2 if level<20 else 3 if level<35 else 4 if level<45 else 5
        dimensions=" | ".join("ShipQualityDimension."+s.strip() for s in row["dimensions"].split(","))
        code.extend([f"            builder.Create(RecipeType.{row['name']}, SkillType.Engineering)",f"                .Category(RecipeCategoryType.{row['category']})",f"                .Resref(\"{row['resref']}\")",f"                .Level({level})",f"                .Quantity({row['quantity']})",f"                .ShipEquipment({dimensions})",f"                .RequirementPerk(PerkType.ShipManufacturing, {license_rank}, \"Ship Manufacturing\")"])
        if row["dimensions"]!="None":code.append("                .EnhancementSlots(RecipeEnhancementType.Module, 1)")
        for material,quantity in row["components"].items():
            if quantity:code.append(f"                .Component(\"{material}\", {quantity})")
        code[-1]+=";";code.append("")
    code.extend(["            return builder.Build();","        }","    }","}",""])
    files={enum_path:enum_source,ROOT/"SWLOR.Game.Server/Feature/RecipeDefinition/EngineeringRecipeDefinition/ShipEquipmentRecipes.cs":"\n".join(code)}
    tracked_paths=set(subprocess.run(["git","ls-files","--","Module/uti"],cwd=ROOT,capture_output=True,text=True,check=True).stdout.splitlines())
    for resref,item in outputs.items():
        path=ROOT/"Module/uti"/(resref+".uti.json")
        tracked=path.relative_to(ROOT).as_posix() in tracked_paths
        if tracked and path.exists():
            old=json.loads(path.read_text(encoding="utf-8"))
            owned=any(v.get("Name",{}).get("value")=="SHIP_EQUIPMENT_RESOURCE" and v.get("Value",{}).get("value")==1 for v in old.get("VarTable",{}).get("value",[]))
            if not owned:raise ValueError("Ship equipment resource collides with an existing unrelated blueprint: "+resref)
        files[path]=json.dumps(item,indent=2,ensure_ascii=False)+"\n"
    table_lines=(ROOT/"SWLOR_Haks/sw_2da/baseitems.2da").read_text(encoding="utf-8").splitlines()
    header=next(line.split() for line in table_lines if line.lstrip().startswith("Name"))
    bases={int(line.split()[0]):dict(zip(header,line.split()[1:])) for line in table_lines if re.match(r"^\d+\s",line)}
    details=["// Generated by tools/GenerateShipEquipment.py.","using System.Collections.Generic;","using SWLOR.Game.Server.Service.SpaceService;","","namespace SWLOR.Game.Server.Feature.ShipModuleDefinition","{","    public sealed class ShipEquipmentModuleDefinition : IShipModuleListDefinition","    {","        public Dictionary<string, ShipModuleDetail> BuildShipModules()","        {","            var result = new Dictionary<string, ShipModuleDetail>();","            var catalog = ShipFittingCatalog.Default;"]
    for module in runtime["modules"]:
        item=outputs[module["item_resref"]];base=bases[item["BaseItem"]["value"]]
        icon="i"+base["ItemClass"]+"_"+str(item["xModelPart1"]["value"]).zfill(3)
        kind={"Weapon":"CombatLaser", "SelfHullRepair":"HullRepairer","HullRepair":"HullRepairer", "SelfShieldRepair":"ShieldRepairer", "ShieldRepair":"ShieldRepairer", "RepairField":"RepairFieldGenerator", "FuelInjection":"HypermatterInjector", "Extraction":"MiningLaser", "Interference":"CapitalEwar"}.get(module["action"],"Passive" if module["action"]=="Passive" else "CapitalPowerDiverter")
        details.extend([f'            result.Add("{module["item_tag"]}", new ShipModuleDetail {{',f'                FittingProfile = catalog.Modules["{module["id"]}"],',f'                Name = catalog.Modules["{module["id"]}"].Name, ShortName = "{module["short_name"]}",',f'                Texture = "{icon.lower()}", Description = catalog.Modules["{module["id"]}"].Effect,',f'                Type = ShipModuleType.{kind}, PowerType = ShipModulePowerType.{module["slot"]}',"            });"])
    for config in source["configurations"]:
        item=outputs[config["resref"]];base=bases[item["BaseItem"]["value"]]
        icon="i"+base["ItemClass"]+"_"+str(item["xModelPart1"]["value"]).zfill(3)
        details.extend([f'            result.Add("fit_{config["id"]}", new ShipModuleDetail {{',f'                ConfigurationProfile = catalog.Configurations["{config["id"]}"],',f'                Name = "{config["name"]}", ShortName = "Configuration", Texture = "{icon.lower()}",',f'                Description = catalog.Configurations["{config["id"]}"].Benefit + ". " + catalog.Configurations["{config["id"]}"].Drawback,',"                Type = ShipModuleType.Passive, PowerType = ShipModulePowerType.Config","            });"])
    details.extend(["            return result;","        }","    }","}",""])
    files[ROOT/"SWLOR.Game.Server/Feature/ShipModuleDefinition/ShipEquipmentModuleDefinition.cs"]="\n".join(details)
    tlk_path=ROOT/"SWLOR_Haks/sw_tlk/sw_tlk.tlk.json"
    tlk=json.loads(tlk_path.read_text(encoding="utf-8"));entries={e["id"]:e for e in tlk["entries"]}
    refs=[]
    for dimension,short,subtype in DIMENSIONS:
        text="Ship "+DIMENSION_LABELS[dimension].title()+" Refinement"
        found=next((e["id"] for e in entries.values() if e.get("text")==text),None)
        if found is None:
            found=next(i for i in range(6202,max(entries)+1) if i not in entries or not entries[i].get("text"))
            entries[found]={"id":found,"text":text}
        refs.append(found+16777216)
    tlk["entries"]=sorted(entries.values(),key=lambda e:e["id"])
    files[tlk_path]=json.dumps(tlk,indent=2,ensure_ascii=False)+"\n"
    subtype_path=ROOT/"SWLOR_Haks/sw_2da/iprp_enhancemod.2da"
    subtype_lines=subtype_path.read_text(encoding="utf-8").splitlines()
    for i,(dimension,short,subtype) in enumerate(DIMENSIONS):
        index=next(n for n,line in enumerate(subtype_lines) if re.match(r"^"+str(subtype)+r"\s",line))
        subtype_lines[index]=f"{subtype}   {refs[i]}   Ship{dimension}   1"
    files[subtype_path]="\n".join(subtype_lines)+"\n"
    files[ROOT/"SWLOR_Haks/sw_2da/iprp_shiptune.2da"]="2DA V2.0\n\n     Name    Label    Cost\n"+"".join(f"{i}   {refs[i]}   {dimension}   1\n" for i,(dimension,_,_) in enumerate(DIMENSIONS))
    props_path=ROOT/"SWLOR_Haks/sw_2da/itempropdef.2da";props=props_path.read_text(encoding="utf-8").splitlines()
    index=next(n for n,line in enumerate(props) if re.match(r"^113\s",line))
    tokens=props[index].split();tokens[3]="iprp_shiptune";props[index]="   ".join(tokens)
    files[props_path]="\n".join(props)+"\n"
    return files,len(rows),len(outputs)


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument("--check",action="store_true");args=parser.parse_args()
    files,recipes,items=generate()
    for path,text in files.items():
        if args.check:
            if not path.exists() or path.read_text(encoding="utf-8-sig")!=text:raise SystemExit("Ship equipment differs from its specification: "+str(path.relative_to(ROOT)))
        else:path.write_text(text,encoding="utf-8")
    print(f"Verified {recipes} ship recipes and {items} equipment/supply blueprints.")


if __name__=="__main__":main()
