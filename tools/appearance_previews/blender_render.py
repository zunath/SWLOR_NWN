"""Blender batch renderer for Appearance Editor part pictures (runs inside Blender 4.0 + Neverblender).

blender -b --python blender_render.py -- <batch.json> <out_dir> <size>
batch.json: [{"key": str, "ascii": path, "nodes": {node: material}, "textures": {material: {"color", "metal"}}}]
Writes <out_dir>/<key>_<f|b|a>.png with a transparent background.
"""
import json
import math
import sys
from pathlib import Path

import addon_utils
import bpy
from mathutils import Vector

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


def render_job(scene, job, out_dir):
    clear_scene()
    bpy.ops.scene.nvb_mdlimport(filepath=job["ascii"], import_walkmesh=False, mat_import=False, anim_import=False)
    meshes = [o for o in scene.objects if o.type == "MESH" and not o.hide_render and len(o.data.polygons)]
    if not meshes:
        print("PREVIEW_NO_MESH", job["key"])
        return
    materials = {}
    for obj in meshes:
        name = node_material(job["nodes"], obj.name)
        if name not in materials:
            materials[name] = make_material(f"{job['key']}:{name}", job["textures"].get(name))
        obj.data.materials.clear()
        obj.data.materials.append(materials[name])
    points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
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
