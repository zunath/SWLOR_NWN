"""Solve each playable body's HornFit for the Flanged Jessoon from its compiled Jessoon clips.

Each body plays the Jessoon clip set solved on its skeleton (PerformanceAnimations.FitToInstrument). Its fit hangs the
instrument from the right hand so that, over the whole loop, each visible hand (the centre of that species' hand mesh)
sits where the male Human's sits on the instrument, the mouthpiece sits at that body's mouth, and the instrument stays
turned in the hand as every set holds it. The mouth point
is where the fits checked close up in the client put the mouthpiece with the horn clips (mouth_fit).

Needs a full SWLOR_Haks checkout with the installed banks and the base game data (for the stock body models):

    python -B tools/SolveJessoonFits.py --game-data "<NWN>/data"
"""
from __future__ import annotations

import argparse
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

AXIS, LOW, TIP = derive.AXIS, derive.BODY_LOW, derive.TIP
RIGS, HANDS, mouth_fit = derive.RIGS, derive.HANDS, derive.mouth_fit
# A hand off the tube or a mouthpiece off the lips shows at a glance; a hand a little higher or lower on the tube does not.
WEIGHTS = {'grip': 1 / .006, 'along': 1 / .02, 'mouth': 1 / .006, 'turn': 1 / math.radians(8)}
CODES = {'Human': 'h', 'Trandoshan': 't', 'MonCalamari': 'm', 'Bothan': 'b', 'Rodian': 'r', 'Wookiee': 'w', 'Cathar': 'c',
         'Ugnaught': 'u', 'Ewok': 'e'}


def clip_set(species, gender):
    """Mirrors PerformanceAnimations.FitToInstrument: the body's own set, or the set of the body it matches."""
    female = gender == 'F'
    own = 'Jessoon' + species + ('Female' if female else '')
    if own in derive.SETS or own == derive.REFERENCE:
        return own
    if species == 'Rodian':
        return 'JessoonBothanFemale'
    return 'JessoonHumanFemale' if female and species != 'Droid' else derive.REFERENCE


def loop_clip(prefix):
    """The set's loop clip: sw_j + the body's letter + m or f (PerformanceAnimations)."""
    species, gender = ('Human', 'M') if prefix == derive.REFERENCE else derive.SETS[prefix]
    return f'sw_j{CODES[species]}{gender.lower()}_loop'


def apply(fit, point):
    return derive.fit_rotation(fit) @ np.asarray(point) + np.asarray(fit[:3])


class Banks:
    def __init__(self, game_data):
        import CoplanarFaces as faces
        import GenerateRobeRgbModels as robes
        import RobePoseAudit as poses
        active, stock = robes.fresh_active_models(), robes.tint.read_stock_key_models(Path(game_data))

        def load(name):
            if name in active:
                return active[name].read_bytes()
            if name in stock:
                return robes.tint.extract_stock_bif_resource(*stock[name])
            return None
        self.load, self.faces = load, faces
        self.library = poses.Library(load)

    def palm(self, base, side, part):
        """Centre of a hand part's mesh in its hand_g frame."""
        data = self.load(f'{base}_hand{side}{part:03d}')
        if data is None:
            raise ValueError(f'Missing hand model {base}_hand{side}{part:03d}')
        return np.vstack([mesh.vertices for mesh in self.faces.binary_meshes(data)]).mean(axis=0)

    def frames(self, base, clip, count):
        model = self.library.model(base)
        animation = self.library.clips(base)[clip]
        result = []
        for time in np.linspace(0, animation[0], count):
            pose = model.pose(animation, float(time), model.scale)
            result.append({name: (np.array(pose[name][0]), R.from_quat(pose[name][1]))
                           for name in ('rhand', 'rhand_g', 'lhand_g', 'head_g')})
        return result


def hands(banks, base, species, frames):
    """Per frame: both visible hands' centres in the right hand's frame."""
    part = HANDS.get(species, 1)
    right, left = banks.palm(base, 'r', part), banks.palm(base, 'l', part)
    return [((rr.inv().apply(gp + gr.apply(right) - rp)), rr.inv().apply(lp + lr.apply(left) - rp))
            for f in frames for (rp, rr), (gp, gr), (lp, lr) in [(f['rhand'], f['rhand_g'], f['lhand_g'])]]


