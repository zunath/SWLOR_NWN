"""Generate the Astromech Armaments tools.

An astromech cannot wield humanoid weapons. Its weapons are "tools": Shock Prods (melee) and Blaster Mounts (ranged), ordinary
weapons that only astromechs can equip (item property UseLimitationRacialType, subtype 170). Tools introduce no balance
numbers: each one is a stat copy of the humanoid quarterstaff or rifle of the same tier, with a new name, the Armaments
(skill 63) rank requirement in place of the Staff (skill 44) or Rifle (skill 46) rank, a new base item, and the
astromech-only property.

  python tools/GenerateAstromechTools.py            write everything
  python tools/GenerateAstromechTools.py --check    write nothing, exit 1 and list what differs

Writes (all derived from the source blueprints and the source Smithery recipes, so a change to a humanoid weapon is picked
up by rerunning, and caught by AstromechToolContentTests):
  - Module/uti/am_w_prod_<01..10>.uti.json and am_w_blst_<01..10>.uti.json, 20 blueprints
  - SWLOR.Game.Server/Feature/RecipeDefinition/EngineeringRecipeDefinition/AstromechToolRecipes.cs
    (the .CircuitParameters(...) values are written by tools/SWLOR.RecipeParameters and kept across reruns)
  - the generated RecipeType block (5231-5250) in SWLOR.Game.Server/Service/CraftService/RecipeType.cs
  - the two Mk I tools in Module/utm/cz220_merchant.utm.json, next to the humanoid Basic Staff and Basic Rifle they copy,
    and in every copy of that store placed in an area (Module/git), which is what the game opens
"""
from __future__ import annotations

import argparse
import re
import sys
import json
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import GenerateAstromechModules as mod  # noqa: E402

ROOT = mod.ROOT
UTI = mod.UTI
RECIPES_CS = ROOT / "SWLOR.Game.Server" / "Feature" / "RecipeDefinition" / "EngineeringRecipeDefinition" / "AstromechToolRecipes.cs"
RECIPE_TYPE_CS = mod.RECIPE_TYPE_CS
STORE = mod.STORE

ARMAMENTS_SKILL = 63
FIRST_RECIPE_ID = 5231

# Tier prefixes of the humanoid staff and rifle ladders, Mk I to Mk X (ascending recipe level).
PREFIXES = ["b", "fld", "tit", "vet", "del", "prm", "proto", "asc", "oph", "chi"]

# kind -> (source suffix, source base item, source skill subtype, new base item, resref code, tool name, enum stem)
KINDS = [
    ("staff", 50, 44, 545, "prod", "Shock Prod", "ShockProd"),
    ("rifle", 7, 46, 546, "blst", "Blaster Mount", "BlasterMount"),
]
TIER_NAMES = {"b": "Basic", "fld": "Field", "tit": "Titan", "vet": "Veteran", "del": "Delta", "prm": "Prime",
              "proto": "Proto", "asc": "Ascendant", "oph": "Ophidian", "chi": "Chiro"}


class Tool:
    def __init__(self, kind_index, tier):
        suffix, base_item, skill, new_base, code, name, stem = KINDS[kind_index]
        prefix = PREFIXES[tier]
        self.source = f"{prefix}_{suffix}"
        self.source_base_item = base_item
        self.source_skill = skill
        self.base_item = new_base
        self.tier = tier  # 0..9
        self.tier_name = TIER_NAMES[prefix]
        self.source_name = f"{self.tier_name} {suffix.capitalize()}"
        self.name = f"{self.tier_name} {name}"
        self.resref = f"am_w_{code}_{tier + 1:02d}"
        self.enum = f"Astromech{stem}{tier + 1:02d}"
        self.recipe_id = FIRST_RECIPE_ID + kind_index * 10 + tier


TOOLS = [Tool(k, t) for k in range(len(KINDS)) for t in range(10)]


# ---------------------------------------------------------------- blueprints

