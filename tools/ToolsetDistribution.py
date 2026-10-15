#!/usr/bin/env python3
"""Package or maintain the toolset on Windows and Linux using one implementation."""
import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

import BuildToolsetContent as content
from toolset_common import ROOT, executable, native_environment, new_output, prepare_native_tools, run, runtime_id


def publish_project(project, destination, rid, version=None):
    arguments = ['dotnet', 'publish', ROOT / project, '-c', 'Release', '-r', rid,
                 '--self-contained', 'true', '-o', destination, '-p:RunPostBuildEvent=Never',
                 '-p:PublishTrimmed=false', '-p:PublishSingleFile=false', '-p:RestoreLockedMode=false',
                 '-p:NuGetLockFilePath=obj/packages.publish.lock.json']
    if version:
        arguments.append('-p:Version=' + version)
    run(arguments)


def copy_tools(native, destination):
    for path in native.iterdir():
        if path.is_file() and (path.suffix.lower() in ('.exe', '.dll', '.so', '.json', '.txt') or '.so.' in path.name or path.name.startswith('nwn_')):
            shutil.copy2(path, destination / path.name)


def notices(destination):
    target = destination / 'ThirdPartyNotices'
    target.mkdir(exist_ok=True)
    for project in ('SWLOR.Toolset', 'SWLOR.CLI'):
        assets = json.loads((ROOT / project / 'obj/project.assets.json').read_text(encoding='utf-8-sig'))
        for identifier, library in assets['libraries'].items():
            if library.get('type') != 'package':
                continue
            for folder in assets['packageFolders']:
                package = Path(folder) / library['path']
                if not package.is_dir():
                    continue
                for path in package.rglob('*'):
                    if path.is_file() and (re.match(r'^(LICENSE|NOTICE|COPYING|COPYRIGHT|THIRD.?PARTY)', path.name, re.I) or path.suffix == '.nuspec'):
                        output = target / identifier / path.relative_to(package)
                        output.parent.mkdir(parents=True, exist_ok=True)
                        shutil.copyfile(path, output)
    shutil.copyfile(ROOT / 'SWLOR.Toolset/LICENSE.GPL-3.0', target / 'LICENSE.GPL-3.0')


def publish(args):
    if not args.skip_workspace and content.git(ROOT, 'status', '--porcelain'):
        raise RuntimeError('Commit or discard source edits before generating a revision-pinned workspace. Use --skip-workspace for an application-only preview.')
    rid = runtime_id()
    native = prepare_native_tools()
    output = new_output(args.output)
    application = output / 'application'
    cli = application / 'tools/SWLOR.CLI'
    release = output / 'release'
    release.mkdir()
    for project, destination in [('SWLOR.Toolset/SWLOR.Toolset.csproj', application), ('SWLOR.CLI/SWLOR.CLI.csproj', cli)]:
        publish_project(project, destination, rid, args.version)
    copy_tools(native, cli)
    for relative in ('SWLOR.NWN.API/NWN', 'SWLOR.NWN.API/NWScript'):
        shutil.copytree(ROOT / relative, application / relative, ignore=shutil.ignore_patterns('bin', 'obj'))
    shutil.copyfile(ROOT / 'LICENSE.txt', application / 'LICENSE.txt')
    shutil.copyfile(ROOT / 'SWLOR.Toolset/LICENSE-NOTICE.md', application / 'LICENSE-NOTICE.md')
    notices(application)
    if not args.skip_workspace:
        if not args.asset_descriptor:
            raise RuntimeError('Specify --asset-descriptor, or use --skip-workspace for an application-only preview.')
        revision = content.git(ROOT, 'rev-parse', 'HEAD')
        catalog = output / 'game-code.json'
        run([application / executable('SWLOR.Toolset'), '--export-catalog', ROOT, revision, catalog], timeout=120)
        assets = json.loads(Path(args.asset_descriptor).read_text(encoding='utf-8-sig'))
        content.workspace(ROOT, release, catalog, assets,
                          f'https://github.com/zunath/SWLOR_NWN/releases/download/toolset-v{args.version}')
    packaging = output / 'packaging-tools'
    run(['dotnet', 'tool', 'install', 'vpk', '--version', '1.2.161', '--tool-path', packaging])
    icon = ROOT / ('SWLOR.Toolset/Assets/swlor.ico' if os.name == 'nt' else 'SWLOR.Toolset/Assets/swlor-logo.png')
    arguments = [packaging / executable('vpk'), 'pack', '--packId', 'SWLOR.Toolset', '--packVersion', args.version,
                 '--packDir', application, '--mainExe', executable('SWLOR.Toolset'), '--runtime', rid,
                 '--packTitle', 'SWLOR Toolset', '--icon', icon, '--outputDir', release]
    if args.sign_parameters:
        if os.name != 'nt':
            raise RuntimeError('--sign-parameters applies only to Windows. Linux packages are unsigned.')
        arguments.extend(['--signParams', args.sign_parameters])
    environment = os.environ.copy()
    if os.name != 'nt':
        environment['APPIMAGE_EXTRACT_AND_RUN'] = '1'
    run(arguments, env=environment)
    # Modern clients use per-platform releases.*.json, avoiding legacy index collisions in a combined release.
    for path in release.glob('RELEASES*'):
        path.unlink()
    if os.name != 'nt':
        shutil.make_archive(str(release / 'SWLOR.Toolset-linux-x64'), 'gztar', root_dir=application)
    print(f'Toolset {rid} release prepared at {release}')


