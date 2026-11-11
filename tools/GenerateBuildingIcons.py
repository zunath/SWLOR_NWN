"""Export the reviewed building sources to native inventory DDS and framed action TGA."""
from pathlib import Path
import argparse
import json
import struct

import numpy as np
from PIL import Image

from FixWeaponInventoryIcons import _codec, thumbnail, tga_bytes, write_icon


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--haks', type=Path, default=Path('SWLOR_Haks'))
    args = parser.parse_args()
    source = args.haks / 'sw_item_source/buildings'
    assets = json.loads((source / 'prompts.json').read_text())['assets']
    codec = _codec()
    header = bytearray(128)
    header[:4] = b'DDS '
    for offset, value in {4: 124, 8: 0x81007, 12: 64, 16: 64, 20: 4096, 28: 1,
                          76: 32, 80: 4, 108: 0x1000}.items():
        struct.pack_into('<I', header, offset, value)
    header[84:88] = b'DXT5'
    info = {'header': bytes(header), 'size': (64, 64), 'fourcc': 'DXT5', 'mips': 1}
    frame = Image.open(args.haks / 'sw_ability/iit_building.tga').convert('RGBA')
    for asset in assets:
        image = Image.open(source / (asset['key'] + '.png')).convert('RGBA')
        alpha = image.getchannel('A').point(lambda value: value if value >= 16 else 0)
        image.putalpha(alpha)
        image = image.crop(alpha.getbbox())
        image.thumbnail((52, 52), Image.Resampling.LANCZOS)
        inventory = Image.new('RGBA', (64, 64))
        inventory.alpha_composite(image, ((64 - image.width) // 2, (64 - image.height) // 2))
        rgba = np.asarray(inventory, dtype=np.float32) / 255
        name = f"iit_midmisc_{asset['model']:03}"
        write_icon(codec, args.haks / 'sw_item' / (name + '.dds'), rgba, info)
        (args.haks / 'sw_item' / (name + '.tga')).write_bytes(tga_bytes(thumbnail(rgba)))
        action = frame.copy()
        action.paste(Image.new('RGBA', (24, 24), (11, 17, 24, 255)), (4, 4))
        image.thumbnail((22, 22), Image.Resampling.LANCZOS)
        action.alpha_composite(image, ((32 - image.width) // 2, (32 - image.height) // 2))
        (args.haks / 'sw_ability' / (asset['actionIcon'] + '.tga')).write_bytes(
            tga_bytes(np.asarray(action, dtype=np.float32) / 255))
    print(f'Exported {len(assets)} building icon families.')


if __name__ == '__main__':
    main()
