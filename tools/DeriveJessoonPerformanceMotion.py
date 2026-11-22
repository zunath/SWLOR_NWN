"""Derive the Flanged Jessoon playing clips from the horn clips.

The horn clips were posed for the kloo horn: both hands sit in the top third of the instrument, eleven centimetres
apart, with the left hand clear of the slim Jessoon's body. The Jessoon is a long straight horn (wmgfs_m_211,
"bithsax"), so its clips keep the horn clips' body motion and move only the arms.

The male Human set is the reference: the instrument stays where the horn clips hold it, the right hand slides down the
body and the left hand closes on the tube just under the flange, gripping it the way the right hand grips it
(mirrored). Each hand keeps the horn clip's motion along the tube, so fingering and accents are unchanged.

Every other body plays the same instrument, so its hands belong at the same places on it, but the bodies' arms, heads
and hands differ too much for one copy. Each distinct body (skeleton, hand part and mouth) therefore gets its own set:
it holds the Jessoon at that body's mouth at the angle the reference holds it to the head, and puts each visible hand
(the centre of the hand mesh, not the hand node, which sits differently in each body's hand) where the reference hand
sits on the instrument, turned the same way, in every key. Where a short arm cannot reach (the horn flourish swings the
bell far out), the instrument swings toward that shoulder about the mouthpiece just far enough (SETS). `PerformanceAnimations.FitToInstrument` picks the
set by body, and tools/SolveJessoonFits.py then solves every body's HornFit for the set it plays.

    python -B tools/DeriveJessoonPerformanceMotion.py          # write the Jessoon projects
    python -B tools/DeriveJessoonPerformanceMotion.py --check  # confirm the committed projects are current
    python -B tools/DeriveJessoonPerformanceMotion.py --holds  # print the largest swing each set needs
"""
from __future__ import annotations

import argparse
import copy
import json
import math
import sys
from pathlib import Path

import numpy as np
from scipy.spatial.transform import Rotation as R

sys.path.insert(0, str(Path(__file__).resolve().parent))
from swg_animation import between, set_world_rotation, solve_limb, world  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
PERFORMANCE = ROOT / 'design' / 'animations' / 'performance'
STATES = ('Loop', 'Low', 'Mid', 'High', 'Flourish')

# Centreline of the Jessoon's straight body and its mouthpiece, measured from wmgfs_m_211 (model metres). The flange
# sits above y = 0.13 and the bell bends away below y = -0.28.
BODY_LOW = np.array([0.077, -0.26, -0.025])
BODY_HIGH = np.array([-0.055, 0.14, -0.001])
TIP = np.array([-0.060, 0.318, 0.035])
# Model y along the body where the reference left hand's grip sits.
LEFT_HAND_GRIP_Y = .07
# The reference right hand slides this far down the body (HornFit.JessoonHold slides the Jessoon up by the same).
RIGHT_HAND_SLIDE = .06

# HornFit.JessoonOnKlooHorn: the horn clips hold the Jessoon there on the male Human body.
ALIGNED_FIT = (-.035, .065, -.017, 12.3, -152.4, 28.6)

