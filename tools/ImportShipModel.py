"""Turn a generated GLB starship into a native NWN ship model.

Ship appearances are simple CHARACTER models: a root dummy, the attach points
(headconjure, impact), engine emitters and textured trimesh hull geometry. A new
Hull Design copies every non-mesh node from the ship it belongs to (the reference)
and is fitted to that ship's size, so effects, targeting and camera behave the same.

Modes (see SWLOR.Game.Server/Readmes/ShipModelImport.md):
  reference  Python   decompile a HAK ship MDL to ASCII for the manifest
  export     Blender  GLB + manifest -> ASCII MDL + TGA texture
  package    Python   compile the MDL and compress the texture to DDS
"""
import argparse
import hashlib
import json
import math
import re
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RESREF = re.compile(r"^[a-z0-9_]{1,16}$")
MESH_TYPES = {"trimesh", "skin", "danglymesh", "animmesh", "aabb"}
CRUNCH_DIFFUSE = ["-fileformat", "dds", "-unflip", "-yflip", "-dxtQuality", "uber",
                  "-dropEmptyAlpha", "-DXT1", "-gamma", "2.2"]
CRUNCH_NORMAL = ["-fileformat", "dds", "-unflip", "-yflip", "-dxtQuality", "uber", "-dropEmptyAlpha",
                 "-DXN", "-normalize", "-renormalize", "-uniformMetrics", "-gamma", "1.0"]
ADJUST_LIMITS = {"exposure": (0.3, 2.0), "contrast": (0.5, 2.0), "saturation": (0.0, 2.0)}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def validate_manifest(config):
    """Check the fit manifest before any geometry is touched."""
    require(config.get("schema_version") == 1, "schema_version must be 1")
    for key in ("source", "sha256", "model", "texture", "reference", "orientation"):
        require(key in config, f"Manifest is missing {key}")
    for key in ("model", "texture"):
        require(RESREF.match(config[key]), f"{key} must be a lowercase resref of at most 16 characters")
    require(re.fullmatch(r"[0-9a-f]{64}", config["sha256"]), "sha256 must be a lowercase hex digest")
    faces = config.get("max_faces", 4000)
    require(isinstance(faces, int) and 100 <= faces <= 20000, "max_faces must be an integer from 100 to 20000")
    size = config.get("texture_size", 1024)
    require(size in (256, 512, 1024, 2048), "texture_size must be 256, 512, 1024 or 2048")
    rotation = config["orientation"]
    require(len(rotation) == 3 and all(len(row) == 3 for row in rotation), "orientation must be a 3x3 matrix")
    require(all(isinstance(v, (int, float)) and math.isfinite(v) for row in rotation for v in row), "orientation must be finite")
    for i in range(3):
        for j in range(3):
            dot = sum(rotation[i][k] * rotation[j][k] for k in range(3))
            require(abs(dot - (1 if i == j else 0)) < 1e-6, "orientation must be orthonormal")
    require(abs(determinant(rotation) - 1) < 1e-6, "orientation must be a proper rotation (determinant 1)")
    for name, position in config.get("node_positions", {}).items():
        require(valid_position(position), f"node_positions.{name} must be [x, y, z]")
    material = config.get("material", {})
    require(isinstance(material, dict) and set(material) <= {"ambient", "diffuse"}, "material accepts only ambient and diffuse")
    for key, value in material.items():
        require(valid_position(value) and all(0 <= v <= 1 for v in value), f"material.{key} must be three values from 0 to 1")
    for key, value in config.get("texture_adjust", {}).items():
        require(key in ADJUST_LIMITS, f"texture_adjust.{key} is not one of {sorted(ADJUST_LIMITS)}")
        low, high = ADJUST_LIMITS[key]
        require(isinstance(value, (int, float)) and low <= value <= high, f"texture_adjust.{key} must be from {low} to {high}")
    require(config.get("normal_map", "none") in ("none", "opengl", "flip_green"),
            "normal_map must be none, opengl or flip_green")
    for name, copy in config.get("node_copies", {}).items():
        require(RESREF.match(name), f"node_copies name {name} must be a lowercase node name")
        require(isinstance(copy, dict) and set(copy) == {"from", "position"} and valid_position(copy["position"]),
                f"node_copies.{name} must be {{\"from\": <node>, \"position\": [x, y, z]}}")
    return {**config, "max_faces": faces, "texture_size": size}


