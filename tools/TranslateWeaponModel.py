"""Translate rigid compiled NWN weapon MDLs without rewriting their geometry."""
from __future__ import annotations

import argparse
import math
from pathlib import Path
import struct


FILE_HEADER_SIZE = 12
MODEL_HEADER_SIZE = 232
NODE_HEADER_SIZE = 112
TRIMESH_HEADER_SIZE = 512
MODEL_BASE = FILE_HEADER_SIZE


def _u32(data: bytes | bytearray, offset: int) -> int:
    return struct.unpack_from("<I", data, offset)[0]


def _range(data: bytes | bytearray, start: int, size: int, description: str,
           model_end: int) -> tuple[int, int]:
    end = start + size
    if start < MODEL_BASE or size < 0 or end > model_end or end > len(data):
        raise ValueError(f"{description} points outside compiled MDL model data")
    return start, end


def _fixed_string(data: bytes | bytearray, offset: int, size: int) -> bytes:
    return bytes(data[offset:offset + size]).split(b"\0", 1)[0]


def _definition(data: bytes | bytearray, offset: int, description: str) -> tuple[int, int]:
    return struct.unpack_from("<II", data, offset)


def _finite_float(data: bytes | bytearray, offset: int, description: str) -> float:
    value = struct.unpack_from("<f", data, offset)[0]
    if not math.isfinite(value):
        raise ValueError(f"{description} must be finite")
    return value


def _finite_triple(values, description: str) -> tuple[float, float, float]:
    if len(values) != 3 or any(not isinstance(value, (int, float)) or not math.isfinite(value)
                               for value in values):
        raise ValueError(f"{description} must contain three finite numbers")
    return tuple(float(value) for value in values)


def _quaternion_product(a: tuple[float, float, float, float],
                        b: tuple[float, float, float, float]) -> tuple[float, float, float, float]:
    """Hamilton product a * b of (x, y, z, w) quaternions: b's rotation, then a's."""
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by,
            aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz)


def _rotate_vector(rotation: tuple[float, float, float, float],
                   vector: tuple[float, float, float]) -> tuple[float, float, float]:
    x, y, z, _ = _quaternion_product(_quaternion_product(rotation, (*vector, 0.0)),
                                     (-rotation[0], -rotation[1], -rotation[2], rotation[3]))
    return x, y, z


