"""Build the astromech's dynamic (part-based) body from the chassis models AstromechCreatureExport.py writes.

  python -B tools/AstromechDynamicBody.py --chassis DIR --work EMPTY [--haks SWLOR_Haks] [--compiler cleanmodels.exe]
                                           [--tools-only]

A playable species has a part-based body: one skeleton, with the head, chest and limbs as separate part models
that worn gear can replace. The exporter writes each chassis as one simple creature model (`c_astro_NN`: body, head
and two legs as rigid meshes) on a shared clip model (`c_astromech`). This tool re-cuts that output, without Blender:

- `pmr0` and `pfr0`: the skeleton (race letter R, both genders), with the standard part node names laid out for a
  droid. The barrel hangs on `torso_g`, the dome on `head_g`, the legs on `lthigh_g` and `rthigh_g`. The arm chain
  is folded onto one point at the front of the barrel, so an item held in a hand comes out of the body there.
- `a_astro`: its animations. A part-based body is asked for the humanoid animation names, so every engine-native
  name (NATIVE_ANIMATIONS) is a chassis clip with its nodes renamed. The clip that slid a built-in tool out now
  slides the hand, and whatever it holds, out of the barrel: the prod's track for melee names, the blaster's for
  ranged ones. The fishing clips keep their names and swing the hand, so the held rod casts.
- `p?r0_headNNN`, `_chestNNN`, `_leglNNN`, `_legrNNN`: chassis NNN cut into parts. Every chassis is measured
  against the one skeleton and each part mesh is moved by the difference, so a matched set stands exactly as its
  chassis did. The tool meshes and the built-in rod are left out.
- `wampr_*` and `wambl_*`: the shock prod and the blaster mount as held items, cut from the tools of chassis 1. A
  weapon is three stacked part models with three stacked icons. The tool is the bottom part; the middle and top
  parts are empty. One set is written for every part number an `am_w_*` blueprint uses, with the icons copied from
  the staff or rifle the blueprint was made from (GenerateAstromechTools.py), so no icon changes.

DIR holds `c_astro_01.mdl` .. `c_astro_08.mdl` and `c_astromech.mdl`, compiled or ASCII. Everything is compiled with
CleanModels and copied into the hak folders. Rows in appearance.2da and the pictures are separate steps. The parts
name the chassis materials, so after installing them run `SWLOR_Haks/tools/GenerateTintMapAssets.py
--refresh-model-bindings`, which gives each part folder its own copy of the material and writes the tintmap.2da rows.
`--tools-only` rebuilds the held tools without touching the parts.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
RACE = 'r'
ROOTS = ('pmr0', 'pfr0')
BANK = 'a_astro'
CHASSIS_COUNT = 8
CLIP_MODEL = 'c_astromech'

# The frame chassis 2 to 8 share, in metres from the ground under the creature. Chassis 1 was built 0.047 further
# back and is moved forward to it, which puts its barrel and dome on the same turning axes as the others.
TORSO = (0.0, 0.051, 0.559)
HEAD = (0.0, 0.0, 0.742)
LEG_X = 0.257
# Where a hand holds an item: the front face of the barrel. The clips slide it back by up to RETRACT to stow it.
HAND = (0.0, 0.30, 0.45)
CHASSIS_SHIFT = {1: (0.0, 0.047, 0.0)}

# Part model stem -> (chassis mesh, skeleton joint in metres from the ground, hak folder).
PARTS = {
    'head': ('head', HEAD, 'sw_pt_head'),
    'chest': ('body', TORSO, 'sw_pt_chest'),
    'legl': ('legl', (-LEG_X, TORSO[1], TORSO[2]), 'sw_pt_lthigh'),
    'legr': ('legr', (LEG_X, TORSO[1], TORSO[2]), 'sw_pt_rthigh'),
}
ROOT_FOLDER = 'sw_pt_root'
BANK_FOLDER = 'sw_cr_creature'

# Held tools: base item -> (item class, tool mesh of chassis 1, item class whose icons it borrows).
TOOL_CHASSIS = 1
TOOL_ITEMS = {545: ('wampr', 'toolr', 'iwdbqs'), 546: ('wambl', 'tooll', 'iwbwxl')}
TOOL_FOLDER = 'sw_weapon'
TOOL_BLUEPRINTS = 'Module/uti/am_w_*.uti.json'


def relative(point, origin):
    return tuple(point[i] - origin[i] for i in range(3))


# (node, parent, position relative to the parent). Order matters: the animations list the same nodes.
SKELETON = [
    ('rootdummy', None, (0, 0, 0)),
    ('torso_g', 'rootdummy', TORSO),
    ('wings', 'torso_g', (0, -0.25, 0)),
    ('neck_g', 'torso_g', (0, 0, 0)),
    ('head_g', 'neck_g', relative(HEAD, TORSO)),
    ('head', 'head_g', (0, 0, 0.3)),
    ('lbicep_g', 'torso_g', relative(HAND, TORSO)),
    ('lshoulder_g', 'lbicep_g', (0, 0, 0)),
    ('lforearm_g', 'lbicep_g', (0, 0, 0)),
    ('lforearm', 'lforearm_g', (0, 0, 0)),
    ('lhand_g', 'lforearm_g', (0, 0, 0)),
    ('lhand', 'lhand_g', (0, 0, 0)),
    ('rbicep_g', 'torso_g', relative(HAND, TORSO)),
    ('rshoulder_g', 'rbicep_g', (0, 0, 0)),
    ('rforearm_g', 'rbicep_g', (0, 0, 0)),
    ('rhand_g', 'rforearm_g', (0, 0, 0)),
    ('rhand', 'rhand_g', (0, 0, 0)),
    ('impact', 'torso_g', (0, 0.2, 0)),
    ('cloak_g', 'torso_g', (0, -0.3, 0.15)),
    ('pelvis_g', 'rootdummy', TORSO),
    ('tail', 'pelvis_g', (0, -0.3, 0)),
    ('lthigh_g', 'pelvis_g', (-LEG_X, 0, 0)),
    ('lshin_g', 'lthigh_g', (0, 0, 0)),
    ('lfoot_g', 'lshin_g', (0, 0, 0)),
    ('rthigh_g', 'pelvis_g', (LEG_X, 0, 0)),
    ('rshin_g', 'rthigh_g', (0, 0, 0)),
    ('rfoot_g', 'rshin_g', (0, 0, 0)),
    ('belt_g', 'rootdummy', TORSO),
    ('handconjure', None, (0, 0.55, 0.5)),
    ('headconjure', None, (0, 0, 1.15)),
]

# Chassis clip node -> skeleton node. The two tool tracks are read apart (hand_track).
NODE_MAP = {'rootdummy': 'rootdummy', 'torso': 'torso_g', 'head': 'head_g', 'legl': 'lthigh_g', 'legr': 'rthigh_g'}
HAND_NODES = ('rbicep_g', 'lbicep_g')
MELEE_TRACK, RANGED_TRACK = 'toolr', 'tooll'

# Every animation name the engine asks of a part-based body: the names in the humanoid skeleton's supermodel chain,
# apart from the authored ability clips (sw_*, custom*), which the server never plays on an astromech.
NATIVE_ANIMATIONS = """
1hcloseh 1hclosel 1hparryl 1hparryr 1hreach 1hreadyl 1hreadyr 1hslashl 1hslasho 1hslashr 1hstab 2hcloseh
2hclosel 2hparryl 2hparryr 2hreach 2hreadyl 2hreadyr 2hslashl 2hslasho 2hslashr 2hstab 2wcloseh 2wclosel
2wparryl 2wparryr 2wreach 2wreadyl 2wreadyr 2wslashl 2wslasho 2wslashr 2wstab appear appear2 bow bowrdy
bowshot castarea castarealp castout castoutlp castpoint castpointlp castself castselflp castup castuplp
conjure1 conjure2 covering_strike cturnr cwalkb cwalkf cwalkl cwalkr damageb damagel damager damages deadbck
deadfnt disappear disappear2 disappear2lp disappearlp dodgelr dodges drink drwleft drwright getlow getlowlp
getmid getmidlp greeting gustandb gustandf gutokdb gutokdf hacking_blade hturnl hturnr invincible kdbck
kdbckdie kdbckdmg kdbckps kdfnt kdfntdie kdfntdmg kdfntps kneel listen lookfar meditate nwcloseh nwclosel
nwkickl nwkickr nwkicks nwreach nwreadyl nwreadyr nwslashl nwslasho nwslashr nwstab pause1 pause2 pausebrd
pausepsn pausesh pausetrd pauseturn pausewalkfl pausewalkfr pausewalkl pausewalkr plcloseh plclosel plparryl
plparryr plpause1 plreach plreadyl plreadyr plslashl plslasho plslashr plstab read run run_bowl run_shieldl
run_swordl run_swordr runfl runfr salute shield_bash shield_wall shieldl sit sitcross sitcrossps sitdown spasm
steal taunt throwr tlkforce tlklaugh tlknorm tlkplead torchl victoryfr victorymg victoryth walk walk_bowl
walk_shieldl walk_swordl walk_swordr walkdead walkinj whirlwind worship xbowr xbowrdy xbowshot
""".split()

# The fishing clips keep their names: the server plays them in place of the working clips (AstromechAnimations).
FISHING_CLIPS = ('cfish', 'cfishlp', 'cfishin', 'cfishreel', 'cfishgive', 'cfishhold', 'cfishland', 'cfishlandlp')

EXACT_SOURCES = {
    'pause2': 'cpause2', 'walkinj': 'cwalkinj', 'walkdead': 'cwalkinj', 'taunt': 'ctaunt', 'spasm': 'cspasm',
    'hturnl': 'chturnl', 'hturnr': 'chturnr', 'cturnr': 'chturnr', 'dodges': 'cdodges', 'dodgelr': 'cdodgelr',
    'damagel': 'cdamagel', 'damager': 'cdamager', 'damages': 'cdamages', 'damageb': 'cdamages',
    'deadbck': 'cdead', 'deadfnt': 'cdead', 'gustandb': 'cgustandb', 'gustandf': 'cgustandb',
    'gutokdb': 'cguptokdb', 'gutokdf': 'cguptokdb', 'getmid': 'cgetmid', 'getmidlp': 'cgetmidlp',
    'getlow': 'cgetlow', 'getlowlp': 'cgetlowlp', 'steal': 'cgetmid', 'drink': 'cgetmid',
    'conjure1': 'cconjure1', 'conjure2': 'cconjure1', 'bowshot': 'ccastout', 'xbowshot': 'ccastout',
    'throwr': 'ccastout', 'whirlwind': 'ca1slashr', 'covering_strike': 'ca1slashr', 'hacking_blade': 'ca1slashl',
    'shield_bash': 'ca1stab', 'shieldl': 'cparryl', 'shield_wall': 'cparryl',
    'bowrdy': 'creadyr', 'xbowrdy': 'creadyr', 'xbowr': 'creadyr', 'bow': 'cpause1',
}
SUFFIX_SOURCES = (('readyr', 'creadyr'), ('readyl', 'creadyl'), ('slashl', 'ca1slashl'), ('slashr', 'ca1slashr'),
                  ('slasho', 'ca1slashr'), ('stab', 'ca1stab'), ('closel', 'cclosel'), ('closeh', 'ccloseh'),
                  ('reach', 'creach'), ('parryl', 'cparryl'), ('parryr', 'cparryr'))
# Names a ranged weapon plays: the hand follows the blaster's track, which slides out to fire.
RANGED_ANIMATIONS = frozenset({'bowrdy', 'bowshot', 'xbowr', 'xbowrdy', 'xbowshot', 'throwr', 'run_bowl', 'walk_bowl'})


def source_clip(name: str) -> str:
    """The chassis clip that plays for one humanoid animation name."""
    if name in EXACT_SOURCES:
        return EXACT_SOURCES[name]
    if name.startswith('victory'):
        return 'cvictory'
    if name.startswith('kd'):
        return 'ckdbckdie' if name.endswith('die') else 'ckdbckps' if name.endswith(('ps', 'dmg')) else 'ckdbck'
    if name.startswith('cast'):
        return 'ccastoutlp' if name.endswith('lp') else 'ccastout'
    if name.startswith('run'):
        return 'crun'
    if name.startswith(('walk', 'cwalk', 'pausewalk')):
        return 'cwalk'
    for suffix, source in SUFFIX_SOURCES:
        if name.endswith(suffix):
            return source
    if name.startswith('nwkick'):
        return 'ca1stab'
    return 'cpause1'


def hand_track(name: str) -> str:
    """Which tool track moves the hands for an animation name."""
    return RANGED_TRACK if name in RANGED_ANIMATIONS or name.startswith('cast') else MELEE_TRACK


def fmt(values) -> str:
    return ' '.join(f'{value:.6g}' for value in values)


# ---------------------------------------------------------------- reading the exporter's models
def read_nodes(text: str) -> dict:
    """Geometry nodes of an ASCII model: name -> kind, parent, position and the node's text."""
    geometry = text.split('beginmodelgeom', 1)[1].split('endmodelgeom', 1)[0]
    nodes = {}
    for match in re.finditer(r'(?ms)^[ \t]*node[ \t]+(\S+)[ \t]+(\S+)[ \t]*\r?\n(.*?)^[ \t]*endnode', geometry):
        kind, name, body = match[1].lower(), match[2].lower(), match[3]
        parent = re.search(r'(?m)^\s*parent\s+(\S+)', body)[1].lower()
        position = re.search(r'(?m)^\s*position\s+(\S+)\s+(\S+)\s+(\S+)', body)
        nodes[name] = {'kind': kind, 'parent': parent, 'block': match[0],
                       'position': tuple(float(value) for value in position.groups()) if position else (0.0, 0.0, 0.0)}
    return nodes


