"""Run with Blender 4.0: blender -b --factory-startup --python-exit-code 1
--python tools/ImportBlasterModel.py -- --manifest PATH --addon-root PATH --output PATH.

Converts static SWTOR GR2 pistol meshes to NWN ASCII MDL and NWN:EE materials.
The output is staged; copying the reviewed resources into sw_weapon is separate.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
import sys
from pathlib import Path


def validate_manifest(config, weapon="pistol"):
    base_item = {"pistol": 11, "rifle": 7}[weapon]
    if config.get("schema_version") != 1 or config.get("base_item") != base_item:
        raise ValueError(f"Expected schema_version 1 and {weapon} base_item {base_item}")
    slot = config.get("middle_slot")
    if type(slot) is not int or not 1 <= slot <= 255:
        raise ValueError("middle_slot must be a native pistol part ID from 1 to 255")
    if not re.fullmatch(r"[a-z][a-z0-9_]{0,13}", config.get("texture", "")):
        raise ValueError("texture must be lowercase and leave room for a two-character map suffix")
    if not isinstance(config.get("scale"), (int, float)) or not math.isfinite(config["scale"]) or config["scale"] <= 0:
        raise ValueError("scale must be finite and positive")
    for key in ("rotation_degrees", "translation"):
        values = config.get(key, [])
        if len(values) != 3 or not all(isinstance(v, (float, int)) and math.isfinite(v) for v in values):
            raise ValueError(f"{key} must have three finite numbers")
    for key in ("source_muzzle_axis", "source_grip_axis"):
        values = config.get("attachment", {}).get(key, [])
        if (len(values) != 3 or not all(isinstance(v, (float, int)) and math.isfinite(v) for v in values)
                or sum(v*v for v in values) < 1e-12):
            raise ValueError(f"attachment.{key} must be a finite nonzero direction")
    if config.get("texture_size") not in (256, 512, 1024, 2048):
        raise ValueError("texture_size must be 256, 512, 1024 or 2048")
    if config.get("material_mode") != "opaque":
        raise ValueError("Only opaque rigid weapons are supported; transparent/emissive assets need material review")
    for key in ("model", "diffuse", "normal", "specular"):
        if key not in config.get("sources", {}):
            raise ValueError(f"Missing source: {key}")
        if not re.fullmatch(r"[0-9a-f]{64}", config.get("sha256", {}).get(key, "")):
            raise ValueError(f"Missing SHA-256 for {key}")


def validate_attachment(rotation, config, weapon="pistol"):
    # NWN pistols point down -Z in attachment space; their handles extend -Y.
    # Verify authored landmarks rather than judging an upright inventory preview.
    from mathutils import Vector
    attachment = config["attachment"]
    axes = ((0, 0, -1), (0, -1, 0)) if weapon == "pistol" else ((1, 0, 0), (0, 0, -1))
    for key, expected in zip(("source_muzzle_axis", "source_grip_axis"), axes):
        actual = (rotation.to_3x3() @ Vector(attachment[key])).normalized()
        if actual.dot(Vector(expected)) < .99:
            raise ValueError(f"{weapon} attachment {key} does not match NWN axes: {tuple(actual)}")


def unpack_normal(pixels, np):
    """SWTOR RotationMap: X = alpha, Y = 1-green, Z reconstructed.

    Matches SWTOR-Slicers' NormalAndAlphaFromSwizzledTexture node group.
    The source red/blue opacity/emission channels are not normal components.
    """
    x = pixels[..., 3] * 2 - 1
    y = (1 - pixels[..., 1]) * 2 - 1
    z = np.sqrt(np.maximum(0, 1 - x*x - y*y))
    result = np.stack((x, y, z), axis=-1)
    result /= np.maximum(np.linalg.norm(result, axis=-1, keepdims=True), 1e-8)
    return result * .5 + .5


def write_mdl(path, model, texture, meshes):
    lines = ["#MAXMODEL ASCII", f"newmodel {model}", f"setsupermodel {model} NULL",
             "classification CHARACTER", "setanimationscale 1", f"beginmodelgeom {model}",
             f"node dummy {model}", "  parent NULL", "endnode"]
    for index, mesh in enumerate(meshes):
        verts, uvs, faces = mesh[:3]
        normals = mesh[3] if len(mesh) == 4 else None
        if not verts or not faces or max(len(verts), len(faces)) > 65535:
            raise ValueError("Empty mesh or NWN vertex/face limit exceeded")
        if len(uvs) != len(verts):
            raise ValueError("Every exported vertex must have a UV coordinate")
        if any(not math.isfinite(n) for v in verts for n in v):
            raise ValueError("Non-finite vertex")
        if any(not math.isfinite(n) for uv in uvs for n in uv):
            raise ValueError("Non-finite UV")
        if any(min(f) < 0 or max(f) >= len(verts) for f in faces):
            raise ValueError("Invalid face index")
        if normals is not None and (len(normals) != len(verts) or any(
                len(n) != 3 or not all(math.isfinite(c) for c in n)
                or abs(sum(c*c for c in n) - 1) > .001 for n in normals)):
            raise ValueError("Every exported vertex must have a finite unit normal")
        lines += [f"node trimesh blaster{index}", f"  parent {model}", "  position 0 0 0",
                  "  orientation 0 0 1 0", "  ambient 1 1 1", "  diffuse 1 1 1",
                  "  specular 0 0 0", "  shininess 20", "  shadow 1", "  render 1",
                  "  tilefade 0", f"  bitmap {texture}", f"  materialname {texture}",
                  "  renderhint NormalAndSpecMapped", f"  verts {len(verts)}"]
        lines += ["    " + " ".join(f"{n:.9g}" for n in v) for v in verts]
        if normals is not None:
            lines += [f"  normals {len(normals)}"]
            lines += ["    " + " ".join(f"{n:.9g}" for n in v) for v in normals]
        lines += [f"  tverts {len(uvs)}"]
        lines += [f"    {u:.9g} {v:.9g} 0" for u, v in uvs]
        lines += [f"  faces {len(faces)}"]
        lines += [f"    {a} {b} {c} 1 {a} {b} {c} 0" for a, b, c in faces]
        lines += ["endnode"]
    lines += [f"endmodelgeom {model}", f"donemodel {model}"]
    path.write_text("\n".join(lines) + "\n", encoding="ascii")


def export_mesh(mesh, transform):
    """Retain transformed authored corner normals when splitting geometry at UV seams."""
    from mathutils import Vector
    mesh.calc_loop_triangles()
    mesh.calc_normals_split()
    # Split on source vertex, UV and normal to retain authored seams/hard edges.
    indices, verts, uvs, faces, normals = {}, [], [], [], []
    normal_transform = transform.to_3x3().inverted().transposed()
    for tri in mesh.loop_triangles:
        face = []
        for li in tri.loops:
            loop = mesh.loops[li]
            uv = tuple(mesh.uv_layers.active.data[li].uv)
            normal = tuple(mesh.corner_normals[li].vector)
            key = (loop.vertex_index, uv, normal)
            if key not in indices:
                indices[key] = len(verts)
                verts.append(tuple(transform @ mesh.vertices[loop.vertex_index].co))
                uvs.append(uv)
                normals.append(tuple((normal_transform @ Vector(normal)).normalized()))
            face.append(indices[key])
        faces.append(tuple(face))
    return verts, uvs, faces, normals


def main(weapon="pistol"):
    import bpy
    import addon_utils
    import numpy as np
    from mathutils import Euler, Matrix, Quaternion, Vector

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--addon-root", type=Path, required=True, help="Directory containing io_scene_gr2")
    parser.add_argument("--output", type=Path, required=True, help="Empty staging directory")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    manifest = args.manifest.resolve()
    config = json.loads(manifest.read_text(encoding="utf-8-sig"))
    validate_manifest(config, weapon)
    paths = {key: (manifest.parent / value).resolve() for key, value in config["sources"].items()}
    for key, path in paths.items():
        if hashlib.sha256(path.read_bytes()).hexdigest() != config["sha256"][key]:
            raise ValueError(f"Source hash mismatch: {path}")
    output = args.output.resolve()
    if output.exists() and any(output.iterdir()):
        raise ValueError("Use an empty output directory; existing review artifacts are never overwritten")
    if not (args.addon_root / "io_scene_gr2" / "__init__.py").is_file():
        raise ValueError("addon-root must contain io_scene_gr2/__init__.py")
    sys.path.insert(0, str(args.addon_root.resolve()))
    addon_utils.enable("io_scene_gr2", default_set=True)
    import io_scene_gr2
    if tuple(io_scene_gr2.bl_info["version"]) != (4, 2, 1):
        raise ValueError("Use the tested SWTOR-Slicers importer release 4.2.1")
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    result = bpy.ops.import_mesh.gr2(filepath=str(paths["model"]), import_collision=False,
                                     scale_object=False, apply_axis_conversion=False)
    if result != {"FINISHED"}:
        raise ValueError("GR2 import failed")
    objects = [ob for ob in bpy.context.scene.objects if ob.type == "MESH"]
    if not objects or any(ob.type == "ARMATURE" for ob in bpy.context.scene.objects):
        raise ValueError("Expected rigid mesh geometry without a skeleton")
    rotation = Euler(tuple(math.radians(n) for n in config["rotation_degrees"])).to_matrix().to_4x4()
    validate_attachment(rotation, config, weapon)
    transform = Matrix.Translation(Vector(config["translation"])) @ rotation @ Matrix.Scale(config["scale"], 4)
    meshes = []
    for ob in objects:
        if len(ob.data.materials) != 1 or ob.modifiers or not ob.data.uv_layers:
            raise ValueError("Only static single-material meshes with UVs are supported")
        if weapon == "rifle" and ob.data.materials[0].name != config.get("source_material"):
            raise ValueError("Rifle source material differs from the inspected manifest assignment")
        # The importer assigns a Blender-only +90 X object rotation. Manifest axes refer
        # to raw SWTOR vertex coordinates, not the importer's viewport conversion.
        ob.matrix_world = transform
        meshes.append(export_mesh(ob.data, transform))
    output.mkdir(parents=True, exist_ok=True)
    resources = output / "resources"
    resources.mkdir()
    item_class = "wbwsh" if weapon == "pistol" else "wbwxl"
    model = f"{item_class}_m_{config['middle_slot']:03d}"
    texture = config["texture"]
    write_mdl(resources / f"{model}.mdl", model, texture, meshes)

    def read_pixels(path, size=None):
        image = bpy.data.images.load(str(path), check_existing=False)
        image.colorspace_settings.name = "Non-Color"
        if image.size[0] != image.size[1] or not image.size[0]:
            raise ValueError(f"Expected a square texture; review resizing for {path}")
        if size and max(image.size) > size:
            image.scale(size, size)
        pixels = np.empty(image.size[0] * image.size[1] * 4, dtype=np.float32)
        image.pixels.foreach_get(pixels)
        return pixels.reshape((image.size[1], image.size[0], 4)).copy()

    def save_texture(name, rgb):
        h, w = rgb.shape[:2]
        rgba = np.ones((h, w, 4), dtype=np.float32)
        rgba[..., :3] = rgb
        image = bpy.data.images.new(name, width=w, height=h, alpha=False)
        image.colorspace_settings.name = "Non-Color"
        image.pixels.foreach_set(rgba.ravel())
        image.filepath_raw = str(resources / (name + ".tga"))
        image.file_format = "TARGA_RAW"
        image.save()
        return image

    diffuse = save_texture(texture, read_pixels(paths["diffuse"], config["texture_size"])[..., :3])
    packed_normal = read_pixels(paths["normal"], config["texture_size"])
    normal = save_texture(texture + "_n", unpack_normal(packed_normal, np))
    # Preserve colored specular RGB; SWTOR gloss alpha is not NWN roughness.
    specular = save_texture(texture + "_s", read_pixels(paths["specular"], config["texture_size"])[..., :3])
    (resources / (texture + ".mtr")).write_text(
        f"renderhint NormalAndSpecMapped\ntexture0 {texture}\ntexture1 {texture}_n\ntexture2 {texture}_s\n",
        encoding="ascii")
    emission = None
    if config.get("preserve_emission") and np.max(packed_normal[..., 2]) > 0:
        # Source blue is emission intensity, independent of the packed normal XY.
        rgb = read_pixels(paths["diffuse"], packed_normal.shape[0])[..., :3]
        linear = np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)
        linear *= packed_normal[..., 2:3]
        encoded = np.where(linear <= .0031308, linear * 12.92, 1.055 * linear ** (1 / 2.4) - .055)
        emission = save_texture(texture + "_e", encoded)
        with (resources / (texture + ".mtr")).open("a", encoding="ascii") as stream:
            stream.write(f"texture5 {texture}_e\n")

    material = bpy.data.materials.new("NWN blaster preview")
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = nodes.get("Principled BSDF")
    shader.inputs["Roughness"].default_value = .48
    # Reload the saved diffuse with the correct color space, just as the game does.
    color = nodes.new("ShaderNodeTexImage")
    color.image = bpy.data.images.load(diffuse.filepath_raw, check_existing=False)
    color.image.colorspace_settings.name = "sRGB"
    links.new(color.outputs["Color"], shader.inputs["Base Color"])
    bump = nodes.new("ShaderNodeTexImage"); bump.image = normal
    normal_node = nodes.new("ShaderNodeNormalMap")
    links.new(bump.outputs["Color"], normal_node.inputs["Color"])
    links.new(normal_node.outputs["Normal"], shader.inputs["Normal"])
    spec = nodes.new("ShaderNodeTexImage"); spec.image = specular
    links.new(spec.outputs["Color"], shader.inputs["Specular IOR Level"])
    if emission:
        glow = nodes.new("ShaderNodeTexImage")
        glow.image = bpy.data.images.load(emission.filepath_raw, check_existing=False)
        glow.image.colorspace_settings.name = "sRGB"
        links.new(glow.outputs["Color"], shader.inputs["Emission Color"])
        shader.inputs["Emission Strength"].default_value = 1
    for ob in objects:
        ob.data.materials.clear()
        ob.data.materials.append(material)
    points = [Vector(v) for mesh in meshes for v in mesh[0]]
    lo = Vector(tuple(min(v[i] for v in points) for i in range(3)))
    hi = Vector(tuple(max(v[i] for v in points) for i in range(3)))
    center = (lo + hi) / 2
    span = max(hi - lo)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = 64
    scene.world.color = (.15, .15, .15)
    scene.view_settings.view_transform = "Standard"
    scene.render.film_transparent = True
    scene.render.resolution_percentage = 100
    for direction, energy in [((1, -1, 2), 150), ((1, 1, .5), 80), ((-1, 0, 1), 100)]:
        bpy.ops.object.light_add(type="AREA", location=center + Vector(direction))
        light = bpy.context.object
        light.data.energy = energy
        light.data.size = 2
        light.rotation_euler = (center - light.location).to_track_quat("-Z", "Y").to_euler()
    direction = (1, -.25, .15) if weapon == "pistol" else (.15, -1, .15)
    bpy.ops.object.camera_add(location=center + Vector(direction))
    camera = bpy.context.object
    camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = span * 1.2
    scene.camera = camera
    scene.render.resolution_x = scene.render.resolution_y = 900
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(output / "preview.png")
    bpy.ops.render.render(write_still=True)
    # Inventory composite middle layer: native 64 x 64 transparent TGA.
    scene.render.resolution_x = scene.render.resolution_y = 64
    if weapon == "rifle":
        # Rifle composite layers fill a 2 x 4 inventory footprint (64 x 128).
        # Align the barrel with image vertical, preserving the entire silhouette.
        camera.rotation_euler = ((center - camera.location).to_track_quat("-Z", "Y") @ Quaternion((0, 0, 1), -math.pi / 2)).to_euler()
        scene.render.resolution_y = 128
        camera.data.ortho_scale = max((hi.x - lo.x) * 1.2, (hi.z - lo.z) * 2.4)
    scene.render.image_settings.file_format = "TARGA_RAW"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.filepath = str(resources / f"i{model}.tga")
    bpy.ops.render.render(write_still=True)
    report = {"manifest": manifest.name, "blender": bpy.app.version_string,
              "importer": "SWTOR-Slicers 4.2.1", "model": model,
              "vertices": sum(len(mesh[0]) for mesh in meshes),
              "triangles": sum(len(mesh[2]) for mesh in meshes),
              "bounds_min": list(lo), "bounds_max": list(hi),
              "resources": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(resources.iterdir())}}
    (output / "report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(output / "review.blend"))
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
