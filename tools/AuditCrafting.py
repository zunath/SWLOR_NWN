"""Read-only crafting content and rotation audit. Uses JSON exported by SWLOR.CraftingAudit."""
from pathlib import Path
import json
import re
import struct
from collections import Counter, defaultdict
from functools import lru_cache
from math import comb
import argparse
import time

ROOT = Path(__file__).resolve().parents[1]
SKILLS = {1: "Smithery", 2: "Engineering", 3: "Fabrication", 4: "Agriculture"}
CRAFT_PROPERTIES = {88: "Control", 89: "Craftsmanship", 115: "CP"}
SLOTS = {16: "Tunic", 17: "Helmet", 19: "Necklace", 21: "Belt", 26: "Leggings", 52: "Ring", 78: "Bracer", 80: "Cloak"}
SKILL_IDS = {"Smithery": 9, "Engineering": 32, "Fabrication": 10, "Agriculture": 31}

def f32(x):
    return struct.unpack("f", struct.pack("f", x))[0]

def better_reward(candidate,best):
    return candidate[0]>best[0]+1e-13 or (abs(candidate[0]-best[0])<=1e-13 and candidate[1]<best[1]-1e-13)

class CraftModel:
    def __init__(self, rank, level, craft=0, control=0, equipment_cp=0, penalty=0):
        self.rank, self.level = rank, level
        self.craft, self.control = craft, control
        self.cp = int(f32(equipment_cp + f32(rank * .75))) + (31 if rank >= 25 else 0)
        chart = json.loads((ROOT / "design/testing/crafting-source-data.json").read_text())["Levels"][str(level)]
        self.durability, self.quality = chart["Durability"], chart["Quality"]
        delta = rank - level
        modifier = f32(-delta * .25) if delta < 0 else min(f32(delta * f32(.05)), .25)
        self.progress = int(f32(chart["Progress"] + f32(chart["Progress"] * modifier))) + penalty
        progress_multiplier = f32(1 + f32(f32(.05) * delta))
        quality_multiplier = progress_multiplier if delta < 0 else 1
        self.syn = [(name, int(f32(f32(base + (21 if rank >= 20 else 0) + f32(craft * f32(.65))) * progress_multiplier)), cp, chance)
                    for name, base, cp, chance, unlock in (("Basic Synthesis",10,0,.9,0),("Rapid Synthesis",30,6,.75,10),("Careful Synthesis",80,10,.5,30)) if rank >= unlock]
        self.touch = [(name, int(f32(f32(base + (115 if rank >= 40 else 0) + f32(control * f32(.75))) * quality_multiplier)), cp, chance)
                      for name, base, cp, chance, unlock in (("Basic Touch",10,3,.9,5),("Standard Touch",30,6,.75,15),("Precise Touch",80,10,.5,35)) if rank >= unlock]

    def describe(self):
        return {"Rank": self.rank, "RecipeLevel": self.level, "Craftsmanship": self.craft, "Control": self.control,
                "CP": self.cp, "Durability": self.durability, "Progress": self.progress, "Quality": self.quality,
                "Synthesis": self.syn, "Touch": self.touch}

    def guaranteed_quality(self):
        """Exact maximum quality with every gain guaranteed (rank >= 40)."""
        assert self.rank >= 40
        @lru_cache(None)
        def solve(cp, dur, waste, vene, remaining):
            if remaining <= 0:
                return 0, 0, ()
            if dur <= 0:
                return -1000000, 1000000, ()
            candidates = []
            loss, w = (5, waste-1) if waste else (10, 0)
            for name, gain, cost, chance in self.syn:
                paid = cost//2 if cost and vene else cost
                v = vene-1 if cost and vene else vene
                if cp >= paid+12:
                    q, clicks, path = solve(cp-paid-12, max(0,dur-loss), w, v, max(0,remaining-gain))
                    candidates.append((q, clicks+2, ("Steady Hand",name)+path))
            for name, gain, cost, chance in self.touch:
                if cp >= cost+12 and dur > loss:
                    q, clicks, path = solve(cp-cost-12,dur-loss,w,vene,remaining)
                    candidates.append((q+gain, clicks+2, ("Muscle Memory",name)+path))
            if cp>=10 and dur<=self.durability-30:
                q,c,path=solve(cp-10,dur+30,waste,vene,remaining)
                candidates.append((q,c+1,("Master's Mend",)+path))
            if cp>=4 and waste==0:
                q,c,path=solve(cp-4,dur,4,vene,remaining)
                candidates.append((q,c+1,("Waste Not",)+path))
            if cp>=8 and vene==0 and dur>loss:
                q,c,path=solve(cp-8,dur-loss,w,4,remaining)
                candidates.append((q,c+1,("Veneration",)+path))
            return max(candidates, key=lambda x:(x[0],-x[1])) if candidates else (-1000000,1000000,())
        q,clicks,path=solve(self.cp,self.durability,0,0,self.progress)
        result={"AchievedQuality": min(self.quality,max(0,q)), "QualityPercent": min(1,max(0,q)/self.quality), "Clicks": clicks,
                "Rotation": path, "States": solve.cache_info().currsize}
        solve.cache_clear()
        return result

    def safe_policy(self, objective="quality", groups=1):
        """Exact adaptive quality-first optimum with a guaranteed synthesis finish.

        No timer exists. A buff may be cast immediately before its consuming action,
        so Steady Hand and Muscle Memory are safely combined with that action.
        The search excludes failed casts, partial/overhealing Mend, and early Waste
        Not refreshes: these cannot improve any of the specified rewards.
        """
        assert self.rank >= 20
        @lru_cache(None)
        def finish(cp,dur,waste,vene,remaining):
            if remaining<=0:
                return 0, ()
            if dur<=0:
                return 100000, ()
            options=[]
            loss,w=(5,waste-1) if waste else (10,0)
            for name,gain,cost,chance in self.syn:
                paid=cost//2 if cost and vene else cost
                v=vene-1 if cost and vene else vene
                if cp>=paid+12:
                    clicks,path=finish(cp-paid-12,max(0,dur-loss),w,v,max(0,remaining-gain))
                    options.append((clicks+2,("Steady Hand",name)+path))
            if self.rank>=10 and cp>=10 and dur<=self.durability-30:
                c,path=finish(cp-10,dur+30,waste,vene,remaining)
                options.append((c+1,("Master's Mend",)+path))
            if self.rank>=8 and cp>=4 and waste==0:
                c,path=finish(cp-4,dur,4,vene,remaining)
                options.append((c+1,("Waste Not",)+path))
            if self.rank>=25 and cp>=8 and vene==0 and dur>loss:
                c,path=finish(cp-8,dur-loss,w,4,remaining)
                options.append((c+1,("Veneration",)+path))
            return min(options,key=lambda x:x[0]) if options else (100000,())
        def reward(q):
            fraction=f32(q/self.quality)
            if objective=="full":
                return float(q>=self.quality)
            if objective=="transfer":
                return (int(f32(fraction*100))/100)**groups
            return fraction
        @lru_cache(None)
        def lower_finish_cost(remaining,vene=0):
            if remaining<=0:return 0
            costs=[]
            for name,gain,cost,chance in self.syn:
                paid=cost//2 if cost and vene else cost
                v=vene-1 if cost and vene else vene
                costs.append(paid+12+lower_finish_cost(max(0,remaining-gain),v))
            if self.rank>=25 and vene==0:
                costs.append(8+lower_finish_cost(remaining,4))
            return min(costs)
        minimum_finish_cp=lower_finish_cost(self.progress)
        optimistic_quality_per_cp=max((gain/cost for name,gain,cost,chance in self.touch),default=0)
        @lru_cache(None)
        def solve(cp,dur,waste,q):
            clicks,path=finish(cp,dur,waste,0,self.progress)
            best=(reward(q),float(clicks),"Finish") if clicks<100000 else (-1,100000.,"None")
            if best[0]<0:return best
            if q>=self.quality:
                return best
            if objective=="full" and q+max(0,cp-minimum_finish_cp)*optimistic_quality_per_cp<self.quality:
                return best
            loss,w=(5,waste-1) if waste else (10,0)
            if dur>loss:
                for name,gain,cost,chance in self.touch:
                    for guaranteed in (False,True) if self.rank>=40 else (False,):
                        paid=cost+(12 if guaranteed else 0)
                        if cp<paid:
                            continue
                        p=1 if guaranteed else chance
                        success=solve(cp-paid,dur-loss,w,min(self.quality,q+gain))
                        failure=solve(cp-paid,dur-loss,w,q) if p<1 else success
                        if success[0]<0 or failure[0]<0:
                            continue
                        candidate=(p*success[0]+(1-p)*failure[0],(2 if guaranteed else 1)+p*success[1]+(1-p)*failure[1], ("Muscle Memory + " if guaranteed else "")+name)
                        if better_reward(candidate,best):
                            best=candidate
            for action,paid,d,w in (("Master's Mend",10,min(self.durability,dur+30),waste),("Waste Not",4,dur,4)):
                if cp<paid or (action=="Master's Mend" and dur>self.durability-30) or (action=="Waste Not" and waste):
                    continue
                v,c,n=solve(cp-paid,d,w,q)
                candidate=(v,c+1,action)
                if v>=0 and better_reward(candidate,best):
                    best=candidate
            return best
        start=(self.cp,self.durability,0,0)
        val,clicks,action=solve(*start)
        # Export the branch on which every touch succeeds; the policy adapts after failures.
        state=start
        path=[]
        while len(path)<200:
            cp,dur,waste,q=state
            v,c,action=solve(*state)
            if action=="Finish":
                path.extend(finish(cp,dur,waste,0,self.progress)[1]);break
            if action=="None":break
            path.append(action)
            if action=="Waste Not":state=(cp-4,dur,4,q)
            elif action=="Master's Mend":state=(cp-10,dur+30,waste,q)
            else:
                name=action.removeprefix("Muscle Memory + ")
                _,gain,cost,chance=next(x for x in self.touch if x[0]==name)
                paid=cost+(12 if action.startswith("Muscle") else 0)
                state=(cp-paid,dur-(5 if waste else 10),max(0,waste-1),min(self.quality,q+gain))
        result={"Objective":objective,"PropertyGroups":groups,"Value":val,"ExpectedClicks":clicks,"SuccessfulTouchBranch":path,
                "States":solve.cache_info().currsize,"FinishStates":finish.cache_info().currsize,"HasGuaranteedFinish":val>=0}
        @lru_cache(None)
        def metrics(cp,dur,waste,q):
            action=solve(cp,dur,waste,q)[2]
            if action in ("Finish","None"):
                if action=="None":return (0.,0.,0.,0.,0.)
                fraction=f32(q/self.quality)
                chance=int(f32(fraction*100))/100
                return (fraction,float(q>=self.quality),chance,chance**2,chance**3)
            if action=="Waste Not":return metrics(cp-4,dur,4,q)
            if action=="Master's Mend":return metrics(cp-10,dur+30,waste,q)
            name=action.removeprefix("Muscle Memory + ")
            _,gain,cost,chance=next(x for x in self.touch if x[0]==name)
            protected=action.startswith("Muscle")
            paid=cost+(12 if protected else 0)
            p=1 if protected else chance
            state=(cp-paid,dur-(5 if waste else 10),max(0,waste-1))
            success=metrics(*state,min(self.quality,q+gain))
            failure=metrics(*state,q) if p<1 else success
            return tuple(p*a+(1-p)*b for a,b in zip(success,failure))
        result["ExpectedQuality"],result["FullQualityChance"],result["AllTransferChance1"],result["AllTransferChance2"],result["AllTransferChance3"]=metrics(*start)
        solve.cache_clear();finish.cache_clear();metrics.cache_clear();lower_finish_cost.cache_clear()
        return result