def world_position(nodes: dict, name: str):
    """A node's place in the model: every chassis node keeps the rest orientation, so positions add up."""
    total = [0.0, 0.0, 0.0]
    while name in nodes:
        for axis in range(3):
            total[axis] += nodes[name]['position'][axis]
        name = nodes[name]['parent']
    return tuple(total)


def read_clips(text: str) -> dict:
    clips = {}
    for match in re.finditer(r'(?ms)^[ \t]*newanim[ \t]+(\S+)[ \t]+\S+[ \t]*\r?\n(.*?)^[ \t]*doneanim[ \t]+\1[ \t]+\S+', text):
        name, body = match[1].lower(), match[2]
        head = body.split('node dummy', 1)[0]
        clip = {'length': float(re.search(r'length\s+(\S+)', head)[1]),
                'transtime': float(re.search(r'transtime\s+(\S+)', head)[1]),
                'events': re.findall(r'(?m)^\s*event\s+(\S+)\s+(\S+)', head), 'nodes': {}}
        for node in re.finditer(r'(?ms)^[ \t]*node[ \t]+dummy[ \t]+(\S+)[ \t]*\r?\n(.*?)^[ \t]*endnode', body):
            keys = {}
            for key in re.finditer(r'(?ms)^[ \t]*(positionkey|orientationkey)[ \t]+\d+[ \t]*\r?\n(.*?)^[ \t]*endlist', node[2]):
                keys[key[1]] = [[float(value) for value in row.split()] for row in key[2].strip().splitlines()]
            if keys:
                clip['nodes'][node[1].lower()] = keys
        clips[name] = clip
    return clips