# Local bone offsets of the arm and head chain, read from the compiled body models. The authoring project carries the
# male Human (pmh0) skeleton, which the droid bodies share.
SKELETONS = {
    'pfh0': {
        'torso_g': (0.0004, 0.0103, 0.0002), 'neck_g': (-0.0011, -0.045, 0.3464), 'head_g': (0.0003, 0.0186, 0.1114),
        'rbicep_g': (0.1376, -0.0356, 0.3055), 'rforearm_g': (0.032, -0.0249, -0.3163),
        'rhand_g': (0.0254, 0.0139, -0.2524), 'rhand': (0.001, 0.0104, -0.0819),
        'lbicep_g': (-0.1395, -0.0356, 0.3055), 'lforearm_g': (-0.032, -0.0249, -0.3163),
        'lhand_g': (-0.0254, 0.0139, -0.2524), 'lhand': (-0.001, 0.0103, -0.0821),
    },
    'pme0': {
        'torso_g': (0.0004, 0.0108, 0.0002), 'neck_g': (0.0, -0.0671, 0.415), 'head_g': (0.0, 0.0147, 0.0807),
        'rbicep_g': (0.173, -0.0655, 0.323), 'rforearm_g': (0.028, 0.0172, -0.2644),
        'rhand_g': (0.0231, 0.0174, -0.2563), 'rhand': (0.0111, 0.0, -0.0793),
        'lbicep_g': (-0.173, -0.0655, 0.323), 'lforearm_g': (-0.028, 0.0172, -0.2644),
        'lhand_g': (-0.0231, 0.0174, -0.2563), 'lhand': (-0.0153, 0.0, -0.0777),
    },
    'pfe0': {
        'torso_g': (0.0007, 0.0107, -0.0005), 'neck_g': (-0.0011, -0.04, 0.3084), 'head_g': (0.0003, -0.0014, 0.0872),
        'rbicep_g': (0.1176, -0.0296, 0.2755), 'rforearm_g': (0.023, -0.0199, -0.2743),
        'rhand_g': (0.0179, 0.0099, -0.2194), 'rhand': (0.004, -0.0046, -0.06),
        'lbicep_g': (-0.1195, -0.0296, 0.2755), 'lforearm_g': (-0.023, -0.0199, -0.2743),
        'lhand_g': (-0.0179, 0.0099, -0.2194), 'lhand': (-0.004, 0.0028, -0.06),
    },
    'pmo0': {
        'torso_g': (0.0004, 0.0108, 0.0002), 'neck_g': (0.0, -0.0811, 0.599), 'head_g': (0.0, 0.0187, 0.0867),
        'rbicep_g': (0.301, -0.0795, 0.46), 'rforearm_g': (0.059, 0.0212, -0.4119),
        'rhand_g': (0.0461, 0.0354, -0.3968), 'rhand': (0.0111, 0.0, -0.0961),
        'lbicep_g': (-0.301, -0.0795, 0.46), 'lforearm_g': (-0.059, 0.0212, -0.4119),
        'lhand_g': (-0.0461, 0.0354, -0.3968), 'lhand': (-0.0153, 0.0, -0.0945),
    },
    'pfo0': {
        'torso_g': (0.0007, 0.0107, -0.0005), 'neck_g': (-0.0011, -0.064, 0.4834), 'head_g': (0.0003, 0.0096, 0.0782),
        'rbicep_g': (0.2756, -0.0506, 0.4235), 'rforearm_g': (0.053, -0.0419, -0.3788),
        'rhand_g': (0.0434, 0.0309, -0.3649), 'rhand': (0.001, 0.0104, -0.1172),
        'lbicep_g': (-0.2775, -0.0506, 0.4235), 'lforearm_g': (-0.053, -0.0419, -0.3788),
        'lhand_g': (-0.0434, 0.0309, -0.3649), 'lhand': (-0.001, 0.0103, -0.1175),
    },
    'pmd0': {
        'torso_g': (0.0004, 0.0074, 0.0002), 'neck_g': (0.0, -0.0617, 0.411), 'head_g': (0.0, 0.0227, 0.0637),
        'rbicep_g': (0.206, -0.0551, 0.3155), 'rforearm_g': (0.041, 0.0162, -0.2834),
        'rhand_g': (0.0321, 0.0269, -0.2718), 'rhand': (0.0111, 0.0, -0.0883),
        'lbicep_g': (-0.206, -0.0551, 0.3155), 'lforearm_g': (-0.041, 0.0162, -0.2834),
        'lhand_g': (-0.0321, 0.0269, -0.2718), 'lhand': (-0.0153, 0.0, -0.0867),
    },
    'pfd0': {
        'torso_g': (0.0007, 0.0117, 0.0025), 'neck_g': (-0.0011, -0.0467, 0.3446), 'head_g': (0.0003, 0.0043, 0.073),
        'rbicep_g': (0.1956, -0.0366, 0.306), 'rforearm_g': (0.04, -0.0349, -0.2758),
        'rhand_g': (0.0344, 0.0269, -0.2544), 'rhand': (-0.002, 0.0104, -0.089),
        'lbicep_g': (-0.1975, -0.0366, 0.306), 'lforearm_g': (-0.04, -0.0349, -0.2758),
        'lhand_g': (-0.0344, 0.0269, -0.2544), 'lhand': (0.0019, 0.0103, -0.0892),
    },
}

