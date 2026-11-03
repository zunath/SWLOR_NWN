"""Read SWG CKAT/KFAT tracks and SKTM skeletons in metres and seconds.

Format references: SWG-Source/type_examples ans-animation.mif and
SWG-Source/client-tools CompressedKeyframeAnimationTemplate.cpp / Skeleton.cpp.
The engine composes local rotations as RPST * delta * BPRO * RPRE.
"""
import copy
import json
import math
import struct
import numpy as np
from scipy.spatial.transform import Rotation as R, Slerp

def blocks(data, depth=0):
    if depth > 32 or len(data) > 64 * 1024 * 1024:
        raise ValueError('IFF exceeds size or depth budget')
    out = []
    offset = 0
    while offset < len(data):
        if offset + 8 > len(data):
            raise ValueError('Truncated IFF header')
        tag = data[offset:offset + 4].decode('ascii')
        size = struct.unpack_from('>I', data, offset + 4)[0]
        payload = data[offset + 8:offset + 8 + size]
        if len(payload) != size:
            raise ValueError('Truncated IFF payload')
        out.append((payload[:4].decode('ascii'), blocks(payload[4:], depth + 1)) if tag == 'FORM' else (tag, payload))
        offset += 8 + size
    return out

def one(items, name):
    found = [v for k, v in items if k == name]
    if len(found) != 1:
        raise ValueError(f'Expected one {name}, got {len(found)}')
    return found[0]

def array(data, fmt):
    n = struct.calcsize('<' + fmt)
    if len(data) % n:
        raise ValueError('Invalid array size')
    return list(struct.iter_unpack('<' + fmt, data))

def expand(packed, formats):
    values = []
    for data, fmt, bits in zip([packed >> 21, packed >> 10, packed], formats, [11, 11, 10]):
        for k in range(7):
            if fmt & (255 ^ ((1 << k) - 1)) == 256 - (1 << (k + 1)):
                break
        else:
            raise ValueError(f'Invalid quaternion format {fmt}')
        separation = 2 / ((1 << k) + 1)
        base = -1 + ((fmt & (1 << k) - 1) + 1) * separation
        mask = (1 << (bits - 1)) - 1
        delta = (data & mask) * separation / mask
        values.append(base - delta if data & (1 << (bits - 1)) else base + delta)
    norm = sum((x * x for x in values))
    if norm > 1.005:
        raise ValueError(f'Invalid quaternion norm {norm}')
    return np.array([*values, math.sqrt(max(0, 1 - norm))]) / math.sqrt(max(1, norm))

def read_skeleton(path):
    top = blocks(path.read_bytes())
    lod = one(one(top, 'SLOD'), '0000')
    parts = one([x for x in lod if x[0] == 'SKTM'][:1], 'SKTM')
    d = one(parts, '0002')
    count = struct.unpack('<i', one(d, 'INFO'))[0]
    names = one(d, 'NAME').decode().rstrip('\x00').split('\x00')
    parents = np.array(array(one(d, 'PRNT'), 'i')).reshape(-1)
    positions = np.array(array(one(d, 'BPTR'), 'fff'))
    rots = {tag: R.from_quat(np.array(array(one(d, tag), 'ffff'))[:, [1, 2, 3, 0]]) for tag in ('RPRE', 'RPST', 'BPRO')}
    if len(names) != count or len(set((n.lower() for n in names))) != count:
        raise ValueError('Invalid skeleton names')
    if any((p >= i or p < (-1 if i == 0 else 0) for i, p in enumerate(parents))):
        raise ValueError('Invalid skeleton hierarchy')
    return dict(names=names, parents=parents, positions=positions, **rots)

