"""Stage DDS portraits without modifying their TGA source directory.

Requires Pillow and ImageMagick. Every portrait gets one DDS level and a
matching mipmap-0 TXI. Review the staged pack before replacing source assets.
"""
import argparse
from concurrent.futures import ThreadPoolExecutor
import csv
import hashlib
from pathlib import Path
import struct
import subprocess
import tempfile

from PIL import Image, ImageStat
from portrait_tga import decode


def digest(data):
    return hashlib.sha256(data).hexdigest()


def convert(source, destination, scratch):
    data = source.read_bytes()
    image = decode(data)
    alpha = image.pixels[3::4] if image.bytes_per_pixel == 4 else b''
    transparent = bool(alpha) and min(alpha) < 255
    block_bytes = 16 if transparent else 8
    size = 128 + ((image.width + 3) // 4) * ((image.height + 3) // 4) * block_bytes
    mode = 'RGBA' if image.bytes_per_pixel == 4 else 'RGB'
    pixels = Image.frombytes(mode, (image.width, image.height), image.pixels,
                             'raw', 'BGRA' if mode == 'RGBA' else 'BGR')
    if not data[17] & 32:
        pixels = pixels.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    if data[17] & 16:
        pixels = pixels.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    rgb = pixels.convert('RGB')
    grid = bytearray()
    for y in range(8):
        for x in range(8):
            cell = rgb.crop((x * image.width // 8, y * image.height // 8,
                             (x + 1) * image.width // 8, (y + 1) * image.height // 8))
            grid.extend(int(value) // (cell.width * cell.height)
                        for value in ImageStat.Stat(cell).sum)
    # NWN samples standard DDS in bottom-up order. Its portrait loader and
    # toolset reverse these stored rows to recover the original visual facing.
    pixels = pixels.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    png = scratch / (source.stem + '.png')
    pixels.save(png)
    target = destination / (source.stem + '.dds')
    fmt = 'DXT5' if transparent else 'DXT1'
    subprocess.run(['magick', str(png), '-define', 'dds:compression=' + fmt.lower(),
                    '-define', 'dds:mipmaps=0', str(target)], check=True,
                   capture_output=True)
    raw = target.read_bytes()
    if (raw[:4] != b'DDS ' or raw[84:88] != fmt.encode() or len(raw) != size
            or struct.unpack_from('<II', raw, 12) != (image.height, image.width)):
        raise ValueError(f'Unexpected DDS encoding: {source.name}')
    header = bytearray(raw)
    struct.pack_into('<I', header, 8, struct.unpack_from('<I', raw, 8)[0] | 0x20000)
    struct.pack_into('<I', header, 28, 1)
    target.write_bytes(header)
    target.with_suffix('.txi').write_bytes(b'mipmap 0\n')
    with Image.open(target) as decoded:
        decoded.load()
        if decoded.size != pixels.size:
            raise ValueError(f'DDS dimensions changed: {source.name}')
        if not transparent and decoded.convert('RGBA').getchannel('A').getextrema() != (255, 255):
            raise ValueError(f'DDS introduced transparency: {source.name}')
    return dict(file=source.name, source_sha256=digest(data), output=target.name,
                output_sha256=digest(header), width=image.width, height=image.height,
                format=fmt, source_bytes=len(data), output_bytes=len(header),
                source_rgb_grid=grid.hex())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True,
                        help='New staging directory; must not already exist')
    args = parser.parse_args()
    files = sorted(args.source.glob('*.tga'))
    if not files or len(files) != len(list(args.source.iterdir())):
        raise ValueError('Source must contain only the complete original TGA pack')
    if len({p.stem.lower() for p in files}) != len(files):
        raise ValueError('Duplicate resource names')
    args.output.mkdir(parents=True, exist_ok=False)
    with tempfile.TemporaryDirectory(prefix='portrait-dds-') as temporary:
        scratch = Path(temporary)
        with ThreadPoolExecutor(max_workers=8) as pool:
            rows = list(pool.map(lambda p: convert(p, args.output, scratch), files))
    manifest = args.output.parent / (args.output.name + '-conversions.csv')
    if not rows:
        raise ValueError('No portraits qualified for conversion')
    with manifest.open('x', newline='', encoding='utf-8') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)
    print(f'Staged {len(rows)} DDS/TXI pairs.')
    print(f'Manifest: {manifest}')


if __name__ == '__main__':
    main()