# ---------------------------------------------------------------- skeleton and animations
def skeleton_model(model: str, supermodel: str) -> list:
    lines = ['#MAXMODEL ASCII', f'newmodel {model}', '    classification CHARACTER',
             f'    setsupermodel {model} {supermodel}', '    setanimationscale 1', '#MAXGEOM ASCII',
             f'beginmodelgeom {model}', f'    node dummy {model}', '        parent NULL', '    endnode']
    for name, parent, position in SKELETON:
        lines += [f'    node dummy {name}', f'        parent {parent or model}', f'        position {fmt(position)}',
                  '        orientation 0 0 1 0', '    endnode']
    lines.append(f'endmodelgeom {model}')
    return lines


def convert_clip(clip: dict, track: str, fishing: bool) -> dict:
    """Skeleton node -> key lists for one chassis clip."""
    nodes = {}
    for source, keys in clip['nodes'].items():
        if source in NODE_MAP:
            nodes[NODE_MAP[source]] = {kind: rows for kind, rows in keys.items()}
    rest = relative(HAND, TORSO)
    slide = clip['nodes'].get(track, {}).get('positionkey') or [[0.0, 0.0, 0.0, 0.0], [clip['length'], 0.0, 0.0, 0.0]]
    if fishing:
        # The rod is held out for the whole clip, and the hand swings as the built-in rod did.
        slide = [[0.0, 0.0, 0.0, 0.0], [clip['length'], 0.0, 0.0, 0.0]]
        swing = clip['nodes'].get('rod', {}).get('orientationkey')
        if swing:
            nodes['rhand_g'] = {'orientationkey': swing}
    for hand in HAND_NODES:
        nodes[hand] = {'positionkey': [[row[0], rest[0] + row[1], rest[1] + row[2], rest[2] + row[3]] for row in slide]}
    return nodes


