"""Audit tileset minimap dependencies using module HAK precedence and installed game data.

The Git index supplies files omitted by a sparse HAK checkout; working files win.
Reports missing references without assuming that a white snow tile is broken.
"""
import argparse
from collections import defaultdict
import hashlib
import io
import json
from pathlib import Path
import re
import struct
import subprocess

from PIL import Image

from appearance_previews.resources import read_key, BASE_TYPES
from appearance_previews.tint import bioware_dds

ROOT = Path(__file__).resolve().parents[1]
EMPTY = {"", "null", "****"}
BASE_TYPES.update({2013: "set", 2016: "wok", 2072: "mtr"})


def sections(data):
    result, current = {}, None
    for line in data.decode("latin1").splitlines():
        line = line.strip()
        if line.startswith("[") and "]" in line:
            current = line[1:line.index("]")].lower()
            result[current] = {}
        elif "=" in line and current:
            key, value = line.split("=", 1)
            result[current][key.lower()] = value.strip()
    return result


def replace_images(data, replacements):
    """Change only specified TILE ImageMap2D fields, preserving the other SET bytes."""
    for tile, image in replacements.items():
        if not re.fullmatch(r"[a-z0-9_]{1,16}", image):
            raise ValueError(f"Invalid minimap resref: {image}")
        pattern = rb"(?ims)(^\[TILE" + str(tile).encode("ascii") + rb"\][^\r\n]*\r?\n)(.*?)(?=^\[|\Z)"

        def replace(match):
            field_pattern = rb"(?im)^([ \t]*ImageMap2D[ \t]*=)[^\r\n]*"
            body, count = re.subn(field_pattern, lambda old: old[1] + image.encode("ascii"), match[2])
            if count > 1:
                raise ValueError(f"Duplicate ImageMap2D in TILE{tile}")
            if not count:
                newline = b"\r\n" if b"\r\n" in match[0] else b"\n"
                if body and not body.endswith(b"\n"):
                    body += newline
                body += b"ImageMap2D=" + image.encode("ascii") + newline
            return match[1] + body

        data, count = re.subn(pattern, replace, data)
        if count != 1:
            raise ValueError(f"Expected one TILE{tile}, found {count}")
    return data


class Resources:
    def __init__(self, haks, module, game):
        self.haks = Path(haks)
        self.entries = {}
        self.cache = {}
        self.bif_tables = {}
        self.git = None
        for key in sorted((Path(game) / "data").glob("*.key")):
            for name, bif, index in read_key(key):
                self.entries[name] = ("bif", str(bif), index)
        if not self.entries:
            raise ValueError("No installed NWN KEY/BIF resources found")
        for archive in sorted((Path(game) / "data/txpk").glob("*.erf")):
            with archive.open("rb") as stream:
                header = stream.read(160)
                if header[:8] != b"ERF V1.0":
                    raise ValueError(f"Invalid texture archive: {archive}")
                count = struct.unpack_from("<I", header, 16)[0]
                stream.seek(struct.unpack_from("<I", header, 24)[0])
                keys = stream.read(count * 24)
                stream.seek(struct.unpack_from("<I", header, 28)[0])
                table = stream.read(count * 8)
                for i in range(count):
                    name, resource_id, kind, _ = struct.unpack_from("<16sIHH", keys, i * 24)
                    extension = BASE_TYPES.get(kind)
                    if extension:
                        name = name.split(b"\0", 1)[0].decode("ascii").lower()
                        offset, size = struct.unpack_from("<II", table, resource_id * 8)
                        self.entries[f"{name}.{extension}"] = ("archive", str(archive), offset, size)
        for file in (Path(game) / "ovr").rglob("*"):
            if file.is_file():
                self.entries[file.name.lower()] = ("file", str(file))
        listing = subprocess.check_output(
            ["git", "-C", str(self.haks), "ls-tree", "-r", "HEAD"], text=True)
        deleted = set(subprocess.check_output(
            ["git", "-C", str(self.haks), "ls-files", "--deleted"], text=True).splitlines())
        tracked = defaultdict(dict)
        for line in listing.splitlines():
            info, path = line.split("\t", 1)
            parts = Path(path).parts
            if len(parts) == 2 and path not in deleted:
                tracked[parts[0]][parts[1].lower()] = ("git", path, info.split()[2])
        ifo = json.loads(Path(module).read_bytes().decode("latin1"))
        self.hak_order = [entry["Mod_Hak"]["value"] for entry in ifo["Mod_HakList"]["value"]]
        for hak in reversed(self.hak_order):
            self.entries.update(tracked[hak])
            for file in (self.haks / hak).glob("*"):
                if file.is_file():
                    self.entries[file.name.lower()] = ("file", str(file))

    def close(self):
        if self.git:
            self.git.stdin.close()
            self.git.wait(timeout=10)
            self.git.stdout.close()
            self.git = None

    def exists(self, name):
        return name.lower() in self.entries

    def read(self, name):
        name = name.lower()
        if name in self.cache:
            return self.cache[name]
        entry = self.entries.get(name)
        if entry is None:
            return None
        kind, path, *args = entry
        if kind == "file":
            data = Path(path).read_bytes()
        elif kind == "git":
            if not self.git:
                self.git = subprocess.Popen(["git", "-C", str(self.haks), "cat-file", "--batch"],
                                            stdin=subprocess.PIPE, stdout=subprocess.PIPE)
            self.git.stdin.write((args[0] + "\n").encode("ascii"))
            self.git.stdin.flush()
            header = self.git.stdout.readline().split()
            data = self.git.stdout.read(int(header[2]))
            self.git.stdout.read(1)
        else:
            with Path(path).open("rb") as stream:
                if kind == "bif":
                    if path not in self.bif_tables:
                        header = stream.read(20)
                        if header[:8] != b"BIFFV1  ":
                            raise ValueError(f"Invalid BIF: {path}")
                        count, _, offset = struct.unpack_from("<III", header, 8)
                        stream.seek(offset)
                        table = stream.read(count * 16)
                        self.bif_tables[path] = {
                            resource_id & 0xfffff: (start, size)
                            for resource_id, start, size, _ in struct.iter_unpack("<IIII", table)
                        }
                    offset, size = self.bif_tables[path][args[0]]
                else:
                    offset, size = args
                stream.seek(offset)
                data = stream.read(size)
        self.cache[name] = data
        return data

    def texture_name(self, resref):
        if not resref or resref.lower() in EMPTY:
            return None
        return next((resref.lower() + ext for ext in (".dds", ".tga")
                     if self.exists(resref + ext)), None)

    def image(self, name):
        data = self.read(name)
        if data is None:
            raise FileNotFoundError(name)
        if name.lower().endswith(".dds") and data[:4] != b"DDS ":
            data = bioware_dds(data)
        image = Image.open(io.BytesIO(data)).convert("RGBA")
        if name.lower().endswith(".dds"):
            image = image.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
        return image


