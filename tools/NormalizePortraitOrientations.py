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


# Independent of the companion CSV: a partial merge must not silently reduce
# the set of reviewed corrections. Update this only with a new reviewed batch.
EXPECTED_CORRECTION_COUNT = 256


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
    if len(rows) != EXPECTED_CORRECTION_COUNT:
        raise ValueError(f'Incomplete correction manifest: expected '
                         f'{EXPECTED_CORRECTION_COUNT} rows, found {len(rows)}')
    pending = []
    targets = {}
    for row in rows:
        name = row['file']
        if Path(name).name != name or name.lower() in targets:
            raise ValueError(f'Unsafe or duplicate manifest filename: {name}')
        if Path(row['canonical']).name != row['canonical']:
            raise ValueError(f'Unsafe canonical filename: {row["canonical"]}')
        targets[name.lower()] = row
    for row in rows:
        name = row['file']
        path = args.portraits/name
        original = path.read_bytes()
        actual = digest(original)
        canonical = args.portraits/row['canonical']
        canonical_actual = digest(canonical.read_bytes())
        canonical_target = targets.get(row['canonical'].lower())
        # A retained canonical can itself be in this repair batch. Accept its
        # original bytes only during replay when its reviewed output is the
        # required reference. Its own row still undergoes full preflight below.
        pending_canonical = (args.apply and canonical_target is not None
                             and canonical_actual == canonical_target['original_sha256']
                             and canonical_target['corrected_sha256'] == row['canonical_sha256'])
        if canonical_actual != row['canonical_sha256'] and not pending_canonical:
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
