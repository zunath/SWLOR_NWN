"""Run the same prompts through the local model and GPT Image, then build a side-by-side report.

GPT sources (--gpt):
  api    OpenAI Images API. Needs a real OPENAI_API_KEY (billed per image).
  codex  Paste each printed prompt into the Codex app; the script picks up each new image
         from ~/.codex/generated_images as it appears. No API key needed.
  folder Pair existing GPT images from --gpt-dir with the prompts, in file-name order.
  none   Local model only.
  auto   api when OPENAI_API_KEY looks real, otherwise codex.

Example:
    C:\\AI\\LocalImageGen\\venv\\Scripts\\python.exe tools\\LocalImageGen\\compare_image_models.py \\
        --prompts-file C:\\AI\\LocalImageGen\\prompts.txt --gpt codex
"""

from __future__ import annotations

import argparse
import base64
import html
import json
import os
import shutil
import time
import urllib.error
import urllib.request
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from local_image_gen import DEFAULT_HOME, MODEL_ID, LocalImageGenerator, parse_size

CODEX_IMAGES = Path(os.environ.get("CODEX_HOME") or Path.home() / ".codex") / "generated_images"
IMAGE_SUFFIXES = {".png", ".jpg", ".jpeg", ".webp"}


def read_prompts(args) -> list[str]:
    prompts = list(args.prompt or [])
    if args.prompts_file:
        for line in args.prompts_file.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if line and not line.startswith("#"):
                prompts.append(line)
    if not prompts:
        raise SystemExit("Give --prompt or --prompts-file.")
    return prompts


def api_key() -> str | None:
    key = os.environ.get("OPENAI_API_KEY", "").strip()
    return key if key.startswith("sk-") else None


def validate_api_size(width: int, height: int) -> None:
    # GPT Image limits from OpenAI's image generation guide; checked up front so a bad
    # size fails before any local image is generated.
    pixels = width * height
    if not 655_360 <= pixels <= 8_294_400:
        raise SystemExit(f"--size {width}x{height} is {pixels} pixels; the GPT Image API needs 655,360 to 8,294,400.")
    if max(width, height) > 3840:
        raise SystemExit("The GPT Image API allows at most 3840 px per edge.")
    if max(width, height) > 3 * min(width, height):
        raise SystemExit("The GPT Image API allows aspect ratios up to 3:1.")


def gpt_from_api(prompt: str, size: str, model: str, quality: str, target: Path) -> float:
    key = api_key()
    if not key:
        raise SystemExit("OPENAI_API_KEY is missing or a placeholder; use --gpt codex or set a real key.")
    body = json.dumps({"model": model, "prompt": prompt, "size": size, "quality": quality, "n": 1}).encode()
    request = urllib.request.Request(
        "https://api.openai.com/v1/images/generations",
        data=body,
        headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json"},
    )
    started = time.perf_counter()
    try:
        with urllib.request.urlopen(request, timeout=600) as response:
            payload = json.load(response)
    except urllib.error.HTTPError as error:
        raise SystemExit(f"OpenAI API error {error.code}: {error.read().decode(errors='replace')}") from error
    target.write_bytes(base64.b64decode(payload["data"][0]["b64_json"]))
    return time.perf_counter() - started


def codex_images() -> set[Path]:
    if not CODEX_IMAGES.exists():
        return set()
    return {path for path in CODEX_IMAGES.rglob("*") if path.suffix.lower() in IMAGE_SUFFIXES}


def choose_codex_image(fresh: list[Path]) -> Path | None:
    """Other Codex sessions write to the same folder, so the user confirms which file is this prompt's."""
    print("New Codex images:")
    for number, path in enumerate(fresh, start=1):
        print(f"  {number}. {path} ({datetime.fromtimestamp(path.stat().st_mtime):%H:%M:%S})")
    while True:
        answer = input("Number of this prompt's image (Enter to keep waiting): ").strip()
        if not answer:
            return None
        if answer.isdigit() and 1 <= int(answer) <= len(fresh):
            return fresh[int(answer) - 1]


def gpt_from_codex(prompt: str, size: str, target: Path, timeout: int) -> Path:
    known = codex_images()
    print("\n" + "=" * 78)
    print("Paste this into the Codex app (any thread) and let it generate one image:\n")
    print(f"Generate one {size} image with your image generation tool. Prompt: {prompt}")
    print("=" * 78)
    print(f"Waiting up to {timeout}s for a new image in {CODEX_IMAGES} ...")
    started = time.perf_counter()
    while time.perf_counter() - started < timeout:
        fresh = sorted(codex_images() - known, key=lambda path: path.stat().st_mtime)
        if fresh:
            time.sleep(1)  # let the writer finish
            chosen = choose_codex_image(fresh)
            if chosen:
                copy = target.with_suffix(chosen.suffix.lower())
                shutil.copyfile(chosen, copy)
                return copy
            known.update(fresh)
        time.sleep(2)
    raise SystemExit("Timed out waiting for a Codex image.")


