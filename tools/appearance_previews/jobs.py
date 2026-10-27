"""Which parts get pictures: read the same lists the Appearance Editor offers players."""
import re
from dataclasses import dataclass, field
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
DEFINITIONS = REPO / "SWLOR.Game.Server/Feature/AppearanceDefinition"
APPEARANCE_ENUM = REPO / "SWLOR.NWN.API/NWScript/Enum/AppearanceType.cs"

# Armor catalog list name -> model stems (right side first).
ARMOR_STEMS = {
    "Neck": ["neck"], "Torso": ["chest"], "Belt": ["belt"], "Pelvis": ["pelvis"], "Robe": ["robe"],
    "Shoulder": ["shor", "shol"], "Bicep": ["bicepr", "bicepl"], "Forearm": ["forer", "forel"],
    "Hand": ["handr", "handl"], "Thigh": ["legr", "legl"], "Shin": ["shinr", "shinl"], "Foot": ["footr", "footl"],
}


CLOAK_ATLAS_PREFIX = "cloaktint#"


@dataclass
class Job:
    key: str                       # picture key; resref = "a" + key + view
    model: str                     # model to render
    materials: dict = field(default_factory=dict)  # mesh material name -> replacement material (cloak textures)


def int_lists(path: Path) -> dict:
    text = path.read_text(encoding="utf-8")
    return {m[1]: [int(v) for v in m[2].split(",") if v.strip()]
            for m in re.finditer(r"public override int\[\] (\w+) \{ get; \} = \{([^}]*)\}", text)}


def appearance_ids() -> dict:
    text = APPEARANCE_ENUM.read_text(encoding="utf-8")
    return {m[1]: int(m[2]) for m in re.finditer(r"^\s*(\w+)\s*=\s*(\d+)", text, re.M)}


def two_da(path: Path) -> list:
    lines = path.read_text(encoding="latin-1").splitlines()
    header = lines[2].split()
    rows = []
    for line in lines[3:]:
        cells = re.findall(r'"[^"]*"|\S+', line)
        if cells and cells[0].isdigit():
            rows.append(dict(zip(["_id"] + header, cells)))
    return rows


def species_bodies(haks_2da: Path) -> dict:
    """Species definition name -> body race letter, from appearance.2da RACE."""
    ids = appearance_ids()
    races = {int(r["_id"]): r.get("RACE", "****") for r in two_da(haks_2da / "appearance.2da")}
    result = {}
    for path in sorted((DEFINITIONS / "RacialAppearance").glob("*RacialAppearance*.cs")):
        name = path.stem.replace("RacialAppearanceDefinition", "").replace("RacialAppearance", "")
        if name in ids and races.get(ids[name], "****") != "****":
            result[name] = races[ids[name]].lower()
    return result


def build(haks_2da: Path, exists) -> list:
    """All jobs whose model exists (exists(resname) -> bool)."""
    jobs, seen = [], set()

    def add(key, model, materials=None):
        if key not in seen and exists(model + ".mdl"):
            seen.add(key)
            jobs.append(Job(key, model, materials or {}))

    general = int_lists(DEFINITIONS / "ItemAppearance/GeneralArmorAppearanceDefinition.cs")
    droid = int_lists(DEFINITIONS / "ItemAppearance/DroidArmorAppearanceDefinition.cs")
    bodies = sorted(set(species_bodies(haks_2da).values()))
    for race in bodies:
        catalog = droid if race == "z" else general
        for gender in "fm":
            body = f"p{gender}{race}0"
            for name, stems in ARMOR_STEMS.items():
                for stem in stems:
                    for part in catalog.get(name, []):
                        if part > 0:
                            model = f"{body}_{stem}{part:03d}"
                            add(model, model)

    for species, race in species_bodies(haks_2da).items():
        path = next((DEFINITIONS / "RacialAppearance").glob(f"{species}RacialAppearance*.cs"))
        heads = int_lists(path)
        for gender, list_name in (("m", "MaleHeads"), ("f", "FemaleHeads")):
            for head in heads.get(list_name, []):
                model = f"p{gender}{race}0_head{head:03d}"
                add(model, model)

    for helmet in general.get("Helmet", []):
        model = f"helm_{helmet:03d}"
        add(model, model)

    cloak_rows = {int(r["_id"]): r for r in two_da(haks_2da / "cloakmodel.2da")}
    cloak_tints = {int(r["_id"]): r for r in two_da(haks_2da / "cloaktint.2da")}
    for race in bodies:
        for gender in "fm":
            body = f"p{gender}{race}0"
            for row in general.get("Cloak", []):
                info = cloak_rows.get(row)
                if not info or not info["MODEL"].isdigit():
                    continue
                texture = int(info["TEXTURE"]) if info["TEXTURE"].isdigit() and int(info["TEXTURE"]) > 0 else row
                tint = cloak_tints.get(texture, {})
                if not tint.get("INDEX", "").isdigit():
                    continue
                # Cloaks share one material; fs_cloaktint picks tile INDEX of the cloak_tints atlases.
                model = f"{body}_cloak_{int(info['MODEL']):03d}"
                add(f"{body}_clk{row:03d}", model, {"*": f"{CLOAK_ATLAS_PREFIX}{int(tint['INDEX'])}"})
    return jobs
