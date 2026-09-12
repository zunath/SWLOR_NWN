"""Validate generated arena resources, make their entry blueprints, and export review/import files."""
import argparse
import copy
import json
import math
import pathlib
import subprocess

from PIL import Image, ImageDraw, ImageFont

TOOL_ROOT = pathlib.Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("output", type=pathlib.Path, nargs="?", help="Preview directory produced by the generator")
parser.add_argument("--repository", type=pathlib.Path, default=TOOL_ROOT, help="Repository passed to the generator")
args = parser.parse_args()
ROOT = args.repository.resolve()
OUTPUT = args.output.resolve() if args.output else ROOT / "artifacts/boss-arenas"
MODULE = ROOT / "Module"
GFF = TOOL_ROOT / "tools/SWLOR.CLI/nwn_gff.exe"
ERF = TOOL_ROOT / "tools/SWLOR.CLI/nwn_erf.exe"


def read(path):
    return json.loads(path.read_text(encoding="cp1252"))


def write(path, data):
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="cp1252", newline="\r\n")


def val(obj, key):
    return obj[key]["value"]


def variables(obj):
    return {val(v, "Name"): val(v, "Value") for v in obj.get("VarTable", {}).get("value", [])}


def normalize(data):
    if isinstance(data, dict):
        return {k: normalize(v) for k, v in data.items() if not k.startswith("__")}
    if isinstance(data, list):
        return [normalize(v) for v in data]
    if isinstance(data, float):
        return round(data, 3)
    return data


def floats_are_typed(data):
    if isinstance(data, dict):
        if data.get("type") == "float":
            assert isinstance(data["value"], float), f"Integer token for GFF float: {data}"
            assert math.isfinite(data["value"])
        for child in data.values():
            floats_are_typed(child)
    elif isinstance(data, list):
        for child in data:
            floats_are_typed(child)


manifest = json.loads((OUTPUT / "manifest.json").read_text())
assert len(manifest) == 8
ifo = read(MODULE / "ifo/module.ifo.json")
registered = [val(a, "Area_Name") for a in val(ifo, "Mod_Area_list")]
palette_path = MODULE / "itp/waypointpalcus.itp.json"
palette = read(palette_path)
palette_rows = val(val(val(palette, "MAIN")[0], "LIST")[0], "LIST")
waypoint_template = read(MODULE / "utw/wp_invinc_ms.utw.json")
binary = OUTPUT / "resources"
binary.mkdir(parents=True, exist_ok=True)
font_path = pathlib.Path("C:/Windows/Fonts/segoeui.ttf")
font = ImageFont.truetype(str(font_path), 18)
small = ImageFont.truetype(str(font_path), 14)
overview = Image.new("RGB", (1120, 4 * 640 + 75), "#111820")
draw = ImageDraw.Draw(overview)
draw.text((24, 16), "CAPSTONE BOSS ARENAS", font=ImageFont.truetype(str(font_path), 26), fill="#f2f4f7")
draw.text((24, 48), "Blue: entrance / recovery    Orange: master activator    Red: boss spawn", font=small, fill="#b9c7d7")
resources = []
validation = []