def animation(name: str, clip: dict, nodes: dict) -> list:
    lines = [f'newanim {name} {BANK}', f'    length {clip["length"]:.6g}', f'    transtime {clip["transtime"]:.6g}']
    lines += [f'    event {time} {event}' for time, event in clip['events']]
    lines += ['    animroot rootdummy', f'    node dummy {BANK}', '        parent NULL', '    endnode']
    for node, parent, _ in SKELETON:
        lines += [f'    node dummy {node}', f'        parent {parent or BANK}']
        for kind, rows in nodes.get(node, {}).items():
            lines.append(f'        {kind} {len(rows)}')
            lines += ['            ' + fmt(row) for row in rows]
            lines.append('        endlist')
        lines.append('    endnode')
    lines.append(f'doneanim {name} {BANK}')
    return lines


def animation_bank(clips: dict) -> tuple:
    lines = skeleton_model(BANK, 'null')
    sources = {}
    for name in NATIVE_ANIMATIONS:
        sources[name] = source_clip(name)
        clip = clips[sources[name]]
        lines += animation(name, clip, convert_clip(clip, hand_track(name), fishing=False))
    for name in FISHING_CLIPS:
        sources[name] = name
        lines += animation(name, clips[name], convert_clip(clips[name], MELEE_TRACK, fishing=True))
    return lines + [f'donemodel {BANK}', ''], sources