# Centre of each hand mesh (right, left) in the hand_g frame, read from the compiled part models, by skeleton and the
# hand part a species wears (RacialAppearanceDefinition.RightHand/LeftHand).
PALMS = {
    ('pmh0', 1): ((0.0075, 0.0138, -0.1007), (-0.0128, 0.0383, -0.095)),
    ('pfh0', 1): ((0.0005, 0.0309, -0.0877), (0.005, 0.0242, -0.0935)),
    ('pmh0', 201): ((-0.0019, 0.0235, -0.101), (0.0019, 0.0235, -0.101)),
    ('pfh0', 201): ((-0.0111, 0.0196, -0.0796), (-0.0069, 0.0259, -0.064)),
    ('pmh0', 204): ((0.014, 0.0465, -0.1059), (-0.014, 0.0465, -0.1059)),
    ('pfh0', 204): ((0.0102, 0.0096, -0.0669), (-0.0102, 0.0096, -0.0669)),
    ('pme0', 1): ((0.006, 0.0095, -0.0806), (-0.0102, 0.0264, -0.076)),
    ('pfe0', 1): ((0.0004, 0.0248, -0.0746), (0.004, 0.0194, -0.0795)),
    ('pme0', 44): ((0.0163, 0.0112, -0.1181), (-0.0163, 0.0112, -0.1181)),
    ('pme0', 208): ((0.0154, 0.0362, -0.0903), (-0.0149, 0.0356, -0.0887)),
    ('pfe0', 208): ((0.0018, 0.0326, -0.1017), (-0.0018, 0.0326, -0.1017)),
    ('pmo0', 201): ((0.0282, 0.0272, -0.0938), (-0.0282, 0.0272, -0.0938)),
    ('pfo0', 201): ((0.0282, 0.0272, -0.0938), (-0.0282, 0.0272, -0.0938)),
    ('pmd0', 1): ((0.0094, 0.0172, -0.1007), (-0.0163, 0.0479, -0.095)),
    ('pfd0', 1): ((0.0011, 0.0166, -0.0824), (-0.0013, 0.0166, -0.0824)),
    ('pmd0', 209): ((0.0179, 0.0308, -0.0785), (-0.0128, 0.0548, -0.0766)),
    ('pfd0', 209): ((0.0179, 0.0308, -0.0785), (-0.0128, 0.0548, -0.0766)),
}

# Each playable species' skeleton (h Human, e elf, o half-orc, d dwarf; the droids share the male Human's) and hand part.
RIGS = {'Human': 'h', 'Chiss': 'h', 'Zabrak': 'h', 'Twilek': 'h', 'Mirialan': 'h', 'Echani': 'h', 'KelDor': 'h',
        'Cyborg': 'h', 'Togruta': 'h', 'Nautolan': 'h', 'Trandoshan': 'h', 'MonCalamari': 'h', 'Bothan': 'e',
        'Rodian': 'e', 'Wookiee': 'e', 'Cathar': 'o', 'Ugnaught': 'd', 'Ewok': 'd', 'Droid': 'h'}
HANDS = {'Cathar': 201, 'Ewok': 209, 'KelDor': 45, 'MonCalamari': 204, 'Rodian': 44, 'Trandoshan': 201, 'Wookiee': 208}


def skeleton_of(species, gender):
    return 'pmh0' if species == 'Droid' else f'p{gender.lower()}{RIGS[species]}0'


# The kloo horn fits that put its mouthpiece at each body's mouth, seen close up in the client (HornFit).
FEMALE_KLOO = {'Human', 'Chiss', 'Zabrak', 'Twilek', 'Mirialan', 'Echani', 'KelDor', 'Cyborg', 'Trandoshan', 'Togruta',
               'Nautolan', 'Bothan', 'Rodian'}
