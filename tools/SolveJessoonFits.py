"""Solve each playable body's HornFit for the Flanged Jessoon from its compiled Jessoon clips.

Each body plays one of the Jessoon clip sets (PerformanceAnimations.FitToInstrument). Its fit hangs the instrument from
the right hand so that, over the whole loop, the body tube passes through the right grip the set was solved with and
through the left grip (the mirror of the right), and the mouthpiece sits at that body's mouth. The mouth point is where
the fits checked close up in the client put the mouthpiece with the horn clips (MOUTH_FITS). The crook keeps the
authoring body's roll unless the body cannot reach the mouth without turning it.

Needs a full SWLOR_Haks checkout with the installed banks and the base game data (for the stock body models):

    python -B tools/SolveJessoonFits.py --game-data "<NWN>/data"
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import numpy as np
from scipy.optimize import least_squares
from scipy.spatial.transform import Rotation as R

TOOLS = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(TOOLS.parent / 'SWLOR_Haks' / 'tools'))
import DeriveJessoonPerformanceMotion as derive  # noqa: E402

AXIS, LOW = derive.AXIS, derive.BODY_LOW
TIP = np.array([-0.060, 0.318, 0.035])  # the mouthpiece, measured from wmgfs_m_211
SET_CLIPS = {'Jessoon': 'sw_jsn_loop', 'JessoonFemale': 'sw_jsf_loop', 'JessoonLongArm': 'sw_jsl_loop',
             'JessoonCatharFemale': 'sw_jcf_loop'}
RIGS = {'Human': 'h', 'Chiss': 'h', 'Zabrak': 'h', 'Twilek': 'h', 'Mirialan': 'h', 'Echani': 'h', 'KelDor': 'h',
        'Cyborg': 'h', 'Togruta': 'h', 'Nautolan': 'h', 'Trandoshan': 'h', 'MonCalamari': 'h', 'Bothan': 'e',
        'Rodian': 'e', 'Wookiee': 'e', 'Cathar': 'o', 'Ugnaught': 'd', 'Ewok': 'd', 'Droid': 'z'}
WEIGHTS = {'right': 1 / .006, 'angle': 1 / math.radians(10), 'left': 1 / .008, 'mouth': 1 / .01,
           'roll': 1 / math.radians(20)}


def clip_set(species, gender):
    """Mirrors PerformanceAnimations.FitToInstrument."""
    if species == 'Cathar':
        return 'JessoonCatharFemale' if gender == 'F' else 'JessoonLongArm'
    if species in ('Ugnaught', 'Ewok'):
        return 'JessoonLongArm'
    if species == 'Droid':
        return 'Jessoon'
    return 'JessoonFemale' if gender == 'F' else 'Jessoon'


def compose(outer, inner):
    ro, ri = derive.fit_rotation(outer), derive.fit_rotation(inner)
    r = ro @ ri
    t = ro @ np.asarray(inner[:3]) + np.asarray(outer[:3])
    return [*t, math.degrees(math.asin(max(-1, min(1, r[2, 1])))), math.degrees(math.atan2(-r[2, 0], r[2, 2])),
            math.degrees(math.atan2(-r[0, 1], r[1, 1]))]


def apply(fit, point):
    return derive.fit_rotation(fit) @ np.asarray(point) + np.asarray(fit[:3])


# The fits on the horn clips that put the mouthpiece at each body's mouth, seen close up in the client.
FEMALE_KLOO = {'Human', 'Chiss', 'Zabrak', 'Twilek', 'Mirialan', 'Echani', 'KelDor', 'Cyborg', 'Trandoshan', 'Togruta',
               'Nautolan', 'Bothan', 'Rodian'}
KLOO = {('MonCalamari', 'M'): (0, 0, 0, -8, 0, 0), ('MonCalamari', 'F'): (0, 0, 0, -10, 0, 0),
        ('Wookiee', 'M'): (0, 0, 0, -8, 0, 0), ('Wookiee', 'F'): (.03, 0, .03, -12, 0, 0),
        ('Cathar', 'F'): (0, 0, 0, -12, 0, 0), ('Trandoshan', 'M'): (0, 0, 0, -8, 0, 0),
        ('Rodian', 'M'): (0, 0, 0, -8, 0, 0), **{(s, 'F'): (.02, 0, 0, -13, 0, 0) for s in FEMALE_KLOO}}


def mouth_fit(species, gender):
    if species == 'Cathar' and gender == 'M':
        return derive.ALIGNED_FIT
    if species == 'Ugnaught':
        return (-.018, -.003, -.018, 12.3, -152.4, 28.6)
    fit = compose(KLOO.get((species, gender), (0, 0, 0, 0, 0, 0)), derive.ALIGNED_FIT)
    if gender == 'F' and (species in FEMALE_KLOO or species == 'MonCalamari'):
        fit = [0, fit[1], 0, *fit[3:]]
    return fit


class Banks:
    def __init__(self, game_data):
        import GenerateRobeRgbModels as robes
        import RobePoseAudit as poses
        active, stock = robes.fresh_active_models(), robes.tint.read_stock_key_models(Path(game_data))

        def load(name):
            if name in active:
                return active[name].read_bytes()
            if name in stock:
                return robes.tint.extract_stock_bif_resource(*stock[name])
            return None
        self.library = poses.Library(load)

    def frames(self, base, clip, count):
        model = self.library.model(base)
        animation = self.library.clips(base)[clip]
        result = []
        for time in np.linspace(0, animation[0], count):
            pose = model.pose(animation, float(time), model.scale)
            result.append({name: (np.array(pose[name][0]), R.from_quat(pose[name][1])) for name in ('rhand', 'lhand', 'head_g')})
        return result


def grips():
    """Each set's right grip (where the tube passes the right hand, and its direction) and playing fit on its skeleton."""
    source = json.loads((derive.PERFORMANCE / 'HornLoop.swlanim').read_text(encoding='utf-8-sig'))
    slide = [*(AXIS * derive.RIGHT_HAND_SLIDE), 0, 0, 0]
    result = {}
    for prefix, (skeleton, fit) in derive.VARIANTS.items():
        project = derive.on_skeleton(source, skeleton)
        joints = project['Joints']
        index = {joint['Name']: i for i, joint in enumerate(joints)}
        result[prefix] = (*derive.right_grip(joints, project['Keys'][0]['Pose'], index, fit), compose(fit, slide))
    return result


