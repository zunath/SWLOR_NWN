"""Lossless horizontal reflection of true-color portrait TGAs, without dependencies.

Pixels are BGR/BGRA bytes in the file's original scanline and pixel storage order.
No color conversion, alpha normalization, cropping, or origin-flag changes occur.
Type 10 output uses packets confined to scanlines; input packets may cross them.
TGA 2 extension pointers are relocated after recompression. Nested image tables
and developer directories fail closed rather than risking stale pointers.
"""

from dataclasses import dataclass
import struct


@dataclass(frozen=True)
class TgaImage:
    width: int
    height: int
    bytes_per_pixel: int
    image_type: int
    pixels: bytes
    prefix: bytes
    trailing: bytes
    payload_end: int


def decode(data: bytes) -> TgaImage:
    """Decode 24/32-bit, non-colormapped type 2/10 TGA; preserve framing bytes."""
    if len(data) < 18:
        raise ValueError("Truncated TGA header")
    id_length, color_map, image_type = data[:3]
    width, height = struct.unpack_from("<HH", data, 12)
    depth = data[16]
    if color_map != 0 or image_type not in (2, 10) or depth not in (24, 32):
        raise ValueError("Only non-colormapped type 2/10, 24/32-bit TGA is supported")
    if not width or not height or data[17] & 0xC0:
        raise ValueError("Empty or interleaved TGA is unsupported")
    bpp = depth // 8
    start = 18 + id_length
    if start > len(data):
        raise ValueError("Truncated TGA image ID")
    cursor = start
    expected = width * height * bpp
    if image_type == 2:
        cursor += expected
        if cursor > len(data):
            raise ValueError("Truncated TGA pixel data")
        pixels = data[start:cursor]
    else:
        pixels = bytearray()
        while len(pixels) < expected:
            if cursor >= len(data):
                raise ValueError("Truncated TGA RLE packet")
            packet = data[cursor]
            cursor += 1
            count = (packet & 127) + 1
            if len(pixels) + count * bpp > expected:
                raise ValueError("TGA RLE packet exceeds image dimensions")
            length = bpp if packet & 128 else count * bpp
            if cursor + length > len(data):
                raise ValueError("Truncated TGA RLE pixels")
            value = data[cursor:cursor + length]
            pixels.extend(value * count if packet & 128 else value)
            cursor += length
        pixels = bytes(pixels)
    return TgaImage(width, height, bpp, image_type, pixels,
                    data[:start], data[cursor:], cursor)


def _relocated_metadata(image: TgaImage, new_payload_end: int) -> bytes:
    """Relocate supported TGA 2 metadata while preserving every other byte.

The portrait corpus uses standalone 495-byte extensions with no nested tables.
Developer directories and nested image/table offsets need their own handling
and are deliberately rejected, including when recompression leaves size equal.
"""
    trailer = image.trailing
    if len(trailer) < 26 or trailer[-18:] != b"TRUEVISION-XFILE.\x00":
        return trailer
    footer = len(trailer) - 26
    extension, developer = struct.unpack_from("<II", trailer, footer)
    if developer:
        raise ValueError("TGA developer directory relocation is unsupported")
    if not extension:
        return trailer
    relative = extension - image.payload_end
    if relative < 0 or relative + 495 > footer:
        raise ValueError("TGA extension offset is outside trailing metadata")
    if struct.unpack_from("<H", trailer, relative)[0] != 495:
        raise ValueError("Only 495-byte TGA extensions are supported")
    if any(struct.unpack_from("<III", trailer, relative + 482)):
        raise ValueError("TGA nested color/postage/scanline table relocation is unsupported")
    adjusted = bytearray(trailer)
    struct.pack_into("<I", adjusted, footer, new_payload_end + relative)
    return bytes(adjusted)


def normalized_metadata(image: TgaImage) -> bytes:
    """Return metadata with supported absolute pointers made trailer-relative.

Compare this value before and after a flip to verify all metadata content is
unchanged despite a shifted RLE payload boundary. Unsupported metadata raises.
"""
    return _relocated_metadata(image, 0)


def _encode_rle(pixels: bytes, width: int, height: int, bpp: int) -> bytes:
    output = bytearray()
    for row in range(height):
        offset = row * width * bpp
        values = [pixels[offset + x * bpp:offset + (x + 1) * bpp]
                  for x in range(width)]
        x = 0
        while x < width:
            end = x + 1
            while end < min(x + 128, width) and values[end] == values[x]:
                end += 1
            if end - x > 1:
                output.append(128 | (end - x - 1))
                output.extend(values[x])
            else:
                while end < min(x + 128, width):
                    if end + 1 < width and values[end] == values[end + 1]:
                        break
                    end += 1
                output.append(end - x - 1)
                output.extend(b"".join(values[x:end]))
            x = end
    return bytes(output)


def encode(image: TgaImage, pixels: bytes | None = None) -> bytes:
    """Encode with original framing, relocating supported RLE metadata offsets."""
    pixels = image.pixels if pixels is None else pixels
    if len(pixels) != image.width * image.height * image.bytes_per_pixel:
        raise ValueError("Pixel buffer length does not match image dimensions")
    if image.image_type == 2:
        payload = pixels
    elif image.image_type == 10:
        payload = _encode_rle(pixels, image.width, image.height, image.bytes_per_pixel)
    else:
        raise ValueError("Unsupported image type")
    trailer = (_relocated_metadata(image, len(image.prefix) + len(payload))
               if image.image_type == 10 else image.trailing)
    return image.prefix + payload + trailer


def flip_horizontal(data: bytes) -> bytes:
    """Return exact horizontal pixel reflection, including portrait padding."""
    image = decode(data)
    bpp = image.bytes_per_pixel
    stride = image.width * bpp
    reflected = bytearray(len(image.pixels))
    for row in range(image.height):
        start = row * stride
        for channel in range(bpp):
            reflected[start + channel:start + stride:bpp] = \
                image.pixels[start + channel:start + stride:bpp][::-1]
    return encode(image, bytes(reflected))
