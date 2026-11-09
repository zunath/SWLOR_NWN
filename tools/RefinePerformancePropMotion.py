"""Reproduce the reviewed prop-motion corrections without moving the violin or microphone.

Writes a recipe candidate; MotionAuthor owns baking it and the animation installer owns
native publication. Measured prop axes are pinned to their actual model hashes.
"""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "SWLOR_Haks/tools"))
import RobePoseAudit as pose

BOW_AXIS = (-.4324, .2303, .5929)
BOW_FACING = (.3652308841, .8844311045, .2904962629)
DRUM_NORMAL = (.0660850566, -.2372743147, .9691922745)
DRUM_CENTER = (-.0744766926, -.1460790976, -.0100544039)
PROP_HASHES = {
    "sw_cr_creature/a_ba.mdl": "812a48e4a74887560c743f600f4c8e53e17bd6b66748bb52dfcc880d8bf680d3",
    "sw_weapon/wmgfs_m_111.mdl": "35071f6e1b47f5887c1d63462cad5a5729e5087ac18deb80c21a0915cc939969",
    "sw_weapon/wmgfs_m_161.mdl": "e5c99f960fd5a33327f62eee21406b871f22a14d442c47c94041bab4e702a47f",
}


def dot(a, b): return sum(x*y for x, y in zip(a, b))
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def unit(v): return tuple(x / math.sqrt(dot(v, v)) for x in v)


def frame_rotation(axis, face, up, facing):
    """Match the prop's two measured axes without introducing a changing bow roll."""
    up, axis = unit(up), unit(axis)
    q = unit((*cross(axis, up), 1 + dot(axis, up)))
    normal = pose.rotate(q, face)
    actual = unit(tuple(a-b*dot(normal, up) for a, b in zip(normal, up)))
    desired = unit(tuple(a-b*dot(facing, up) for a, b in zip(facing, up)))
    angle = math.atan2(dot(up, cross(actual, desired)), dot(actual, desired))
    roll = (*[v*math.sin(angle/2) for v in up], math.cos(angle/2))
    return pose.multiply(roll, q)


def elbow_pole(hand, rotation):
    # Native forearm-to-wrist bind offset: choose the elbow plane closest to a neutral wrist.
    toward_elbow = pose.rotate(rotation, (-.03309, -.02289, .29176))
    return [a+b for a, b in zip(hand, toward_elbow)]


def refine(source):
    result = copy.deepcopy(source)
    direction = unit((-.6, .35, .72))
    for motion in result["motions"]:
        name = motion["id"]
        duration = motion["beats"][-1]["time"]
        if name.startswith("Vocals"):
            motion["relaxRightHand"] = True
            motion["interpretation"] = "The left hand keeps the microphone at the lips; the free right wrist follows its forearm without a sword-aim twist."
        elif name.startswith("Violin"):
            length = next(value for suffix, value in (("Loop", .30), ("Low", .22), ("Mid", .28), ("High", .34), ("Flourish", .38)) if name.endswith(suffix))
            for beat in motion["beats"]:
                phase = beat["time"] / duration
                stroke = length * (1-math.cos(2*math.pi*(4 if name.endswith("Loop") else 1)*phase))/2
                p = beat["pose"]
                p["rightHand"] = [base + step*stroke for base, step in zip((.25, .22, 1.22), direction)]
                p["rightGripNormal"] = [0, 1, 0]
                p["rightGripFacing"] = list(BOW_FACING)
                p["rightElbow"] = elbow_pole(p["rightHand"], frame_rotation(BOW_AXIS, (0, 1, 0), p["blade"], BOW_FACING))
            motion["interpretation"] = "The violin retains its accepted left-hand placement while the bow slides along its own length with a fixed bowing angle."
        elif name.startswith("Tambourine"):
            for beat in motion["beats"]:
                p = beat["pose"]
                phase = beat["time"] / duration
                hit = (1-math.cos(2*math.pi*4*phase))/2 if name.endswith("Loop") else math.sin(math.pi*phase)**2
                wave = math.sin(2*math.pi*(4 if name.endswith("Loop") else 3 if duration > 1 else 1)*phase)
                if not name.endswith("Loop"): wave *= hit
                # Bring the rim 6 cm toward the body so the tapping arm keeps a bent elbow.
                p["rightHand"][0] = .24 + .012*wave
                p["rightGripNormal"] = list(DRUM_NORMAL)
                p["rightGripFacing"] = [0, 1, 0]
                q = frame_rotation((0, 1, 0), DRUM_NORMAL, p["blade"], (0, 1, 0))
                p["rightElbow"] = elbow_pole(p["rightHand"], q)
                # The native rhand hook is offset from the wrist joint.
                center = tuple(a+b for a, b in zip(p["rightHand"], pose.rotate(q, tuple(a+b for a, b in zip((.01107, 0, -.09613), DRUM_CENTER)))))
                normal = pose.rotate(q, unit(DRUM_NORMAL))
                p["leftHand"] = [center[i] + normal[i]*(.11 + .12*(1-hit)) + (-.08 if i == 0 else 0) for i in range(3)]
            motion["interpretation"] = "The playing face points outward with ribbons hanging down; the free hand approaches that face for taps."
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    for path, expected in PROP_HASHES.items():
        if hashlib.sha256((ROOT / "SWLOR_Haks" / path).read_bytes()).hexdigest() != expected:
            raise ValueError(f"Remeasure changed prop model: {path}")
    source = json.loads((ROOT / "design/animations/performance/props-recipe.json").read_text())
    result = refine(source)
    if args.check:
        if result != source: raise ValueError("Performance prop recipe needs refinement")
    elif args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(result, indent=2) + "\n")
    else:
        parser.error("Choose --output or --check")


if __name__ == "__main__": main()
