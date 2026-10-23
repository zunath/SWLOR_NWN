# Local Image Generation

A local alternative to GPT Image: [Z-Image-Turbo](https://huggingface.co/Tongyi-MAI/Z-Image-Turbo) (Tongyi-MAI, 6B, Apache 2.0) run through diffusers on the local NVIDIA GPU. No API key, no per-image cost.

This is an evaluation tool. It does not change the sanctioned icon pipelines in `SWLOR.Game.Server/Readmes/IconStandards.md`.

## Setup

```powershell
powershell -NoProfile -File tools/LocalImageGen/Setup-LocalImageGen.ps1
```

Installs into `C:\AI\LocalImageGen` (override with `-ImageHome` or `SWLOR_LOCAL_IMAGE_HOME`): a Python 3.13 venv, CUDA PyTorch, diffusers, and about 20 GB of weights in `hf-cache`. Nothing is written to the repository.

## Generate

```powershell
C:\AI\LocalImageGen\venv\Scripts\python.exe tools/LocalImageGen/local_image_gen.py --prompt "..." --out C:\AI\LocalImageGen\out\test.png --count 4
```

`--count` makes seed variations. `--size` takes `WIDTHxHEIGHT` in multiples of 16.

## Compare with GPT

```powershell
C:\AI\LocalImageGen\venv\Scripts\python.exe tools/LocalImageGen/compare_image_models.py --prompts-file C:\AI\LocalImageGen\prompts.txt --gpt codex
```

The prompts file holds one prompt per line (`#` lines are skipped) and lives outside the repository. `--prompt "..."` (repeatable) works without a file.

Each prompt is generated locally, then paired with a GPT image. The run writes `index.html` (side by side, with a "Better" picker per prompt), `NN_side_by_side.png`, and `manifest.json` to `C:\AI\LocalImageGen\comparisons\<timestamp>`.

GPT sources:

| `--gpt` | How the GPT image is made |
| --- | --- |
| `codex` | The script prints each prompt; paste it into the Codex app. New images in `$CODEX_HOME/generated_images` (default `~/.codex`) are listed and you pick this prompt's image by number, since other Codex sessions write to the same folder. Headless `codex exec` does not expose image generation, so this step is manual. |
| `api` | OpenAI Images API (`--gpt-model`, default `gpt-image-2`; `--gpt-quality`, default `high`). Needs a real `OPENAI_API_KEY` and is billed per image. |
| `folder` | Pairs existing GPT images in `--gpt-dir` with the prompts in file-name order. |
| `none` | Local images only. |

`auto` (the default) uses `api` when `OPENAI_API_KEY` starts with `sk-`, otherwise `codex`.

## VRAM

The bf16 transformer alone is about 12 GB, so on a 12 GB card (RTX 3080 Ti) it spills into shared memory and stalls for many minutes per image. `--precision auto` uses 4-bit NF4 unless at least 20 GB of VRAM is free; NF4 runs at about 15 seconds per 1024x1024 image on the 3080 Ti with `--offload model` (default). Force a mode with `--precision bf16|int8|nf4`; the report header records which one ran.

The first quantized load takes about 5 minutes; the quantized pipeline is then saved to `<image home>\models\z-image-turbo-<precision>` and later loads read that copy. Other GPU jobs (ComfyUI, games) slow generation down or make it fail; close them first.
