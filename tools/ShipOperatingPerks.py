"""Typed operational metadata for the approved ship perk rows.

Numbers for ranks, SP, capacitor and cooldown come from the Design Bible authoring
model. This table declares stat units, scopes and hardware rather than parsing prose.
"""
import re

def identifier(name):
    return "Ship" + re.sub(r"[^A-Za-z0-9]", "", name)

def base_name(row):
    return re.sub(r" (I|II|III)$", "", row["name"]) if row["kind"] in ("Technique", "Trait") else row["name"]

def metadata(row):
    name=base_name(row); v=row["magnitude"]; p=v/100
    result=dict(key=identifier(name),name=row["name"],kind=row["kind"],skill=row["skill"].replace(" ",""),style=row["style"],
                rank=row["rank"],skill_rank=row["skill_rank"],price=row["price"],capacitor=row["capacitor"],cooldown=row["cooldown"],
                description=row["description"],target="Self",range=0,actions=[],designs=[],effects=[],stats={},duration=0,
                next_operation=False,bank=False,preparation=0,channel=0,discovery=False,min_signature=0,paid_leg=False)
    def effect(stat,amount,seconds,scope="Self",once=False,allies_only=False):
        result["effects"].append(dict(stat="Ship"+stat,amount=amount,seconds=seconds,scope=scope,once=once,allies_only=allies_only))
    def hardware(*actions,designs=(),bank=False):
        result["actions"]=list(actions); result["designs"]=list(designs); result["bank"]=bank
    traits={
        "Maneuver Handling":("Speed",p),"Pursuit Efficiency":("PropulsionCapacitorDiscount",p),
        "Navigation Economy":("ServiceDiscount",p),"Hazard Handling":("EnvironmentalMitigation",p),
        "Weapon Handling":("Tracking",p),"Firing Efficiency":("WeaponCapacitorDiscount",p),
        "Ordnance Handling":("OrdnanceTracking",p),"Ammunition Economy":("AmmunitionDiscount",p),
        "Recovery Efficiency":("RepairCapacitorDiscount",p),"Power Discipline":("CapacitorDamageMitigation",p),
        "Projector Handling":("RepairRange",p),"Transfer Efficiency":("TransferEfficiency",p),
        "Survey Resolution":("ScannerResolution",v),"Scan Economy":("ScannerCapacitorDiscount",p),
        "Sensor Handling":("ElectronicRange",p),"Interference Efficiency":("ElectronicCapacitorDiscount",p),
        "Beam Handling":("ResourceRecovery",p),"Industrial Efficiency":("IndustryCapacitorDiscount",p),
        "Component Recovery":("IntactSalvageChance",p),"Freight Efficiency":("ServiceDiscount",p)}
    modes={
        "Pursuit":{"Speed":.05,"Tracking":.10,"ShieldResistance":-10},
        "Cruise":{"Speed":.10,"IncomingDamage":.15,"WeaponOutput":-.20},
        "Precision":{"Tracking":.15,"WeaponOutput":-.10},
        "Siege":{"OrdnanceOutput":.10,"Speed":-.20,"Evasion":-.06},
        "Defensive Routing":{"SelfRecoveryOutput":.10,"WeaponOutput":-.15},
        "Support Routing":{"ExternalRecoveryOutput":.10,"WeaponOutput":-.20},
        "Detailed Survey":{"ScannerResolution":10,"SurveyCycleDuration":.25},
        "Interference":{"InterferenceStrength":.02,"Signature":.15,"WeaponOutput":-.10},
        "Careful Extraction":{"ResourceRecovery":.05,"ReserveRemoval":-.20},
        "Recovery Operations":{"SalvageRecovery":.05,"IntactSalvageChance":.03,"SalvageCycleDuration":.20}}
    if name in traits:
        stat,amount=traits[name]; result["stats"]={"Ship"+stat:amount}; return result
    if name in modes:
        result["stats"]={"Ship"+k:amount for k,amount in modes[name].items()}; return result
    if name=="Intercept":
        result.update(target="Pursuit",range=40);effect("Speed",p,8)
    elif name=="Break Away":
        effect("SoftControlImmunity",1,v);effect("WeaponLock",1,3)
    elif name=="Evasive Maneuver":
        effect("Evasion",p,5);effect("WeaponOutput",-.15,5)
    elif name=="Ace Maneuver":
        effect("Speed",.35,6);effect("Evasion",.12,6);effect("WeaponLock",1,2)
    elif name=="Efficient Transit":
        result["paid_leg"]=True;effect("ServiceDiscount",p,60,once=True)
    elif name=="Hazard Run":effect("EnvironmentalMitigation",p,8)
    elif name=="Emergency Escape":
        effect("Speed",p,10);effect("WeaponLock",1,10)
    elif name=="Safe Passage":
        result["paid_leg"]=True;effect("EnvironmentalMitigation",.5,12);effect("Speed",.25,12);effect("WeaponLock",1,12)
    elif name=="Controlled Burst":
        hardware("Weapon",bank=True);effect("WeaponOutput",p,8,"Hardware");effect("WeaponCapacitorDemand",.20,8,"Hardware")
    elif name=="Tracking Solution":
        hardware("Weapon",bank=True);result.update(target="Hostile",range=40);effect("Tracking",p,10,"Hardware")
    elif name=="Exploit Opening":
        hardware("Weapon",bank=True);result.update(target="ExposedHostile");effect("WeaponOutput",p,8,"Hardware")
    elif name=="Perfect Solution":
        hardware("Weapon",bank=True);effect("Accuracy",.08,10,"Hardware");effect("Tracking",.30,10,"Hardware")
    elif name in ("Prepared Volley","Torpedo Run","Bombardment","Coordinated Salvo"):
        designs={"Torpedo Run":["torpedo"],"Bombardment":["heavy_missile","bombardment"],"Prepared Volley":[],"Coordinated Salvo":[]}[name]
        hardware("Weapon",designs=designs,bank=True);result["ordnance"]=True
        duration=8 if name=="Torpedo Run" else 10
        result["min_signature"]={"Torpedo Run":100,"Bombardment":200,"Coordinated Salvo":100}.get(name,0)
        if result["min_signature"]:result["target"]="Hostile"
        effect("OrdnanceOutput",.30 if name=="Coordinated Salvo" else p,duration,"Hardware",True)
        if name=="Torpedo Run":effect("Speed",-.20,duration)
        if name=="Coordinated Salvo":
            result["preparation"]=3;effect("AmmunitionDemand",.50,duration,"Hardware",True)
    elif name in ("Emergency Repair","Power Routing","Shield Recovery","Damage Control"):
        hardware("SelfShieldRepair","SelfHullRepair")
        if name=="Shield Recovery":hardware("SelfShieldRepair")
        duration={"Emergency Repair":8,"Power Routing":10,"Shield Recovery":8,"Damage Control":8}[name]
        effect("SelfRecoveryOutput",.30 if name=="Damage Control" else p,duration,"Hardware",name=="Shield Recovery")
        if name=="Power Routing":effect("WeaponOutput",-p,duration)
        if name=="Damage Control":
            effect("HullResistance",25,8);effect("ShieldResistance",25,8)
    elif name in ("Repair Link","Support Surge","Fleet Stabilization"):
        hardware("ShieldRepair","HullRepair","RepairField",bank=True)
        duration=8 if name=="Support Surge" else 10
        if name=="Repair Link":result["target"]="Allied"
        effect("ExternalRecoveryOutput",.30 if name=="Fleet Stabilization" else p,duration,"Hardware")
        if name=="Support Surge":effect("CapacitorDemand",.20,duration,"Hardware")
        if name=="Fleet Stabilization":effect("SupportRecipients",3,10,"Hardware")
    elif name=="Capacitor Transfer":
        hardware("CapacitorTransfer");effect("TransferEfficiency",(v-80)/100,8,"Hardware",True)
    elif name in ("Deposit Analysis","Anomaly Scan","Deep Survey"):
        hardware("Survey");result["target"]="Anomaly" if name=="Anomaly Scan" else "Site"
        effect("ScannerResolution",25 if name=="Deep Survey" else v,10,"Hardware",True)
        if name=="Deep Survey":result.update(channel=15,discovery=True,designs=["deep_scanner"])
    elif name=="Route Survey":
        hardware("Survey");result.update(paid_leg=True,preparation=8);effect("EnvironmentalMitigation",p,60)
    elif name=="Target Analysis":
        hardware("Survey","Passive",designs=["survey_scanner","deep_scanner","tracking_computer","precision_array"])
        result.update(target="Hostile",range=35);effect("IncomingAccuracy",p,10,"Target",allies_only=True)
    elif name=="Sensor Disruption":
        hardware("Interference");effect("InterferenceStrength",p,8,"Hardware",True)
    elif name=="Countermeasure Timing":
        hardware("Countermeasures");effect("CountermeasureStrength",p,8,"Hardware",True)
    elif name=="Signal Break":
        hardware("Interference");result.update(target="Hostile",range=30,preparation=2,hard_control=True);effect("ActivationLock",1,3,"Target")
    elif name in ("Precision Extraction","Extraction Surge","Selective Recovery","Deep-Core Extraction"):
        hardware("Extraction")
        if name=="Precision Extraction":
            effect("ResourceRecovery",p,18,"Hardware");effect("ReserveRemoval",-.10,18,"Hardware")
        elif name=="Extraction Surge":
            effect("ReserveRemoval",p,18,"Hardware");effect("CapacitorDemand",.20,18,"Hardware")
        elif name=="Selective Recovery":
            result.update(target="SurveyedSite",designs=["precision_cutter","deep_drill","compact_drill"],selection=True)
            effect("SelectedRecovery",p,30,"Hardware",True)
        else:
            result.update(target="Site",designs=["deep_drill","compact_drill"],channel=20,movement_lock=True)
            effect("ExtractionHardness",10,30,"Hardware",True);effect("ResourceRecovery",.08,30,"Hardware",True)
    elif name in ("Careful Dismantling","Recovery Sweep","Cargo Handling","Specialist Recovery"):
        if name=="Cargo Handling":effect("LoadingSpeed",p,60,once=True)
        elif name=="Recovery Sweep":
            hardware("BulkSalvage");effect("ReserveRemoval",p,18,"Hardware");effect("CapacitorDemand",.20,18,"Hardware")
        else:
            hardware("IntactSalvage");effect("IntactSalvageChance",.08 if name=="Specialist Recovery" else p,10 if name=="Careful Dismantling" else 30,"Hardware",True)
            if name=="Specialist Recovery":result.update(target="Site",channel=25,discovery=True,difficult_component=True)
    else:raise ValueError("Missing operational metadata: "+name)
    return result

