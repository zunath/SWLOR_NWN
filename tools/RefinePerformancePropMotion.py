"""Author the microphone, panpipes and tambourine motion and keep the reviewed violin bow contact.

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


# A relaxed performer stance: feet under the shoulders and the hips 2 cm higher than the combat-ready crouch.
ROOT_HEIGHT = 1.19
LEFT_FOOT, RIGHT_FOOT = (-.15, .06, .14), (.15, -.04, .14)
# The ball of the foot, ahead of and below the ankle, which a lifted heel pivots on.
FOOT_BALL = (0, .12, -.14)
FOOT_YAW = {"left": 8, "right": -12}
RATES = {"Vocals": 20, "Pipes": 20, "Tambourine": 60}


def smooth(x):
    x = max(0, min(1, x))
    return x*x*(3-2*x)


def bump(offset, width):
    """A raised-cosine pulse centred on zero."""
    return math.cos(math.pi*offset/(2*width))**2 if abs(offset) < width else 0


def window(time, rise, full, fall, end):
    """Zero outside rise..end and one between full..fall, with smooth shoulders."""
    if time <= rise or time >= end: return 0
    if time < full: return smooth((time-rise)/(full-rise))
    if time <= fall: return 1
    return 1-smooth((time-fall)/(end-fall))


def path(points, x):
    """A smooth curve through timed points that starts and ends at rest."""
    index = next((i for i in range(1, len(points)) if points[i][0] >= x), len(points)-1)
    (x0, a), (x1, b) = points[index-1], points[index]
    def tangent(i):
        if i in (0, len(points)-1): return (0,)*len(a)
        (xa, va), (xb, vb) = points[i-1], points[i+1]
        return tuple((q-p)/(xb-xa) for p, q in zip(va, vb))
    t = max(0, min(1, (x-x0)/(x1-x0)))
    width = x1-x0
    m0, m1 = tangent(index-1), tangent(index)
    return [(2*t**3-3*t**2+1)*p + (t**3-2*t**2+t)*width*u + (-2*t**3+3*t**2)*q + (t**3-t**2)*width*v
            for p, q, u, v in zip(a, b, m0, m1)]


def lifted_foot(side, degrees):
    """The ankle target while the heel lifts by pivoting on the ball of the foot."""
    base = LEFT_FOOT if side == "left" else RIGHT_FOOT
    angle, yaw = math.radians(degrees), math.radians(FOOT_YAW[side])
    forward = -FOOT_BALL[1]*math.cos(angle) - FOOT_BALL[2]*math.sin(angle) + FOOT_BALL[1]
    up = FOOT_BALL[1]*math.sin(angle) - FOOT_BALL[2]*math.cos(angle) + FOOT_BALL[2]
    return [base[0] - forward*math.sin(yaw), base[1] + forward*math.cos(yaw), base[2] + up]


def body(root=(0, 0), chest=(0, 0, 0), hips=(0, 0, 0), head=(0, 0, 0), left_heel=0, right_heel=0):
    """The shared trunk controls: weight shift, knee dip, hip and chest counter-rotation, head and heels."""
    left_heel, right_heel = max(0, left_heel), max(0, right_heel)
    return {"root": [root[0], 0, ROOT_HEIGHT + root[1]], "chest": list(chest), "hips": list(hips), "head": list(head),
            "leftFoot": lifted_foot("left", left_heel), "rightFoot": lifted_foot("right", right_heel),
            "leftHeel": left_heel, "rightHeel": right_heel}


def author(motion, rate, pose_at):
    duration = motion["beats"][-1]["time"]
    count = round(duration*rate)
    label = "Playing" if motion["id"].endswith("Loop") else "Accent"
    beats = []
    for index in range(count+1):
        time = min(index/rate, duration)
        beat = {"time": time, "label": label, "pose": pose_at(time, duration)}
        if 0 < index < count: beat["through"] = True
        beats.append(beat)
    motion["beats"] = beats


def accent(phase, peak=.4):
    """A quick attack and a slower release, for half-second accents."""
    return smooth(phase/peak) if phase < peak else 1-smooth((phase-peak)/(1-peak))


# --- Vocals -------------------------------------------------------------------------------------
MIC_HAND = (-.11, .225, 1.63)
STATES = ("Loop", "Low", "Mid", "High", "Flourish")
# One singing clip serves every body, fitted to a Human-height mouth. An Ugnaught's mouth sits lower on its
# head, so its singers play a copy with the microphone hand lowered by this much on the authoring rig.
LOW_MOUTH = "VocalsLowMouth"
LOW_MOUTH_DROP = .10
# A female Wookiee's mouth sits between the two: the ordinary hand covers her nose and the lowered one her throat.
MID_MOUTH = "VocalsMidMouth"
MID_MOUTH_DROP = .05
# The body bank holds 128 clips, so the Wookiee sets leave out the middle accent; the server plays the low one for it.
WOOKIEE_STATES = ("Loop", "Low", "High", "Flourish")
MOUTH_DROPS = {LOW_MOUTH: (LOW_MOUTH_DROP, "Vocals Low Mouth ", STATES),
               MID_MOUTH: (MID_MOUTH_DROP, "Vocals Mid Mouth ", WOOKIEE_STATES)}
MIC_GRIP = (.3, -.72, .62)
MIC_PALM = (.3317419670, -.5323938892, -.7787839094)
FREE_HAND = (.28, .10, 1.08)
# Two sung phrases: an open conversational gesture, then a wider sweep to the side.
VOCAL_GESTURE = [
    (0, FREE_HAND), (.12, (.30, .30, 1.22)), (.25, (.36, .37, 1.31)), (.37, (.33, .30, 1.22)),
    (.50, (.30, .17, 1.12)), (.62, (.46, .16, 1.20)), (.75, (.49, .23, 1.38)), (.88, (.37, .14, 1.16)), (1, FREE_HAND)]


def vocals_pose(name, time, duration):
    drop = next((drop for prefix, (drop, _, _) in MOUTH_DROPS.items() if name.startswith(prefix)), 0)
    phase = time/duration
    hand = list(FREE_HAND)
    if name.endswith("Loop"):
        sway = math.sin(2*math.pi*phase)
        hand = path(VOCAL_GESTURE, phase)
        hand[2] += .010*math.sin(2*math.pi*7*phase)*math.sin(math.pi*phase)**2
        controls = body(root=(.03*sway, -.008*math.sin(2*math.pi*phase)**2),
                     chest=(1.2*math.sin(4*math.pi*phase), -3.0*sway, 2.2*sway), hips=(0, 2.0*sway, -1.8*sway),
                     head=(-5*math.sin(2*math.pi*phase)**2 + 1.8*math.sin(8*math.pi*phase)*math.sin(math.pi*phase),
                           9*sway, 3*math.sin(4*math.pi*phase)),
                     left_heel=7*max(0, sway)**1.5, right_heel=5*max(0, -sway)**1.5)
    elif name.endswith("Flourish"):
        level = window(phase, 0, .30, .62, 1)
        hold = window(phase, .22, .34, .58, .70)
        target = (.52, .14, 1.62)
        hand = [a + (b-a)*level for a, b in zip(FREE_HAND, target)]
        hand[1] += .16*math.sin(math.pi*level)
        hand[2] += .009*math.sin(2*math.pi*6.5*time)*hold
        controls = body(root=(-.012*level, .006*level), chest=(-3.5*level, -4*level, 2*level), hips=(0, 3*level, -2*level),
                     head=(-11*level, 8*level, 0), left_heel=9*level)
    else:
        level = accent(phase)
        reach, chest, head, heel = {
            "Low": ((.02, .14, -.06), (2.5, 0, 0), (7, -4, 0), 0),
            "Mid": ((.08, .20, .14), (0, -3, 0), (-2, 5, 3), 0),
            "High": ((.14, .14, .34), (-2.5, 0, 0), (-9, 0, -2), 6),
        }[next(suffix for suffix in ("Low", "Mid", "High") if name.endswith(suffix))]
        hand = [a + b*level for a, b in zip(FREE_HAND, reach)]
        controls = body(chest=[v*level for v in chest], head=[v*level for v in head], left_heel=heel*level,
                     root=(0, -.006*level if name.endswith("Low") else 0))
    controls.update({
        "leftHand": [MIC_HAND[0], MIC_HAND[1], MIC_HAND[2] - drop], "leftGrip": list(MIC_GRIP),
        "leftPalmFacing": list(MIC_PALM),
        "rightHand": hand, "rightElbow": [.37 + .5*(hand[0]-FREE_HAND[0]), -.04, 1.21 + .35*(hand[2]-FREE_HAND[2])],
        "blade": [.1, .597, -.796], "shield": [0, 0, 0]})
    return controls


# --- Pipes --------------------------------------------------------------------------------------
PIPES_RIGHT = (.14, .05, 1.65)
PIPES_LEFT = (-.13, .13, 1.53)
# The pipes keep their fitted place at the mouth: this is the hold's own facing, so only the elbows come down.
PIPES_FACING = (-.135844, .988957, .059255)
PIPES_RIGHT_ELBOW = (.42, .04, 1.36)
PIPES_LEFT_ELBOW = (-.42, .12, 1.30)
# Eight notes across the row; the pipes slide under the lips between notes and rest on each one.
PIPE_NOTES = (0, .022, .038, .014, -.016, -.038, -.020, 0)
PIPE_SLOT = .65
# A Wookiee's large hands overlap at the mouth and push the pipes aside, so Wookiees play a copy with each
# hand moved outward by this much on the authoring rig.
WIDE_PIPES = "PipesWide"
WIDE_PIPES_SPREAD = .05


def pipes_pose(name, time, duration):
    phase = time/duration
    slide, dip = 0, 0
    if name.endswith("Loop"):
        slot = min(int(time/PIPE_SLOT), len(PIPE_NOTES)-1)
        into = time - slot*PIPE_SLOT
        previous = PIPE_NOTES[slot-1] if slot else 0
        slide = previous + (PIPE_NOTES[slot]-previous)*smooth(into/.20)
        attack = math.sin(math.pi*min(into/.32, 1))**2
        breath = window(time, 2.15, 2.40, 2.44, 2.62) + window(time, 4.72, 4.95, 4.99, 5.18)
        sway = math.sin(2*math.pi*phase)
        dip = .004*attack
        controls = body(root=(.022*sway, -.008*attack), chest=(1.6*attack - 2.2*breath, -2.0*sway, 1.8*sway),
                     hips=(0, 2.0*sway, -1.8*sway), head=(2.0*attack - 3.5*breath, -120*slide, 60*slide),
                     left_heel=5*attack)
    elif name.endswith("Flourish"):
        run = math.sin(2*math.pi*phase)*math.sin(math.pi*phase)
        lift = window(phase, .55, .76, .80, 1)
        slide = -.045*run
        controls = body(root=(.01*run, .005*lift), chest=(-2.5*lift + 1.0*abs(run), 0, 4*run), hips=(0, 0, -2*run),
                     head=(-6*lift, -120*slide, 60*slide), left_heel=7*lift)
    else:
        level = accent(phase)
        if name.endswith("Low"):
            slide = -.035*level
            controls = body(chest=(2*level, 0, -3*level), head=(3*level, 3*level, -2.5*level), root=(0, -.005*level))
        elif name.endswith("High"):
            slide = .035*level
            controls = body(chest=(-1.5*level, 0, 3*level), head=(-4*level, -3*level, 2.5*level), left_heel=5*level)
        else:
            dip = .008*level
            controls = body(chest=(2.5*level, 0, 0), head=(4.5*level, 0, 0), root=(0, -.006*level))
    spread = WIDE_PIPES_SPREAD if name.startswith(WIDE_PIPES) else 0
    controls.update({
        "rightHand": [PIPES_RIGHT[0]+slide+spread, PIPES_RIGHT[1], PIPES_RIGHT[2]-dip],
        "leftHand": [PIPES_LEFT[0]+slide-spread, PIPES_LEFT[1], PIPES_LEFT[2]-dip],
        "blade": [0, -.06, 1], "rightGripAxis": [0, 1, 0], "rightGripNormal": [1, 0, 0],
        "rightGripFacing": list(PIPES_FACING), "rightElbow": list(PIPES_RIGHT_ELBOW), "leftElbow": list(PIPES_LEFT_ELBOW),
        "shield": [-100, 160, 25]})
    return controls


# --- Tambourine ---------------------------------------------------------------------------------
DRUM_HAND = (-.04, .12, 1.40)
DRUM_HOOK = (.01107, 0, -.09613)
PALM_REST, PALM_BACK, PALM_CONTACT, PALM_BOUNCE = .150, .168, .045, .115
# Where the tapping arm bends, and how far the fingers dip toward the skin, in degrees.
PALM_ELBOW = (-.30, .22, 1.38)
PALM_DIP = 20
# Six strikes and two shaken bars over the 5.2 second loop, at 60 Hz frame times.
DRUM_SLOT = .65
DRUM_STRIKES = tuple(slot*DRUM_SLOT + .35 for slot in (0, 1, 2, 4, 5, 6))
DRUM_SHAKES = tuple(slot*DRUM_SLOT + .30 for slot in (3, 7))


def drum_frame(hand, roll, turn):
    """The skin's centre and outward normal for a hand position, a roll about the skin and a turn of its face."""
    blade = [math.sin(math.radians(roll)), 0, math.cos(math.radians(roll))]
    facing = [math.sin(math.radians(turn)), math.cos(math.radians(turn)), 0]
    q = frame_rotation(pose.rotate(DRUM_ATTACHMENT, DRUM_UP), pose.rotate(DRUM_ATTACHMENT, DRUM_NORMAL), blade, facing)
    prop = pose.multiply(q, DRUM_ATTACHMENT)
    centre = tuple(a+b+c for a, b, c in zip(hand, pose.rotate(q, DRUM_HOOK), pose.rotate(prop, DRUM_CENTER)))
    return blade, facing, centre, pose.rotate(prop, unit(DRUM_NORMAL))