def valid_position(position):
    return isinstance(position, list) and len(position) == 3 and all(
        isinstance(v, (int, float)) and math.isfinite(v) for v in position)


def determinant(m):
    return (m[0][0] * (m[1][1] * m[2][2] - m[1][2] * m[2][1])
            - m[0][1] * (m[1][0] * m[2][2] - m[1][2] * m[2][0])
            + m[0][2] * (m[1][0] * m[2][1] - m[1][1] * m[2][0]))


def parse_reference(text):
    """Return (model name, non-mesh nodes, mesh bounds) from an ASCII ship MDL."""
    model = re.search(r"(?im)^\s*newmodel\s+(\S+)", text)
    require(model, "Reference is not an ASCII model (no newmodel line)")
    model = model[1]
    require(not re.search(r"(?im)^\s*newanim\b", text), "Reference has animations; ship designs must be static")
    nodes = []
    low, high = [math.inf] * 3, [-math.inf] * 3
    for match in re.finditer(r"(?ims)^\s*node\s+(\S+)\s+(\S+)[^\n]*\n(.*?)^\s*endnode\b", text):
        kind, name, body = match[1].lower(), match[2], match[3]
        parent = re.search(r"(?im)^\s*parent\s+(\S+)", body)
        parent = parent[1] if parent else "NULL"
        if name.lower() == model.lower() and parent.upper() == "NULL":
            continue
        require(parent.lower() == model.lower(), f"Reference node {name} is not a direct child of the root")
        if kind in MESH_TYPES:
            position = re.search(r"(?im)^\s*position\s+(\S+)\s+(\S+)\s+(\S+)", body)
            offset = [float(v) for v in position.groups()] if position else [0.0, 0.0, 0.0]
            orientation = re.search(r"(?im)^\s*orientation\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)", body)
            require(not orientation or abs(float(orientation[4])) < 1e-6, f"Reference mesh {name} is rotated; unsupported")
            verts = re.search(r"(?im)^\s*verts\s+(\d+)\s*\n", body)
            require(verts, f"Reference mesh {name} has no verts")
            lines = body[verts.end():].splitlines()[:int(verts[1])]
            for line in lines:
                point = [float(v) for v in line.split()[:3]]
                for axis in range(3):
                    low[axis] = min(low[axis], point[axis] + offset[axis])
                    high[axis] = max(high[axis], point[axis] + offset[axis])
            continue
        require(kind in {"dummy", "emitter", "light"}, f"Unsupported reference node type {kind} ({name})")
        nodes.append({"type": kind, "name": name, "body": strip_comments(body)})
    require(math.isfinite(low[0]), "Reference has no hull geometry")
    require({"headconjure", "impact"} <= {n["name"].lower() for n in nodes}, "Reference is missing headconjure or impact")
    return model, nodes, (low, high)


def strip_comments(body):
    return "".join(line + "\n" for line in body.splitlines() if line.strip() and not line.strip().startswith("#"))


def fit_transform(low, high, ref_low, ref_high):
    """Uniform scale and offset that give the hull the reference's footprint and center."""
    span = max(high[0] - low[0], high[1] - low[1])
    ref_span = max(ref_high[0] - ref_low[0], ref_high[1] - ref_low[1])
    require(span > 0 and ref_span > 0, "Degenerate hull bounds")
    scale = ref_span / span
    offset = [(ref_low[a] + ref_high[a]) / 2 - scale * (low[a] + high[a]) / 2 for a in range(3)]
    return scale, offset


