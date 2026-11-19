"""Generate the Astromech gear modules.

An astromech cannot wear humanoid gear. Its gear is "modules": ordinary items in the normal equipment slots that only
astromechs can equip (item property UseLimitationRacialType, subtype 170). Modules introduce no balance numbers: each one
is a stat copy of the humanoid armor-slot item of the same slot and tier, with a new name, a Chassis (skill 61) rank
requirement in place of the Armor (skill 6) rank, and the astromech-only property.

  python tools/GenerateAstromechModules.py            write everything
  python tools/GenerateAstromechModules.py --check    write nothing, exit 1 and list what differs

Writes (all derived from the source blueprints and the source Smithery recipes, so a change to a humanoid item is
picked up by rerunning, and caught by AstromechModuleContentTests):
  - Module/uti/am_<h|t>_<code>_<01..10>.uti.json, 160 blueprints; each Chassis Plating wears a paint job (PAINT_JOBS)
    and has the part models of its source cleared
  - Module/uti/am_plat_start.uti.json, the stat-less plating a new astromech starts in
  - SWLOR.Game.Server/Feature/RecipeDefinition/EngineeringRecipeDefinition/AstromechModuleRecipes.cs
    (the .CircuitParameters(...) values are written by tools/SWLOR.RecipeParameters and kept across reruns)
  - the generated RecipeType block (5071-5230) in SWLOR.Game.Server/Service/CraftService/RecipeType.cs
  - the sixteen Mk I modules in Module/utm/cz220_merchant.utm.json, next to the humanoid Mk I pieces they copy,
    and in every copy of that store placed in an area (Module/git), which is what the game opens
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import AstromechPlacedStores as placed  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
UTI = ROOT / "Module" / "uti"
SMITHERY = ROOT / "SWLOR.Game.Server" / "Feature" / "RecipeDefinition" / "SmitheryRecipeDefinition"
RECIPES_CS = ROOT / "SWLOR.Game.Server" / "Feature" / "RecipeDefinition" / "EngineeringRecipeDefinition" / "AstromechModuleRecipes.cs"
RECIPE_TYPE_CS = ROOT / "SWLOR.Game.Server" / "Service" / "CraftService" / "RecipeType.cs"
STORE = ROOT / "Module" / "utm" / "cz220_merchant.utm.json"

ASTROMECH_RACE = 170
CHASSIS_SKILL = 61
ARMOR_SKILL = 6
FIRST_RECIPE_ID = 5071
RACIAL_PROPERTY = 64
REQUIRES_SKILL_PROPERTY = 131

# Tier prefixes of the humanoid ladders, Mk I to Mk X. Hardened is the defense-first (bulwark) piece, Tuned the skirmish piece.
# The per-slot suffix follows the humanoid resrefs: the defense-first Mk II, IV, VI and VIII pieces use the short slot words.
HARDENED = ["bm", "fld_bul", "tit", "vet_bul", "qk", "prm_bul", "ar", "asc_bul", "ec", "ch"]
TUNED = ["com", "fld_skm", "val", "vet_skm", "for", "prm_skm", "sur", "asc_skm", "sup", "imm"]
SHORT_WORD_TIERS = ("fld_", "vet_", "prm_", "asc_")
ROMAN = ["I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X"]

# slot key -> module name, resref code, hardened suffix, tuned suffix, short-word hardened/tuned suffix (for the tiers above)
SLOTS = [
    ("head", "Sensor Package", "sens", "helmet", "cap", "helm", "cap"),
    ("chest", "Chassis Plating", "plat", "armor", "tunic", "armor", "tunic"),
    ("hands", "Manipulator Servo", "serv", "bracer", "gloves", "bracer", "gloves"),
    ("feet", "Drive Unit", "driv", "leggings", "boots", "legs", "boots"),
    ("belt", "Power Cell", "cell", "belt", "belt", "belt", "belt"),
    ("neck", "Logic Module", "logc", "necklace", "necklace", "neck", "neck"),
    ("ring", "Expansion Socket", "sock", "ring", "ring", "ring", "ring"),
    ("cloak", "Deflector Emitter", "defl", "cloak", "cloak", "cloak", "cloak"),
]
LADDERS = [("h", "Hardened", HARDENED), ("t", "Tuned", TUNED)]

# Chassis Plating is the chest module, and a worn chest piece dyes the body it covers: on an astromech its second cloth
# dye is the main paint, its first cloth dye the accent panels and its second metal dye the trim. A copied humanoid piece
# carries dyes picked for cloth and armor, so each plating takes one of these paint jobs instead (palette rows: main,
# accent, trim). A Tuned plating starts four jobs after the Hardened one of its tier, so the two never match.
PAINT_FIELDS = ("Cloth2Color", "Cloth1Color", "Metal2Color")
PAINT_JOBS = [(20, 27, 132), (35, 133, 126), (106, 93, 132), (171, 90, 1),
              (7, 143, 126), (20, 90, 1), (27, 20, 132), (93, 171, 1)]

# The plating a new astromech starts in, as others start in Traveler's Clothes: no stats, only paint to change.
STARTER_SOURCE = "traveler_m"
STARTER_RESREF = "am_plat_start"
STARTER_NAME = "Basic Chassis Plating"
STARTER_DESCRIPTION = ("Factory plating for an astromech chassis. It protects nothing a careful owner would not, "
                       "but it takes paint well.")


class Module:
    def __init__(self, slot_index, ladder_index, tier):
        slot = SLOTS[slot_index]
        letter, ladder_name, prefixes = LADDERS[ladder_index]
        prefix = prefixes[tier]
        short = prefix.startswith(SHORT_WORD_TIERS)
        if letter == "h":
            suffix = slot[5] if short else slot[3]
        else:
            suffix = slot[6] if short else slot[4]
        self.slot_key = slot[0]
        self.module_name = slot[1]
        self.code = slot[2]
        self.ladder_name = ladder_name
        self.letter = letter
        self.tier = tier  # 0..9
        self.source = f"{prefix}_{suffix}"
        self.resref = f"am_{letter}_{self.code}_{tier + 1:02d}"
        self.name = f"{ladder_name} {self.module_name} Mk {ROMAN[tier]}"
        self.enum = f"Astromech{ladder_name}{self.module_name.replace(' ', '')}{tier + 1:02d}"
        self.recipe_id = FIRST_RECIPE_ID + (slot_index * 2 + ladder_index) * 10 + tier


MODULES = [Module(s, l, t) for s in range(len(SLOTS)) for l in range(2) for t in range(10)]


def to_crlf(text: str) -> str:
    """Text with every line ending as CRLF, whichever form the checkout gave it."""
    return text.replace("\r\n", "\n").replace("\n", "\r\n")


def read(path: Path, encoding: str = "latin-1") -> str:
    """A file as text with CRLF line endings. The generators parse and build in CRLF; a checkout that normalizes
    these files to LF (`* text=auto` on Linux) reads the same, and `localize` gives the output its line endings back."""
    return to_crlf(path.read_bytes().decode(encoding))


def checkout_newline(path: Path, fallback: bytes) -> bytes:
    """The line ending a file has in this checkout, or the fallback for a file that does not exist yet."""
    if not path.exists():
        return fallback
    return b"\r\n" if b"\r\n" in path.read_bytes() else b"\n"


def localize(outputs: dict) -> dict:
    """Generated content with the line endings of this checkout: each file keeps the form it already has on disk,
    and a new file takes the form RecipeType.cs is checked out in."""
    fallback = checkout_newline(RECIPE_TYPE_CS, b"\r\n")
    return {path: content.replace(b"\r\n", b"\n").replace(b"\n", checkout_newline(path, fallback))
            for path, content in outputs.items()}


# ---------------------------------------------------------------- source recipes

def parse_smithery() -> dict:
    out: dict[str, list] = {}
    for file in sorted(SMITHERY.glob("*.cs")):
        text = file.read_text(encoding="utf-8")
        for m in re.finditer(r"_builder\.Create\(RecipeType\.(\w+), SkillType\.Smithery\)(.*?);", text, re.S):
            body = m.group(2)
            resref = re.search(r'\.Resref\("([^"]+)"\)', body)
            if not resref:
                continue
            out.setdefault(resref.group(1), []).append({
                "type": m.group(1),
                "level": int(re.search(r"\.Level\((\d+)\)", body).group(1)),
                "quantity": int(re.search(r"\.Quantity\((\d+)\)", body).group(1)),
                "enhancement": re.findall(r"\.EnhancementSlots\(RecipeEnhancementType\.(\w+), (\d+)\)", body),
                "components": re.findall(r'\.Component\("([^"]+)", (\d+)\)', body),
                "locked": ".RequirementUnlocked()" in body,
                "research": re.findall(r"\.ResearchCostModifier\(([^)]+)\)", body),
            })
    return out


# ---------------------------------------------------------------- blueprints

PROPERTY_BLOCK = re.compile(r"\r\n      \{\r\n(.*?)\r\n      \}", re.S)

NEW_PROPERTY = (
    '      {\r\n'
    '        "__struct_id": 0,\r\n'
    '        "ChanceAppear": {\r\n          "type": "byte",\r\n          "value": 100\r\n        },\r\n'
    '        "CostTable": {\r\n          "type": "byte",\r\n          "value": 0\r\n        },\r\n'
    '        "CostValue": {\r\n          "type": "word",\r\n          "value": 0\r\n        },\r\n'
    '        "Param1": {\r\n          "type": "byte",\r\n          "value": 255\r\n        },\r\n'
    '        "Param1Value": {\r\n          "type": "byte",\r\n          "value": 0\r\n        },\r\n'
    '        "PropertyName": {\r\n          "type": "word",\r\n          "value": %d\r\n        },\r\n'
    '        "Subtype": {\r\n          "type": "word",\r\n          "value": %d\r\n        }\r\n'
    '      }' % (RACIAL_PROPERTY, ASTROMECH_RACE))


def renamed(source: str, resref: str, name: str) -> str:
    """The source blueprint under a new resref and name."""
    path = UTI / f"{source}.uti.json"
    if not path.exists():
        raise SystemExit(f"missing source blueprint {path}")
    text = read(path)

    name_pattern = re.compile(
        r'  "LocalizedName": \{\r\n(?:    "id": \d+,\r\n)?    "type": "cexolocstring",\r\n    "value": \{\r\n      "0": "[^"\r\n]*"\r\n    \}\r\n  \},')
    if len(name_pattern.findall(text)) != 1:
        raise SystemExit(f"{source}: LocalizedName has an unexpected shape")
    new_name = ('  "LocalizedName": {\r\n    "type": "cexolocstring",\r\n    "value": {\r\n'
                f'      "0": "{name}"\r\n    }}\r\n  }},')
    text = name_pattern.sub(lambda _m: new_name, text)

    for key, kind in (("Tag", "cexostring"), ("TemplateResRef", "resref")):
        pattern = re.compile(rf'  "{key}": \{{\r\n    "type": "{kind}",\r\n    "value": "{re.escape(source)}"\r\n  \}}')
        if len(pattern.findall(text)) != 1:
            raise SystemExit(f"{source}: {key} is not the resref")
        text = pattern.sub(lambda _m: f'  "{key}": {{\r\n    "type": "{kind}",\r\n    "value": "{resref}"\r\n  }}', text)
    return text


ARMOR_MODEL_FIELD = re.compile(r'(  "x?ArmorPart_\w+": \{\r\n    "type": "(?:byte|word)",\r\n    "value": )\d+(\r\n  \})')


# The torso number every plating carries. No body has a model under it, so the chassis still shows through; it exists
# for its inventory icon (ip?_chest240, drawn by tools/BuildAstromechIcons.py). An armor icon is built from the part
# numbers, and an armor with none shows a bare torso.
PLATING_TORSO = 240
TORSO_FIELD = re.compile(r'(  "x?ArmorPart_Torso": \{\r\n    "type": "(?:byte|word)",\r\n    "value": )\d+(\r\n  \})')


def without_part_models(text: str, source: str) -> str:
    """A copied armor blueprint with the part models of plating: none but the plating torso. An astromech module has
    no models of its own; a part number left over from the humanoid piece can name a chassis part (thigh 6 is the
    legs of design 6) and would replace the part the wearer chose."""
    if not ARMOR_MODEL_FIELD.search(text) or len(TORSO_FIELD.findall(text)) != 2:
        raise SystemExit(f"{source}: unexpected armor part fields")
    text = ARMOR_MODEL_FIELD.sub(lambda m: f"{m.group(1)}0{m.group(2)}", text)
    return TORSO_FIELD.sub(lambda m: f"{m.group(1)}{PLATING_TORSO}{m.group(2)}", text)


def painted(text: str, source: str, job: tuple) -> str:
    """The blueprint with a paint job in place of the dyes its source wore."""
    for field, row in zip(PAINT_FIELDS, job):
        pattern = re.compile(rf'(  "{field}": \{{\r\n    "type": "byte",\r\n    "value": )\d+(\r\n  \}})')
        if len(pattern.findall(text)) != 1:
            raise SystemExit(f"{source}: {field} has an unexpected shape")
        text = pattern.sub(lambda m, row=row: f"{m.group(1)}{row}{m.group(2)}", text)
    return text


def starter_blueprint() -> str:
    text = renamed(STARTER_SOURCE, STARTER_RESREF, STARTER_NAME)
    for key in ("DescIdentified", "Description"):
        pattern = re.compile(
            rf'(  "{key}": \{{\r\n    "type": "cexolocstring",\r\n    "value": \{{\r\n      "0": ")[^"\r\n]*("\r\n    \}}\r\n  \}})')
        if len(pattern.findall(text)) != 1:
            raise SystemExit(f"{STARTER_SOURCE}: {key} has an unexpected shape")
        text = pattern.sub(lambda m: f"{m.group(1)}{STARTER_DESCRIPTION}{m.group(2)}", text)
    empty = '  "PropertiesList": {\r\n    "type": "list",\r\n    "value": []\r\n  },'
    if text.count(empty) != 1:
        raise SystemExit(f"{STARTER_SOURCE}: expected no item properties")
    text = text.replace(empty, '  "PropertiesList": {\r\n    "type": "list",\r\n    "value": [\r\n'
                        + NEW_PROPERTY + '\r\n    ]\r\n  },')
    return painted(without_part_models(text, STARTER_SOURCE), STARTER_SOURCE, PAINT_JOBS[0])


def module_blueprint(module: Module) -> str:
    text = renamed(module.source, module.resref, module.name)

    changed = 0
    for match in PROPERTY_BLOCK.finditer(text):
        block = match.group(1)
        number = int(re.search(r'"PropertyName": \{\r\n          "type": "word",\r\n          "value": (\d+)', block).group(1))
        if number == RACIAL_PROPERTY:
            raise SystemExit(f"{module.source} already carries a racial limitation")
        if number == REQUIRES_SKILL_PROPERTY:
            pattern = re.compile(r'("Subtype": \{\r\n          "type": "word",\r\n          "value": )%d(\r\n)' % ARMOR_SKILL)
            if len(pattern.findall(block)) != 1:
                raise SystemExit(f"{module.source}: RequiresSkill is not Armor")
            new_block = pattern.sub(lambda m: f"{m.group(1)}{CHASSIS_SKILL}{m.group(2)}", block)
            text = text[:match.start(1)] + new_block + text[match.end(1):]
            changed += 1
            break
    if changed != 1:
        raise SystemExit(f"{module.source}: expected exactly one Armor RequiresSkill property")

    tail = '      }\r\n    ]\r\n  },\r\n  "StackSize"'
    if text.count(tail) != 1:
        raise SystemExit(f"{module.source}: PropertiesList has an unexpected end")
    text = text.replace(tail, '      },\r\n' + NEW_PROPERTY + '\r\n    ]\r\n  },\r\n  "StackSize"')
    if module.slot_key == "chest":
        job = module.tier + (len(PAINT_JOBS) // 2 if module.letter == "t" else 0)
        text = painted(without_part_models(text, module.source), module.source, PAINT_JOBS[job % len(PAINT_JOBS)])
    return text


# ---------------------------------------------------------------- recipes

def existing_circuit_parameters() -> dict:
    if not RECIPES_CS.exists():
        return {}
    text = RECIPES_CS.read_text(encoding="utf-8")
    found = {}
    for m in re.finditer(r"_builder\.Create\(RecipeType\.(\w+), SkillType\.Engineering\)(.*?);", text, re.S):
        p = re.search(r"\.CircuitParameters\(([^)]*)\)", m.group(2))
        if p:
            found[m.group(1)] = p.group(1)
    return found


def recipes_source(smithery: dict) -> str:
    parameters = existing_circuit_parameters()
    lines = [
        "using System.Collections.Generic;",
        "using SWLOR.Game.Server.Service.CraftService;",
        "using SWLOR.Game.Server.Service.SkillService;",
        "",
        "namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition",
        "{",
        "    /// <summary>",
        "    /// The Astromech gear modules: one Engineering recipe per module, copying the Level, Quantity, enhancement slots and",
        "    /// components of the Smithery recipe of the humanoid piece the module copies. Generated by tools/GenerateAstromechModules.py;",
        "    /// the circuit parameters are written by tools/SWLOR.RecipeParameters.",
        "    /// </summary>",
        "    public class AstromechModuleRecipes : IRecipeListDefinition",
        "    {",
        "        private readonly RecipeBuilder _builder = new RecipeBuilder();",
        "",
        "        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()",
        "        {",
    ]
    for index, module in enumerate(MODULES):
        source = smithery.get(module.source, [])
        if len(source) != 1:
            raise SystemExit(f"{module.source}: expected one Smithery recipe, found {len(source)}")
        s = source[0]
        if len(s["enhancement"]) > 1:
            raise SystemExit(f"{module.source}: more than one EnhancementSlots call")
        if index:
            lines.append("")
        lines.append(f"            // {module.name} (copies {module.source})")
        lines.append(f"            _builder.Create(RecipeType.{module.enum}, SkillType.Engineering)")
        lines.append("                .Category(RecipeCategoryType.AstromechModule)")
        lines.append(f'                .Resref("{module.resref}")')
        lines.append(f"                .Level({s['level']})")
        lines.append(f"                .Quantity({s['quantity']})")
        if s["locked"]:
            # A learned humanoid recipe is learned from a book. The module version is learned with it:
            # whoever has learned to make the piece can make its astromech form, so no second book exists.
            lines.append(f"                .RequirementUnlocked(RecipeType.{s['type']})")
        for kind, slots in s["enhancement"]:
            lines.append(f"                .EnhancementSlots(RecipeEnhancementType.{kind}, {slots})")
        for modifier in s["research"]:
            lines.append(f"                .ResearchCostModifier({modifier})")
        for resref, quantity in s["components"]:
            lines.append(f'                .Component("{resref}", {quantity})')
        circuit = parameters.get(module.enum)
        if circuit:
            lines.append(f"                .CircuitParameters({circuit});")
        else:
            lines[-1] += ";"
    lines += ["", "            return _builder.Build();", "        }", "    }", "}", ""]
    return "\r\n".join(lines)


def recipe_type_text(current: str) -> str:
    begin = "        // BEGIN astromech modules (generated by tools/GenerateAstromechModules.py)"
    end = "        // END astromech modules"
    block = [begin,
             "        // Astromech gear modules: Engineering copies of the Hardened and Tuned humanoid armor ladders, 5071-5230.",
             ]
    block += [f"        {m.enum} = {m.recipe_id}," for m in MODULES]
    block.append(end)
    block_text = "\r\n".join(block)
    if begin in current:
        pattern = re.compile(re.escape(begin) + r".*?" + re.escape(end), re.S)
        return pattern.sub(lambda _m: block_text, current, count=1)
    anchor = "        BiolabNecklace = 5070,\r\n"
    if current.count(anchor) != 1:
        raise SystemExit("RecipeType.cs: anchor BiolabNecklace = 5070 not found")
    return current.replace(anchor, anchor + block_text + "\r\n")


# ---------------------------------------------------------------- store

def store_item(index: int, resref: str, x: int, y: int) -> str:
    return (
        '            {\r\n'
        f'              "__struct_id": {index},\r\n'
        '              "Infinite": {\r\n                "type": "byte",\r\n                "value": 1\r\n              },\r\n'
        '              "InventoryRes": {\r\n                "type": "resref",\r\n'
        f'                "value": "{resref}"\r\n              }},\r\n'
        f'              "Repos_PosX": {{\r\n                "type": "word",\r\n                "value": {x}\r\n              }},\r\n'
        f'              "Repos_Posy": {{\r\n                "type": "word",\r\n                "value": {y}\r\n              }}\r\n'
        '            }')


def store_text(current: str) -> str:
    data = json.loads(current)
    tabs = [[i["InventoryRes"]["value"] for i in s["ItemList"]["value"]] for s in data["StoreList"]["value"]]
    positions = [[(i["Repos_Posy"]["value"], i["Repos_PosX"]["value"]) for i in s["ItemList"]["value"]]
               for s in data["StoreList"]["value"]]
    present = {r for tab in tabs for r in tab}
    additions: dict[int, list] = {}
    for module in MODULES:
        if module.tier != 0 or module.resref in present:
            continue
        homes = [i for i, tab in enumerate(tabs) if module.source in tab]
        if len(homes) != 1:
            raise SystemExit(f"store: {module.source} is not listed exactly once")
        additions.setdefault(homes[0], []).append(module.resref)

    ends = [m.start() for m in re.finditer(r"\r\n            \}\r\n          \]\r\n        \}\r\n      \}", current)]
    if len(ends) != len(tabs):
        raise SystemExit("store: unexpected tab layout")
    result = current
    for tab in sorted(additions, reverse=True):
        y, x = max(positions[tab])
        count = len(tabs[tab])
        pieces = []
        for resref in additions[tab]:
            x += 2
            if x > 8:
                x, y = 0, y + 2
            pieces.append(store_item(count, resref, x, y))
            count += 1
        at = ends[tab] + len("\r\n            }")
        result = result[:at] + ",\r\n" + ",\r\n".join(pieces) + result[at:]
    return result


# ---------------------------------------------------------------- driver

def expected_outputs() -> dict:
    smithery = parse_smithery()
    outputs = {}
    for module in MODULES:
        outputs[UTI / f"{module.resref}.uti.json"] = module_blueprint(module).encode("latin-1")
    outputs[UTI / f"{STARTER_RESREF}.uti.json"] = starter_blueprint().encode("latin-1")
    outputs[RECIPES_CS] = recipes_source(smithery).encode("utf-8")
    outputs[RECIPE_TYPE_CS] = recipe_type_text(read(RECIPE_TYPE_CS, "utf-8")).encode("utf-8")
    outputs[STORE] = store_text(read(STORE)).encode("latin-1")
    # The game opens the copies of the store placed in areas, which hold their own item lists.
    outputs.update(placed.stock({STORE.name.split(".")[0]: [(m.source, m.resref) for m in MODULES if m.tier == 0]},
                                lambda resref: outputs[UTI / f"{resref}.uti.json"]))
    return localize(outputs)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--check", action="store_true", help="write nothing; exit 1 and list differences")
    args = parser.parse_args()

    outputs = expected_outputs()
    differences = []
    for path, content in outputs.items():
        if not path.exists() or path.read_bytes() != content:
            differences.append(path)

    if args.check:
        for path in differences:
            print(f"differs: {path.relative_to(ROOT)}")
        if differences:
            print(f"{len(differences)} file(s) differ; run tools/GenerateAstromechModules.py")
            return 1
        print("Astromech modules are up to date.")
        return 0

    for path in differences:
        path.write_bytes(outputs[path])
    print(f"wrote {len(differences)} of {len(outputs)} files")
    return 0


if __name__ == "__main__":
    sys.exit(main())