KLOO = {('MonCalamari', 'M'): (0, 0, 0, -8, 0, 0), ('MonCalamari', 'F'): (0, 0, 0, -10, 0, 0),
        ('Wookiee', 'M'): (0, 0, 0, -8, 0, 0), ('Wookiee', 'F'): (.03, 0, .03, -12, 0, 0),
        ('Cathar', 'F'): (0, 0, 0, -12, 0, 0), ('Trandoshan', 'M'): (0, 0, 0, -8, 0, 0),
        ('Rodian', 'M'): (0, 0, 0, -8, 0, 0), **{(s, 'F'): (.02, 0, 0, -13, 0, 0) for s in FEMALE_KLOO}}


def mouth_fit(species, gender):
    """The Jessoon fit on the horn clips that puts its mouthpiece where that body's kloo horn mouthpiece sits."""
    if species == 'Cathar' and gender == 'M':
        return ALIGNED_FIT
    if species == 'Ugnaught':
        return (-.018, -.003, -.018, 12.3, -152.4, 28.6)
    fit = compose(KLOO.get((species, gender), (0, 0, 0, 0, 0, 0)), ALIGNED_FIT)
    if gender == 'F' and (species in FEMALE_KLOO or species == 'MonCalamari'):
        fit = (0, fit[1], 0, *fit[3:])
    return fit


REFERENCE = 'JessoonHuman'
# Project name prefix -> the body the set is solved on. The other bodies play the set of the body they match: KelDor and
# the droids the Human sets, and the Rodian female the Bothan female's (tools/SolveJessoonFits.py).
SETS = {
    'JessoonHumanFemale': ('Human', 'F'), 'JessoonTrandoshan': ('Trandoshan', 'M'),
    'JessoonTrandoshanFemale': ('Trandoshan', 'F'), 'JessoonMonCalamari': ('MonCalamari', 'M'),
    'JessoonMonCalamariFemale': ('MonCalamari', 'F'), 'JessoonBothan': ('Bothan', 'M'),
    'JessoonBothanFemale': ('Bothan', 'F'), 'JessoonRodian': ('Rodian', 'M'), 'JessoonWookiee': ('Wookiee', 'M'),
    'JessoonWookieeFemale': ('Wookiee', 'F'), 'JessoonCathar': ('Cathar', 'M'), 'JessoonCatharFemale': ('Cathar', 'F'),
    'JessoonUgnaught': ('Ugnaught', 'M'), 'JessoonUgnaughtFemale': ('Ugnaught', 'F'), 'JessoonEwok': ('Ewok', 'M'),
    'JessoonEwokFemale': ('Ewok', 'F'),
}
# A short arm cannot follow the horn flourish, which swings the bell far out: in those keys the instrument swings toward
# that shoulder about the mouthpiece until the arm is this much short of straight, turning at most MAX_SWING degrees.
SLACK = .01
MAX_SWING = 40


def fit_rotation(fit) -> np.ndarray:
    """HornFitTransform's convention: RotateX about model Z, RotateY about X, RotateZ about Y, composed X * Y * Z."""
    def about(axis, degrees):
        return R.from_rotvec(np.radians(degrees) * np.eye(3)[axis]).as_matrix()
    _, _, _, rotate_y, rotate_z, rotate_x = fit
    return about(2, rotate_x) @ about(0, rotate_y) @ about(1, rotate_z)


def fit_from(translation, rotation) -> tuple:
    """The HornFit channels (x, y, z, RotateY, RotateZ, RotateX) of a translation and rotation matrix."""
    r = np.asarray(rotation)
    return (*(float(v) for v in translation), math.degrees(math.asin(max(-1, min(1, r[2, 1])))),
            math.degrees(math.atan2(-r[2, 0], r[2, 2])), math.degrees(math.atan2(-r[0, 1], r[1, 1])))


def compose(outer, inner) -> tuple:
    """HornFitTransform.Compose: the inner fit applied in the outer fit's frame."""
    ro, ri = fit_rotation(outer), fit_rotation(inner)
    return fit_from(ro @ np.asarray(inner[:3]) + np.asarray(outer[:3]), ro @ ri)


