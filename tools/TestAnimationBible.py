import copy
import json
import os
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch

from UpdateAnimationBible import synchronize, synchronize_files, replace_outputs


class AnimationBibleTests(unittest.TestCase):
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
            native_replace = os.replace
            calls = 0

            def fail_second(source, target):
                nonlocal calls
                calls += 1
                if calls == 2:
                    raise PermissionError("simulated locked manifest")
                return native_replace(source, target)

            with patch("UpdateAnimationBible.os.replace", side_effect=fail_second):
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
                "xl/worksheets/sheet2.xml": '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1"><c r="C1" t="inlineStr"><is><t>Name</t></is></c></row><row r="2"><c r="B2" t="inlineStr"><is><t>Force</t></is></c><c r="C2" t="inlineStr"><is><t>Push</t></is></c></row></sheetData></worksheet>',
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
            self.assertEqual([e["BibleAnimationRow"] for e in entries], [2, 3])
            synchronize(path, copy.deepcopy(entries), registry)
            with zipfile.ZipFile(path) as z:
                self.assertEqual(z.read("xl/worksheets/sheet2.xml"), first)

    def test_rejects_uninstalled_names_before_writing(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "bible.xlsx"
            path.write_bytes(b"untouched")
            with self.assertRaisesRegex(ValueError, "not installed"):
                synchronize(path, [{"Id": "Push", "InternalName": "sw_push"}], [])
            self.assertEqual(path.read_bytes(), b"untouched")


if __name__ == "__main__":
    unittest.main()