def strike(offset, wind=.16, swing=.14, rebound=.09, settle=.23, rest=PALM_REST):
    """Palm distance from the skin around a strike: draw back, accelerate in, bounce off, settle."""
    if offset <= -(wind+swing) or offset >= rebound+settle: return rest
    if offset < -swing: return rest + (PALM_BACK-rest)*smooth((offset+wind+swing)/wind)
    if offset < 0: return PALM_BACK - (PALM_BACK-PALM_CONTACT)*((offset+swing)/swing)**2.2
    if offset < rebound: return PALM_CONTACT + (PALM_BOUNCE-PALM_CONTACT)*(1-(1-offset/rebound)**2)
    return PALM_BOUNCE + (rest-PALM_BOUNCE)*smooth((offset-rebound)/settle)


def meet(offset, swing=.14, tail=.25):
    """The drum comes forward to meet the palm, gives under the hit and springs back."""
    if offset <= -swing or offset >= .05+tail: return 0
    if offset < 0: return .012*smooth((offset+swing)/swing)
    if offset < .05: return .012*math.cos(math.pi*offset/.05)
    return -.012*(1-smooth((offset-.05)/tail))


def rattle(offset, length=.35):
    """The jingles ring on after a hit: a short decaying roll of the frame."""
    if offset <= 0 or offset >= length: return 0
    return 5*math.exp(-offset/.08)*math.sin(2*math.pi*9*offset)*(1-smooth((offset-length+.10)/.10))