AXIS = (BODY_HIGH - BODY_LOW) / np.linalg.norm(BODY_HIGH - BODY_LOW)
LEFT_GRIP_ALONG = (LEFT_HAND_GRIP_Y - BODY_LOW[1]) / AXIS[1]
MIRROR = np.array([-1.0, 1.0, 1.0])  # the hand frames mirror across local X (rhand X +0.011, lhand X -0.015)
# HornFit.JessoonHold: the reference set's fit, the aligned fit slid up through the right hand.
JESSOON_HOLD = compose(ALIGNED_FIT, (*(AXIS * RIGHT_HAND_SLIDE), 0, 0, 0))


def position(pose, index) -> np.ndarray:
    return np.array([pose[index]['Position'][axis] for axis in 'XYZ'])


class Hold:
    """The Jessoon's frame in the world, where the playing fit hangs it from the right hand."""

    def __init__(self, joints, pose, index, fit):
        positions, rotations = world(joints, pose)
        hand, hand_rotation = positions[index['rhand']], rotations[index['rhand']]
        self.rotation = hand_rotation * R.from_matrix(fit_rotation(fit))
        self.origin = hand + hand_rotation.apply(np.array(fit[:3]))

    def to_model(self, point):
        return self.rotation.inv().apply(np.asarray(point) - self.origin)

    def to_world(self, point):
        return self.origin + self.rotation.apply(point)

    def along(self, point):
        return float(np.dot(self.to_model(point) - BODY_LOW, AXIS))


def on_skeleton(project, skeleton):
    """A working copy of the project whose arm and head chain has the given skeleton's bone offsets."""
    result = copy.deepcopy(project)
    if skeleton == 'pmh0':
        return result
    offsets = SKELETONS[skeleton]
    for i, joint in enumerate(result['Joints']):
        if joint['Name'] in offsets:
            value = dict(zip('XYZ', offsets[joint['Name']]))
            joint['Rest']['Position'] = dict(value)
            for key in result['Keys']:
                key['Pose'][i]['Position'] = dict(value)
    return result


def right_grip(joints, pose, index, fit):
    """The body's axis relative to the right hand: where it passes through the fist and which way it runs."""
    hold = Hold(joints, pose, index, fit)
    positions, rotations = world(joints, pose)
    hand, hand_rotation = positions[index['rhand']], rotations[index['rhand']]
    model = hold.to_model(hand)
    nearest = BODY_LOW + AXIS * np.dot(model - BODY_LOW, AXIS)
    offset = hand_rotation.inv().apply(hold.to_world(nearest) - hand)
    direction = hand_rotation.inv().apply(hold.rotation.apply(AXIS))
    return offset, direction


def slide_hands(project, fit, slide, grip, left_rest_along):
    """The reference: move the hands along the instrument in every key; returns the worst miss of each hand's target."""
    joints = project['Joints']
    index = {joint['Name']: i for i, joint in enumerate(joints)}
    right_wrist, left_wrist = index['rhand_g'], index['lhand_g']
    right_offset, right_direction = grip
    left_offset, left_direction = right_offset * MIRROR, right_direction * MIRROR
    worst = 0.0
    for key in project['Keys']:
        pose = key['Pose']
        hold = Hold(joints, pose, index, fit)
        axis_world = hold.rotation.apply(AXIS)
        positions, rotations = world(joints, pose)

        # Right hand: the same grip, slid down the body; the instrument stays where it is.
        hand = positions[index['rhand']] - axis_world * slide
        wrist_target = hand - rotations[right_wrist].apply(position(pose, index['rhand']))
        solve_limb(joints, pose, right_wrist, wrist_target)
        set_world_rotation(joints, pose, right_wrist, rotations[right_wrist])

        # Left hand: on the tube at its grip, moving along the body as the horn clip's left hand does.
        along = LEFT_GRIP_ALONG + hold.along(positions[index['lhand']]) - left_rest_along
        left_rotation = rotations[index['lhand']]
        left_rotation = between(left_rotation.apply(left_direction), axis_world) * left_rotation
        left_hand = hold.to_world(BODY_LOW + AXIS * along) - left_rotation.apply(left_offset)
        left_target = left_hand - left_rotation.apply(position(pose, index['lhand']))
        solve_limb(joints, pose, left_wrist, left_target)
        set_world_rotation(joints, pose, left_wrist, left_rotation)

        solved, _ = world(joints, pose)
        worst = max(worst, float(np.linalg.norm(solved[index['rhand']] - hand)),
                    float(np.linalg.norm(solved[index['lhand']] - left_hand)))
    return worst


