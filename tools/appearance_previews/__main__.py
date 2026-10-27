"""Generate Appearance Editor part pictures.

python tools/GenerateAppearancePreviews.py --hak-dir <packed haks> --output <hak source folder> --work <scratch>

Reads the parts the editor offers (armor for every body type, every species' heads, helmets,
cloaks), extracts each model and its tint material from the packed HAKs, renders front, back
and 3/4 views in Blender, and writes 96x96 DXT1 DDS files named a<key><f|b|a>.

Pictures of a non-human body part that look the same as the human body's picture are left
out; the game server falls back to the human picture when a species has none of its own.
Renders are cached in <work>/renders, so later runs only render parts without one.
"""
import argparse
import json
import os
import re
import shutil
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import numpy as np
from PIL import Image

from . import VIEWS, preview_resref
from .jobs import CLOAK_ATLAS_PREFIX, REPO, build
from .resources import HakIndex
from .tint import bake, cloak_tile, plain_texture, read_tint_map, save_for_blender

BACKGROUND = (60, 58, 55)
DEFAULT_BLENDER = r"C:\Program Files\Blender Foundation\Blender 4.0\blender.exe"


def module_haks():
    ifo = json.loads((REPO / "Module/ifo/module.ifo.json").read_text(encoding="latin-1"))
    return [h["Mod_Hak"]["value"] for h in ifo["Mod_HakList"]["value"]]


def cleanmodels():
    configured = os.environ.get("CLEANMODELS")
    installed = Path(os.environ.get("LOCALAPPDATA", "")) / "SWLOR/tools/cleanmodels-qt/1.1.7/cleanmodels-qt/cleanmodels.exe"
    path = Path(configured) if configured else installed
    if not path.is_file():
        raise SystemExit("CleanModels is missing. Run SWLOR_Haks/tools/SetupModelCompiler.ps1 or set CLEANMODELS.")
    return path


def decompile(models: Path, ascii_dir: Path):
    """ASCII copies of every extracted model (CleanModels decompile for binary ones)."""
    ascii_dir.mkdir(parents=True, exist_ok=True)
    binary_dir = models.parent / "binary"
    binary_dir.mkdir(exist_ok=True)
    pending = []
    for path in models.glob("*.mdl"):
        target = ascii_dir / path.name
        if target.exists():
            continue
        if path.read_bytes()[:4] == b"\0\0\0\0":
            link = binary_dir / path.name
            if not link.exists():
                os.link(path, link)
            pending.append(link)
        else:
            target.write_bytes(path.read_bytes())
    if pending:
        result = subprocess.run([str(cleanmodels()), "decompile", "--json", "--overwrite", str(binary_dir), str(ascii_dir)],
                                capture_output=True, text=True, encoding="utf8", errors="replace")
        (models.parent / "decompile.log").write_text(result.stdout + result.stderr, encoding="utf8")


def mesh_materials(ascii_path: Path, bitmap_fallbacks=None) -> dict:
    """Rendered mesh node -> material (materialname, else bitmap).

    Repeated node names get Blender's .001, .002 suffixes, in file order, as the importer names them.
    """
    result, node, seen = {}, None, {}
    bitmaps = bitmap_fallbacks if bitmap_fallbacks is not None else {}
    for line in ascii_path.read_text(errors="ignore").splitlines():
        parts = line.split()
        if not parts:
            continue
        if parts[0] == "node":
            node = None
            if len(parts) > 2 and parts[1] in ("trimesh", "skin", "danglymesh"):
                count = seen.get(parts[2].lower(), 0)
                seen[parts[2].lower()] = count + 1
                node = parts[2] if count == 0 else f"{parts[2]}.{count:03d}"
        elif node and parts[0] in ("materialname", "bitmap") and len(parts) > 1 and parts[1].lower() != "null":
            if parts[0] == "materialname" or node not in result:
                result[node] = parts[1].lower()
            if parts[0] == "bitmap":
                bitmaps[node] = parts[1].lower()
        elif parts[0] == "endnode":
            node = None
    return result


