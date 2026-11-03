"""Find and batch-extract resources from packed HAKs and the installed game's KEY/BIF data."""
import struct
import subprocess
from collections import defaultdict
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
ERF = REPO / "tools/SWLOR.CLI/nwn_erf.exe"

# KEY/BIF resource types the generator reads from the base game, by extension.
BASE_TYPES = {2002: "mdl", 3: "tga", 6: "plt", 2033: "dds", 2022: "txi"}


def read_key(path: Path):
    """(resource name with extension, BIF path, resource index) for every BASE_TYPES entry of a KEY file."""
    data = path.read_bytes()
    if data[:8] != b"KEY V1  ":
        raise ValueError(f"Not a KEY file: {path}")
    bif_count, key_count, file_table, key_table = struct.unpack_from("<IIII", data, 8)
    bifs = []
    for i in range(bif_count):
        _, name_offset, name_size, _ = struct.unpack_from("<IIHH", data, file_table + i * 12)
        relative = data[name_offset:name_offset + name_size].rstrip(b"\0").decode("ascii").replace("\\", "/")
        bifs.append(path.parent.parent / relative)
    for i in range(key_count):
        resref, restype, resource_id = struct.unpack_from("<16sHI", data, key_table + i * 22)
        extension = BASE_TYPES.get(restype)
        if extension:
            stem = resref.rstrip(b"\0").decode("ascii").lower()
            yield f"{stem}.{extension}",bifs[resource_id >> 20], resource_id & 0xFFFFF


def read_bif(path: Path, wanted: dict) -> dict:
    """{resource index: bytes} for the wanted {resource index: name} entries of a BIF."""
    result = {}
    with path.open("rb") as bif:
        header = bif.read(20)
        if header[:8] != b"BIFFV1  ":
            raise ValueError(f"Not a BIF file: {path}")
        count, _, table = struct.unpack_from("<III", header, 8)
        bif.seek(table)
        entries = bif.read(count * 16)
        for i in range(count):
            resource_id, offset, size, _ = struct.unpack_from("<IIII", entries, i * 16)
            index = resource_id & 0xFFFFF
            if index in wanted:
                bif.seek(offset)
                result[index] = bif.read(size)
    return result


class HakIndex:
    """Resource name -> HAK (or base game BIF); module HAK order decides which copy wins (first listed).

    HAKs always win over the installed game's data, as in game.
    """

    def __init__(self, hak_dir: Path, hak_order, nwn_root: Path = None):
        self.where = {}
        self.base = {}  # resource name -> (BIF path, resource index), for names no HAK has
        self.missing = [name for name in hak_order if not (hak_dir / f"{name}.hak").is_file()]
        for name in hak_order:
            hak = hak_dir / f"{name}.hak"
            if not hak.is_file():
                continue
            listing = subprocess.run([str(ERF), "-t", "-f", str(hak)], capture_output=True, text=True, check=True).stdout
            for resource in listing.split():
                self.where.setdefault(resource.lower(), hak)
        if nwn_root:
            for key in sorted((Path(nwn_root) / "data").glob("*.key")):
                for name, bif, index in read_key(key):
                    if name not in self.where:
                        self.where[name] = bif
                        self.base[name] = (bif, index)

    def exists(self, resname: str) -> bool:
        return resname.lower() in self.where

    def extract(self, resnames, dest: Path, chunk=300):
        """Extract every name that exists into dest; returns the extracted names."""
        dest.mkdir(parents=True, exist_ok=True)
        by_hak, by_bif = defaultdict(list), defaultdict(dict)
        for name in {n.lower() for n in resnames}:
            if name in self.where and not (dest / name).exists():
                if name in self.base:
                    bif, index = self.base[name]
                    by_bif[bif][index] = name
                else:
                    by_hak[self.where[name]].append(name)
        for hak, names in by_hak.items():
            for i in range(0, len(names), chunk):
                subprocess.run([str(ERF), "-x", "-f", str(hak), *names[i:i + chunk]], cwd=dest,
                               capture_output=True, check=True)
        for bif, wanted in by_bif.items():
            for index, data in read_bif(bif, wanted).items():
                (dest / wanted[index]).write_bytes(data)
        return [n for n in {n.lower() for n in resnames} if (dest / n).exists()]
