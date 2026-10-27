"""Space NPC ship presets: every NPC ship creature converted from the ground Enemy Stat Presets by model.npc_ship.

catalog.py writes the result to Feature/SpaceObjectDefinition/RevampNpcShipCatalog.cs.
"""
import json
import re

import model as m

# Hull type (creature tag suffix) -> space role, difficulty, size, weapons (model weapon name -> module tag).
HULLS = {
    "fighter": ("Fighter", "Normal", "Small"),
    "interceptor": ("Interceptor", "Normal", "Small"),
    "bomber": ("Bomber", "Normal", "Small"),
    "gunship": ("Gunship", "Normal", "Medium"),
    "cargo": ("Logistics", "Normal", "Medium"),
    "platform": ("Gunship", "Elite", "Large"),
    "capital": ("Gunship", "Boss", "Capital"),
}
MODULE_FOR = {
    "Laser Cannon": "sm_laser_00", "Ion Cannon": "sm_ion_00", "Beam Cannon": "sm_beam_00",
    "Missile Launcher": "sm_missl_00", "Torpedo": "sm_torp_00", "Turbolaser": "sm_turbo_40",
    "Point-Defense": "sm_pdef_00",
}
MODULE_CYCLE = {"sm_laser_00": 3.0, "sm_ion_00": 3.0, "sm_beam_00": 3.0, "sm_missl_00": 3.0, "sm_torp_00": 18.0,
                "sm_turbo_40": 4.0, "sm_pdef_00": 2.5}
# Faction capitals are all level 65; their legacy hull + shield (relative to the Thranta Corvette's 2000) keeps
# the ladder from corvette to dreadnought. Damage grows with the 0.3 power of that, capped at x2.
FACTION_CAPITAL_SCALE = {"sithcap1": 1.0, "sithcap2": 1.25, "sithcap3": 2.0, "sithcap4": 3.0, "sithcap5": 4.5,
                         "sithcap6": 6.0, "sithcap7": 10.0, "mandocap1": 2.0, "mandocap2": 6.0, "mandocap3": 10.0}
NUMERIC = ("mgt", "per", "wil", "vit", "agi", "hp", "stm", "dmg", "atk", "eva", "pdef", "delay")


def extend_presets(max_level):
    """Ground presets stop at level 50; carry the 45-50 slope on for the level 51+ ships."""
    for (level, diff, role) in list(m.PRESETS):
        if level != 50:
            continue
        p50, p45 = m.PRESETS[(50, diff, role)], m.PRESETS[(45, diff, role)]
        for lv in range(51, max_level + 1):
            m.PRESETS[(lv, diff, role)] = {k: (p50[k] if k == "delay" else p50[k] + (p50[k] - p45[k]) / 5 * (lv - 50))
                                           for k in NUMERIC}


def creature_level(root, tag):
    utc = json.loads((root / "Module" / "utc" / f"{tag}.utc.json").read_text(encoding="utf-8"))
    skin = next(e["EquippedRes"]["value"] for e in utc["Equip_ItemList"]["value"] if e["__struct_id"] == 131072)
    uti = json.loads((root / "Module" / "uti" / f"{skin}.uti.json").read_text(encoding="utf-8"))
    return next(p["CostValue"]["value"] for p in uti["PropertiesList"]["value"] if p["PropertyName"]["value"] == 99)


def hull_type(tag):
    if tag.startswith(("sithcap", "mandocap")):
        return "capital"
    return re.match(r"t\d(\w+)$", tag).group(1)


def entries(root, tags):
    levels = {t: creature_level(root, t) for t in tags}
    extend_presets(max(levels.values()))
    out = []
    for tag in tags:
        role, diff, size = HULLS[hull_type(tag)]
        level = levels[tag]
        scale = FACTION_CAPITAL_SCALE.get(tag, 1.0)
        dmg_scale = min(2.0, scale ** 0.3)
        n = m.npc_ship(role, level, diff, size=size, vs_size="Large", hp_scale=scale)
        weapons = []
        for w in n["weapons"]:
            label = w["name"].replace("NPC ", "")
            key = ("Turbolaser" if label.startswith("Turbolaser") else "Point-Defense" if label.startswith("Point-Defense")
                   else "Torpedo" if label == "Torpedo" else label)
            module = MODULE_FOR[key]
            per_cycle = w["dmg"] * dmg_scale * MODULE_CYCLE[module] / w["cycleSeconds"]
            attribute = n["pilot"]["per"] if w["family"] == "Energy" else n["pilot"]["mgt"]
            weapons.append((module, max(1, round(per_cycle)), w["attack"] - attribute, w["accuracy"], w["rank"]))
        p = n["pilot"]
        out.append(dict(
            CreatureTag=tag, Level=level, Difficulty=diff, Role=role, Size=size,
            Hull=n["maxHull"], Shield=n["maxShield"], ShieldRegenPerSecond=n["shieldRegenPerSecond"],
            # The game adds the pilot's VIT to Defense and AGI x 3 to Evasion when a shot resolves; the model's Defense
            # already holds VIT, so it comes out here.
            Attack=n["attack"], Accuracy=n["accuracy"], Defense=n["defense"] - n["pilot"]["vit"], Evasion=n["evasion"],
            ResistAll=n["resistances"]["em"], Agility=p["agi"], Perception=p["per"], Vitality=p["vit"],
            Willpower=p["wil"], Might=p["mgt"], Weapons=weapons))
    return out


def catalog_cs(header, rows):
    lines = []
    for r in rows:
        weapons = ", ".join(f'new ShipNpcWeapon {{ ModuleTag = "{t}", Dmg = {d}, Attack = {a}, Accuracy = {acc}, Rank = {rk} }}'
                            for t, d, a, acc, rk in r["Weapons"])
        fields = ", ".join(
            f"{k} = ShipSize.{v}" if k == "Size" else f'{k} = "{v}"' if isinstance(v, str) else f"{k} = {v}"
            for k, v in r.items() if k != "Weapons")
        lines.append(f"            new ShipNpcEntry {{ {fields}, Weapons = new[] {{ {weapons} }} }},")
    return (header + "using System.Collections.Generic;\nusing SWLOR.Game.Server.Service.SpaceService;\n\n"
            "namespace SWLOR.Game.Server.Feature.SpaceObjectDefinition\n{\n"
            "    public static class RevampNpcShipCatalog\n    {\n"
            "        public static readonly IReadOnlyList<ShipNpcEntry> Ships = new List<ShipNpcEntry>\n        {\n"
            + "\n".join(lines) + "\n        };\n    }\n}\n")
