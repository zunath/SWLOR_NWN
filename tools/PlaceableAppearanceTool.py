"""Install packaged placeable models into the haks and point the station blueprints at them.

  python tools/PlaceableAppearanceTool.py install-haks --package <runtime_resources> [--only smithery_bench ...]
  python tools/PlaceableAppearanceTool.py swap-module [--only smithery_bench ...]
  python tools/PlaceableAppearanceTool.py check

`install-haks` copies the packaged MDL/PWK/MTR/DDS files into SWLOR_Haks/sw_plc and appends one placeables.2da row per
station model (existing rows are never edited or removed; an existing ModelName is reused). `swap-module` then sets the
Appearance of each station's blueprint (Module/utp) and of every placed instance in Module/git to that row. Edits are
byte-level (CRLF and float formatting untouched); the Appearance dword is the only byte range that changes.
`check` is read-only: it fails when a station still points at another appearance or a resource is missing.
Station data lives in tools/StationModels.json.
"""
from __future__ import annotations

import argparse, glob, json, re, shutil, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONFIG = ROOT / 'tools/StationModels.json'
HAKS = ROOT / 'SWLOR_Haks'
FOLDER = 'sw_plc'
PLACEABLES = 'sw_2da/placeables.2da'
APPEARANCE = re.compile(rb'("Appearance":\s*\{\s*"type":\s*"dword",\s*"value":\s*)(\d+)(\s*\})')
STRUCT = b'"__struct_id"'
HEADER_COLUMNS = ('Label', 'StrRef', 'ModelName', 'LightColor', 'LightOffsetX', 'LightOffsetY', 'LightOffsetZ',
                  'SoundAppType', 'ShadowSize', 'BodyBag', 'LowGore', 'Reflection', 'Static')


def load_stations(only=None):
    config = json.loads(CONFIG.read_text(encoding='utf-8'))
    if config.get('schema_version') != 1:
        raise ValueError('Unsupported StationModels.json schema')
    stations = config['stations']
    if only:
        unknown = set(only) - {s['blueprint'] for s in stations}
        if unknown:
            raise ValueError('Unknown station blueprint(s): ' + ', '.join(sorted(unknown)))
        stations = [s for s in stations if s['blueprint'] in set(only)]
    return stations


def read_rows(path):
    """Return ({physical row index: ModelName lowercased}, last row label) of a 2DA.

    The engine and every reader here index rows by physical order; the human-readable first column can drift (placeables.2da
    has gaps and reordered labels), so a row's appearance ID is its position, never its label."""
    lines = Path(path).read_text(encoding='latin-1').split('\n')
    header = lines[2].split()
    column = header.index('ModelName') + 1
    rows, label = {}, -1
    for line in lines[3:]:
        if not line.strip():
            continue
        tokens = re.findall(r'"[^"]*"|\S+', line)
        rows[len(rows)] = tokens[column].strip('"').lower()
        label = int(tokens[0])
    return rows, label


def format_row(index, station, offsets):
    cells = [str(index), '"' + station['label'] + '"', '****', station['model'], '****', '****', '****', '****',
             '****' if station.get('sound_app_type') is None else str(station['sound_app_type']), '1', '0', '****', '****', '1']
    # The first nine columns follow the header's alignment closely enough for human diffs; tokens are what the engine reads.
    line = ''
    positions = [0] + list(offsets)
    for cell, position in zip(cells, positions):
        line = line.ljust(position) + cell
        line += ' '
    return line.rstrip() + '\n'


def header_offsets(path):
    header = Path(path).read_text(encoding='latin-1').split('\n')[2]
    found = {m.group(0): m.start() for m in re.finditer(r'\S+', header)}
    return [found[name] for name in HEADER_COLUMNS]


def appearance_rows(haks):
    """(ModelName -> physical row index (the appearance ID), next row label)."""
    rows, label = read_rows(Path(haks) / PLACEABLES)
    by_model = {}
    for index, model in rows.items():
        by_model.setdefault(model, index)
    return by_model, label


def install_haks(package, haks, only):
    package = Path(package)
    stations = load_stations(only)
    folder = Path(haks) / FOLDER
    if not folder.is_dir():
        raise FileNotFoundError(f'{folder} is not checked out')
    files = sorted(p for p in package.iterdir() if p.is_file() and p.suffix.lower() in ('.mdl', '.pwk', '.mtr', '.dds'))
    if not files:
        raise ValueError('Package has no resources')
    packaged_models = {p.stem.lower() for p in files if p.suffix.lower() == '.mdl'}
    selected = [s for s in stations if s['model'] in packaged_models]
    if not selected:
        raise ValueError('No station model from the configuration is in the package')
    own = {p.stem.lower() for p in files}
    for source in files:
        target = folder / source.name
        if target.exists() and target.read_bytes() != source.read_bytes():
            print(f'replace {FOLDER}/{source.name}')
        shutil.copyfile(source, target)
    print(f'copied {len(files)} files into {FOLDER}')
    path = Path(haks) / PLACEABLES
    rows, last_label = read_rows(path)
    by_model, _ = appearance_rows(haks)
    offsets = header_offsets(path)
    text = path.read_bytes().decode('latin-1')
    if not text.endswith('\n'):
        text += '\n'
    # Rows are indexed by physical position; each new row's label continues the file's last label for readability only.
    added = {}
    for station in selected:
        if station['model'] in by_model:
            added[station['blueprint']] = by_model[station['model']]
            continue
        last_label += 1
        text += format_row(last_label, station, offsets)
        by_model[station['model']] = len(rows)
        added[station['blueprint']] = len(rows)
        rows[len(rows)] = station['model']
    path.write_bytes(text.encode('latin-1'))
    print(json.dumps(added, indent=2))
    return added


