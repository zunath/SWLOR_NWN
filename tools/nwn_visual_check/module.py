"""Build temporary review modules with the repository's NWN conversion tools."""
from pathlib import Path
import os
import re
import shutil
import subprocess


def find_tool(name, directory=None):
    repo = Path(__file__).resolve().parents[2]
    candidates = ([Path(directory) / name] if directory else []) + [repo / 'tools/SWLOR.CLI' / name, repo / 'SWLOR_Haks' / name]
    located = shutil.which(name)
    if located:
        candidates.append(Path(located))
    for candidate in candidates:
        if candidate.is_file() and candidate.stat().st_size > 1024:
            return candidate.resolve()
    raise FileNotFoundError(f'{name} not found; provide --tools or add it to PATH')


def checked(command, timeout=120, cwd=None):
    result = subprocess.run([str(x) for x in command],cwd=cwd,capture_output=True,text=True,timeout=timeout)
    if result.returncode:
        raise RuntimeError(result.stdout + result.stderr)
    return result


def convert_json(source, output, tools=None):
    checked([find_tool('nwn_gff.exe',tools),'-i',source,'-l','json','-o',output,'-k','gff'])


def validate_tilesets(source):
    """The native SET reader needs CRLF, as required by the HAK .gitattributes."""
    for path in Path(source).iterdir():
        if path.is_file() and path.suffix.lower() == '.set':
            remaining = path.read_bytes().replace(b'\r\n', b'')
            if b'\n' in remaining or b'\r' in remaining:
                raise ValueError(f'{path.name} must use CRLF line endings; LF tilesets can overrun the native NWN loader buffer')


def pack_hak(source, output, tools=None):
    validate_tilesets(source)
    checked([find_tool('nwn_erf.exe',tools),'-c','--add-restypes','lod:2078','-e','HAK','-f',output,source],timeout=240)


def compile_script(script, profile, client, tools=None):
    """Accept compiler-declared includes while rejecting actual diagnostics."""
    command=[find_tool('nwn_script_comp.exe',tools),'--root',client.parents[2],
             '--userdirectory',profile,'--no-ovr','--dirs',script.parent,script]
    result=subprocess.run([str(value) for value in command],capture_output=True,text=True,timeout=120)
    output=result.stdout+result.stderr
    # Match the existing ScriptCompiler's include-only outcome. No main produces
    # no bytecode and can return nonzero; positioned ERROR diagnostics still fail.
    skipped=re.search(r'\b0 successful,\s*1 skipped,\s*0 errored\b',output,re.I) is not None
    errors=re.search(r'(?im)^.*\.nss\(\d+\)\s*:\s*ERROR\s*:',output)
    if errors or (result.returncode and not skipped):
        raise RuntimeError(output)
    return result


def build_module(source, profile, client, tools=None):
    source = Path(source).resolve()
    validate_tilesets(source)
    staging = profile / 'build'
    staging.mkdir()
    for path in sorted(source.iterdir()):
        if not path.is_file():
            continue
        if path.name.endswith('.json'):
            convert_json(path,staging / path.name[:-5],tools)
        elif not (source / (path.name+'.json')).exists():
            shutil.copyfile(path,staging / path.name)
    for script in sorted(staging.glob('*.nss')):
        compile_script(script,profile,client,tools)
    if not (staging / 'module.ifo').is_file():
        raise FileNotFoundError('Review sources must contain module.ifo or module.ifo.json')
    output = profile / 'modules/visual_review.mod'
    checked([find_tool('nwn_erf.exe',tools),'-c','--add-restypes','lod:2078','-e','MOD','-f',output,staging],timeout=240)
    return output


def discover_client(explicit=None):
    configured = explicit or os.environ.get('NWN_CLIENT')
    if configured:
        candidate = Path(configured).resolve()
        if candidate.is_file() and candidate.name.lower() == 'nwmain.exe':
            return candidate
        raise FileNotFoundError(f'NWN client not found: {candidate}')
    roots = [Path(os.environ.get('ProgramFiles(x86)', 'C:/Program Files (x86)')) / 'Steam/steamapps/common/Neverwinter Nights',Path('C:/GOG Games/Neverwinter Nights Enhanced Edition')]
    for root in roots:
        candidate = root / 'bin/win32/nwmain.exe'
        if candidate.is_file():
            return candidate.resolve()
    raise FileNotFoundError('nwmain.exe not found; set NWN_CLIENT or provide --client')
