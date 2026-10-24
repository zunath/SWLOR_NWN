"""Generate images locally with Z-Image-Turbo (Apache 2.0) through diffusers.

Run with the venv that Setup-LocalImageGen.ps1 creates:

    C:\\AI\\LocalImageGen\\venv\\Scripts\\python.exe tools\\LocalImageGen\\local_image_gen.py \\
        --prompt "a glowing blue kyber crystal on a stone pedestal" --out crystal.png
"""

from __future__ import annotations

import argparse
import os
import time
from pathlib import Path

DEFAULT_HOME = Path(os.environ.get("SWLOR_LOCAL_IMAGE_HOME", r"C:\AI\LocalImageGen"))
MODEL_ID = "Tongyi-MAI/Z-Image-Turbo"

# Weights download to the image home, not the user's C: drive Hugging Face cache.
os.environ.setdefault("HF_HOME", str(DEFAULT_HOME / "hf-cache"))
os.environ.setdefault("HF_HUB_DISABLE_TELEMETRY", "1")

try:
    # Use the Windows certificate store so TLS-inspecting proxies do not break Hub downloads.
    import truststore

    truststore.inject_into_ssl()
except ImportError:
    pass

import torch  # noqa: E402


def free_vram_gb() -> float:
    free, _ = torch.cuda.mem_get_info()
    return free / 1024**3


def resolve_precision(precision: str) -> str:
    if precision != "auto":
        return precision
    # The bf16 transformer alone is about 12 GB. On a 12 GB card it spills into shared
    # system memory and a single image takes many minutes, so only use it on 20 GB+ cards.
    return "bf16" if free_vram_gb() >= 20.0 else "nf4"


class LocalImageGenerator:
    """Loads the pipeline once and generates any number of images."""

    def __init__(self, precision: str = "auto", offload: str = "model"):
        from diffusers import ZImagePipeline

        if not torch.cuda.is_available():
            raise SystemExit("CUDA is not available; the local image model needs the NVIDIA GPU.")

        self.precision = resolve_precision(precision)
        quantization = None
        if self.precision in ("int8", "nf4"):
            from diffusers.quantizers import PipelineQuantizationConfig

            if self.precision == "int8":
                backend, kwargs = "bitsandbytes_8bit", {"load_in_8bit": True}
            else:
                backend, kwargs = "bitsandbytes_4bit", {
                    "load_in_4bit": True,
                    "bnb_4bit_quant_type": "nf4",
                    "bnb_4bit_compute_dtype": torch.bfloat16,
                }
            quantization = PipelineQuantizationConfig(
                quant_backend=backend,
                quant_kwargs=kwargs,
                components_to_quantize=["transformer", "text_encoder"],
            )

        started = time.perf_counter()
        # Quantizing on load takes minutes, so keep a quantized copy and load that next time.
        cached = DEFAULT_HOME / "models" / f"z-image-turbo-{self.precision}"
        if quantization is not None and (cached / "model_index.json").exists():
            self.pipe = ZImagePipeline.from_pretrained(cached, torch_dtype=torch.bfloat16)
        else:
            self.pipe = ZImagePipeline.from_pretrained(
                MODEL_ID,
                torch_dtype=torch.bfloat16,
                quantization_config=quantization,
            )
            if quantization is not None:
                self.pipe.save_pretrained(cached)
        self.offload = offload
        if offload == "model":
            self.pipe.enable_model_cpu_offload()
        elif offload == "sequential":
            self.pipe.enable_sequential_cpu_offload()
        else:
            self.pipe.to("cuda")
        self.load_seconds = time.perf_counter() - started

    def generate(self, prompt: str, width: int, height: int, seed: int, steps: int = 9):
        started = time.perf_counter()
        image = self.pipe(
            prompt=prompt,
            width=width,
            height=height,
            num_inference_steps=steps,
            guidance_scale=0.0,  # Turbo is distilled; guidance must stay 0.
            generator=torch.Generator("cuda").manual_seed(seed),
        ).images[0]
        return image, time.perf_counter() - started


def parse_size(value: str) -> tuple[int, int]:
    width, height = (int(part) for part in value.lower().split("x"))
    if width % 16 or height % 16:
        raise argparse.ArgumentTypeError("width and height must be multiples of 16")
    return width, height


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--prompt", required=True)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--size", default="1024x1024", type=parse_size)
    parser.add_argument("--seed", default=42, type=int)
    parser.add_argument("--steps", default=9, type=int)
    parser.add_argument("--count", default=1, type=int, help="images to make, seeds seed..seed+count-1")
    parser.add_argument("--precision", default="auto", choices=["auto", "bf16", "int8", "nf4"])
    parser.add_argument("--offload", default="model", choices=["model", "sequential", "none"])
    args = parser.parse_args()

    generator = LocalImageGenerator(args.precision, args.offload)
    print(f"Loaded {MODEL_ID} ({generator.precision}, offload={args.offload}) in {generator.load_seconds:.1f}s")
    args.out.parent.mkdir(parents=True, exist_ok=True)
    width, height = args.size
    for index in range(args.count):
        seed = args.seed + index
        image, seconds = generator.generate(args.prompt, width, height, seed, args.steps)
        target = args.out if args.count == 1 else args.out.with_stem(f"{args.out.stem}_{seed}")
        image.save(target)
        print(f"{target} seed={seed} {seconds:.1f}s")


if __name__ == "__main__":
    main()