def read_animation(path):
    top = blocks(path.read_bytes())
    kind, versions = top[0]
    compressed = kind == 'CKAT'
    if kind not in ('CKAT', 'KFAT') or len(top) != 1:
        raise ValueError('Unsupported animation')
    d = one(versions, '0001' if compressed else '0003')
    fps, frames, transforms, rotation_count, static_rotation_count, translation_count, static_translation_count = struct.unpack('<f6h' if compressed else '<f6i', one(d, 'INFO'))
    if not math.isfinite(fps) or not 0 < fps <= 60 or not 0 < frames <= 18000 or frames / fps > 600:
        raise ValueError('Invalid timing')
    rs = []
    for tag, data in one(d, 'AROT'):
        if tag != 'QCHN':
            raise ValueError(tag)
        if compressed:
            count, *formats = struct.unpack_from('<HBBB', data)
            keys = array(data[5:], 'HI')
            values = [expand(v, formats) for _, v in keys]
        else:
            count = struct.unpack_from('<i', data)[0]
            keys = array(data[4:], 'iffff')
            values = [np.array(q)[[1, 2, 3, 0]] for _, *q in keys]
        if count != len(keys):
            raise ValueError('Rotation channel length mismatch')
        rs.append((np.array([k[0] for k in keys]), R.from_quat(values)))
    srdata = one(d, 'SROT') if static_rotation_count else b''
    srot = [expand(v, [x, y, z]) for x, y, z, v in array(srdata, 'BBBI')] if compressed else [np.array(q)[[1, 2, 3, 0]] for q in array(srdata, 'ffff')]
    ts = []
    for tag, data in one(d, 'ATRN'):
        if tag != 'CHNL':
            raise ValueError(tag)
        count = struct.unpack_from('<h' if compressed else '<i', data)[0]
        keys = array(data[2 if compressed else 4:], 'hf' if compressed else 'if')
        if count != len(keys):
            raise ValueError('Translation channel length mismatch')
        ts.append((np.array([k[0] for k in keys]), np.array([k[1] for k in keys])))
    st = np.array(array(one(d, 'STRN'), 'f')).reshape(-1)
    tracks = {}
    for tag, data in one(d, 'XFRM'):
        if tag != 'XFIN':
            raise ValueError(tag)
        end = data.index(0)
        name = data[:end].decode().lower()
        animated, rotation, mask, *indices = struct.unpack('<BhBhhh' if compressed else '<Bi4i', data[end + 1:])
        if name in tracks:
            raise ValueError('Duplicate track')
        if animated not in (0, 1) or not 0 <= mask <= 63 or not 0 <= rotation < (len(rs) if animated else len(srot)):
            raise ValueError('Invalid transform channel reference')
        if any(not 0 <= index < (len(ts) if mask & (8 << axis) else len(st)) for axis, index in enumerate(indices)):
            raise ValueError('Invalid translation channel reference')
        if animated:
            keys, values = rs[rotation]
            qr = (keys, values)
        else:
            qr = (np.array([0]), R.from_quat([srot[rotation]]))
        translations = []
        for axis, index in enumerate(indices):
            translations.append(ts[index] if mask & 8 << axis else (np.array([0]), np.array([st[index]])))
        tracks[name] = ((*qr, Slerp(*qr) if len(qr[0]) > 1 else None), translations)
    if [len(tracks), len(rs), len(srot), len(ts), len(st)] != [transforms, rotation_count, static_rotation_count, translation_count, static_translation_count]:
        raise ValueError('Header counts mismatch')
    for qr, tr in tracks.values():
        for keys in [qr[0], *[x[0] for x in tr]]:
            if len(keys) == 0 or np.any(np.diff(keys) <= 0) or keys[0] < 0 or (keys[-1] > frames):
                raise ValueError('Invalid key times')
    locomotion = None
    if any((tag == 'LOCT' for tag, _ in d)):
        data = one(d, 'LOCT')
        offset = 6 if compressed else 8
        stride = 'hfff' if compressed else 'ifff'
        loco = array(data[offset:], stride)
        count = struct.unpack_from('<h' if compressed else '<i', data, 4)[0]
        if count != len(loco) or not loco or any(loco[i][0] >= loco[i+1][0] for i in range(len(loco)-1)) or loco[0][0] < 0 or loco[-1][0] > frames:
            raise ValueError('Invalid locomotion keys')
        locomotion = (np.array([x[0] for x in loco]), np.array([x[1:] for x in loco]))
    return dict(name=path.stem, fps=fps, frames=frames, duration=frames / fps, tracks=tracks, kind=kind, locomotion=locomotion)