def crook(fit):
    start, direction = apply(fit, LOW), derive.fit_rotation(fit) @ AXIS
    offset = apply(fit, TIP) - start
    offset -= direction * np.dot(offset, direction)
    return offset / np.linalg.norm(offset)


def solve(banks, grip, species, gender, samples=24):
    prefix = clip_set(species, gender)
    right, direction, hold = grip[prefix]
    left = right * derive.MIRROR
    base = f'p{gender.lower()}{RIGS[species]}0'
    mouth = np.mean([hr.inv().apply(rp + rr.apply(apply(mouth_fit(species, gender), TIP)) - hp)
                     for f in banks.frames(base, 'sw_hrn_loop', samples)
                     for (rp, rr), (hp, hr) in [(f['rhand'], f['head_g'])]], axis=0)
    targets = [(rr.inv().apply(lp + lr.apply(left) - rp), rr.inv().apply(hp + hr.apply(mouth) - rp))
               for f in banks.frames(base, SET_CLIPS[prefix], samples)
               for (rp, rr), (lp, lr), (hp, hr) in [(f['rhand'], f['lhand'], f['head_g'])]]
    reference = crook(hold)

    def parts(fit):
        start, axis = apply(fit, LOW), derive.fit_rotation(fit) @ AXIS
        off = lambda point: (point - start) - axis * np.dot(point - start, axis)
        return (off(right), math.acos(max(-1, min(1, float(np.dot(axis, direction))))),
                [off(target) for target, _ in targets], [apply(fit, TIP) - target for _, target in targets],
                math.acos(max(-1, min(1, float(np.dot(crook(fit), reference))))))

    def residuals(fit):
        r, angle, lefts, mouths, roll = parts(fit)
        out = [*(r * WEIGHTS['right']), angle * WEIGHTS['angle'], roll * WEIGHTS['roll']]
        for l, m in zip(lefts, mouths):
            out += [*(l * WEIGHTS['left'] / math.sqrt(len(targets))), *(m * WEIGHTS['mouth'] / math.sqrt(len(targets)))]
        return np.array(out)

    fit = least_squares(residuals, np.array(hold), x_scale=[.01, .01, .01, 5, 5, 5]).x
    r, angle, lefts, mouths, roll = parts(fit)
    report = {'right_cm': np.linalg.norm(r) * 100, 'angle_deg': math.degrees(angle),
              'left_cm': max(np.linalg.norm(l) for l in lefts) * 100,
              'mouth_cm': max(np.linalg.norm(m) for m in mouths) * 100, 'roll_deg': math.degrees(roll)}
    return prefix, [float(v) for v in fit], report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--game-data', required=True)
    arguments = parser.parse_args()
    banks, grip = Banks(arguments.game_data), grips()
    for species in RIGS:
        for gender in 'MF':
            prefix, fit, report = solve(banks, grip, species, gender)
            numbers = ', '.join(f'{v:.4f}f' if i < 3 else f'{v:.2f}f' for i, v in enumerate(fit))
            print(f'{species:12s} {gender} {prefix:20s} ' + ' '.join(f'{k}={v:.1f}' for k, v in report.items()) +
                  f'  new({numbers})', flush=True)
    return 0


if __name__ == '__main__':
    sys.exit(main())
