"""Compile staged rigid blasters with NWN:EE; keep material maps beside input MDLs."""
import argparse
from pathlib import Path
import re
import shutil
import struct
import subprocess
import tempfile


def compile_models(nwmain, resources, output, names):
    nwmain, resources, output = (Path(p).resolve() for p in (nwmain, resources, output))
    if not nwmain.is_file() or not resources.is_dir():
        raise ValueError("An installed NWN:EE executable and resource directory are required")
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
    startup = None
    if hasattr(subprocess, "STARTUPINFO"):
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = 0
    # Each command compiles one model and exits; no client/server stays running.
    with tempfile.TemporaryDirectory(prefix="nwn-compile-", dir=output.parent) as temporary:
        user = Path(temporary)
        development = user / "development"
        development.mkdir()
        for resource in resources.iterdir():
            if resource.is_file() and resource.suffix.lower() in {".mdl", ".mtr", ".dds", ".tga", ".txi"}:
                shutil.copyfile(resource, development / resource.name)
        for model in models:
            result = subprocess.run(
                [str(nwmain), "-userdirectory", str(user), "compilemodel", model.stem],
                cwd=nwmain.parent, capture_output=True, timeout=60, startupinfo=startup)
            log = user / "logs/nwengineLog.txt"
            log_text = log.read_text(errors="replace") if log.exists() else ""
            (output / (model.stem + ".compile.log")).write_text(log_text)
            compiled = user / "modelcompiler" / model.name
            if result.returncode != 0 or f"Successfully compiled model {model.stem}" not in log_text or not compiled.is_file():
                raise RuntimeError(f"Compilation failed: {model.name}; see output log")
            data = compiled.read_bytes()
            if len(data) < 12:
                raise ValueError(f"Truncated compiled MDL: {model.name}")
            magic, model_size, raw_size = struct.unpack_from("<III", data)
            if magic != 0 or 12 + model_size + raw_size != len(data):
                raise ValueError(f"Invalid compiled MDL header: {model.name}")
        # Publish only after every compile succeeds. Logs remain local diagnostics.
        for model in models:
            shutil.copyfile(user / "modelcompiler" / model.name, output / model.name)
            print(f"Compiled {model.name}: {model.stat().st_size:,} -> {(output / model.name).stat().st_size:,} bytes")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--nwmain", required=True, help="Installed NWN:EE nwmain.exe")
    parser.add_argument("--resources", required=True, help="Staged ASCII MDLs and material/texture dependencies")
    parser.add_argument("--output", required=True, help="Fresh local output directory")
    parser.add_argument("--models", nargs="*", default=[], help="Optional extensionless model resrefs")
    args = parser.parse_args()
    compile_models(args.nwmain, args.resources, args.output, args.models)
