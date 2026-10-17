"""Generate local TRELLIS.2 GLB sources, serially, without altering ComfyUI services.

Manifest schema 1: {"schema_version":1,"assets":[{"id":"weapon_name",
"image":"relative/concept.png","seed":123,"shape_resolution":512,
"texture_size":1024,"target_faces":6000}]}. Input images and outputs belong in
ignored staging or outside Git. Run --preflight first, then repeat without it.
Repeat the identical command to recover a known submission; a lost POST response
is reconciled by its journal token, never blindly resubmitted. Expired jobs may
be collected if complete; they are never interrupted or given a new deadline.
This produces authoring sources, not fitted or native-validated NWN resources.
"""
from __future__ import annotations

import argparse
import contextlib
import copy
import hashlib
import ipaddress
import json
import math
import os
from pathlib import Path
import re
import struct
import subprocess
import tempfile
import time
import urllib.parse
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE = ROOT / 'tools/workflows/trellis2_weapon.json'
VERSION = 'trellis2-weapon-v1'


def digest(data):
    return hashlib.sha256(data).hexdigest()


def write_json(path, value):
    """Publish a complete durable journal before any non-idempotent request."""
    temporary = path.with_suffix(path.suffix + '.tmp')
    with temporary.open('w', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2)
        stream.write('\n')
        stream.flush()
        os.fsync(stream.fileno())
    durable_replace(temporary, path)


def write_bytes(path, payload):
    """Flush artifact contents and publish their directory entry before completion."""
    temporary = path.with_suffix(path.suffix + '.tmp')
    with temporary.open('wb') as stream:
        stream.write(payload)
        stream.flush()
        os.fsync(stream.fileno())
    durable_replace(temporary, path)


def replace_windows(source, target):
    import ctypes
    from ctypes import wintypes
    move = ctypes.WinDLL('kernel32', use_last_error=True).MoveFileExW
    move.argtypes = [wintypes.LPCWSTR, wintypes.LPCWSTR, wintypes.DWORD]
    move.restype = wintypes.BOOL
    # MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH; never defer or copy.
    if not move(str(source.absolute()), str(target.absolute()), 0x1 | 0x8):
        raise ctypes.WinError(ctypes.get_last_error())


def durable_replace(source, target):
    if os.name == 'nt':
        replace_windows(source, target)
    else:
        os.replace(source, target)
        sync_directory(target.parent)


def sync_directory(folder):
    descriptor = os.open(folder, os.O_RDONLY | getattr(os, 'O_DIRECTORY', 0))
    try:
        os.fsync(descriptor)
    finally:
        os.close(descriptor)


def create_output_folder(folder):
    folder.mkdir(parents=True, exist_ok=True)
    if os.name != 'nt':
        # Include existing ancestors: a prior failed sync may have left new entries.
        for parent in folder.parents:
            sync_directory(parent)


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def validate_url(url):
    parsed = urllib.parse.urlsplit(url)
    try:
        loopback = ipaddress.ip_address(parsed.hostname).is_loopback
    except ValueError:
        loopback = parsed.hostname == 'localhost'
    if parsed.scheme != 'http' or not loopback or parsed.username or parsed.password or parsed.path not in ('', '/') or parsed.query or parsed.fragment:
        raise ValueError('Use a local loopback HTTP ComfyUI URL without credentials or path')
    return url.rstrip('/')


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, file, code, message, headers, new_url):
        raise ValueError('Local ComfyUI redirected the request; refusing external dispatch')