def equipment_search():
    """Search every 5-point Control/Craftsmanship allocation of crafted enhancements.

    Nine armor/accessory pieces (including two rings) and two level-40+ knives.
    Two ordinary slots: 200 points. Three researched slots: 300 points.
    No food, random blueprint stat rolls, or guaranteed blueprint stat rolls assumed.
    """
    output=ROOT/"design/testing/crafting-equipment-search.json"
    results=json.loads(output.read_text()) if output.exists() else []
    for slots in (2,3):
        pool=slots*100
        for load in range(slots+1):
            if any(r["SlotsPerEquippedItem"]==slots and r["LoadedEnhancements"]==load for r in results):continue
            best=None
            for craft_points in range(0,pool+1,5):
                model=CraftModel(50,50,30+craft_points,29+pool-craft_points,37,load*50)
                result=model.safe_policy("full")
                row={"SlotsPerEquippedItem":slots,"LoadedEnhancements":load,"AllocatedCraftsmanship":craft_points,"AllocatedControl":pool-craft_points,**model.describe(),**result}
                if best is None or (row["FullQualityChance"],-row["ExpectedClicks"])>(best["FullQualityChance"]+1e-12,-best["ExpectedClicks"]):best=row
                if craft_points%50==0:print("SEARCH",slots,load,craft_points,"states",result["States"],flush=True)
            results.append(best)
            output.write_text(json.dumps(results,indent=2),encoding="utf-8")
            print("BEST",slots,load,best["Craftsmanship"],best["Control"],round(best["FullQualityChance"],8),round(best["ExpectedClicks"],3),flush=True)
    return results

