"""Export NWN gameplay DDS icons from retained lossless TGA production sources.

Uses ImageMagick for block compression. Never modifies source artwork. The CSV
records both hashes so audits reject stale DDS exports without recompressing art.
"""
import argparse
from concurrent.futures import ThreadPoolExecutor
import csv
import hashlib
import re
from pathlib import Path
import struct
import subprocess
import tempfile

from PIL import Image
from portrait_tga import decode


def sha(data):
    return hashlib.sha256(data).hexdigest()


def export(source, output, scratch, magick):
    raw = source.read_bytes()
    image = decode(raw)
    mode = 'RGBA' if image.bytes_per_pixel == 4 else 'RGB'
    pixels = Image.frombytes(mode, (image.width, image.height), image.pixels,
                             'raw', 'BGRA' if mode == 'RGBA' else 'BGR').convert('RGBA')
    if not raw[17] & 32:
        pixels = pixels.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    if raw[17] & 16:
        pixels = pixels.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    alpha = pixels.getchannel('A')
    fmt = 'DXT1' if alpha.getextrema() == (255, 255) else 'DXT5'
    target = output / (source.stem + '.dds')
    block_size = 8 if fmt == 'DXT1' else 16
    payload_size = ((image.width + 3) // 4) * ((image.height + 3) // 4) * block_size
    # NWN samples standard DDS bottom-up. Flip exactly once at the export boundary.
    png = scratch / (source.stem + '.png')
    pixels.transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(png)
    staged = scratch / target.name
    subprocess.run([magick, str(png), '-define', 'dds:compression=' + fmt.lower(),
                    '-define', 'dds:cluster-fit=true', '-define', 'dds:mipmaps=0', str(staged)], check=True, capture_output=True)
    data = bytearray(staged.read_bytes())
    if (data[:4] != b'DDS ' or data[84:88] != fmt.encode('ascii')
            or len(data) != 128 + payload_size
            or struct.unpack_from('<II', data, 12) != (image.height, image.width)):
        raise ValueError('Unexpected DDS encoding: ' + source.name)
    struct.pack_into('<I', data, 8, struct.unpack_from('<I', data, 8)[0] | 0x20000)
    struct.pack_into('<I', data, 28, 1)
    staged.write_bytes(data)
    with Image.open(staged) as decoded:
        visible = decoded.convert('RGBA').transpose(Image.Transpose.FLIP_TOP_BOTTOM)
        if visible.size != pixels.size:
            raise ValueError('DDS dimensions changed: ' + source.name)
        if fmt == 'DXT1' and visible.getchannel('A').getextrema() != (255, 255):
            raise ValueError('DDS introduced transparency: ' + source.name)
        # DXT5 must preserve endpoints, including clear legacy icon backgrounds.
        if fmt == 'DXT5' and visible.getchannel('A').getextrema() != alpha.getextrema():
            raise ValueError('DDS alpha endpoints changed: ' + source.name)
    staged.replace(target)
    target.with_suffix('.txi').write_bytes(b'mipmap 0\n')
    return dict(IconResRef=source.stem, SourceSHA256=sha(raw), DdsSHA256=sha(data),
                Width=image.width, Height=image.height, Format=fmt,
                SourceBytes=len(raw), DdsBytes=len(data), Encoder='ImageMagick cluster-fit v1')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--magick', default='magick')
    parser.add_argument('--force', action='store_true')
    args = parser.parse_args(argv)
    files = sorted(args.source.glob('*.tga'))
    if not files:
        raise ValueError('No lossless icon sources found: ' + str(args.source))
    names = {p.stem.lower() for p in files}
    if len(names) != len(files) or any(len(p.stem) > 16 for p in files):
        raise ValueError('Duplicate or overlength NWN resource names')
    if args.source.resolve() == args.output.resolve():
        raise ValueError('Lossless sources must be outside the runtime directory')
    args.output.mkdir(parents=True, exist_ok=True)
    if list(args.output.glob('*.tga')):
        raise ValueError('Runtime TGAs would shadow DDS; move them to the source directory first')
    previous = {}
    if args.manifest.exists():
        with args.manifest.open(newline='', encoding='utf-8') as stream:
            records = list(csv.DictReader(stream))
            previous = {r['IconResRef'].casefold(): r for r in records}
        if len(previous) != len(records):
            raise ValueError('Duplicate case-insensitive resource names in DDS export manifest')
        if any(not re.fullmatch(r"[a-zA-Z0-9_&'-]{1,16}", name) for name in previous):
            raise ValueError('Unsafe resource name in DDS export manifest')
    rows, pending = [], []
    for source in files:
        row = previous.get(source.stem.casefold())
        if row and row['IconResRef'] != source.stem:
            # Windows/NWN resolves names without casing. Rename the existing pair
            # together and retain the cache instead of retiring the same file.
            for extension in ('.dds', '.txi'):
                prior = args.output / (row['IconResRef'] + extension)
                if prior.exists():
                    prior.replace(args.output / (source.stem + extension))
            row = dict(row, IconResRef=source.stem)
        target = args.output / (source.stem + '.dds')
        txi = target.with_suffix('.txi')
        if (not args.force and row and row.get('Encoder') == 'ImageMagick cluster-fit v1' and row['SourceSHA256'] == sha(source.read_bytes())
                and target.exists() and row['DdsSHA256'] == sha(target.read_bytes())
                and txi.exists() and txi.read_bytes() == b'mipmap 0\n'):
            rows.append(row)
        else:
            pending.append(source)
    with tempfile.TemporaryDirectory(prefix='swlor-icon-dds-') as temporary:
        with ThreadPoolExecutor(max_workers=8) as pool:
            rows.extend(pool.map(lambda p: export(p, args.output, Path(temporary), args.magick), pending))
    # Retired source artwork must also retire its deployed texture and TXI.
    for key in set(previous) - names:
        name = previous[key]['IconResRef']
        for extension in ('.dds', '.txi'):
            (args.output / (name + extension)).unlink(missing_ok=True)
    args.manifest.parent.mkdir(parents=True, exist_ok=True)
    with args.manifest.with_suffix('.tmp').open('w', newline='', encoding='utf-8') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(sorted(rows, key=lambda r: r['IconResRef']))
    args.manifest.with_suffix('.tmp').replace(args.manifest)
    total_source = sum(int(r['SourceBytes']) for r in rows)
    total_dds = sum(int(r['DdsBytes']) + 9 for r in rows)
    print(f'Exported {len(pending)} DDS icons; {len(rows)} lossless sources retained.')
    print(f'Runtime textures plus TXI: {total_source:,} -> {total_dds:,} bytes; saved {total_source-total_dds:,}.')


if __name__ == '__main__':
    main()
