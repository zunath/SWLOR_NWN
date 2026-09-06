"""Validate NPC portrait sizes against stock resources and the checked-out HAKs.

The stock inventory contains resource names only, extracted from NWN:EE KEY files.
Huge is deliberately excluded: the client falls back to Large for that size.
"""
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]


def read_json(path):
    data = path.read_bytes()
    try:
        text = data.decode('utf-8-sig')
    except UnicodeDecodeError:
        # Two legacy UTC descriptions still use Windows-1252 punctuation.
        text = data.decode('cp1252')
    return json.loads(text)


def portrait_table(root=ROOT):
    # NWN addresses 2DA rows by physical index, not the decorative row label.
    lines = (root / 'SWLOR_Haks/sw_2da/portraits.2da').read_text().splitlines()
    rows = [line.split() for line in lines if line.split() and line.split()[0].isdigit()]
    return {i: 'po_' + row[1].lower() for i, row in enumerate(rows)}


def resources(root=ROOT):
    stock = root / 'tools/data/nwn_stock_portraits.txt'
    names = {line for line in stock.read_text().splitlines() if line and not line.startswith('#')}
    haks = root / 'SWLOR_Haks'
    if not (haks / 'sw_portrait').is_dir():
        raise ValueError('Initialize the SWLOR_Haks submodule before auditing portraits')
    configured = {hak['Name']: hak['Path'] for hak in read_json(haks / 'hakbuilder.json')['HakList']}
    loaded = read_json(root / 'Module/ifo/module.ifo.json')['Mod_HakList']['value']
    for hak in loaded:
        name = hak['Mod_Hak']['value']
        source = haks / configured[name]
        if not source.is_dir():
            raise ValueError(f'Missing loaded HAK source: {name}')
        names.update(p.stem.lower() for p in source.rglob('*') if p.suffix.lower() in ('.tga', '.dds'))
    return names


def creatures(root=ROOT):
    for path in sorted((root / 'Module/utc').glob('*.utc.json')):
        yield path, 0, read_json(path)
    for path in sorted((root / 'Module/git').glob('*.git.json')):
        for index, creature in enumerate(read_json(path).get('Creature List', {}).get('value', [])):
            yield path, index, creature


def missing_sizes(creature, table, names):
    explicit = creature.get('Portrait', {}).get('value', '')
    base = explicit.lower() if explicit else table.get(creature.get('PortraitId', {}).get('value'), '')
    return [size for size in 'lmst' if not base or base + size not in names]


def audit(root=ROOT):
    table, names = portrait_table(root), resources(root)
    failures = []
    count = 0
    for path, index, creature in creatures(root):
        count += 1
        missing = missing_sizes(creature, table, names)
        if missing:
            failures.append(f'{path.relative_to(root)}[{index}]: missing {",".join(missing)}')
    # Incubated and tamed companions can override the UTC portrait at spawn.
    for path in (root / 'SWLOR.Game.Server/Feature/BeastDefinition').rglob('*.cs'):
        for value in re.findall(r'\.PortraitId\((\d+)\)', path.read_text()):
            missing = missing_sizes({'PortraitId': {'value': int(value)}}, table, names)
            if missing:
                failures.append(f'{path.relative_to(root)}: portrait {value} missing {",".join(missing)}')
    return count, failures


if __name__ == '__main__':
    count, failures = audit()
    print(f'Audited {count} NPC blueprints/placements and all literal beast portrait overrides.')
    print('\n'.join(failures) if failures else 'All NPC portraits have Large, Medium, Small and Tiny resources.')
    raise SystemExit(bool(failures))