def run_benchmarks():
    cases=[("Starter crafting set",0,1,4,4,8,0),
           ("Bare endgame set",50,50,30,29,37,0),
           ("Bare set, one V enhancement",50,50,30,29,37,50),
           ("Bare set, two V enhancements",50,50,30,29,37,100),
           ("Bare set, three V enhancements",50,50,30,29,37,150),
           ("Rank 20 Smithery set",20,20,12,13,23,0),
           ("Rank 25 Smithery set",25,25,12,13,23,0),
           ("Rank 30 Engineering set",30,30,18,18,26,0),
           ("Rank 35 Engineering set",35,35,18,18,26,0),
           ("Rank 40 endgame set",40,40,30,29,37,0),
           ("Rank 50 Espionage",50,50,0,0,0,0)]
    output=[]
    for label,*inputs in cases:
        model=CraftModel(*inputs)
        if model.rank<20:
            gain=model.syn[0][1]
            required=(model.progress+gain-1)//gain
            trials=model.durability//10
            probability=sum(comb(trials,k)*.9**k*.1**(trials-k) for k in range(required,trials+1))
            result={"BasicSynthesisCompletionChance":probability,"RequiredSuccesses":required,"MaximumAttempts":trials}
        else:
            result=model.safe_policy("quality")
            if model.rank>=40:result["GuaranteedQuality"]=model.guaranteed_quality()
        output.append({"Label":label,**model.describe(),**result})
        (ROOT/"design/testing/crafting-rotation-benchmarks.json").write_text(json.dumps(output,indent=2),encoding="utf-8")
        print(label,round(result.get("ExpectedQuality",0),6),round(result.get("AllTransferChance3",0),6),flush=True)
    return output