def shake(offset, width, rate=7.5):
    """A wrist shake: the frame rolls back and forth while the hand jitters."""
    level = bump(offset, width)
    return 6.5*level*math.sin(2*math.pi*rate*offset), level, .008*level*math.cos(2*math.pi*rate*offset)


def tambourine_pose(name, time, duration):
    phase = time/duration
    distance, give, roll, turn, lift, out, away = PALM_REST, 0, 0, 0, 0, 0, 0
    if name.endswith("Loop"):
        pulse = sum(bump(time-beat, .20) for beat in DRUM_STRIKES)
        step = sum(bump(time-beat+.16, .16) for beat in DRUM_STRIKES)
        distance = PALM_REST + sum(strike(time-beat)-PALM_REST for beat in DRUM_STRIKES)
        give = sum(meet(time-beat) for beat in DRUM_STRIKES)
        roll = sum(rattle(time-beat) for beat in DRUM_STRIKES)
        for centre in DRUM_SHAKES:
            wobble, level, jitter = shake(time-centre, .25)
            roll += wobble
            lift += .03*level + jitter
            out += .02*level
            away += .02*level
        sway = math.sin(4*math.pi*phase)
        controls = body(root=(.03*sway, -.025*pulse), chest=(1.5*pulse, -2.5*sway, 2.0*sway), hips=(0, 3.0*sway, -2.0*sway),
                     head=(3.0*pulse, 5*math.sin(2*math.pi*phase), 3.0*sway), left_heel=11*step)
        lift -= .010*pulse
    elif name.endswith("Flourish"):
        raised = window(time, 0, .35, .90, 1.18)
        shaking = window(time, .25, .35, .85, .95)
        beat = 1.25
        distance = strike(time-beat, .12, .13, .08, .16) + .05*raised
        give = meet(time-beat, .13, .15)
        roll = 6.5*shaking*math.sin(2*math.pi*8*(time-.3)) + rattle(time-beat, .24)
        turn = 8*shaking*math.sin(2*math.pi*(time-.3)/.6)
        lift = .10*raised + .010*shaking*math.cos(2*math.pi*8*(time-.3)) - .012*bump(time-beat, .15)
        out = .06*raised
        away = .02*raised
        controls = body(root=(.01*raised, .006*raised - .014*bump(time-beat, .18)), chest=(-3*raised + 2*bump(time-beat, .2), 0, 0),
                     hips=(0, 2*raised, 0), head=(-7*raised + 4*bump(time-beat, .2), 0, 3*raised), left_heel=8*raised)
    elif name.endswith("Mid"):
        # Two quick taps.
        keys = [(0, PALM_REST), (.01, PALM_REST), (.06, .162), (.15, PALM_CONTACT), (.21, .105), (.26, .112),
                (.35, PALM_CONTACT), (.42, PALM_BOUNCE), (.5, PALM_REST)]
        index = next(i for i in range(1, len(keys)) if keys[i][0] >= time)
        (t0, a), (t1, b) = keys[index-1], keys[index]
        u = (time-t0)/(t1-t0)
        distance = a + (b-a)*(u**2.2 if b == PALM_CONTACT else 1-(1-u)**2 if a == PALM_CONTACT else smooth(u))
        give = meet(time-.15, .09, .08) + meet(time-.35, .09, .09)
        roll = rattle(time-.15, .18) + rattle(time-.35, .14)
        level = accent(phase)
        controls = body(root=(0, -.010*(bump(time-.15, .1)+bump(time-.35, .1))), chest=(1.5*level, 2*level, 0),
                     head=(3*level, 0, 3*level))
    else:
        low = name.endswith("Low")
        beat = .25
        level = accent(phase, .5)
        distance = strike(time-beat, .10, .11, .07, .17)
        give = meet(time-beat, .11, .18)
        if low:
            roll = rattle(time-beat, .24)
            lift = -.03*level
            controls = body(root=(0, -.014*bump(time-beat, .2)), chest=(2*level, 0, 0), head=(5*level, 0, 0))
        else:
            wobble, shaking, jitter = shake(time-.37, .11, 9)
            roll = wobble
            lift = .07*level + jitter
            controls = body(root=(0, .004*level), chest=(-1.5*level, 0, 0), head=(-4*level, 0, 0), left_heel=6*level)
    sway_x, dip = controls["root"][0], controls["root"][2]-ROOT_HEIGHT
    hand = [DRUM_HAND[0] + sway_x + out, DRUM_HAND[1] + give, DRUM_HAND[2] + dip + lift]
    blade, facing, centre, normal = drum_frame(hand, roll, turn)
    palm = [centre[i] + normal[i]*distance + (-.055-away if i == 0 else .035 if i == 2 else 0) for i in range(3)]
    # The palm faces the skin with the fingers dipping toward it, and the hand lies along the forearm
    # rather than bending back at the wrist.
    elbow = (PALM_ELBOW[0] + sway_x, PALM_ELBOW[1], PALM_ELBOW[2] + dip + .4*lift)
    toward = tuple(a-b for a, b in zip(elbow, palm))
    along = unit(tuple(a-b*dot(toward, normal) for a, b in zip(toward, normal)))
    tilt = math.radians(PALM_DIP)
    wrist = tuple(a*math.cos(tilt) + b*math.sin(tilt) for a, b in zip(along, normal))
    facing_palm = tuple(-b*math.cos(tilt) + a*math.sin(tilt) for a, b in zip(along, normal))
    controls.update({
        "rightHand": hand, "blade": blade, "rightGripAxis": list(DRUM_UP), "rightGripNormal": list(DRUM_NORMAL),
        "rightGripFacing": facing, "rightElbow": [.38 + .5*out, .02, 1.18 + dip + .6*lift],
        "leftHand": palm, "leftElbow": list(elbow), "shield": [0, 0, 0],
        "leftGrip": list(cross(wrist, facing_palm)), "leftPalmFacing": list(facing_palm)})
    return controls