# ---------------------------------------------------------------- parts
def move_mesh(block: str, offset) -> str:
    """The mesh block with every vertex moved by offset (the skeleton now carries the part's place)."""
    lines = block.splitlines()
    start = next(i for i, line in enumerate(lines) if re.match(r'\s*verts\s+\d+\s*$', line))
    count = int(lines[start].split()[1])
    for index in range(start + 1, start + 1 + count):
        indent = re.match(r'\s*', lines[index])[0]
        x, y, z = (float(value) for value in lines[index].split()[:3])
        lines[index] = f'{indent}{x + offset[0]:.6f} {y + offset[1]:.6f} {z + offset[2]:.6f}'
    return '\n'.join(lines)


def part_model(name: str, block: str, offset) -> str:
    block = re.sub(r'(?m)^(\s*parent\s+)\S+', r'\g<1>' + name, block, count=1)
    block = re.sub(r'(?m)^\s*position\s+.*\r?\n', '', block, count=1)
    block = move_mesh(block, offset)
    return '\n'.join(['#MAXMODEL ASCII', f'newmodel {name}', '    classification CHARACTER',
                      f'    setsupermodel {name} null', '    setanimationscale 1', '#MAXGEOM ASCII',
                      f'beginmodelgeom {name}', f'    node dummy {name}', '        parent NULL', '    endnode',
                      block, f'endmodelgeom {name}', f'donemodel {name}', ''])


def chassis_parts(number: int, text: str) -> dict:
    """Part model name -> (ASCII text, hak folder) for one chassis, both genders."""
    nodes = read_nodes(text)
    shift = CHASSIS_SHIFT.get(number, (0.0, 0.0, 0.0))
    result, report = {}, {}
    for stem, (mesh, joint, folder) in PARTS.items():
        if nodes.get(mesh, {}).get('kind') != 'trimesh':
            raise SystemExit(f'c_astro_{number:02d} has no {mesh} mesh')
        place = world_position(nodes, mesh)
        offset = tuple(place[axis] + shift[axis] - joint[axis] for axis in range(3))
        report[stem] = [round(value, 4) for value in offset]
        for root in ROOTS:
            name = f'{root}_{stem}{number:03d}'
            result[name] = (part_model(name, nodes[mesh]['block'], offset), folder)
    return result, report