for idx, arena in enumerate(manifest):
    resref = arena["Resref"]
    assert len(resref) <= 16 and registered.count(resref) == 1
    are = read(MODULE / f"are/{resref}.are.json")
    git = read(MODULE / f"git/{resref}.git.json")
    gic = read(MODULE / f"gic/{resref}.gic.json")
    assert val(are, "Tag") == resref and val(are, "Name")["0"] == arena["Name"]
    assert val(are, "Tileset") == arena["Tileset"]
    assert val(are, "Width") == val(are, "Height") == 10
    assert len(val(are, "Tile_List")) == 100
    for list_name in ("Creature List", "Door List", "Encounter List", "List", "Placeable List", "SoundList", "StoreList", "TriggerList", "WaypointList"):
        assert len(val(git, list_name)) == len(val(gic, list_name)), (resref, list_name)
    assert not val(git, "Creature List") and not val(git, "Encounter List")
    assert "CREATURE_SPAWN_TABLE_ID" not in variables(git)
    masters = [p for p in val(git, "Placeable List") if p.get("OnUsed", {}).get("value") == "quest_enc"]
    assert len(masters) == 3
    tags = [val(p, "Tag") for p in val(git, "WaypointList")]
    assert len(tags) == len(set(tags)) == 5
    assert arena["EntryTag"] in tags and "STUCK_WAYPOINT" in tags
    for line in arena["Lines"]:
        activator = next(p for p in masters if val(p, "Tag") == line["Code"] + "_ms_call")
        local = variables(activator)
        assert local["QUEST_ID"] == line["Stem"] + "_mastery"
        assert local["QUEST_STATE"] == 1
        assert local["QUEST_ENCOUNTER_ID"] == line["Stem"] + "_mastery_master"
        assert local["QUEST_ENCOUNTER_RESREF"] == "cp_" + line["Code"] + "_ms"
        assert local["QUEST_ENCOUNTER_WAYPOINT"] == "CAPSTONE_" + line["Code"].upper() + "_MS_SPAWN"
        assert local["QUEST_ENCOUNTER_WAYPOINT"] in tags
        assert local["QUEST_ENCOUNTER_COOLDOWN_MINUTES"] == 60
        assert local["QUEST_ENCOUNTER_IDLE_MINUTES"] == 10
        assert local["VISIBILITY_HIDDEN_DEFAULT"] == 1
        assert local["VISIBILITY_OBJECT_ID"] == val(activator, "Tag")
        assert (MODULE / f"utc/{local['QUEST_ENCOUNTER_RESREF']}.utc.json").is_file()
    for decoration in val(git, "Placeable List"):
        if decoration in masters:
            continue
        assert val(decoration, "Useable") == 0 and val(decoration, "Static") == 1
        assert not val(decoration, "VarTable") and not val(decoration, "ItemList")

    # Destination blueprint appears in the existing standard waypoint palette category.
    entry_name = "sithrit" if resref == "pw_sc_sithritual" else resref.removeprefix("pw_sc_")
    entry_resref = "wp_" + entry_name + "_ent"
    assert len(entry_resref) <= 16
    waypoint = copy.deepcopy(waypoint_template)
    waypoint["Tag"]["value"] = arena["EntryTag"]
    waypoint["TemplateResRef"]["value"] = entry_resref
    waypoint["LocalizedName"]["value"]["0"] = arena["Name"] + " - Entrance"
    write(MODULE / f"utw/{entry_resref}.utw.json", waypoint)
    entry_instance = next(p for p in val(git, "WaypointList") if val(p, "Tag") == arena["EntryTag"])
    entry_instance["TemplateResRef"]["value"] = entry_resref
    write(MODULE / f"git/{resref}.git.json", git)
    palette_entry = {"__struct_id": 0, "NAME": {"type": "cexostring", "value": arena["Name"] + " - Entrance"},
                     "RESREF": {"type": "resref", "value": entry_resref}}
    existing = next((p for p in palette_rows if p.get("RESREF", {}).get("value") == entry_resref), None)
    if existing is None:
        palette_rows.append(palette_entry)
    else:
        existing.update(palette_entry)

    for kind, name in (("are", resref), ("git", resref), ("gic", resref), ("utw", entry_resref)):
        source = MODULE / f"{kind}/{name}.{kind}.json"
        data = read(source)
        floats_are_typed(data)
        destination = binary / f"{name}.{kind}"
        subprocess.run([str(GFF), "-i", str(source), "-o", str(destination), "-k", "gff"], check=True, capture_output=True)
        roundtrip = subprocess.run([str(GFF), "-i", str(destination), "-k", "json"], check=True, capture_output=True)
        restored = json.loads(roundtrip.stdout.decode("cp1252"))
        assert normalize(data) == normalize(restored), f"Native GFF round-trip changed {source.name}"
        resources.append(destination)

    preview = Image.frombytes("RGBA", (arena["PreviewWidth"], arena["PreviewHeight"]), (OUTPUT / f"{resref}.rgba").read_bytes()).convert("RGB")
    overlay = ImageDraw.Draw(preview)
    scale = arena["PreviewWidth"] / 100
    def marker(x, y, fill, radius):
        cx, cy = x * scale, arena["PreviewHeight"] - y * scale
        overlay.ellipse((cx-radius, cy-radius, cx+radius, cy+radius), fill=fill, outline="white", width=1)
    for p in val(git, "WaypointList"):
        is_boss = val(p, "Tag").endswith("_MS_SPAWN")
        marker(val(p, "XPosition"), val(p, "YPosition"), "#ef5d62" if is_boss else "#55bfff", 6)
    for p in masters:
        marker(val(p, "X"), val(p, "Y"), "#ffbc55", 5)
    preview.save(OUTPUT / f"{resref}.png")
    px, py = (idx % 2) * 560, (idx // 2) * 640 + 75
    overview.paste(preview.resize((520, 520)), (px + 20, py + 52))
    draw.text((px+20, py+5), arena["Name"], font=font, fill="#f2f4f7")
    draw.text((px+20, py+30), f"{resref}  |  {arena['Tileset']}  |  10 x 10 tiles", font=small, fill="#b9c7d7")
    draw.text((px+20, py+580), "3 quest-gated masters; dungeon connection pending", font=small, fill="#b9c7d7")
    if arena["MissingTileGraphics"]:
        draw.text((px+20, py+603), "Schematic preview: tileset provides no 2D map artwork", font=small, fill="#ffbc55")
    validation.append({"area": resref, "masters": 3, "waypoints": 5,
                       "decorations": len(val(git, "Placeable List"))-3, "gff_roundtrip": "PASS",
                       "encounter_setup": "PASS", "area_registration": "PASS", "gic_alignment": "PASS"})

palette_rows.sort(key=lambda p: p.get("NAME", {}).get("value", "").casefold())
write(palette_path, palette)
overview.save(OUTPUT / "boss-arenas-overview.png")
archive = OUTPUT / "capstone-boss-arenas.erf"
subprocess.run([str(ERF), "-f", str(archive), "-c", *map(str, resources)], check=True, capture_output=True)
listing = subprocess.run([str(ERF), "-f", str(archive), "-t"], check=True, capture_output=True).stdout.decode("cp1252")
(OUTPUT / "erf-contents.txt").write_text(listing)
for resource in resources:
    assert resource.name in listing
(OUTPUT / "validation.json").write_text(json.dumps(validation, indent=2) + "\n")
print(f"PASS: {len(manifest)} arenas, 24 masters, {len(resources)} native resources round-tripped and packaged.")
print(archive)
