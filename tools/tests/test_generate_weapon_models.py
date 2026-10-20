"""Recovery and source-output checks for the local TRELLIS runner; no GPU required."""
import importlib.util
import json
from pathlib import Path
import stat
import struct
import subprocess
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('generator', ROOT / 'tools/GenerateWeaponModels.py')
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)


def tracked_file_path(descriptor, paths):
    # POSIX directory sync may reuse a descriptor from a closed, renamed artifact.
    if not stat.S_ISREG(generator.os.fstat(descriptor).st_mode):
        return None
    return paths.get(descriptor)


def glb(view=None):
    document = {'asset': {'version': '2.0'}, 'meshes': [{'primitives': []}],
                'bufferViews': [view or {'buffer': 0, 'byteOffset': 0, 'byteLength': 4}],
                'images': [{'bufferView': 0, 'mimeType': 'image/png'},
                           {'bufferView': 0, 'mimeType': 'image/jpeg'}],
                'textures': [{'source': 0}], 'materials': [{'pbrMetallicRoughness': {
                    'baseColorTexture': {'index': 0}, 'metallicRoughnessTexture': {'index': 0}}}]}
    raw = json.dumps(document).encode(); raw += b' ' * (-len(raw) % 4)
    chunks = struct.pack('<II', len(raw), 0x4E4F534A) + raw + struct.pack('<II', 4, 0x004E4942) + b'test'
    return struct.pack('<4sII', b'glTF', 2, len(chunks) + 12) + chunks


def scalar_schemas():
    """Offline object_info fixture with native scalar types and linked output slots."""
    graph = generator.read_json(generator.TEMPLATE)['graph']
    info = {}
    kinds = {int: 'INT', float: 'FLOAT', str: 'STRING', bool: 'BOOLEAN'}
    for node in graph.values():
        schema = info.setdefault(node['class_type'], {'input': {'required': {}}, 'output': []})
        for name, value in node['inputs'].items():
            schema['input']['required'][name] = ['*' if isinstance(value, list) else kinds[type(value)]]
    for node in graph.values():
        for value in node['inputs'].values():
            if isinstance(value, list):
                output = info[graph[value[0]]['class_type']]['output']
                output.extend(['*'] * (value[1] + 1 - len(output)))
    return info


class FakeAPI:
    url = 'http://127.0.0.1:8189'
    deadline = None
    def __init__(self, folder, lost=False, fail=False):
        self.folder = folder; self.lost = lost; self.fail = fail
        self.posts = 0; self.token = None; self.history_reads = 0
        self.uploads = []; self.schemas = scalar_schemas(); self.schema_reads = 0

    def get(self, route):
        if route.startswith('/object_info/'):
            self.schema_reads += 1
            name = route.rsplit('/', 1)[-1]
            return {name: self.schemas[name]}
        if route == '/queue':
            return {'queue_running': [], 'queue_pending': []}
        entry = {'prompt': [1, 'owned', {}, {'swlor_token': self.token}],
                 'status': {'completed': not self.fail, 'status_str': 'error' if self.fail else 'success'},
                 'outputs': {'900': {'3d': [{'filename': 'source.glb', 'type': 'output'}]}}}
        self.history_reads += 1
        return {'owned': entry}

    def upload(self, data, suffix):
        self.uploads.append((data, suffix))
        return {'name': 'input.png'}

    def post(self, route, value):
        self.posts += 1
        assert route == '/prompt'
        state = generator.read_json(self.folder / 'submission.json')
        assert state['status'] == 'submission-started'
        assert generator.read_json(self.folder / 'prompt.json') == value['prompt']
        self.token = value['extra_data']['swlor_token']
        assert state['token'] == self.token
        if self.lost:
            raise TimeoutError('Response lost after accepted POST')
        return {'prompt_id': 'owned', 'node_errors': {}}

    def request(self, route, binary=False):
        assert route.startswith('/view?') and binary
        return glb()


class WeaponGenerationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.folder = Path(self.temporary.name)
        # Never contend with an actual generation process during offline tests.
        lock_namespace = patch.object(generator.tempfile, 'gettempdir', return_value=str(self.folder))
        lock_namespace.start()
        self.addCleanup(lock_namespace.stop)
        self.image = self.folder / 'input.png'; self.image.write_bytes(b'concept')
        self.output = self.folder / 'result'
        self.template = generator.read_json(generator.TEMPLATE)
        self.asset = {'id': 'test_weapon', 'image': 'input.png', 'seed': 123}

    def run_asset(self, api):
        return generator.run_asset(api, self.template, self.asset, self.image, self.output, 60, 'test')

    def test_journal_precedes_post_and_completion_is_not_resubmitted(self):
        api = FakeAPI(self.output)
        first = self.run_asset(api)
        self.assertEqual(first, self.run_asset(api))
        self.assertEqual(api.posts, 1)
        self.assertEqual((self.output / 'source.glb').read_bytes(), glb())
        self.assertEqual((self.output / 'image_0.png').read_bytes(), b'test')
        self.assertIn('linear G roughness', first['pbr']['channels']['metallicRoughnessTexture'])

    def test_journal_replacement_flushes_before_the_submission_post(self):
        publish=generator.durable_replace;barriers=[]
        def durable(source,target):
            publish(source,target)
            if target.name=='submission.json':barriers.append(generator.read_json(target)['status'])
        api=FakeAPI(self.output);post=api.post
        def submit(route,value):
            self.assertEqual(barriers,['prepared','submission-started'])
            return post(route,value)
        api.post=submit
        with patch.object(generator,'durable_replace',side_effect=durable):self.run_asset(api)
        self.assertEqual(api.posts,1)

    def test_journal_sync_failure_prevents_submission(self):
        publish=generator.durable_replace
        def fail_before_post(source,target):
            if target.name=='submission.json' and generator.read_json(source)['status']=='submission-started':
                raise OSError('journal metadata sync failed')
            publish(source,target)
        api=FakeAPI(self.output)
        with patch.object(generator,'durable_replace',side_effect=fail_before_post):
            with self.assertRaisesRegex(OSError,'metadata sync'):self.run_asset(api)
        self.assertEqual(api.posts,0)

    def test_posix_replacement_syncs_directory_after_rename_and_closes_on_failure(self):
        target=self.folder/'journal.json';source=self.folder/'journal.json.tmp'
        events=[]
        def renamed(*args):events.append('rename')
        def opened(path,flags):
            self.assertEqual(path,target.parent);events.append('open');return 37
        def synced(fd):self.assertEqual(fd,37);events.append('sync')
        def closed(fd):self.assertEqual(fd,37);events.append('close')
        with patch.object(generator.os,'name','posix'),patch.object(generator.os,'replace',side_effect=renamed),patch.object(generator.os,'open',side_effect=opened),patch.object(generator.os,'fsync',side_effect=synced),patch.object(generator.os,'close',side_effect=closed):
            generator.durable_replace(source,target)
        self.assertEqual(events,['rename','open','sync','close'])
        with patch.object(generator.os,'name','posix'),patch.object(generator.os,'replace'),patch.object(generator.os,'open',return_value=37),patch.object(generator.os,'fsync',side_effect=OSError('sync failed')),patch.object(generator.os,'close') as close:
            with self.assertRaises(OSError):generator.durable_replace(source,target)
            close.assert_called_once_with(37)

    def test_new_output_directories_publish_parent_entries_before_submission(self):
        folder=self.folder/'nested'/'new_asset';synced=[]
        with patch.object(generator.os,'name','posix'),patch.object(generator,'sync_directory',side_effect=synced.append):
            generator.create_output_folder(folder)
        self.assertTrue(folder.is_dir())
        self.assertEqual(synced,list(folder.parents))
        with patch.object(generator.os,'name','posix'),patch.object(generator,'sync_directory') as sync:
            generator.create_output_folder(folder)
            self.assertEqual([call.args[0] for call in sync.call_args_list],list(folder.parents))
        api=FakeAPI(self.output)
        with patch.object(generator.os,'name','posix'),patch.object(generator,'sync_directory',side_effect=OSError('directory sync failed')):
            with self.assertRaisesRegex(OSError,'directory sync'):self.run_asset(api)
        self.assertEqual(api.posts,0)

    def test_windows_replacement_requests_write_through_and_propagates_failure(self):
        if generator.os.name!='nt':self.skipTest('Windows native replacement')
        import ctypes
        from unittest.mock import Mock
        move=Mock(return_value=1);kernel=Mock(MoveFileExW=move)
        source=self.folder/'journal.tmp';target=self.folder/'journal.json'
        with patch.object(ctypes,'WinDLL',return_value=kernel) as load:
            generator.replace_windows(source,target)
            load.assert_called_once_with('kernel32',use_last_error=True)
            move.assert_called_once_with(str(source.absolute()),str(target.absolute()),9)
            move.return_value=0
            with patch.object(ctypes,'get_last_error',return_value=5),self.assertRaises(OSError):
                generator.replace_windows(source,target)

    def test_lost_response_recovers_owned_history_without_post(self):
        api = FakeAPI(self.output, lost=True)
        with self.assertRaises(TimeoutError): self.run_asset(api)
        deadline = generator.read_json(self.output / 'submission.json')['deadline_unix']
        result = self.run_asset(api)
        self.assertEqual(result['prompt_id'], 'owned')
        self.assertEqual(api.posts, 1)
        self.assertEqual(generator.read_json(self.output / 'submission.json')['deadline_unix'], deadline)

    def test_saved_success_recollects_after_download_failure_and_comfy_restart(self):
        api = FakeAPI(self.output)
        with patch.object(api, 'request', side_effect=TimeoutError('download interrupted')):
            with self.assertRaisesRegex(TimeoutError, 'download interrupted'):
                self.run_asset(api)
        journal = (self.output / 'submission.json').read_bytes()
        history = (self.output / 'history.json').read_bytes()
        deadline = generator.read_json(self.output / 'submission.json')['deadline_unix']
        # Restarted Comfy has no in-memory history, but its output file remains.
        with patch.object(api, 'get', side_effect=AssertionError('Do not require volatile server history')), patch.object(
                generator.time, 'time', return_value=deadline + 120):
            result = self.run_asset(api)
        self.assertEqual(result['prompt_id'], 'owned');self.assertEqual(api.posts, 1)
        self.assertEqual(len(api.uploads), 1)
        self.assertEqual((self.output / 'submission.json').read_bytes(), journal)
        self.assertEqual((self.output / 'history.json').read_bytes(), history)
        self.assertEqual((self.output / 'source.glb').read_bytes(), glb())

    def test_saved_terminal_error_remains_failure_after_comfy_restart(self):
        api = FakeAPI(self.output, fail=True)
        with self.assertRaisesRegex(RuntimeError, 'Owned generation failed'):
            self.run_asset(api)
        journal = (self.output / 'submission.json').read_bytes()
        deadline = generator.read_json(self.output / 'submission.json')['deadline_unix']
        with patch.object(api, 'get', side_effect=AssertionError('Do not require volatile server history')), patch.object(
                api, 'request', side_effect=AssertionError('Failed job has no output to collect')), patch.object(
                generator.time, 'time', return_value=deadline + 120):
            with self.assertRaisesRegex(RuntimeError, 'Owned generation failed'):
                self.run_asset(api)
        self.assertEqual(api.posts, 1);self.assertEqual(len(api.uploads), 1)
        self.assertEqual((self.output / 'submission.json').read_bytes(), journal)

    def test_saved_history_rejects_mismatched_prompt_id_or_submission_token(self):
        api = FakeAPI(self.output)
        with patch.object(api, 'request', side_effect=TimeoutError('download interrupted')):
            with self.assertRaises(TimeoutError):
                self.run_asset(api)
        path = self.output / 'history.json';original = path.read_bytes()
        for field in ('prompt_id', 'token'):
            with self.subTest(field=field):
                history = json.loads(original)
                if field == 'prompt_id':history['prompt'][1] = 'another-prompt'
                else:history['prompt'][3]['swlor_token'] = 'another-token'
                generator.write_json(path, history)
                with patch.object(api, 'get', side_effect=AssertionError('Unexpected API read')), patch.object(
                        api, 'request', side_effect=AssertionError('Wrong-owned output must not be downloaded')):
                    with self.assertRaisesRegex(ValueError, 'different prompt|different submission token'):
                        self.run_asset(api)
                self.assertFalse((self.output / 'generation.json').exists())
        self.assertEqual(api.posts, 1)

    def test_saved_raw_history_without_prompt_metadata_remains_readable(self):
        api = FakeAPI(self.output)
        with patch.object(api, 'request', side_effect=TimeoutError('download interrupted')):
            with self.assertRaises(TimeoutError):
                self.run_asset(api)
        path = self.output / 'history.json';history = generator.read_json(path);history.pop('prompt')
        generator.write_json(path, history)
        with patch.object(api, 'get', side_effect=AssertionError('Unexpected API read')):
            self.assertEqual(self.run_asset(api)['prompt_id'], 'owned')
        self.assertEqual(api.posts, 1)

    def test_uncertain_missing_prompt_is_not_resubmitted(self):
        api = FakeAPI(self.output, lost=True)
        with self.assertRaises(TimeoutError): self.run_asset(api)
        api.token = 'someone_else'
        with self.assertRaisesRegex(RuntimeError, 'Uncertain'): self.run_asset(api)
        self.assertEqual(api.posts, 1)

    def test_terminal_failure_is_not_resubmitted(self):
        api = FakeAPI(self.output, fail=True)
        for _ in range(2):
            with self.assertRaisesRegex(RuntimeError, 'failed'): self.run_asset(api)
        self.assertEqual(api.posts, 1)

    def test_mutated_inputs_refused_on_resume(self):
        api = FakeAPI(self.output); self.run_asset(api)
        self.image.write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError, 'differs'): self.run_asset(api)
        self.assertEqual(api.posts, 1)

    def test_expired_pending_job_keeps_original_deadline_and_no_post(self):
        api = FakeAPI(self.output, lost=True)
        with self.assertRaises(TimeoutError): self.run_asset(api)
        state = generator.read_json(self.output / 'submission.json')
        state.update(prompt_id='owned', deadline_unix=time.time()-5)
        generator.write_json(self.output / 'submission.json', state)
        api.get = lambda route: {}
        with self.assertRaises(TimeoutError): self.run_asset(api)
        self.assertEqual(api.posts, 1)
        self.assertEqual(generator.read_json(self.output / 'submission.json')['deadline_unix'], state['deadline_unix'])

    def test_unsubmitted_prepared_job_renews_an_expired_deadline(self):
        api = FakeAPI(self.output)
        generator.create_output_folder(self.output)
        fingerprint = generator.digest(json.dumps({'asset': self.asset, 'image_sha256': generator.digest(self.image.read_bytes()),
                                                   'workflow': self.template, 'url': api.url}, sort_keys=True).encode())
        generator.write_json(self.output / 'submission.json', {'fingerprint': fingerprint, 'token': 'abc',
                                                               'deadline_unix': time.time() - 5, 'status': 'prepared'})
        self.assertEqual(self.run_asset(api)['status'], 'generated')
        self.assertGreater(generator.read_json(self.output / 'submission.json')['deadline_unix'], time.time())
        self.assertEqual(api.posts, 1)

    def test_occupied_asset_folder_without_a_journal_is_refused(self):
        self.output.mkdir()
        keep = self.output / 'source.glb'; keep.write_bytes(b'manual')
        api = FakeAPI(self.output)
        with self.assertRaisesRegex(ValueError, 'no job journal'): self.run_asset(api)
        self.assertEqual(keep.read_bytes(), b'manual')
        self.assertEqual(api.posts, 0); self.assertEqual(api.uploads, [])

    def test_interrupted_first_journal_write_is_recovered(self):
        self.output.mkdir()
        (self.output / 'submission.json.tmp').write_text('{"fingerprint": "trunc')
        api = FakeAPI(self.output)
        self.assertEqual(self.run_asset(api)['status'], 'generated')
        self.assertFalse((self.output / 'submission.json.tmp').exists()); self.assertEqual(api.posts, 1)
        # Anything beyond the runner's own temporary journal still marks the folder as someone else's.
        other = self.output.parent / 'other'; other.mkdir()
        (other / 'submission.json.tmp').write_text('{}'); keep = other / 'source.glb'; keep.write_bytes(b'manual')
        with self.assertRaisesRegex(ValueError, 'no job journal'):
            generator.run_asset(FakeAPI(other), self.template, self.asset, self.image, other, 60, 'test')
        self.assertEqual(keep.read_bytes(), b'manual')

    def test_budget_512_removes_upsampling_and_1024_retains_it(self):
        low = generator.build_graph(self.template, self.asset)
        self.assertNotIn('23', low); self.assertNotIn('94', low)
        self.assertEqual(low['92']['inputs']['samples'], ['18', 0])
        self.assertEqual(low['210']['inputs']['normal_map'], ['224', 0])
        self.assertEqual(low['224']['inputs']['resolution'], ['288', 0])
        high = generator.build_graph(self.template, {**self.asset, 'shape_resolution': 1024, 'texture_size': 512, 'target_faces': 3500})
        self.assertIn('23', high)
        self.assertEqual(high['288']['inputs']['value'], 512)
        self.assertEqual(high['186']['inputs']['target_face_count'], 3500)
        self.assertEqual(high['23']['inputs']['seed'], 123)

    def test_invalid_budgets_and_workflow_fail(self):
        for values in ({'shape_resolution': 256}, {'texture_size': 2048}, {'target_faces': 0}, {'seed': -1}):
            with self.assertRaises(ValueError): generator.build_graph(self.template, {**self.asset, **values})
        with self.assertRaises(ValueError): generator.build_graph({**self.template, 'workflow_version': 'future'}, self.asset)

    def test_live_schema_drift_fails_closed(self):
        graph = {'1': {'class_type': 'Loader', 'inputs': {'model': 'missing'}}}
        info = {'Loader': {'input': {'required': {'model': [['available']] }}, 'output': ['MODEL']}}
        with self.assertRaisesRegex(ValueError, 'Unavailable'): generator.preflight(graph, info)
        info['Loader']['input']['required']['model'] = ['COMBO', {'options': ['available']}]
        with self.assertRaisesRegex(ValueError, 'Unavailable'): generator.preflight(graph, info)
        graph['1']['inputs'] = {'renamed': 'available'}
        with self.assertRaisesRegex(ValueError, 'missing'): generator.preflight(graph, info)
        with self.assertRaisesRegex(ValueError, 'Missing'): generator.preflight(graph, {})

    def test_linked_type_compatibility_matches_comfy_union_and_wildcard_rules(self):
        cases = [('IMAGE', 'MODEL', False), ('MODEL', 'MODEL', True),
                 ('IMAGE, MODEL', 'MODEL,LATENT', True), ('IMAGE,MODEL', 'LATENT', False),
                 ('IMAGE,MODEL', 'MODEL', True), ('IMAGE', 'MODEL,*', True),
                 ('*', 'MODEL', True), ('IMAGE', '*', True),
                 ('COMFY_MATCHTYPE_V3', 'MODEL', True), ('IMAGE', 'COMFY_MATCHTYPE_V3', True),
                 (['first', 'second'], 'COMBO', True), (['first'], ['first'], True),
                 (['first'], ['second'], False), ('COMBO', ['first'], False)]
        for received, expected, valid in cases:
            with self.subTest(received=received, expected=expected):
                graph = {'1': {'class_type': 'Producer', 'inputs': {}},
                         '2': {'class_type': 'Consumer', 'inputs': {'value': ['1', 0]}}}
                info = {'Producer': {'input': {'required': {}}, 'output': [received]},
                        'Consumer': {'input': {'required': {'value': [expected]}}}}
                if valid:
                    generator.preflight(graph, info)
                else:
                    with self.assertRaisesRegex(ValueError, 'Incompatible link'):
                        generator.preflight(graph, info)

    def test_link_type_schema_drift_blocks_fresh_and_prepared_jobs_before_upload(self):
        self.output = self.folder / self.asset['id']
        api = FakeAPI(self.output)
        graph = generator.build_graph(self.template, self.asset)
        sampler = next(node for node in graph.values() if node['class_type'] == 'KSampler')
        upstream, slot = sampler['inputs']['model']
        api.schemas['KSampler']['input']['required']['model'] = ['MODEL']
        # The node and slot still exist, but their live output type changed.
        api.schemas[graph[upstream]['class_type']]['output'][slot] = 'IMAGE'
        for _ in range(2):
            with self.assertRaisesRegex(ValueError, "Incompatible link .*IMAGE.*MODEL"):
                self.invoke_main(api)
            self.assertEqual(api.uploads, []);self.assertEqual(api.posts, 0)
            self.assertEqual(generator.read_json(self.output / 'submission.json')['status'], 'prepared')

    def test_external_api_nodes_and_dynamic_input_drift_refused(self):
        graph = {'1': {'class_type': 'Remote', 'inputs': {}}}
        with self.assertRaisesRegex(ValueError, 'External API'):
            generator.preflight(graph, {'Remote': {'api_node': True}})
        graph = {'1': {'class_type': 'Remesh', 'inputs': {'mode': 'udf', 'mode.misspelled': False}}}
        info = {'Remesh': {'input': {'required': {'mode': ['COMFY_DYNAMICCOMBO_V3', {
            'options': [{'key': 'udf', 'inputs': {'required': {'qef': ['BOOLEAN']}}}]}]}}}}
        with self.assertRaisesRegex(ValueError, 'missing'):
            generator.preflight(graph, info)
        graph['1']['inputs'] = {'mode': 'udf', 'mode.qef': False}
        generator.preflight(graph, info)

    def test_changed_pbr_output_is_not_silently_reused(self):
        api = FakeAPI(self.output); self.run_asset(api)
        (self.output / 'image_0.png').write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError, 'PBR image changed'): self.run_asset(api)
        self.assertEqual(api.posts, 1)

    def test_nonlocal_endpoints_and_repository_outputs_refused(self):
        for value in ('https://example.com', 'http://192.168.1.1:8000', 'http://127.0.0.1/api', 'http://user@localhost:8000'):
            with self.assertRaises(ValueError): generator.validate_url(value)
        self.assertEqual(generator.validate_url('http://[::1]:8189/'), 'http://[::1]:8189')
        repository = self.folder / 'repository'
        repository.mkdir()
        subprocess.run(['git', 'init', '--quiet', str(repository)], check=True)
        with self.assertRaises(ValueError): generator.staging_path(repository / 'authoring')

    def test_process_lock_rejects_concurrent_owner_even_other_url(self):
        with generator.endpoint_lock('http://127.0.0.1:8189'):
            self.assertTrue((self.folder / 'swlor-trellis-gpu.lock').is_file())
            with self.assertRaises(OSError):
                with generator.endpoint_lock('http://localhost:8189'):
                    self.fail('Second owner entered')

    def test_serial_main_waits_for_result_before_next_submission(self):
        manifest = self.folder / 'manifest.json'
        manifest.write_text(json.dumps({'schema_version': 1, 'assets': [self.asset, {**self.asset, 'id': 'second'}]}))
        calls = []
        class SerialAPI(FakeAPI):
            def __init__(inner, url):
                super().__init__(self.folder)
            def get(inner, route):
                if route.startswith('/object_info/'): return {}
                return super().get(route)
        def run(api, template, asset, image, folder, timeout, identity):
            calls.append(asset['id'])
            self.assertEqual(len(calls), 1 if asset['id'] == 'test_weapon' else 2)
            if asset['id'] == 'test_weapon':
                raise TimeoutError('First prompt still running')
        with patch.object(generator, 'LocalAPI', SerialAPI), patch.object(generator, 'preflight'), patch.object(generator, 'run_asset', run), patch('sys.argv', ['runner', str(manifest), '--output', str(self.output)]):
            with self.assertRaises(TimeoutError): generator.main()
        self.assertEqual(calls, ['test_weapon'])

    def invoke_main(self, api, *options):
        manifest = self.folder / 'manifest.json'
        manifest.write_text(json.dumps({'schema_version': 1, 'assets': [self.asset]}))
        with patch.object(generator, 'LocalAPI', return_value=api), patch('sys.argv', [
                'runner', str(manifest), '--output', str(self.folder), *options]):
            generator.main()

    def test_main_completed_job_is_verified_without_api_access(self):
        self.output = self.folder / self.asset['id']
        api = FakeAPI(self.output)
        self.run_asset(api)
        with patch.object(api, 'get', side_effect=AssertionError('Offline API access')):
            self.invoke_main(api)
            self.image.write_bytes(b'changed input')
            with self.assertRaisesRegex(ValueError, 'differs'):
                self.invoke_main(api)
        self.assertEqual(api.posts, 1)
        self.assertEqual(len(api.uploads), 1)

    def test_main_recovers_owned_jobs_without_current_schemas(self):
        for status in ('submitted', 'submission-started'):
            with self.subTest(status=status):
                self.asset = {**self.asset, 'id': status}
                self.output = self.folder / status
                api = FakeAPI(self.output, lost=True)
                with self.assertRaises(TimeoutError):
                    self.run_asset(api)
                if status == 'submitted':
                    state = generator.read_json(self.output / 'submission.json')
                    state.update(status=status, prompt_id='owned')
                    generator.write_json(self.output / 'submission.json', state)
                get = api.get
                def no_schema(route):
                    self.assertFalse(route.startswith('/object_info/'))
                    return get(route)
                with patch.object(api, 'get', side_effect=no_schema):
                    self.invoke_main(api)
                self.assertTrue((self.output / 'generation.json').exists())
                self.assertEqual(api.posts, 1)
                self.assertEqual(len(api.uploads), 1)

    def test_main_fresh_and_prepared_jobs_validate_live_bounds_before_upload(self):
        self.output = self.folder / self.asset['id']
        api = FakeAPI(self.output)
        api.schemas['PrimitiveInt']['input']['required']['value'] = ['INT', {'max': 512}]
        for _ in range(2):
            before = api.schema_reads
            with self.assertRaisesRegex(ValueError, 'above maximum'):
                self.invoke_main(api)
            self.assertGreater(api.schema_reads, before)
            self.assertEqual(api.posts, 0)
            self.assertEqual(api.uploads, [])
            self.assertEqual(generator.read_json(self.output / 'submission.json')['status'], 'prepared')

    def test_explicit_preflight_remains_live_and_read_only(self):
        api = FakeAPI(self.output)
        before = set(self.folder.rglob('*'))
        # The manifest is an input, written by this test's main helper.
        with patch.object(generator, 'endpoint_lock', side_effect=AssertionError('Unexpected lock')):
            self.invoke_main(api, '--preflight')
        self.assertGreater(api.schema_reads, 0)
        self.assertEqual(api.uploads, [])
        self.assertEqual(api.posts, 0)
        self.assertEqual(set(self.folder.rglob('*')) - before, {self.folder / 'manifest.json'})

    def test_queued_job_uses_one_concept_snapshot_for_upload_and_journal(self):
        api = FakeAPI(self.output)
        get = api.get
        queue_reads = 0
        def queued(route):
            nonlocal queue_reads
            if route == '/queue':
                queue_reads += 1
                if queue_reads == 1:
                    return {'queue_pending': ['other job']}
            return get(route)
        read = Path.read_bytes
        concept_reads = []
        def read_bytes(path):
            if path == self.image:
                concept_reads.append(path)
            return read(path)
        with patch.object(api, 'get', side_effect=queued), patch.object(generator.time, 'sleep',
                side_effect=lambda _: self.image.write_bytes(b'changed during queue wait')), patch.object(
                    Path, 'read_bytes', read_bytes):
            result = self.run_asset(api)
        self.assertEqual(concept_reads, [self.image])
        self.assertEqual(api.uploads, [(b'concept', '.png')])
        self.assertEqual(result['input_sha256'], generator.digest(b'concept'))
        fingerprint = generator.digest(json.dumps({'asset': self.asset,
            'image_sha256': result['input_sha256'], 'workflow': self.template,
            'url': api.url}, sort_keys=True).encode())
        self.assertEqual(generator.read_json(self.output / 'submission.json')['fingerprint'], fingerprint)
        with self.assertRaisesRegex(ValueError, 'differs'):
            self.run_asset(api)

    def test_upload_multipart_filename_and_payload_share_supplied_snapshot(self):
        api = generator.LocalAPI(FakeAPI.url)
        with patch.object(api, 'request', return_value={'name': 'uploaded'}) as request:
            api.upload(b'concept snapshot', '.PNG')
        route, payload, content_type = request.call_args.args
        self.assertEqual(route, '/upload/image')
        self.assertIn(('swlor-' + generator.digest(b'concept snapshot') + '.png').encode(), payload)
        self.assertIn(b'\r\n\r\nconcept snapshot\r\n', payload)
        self.assertTrue(content_type.startswith('multipart/form-data; boundary='))

    def test_every_artifact_is_fsynced_and_published_before_completion(self):
        opened = Path.open
        fsync = generator.os.fsync
        publish = generator.durable_replace
        descriptors = {}
        events = []
        def open_file(path, *args, **kwargs):
            stream = opened(path, *args, **kwargs)
            descriptors[stream.fileno()] = path
            return stream
        def sync_file(fd):
            path = tracked_file_path(fd, descriptors)
            if path and path.name.endswith(('.glb.tmp', '.png.tmp', '.jpg.tmp')):
                self.assertGreater(path.stat().st_size, 0)  # Python buffering was flushed.
                events.append(('sync', path.name))
            fsync(fd)
        def publish_file(source, target):
            if target.name == 'generation.json':
                for name in ('source.glb', 'image_0.png', 'image_1.jpg'):
                    self.assertIn(('sync', name + '.tmp'), events)
                    self.assertIn(('publish', name), events)
                    self.assertLess(events.index(('sync', name + '.tmp')), events.index(('publish', name)))
            publish(source, target)
            events.append(('publish', target.name))
        with patch.object(Path, 'open', open_file), patch.object(generator.os, 'fsync', side_effect=sync_file), patch.object(
                generator, 'durable_replace', side_effect=publish_file):
            self.run_asset(FakeAPI(self.output))
        self.assertEqual(events[-1], ('publish', 'generation.json'))

    def test_artifact_sync_tracking_ignores_directory_descriptor_reuse(self):
        temporary = self.folder / 'source.glb.tmp'
        with temporary.open('wb') as stream:
            descriptor = stream.fileno()
            paths = {descriptor: temporary}
            self.assertEqual(tracked_file_path(descriptor, paths), temporary)
        temporary.rename(self.folder / 'source.glb')
        self.assertFalse(temporary.exists())
        # Model the same numeric descriptor being allocated to the parent directory
        # after rename. This exercises POSIX reuse deterministically on Windows too.
        with patch.object(generator.os, 'fstat', return_value=self.folder.stat()) as fstat:
            self.assertIsNone(tracked_file_path(descriptor, paths))
        fstat.assert_called_once_with(descriptor)

    def test_artifact_fsync_failure_never_publishes_completion(self):
        opened = Path.open
        fsync = generator.os.fsync
        for name in ('source.glb.tmp', 'image_0.png.tmp', 'image_1.jpg.tmp'):
            with self.subTest(artifact=name):
                self.output = self.folder / name
                descriptors = {}
                def open_file(path, *args, **kwargs):
                    stream = opened(path, *args, **kwargs)
                    descriptors[stream.fileno()] = path
                    return stream
                def sync_file(fd):
                    path = tracked_file_path(fd, descriptors)
                    if path and path.name == name:
                        raise OSError('artifact sync failed')
                    fsync(fd)
                api = FakeAPI(self.output)
                with patch.object(Path, 'open', open_file), patch.object(generator.os, 'fsync', side_effect=sync_file):
                    with self.assertRaisesRegex(OSError, 'artifact sync'):
                        self.run_asset(api)
                self.assertFalse((self.output / 'generation.json').exists())
                self.run_asset(api)
                self.assertEqual(api.posts, 1)

    def test_live_scalar_types_and_bounds_reject_invalid_authored_values(self):
        cases = [('INT', 1.5, {}), ('INT', True, {}), ('INT', '1', {}),
                 ('FLOAT', False, {}), ('FLOAT', '1.5', {}), ('FLOAT', float('nan'), {}),
                 ('FLOAT', float('inf'), {}), ('BOOLEAN', 1, {}), ('BOOLEAN', 'false', {}),
                 ('STRING', 12, {}), ('STRING', None, {}), ('INT', 0, {'min': 1}),
                 ('INT', 11, {'max': 10}), ('FLOAT', 0.09, {'min': 0.1}),
                 ('FLOAT', 1.01, {'max': 1.0})]
        for kind, value, options in cases:
            with self.subTest(kind=kind, value=value, options=options):
                graph = {'1': {'class_type': 'Scalar', 'inputs': {'value': value}}}
                info = {'Scalar': {'input': {'required': {'value': [kind, options]}}}}
                with self.assertRaises(ValueError):
                    generator.preflight(graph, info)

    def test_live_scalar_boundaries_and_widget_steps_accept_valid_values(self):
        for kind, value, options in [('INT', 1, {'min': 1, 'max': 1}),
                ('FLOAT', 1, {'min': 1, 'max': 1}), ('BOOLEAN', False, {}), ('STRING', '', {}),
                ('INT', 3, {'min': 0, 'max': 10, 'step': 2}),
                ('FLOAT', 0.15, {'min': 0, 'max': 1, 'step': 0.1})]:
            with self.subTest(kind=kind, value=value):
                generator.preflight({'1': {'class_type': 'Scalar', 'inputs': {'value': value}}},
                    {'Scalar': {'input': {'required': {'value': [kind, options]}}}})

    def test_staging_rejects_force_tracked_ignored_files_and_descendants(self):
        repository = self.folder / 'repository'
        repository.mkdir()
        subprocess.run(['git', 'init', '--quiet', str(repository)], check=True)
        (repository / '.gitignore').write_text('*.png\nstaging/\n')
        staging = repository / 'staging'
        staging.mkdir()
        nested = staging / 'nested'
        nested.mkdir()
        for path in (repository / '[tracked].png', nested / 'source.png'):
            path.write_bytes(b'tracked source')
            subprocess.run(['git', '-C', str(repository), '--literal-pathspecs', 'add', '-f', '--',
                            path.relative_to(repository).as_posix()], check=True)
        for path in (repository / '[tracked].png', nested / 'source.png', nested, staging):
            with self.subTest(path=path), self.assertRaisesRegex(ValueError, 'tracked'):
                generator.staging_path(path)
        for path in (repository / 'untracked.png', staging / 'fresh', self.folder / 'external'):
            self.assertEqual(generator.staging_path(path), path.resolve())

    def test_invalid_glb_cannot_publish_completion(self):
        with self.assertRaises(ValueError): generator.extract_glb(b'glTF', self.folder)
        data = bytearray(glb()); struct.pack_into('<I', data, 12, len(data)*2)
        with self.assertRaises(ValueError): generator.extract_glb(bytes(data), self.folder)

    def test_embedded_image_ranges_must_be_nonnegative_and_inside_blob(self):
        for view in ({'byteOffset': -4, 'byteLength': 4}, {'byteOffset': 4, 'byteLength': -4},
                     {'byteOffset': 0, 'byteLength': -1}, {'byteOffset': 1.0, 'byteLength': 2},
                     {'byteOffset': 0, 'byteLength': True}, {'byteOffset': 0}, {'byteOffset': 2, 'byteLength': 3}):
            with self.subTest(view=view), self.assertRaisesRegex(ValueError, 'bounds'):
                generator.extract_glb(glb(dict(view, buffer=0)), self.folder)
        self.assertEqual(len(generator.extract_glb(glb({'buffer': 0, 'byteOffset': 1, 'byteLength': 3}), self.folder)['images']), 2)

    def test_image_buffer_view_index_must_exist(self):
        for index in (-1, 1, '0', True, 0.0):
            data = glb(); doc_end = 20 + struct.unpack_from('<I', data, 12)[0]
            document = json.loads(data[20:doc_end]); document['images'][0]['bufferView'] = index
            raw = json.dumps(document).encode(); raw += b' ' * (-len(raw) % 4)
            chunks = struct.pack('<II', len(raw), 0x4E4F534A) + raw + data[doc_end:]
            with self.subTest(index=index), self.assertRaisesRegex(ValueError, 'bufferView'):
                generator.extract_glb(struct.pack('<4sII', b'glTF', 2, len(chunks) + 12) + chunks, self.folder)


if __name__ == '__main__':
    unittest.main()
