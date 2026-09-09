import copy
import json
import os
import tempfile
import unittest
import zipfile
from xml.etree import ElementTree as ET
from pathlib import Path
from unittest.mock import patch

from UpdateAnimationBible import synchronize, synchronize_files, replace_outputs, conditional_replace


class AnimationBibleTests(unittest.TestCase):
    def test_explicit_native_perk_stays_out_of_custom_animation_rows(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            workbook, manifest, registry, provenance, plan = [root / name for name in
                ("bible.xlsx", "manifest.json", "registry.json", "provenance.json", "plan.csv")]
            workbook.write_bytes(b"original workbook")
            manifest.write_text(json.dumps([{"Id": "Push", "InternalName": "sw_push", "BibleAnimationRow": 2}]))
            registry.write_text("[]")
            provenance.write_text(json.dumps({"Animations": [{"Id": "Push"}]}))
            plan.write_text("PerkId,Status,BibleAnimationRow,InternalName\nPush,Needed,2,sw_push\nStealth,Native,99,sw_stealth\n")
            with patch("UpdateAnimationBible.render_workbook", return_value=b"new workbook") as render:
                synchronize_files(manifest, workbook, registry, provenance, plan)
            self.assertEqual([e["Id"] for e in render.call_args.args[1]], ["Push"])
            self.assertIn("Stealth,Native,,", plan.read_text())
            self.assertNotIn("sw_stealth", plan.read_text())

    def test_edits_after_render_to_any_captured_input_abort_without_overwrite(self):
        for changed_name in ("bible.xlsx", "manifest.json", "plan.csv", "registry.json", "provenance.json"):
            with self.subTest(changed_name=changed_name), tempfile.TemporaryDirectory() as folder:
                root = Path(folder)
                workbook, manifest, registry, provenance, plan = [root / name for name in
                    ("bible.xlsx", "manifest.json", "registry.json", "provenance.json", "plan.csv")]
                workbook.write_bytes(b"captured workbook")
                manifest.write_text(json.dumps([{"Id": "Push", "InternalName": "sw_push", "BibleAnimationRow": 2}]))
                registry.write_text("[]")
                provenance.write_text(json.dumps({"Animations": [{"Id": "Push"}]}))
                plan.write_text("PerkId,Status,BibleAnimationRow\nPush,Needed,2\n")
                originals = {path: path.read_bytes() for path in (workbook, manifest, registry, provenance, plan)}

                def render(path, entries, records, captured):
                    self.assertEqual(captured, originals[workbook])
                    (root / changed_name).write_bytes(b"external edit")
                    return b"rendered workbook"

                with patch("UpdateAnimationBible.render_workbook", side_effect=render):
                    with self.assertRaisesRegex(OSError, "Concurrent edit"):
                        synchronize_files(manifest, workbook, registry, provenance, plan)
                for path, payload in originals.items():
                    self.assertEqual(path.read_bytes(), b"external edit" if path.name == changed_name else payload)
                self.assertEqual(list(root.glob(".animation-*")), [])

    def test_partial_publish_preserves_external_edit_and_restores_other_outputs(self):
        with tempfile.TemporaryDirectory() as folder:
            paths = [Path(folder) / name for name in ("first", "second", "third")]
            for path in paths:
                path.write_bytes(b"original")
            calls = 0

            def race(path, staged, expected):
                nonlocal calls
                calls += 1
                if calls == 3:
                    paths[0].write_bytes(b"external first")
                    raise PermissionError("third locked")
                return conditional_replace(path, staged, expected)

            with patch("UpdateAnimationBible.conditional_replace", side_effect=race):
                with self.assertRaises(ExceptionGroup):
                    replace_outputs({path: b"published" for path in paths})
            self.assertEqual(paths[0].read_bytes(), b"external first")
            self.assertEqual(paths[1].read_bytes(), b"original")
            self.assertEqual(paths[2].read_bytes(), b"original")
            retained = list(Path(folder).glob(".animation-sync-*"))
            self.assertEqual(len(retained), 1)
            self.assertEqual(retained[0].read_bytes(), b"original")

    def test_atomic_capture_detects_edit_after_prepublication_check(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "bible.xlsx"
            path.write_bytes(b"original")
            native_rename = os.rename

            def race(source, destination):
                Path(source).write_bytes(b"racing writer")
                return native_rename(source, destination)

            with patch("UpdateAnimationBible.os.rename", side_effect=race):
                with self.assertRaisesRegex(OSError, "Concurrent edit"):
                    replace_outputs({path: b"published"}, {path: b"original"})
            self.assertEqual(path.read_bytes(), b"racing writer")
            self.assertEqual(list(Path(folder).glob(".animation-*")), [])

    def test_stale_plan_validation_does_not_write_workbook_or_manifest(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            workbook, manifest, registry, provenance, plan = [root / name for name in
                ("bible.xlsx", "manifest.json", "registry.json", "provenance.json", "plan.csv")]
            workbook.write_bytes(b"original workbook")
            manifest.write_text(json.dumps([{"Id": "Push", "InternalName": "sw_push", "BibleAnimationRow": 2}]))
            registry.write_text("[]")
            provenance.write_text(json.dumps({"Animations": [{"Id": "Push"}]}))
            plan.write_text("PerkId,Status,BibleAnimationRow\nDeletedPerk,Needed,\n")
            originals = {path: path.read_bytes() for path in (workbook, manifest, plan)}
            with patch("UpdateAnimationBible.render_workbook", return_value=b"new workbook"):
                with self.assertRaises(KeyError):
                    synchronize_files(manifest, workbook, registry, provenance, plan)
            for path, payload in originals.items():
                self.assertEqual(path.read_bytes(), payload)
            self.assertEqual(list(root.glob(".animation-sync-*")), [])

    def test_failed_second_replacement_restores_all_original_outputs(self):
        with tempfile.TemporaryDirectory() as folder:
            paths = [Path(folder) / name for name in ("bible.xlsx", "manifest.json", "plan.csv")]
            for index, path in enumerate(paths):
                path.write_bytes(f"original {index}".encode())
            originals = {path: path.read_bytes() for path in paths}
            native_replace = conditional_replace
            calls = 0

            def fail_second(path, staged, expected):
                nonlocal calls
                calls += 1
                if calls == 2:
                    raise PermissionError("simulated locked manifest")
                return native_replace(path, staged, expected)

            with patch("UpdateAnimationBible.conditional_replace", side_effect=fail_second):
                with self.assertRaisesRegex(PermissionError, "locked manifest"):
                    replace_outputs({path: b"updated" for path in paths})
            for path, payload in originals.items():
                self.assertEqual(path.read_bytes(), payload)
            self.assertEqual(list(Path(folder).glob(".animation-sync-*")), [])

    def test_successful_replacement_commits_all_staged_outputs(self):
        with tempfile.TemporaryDirectory() as folder:
            paths = [Path(folder) / name for name in ("bible.xlsx", "manifest.json", "plan.csv")]
            for path in paths:
                path.write_bytes(b"original")
            replace_outputs({path: b"updated" for path in paths})
            self.assertTrue(all(path.read_bytes() == b"updated" for path in paths))
            self.assertEqual(list(Path(folder).glob(".animation-sync-*")), [])

    def test_preserves_other_sheets_and_formula_caches_and_existing_rows(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "bible.xlsx"
            files = {
                "xl/workbook.xml": '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Animations" sheetId="2" r:id="rId2"/></sheets></workbook>',
                "xl/_rels/workbook.xml.rels": '<Relationships><Relationship Id="rId2" Target="worksheets/sheet2.xml"/></Relationships>',
                "xl/worksheets/sheet1.xml": '<worksheet><c r="A1"><f>1+2</f><v>3</v></c></worksheet>',
                "xl/worksheets/sheet2.xml": '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><dimension ref="A1:I2"/><sheetData><row r="1"><c r="C1" s="17" t="inlineStr"><is><t>Name</t></is></c><c r="D1" s="17"/><c r="Z1" s="8"/><c r="F1" s="17" t="inlineStr"><is><t>Old Internal Name</t></is></c></row><row r="2"><c r="A2" s="18"/><c r="B2" s="18" t="inlineStr"><is><t>Force</t></is></c><c r="C2" s="18" t="inlineStr"><is><t>Push</t></is></c><c r="D2" s="4"/><c r="E2" s="31"/><c r="F2" s="4"/><c r="Z2" t="inlineStr"><is><t>Retained after blank</t></is></c></row></sheetData></worksheet>',
            }
            with zipfile.ZipFile(path, "w") as z:
                for name, value in files.items():
                    z.writestr(name, value)
            entries = [{"Id": "Push", "Name": "Push", "Category": "Force", "InternalName": "sw_push"},
                       {"Id": "Pull", "Name": "Pull", "Category": "Force", "InternalName": "sw_pull"}]
            registry = [{"Name": e["Id"], "AnimationName": e["InternalName"], "ProjectPath": e["Id"] + ".swlanim"} for e in entries]
            synchronize(path, entries, registry)
            with zipfile.ZipFile(path) as z:
                for name, value in files.items():
                    if name != "xl/worksheets/sheet2.xml":
                        self.assertEqual(z.read(name), value.encode())
                first = z.read("xl/worksheets/sheet2.xml")
                self.assertIn(b'F2', first)
                self.assertIn(b'sw_push', first)
                self.assertIn(b'sw_pull', first)
                ns = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
                sheet = ET.fromstring(first)
                for row in sheet.findall("s:sheetData/s:row", ns):
                    refs = [c.get("r") for c in row]
                    columns = [''.join(ch for ch in ref if ch.isalpha()) for ref in refs]
                    self.assertEqual(columns, sorted(columns, key=lambda c: (len(c), c)))
                    self.assertEqual(len(refs), len(set(refs)))
                self.assertEqual(sheet.find('.//s:c[@r="Z2"]/s:is/s:t', ns).text, "Retained after blank")
                self.assertEqual(sheet.find("s:dimension", ns).get("ref"), "A1:Z3")
                for column, style in {"A": "18", "B": "18", "C": "18", "D": "4", "E": "31"}.items():
                    self.assertEqual(sheet.find(f'.//s:c[@r="{column}3"]', ns).get("s"), style)
            self.assertEqual([e["BibleAnimationRow"] for e in entries], [2, 3])
            synchronize(path, copy.deepcopy(entries), registry)
            with zipfile.ZipFile(path) as z:
                self.assertEqual(z.read("xl/worksheets/sheet2.xml"), first)

    def test_prefixed_sheet_data_is_updated_and_existing_used_range_is_retained(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "bible.xlsx"
            files = {
                "xl/workbook.xml": '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Animations" r:id="rId2"/></sheets></workbook>',
                "xl/_rels/workbook.xml.rels": '<Relationships><Relationship Id="rId2" Target="worksheets/sheet2.xml"/></Relationships>',
                "xl/worksheets/sheet2.xml": '<s:worksheet xmlns:s="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><s:dimension ref="A1:AA10"/><s:sheetData><s:row r="1"><s:c r="C1" t="inlineStr"><s:is><s:t>Name</s:t></s:is></s:c><s:c r="Z1" t="inlineStr"><s:is><s:t>Keep</s:t></s:is></s:c></s:row></s:sheetData><s:autoFilter ref="A1:E1"/></s:worksheet>',
            }
            with zipfile.ZipFile(path, "w") as z:
                for name, value in files.items():
                    z.writestr(name, value)
            entries = [{"Id": "Push", "Name": "Push", "Category": "Force", "InternalName": "sw_push"}]
            registry = [{"Name": "Push", "AnimationName": "sw_push", "ProjectPath": "Push.swlanim"}]
            synchronize(path, entries, registry)
            ns = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
            with zipfile.ZipFile(path) as z:
                sheet = ET.fromstring(z.read("xl/worksheets/sheet2.xml"))
            self.assertEqual(sheet.find('.//s:c[@r="F2"]/s:is/s:t', ns).text, "sw_push")
            self.assertEqual(sheet.find('.//s:c[@r="Z1"]/s:is/s:t', ns).text, "Keep")
            self.assertEqual(sheet.find('s:dimension', ns).get("ref"), "A1:AA10")
            self.assertEqual(sheet.find('s:autoFilter', ns).get("ref"), "A1:I2")
            # A second pass reads the locally default-namespaced sheetData under a prefixed root.
            synchronize(path, copy.deepcopy(entries), registry)

    def test_locally_declared_prefix_on_empty_sheet_data_remains_bound(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "bible.xlsx"
            with zipfile.ZipFile(path, "w") as z:
                z.writestr("xl/workbook.xml", '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Animations" r:id="rId2"/></sheets></workbook>')
                z.writestr("xl/_rels/workbook.xml.rels", '<Relationships><Relationship Id="rId2" Target="worksheets/sheet2.xml"/></Relationships>')
                z.writestr("xl/worksheets/sheet2.xml", '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><s:sheetData xmlns:s="http://schemas.openxmlformats.org/spreadsheetml/2006/main"/></worksheet>')
            entries = [{"Id": "Push", "Name": "Push", "Category": "Force", "InternalName": "sw_push"}]
            registry = [{"Name": "Push", "AnimationName": "sw_push", "ProjectPath": "Push.swlanim"}]
            synchronize(path, entries, registry)
            with zipfile.ZipFile(path) as z:
                sheet = ET.fromstring(z.read("xl/worksheets/sheet2.xml"))
            ns = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
            self.assertEqual(sheet.find('.//s:c[@r="F2"]/s:is/s:t', ns).text, "sw_push")

    def test_notes_and_formatting_rows_survive_and_reserve_append_positions(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "bible.xlsx"
            worksheet = '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><dimension ref="A1:AC12"/><sheetData><row r="1"><c r="C1" t="inlineStr"><is><t>Name</t></is></c></row><row r="2" ht="30" customHeight="1"><c r="A2" s="8" t="inlineStr"><is><t>Animator note</t></is></c><c r="Z2" s="9" t="inlineStr"><is><t>Keep side note</t></is></c></row><row r="3"><c r="A3" s="18"/><c r="B3" t="inlineStr"><is><t>Force</t></is></c><c r="C3" t="inlineStr"><is><t>Push</t></is></c></row><row r="5" hidden="1" s="12" customFormat="1"><c r="AA5" s="7"/><extLst><ext uri="urn:animator-note"><note xmlns="urn:animator">Keep metadata</note></ext></extLst></row><row r="8" ht="24" customHeight="1"/><row r="12"><c r="AC12" s="10" t="inlineStr"><is><t>Footer notes</t></is></c></row></sheetData></worksheet>'
            with zipfile.ZipFile(path, "w") as z:
                z.writestr("xl/workbook.xml", '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Animations" r:id="rId2"/></sheets></workbook>')
                z.writestr("xl/_rels/workbook.xml.rels", '<Relationships><Relationship Id="rId2" Target="worksheets/sheet2.xml"/></Relationships>')
                z.writestr("xl/worksheets/sheet2.xml", worksheet)
            entries = [{"Id": name, "Name": name, "Category": "Force", "InternalName": "sw_" + name.lower()} for name in ("Push", "Pull")]
            registry = [{"Name": e["Id"], "AnimationName": e["InternalName"], "ProjectPath": e["Id"] + ".swlanim"} for e in entries]
            ns = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
            original = ET.fromstring(worksheet)
            synchronize(path, entries, registry)
            with zipfile.ZipFile(path) as z:
                first = z.read("xl/worksheets/sheet2.xml")
            actual = ET.fromstring(first)
            for number in (2, 5, 8, 12):
                selector = f's:sheetData/s:row[@r="{number}"]'
                self.assertEqual(ET.tostring(actual.find(selector, ns)), ET.tostring(original.find(selector, ns)))
            self.assertEqual([e["BibleAnimationRow"] for e in entries], [3, 13])
            numbers = [int(row.get("r")) for row in actual.findall('s:sheetData/s:row', ns)]
            self.assertEqual(numbers, [1, 2, 3, 5, 8, 12, 13])
            self.assertEqual(actual.find('s:dimension', ns).get("ref"), "A1:AC13")
            self.assertEqual(actual.find('.//s:c[@r="A13"]', ns).get("s"), "18")
            self.assertEqual(actual.find('.//s:c[@r="A3"]', ns).get("s"), "18")
            synchronize(path, copy.deepcopy(entries), registry)
            with zipfile.ZipFile(path) as z:
                self.assertEqual(z.read("xl/worksheets/sheet2.xml"), first)

    def test_missing_sheet_data_fails_before_writing(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "bible.xlsx"
            with zipfile.ZipFile(path, "w") as z:
                z.writestr("xl/workbook.xml", '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Animations" r:id="rId2"/></sheets></workbook>')
                z.writestr("xl/_rels/workbook.xml.rels", '<Relationships><Relationship Id="rId2" Target="worksheets/sheet2.xml"/></Relationships>')
                z.writestr("xl/worksheets/sheet2.xml", '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"/>')
            original = path.read_bytes()
            with self.assertRaisesRegex(ValueError, "exactly one"):
                synchronize(path, [], [])
            self.assertEqual(path.read_bytes(), original)

    def test_rejects_uninstalled_names_before_writing(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "bible.xlsx"
            path.write_bytes(b"untouched")
            with self.assertRaisesRegex(ValueError, "not installed"):
                synchronize(path, [{"Id": "Push", "InternalName": "sw_push"}], [])
            self.assertEqual(path.read_bytes(), b"untouched")


if __name__ == "__main__":
    unittest.main()
