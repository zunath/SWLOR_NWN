"""Reproduce the reviewed prop-motion corrections while preserving the violin placement.

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

# The playable hair ribbon, rather than the wooden bow's bounding diagonal.
BOW_HAIR_START = (.0023665, .0244655, .0301885)
BOW_HAIR_END = (-.344768, .213982, .4864385)
BOW_NORMAL = (-.6233364, -.76618906, -.15622438)
BOW_ATTACHMENT = (-.7291083536420421, .6194369143952155, .2659774447401658, -.11813093000318564)
VIOLIN_CONTACT = (.17881616842359055, .04933464638279063, .245)
VIOLIN_STRING_AXIS = (.5701070601892182, .13283710612481703, .8107602871125295)
DRUM_NORMAL = (.0660850566, -.2372743147, .9691922745)
DRUM_CENTER = (-.0744766926, -.1460790976, -.0100544039)
DRUM_UP = (-.7167083083150231, .6646320290532998, .21117165233219967)
# Neutral wrist solved on the pinned rig, before aiming the measured drum frame.
DRUM_ATTACHMENT = (-.5087549785379185, .6253516048634483, -.3564199797010802, .4723012886345016)
PROP_HASHES = {
    "sw_cr_creature/a_ba.mdl": "812a48e4a74887560c743f600f4c8e53e17bd6b66748bb52dfcc880d8bf680d3",
    "sw_item/it_torch_021.mdl": "e1e9c75c3bc3a476d5c42e52eb2ca70e656ba03159bf8aae093f8b7a918a6195",
    "sw_weapon/wmgfs_m_111.mdl": "35071f6e1b47f5887c1d63462cad5a5729e5087ac18deb80c21a0915cc939969",
    "sw_weapon/wmgfs_m_161.mdl": "e5c99f960fd5a33327f62eee21406b871f22a14d442c47c94041bab4e702a47f",
    "sw_item/it_torch_041.mdl": "69b7a199e306b82429fa8086f632e1ca0afc8b61cbd1df262ac9aa28bcf8b964",
    "sw_item/it_torch_042.mdl": "0ec96b2296cee41f11da302d834e5c1b3bf0a5ce60e509971a826bf7b751b858",
    "sw_cr_creature/a_ba_non_combat.mdl": "8cfe67b2e901491cde0042b460c6c25e729cb982fbc3591c8eb88478e8a102f9",
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


def violin_left_frames(motion):
    """Read the accepted left-side hold, which this refinement never changes.

    Violin play retains NWN's native torch layer on the left arm. Compose that
    layer before measuring contact; the draft's left arm is not the rendered
    violin hold. The unchanged native hold remains independent of the bow arm.
    """
    path = ROOT / "design/animations/performance" / (motion["id"] + ".swlanim")
    project = json.loads(path.read_text())
    native = pose.Model((ROOT / "SWLOR_Haks/sw_cr_creature/a_ba_non_combat.mdl").read_bytes())
    carry = {name: pose.sample(controllers[20], 0, True)
             for name, _, _, controllers, _ in native.clips["torchl"][1] if 20 in controllers}
    frames = []
    for beat in motion["beats"]:
        time = beat["time"]
        keys = project["Keys"]
        if time < keys[0]["Time"] or time > keys[-1]["Time"] + .00001:
            raise ValueError(f"Stale left-hand duration: {motion['id']}")
        index = next((i for i in range(1, len(keys)) if keys[i]["Time"] >= time), len(keys)-1)
        a, b = keys[index-1], keys[index]
        weight = max(0, min(1, (time-a["Time"])/(b["Time"]-a["Time"])))
        world, rendered = [], []
        for joint, left, right in zip(project["Joints"], a["Pose"], b["Pose"]):
            position = pose.interpolate(tuple(left["Position"][a] for a in "XYZ"),
                tuple(right["Position"][a] for a in "XYZ"), weight)
            rotation = pose.interpolate(tuple(left["Orientation"][a] for a in "XYZW"),
                tuple(right["Orientation"][a] for a in "XYZW"), weight, True)
            scale = left["Scale"] + weight*(right["Scale"]-left["Scale"])
            local_position, local_rotation, local_scale = position, rotation, scale
            if joint["Parent"] >= 0:
                parent_position, parent_rotation, parent_scale = world[joint["Parent"]]
                position = tuple(a+b for a, b in zip(parent_position,
                    pose.rotate(parent_rotation, tuple(v*parent_scale for v in position))))
                rotation = pose.multiply(parent_rotation, rotation)
                scale *= parent_scale
            world.append((position, rotation, scale))
            position, rotation, scale = local_position, carry.get(joint["Name"], local_rotation), local_scale
            if joint["Parent"] >= 0:
                parent_position, parent_rotation, parent_scale = rendered[joint["Parent"]]
                position = tuple(a+b for a, b in zip(parent_position,
                    pose.rotate(parent_rotation, tuple(v*parent_scale for v in position))))
                rotation = pose.multiply(parent_rotation, rotation)
                scale *= parent_scale
            rendered.append((position, rotation, scale))
        hand = next(world[i] for i, joint in enumerate(project["Joints"]) if joint["Name"] == "lhand_g")
        if math.dist(hand[0], beat["pose"]["leftHand"]) > .001:
            raise ValueError(f"Accepted violin hold changed: {motion['id']}")
        frames.append(next(rendered[i] for i, joint in enumerate(project["Joints"]) if joint["Name"] == "lhand"))
    return frames


def violin_contact_beats(motion):
    """Retain the authored body targets and add 60 Hz bow-contact solve points."""
    old = motion["beats"]
    duration = old[-1]["time"]
    beats = []
    for frame in range(round(duration*60)+1):
        time = min(frame/60, duration)
        index = next((i for i in range(1, len(old)) if old[i]["time"] >= time), len(old)-1)
        a, b = old[index-1], old[index]
        weight = max(0, min(1, (time-a["time"])/(b["time"]-a["time"])))
        values = {name: [x+(y-x)*weight for x, y in zip(value, b["pose"][name])]
                  for name, value in a["pose"].items()}
        label = b["label"] if abs(time-b["time"]) < 1e-9 else a["label"]
        beats.append({"time": time, "label": label, "pose": values})
    motion["beats"] = beats


def refine(source):
    result = copy.deepcopy(source)
    for motion in result["motions"]:
        name = motion["id"]
        duration = motion["beats"][-1]["time"]
        if name.startswith("Vocals"):
            motion["relaxRightHand"] = True
            for beat in motion["beats"]:
                phase = beat["time"] / duration
                p = beat["pose"]
                p["leftHand"] = [-.11, .22, 1.64]
                p["leftPalmFacing"] = [.3317419670, -.5323938892, -.7787839094]
                envelope = math.sin(math.pi*phase)**2
                if name.endswith("Loop"):
                    p["rightHand"] = [.28 + .012*math.sin(2*math.pi*phase), .10 + .015*envelope, 1.06 + .025*envelope]
                    p["chest"] = [.6*envelope, 0, 1.2*math.sin(2*math.pi*phase)]
                else:
                    height = next(value for suffix, value in (("Low", .06), ("Mid", .11), ("High", .16), ("Flourish", .25)) if name.endswith(suffix))
                    # Keep the gesture small and in front of the relaxed elbow.
                    p["rightHand"] = [.28 + .055*envelope, .10 + .08*envelope, 1.06 + height*envelope]
                    p["chest"] = [-1.2*envelope, -.8*envelope, 0]
                p["rightElbow"] = [.37, -.04, 1.19 + .35*(p["rightHand"][2]-1.06)]
            motion["interpretation"] = "The microphone stays at the lips while the free arm follows a restrained elbow-led gesture, with a neutral wrist and soft breathing motion."
        elif name.startswith("Violin"):
            violin_contact_beats(motion)
            motion["rightHandAttachment"] = list(BOW_ATTACHMENT)
            axis = unit(tuple(a-b for a, b in zip(BOW_HAIR_END, BOW_HAIR_START)))
            inverse_attachment = (*[-v for v in BOW_ATTACHMENT[:3]], BOW_ATTACHMENT[3])
            length = next(value for suffix, value in (("Loop", .28), ("Low", .22), ("Mid", .28), ("High", .30), ("Flourish", .26)) if name.endswith(suffix))
            for beat, (left_position, left_rotation, _) in zip(motion["beats"], violin_left_frames(motion)):
                phase = beat["time"] / duration
                cycles = 4 if name.endswith("Loop") else 3 if name.endswith("Flourish") else 1
                stroke = length * (1-math.cos(2*math.pi*cycles*phase))/2
                p = beat["pose"]
                string = pose.rotate(left_rotation, VIOLIN_STRING_AXIS)
                seed = (-.95, 0, .30)
                draw = unit(tuple(a-b*dot(seed, string) for a, b in zip(seed, string)))
                normal = pose.rotate(left_rotation, (0, 1, 0))
                facing = unit(tuple(a-b*dot(normal, draw) for a, b in zip(normal, draw)))
                prop_rotation = frame_rotation(axis, BOW_NORMAL, draw, facing)
                hand_rotation = pose.multiply(prop_rotation, inverse_attachment)
                contact = tuple(a+b for a, b in zip(left_position, pose.rotate(left_rotation, VIOLIN_CONTACT)))
                hair_point = tuple(a+b*(.50-stroke) for a, b in zip(BOW_HAIR_START, axis))
                # The native hook translation is in wrist space; the hair is in item space.
                p["rightHand"] = [a-b-c for a, b, c in zip(contact,
                    pose.rotate(hand_rotation, (.01107, 0, -.09613)), pose.rotate(prop_rotation, hair_point))]
                p["blade"] = list(draw)
                p["rightGripAxis"] = list(axis)
                p["rightGripNormal"] = list(BOW_NORMAL)
                p["rightGripFacing"] = list(facing)
                p["rightElbow"] = [.60, .12, 1.40]
            motion["interpretation"] = "The bow hair remains on the violin string near the bridge throughout the draw; the accepted violin hold is preserved."
        elif name.startswith("Tambourine"):
            motion["rightHandAttachment"] = list(DRUM_ATTACHMENT)
            for beat in motion["beats"]:
                p = beat["pose"]
                phase = beat["time"] / duration
                hit = (1-math.cos(2*math.pi*4*phase))/2 if name.endswith("Loop") else math.sin(math.pi*phase)**2
                wave = math.sin(2*math.pi*(4 if name.endswith("Loop") else 3 if duration > 1 else 1)*phase)
                if not name.endswith("Loop"): wave *= hit
                lift = .04*hit if name.endswith("Flourish") else .012*hit
                p["rightHand"] = [.10 + .008*wave, .16 + .006*wave, 1.44 + lift]
                p["blade"] = [0, 0, 1]
                p["rightGripAxis"] = list(DRUM_UP)
                p["rightGripNormal"] = list(DRUM_NORMAL)
                p["rightGripFacing"] = [0, 1, 0]
                q = frame_rotation(pose.rotate(DRUM_ATTACHMENT, DRUM_UP), pose.rotate(DRUM_ATTACHMENT, DRUM_NORMAL), p["blade"], (0, 1, 0))
                p["rightElbow"] = [.40, 0, 1.25 + lift]
                prop = pose.multiply(q, DRUM_ATTACHMENT)
                center = tuple(a+b+c for a, b, c in zip(p["rightHand"], pose.rotate(q, (.01107, 0, -.09613)), pose.rotate(prop, DRUM_CENTER)))
                normal = pose.rotate(prop, unit(DRUM_NORMAL))
                p["leftHand"] = [center[i] + normal[i]*(.045 + .12*(1-hit)) + (-.055 if i == 0 else .035 if i == 2 else 0) for i in range(3)]
                p["leftElbow"] = [-.25, .45, 1.20 + lift]
                p["shield"] = [0, 0, 0]
                # Fingers rise diagonally across the skin, following the forearm
                # instead of demanding a vertical hand on a horizontal arm.
                p["leftGrip"] = [-math.sqrt(1-.75**2), -.375, .75*math.cos(math.radians(30))]
                p["leftPalmFacing"] = [0, -math.cos(math.radians(30)), -.5]
            motion["interpretation"] = "An authored hand attachment keeps the wrist neutral while the measured drum face points outward and its ribbon strips hang down; the free palm approaches the skin and recoils."
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