def write_mdl(path, model, texture, nodes, mesh, node_positions=None, node_copies=None, material=None,
              normal_mapped=False):
    """Write the ship: root, copied reference nodes, then one hull trimesh.

    node_copies adds a duplicate of a reference node (for example one engine
    emitter per nozzle) under a new name at a new position.
    """
    verts, uvs, faces, normals = mesh
    require(verts and faces and len(verts) <= 65535 and len(faces) <= 65535, "Empty hull or NWN vertex/face limit exceeded")
    require(len(uvs) == len(verts) == len(normals), "Every vertex needs a UV and a normal")
    require(all(math.isfinite(c) for v in verts + uvs + normals for c in v), "Non-finite hull data")
    require(all(0 <= i < len(verts) for f in faces for i in f), "Invalid face index")
    node_positions = {k.lower(): v for k, v in (node_positions or {}).items()}
    names = {n["name"].lower() for n in nodes}
    require(set(node_positions) <= names, f"node_positions names unknown nodes: {sorted(set(node_positions) - names)}")
    by_name = {n["name"].lower(): n for n in nodes}
    for name, copy in (node_copies or {}).items():
        require(name.lower() not in names, f"node_copies name {name} already exists")
        require(copy["from"].lower() in by_name, f"node_copies.{name} copies unknown node {copy['from']}")
        source = by_name[copy["from"].lower()]
        nodes = nodes + [{"type": source["type"], "name": name, "body": source["body"]}]
        names.add(name.lower())
        node_positions[name.lower()] = copy["position"]
    require("hull" not in names and model.lower() not in names, "Reference node names collide with the hull or root")
    lines = ["# Generated by tools/ImportShipModel.py", "#MAXMODEL ASCII", f"newmodel {model}",
             "  classification CHARACTER", f"  setsupermodel {model} null", "  setanimationscale 1",
             "#MAXGEOM ASCII", f"beginmodelgeom {model}", f"node dummy {model}", "  parent NULL", "endnode"]
    for node in nodes:
        body = re.sub(r"(?im)^(\s*parent\s+)\S+", lambda m: m[1] + model, node["body"], count=1)
        position = node_positions.get(node["name"].lower())
        if position is not None:
            replacement = "position " + " ".join(f"{v:.6g}" for v in position)
            body, count = re.subn(r"(?im)^(\s*)position\s+[^\n]*", lambda m: m[1] + replacement, body, count=1)
            if not count:
                body += f"  {replacement}\n"
        lines += [f"node {node['type']} {node['name']}", body.rstrip("\n"), "endnode"]
    material = material or {}

    def color(key):
        return " ".join(f"{v:.4g}" for v in material.get(key, [1, 1, 1]))

    lines += ["node trimesh hull", f"  parent {model}", "  position 0 0 0", "  orientation 0 0 1 0",
              f"  ambient {color('ambient')}", f"  diffuse {color('diffuse')}", "  specular 0 0 0", "  shininess 1",
              f"  bitmap {texture}", "  render 1", "  shadow 0", "  beaming 0", "  transparencyhint 0"]
    if normal_mapped:
        lines += [f"  materialname {texture}", "  renderhint NormalAndSpecMapped"]
    lines += [f"  verts {len(verts)}"]
    lines += ["    " + " ".join(f"{c:.7g}" for c in v) for v in verts]
    lines += [f"  normals {len(normals)}"]
    lines += ["    " + " ".join(f"{c:.7g}" for c in n) for n in normals]
    lines += [f"  tverts {len(uvs)}"]
    lines += [f"    {u:.7g} {v:.7g} 0" for u, v in uvs]
    lines += [f"  faces {len(faces)}"]
    lines += [f"    {a} {b} {c} 1 {a} {b} {c} 0" for a, b, c in faces]
    lines += ["endnode", f"endmodelgeom {model}", f"donemodel {model}"]
    Path(path).write_text("\n".join(lines) + "\n", encoding="ascii")


def empty_output(path):
    path = Path(path).resolve()
    require(ROOT not in path.parents and path != ROOT, "Output must be outside the repository")
    path.mkdir(parents=True, exist_ok=True)
    require(not any(path.iterdir()), f"Output directory must be empty: {path}")
    return path


# ---------------------------------------------------------------- Blender export