def reference(banks, samples):
    """Where the male Human's visible hands sit on the instrument, per frame of its loop."""
    frames = banks.frames('pmh0', loop_clip(derive.REFERENCE), samples)
    inverse = lambda point: derive.fit_rotation(derive.JESSOON_HOLD).T @ (point - np.asarray(derive.JESSOON_HOLD[:3]))
    return [(inverse(right), inverse(left)) for right, left in hands(banks, 'pmh0', 'Human', frames)]


def solve(banks, targets, species, gender, samples=24):
    prefix = clip_set(species, gender)
    base = f'p{gender.lower()}{"z" if species == "Droid" else RIGS[species]}0'
    mouth = np.mean([hr.inv().apply(rp + rr.apply(apply(mouth_fit(species, gender), TIP)) - hp)
                     for f in banks.frames(base, 'sw_hrn_loop', samples)
                     for (rp, rr), (hp, hr) in [(f['rhand'], f['head_g'])]], axis=0)
    frames = banks.frames(base, loop_clip(prefix), samples)
    palms = hands(banks, base, species, frames)
    mouths = [rr.inv().apply(hp + hr.apply(mouth) - rp)
              for f in frames for (rp, rr), (hp, hr) in [(f['rhand'], f['head_g'])]]

    def parts(fit):
        rotation, origin = derive.fit_rotation(fit), np.asarray(fit[:3])
        model = lambda point: rotation.T @ (point - origin)
        return ([model(right) - target for (right, _), (target, _) in zip(palms, targets)],
                [model(left) - target for (_, left), (_, target) in zip(palms, targets)],
                [apply(fit, TIP) - point for point in mouths])

    # Every set holds the instrument turned the same way in the right hand; a body keeps that turn within a few degrees
    # rather than roll the crook away from the face to bring a hand or the mouthpiece closer.
    held = R.from_matrix(derive.fit_rotation(derive.JESSOON_HOLD))
    turn = lambda fit: (R.from_matrix(derive.fit_rotation(fit)) * held.inv()).magnitude()

    def residuals(fit):
        rights, lefts, tips = parts(fit)
        scale = 1 / math.sqrt(len(frames))
        hands = np.array(rights + lefts)
        along = hands @ AXIS
        grip = hands - np.outer(along, AXIS)
        return np.concatenate([np.ravel(grip) * WEIGHTS['grip'] * scale, along * WEIGHTS['along'] * scale,
                               np.ravel(tips) * WEIGHTS['mouth'] * scale, [turn(fit) * WEIGHTS['turn']]])

    fit = least_squares(residuals, np.array(derive.JESSOON_HOLD), x_scale=[.01, .01, .01, 5, 5, 5]).x
    rights, lefts, tips = parts(fit)
    worst = lambda values: max(np.linalg.norm(v) for v in values) * 100
    off = lambda values: max(np.linalg.norm(v - AXIS * np.dot(v, AXIS)) for v in values) * 100
    report = {'right_cm': worst(rights), 'right_off_cm': off(rights), 'left_cm': worst(lefts), 'left_off_cm': off(lefts),
              'mouth_cm': worst(tips), 'turn_deg': math.degrees(turn(fit))}
    return prefix, [float(v) for v in fit], report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--game-data', required=True)
    arguments = parser.parse_args()
    banks = Banks(arguments.game_data)
    targets = reference(banks, 24)
    for species in RIGS:
        for gender in 'MF':
            prefix, fit, report = solve(banks, targets, species, gender)
            numbers = ', '.join(f'{v:.4f}f' if i < 3 else f'{v:.2f}f' for i, v in enumerate(fit))
            print(f'{species:12s} {gender} {prefix:20s} ' + ' '.join(f'{k}={v:.1f}' for k, v in report.items()) +
                  f'  new({numbers})', flush=True)
    return 0


if __name__ == '__main__':
    sys.exit(main())
