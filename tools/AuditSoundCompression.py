#!/usr/bin/env python3
"""Keep game-configured loops out of the lossy sound conversion manifest.

Generate converter exclusions before encoding, then check the resulting manifest:
  python tools/AuditSoundCompression.py --write-exclusions artifacts/audio-exclusions.json
  python tools/AuditSoundCompression.py --check SWLOR_Haks/tools/SoundCompressionManifest.json

Cue/sampler chunks are excluded separately by the HAK converter. This audit uses
explicit playback fields, never sound names, to find additional looping sounds.
It does not claim to prove that unreferenced audio has no timing requirements.
"""

import argparse
import hashlib
import json
import pathlib
import shlex
import sys
from collections import defaultdict


ROOT = pathlib.Path(__file__).resolve().parents[1]
LOOP_COLUMNS = {
    # Area soundtrack beds can repeat for the area's entire day/night period.
    "ambientsound.2da": "Resource",
    "appearancesndset.2da": "Looping",
    "visualeffects.2da": "SoundDuration",
    "vfx_persistent.2da": "SoundDuration",
}


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sound_object_loops(document, evidence_path):
    """Yield resref/evidence pairs from UTS or embedded GIT sound objects."""
    def walk(node, location):
        if isinstance(node, dict):
            if "Looping" in node:
                field = node["Looping"]
                looping = field.get("value") if isinstance(field, dict) else None
                if looping not in (0, 1):
                    raise ValueError(f"{evidence_path}:{location}.Looping has no valid byte value")
                if looping == 1:
                    sounds = node.get("Sounds", {}).get("value")
                    if not isinstance(sounds, list):
                        raise ValueError(f"{evidence_path}:{location}.Sounds is not a sound list")
                    for index, sound in enumerate(sounds):
                        resref = sound.get("Sound", {}).get("value")
                        if not isinstance(resref, str) or not resref.strip():
                            raise ValueError(f"{evidence_path}:{location}.Sounds[{index}] has no resref")
                        yield resref.lower(), f"{evidence_path}:{location}.Looping=1; Sounds.value[{index}]"
            for name, child in node.items():
                yield from walk(child, f"{location}.{name}")
        elif isinstance(node, list):
            for index, child in enumerate(node):
                yield from walk(child, f"{location}[{index}]")

    yield from walk(document, "$")


def table_loops(path, column, evidence_path):
    """Read a loop or area-soundtrack column, preserving quoted 2DA cells."""
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    content = [(number, line.strip()) for number, line in enumerate(lines, 1)
               if line.strip() and not line.lstrip().startswith("//")]
    if not content or content[0][1] != "2DA V2.0":
        raise ValueError(f"{path} is not a supported text 2DA")
    content = content[1:]
    if content and content[0][1].upper().startswith("DEFAULT:"):
        content = content[1:]
    if not content:
        raise ValueError(f"{path} has no column header")
    columns = shlex.split(content[0][1])
    if column not in columns:
        raise ValueError(f"{path} is missing required {column} column")
    index = columns.index(column) + 1  # Each data row begins with its row number.
    for line_number, line in content[1:]:
        cells = shlex.split(line)
        if len(cells) <= index:
            raise ValueError(f"{path}:{line_number} is missing its {column} cell")
        resref = cells[index]
        if resref not in ("", "****"):
            yield resref.lower(), f"{evidence_path}:{line_number}: row {cells[0]} {column}={resref}"


def collect_exclusions(root, hak_root):
    evidence = defaultdict(set)
    for folder, pattern in (("git", "*.git.json"), ("uts", "*.uts.json")):
        directory = root / "Module" / folder
        paths = sorted(directory.glob(pattern))
        if not paths:
            raise ValueError(f"No {pattern} resources found in {directory}; cannot audit loops")
        for path in paths:
            for resref, reason in sound_object_loops(read_json(path), path.relative_to(root).as_posix()):
                evidence[resref].add(reason)
    for table, column in LOOP_COLUMNS.items():
        path = hak_root / "sw_2da" / table
        for resref, reason in table_loops(path, column, f"SWLOR_Haks/sw_2da/{table}"):
            evidence[resref].add(reason)
    return {resref: sorted(reasons) for resref, reasons in sorted(evidence.items())}


