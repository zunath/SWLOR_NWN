"""Run with: python -m unittest discover -s tools/tests -p test_trellis_nwn_material.py"""
import importlib.util
import struct
import tempfile
import unittest
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('trellis_nwn_material', ROOT / 'tools/TrellisNwnMaterial.py')
material = importlib.util.module_from_spec(spec)
spec.loader.exec_module(material)


def dds_header(width, height, mips, fourcc):
    """Minimal standard DDS header with the fields the converter reads and preserves."""
    header = bytearray(128)
    header[:4] = b'DDS '
    struct.pack_into('<IIIII', header, 4, 124, 0x000A1007, height, width, 0)
    struct.pack_into('<I', header, 28, mips)
    struct.pack_into('<I', header, 76, 32)
    struct.pack_into('<I', header, 80, 4)
    header[84:88] = fourcc.encode()
    return bytes(header)


def write_map(path, image, fourcc, color=False):
    h, w = image.shape[:2]
    mips = int(np.log2(max(w, h))) + 1
    info = {'header': dds_header(w, h, mips, fourcc), 'fourcc': fourcc, 'mips': mips, 'size': (w, h)}
    material.write_dds(path, image, info, color=color)


class MaterialResponseTests(unittest.TestCase):
    def test_specularity_reproduces_metal_and_keeps_non_metal_low(self):
        self.assertAlmostEqual(float(material.metal_specularity(0)), material.DIELECTRIC_SPECULARITY)
        for metal in (.1, .5, 1):
            spec = material.metal_specularity(metal)
            self.assertAlmostEqual(float(material.specularity_to_metallic(spec)), metal, places=5)
        # Non-metal specularity never turns into shader metallicness.
        for metal in np.linspace(0, .1, 11):
            self.assertLessEqual(float(material.specularity_to_metallic(material.metal_specularity(metal))), metal + 1e-6)

    def test_metal_albedo_rises_and_keeps_hue(self):
        dark_bronze = np.array([[[.30, .20, .12]]], np.float32)
        raised = material.metal_base_color(dark_bronze, np.ones((1, 1)))
        before, after = material.linear(dark_bronze)[0, 0], material.linear(raised)[0, 0]
        self.assertGreaterEqual(float(after @ material.LUMINANCE), material.METAL_LUMINANCE_FLOOR - 1e-4)
        np.testing.assert_allclose(after / after.sum(), before / before.sum(), atol=1e-3)
        # Non-metal texels are untouched.
        np.testing.assert_allclose(material.metal_base_color(dark_bronze, np.zeros((1, 1))), dark_bronze, atol=1e-5)

    def test_saturated_metal_gains_less_than_neutral_metal(self):
        grey = material.srgb(np.full((1, 1, 3), .05, np.float32))
        blue = material.srgb(np.array([[[.005, .02, .14]]], np.float32))
        gain = lambda c: float((material.linear(material.metal_base_color(c, np.ones((1, 1)))) @ material.LUMINANCE)[0, 0]
                               / (material.linear(c) @ material.LUMINANCE)[0, 0])
        self.assertLess(gain(blue), gain(grey))

    def test_metal_roughness_tightens_only_metal(self):
        rough = np.array([.2, .5, .9], np.float32)
        np.testing.assert_allclose(material.metal_roughness(rough, np.zeros(3)), rough)
        metal = material.metal_roughness(rough, np.ones(3))
        self.assertTrue(np.all(metal <= rough))
        self.assertTrue(np.all((metal >= material.METAL_ROUGHNESS_RANGE[0]) & (metal <= material.METAL_ROUGHNESS_RANGE[1])))

    def test_resample_nearest_matches_requested_shape(self):
        mask = np.arange(16, dtype=np.float32).reshape(4, 4)
        self.assertEqual(material.resample_nearest(mask, (8, 2)).shape, (8, 2))
        self.assertIs(material.resample_nearest(mask, (4, 4)), mask)


class DdsCodecTests(unittest.TestCase):
    def test_round_trip_keeps_size_mips_and_detail(self):
        rng = np.random.default_rng(1)
        smooth = np.kron(rng.random((8, 8, 3)), np.ones((4, 4, 1))).astype(np.float32)
        with tempfile.TemporaryDirectory() as folder:
            for name, image, fourcc in (('c.dds', smooth, 'DXT1'), ('s.dds', smooth[..., :1], 'ATI1')):
                path = Path(folder) / name
                write_map(path, image, fourcc, color=fourcc == 'DXT1')
                decoded, info = material.read_dds(path)
                self.assertEqual((info['fourcc'], info['size'], info['mips']), (fourcc, (32, 32), 6))
                blocks = sum(max(1, (32 >> i) // 4) ** 2 for i in range(6))
                self.assertEqual(path.stat().st_size, 128 + blocks * 8)
                self.assertLess(float(np.sqrt(((decoded - image) ** 2).mean())), .03)


class ShippedFixTests(unittest.TestCase):
    def make_material(self, folder, name, spec, rough, base_value):
        (folder / f'{name}.mtr').write_text(
            f'renderhint NormalAndSpecMapped\ntexture0 {name}\ntexture1 {name}_n\ntexture2 {spec}\ntexture3 {rough}\n')
        write_map(folder / f'{name}.dds', np.full((8, 8, 3), base_value, np.float32), 'DXT1', color=True)

    def test_converts_shared_maps_once_and_refuses_a_second_run(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp)
            metal = np.zeros((8, 8, 1), np.float32)
            metal[:, 4:] = material.metal_specularity(1)
            metal[:, :4] = .2  # the old exporter's dielectric value
            write_map(folder / 'shared_s.dds', metal, 'ATI1')
            write_map(folder / 'shared_r.dds', np.full((8, 8, 1), .6, np.float32), 'ATI1')
            self.make_material(folder, 'one', 'shared_s', 'shared_r', .2)
            self.make_material(folder, 'two', 'shared_s', 'shared_r', .3)
            material.fix_shipped(folder, ['one.mtr', 'two.mtr'])
            spec = material.read_dds(folder / 'shared_s.dds')[0][..., 0]
            self.assertAlmostEqual(float(spec[:, :4].mean()), material.DIELECTRIC_SPECULARITY, delta=.01)
            rough = material.read_dds(folder / 'shared_r.dds')[0][..., 0]
            self.assertLess(float(rough[:, 4:].mean()), .3)
            self.assertAlmostEqual(float(rough[:, :4].mean()), .6, delta=.01)
            base = material.read_dds(folder / 'one.dds')[0]
            self.assertGreater(float(base[:, 4:].mean()), float(base[:, :4].mean()))
            with self.assertRaisesRegex(ValueError, 'Already converted'):
                material.fix_shipped(folder, ['one.mtr'])

    def test_rejects_roughness_shared_by_different_specularity_maps(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp)
            for spec in ('a_s', 'b_s'):
                write_map(folder / f'{spec}.dds', np.full((8, 8, 1), .2, np.float32), 'ATI1')
            write_map(folder / 'r.dds', np.full((8, 8, 1), .5, np.float32), 'ATI1')
            self.make_material(folder, 'one', 'a_s', 'r', .2)
            self.make_material(folder, 'two', 'b_s', 'r', .2)
            with self.assertRaisesRegex(ValueError, 'more than one specularity'):
                material.fix_shipped(folder, ['one.mtr', 'two.mtr'], check=True)


if __name__ == '__main__':
    unittest.main()
