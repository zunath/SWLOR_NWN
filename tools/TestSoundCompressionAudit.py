#!/usr/bin/env python3
"""Focused regression checks for configuration-driven sound loop exclusions."""

import json
import hashlib
import pathlib
import tempfile
import unittest

import AuditSoundCompression as audit


def sound(resref, looping=1, continuous=0):
    return {
        "Looping": {"type": "byte", "value": looping},
        "Continuous": {"type": "byte", "value": continuous},
        "Sounds": {"type": "list", "value": [{"Sound": {"type": "resref", "value": resref}}]},
    }


class SoundCompressionAuditTests(unittest.TestCase):
    def test_historical_conversion_allows_only_verified_original_restoration(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            (root / "sw_sound").mkdir()
            path = root / "sw_sound/hum.wav"
            original = b"original PCM sound"
            manifest = {"schema_version": 1, "files": [{
                "path": "sw_sound/hum.wav", "status": "converted",
                "source_sha256": hashlib.sha256(original).hexdigest(),
            }]}
            path.write_bytes(b"converted or edited sound")
            self.assertEqual(len(audit.check_manifest(manifest, {"hum": ["new loop"]}, root)), 1)
            path.write_bytes(original)
            self.assertEqual(audit.check_manifest(manifest, {"hum": ["new loop"]}, root), [])
            manifest["files"][0]["path"] = "../hum.wav"
            with self.assertRaisesRegex(ValueError, "outside the HAK"):
                audit.check_manifest(manifest, {"hum": ["new loop"]}, root)

    def test_nested_placed_loops_are_case_insensitive_but_interval_ambience_is_allowed(self):
        placed = {"SoundList": {"value": [sound("SpeederSound"), sound("crowd", 0, 1)]}}
        pairs = list(audit.sound_object_loops(placed, "Module/git/test.git.json"))
        self.assertEqual([resref for resref, _ in pairs], ["speedersound"])
        self.assertIn("SoundList.value[0].Looping=1", pairs[0][1])

    def test_malformed_loop_does_not_silently_drop_its_exclusion(self):
        with self.assertRaisesRegex(ValueError, "not a sound list"):
            list(audit.sound_object_loops({"Looping": {"value": 1}}, "broken.uts.json"))

    def test_2da_quotes_default_and_resource_column_are_respected(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = pathlib.Path(temporary) / "appearancesndset.2da"
            path.write_text('2DA V2.0\n\nDEFAULT: ****\n\nLabel Looping FallFwd\n'
                            '0 "Wasp creature" fs_Wasp1 fall\n1 Other **** impact\n', encoding="utf-8")
            pairs = list(audit.table_loops(path, "Looping", "appearancesndset.2da"))
            self.assertEqual(pairs, [("fs_wasp1", "appearancesndset.2da:6: row 0 Looping=fs_Wasp1")])
            with self.assertRaisesRegex(ValueError, "missing required"):
                list(audit.table_loops(path, "SoundDuration", "appearancesndset.2da"))

    def test_current_configuration_is_checked_instead_of_saved_exclusions(self):
        manifest = {"schema_version": 1, "files": [
            {"path": "sw_sound/Shot.wav", "status": "converted"},
            {"path": "sw_sound/hum.wav", "status": "skipped"},
        ]}
        self.assertEqual(audit.check_manifest(manifest, {"hum": ["original loop"]}), [])
        violations = audit.check_manifest(manifest, {"shot": ["new area loop"], "hum": ["original loop"]})
        self.assertEqual(violations, ["sw_sound/Shot.wav: new area loop"])
        manifest["files"][0]["status"] = "would_convert"
        self.assertEqual(audit.check_manifest(manifest, {"shot": ["new area loop"]}), violations)

    def test_manifest_rejects_unknown_status_and_empty_inventory(self):
        with self.assertRaisesRegex(ValueError, "Unknown conversion status"):
            audit.check_manifest({"schema_version": 1, "files": [{"path": "a.wav", "status": "typo"}]}, {})
        with self.assertRaisesRegex(ValueError, "no file inventory"):
            audit.check_manifest({"schema_version": 1, "files": []}, {})

    def test_corpus_scan_includes_uts_git_and_each_explicit_2da_loop_column(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            for folder in ("git", "uts"):
                directory = root / "Module" / folder
                directory.mkdir(parents=True)
                document = sound(folder)
                if folder == "git":
                    document = {"SoundList": {"value": [document]}}
                (directory / f"test.{folder}.json").write_text(json.dumps(document), encoding="utf-8")
            tables = root / "SWLOR_Haks" / "sw_2da"
            tables.mkdir(parents=True)
            for filename, column in audit.LOOP_COLUMNS.items():
                (tables / filename).write_text(f'2DA V2.0\n\nLabel {column}\n0 Sound {filename[:-4]}\n', encoding="utf-8")
            evidence = audit.collect_exclusions(root, root / "SWLOR_Haks")
            self.assertEqual(set(evidence), {"git", "uts", "ambientsound", "appearancesndset", "visualeffects", "vfx_persistent"})
            exclusions = audit.converter_exclusions(evidence)
            self.assertEqual(set(exclusions["excluded_resrefs"]), set(evidence))
            self.assertIn("Looping=1", exclusions["excluded_resrefs"]["git"])
            (tables / "visualeffects.2da").unlink()
            with self.assertRaises(OSError):
                audit.collect_exclusions(root, root / "SWLOR_Haks")


if __name__ == "__main__":
    unittest.main()
