"""Derive the Flanged Jessoon playing clips from the horn clips.

The horn clips were posed for the kloo horn: both hands sit in the top third of the instrument, eleven centimetres
apart, with the left hand clear of the slim Jessoon's body. The Jessoon is a long straight horn (wmgfs_m_211,
"bithsax"), so its clips keep the horn clips' body motion and instrument path and move only the hands along it:
the right hand slides down the body and the left hand closes on the tube just under the flange, gripping it the way
the right hand grips it (mirrored). Each hand keeps the horn clip's motion along the tube, so fingering and accents
are unchanged.

One clip plays on every body, but the bodies' arms differ: on the female and Cathar skeletons the authoring body's
hands leave the tube several centimetres outside a hand or the mouthpiece away from the mouth, and no item fit can fix
both. Those skeletons get their own copies, solved on their own bone lengths from where their verified horn fit holds
the instrument at the mouth. The long-armed Cathar male's copies also suit the short Ugnaught and Ewok bodies best
(measured on the compiled banks), so PerformanceAnimations.FitToInstrument plays them there too, and
tools/SolveJessoonFits.py then solves every body's HornFit for the copies it plays.

    python -B tools/DeriveJessoonPerformanceMotion.py          # write the Jessoon projects
    python -B tools/DeriveJessoonPerformanceMotion.py --check  # confirm the committed projects are current
"""
from __future__ import annotations

import argparse
import copy
import json
import sys
from pathlib import Path

import numpy as np
from scipy.spatial.transform import Rotation as R

sys.path.insert(0, str(Path(__file__).resolve().parent))
from swg_animation import between, set_world_rotation, solve_limb, world  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
PERFORMANCE = ROOT / 'design' / 'animations' / 'performance'
STATES = ('Loop', 'Low', 'Mid', 'High', 'Flourish')

# Centreline of the Jessoon's straight body, measured from wmgfs_m_211 (model metres). The flange sits above
# y = 0.13 and the bell bends away below y = -0.28.
BODY_LOW = np.array([0.077, -0.26, -0.025])
BODY_HIGH = np.array([-0.055, 0.14, -0.001])
# Model y along the body where the left hand's grip sits in the playing hold.
LEFT_HAND_GRIP_Y = .07
# The right hand slides at most this far down the body (HornFit slides the Jessoon up by the same); a skeleton whose arm
# cannot reach that far in every clip slides it as far as it can, in whole centimetres.
RIGHT_HAND_SLIDE = .06

# HornFit.JessoonOnKlooHorn: the horn clips hold the Jessoon there on the male Human body.
ALIGNED_FIT = (-.035, .065, -.017, 12.3, -152.4, 28.6)
# The full-height female bodies' fit, seen close up in the client with the mouthpiece at the mouth on the horn clips.
FEMALE_FIT = (0.0, .0595, 0.0, .8366, -146.2176, 27.8886)
# The Cathar female's fit on the horn clips, also seen at the mouth.
CATHAR_FEMALE_FIT = (-.035, .06, -.0301, 1.7204, -146.6856, 27.899)

# Local bone offsets of the arm and head chain, read from the compiled pfh0, pmo0 and pfo0 body models. The authoring
# project carries the male Human (pmh0) skeleton, which the droid bodies share.
SKELETONS = {
    'pfh0': {
        'torso_g': (0.0004, 0.0103, 0.0002), 'neck_g': (-0.0011, -0.045, 0.3464), 'head_g': (0.0003, 0.0186, 0.1114),
        'rbicep_g': (0.1376, -0.0356, 0.3055), 'rforearm_g': (0.032, -0.0249, -0.3163),
        'rhand_g': (0.0254, 0.0139, -0.2524), 'rhand': (0.001, 0.0104, -0.0819),
        'lbicep_g': (-0.1395, -0.0356, 0.3055), 'lforearm_g': (-0.032, -0.0249, -0.3163),
        'lhand_g': (-0.0254, 0.0139, -0.2524), 'lhand': (-0.001, 0.0103, -0.0821),
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
}

# Project name prefix -> (skeleton it is solved on, None for the authoring skeleton; the horn fit that skeleton plays with).
VARIANTS = {
    'Jessoon': (None, ALIGNED_FIT),
    'JessoonFemale': ('pfh0', FEMALE_FIT),
    'JessoonLongArm': ('pmo0', ALIGNED_FIT),
    'JessoonCatharFemale': ('pfo0', CATHAR_FEMALE_FIT),
}


def fit_rotation(fit) -> np.ndarray:
    """HornFitTransform's convention: RotateX about model Z, RotateY about X, RotateZ about Y, composed X * Y * Z."""
    def about(axis, degrees):
        return R.from_rotvec(np.radians(degrees) * np.eye(3)[axis]).as_matrix()
    _, _, _, rotate_y, rotate_z, rotate_x = fit
    return about(2, rotate_x) @ about(0, rotate_y) @ about(1, rotate_z)


AXIS = (BODY_HIGH - BODY_LOW) / np.linalg.norm(BODY_HIGH - BODY_LOW)
LEFT_GRIP_ALONG = (LEFT_HAND_GRIP_Y - BODY_LOW[1]) / AXIS[1]
MIRROR = np.array([-1.0, 1.0, 1.0])  # the hand frames mirror across local X (rhand X +0.011, lhand X -0.015)


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
    if skeleton is None:
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


def derive(project, fit, slide, grip, left_rest_along):
    """Move the hands along the instrument in every key; returns the worst miss of each hand's target."""
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


def build_variant(prefix, sources):
    skeleton, fit = VARIANTS[prefix]
    working = {state: on_skeleton(sources[state], skeleton) for state in STATES}
    reference = working['Loop']
    joints = reference['Joints']
    index = {joint['Name']: i for i, joint in enumerate(joints)}
    hold_pose = reference['Keys'][0]['Pose']
    grip = right_grip(joints, hold_pose, index, fit)
    positions, _ = world(joints, hold_pose)
    left_rest_along = Hold(joints, hold_pose, index, fit).along(positions[index['lhand']])
    for centimetres in range(round(RIGHT_HAND_SLIDE * 100), -1, -1):
        slide = centimetres / 100
        solved = {state: copy.deepcopy(working[state]) for state in STATES}
        if all(derive(solved[state], fit, slide, grip, left_rest_along) <= .002 for state in STATES):
            break
    else:
        raise ValueError(f'{prefix}: the hands cannot reach the instrument')
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
    return outputs, slide


def build():
    sources = {state: json.loads((PERFORMANCE / f'Horn{state}.swlanim').read_text(encoding='utf-8-sig')) for state in STATES}
    outputs, slides = {}, {}
    for prefix in VARIANTS:
        variant, slides[prefix] = build_variant(prefix, sources)
        outputs.update(variant)
    return outputs, slides


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
    arguments = parser.parse_args()
    outputs, slides = build()
    for prefix, slide in slides.items():
        print(f'{prefix}: right hand {slide * 100:.0f} cm down the body')
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
    if stale:
        print('Stale Jessoon projects:', ', '.join(stale))
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