def difficult_recipe_search():
    results=[]
    for level,load in ((51,0),(52,0),(52,3),(53,0)):
        best=None
        for allocation in range(0,301,5):
            model=CraftModel(50,level,30+allocation,329-allocation,37,load*50)
            result=model.safe_policy("quality")
            if not result["HasGuaranteedFinish"]:continue
            row={"AllocatedCraftsmanship":allocation,"LoadedEnhancements":load,**model.describe(),**result}
            if best is None or row["ExpectedQuality"]>best["ExpectedQuality"]+1e-12:best=row
        results.append(best)
        (ROOT/"design/testing/crafting-difficult-recipes.json").write_text(json.dumps(results,indent=2),encoding="utf-8")
        print("DIFFICULT",level,load,"none" if best is None else (best["Craftsmanship"],best["Control"],best["ExpectedQuality"],best["ExpectedClicks"]),flush=True)
    return results

def refresh_selected_results():
    for name in ("crafting-equipment-search.json","crafting-difficult-recipes.json"):
        path=ROOT/"design/testing"/name
        rows=json.loads(path.read_text())
        for row in rows:
            model=CraftModel(row["Rank"],row["RecipeLevel"],row["Craftsmanship"],row["Control"],37,row["LoadedEnhancements"]*50)
            row.update(model.safe_policy(row["Objective"]))
        path.write_text(json.dumps(rows,indent=2),encoding="utf-8")
    rows=json.loads((ROOT/"design/testing/crafting-equipment-search.json").read_text())
    food=[]
    for base in rows:
        if base["SlotsPerEquippedItem"]!=3:continue
        best=None
        for craft_food,control_food in ((0,7),(6,6)):
            for allocation in range(max(0,base["AllocatedCraftsmanship"]-10),min(300,base["AllocatedCraftsmanship"]+10)+1,5):
                model=CraftModel(50,50,30+allocation+craft_food,329-allocation+control_food,37,base["LoadedEnhancements"]*50)
                row={"LoadedEnhancements":base["LoadedEnhancements"],"AllocatedCraftsmanship":allocation,"FoodCraftsmanship":craft_food,"FoodControl":control_food,**model.describe(),**model.safe_policy("full")}
                if best is None or better_reward((row["Value"],row["ExpectedClicks"]),(best["Value"],best["ExpectedClicks"])):best=row
        food.append(best)
        print("FOOD",best["LoadedEnhancements"],best["FoodCraftsmanship"],best["FoodControl"],best["Craftsmanship"],best["Control"],best["FullQualityChance"],flush=True)
    (ROOT/"design/testing/crafting-food-search.json").write_text(json.dumps(food,indent=2),encoding="utf-8")

