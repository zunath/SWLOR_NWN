"""python -m nwn_visual_check --help (run from tools/, or set PYTHONPATH=tools)."""
from pathlib import Path
import argparse
import ctypes
import hashlib
import json
import re
import sys

from . import module, profile


def parse_captures(values):
    captures = {}
    for value in values:
        marker, separator, label = value.partition('=')
        if not separator or not marker or '\n' in marker or not re.fullmatch('[a-z][a-z0-9_-]{0,63}', label):
            raise ValueError('Captures use MARKER=label; label must be a safe lowercase filename')
        if marker in captures or label in captures.values():
            raise ValueError('Capture markers and labels must be unique')
        captures[marker] = label
    return captures


def digest(path):
    result = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b''):
            result.update(chunk)
    return result.hexdigest()


def main(argv=None):
    parser = argparse.ArgumentParser(description='Capture a silent NWN client on an isolated private desktop. Windows and Python 3.12+ required.')
    inputs = parser.add_mutually_exclusive_group(required=True)
    inputs.add_argument('--module', type=Path, help='Already packed faithful review module')
    inputs.add_argument('--sources', type=Path, help='Flat directory of review GFF JSON, NSS and binary resources')
    parser.add_argument('--client', help='Installed bin/win32/nwmain.exe (or NWN_CLIENT)')
    parser.add_argument('--tools', type=Path, help='Directory containing NWN GFF/ERF/script tools')
    parser.add_argument('--hak-dir', type=Path, help='Current packed HAKs; copied into the owned profile')
    parser.add_argument('--tlk', type=Path, help='Matching custom TLK')
    parser.add_argument('--output', type=Path, required=True, help='New evidence directory outside the repo')
    parser.add_argument('--capture', action='append', required=True, metavar='MARKER=label')
    parser.add_argument('--complete-marker', help='Optional log marker required after all captures')
    parser.add_argument('--timeout', type=float, default=120, help='Client wall-clock limit, at most 600 seconds')
    args = parser.parse_args(argv)
    captures = parse_captures(args.capture)
    if sys.platform != 'win32' or sys.version_info < (3,12):
        raise RuntimeError('Native verification requires Windows and Python 3.12+')
    if not 0 < args.timeout <= 600:
        raise ValueError('Timeout must be between 0 and 600 seconds')
    from . import offline_client
    from .native_frames import NativeFrames
    from .private_desktop import process_desktop
    client = module.discover_client(args.client)
    output = args.output.resolve()
    repo = Path(__file__).resolve().parents[2]
    if output.is_relative_to(repo):
        raise ValueError('Evidence must be outside the repository')
    output.mkdir(parents=True,exist_ok=False)
    owned = profile.create_profile()
    state = {'pid':None,'frames':None}
    manifest = {'success':False,'client':str(client),'profile':str(owned),'captures':{},'inputs':{},'profile_removed':False}
    try:
        if args.hak_dir:
            for hak in sorted(args.hak_dir.glob('*.hak')):
                profile.copy_input(hak,owned / 'hak' / hak.name)
                manifest['inputs'][str(hak.resolve())] = digest(owned / 'hak' / hak.name)
        if args.tlk:
            profile.copy_input(args.tlk,owned / 'tlk' / args.tlk.name)
            manifest['inputs'][str(args.tlk.resolve())] = digest(owned / 'tlk' / args.tlk.name)
        if args.module:
            fixture = owned / 'modules' / args.module.name
            profile.copy_input(args.module,fixture)
            manifest['inputs'][str(args.module.resolve())] = digest(fixture)
        else:
            fixture = module.build_module(args.sources,owned,client,args.tools)
            manifest['inputs'].update({str(p.resolve()):digest(p) for p in sorted(args.sources.iterdir()) if p.is_file()})
        manifest['module_sha256'] = digest(fixture)
        ctypes.windll.user32.SetProcessDPIAware()

        def started(pid):
            state['pid'] = pid
            manifest['pid'] = pid

        def finished():
            log = profile.log_text(owned)
            pending = [(marker,label) for marker,label in captures.items() if marker in log and label not in manifest['captures']]
            if len(pending) > 1:
                raise RuntimeError('Multiple uncaptured markers: hold each view until its fresh capture completes')
            for marker,label in pending:
                if state['frames'] is None:
                    state['frames'] = NativeFrames(state['pid'],process_desktop(state['pid']),owned)
                target = output / (label+'.png')
                state['frames'].save(target)
                manifest['captures'][label] = {'marker':marker,'file':target.name,'sha256':digest(target)}
                print('Captured '+label,flush=True)
            return len(manifest['captures']) == len(captures) and (not args.complete_marker or args.complete_marker in log)

        result = offline_client.run([str(client),'-userdirectory',str(owned),'+TestNewModule',fixture.stem],profile=owned,cwd=client.parent,timeout=args.timeout,on_start=started,until=finished)
        if not result.stopped or len(manifest['captures']) != len(captures):
            raise RuntimeError('Client exited before required evidence was captured')
        manifest['success'] = True
    except BaseException as error:
        manifest['failure'] = str(error)
        raise
    finally:
        try:
            if state['frames']:
                state['frames'].close()
            (output / 'client.log').write_text(profile.log_text(owned),encoding='utf8')
            if (owned / 'isolation.json').exists():
                profile.copy_input(owned / 'isolation.json',output / 'isolation.json')
        except BaseException as error:
            manifest['success'] = False
            manifest['evidence_failure'] = str(error)
            raise
        finally:
            try:
                profile.cleanup(owned)
                manifest['profile_removed'] = True
            except BaseException as error:
                manifest['success'] = False
                manifest['cleanup_failure'] = str(error)
                raise
            finally:
                (output / 'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf8')
    print('Native evidence saved to '+str(output),flush=True)


if __name__ == '__main__':
    main()