def adjust_pixels(pixels, adjust):
    """Exposure, then contrast about mid grey, then saturation, on sRGB RGBA floats."""
    import numpy as np
    rgb = np.asarray(pixels, dtype=np.float32).reshape(-1, 4)[:, :3]
    rgb = rgb * adjust.get("exposure", 1.0)
    rgb = (rgb - 0.5) * adjust.get("contrast", 1.0) + 0.5
    grey = (rgb @ np.array([0.2126, 0.7152, 0.0722], np.float32))[:, None]
    rgb = grey + (rgb - grey) * adjust.get("saturation", 1.0)
    out = np.ones((rgb.shape[0], 4), np.float32)
    out[:, :3] = np.clip(rgb, 0, 1)
    return out.ravel()


def normal_image(objects):
    images = []
    for obj in objects:
        for slot in obj.material_slots:
            principled = [n for n in slot.material.node_tree.nodes if n.type == "BSDF_PRINCIPLED"][0]
            links = principled.inputs["Normal"].links
            require(links and links[0].from_node.type == "NORMAL_MAP", f"Material {slot.material.name} has no normal map")
            color = links[0].from_node.inputs["Color"].links
            require(color and color[0].from_node.type == "TEX_IMAGE", f"Material {slot.material.name} normal map has no image")
            if color[0].from_node.image not in images:
                images.append(color[0].from_node.image)
    require(len(images) == 1, f"Expected one normal map, found {len(images)}")
    return images[0]


