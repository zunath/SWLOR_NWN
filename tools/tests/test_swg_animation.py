import json
from pathlib import Path
import struct
import sys
import tempfile
import unittest
from unittest.mock import patch

import numpy as np
from scipy.spatial.transform import Rotation

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import swg_animation as swg
import ImportSwgPerformanceAnimations as importer


def chunk(tag, payload):
    return tag.encode() + struct.pack('>I', len(payload)) + payload


def form(tag, *children):
    return chunk('FORM', tag.encode() + b''.join(children))


def quaternion(rotation):
    q = rotation.as_quat()
    return struct.pack('<4f', q[3], *q[:3])


def skeleton():
    return form('SLOD', form('0000', chunk('INFO', struct.pack('<h', 1)), form('SKTM', form('0002',
        chunk('INFO', struct.pack('<i', 1)), chunk('NAME', b'root\0'), chunk('PRNT', struct.pack('<i', -1)),
        chunk('RPRE', quaternion(Rotation.from_euler('y', 20, degrees=True))),
        chunk('RPST', quaternion(Rotation.from_euler('z', 10, degrees=True))),
        chunk('BPRO', quaternion(Rotation.from_euler('x', 30, degrees=True))),
        chunk('BPTR', struct.pack('<3f', 0, 1, 0)), chunk('JROR', struct.pack('<i', 0))))))


def animation(compressed, duplicate_time=False):
    short = compressed
    kind, version = ('CKAT', '0001') if short else ('KFAT', '0003')
    info = struct.pack('<f6h' if short else '<f6i', 30, 30, 1, 1, 0, 1, 2)
    transform = b'root\0' + struct.pack('<BhBhhh' if short else '<Bi4i', 1, 0, 8, 0, 0, 1)
    last = 0 if duplicate_time else 30
    if short:
        rotation = struct.pack('<HBBBHIHI', 2, 254, 254, 254, 0, 0, last, 361)
        translation = struct.pack('<hhfhf', 2, 0, 0, 30, 1)
    else:
        rotation = struct.pack('<ii', 2, 0) + quaternion(Rotation.identity()) + struct.pack('<i', last) + quaternion(Rotation.from_euler('z', 90, degrees=True))
        translation = struct.pack('<iifif', 2, 0, 0, 30, 1)
    return form(kind, form(version, chunk('INFO', info), form('XFRM', chunk('XFIN', transform)),
        form('AROT', chunk('QCHN', rotation)), form('ATRN', chunk('CHNL', translation)),
        chunk('STRN', struct.pack('<ff', 0, 0))))