def prepare_textures(index: HakIndex, materials, work: Path, palette: Path) -> dict:
    """Material name -> baked texture paths."""
    raw, baked = work / "raw", work / "textures"
    baked.mkdir(parents=True, exist_ok=True)
    result = {}
    atlas_tiles = sorted(int(m[len(CLOAK_ATLAS_PREFIX):]) for m in materials if m.startswith(CLOAK_ATLAS_PREFIX))
    if atlas_tiles:
        atlas_names = [f"cloak_tints{i}.dds" for i in range(4)]
        index.extract(atlas_names, raw)
        atlases = [raw / name for name in atlas_names]
        for tile in atlas_tiles:
            material = f"{CLOAK_ATLAS_PREFIX}{tile}"
            color, metal = baked / f"cloaktint_{tile}_c.png", baked / f"cloaktint_{tile}_m.png"
            if not color.exists():
                save_for_blender(*bake(*cloak_tile(atlases, tile), palette), color, metal)
            result[material] = {"color": str(color), "metal": str(metal), "sources": atlas_names + [palette.name]}
    materials = [m for m in materials if not m.startswith(CLOAK_ATLAS_PREFIX)]
    index.extract([m + ".mtr" for m in materials], raw)
    sources = {}
    for material in materials:
        mtr = raw / f"{material}.mtr"
        tint = None
        if mtr.exists():
            found = re.search(r"^\s*texture7\s+(\S+)", mtr.read_text(errors="ignore"), re.M)
            tint = found[1].lower() if found else None
        sources[material] = [f"{tint}.dds", f"{tint}.tga"] if tint else []
        sources[material] += [f"{material}.plt", f"{material}.dds", f"{material}.tga"]
    index.extract([name for names in sources.values() for name in names], raw)
    for material, names in sources.items():
        color, metal = baked / f"{material}_c.png", baked / f"{material}_m.png"
        source = next((raw / n for n in names if (raw / n).exists()), None)
        if source is None:
            continue
        used = [f"{material}.mtr", source.name, palette.name]
        if color.exists():
            result[material] = {"color": str(color), "metal": str(metal) if metal.exists() else None, "sources": used}
            continue
        try:
            if source.name.startswith("tm_") or source.suffix == ".plt":
                rgb, env = bake(*read_tint_map(source), palette)
                save_for_blender(rgb, env, color, metal)
                result[material] = {"color": str(color), "metal": str(metal), "sources": used}
            else:
                plain_texture(source, color)
                result[material] = {"color": str(color), "metal": None, "sources": used}
        except (OSError, ValueError) as error:
            # Bioware-format DDS and other legacy textures Pillow cannot read render untextured.
            print("PREVIEW_UNREADABLE_TEXTURE", source.name, error)
    return result


# A picture whose mean channel difference from the human twin is below this is a duplicate.
DUPLICATE_THRESHOLD = 4.0
HUMAN_TWIN = re.compile(r"^p([fm])[deoz]0_(.+)$")


def picture(png: Path, size: int) -> Image.Image:
    image = Image.open(png).convert("RGBA")
    tile = Image.new("RGBA", image.size, BACKGROUND + (255,))
    tile.alpha_composite(image)
    return tile.convert("RGB").resize((size, size), Image.LANCZOS)


def human_twin(key: str):
    """The human body's key for a non-human body part key, or None."""
    match = HUMAN_TWIN.match(key)
    return f"p{match[1]}h0_{match[2]}" if match else None


def is_duplicate(image: Image.Image, twin: Image.Image) -> bool:
    difference = np.abs(np.asarray(image, np.int16) - np.asarray(twin, np.int16))
    return float(difference.mean()) < DUPLICATE_THRESHOLD


EMPTY_PICTURE = "apart_empty"  # mirrors AppearancePreview.EmptyPicture


def save_dds(image: Image.Image, path: Path):
    # NUI draws DDS rows bottom-up; store the picture flipped so it shows upright.
    image.transpose(Image.FLIP_TOP_BOTTOM).save(path, "DDS", pixel_format="DXT1")