def audit(resources, inspect_images=False):
    tiles, sets, image_errors = [], [], []
    checked = set()
    for name in sorted(n for n in resources.entries if n.endswith(".set")):
        doc = sections(resources.read(name))
        owner = resources.entries[name]
        rows = []
        for section, fields in doc.items():
            if not re.fullmatch(r"tile\d+", section):
                continue
            image = fields.get("imagemap2d", "").lower()
            texture = resources.texture_name(image)
            row = {"tileset": name, "tile": int(section[4:]), "model": fields.get("model", "").lower(),
                   "image": image, "texture": texture, "source": owner[1]}
            rows.append(row)
            tiles.append(row)
            if inspect_images and texture and texture not in checked:
                checked.add(texture)
                try:
                    im = resources.image(texture)
                    if min(im.size) < 1 or im.getextrema()[3][1] == 0:
                        image_errors.append({"texture": texture, "error": "empty image"})
                except (OSError, ValueError) as error:
                    image_errors.append({"texture": texture, "error": str(error)})
        sets.append({"tileset": name, "source": owner[1], "name": doc.get("general", {}).get("unlocalizedname", ""),
                     "tiles": len(rows), "missing": sum(row["texture"] is None for row in rows)})
    missing = [row for row in tiles if row["texture"] is None]
    return {"sets": sets, "tiles": tiles, "missing": missing, "image_errors": image_errors,
            "summary": {"tilesets": len(sets), "tiles": len(tiles), "missing": len(missing),
                        "image_errors": len(image_errors)}}


def check_manifest(resources, manifest, result):
    problems = []
    actual_gaps = {(row["tileset"], row["tile"], row["model"], row["image"])
                   for row in result["missing"]}
    expected_gaps = {(row["tileset"], row["tile"], row["model"], row["image"])
                     for row in manifest["unrenderable"]}
    if actual_gaps != expected_gaps:
        problems.append("The unresolved tile list differs from the recorded missing-model definitions")
    for row in manifest["unrenderable"]:
        if resources.exists(row["model"] + ".mdl"):
            problems.append("Source model is now available: " + row["model"])
    for row in manifest["generated"]:
        name = Path(row["path"]).name
        data = resources.read(name)
        if data is None or hashlib.sha256(data).hexdigest() != row["sha256"]:
            problems.append("Changed or missing minimap: " + row["path"])
        if "source_sha256" in row:
            data = resources.read(row["model"] + ".mdl")
            if data is None or hashlib.sha256(data).hexdigest() != row["source_sha256"]:
                problems.append("Changed source model: " + row["model"])
        for name, expected in row["textures"].items():
            data = resources.read(name)
            if data is None or hashlib.sha256(data).hexdigest() != expected:
                problems.append("Changed source texture: " + name)
    tiles = {(row["tileset"], row["tile"]): row for row in result["tiles"]}
    for expected in manifest["references"]:
        row = tiles.get((expected["tileset"], expected["tile"]))
        if row is None or row["image"] != expected["image"] or row["model"] != expected["model"]:
            problems.append(f"Changed tile definition: {expected['tileset']} TILE{expected['tile']}")
    return sorted(set(problems))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--haks", type=Path, default=ROOT / "SWLOR_Haks")
    parser.add_argument("--module", type=Path, default=ROOT / "Module/ifo/module.ifo.json")
    parser.add_argument("--game", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--inspect-images", action="store_true")
    parser.add_argument("--manifest", type=Path, help="Verify generated hashes and the explicitly recorded missing-model gaps")
    args = parser.parse_args()
    resources = Resources(args.haks, args.module, args.game)
    try:
        result = audit(resources, args.inspect_images)
        if args.manifest:
            result["manifest_errors"] = check_manifest(resources, json.loads(args.manifest.read_text()), result)
            result["summary"]["manifest_errors"] = len(result["manifest_errors"])
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf8")
        print(json.dumps(result["summary"]))
        raise SystemExit(bool(result["image_errors"] or result.get("manifest_errors", [])
                              or (result["missing"] and not args.manifest)))
    finally:
        resources.close()


if __name__ == "__main__":
    main()
