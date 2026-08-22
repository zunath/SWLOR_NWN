#!/usr/bin/env python3
"""Flag non-obtainable item blueprints with NO_ECONOMY=1.

An item that is not obtainable by players through any source, and is not already
excluded by the runtime classifier (creature base type / [NPC] name), would
otherwise leak into player-facing item search. This stamps such blueprints with
the NO_ECONOMY local variable. Obtainability is drawn from every player source
(loot, stores, placed containers, recipe outputs/components, refining, fishing,
quest rewards, training store, starting gear, and CreateItemOnObject literals);
verified that no computed-resref or data-driven item source exists beyond these.

Usage:
    python tools/FlagNpcEconomyItems.py            # stamp any missing flags
    python tools/FlagNpcEconomyItems.py --check     # audit only; exit 1 if any unflagged

The EconomyEquipCoverageTests unit test enforces this in CI; run this script to fix
a failure. Blueprints are edited by pure text insertion (VarTable added in its
alphabetical position), preserving all other bytes. Requires a module repack on deploy.
"""
import glob, json, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CREATURE_BASE = {69, 70, 71, 72, 73}


def read_text(path):
    # surrogateescape preserves any non-UTF-8 bytes (some blueprints use CP-1252).
    with open(path, "rb") as f:
        return f.read().decode("utf-8", "surrogateescape")


def load(path):
    return json.loads(read_text(path))


def collect():
    equipped, obtainable = set(), set()

    def add(s, r):
        if r:
            s.add(r.lower())

    for f in glob.glob(os.path.join(ROOT, "Module", "utc", "*.utc.json")):
        try:
            d = load(f)
        except Exception:
            continue
        for e in d.get("Equip_ItemList", {}).get("value", []):
            add(equipped, e.get("EquippedRes", {}).get("value"))
        for e in d.get("ItemList", {}).get("value", []):
            if e.get("Dropable", {}).get("value") == 1:
                add(obtainable, e.get("InventoryRes", {}).get("value"))

    for sub, pat in (("utm", "*.utm.json"), ("utp", "*.utp.json")):
        for f in glob.glob(os.path.join(ROOT, "Module", sub, pat)):
            try:
                d = load(f)
            except Exception:
                continue
            for st in d.get("StoreList", {}).get("value", []):
                for e in st.get("ItemList", {}).get("value", []):
                    add(obtainable, e.get("InventoryRes", {}).get("value"))
            for e in d.get("ItemList", {}).get("value", []):
                add(obtainable, e.get("InventoryRes", {}).get("value"))

    patterns = [
        r'\.AddItem\(\s*"([^"]+)"', r'\.Resref\(\s*"([^"]+)"', r'\.Component\(\s*"([^"]+)"',
        r'CreateItemOnObject\(\s*"([^"]+)"', r'CopyItemAndModify\(\s*"([^"]+)"',
        r'new\s+ItemReward\(\s*"([^"]+)"', r'\.RewardItem\(\s*"([^"]+)"',
        r'\.AddItemReward\(\s*"([^"]+)"',
        r'RefinedItemResref\s*=\s*"([^"]+)"', r'new\s+TerminalItem\([^,]+,\s*"([^"]+)"',
        r'(?:Named|Schematic|Note|Tool)\(\s*SlicingSourceType\.[^,]+,\s*\d+,\s*"([^"]+)"',
    ]
    # Files whose bare string literals are all item resrefs (attribute-decorated registries).
    literal_registries = (
        "FishType.cs", "FishingRodType.cs", "FishingBaitType.cs",
        "SlicingCacheSmitheryRecipes.cs", "SlicingCacheCookingRecipes.cs", "TraceFuseRecipes.cs",
        "SlicingTerminalFurnitureRecipes.cs", "ConcentratedVenomRecipes.cs",
    )
    for f in glob.glob(os.path.join(ROOT, "SWLOR.Game.Server", "**", "*.cs"), recursive=True):
        if os.path.join(".claude", "worktrees") in f:
            continue
        try:
            s = read_text(f)
        except Exception:
            continue
        for pat in patterns:
            for m in re.findall(pat, s):
                add(obtainable, m)
        if f.endswith(literal_registries):
            for m in re.findall(r'"([a-z0-9_]{2,16})"', s):
                add(obtainable, m)

    for r in ("beast_dna", "beast_egg", "blueprint", "survival_knife",
              "fresh_bread", "dlarproto", "travelers_clothes"):
        add(obtainable, r)

    return equipped, obtainable