def new_output_dir() -> Path:
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    for suffix in range(100):
        candidate = DEFAULT_HOME / "comparisons" / (stamp if suffix == 0 else f"{stamp}-{suffix}")
        try:
            candidate.mkdir(parents=True)
            return candidate
        except FileExistsError:
            continue
    raise SystemExit("Could not create a unique comparison folder.")


def label_tile(image: Image.Image, label: str, tile: int) -> Image.Image:
    image = image.convert("RGB")
    image.thumbnail((tile, tile))
    canvas = Image.new("RGB", (tile, tile + 36), (24, 24, 28))
    canvas.paste(image, ((tile - image.width) // 2, 36 + (tile - image.height) // 2))
    try:
        font = ImageFont.truetype("arial.ttf", 20)
    except OSError:
        font = ImageFont.load_default()
    ImageDraw.Draw(canvas).text((10, 7), label, fill=(235, 235, 235), font=font)
    return canvas


def side_by_side(paths: list[tuple[str, Path]], target: Path, tile: int = 768) -> None:
    tiles = [label_tile(Image.open(path), label, tile) for label, path in paths]
    sheet = Image.new("RGB", (tile * len(tiles) + 8 * (len(tiles) - 1), tiles[0].height), (60, 60, 66))
    for index, part in enumerate(tiles):
        sheet.paste(part, (index * (tile + 8), 0))
    sheet.save(target)


def write_report(out: Path, rows: list[dict], meta: dict) -> Path:
    cards = []
    for row in rows:
        cells = []
        for key, label in (("local", meta["local_label"]), ("gpt", meta["gpt_label"])):
            entry = row.get(key)
            if entry:
                timing = f'{entry["seconds"]:.1f}s' if entry["seconds"] is not None else ""
                cells.append(
                    f'<figure><figcaption>{html.escape(label)} <span>{timing}</span></figcaption>'
                    f'<a href="{entry["file"]}"><img src="{entry["file"]}" loading="lazy"></a></figure>'
                )
            else:
                cells.append(f'<figure><figcaption>{html.escape(label)}</figcaption><div class="missing">not generated</div></figure>')
        cards.append(
            f'<section><h2>{row["index"]:02d}. {html.escape(row["prompt"])}</h2>'
            f'<div class="pair">{"".join(cells)}</div>'
            f'<label class="pick">Better: <select><option></option><option>{html.escape(meta["local_label"])}</option>'
            f'<option>{html.escape(meta["gpt_label"])}</option><option>Tie</option></select></label></section>'
        )
    page = f"""<!doctype html><html><head><meta charset="utf-8"><title>Image model comparison</title>
<style>
body{{background:#16161a;color:#e6e6ea;font:15px/1.45 system-ui,sans-serif;margin:0;padding:24px}}
header p{{color:#a0a0aa}} section{{margin:28px 0;padding-top:16px;border-top:1px solid #33333a}}
h2{{font-size:16px;font-weight:600}} .pair{{display:grid;grid-template-columns:1fr 1fr;gap:16px}}
figure{{margin:0}} figcaption{{font-weight:600;margin-bottom:6px}} figcaption span{{color:#a0a0aa;font-weight:400}}
img{{width:100%;border-radius:6px;background:#000}} .missing{{aspect-ratio:1;display:grid;place-items:center;background:#222;border-radius:6px;color:#888}}
.pick{{display:block;margin-top:8px;color:#a0a0aa}} #tally{{font-weight:600}}
@media(max-width:700px){{.pair{{grid-template-columns:1fr}}}}
</style></head><body>
<header><h1>Image model comparison</h1>
<p>{html.escape(meta["created"])} &middot; size {meta["size"]} &middot; local seed {meta["seed"]} &middot; {html.escape(meta["local_detail"])}</p>
<p id="tally"></p></header>
{"".join(cards)}
<script>
const tally=()=>{{const c={{}};document.querySelectorAll('.pick select').forEach(s=>{{if(s.value)c[s.value]=(c[s.value]||0)+1}});
document.getElementById('tally').textContent=Object.keys(c).length?'Your picks: '+Object.entries(c).map(([k,v])=>k+' '+v).join(', '):'';}};
document.querySelectorAll('.pick select').forEach(s=>s.addEventListener('change',tally));
</script></body></html>"""
    target = out / "index.html"
    target.write_text(page, encoding="utf-8")
    return target


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--prompt", action="append", help="repeatable")
    parser.add_argument("--prompts-file", type=Path, help="one prompt per line; # comments allowed")
    parser.add_argument("--out", type=Path, help="default: <image home>\\comparisons\\<timestamp>")
    parser.add_argument("--size", default="1024x1024")
    parser.add_argument("--seed", default=42, type=int)
    parser.add_argument("--gpt", default="auto", choices=["auto", "api", "codex", "folder", "none"])
    parser.add_argument("--gpt-model", default="gpt-image-2")
    parser.add_argument("--gpt-quality", default="high")
    parser.add_argument("--gpt-dir", type=Path, help="existing GPT images for --gpt folder")
    parser.add_argument("--codex-timeout", default=900, type=int)
    parser.add_argument("--precision", default="auto", choices=["auto", "bf16", "int8", "nf4"])
    parser.add_argument("--offload", default="model", choices=["model", "sequential", "none"])
    args = parser.parse_args()

    prompts = read_prompts(args)
    width, height = parse_size(args.size)
    gpt_mode = args.gpt if args.gpt != "auto" else ("api" if api_key() else "codex")
    gpt_files: list[Path] = []
    if gpt_mode == "folder":
        if not args.gpt_dir:
            raise SystemExit("--gpt folder needs --gpt-dir.")
        gpt_files = sorted(p for p in args.gpt_dir.iterdir() if p.suffix.lower() in IMAGE_SUFFIXES)
        if len(gpt_files) < len(prompts):
            raise SystemExit(f"{args.gpt_dir} has {len(gpt_files)} images for {len(prompts)} prompts.")

    if gpt_mode == "api":
        validate_api_size(width, height)

    if args.out:
        out = args.out
        out.mkdir(parents=True, exist_ok=True)
    else:
        out = new_output_dir()

    print(f"Loading {MODEL_ID} ...")
    local = LocalImageGenerator(args.precision, args.offload)
    print(f"Loaded ({local.precision}, offload={args.offload}) in {local.load_seconds:.1f}s")

    meta = {
        "created": datetime.now().strftime("%Y-%m-%d %H:%M"),
        "size": args.size,
        "seed": args.seed,
        "local_label": "Z-Image-Turbo (local)",
        "local_detail": f"Z-Image-Turbo {local.precision}, 9 steps",
        "gpt_label": f"{args.gpt_model} (API)" if gpt_mode == "api" else "GPT Image (Codex)" if gpt_mode == "codex" else "GPT Image",
        "gpt_mode": gpt_mode,
    }
    rows = []
    for index, prompt in enumerate(prompts, start=1):
        stem = f"{index:02d}"
        row = {"index": index, "prompt": prompt}
        image, seconds = local.generate(prompt, width, height, args.seed)
        local_file = out / f"{stem}_local.png"
        image.save(local_file)
        row["local"] = {"file": local_file.name, "seconds": seconds}
        print(f"[{stem}] local {seconds:.1f}s -> {local_file}")

        if gpt_mode != "none":
            gpt_file = out / f"{stem}_gpt.png"
            if gpt_mode == "api":
                seconds = gpt_from_api(prompt, args.size, args.gpt_model, args.gpt_quality, gpt_file)
            elif gpt_mode == "codex":
                # Wall time here includes pasting the prompt, so it is not a model timing.
                gpt_file = gpt_from_codex(prompt, args.size, gpt_file, args.codex_timeout)
                seconds = None
            else:
                gpt_file = out / f"{stem}_gpt{gpt_files[index - 1].suffix.lower()}"
                shutil.copyfile(gpt_files[index - 1], gpt_file)
                seconds = None
            row["gpt"] = {"file": gpt_file.name, "seconds": seconds}
            side_by_side([(meta["local_label"], local_file), (meta["gpt_label"], gpt_file)], out / f"{stem}_side_by_side.png")
            print(f"[{stem}] gpt -> {gpt_file}")
        rows.append(row)
        (out / "manifest.json").write_text(json.dumps({"meta": meta, "rows": rows}, indent=2), encoding="utf-8")
        report = write_report(out, rows, meta)

    print(f"\nReport: {report}")


if __name__ == "__main__":
    main()
