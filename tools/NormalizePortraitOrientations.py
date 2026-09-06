"""Apply or verify the reviewed portrait corrections using only the standard library.

The manifest fixes explicit resources; this tool never infers orientation from a
filename or guesses which artwork is canonical. --apply accepts original or
already-corrected hashes and preflights every entry before writing anything.
"""
import argparse
import csv
import hashlib
from pathlib import Path

from portrait_tga import decode, flip_horizontal, normalized_metadata


def digest(data):
    return hashlib.sha256(data).hexdigest()


def validate_reflection(original, corrected):
    before, after = decode(original), decode(corrected)
    if before.prefix != after.prefix or (before.trailing != after.trailing
            and normalized_metadata(before) != normalized_metadata(after)):
        raise ValueError('TGA header, image ID, or trailing metadata changed')
    bpp = before.bytes_per_pixel
    stride = before.width * bpp
    for y in range(before.height):
        start = y * stride
        expected = b''.join(before.pixels[x:x+bpp]
                            for x in range(start+stride-bpp, start-1, -bpp))
        if after.pixels[start:start+stride] != expected:
            raise ValueError('Pixel data is not an exact horizontal reflection')


def main():
    repository = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--portraits', type=Path,
                        default=repository/'SWLOR_Haks/sw_portrait')
    parser.add_argument('--manifest', type=Path,
                        default=repository/'SWLOR_Haks/portrait_orientation_corrections.csv')
    parser.add_argument('--apply', action='store_true', help='Apply reviewed corrections; default is verification only')
    args = parser.parse_args()
    rows = list(csv.DictReader(args.manifest.open(newline='', encoding='utf-8')))
    pending = []
    seen = set()
    for row in rows:
        name = row['file']
        if Path(name).name != name or name.lower() in seen:
            raise ValueError(f'Unsafe or duplicate manifest filename: {name}')
        seen.add(name.lower())
        path = args.portraits/name
        original = path.read_bytes()
        actual = digest(original)
        canonical = args.portraits/row['canonical']
        if digest(canonical.read_bytes()) != row['canonical_sha256']:
            raise ValueError(f'Canonical portrait changed: {canonical.name}')
        if actual == row['corrected_sha256']:
            continue
        if actual != row['original_sha256']:
            raise ValueError(f'Unexpected image content: {name}')
        if not args.apply:
            raise ValueError(f'Correction has not been applied: {name}')
        corrected = flip_horizontal(original)
        validate_reflection(original, corrected)
        if digest(corrected) != row['corrected_sha256']:
            raise ValueError(f'Correction does not match reviewed output: {name}')
        pending.append((path, original, corrected))
    for path, original, corrected in pending:
        if path.read_bytes() != original:
            raise ValueError(f'Image changed after preflight: {path.name}')
        temporary = path.with_suffix('.tga.portrait-tmp')
        created = False
        try:
            with temporary.open('xb') as stream:
                created = True
                stream.write(corrected)
            temporary.replace(path)
        finally:
            if created and temporary.exists():
                temporary.unlink()
    for row in rows:
        if digest((args.portraits/row['file']).read_bytes()) != row['corrected_sha256']:
            raise ValueError(f'Post-write verification failed: {row["file"]}')
    print(f'Verified {len(rows)} portrait corrections; applied {len(pending)} lossless flips.')


if __name__ == '__main__':
    main()
