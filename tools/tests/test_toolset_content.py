import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

SPEC = importlib.util.spec_from_file_location('content', Path(__file__).parents[1] / 'BuildToolsetContent.py')
content = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(content)

class ToolsetContentTests(unittest.TestCase):
    def test_archive_parts_reassemble_with_per_part_and_combined_checksums(self):
        with tempfile.TemporaryDirectory() as directory:
            archive = Path(directory) / 'assets.zip'
            payload = bytes(range(256)) * 20
            archive.write_bytes(payload)
            original_limit = content.PART_BYTES
            content.PART_BYTES = 1024
            try:
                descriptor = content.split_archive(archive, 'https://example.com/release')
            finally:
                content.PART_BYTES = original_limit
            parts = sorted(Path(directory).glob('assets.zip.*'))
            self.assertEqual(payload, b''.join(path.read_bytes() for path in parts))
            self.assertFalse(archive.exists())
            for path, part in zip(parts, descriptor['Parts']):
                self.assertEqual(path.stat().st_size, part['Length'])
                self.assertEqual(content.digest(path), part['Sha256'])

    def test_snapshot_path_policy_excludes_deployments_and_transaction_debris(self):
        for path in ['Module/utc/test.utc.json', 'Module/ncs/test.ncs', 'SWLOR.Game.Server/ConversationData/test.conversation.json', 'toolset/categories.json']:
            self.assertIsNotNone(content.CONTENT.fullmatch(path))
        for path in ['Module/utc/test.backup.utc.json', 'Module/packing/test.utc', 'SWLOR.Game.Server/Service/Combat.cs', 'Module/../outside.json']:
            self.assertIsNone(content.CONTENT.fullmatch(path))


    def test_real_module_ifo_with_raw_void_bytes_can_be_read(self):
        root = Path(__file__).resolve().parents[2]
        original = (root / 'Module/ifo/module.ifo.json').read_bytes()
        self.assertIn(b'\xff', original)
        self.assertIn('sw_2da', content.assigned_haks(root))

    def test_asset_bundle_contains_exact_assigned_haks_and_both_tlk_forms(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'Module/ifo').mkdir(parents=True)
            (root / 'Module/ifo/module.ifo.json').write_text(json.dumps({'Mod_HakList': {'value': [{'Mod_Hak': {'value': 'sw_test'}}]}}))
            (root / 'SWLOR_Haks/sw_tlk').mkdir(parents=True)
            (root / 'SWLOR_Haks/sw_tlk/sw_tlk.tlk.json').write_text('{"language":0,"entries":[]}')
            packed = root / 'packed'
            (packed / 'hak').mkdir(parents=True)
            (packed / 'tlk').mkdir()
            (packed / 'hak/sw_test.hak').write_bytes(b'archive')
            (packed / 'tlk/sw_tlk.tlk').write_bytes(b'tlk')
            output = root / 'output'
            output.mkdir()
            with patch.object(content, 'revisions', return_value=('1' * 40, '2' * 40)):
                content.asset_pack(root, output, packed, 'https://example.com/release')
            descriptor = json.loads((output / 'asset-pack.json').read_text())
            self.assertEqual('2' * 40, descriptor['AssetRevision'])
            archive = output / 'assets.zip.001'
            with zipfile.ZipFile(archive) as bundle:
                self.assertEqual({'hak/sw_test.hak', 'tlk/sw_tlk.tlk', 'tlk/sw_tlk.tlk.json', 'asset-manifest.json'}, set(bundle.namelist()))
                manifest = json.loads(bundle.read('asset-manifest.json'))
                self.assertEqual(content.digest(packed / 'hak/sw_test.hak'), manifest['Files']['hak/sw_test.hak'])

    def test_workspace_bundle_keeps_original_bytes_and_rejects_mismatched_assets(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / 'Module/utc/example.utc.json'
            source.parent.mkdir(parents=True)
            original = b'{ }\r\n'
            source.write_bytes(original)
            catalog = root / 'catalog.json'
            catalog.write_text(json.dumps({'ContentRevision': '1' * 40}))
            output = root / 'output'
            output.mkdir()
            assets = {'AssetRevision': '2' * 40, 'Archive': {'Sha256': 'A' * 64, 'Parts': []}}
            with patch.object(content, 'revisions', return_value=('1' * 40, '2' * 40)), patch.object(content, 'git', return_value='Module/utc/example.utc.json'):
                content.workspace(root, output, catalog, assets, 'https://example.com/release')
            with zipfile.ZipFile(output / 'workspace.zip.001') as bundle:
                self.assertEqual(original, bundle.read('Module/utc/example.utc.json'))
                manifest = json.loads(bundle.read('swlor-workspace.json'))
                self.assertEqual(content.digest(source), manifest['Baseline']['Module/utc/example.utc.json'])
            with patch.object(content, 'revisions', return_value=('1' * 40, '3' * 40)):
                with self.assertRaisesRegex(ValueError, 'do not match'):
                    content.workspace(root, output, catalog, assets, 'https://example.com/release')

if __name__ == '__main__':
    unittest.main()
