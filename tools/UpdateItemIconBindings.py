"""Apply reviewed original inventory artwork without changing item gameplay data.

Published native aliases are stable. Allocate once, then reuse the checked-in bindings.
All item classes, stack limits, properties, names, tags and recipe references are preserved.
"""
import argparse
import csv
import io
import json
import re
from collections import defaultdict
from pathlib import Path

from UpdateShipItemIcons import base_classes, fields, read_text, write_csv

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'SWLOR.Game.Server/Readmes'
ART = ROOT / 'SWLOR_Haks/sw_item_source/general/item-artwork.json'
BINDINGS = DATA / 'ItemIconBindings.csv'
MANIFEST = DATA / 'ItemIconManifest.csv'


def allocate(assets):
    if BINDINGS.exists():
        raise ValueError('Bindings already exist; reuse published aliases with --apply.')
    classes = base_classes()
    reserved = defaultdict(set)
    with (DATA / 'ShipItemIconBindings.csv').open(newline='', encoding='utf-8') as stream:
        for row in csv.DictReader(stream):
            reserved[classes[int(row['BaseItem'])]].add(int(row['NewModel']))
    reviewed = {resref for asset in assets for resref in asset['items']}
    original_models = {resref: fields(json.loads(read_text(ROOT / f'Module/uti/{resref}.uti.json')[0]))['ModelPart1']
                       for resref in reviewed}
    aliases = {}
    # Preserve customized/anonymous embedded item appearances and literal resources.
    def visit(obj):
        if isinstance(obj, dict):
            data = fields(obj)
            item_class = classes.get(data.get('BaseItem'), '')
            model = data.get('ModelPart1')
            resref = data.get('TemplateResRef', '')
            if item_class.startswith('it_ess') and isinstance(model, int) and (resref not in reviewed or model != original_models[resref]):
                reserved[item_class].add(model)
            for value in obj.values():
                visit(value)
        elif isinstance(obj, list):
            for value in obj:
                visit(value)
    for path in (ROOT / 'Module').rglob('*.json'):
        visit(json.loads(read_text(path)[0]))
    for folder in ('SWLOR.Game.Server', 'Module', 'SWLOR_Haks/sw_2da'):
        for path in (ROOT / folder).rglob('*'):
            if path.suffix not in ('.cs', '.json', '.2da', '.nss'):
                continue
            for bank, model in re.findall(r'i(it_ess[2-9]?)_(\d{3})', path.read_bytes().decode('latin1')):
                reserved[bank].add(int(model))
    # Reference scans exclude CSVs: old migration models deliberately remain recognized,
    # while final ship aliases are reserved above and never repurposed.
    source_texts = [(str(path.relative_to(ROOT)).replace('\\', '/'), path.read_bytes().decode('latin1'))
                    for path in (ROOT / 'SWLOR.Game.Server/Feature').rglob('*.cs')]
    rows = []
    for asset in assets:
        for resref in asset['items']:
            path = ROOT / f'Module/uti/{resref}.uti.json'
            data = fields(json.loads(read_text(path)[0]))
            item_class = classes[data['BaseItem']]
            key = item_class, asset['key']
            if key not in aliases:
                model = next((number for number in range(1, 255) if number not in reserved[item_class]), None)
                if model is None:
                    raise ValueError(f'No model slot for {item_class}: {asset["name"]}')
                aliases[key] = model
                reserved[item_class].add(model)
            variables = [fields(value) for value in data.get('VarTable', [])]
            internal = any(value.get('Name') == 'NO_ECONOMY' and value.get('Value') == 1 for value in variables)
            name = data.get('LocalizedName', {}).get('0', resref)
            rows.append(dict(ResRef=resref, DisplayName=name, Role=asset['key'], BaseItem=data['BaseItem'],
                OldModel=data['ModelPart1'], NewModel=aliases[key],
                OldIcon=f'i{item_class}_{data["ModelPart1"]:03}',
                InventoryIcon=f'i{item_class}_{aliases[key]:03}', ActionIcon=asset['icon'],
                SemanticCategory=asset['category'], GameplayUse=asset['name'],
                Sources=';'.join(source for source, text in source_texts if f'"{resref}"' in text),
                Internal=str(internal).lower()))
    write_csv(BINDINGS, sorted(rows, key=lambda row: row['ResRef']))
    return rows