def sample(skeleton, animation, time):
    frame = min(animation['frames'], max(0, time * animation['fps']))
    wp = []
    wr = []
    for i, name in enumerate(skeleton['names']):
        position = skeleton['positions'][i].copy()
        delta = R.identity()
        if name.lower() in animation['tracks']:
            (keys, values, interpolator), translations = animation['tracks'][name.lower()]
            delta = values[0] if len(keys) == 1 else interpolator(min(keys[-1], max(keys[0], frame)))
            position += np.array([np.interp(frame, *axis) for axis in translations])
        rotation = skeleton['RPST'][i] * delta * skeleton['BPRO'][i] * skeleton['RPRE'][i]
        parent = skeleton['parents'][i]
        if parent >= 0:
            position = wp[parent] + wr[parent].apply(position)
            rotation = wr[parent] * rotation
        wp.append(position)
        wr.append(rotation)
    wp = np.array(wp)
    if animation.get('locomotion') is not None:
        keys, values = animation['locomotion']
        loco = np.array([np.interp(frame, keys, values[:, i]) for i in range(3)])
        delta = loco - values[0] - (values[-1] - values[0]) * frame / animation['frames']
        wp += delta
    return (wp, wr)
S = np.array([[1, 0, 0], [0, 0, 1], [0, 1, 0]])

def cross_coordinate(q):
    return R.from_matrix(S @ q.as_matrix() @ S)

def between(a, b):
    a = a / np.linalg.norm(a)
    b = b / np.linalg.norm(b)
    d = float(np.dot(a, b))
    if d < -0.999999:
        axis = np.cross(a, [1, 0, 0])
        if np.linalg.norm(axis) < 1e-06:
            axis = np.cross(a, [0, 1, 0])
        return R.from_rotvec(axis / np.linalg.norm(axis) * math.pi)
    q = np.r_[np.cross(a, b), 1 + d]
    return R.from_quat(q / np.linalg.norm(q))

def world(joints, pose):
    wp = []
    wr = []
    for j, p in zip(joints, pose):
        t = np.array([p['Position'][k] for k in 'XYZ'])
        q = R.from_quat([p['Orientation'][k] for k in 'XYZW'])
        if j['Parent'] >= 0:
            t = wp[j['Parent']] + wr[j['Parent']].apply(t)
            q = wr[j['Parent']] * q
        wp.append(t)
        wr.append(q)
    return (np.array(wp), wr)


def set_world_rotation(joints, pose, index, rotation):
    _, rotations = world(joints, pose)
    parent = joints[index]['Parent']
    local = rotations[parent].inv() * rotation if parent >= 0 else rotation
    pose[index]['Orientation'] = dict(zip('XYZW', map(float, local.as_quat())))


def solve_limb(joints, pose, end, target):
    mid = joints[end]['Parent']
    root = joints[mid]['Parent']
    positions, rotations = world(joints, pose)
    a, b, c = positions[root], positions[mid], positions[end]
    original_end_rotation = rotations[end]
    first, second = np.linalg.norm(b - a), np.linalg.norm(c - b)
    direction = np.asarray(target) - a
    if min(first, second, np.linalg.norm(direction)) < 1e-6:
        raise ValueError('Degenerate limb constraint')
    distance = min(first + second - .0001, max(abs(first - second) + .0001, np.linalg.norm(direction)))
    direction /= np.linalg.norm(direction)
    bend = b - a - direction * np.dot(b - a, direction)
    if np.linalg.norm(bend) < 1e-6:
        bend = np.cross(direction, [0, 0, 1] if abs(direction[2]) < .9 else [1, 0, 0])
    bend /= np.linalg.norm(bend)
    along = (first * first + distance * distance - second * second) / (2 * distance)
    elbow = a + direction * along + bend * math.sqrt(max(0, first * first - along * along))
    set_world_rotation(joints, pose, root, between(b - a, elbow - a) * rotations[root])
    positions, rotations = world(joints, pose)
    set_world_rotation(joints, pose, mid, between(positions[end] - positions[mid], a + direction * distance - positions[mid]) * rotations[mid])
    set_world_rotation(joints, pose, end, original_end_rotation)