def write_pictures(keys, renders: Path, output: Path, size: int):
    """Write every picture, skipping non-human pictures that duplicate the human one."""
    kept = skipped = 0
    save_dds(Image.new("RGB", (size, size), BACKGROUND), output / f"{EMPTY_PICTURE}.dds")
    wanted = {f"{EMPTY_PICTURE}.dds"}
    for key in keys:
        twin = human_twin(key)
        for view in VIEWS:
            png = renders / f"{key}_{view}.png"
            if not png.exists():
                continue
            image = picture(png, size)
            twin_png = renders / f"{twin}_{view}.png" if twin else None
            if twin_png is not None and twin_png.exists() and is_duplicate(image, picture(twin_png, size)):
                skipped += 1
                continue
            name = f"{preview_resref(key, view)}.dds"
            save_dds(image, output / name)
            wanted.add(name)
            kept += 1
    return kept, skipped, wanted


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--hak-dir", type=Path, required=True, help="Packed HAKs (e.g. debugserver/hak)")
    parser.add_argument("--output", type=Path, required=True, help="Folder that receives the DDS files")
    parser.add_argument("--work", type=Path, required=True, help="Scratch folder (models, textures, renders)")
    parser.add_argument("--two-da", type=Path, default=REPO / "SWLOR_Haks/sw_2da")
    parser.add_argument("--blender", default=os.environ.get("BLENDER", DEFAULT_BLENDER))
    parser.add_argument("--only", help="Regex on picture keys, for partial runs")
    parser.add_argument("--render-size", type=int, default=256)
    parser.add_argument("--size", type=int, default=96)
    parser.add_argument("--parallel", type=int, default=2)
    parser.add_argument("--batch", type=int, default=200)
    args = parser.parse_args(argv)

    index = HakIndex(args.hak_dir, module_haks())
    jobs = build(args.two_da, index.exists)
    if args.only:
        jobs = [j for j in jobs if re.search(args.only, j.key)]
    args.output.mkdir(parents=True, exist_ok=True)
    renders = args.work / "renders"
    all_keys = [j.key for j in jobs]
    cache = RenderCache(args.work, index)
    jobs = [j for j in jobs if not cache.is_fresh(j.key, renders)]
    print(f"{len(jobs)} parts to render")
    problems = render(args, index, jobs, renders, cache) if jobs else []
    cache.save()

    kept, skipped, wanted = write_pictures(all_keys, renders, args.output, args.size)
    if not args.only:
        for stale in args.output.glob("*.dds"):
            if stale.name not in wanted:
                stale.unlink()
    print(f"{kept} pictures written, {skipped} duplicates of the human body skipped, {len(problems)} problems")
    return 1 if problems else 0


def batch_problems(returncode, stdout, stderr, batch, renders):
    """Every reason a Blender batch did not produce all of its renders."""
    problems = [line for line in stdout.splitlines() if line.startswith(("PREVIEW_FAILED", "PREVIEW_NO_MESH"))]
    if returncode:
        tail = (stderr or stdout).strip().splitlines()[-1:] or [""]
        problems.append(f"PREVIEW_BATCH_FAILED exit {returncode}: {tail[0]}")
    reported = {line.split()[1] for line in problems if line.startswith(("PREVIEW_FAILED", "PREVIEW_NO_MESH"))}
    for job in batch:
        if job["key"] not in reported and not all((renders / f"{job['key']}_{view}.png").exists() for view in VIEWS):
            problems.append(f"PREVIEW_MISSING_RENDER {job['key']}")
    return problems