def self_check():
    source=json.loads((ROOT/"design/testing/crafting-source-data.json").read_text())
    knife=next(r for r in source["Recipes"] if r["Id"]=="BasicKnife")
    assert knife["BaseXPByRank"][0]==676 and knife["BaseXPByRank"][1]==600
    model=CraftModel(50,50,30,29,37,0)
    assert (model.cp,model.progress,model.quality,model.syn[-1][1],model.touch[0][1])==(105,186,2641,120,146)
    result=model.safe_policy("quality")
    # Independently calculated binomial expectation for the selected 14-touch route.
    assert abs(result["ExpectedQuality"]-14*.9*146/2641)<1e-7
    assert model.guaranteed_quality()["AchievedQuality"]==604
    hard=CraftModel(50,52,280,79,37,0)
    assert (hard.progress,hard.quality)==(754,5172)
    summary=json.loads((ROOT/"design/testing/crafting-content-audit.json").read_text())
    assert all(r["Id"]!="TrapKit5" for r in summary["Training"]["Espionage"]["49"])
    assert not any(r["Id"]=="TrapKit3" for r in summary["Training"]["Espionage"]["27"])
    print("Audit checks passed: XP initialization, perk gates, resource math, binomial expectation, deterministic quality, and level-52 targets.")

def content_summary(items, source):
    recipes=source["Recipes"]
    recipe_by_item=defaultdict(list)
    for recipe in recipes:
        recipe_by_item[recipe["Resref"]].append(recipe)
    gear={}
    for skill,sid in SKILL_IDS.items():
        gear[skill]={}
        for rank in (0,10,20,30,40,50):
            by_slot=defaultdict(list)
            for resref,item in items.items():
                if skill not in item["Stats"] or item["BaseItem"] not in SLOTS or item["Variables"].get("NO_ECONOMY") or not recipe_by_item[resref]:
                    continue
                gates=[ip for ip in item["Properties"] if ip["PropertyName"]==131]
                if any(ip["Subtype"]!=sid or ip["CostValue"]>rank for ip in gates):continue
                # Human sets: droid equipment uses different equip restrictions.
                if resref.startswith(("ds","de","df","da")) and re.search(r"\d{3}$",resref):continue
                by_slot[SLOTS[item["BaseItem"]]].append((resref,item))
            chosen={}
            totals=defaultdict(int)
            for slot,options in by_slot.items():
                # Equal-stat duplicates remain available in the full content export.
                resref,item=max(options,key=lambda x:(sum(x[1]["Stats"][skill].values()),x[1]["Stats"][skill].get("CP",0),x[0]))
                chosen[slot]={"Resref":resref,"Name":item["Name"],"Stats":item["Stats"][skill],"Recipes":[{"Id":r["Id"],"Level":r["Level"],"Slots":r["EnhancementSlots"]} for r in recipe_by_item[resref]]}
                for stat,amount in item["Stats"][skill].items():totals[stat]+=amount*(2 if slot=="Ring" else 1)
            gear[skill][rank]={"Items":chosen,"Totals":dict(totals)}
    training={}
    for skill in list(SKILL_IDS)+["Espionage"]:
        training[skill]={}
        for rank in range(50):
            candidates=[r for r in recipes if r["IsActive"] and r["Skill"]==skill and r["RequiredRank"]<=rank and r["BaseXPByRank"][rank]>0 and r["Components"] and all(gate["Skill"]==skill and gate["Rank"]<=rank for gate in r["PerkSkillGates"])]
            candidates.sort(key=lambda r:(-r["BaseXPByRank"][rank]/sum(r["Components"].values()),len(r["Requirements"]),-r["Level"],r["Id"]))
            training[skill][rank]=[{"Id":r["Id"],"Name":items.get(r["Resref"],{}).get("Name",r["Id"]),"Level":r["Level"],"BaseXP":r["BaseXPByRank"][rank],"DirectInputUnits":sum(r["Components"].values()),"Quantity":r["Quantity"],"Components":r["Components"],"Requirements":r["Requirements"]} for r in candidates[:10]]
    enhancements=[]
    for resref,item in items.items():
        properties=[ip for ip in item["Properties"] if ip["PropertyName"] in (101,102,107,108,109,110,116)]
        if not properties:continue
        enhancements.append({"Resref":resref,"Name":item["Name"],"Properties":properties,
            "ProgressPenalty":sum(p["CostValue"] for p in item["Properties"] if p["PropertyName"]==95),
            "EnhancementLevel":max((p["CostValue"] for p in item["Properties"] if p["PropertyName"]==104),default=0),
            "Recipes":[r["Id"] for r in recipe_by_item[resref]],"Restricted":bool(item["Variables"].get("NO_ECONOMY"))})
    return {"ItemCount":len(items),"RecipeCount":len(recipes),"RecipesBySkill":dict(Counter(r["Skill"] for r in recipes)),"Equipment":gear,"Training":training,"Enhancements":enhancements}

