"""Verify conversion provenance, NWN orientation, alpha, and complete DDS mips."""
import copy
import json
from pathlib import Path
import shutil
import struct
import sys
import tempfile
import unittest
from unittest.mock import patch

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import ConvertWorldTexturesToDds as converter


def tga_bytes(image, descriptor=8):
    """Write the same image in any of the four TGA storage orientations."""
    stored = image
    if not descriptor & 32:
        stored = stored.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    if descriptor & 16:
        stored = stored.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    header = bytearray(18)
    header[2] = 2
    struct.pack_into("<HH", header, 12, image.width, image.height)
    header[16] = 32 if image.mode == "RGBA" else 24
    header[17] = descriptor
    return bytes(header) + stored.tobytes("raw", "BGRA" if image.mode == "RGBA" else "BGR")


def candidate(name, raw, width, height, alpha="opaque"):
    return {"path": name, "source_sha256": converter.sha256(raw), "width": width,
            "height": height, "alpha": alpha, "format": "DXT1" if alpha == "opaque" else "DXT5"}


def synthetic_dds(width=8, height=4, fmt="DXT1"):
    """A structurally valid zero-filled compressed image, independent of encoders."""
    data = bytearray(converter.expected_dds_size(width, height, fmt))
    data[:4] = b"DDS "
    struct.pack_into("<I", data, 4, 124)
    struct.pack_into("<I", data, 8, 0xA1007)
    struct.pack_into("<II", data, 12, height, width)
    struct.pack_into("<I", data, 20, ((width + 3) // 4) * ((height + 3) // 4) * (8 if fmt == "DXT1" else 16))
    struct.pack_into("<I", data, 28, len(converter.mip_dimensions(width, height)))
    struct.pack_into("<II", data, 76, 32, 4)
    data[84:88] = fmt.encode("ascii")
    struct.pack_into("<I", data, 108, 0x401008)
    return bytes(data)


class WorldTexturePreflightTests(unittest.TestCase):
    def test_tga_origin_flags_preserve_visual_facing_and_hidden_rgb(self):
        source = Image.new("RGBA", (8, 4))
        source.putdata([(x * 30, y * 60, (x + y) * 20, 0 if x < 3 else 255)
                        for y in range(4) for x in range(8)])
        for descriptor in (8, 24, 40, 56):
            with self.subTest(descriptor=descriptor):
                self.assertEqual(converter.decode_tga(tga_bytes(source, descriptor)).tobytes(), source.tobytes())

    def test_tiny_legacy_tga_does_not_require_optional_footer(self):
        source = Image.new("RGB", (1, 1), (255, 127, 0))
        raw = tga_bytes(source, 32)
        self.assertEqual(len(raw), 21)
        self.assertEqual(converter.decode_tga(raw).getpixel((0, 0)), (255, 127, 0, 255))

    def test_truncated_and_interleaved_tgas_are_rejected(self):
        raw = tga_bytes(Image.new("RGBA", (8, 4), "red"))
        for changed in (raw[:17], raw[:-10], raw[:17] + bytes([0xC8]) + raw[18:]):
            with self.subTest(length=len(changed)):
                with self.assertRaises(ValueError):
                    converter.decode_tga(changed)

    def test_manifest_rejects_unsafe_duplicate_and_mismatched_entries(self):
        row = candidate("world/texture.tga", b"source", 8, 4)
        for path in ("../texture.tga", "/texture.tga", "world/../texture.tga",
                     "world//texture.tga", "C:/texture.tga", "world\\texture.tga"):
            with self.subTest(path=path), self.assertRaisesRegex(ValueError, "Unsafe"):
                converter.validate_candidates([{**row, "path": path}])
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            converter.validate_candidates([row, {**row, "path": "WORLD/Texture.tga"}])
        for changes in ({"alpha": "binary"}, {"source_sha256": "bad"}, {"width": 7}, {"height": 2}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                converter.validate_candidates([{**row, **changes}])

    def test_preflight_checks_actual_alpha_dimensions_and_source_digest(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            raw = tga_bytes(Image.new("RGBA", (8, 4), (255, 0, 0, 64)))
            (root / "texture.tga").write_bytes(raw)
            row = candidate("texture.tga", raw, 8, 4, "graded")
            self.assertEqual(converter.verify_source(root, row)[1].size, (8, 4))
            for changes, message in (({"source_sha256": "0" * 64}, "hash"),
                                     ({"height": 8}, "dimensions"),
                                     ({"alpha": "opaque", "format": "DXT1"}, "alpha")):
                with self.subTest(changes=changes), self.assertRaisesRegex(ValueError, message):
                    converter.verify_source(root, {**row, **changes})

    def test_full_chain_accounts_for_rectangular_and_sub_block_mips(self):
        self.assertEqual(converter.mip_dimensions(8, 4), [(8, 4), (4, 2), (2, 1), (1, 1)])
        self.assertEqual(converter.expected_dds_size(8, 4, "DXT1"), 168)
        self.assertEqual(converter.expected_dds_size(8, 4, "DXT5"), 208)
        for fmt in ("DXT1", "DXT5"):
            converter.validate_dds(synthetic_dds(fmt=fmt), 8, 4, fmt)

    def test_dds_rejects_truncation_extra_payload_and_inconsistent_headers(self):
        original = synthetic_dds()
        for changed in (original[:120], original[:-1], original + b"extra"):
            with self.subTest(length=len(changed)), self.assertRaises(ValueError):
                converter.validate_dds(changed, 8, 4, "DXT1")
        for offset, value in ((4, 123), (8, 0x81007), (12, 8), (20, 999), (24, 1),
                              (28, 1), (76, 31), (80, 0), (108, 0x1000), (112, 0x200)):
            changed = bytearray(original)
            struct.pack_into("<I", changed, offset, value)
            with self.subTest(offset=offset), self.assertRaises(ValueError):
                converter.validate_dds(changed, 8, 4, "DXT1")
        with self.assertRaises(ValueError):
            converter.validate_dds(original[:84] + b"DXT5" + original[88:], 8, 4, "DXT1")

    def test_metrics_distinguish_flipped_images_without_quality_gate(self):
        source = Image.new("RGBA", (8, 8), "red")
        source.paste((0, 0, 255, 255), (0, 0, 8, 4))
        flipped = source.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
        metrics = converter.image_metrics(source, flipped)
        self.assertGreater(metrics["rgb_mae"], 100)
        self.assertEqual(metrics["orientation_rgb_mae"]["vertical_flip"], 0)
        self.assertEqual(len(converter.rgb_grid(source)), 384)

    def test_staging_resolves_existing_parents_without_resolving_missing_leaves(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            rows = [{"path": "world/nested/texture.tga"}, {"path": "other/texture.tga"}]
            converter.prepare_staging_directories(root, rows)
            self.assertTrue((root / "world/nested").is_dir())
            self.assertTrue((root / "other").is_dir())
            original_resolve = Path.resolve

            def reject_missing_leaf(path, *args, **kwargs):
                if path.suffix == ".dds" and not path.exists():
                    raise AssertionError("Missing output leaves must not use Path.resolve")
                return original_resolve(path, *args, **kwargs)

            with patch.object(Path, "resolve", reject_missing_leaf):
                result = converter.staging_path(root, "world/nested/texture.dds")
            self.assertEqual(result, root.resolve() / "world/nested/texture.dds")
            with self.assertRaisesRegex(ValueError, "Unsafe"):
                converter.staging_path(root, "../outside.dds")

    def test_staging_rejects_a_resolved_parent_outside_its_root(self):
        with tempfile.TemporaryDirectory() as temporary:
            base = Path(temporary)
            root, outside = base / "stage", base / "outside"
            root.mkdir()
            outside.mkdir()
            linked = root / "link"
            original_resolve = Path.resolve

            def simulate_directory_link(path, *args, **kwargs):
                if path == linked:
                    return outside.resolve()
                return original_resolve(path, *args, **kwargs)

            with patch.object(Path, "resolve", simulate_directory_link):
                with self.assertRaisesRegex(ValueError, "escapes its root.*root=.*resolved="):
                    converter.staging_path(root, "link/texture.dds")


@unittest.skipUnless(shutil.which("magick"), "ImageMagick is required for encoder integration")
class WorldTextureEncodingTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.source = self.root / "source"
        self.source.mkdir()
        self.output = self.root / "output"
        self.manifest = self.root / "candidates.json"
        self.report = self.root / "report.json"

    def prepare(self, transparent=False, descriptor=8):
        image = Image.new("RGBA", (16, 8), (255, 0, 0, 0 if transparent else 255))
        image.paste((0, 255, 0, 64 if transparent else 255), (8, 0, 16, 4))
        image.paste((0, 0, 255, 128 if transparent else 255), (0, 4, 8, 8))
        image.paste((255, 255, 255, 255), (8, 4, 16, 8))
        raw = tga_bytes(image, descriptor)
        (self.source / "texture.tga").write_bytes(raw)
        rows = [candidate("texture.tga", raw, 16, 8, "graded" if transparent else "opaque")]
        self.manifest.write_text(json.dumps(rows))
        return image, raw

    def convert(self):
        return converter.stage(self.manifest, self.source, self.output, self.report, workers=1)

    def test_real_encoder_preserves_orientation_hidden_rgb_alpha_and_full_chain(self):
        for transparent, descriptor in ((False, 8), (True, 56)):
            with self.subTest(transparent=transparent):
                if self.output.exists():
                    shutil.rmtree(self.output)
                    self.report.unlink()
                image, raw = self.prepare(transparent, descriptor)
                with patch.object(converter.subprocess, "run", wraps=converter.subprocess.run) as encoder:
                    report = self.convert()
                encoding_calls = [call.args[0] for call in encoder.call_args_list
                                  if str(call.args[0][-1]).endswith(".dds")]
                self.assertEqual(len(encoding_calls), 1)
                self.assertIn("dds:cluster-fit=true", encoding_calls[0])
                self.assertEqual(report["encoder_settings"], {"dds:cluster-fit": True})
                row = report["entries"][0]
                encoded = (self.output / "texture.dds").read_bytes()
                self.assertEqual(row["format"], "DXT5" if transparent else "DXT1")
                self.assertEqual(row["mip_count"], 5)
                self.assertEqual(row["metrics"]["rgb_mae"], 0)
                self.assertEqual(row["metrics"]["alpha_mae"], 0)
                self.assertEqual(converter.decode_nwn_dds(encoded).tobytes(), image.tobytes())
                self.assertEqual((self.source / "texture.tga").read_bytes(), raw)
                self.assertEqual(converter.validate_report(self.report, self.output), report["totals"])

    def test_validation_detects_tampering_and_retained_override_without_sources(self):
        self.prepare()
        original = self.convert()
        destination = self.output / "texture.dds"
        raw = destination.read_bytes()
        destination.write_bytes(raw[:-1] + bytes([raw[-1] ^ 1]))
        with self.assertRaisesRegex(ValueError, "DDS changed"):
            converter.validate_report(self.report, self.output)
        destination.write_bytes(raw)
        changed = copy.deepcopy(original)
        changed["entries"][0]["decoded_display_rgb_grid"] = "00" * 192
        self.report.write_text(json.dumps(changed))
        with self.assertRaisesRegex(ValueError, "display changed"):
            converter.validate_report(self.report, self.output)
        self.report.write_text(json.dumps(original))
        (self.output / "texture.tga").write_bytes(b"override")
        with self.assertRaisesRegex(ValueError, "would override"):
            converter.validate_report(self.report, self.output)

    def test_no_overwrite_or_partial_conversion_after_bad_preflight(self):
        self.prepare()
        rows = json.loads(self.manifest.read_text())
        rows[0]["source_sha256"] = "0" * 64
        self.manifest.write_text(json.dumps(rows))
        with self.assertRaisesRegex(ValueError, "hash changed"):
            self.convert()
        self.assertFalse(self.output.exists())
        self.assertFalse(self.report.exists())
        self.prepare()
        (self.source / "texture.dds").write_bytes(b"existing source asset")
        with self.assertRaisesRegex(ValueError, "Existing sibling"):
            self.convert()
        self.assertFalse(self.output.exists())
        (self.source / "texture.dds").unlink()
        self.convert()
        with self.assertRaisesRegex(ValueError, "must not already exist"):
            self.convert()


if __name__ == "__main__":
    unittest.main()
