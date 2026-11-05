"""Retarget the configured SWG Performance clips from a source directory or ZIP."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import tempfile
import zipfile

import numpy as np

from swg_animation import fit_contacts, read_animation, retarget


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def comparable(value):
    """Ignore installer metadata and numerical noise in effectively zero channels."""
    if isinstance(value, dict):
        return {key: comparable(item) for key, item in value.items() if key != 'IsIdentity'}
    if isinstance(value, list):
        return [comparable(item) for item in value]
    if isinstance(value, float):
        return 0.0 if abs(value) < 1e-12 else float(np.float32(value))
    return value


def resource(source: Path, name: str) -> bytes:
    relative = PurePosixPath(name)
    if relative.is_absolute() or '..' in relative.parts or '\\' in name:
        raise ValueError(f'Invalid source resource: {name}')
    if source.is_dir():
        path = (source / Path(*relative.parts)).resolve()
        if not path.is_relative_to(source.resolve()):
            raise ValueError(f'Redirected source resource: {name}')
        return path.read_bytes()
    with zipfile.ZipFile(source) as archive:
        matches = [item for item in archive.infolist() if item.filename == name]
        if len(matches) != 1 or matches[0].file_size > 64 * 1024 * 1024:
            raise ValueError(f'Missing, duplicate, or oversized source resource: {name}')
        return archive.read(matches[0])


def publish(output: Path, files: dict[str, bytes], overwrite: bool) -> None:
    output.mkdir(parents=True, exist_ok=True)
    before = {name: (output / name).read_bytes() if (output / name).exists() else None for name in files}
    if not overwrite and any(value is not None for value in before.values()):
        raise ValueError('Output exists; pass --overwrite to replace it.')
    staged = {}
    written = []
    try:
        # Adjacent staging keeps renames on one filesystem and inherits the
        # destination ACL instead of Windows' private temporary-directory ACL.
        for name, data in files.items():
            descriptor, temporary = tempfile.mkstemp(prefix='.swg-animation-', dir=output)
            staged[name] = Path(temporary)
            with os.fdopen(descriptor, 'wb') as stream:
                stream.write(data)
        for name in files:
            os.replace(staged[name], output / name)
            written.append(name)
    except BaseException:
        for name in reversed(written):
            if before[name] is None:
                (output / name).unlink()
            else:
                (output / name).write_bytes(before[name])
        raise
    finally:
        for path in staged.values():
            path.unlink(missing_ok=True)


def generate(source: Path, configuration: Path, repository: Path, output: Path, overwrite: bool, finalize_installed: bool = False) -> None:
    config_text = configuration.read_text(encoding='utf-8-sig').replace('\r\n', '\n')
    config = json.loads(config_text)
    if config['Version'] != 1:
        raise ValueError('Unsupported import configuration version.')
    if source.is_file() and digest(source.read_bytes()) != config['SourceArchiveSha256']:
        raise ValueError('Source archive hash does not match the configured source.')
    skeleton = resource(source, config['Skeleton'])
    if digest(skeleton) != config['SkeletonSha256']:
        raise ValueError('Skeleton hash does not match the configured source.')
    reference = (repository / config['ReferenceProject']).resolve()
    if not reference.is_relative_to(repository.resolve()):
        raise ValueError('Reference project must be inside the repository.')
    blend = float(config['BoundaryBlendSeconds'])
    if not 0 < blend <= 1:
        raise ValueError('Boundary blend must be between zero and one second.')
    files = {}
    reports = []
    with tempfile.TemporaryDirectory(prefix='swg-source-') as temporary:
        folder = Path(temporary)
        skeleton_path = folder / 'source.skt'
        skeleton_path.write_bytes(skeleton)
        for clip in config['Clips']:
            identity = clip['Id']
            if not re.fullmatch('[A-Z][A-Za-z0-9_]{0,62}', identity) or identity + '.swlanim' in files:
                raise ValueError(f'Invalid or duplicate project identity: {identity}')
            data = resource(source, clip['Source'])
            if digest(data) != clip['SourceSha256']:
                raise ValueError(f'Source hash mismatch: {clip["Source"]}')
            animation_path = folder / 'source.ans'
            animation_path.write_bytes(data)
            animation = read_animation(animation_path)
            if abs(animation['duration'] - clip['Duration']) > 1e-6:
                raise ValueError(f'Source duration mismatch: {clip["Source"]}')
            clip_reference = (repository / clip.get('ReferenceProject', config['ReferenceProject'])).resolve()
            if not clip_reference.is_relative_to(repository.resolve()):
                raise ValueError('Clip reference must be inside the repository.')
            contact_paths = [(repository / item).resolve() for item in clip.get('ContactProjects', [])]
            if any(not path.is_relative_to(repository.resolve()) for path in contact_paths):
                raise ValueError('Contact projects must be inside the repository.')
            contact = fit_contacts(contact_paths, clip['InstrumentFamily'], clip.get('NativeCarry')) if contact_paths else None
            project = retarget(skeleton_path, animation_path, clip_reference, identity, blend, clip.get('Calibration', 'bind'), clip.get('Mirror', False), contact)
            payload = (json.dumps(project, indent=2, allow_nan=False) + '\n').encode()
            if len(payload) > 64 * 1024 * 1024:
                raise ValueError('Retargeted project exceeds 64 MiB.')
            if finalize_installed:
                installed = (output / (identity + '.swlanim')).read_text(encoding='utf-8-sig').replace('\r\n', '\n')
                if comparable(project) != comparable(json.loads(installed)):
                    raise ValueError(f'Installed project differs from the source motion: {identity}')
                payload = installed.encode()
            else:
                files[identity + '.swlanim'] = payload
            stance = 'Shared playing hold' if clip.get('InstrumentFamily') else 'Shared dance stance'
            reports.append({
                'Id': identity, 'Name': clip.get('Name', identity), 'BibleRow': 0, 'Reference': '',
                'Observation': 'Original SWG ' + clip.get('SourceLabel', clip.get('Dance', '') + ' dance') + '.',
                'Interpretation': 'Retargeted at the original speed; root locomotion stays inside the model. '
                                  f'The first and last {blend:g} seconds blend to the shared ' + ('playing hold.' if clip.get('Calibration') == 'first-frame' else 'dance stance.'),
                'Activity': 'Performing', 'Status': 'Draft: visual review required',
                'Project': identity + '.swlanim', 'Duration': project['Duration'],
                'Keyframes': len(project['Keys']), 'Joints': len(project['Joints']),
                'Beats': [{'Time': time, 'Label': label} for time, label in
                          [(0, stance), (blend, 'Original SWG motion'),
                           (project['Duration'] - blend, 'Return to shared stance'),
                           (project['Duration'], stance)]],
                'ProjectSha256': digest(payload), 'Source': clip['Source'],
                'SourceSha256': clip['SourceSha256'], 'SourceFormat': animation['kind'],
                'SourceFramesPerSecond': animation['fps'], 'SourceFrameCount': animation['frames'],
                'Calibration': clip.get('Calibration', 'bind'), 'Mirror': clip.get('Mirror', False),
                **({'NativeCarry': clip['NativeCarry']} if 'NativeCarry' in clip else {}),
                'ReferenceProject': clip.get('ReferenceProject', config['ReferenceProject']),
                'ReferenceProjectSha256': digest(clip_reference.read_text(encoding='utf-8-sig').replace('\r\n', '\n').encode()),
                'ContactProjects': {item: digest((repository / item).read_text(encoding='utf-8-sig').replace('\r\n', '\n').encode()) for item in clip.get('ContactProjects', [])}
            })
    manifest_path = output / 'manifest.json'
    manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig')) if manifest_path.exists() else {
        'Version': 1, 'Workbook': '', 'Sheet': 'Animations', 'Model': 'a_ba', 'Animations': []}
    replacements = {report['Id']: report for report in reports}
    merged = []
    for report in manifest['Animations']:
        merged.append(replacements.pop(report['Id'], report))
    merged.extend(replacements.values())
    manifest['Animations'] = merged
    manifest['Notes'] = ('Authored clips retain their recipe timing. Imported SWG loops retain source timing, '
                         'with shared boundary stances or holds. Installation and client verification are separate steps.')
    manifest['SwgImport'] = {
        'ConfigurationSha256': digest(config_text.encode()), 'Skeleton': config['Skeleton'],
        'SkeletonSha256': config['SkeletonSha256'], 'ReferenceProject': config['ReferenceProject'],
        'ReferenceProjectSha256': digest(reference.read_text(encoding='utf-8-sig').replace('\r\n', '\n').encode()),
        'SourceArchiveSha256': config['SourceArchiveSha256'], 'BoundaryBlendSeconds': blend
    }
    files['manifest.json'] = (json.dumps(manifest, indent=2, allow_nan=False) + '\n').encode()
    publish(output, files, overwrite)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True, help='Original source ZIP, or a directory containing appearance/.')
    parser.add_argument('--configuration', type=Path, default=Path('design/animations/performance/swg-imports.json'))
    parser.add_argument('--repository', type=Path, default=Path.cwd())
    parser.add_argument('--output', type=Path, default=Path('design/animations/performance'))
    parser.add_argument('--overwrite', action='store_true')
    parser.add_argument('--finalize-installed', action='store_true', help='Verify the installer only rounded to float32, then refresh portable project hashes.')
    args = parser.parse_args()
    generate(args.source.resolve(), args.configuration.resolve(), args.repository.resolve(), args.output.resolve(), args.overwrite, args.finalize_installed)


if __name__ == '__main__':
    main()
