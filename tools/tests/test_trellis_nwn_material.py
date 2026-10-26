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


    def test_chroma_only_block_keeps_both_colors(self):
        # Red/green halves share luminance-free variation orthogonal to (1, 1, 1).
        block = np.zeros((4, 4, 3), np.float32)
        block[:, :2] = (.8, .2, .5)
        block[:, 2:] = (.2, .8, .5)
        info = {'header': dds_header(4, 4, 1, 'DXT1'), 'fourcc': 'DXT1', 'mips': 1, 'size': (4, 4)}
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'c.dds'
            material.write_dds(path, block, info)
            decoded, _ = material.read_dds(path)
        self.assertLess(float(np.abs(decoded - block).max()), .05)

    def test_leading_axis_wins_over_a_larger_covariance_column(self):
        # Correlated red/green carries the most variance; independent blue has the largest column.
        # Uncorrelated patterns: red/green alternate by column, blue by row. Red/green variance sums
        # to 2a^2 while each red/green column has norm 1.41a^2 < b^2 < 2a^2 for blue.
        index = np.arange(16)
        rg = .5 + .35 * np.where(index % 2, 1, -1)
        blue = .5 + .45 * np.where(index // 4 % 2, 1, -1)
        block = np.stack([rg, rg, blue], -1).reshape(4, 4, 3).astype(np.float32)
        info = {'header': dds_header(4, 4, 1, 'DXT1'), 'fourcc': 'DXT1', 'mips': 1, 'size': (4, 4)}
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'c.dds'
            material.write_dds(path, block, info)
            decoded, _ = material.read_dds(path)
        self.assertGreater(float(np.ptp(decoded[..., 0])), .5)


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

    def test_refuses_a_second_run_on_metal_only_maps(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp)
            write_map(folder / 'm_s.dds', np.full((8, 8, 1), material.metal_specularity(1), np.float32), 'ATI1')
            write_map(folder / 'm_r.dds', np.full((8, 8, 1), .5, np.float32), 'ATI1')
            self.make_material(folder, 'metal', 'm_s', 'm_r', .2)
            material.fix_shipped(folder, ['metal.mtr'])
            for name in ('metal.dds', 'm_s.dds', 'm_r.dds'):
                self.assertTrue(material.is_marked(material.read_dds(folder / name)[1]), name)
            with self.assertRaisesRegex(ValueError, 'Already converted'):
                material.fix_shipped(folder, ['metal.mtr'], check=True)

    def test_size_mismatch_writes_nothing(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp)
            write_map(folder / 'a_s.dds', np.full((8, 8, 1), .2, np.float32), 'ATI1')
            write_map(folder / 'a_r.dds', np.full((8, 8, 1), .5, np.float32), 'ATI1')
            write_map(folder / 'b_s.dds', np.full((8, 8, 1), .2, np.float32), 'ATI1')
            write_map(folder / 'b_r.dds', np.full((4, 4, 1), .5, np.float32), 'ATI1')
            self.make_material(folder, 'one', 'a_s', 'a_r', .2)
            self.make_material(folder, 'two', 'b_s', 'b_r', .2)
            before = {p.name: p.read_bytes() for p in folder.glob('*.dds')}
            with self.assertRaisesRegex(ValueError, 'sizes differ'):
                material.fix_shipped(folder, ['one.mtr', 'two.mtr'])
            self.assertEqual({p.name: p.read_bytes() for p in folder.glob('*.dds')}, before)

    def test_rejects_base_shared_by_different_specularity_maps(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp)
            for spec in ('a_s', 'b_s'):
                write_map(folder / f'{spec}.dds', np.full((8, 8, 1), .2, np.float32), 'ATI1')
                write_map(folder / f'{spec[0]}_r.dds', np.full((8, 8, 1), .5, np.float32), 'ATI1')
            write_map(folder / 'shared.dds', np.full((8, 8, 3), .2, np.float32), 'DXT1', color=True)
            for name, spec in (('one', 'a'), ('two', 'b')):
                (folder / f'{name}.mtr').write_text(
                    f'renderhint NormalAndSpecMapped\ntexture0 shared\ntexture1 n\ntexture2 {spec}_s\ntexture3 {spec}_r\n')
            with self.assertRaisesRegex(ValueError, 'Base map shared pairs'):
                material.fix_shipped(folder, ['one.mtr', 'two.mtr'])

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
