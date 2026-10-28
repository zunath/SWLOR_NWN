"""Blender batch renderer for Appearance Editor part pictures (runs inside Blender 4.0 + Neverblender).

blender -b --python blender_render.py -- <batch.json> <out_dir> <size>
batch.json: [{"key": str, "ascii": path, "nodes": {node: material}, "textures": {material: {"color", "metal"}},
              "weapon": "" | "weapon" | "shield", "emitters": [{"node", "image", "width", "length"}]}]
Writes <out_dir>/<key>_<f|b|a>.png with a transparent background. A weapon part with nothing to draw
prints PREVIEW_EMPTY and writes no pictures.
"""
import json
import math
import sys
from pathlib import Path

import addon_utils
import bpy
from mathutils import Matrix, Vector

# NWN models face +Y. Camera yaw 180 looks at the face, 0 at the back, 220 is a 3/4 front view.
VIEWS = {"f": 180.0, "b": 0.0, "a": 220.0}


def setup_scene(size):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.film_transparent = True
    scene.render.resolution_x = scene.render.resolution_y = size
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.eevee.taa_render_samples = 32
    scene.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.new("preview")
    world.use_nodes = True
    background = world.node_tree.nodes["Background"]
    background.inputs["Color"].default_value = (0.35, 0.35, 0.37, 1)
    background.inputs["Strength"].default_value = 0.9
    scene.world = world
    return scene


def make_material(name, textures):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    tree = material.node_tree
    bsdf = tree.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.55
    if not textures:
        bsdf.inputs["Base Color"].default_value = (0.6, 0.6, 0.6, 1)
        return material
    color = tree.nodes.new("ShaderNodeTexImage")
    color.image = bpy.data.images.load(textures["color"])
    color.interpolation = "Closest"  # dye layers are not blended in game
    tree.links.new(color.outputs["Color"], bsdf.inputs["Base Color"])
    if textures.get("cutout"):
        tree.links.new(color.outputs["Alpha"], bsdf.inputs["Alpha"])
        material.blend_method = "CLIP"
        material.shadow_method = "CLIP"
    if textures.get("metal"):
        metal = tree.nodes.new("ShaderNodeTexImage")
        metal.image = bpy.data.images.load(textures["metal"])
        metal.image.colorspace_settings.name = "Non-Color"
        tree.links.new(metal.outputs["Color"], bsdf.inputs["Metallic"])
    return material


def clear_scene():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for collection in (bpy.data.meshes, bpy.data.materials, bpy.data.images, bpy.data.lights, bpy.data.cameras,
                       bpy.data.armatures, bpy.data.actions):
        for block in list(collection):
            if block.users == 0:
                collection.remove(block)


def add_sun(scene, name, energy):
    light = bpy.data.lights.new(name, "SUN")
    light.energy = energy
    obj = bpy.data.objects.new(name, light)
    scene.collection.objects.link(obj)
    return obj


def node_material(nodes, object_name):
    """Material of the MDL node Blender imported as object_name (Blender may add a .00N suffix)."""
    lowered = {n.lower(): m for n, m in nodes.items()}
    name = object_name.lower()
    if name in lowered:
        return lowered[name]
    base, _, suffix = name.rpartition(".")
    if base and suffix.isdigit() and base in lowered:
        return lowered[base]
    distinct = set(nodes.values())
    return distinct.pop() if len(distinct) == 1 else None


