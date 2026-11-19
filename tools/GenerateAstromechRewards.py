"""Generate the astromech forms of humanoid quest rewards, guild store weapons and looted gear.

An astromech equips only astromech modules and tools, so a quest, guild store or loot table that hands out a humanoid
necklace, bracer, staff or rifle would hand it something it cannot use. Each such item gets an astromech form here: a
stat copy with a new name, the Chassis (skill 61) or Armaments (skill 63) rank requirement in place of the Armor, Staff
or Rifle rank, the tool base item for a weapon, and the astromech-only property. No balance number is introduced.

Looted gear is found by reading the loot tables (Feature/LootTableDefinition): every armor-slot piece, staff and rifle a
table can drop gets a form, named by putting the module name in place of the humanoid slot word ("Overcharge Sash"
becomes "Overcharge Power Cell"; tools/AstromechLootNames.json names the pieces that rule cannot). The game drops a
form wherever its humanoid piece drops (Service/AstromechService/AstromechLoot.cs), so no loot table is edited.
Weapons of other kinds have no astromech tool to become and get no form.

  python tools/GenerateAstromechRewards.py            write everything
  python tools/GenerateAstromechRewards.py --check    write nothing, exit 1 and list what differs

Writes:
  - Module/uti/am_q_*.uti.json (quest reward modules) and am_w_*_cap / am_w_*_h<1..5>.uti.json (reward and guild tools)
  - Module/uti/am_<looted piece>.uti.json, one per looted piece
  - the Hunter tools in Module/utm/gp_hunt_<1..5>.utm.json, next to the Hunter Staff and Hunter Rifle they copy,
    and in every copy of those stores placed in an area (Module/git), which is what the game opens
  - SWLOR.Game.Server/Service/AstromechService/AstromechItemTwins.Generated.cs: the humanoid resref to astromech resref
    table (these items plus every crafted module and tool), which quest item rewards read
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import GenerateAstromechModules as mod  # noqa: E402
import GenerateAstromechTools as tools  # noqa: E402

ROOT = mod.ROOT
UTI = mod.UTI
UTM = ROOT / "Module" / "utm"
TWINS_CS = ROOT / "SWLOR.Game.Server" / "Service" / "AstromechService" / "AstromechItemTwins.Generated.cs"

ARMOR_SKILL = mod.ARMOR_SKILL
CHASSIS_SKILL = mod.CHASSIS_SKILL
ARMAMENTS_SKILL = tools.ARMAMENTS_SKILL
STAFF = (50, 44, 545)   # source base item, source skill, tool base item
RIFLE = (7, 46, 546)
ROMAN = ["I", "II", "III", "IV", "V"]


class Twin:
    def __init__(self, source, resref, name, weapon=None, store=None, looted=False):
        self.source = source
        self.resref = resref
        self.name = name
        self.weapon = weapon      # None for a module, STAFF or RIFLE for a tool
        self.store = store        # the store blueprint that sells the source, if any
        self.looted = looted      # found in a loot table (loot_twins), not listed by hand
        assert len(resref) <= 16, resref


# Quest reward modules: the humanoid piece keeps its slot and takes the module name of that slot.
TWINS = [
    Twin("tidecall_pendant", "am_q_tidecall", "Tidecaller's Logic Module"),
    Twin("reefwarden_band", "am_q_reefwarden", "Reefwarden's Expansion Socket"),
    Twin("rancorhide_belt", "am_q_rancorhide", "Rancor-Hide Power Cell"),
    Twin("kwi_spirit_charm", "am_q_kwispirit", "Kwi Spirit Logic Module"),
    Twin("hunter_vambrace", "am_q_hunter", "Hunter's Manipulator Servo"),
    Twin("fdybreak_bracer", "am_q_fdybreak", "Foundry Breaker's Manipulator Servo"),
    Twin("chieftain_torc", "am_q_chieftain", "Chieftain's Logic Module"),
    Twin("caravan_sash", "am_q_caravan", "Caravan Master's Power Cell"),
    Twin("canopy_band", "am_q_canopy", "Canopy Stalker's Expansion Socket"),
    Twin("broodguard_ring", "am_q_broodguard", "Broodguard Expansion Socket"),
    Twin("apprentice_dark_", "am_q_darkside", "Apprentice Dark Side Plating"),
    Twin("cap_staff", "am_w_prod_cap", "Captain Shock Prod", STAFF),
    Twin("cap_rifle", "am_w_blst_cap", "Captain Blaster Mount", RIFLE),
]
for _rank in range(1, 6):
    TWINS.append(Twin(f"h_staff_{_rank}", f"am_w_prod_h{_rank}", f"Hunter Shock Prod {ROMAN[_rank - 1]}", STAFF,
                      f"gp_hunt_{_rank}"))
    TWINS.append(Twin(f"h_rifle_{_rank}", f"am_w_blst_h{_rank}", f"Hunter Blaster Mount {ROMAN[_rank - 1]}", RIFLE,
                      f"gp_hunt_{_rank}"))


# ---------------------------------------------------------------- looted gear

LOOT_TABLES = ROOT / "SWLOR.Game.Server" / "Feature" / "LootTableDefinition"
LOOT_NAMES = Path(__file__).resolve().parent / "AstromechLootNames.json"
DROID_RACE = 167
# Skills a looted piece may ask for besides Armor that an astromech trains too; the requirement is kept as it is.
SHARED_SKILLS = (9,)  # Smithery
# Base item of a looted piece -> (module name its astromech form takes, the humanoid slot words that name replaces).
LOOT_MODULES = {
    17: ("Sensor Package", "Visor Cowl Mask Cap Helm Helmet Hood Lens Goggles Headdress Respirator Crown Headwrap Wrap"),
    16: ("Chassis Plating", "Plating Harness Tunic Plate Armor Robe Coat Vest Robes Wrap Protection Coveralls Longcoat "
                            "Cuirass Hide Vestments"),
    36: ("Manipulator Servo", "Bracer Gloves Vambrace Gauntlets Wraps Rig Shroud Guard Cestus Grip Array Gauntlet Frame "
                              "Charm"),
    78: ("Manipulator Servo", "Bracer Gloves Vambrace Gauntlets Wraps Rig Shroud Guard Cestus Grip Array Gauntlet Frame "
                              "Charm"),
    26: ("Drive Unit", "Boots Leggings Trailboots Shoes Treads Clawboots"),
    21: ("Power Cell", "Sash Belt Wrap Coil Rig"),
    19: ("Logic Module", "Necklace Charm Fetish Totem Relic Core Pendant Keyloop Lens Talisman Sigil Beads Amulet"),
    52: ("Expansion Socket", "Band Ring Signet"),
    80: ("Deflector Emitter", "Mantle Cloak Cape Shawl Shroud"),
    STAFF[0]: ("Shock Prod", "Staff Rod Pole Conductor Gaderffii Pike"),
    RIFLE[0]: ("Blaster Mount", "Rifle Carbine"),
}
LOOT_WEAPONS = {STAFF[0]: STAFF, RIFLE[0]: RIFLE}
# A looted tool keeps no part numbers of the staff or rifle it copies: every tool is drawn from one model set.
LOOT_TOOL_PART = 11
RESERVED_PREFIXES = ("am_h_", "am_t_", "am_w_", "am_q_", "am_plat_")


def looted_resrefs() -> list:
    """Every item resref a loot table names, in order of first appearance."""
    found = {}
    for path in sorted(LOOT_TABLES.glob("*.cs")):
        for resref in re.findall(r'\.AddItem\(\s*"([^"]+)"', path.read_text(encoding="utf-8-sig")):
            found.setdefault(resref.lower(), None)
    return list(found)


def loot_resref(source: str) -> str:
    """am_<source>, squeezed to a resource name: underscores go first, then the middle (the last two characters tell
    the pieces of a set apart)."""
    room = 16 - len("am_")
    body = source if len(source) <= room else source.replace("_", "")
    if len(body) > room:
        body = body[:room - 2] + body[-2:]
    return "am_" + body


def loot_name(name: str, base: int, overrides: dict, source: str) -> str:
    if source in overrides:
        return overrides[source]
    module, slot_words = LOOT_MODULES[base]
    words = name.split()
    if len(words) > 1 and words[-1] in slot_words.split():
        return " ".join(words[:-1] + [module])
    return f"{name} {module}"


def loot_twins(taken: list) -> list:
    """The astromech form of every looted armor-slot piece, staff and rifle that has none yet."""
    overrides = json.loads(LOOT_NAMES.read_text(encoding="utf-8"))
    have_form = {t.source for t in taken} | {m.source for m in mod.MODULES} | {t.source for t in tools.TOOLS}
    used = {t.resref for t in taken} | {m.resref for m in mod.MODULES} | {t.resref for t in tools.TOOLS}
    used.add(mod.STARTER_RESREF)
    names = {t.name for t in taken} | {m.name for m in mod.MODULES} | {t.name for t in tools.TOOLS}
    twins = []
    for source in sorted(looted_resrefs()):
        path = UTI / f"{source}.uti.json"
        if source in have_form or source.startswith("am_") or not path.exists():
            continue
        item = json.loads(path.read_bytes().decode("latin-1"))
        base = item["BaseItem"]["value"]
        if base not in LOOT_MODULES:
            continue
        name = loot_name(item["LocalizedName"]["value"]["0"], base, overrides, source)
        resref = loot_resref(source)
        if resref in used or resref.startswith(RESERVED_PREFIXES):
            raise SystemExit(f"{source}: the resref {resref} is taken or reserved")
        if name in names:
            raise SystemExit(f"{source}: the name '{name}' is taken; add one to {LOOT_NAMES.name}")
        if not name.isascii() or '"' in name or "\\" in name:
            raise SystemExit(f"{source}: the name '{name}' is not plain ASCII")
        used.add(resref)
        names.add(name)
        twins.append(Twin(source, resref, name, LOOT_WEAPONS.get(base), looted=True))
    unused = sorted(set(overrides) - {t.source for t in twins})
    if unused:
        raise SystemExit(f"{LOOT_NAMES.name} names pieces that are not looted gear: {', '.join(unused)}")
    return twins


TWINS += loot_twins(TWINS)


# ---------------------------------------------------------------- blueprints

VAR_TABLE = re.compile(r'(  "VarTable": \{\r\n    "type": "list",\r\n    "value": )(\[\]|\[\r\n.*?\r\n    \])(\r\n  \})', re.S)
REVISION_MARKER = "_STATS_REVISION"


def without_revision_markers(text: str) -> str:
    """The blueprint without the local variables that mark a humanoid piece as already carrying revised stats.
    The stat migrations find items by resref, and no astromech form existed before any of them, so a form needs none."""
    table = VAR_TABLE.search(text)
    if not table or table.group(2) == "[]":
        return text
    entries = json.loads(table.group(2))
    kept = [entry for entry in entries if not entry["Name"]["value"].endswith(REVISION_MARKER)]
    if len(kept) == len(entries):
        return text
    body = "[]" if not kept else json.dumps(kept, indent=2, ensure_ascii=False).replace("\n", "\r\n    ")
    return text[:table.start(2)] + body + text[table.end(2):]


def twin_blueprint(twin: Twin) -> str:
    path = UTI / f"{twin.source}.uti.json"
    if not path.exists():
        raise SystemExit(f"missing source blueprint {path}")
    text = mod.read(path)

    name_pattern = re.compile(
        r'  "LocalizedName": \{\r\n(?:    "id": \d+,\r\n)?    "type": "cexolocstring",\r\n    "value": \{\r\n'
        r'(?:      "0": "[^"\r\n]*"\r\n)?    \}\r\n  \},')
    if len(name_pattern.findall(text)) != 1:
        raise SystemExit(f"{twin.source}: LocalizedName has an unexpected shape")
    new_name = ('  "LocalizedName": {\r\n    "type": "cexolocstring",\r\n    "value": {\r\n'
                f'      "0": "{twin.name}"\r\n    }}\r\n  }},')
    text = name_pattern.sub(lambda _m: new_name, text)

    for key, kind in (("Tag", "cexostring"), ("TemplateResRef", "resref")):
        pattern = re.compile(rf'  "{key}": \{{\r\n    "type": "{kind}",\r\n    "value": "[^"\r\n]*"\r\n  \}}')
        if len(pattern.findall(text)) != 1:
            raise SystemExit(f"{twin.source}: {key} has an unexpected shape")
        text = pattern.sub(lambda _m: f'  "{key}": {{\r\n    "type": "{kind}",\r\n    "value": "{twin.resref}"\r\n  }}', text)

    source_skill, new_skill = ARMOR_SKILL, CHASSIS_SKILL
    if twin.weapon:
        source_base, source_skill, new_base = twin.weapon
        new_skill = ARMAMENTS_SKILL
        base_pattern = re.compile(r'(  "BaseItem": \{\r\n    "type": "int",\r\n    "value": )(\d+)(\r\n  \})')
        base = base_pattern.findall(text)
        if len(base) != 1 or int(base[0][1]) != source_base:
            raise SystemExit(f"{twin.source}: BaseItem is not {source_base}")
        text = base_pattern.sub(lambda m: f"{m.group(1)}{new_base}{m.group(3)}", text)

    subtype = r'("Subtype": \{\r\n          "type": "word",\r\n          "value": )%d(\r\n)'
    skills = droid_only = 0
    # Each change keeps the length of the text before it, so the blocks are walked from the last to the first.
    for match in reversed(list(mod.PROPERTY_BLOCK.finditer(text))):
        block = match.group(1)
        found = re.search(r'"PropertyName": \{\r\n          "type": "word",\r\n          "value": (\d+)', block)
        if not found:
            continue  # a list entry that is not an item property (a local variable)
        number = int(found.group(1))
        new_block = block
        if number == mod.RACIAL_PROPERTY:
            # Gear made for the humanoid Droid species: its astromech form is limited to astromechs instead.
            pattern = re.compile(subtype % DROID_RACE)
            if not twin.looted or len(pattern.findall(block)) != 1:
                raise SystemExit(f"{twin.source} already carries a racial limitation")
            new_block = pattern.sub(lambda m: f"{m.group(1)}{mod.ASTROMECH_RACE}{m.group(2)}", block)
            droid_only += 1
        elif number == mod.REQUIRES_SKILL_PROPERTY:
            skills += 1
            pattern = re.compile(subtype % source_skill)
            if len(pattern.findall(block)) == 1:
                new_block = pattern.sub(lambda m: f"{m.group(1)}{new_skill}{m.group(2)}", block)
            elif not (twin.looted and any(re.search(subtype % skill, block) for skill in SHARED_SKILLS)):
                raise SystemExit(f"{twin.source}: RequiresSkill is not skill {source_skill}")
        text = text[:match.start(1)] + new_block + text[match.end(1):]
    if skills != 1 or droid_only > 1:
        raise SystemExit(f"{twin.source}: expected exactly one RequiresSkill property and at most one racial limit")

    if not droid_only:
        tail = '      }\r\n    ]\r\n  },\r\n  "StackSize"'
        if text.count(tail) != 1:
            raise SystemExit(f"{twin.source}: PropertiesList has an unexpected end")
        text = text.replace(tail, '      },\r\n' + mod.NEW_PROPERTY + '\r\n    ]\r\n  },\r\n  "StackSize"')
    if twin.looted and twin.weapon:
        parts = re.compile(r'(  "x?ModelPart[123]": \{\r\n    "type": "(?:byte|word)",\r\n    "value": )\d+(\r\n  \})')
        if len(parts.findall(text)) != 6:
            raise SystemExit(f"{twin.source}: unexpected weapon part fields")
        text = parts.sub(lambda m: f"{m.group(1)}{LOOT_TOOL_PART}{m.group(2)}", text)
    text = without_revision_markers(text)
    # A plating twin keeps the dyes of the piece it copies (they are its paint) but none of its part models.
    return mod.without_part_models(text, twin.source) if mod.ARMOR_MODEL_FIELD.search(text) else text


# ---------------------------------------------------------------- stores

def store_text(current: str, twins: list) -> str:
    data = json.loads(current)
    tabs = [[i["InventoryRes"]["value"] for i in s.get("ItemList", {}).get("value", [])] for s in data["StoreList"]["value"]]
    positions = [[(i["Repos_Posy"]["value"], i["Repos_PosX"]["value"]) for i in s.get("ItemList", {}).get("value", [])]
                 for s in data["StoreList"]["value"]]
    present = {r for tab in tabs for r in tab}
    additions: dict[int, list] = {}
    for twin in twins:
        if twin.resref in present:
            continue
        homes = [i for i, tab in enumerate(tabs) if twin.source in tab]
        if len(homes) != 1:
            raise SystemExit(f"store: {twin.source} is not listed exactly once")
        additions.setdefault(homes[0], []).append(twin.resref)
    if not additions:
        return current

    filled = [i for i, tab in enumerate(tabs) if tab]
    ends = [m.start() for m in re.finditer(r"\r\n            \}\r\n          \]\r\n        \}", current)]
    if len(ends) != len(filled):
        raise SystemExit("store: unexpected tab layout")
    end_of = dict(zip(filled, ends))
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
        at = end_of[tab] + len("\r\n            }")
        result = result[:at] + ",\r\n" + ",\r\n".join(pieces) + result[at:]
    return result


# ---------------------------------------------------------------- the table

def twins_source() -> str:
    pairs = [(m.source, m.resref) for m in mod.MODULES]
    pairs += [(t.source, t.resref) for t in tools.TOOLS]
    pairs += [(t.source, t.resref) for t in TWINS]
    sources = [p[0] for p in pairs]
    if len(set(sources)) != len(sources):
        raise SystemExit("a humanoid item has more than one astromech form")
    lines = [
        "using System.Collections.Generic;",
        "",
        "namespace SWLOR.Game.Server.Service.AstromechService",
        "{",
        "    // Generated by tools/GenerateAstromechRewards.py. Do not edit by hand.",
        "    public static partial class AstromechItemTwins",
        "    {",
        "        private static readonly Dictionary<string, string> Twins = new()",
        "        {",
    ]
    lines += [f'            ["{source}"] = "{resref}",' for source, resref in pairs]
    lines += ["        };", "    }", "}", ""]
    return "\r\n".join(lines)


# ---------------------------------------------------------------- main

def expected_outputs() -> dict:
    outputs = {UTI / f"{t.resref}.uti.json": twin_blueprint(t).encode("latin-1") for t in TWINS}
    for store in sorted({t.store for t in TWINS if t.store}):
        path = UTM / f"{store}.utm.json"
        outputs[path] = store_text(mod.read(path), [t for t in TWINS if t.store == store]).encode("latin-1")
    outputs[TWINS_CS] = twins_source().encode("utf-8")
    # The game opens the copies of a store placed in areas, which hold their own item lists.
    additions = {}
    for twin in TWINS:
        if twin.store:
            additions.setdefault(twin.store, []).append((twin.source, twin.resref))
    outputs.update(mod.placed.stock(additions, lambda resref: outputs[UTI / f"{resref}.uti.json"]))
    return mod.localize(outputs)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    stale = []
    for path, content in expected_outputs().items():
        if path.exists() and path.read_bytes() == content:
            continue
        stale.append(path)
        if not args.check:
            path.write_bytes(content)
    for path in stale:
        print(("stale: " if args.check else "wrote: ") + str(path.relative_to(ROOT)))
    if not stale:
        print("up to date")
    return 1 if args.check and stale else 0


if __name__ == "__main__":
    sys.exit(main())
