import hashlib
import importlib.util
import io
import json
import math
import os
import re
import struct
import tempfile
import unittest
import wave
from pathlib import Path

SPEC = importlib.util.spec_from_file_location(
    "astromech", Path(__file__).parents[1] / "GenerateAstromechSoundset.py")
astromech = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(astromech)

SOUND_ROOTS = [
    Path(os.environ["SWLOR_HAKS_ROOT"]) / "sw_sound" if os.environ.get("SWLOR_HAKS_ROOT") else None,
    Path(__file__).parents[2] / "SWLOR_Haks" / "sw_sound",
]


def find_existing_ssf(name):
    for root in SOUND_ROOTS:
        if root is not None:
            candidate = root / name
            if candidate.is_file() and candidate.stat().st_size == 1216:
                return candidate
    return None


class SsfWriterTests(unittest.TestCase):
    def test_layout_of_a_synthetic_file(self):
        entries = [("vs_x_%d" % i, i) if i % 2 else ("", astromech.SSF_NO_STRREF) for i in range(49)]
        data = astromech.write_ssf(entries)
        self.assertEqual(len(data), 1216)
        self.assertEqual(data[:8], b"SSF V1.0")
        self.assertEqual(struct.unpack_from("<II", data, 8), (49, 40))
        self.assertEqual(data[16:40], bytes(24))
        offsets = struct.unpack_from("<49I", data, 40)
        self.assertEqual(offsets, tuple(236 + 20 * i for i in range(49)))
        self.assertEqual(astromech.parse_ssf(data), entries)

    def test_rejects_wrong_entry_count_and_long_resref(self):
        with self.assertRaises(ValueError):
            astromech.write_ssf([("a", 0)] * 48)
        with self.assertRaises(ValueError):
            astromech.write_ssf([("a" * 17, 0)] * 49)

    def test_rewrites_existing_files_byte_for_byte(self):
        checked = 0
        for name in ("c_monodrn.ssf", "Droid.ssf"):
            path = find_existing_ssf(name)
            if path is None:
                continue
            original = path.read_bytes()
            self.assertEqual(astromech.write_ssf(astromech.parse_ssf(original)), original, name)
            checked += 1
        if checked == 0:
            self.skipTest("SWLOR_Haks/sw_sound not available")


class SoundsetTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.ssf, cls.sounds = astromech.build_soundset()
        cls.by_event = {sound["event"]: sound for sound in cls.sounds}

    def test_required_entries_are_filled(self):
        entries = astromech.parse_ssf(self.ssf)
        for suffix in astromech.REQUIRED_SUFFIXES:
            index = astromech.EVENTS.index(suffix)
            self.assertEqual(entries[index][0], "vs_astromec_" + suffix, suffix)
            self.assertIn(suffix, self.by_event)

    def test_resref_rules(self):
        for sound in self.sounds:
            resref = sound["resref"]
            self.assertLessEqual(len(resref), 16, resref)
            self.assertRegex(resref, r"^[a-z0-9_]+$")
            self.assertTrue(resref.startswith("vs_astromec_"))
        self.assertLessEqual(len(astromech.DEFAULT_PREFIX), 11)
        self.assertEqual(len({sound["resref"] for sound in self.sounds}), len(self.sounds))

    def test_events_are_in_standard_ssf_order(self):
        self.assertEqual(len(astromech.EVENTS), 49)
        expected = {0: "attk", 1: "bat1", 14: "hit1", 18: "dead", 33: "slct", 35: "yes", 48: "warn"}
        for index, suffix in expected.items():
            self.assertEqual(astromech.EVENTS[index], suffix)

    def test_wav_format_duration_and_level(self):
        ceiling = 32767 * 10 ** (astromech.PEAK_DBFS / 20)
        for sound in self.sounds:
            with wave.open(io.BytesIO(sound["wav"])) as handle:
                self.assertEqual(handle.getframerate(), 22050)
                self.assertEqual(handle.getnchannels(), 1)
                self.assertEqual(handle.getsampwidth(), 2)
                seconds = handle.getnframes() / handle.getframerate()
            self.assertGreaterEqual(seconds, 0.3, sound["event"])
            self.assertLessEqual(seconds, 1.6, sound["event"])
            peak = max(abs(s) for s in sound["samples"])
            self.assertLessEqual(peak, 32767)
            self.assertAlmostEqual(20 * math.log10(peak / 32767), -3.0, delta=0.1, msg=sound["event"])
            self.assertLessEqual(peak, ceiling + 1)

    def test_no_clicks_at_the_ends(self):
        for sound in self.sounds:
            samples = sound["samples"]
            self.assertLessEqual(abs(samples[0]), 300, sound["event"])
            self.assertLessEqual(abs(samples[-1]), 300, sound["event"])
            self.assertLessEqual(max(abs(s) for s in samples[:20]), 3000, sound["event"])
            self.assertLessEqual(max(abs(s) for s in samples[-20:]), 3000, sound["event"])

    def test_no_two_events_share_audio(self):
        digests = [hashlib.sha256(sound["wav"]).hexdigest() for sound in self.sounds]
        self.assertEqual(len(set(digests)), len(digests))

    def test_phrase_table_covers_exactly_known_events(self):
        self.assertTrue(set(astromech.PHRASES) <= set(astromech.EVENTS))
        for steps in astromech.PHRASES.values():
            for name, _params in steps:
                self.assertIn(name, astromech.PRIMITIVES)


class CommandLineTests(unittest.TestCase):
    def test_two_runs_are_identical_and_manifest_matches(self):
        with tempfile.TemporaryDirectory() as first, tempfile.TemporaryDirectory() as second:
            self.assertEqual(astromech.main(["--output", first]), 0)
            self.assertEqual(astromech.main(["--output", second]), 0)
            names = sorted(path.name for path in Path(first).iterdir())
            self.assertEqual(names, sorted(path.name for path in Path(second).iterdir()))
            self.assertIn("vs_astromec.ssf", names)
            self.assertIn("manifest.json", names)
            for name in names:
                self.assertEqual((Path(first) / name).read_bytes(), (Path(second) / name).read_bytes(), name)
            manifest = json.loads((Path(first) / "manifest.json").read_text())
            for row in manifest:
                data = (Path(first) / (row["resref"] + ".wav")).read_bytes()
                self.assertEqual(hashlib.sha256(data).hexdigest(), row["sha256"])

    def test_refuses_a_non_empty_directory(self):
        with tempfile.TemporaryDirectory() as directory:
            (Path(directory) / "keep.txt").write_text("x")
            self.assertEqual(astromech.main(["--output", directory]), 1)

    def test_custom_prefix_must_fit_resref_limit(self):
        with tempfile.TemporaryDirectory() as directory:
            self.assertEqual(astromech.main(["--output", directory, "--prefix", "vs_astromech_long"]), 1)


if __name__ == "__main__":
    unittest.main()