def assets(args):
    native = prepare_native_tools()
    revision = content.revisions(ROOT)[1]
    source = ROOT / 'SWLOR_Haks'
    actual = content.git(source, 'rev-parse', 'HEAD')
    if actual != revision:
        raise RuntimeError('Initialize SWLOR_Haks at the parent repository pinned revision first.')
    if content.git(source, 'status', '--porcelain'):
        raise RuntimeError('The asset source checkout must be clean before publishing an immutable pack.')
    output = new_output(args.output)
    cli = output / 'cli'
    packed = output / 'packed'
    release = output / 'release'
    release.mkdir()
    publish_project('SWLOR.CLI/SWLOR.CLI.csproj', cli, runtime_id())
    copy_tools(native, cli)
    config = json.loads((ROOT / 'Build/hakbuilder.json').read_text(encoding='utf-8-sig'))
    for hak in config['HakList']:
        hak['Path'] = str((ROOT / 'Build' / hak['Path']).resolve())
    config['TlkPath'] = str((ROOT / 'Build' / config['TlkPath']).resolve())
    config['OutputPath'] = str(packed) + os.sep
    config['EnableChecksumChecking'] = False
    (output / 'hakbuilder.json').write_text(json.dumps(config, indent=2) + '\n')
    run([cli / executable('SWLOR.CLI'), '-k'], cwd=output, env=native_environment(cli))
    content.asset_pack(ROOT, release, packed, f'https://github.com/zunath/SWLOR_NWN/releases/download/toolset-assets-{revision}')
    print(f'Asset pack prepared at {release}. Upload it to toolset-assets-{revision} with latest disabled.')


def import_contribution(args):
    archive = Path(args.archive).resolve()
    toolset = Path(args.toolset).resolve()
    if not archive.is_file() or not toolset.is_file():
        raise RuntimeError('The contribution or toolset executable was not found.')
    if args.apply:
        branch = content.git(ROOT, 'symbolic-ref', '--short', 'HEAD')
        if branch in ('master', 'main') or content.git(ROOT, 'status', '--porcelain'):
            raise RuntimeError('Apply contributions in a clean feature branch.')
    import zipfile
    with zipfile.ZipFile(archive) as bundle:
        entry = bundle.getinfo('contribution.json')
        if entry.file_size > 16 * 1024 * 1024:
            raise RuntimeError('The contribution manifest is too large.')
        manifest = json.loads(bundle.read(entry))
    if manifest['AssetRevision'] != content.revisions(ROOT)[1]:
        raise RuntimeError('Review this contribution on a branch with its matching pinned asset revision.')
    run([toolset, '--import-contribution' if args.apply else '--check-contribution', archive, ROOT], timeout=None if args.apply else 120)
    print('Contribution checked. Review git diff before committing; conversations require a matching server build.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    commands.add_parser('prepare-tools')
    package = commands.add_parser('publish')
    package.add_argument('--version', required=True)
    package.add_argument('--asset-descriptor')
    package.add_argument('--output', required=True)
    package.add_argument('--skip-workspace', action='store_true')
    package.add_argument('--sign-parameters')
    asset = commands.add_parser('assets')
    asset.add_argument('--output', required=True)
    contribution = commands.add_parser('import-contribution')
    contribution.add_argument('--archive', required=True)
    contribution.add_argument('--toolset', required=True)
    contribution.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    if args.command == 'publish' and not re.fullmatch(r'\d+\.\d+\.\d+(-[A-Za-z0-9.-]+)?', args.version):
        parser.error('Use a semantic version such as 1.0.0.')
    try:
        if args.command == 'prepare-tools':
            print(prepare_native_tools())
        elif args.command == 'publish':
            publish(args)
        elif args.command == 'assets':
            assets(args)
        else:
            import_contribution(args)
    except (OSError, RuntimeError, subprocess.SubprocessError, ValueError) as error:
        print(str(error), file=sys.stderr)
        return 1
    return 0

if __name__ == '__main__':
    sys.exit(main())