def build_reference(sources):
    joints = sources['Loop']['Joints']
    index = {joint['Name']: i for i, joint in enumerate(joints)}
    hold_pose = sources['Loop']['Keys'][0]['Pose']
    grip = right_grip(joints, hold_pose, index, ALIGNED_FIT)
    positions, _ = world(joints, hold_pose)
    left_rest_along = Hold(joints, hold_pose, index, ALIGNED_FIT).along(positions[index['lhand']])
    solved = {state: copy.deepcopy(sources[state]) for state in STATES}
    worst = max(slide_hands(solved[state], ALIGNED_FIT, RIGHT_HAND_SLIDE, grip, left_rest_along) for state in STATES)
    if worst > .002:
        raise ValueError(f'{REFERENCE}: the hands cannot reach the instrument')
    return solved


def mouth(project, fit):
    """Where the mouthpiece sits in the head frame on the horn clip, with that body's horn fit."""
    joints = project['Joints']
    index = {joint['Name']: i for i, joint in enumerate(joints)}
    head = index['head_g']
    tips = []
    for key in project['Keys']:
        positions, rotations = world(joints, key['Pose'])
        tip = Hold(joints, key['Pose'], index, fit).to_world(TIP)
        tips.append(rotations[head].inv().apply(tip - positions[head]))
    return np.mean(tips, axis=0)


def reference_hold(project, index):
    """Per key of the reference: the instrument in the head frame, and each hand's centre and turn on the instrument."""
    joints = project['Joints']
    result = []
    for key in project['Keys']:
        pose = key['Pose']
        positions, rotations = world(joints, pose)
        hold = Hold(joints, pose, index, JESSOON_HOLD)
        head_position, head_rotation = positions[index['head_g']], rotations[index['head_g']]
        hands = {}
        for side, wrist in enumerate(('rhand_g', 'lhand_g')):
            palm = positions[index[wrist]] + rotations[index[wrist]].apply(PALMS[('pmh0', 1)][side])
            hands[wrist] = (hold.to_model(palm), hold.rotation.inv() * rotations[index[wrist]])
        result.append((head_rotation.inv() * hold.rotation,
                       head_rotation.inv().apply(hold.to_world(TIP) - head_position), hands))
    return result


def arm(joints, positions, index, wrist):
    """The shoulder and the two bone lengths of the arm ending at that wrist."""
    end = index[wrist]
    middle = joints[end]['Parent']
    root = joints[middle]['Parent']
    return (positions[root], np.linalg.norm(positions[middle] - positions[root]),
            np.linalg.norm(positions[end] - positions[middle]))


def hand_targets(rotation, origin, hands, palms):
    """Where each wrist goes, and how it turns, to put the hand's centre where the reference's sits."""
    result = {}
    for side, wrist in enumerate(('rhand_g', 'lhand_g')):
        palm, turn = hands[wrist]
        hand_rotation = rotation * turn
        result[wrist] = (origin + rotation.apply(palm) - hand_rotation.apply(palms[side]), hand_rotation)
    return result


