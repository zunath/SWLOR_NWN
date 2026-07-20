#!/usr/bin/env python3
"""Synchronize reviewed Design Bible descriptions into C# and TLK text.

The current manifest is generated from the local workbook. Its HEAD version provides the exact
pre-review wording, allowing surgical old-to-new replacements without guessing feat or TLK IDs.
"""

from __future__ import annotations

import csv
import io
import json
import os
from pathlib import Path
import re
import subprocess


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv"
TLK_JSON = ROOT / "SWLOR_Haks" / "sw_tlk" / "sw_tlk.tlk.json"


def collapse_whitespace(value: str) -> str:
    return " ".join(value.split())


def read_head_manifest() -> list[dict[str, str]]:
    environment = os.environ.copy()
    environment["GIT_LFS_SKIP_SMUDGE"] = "1"
    result = subprocess.run(
        [
            "git",
            "-c",
            "core.hooksPath=NUL",
            "-c",
            "core.fsmonitor=false",
            "show",
            "HEAD:SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv",
        ],
        cwd=ROOT,
        env=environment,
        check=True,
        capture_output=True,
        text=True,
        encoding="utf-8-sig",
    )
    return list(csv.DictReader(io.StringIO(result.stdout)))


def read_current_manifest() -> list[dict[str, str]]:
    with MANIFEST.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def changed_descriptions() -> list[tuple[str, str, str]]:
    original = {
        (row["Tab"], row["Row"], row["PerkName"]): row["Description"]
        for row in read_head_manifest()
    }
    changes: list[tuple[str, str, str]] = []
    for row in read_current_manifest():
        key = (row["Tab"], row["Row"], row["PerkName"])
        old = original.get(key)
        new = row["Description"]
        if old is None or collapse_whitespace(old) == collapse_whitespace(new):
            continue
        changes.append((row["PerkName"], collapse_whitespace(old), collapse_whitespace(new)))
    return changes


def csharp_escape(value: str) -> str:
    return value.replace("\\", "\\\\").replace('"', '\\"')


def build_replacement_map(changes: list[tuple[str, str, str]]) -> dict[str, str]:
    replacements: dict[str, str] = {}
    source_perks: dict[str, str] = {}
    for perk_name, old, new in changes:
        existing = replacements.get(old)
        if existing is not None and existing != new:
            raise RuntimeError(
                "Cannot safely synchronize duplicate old description text: "
                f"{source_perks[old]!r} and {perk_name!r} map {old!r} to "
                "different replacements."
            )
        replacements[old] = new
        source_perks.setdefault(old, perk_name)
    return replacements


def update_csharp(changes: list[tuple[str, str, str]]) -> tuple[int, int]:
    replacement_map = {
        csharp_escape(old): csharp_escape(new)
        for old, new in build_replacement_map(changes).items()
    }
    description_pattern = re.compile(
        r'\.Description\("(?P<text>(?:\\.|[^"\\])*)"\)'
    )

    files_changed = 0
    replacements = 0
    for path in (ROOT / "SWLOR.Game.Server").rglob("*.cs"):
        original_bytes = path.read_bytes()
        had_utf8_bom = original_bytes.startswith(b"\xef\xbb\xbf")
        original = original_bytes.decode("utf-8-sig")
        current_matches = list(description_pattern.finditer(original))
        if not current_matches:
            continue

        relative_path = path.relative_to(ROOT).as_posix()
        head_result = subprocess.run(
            [
                "git",
                "-c",
                "core.hooksPath=NUL",
                "-c",
                "core.fsmonitor=false",
                "show",
                f"HEAD:{relative_path}",
            ],
            cwd=ROOT,
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8-sig",
        )
        if head_result.returncode != 0:
            continue

        head_matches = list(description_pattern.finditer(head_result.stdout))
        if len(head_matches) != len(current_matches):
            raise RuntimeError(
                f"Cannot safely synchronize descriptions in {relative_path}: "
                f"HEAD has {len(head_matches)} description calls but the working file has "
                f"{len(current_matches)}."
            )

        pieces: list[str] = []
        cursor = 0
        replacement_count = 0
        for head_match, current_match in zip(head_matches, current_matches):
            head_text = head_match.group("text")
            current_text = current_match.group("text")
            desired = replacement_map.get(head_text)
            if desired is None or desired == current_text:
                continue
            if current_text != head_text:
                line_number = original.count("\n", 0, current_match.start()) + 1
                raise RuntimeError(
                    f"Cannot safely synchronize description in {relative_path}:{line_number}: "
                    "the working description matches neither the paired HEAD text nor its "
                    "reviewed replacement."
                )

            pieces.append(original[cursor:current_match.start("text")])
            pieces.append(desired)
            cursor = current_match.end("text")
            replacement_count += 1

        if replacement_count:
            pieces.append(original[cursor:])
            updated = "".join(pieces)
            replacements += replacement_count
        else:
            updated = original

        if updated != original:
            encoding = "utf-8-sig" if had_utf8_bom else "utf-8"
            path.write_bytes(updated.encode(encoding))
            files_changed += 1
    return files_changed, replacements