SHORT_NAMES = ["Intercept","Break Away","Evasive","Handling","Pursuit Econ","Pursuit","Ace Maneuver",
 "Transit","Hazard Run","Escape","Nav Economy","Hazard Skill","Cruise","Safe Passage",
 "Burst","Track Solution","Opening","Gun Handling","Firing Econ","Precision","Perfect Aim",
 "Volley","Torpedo Run","Bombardment","Ord Handling","Ammo Economy","Siege","Salvo",
 "Emergency Fix","Power Routing","Shield Recover","Repair Econ","Power Skill","Defense","Damage Control",
 "Repair Link","Cap Transfer","Support Surge","Projectors","Transfer Skill","Support","Fleet Stabilize",
 "Deposit Scan","Anomaly Scan","Route Survey","Survey Skill","Scan Economy","Detailed Scan","Deep Survey",
 "Target Analyze","Disruption","Counter Timing","Sensor Skill","EWar Economy","Interference","Signal Break",
 "Precision Mine","Mine Surge","Select Ore","Beam Handling","Industry Econ","Careful Mine","Deep Core",
 "Dismantle","Sweep","Cargo Handling","Components","Freight Econ","Recovery","Specialist"]
ICON_NAMES = ["intercept","breakaway","evade","maneuver","pursuitecon","pursuit","ace",
 "transit","hazardrun","escape","navecon","hazard","cruise","passage",
 "burst","tracksol","opening","gunhandle","firingecon","precision","perfectaim",
 "volley","torprun","bombard","ordhandle","ammoecon","siege","salvo",
 "emergrepair","pwrroute","shieldrec","repairecon","pwrdisc","defroute","dmgcontrol",
 "repairlink","captransfer","supportsurge","projector","transfereff","supportroute","fleetstab",
 "deposcan","anomscan","routesurvey","surveyres","scanecon","detailsurvey","deepsurvey",
 "targetanalyze","disruption","countertime","sensor","ewarecon","interference","signalbreak",
 "precismine","minesurge","selectore","beamhandle","industryecon","carefulmine","deepcore",
 "dismantle","recoverysweep","cargohandle","components","freightecon","recovery","specialist"]