def hold_key(joints, positions, rotations, index, reference_key, palms, own_mouth, reference_mouth):
    """The instrument and wrist targets for one key, swung toward the right shoulder if that arm cannot reach.

    The instrument keeps the reference's angle to the head with its mouthpiece at this body's mouth. When the right
    wrist's target lies beyond the arm, the instrument turns about the mouthpiece, in the plane of the mouthpiece, that
    target and the shoulder, just far enough to bring it within reach. Returns the instrument's rotation and origin, the
    wrist targets and the swing in degrees, or None when no swing up to MAX_SWING reaches.
    """
    head_turn, tip, hands = reference_key
    head = index['head_g']
    rotation = rotations[head] * head_turn
    mouthpiece = positions[head] + rotations[head].apply(own_mouth + tip - reference_mouth)
    shoulder, upper, lower = arm(joints, positions, index, 'rhand_g')

    def place(angle, axis):
        turned = R.from_rotvec(np.radians(angle) * axis) * rotation if angle else rotation
        return turned, mouthpiece - turned.apply(TIP)

    def short(turned, origin):
        target = hand_targets(turned, origin, hands, palms)['rhand_g'][0]
        return np.linalg.norm(target - shoulder) - (upper + lower - SLACK)

    turned, origin = place(0, None)
    swing = 0.0
    if short(turned, origin) > 0:
        target = hand_targets(turned, origin, hands, palms)['rhand_g'][0]
        axis = np.cross(target - mouthpiece, shoulder - mouthpiece)
        axis /= np.linalg.norm(axis)
        if short(*place(MAX_SWING, axis)) > 0:
            return None
        low, high = 0.0, float(MAX_SWING)
        for _ in range(30):
            middle = (low + high) / 2
            low, high = (middle, high) if short(*place(middle, axis)) > 0 else (low, middle)
        turned, origin = place(high, axis)
        swing = high
    targets = hand_targets(turned, origin, hands, palms)
    for wrist, (target, _) in targets.items():
        root, first, second = arm(joints, positions, index, wrist)
        if not abs(first - second) + .002 <= np.linalg.norm(target - root) <= first + second - .002:
            return None
    return turned, origin, targets, swing


def place_hands(project, palms, reference, reference_mouth, own_mouth):
    """Hold the instrument at the body's mouth and put its hands where the reference's are.

    Returns the worst miss of a wrist target and the instrument's fit in the right hand per key, or None when an arm
    cannot reach.
    """
    joints = project['Joints']
    index = {joint['Name']: i for i, joint in enumerate(joints)}
    worst, fits = 0.0, []
    for key, reference_key in zip(project['Keys'], reference):
        pose = key['Pose']
        positions, rotations = world(joints, pose)
        held = hold_key(joints, positions, rotations, index, reference_key, palms, own_mouth, reference_mouth)
        if held is None:
            return None
        rotation, origin, targets, _ = held
        for wrist, (target, hand_rotation) in targets.items():
            solve_limb(joints, pose, index[wrist], target)
            set_world_rotation(joints, pose, index[wrist], hand_rotation)
        solved, turned = world(joints, pose)
        worst = max([worst] + [float(np.linalg.norm(solved[index[wrist]] - target)) for wrist, (target, _) in targets.items()])
        hand, hand_rotation = solved[index['rhand']], turned[index['rhand']]
        fits.append(fit_from(hand_rotation.inv().apply(origin - hand), (hand_rotation.inv() * rotation).as_matrix()))
    return worst, fits


class Body:
    """A set's body: its skeleton's copies of the horn clips, its hands, its mouth and the reference hold per key."""

    def __init__(self, prefix, sources, reference):
        species, gender = SETS[prefix]
        skeleton = skeleton_of(species, gender)
        self.palms = PALMS[(skeleton, HANDS.get(species, 1))]
        self.working = {state: on_skeleton(sources[state], skeleton) for state in STATES}
        index = {joint['Name']: i for i, joint in enumerate(self.working['Loop']['Joints'])}
        self.reference_mouth = mouth(sources['Loop'], mouth_fit('Human', 'M'))
        self.own_mouth = mouth(self.working['Loop'], mouth_fit(species, gender))
        self.holds = {state: reference_hold(reference[state], index) for state in STATES}

    def largest_swing(self):
        """The most any key swings the instrument, or None when an arm cannot reach in some key."""
        largest = 0.0
        for state in STATES:
            project = self.working[state]
            joints = project['Joints']
            index = {joint['Name']: i for i, joint in enumerate(joints)}
            for key, reference_key in zip(project['Keys'], self.holds[state]):
                positions, rotations = world(joints, key['Pose'])
                held = hold_key(joints, positions, rotations, index, reference_key, self.palms, self.own_mouth,
                                self.reference_mouth)
                if held is None:
                    return None
                largest = max(largest, held[3])
        return largest