# ---------------------------------------------------------------- held tools
def tool_part_numbers() -> dict:
    """Base item -> the bottom, middle and top part numbers its blueprints use."""
    used = {base: {'b': set(), 'm': set(), 't': set()} for base in TOOL_ITEMS}
    for path in sorted(REPO.glob(TOOL_BLUEPRINTS)):
        item = json.loads(path.read_text(encoding='utf-8'))
        base = item['BaseItem']['value']
        if base not in used:
            raise SystemExit(f'{path.name} is not a shock prod or a blaster mount')
        for slot, field in (('b', 'ModelPart1'), ('m', 'ModelPart2'), ('t', 'ModelPart3')):
            used[base][slot].add(item[field]['value'])
    return used


def empty_model(name: str) -> str:
    return '\n'.join(['#MAXMODEL ASCII', f'newmodel {name}', '    classification CHARACTER',
                      f'    setsupermodel {name} null', '    setanimationscale 1', '#MAXGEOM ASCII',
                      f'beginmodelgeom {name}', f'    node dummy {name}', '        parent NULL', '    endnode',
                      f'endmodelgeom {name}', f'donemodel {name}', ''])


def tool_models(text: str) -> dict:
    """Held tool model name -> ASCII text. The hand holds a tool on its axis, at the face of the barrel."""
    nodes = read_nodes(text)
    shift = CHASSIS_SHIFT.get(TOOL_CHASSIS, (0.0, 0.0, 0.0))
    result = {}
    for base, slots in tool_part_numbers().items():
        item_class, mesh, _ = TOOL_ITEMS[base]
        if nodes.get(mesh, {}).get('kind') != 'trimesh':
            raise SystemExit(f'c_astro_{TOOL_CHASSIS:02d} has no {mesh} mesh')
        offset = (0.0, world_position(nodes, mesh)[1] + shift[1] - HAND[1], 0.0)
        for number in sorted(slots['b']):
            name = f'{item_class}_b_{number:03d}'
            result[name] = part_model(name, nodes[mesh]['block'], offset)
        for slot in ('m', 't'):
            for number in sorted(slots[slot]):
                name = f'{item_class}_{slot}_{number:03d}'
                result[name] = empty_model(name)
    return result


def copy_tool_icons(haks: Path) -> int:
    """Each tool part number keeps the icon of the weapon part it was made from."""
    folder, copied = haks / TOOL_FOLDER, 0
    for base, slots in tool_part_numbers().items():
        item_class, _, icon_class = TOOL_ITEMS[base]
        for slot, numbers in slots.items():
            for number in sorted(numbers):
                sources = sorted(folder.glob(f'{icon_class}_{slot}_{number:03d}.*'))
                if not sources:
                    raise SystemExit(f'{TOOL_FOLDER} has no icon {icon_class}_{slot}_{number:03d}')
                for source in sources:
                    shutil.copyfile(source, folder / f'i{item_class}_{slot}_{number:03d}{source.suffix}')
                    copied += 1
    return copied


# ---------------------------------------------------------------- compiling
def compiler_path(explicit) -> Path:
    if explicit:
        return Path(explicit)
    if os.environ.get('CLEANMODELS'):
        return Path(os.environ['CLEANMODELS'])
    return Path(os.environ['LOCALAPPDATA']) / 'SWLOR/tools/cleanmodels-qt/1.1.7/cleanmodels-qt/cleanmodels.exe'


def run_compiler(compiler: Path, command: str, source: Path, target: Path, log: Path):
    target.mkdir(parents=True, exist_ok=True)
    result = subprocess.run([str(compiler), command, '--overwrite', str(source), str(target)],
                            capture_output=True, text=True, encoding='utf8', errors='replace')
    log.write_text(result.stdout + result.stderr, encoding='utf8')
    if result.returncode:
        raise SystemExit(f'CleanModels {command} failed; see {log}')


