"""Which parts get pictures: read the same lists the Appearance Editor offers players."""
import re
from dataclasses import dataclass, field
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
DEFINITIONS = REPO / "SWLOR.Game.Server/Feature/AppearanceDefinition"
APPEARANCE_ENUM = REPO / "SWLOR.NWN.API/NWScript/Enum/AppearanceType.cs"
BASE_ITEM_ENUM = REPO / "SWLOR.NWN.API/NWScript/Enum/Item/BaseItem.cs"
EDITOR_VIEW_MODEL = REPO / "SWLOR.Game.Server/Feature/GuiDefinition/ViewModel/AppearanceEditorViewModel.cs"

# Armor catalog list name -> model stems (right side first).
ARMOR_STEMS = {
    "Neck": ["neck"], "Torso": ["chest"], "Belt": ["belt"], "Pelvis": ["pelvis"], "Robe": ["robe"],
    "Shoulder": ["shor", "shol"], "Bicep": ["bicepr", "bicepl"], "Forearm": ["forer", "forel"],
    "Hand": ["handr", "handl"], "Thigh": ["legr", "legl"], "Shin": ["shinr", "shinl"], "Foot": ["footr", "footl"],
}


CLOAK_ATLAS_PREFIX = "cloaktint#"
BODY_STEMS = {
    "Torsos": "chest", "Pelvis": "pelvis",
    "RightBicep": "bicepr", "LeftBicep": "bicepl",
    "RightForearm": "forer", "LeftForearm": "forel",
    "RightHand": "handr", "LeftHand": "handl",
    "RightThigh": "legr", "LeftThigh": "legl",
    "RightShin": "shinr", "LeftShin": "shinl",
    "RightFoot": "footr", "LeftFoot": "footl",
}


@dataclass
class Job:
    key: str                       # picture key; resref = "a" + key + view
    model: str                     # model to render
    materials: dict = field(default_factory=dict)  # mesh material name -> replacement material (cloak textures)
    weapon: str = ""               # "weapon" or "shield": a lone part, framed to fill the picture


def int_lists(path: Path) -> dict:
    text = path.read_text(encoding="utf-8")
    return {m[1]: [int(v) for v in m[2].split(",") if v.strip()]
            for m in re.finditer(r"public (?:override|virtual) int\[\] (\w+) \{ get; \} = \{([^}]*)\}", text)}


def racial_lists(path: Path) -> dict:
    """Body choices include inherited defaults, with the species' overrides taking precedence."""
    text = path.read_text(encoding="utf-8")
    parent = re.search(r"class\s+\w+\s*:\s*(\w+)", text)
    base = path.with_name(f"{parent[1]}.cs") if parent else None
    lists = racial_lists(base) if base and base.is_file() else {}
    lists.update(int_lists(path))
    return lists


def weapon_definition(path: Path):
    """(IsSimple, {list name: part ids}) of a weapon appearance definition; comments are ignored."""
    text = re.sub(r"//[^\n]*", "", path.read_text(encoding="utf-8"))
    simple = re.search(r"IsSimple\s*=>\s*(true|false)", text)
    if not simple:
        raise ValueError(f"No IsSimple in {path}")
    lists = {m[1]: [int(v) for v in m[2].split(",") if v.strip()]
             for m in re.finditer(r"override int\[\] (\w+) \{ get; \} =\s*\{([^}]*)\}", text)}
    return simple[1] == "true", lists


def weapon_definitions() -> dict:
    """BaseItem name -> definition class name, as the Appearance Editor registers them."""
    text = EDITOR_VIEW_MODEL.read_text(encoding="utf-8")
    body = re.search(r"void LoadWeaponAppearances\(\)\s*\{(.*?)\n\s*\}", text, re.S)[1]
    variables = dict(re.findall(r"var (\w+) = new (\w+)\(\);", body))
    result = {}
    for base_item, value in re.findall(r"_weaponAppearances\[BaseItem\.(\w+)\] = ([^;]+);", body):
        created = re.fullmatch(r"new (\w+)\(\)", value.strip())
        result[base_item] = created[1] if created else variables[value.strip()]
    return result


def base_item_ids() -> dict:
    text = BASE_ITEM_ENUM.read_text(encoding="utf-8")
    return {m[1]: int(m[2]) for m in re.finditer(r"^\s*(\w+)\s*=\s*(-?\d+)", text, re.M)}


def item_classes(haks_2da: Path) -> dict:
    """baseitems.2da row id -> ItemClass (lowercase), the prefix of the item's model resrefs."""
    return {int(r["_id"]): r["ItemClass"].lower() for r in two_da(haks_2da / "baseitems.2da")
            if r.get("ItemClass", "****") != "****"}


SHIELD_CLASS_PREFIX = "ash"  # shield ItemClasses (AShLw); their painted face looks toward -X
WEAPON_PART_LETTERS = {"TopParts": "t", "MiddleParts": "m", "BottomParts": "b"}


def composite_weapon_model(item_class: str, letter: str, part_id: int) -> str:
    """Model of a composite weapon part (mirrors AppearancePreview.WeaponPartKey).

    The editor's part id is color * 100 + model; the engine's model resref ends in model * 10 + color,
    so part 101 of a longsword top is wswls_t_011. Returns "" for an id without a model or colour.
    """
    color, model = divmod(part_id, 100)
    if model <= 0 or color <= 0:
        return ""
    return f"{item_class}_{letter}_{model * 10 + color:03d}"


def simple_weapon_model(item_class: str, part_id: int) -> str:
    """Model of a one-part item such as a shield (mirrors AppearancePreview.SimpleWeaponKey)."""
    return f"{item_class}_{part_id:03d}" if part_id > 0 else ""


def weapon_models(haks_2da: Path) -> list:
    """(BaseItem name, model resref) for every part the editor offers for weapons and shields."""
    ids, classes = base_item_ids(), item_classes(haks_2da)
    result = []
    for base_item, definition in weapon_definitions().items():
        item_class = classes[ids[base_item]]
        simple, lists = weapon_definition(DEFINITIONS / "ItemAppearance" / f"{definition}.cs")
        if simple:
            result += [(base_item, simple_weapon_model(item_class, p)) for p in lists.get("SimpleParts", [])]
        else:
            for list_name, letter in WEAPON_PART_LETTERS.items():
                result += [(base_item, composite_weapon_model(item_class, letter, p)) for p in lists.get(list_name, [])]
    return [(base_item, model) for base_item, model in result if model]


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

    def add(key, model, materials=None, weapon=""):
        if key not in seen and exists(model + ".mdl"):
            seen.add(key)
            jobs.append(Job(key, model, materials or {}, weapon))

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
        choices = racial_lists(path)
        for gender, list_name in (("m", "MaleHeads"), ("f", "FemaleHeads")):
            for head in choices.get(list_name, []):
                model = f"p{gender}{race}0_head{head:03d}"
                add(model, model)
            for name, stem in BODY_STEMS.items():
                for part in choices.get(name, []):
                    if part > 0:
                        model = f"p{gender}{race}0_{stem}{part:03d}"
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

    # Weapon and shield parts: the picture key is the part's model resref.
    for _, model in weapon_models(haks_2da):
        add(model, model, weapon="shield" if model.startswith(SHIELD_CLASS_PREFIX) else "weapon")
    return jobs