def blueprint_path(root, resref):
    return Path(root) / 'Module/utp' / f'{resref}.utp.json'


def swap_instances(data, resref, row):
    """Set Appearance on every `resref` placeable struct in an area GIT. Returns (new bytes, edits, already)."""
    out, cursor, edits, already = [], 0, 0, 0
    pattern = re.compile(rb'"TemplateResRef":\s*\{\s*"type":\s*"resref",\s*"value":\s*"' + re.escape(resref.encode()) + rb'"')
    matches = [m.start() for m in pattern.finditer(data)]
    for start in matches:
        found = data.rfind(b'"Appearance"', 0, start)
        if found < 0 or data.find(STRUCT, found, start) != -1:
            raise ValueError(f'Could not locate the Appearance of a {resref} instance')
        match = APPEARANCE.match(data, found)
        if match is None:
            raise ValueError(f'Unexpected Appearance layout for a {resref} instance')
        out.append(data[cursor:match.start(2)])
        if int(match.group(2)) == row:
            out.append(match.group(2))
            already += 1
        else:
            out.append(str(row).encode())
            edits += 1
        cursor = match.end(2)
    out.append(data[cursor:])
    return b''.join(out), edits, already


def swap_module(root, haks, only, check=False):
    stations = load_stations(only)
    by_model, _ = appearance_rows(haks)
    report, problems = {}, []
    for station in stations:
        resref, model = station['blueprint'], station['model']
        row = by_model.get(model)
        if row is None:
            problems.append(f'{resref}: placeables.2da has no row for {model}')
            continue
        blueprint = blueprint_path(root, resref)
        data = blueprint.read_bytes()
        match = APPEARANCE.search(data)
        if match is None:
            raise ValueError(f'{blueprint.name}: no Appearance')
        changed = int(match.group(2)) != row
        if changed and not check:
            blueprint.write_bytes(data[:match.start(2)] + str(row).encode() + data[match.end(2):])
        instances, already, files = 0, 0, 0
        for path in sorted(glob.glob(str(Path(root) / 'Module/git/*.git.json'))):
            raw = Path(path).read_bytes()
            if f'"{resref}"'.encode() not in raw:
                continue
            updated, edits, same = swap_instances(raw, resref, row)
            already += same
            if edits:
                instances += edits
                files += 1
                if not check:
                    Path(path).write_bytes(updated)
        report[resref] = {'model': model, 'row': row, 'blueprint_changed': changed, 'instances_changed': instances,
                          'instances_already_current': already, 'area_files_changed': files}
        if check and (changed or instances):
            problems.append(f'{resref}: blueprint {"stale" if changed else "ok"}, {instances} stale instance(s)')
    print(json.dumps(report, indent=2))
    return report, problems


def check_resources(haks, only):
    problems = []
    by_model, _ = appearance_rows(haks)
    for station in load_stations(only):
        model = station['model']
        if model not in by_model:
            problems.append(f'no placeables.2da row for {model}')
        for extension in ('mdl', 'pwk'):
            if not (Path(haks) / FOLDER / f'{model}.{extension}').is_file():
                problems.append(f'missing {FOLDER}/{model}.{extension}')
    return problems


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('command', choices=['install-haks', 'swap-module', 'check'])
    parser.add_argument('--root', type=Path, default=ROOT)
    parser.add_argument('--haks', type=Path)
    parser.add_argument('--package', type=Path)
    parser.add_argument('--only', nargs='*')
    args = parser.parse_args()
    haks = args.haks or args.root / 'SWLOR_Haks'
    if args.command == 'install-haks':
        if args.package is None:
            parser.error('install-haks requires --package')
        install_haks(args.package, haks, args.only)
    elif args.command == 'swap-module':
        swap_module(args.root, haks, args.only)
    else:
        problems = check_resources(haks, args.only)
        _, more = swap_module(args.root, haks, args.only, check=True)
        problems += more
        if problems:
            print('\n'.join(problems))
            sys.exit(1)
        print('ok')


if __name__ == '__main__':
    main()