def build_variant(prefix, sources, reference):
    body = Body(prefix, sources, reference)
    solved = {state: copy.deepcopy(body.working[state]) for state in STATES}
    results = {state: place_hands(solved[state], body.palms, body.holds[state], body.reference_mouth, body.own_mouth)
               for state in STATES}
    if any(result is None or result[0] > .002 for result in results.values()):
        raise ValueError(f'{prefix}: the hands cannot reach their places')
    fits = np.array([fit for _, state_fits in results.values() for fit in state_fits])
    spread = fits.max(axis=0) - fits.min(axis=0)
    if max(spread[:3]) > .004 or max(spread[3:]) > .5:
        raise ValueError(f'{prefix}: the instrument moves in the right hand {spread}')
    return solved, tuple(float(v) for v in fits[0])


def write_projects(prefix, sources, solved):
    outputs = {}
    for state in STATES:
        # Keep the authoring project's skeleton: installation writes the solved rotations, and each body plays them on its own bones.
        project = copy.deepcopy(sources[state])
        project['Name'] = prefix + state
        for key, solved_key in zip(project['Keys'], solved[state]['Keys']):
            for joint, solved_joint in zip(key['Pose'], solved_key['Pose']):
                orientation = {axis: solved_joint['Orientation'][axis] for axis in 'XYZW'}
                if orientation != {axis: joint['Orientation'][axis] for axis in 'XYZW'}:
                    joint['Orientation'] = {'IsIdentity': False, **orientation}
        outputs[prefix + state] = json.dumps(project, indent=2, ensure_ascii=False) + '\n'
    return outputs


def sources_and_reference():
    sources = {state: json.loads((PERFORMANCE / f'Horn{state}.swlanim').read_text(encoding='utf-8-sig')) for state in STATES}
    return sources, build_reference(sources)


def build():
    sources, reference = sources_and_reference()
    outputs = write_projects(REFERENCE, sources, reference)
    fits = {REFERENCE: JESSOON_HOLD}
    for prefix in SETS:
        solved, fits[prefix] = build_variant(prefix, sources, reference)
        outputs.update(write_projects(prefix, sources, solved))
    return outputs, fits


def find_holds():
    """The largest swing each set needs."""
    sources, reference = sources_and_reference()
    for prefix in SETS:
        swing = Body(prefix, sources, reference).largest_swing()
        print(f'{prefix}: ' + ('out of reach' if swing is None else f'swings at most {swing:.1f} degrees'), flush=True)


def comparable(value):
    """Installation rewrites projects with float32 values and its own number format; compare what it keeps."""
    if isinstance(value, dict):
        return {key: comparable(item) for key, item in value.items() if key != 'IsIdentity'}
    if isinstance(value, list):
        return [comparable(item) for item in value]
    if isinstance(value, float):
        return 0.0 if abs(value) < 1e-12 else float(np.float32(value))
    return value


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--check', action='store_true', help='fail when the committed projects differ')
    parser.add_argument('--holds', action='store_true', help='print the largest swing each set needs')
    arguments = parser.parse_args()
    if arguments.holds:
        find_holds()
        return 0
    outputs, fits = build()
    for prefix, fit in fits.items():
        numbers = ', '.join(f'{v:.4f}' if i < 3 else f'{v:.2f}' for i, v in enumerate(fit))
        print(f'{prefix}: fit ({numbers})')
    stale = []
    for name, text in outputs.items():
        path = PERFORMANCE / f'{name}.swlanim'
        current = path.read_text(encoding='utf-8-sig') if path.exists() else None
        if current is not None and comparable(json.loads(current)) == comparable(json.loads(text)):
            continue
        if arguments.check:
            stale.append(path.name)
        else:
            path.write_text(text, encoding='utf-8', newline='\n')
            print('wrote', path.relative_to(ROOT))
    known = {f'{name}.swlanim' for name in outputs}
    extra = sorted(path.name for path in PERFORMANCE.glob('Jessoon*.swlanim') if path.name not in known)
    if extra:
        print('Projects no longer derived:', ', '.join(extra))
        return 1
    if stale:
        print('Stale Jessoon projects:', ', '.join(stale))
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