def contact_world(joints, pose, carry=None):
    """Evaluate the arm layer the client actually uses for held instruments."""
    if not carry:
        return world(joints, pose)
    evaluated = copy.deepcopy(pose)
    index = {joint['Name']: i for i, joint in enumerate(joints)}
    for name, quaternion in carry.items():
        evaluated[index[name]]['Orientation'] = dict(zip('XYZW', quaternion))
    return world(joints, evaluated)


def fit_contacts(paths, family, carry=None):
    if carry is not None:
        if family != 'Strings' or set(carry) != {'rbicep_g', 'rforearm_g', 'rhand_g', 'rhand'}:
            raise ValueError('Carry layer must describe exactly the string instrument arm')
        for quaternion in carry.values():
            if len(quaternion) != 4 or not np.all(np.isfinite(quaternion)) or abs(np.linalg.norm(quaternion) - 1) > 1e-5:
                raise ValueError('Carry layer requires normalized finite quaternions')
    samples = []
    for path in paths:
        project = json.loads(path.read_text(encoding='utf-8-sig'))
        index = {joint['Name']: i for i, joint in enumerate(project['Joints'])}
        for key in project['Keys']:
            positions, rotations = contact_world(project['Joints'], key['Pose'], carry)
            samples.append((positions, rotations, index))
    if family == 'Horns':
        matrix, values = [], []
        for positions, rotations, index in samples:
            head, hand = index['head_g'], index['rhand']
            matrix.extend(np.c_[rotations[hand].as_matrix(), -rotations[head].as_matrix()])
            values.extend(positions[head] - positions[hand])
        solution, _, rank, _ = np.linalg.lstsq(np.array(matrix), np.array(values), rcond=None)
        errors = (np.array(matrix) @ solution - values).reshape(-1, 3)
        residual = float(max(np.linalg.norm(errors, axis=1)))
        if rank != 6 or residual > .005 or np.linalg.norm(solution[3:]) > .25:
            raise ValueError('Existing horn clips do not establish a consistent mouth contact')
        return {'Family': family, 'HandPoint': solution[:3], 'HeadPoint': solution[3:], 'MaximumResidualMetres': residual}
    if family != 'Strings':
        raise ValueError('Unknown instrument contact family')
    vectors, middle = [], []
    for path in paths:
        project = json.loads(path.read_text(encoding='utf-8-sig'))
        index = {joint['Name']: i for i, joint in enumerate(project['Joints'])}
        local = []
        for key in project['Keys']:
            positions, rotations = contact_world(project['Joints'], key['Pose'], carry)
            vector = rotations[index['rhand']].inv().apply(positions[index['lhand_g']] - positions[index['rhand']])
            vectors.append(vector)
            local.append(vector)
        middle.append(local[len(local) // 2])
    axis = middle[-1] - middle[0]
    if np.linalg.norm(axis) < .01:
        raise ValueError('Existing string accents do not establish a playing direction')
    axis /= np.linalg.norm(axis)
    return {'Family': family, 'PlayingAxis': axis, 'PlayingPositions': np.array(vectors), 'NativeCarry': carry}


def constrain_contacts(joints, pose, reference, contact):
    index = {joint['Name']: i for i, joint in enumerate(joints)}
    initial, initial_rotation = contact_world(joints, reference, contact.get('NativeCarry'))
    position, rotation = contact_world(joints, pose, contact.get('NativeCarry'))
    prop, wrist, left, head = [index[name] for name in ['rhand', 'rhand_g', 'lhand_g', 'head_g']]
    if contact['Family'] == 'Horns':
        desired_prop = position[head] + rotation[head].apply(contact['HeadPoint']) - rotation[prop].apply(contact['HandPoint'])
        attachment_offset = rotation[wrist].apply(np.array([pose[prop]['Position'][axis] for axis in 'XYZ']))
        solve_limb(joints, pose, wrist, desired_prop - attachment_offset)
        position, rotation = world(joints, pose)
    left_reference = initial_rotation[prop].inv().apply(initial[left] - initial[prop])
    offset = left_reference.copy()
    if contact['Family'] == 'Strings':
        current = rotation[prop].inv().apply(position[left] - position[prop])
        axis = contact['PlayingAxis']
        bounds = (contact['PlayingPositions'] - left_reference) @ axis
        displacement = np.clip(np.dot(current - left_reference, axis), min(bounds), max(bounds))
        offset += axis * displacement
    target = position[prop] + rotation[prop].apply(offset)
    desired_rotation = rotation[prop] * initial_rotation[prop].inv() * initial_rotation[left]
    solve_limb(joints, pose, left, target)
    set_world_rotation(joints, pose, left, desired_rotation)


def retarget(skeleton_path, animation_path, reference_path, name, boundary_blend=0.25, calibration="bind", mirror=False, contact=None):
    sk = read_skeleton(skeleton_path)
    a = read_animation(animation_path)
    unknown = set(a['tracks']) - {joint.lower() for joint in sk['names']}
    if unknown:
        raise ValueError('Animation does not match the configured skeleton: ' + ', '.join(sorted(unknown)))
    restanim = dict(frames=0, fps=30, tracks={})
    if calibration not in ('bind', 'first-frame'):
        raise ValueError('Unknown calibration mode')
    coordinate = np.diag([-1, 1, 1]) @ S if mirror else S
    def convert_rotation(q):
        return R.from_matrix(coordinate @ q.as_matrix() @ coordinate.T)
    bp, br = sample(sk, a if calibration == 'first-frame' else restanim, 0)
    bp = bp @ coordinate.T
    br = [convert_rotation(q) for q in br]
    sn = {n.lower(): i for i, n in enumerate(sk['names'])}
    rig = json.loads(reference_path.read_text(encoding='utf-8-sig'))
    joints = rig['Joints']
    rest = [copy.deepcopy(j['Rest']) for j in joints]
    reference = rig['Keys'][0]['Pose']
    base_pose = reference if calibration == 'first-frame' else rest
    tp, tr = world(joints, base_pose)
    tn = {j['Name']: i for i, j in enumerate(joints)}
    mapping = {'rootdummy': 'root', 'pelvis_g': 'root', 'torso_g': 'spine3', 'neck_g': 'neck', 'head_g': 'head', 'lbicep_g': 'larm', 'lforearm_g': 'lforearm', 'lhand_g': 'lwrist', 'rbicep_g': 'rarm', 'rforearm_g': 'rforearm', 'rhand_g': 'rwrist', 'lthigh_g': 'lthigh', 'lshin_g': 'lshin', 'lfoot_g': 'lankle', 'rthigh_g': 'rthigh', 'rshin_g': 'rshin', 'rfoot_g': 'rankle'}
    endpoints = {'lbicep_g': ('lforearm_g', 'lforearm'), 'lforearm_g': ('lhand_g', 'lwrist'), 'rbicep_g': ('rforearm_g', 'rforearm'), 'rforearm_g': ('rhand_g', 'rwrist'), 'lthigh_g': ('lshin_g', 'lshin'), 'lshin_g': ('lfoot_g', 'lankle'), 'rthigh_g': ('rshin_g', 'rshin'), 'rshin_g': ('rfoot_g', 'rankle')}
    if mirror:
        mapping = {target: ('r' + source[1:] if source.startswith('l') else 'l' + source[1:] if source.startswith('r') and source != 'root' else source) for target, source in mapping.items()}
    offsets = {}
    for t, s in mapping.items():
        ti = tn[t]
        si = sn[s]
        calibrated = tr[ti]
        if t in endpoints and calibration == "bind":
            te, se = endpoints[t]
            calibrated = between(tp[tn[te]] - tp[ti], bp[sn[se]] - bp[si]) * calibrated
        offsets[ti] = (si, br[si].inv() * calibrated)
    source_leg = np.linalg.norm(bp[sn['lshin']] - bp[sn['lthigh']]) + np.linalg.norm(bp[sn['lankle']] - bp[sn['lshin']])
    target_leg = np.linalg.norm(tp[tn['lshin_g']] - tp[tn['lthigh_g']]) + np.linalg.norm(tp[tn['lfoot_g']] - tp[tn['lshin_g']])
    scale = target_leg / source_leg
    target_root = tn['rootdummy']
    result = copy.deepcopy(rig)
    result['Keys'] = []
    result['Events'] = []
    result['Name'] = name
    result['Duration'] = a['duration']
    times = [i / a['fps'] for i in range(a['frames'] + 1)]
    minfoot = 1000000000.0
    maxfoot = -1000000000.0
    reference = rig['Keys'][0]['Pose']
    reference_world, _ = world(joints, reference)
    clearance = min((reference_world[tn[f]][2] for f in ['lfoot_g', 'rfoot_g']))
    for time in times:
        sp, sr = sample(sk, a, time)
        sp = sp @ coordinate.T
        sr = [convert_rotation(q) for q in sr]
        pose = copy.deepcopy(base_pose)
        wp = []
        wr = []
        for i, (j, p) in enumerate(zip(joints, pose)):
            par = j['Parent']
            parent = wr[par] if par >= 0 else R.identity()
            q = R.from_quat([p['Orientation'][k] for k in 'XYZW'])
            v = np.array([p['Position'][k] for k in 'XYZ'])
            if i in offsets:
                si, offset = offsets[i]
                q = parent.inv() * sr[si] * offset
                if i == target_root:
                    v = tp[i] + (sp[si] - bp[si]) * scale
            p['Orientation'] = {k: float(x) for k, x in zip('XYZW', q.as_quat())}
            p['Position'] = {k: float(x) for k, x in zip('XYZ', v)}
            wp.append(wp[par] + parent.apply(v) if par >= 0 else v)
            wr.append(parent * q)
        feet = min((wp[tn[f]][2] for f in ['lfoot_g', 'rfoot_g']))
        minfoot = min(minfoot, feet)
        maxfoot = max(maxfoot, feet)
        pose[target_root]['Position']['Z'] += max(0, clearance - feet)
        if contact is not None:
            constrain_contacts(joints, pose, reference, contact)
        if boundary_blend > 0:
            blend = min(1, time / boundary_blend, (a['duration'] - time) / boundary_blend)
            blend = max(0, blend)
            blend = blend * blend * (3 - 2 * blend)
            for i, p in enumerate(pose):
                for axis in 'XYZ':
                    p['Position'][axis] = reference[i]['Position'][axis] * (1 - blend) + p['Position'][axis] * blend
                q0 = R.from_quat([reference[i]['Orientation'][k] for k in 'XYZW'])
                q1 = R.from_quat([p['Orientation'][k] for k in 'XYZW'])
                q = Slerp([0, 1], R.from_quat([q0.as_quat(), q1.as_quat()]))(blend)
                p['Orientation'] = {k: float(x) for k, x in zip('XYZW', q.as_quat())}
        final_world, _ = world(joints, pose)
        final_clearance = min(final_world[tn[foot]][2] for foot in ['lfoot_g', 'rfoot_g'])
        pose[target_root]['Position']['Z'] += max(0, clearance - final_clearance)
        result['Keys'].append({'Time': time, 'Pose': pose})
    print(name, 'duration', a['duration'], 'frames', len(times), 'scale', scale, 'foot range', minfoot, maxfoot)
    return result
