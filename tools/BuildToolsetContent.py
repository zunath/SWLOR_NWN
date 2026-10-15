#!/usr/bin/env python3
"""Build immutable download manifests from a checked-out revision and packed HAKs."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import zipfile

PART_BYTES = 1024 * 1024 * 1024
CONTENT = re.compile(r"^(Module/(are|dlg|fac|gic|git|ifo|itp|jrl|utc|utd|ute|uti|utm|utp|uts|utt|utw)/[A-Za-z0-9_]+\.[a-z]+\.json|Module/(nss|ncs)/[A-Za-z0-9_]+\.(nss|ncs)|SWLOR\.Game\.Server/ConversationData/[A-Za-z0-9_]+\.conversation\.json|toolset/categories\.json)$")

def git(root, *args):
    return subprocess.check_output(['git', '-C', str(root), *args], text=True).strip()

def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest().upper()

def write_json(path, data):
    path.write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')

def revisions(root):
    return git(root, 'rev-parse', 'HEAD'), git(root, 'rev-parse', 'HEAD:SWLOR_Haks')

def split_archive(path, release_url):
    parts = []
    with path.open('rb') as source:
        number = 1
        while True:
            # Keep memory bounded while producing release assets below GitHub's per-file limit.
            first = source.read(min(1024 * 1024, PART_BYTES))
            if not first:
                break
            target = path.with_name(f'{path.name}.{number:03d}')
            length = 0
            checksum = hashlib.sha256()
            with target.open('xb') as output:
                block = first
                while block:
                    output.write(block)
                    checksum.update(block)
                    length += len(block)
                    if length == PART_BYTES:
                        break
                    block = source.read(min(1024 * 1024, PART_BYTES - length))
            parts.append({'Url': release_url.rstrip('/') + '/' + target.name,
                          'Sha256': checksum.hexdigest().upper(), 'Length': length})
            number += 1
    descriptor = {'Sha256': digest(path), 'Parts': parts}
    path.unlink()  # Only the newly-created intermediate ZIP, never source assets.
    return descriptor

def workspace(root, output, catalog, assets, release_url):
    revision, asset_revision = revisions(root)
    if assets['AssetRevision'] != asset_revision:
        raise ValueError('The published assets do not match the pinned SWLOR_Haks commit.')
    if json.loads(catalog.read_text(encoding='utf-8'))['ContentRevision'] != revision:
        raise ValueError('The editor catalog does not match the source revision.')
    files = [name for name in git(root, 'ls-files', 'Module', 'SWLOR.Game.Server/ConversationData', 'toolset/categories.json').splitlines()
             if CONTENT.fullmatch(name)]
    manifest = {'SchemaVersion': 1, 'ContentRevision': revision, 'AssetRevision': asset_revision,
                'Baseline': {name: digest(root / name) for name in files}}
    archive = output / 'workspace.zip'
    with zipfile.ZipFile(archive, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as bundle:
        for name in files:
            bundle.write(root / name, name)
        bundle.writestr('swlor-workspace.json', json.dumps(manifest, indent=2))
        bundle.write(catalog, 'Metadata/game-code.json')
    descriptor = split_archive(archive, release_url)
    write_json(output / 'toolset-feed.json', {'SchemaVersion': 1, 'ContentRevision': revision,
               'AssetRevision': asset_revision, 'Workspace': descriptor, 'Assets': assets['Archive']})

def assigned_haks(root):
    # Legacy Nim void values can contain raw bytes; HAK names remain ordinary JSON text.
    ifo = json.loads((root / 'Module/ifo/module.ifo.json').read_text(encoding='utf-8-sig', errors='surrogateescape'))
    return [entry['Mod_Hak']['value'] for entry in ifo['Mod_HakList']['value']]


def asset_pack(root, output, packed, release_url):
    _, revision = revisions(root)
    hak_names = assigned_haks(root)
    files = {f'hak/{name}.hak': packed / 'hak' / f'{name}.hak' for name in hak_names}
    files['tlk/sw_tlk.tlk'] = packed / 'tlk/sw_tlk.tlk'
    files['tlk/sw_tlk.tlk.json'] = root / 'SWLOR_Haks/sw_tlk/sw_tlk.tlk.json'
    for path in files.values():
        if not path.is_file() or path.stat().st_size == 0:
            raise FileNotFoundError(f'Required asset is missing or empty: {path}')
    manifest = {'SchemaVersion': 1, 'AssetRevision': revision,
                'Files': {name: digest(path) for name, path in files.items()}}
    archive = output / 'assets.zip'
    with zipfile.ZipFile(archive, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=1) as bundle:
        for name, path in files.items():
            bundle.write(path, name)
        bundle.writestr('asset-manifest.json', json.dumps(manifest, indent=2))
    descriptor = split_archive(archive, release_url)
    write_json(output / 'asset-pack.json', {'SchemaVersion': 1, 'AssetRevision': revision, 'Archive': descriptor})

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['workspace', 'assets'])
    parser.add_argument('--root', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--release-url', required=True)
    parser.add_argument('--catalog', type=Path)
    parser.add_argument('--assets', type=Path)
    parser.add_argument('--packed', type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    if args.mode == 'workspace':
        workspace(args.root, args.output, args.catalog, json.loads(args.assets.read_text(encoding='utf-8-sig')), args.release_url)
    else:
        asset_pack(args.root, args.output, args.packed, args.release_url)

if __name__ == '__main__':
    main()