class LocalAPI:
    def __init__(self, url):
        self.url = validate_url(url)
        self.opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
        self.deadline = None

    def request(self, route, data=None, content_type='application/json', binary=False):
        timeout = 30 if self.deadline is None else min(30, self.deadline - time.time())
        if timeout <= 0:
            raise TimeoutError('Fixed generation deadline expired; resume only to collect completed work')
        request = urllib.request.Request(self.url + route, data=data)
        if data is not None:
            request.add_header('Content-Type', content_type)
        with self.opener.open(request, timeout=timeout) as response:
            payload = response.read()
        return payload if binary else json.loads(payload)

    def get(self, route):
        return self.request(route)

    def post(self, route, value):
        return self.request(route, json.dumps(value).encode())

    def upload(self, data, suffix):
        boundary = 'swlor-' + uuid.uuid4().hex
        name = 'swlor-' + digest(data) + suffix.lower()
        payload = (f'--{boundary}\r\nContent-Disposition: form-data; name="image"; filename="{name}"\r\n'
                   'Content-Type: application/octet-stream\r\n\r\n').encode()
        payload += data + f'\r\n--{boundary}--\r\n'.encode()
        return self.request('/upload/image', payload, 'multipart/form-data; boundary=' + boundary)


@contextlib.contextmanager
def endpoint_lock(url):
    """OS lock releases on crash; all SWLOR output folders share one GPU owner."""
    # Port-independent: localhost, IPv4 and IPv6 aliases must not bypass ownership.
    path = Path(tempfile.gettempdir()) / 'swlor-trellis-gpu.lock'
    with path.open('a+b') as stream:
        stream.seek(0)
        if not stream.read(1):
            stream.write(b'0')
            stream.flush()
        stream.seek(0)
        if os.name == 'nt':
            import msvcrt
            msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
        else:
            import fcntl
            fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
        try:
            yield
        finally:
            stream.seek(0)
            if os.name == 'nt':
                msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)
            else:
                fcntl.flock(stream, fcntl.LOCK_UN)


def staging_path(path):
    path = path.resolve()
    repository = next((parent for parent in (path, *path.parents) if (parent / '.git').exists()), None)
    if repository is not None:
        relative = path.relative_to(repository).as_posix()
        tracked = subprocess.run(['git', '-C', str(repository), '--literal-pathspecs',
                                  'ls-files', '-z', '--', relative],
                                 check=True, capture_output=True)
        if tracked.stdout:
            raise ValueError(f'Authoring files must not be tracked or contain tracked files: {path}')
        result = subprocess.run(['git', '-C', str(repository), 'check-ignore', '--no-index', '--quiet', '--', relative], check=False)
        if result.returncode != 0:
            raise ValueError(f'Authoring files must be outside Git or explicitly ignored: {path}')
    return path


def build_graph(template, asset, image='input.png', prefix='3d/SWLOR/weapons/preflight'):
    if template.get('schema_version') != 1 or template.get('workflow_version') != VERSION:
        raise ValueError('Unsupported TRELLIS workflow version')
    graph = copy.deepcopy(template['graph'])
    shape = asset.get('shape_resolution', 512)
    texture = asset.get('texture_size', 1024)
    faces = asset.get('target_faces', 6000)
    seed = asset['seed']
    if shape not in (512, 1024) or texture not in (512, 1024):
        raise ValueError('Shape and texture resolutions must be 512 or 1024')
    if type(faces) is not int or not 100 <= faces <= 100000 or type(seed) is not int or not 0 <= seed < 2**64:
        raise ValueError('Expected 100..100000 target faces and unsigned 64-bit seed')
    graph['122']['inputs']['image'] = image
    graph['900']['inputs']['filename_prefix'] = prefix
    graph['288']['inputs']['value'] = texture
    graph['186']['inputs']['target_face_count'] = faces
    for node in ('3', '18', '23', '12'):
        graph[node]['inputs']['seed'] = seed
    if shape == 512:
        for node in graph.values():
            for key, value in node['inputs'].items():
                if isinstance(value, list) and len(value) == 2 and str(value[0]) in ('23', '94'):
                    node['inputs'][key] = [{'23': '18', '94': '91'}[str(value[0])], value[1]]
    needed = set()
    def visit(key):
        if key in needed:
            return
        needed.add(key)
        for value in graph[key]['inputs'].values():
            if isinstance(value, list) and len(value) == 2:
                visit(str(value[0]))
    visit('900')
    return {key: value for key, value in graph.items() if key in needed}


def node_schemas(api, graphs):
    names = sorted({node['class_type'] for graph in graphs for node in graph.values()})
    info = {}
    for name in names:
        info.update(api.get('/object_info/' + urllib.parse.quote(name)))
    return info


