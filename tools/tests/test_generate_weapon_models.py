"""Recovery and source-output checks for the local TRELLIS runner; no GPU required."""
import importlib.util
import json
from pathlib import Path
import struct
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('generator', ROOT / 'tools/GenerateWeaponModels.py')
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)


def glb():
    document = {'asset': {'version': '2.0'}, 'meshes': [{'primitives': []}],
                'bufferViews': [{'buffer': 0, 'byteOffset': 0, 'byteLength': 4}],
                'images': [{'bufferView': 0, 'mimeType': 'image/png'}],
                'textures': [{'source': 0}], 'materials': [{'pbrMetallicRoughness': {
                    'baseColorTexture': {'index': 0}, 'metallicRoughnessTexture': {'index': 0}}}]}
    raw = json.dumps(document).encode(); raw += b' ' * (-len(raw) % 4)
    chunks = struct.pack('<II', len(raw), 0x4E4F534A) + raw + struct.pack('<II', 4, 0x004E4942) + b'test'
    return struct.pack('<4sII', b'glTF', 2, len(chunks) + 12) + chunks


class FakeAPI:
    url = 'http://127.0.0.1:8189'
    deadline = None
    def __init__(self, folder, lost=False, fail=False):
        self.folder = folder; self.lost = lost; self.fail = fail
        self.posts = 0; self.token = None; self.history_reads = 0

    def get(self, route):
        if route == '/queue':
            return {'queue_running': [], 'queue_pending': []}
        entry = {'prompt': [1, 'owned', {}, {'swlor_token': self.token}],
                 'status': {'completed': not self.fail, 'status_str': 'error' if self.fail else 'success'},
                 'outputs': {'900': {'3d': [{'filename': 'source.glb', 'type': 'output'}]}}}
        self.history_reads += 1
        return {'owned': entry}

    def upload(self, image):
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
        with self.assertRaises(ValueError): generator.staging_path(ROOT / 'tools' / 'authoring')

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

    def test_invalid_glb_cannot_publish_completion(self):
        with self.assertRaises(ValueError): generator.extract_glb(b'glTF', self.folder)
        data = bytearray(glb()); struct.pack_into('<I', data, 12, len(data)*2)
        with self.assertRaises(ValueError): generator.extract_glb(bytes(data), self.folder)


if __name__ == '__main__':
    unittest.main()