def apply(rows):
    by_resref = {row['ResRef']: row for row in rows}
    with (DATA / 'ShipItemIconBindings.csv').open(newline='', encoding='utf-8-sig') as stream:
        by_resref.update((row['ResRef'], row) for row in csv.DictReader(stream))
    def update(text):
        # Find the smallest object containing a TemplateResRef key, respecting quoted
        # strings/escaped braces. Replace only its direct appearance value spans.
        stack, spans = [], []
        for token in re.finditer(r'"(?:\\.|[^"\\])*"|[{}]', text):
            if token[0] == '{': stack.append(token.start())
            elif token[0] == '}': spans.append((stack.pop(), token.end()))
        edits = []
        for match in re.finditer(r'"TemplateResRef"\s*:\s*\{\s*"type"\s*:\s*"resref",\s*"value"\s*:\s*"([^"\\]+)"', text):
            row = by_resref.get(match[1])
            if not row: continue
            start, end = min((span for span in spans if span[0] < match.start() < span[1]), key=lambda span: span[1]-span[0])
            fragment = text[start:end]
            values = fields(json.loads(fragment))
            if values.get('BaseItem') != int(row['BaseItem']) or values.get('ModelPart1') != int(row['OldModel']): continue
            for field in ('ModelPart1', 'xModelPart1'):
                appearance = re.search(r'"'+field+r'"\s*:\s*\{\s*"type"\s*:\s*"(?:byte|word)",\s*"value"\s*:\s*(\d+)', fragment)
                if appearance: edits.append((start+appearance.start(1), start+appearance.end(1), str(row['NewModel'])))
        for start, end, replacement in sorted(edits, reverse=True): text = text[:start]+replacement+text[end:]
        return text, bool(edits)
    # Standalone blueprints receive surgical edits, preserving authored formatting.
    for row in rows:
        path = ROOT / f'Module/uti/{row["ResRef"]}.uti.json'
        text, encoding = read_text(path)
        original = fields(json.loads(text))['ModelPart1']
        if original not in (int(row['OldModel']), int(row['NewModel'])):
            raise ValueError(f'Customized blueprint model changed unexpectedly: {path}')
        for field in ('ModelPart1', 'xModelPart1'):
            text = re.sub(r'("' + field + r'"\s*:\s*\{\s*"type"\s*:\s*"(?:byte|word)",\s*"value"\s*:\s*)\d+',
                          lambda match: match[1] + str(row['NewModel']), text)
        path.write_bytes(text.encode(encoding))
    # Embedded inventories are rendered only when a recognized old appearance changes.
    for path in (ROOT / 'Module').rglob('*.json'):
        if path.parent.name == 'uti': continue
        text, encoding = read_text(path)
        updated, changed = update(text)
        if changed: path.write_bytes(updated.encode(encoding))
    from GenerateItemIconMigration import generate_migration
    generate_migration()


def manifest(assets):
    rows = [dict(Type='Item', Key=asset['key'], DisplayName=asset['name'], SemanticCategory=asset['category'],
        Rank='', IconResRef=asset['icon'], SourcePath=f'SWLOR_Haks/sw_item_source/general/{asset["source"]}', Alignment='')
        for asset in assets]
    keys = {row['IconResRef'] for row in rows}
    if MANIFEST.exists():
        with MANIFEST.open(newline='', encoding='utf-8-sig') as stream:
            keys.update(row['IconResRef'] for row in csv.DictReader(stream))
    write_csv(MANIFEST, rows)
    target = DATA / 'GameplayIconManifest.csv'
    original = target.read_bytes()
    existing = original.decode('utf-8-sig').splitlines(keepends=True)
    existing = [line for line in existing if not any(f'"{key}"' in line or f',{key},' in line for key in keys)]
    buffer = io.StringIO(newline='')
    writer = csv.writer(buffer, quoting=csv.QUOTE_ALL, lineterminator='\r\n')
    writer.writerows(list(row.values()) for row in rows)
    position = next((i for i,line in enumerate(existing) if line.startswith(('"Spell",','"StatusEffect",'))),len(existing))
    existing.insert(position,buffer.getvalue())
    target.write_bytes((b'\xef\xbb\xbf' if original.startswith(b'\xef\xbb\xbf') else b'')+''.join(existing).encode('utf-8'))


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--allocate',action='store_true')
    parser.add_argument('--apply',action='store_true')
    args=parser.parse_args()
    assets=json.loads(ART.read_text(encoding='utf-8-sig'))['assets']
    if args.allocate: rows=allocate(assets)
    else:
        with BINDINGS.open(newline='',encoding='utf-8') as stream: rows=list(csv.DictReader(stream))
    manifest(assets)
    if args.apply: apply(rows)
    print(f'{len(rows)} item blueprints; {len(assets)} original artwork families.')


if __name__=='__main__': main()