def tool_blueprint(tool: Tool) -> str:
    path = UTI / f"{tool.source}.uti.json"
    if not path.exists():
        raise SystemExit(f"missing source blueprint {path}")
    text = mod.read(path)

    name_pattern = re.compile(
        r'  "LocalizedName": \{\r\n(?:    "id": \d+,\r\n)?    "type": "cexolocstring",\r\n    "value": \{\r\n      "0": "([^"\r\n]*)"\r\n    \}\r\n  \},')
    found = name_pattern.findall(text)
    if len(found) != 1 or found[0] != tool.source_name:
        raise SystemExit(f"{tool.source}: LocalizedName is {found}, expected {tool.source_name}")
    new_name = ('  "LocalizedName": {\r\n    "type": "cexolocstring",\r\n    "value": {\r\n'
                f'      "0": "{tool.name}"\r\n    }}\r\n  }},')
    text = mod.described(name_pattern.sub(lambda _m: new_name, text), tool.source)

    base_pattern = re.compile(r'(  "BaseItem": \{\r\n    "type": "int",\r\n    "value": )(\d+)(\r\n  \})')
    base = base_pattern.findall(text)
    if len(base) != 1 or int(base[0][1]) != tool.source_base_item:
        raise SystemExit(f"{tool.source}: BaseItem is not {tool.source_base_item}")
    text = base_pattern.sub(lambda m: f"{m.group(1)}{tool.base_item}{m.group(3)}", text)

    for key, kind in (("Tag", "cexostring"), ("TemplateResRef", "resref")):
        pattern = re.compile(rf'  "{key}": \{{\r\n    "type": "{kind}",\r\n    "value": "{re.escape(tool.source)}"\r\n  \}}')
        if len(pattern.findall(text)) != 1:
            raise SystemExit(f"{tool.source}: {key} is not the resref")
        text = pattern.sub(lambda _m: f'  "{key}": {{\r\n    "type": "{kind}",\r\n    "value": "{tool.resref}"\r\n  }}', text)

    changed = 0
    for match in mod.PROPERTY_BLOCK.finditer(text):
        block = match.group(1)
        number = int(re.search(r'"PropertyName": \{\r\n          "type": "word",\r\n          "value": (\d+)', block).group(1))
        if number == mod.RACIAL_PROPERTY:
            raise SystemExit(f"{tool.source} already carries a racial limitation")
        if number == mod.REQUIRES_SKILL_PROPERTY:
            pattern = re.compile(r'("Subtype": \{\r\n          "type": "word",\r\n          "value": )%d(\r\n)' % tool.source_skill)
            if len(pattern.findall(block)) != 1:
                raise SystemExit(f"{tool.source}: RequiresSkill is not skill {tool.source_skill}")
            new_block = pattern.sub(lambda m: f"{m.group(1)}{ARMAMENTS_SKILL}{m.group(2)}", block)
            text = text[:match.start(1)] + new_block + text[match.end(1):]
            changed += 1
            break
    if changed != 1:
        raise SystemExit(f"{tool.source}: expected exactly one RequiresSkill property of skill {tool.source_skill}")

    tail = '      }\r\n    ]\r\n  },\r\n  "StackSize"'
    if text.count(tail) != 1:
        raise SystemExit(f"{tool.source}: PropertiesList has an unexpected end")
    text = text.replace(tail, '      },\r\n' + mod.NEW_PROPERTY + '\r\n    ]\r\n  },\r\n  "StackSize"')
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
        "    /// The Astromech Armaments tools: one Engineering recipe per Shock Prod and Blaster Mount, copying the Level, Quantity,",
        "    /// enhancement slots and components of the Smithery recipe of the humanoid staff or rifle the tool copies. Generated by",
        "    /// tools/GenerateAstromechTools.py; the circuit parameters are written by tools/SWLOR.RecipeParameters.",
        "    /// </summary>",
        "    public class AstromechToolRecipes : IRecipeListDefinition",
        "    {",
        "        private readonly RecipeBuilder _builder = new RecipeBuilder();",
        "",
        "        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()",
        "        {",
    ]
    for index, tool in enumerate(TOOLS):
        source = smithery.get(tool.source, [])
        if len(source) != 1:
            raise SystemExit(f"{tool.source}: expected one Smithery recipe, found {len(source)}")
        s = source[0]
        if len(s["enhancement"]) > 1:
            raise SystemExit(f"{tool.source}: more than one EnhancementSlots call")
        if index:
            lines.append("")
        lines.append(f"            // {tool.name} (copies {tool.source})")
        lines.append(f"            _builder.Create(RecipeType.{tool.enum}, SkillType.Engineering)")
        lines.append("                .Category(RecipeCategoryType.AstromechTool)")
        lines.append(f'                .Resref("{tool.resref}")')
        lines.append(f"                .Level({s['level']})")
        lines.append(f"                .Quantity({s['quantity']})")
        if s["locked"]:
            # A learned humanoid recipe is learned from a book. The tool version is learned with it:
            # whoever has learned to make the weapon can make its astromech form, so no second book exists.
            lines.append(f"                .RequirementUnlocked(RecipeType.{s['type']})")
        for kind, slots in s["enhancement"]:
            lines.append(f"                .EnhancementSlots(RecipeEnhancementType.{kind}, {slots})")
        for modifier in s["research"]:
            lines.append(f"                .ResearchCostModifier({modifier})")
        for resref, quantity in s["components"]:
            lines.append(f'                .Component("{resref}", {quantity})')
        circuit = parameters.get(tool.enum)
        if circuit:
            lines.append(f"                .CircuitParameters({circuit});")
        else:
            lines[-1] += ";"
    lines += ["", "            return _builder.Build();", "        }", "    }", "}", ""]
    return "\r\n".join(lines)