def base_color_image(objects):
    images = []
    for obj in objects:
        for slot in obj.material_slots:
            material = slot.material
            require(material and material.use_nodes, f"{obj.name} has a material without nodes")
            principled = [n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED"]
            require(len(principled) == 1, f"Material {material.name} needs exactly one Principled BSDF")
            links = principled[0].inputs["Base Color"].links
            require(links and links[0].from_node.type == "TEX_IMAGE", f"Material {material.name} has no base color texture")
            image = links[0].from_node.image
            if image not in images:
                images.append(image)
    require(len(images) == 1, f"Expected one base color texture, found {len(images)}")
    return images[0]


def save_texture(image, path, size, adjust=None, flip_green=False):
    """Resample a map into a square, opaque TGA (Blender rows: bottom-left origin)."""
    import bpy
    import numpy as np
    scaled = image.copy()
    scaled.scale(size, size)
    pixels = np.asarray(scaled.pixels[:], dtype=np.float32)
    if adjust:
        pixels = adjust_pixels(pixels, adjust)
    pixels = pixels.reshape(-1, 4)
    if flip_green:
        pixels[:, 1] = 1.0 - pixels[:, 1]
    pixels[:, 3] = 1.0
    pixels = pixels.ravel()
    target = bpy.data.images.new(path.stem, width=size, height=size, alpha=False)
    target.colorspace_settings.name = image.colorspace_settings.name
    target.pixels.foreach_set(pixels)
    target.filepath_raw = str(path)
    target.file_format = "TARGA_RAW"
    target.save()
    bpy.data.images.remove(target)
    bpy.data.images.remove(scaled)


def mesh_arrays(mesh):
    """Split vertices on UV and normal seams; keep face winding."""
    mesh.calc_loop_triangles()
    if hasattr(mesh, "calc_normals_split"):
        mesh.calc_normals_split()
        loop_normal = lambda index: tuple(mesh.loops[index].normal)
    else:
        loop_normal = lambda index: tuple(mesh.corner_normals[index].vector)
    uv_layer = mesh.uv_layers.active
    require(uv_layer, "Hull has no UV map")
    indices, verts, uvs, normals, faces = {}, [], [], [], []
    for tri in mesh.loop_triangles:
        face = []
        for loop_index in tri.loops:
            vertex = mesh.loops[loop_index].vertex_index
            uv = tuple(round(c, 6) for c in uv_layer.data[loop_index].uv)
            normal = loop_normal(loop_index)
            length = math.sqrt(sum(c * c for c in normal)) or 1.0
            normal = tuple(round(c / length, 5) for c in normal)
            key = (vertex, uv, normal)
            if key not in indices:
                indices[key] = len(verts)
                verts.append(tuple(mesh.vertices[vertex].co))
                uvs.append(uv)
                normals.append(normal)
            face.append(indices[key])
        faces.append(tuple(face))
    return verts, uvs, faces, normals


def blender_export(manifest_path, output):
    import bpy
    from mathutils import Matrix
    manifest_path = Path(manifest_path).resolve()
    config = validate_manifest(json.loads(manifest_path.read_text(encoding="utf8")))
    source = (manifest_path.parent / config["source"]).resolve()
    require(sha256(source) == config["sha256"], "Source GLB hash differs from the manifest")
    reference = (manifest_path.parent / config["reference"]).resolve()
    ref_model, nodes, (ref_low, ref_high) = parse_reference(reference.read_text(encoding="latin1"))
    output = empty_output(output)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    objects = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    require(objects, "Source GLB has no meshes")
    image = base_color_image(objects)
    for obj in objects:
        world = obj.matrix_world.copy()
        obj.parent = None
        obj.matrix_world = world
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    hull = bpy.context.view_layer.objects.active
    hull.data.transform(hull.matrix_world)
    hull.matrix_world = Matrix.Identity(4)
    # glTF import already converts +Y up to Blender's +Z up; the manifest turns the
    # imported model so its nose points +Y and its top points +Z, the NWN ship frame.
    hull.data.transform(Matrix(config["orientation"]).to_4x4())

    source_faces = sum(len(p.vertices) - 2 for p in hull.data.polygons)
    if source_faces > config["max_faces"]:
        modifier = hull.modifiers.new("budget", "DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = config["max_faces"] / source_faces
        modifier.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    modifier = hull.modifiers.new("triangles", "TRIANGULATE")
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    if hasattr(hull.data, "use_auto_smooth"):
        hull.data.use_auto_smooth = True
        hull.data.auto_smooth_angle = math.radians(35)
    for polygon in hull.data.polygons:
        polygon.use_smooth = True
    if hasattr(hull.data, "has_custom_normals") and hull.data.has_custom_normals:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()

    coords = [v.co for v in hull.data.vertices]
    low = [min(c[a] for c in coords) for a in range(3)]
    high = [max(c[a] for c in coords) for a in range(3)]
    scale, offset = fit_transform(low, high, ref_low, ref_high)
    hull.data.transform(Matrix.Translation(offset) @ Matrix.Scale(scale, 4))
    mesh = mesh_arrays(hull.data)
    require(len(mesh[2]) <= config["max_faces"], f"Decimation left {len(mesh[2])} faces, over the budget")

    normal_mode = config.get("normal_map", "none")
    save_texture(image, output / f"{config['texture']}.tga", config["texture_size"], config.get("texture_adjust"))
    if normal_mode != "none":
        save_texture(normal_image(objects), output / f"{config['texture']}_n.tga", config["texture_size"],
                     flip_green=normal_mode == "flip_green")
        (output / f"{config['texture']}.mtr").write_text(
            f"renderhint NormalAndSpecMapped\ntexture0 {config['texture']}\ntexture1 {config['texture']}_n\n"
            "texture2 null\n", encoding="ascii", newline="\r\n")
    write_mdl(output / f"{config['model']}.mdl", config["model"], config["texture"], nodes, mesh,
              config.get("node_positions"), config.get("node_copies"), config.get("material"), normal_mode != "none")
    report = {"schema_version": 1, "model": config["model"], "texture": config["texture"], "normal_map": normal_mode,
              "reference": ref_model, "source_sha256": config["sha256"], "source_faces": source_faces,
              "faces": len(mesh[2]), "verts": len(mesh[0]), "scale": scale, "offset": offset,
              "reference_bounds": [ref_low, ref_high], "texture_size": config["texture_size"],
              "copied_nodes": [n["name"] for n in nodes] + list(config.get("node_copies", {})),
              "files": {p.name: sha256(p) for p in sorted(output.iterdir())}}
    (output / "export_manifest.json").write_text(json.dumps(report, indent=2), encoding="utf8")
    print(json.dumps({k: report[k] for k in ("model", "faces", "verts", "scale")}))


# ---------------------------------------------------------------- Python modes

def clean_models(haks):
    tools = Path(haks) / "tools"
    require((tools / "CleanModels.py").is_file(), f"CleanModels.py not found under {tools}; pass --haks with a full SWLOR_Haks checkout")
    sys.path.insert(0, str(tools))
    import CleanModels
    return CleanModels


def reference(args):
    CleanModels = clean_models(args.haks)
    output = empty_output(args.output)
    source = Path(args.mdl).resolve()
    compiler = CleanModels.find_compiler()
    CleanModels.run(compiler, output, "decompile", [source], output, "decompile.log", [source.parent])
    (output / "decompile.log").unlink()
    print(output / source.name)


def package(args):
    CleanModels = clean_models(args.haks)
    export = Path(args.export).resolve()
    report = json.loads((export / "export_manifest.json").read_text(encoding="utf8"))
    for name, digest in report["files"].items():
        require(sha256(export / name) == digest, f"{name} changed after export")
    output = empty_output(args.output)
    runtime, staging = output / "runtime_resources", output / "staging"
    runtime.mkdir()
    staging.mkdir()
    model, texture = report["model"], report["texture"]
    compiler = CleanModels.find_compiler()
    CleanModels.run(compiler, staging, "compile", [export / f"{model}.mdl"], runtime, "compile.log", [export])
    CleanModels.validate_binary(runtime / f"{model}.mdl")
    check = staging / "decompiled"
    check.mkdir()
    CleanModels.run(compiler, staging, "decompile", [runtime / f"{model}.mdl"], check, "decompile.log", [runtime])
    text = (check / f"{model}.mdl").read_text(encoding="latin1")
    faces = sum(int(n) for n in re.findall(r"(?im)^\s*faces\s+(\d+)", text))
    require(faces == report["faces"], f"Compiled model has {faces} faces, export had {report['faces']}")
    for name in report["copied_nodes"] + ["hull"]:
        require(re.search(rf"(?im)^\s*node\s+\S+\s+{re.escape(name)}\b", text), f"Compiled model lost node {name}")
    crunch = Path(args.crunch).resolve()
    maps = [(texture, CRUNCH_DIFFUSE)]
    if report.get("normal_map", "none") != "none":
        maps.append((f"{texture}_n", CRUNCH_NORMAL))
        shutil.copyfile(export / f"{texture}.mtr", runtime / f"{texture}.mtr")
    for name, options in maps:
        result = subprocess.run([str(crunch), "-file", str(export / f"{name}.tga"), "-out",
                                 str(runtime / f"{name}.dds"), *options],
                                capture_output=True, text=True, timeout=300)
        (staging / f"crunch_{name}.log").write_text(result.stdout + result.stderr, encoding="utf8")
        require(result.returncode == 0 and (runtime / f"{name}.dds").is_file(), f"NWN Crunch failed on {name}; see staging")
    summary = {"model": model, "texture": texture, "faces": faces,
               "runtime": {p.name: sha256(p) for p in sorted(runtime.iterdir())}}
    (output / "package_report.json").write_text(json.dumps(summary, indent=2), encoding="utf8")
    if not args.keep_staging:
        shutil.rmtree(staging)
    print(json.dumps(summary))


def main(argv):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--mode", required=True, choices=("reference", "export", "package"))
    parser.add_argument("--manifest", help="export: fit manifest JSON")
    parser.add_argument("--mdl", help="reference: binary ship MDL from the HAK sources")
    parser.add_argument("--export", help="package: export output directory")
    parser.add_argument("--output", required=True, help="new empty directory outside the repository")
    parser.add_argument("--haks", default=str(ROOT / "SWLOR_Haks"), help="full SWLOR_Haks checkout (for CleanModels)")
    parser.add_argument("--crunch", help="package: nwn_crunch.exe")
    parser.add_argument("--keep-staging", action="store_true", help="package: keep compile logs and the decompiled check")
    args = parser.parse_args(argv)
    if args.mode == "export":
        if not args.manifest:
            parser.error("export needs --manifest")
        blender_export(args.manifest, args.output)
    elif args.mode == "reference":
        if not args.mdl:
            parser.error("reference needs --mdl")
        reference(args)
    else:
        if not args.export or not args.crunch:
            parser.error("package needs --export and --crunch")
        package(args)


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:])
