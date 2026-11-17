"""Generate the astromech forms of humanoid quest rewards and guild store weapons.

An astromech equips only astromech modules and tools, so a quest or guild store that hands out a humanoid necklace,
bracer, staff or rifle would hand it something it cannot use. Each such item gets an astromech form here: a stat copy
with a new name, the Chassis (skill 61) or Armaments (skill 63) rank requirement in place of the Armor, Staff or Rifle
rank, the tool base item for a weapon, and the astromech-only property. No balance number is introduced.

  python tools/GenerateAstromechRewards.py            write everything
  python tools/GenerateAstromechRewards.py --check    write nothing, exit 1 and list what differs

Writes:
  - Module/uti/am_q_*.uti.json (quest reward modules) and am_w_*_cap / am_w_*_h<1..5>.uti.json (reward and guild tools)
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
    def __init__(self, source, resref, name, weapon=None, store=None):
        self.source = source
        self.resref = resref
        self.name = name
        self.weapon = weapon      # None for a module, STAFF or RIFLE for a tool
        self.store = store        # the store blueprint that sells the source, if any
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


# ---------------------------------------------------------------- blueprints

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

    changed = 0
    for match in mod.PROPERTY_BLOCK.finditer(text):
        block = match.group(1)
        number = int(re.search(r'"PropertyName": \{\r\n          "type": "word",\r\n          "value": (\d+)', block).group(1))
        if number == mod.RACIAL_PROPERTY:
            raise SystemExit(f"{twin.source} already carries a racial limitation")
        if number == mod.REQUIRES_SKILL_PROPERTY:
            pattern = re.compile(r'("Subtype": \{\r\n          "type": "word",\r\n          "value": )%d(\r\n)' % source_skill)
            if len(pattern.findall(block)) != 1:
                raise SystemExit(f"{twin.source}: RequiresSkill is not skill {source_skill}")
            new_block = pattern.sub(lambda m: f"{m.group(1)}{new_skill}{m.group(2)}", block)
            text = text[:match.start(1)] + new_block + text[match.end(1):]
            changed += 1
            break
    if changed != 1:
        raise SystemExit(f"{twin.source}: expected exactly one RequiresSkill property of skill {source_skill}")

    tail = '      }\r\n    ]\r\n  },\r\n  "StackSize"'
    if text.count(tail) != 1:
        raise SystemExit(f"{twin.source}: PropertiesList has an unexpected end")
    return text.replace(tail, '      },\r\n' + mod.NEW_PROPERTY + '\r\n    ]\r\n  },\r\n  "StackSize"')


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