def ascii_sources(chassis: Path, work: Path, compiler: Path) -> Path:
    """ASCII copies of the exporter's models, decompiling the compiled ones."""
    names = [f'c_astro_{number:02d}.mdl' for number in range(1, CHASSIS_COUNT + 1)] + [f'{CLIP_MODEL}.mdl']
    missing = [name for name in names if not (chassis / name).is_file()]
    if missing:
        raise SystemExit(f'Missing in {chassis}: {", ".join(missing)}')
    ascii_dir, binary_dir = work / 'chassis_ascii', work / 'chassis_binary'
    ascii_dir.mkdir(parents=True)
    binary_dir.mkdir(parents=True)
    for name in names:
        data = (chassis / name).read_bytes()
        (binary_dir if data[:4] == bytes(4) else ascii_dir).joinpath(name).write_bytes(data)
    if any(binary_dir.iterdir()):
        run_compiler(compiler, 'decompile', binary_dir, ascii_dir, work / 'decompile.log')
    return ascii_dir


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--chassis', type=Path, required=True, help='c_astro_NN.mdl and c_astromech.mdl from the exporter')
    parser.add_argument('--work', type=Path, required=True, help='Empty scratch folder')
    parser.add_argument('--haks', type=Path, default=REPO / 'SWLOR_Haks')
    parser.add_argument('--compiler', type=Path)
    parser.add_argument('--no-install', action='store_true', help='Build in the scratch folder only')
    parser.add_argument('--tools-only', action='store_true', help='Build the held tools only, leaving the parts')
    args = parser.parse_args(argv)
    if args.work.exists() and any(args.work.iterdir()):
        raise SystemExit(f'{args.work} must be empty')
    args.work.mkdir(parents=True, exist_ok=True)
    compiler = compiler_path(args.compiler)
    if not compiler.is_file():
        raise SystemExit('CleanModels is missing. Run SWLOR_Haks/tools/SetupModelCompiler.ps1 or set CLEANMODELS.')

    sources = ascii_sources(args.chassis, args.work, compiler)
    clips = read_clips((sources / f'{CLIP_MODEL}.mdl').read_text(errors='ignore'))
    unknown = sorted(({source_clip(name) for name in NATIVE_ANIMATIONS} | set(FISHING_CLIPS)) - set(clips))
    if unknown:
        raise SystemExit(f'{CLIP_MODEL} has no clip {", ".join(unknown)}')

    ascii_out = args.work / 'ascii'
    ascii_out.mkdir()
    folders, offsets, animation_sources = {}, {}, {}
    if not args.tools_only:
        bank, animation_sources = animation_bank(clips)
        (ascii_out / f'{BANK}.mdl').write_text('\n'.join(bank))
        folders[BANK] = BANK_FOLDER
        for root in ROOTS:
            (ascii_out / f'{root}.mdl').write_text('\n'.join(skeleton_model(root, BANK) + [f'donemodel {root}', '']))
            folders[root] = ROOT_FOLDER
        for number in range(1, CHASSIS_COUNT + 1):
            parts, offsets[f'c_astro_{number:02d}'] = chassis_parts(
                number, (sources / f'c_astro_{number:02d}.mdl').read_text(errors='ignore'))
            for name, (text, folder) in parts.items():
                (ascii_out / f'{name}.mdl').write_text(text)
                folders[name] = folder
    for name, text in tool_models((sources / f'c_astro_{TOOL_CHASSIS:02d}.mdl').read_text(errors='ignore')).items():
        (ascii_out / f'{name}.mdl').write_text(text)
        folders[name] = TOOL_FOLDER

    compiled = args.work / 'compiled'
    run_compiler(compiler, 'compile', ascii_out, compiled, args.work / 'compile.log')
    built = sorted(path.stem for path in compiled.glob('*.mdl'))
    if built != sorted(folders):
        raise SystemExit(f'CleanModels wrote {len(built)} of {len(folders)} models; see {args.work / "compile.log"}')
    (args.work / 'report.json').write_text(json.dumps(
        {'animations': animation_sources, 'part_offsets': offsets, 'models': folders}, indent=1))

    if not args.no_install:
        for name, folder in folders.items():
            shutil.copyfile(compiled / f'{name}.mdl', args.haks / folder / f'{name}.mdl')
        copy_tool_icons(args.haks)
    print(f'{len(folders)} models ({len(animation_sources)} animations)',
          'built' if args.no_install else f'installed under {args.haks}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
