"""The crafting stations must point at their own models everywhere the game reads an appearance.

Needs the initialized SWLOR_Haks submodule (sw_2da, sw_plc, sw_plc_* folders for the walkmesh engine-name check).
Run with: python -m unittest discover -s tools/tests -p test_station_models.py
"""
import glob
import importlib.util
import json
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('appearance_tool', ROOT / 'tools/PlaceableAppearanceTool.py')
tool = importlib.util.module_from_spec(spec)
spec.loader.exec_module(tool)
spec = importlib.util.spec_from_file_location('placeable_export', ROOT / 'tools/TrellisPlaceableExport.py')
exporter = importlib.util.module_from_spec(spec)
spec.loader.exec_module(exporter)

HAKS = ROOT / 'SWLOR_Haks'
PLC = HAKS / 'sw_plc'
STATIONS = json.loads((ROOT / 'tools/StationModels.json').read_text(encoding='utf-8'))['stations']
STRUCTURES = (ROOT / 'SWLOR.Game.Server/Service/PropertyService/StructureType.cs').read_text(encoding='utf-8')


def appearance(path):
    return int(re.search(rb'"Appearance":\s*\{\s*"type":\s*"dword",\s*"value":\s*(\d+)', Path(path).read_bytes())[1])


@unittest.skipUnless((HAKS / 'sw_2da/placeables.2da').is_file() and PLC.is_dir(), 'SWLOR_Haks sw_2da and sw_plc must be checked out')
class StationModelTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows = tool.appearance_rows(HAKS)[0]

    def test_every_station_has_a_row_for_its_model_at_its_physical_position(self):
        for station in STATIONS:
            with self.subTest(station['blueprint']):
                self.assertIn(station['model'], self.rows)
                lines = [l for l in (HAKS / 'sw_2da/placeables.2da').read_text(encoding='latin-1').split('\n')[3:] if l.strip()]
                tokens = re.findall(r'"[^"]*"|\S+', lines[self.rows[station['model']]])
                self.assertEqual(tokens[3], station['model'])
                self.assertEqual(tokens[1].strip('"'), station['label'])

    def test_blueprint_resref_and_structure_mapping_are_unchanged(self):
        for station in STATIONS:
            with self.subTest(station['blueprint']):
                blueprint = json.loads((ROOT / 'Module/utp' / f"{station['blueprint']}.utp.json").read_bytes().decode('latin-1'))
                self.assertEqual(blueprint['TemplateResRef']['value'], station['blueprint'])
                self.assertEqual(blueprint['Tag']['value'], station['blueprint'])
                if station['structure_type']:
                    self.assertRegex(STRUCTURES, rf'\[Structure\("[^"]*",\s*"{station["blueprint"]}",[^\]]*\]\s*{station["structure_type"]} = \d+')

    def test_blueprint_and_every_placed_instance_use_the_station_row(self):
        for station in STATIONS:
            row = self.rows[station['model']]
            with self.subTest(station['blueprint']):
                self.assertEqual(appearance(ROOT / 'Module/utp' / f"{station['blueprint']}.utp.json"), row)
                pattern = re.compile(rb'"TemplateResRef":\s*\{\s*"type":\s*"resref",\s*"value":\s*"' + station['blueprint'].encode() + rb'"')
                placed = 0
                for path in glob.glob(str(ROOT / 'Module/git/*.git.json')):
                    data = Path(path).read_bytes()
                    for match in pattern.finditer(data):
                        start = data.rfind(b'"Appearance"', 0, match.start())
                        value = int(re.match(rb'"Appearance":\s*\{\s*"type":\s*"dword",\s*"value":\s*(\d+)', data[start:start + 120])[1])
                        self.assertEqual(value, row, f'{Path(path).name}: placed {station["blueprint"]}')
                        placed += 1
                # The Research terminal is only ever placed by players in labs; every other station is placed in areas.
                self.assertGreaterEqual(placed, 0 if station['blueprint'] == 'research_term' else 1, 'placed instances scanned')

    def test_model_resources_exist_and_the_material_is_complete(self):
        for station in STATIONS:
            model = station['model']
            with self.subTest(model):
                for extension in ('mdl', 'pwk'):
                    self.assertTrue((PLC / f'{model}.{extension}').is_file(), extension)
                data = (PLC / f'{model}.mdl').read_bytes()
                self.assertEqual(data[:4], bytes(4), 'models ship compiled')
                mtrs = [p for p in PLC.glob('stn_*.mtr') if p.stem.encode() in data]
                self.assertTrue(mtrs, 'the model must name a stn_*.mtr material')
                for mtr in mtrs:
                    for name in re.findall(r'^texture\d+\s+(\S+)', mtr.read_text(), re.M):
                        if name.lower() != 'null':
                            self.assertTrue((PLC / f'{name}.dds').is_file(), f'{mtr.name} -> {name}.dds')
                            self.assertFalse((PLC / f'{name}.tga').exists(), 'a TGA would shadow the DDS')

    def test_walkmesh_is_a_flat_nonwalkable_footprint_with_engine_readable_use_nodes(self):
        for station in STATIONS:
            model = station['model']
            with self.subTest(model):
                info = exporter.validate_pwk(PLC / f'{model}.pwk', model)
                self.assertGreaterEqual(info['vertices'], 3)
                self.assertLessEqual(len(info['use_nodes']), 2)
                for name, x, y in info['use_nodes']:
                    self.assertRegex(name, r'^.{4}pwk_use0[12]$')
                    self.assertTrue(x != 0 or y != 0)

    def test_station_resource_names_are_unique_across_the_haks(self):
        names = [Path(p).stem.lower() for p in glob.glob(str(HAKS / 'sw_*/*.mdl'))]
        for station in STATIONS:
            self.assertEqual(names.count(station['model']), 1, station['model'])


if __name__ == '__main__':
    unittest.main()
