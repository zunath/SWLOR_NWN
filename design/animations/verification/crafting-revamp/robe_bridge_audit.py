#!/usr/bin/env python3
"""Offline audit: do the robe animation bridges carry the crafting-revamp work clips?

Run from the SWLOR_Haks checkout:
    python -B <path>/robe_bridge_audit.py --game-data "<NWN>/data" --stage <scratch folder> [--json out.json] [--markdown out.md]

For every shared robe family recorded in tools/RobeRgbModels.json it walks the bridge bank chain
(head, _b01, ...) and reports, per work clip: the bank that owns it, its duration against the body
chain's clip, and whether the inherited body pose of the 15 clips matches the body chain at five
phases (RobePoseAudit.validate_body_poses, the same check the robe generator runs). It also counts
how many garment tracks each bridge clip carries, which is what the bridge adds over plain
inheritance from the body chain.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

ROOT = Path.cwd()
sys.path.insert(0, str(ROOT / "tools"))

import CompileModels as mdl  # noqa: E402
import GenerateTintMapAssets as tint  # noqa: E402
import RobePoseAudit as poses  # noqa: E402

CLIPS = ("sw_forgework", "sw_circuitry", "sw_fabricate", "sw_cooking", "sw_compound", "sw_survey", "sw_strike",
         "sw_hvystrike", "sw_extract", "sw_search", "sw_dig", "sw_reel", "sw_giveline", "sw_holdrod", "sw_landfish")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--game-data", type=Path, required=True)
    parser.add_argument("--json", type=Path)
    parser.add_argument("--markdown", type=Path)
    parser.add_argument("--stage", type=Path, required=True, help="Scratch folder for compiling ASCII references")
    args = parser.parse_args()
    args.stage.mkdir(parents=True, exist_ok=True)
    (args.stage / "reference").mkdir(exist_ok=True)
    compiler, _ = mdl.prepare_compiler(args.stage)

    active = tint.find_active_models()
    stock = tint.read_stock_key_models(args.game_data)

    def load(name):
        if name in active:
            data = active[name].read_bytes()
        elif name in stock:
            data = tint.extract_stock_bif_resource(*stock[name])
        else:
            return None
        if not mdl.binary(data):
            # An authored ASCII owner is audited through its compiled form, as the generator does.
            (args.stage / f"{name}.mdl").write_bytes(data)
            mdl.run_compiler(compiler, args.stage, ["-cne", str(args.stage / f"{name}.mdl"),
                                                    str(args.stage / "reference") + "/"], "reference.log")
            data = (args.stage / "reference" / f"{name}.mdl").read_bytes()
        return data

    library = poses.Library(load)
    manifest = json.loads((ROOT / "tools" / "RobeRgbModels.json").read_text())
    families = manifest["animation_bank_sets"]

    class WorkClipLibrary(poses.Library):
        """Restricts the body chain's expected clips to the work clips, so the five-phase pose
        comparison the generator runs covers exactly the clips under audit."""

        def clips(self, name):
            result = super().clips(name)
            return {key: value for key, value in result.items() if key in CLIPS} if name == self.only else result

    report = {"clips": list(CLIPS), "families": {}}
    for head in sorted(families):
        parts = families[head]["parts"]
        base = head[:3] + "0"
        scoped = WorkClipLibrary(load)
        scoped.only = base
        body_clips = library.clips(base)
        bridge_clips = library.clips(head)
        record = {"body": base, "banks": parts, "bridge_clip_count": len(bridge_clips), "clips": {}}
        owners = {}
        garment = {}
        for part in parts:
            model = library.model(part)
            for clip, (duration, nodes) in model.clips.items():
                owners[clip] = part
                garment[clip] = sum(1 for node in nodes if node[2] >= 0 and node[3])
        for clip in CLIPS:
            body_duration = body_clips[clip][0] if clip in body_clips else None
            if clip not in bridge_clips:
                record["clips"][clip] = {"present": False}
                continue
            duration = bridge_clips[clip][0]
            record["clips"][clip] = {
                "present": True,
                "ownedByBridgeBank": owners.get(clip),
                "durationSeconds": round(duration, 4),
                "matchesBodyDuration": body_duration is not None and math.isclose(duration, body_duration, abs_tol=2e-6),
                "animatedGarmentOrBodyTracks": garment.get(clip, 0),
            }
        # The pose comparison only makes sense for clips the bridge exposes.
        present = [clip for clip in CLIPS if clip in bridge_clips]
        try:
            samples = poses.validate_body_poses(head, base, scoped) if present else 0
            record["poseSamplesCompared"] = samples
            record["poseMismatch"] = None
        except ValueError as error:
            record["poseSamplesCompared"] = 0
            record["poseMismatch"] = str(error)
        report["families"][head] = record

    complete = all(all(c["present"] for c in f["clips"].values()) for f in report["families"].values())
    own = all(all(c.get("ownedByBridgeBank") for c in f["clips"].values() if c["present"]) for f in report["families"].values())
    report["allFamiliesCoverAllClips"] = complete
    report["everyClipOwnedByABridgeBank"] = own
    if args.json:
        args.json.write_text(json.dumps(report, indent=2) + "\n")

    lines = []
    for head, record in report["families"].items():
        hit = sum(1 for c in record["clips"].values() if c["present"])
        owned = sum(1 for c in record["clips"].values() if c.get("ownedByBridgeBank"))
        lines.append(f"{head}: {hit}/{len(CLIPS)} clips reachable, {owned}/{len(CLIPS)} owned by a bridge bank, "
                     f"{record['poseSamplesCompared']} pose samples, mismatch={record['poseMismatch']}")
    summary = "\n".join(lines)
    print(summary)
    print("all families cover all clips:", complete, "| every clip owned by a bridge bank:", own)
    if args.markdown:
        args.markdown.write_text(summary + "\n")


if __name__ == "__main__":
    main()
