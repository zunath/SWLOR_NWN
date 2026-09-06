import sys
from pathlib import Path
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from audit_npc_portraits import audit, missing_sizes


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


if __name__ == '__main__':
    unittest.main()