class SwgAnimationTests(unittest.TestCase):
    def test_compressed_and_uncompressed_tracks_use_the_engine_transform_order(self):
        with tempfile.TemporaryDirectory() as temporary:
            folder = Path(temporary)
            (folder / 'source.skt').write_bytes(skeleton())
            rig = swg.read_skeleton(folder / 'source.skt')
            for compressed in (False, True):
                with self.subTest(compressed=compressed):
                    (folder / 'source.ans').write_bytes(animation(compressed))
                    clip = swg.read_animation(folder / 'source.ans')
                    position, rotations = swg.sample(rig, clip, .5)
                    np.testing.assert_allclose(position[0], [.5, 1, 0], atol=1e-6)
                    angle = np.degrees(2 * np.arcsin(361 / 511)) / 2 if compressed else 45
                    expected = Rotation.from_euler('z', 10, degrees=True) * Rotation.from_euler('z', angle, degrees=True) * Rotation.from_euler('x', 30, degrees=True) * Rotation.from_euler('y', 20, degrees=True)
                    np.testing.assert_allclose(rotations[0].as_matrix(), expected.as_matrix(), atol=1e-6)
                    self.assertEqual(clip['duration'], 1)

    def test_duplicate_key_times_fail_instead_of_changing_the_motion(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / 'broken.ans'
            path.write_bytes(animation(True, duplicate_time=True))
            with self.assertRaises(ValueError):
                swg.read_animation(path)

    def test_iff_rejects_truncation_and_excessive_nesting(self):
        with self.assertRaises(ValueError):
            swg.blocks(b'FORM' + struct.pack('>I', 100) + b'CKAT')
        nested = chunk('DATA', b'')
        for _ in range(34):
            nested = form('TEST', nested)
        with self.assertRaises(ValueError):
            swg.blocks(nested)

    def test_quaternion_formats_keep_sign_bits_separate_from_magnitude(self):
        q = swg.expand((1024 | 100) << 21, [254, 254, 254])
        self.assertAlmostEqual(q[0], -100 / 1023)
        self.assertAlmostEqual(float(np.dot(q, q)), 1)
        with self.assertRaises(ValueError):
            swg.expand(0, [255, 254, 254])

    def test_limb_constraint_preserves_lengths_and_hand_orientation(self):
        joints = [{'Name': 'root', 'Parent': -1}, {'Name': 'elbow', 'Parent': 0}, {'Name': 'hand', 'Parent': 1}]
        pose = [{'Position': dict(zip('XYZ', position)), 'Orientation': dict(zip('XYZW', [0, 0, 0, 1])), 'Scale': 1}
                for position in ([0, 0, 0], [0, 0, -1], [0, 0, -1])]
        target = np.array([.8, .2, -1.2])
        swg.solve_limb(joints, pose, 2, target)
        positions, rotations = swg.world(joints, pose)
        np.testing.assert_allclose(positions[2], target, atol=1e-6)
        self.assertAlmostEqual(np.linalg.norm(positions[1] - positions[0]), 1)
        self.assertAlmostEqual(np.linalg.norm(positions[2] - positions[1]), 1)
        np.testing.assert_allclose(rotations[2].as_matrix(), np.eye(3), atol=1e-6)

    def test_source_paths_cannot_escape_the_source_directory(self):
        with tempfile.TemporaryDirectory() as temporary:
            with self.assertRaises(ValueError):
                importer.resource(Path(temporary), '../private-file')

    def test_contact_evaluation_uses_carry_layer_without_mutating_authored_pose(self):
        joints = [{'Name': 'root', 'Parent': -1}, {'Name': 'rhand', 'Parent': 0}, {'Name': 'tip', 'Parent': 1}]
        pose = [{'Position': dict(zip('XYZ', position)), 'Orientation': dict(zip('XYZW', [0, 0, 0, 1])), 'Scale': 1}
                for position in ([0, 0, 0], [0, 0, 1], [1, 0, 0])]
        carry = {'rhand': Rotation.from_euler('z', 90, degrees=True).as_quat().tolist()}
        positions, _ = swg.contact_world(joints, pose, carry)
        np.testing.assert_allclose(positions[2], [0, 1, 1], atol=1e-6)
        self.assertEqual(pose[1]['Orientation']['W'], 1)
        np.testing.assert_allclose(swg.world(joints, pose)[0][2], [1, 0, 1])

    def test_install_verification_ignores_only_float32_rounding_and_near_zero_noise(self):
        self.assertEqual(importer.comparable({'X': 1.387778739422415e-17}),
                         importer.comparable({'X': 1.3877787e-17, 'IsIdentity': False}))
        self.assertEqual(importer.comparable(.123456789), importer.comparable(float(np.float32(.123456789))))
        self.assertNotEqual(importer.comparable(.123456789), importer.comparable(.123456))
        self.assertNotEqual(importer.comparable(0.0), importer.comparable(1e-8))

    def test_failed_publication_restores_existing_projects(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / 'output'
            output.mkdir()
            (output / 'First.swlanim').write_bytes(b'old first')
            (output / 'manifest.json').write_bytes(b'old manifest')
            real_replace = importer.os.replace
            calls = 0
            def fail_second(source, destination):
                nonlocal calls
                calls += 1
                if calls == 2:
                    raise OSError('Simulated interrupted publication')
                return real_replace(source, destination)
            with patch.object(importer.os, 'replace', side_effect=fail_second):
                with self.assertRaises(OSError):
                    importer.publish(output, {'First.swlanim': b'new first', 'manifest.json': b'new manifest'}, True)
            self.assertEqual((output / 'First.swlanim').read_bytes(), b'old first')
            self.assertEqual((output / 'manifest.json').read_bytes(), b'old manifest')
            self.assertEqual(sorted(path.name for path in output.iterdir()), ['First.swlanim', 'manifest.json'])


if __name__ == '__main__':
    unittest.main()
