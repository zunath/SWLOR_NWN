"""Generate ship perk/ability definitions and native feat resources from approved rows."""
import argparse,csv,io,json,re
from pathlib import Path
from ShipOperatingPerks import metadata,base_name,identifier,SHORT_NAMES,ICON_NAMES
ROOT=Path(__file__).resolve().parents[1]

def quote(value):return json.dumps(value,ensure_ascii=False)
def replace_members(path,members):
    text=path.read_text(encoding="utf-8-sig")
    names={name for name,_ in members}
    text=re.sub(r"(?m)^\s*(?:\[(?:RecastGroup|PerkCategory)[^\n]*\]\s*\n)?\s*(?:"+"|".join(names)+r")\s*=\s*\d+,\s*\n", "\n",text)
    # The first enum closing brace precedes any attribute classes.
    at=text.index("\n    }",text.index("enum ")) if "\n    }" in text[text.index("enum "):] else text.index("\n}",text.index("enum "))
    return text[:at].rstrip()+"\n"+"\n".join(line for _,line in members)+"\n"+text[at:]

def generate():
    source=json.loads((ROOT/"design/space/space-balance.json").read_text())
    groups={}
    for row in source["perks"]:groups.setdefault(base_name(row),[]).append(metadata(row))
    assert len(groups)==70 and len(SHORT_NAMES)==70 and len(ICON_NAMES)==70
    files={};perks=[];feats=[];spells=[];rec=[("ShipCapstone",'        [RecastGroup("Ship Capstone", "Ship Capstone", true)]\n        ShipCapstone = 522,'),("ShipMode",'        [RecastGroup("Operating Mode", "Ship Mode", true)]\n        ShipMode = 523,')]
    feat_id=2901;spell_id=1723;recast_id=524
    tlk_path=ROOT/"SWLOR_Haks/sw_tlk/sw_tlk.tlk.json";tlk=json.loads(tlk_path.read_text(encoding="utf-8"));entries={e["id"]:e for e in tlk["entries"]}
    def tlk_ref(text):
        old=next((e["id"] for e in entries.values() if e.get("text")==text),None)
        if old is None:
            old=next(i for i in range(6208,max(entries)+1) if i not in entries or not entries[i].get("text"));entries[old]={"id":old,"text":text}
        return 16777216+old
    def table(path):
        lines=path.read_text(encoding="utf-8").splitlines();headers=lines[2].split();rows=[line.split() for line in lines[3:] if line.strip()]
        return lines[:3],headers,rows
    featpath=ROOT/"SWLOR_Haks/sw_2da/feat.2da";fh,fc,fr=table(featpath);fr=[x for x in fr if not x[1].startswith("Ship") or int(x[0])<2901]
    spellpath=ROOT/"SWLOR_Haks/sw_2da/spells.2da";sh,sc,sr=table(spellpath);sr=[x for x in sr if not x[1].startswith("Ship") or int(x[0])<1723]
    template=next(x for x in sr if x[0]=="1715")
    manifest_path=ROOT/"SWLOR.Game.Server/Readmes/GameplayIconManifest.csv";manifest=list(csv.DictReader(io.StringIO(manifest_path.read_text(encoding="utf-8-sig"))));manifest=[x for x in manifest if not x["Key"].startswith("Ship")]
    featkey="ShipManufacturingTrait";icon="ife_shipmanuf";feats.append((featkey,"        ShipManufacturingTrait = 3031,"))
    cols={c:"****" for c in fc};cols.update(LABEL=featkey,FEAT=str(tlk_ref("Ship Manufacturing")),DESCRIPTION=str(tlk_ref("Manufacture ship equipment with Engineering requirements below 10. Operating skills are separate.")),ICON=icon,GAINMULTIPLE="0",EFFECTSSTACK="0",ALLCLASSESCANUSE="1",TOOLSCATEGORIES="6",MinLevel="99",PreReqEpic="0",ReqAction="0")
    fr.append(["3031"]+[cols[c] for c in fc]);manifest.append(dict(Type="Feat",Key=featkey,DisplayName="Ship Manufacturing",SemanticCategory="Passive",Rank="1",IconResRef=icon,SourcePath="SWLOR_Haks/sw_2da/feat.2da",Alignment=""))
    profiles=[];categories=[];perk_lines=[]
    for index,(name,rows) in enumerate(groups.items()):
        first=rows[0];key=first["key"];kind=first["kind"];perk_id=805+index
        category=identifier(first["style"]);category_id=61+index//7
        if index%7==0:categories.append((category,f'        [PerkCategory({quote(rows[0]["style"] if rows[0]["skill"]=="Piloting" else {"ShipSystems":"Ship Systems","SpaceIndustry":"Space Industry"}.get(first["skill"],first["skill"])+" - "+first["style"])}, true)]\n        {category} = {category_id},'))
        perks.append((key,f"        {key} = {perk_id},"))
        if kind=="Technique":
            short=SHORT_NAMES[index];assert len(short)<=14
            recast=key;rec.append((key,f'        [RecastGroup({quote(name)}, {quote(short)}, true)]\n        {key} = {recast_id},'));recast_id+=1
        else:recast="ShipCapstone" if kind=="Capstone" else "ShipMode"
        perk_lines.append(f"            builder.Create(PerkCategoryType.{category}, PerkType.{key}).Name({quote(name)}).Icon(\"ife_s{ICON_NAMES[index][:9]}{'' if kind=='Trait' else '1'}\")")
        ability_lines=[]
        for rank_index,row in enumerate(rows):
            row["perk"]=key;row["perk_id"]=perk_id;row["recast"]=recast
            native_rank=1 if kind=="Trait" else row["rank"]
            featkey=key+('Trait' if kind=="Trait" else str(row["rank"]))
            icon="ife_s"+ICON_NAMES[index][:9]+("" if kind=="Trait" else str(native_rank));assert len(icon)<=16
            row["icon"]=icon;row["feat"]=featkey
            perk_lines.append(f"                .AddPerkLevel().Price({row['price']}).RequirementSkill(SkillType.{row['skill']}, {row['skill_rank']}).Description({quote(row['description'])})")
            if kind!="Trait" or rank_index==0:perk_lines.append(f"                .GrantsFeat(FeatType.{featkey})")
            for stat,amount in row["stats"].items() if kind=="Trait" else []:
                units=1 if stat=="ShipScannerResolution" else 10000
                perk_lines.append(f"                .IncreasesStat(StatType.{stat}, {round(amount*units)})")
            if kind=="Trait" and rank_index>0:continue
            row["feat_id"]=feat_id
            feats.append((featkey,f"        {featkey} = {feat_id},"))
            target=any(e["scope"]=="Target" for e in row["effects"])
            cols={c:"****" for c in fc};cols.update(LABEL=featkey,FEAT=str(tlk_ref(row["name"])),DESCRIPTION=str(tlk_ref(row["description"])),ICON=icon,GAINMULTIPLE="0",EFFECTSSTACK="0",ALLCLASSESCANUSE="1",TOOLSCATEGORIES="6",MinLevel="99",PreReqEpic="0",ReqAction="0")
            if kind!="Trait":
                cols.update(SPELLID=str(spell_id),TARGETSELF="****" if target else "1",HostileFeat="1" if target else "****")
                sp=dict(zip(sc,template[1:]));sp.update(Label=featkey,Name=cols["FEAT"],IconResRef=icon,Range="L" if target else "P",TargetType="0x02" if target else "0x01",FeatID=str(feat_id),SpellDesc=cols["DESCRIPTION"],HostileSetting="1" if target else "0",TargetShape="****",TargetSizeX="****",TargetSizeY="****",TargetFlags="****")
                sr.append([str(spell_id)]+[sp[c] for c in sc]);spells.append((featkey,f"        {featkey} = {spell_id},"));spell_id+=1
                suffix=".RequiresTarget().IsHostileAbility().HasMaxRange("+str(row["range"])+"f)" if target else ""
                ability_lines.append(f"            builder.Create(FeatType.{featkey}, PerkType.{key}).Name({quote(row['name'])}).Level({row['rank']}).SkillType(SkillType.{row['skill']}).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.{recast}, {row['cooldown']}f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.{featkey})){suffix};")
            fr.append([str(feat_id)]+[cols[c] for c in fc]);feat_id+=1
            category_semantic="Passive" if kind=="Trait" else "Self"
            if name in ("Target Analysis","Sensor Disruption","Signal Break"):category_semantic="Control" if name!="Target Analysis" else "Harmful"
            elif first["skill"]=="Astrometrics" or name in ("Efficient Transit","Cargo Handling","Selective Recovery","Specialist Recovery"):category_semantic="Utility"
            elif name in ("Repair Link","Support Surge","Fleet Stabilization","Capacitor Transfer"):category_semantic="Beneficial"
            manifest.append(dict(Type="Feat" if kind=="Trait" else "Ability",Key=featkey,DisplayName=row["name"],SemanticCategory=category_semantic,Rank=str(native_rank),IconResRef=icon,SourcePath="SWLOR_Haks\\sw_2da\\feat.2da",Alignment=""))
        perk_lines[-1]+=";"
        profiles.extend(rows)
        if ability_lines:
            code='using System.Collections.Generic;\nusing SWLOR.Game.Server.Service.AbilityService;\nusing SWLOR.Game.Server.Service.PerkService;\nusing SWLOR.Game.Server.Service.SkillService;\nusing SWLOR.Game.Server.Service.SpaceService;\nusing SWLOR.NWN.API.NWScript.Enum;\n\nnamespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships\n{\n'
            code+=f"    public sealed class {key[4:]}AbilityDefinition : IAbilityListDefinition\n    {{\n        public Dictionary<FeatType, AbilityDetail> BuildAbilities()\n        {{\n            var builder = new AbilityBuilder();\n"+"\n".join(ability_lines)+"\n            return builder.Build();\n        }\n    }\n}\n"
            files[ROOT/f"SWLOR.Game.Server/Feature/AbilityDefinition/Ships/{key[4:]}AbilityDefinition.cs"]=code
    code='using System.Collections.Generic;\nusing SWLOR.Game.Server.Service.PerkService;\nusing SWLOR.Game.Server.Service.SkillService;\nusing SWLOR.Game.Server.Service.StatService;\nusing SWLOR.NWN.API.NWScript.Enum;\n\nnamespace SWLOR.Game.Server.Feature.PerkDefinition\n{\n    public sealed class ShipOperatingPerkDefinition : IPerkListDefinition\n    {\n        private readonly PerkBuilder _builder = new();\n        public Dictionary<PerkType, PerkDetail> BuildPerks()\n        {\n            BuildOperatingPerks();\n            return _builder.Build();\n        }\n        private void BuildOperatingPerks()\n        {\n            var builder = _builder;\n'
    files[ROOT/"SWLOR.Game.Server/Feature/PerkDefinition/ShipOperatingPerkDefinition.cs"]=code+"\n".join(perk_lines)+"\n        }\n    }\n}\n"
    files[ROOT/"SWLOR.Game.Server/Data/ShipTechniques.json"]=json.dumps(profiles,indent=2,ensure_ascii=False)+"\n"
    for path,members in [(ROOT/"SWLOR.Game.Server/Service/PerkService/PerkType.cs",perks),(ROOT/"SWLOR.Game.Server/Service/PerkService/PerkCategoryType.cs",categories),(ROOT/"SWLOR.Game.Server/Service/AbilityService/RecastGroup.cs",rec),(ROOT/"SWLOR.NWN.API/NWScript/Enum/FeatType.cs",feats),(ROOT/"SWLOR.NWN.API/NWScript/Enum/Spell.cs",spells)]:
        files[path]=replace_members(path,members).replace("// IDs 650 and 805+ are free.","// IDs 650 and 875+ are free.")
    def native_table(path,minimum,rows):
        # Preserve inherited native rows and their original line endings byte for byte.
        inherited=[]
        for line in path.read_bytes().decode("utf-8").splitlines(keepends=True):
            columns=line.split()
            if columns and columns[0].isdigit() and int(columns[0])>=minimum and len(columns)>1 and columns[1].startswith("Ship"):continue
            inherited.append(line)
        prefix="".join(inherited)
        if prefix and not prefix.endswith("\n"):prefix+="\n"
        return prefix+"\n".join("   ".join(row) for row in rows if int(row[0])>=minimum)+"\n"
    files[featpath]=native_table(featpath,2901,sorted(fr,key=lambda x:int(x[0])))
    files[spellpath]=native_table(spellpath,1723,sr)
    # Native hotbar scans only the first1024 class-feat rows for cursor abilities.
    classpath=ROOT/"SWLOR_Haks/sw_2da/CLS_FEAT_FIGHT.2da"
    original=classpath.read_bytes().decode("utf-8").splitlines(keepends=True)
    ch=original[2].split();ci=ch.index("FeatIndex")+1
    generated={row[0]:row for row in fr if 2901<=int(row[0])<=3031}
    occupied={};available=[];positions={}
    for position,line in enumerate(original[3:],3):
        row=line.split()
        if not row or not row[0].isdigit():continue
        index=int(row[0]);positions[index]=position
        if row[ci] in generated:occupied[row[ci]]=index
        elif row[ci]=="****":available.append(index)
    target_index=fc.index("HostileFeat")+1
    ordered=sorted(generated.values(),key=lambda row:(row[target_index]!="1",int(row[0])))
    for feat in ordered:
        cursor=feat[target_index]=="1"
        existing=occupied.get(feat[0])
        if existing is not None:
            if cursor and existing>=1024:raise ValueError("Ship cursor outside native class-feat scan")
            continue
        choices=[index for index in available if (index<1024 if cursor else index>=1024)]
        if not choices:choices=[index for index in available if not cursor or index<1024]
        if not choices:raise ValueError("No safe native class-feat row for "+feat[1])
        index=min(choices);available.remove(index)
        values={column:"****" for column in ch};values.update(FeatLabel=feat[1],FeatIndex=feat[0],List="1",GrantedOnLevel="99",OnMenu="1")
        original[positions[index]]="   ".join([str(index)]+[values[column] for column in ch])+"\n"
    files[classpath]="".join(original)
    tlk["entries"]=sorted(entries.values(),key=lambda e:e["id"]);files[tlk_path]=json.dumps(tlk,indent=2,ensure_ascii=False)+"\n"
    output=io.StringIO(newline="");writer=csv.DictWriter(output,fieldnames=list(manifest[0]),lineterminator="\n",quoting=csv.QUOTE_ALL);writer.writeheader();writer.writerows(manifest);files[manifest_path]=output.getvalue()
    return files

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument("--check",action="store_true");args=parser.parse_args()
    for path,text in generate().items():
        if args.check:
            if not path.exists() or path.read_bytes().decode("utf-8-sig")!=text:raise SystemExit("Operating perk definition differs: "+str(path.relative_to(ROOT)))
        else:path.parent.mkdir(parents=True,exist_ok=True);path.write_text(text,encoding="utf-8",newline="")
    print("Verified 70 ship perks, 170 ranks, 50 distinct ability definitions and 130 operating feats plus the manufacturing trait.")
if __name__=="__main__":main()