def refine(source):
    result = copy.deepcopy(source)
    # The low-mouth singing clips are copies of the ordinary ones, authored again with the lowered hand.
    result["motions"] = [motion for motion in result["motions"]
                         if not motion["id"].startswith((*MOUTH_DROPS, WIDE_PIPES))]
    for prefix, (_, label, states) in MOUTH_DROPS.items():
        for state in states:
            variant = copy.deepcopy(next(motion for motion in result["motions"] if motion["id"] == "Vocals" + state))
            variant["id"], variant["name"] = prefix + state, label + state
            result["motions"].append(variant)
    # The wide pipes clips are copies of the ordinary ones, authored again with the hands apart.
    for state in WOOKIEE_STATES:
        variant = copy.deepcopy(next(motion for motion in result["motions"] if motion["id"] == "Pipes" + state))
        variant["id"], variant["name"] = WIDE_PIPES + state, "Pipes Wide " + state
        result["motions"].append(variant)
    for motion in result["motions"]:
        name = motion["id"]
        duration = motion["beats"][-1]["time"]
        if name.startswith("Vocals"):
            motion["relaxRightHand"] = True
            motion["leftHandFollowsHead"] = True
            author(motion, RATES["Vocals"], lambda time, length, name=name: vocals_pose(name, time, length))
            motion["interpretation"] = "The singer shifts weight, turns and lifts the head with each phrase and gestures from the elbow with the free hand; the microphone stays at the lips as the head moves."
            if name.startswith(tuple(MOUTH_DROPS)):
                motion["interpretation"] += " The hand is lowered for a mouth set low on the head."
        elif name.startswith("Pipes"):
            motion["naturalGrip"] = False
            motion["leftHandFollowsHead"] = True
            motion["rightHandFollowsHead"] = True
            author(motion, RATES["Pipes"], lambda time, length, name=name: pipes_pose(name, time, length))
            motion["interpretation"] = "The pipes slide under the lips from note to note and rest on each one, with a breath between phrases; the head leads each slide and both hands stay with the mouth."
            if name.startswith(WIDE_PIPES):
                motion["interpretation"] += " The hands are set apart for large hands that would overlap at the mouth."
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
            author(motion, RATES["Tambourine"], lambda time, length, name=name: tambourine_pose(name, time, length))
            motion["interpretation"] = "The drum is held in front of the chest with its face outward and its ribbons down; the free palm draws back, strikes and bounces off the skin on the beat while the knees, head and a tapping heel keep time, and the wrist shakes the jingles between strikes."
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
