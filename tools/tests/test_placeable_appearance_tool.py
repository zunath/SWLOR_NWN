"""Checks for the station appearance installer on synthetic haks and module data.

Run with: python -m unittest discover -s tools/tests -p test_placeable_appearance_tool.py
"""
import contextlib
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('appearance_tool', ROOT / 'tools/PlaceableAppearanceTool.py')
tool = importlib.util.module_from_spec(spec)
spec.loader.exec_module(tool)

HEADER = '         Label                  StrRef      ModelName   LightColor  LightOffsetX LightOffsetY LightOffsetZ SoundAppType ShadowSize BodyBag LowGore Reflection Static\n'
# Labels skip and repeat on purpose: the engine indexes by physical position, as placeables.2da does.
ROWS = ['0 "Armoire" **** plc_a01 **** **** **** **** 13 1 0 **** **** 1',
        '1 "Bench" **** plc_a02 **** **** **** **** 13 1 0 **** **** 1',
        '5 "Gap" **** plc_a03 **** **** **** **** 13 1 0 **** **** 1',
        '6 "Old bench" **** pkt_mstation **** **** **** **** 13 1 0 **** **** 1']
CONFIG = {'schema_version': 1, 'stations': [
    {'blueprint': 'smithery_bench', 'model': 'swc_stn_forge', 'label': '[SWLOR] Station - Plasma Forge', 'sound_app_type': 13},
    {'blueprint': 'research_term', 'model': 'swc_stn_research', 'label': '[SWLOR] Station - Research Terminal', 'sound_app_type': None}]}


def instance(resref, appearance):
    lines = ['      {', '        "__struct_id": 9,', '        "Appearance": {', '          "type": "dword",',
             f'          "value": {appearance}', '        },', '        "Tag": {', '          "type": "cexostring",',
             f'          "value": "{resref}"', '        },', '        "TemplateResRef": {', '          "type": "resref",',
             f'          "value": "{resref}"', '        },', '        "VarTable": {', '          "type": "list",', '          "value": [',
             '            {', '              "__struct_id": 0,', '              "Name": "Appearance"', '            }', '          ]', '        }', '      }']
    return '\r\n'.join(lines)


def area(*entries):
    return ('{\r\n  "Placeable List": {\r\n    "type": "list",\r\n    "value": [\r\n' + ',\r\n'.join(entries) + '\r\n    ]\r\n  }\r\n}\r\n').encode()


