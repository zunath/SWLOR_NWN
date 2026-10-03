"""Focused binary-preservation tests for TranslateWeaponModel."""
import math
from pathlib import Path
import struct
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from TranslateWeaponModel import translate_model


def fixture() -> bytes:
    model_length = 1800
    raw_length = 16
    data = bytearray(12 + model_length + raw_length)
    struct.pack_into("<III", data, 0, 0, model_length, raw_length)
    base = 12
    struct.pack_into("<I", data, base + 72, 232)  # root node
    struct.pack_into("<6f", data, base + 136, -1, -2, -3, 4, 5, 6)
    struct.pack_into("<f", data, base + 160, 8.0)
    struct.pack_into("<f", data, base + 164, 1.0)
    data[base + 168:base + 172] = b"NULL"

    root = base + 232
    struct.pack_into("<II", data, root + 72, 344, 1)  # child pointer array
    struct.pack_into("<I", data, root + 108, 1)  # rigid dummy
    struct.pack_into("<I", data, base + 344, 348)  # child node pointer

    child = base + 348
    struct.pack_into("<II", data, child + 84, 1000, 2)  # controller keys
    struct.pack_into("<II", data, child + 96, 1024, 9)  # controller values
    struct.pack_into("<I", data, child + 108, 33)  # trimesh
    struct.pack_into("<IHHHBB", data, base + 1000, 8, 1, 0, 1, 3, 0)
    struct.pack_into("<IHHHBB", data, base + 1012, 20, 1, 4, 5, 4, 0)
    struct.pack_into("<9f", data, base + 1024, 0, .1, .2, .3, 0, 0, 0, 1, 777)
    data[base + 1100:base + 1116] = b"mesh sentinel 01"
    data[-raw_length:] = bytes(range(raw_length))
    return bytes(data)


class TranslateWeaponModelTests(unittest.TestCase):
    def test_zero_offset_preserves_every_input_byte(self):
        source = fixture()
        self.assertEqual(translate_model(source, (0, 0, 0)), source)

    def test_translation_changes_only_position_bounds_and_radius(self):
        source = fixture()
        delta = (0.0, -0.015, 0.0)
        translated = translate_model(source, delta)
        base = 12
        self.assertEqual(len(translated), len(source))
        for actual, expected in zip(struct.unpack_from("<3f", translated, base + 1024 + 4),
                                    (.1, .185, .3)):
            self.assertAlmostEqual(actual, expected, places=6)
        for actual, expected in zip(struct.unpack_from("<6f", translated, base + 136),
                                    (-1, -2.015, -3, 4, 4.985, 6)):
            self.assertAlmostEqual(actual, expected, places=6)
        self.assertAlmostEqual(struct.unpack_from("<f", translated, base + 160)[0], 8.015, places=6)
        expected = bytearray(source)
        struct.pack_into("<3f", expected, base + 1024 + 4, .1, .185, .3)
        struct.pack_into("<f", expected, base + 140, -2.015)
        struct.pack_into("<f", expected, base + 152, 4.985)
        struct.pack_into("<f", expected, base + 160, 8.015)
        self.assertEqual(translated, bytes(expected))
        self.assertEqual(translated[base + 1024 + 20:base + 1024 + 36],
                         source[base + 1024 + 20:base + 1024 + 36])
        self.assertEqual(translated[-16:], source[-16:])

    def test_rejects_bad_offset(self):
        for delta in ((1, 2), (math.nan, 0, 0), (math.inf, 0, 0)):
            with self.subTest(delta=delta), self.assertRaises(ValueError):
                translate_model(fixture(), delta)

    def test_rejects_animated_models(self):
        source = bytearray(fixture())
        struct.pack_into("<II", source, 12 + 120, 700, 1)
        with self.assertRaisesRegex(ValueError, "animated"):
            translate_model(bytes(source), (0, 0, -0.01))

    def test_rejects_missing_or_multrow_position(self):
        for key in (
            (7, 1, 0, 1, 3, 0),
            (8, 2, 0, 1, 3, 0),
        ):
            source = bytearray(fixture())
            struct.pack_into("<IHHHBB", source, 12 + 1000, *key)
            with self.subTest(key=key), self.assertRaises(ValueError):
                translate_model(bytes(source), (0, 0, -0.01))

    def test_rejects_skinned_nodes_and_aliased_arrays(self):
        skinned = bytearray(fixture())
        struct.pack_into("<I", skinned, 12 + 348 + 108, 33 | 64)
        with self.assertRaisesRegex(ValueError, "skinned"):
            translate_model(bytes(skinned), (0, 0, -0.01))

        aliased = bytearray(fixture())
        struct.pack_into("<I", aliased, 12 + 348 + 96, 1000)
        with self.assertRaisesRegex(ValueError, "alias"):
            translate_model(bytes(aliased), (0, 0, -0.01))


if __name__ == "__main__":
    unittest.main()
