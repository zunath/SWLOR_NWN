"""Compile staged rigid weapons with CleanModels; keep maps/MTRs beside MDLs."""
import argparse
from pathlib import Path
import re
import shutil
import sys
import tempfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "SWLOR_Haks/tools"))
import CleanModels


def compile_models(compiler, resources, output, names):
    compiler = CleanModels.find_compiler(compiler)
    resources, output = (Path(p).resolve() for p in (resources, output))
    if not resources.is_dir():
        raise ValueError("A staged resource directory is required")
    for name in names:
        if not re.fullmatch(r"[a-zA-Z0-9_]{1,16}", name):
            raise ValueError(f"Invalid model resource: {name}")
    models = [resources / (name + ".mdl") for name in names] if names else sorted(resources.glob("*.mdl"))
    if not models:
        raise ValueError("No input models")
    for model in models:
        if not re.fullmatch(r"[a-zA-Z0-9_]{1,16}", model.stem) or not model.is_file():
            raise ValueError(f"Invalid model resource: {model}")
        if model.read_bytes()[:4] == bytes(4):
            raise ValueError(f"Already binary; use a local ASCII export: {model.name}")
        if (output / model.name).exists():
            raise FileExistsError(f"Use a fresh output directory: {output / model.name}")
    output.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="cleanmodels-") as temporary:
        staging = Path(temporary)
        binary = staging / "binary"
        binary.mkdir()
        try:
            CleanModels.run(compiler, staging, "compile", models, binary, "compile.log", [resources])
        finally:
            if (staging / "compile.log").exists():
                shutil.copyfile(staging / "compile.log", output / "compile.log")
        for model in models:
            CleanModels.validate_binary(binary / model.name)
        # Publish only after every compile succeeds. Logs remain local diagnostics.
        for model in models:
            shutil.copyfile(binary / model.name, output / model.name)
            print(f"Compiled {model.name}: {model.stat().st_size:,} -> {(output / model.name).stat().st_size:,} bytes")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--compiler", help="Pinned cleanmodels.exe (default: CLEANMODELS, local install, or PATH)")
    parser.add_argument("--resources", required=True, help="Staged ASCII MDLs and material/texture dependencies")
    parser.add_argument("--output", required=True, help="Fresh local output directory")
    parser.add_argument("--models", nargs="*", default=[], help="Optional extensionless model resrefs")
    args = parser.parse_args()
    compile_models(args.compiler, args.resources, args.output, args.models)