def translate_model(data: bytes, delta: tuple[float, float, float], pitch: float = 0.0,
                    pivot: tuple[float, float, float] = (0.0, 0.0, 0.0), roll: float = 0.0,
                    yaw: float = 0.0) -> bytes:
    """Return a translated copy of one rigid, static compiled weapon MDL.

    Only each direct root child's one-row position values and the model bounds
    change. Vertex, mesh, material and orientation data remain byte-identical.
    Nonzero angles (degrees) first rotate each child through `pivot`: pitch
    about the weapon X axis (positive raises a -Z barrel), then roll about Y,
    then yaw about Z (both right-handed). Rotation also rewrites the child's
    static orientation and conservatively widens the bounds.
    """
    offset = _finite_triple(delta, "offset")
    pivot = _finite_triple(pivot, "pivot")
    for name, value in (("pitch", pitch), ("roll", roll), ("yaw", yaw)):
        if not isinstance(value, (int, float)) or not math.isfinite(value):
            raise ValueError(f"{name} must be finite")
    angles = tuple(math.radians(float(value)) for value in (pitch, roll, yaw))
    rotation = (0.0, 0.0, 0.0, 1.0)
    for axis, value in enumerate(angles):
        if value:
            step = [0.0, 0.0, 0.0, math.cos(value / 2)]
            step[axis] = math.sin(value / 2)
            rotation = _quaternion_product(tuple(step), rotation)
    angle = min(math.pi, sum(abs(value) for value in angles))
    distance = math.sqrt(sum(value * value for value in offset))
    if not math.isfinite(distance):
        raise ValueError("offset length must be finite")
    if len(data) < FILE_HEADER_SIZE + MODEL_HEADER_SIZE:
        raise ValueError("compiled MDL is shorter than its headers")

    magic, model_length, raw_length = struct.unpack_from("<III", data, 0)
    if magic != 0:
        raise ValueError("input is not a compiled NWN MDL")
    model_end = MODEL_BASE + model_length
    if model_length < MODEL_HEADER_SIZE or model_end + raw_length != len(data):
        raise ValueError("compiled MDL lengths do not match the input")
    _range(data, MODEL_BASE, MODEL_HEADER_SIZE, "model header", model_end)

    if _fixed_string(data, MODEL_BASE + 168, 64) not in (b"", b"NULL"):
        raise ValueError("weapon MDL must have an empty or NULL supermodel")
    _, animation_count = _definition(data, MODEL_BASE + 120, "model animations")
    if animation_count != 0:
        raise ValueError("animated weapon models are not supported")

    bounds = [_finite_float(data, MODEL_BASE + position, f"model bound {index}")
              for index, position in enumerate((136, 140, 144, 148, 152, 156))]
    minimum, maximum = bounds[:3], bounds[3:]
    if any(low > high for low, high in zip(minimum, maximum)):
        raise ValueError("model bounds are inverted")
    radius = _finite_float(data, MODEL_BASE + 160, "model radius")
    if radius < 0:
        raise ValueError("model radius cannot be negative")

    root_pointer = _u32(data, MODEL_BASE + 72)
    if root_pointer == 0:
        raise ValueError("weapon model has no geometry root")
    root = MODEL_BASE + root_pointer
    if root < MODEL_BASE + MODEL_HEADER_SIZE:
        raise ValueError("geometry root overlaps the model header")
    _range(data, root, NODE_HEADER_SIZE, "geometry root", model_end)
    root_flags = _u32(data, root + 108)
    if root_flags != 1:
        raise ValueError("weapon geometry root must be a rigid dummy")
    root_key_pointer, root_key_count = _definition(data, root + 84, "root controllers")
    root_value_pointer, root_value_count = _definition(data, root + 96, "root controller values")
    if any((root_key_pointer, root_key_count, root_value_pointer, root_value_count)):
        raise ValueError("geometry root must not have controllers")

    children_pointer, children_count = _definition(data, root + 72, "root children")
    if children_count == 0:
        raise ValueError("weapon geometry root has no mesh children")
    children_start = MODEL_BASE + children_pointer
    child_array = _range(data, children_start, children_count * 4, "root child array", model_end)
    child_pointers = [struct.unpack_from("<I", data, children_start + index * 4)[0]
                      for index in range(children_count)]
    if any(pointer == 0 for pointer in child_pointers) or len(set(child_pointers)) != children_count:
        raise ValueError("root child pointers must be nonzero and unique")

    # Arrays must be distinct from structural records and from each other.
    protected = [(MODEL_BASE, MODEL_BASE + MODEL_HEADER_SIZE, "model header"),
                 (root, root + NODE_HEADER_SIZE, "root node"),
                 (*child_array, "root child array")]
    edits: list[tuple[int, tuple[float, float, float]]] = []
    arrays: list[tuple[int, int, str]] = []
    for child_pointer in child_pointers:
        child = MODEL_BASE + child_pointer
        _range(data, child, NODE_HEADER_SIZE + TRIMESH_HEADER_SIZE,
               "rigid trimesh node", model_end)
        if any(child < other_end and other_start < child + NODE_HEADER_SIZE + TRIMESH_HEADER_SIZE
               for other_start, other_end, description in protected):
            raise ValueError("trimesh node aliases another model structure")
        flags = _u32(data, child + 108)
        if flags & 0x40:
            raise ValueError("skinned weapon meshes are not supported")
        if flags != 0x21:
            raise ValueError("direct root children must be rigid trimesh nodes")
        _, nested_count = _definition(data, child + 72, "trimesh children")
        if nested_count:
            raise ValueError("nested weapon nodes are not supported")
        key_pointer, key_count = _definition(data, child + 84, "trimesh controllers")
        value_pointer, value_count = _definition(data, child + 96, "trimesh controller values")
        if key_count != 2 or value_count != 9 or key_pointer == 0 or value_pointer == 0:
            raise ValueError("trimesh must have static position and orientation controllers")
        key_start = MODEL_BASE + key_pointer
        value_start = MODEL_BASE + value_pointer
        key_range = _range(data, key_start, key_count * 12, "trimesh controller keys", model_end)
        value_range = _range(data, value_start, value_count * 4, "trimesh controller values", model_end)
        keys = [struct.unpack_from("<IHHHBB", data, key_start + index * 12)
                for index in range(key_count)]
        position_key = next((key for key in keys if key[0] == 8), None)
        orientation_key = next((key for key in keys if key[0] == 20), None)
        if position_key is None or orientation_key is None:
            raise ValueError("trimesh must have position and orientation controllers")
        if keys[0] != (8, 1, 0, 1, 3, keys[0][5]) or keys[1] != (20, 1, 4, 5, 4, keys[1][5]):
            raise ValueError("trimesh transform controller windows are unexpected")
        if position_key[1] != 1 or position_key[4] != 3:
            raise ValueError("position controller must contain one row of three values")
        if orientation_key[1] != 1 or orientation_key[4] != 4:
            raise ValueError("orientation controller must contain one static quaternion")
        time_start, value_index = position_key[2], position_key[3]
        for key in (position_key, orientation_key):
            if key[2] >= value_count or key[3] + key[1] * key[4] > value_count:
                raise ValueError("transform controller ranges exceed its values array")
        windows = [(key[2], key[2] + key[1]) for key in (position_key, orientation_key)] + [
            (key[3], key[3] + key[1] * key[4]) for key in (position_key, orientation_key)]
        for index, (start, end) in enumerate(windows):
            if any(start < other_end and other_start < end for other_start, other_end in windows[:index]):
                raise ValueError("transform controller windows overlap")
        position = tuple(_finite_float(data, value_start + (value_index + axis) * 4,
                                       "position controller value") for axis in range(3))
        # Static transform arrays use independent time and value windows.
        orientation = tuple(_finite_float(data, value_start + (orientation_key[3] + axis) * 4,
                                          "orientation controller value") for axis in range(4))
        protected.append((child, child + NODE_HEADER_SIZE + TRIMESH_HEADER_SIZE, "trimesh node"))
        arrays.extend([(key_range[0], key_range[1], "controller keys"),
                       (value_range[0], value_range[1], "controller values")])
        edits.append((value_start + value_index * 4, position,
                      value_start + orientation_key[3] * 4, orientation))

    for index, (start, end, description) in enumerate(arrays):
        if any(start < other_end and other_start < end
               for other_start, other_end, other_description in protected):
            raise ValueError(f"{description} alias a structural record")
        for other_start, other_end, other_description in arrays[:index]:
            if start < other_end and other_start < end:
                raise ValueError(f"{description} alias another controller array")

    result = bytearray(data)
    half_sine = math.sin(angle / 2)
    for value_offset, position, orientation_offset, orientation in edits:
        if angle:
            relative = _rotate_vector(rotation, tuple(position[axis] - pivot[axis] for axis in range(3)))
            position = tuple(pivot[axis] + relative[axis] for axis in range(3))
            # The rotation applies in the parent frame, after the child's own static orientation.
            rotated = _quaternion_product(rotation, orientation)
            length = math.sqrt(sum(value * value for value in rotated))
            if not math.isfinite(length) or length == 0:
                raise ValueError("rotated orientation must be a finite, nonzero quaternion")
            rotated = tuple(value / length for value in rotated)
            struct.pack_into("<4f", result, orientation_offset, *rotated)
        for axis, amount in enumerate(offset):
            if amount == 0 and not angle:
                continue
            translated_position = position[axis] + amount
            if not math.isfinite(translated_position):
                raise ValueError("translated position must remain finite")
            struct.pack_into("<f", result, value_offset + axis * 4, translated_position)
    # A point at distance d from the pivot moves at most 2 d sin(|angle| / 2).
    sweep = 2 * (radius + math.sqrt(sum(value * value for value in pivot))) * abs(half_sine)
    for axis, amount in enumerate(offset):
        if amount == 0 and not sweep:
            continue
        translated_minimum = minimum[axis] + amount - sweep
        translated_maximum = maximum[axis] + amount + sweep
        if not math.isfinite(translated_minimum) or not math.isfinite(translated_maximum):
            raise ValueError("translated model bounds must remain finite")
        struct.pack_into("<f", result, MODEL_BASE + 136 + axis * 4, translated_minimum)
        struct.pack_into("<f", result, MODEL_BASE + 148 + axis * 4, translated_maximum)
    distance += sweep
    translated_radius = radius + distance
    if not math.isfinite(translated_radius):
        raise ValueError("translated model radius must remain finite")
    if distance != 0:
        struct.pack_into("<f", result, MODEL_BASE + 160, translated_radius)
    return bytes(result)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--offset", nargs=3, type=float, default=(0.0, 0.0, 0.0), metavar=("X", "Y", "Z"))
    parser.add_argument("--pitch", type=float, default=0.0,
                        help="Degrees about the weapon X axis through --pivot; positive raises the muzzle")
    parser.add_argument("--roll", type=float, default=0.0,
                        help="Degrees about the weapon Y axis through --pivot, applied after --pitch")
    parser.add_argument("--yaw", type=float, default=0.0,
                        help="Degrees about the weapon Z axis through --pivot, applied after --roll")
    parser.add_argument("--pivot", nargs=3, type=float, default=(0.0, 0.0, 0.0), metavar=("X", "Y", "Z"),
                        help="Rotation pivot in weapon attachment space, normally the grip contact")
    args = parser.parse_args()
    source = args.input.resolve()
    destination = args.output.resolve()
    if source == destination:
        parser.error("input and output must be different files")
    if destination.exists():
        parser.error(f"output already exists: {destination}")
    translated = translate_model(source.read_bytes(), tuple(args.offset), args.pitch, tuple(args.pivot),
                                 args.roll, args.yaw)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(translated)


if __name__ == "__main__":
    main()