def uti_attrs(resref):
    p = os.path.join(ROOT, "Module", "uti", resref + ".uti.json")
    if not os.path.exists(p):
        return None
    try:
        d = load(p)
    except Exception:
        return None
    return d.get("BaseItem", {}).get("value"), (d.get("LocalizedName", {}).get("value", {}).get("0") or "")


def already_restricted(base, name):
    if base in CREATURE_BASE:
        return True
    n = (name or "").strip()
    return not n or n.startswith("[NPC]") or n.startswith("(NPC")


def has_flag(d):
    for e in d.get("VarTable", {}).get("value", []):
        if e.get("Name", {}).get("value") == "NO_ECONOMY":
            return True
    return False


def detect_eol(text):
    crlf = text.count("\r\n")
    return "\r\n" if (text.count("\n") - crlf) == 0 else "\n"


def field_block(eol):
    L = ['  "VarTable": {', '    "type": "list",', '    "value": [', '      {',
         '        "__struct_id": 0,', '        "Name": {', '          "type": "cexostring",',
         '          "value": "NO_ECONOMY"', '        },', '        "Type": {',
         '          "type": "dword",', '          "value": 1', '        },', '        "Value": {',
         '          "type": "int",', '          "value": 1', '        }', '      }', '    ]', '  },']
    return eol.join(L) + eol


def struct_block(eol):
    L = ['      {', '        "__struct_id": 0,', '        "Name": {', '          "type": "cexostring",',
         '          "value": "NO_ECONOMY"', '        },', '        "Type": {', '          "type": "dword",',
         '          "value": 1', '        },', '        "Value": {', '          "type": "int",',
         '          "value": 1', '        }', '      }']
    return eol.join(L)


def stamp(resref):
    p = os.path.join(ROOT, "Module", "uti", resref + ".uti.json")
    text = read_text(p)
    eol = detect_eol(text)
    d = json.loads(text)
    if "VarTable" in d:
        if has_flag(d):
            return False
        vt = text.index('"VarTable"')
        arr = text.index("[", vt)
        close = text.index(eol + "    ]", arr)
        last = text.rindex(eol + "      }", arr, close + 1)
        at = last + len(eol) + len("      }")
        out = text[:at] + "," + eol + struct_block(eol) + text[at:]
    else:
        follow = min(k for k in d if k != "__data_type" and k > "VarTable")
        pos = text.index(eol + '  "' + follow + '":')
        at = pos + len(eol)
        out = text[:at] + field_block(eol) + text[at:]
    with open(p, "wb") as f:
        f.write(out.encode("utf-8", "surrogateescape"))
    return True


def main():
    check = "--check" in sys.argv
    _, obtainable = collect()
    unflagged = []
    for f in sorted(glob.glob(os.path.join(ROOT, "Module", "uti", "*.uti.json"))):
        r = os.path.basename(f)[:-len(".uti.json")].lower()
        if r in obtainable:
            continue
        a = uti_attrs(r)
        if a is None:
            continue
        base, name = a
        if already_restricted(base, name):
            continue
        if not has_flag(load(f)):
            unflagged.append(r)

    if check:
        if unflagged:
            print(f"{len(unflagged)} non-obtainable item(s) missing NO_ECONOMY:")
            for r in unflagged:
                print(" ", r)
            sys.exit(1)
        print("All non-obtainable items are flagged.")
        return

    stamped = sum(1 for r in unflagged if stamp(r))
    print(f"stamped {stamped} blueprint(s) with NO_ECONOMY (obtainable set = {len(obtainable)}).")


if __name__ == "__main__":
    main()
