import sys
from pathlib import Path
import unittest
import tempfile
import json

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from audit_npc_portraits import audit, missing_sizes, read_json, resources


class NpcPortraitTests(unittest.TestCase):
    def test_complete_npc_corpus(self):
        count, failures = audit()
        self.assertGreater(count, 2000, 'A partial module checkout must not pass')
        self.assertEqual(failures, [])

    def test_placeable_portrait_without_large_is_rejected(self):
        self.assertEqual(missing_sizes({'PortraitId': {'value': 376}},
                                      {376: 'po_plc_b09_'},
                                      {'po_plc_b09_' + s for s in 'mst'}), ['l'])

    def test_explicit_resref_overrides_numeric_id(self):
        self.assertEqual(missing_sizes({'Portrait': {'value': 'PO_CUSTOM_'},
                                        'PortraitId': {'value': 1}},
                                       {1: 'po_valid_'},
                                       {'po_valid_' + s for s in 'lmst'}), list('lmst'))

    def test_blank_and_unknown_ids_are_rejected(self):
        for creature in ({}, {'PortraitId': {'value': 65535}}):
            self.assertEqual(missing_sizes(creature, {}, set()), list('lmst'))

    def test_legacy_description_encoding_does_not_break_linux_audits(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'legacy.utc.json'
            for encoding in ('utf-8-sig', 'cp1252'):
                path.write_bytes('{"Description": "It’s a creature", "PortraitId": {"value": 1}}'.encode(encoding))
                self.assertEqual(read_json(path)['PortraitId']['value'], 1)

    def test_unloaded_hak_and_staging_resources_cannot_validate_a_portrait(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for folder in ('tools/data', 'Module/ifo', 'SWLOR_Haks/sw_portrait',
                           'SWLOR_Haks/unused', 'SWLOR_Haks/output'):
                (root / folder).mkdir(parents=True)
            (root / 'tools/data/nwn_stock_portraits.txt').write_text('po_stock_l\n')
            (root / 'SWLOR_Haks/hakbuilder.json').write_text(json.dumps({'HakList': [
                {'Name': 'sw_portrait', 'Path': './sw_portrait'},
                {'Name': 'unused', 'Path': './unused'}]}))
            (root / 'Module/ifo/module.ifo.json').write_text(json.dumps({
                'Mod_HakList': {'value': [{'Mod_Hak': {'value': 'sw_portrait'}}]}}))
            for folder in ('sw_portrait', 'unused', 'output'):
                (root / 'SWLOR_Haks' / folder / f'{folder}.dds').touch()
            self.assertEqual(resources(root), {'po_stock_l', 'sw_portrait'})


if __name__ == '__main__':
    unittest.main()
