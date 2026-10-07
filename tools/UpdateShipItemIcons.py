"""Bind original ship artwork to native inventory appearances without changing item behavior.

The artwork specification is checked into the HAK repository. This tool allocates only
unused native model slots, records every affected blueprint and its gameplay sources,
and regenerates the runtime appearance catalogue from that reviewable CSV.
"""
import argparse
import csv
import io
import json
import re
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'SWLOR.Game.Server/Readmes'
AUDIT = DATA / 'ShipItemIconBindings.csv'
ART = ROOT / 'SWLOR_Haks/sw_item_source/ship-artwork.json'


def read_text(path):
    raw = path.read_bytes()
    try:
        return raw.decode('utf-8-sig'), 'utf-8-sig' if raw.startswith(b'\xef\xbb\xbf') else 'utf-8'
    except UnicodeDecodeError:
        return raw.decode('cp1252'), 'cp1252'


def write_csv(path, rows):
    with path.open('w', encoding='utf-8', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def fields(obj):
    return {key: value.get('value') for key, value in obj.items() if isinstance(value, dict)}


def matches(resref, pattern):
    return resref == pattern[1:] if pattern.startswith('=') else resref.startswith(pattern)


def base_classes():
    lines = (ROOT / 'SWLOR_Haks/sw_2da/baseitems.2da').read_text().splitlines()
    columns = next(line.split() for line in lines if 'ItemClass' in line and 'DefaultIcon' in line)
    return {int(row[0]): dict(zip(columns, row[1:]))['ItemClass']
            for line in lines if re.match(r'^\d+\s', line) for row in [line.split()]}


def collect():
    spec = json.loads(ART.read_text(encoding='utf-8-sig'))
    classes = base_classes()
    occupied = defaultdict(set)
    items = []
    for path in sorted((ROOT / 'Module/uti').glob('*.uti.json')):
        text, encoding = read_text(path)
        data = fields(json.loads(text))
        base, model = data.get('BaseItem'), data.get('ModelPart1')
        item_class = classes.get(base, '')
        if re.fullmatch(r'it_ess[2-9]?', item_class) and model is not None:
            occupied[item_class].add(model)
        resref = path.name.removesuffix('.uti.json')
        selected = [a for a in spec['assets'] if any(matches(resref, p) for p in a['patterns'].split(','))]
        if len(selected) > 1:
            raise ValueError(f'Ambiguous role: {resref}: {selected}')
        if selected:
            assert re.fullmatch(r'it_ess[2-9]?', item_class), (resref, item_class)
            items.append((path, encoding, data, selected[0], item_class))

    # Reserve literal resources used outside inventory blueprints too.
    for folder in ['SWLOR.Game.Server', 'Module', 'SWLOR_Haks/sw_2da']:
        for path in (ROOT / folder).rglob('*'):
            if path.suffix not in ('.cs', '.json', '.2da', '.nss'):
                continue
            # Resource identifiers are ASCII even in legacy scripts or non-UTF8 assets.
            for item_class, model in re.findall(r'i(it_ess[2-9]?)_(\d{3})', path.read_bytes().decode('latin1')):
                occupied[item_class].add(int(model))

    # Placed or embedded native inventories can contain appearances without string resources.
    def reserve(obj):
        if isinstance(obj, dict):
            data = fields(obj)
            item_class = classes.get(data.get('BaseItem'), '')
            if re.fullmatch(r'it_ess[2-9]?', item_class) and isinstance(data.get('ModelPart1'), int):
                occupied[item_class].add(data['ModelPart1'])
            for value in obj.values():
                reserve(value)
        elif isinstance(obj, list):
            for value in obj:
                reserve(value)
    for path in (ROOT / 'Module').rglob('*.json'):
        reserve(json.loads(read_text(path)[0]))
    return spec, items, occupied


def generate_bindings():
    spec, items, occupied = collect()
    aliases = {}
    for _, _, _, asset, item_class in items:
        key = asset['key'], item_class
        if key not in aliases:
            free = next((n for n in range(254, 0, -1) if n not in occupied[item_class]), None)
            if free is None:
                raise ValueError(f'No unused native icon slot in {item_class}')
            aliases[key] = free
            occupied[item_class].add(free)
    sources = []
    for folder in ['SWLOR.Game.Server/Feature', 'Module']:
        for path in (ROOT / folder).rglob('*'):
            if path.suffix in ('.cs', '.json') and path.parent.name != 'uti':
                sources.append((path.relative_to(ROOT).as_posix(), read_text(path)[0]))
    bindings = []
    for path, _, data, asset, item_class in items:
        resref = path.name.removesuffix('.uti.json')
        refs = [name for name, text in sources if f'"{resref}"' in text]
        new_model = aliases[asset['key'], item_class]
        bindings.append(dict(ResRef=resref, DisplayName=data['LocalizedName'].get('0', ''),
            Role=asset['key'], BaseItem=data['BaseItem'], OldModel=data['ModelPart1'], NewModel=new_model,
            OldIcon=f'i{item_class}_{data["ModelPart1"]:03}', InventoryIcon=f'i{item_class}_{new_model:03}',
            ActionIcon=asset['icon'], SemanticCategory=asset['category'],
            Use=asset['subject'], Sources=';'.join(refs), Internal=data.get('VarTable', [])))
    # Internal carries the blueprint's explicit availability restriction, not a guessed name heuristic.
    for row in bindings:
        row['Internal'] = str(any(v.get('Name', {}).get('value') == 'NO_ECONOMY' and
                                 v.get('Value', {}).get('value') == 1 for v in row['Internal'])).lower()
    write_csv(AUDIT, bindings)
    return spec, bindings


def apply_bindings(bindings):
    for row in bindings:
        path = ROOT / f'Module/uti/{row["ResRef"]}.uti.json'
        text, encoding = read_text(path)
        current = fields(json.loads(text))['ModelPart1']
        if current not in (int(row['OldModel']), int(row['NewModel'])):
            raise ValueError(f'Unexpected customized blueprint appearance: {path}: {current}')
        text, count = re.subn(r'("ModelPart1"\s*:\s*\{\s*"type"\s*:\s*"byte",\s*"value"\s*:\s*)\d+',
                             lambda m: m[1] + str(row['NewModel']), text)
        assert count == 1, path
        text = re.sub(r'("xModelPart1"\s*:\s*\{\s*"type"\s*:\s*"word",\s*"value"\s*:\s*)\d+',
                      lambda m: m[1] + str(row['NewModel']), text)
        path.write_bytes(text.encode(encoding))


def generate_manifest(spec):
    rows = [dict(Type='Item', Key=a['key'], DisplayName=a['name'], SemanticCategory=a['category'],
                 Rank='', IconResRef=a['icon'], SourcePath=f'SWLOR_Haks/sw_item_source/{a["source"]}', Alignment='')
            for a in spec['assets']]
    ship_manifest = DATA / 'ShipItemIconManifest.csv'
    owned = {row['IconResRef'] for row in rows}
    if ship_manifest.exists():
        with ship_manifest.open(encoding='utf-8-sig', newline='') as stream:
            owned.update(row['IconResRef'] for row in csv.DictReader(stream))
    write_csv(ship_manifest, rows)
    # Preserve the existing gameplay manifest byte layout and unrelated entries.
    # In particular, a ship-artwork update must not enroll unfinished status effects.
    main = DATA / 'GameplayIconManifest.csv'
    raw = main.read_bytes()
    original = raw.decode('utf-8-sig')
    lines = [line for line in original.splitlines(keepends=True)
             if not (line.startswith(('"Item",', 'Item,')) and
                     any(f'"{icon}"' in line or f',{icon},' in line for icon in owned))]
    output = io.StringIO(newline='')
    writer = csv.writer(output, quoting=csv.QUOTE_ALL, lineterminator='\r\n')
    writer.writerows([list(row.values()) for row in sorted(rows, key=lambda r: r['Key'])])
    position = next((index for index, line in enumerate(lines)
                     if line.startswith(('"Spell",', '"StatusEffect",'))), len(lines))
    lines.insert(position, output.getvalue())
    main.write_bytes((b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'') + ''.join(lines).encode('utf-8'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--allocate', action='store_true', help='Initial allocation only; never rerun after deployment.')
    parser.add_argument('--apply', action='store_true', help='Update blueprints and one-off migration from existing bindings.')
    args = parser.parse_args()
    if args.allocate:
        if AUDIT.exists():
            parser.error('Bindings already exist. Reuse them with --apply; do not change published appearance IDs.')
        spec, bindings = generate_bindings()
    else:
        spec = json.loads(ART.read_text(encoding='utf-8-sig'))
        with AUDIT.open(encoding='utf-8', newline='') as f:
            bindings = list(csv.DictReader(f))
    generate_manifest(spec)
    if args.apply:
        apply_bindings(bindings)
        from GenerateItemIconMigration import generate_migration
        generate_migration()
    print(f'{len(bindings)} ship blueprints; {len(spec["assets"])} original artwork roles; '
          f'{len(set(r["InventoryIcon"] for r in bindings))} native inventory resources.')


if __name__ == '__main__':
    main()