def emitter_material(name, texture):
    """Glow: the emitter texture's colour, see-through where its alpha is."""
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    tree = material.node_tree
    bsdf = tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0, 0, 0, 1)
    image = tree.nodes.new("ShaderNodeTexImage")
    image.image = bpy.data.images.load(texture)
    tree.links.new(image.outputs["Color"], bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 1.6
    tree.links.new(image.outputs["Alpha"], bsdf.inputs["Alpha"])
    material.blend_method = "BLEND"
    material.shadow_method = "NONE"
    return material


def emitter_quads(scene, job):
    """One camera-facing quad per textured emitter, as NWN draws a lightsaber blade's particle billboard.

    Each quad is centred on its emitter, sizeStart wide and sizeStart_y long along the emitter's local Y.
    """
    objects = {o.name.lower(): o for o in scene.objects}
    quads = []
    for number, emitter in enumerate(job.get("emitters", [])):
        source = objects.get(emitter["node"].lower())
        if source is None or not emitter.get("image"):
            continue
        axis = (source.matrix_world.to_3x3() @ Vector((0, 1, 0))).normalized()
        mesh = bpy.data.meshes.new(f"glow{number}")
        mesh.from_pydata([(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)], [], [(0, 1, 2, 3)])
        uv = mesh.uv_layers.new()
        for loop, coords in zip(uv.data, ((0, 0), (1, 0), (1, 1), (0, 1))):
            loop.uv = coords
        mesh.materials.append(emitter_material(f"{job['key']}:glow{number}", emitter["image"]))
        obj = bpy.data.objects.new(f"glow{number}", mesh)
        scene.collection.objects.link(obj)
        quads.append({"object": obj, "center": source.matrix_world.translation.copy(), "axis": axis,
                      "width": emitter["width"], "length": emitter["length"]})
    return quads


def face_quads(quads, toward):
    for quad in quads:
        axis = quad["axis"]
        normal = toward - axis * toward.dot(axis)
        if normal.length < 1e-6:
            normal = axis.orthogonal()
        side = axis.cross(normal.normalized()).normalized()
        half_side, half_axis = side * quad["width"] / 2, axis * quad["length"] / 2
        corners = (-half_side - half_axis, half_side - half_axis, half_side + half_axis, -half_side + half_axis)
        for vertex, corner in zip(quad["object"].data.vertices, corners):
            vertex.co = quad["center"] + corner
        quad["object"].data.update()


def quad_points(quads):
    """Points bounding each quad whichever way it faces."""
    points = []
    for quad in quads:
        reach = quad["axis"] * quad["length"] / 2
        for end in (quad["center"] + reach, quad["center"] - reach):
            points += [end + Vector(offset) * quad["width"] / 2
                       for offset in ((1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1))]
    return points


# Weapon parts lie along +Y (blades, staves, shields) or Z (blasters) with their flat side facing X.
WEAPON_ANGLE = math.radians(35)
DIAGONAL_ASPECT = 1.5  # parts this much longer than wide are laid on the picture's diagonal


def weapon_cameras(points, front=Vector((1, 0, 0))):
    """{view: (right, up, toward)} camera bases for front, back and 3/4 pictures of a weapon part.

    front: direction from the part toward the front camera."""
    span = [max(p[i] for p in points) - min(p[i] for p in points) for i in range(3)]
    long_axis = Vector((0, 0, 1)) if span[2] > span[1] else Vector((0, 1, 0))
    sideways = Matrix.Rotation(WEAPON_ANGLE, 3, long_axis) @ front
    result, roll = {}, None
    for view, toward in (("f", front), ("b", -front), ("a", sideways)):
        up = (Vector((0, 1, 0)) - toward * toward[1]).normalized()
        right = up.cross(toward)
        if roll is None:
            width = max(p.dot(right) for p in points) - min(p.dot(right) for p in points)
            height = max(p.dot(up) for p in points) - min(p.dot(up) for p in points)
            # Long parts lie on the rising diagonal: upright ones lean right, flat ones lift their right end.
            if height > width * DIAGONAL_ASPECT:
                roll = -math.radians(45)
            elif width > height * DIAGONAL_ASPECT:
                roll = math.radians(45)
            else:
                roll = 0.0
        right, up = right * math.cos(roll) - up * math.sin(roll), right * math.sin(roll) + up * math.cos(roll)
        result[view] = (right, up, toward)
    return result


def aim_sun(light, travel):
    """Point a sun so its light travels along travel."""
    light.rotation_euler = travel.normalized().to_track_quat("-Z", "Y").to_euler()


def render_weapon(scene, job, out_dir, meshes, quads):
    points = [obj.matrix_world @ v.co for obj in meshes for v in obj.data.vertices] + quad_points(quads)
    camera_data = bpy.data.cameras.new("camera")
    camera_data.type = "ORTHO"
    camera = bpy.data.objects.new("camera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    key, fill = add_sun(scene, "key", 3.0), add_sun(scene, "fill", 1.2)
    # A shield's painted face looks away from the arm, toward -X; weapons show their +X side.
    front = Vector((-1, 0, 0)) if job.get("weapon") == "shield" else Vector((1, 0, 0))
    for view, (right, up, toward) in weapon_cameras(points, front).items():
        face_quads(quads, toward)
        xs, ys, zs = [p.dot(right) for p in points], [p.dot(up) for p in points], [p.dot(toward) for p in points]
        size = max(max(xs) - min(xs), max(ys) - min(ys), 0.01)
        camera_data.ortho_scale = size * 1.08
        camera.location = (right * (max(xs) + min(xs)) / 2 + up * (max(ys) + min(ys)) / 2
                           + toward * (max(zs) + size + 1))
        camera_data.clip_end = max(zs) - min(zs) + size * 2 + 10
        camera.rotation_euler = Matrix((right, up, toward)).transposed().to_euler()
        aim_sun(key, -(toward + up * 0.8 - right * 0.6))
        aim_sun(fill, -(toward * 0.7 + right * 0.9 - up * 0.2))
        scene.render.filepath = str(out_dir / f"{job['key']}_{view}.png")
        bpy.ops.render.render(write_still=True)
    print("PREVIEW_RENDERED", job["key"])


def render_job(scene, job, out_dir):
    clear_scene()
    bpy.ops.scene.nvb_mdlimport(filepath=job["ascii"], import_walkmesh=False, mat_import=False, anim_import=False)
    meshes = [o for o in scene.objects if o.type == "MESH" and not o.hide_render and len(o.data.polygons)]
    quads = emitter_quads(scene, job) if job.get("weapon") else []
    if not meshes and not quads:
        # Some weapon parts are deliberately empty (a pistol's bare top, a lightsaber's hidden middle).
        print("PREVIEW_EMPTY" if job.get("weapon") else "PREVIEW_NO_MESH", job["key"])
        return
    materials = {}
    for obj in meshes:
        name = node_material(job["nodes"], obj.name)
        if name not in materials:
            materials[name] = make_material(f"{job['key']}:{name}", job["textures"].get(name))
        obj.data.materials.clear()
        obj.data.materials.append(materials[name])
    if job.get("weapon"):
        render_weapon(scene, job, out_dir, meshes, quads)
        return
    points =[obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    center, span = (low + high) / 2, high - low
    camera_data = bpy.data.cameras.new("camera")
    camera_data.type = "ORTHO"
    camera = bpy.data.objects.new("camera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    key, fill = add_sun(scene, "key", 3.0), add_sun(scene, "fill", 1.2)
    for view, yaw in VIEWS.items():
        angle = math.radians(yaw)
        toward_camera = Vector((math.sin(angle), -math.cos(angle), 0.0))
        across = abs(span.x * math.cos(angle)) + abs(span.y * math.sin(angle))
        camera_data.ortho_scale = max(across, span.z, 0.01) * 1.1
        camera.location = center + toward_camera * (max(span) * 4 + 1)
        camera.rotation_euler = (math.radians(90), 0, angle)
        key.rotation_euler = (math.radians(55), 0, angle - math.radians(25))
        fill.rotation_euler = (math.radians(70), 0, angle + math.radians(35))
        scene.render.filepath = str(out_dir / f"{job['key']}_{view}.png")
        bpy.ops.render.render(write_still=True)
    print("PREVIEW_RENDERED", job["key"])


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    batch = json.loads(Path(argv[0]).read_text())
    out_dir = Path(argv[1])
    out_dir.mkdir(parents=True, exist_ok=True)
    addon_utils.enable("neverblender", default_set=True)
    scene = setup_scene(int(argv[2]))
    for job in batch:
        try:
            render_job(scene, job, out_dir)
        except Exception as error:  # one bad model must not stop the batch
            print("PREVIEW_FAILED", job["key"], repr(error))


main()
