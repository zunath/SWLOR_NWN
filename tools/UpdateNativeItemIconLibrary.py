"""Replace and audit the complete native inventory artwork library.

Use --apply after importing reviewed replacements. Preserve unused numeric slots
as original artwork aliases; fail closed on unreviewed gameplay references.
"""
import argparse
import csv
import hashlib
import json
import re
from pathlib import Path

from UpdateShipItemIcons import base_classes, fields, read_text

ROOT=Path(__file__).resolve().parents[1]
DATA=ROOT/'SWLOR.Game.Server/Readmes'
BANK=re.compile(r'iit_ess[2-9]?_\d{3}')
COOLDOWN=re.compile(r'pr[0-5]_ess[2-9]?_\d{3}')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply',action='store_true')
    args=parser.parse_args()
    classes=base_classes();approved=set();known={};required=set();problems=[]
    for name in ('ItemIconBindings.csv','ShipItemIconBindings.csv'):
        with (DATA/name).open(newline='',encoding='utf-8-sig') as stream:
            for row in csv.DictReader(stream):
                approved.add(row['InventoryIcon']);known[row['ResRef']]=row
    for name in ('ItemIconManifest.csv','ShipItemIconManifest.csv'):
        with (DATA/name).open(newline='',encoding='utf-8-sig') as stream:
            for row in csv.DictReader(stream):
                for path in (ROOT/row['SourcePath'],ROOT/f'SWLOR_Haks/sw_ability/{row["IconResRef"]}.tga'):
                    if not path.exists():problems.append(f'Missing original artwork: {path.relative_to(ROOT)}')
    for icon in approved:
        if not (ROOT/f'SWLOR_Haks/sw_item/{icon}.tga').exists():problems.append(f'Missing inventory alias: {icon}')
    def visit(obj,path):
        if isinstance(obj,dict):
            data=fields(obj);bank=classes.get(data.get('BaseItem'),'');model=data.get('ModelPart1')
            if re.fullmatch(r'it_ess[2-9]?',bank) and isinstance(model,int):
                icon=f'i{bank}_{model:03}';required.add(icon)
                if icon not in approved:problems.append(f'Unreviewed embedded/blueprint appearance: {path}: {data.get("TemplateResRef", "")} -> {icon}')
            for value in obj.values():visit(value,path)
        elif isinstance(obj,list):
            for value in obj:visit(value,path)
    for path in (ROOT/'Module').rglob('*.json'):visit(json.loads(read_text(path)[0]),path.relative_to(ROOT))
    for folder in ('SWLOR.Game.Server','Module','SWLOR_Haks/sw_2da'):
        for path in (ROOT/folder).rglob('*'):
            if path.suffix not in ('.cs','.json','.2da','.nss'):continue
            for icon in re.findall(r'\b(?:iit_|pr[0-5]_)ess[2-9]?_\d{3}\b',path.read_bytes().decode('latin1')):
                if icon not in approved:problems.append(f'Unreviewed literal resource reference: {path.relative_to(ROOT)} -> {icon}')
    if problems:
        raise ValueError('\n'.join(problems))
    catalogue=ROOT/'SWLOR_Haks/sw_item_source/general/native-library.csv'
    import shutil, subprocess, tempfile
    bindings={}
    for name in ('ItemIconBindings.csv','ShipItemIconBindings.csv'):
        with (DATA/name).open(newline='',encoding='utf-8-sig') as stream:
            for row in csv.DictReader(stream):bindings[row['InventoryIcon']]=row['ActionIcon']
    paths=sorted((ROOT/'SWLOR_Haks/sw_item').glob('iit_ess*.tga'))
    rows=[]
    originals={}
    if catalogue.exists():
        with catalogue.open(newline='',encoding='utf-8') as stream:
            originals={row['Resource']:row['PreviousSHA256'] for row in csv.DictReader(stream)}
    for index,path in enumerate(paths):
        if not BANK.fullmatch(path.stem):continue
        choices=sorted(icon for icon in approved if icon.rsplit('_',1)[0]==path.stem.rsplit('_',1)[0])
        if not choices:choices=sorted(approved)
        alias=path.stem if path.stem in approved else choices[index % len(choices)]
        resource=f'sw_item/{path.name}'
        before=originals.get(resource,hashlib.sha256(path.read_bytes()).hexdigest())
        if args.apply and alias!=path.stem:shutil.copyfile(ROOT/f'SWLOR_Haks/sw_item/{alias}.tga',path)
        rows.append(dict(Resource=resource,SourceResource=f'sw_item/{alias}.tga',ActionIcon=bindings[alias],Usage='Gameplay' if path.stem in approved else 'Compatibility library alias',PreviousSHA256=before,SHA256=hashlib.sha256(path.read_bytes()).hexdigest()))
    mapping={Path(row['Resource']).stem:row for row in rows}
    frames=sorted(path for path in (ROOT/'SWLOR_Haks/sw_ability').glob('pr?_ess*.tga') if COOLDOWN.fullmatch(path.stem))
    before_frames={path.name:originals.get(f'sw_ability/{path.name}',hashlib.sha256(path.read_bytes()).hexdigest()) for path in frames}
    if args.apply:
        with tempfile.TemporaryDirectory(prefix='swlor-original-library-') as temporary:
            names=sorted(set('iit_'+path.stem[4:] for path in frames))
            for name in names:shutil.copyfile(ROOT/f'SWLOR_Haks/sw_ability/{mapping[name]["ActionIcon"]}.tga',Path(temporary)/f'{name}.tga')
            subprocess.run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File',str(ROOT/'tools/GenerateCooldownIcons.ps1'),'-SourceIconPath',temporary,'-IconResRefs',','.join(names),'-Force'],cwd=ROOT,check=True)
    for path in frames:
        source=mapping['iit_'+path.stem[4:]]
        rows.append(dict(Resource=f'sw_ability/{path.name}',SourceResource=f'sw_ability/{source["ActionIcon"]}.tga',ActionIcon=source['ActionIcon'],Usage='Compatibility recharge alias',PreviousSHA256=before_frames[path.name],SHA256=hashlib.sha256(path.read_bytes()).hexdigest()))
    if args.apply:
        with catalogue.open('w',newline='',encoding='utf-8') as stream:
            writer=csv.DictWriter(stream,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
    else:
        with catalogue.open(newline='',encoding='utf-8') as stream:recorded=list(csv.DictReader(stream))
        if rows!=recorded:raise ValueError('Original library catalogue hashes or mappings do not match resources; run --apply after importing art.')
        for row in rows:
            if row['Usage']!='Compatibility recharge alias' and row['SHA256']!=hashlib.sha256((ROOT/'SWLOR_Haks'/row['SourceResource']).read_bytes()).hexdigest():raise ValueError(f'Alias artwork differs: {row["Resource"]}')
    print(f'{len(approved)} reviewed aliases cover {len(required)} referenced resources; {len(paths)} inventory textures and {len(frames)} recharge textures use original artwork. No resource slots deleted.')


if __name__=='__main__':main()