class AppearanceToolTests(unittest.TestCase):
    def setUp(self):
        self.enterContext(contextlib.redirect_stdout(io.StringIO()))
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        (self.root / 'cfg.json').write_text(json.dumps(CONFIG))
        tool.CONFIG = self.root / 'cfg.json'
        (self.root / 'haks/sw_2da').mkdir(parents=True)
        (self.root / 'haks/sw_plc').mkdir()
        (self.root / 'haks/sw_2da/placeables.2da').write_text('2DA V2.0\n\n' + HEADER + '\n'.join(ROWS) + '\n', encoding='latin-1')
        (self.root / 'Module/utp').mkdir(parents=True)
        (self.root / 'Module/git').mkdir()
        for resref in ('smithery_bench', 'research_term'):
            (self.root / f'Module/utp/{resref}.utp.json').write_bytes(
                b'{\r\n  "Appearance": {\r\n    "type": "dword",\r\n    "value": 20105\r\n  },\r\n  "Tag": "' + resref.encode() + b'"\r\n}\r\n')
        self.package = self.root / 'package'
        self.package.mkdir()
        for name in ('swc_stn_forge.mdl', 'swc_stn_forge.pwk', 'stn_forge.mtr', 'stn_forge.dds'):
            (self.package / name).write_bytes(name.encode())
        self.haks = self.root / 'haks'

    def tearDown(self):
        self.temp.cleanup()

    def table(self):
        return (self.haks / 'sw_2da/placeables.2da').read_bytes()

    def test_appearance_id_is_the_physical_row_not_the_label(self):
        rows, label = tool.read_rows(self.haks / 'sw_2da/placeables.2da')
        self.assertEqual(rows[3], 'pkt_mstation')
        self.assertEqual(label, 6)
        self.assertEqual(tool.appearance_rows(self.haks)[0]['pkt_mstation'], 3)

    def test_install_appends_one_row_at_the_next_physical_index_and_never_edits_existing_rows(self):
        before = self.table()
        added = tool.install_haks(self.package, self.haks, ['smithery_bench'])
        self.assertEqual(added, {'smithery_bench': 4})
        after = self.table()
        self.assertTrue(after.startswith(before))
        new = after[len(before):].decode('latin-1')
        self.assertEqual(new.count('\n'), 1)
        self.assertTrue(new.startswith('7 '))
        self.assertIn('"[SWLOR] Station - Plasma Forge"', new)
        self.assertEqual(tool.appearance_rows(self.haks)[0]['swc_stn_forge'], 4)
        for name in ('swc_stn_forge.mdl', 'swc_stn_forge.pwk', 'stn_forge.mtr', 'stn_forge.dds'):
            self.assertTrue((self.haks / 'sw_plc' / name).is_file())

    def test_install_is_idempotent(self):
        tool.install_haks(self.package, self.haks, None)
        once = self.table()
        self.assertEqual(tool.install_haks(self.package, self.haks, None), {'smithery_bench': 4})
        self.assertEqual(self.table(), once)

    def test_install_needs_a_configured_model_in_the_package(self):
        (self.package / 'swc_stn_forge.mdl').unlink()
        with self.assertRaises(ValueError):
            tool.install_haks(self.package, self.haks, None)

    def test_swap_instances_edits_only_the_appearance_digits_of_the_named_station(self):
        data = area(instance('smithery_bench', 20105), instance('cookpot', 4892), instance('smithery_bench', 20247))
        updated, edits, already = tool.swap_instances(data, 'smithery_bench', 32094)
        self.assertEqual((edits, already), (2, 0))
        self.assertEqual(updated.count(b'32094'), 2)
        self.assertIn(b'4892', updated)
        # Only digits changed: removing them leaves identical bytes.
        strip = lambda b: b.replace(b'32094', b'').replace(b'20105', b'').replace(b'20247', b'')
        self.assertEqual(strip(updated), strip(data))
        self.assertEqual(updated.count(b'\r\n'), data.count(b'\r\n'))

    def test_swap_instances_counts_instances_that_are_already_current(self):
        data = area(instance('smithery_bench', 32094))
        updated, edits, already = tool.swap_instances(data, 'smithery_bench', 32094)
        self.assertEqual((edits, already, updated), (0, 1, data))

    def test_swap_instances_does_not_cross_into_the_previous_struct(self):
        data = area(instance('cookpot', 4892), instance('smithery_bench', 20105))
        updated, edits, _ = tool.swap_instances(data, 'smithery_bench', 32094)
        self.assertEqual(edits, 1)
        self.assertIn(b'4892', updated)

    def test_swap_module_updates_blueprint_and_every_area_then_check_passes(self):
        tool.install_haks(self.package, self.haks, ['smithery_bench'])
        (self.root / 'Module/git/a.git.json').write_bytes(area(instance('smithery_bench', 20105), instance('smithery_bench', 5530)))
        (self.root / 'Module/git/b.git.json').write_bytes(area(instance('cookpot', 4892)))
        report, problems = tool.swap_module(self.root, self.haks, ['smithery_bench'])
        self.assertEqual(report['smithery_bench']['instances_changed'], 2)
        self.assertEqual(report['smithery_bench']['area_files_changed'], 1)
        self.assertIn(b'"value": 4\r\n', (self.root / 'Module/utp/smithery_bench.utp.json').read_bytes())
        self.assertEqual(problems, [])
        _, problems = tool.swap_module(self.root, self.haks, ['smithery_bench'], check=True)
        self.assertEqual(problems, [])
        (self.root / 'Module/git/a.git.json').write_bytes(area(instance('smithery_bench', 20105)))
        _, problems = tool.swap_module(self.root, self.haks, ['smithery_bench'], check=True)
        self.assertEqual(len(problems), 1)

    def test_swap_reports_a_station_whose_model_has_no_row(self):
        _, problems = tool.swap_module(self.root, self.haks, ['research_term'])
        self.assertEqual(len(problems), 1)

    def test_unknown_station_is_rejected(self):
        with self.assertRaises(ValueError):
            tool.load_stations(['nope'])


if __name__ == '__main__':
    unittest.main()