def converter_exclusions(evidence):
    # Retain a concrete reference in the converter's concise per-file skip reason.
    return {
        "schema_version": 1,
        "excluded_resrefs": {
            resref: "Game-configured loop: " + reasons[0]
            + (f" (+{len(reasons) - 1} more references)" if len(reasons) > 1 else "")
            for resref, reasons in evidence.items()
        },
        "evidence": evidence,
    }


def check_manifest(manifest, evidence, hak_root=None):
    if not isinstance(manifest, dict) or manifest.get("schema_version") != 1:
        raise ValueError("Unsupported sound compression manifest schema")
    rows = manifest.get("files")
    if not isinstance(rows, list) or not rows:
        raise ValueError("Sound compression manifest has no file inventory")
    violations = []
    seen = set()
    for row in rows:
        if not isinstance(row, dict) or not isinstance(row.get("path"), str) or not row["path"]:
            raise ValueError("Sound compression manifest contains a file without a path")
        path = pathlib.PurePosixPath(row["path"].replace("\\", "/"))
        normalized = str(path).lower()
        if normalized in seen:
            raise ValueError(f"Duplicate sound compression manifest path: {row['path']}")
        seen.add(normalized)
        status = row.get("status")
        if status not in ("converted", "would_convert", "skipped"):
            raise ValueError(f"Unknown conversion status for {row['path']}: {status}")
        if status in ("converted", "would_convert") and path.stem.lower() in evidence:
            if hak_root is not None and status == "converted":
                root = hak_root.resolve(strict=True)
                resource = (root / path).resolve()
                if path.is_absolute() or ".." in path.parts or not resource.is_relative_to(root):
                    raise ValueError(f"Manifest resource is outside the HAK repository: {path}")
                # Keep historical provenance immutable after restoring a loop's
                # original PCM. Only an exact original hash clears the finding.
                if hashlib.sha256(resource.read_bytes()).hexdigest() == row.get("source_sha256"):
                    continue
            violations.append(f"{row['path']}: {evidence[path.stem.lower()][0]}")
    return violations


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--root", type=pathlib.Path, default=ROOT, help="Parent repository root")
    parser.add_argument("--hak-root", type=pathlib.Path, help="HAK source root (default: ROOT/SWLOR_Haks)")
    parser.add_argument("--write-exclusions", type=pathlib.Path, help="Write converter exclusion JSON")
    parser.add_argument("--check", type=pathlib.Path, metavar="MANIFEST", help="Reject conversions of configured loops")
    args = parser.parse_args(argv)
    if not args.write_exclusions and not args.check:
        parser.error("provide --write-exclusions and/or --check")
    try:
        evidence = collect_exclusions(args.root, args.hak_root or args.root / "SWLOR_Haks")
        if args.write_exclusions:
            args.write_exclusions.parent.mkdir(parents=True, exist_ok=True)
            args.write_exclusions.write_text(json.dumps(converter_exclusions(evidence), indent=2) + "\n", encoding="utf-8")
            print(f"Wrote {len(evidence)} game-configured loop exclusions to {args.write_exclusions}")
        if args.check:
            violations = check_manifest(read_json(args.check), evidence, args.hak_root or args.root / "SWLOR_Haks")
            if violations:
                print("Converted sounds are configured to loop:", file=sys.stderr)
                for violation in violations:
                    print(f"  {violation}", file=sys.stderr)
                return 1
            print(f"Sound compression manifest preserves all {len(evidence)} game-configured loop resrefs")
    except (OSError, ValueError, TypeError) as error:
        print(f"Sound compression audit failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
