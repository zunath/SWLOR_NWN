"""Byte-level reflection regressions, including legacy cross-row RLE streams."""
import random
import struct
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from portrait_tga import decode, encode, flip_horizontal, normalized_metadata


def tga(width, height, pixels, depth=24, kind=2, descriptor=0, trailer=b""):
    header = bytearray(18)
    header[:3] = bytes((3, 0, kind))
    struct.pack_into("<HH", header, 8, 7, 9)  # Preserve origin coordinates too.
    struct.pack_into("<HH", header, 12, width, height)
    header[16:18] = bytes((depth, descriptor))
    return bytes(header) + b"ID!" + pixels + trailer


def reflect(pixels, width, height, bpp):
    return b"".join(pixels[(y * width + x) * bpp:(y * width + x + 1) * bpp]
                    for y in range(height) for x in reversed(range(width)))


class PortraitTgaTests(unittest.TestCase):
    def test_uncompressed_all_origins_and_alpha_are_exact(self):
        for depth in (24, 32):
            for origin in (0, 16, 32, 48):
                with self.subTest(depth=depth, origin=origin):
                    pixels = bytes(range(6 * depth // 8))
                    original = tga(3, 2, pixels, depth, descriptor=origin,
                                   trailer=b"arbitrary trailing metadata")
                    changed = flip_horizontal(original)
                    decoded = decode(changed)
                    self.assertEqual(decoded.pixels, reflect(pixels, 3, 2, depth // 8))
                    self.assertEqual(decoded.prefix, original[:21])
                    self.assertEqual(decoded.trailing, b"arbitrary trailing metadata")
                    self.assertEqual(flip_horizontal(changed), original)

    def test_rle_packets_crossing_scanlines_and_footer_preservation(self):
        # A four-pixel run crosses the first 3-pixel scanline boundary.
        a, b, c = b"abc", b"def", b"ghi"
        footer = b"opaque metadata" + bytes(8) + b"TRUEVISION-XFILE.\x00"
        original = tga(3, 2, bytes((131,)) + a + bytes((1,)) + b + c,
                       kind=10, trailer=footer)
        before = decode(original)
        self.assertEqual(before.pixels, a * 4 + b + c)
        changed = flip_horizontal(original)
        after = decode(changed)
        self.assertEqual(after.pixels, a * 3 + c + b + a)
        self.assertEqual(after.prefix, before.prefix)
        self.assertEqual(after.trailing, before.trailing)
        self.assertEqual(decode(flip_horizontal(changed)).pixels, before.pixels)
        self.assert_scanline_packets(changed)

    def assert_scanline_packets(self, data):
        image = decode(data)
        cursor, total = len(image.prefix), 0
        while total < image.width * image.height:
            packet = data[cursor]
            count = (packet & 127) + 1
            self.assertLessEqual(total % image.width + count, image.width)
            cursor += 1 + image.bytes_per_pixel * (1 if packet & 128 else count)
            total += count
        self.assertEqual(data[cursor:], image.trailing)

    def test_rle_raw_crossing_rows_and_long_packets_round_trip(self):
        rng = random.Random(7)
        for depth in (24, 32):
            bpp = depth // 8
            # Include long runs, raw pixels and a run at the 128-pixel limit.
            pixels = bytes(bpp) * 130 + rng.randbytes(270 * bpp)
            raw_packets = b"".join(bytes((min(128, 400 - x) - 1,))
                                    + pixels[x * bpp:(x + 128) * bpp]
                                    for x in range(0, 400, 128))
            original = tga(200, 2, raw_packets, depth, kind=10)
            self.assertEqual(decode(original).pixels, pixels)
            output = flip_horizontal(original)
            self.assertEqual(decode(output).pixels, reflect(pixels, 200, 2, bpp))
            self.assertEqual(decode(flip_horizontal(output)).pixels, pixels)
            self.assert_scanline_packets(output)

    def test_offset_bearing_rle_metadata_fails_closed(self):
        for offsets in ((30, 0), (0, 30)):
            footer = struct.pack("<II", *offsets) + b"TRUEVISION-XFILE.\x00"
            data = tga(2, 1, b"\x81abc", kind=10, trailer=footer)
            with self.assertRaises(ValueError):
                flip_horizontal(data)

    def test_rle_extension_relocation_preserves_metadata(self):
        extension = bytearray(495)
        struct.pack_into("<H", extension, 0, 495)
        extension[2:12] = b"Artist ABC"
        # Original 4-pixel cross-row run gets split and therefore grows.
        payload = b"\x83abc\x01defghi"
        prefix_gap = b"vendor padding"
        extension_offset = 21 + len(payload) + len(prefix_gap)
        footer = struct.pack("<II", extension_offset, 0) + b"TRUEVISION-XFILE.\x00"
        original = tga(3, 2, payload, kind=10,
                       trailer=prefix_gap + extension + footer)
        changed = flip_horizontal(original)
        before, after = decode(original), decode(changed)
        self.assertNotEqual(len(original), len(changed))
        self.assertEqual(normalized_metadata(before), normalized_metadata(after))
        updated_offset = struct.unpack_from("<I", changed, len(changed) - 26)[0]
        self.assertEqual(updated_offset, after.payload_end + len(prefix_gap))
        self.assertEqual(changed[updated_offset:updated_offset + 495], extension)
        self.assertEqual(after.trailing[:-26], before.trailing[:-26])
        twice = decode(flip_horizontal(changed))
        self.assertEqual(twice.pixels, before.pixels)
        self.assertEqual(normalized_metadata(twice), normalized_metadata(before))

    def test_nested_extension_tables_fail_closed(self):
        for pointer_offset in (482, 486, 490):
            extension = bytearray(495)
            struct.pack_into("<H", extension, 0, 495)
            struct.pack_into("<I", extension, pointer_offset, 25)
            footer = struct.pack("<II", 25, 0) + b"TRUEVISION-XFILE.\x00"
            original = tga(2, 1, b"\x81abc", kind=10, trailer=extension + footer)
            with self.assertRaisesRegex(ValueError, "nested"):
                flip_horizontal(original)

    def test_uncompressed_offset_bearing_metadata_is_preserved(self):
        footer = struct.pack("<II", 27, 0) + b"TRUEVISION-XFILE.\x00"
        data = tga(2, 1, b"abcdef", trailer=b"extension" + footer)
        output = flip_horizontal(data)
        self.assertEqual(len(output), len(data))
        self.assertEqual(output[27:], data[27:])

    def test_rejects_truncated_invalid_and_overlong_input(self):
        invalid = [b"", tga(1, 1, b"ab"), tga(1, 1, b"\x80ab", kind=10),
                   tga(1, 1, b"\x81abc", kind=10), tga(1, 1, b"", kind=10),
                   tga(0, 1, b""), tga(1, 1, b"abc", descriptor=64),
                   tga(1, 1, b"ab", depth=16), tga(1, 1, b"abc", kind=1)]
        for data in invalid:
            with self.subTest(data=data):
                with self.assertRaises(ValueError):
                    decode(data)
        with self.assertRaisesRegex(ValueError, "buffer length"):
            encode(decode(tga(1, 1, b"abc")), b"a")


if __name__ == "__main__":
    unittest.main()