class RenderCache:
    """Remembers which HAKs each cached render came from.

    A render is reused only while every HAK it was made from is unchanged (size and
    modification time). Extracted models and baked textures are cleared whenever any HAK
    changes, since they are cheap to rebuild and are shared between parts.
    """

    FILE = "render_cache.json"
    ANY = "*"  # the render depends on a resource that was missing; any HAK change may add it

    def __init__(self, work: Path, index: HakIndex):
        self.path = work / self.FILE
        self.index = index
        self.current = {hak.name: f"{hak.stat().st_size}:{hak.stat().st_mtime_ns}"
                        for hak in sorted(set(index.where.values()), key=lambda h: h.name)}
        try:
            state = json.loads(self.path.read_text())
        except (OSError, ValueError):
            state = {}
        self.previous = state.get("haks", {})
        self.dependencies = state.get("renders", {})
        if self.previous != self.current:
            for folder in ("models", "binary", "ascii", "raw", "textures"):
                shutil.rmtree(work / folder, ignore_errors=True)

    def is_fresh(self, key: str, renders: Path) -> bool:
        haks = self.dependencies.get(key)
        if not haks or not all((renders / f"{key}_{view}.png").exists() for view in VIEWS):
            return False
        if self.ANY in haks:
            return self.previous == self.current
        return all(self.previous.get(hak) is not None and self.previous.get(hak) == self.current.get(hak) for hak in haks)

    def record(self, key: str, resources, complete: bool):
        haks = {self.index.where[name].name for name in resources if name in self.index.where}
        if not complete:
            haks.add(self.ANY)
        self.dependencies[key] = sorted(haks)

    def forget(self, key: str):
        self.dependencies.pop(key, None)

    def save(self):
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.path.write_text(json.dumps({"haks": self.current, "renders": self.dependencies}, indent=0))


def render(args, index, jobs, renders, cache):

    models = args.work / "models"
    index.extract([j.model + ".mdl" for j in jobs], models)
    decompile(models, args.work / "ascii")
    palette = args.work / "raw" / "plt_palette.tga"
    index.extract(["plt_palette.tga"], palette.parent)

    batch_jobs, materials, problems = [], set(), []
    for job in jobs:
        cache.forget(job.key)
        ascii_path = args.work / "ascii" / f"{job.model}.mdl"
        if not ascii_path.exists():
            # Extraction or CleanModels decompile failed; see decompile.log in the work folder.
            problems.append(f"PREVIEW_NO_MODEL {job.key}")
            continue
        bitmaps = {}
        nodes = mesh_materials(ascii_path, bitmaps)
        if "*" in job.materials:
            nodes = {node: job.materials["*"] for node in nodes}
            bitmaps = {}
        materials.update(nodes.values())
        materials.update(bitmaps.values())
        batch_jobs.append({"key": job.key, "ascii": str(ascii_path), "nodes": nodes, "bitmaps": bitmaps,
                           "model": f"{job.model}.mdl"})
    textures = prepare_textures(index, materials, args.work, palette)
    for job in batch_jobs:
        # A material without resources falls back to the node's own bitmap texture.
        for node, material in list(job["nodes"].items()):
            fallback = job.get("bitmaps", {}).get(node)
            if material not in textures and fallback in textures:
                job["nodes"][node] = fallback
        job.pop("bitmaps", None)
        job["textures"] = {m: dict(textures[m]) for m in set(job["nodes"].values()) if m in textures}
        job["sources"] = [job.pop("model")] + [name for m in job["textures"].values() for name in m.pop("sources", [])]
        job["complete"] = all(m in job["textures"] for m in set(job["nodes"].values()))

    script = Path(__file__).with_name("blender_render.py")
    batches = [batch_jobs[i:i + args.batch] for i in range(0, len(batch_jobs), args.batch)]

    def run(numbered):
        number, batch = numbered
        batch_file = args.work / f"batch_{number:04d}.json"
        batch_file.write_text(json.dumps(batch))
        result = subprocess.run([args.blender, "-b", "--python-exit-code", "1", "--python", str(script), "--",
                                 str(batch_file), str(renders), str(args.render_size)],
                                capture_output=True, text=True, encoding="utf8", errors="replace")
        problems = batch_problems(result.returncode, result.stdout, result.stderr, batch, renders)
        for job in batch:
            if all((renders / f"{job['key']}_{view}.png").exists() for view in VIEWS):
                cache.record(job["key"], job["sources"], job["complete"])
        print(f"batch {number + 1}/{len(batches)} done", *problems, sep="\n  ", flush=True)
        return problems

    with ThreadPoolExecutor(max_workers=args.parallel) as pool:
        return problems + [p for result in pool.map(run, enumerate(batches)) for p in result]


if __name__ == "__main__":
    sys.exit(main())