def update_tlk(changes: list[tuple[str, str, str]]) -> tuple[int, int]:
    raw_bytes = TLK_JSON.read_bytes()
    had_utf8_bom = raw_bytes.startswith(b"\xef\xbb\xbf")
    raw = raw_bytes.decode("utf-8-sig")
    new_by_old = build_replacement_map(changes)

    tree_result = subprocess.run(
        ["git", "ls-tree", "HEAD", "SWLOR_Haks"],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    submodule_sha = tree_result.stdout.split()[2]
    head_tlk_result = subprocess.run(
        [
            "git",
            "-c",
            f"safe.directory={ROOT / 'SWLOR_Haks'}",
            "-C",
            str(ROOT / "SWLOR_Haks"),
            "show",
            f"{submodule_sha}:sw_tlk/sw_tlk.tlk.json",
        ],
        check=True,
        capture_output=True,
        text=True,
        encoding="utf-8-sig",
    )
    head_document = json.loads(head_tlk_result.stdout)
    head_text_by_id = {
        int(entry["id"]): entry.get("text", "")
        for entry in head_document["entries"]
    }
    desired_by_id = {
        entry_id: new_by_old[collapse_whitespace(head_text)]
        for entry_id, head_text in head_text_by_id.items()
        if collapse_whitespace(head_text) in new_by_old
    }

    entry_pattern = re.compile(
        r'(?P<prefix>\{\s*"id":\s*(?P<id>\d+),\s*"text":\s*)'
        r'(?P<text>"(?:\\.|[^"\\])*")',
        re.MULTILINE,
    )

    replacement_count = 0

    def replace_entry(match: re.Match[str]) -> str:
        nonlocal replacement_count
        entry_id = int(match.group("id"))
        desired = desired_by_id.get(entry_id)
        if desired is None:
            return match.group(0)

        current = json.loads(match.group("text"))
        if current == desired:
            return match.group(0)

        head_text = head_text_by_id[entry_id]
        if current != head_text:
            raise RuntimeError(
                f"Cannot safely synchronize TLK entry {entry_id}: "
                "the working text matches neither the pinned submodule text nor its "
                "reviewed replacement."
            )

        replacement_count += 1
        return match.group("prefix") + json.dumps(desired, ensure_ascii=False)

    updated = entry_pattern.sub(replace_entry, raw)

    if replacement_count:
        encoding = "utf-8-sig" if had_utf8_bom else "utf-8"
        TLK_JSON.write_bytes(updated.encode(encoding))
    return len(desired_by_id), replacement_count


def main() -> None:
    changes = changed_descriptions()
    csharp_files, csharp_replacements = update_csharp(changes)
    tlk_texts, tlk_replacements = update_tlk(changes)
    print(f"Reviewed description changes: {len(changes)}")
    print(f"C# files updated: {csharp_files} ({csharp_replacements} exact replacements)")
    print(f"TLK texts updated: {tlk_texts} ({tlk_replacements} entries)")


if __name__ == "__main__":
    main()