def preflight(graph, info):
    """Fail on node/input drift and missing model files before consuming GPU time."""
    for key, node in graph.items():
        schema = info.get(node['class_type'])
        if schema is None:
            raise ValueError(f'Missing ComfyUI node: {node["class_type"]}')
        if schema.get('api_node'):
            raise ValueError(f'External API node is not allowed: {node["class_type"]}')
        inputs = schema.get('input', {})
        required = inputs.get('required', {})
        allowed = {**required, **inputs.get('optional', {})}
        actual = node['inputs']
        required = dict(required)
        for name, description in list(allowed.items()):
            if description[0] == 'COMFY_DYNAMICCOMBO_V3':
                options = description[1]['options']
                selected = next((option for option in options if option['key'] == actual.get(name)), None)
                if selected is None:
                    raise ValueError(f'Invalid dynamic option {key}.{name}')
                for kind in ('required', 'optional'):
                    for child, definition in selected.get('inputs', {}).get(kind, {}).items():
                        allowed[name + '.' + child] = definition
                        if kind == 'required':
                            required[name + '.' + child] = definition
        missing = set(required) - set(actual)
        unknown = set(actual) - set(allowed)
        if missing or unknown:
            raise ValueError(f'Node {key} {node["class_type"]}: missing {missing}, unknown {unknown}')
        for name, value in actual.items():
            if isinstance(value, list):
                if len(value) != 2:
                    raise ValueError(f'Invalid link {key}.{name}')
                upstream, slot = str(value[0]), value[1]
                if upstream not in graph or type(slot) is not int or slot < 0 or slot >= len(info[graph[upstream]['class_type']]['output']):
                    raise ValueError(f'Invalid link {key}.{name}')
            elif name in allowed:
                definition = allowed[name]
                choices = definition[0] if isinstance(definition[0], list) else None
                if definition[0] == 'COMBO' and len(definition) > 1:
                    choices = definition[1].get('options')
                if choices is not None and name != 'image' and value not in choices:
                    raise ValueError(f'Unavailable option/model for {key}.{name}: {value}')
                kind = definition[0]
                scalar_types = {'INT': (int,), 'FLOAT': (int, float),
                                'BOOLEAN': (bool,), 'STRING': (str,)}
                if isinstance(kind, str) and kind in scalar_types:
                    # Reject implicit coercions that silently change authored values.
                    if type(value) not in scalar_types[kind]:
                        raise ValueError(f'Invalid {kind} value for {key}.{name}: {value!r}')
                    if kind in ('INT', 'FLOAT'):
                        if type(value) is float and not math.isfinite(value):
                            raise ValueError(f'Non-finite value for {key}.{name}')
                        options = definition[1] if len(definition) > 1 else {}
                        if 'min' in options and value < options['min']:
                            raise ValueError(f'Value below minimum for {key}.{name}: {value}')
                        if 'max' in options and value > options['max']:
                            raise ValueError(f'Value above maximum for {key}.{name}: {value}')
                        # ComfyUI step is a widget increment, not a server value constraint:
                        # https://docs.comfy.org/custom-nodes/backend/datatypes#additional-parameters



def recover_prompt(api, token):
    matches = set()
    queue = api.get('/queue')
    candidates = queue.get('queue_running', []) + queue.get('queue_pending', [])
    candidates += [entry.get('prompt', []) for entry in api.get('/history').values()]
    for entry in candidates:
        if len(entry) >= 4 and entry[3].get('swlor_token') == token:
            matches.add(entry[1])
    if len(matches) != 1:
        raise RuntimeError('Uncertain submission: no unique owned prompt in queue/history; do not resubmit')
    return matches.pop()


