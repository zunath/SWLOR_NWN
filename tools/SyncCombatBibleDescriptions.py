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


def update_csharp(changes: list[tuple[str, str, str]]) -> tuple[int, int]:
    files_changed = 0
    replacements = 0
    for path in (ROOT / "SWLOR.Game.Server").rglob("*.cs"):
        original = path.read_text(encoding="utf-8-sig")
        updated = original
        for _, old, new in changes:
            old_literal = csharp_escape(old)
            new_literal = csharp_escape(new)
            count = updated.count(old_literal)
            if count:
                updated = updated.replace(old_literal, new_literal)
                replacements += count
        if updated != original:
            path.write_text(updated, encoding="utf-8", newline="")
            files_changed += 1
    return files_changed, replacements


def update_tlk(changes: list[tuple[str, str, str]]) -> tuple[int, int]:
    raw = TLK_JSON.read_text(encoding="utf-8-sig")
    document = json.loads(raw)
    replacements: dict[str, str] = {}
    new_by_old = {old: new for _, old, new in changes}

    for entry in document["entries"]:
        current = entry.get("text", "")
        normalized = collapse_whitespace(current)
        if normalized in new_by_old:
            replacements[current] = new_by_old[normalized]

    replacement_count = 0
    for old, new in replacements.items():
        old_token = f'"text": {json.dumps(old, ensure_ascii=False)}'
        new_token = f'"text": {json.dumps(new, ensure_ascii=False)}'
        count = raw.count(old_token)
        if count:
            raw = raw.replace(old_token, new_token)
            replacement_count += count

    TLK_JSON.write_text(raw, encoding="utf-8", newline="")
    return len(replacements), replacement_count


def main() -> None:
    changes = changed_descriptions()
    csharp_files, csharp_replacements = update_csharp(changes)
    tlk_texts, tlk_replacements = update_tlk(changes)
    print(f"Reviewed description changes: {len(changes)}")
    print(f"C# files updated: {csharp_files} ({csharp_replacements} exact replacements)")
    print(f"TLK texts updated: {tlk_texts} ({tlk_replacements} entries)")


if __name__ == "__main__":
    main()