def recipe_type_text(current: str) -> str:
    begin = "        // BEGIN astromech tools (generated by tools/GenerateAstromechTools.py)"
    end = "        // END astromech tools"
    block = [begin,
             "        // Astromech Armaments tools: Engineering copies of the humanoid quarterstaff and rifle ladders, 5231-5250.",
             ]
    block += [f"        {t.enum} = {t.recipe_id}," for t in TOOLS]
    block.append(end)
    block_text = "\r\n".join(block)
    if begin in current:
        pattern = re.compile(re.escape(begin) + r".*?" + re.escape(end), re.S)
        return pattern.sub(lambda _m: block_text, current, count=1)
    anchor = "        // END astromech modules\r\n"
    if current.count(anchor) != 1:
        raise SystemExit("RecipeType.cs: anchor '// END astromech modules' not found")
    return current.replace(anchor, anchor + block_text + "\r\n")


# ---------------------------------------------------------------- store

def store_text(current: str) -> str:
    data = json.loads(current)
    tabs = [[i["InventoryRes"]["value"] for i in s["ItemList"]["value"]] for s in data["StoreList"]["value"]]
    positions = [[(i["Repos_Posy"]["value"], i["Repos_PosX"]["value"]) for i in s["ItemList"]["value"]]
               for s in data["StoreList"]["value"]]
    present = {r for tab in tabs for r in tab}
    additions: dict[int, list] = {}
    for tool in TOOLS:
        if tool.tier != 0 or tool.resref in present:
            continue
        homes = [i for i, tab in enumerate(tabs) if tool.source in tab]
        if len(homes) != 1:
            raise SystemExit(f"store: {tool.source} is not listed exactly once")
        additions.setdefault(homes[0], []).append(tool.resref)

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
            pieces.append(mod.store_item(count, resref, x, y))
            count += 1
        at = ends[tab] + len("\r\n            }")
        result = result[:at] + ",\r\n" + ",\r\n".join(pieces) + result[at:]
    return result


# ---------------------------------------------------------------- driver

def expected_outputs() -> dict:
    smithery = mod.parse_smithery()
    outputs = {}
    for tool in TOOLS:
        outputs[UTI / f"{tool.resref}.uti.json"] = tool_blueprint(tool).encode("latin-1")
    outputs[RECIPES_CS] = recipes_source(smithery).encode("utf-8")
    outputs[RECIPE_TYPE_CS] = recipe_type_text(mod.read(RECIPE_TYPE_CS, "utf-8")).encode("utf-8")
    outputs[STORE] = store_text(mod.read(STORE)).encode("latin-1")
    # The game opens the copies of the store placed in areas, which hold their own item lists.
    outputs.update(mod.placed.stock({STORE.name.split(".")[0]: [(t.source, t.resref) for t in TOOLS if t.tier == 0]},
                                    lambda resref: outputs[UTI / f"{resref}.uti.json"]))
    return mod.localize(outputs)


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
            print(f"{len(differences)} file(s) differ; run tools/GenerateAstromechTools.py")
            return 1
        print("Astromech tools are up to date.")
        return 0

    for path in differences:
        path.write_bytes(outputs[path])
    print(f"wrote {len(differences)} of {len(outputs)} files")
    return 0


if __name__ == "__main__":
    sys.exit(main())