def extract_glb(data, folder):
    """Retain embedded PBR image bytes and explicit glTF channel/color-space metadata."""
    if len(data) < 20 or struct.unpack_from('<4sII', data) != (b'glTF', 2, len(data)):
        raise ValueError('Invalid GLB header')
    offset = 12; doc = None; blob = None
    while offset < len(data):
        size, kind = struct.unpack_from('<II', data, offset); offset += 8
        if offset + size > len(data):
            raise ValueError('Truncated GLB chunk')
        chunk = data[offset:offset+size]; offset += size
        if kind == 0x4E4F534A: doc = json.loads(chunk)
        elif kind == 0x004E4942: blob = chunk
    if doc is None or blob is None or not doc.get('meshes'):
        raise ValueError('GLB has no embedded mesh data')
    images = []
    for index, image in enumerate(doc.get('images', [])):
        if 'bufferView' not in image:
            raise ValueError('Expected embedded PBR images, not external URIs')
        view = doc['bufferViews'][image['bufferView']]
        start = view.get('byteOffset', 0); end = start + view['byteLength']
        if view.get('buffer', 0) != 0 or end > len(blob):
            raise ValueError('Invalid embedded image bounds')
        suffix = {'image/png': '.png', 'image/jpeg': '.jpg'}.get(image.get('mimeType'))
        if not suffix:
            raise ValueError('Unsupported embedded image type')
        name = f'image_{index}{suffix}'; payload = blob[start:end]
        write_bytes(folder / name, payload)
        images.append({'file': name, 'sha256': digest(payload)})
    materials = doc.get('materials', [])
    if not materials or not images:
        raise ValueError('Expected textured PBR GLB')
    return {'images': images, 'materials': materials, 'textures': doc.get('textures', []),
            'channels': {'baseColorTexture': 'sRGB RGB, linear alpha',
                         'metallicRoughnessTexture': 'linear G roughness, B metallic',
                         'normalTexture': 'OpenGL tangent RGB, only when present'},
            'mesh_count': len(doc['meshes'])}


