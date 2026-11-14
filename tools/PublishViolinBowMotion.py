"""Publish only the measured violin bow-arm rotations into the existing body banks.

Run after the animation installer and before CompileModels.py. Dense contact
solves must not resample existing body translations or unrelated arm/cloth
tracks. The public baseline supplies those tracks verbatim.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1] / "SWLOR_Haks"
CLIPS = {"sw_vio_" + name + phase for name in ("loop", "low", "mid", "high", "flour")
         for phase in ("", "_in", "_out")}
JOINTS = {"rbicep_g", "rforearm_g", "rhand_g", "rhand"}
ANIMATIONS = re.compile(r"(?mi)^[ \t]*newanim[ \t]+(\S+)[^\n]*\n([\s\S]*?)^[ \t]*doneanim[^\n]*(?:\n|$)")
NODES = re.compile(r"(?mi)^[ \t]*node[ \t]+\S+[ \t]+(\S+)[^\n]*\n([\s\S]*?)^[ \t]*endnode[^\n]*(?:\n|$)")


def rotation_span(text):
    """Return one complete quaternion controller without touching adjacent data."""
    match = re.search(r"(?mi)^[ \t]*orientationkey[ \t]+(\d+)[^\n]*\n", text)
    if not match:
        match = re.search(r"(?mi)^[ \t]*orientation[ \t]+[^\n]*(?:\n|$)", text)
        if not match:
            raise ValueError("The bow joint has no existing orientation controller")
        return match.start(), match.end()
    count = int(match[1])
    lines = text[match.end():].splitlines(keepends=True)[:count]
    if count < 1 or len(lines) != count:
        raise ValueError("Incomplete bow rotation controller")
    times = []
    for line in lines:
        values = [float(x) for x in line.split()]
        if len(values) != 5 or not all(math.isfinite(x) for x in values):
            raise ValueError("Malformed bow rotation key")
        times.append(values[0])
    if any(a >= b for a, b in zip(times, times[1:])):
        raise ValueError("Bow rotation keys are not strictly ordered")
    return match.start(), match.end() + sum(map(len, lines))


def publish(before, candidate):
    """Keep the baseline source except the four bow-arm quaternion tracks."""
    def rig(text):
        if text.startswith("# SWLOR compiled animation source v1 "):
            text = text.split("\n", 1)[1]
        return ANIMATIONS.sub("", text).split()
    if rig(before) != rig(candidate):
        raise ValueError("Body model geometry or metadata changed")
    old = {m[1]: m for m in ANIMATIONS.finditer(before)}
    new = {m[1]: m for m in ANIMATIONS.finditer(candidate)}
    if old.keys() != new.keys() or not CLIPS <= old.keys():
        raise ValueError("Body animation inventory changed")
    for clip in old:
        if NODES.sub("", old[clip][0]).split() != NODES.sub("", new[clip][0]).split():
            raise ValueError("Animation timing, events or identity changed: " + clip)
        if clip not in CLIPS and old[clip][0].split() != new[clip][0].split():
            raise ValueError("An unrelated animation changed: " + clip)
    edits = []
    for clip in sorted(CLIPS):
        left = {m[1]: m for m in NODES.finditer(old[clip][2])}
        right = {m[1]: m for m in NODES.finditer(new[clip][2])}
        if left.keys() != right.keys() or not JOINTS <= left.keys():
            raise ValueError("Violin animation node inventory changed: " + clip)
        for name in left:
            a, b = left[name], right[name]
            pa = re.search(r"(?mi)^[ \t]*parent[ \t]+(\S+)", a[2])
            pb = re.search(r"(?mi)^[ \t]*parent[ \t]+(\S+)", b[2])
            if a[0].splitlines()[0].split() != b[0].splitlines()[0].split() or not pa or not pb or pa[1] != pb[1]:
                raise ValueError("Animation node binding changed: " + clip + "/" + name)
        for joint in sorted(JOINTS):
            a, b = left[joint], right[joint]
            start, end = rotation_span(a[2])
            ns, ne = rotation_span(b[2])
            offset = old[clip].start(2) + a.start(2)
            edits.append((offset+start, offset+end, b[2][ns:ne]))
    for start, end, value in sorted(edits, reverse=True):
        before = before[:start] + value + before[end:]
    if before.startswith("# SWLOR compiled animation source v1 "):
        before = before.split("\n", 1)[1]
    if not before.startswith("# SWLOR authored animations for "):
        raise ValueError("The baseline is not an owned editable animation bank")
    return before, len(edits)


def main():
    """Publish scoped bow rotations from a pinned public asset baseline."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", required=True, help="Public asset commit before the bow correction")
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    plans, records = [], []
    for name in ("an_a_ba", "an_a_fa"):
        source = "model_sources/sw_cr_creature/" + name + ".mdl.ascii"
        old = subprocess.check_output(["git", "-C", str(ROOT), "show", args.baseline + ":" + source])
        candidate = (ROOT/source).read_text()
        text, count = publish(old.decode("utf-8-sig").replace("\r\n", "\n"), candidate)
        payload = text.encode("utf-8")
        plans.append((name, payload))
        records.append({"model": name, "rotation_tracks": count, "source_sha256": hashlib.sha256(payload).hexdigest()})
    args.output.mkdir(parents=True, exist_ok=True)
    for name, payload in plans:
        (args.output/(name+".mdl")).write_bytes(payload)
    if args.apply:
        for name, payload in plans:
            (ROOT/"sw_cr_creature"/(name+".mdl")).write_bytes(payload)
    report = {"baseline": args.baseline, "models": records,
              "preservation": "Baseline source retained verbatim outside four right-arm rotation controllers in the15selected clips.",
              "next": "Compile both body banks, then run the native rotation-only preservation and robe-bridge guards."}
    (args.output/"report.json").write_text(json.dumps(report, indent=2)+"\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