def value(obj, key, default=0):
    return obj.get(key, {}).get("value", default)

def load_items():
    items = {}
    for path in (ROOT / "Module/uti").glob("*.uti.json"):
        try:
            raw = path.read_text(encoding="utf-8-sig")
        except UnicodeDecodeError:
            raw = path.read_text(encoding="cp1252")
        obj = json.loads(raw)
        stats = defaultdict(lambda: defaultdict(int))
        properties = []
        for ip in value(obj, "PropertiesList", []):
            prop = {k: value(ip, k) for k in ("PropertyName", "Subtype", "CostValue", "CostTable", "Param1", "Param1Value")}
            properties.append(prop)
            if prop["PropertyName"] in CRAFT_PROPERTIES:
                stats[SKILLS.get(prop["Subtype"], "Invalid")][CRAFT_PROPERTIES[prop["PropertyName"]]] += prop["CostValue"]
        variables = {value(var, "Name", ""): value(var, "Value", "") for var in value(obj, "VarTable", [])}
        name = value(obj, "LocalizedName", {}).get("0", path.stem)
        items[path.name.removesuffix(".uti.json")] = {
            "Name": name, "BaseItem": value(obj, "BaseItem"), "Stats": {k: dict(v) for k, v in stats.items()},
            "Properties": properties, "Variables": variables,
            "Cost": value(obj, "Cost"), "AddCost": value(obj, "AddCost"),
            "Charges": value(obj, "Charges"), "StackSize": value(obj, "StackSize", 1),
            "Path": str(path.relative_to(ROOT)).replace("\\", "/")}
    return items

if __name__ == "__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--case",help="rank,level,craft,control,equipmentCP,progressPenalty")
    parser.add_argument("--objective",default="quality",choices=("quality","transfer","full","guaranteed"))
    parser.add_argument("--groups",type=int,default=1)
    parser.add_argument("--search-equipment",action="store_true")
    parser.add_argument("--benchmarks",action="store_true")
    parser.add_argument("--difficult-recipes",action="store_true")
    parser.add_argument("--refresh-selected",action="store_true")
    parser.add_argument("--check",action="store_true")
    args=parser.parse_args()
    if args.check:
        self_check()
    elif args.refresh_selected:
        refresh_selected_results()
    elif args.difficult_recipes:
        difficult_recipe_search()
    elif args.benchmarks:
        run_benchmarks()
    elif args.search_equipment:
        equipment_search()
    elif args.case:
        model=CraftModel(*map(int,args.case.split(',')))
        start=time.monotonic()
        result=model.guaranteed_quality() if args.objective=="guaranteed" else model.safe_policy(args.objective,args.groups)
        result.update(model.describe());result["Seconds"]=time.monotonic()-start
        print(json.dumps(result,indent=2))
    else:
        source=json.loads((ROOT/"design/testing/crafting-source-data.json").read_text())
        summary=content_summary(load_items(),source)
        path=ROOT/"design/testing/crafting-content-audit.json"
        path.write_text(json.dumps(summary,indent=2),encoding="utf-8")
        print(f"Saved {summary['RecipeCount']} recipes and {summary['ItemCount']} items: {path}")
        for skill,ranks in summary["Equipment"].items():
            print(skill,[(rank,data["Totals"]) for rank,data in ranks.items()])