def run_asset(api, template, asset, image, folder, timeout, identity):
    # One immutable snapshot owns the fingerprint, uploaded bytes and result hash.
    image_data = image.read_bytes()
    image_hash = digest(image_data)
    create_output_folder(folder)
    path = folder / 'submission.json'
    fingerprint = digest(json.dumps({'asset': asset, 'image_sha256': image_hash,
                                     'workflow': template, 'url': api.url}, sort_keys=True).encode())
    if path.exists():
        state = read_json(path)
        if state['fingerprint'] != fingerprint:
            raise ValueError('Existing job differs from input/workflow/endpoint; use a fresh asset ID/output')
    else:
        state = {'fingerprint': fingerprint, 'token': uuid.uuid4().hex,
                 'deadline_unix': time.time() + timeout, 'status': 'prepared'}
        write_json(path, state)
    complete = folder / 'generation.json'
    if complete.exists():
        result = read_json(complete)
        if result['sha256'] != digest((folder / 'source.glb').read_bytes()):
            raise ValueError('Completed source GLB changed')
        for image_record in result['pbr']['images']:
            if digest((folder / image_record['file']).read_bytes()) != image_record['sha256']:
                raise ValueError('Completed PBR image changed')
        return result
    api.deadline = None  # One bounded read can collect a completed job after its deadline.
    if state['status'] == 'submission-started' and 'prompt_id' not in state:
        state['prompt_id'] = recover_prompt(api, state['token'])
        write_json(path, state)
    if 'prompt_id' not in state:
        api.deadline = state['deadline_unix']
        graph = build_graph(template, asset)
        info = node_schemas(api, [graph])
        preflight(graph, info)
        write_json(folder / 'preflight.json', {'workflow_version': VERSION,
                   'workflow_sha256': digest(json.dumps(template, sort_keys=True).encode()),
                   'object_info_sha256': digest(json.dumps(info, sort_keys=True).encode()),
                   'checked_unix': time.time(), 'url': api.url})
        while True:
            queue = api.get('/queue')
            if not queue.get('queue_running') and not queue.get('queue_pending'):
                break
            time.sleep(min(2, max(0, api.deadline - time.time())))
        uploaded = api.upload(image_data, image.suffix)
        image_name = '/'.join(x for x in (uploaded.get('subfolder'), uploaded['name']) if x)
        graph = build_graph(template, asset, image_name, '3d/SWLOR/weapons/' + state['token'])
        write_json(folder / 'prompt.json', graph)
        state['status'] = 'submission-started'
        write_json(path, state)  # Persist before POST, including uncertain network outcomes.
        response = api.post('/prompt', {'prompt': graph, 'client_id': identity,
                                       'extra_data': {'swlor_token': state['token']}})
        if response.get('node_errors') or not response.get('prompt_id'):
            state['response'] = response
            write_json(path, state)
            raise RuntimeError('Prompt rejected/uncertain; retained response, no automatic retry')
        state.update(prompt_id=response['prompt_id'], status='submitted')
        write_json(path, state)
    while True:
        history = api.get('/history/' + urllib.parse.quote(state['prompt_id'])).get(state['prompt_id'])
        if history and (history.get('status', {}).get('completed') or history.get('status', {}).get('status_str') == 'error'):
            write_json(folder / 'history.json', history)
            if history['status']['status_str'] != 'success':
                raise RuntimeError('Owned generation failed; see history.json; not resubmitted')
            break
        if time.time() >= state['deadline_unix']:
            raise TimeoutError('Generation deadline exhausted; owned prompt retained, no interrupt/resubmit')
        api.deadline = state['deadline_unix']
        time.sleep(min(2, max(0, api.deadline - time.time())))
    api.deadline = None
    outputs = history['outputs']['900']['3d']
    if len(outputs) != 1:
        raise ValueError('Expected one SaveGLB result')
    descriptor = outputs[0]
    if descriptor.get('type', 'output') != 'output':
        raise ValueError('Unexpected GLB output location')
    data = api.request('/view?' + urllib.parse.urlencode({**descriptor, 'type': 'output'}), binary=True)
    metadata = extract_glb(data, folder)
    write_bytes(folder / 'source.glb', data)
    result = {'workflow_version': VERSION, 'asset': asset, 'prompt_id': state['prompt_id'],
              'input_sha256': image_hash, 'sha256': digest(data), 'bytes': len(data),
              'pbr': metadata, 'status': 'generated', 'native_verified': False}
    write_json(complete, result)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('manifest', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--url', default='http://127.0.0.1:8000')
    parser.add_argument('--workflow', type=Path, default=TEMPLATE)
    parser.add_argument('--timeout', type=int, default=1800, help='Fixed deadline seconds per asset, including queue wait')
    parser.add_argument('--preflight', action='store_true', help='Read-only API schema/model check; no upload or submission')
    args = parser.parse_args()
    if not 30 <= args.timeout <= 14400:
        raise ValueError('Timeout must be 30..14400 seconds')
    manifest_path = staging_path(args.manifest)
    manifest = read_json(manifest_path); template = read_json(args.workflow)
    if manifest.get('schema_version') != 1 or not manifest.get('assets'):
        raise ValueError('Expected schema_version 1 and nonempty assets')
    ids = [asset['id'] for asset in manifest['assets']]
    if len(set(ids)) != len(ids) or any(not re.fullmatch('[a-z][a-z0-9_-]{0,63}', key) for key in ids):
        raise ValueError('Asset IDs must be unique safe lowercase directory names')
    output = staging_path(args.output); api = LocalAPI(args.url)
    images = []
    for asset in manifest['assets']:
        image = staging_path(manifest_path.parent / asset['image'])
        if not image.is_file() or image.suffix.lower() not in ('.png', '.jpg', '.jpeg', '.webp'):
            raise ValueError(f'Missing/unsupported concept image: {image}')
        images.append(image)
    if args.preflight:
        graphs = [build_graph(template, asset) for asset in manifest['assets']]
        info = node_schemas(api, graphs)
        for graph in graphs:
            preflight(graph, info)
        print(f'Preflight passed: {len(ids)} assets, workflow {VERSION}; no jobs submitted')
        return
    with endpoint_lock(api.url):
        for asset, image in zip(manifest['assets'], images):
            print('TRACKING', asset['id'], flush=True)
            run_asset(api, template, asset, image, output / asset['id'], args.timeout, 'swlor-trellis')
            print('GENERATED', asset['id'], flush=True)


if __name__ == '__main__':
    main()
