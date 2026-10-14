"""Remove the authored semantic frame and dark matte from inventory TGAs.

Action/cooldown resources are deliberately excluded. RGB artwork is preserved;
only alpha changes. Compatibility aliases and their hashes stay synchronized.
"""
import argparse
import csv
import hashlib
from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter


def boxed(image):
    if image.size != (64, 64):
        return False
    pixels = image.load()
    # The shared frame has a continuous, light inset rail on all four sides.
    rail = [pixels[x, 2] for x in range(8, 56)]
    rail += [pixels[x, 61] for x in range(8, 56)]
    rail += [pixels[2, y] for y in range(8, 56)]
    rail += [pixels[61, y] for y in range(8, 56)]
    return all(p[3] == 255 and max(p[:3]) > 80 for p in rail) and len({p[:3] for p in rail}) <= 8


def cutout(image):
    result = image.copy()
    pixels = result.load()
    width, height = result.size
    queue = deque()
    seen = set()
    for y in range(height):
        for x in range(width):
            if min(x, y, width - 1 - x, height - 1 - y) < 6:
                r, g, b, _ = pixels[x, y]
                pixels[x, y] = (r, g, b, 0)
                queue.append((x, y))
                seen.add((x, y))
    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if not (0 <= nx < width and 0 <= ny < height) or (nx, ny) in seen:
                continue
            seen.add((nx, ny))
            r, g, b, a = pixels[nx, ny]
            if max(r, g, b) <= 24 or (max(r, g, b) <= 40 and max(r, g, b) - min(r, g, b) <= 6):
                pixels[nx, ny] = (r, g, b, 0)
                queue.append((nx, ny))
    # Close narrow dark seams before restoring enclosed dark object surfaces.
    # Otherwise a black vent can connect the matte to the item's whole body.
    mask = result.getchannel('A').filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3))
    exterior = Image.new('L', result.size, 0)
    mp, ep = mask.load(), exterior.load()
    queue = deque([(0, 0)])
    ep[0, 0] = 255
    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < width and 0 <= ny < height and not ep[nx, ny] and not mp[nx, ny]:
                ep[nx, ny] = 255
                queue.append((nx, ny))
    for y in range(height):
        for x in range(width):
            r, g, b, _ = pixels[x, y]
            pixels[x, y] = (r, g, b, 0 if ep[x, y] else 255)
    return result


def save_alpha(path, image):
    data = bytearray(path.read_bytes())
    if data[2] != 2 or data[16] != 32 or data[17] != 8:
        raise ValueError(f"Unsupported TGA layout: {path}")
    width, height = image.size
    pixels = image.load()
    offset = 18 + data[0]
    for y in range(height):
        for x in range(width):
            data[offset + ((height - 1 - y) * width + x) * 4 + 3] = pixels[x, y][3]
    path.write_bytes(data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    haks = root / 'SWLOR_Haks'
    changed = []
    for path in sorted((haks / 'sw_item').glob('*.tga')):
        with Image.open(path) as original:
            image = original.convert('RGBA')
        if not path.stem.startswith('iit_ess') or not boxed(image):
            continue
        changed.append(path)
        if args.apply:
            save_alpha(path, cutout(image))
    if args.apply:
        library = haks / 'sw_item_source/general/native-library.csv'
        with library.open(newline='', encoding='utf-8-sig') as stream:
            reader = csv.DictReader(stream)
            fields = reader.fieldnames
            rows = list(reader)
        for row in rows:
            row['SHA256'] = hashlib.sha256((haks / row['Resource']).read_bytes()).hexdigest()
        with library.open('w', newline='', encoding='utf-8') as stream:
            writer = csv.DictWriter(stream, fields)
            writer.writeheader()
            writer.writerows(rows)
    print(f'{len(changed)} boxed inventory icons' + (' normalized' if args.apply else ' found'))


if __name__ == '__main__':
    main()
