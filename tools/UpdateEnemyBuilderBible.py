from __future__ import annotations

import math
import json
import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

WORKBOOK = Path("design/bible/SWLOR Design Bible - Combat Upgrade.xlsx")

MAIN_NS = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
REL_NS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
PKG_REL_NS = "http://schemas.openxmlformats.org/package/2006/relationships"
CONTENT_NS = "http://schemas.openxmlformats.org/package/2006/content-types"
WORKSHEET_REL_TYPE = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"
WORKSHEET_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"

ET.register_namespace("", MAIN_NS)
ET.register_namespace("r", REL_NS)

NEW_SHEETS = [
    "Enemy Builder Guide",
    "Enemy Stat Presets",
    "Enemy Resistance Packages",
    "Enemy Ability Packages",
    "Enemy Modifiers",
    "World NPC Weapon Delays",
    "Enemy Formula Source",
]

MINIMUM_ATTACK_DELAY = 290
ITEM_PROPERTY_DELAY = 98
WEAPON_BASE_ITEM_TYPES = {
    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 18, 22, 28, 31, 32, 33,
    35, 37, 38, 41, 42, 47, 50, 51, 53, 55, 58, 59, 60, 63, 69, 70, 71, 72,
    61, 95, 108, 111, 310, 511, 512, 525, 537,
}
WEAPON_EQUIPMENT_SLOTS = {16, 32, 16384, 32768, 65536}

DIFFICULTIES = {
    "Normal": dict(hp=1.00, resource=1.00, dmg=1.00, ability=0, offense=0, defense=0, evasion=0, resistance=0, delay=1.00),
    "Tough": dict(hp=1.45, resource=1.20, dmg=1.08, ability=1, offense=1, defense=1, evasion=0, resistance=1, delay=0.97),
    "Elite": dict(hp=2.25, resource=1.50, dmg=1.15, ability=2, offense=2, defense=2, evasion=1, resistance=2, delay=0.94),
    "Boss": dict(hp=5.00, resource=2.50, dmg=1.25, ability=3, offense=3, defense=3, evasion=1, resistance=3, delay=0.90),
}

ROLES = {
    "Melee": dict(primary=["MGT"], secondary=["VIT", "AGI"], tertiary=["PER", "WIL"], hp=1.00, stm=1.15, fp=0.20, dmg=1.05, attack=5, force=0, evasion=0, pdef=2, fdef=0, delay=310, counts=(2, 3, 4, 6)),
    "Ranged": dict(primary=["PER"], secondary=["AGI", "VIT"], tertiary=["MGT", "WIL"], hp=0.90, stm=1.00, fp=0.25, dmg=1.00, attack=5, force=0, evasion=3, pdef=0, fdef=0, delay=370, counts=(2, 3, 4, 6)),
    "Force": dict(primary=["WIL"], secondary=["PER", "AGI"], tertiary=["MGT", "VIT"], hp=0.85, stm=0.45, fp=1.25, dmg=0.95, attack=0, force=6, evasion=1, pdef=-1, fdef=5, delay=410, counts=(2, 3, 4, 6)),
    "Controller": dict(primary=["WIL"], secondary=["PER", "AGI"], tertiary=["MGT", "VIT"], hp=0.85, stm=0.70, fp=0.90, dmg=0.80, attack=1, force=3, evasion=2, pdef=-1, fdef=3, delay=410, counts=(2, 3, 4, 6)),
    "Support": dict(primary=["WIL"], secondary=["VIT", "PER"], tertiary=["MGT", "AGI"], hp=0.95, stm=0.80, fp=1.00, dmg=0.75, attack=0, force=3, evasion=0, pdef=0, fdef=2, delay=430, counts=(2, 3, 4, 6)),
    "Tank": dict(primary=["VIT"], secondary=["MGT", "WIL"], tertiary=["PER", "AGI"], hp=1.35, stm=1.05, fp=0.35, dmg=0.85, attack=2, force=0, evasion=-2, pdef=6, fdef=3, delay=470, counts=(2, 3, 4, 6)),
    "Swarm": dict(primary=["AGI"], secondary=["PER"], tertiary=["MGT", "WIL", "VIT"], hp=0.45, stm=0.50, fp=0.15, dmg=0.55, attack=0, force=0, evasion=5, pdef=-4, fdef=-4, delay=290, counts=(1, 2, 3, 4)),
}

RESISTS = ["Fire", "Poison", "Electrical", "Ice", "Mind", "Mobility", "Trauma", "Disruption"]
RESIST_OFFSETS = {
    "Humanoid": dict(Fire=0, Poison=0, Electrical=0, Ice=0, Mind=0, Mobility=0, Trauma=1, Disruption=0, notes="Balanced default for ordinary living, thinking enemies."),
    "Beast": dict(Fire=0, Poison=2, Electrical=0, Ice=0, Mind=-2, Mobility=3, Trauma=4, Disruption=-2, notes="Physical creature package; stronger against trauma and movement control, weaker to mind and disruption."),
    "Droid": dict(Fire=1, Poison=8, Electrical=-3, Ice=1, Mind=8, Mobility=1, Trauma=2, Disruption=-2, notes="Mechanical package; poison and mind effects are poor choices, electrical/disruption are good counters."),
    "Force User": dict(Fire=1, Poison=0, Electrical=1, Ice=1, Mind=5, Mobility=0, Trauma=-1, Disruption=6, notes="Force-trained package; better against mind/disruption pressure, not especially tough against physical trauma."),
    "Aberration": dict(Fire=0, Poison=4, Electrical=0, Ice=0, Mind=6, Mobility=2, Trauma=4, Disruption=2, notes="Mutated, undead, or unnatural package; broadly resilient to control/status pressure."),
    "Elemental Hazard": dict(Fire=8, Poison=4, Electrical=8, Ice=8, Mind=2, Mobility=2, Trauma=2, Disruption=4, notes="Environmental or energy hazard package; use sparingly because it has broad elemental coverage."),
}

MODIFIERS = {
    "None": dict(hp=1.00, dmg=1.00, stm=1.00, fp=1.00, attack=0, force=0, evasion=0, defense=0, resistance=0, delay=1.00, limit="Default. Use this for all migrated World NPC rows.", notes="No exception modifier."),
    "Hard to Hit": dict(hp=0.85, dmg=0.90, stm=1.00, fp=1.00, attack=0, force=0, evasion=8, defense=4, resistance=0, delay=1.00, limit="Rare duelists, shielded enemies, evasive encounter gimmicks.", notes="Pair with lower HP or clear counterplay; do not combine casually with High Durability."),
    "High Damage": dict(hp=0.85, dmg=1.25, stm=1.00, fp=1.00, attack=4, force=4, evasion=0, defense=-1, resistance=0, delay=1.00, limit="Enemies meant to threaten quickly but die normally.", notes="Use fewer of these in one pull."),
    "High Durability": dict(hp=1.40, dmg=0.90, stm=1.10, fp=1.10, attack=-1, force=-1, evasion=-1, defense=5, resistance=2, delay=1.05, limit="Defenders, large creatures, armored enemies.", notes="Avoid pairing with Hard to Hit unless the encounter is intentionally special."),
    "High Resistance": dict(hp=1.00, dmg=0.95, stm=1.00, fp=1.00, attack=0, force=0, evasion=0, defense=0, resistance=10, delay=1.00, limit="Strong theme resistance; players should have alternate damage/status options.", notes="Broad resistance modifier. Use sparingly and document the intended counterplay."),
    "Low Resource": dict(hp=1.00, dmg=0.95, stm=0.55, fp=0.55, attack=0, force=0, evasion=0, defense=0, resistance=0, delay=1.00, limit="Attrition targets or enemies that should run dry.", notes="Best for enemies with dangerous but limited ability pressure."),
    "Glass Cannon": dict(hp=0.65, dmg=1.35, stm=0.90, fp=0.90, attack=5, force=5, evasion=-2, defense=-4, resistance=-2, delay=0.95, limit="High pressure enemies players can remove quickly.", notes="Use in small numbers; avoid stacking with many controllers."),
}

ABILITY_PACKAGES = {
    ("Normal", "Melee"): ["Raking Claws", "Rending Bite"],
    ("Tough", "Melee"): ["Pouncing Strike", "Rending Bite", "Tail Sweep"],
    ("Elite", "Melee"): ["Pouncing Strike", "Mauling Bite", "Tail Sweep", "Terrifying Bellow"],
    ("Boss", "Melee"): ["Mauling Bite", "Bonecrusher Bite", "Tail Sweep", "Terrifying Bellow", "Chitin Guard", "Rupturing Quake"],
    ("Normal", "Ranged"): ["Suppressing Shot", "Precision Shot"],
    ("Tough", "Ranged"): ["Suppressing Shot", "Precision Shot", "Tactical Mark"],
    ("Elite", "Ranged"): ["Tactical Mark", "Precision Shot", "Piercing Quills", "Grenade Burst"],
    ("Boss", "Ranged"): ["Tactical Mark", "Suppressing Shot", "Precision Shot", "Piercing Quills", "Shrapnel Burst", "Overload Shot"],
    ("Normal", "Force"): ["Force Rend", "Mind Spike"],
    ("Tough", "Force"): ["Force Rend", "Mind Spike", "Dark Shock"],
    ("Elite", "Force"): ["Force Rend", "Mind Spike", "Dark Shock", "Dread Wave"],
    ("Boss", "Force"): ["Force Rend", "Mind Spike", "Dark Shock", "Dread Wave", "Inferno Blast", "Chitin Guard"],
    ("Normal", "Controller"): ["Sonic Shriek", "Disorienting Screech"],
    ("Tough", "Controller"): ["Sonic Shriek", "Disorienting Screech", "Tail Sweep"],
    ("Elite", "Controller"): ["Sonic Shriek", "Disorienting Screech", "Tactical Mark", "Crippling Talons"],
    ("Boss", "Controller"): ["Sonic Shriek", "Disorienting Screech", "Terrifying Bellow", "Tactical Mark", "Crippling Talons", "Rupturing Quake"],
    ("Normal", "Support"): ["Iron Carapace", "Savage Roar"],
    ("Tough", "Support"): ["Iron Carapace", "Savage Roar", "Tactical Mark"],
    ("Elite", "Support"): ["Iron Carapace", "Chitin Guard", "Savage Roar", "Tactical Mark"],
    ("Boss", "Support"): ["Iron Carapace", "Chitin Guard", "Savage Roar", "Tactical Mark", "Terrifying Bellow", "Dread Wave"],
    ("Normal", "Tank"): ["Iron Carapace", "Rending Bite"],
    ("Tough", "Tank"): ["Iron Carapace", "Chitin Guard", "Rending Bite"],
    ("Elite", "Tank"): ["Iron Carapace", "Chitin Guard", "Bonecrusher Bite", "Tail Sweep"],
    ("Boss", "Tank"): ["Iron Carapace", "Chitin Guard", "Bonecrusher Bite", "Terrifying Bellow", "Seismic Slam", "Rupturing Quake"],
    ("Normal", "Swarm"): ["Raking Claws"],
    ("Tough", "Swarm"): ["Raking Claws", "Pouncing Strike"],
    ("Elite", "Swarm"): ["Raking Claws", "Pouncing Strike", "Toxic Spit"],
    ("Boss", "Swarm"): ["Raking Claws", "Pouncing Strike", "Toxic Spit", "Venom Spray"],
}

CREATURE_SWAPS = {
    "Humanoid": ["Serrated Slash", "Brutal Bash", "Tactical Mark", "Suppressing Shot", "Precision Shot", "Grenade Burst"],
    "Beast": ["Raking Claws", "Rending Bite", "Pouncing Strike", "Tail Sweep", "Sonic Shriek", "Toxic Spit"],
    "Droid": ["Overload Shot", "Arc Pulse", "Ion Burst", "Target Lock", "Shrapnel Burst", "Static Burst"],
    "Force User": ["Force Rend", "Mind Spike", "Dark Shock", "Dread Wave", "Inferno Blast", "Savage Roar"],
    "Aberration": ["Disorienting Screech", "Toxic Cloud", "Venom Spray", "Barbed Volley", "Chitin Guard", "Rupturing Quake"],
    "Elemental Hazard": ["Scorching Breath", "Frost Spit", "Static Burst", "Toxic Cloud", "Inferno Blast", "Dark Shock"],
}

STYLE_COLORS = {
    "header": "FFD9EAD3",
    "input": "FFFFF2CC",
    "formula": "FFDDEBF7",
    "warning": "FFFCE4D6",
    "source": "FFE7E6E6",
}


def rnd(value: float) -> int:
    return int(math.floor(value + 0.5))


def col_name(index: int) -> str:
    value = ""
    while index:
        index, rem = divmod(index - 1, 26)
        value = chr(65 + rem) + value
    return value


def cell_ref(row: int, col: int) -> str:
    return f"{col_name(col)}{row}"


def q(sheet_name: str) -> str:
    return "'" + sheet_name.replace("'", "''") + "'"


def cell_col(ref: str) -> int:
    result = 0
    for char in re.match(r"([A-Z]+)", ref).group(1):
        result = result * 26 + ord(char) - 64
    return result


def ability_scores(level: int, difficulty: str, role: str) -> dict[str, int]:
    data = ROLES[role]
    base = 8 + rnd(level * 0.30) + DIFFICULTIES[difficulty]["ability"]
    values = {stat: max(6, base - 1) for stat in ["MGT", "PER", "WIL", "VIT", "AGI"]}
    for stat in data["secondary"]:
        values[stat] = base + rnd(level * 0.14) + 2
    for stat in data["primary"]:
        values[stat] = base + rnd(level * 0.25) + 4
    return values


def stat_preset(level: int, difficulty: str, role: str) -> dict[str, object]:
    diff = DIFFICULTIES[difficulty]
    role_data = ROLES[role]
    scores = ability_scores(level, difficulty, role)
    difficulty_index = list(DIFFICULTIES).index(difficulty)
    base_hp = 35 + level * 12 + level * level * 0.18
    base_resource = 8 + level * 1.6
    base_dmg = 5 + level * 1.45
    base_offense = max(0, math.floor(level * 0.35))
    base_evasion = max(0, math.floor(level * 0.20))
    base_defense = max(0, math.floor(level * 0.35))
    row = {
        "Preset Key": f"{level}|{difficulty}|{role}",
        "Level": level,
        "Difficulty": difficulty,
        "Role": role,
        "Ability Count": role_data["counts"][difficulty_index],
        "MGT": scores["MGT"],
        "PER": scores["PER"],
        "WIL": scores["WIL"],
        "VIT": scores["VIT"],
        "AGI": scores["AGI"],
        "HP": max(1, min(30000, rnd(base_hp * diff["hp"] * role_data["hp"]))),
        "STM": max(0, rnd(base_resource * diff["resource"] * role_data["stm"])),
        "FP": max(0, rnd(base_resource * diff["resource"] * role_data["fp"])),
        "DMG": max(1, rnd(base_dmg * diff["dmg"] * role_data["dmg"])),
        "Attack": max(0, base_offense + role_data["attack"] + diff["offense"]),
        "Force Attack": max(0, (base_offense if role_data["force"] > 0 else 0) + role_data["force"] + diff["offense"]),
        "Evasion": max(0, base_evasion + role_data["evasion"] + diff["evasion"]),
        "Physical Defense": max(0, base_defense + role_data["pdef"] + diff["defense"]),
        "Force Defense": max(0, base_defense + role_data["fdef"] + diff["defense"]),
        "Delay": max(MINIMUM_ATTACK_DELAY, rnd(role_data["delay"] * diff["delay"] / 10) * 10),
    }
    return row


def resistance_preset(level: int, difficulty: str, creature_type: str) -> dict[str, object]:
    base = max(0, math.floor(level * 0.18) + DIFFICULTIES[difficulty]["resistance"])
    offsets = RESIST_OFFSETS[creature_type]
    row = {"Resistance Key": f"{level}|{difficulty}|{creature_type}", "Level": level, "Difficulty": difficulty, "Creature Type": creature_type}
    for name in RESISTS:
        row[name] = max(0, min(100, base + offsets[name]))
    row["Notes"] = offsets["notes"]
    return row


def apply_modifier(field: str, value: int, modifier: str) -> int:
    mod = MODIFIERS[modifier]
    if field == "HP":
        return max(1, min(30000, rnd(value * mod["hp"])))
    if field == "STM":
        return max(0, rnd(value * mod["stm"]))
    if field == "FP":
        return max(0, rnd(value * mod["fp"]))
    if field == "DMG":
        return max(1, rnd(value * mod["dmg"]))
    if field == "Attack":
        return max(0, value + mod["attack"])
    if field == "Force Attack":
        return max(0, value + mod["force"])
    if field == "Evasion":
        return max(0, value + mod["evasion"])
    if field in {"Physical Defense", "Force Defense"}:
        return max(0, value + mod["defense"])
    if field in RESISTS:
        return max(0, min(100, value + mod["resistance"]))
    if field == "Delay":
        return max(MINIMUM_ATTACK_DELAY, rnd(value * mod["delay"] / 10) * 10)
    return value


def text_from_cell(cell: ET.Element, shared: list[str]) -> str:
    cell_type = cell.attrib.get("t")
    if cell_type == "s":
        value = cell.find(f"{{{MAIN_NS}}}v")
        return shared[int(value.text)] if value is not None and value.text is not None else ""
    if cell_type == "inlineStr":
        inline = cell.find(f"{{{MAIN_NS}}}is")
        return "".join(t.text or "" for t in inline.iter(f"{{{MAIN_NS}}}t")) if inline is not None else ""
    value = cell.find(f"{{{MAIN_NS}}}v")
    return value.text if value is not None and value.text is not None else ""


def shared_strings(files: dict[str, bytes]) -> list[str]:
    if "xl/sharedStrings.xml" not in files:
        return []
    root = ET.fromstring(files["xl/sharedStrings.xml"])
    return ["".join(t.text or "" for t in si.iter(f"{{{MAIN_NS}}}t")) for si in root.findall(f"{{{MAIN_NS}}}si")]


def old_world_rows(files: dict[str, bytes], target: str) -> list[dict[str, object]]:
    shared = shared_strings(files)
    path = "xl/" + target if not target.startswith("xl/") else target
    root = ET.fromstring(files[path])
    rows = []
    for row in root.find(f"{{{MAIN_NS}}}sheetData").findall(f"{{{MAIN_NS}}}row"):
        cells = {cell_col(c.attrib["r"]): text_from_cell(c, shared) for c in row.findall(f"{{{MAIN_NS}}}c")}
        values = [cells.get(i, "") for i in range(1, 22)]
        area, name = values[0].strip(), values[1].strip()
        if not area or not name or name.upper() in {"NPC", "MGT"} or area == "Source":
            continue
        notes = values[20].strip()
        match = re.search(r"\(UTC:\s*([^\)]+)\)", notes)
        utc = match.group(1).strip() if match else ""
        abilities = re.sub(r"\s*\(UTC:\s*[^\)]+\)\s*", "", notes).strip()
        rows.append(dict(area=area, name=name, utc=utc, level_text=values[18].strip(), abilities=abilities))
    return rows


def is_new_world_format(files: dict[str, bytes], target: str) -> bool:
    shared = shared_strings(files)
    path = "xl/" + target if not target.startswith("xl/") else target
    root = ET.fromstring(files[path])
    first_row = root.find(f"{{{MAIN_NS}}}sheetData").find(f"{{{MAIN_NS}}}row")
    if first_row is None:
        return False
    values = {cell_col(c.attrib["r"]): text_from_cell(c, shared) for c in first_row.findall(f"{{{MAIN_NS}}}c")}
    return values.get(1) == "Area" and values.get(3) == "UTC/ResRef" and values.get(8) == "Modifier"


def converted_world_rows(files: dict[str, bytes], target: str) -> list[dict[str, object]]:
    shared = shared_strings(files)
    path = "xl/" + target if not target.startswith("xl/") else target
    root = ET.fromstring(files[path])
    rows = root.find(f"{{{MAIN_NS}}}sheetData").findall(f"{{{MAIN_NS}}}row")
    if not rows:
        return []
    header_cells = {cell_col(c.attrib["r"]): text_from_cell(c, shared) for c in rows[0].findall(f"{{{MAIN_NS}}}c")}
    headers = {value: index for index, value in header_cells.items()}
    result = []
    required = ["Area", "Enemy Name", "UTC/ResRef", "Level", "Difficulty", "Role", "Creature Type", "Modifier", "Existing Abilities", "Setup Notes"]
    if any(name not in headers for name in required):
        return []
    for row in rows[1:]:
        cells = {cell_col(c.attrib["r"]): text_from_cell(c, shared) for c in row.findall(f"{{{MAIN_NS}}}c")}
        area = cells.get(headers["Area"], "").strip()
        name = cells.get(headers["Enemy Name"], "").strip()
        if not area or not name:
            continue
        level, _ = parse_level(cells.get(headers["Level"], "1"))
        result.append({
            "Area": area,
            "Enemy Name": name,
            "UTC/ResRef": cells.get(headers["UTC/ResRef"], "").strip(),
            "Level": level,
            "Difficulty": cells.get(headers["Difficulty"], "Normal").strip() or "Normal",
            "Role": cells.get(headers["Role"], "Melee").strip() or "Melee",
            "Creature Type": cells.get(headers["Creature Type"], "Humanoid").strip() or "Humanoid",
            "Modifier": cells.get(headers["Modifier"], "None").strip() or "None",
            "Existing Abilities": cells.get(headers["Existing Abilities"], "").strip(),
            "Setup Notes": cells.get(headers["Setup Notes"], "").strip() or "Migrated to preset system; no overrides.",
        })
    return result


def parse_level(level_text: str) -> tuple[int, str]:
    note = ""
    try:
        raw = float(str(level_text).replace(",", "").strip())
    except ValueError:
        raw = 1
        note = f'Old level "{level_text}" could not be read; preset level set to 1.'
    level = rnd(raw)
    if level < 1:
        note = f"Old level {level_text} was below 1; preset level set to 1."
        level = 1
    if level > 100:
        note = f"Old level {level_text} exceeded preset range; preset level capped at 100."
        level = 100
    return level, note


def infer_type(name: str, area: str, utc: str, abilities: str) -> str:
    text = f"{name} {area} {utc} {abilities}".lower()
    if any(token in text for token in ["droid", "drone", "unit", "turret", "bot", "combot", "observation", "warform"]):
        return "Droid"
    if any(token in text for token in ["dark lord", "force witch", "inquisitor", "sorcer", "adept", "apprentice ghost", "temple master", "nephthyra", "sirtu", "valerius"]):
        return "Force User"
    if any(token in text for token in ["zombie", "mutated", "alchemized", "crystal-crazed", "crystal crazed", "corrupted", "ghost viper", "flesheater", "fleshleader", "swamp vines"]):
        return "Aberration"
    humanoid = ["outlaw", "hunter", "ranger", "warrior", "hero", "mandalorian", "tusken", "byysk", "kwi ", "dantari", "sith", "pirate", "jawa", "rodian", "weequay", "chef", "prisoner", "exchange", "gero", "human", "trooper", "soldier", "raider", "scout", "officer", "commander", "medic", "honor guard", "guardian", "marauder"]
    if any(token in text for token in humanoid):
        return "Humanoid"
    beast = ["mynock", "hound", "warocas", "spider", "kinrath", "gimpassa", "raivor", "raptor", "cairnmog", "nashtah", "bug", "slug", "shyrack", "serpent", "hydrus", "aradile", "viper", "tench", "scorchellus", "qion", "womprat", "sandswimmer", "beetle", "demon", "worm", "purbole", "mite", "sprantal", "squell", "ssurian", "turtle", "chirodactyl", "iriaz", "voritor", "gizka", "thune", "bol", "frog", "rancor", "graul", "krayt", "drake", "colicoid", "pelko", "wraid", "kath", "larvae"]
    if any(token in text for token in beast):
        return "Beast"
    if any(token in text for token in ["fire", "ice", "electrical", "elemental", "hazard"]):
        return "Elemental Hazard"
    return "Humanoid"


def infer_difficulty(name: str, level_note: str, abilities: str) -> str:
    text = f"{name} {abilities}".lower()
    normalized = re.sub(r"[_-]+", " ", text)

    def has_any(phrases: list[str]) -> bool:
        for phrase in phrases:
            if " " in phrase:
                if phrase in normalized:
                    return True
            elif re.search(rf"\b{re.escape(phrase)}\b", normalized):
                return True
        return False

    if "capped" in level_note:
        return "Boss"
    if has_any(["boss", "king", "queen", "lord", "chieftain", "broodmother", "ancient", "krayt", "rancor", "graul", "dragon turtle", "sand worm", "prototype", "council", "temple master", "peerless", "dark lord", "warform", "adept", "sand demon", "kinrath queen", "forest king"]):
        return "Boss"
    if has_any(["elite", "leader", "alpha", "champion", "guardian", "honor guard", "commander", "master", "blademaster", "warmonger", "juggernaut", "lieutenant", "shaman", "hero", "fleshleader"]):
        return "Elite"
    if has_any(["veteran", "heavy", "officer", "enforcer", "scout", "marauder", "sorceress", "inquisitor"]):
        return "Tough"
    return "Normal"


def infer_role(name: str, creature_type: str, abilities: str) -> str:
    text = f"{name} {abilities}".lower()
    if "swarm" in text or "larvae" in text or "fodder" in text:
        return "Swarm"
    if any(token in text for token in ["shaman", "medic", "officer"]) and creature_type == "Humanoid":
        return "Support"
    if creature_type == "Force User" or any(token in text for token in ["force rend", "mind spike", "dark shock", "dread wave"]):
        return "Force"
    if any(token in text for token in ["iron carapace", "chitin guard"]) and any(token in text for token in ["guardian", "turtle", "worm", "krayt", "rancor", "graul", "slug", "wraid", "tank"]):
        return "Tank"
    if any(token in text for token in ["ranged", "sniper", "gunner", "rifle", "turret", "scout"]) or any(token in text for token in ["suppressing shot", "precision shot", "piercing quills", "shrapnel burst", "overload shot", "target lock"]):
        return "Ranged"
    if any(token in text for token in ["sonic shriek", "disorienting screech", "terrifying bellow", "crippling talons"]):
        return "Controller"
    if any(token in text for token in ["iron carapace", "chitin guard", "savage roar", "tactical mark"]) and any(token in text for token in ["leader", "alpha", "support"]):
        return "Support"
    return "Melee"


def migrate(rows: list[dict[str, object]]) -> list[dict[str, object]]:
    result = []
    seen = {}
    for row in rows:
        area = row["area"].replace("*", "").strip()
        name = row["name"].strip()
        utc = row["utc"].strip()
        abilities = row["abilities"].strip()
        level, level_note = parse_level(row["level_text"])
        creature_type = infer_type(name, area, utc, abilities)
        difficulty = infer_difficulty(name, level_note, abilities)
        role = infer_role(name, creature_type, abilities)
        key = (area, name, utc)
        seen[key] = seen.get(key, 0) + 1
        display_name = f"{name} ({seen[key]})" if seen[key] > 1 else name
        notes = "Migrated to preset system; no overrides."
        if level_note:
            notes += " " + level_note
        result.append({"Area": area, "Enemy Name": display_name, "UTC/ResRef": utc, "Level": level, "Difficulty": difficulty, "Role": role, "Creature Type": creature_type, "Modifier": "None", "Existing Abilities": abilities, "Setup Notes": notes})
    return result


def ensure_enemy_styles(files: dict[str, bytes]) -> dict[str, int]:
    root = ET.fromstring(files["xl/styles.xml"])
    fills = root.find(f"{{{MAIN_NS}}}fills")
    cell_xfs = root.find(f"{{{MAIN_NS}}}cellXfs")
    if fills is None or cell_xfs is None:
        return {name: 0 for name in STYLE_COLORS}

    def fill_id(color: str) -> int:
        for idx, fill in enumerate(fills.findall(f"{{{MAIN_NS}}}fill")):
            pattern = fill.find(f"{{{MAIN_NS}}}patternFill")
            if pattern is None or pattern.attrib.get("patternType") != "solid":
                continue
            fg = pattern.find(f"{{{MAIN_NS}}}fgColor")
            if fg is not None and fg.attrib.get("rgb") == color:
                return idx
        fill = ET.SubElement(fills, f"{{{MAIN_NS}}}fill")
        pattern = ET.SubElement(fill, f"{{{MAIN_NS}}}patternFill", {"patternType": "solid"})
        ET.SubElement(pattern, f"{{{MAIN_NS}}}fgColor", {"rgb": color})
        ET.SubElement(pattern, f"{{{MAIN_NS}}}bgColor", {"indexed": "64"})
        fills.set("count", str(len(fills.findall(f"{{{MAIN_NS}}}fill"))))
        return len(fills.findall(f"{{{MAIN_NS}}}fill")) - 1

    def xf_id(fill_index: int) -> int:
        for idx, xf in enumerate(cell_xfs.findall(f"{{{MAIN_NS}}}xf")):
            if xf.attrib.get("fillId") == str(fill_index) and xf.attrib.get("applyFill") == "1":
                return idx
        ET.SubElement(cell_xfs, f"{{{MAIN_NS}}}xf", {
            "numFmtId": "0",
            "fontId": "0",
            "fillId": str(fill_index),
            "borderId": "0",
            "xfId": "0",
            "applyFill": "1",
        })
        cell_xfs.set("count", str(len(cell_xfs.findall(f"{{{MAIN_NS}}}xf"))))
        return len(cell_xfs.findall(f"{{{MAIN_NS}}}xf")) - 1

    styles = {name: xf_id(fill_id(color)) for name, color in STYLE_COLORS.items()}
    files["xl/styles.xml"] = ET.tostring(root, encoding="utf-8", xml_declaration=True)
    return styles


def make_cell(row: int, col: int, value=None, formula: str | None = None, cached=None, string_formula: bool = False, style: int | None = None) -> ET.Element:
    attrs = {"r": cell_ref(row, col)}
    if style is not None:
        attrs["s"] = str(style)
    c = ET.Element(f"{{{MAIN_NS}}}c", attrs)
    if formula is not None:
        f = ET.SubElement(c, f"{{{MAIN_NS}}}f")
        f.text = formula[1:] if formula.startswith("=") else formula
        if string_formula:
            c.set("t", "str")
        if cached is not None:
            ET.SubElement(c, f"{{{MAIN_NS}}}v").text = str(cached)
        return c
    if value is None:
        return c
    if isinstance(value, (int, float)) and not isinstance(value, bool):
        ET.SubElement(c, f"{{{MAIN_NS}}}v").text = str(int(value)) if float(value).is_integer() else str(value)
    else:
        c.set("t", "inlineStr")
        inline = ET.SubElement(c, f"{{{MAIN_NS}}}is")
        text = ET.SubElement(inline, f"{{{MAIN_NS}}}t")
        value = str(value)
        if value.strip() != value or "\n" in value:
            text.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
        text.text = value
    return c


def add_data_validations(worksheet: ET.Element, validations: list[dict[str, object]]) -> None:
    if not validations:
        return
    parent = ET.SubElement(worksheet, f"{{{MAIN_NS}}}dataValidations", {"count": str(len(validations))})
    for validation in validations:
        attrs = {
            "type": str(validation["type"]),
            "allowBlank": "1",
            "showErrorMessage": "1",
            "sqref": str(validation["sqref"]),
        }
        if validation.get("operator"):
            attrs["operator"] = str(validation["operator"])
        if validation.get("errorTitle"):
            attrs["errorTitle"] = str(validation["errorTitle"])
        if validation.get("error"):
            attrs["error"] = str(validation["error"])
        node = ET.SubElement(parent, f"{{{MAIN_NS}}}dataValidation", attrs)
        ET.SubElement(node, f"{{{MAIN_NS}}}formula1").text = str(validation["formula1"])
        if "formula2" in validation:
            ET.SubElement(node, f"{{{MAIN_NS}}}formula2").text = str(validation["formula2"])


def list_validation(sqref: str, values: list[str]) -> dict[str, object]:
    return {
        "type": "list",
        "sqref": sqref,
        "formula1": '"' + ",".join(values) + '"',
        "errorTitle": "Choose from the list",
        "error": "Use one of the preset values from the dropdown.",
    }


def level_validation(sqref: str) -> dict[str, object]:
    return {
        "type": "whole",
        "operator": "between",
        "sqref": sqref,
        "formula1": "1",
        "formula2": "100",
        "errorTitle": "Level 1-100",
        "error": "Enemy level must be a whole number from 1 to 100.",
    }


def build_sheet(rows: list[list[object]], formulas=None, widths=None, freeze=1, auto_filter=False, protect=False, cell_styles=None, data_validations=None) -> bytes:
    formulas = formulas or {}
    widths = widths or {}
    cell_styles = cell_styles or {}
    data_validations = data_validations or []
    worksheet = ET.Element(f"{{{MAIN_NS}}}worksheet", {f"xmlns:r": REL_NS})
    if freeze:
        views = ET.SubElement(worksheet, f"{{{MAIN_NS}}}sheetViews")
        view = ET.SubElement(views, f"{{{MAIN_NS}}}sheetView", {"workbookViewId": "0"})
        ET.SubElement(view, f"{{{MAIN_NS}}}pane", {"ySplit": str(freeze), "topLeftCell": f"A{freeze + 1}", "activePane": "bottomLeft", "state": "frozen"})
        ET.SubElement(view, f"{{{MAIN_NS}}}selection", {"pane": "bottomLeft", "activeCell": f"A{freeze + 1}", "sqref": f"A{freeze + 1}"})
    ET.SubElement(worksheet, f"{{{MAIN_NS}}}sheetFormatPr", {"defaultRowHeight": "15"})
    if widths:
        cols = ET.SubElement(worksheet, f"{{{MAIN_NS}}}cols")
        for col, width in sorted(widths.items()):
            ET.SubElement(cols, f"{{{MAIN_NS}}}col", {"min": str(col), "max": str(col), "width": str(width), "customWidth": "1"})
    data = ET.SubElement(worksheet, f"{{{MAIN_NS}}}sheetData")
    max_col = 0
    for r_idx, values in enumerate(rows, 1):
        max_col = max(max_col, len(values))
        row = ET.SubElement(data, f"{{{MAIN_NS}}}row", {"r": str(r_idx)})
        for c_idx, value in enumerate(values, 1):
            if (r_idx, c_idx) in formulas:
                formula, cached, is_string = formulas[(r_idx, c_idx)]
                row.append(make_cell(r_idx, c_idx, formula=formula, cached=cached, string_formula=is_string, style=cell_styles.get((r_idx, c_idx))))
            else:
                row.append(make_cell(r_idx, c_idx, value=value, style=cell_styles.get((r_idx, c_idx))))
    if auto_filter and rows and max_col:
        ET.SubElement(worksheet, f"{{{MAIN_NS}}}autoFilter", {"ref": f"A1:{col_name(max_col)}{len(rows)}"})
    if protect:
        ET.SubElement(worksheet, f"{{{MAIN_NS}}}sheetProtection", {"sheet": "1", "objects": "1", "scenarios": "1"})
    add_data_validations(worksheet, data_validations)
    ET.SubElement(worksheet, f"{{{MAIN_NS}}}pageMargins", {"left": "0.7", "right": "0.7", "top": "0.75", "bottom": "0.75", "header": "0.3", "footer": "0.3"})
    return ET.tostring(worksheet, encoding="utf-8", xml_declaration=True)


def lookup_stat(level_ref: str, difficulty_ref: str, role_ref: str, col: str) -> str:
    return f"INDEX({q('Enemy Stat Presets')}!${col}:${col},MATCH({level_ref}&\"|\"&{difficulty_ref}&\"|\"&{role_ref},{q('Enemy Stat Presets')}!$A:$A,0))"


def lookup_res(level_ref: str, difficulty_ref: str, type_ref: str, col: str) -> str:
    return f"INDEX({q('Enemy Resistance Packages')}!${col}:${col},MATCH({level_ref}&\"|\"&{difficulty_ref}&\"|\"&{type_ref},{q('Enemy Resistance Packages')}!$A:$A,0))"


def lookup_mod(mod_ref: str, col: str) -> str:
    return f"INDEX({q('Enemy Modifiers')}!${col}:${col},MATCH({mod_ref},{q('Enemy Modifiers')}!$A:$A,0))"


def stat_formula(row: int, field: str, col: str) -> str:
    base = lookup_stat(f"$D{row}", f"$E{row}", f"$F{row}", col)
    mod = f"$H{row}"
    if field == "HP":
        return f"ROUND({base}*{lookup_mod(mod, 'B')},0)"
    if field == "DMG":
        return f"ROUND({base}*{lookup_mod(mod, 'C')},0)"
    if field == "STM":
        return f"ROUND({base}*{lookup_mod(mod, 'D')},0)"
    if field == "FP":
        return f"ROUND({base}*{lookup_mod(mod, 'E')},0)"
    if field == "Attack":
        return f"MAX(0,{base}+{lookup_mod(mod, 'F')})"
    if field == "Force Attack":
        return f"MAX(0,{base}+{lookup_mod(mod, 'G')})"
    if field == "Evasion":
        return f"MAX(0,{base}+{lookup_mod(mod, 'H')})"
    if field in {"Physical Defense", "Force Defense"}:
        return f"MAX(0,{base}+{lookup_mod(mod, 'I')})"
    if field == "Delay":
        return f"MAX({MINIMUM_ATTACK_DELAY},ROUND({base}*{lookup_mod(mod, 'K')}/10,0)*10)"
    return base


def world_weapon_delay_formula(row: int) -> str:
    fallback = stat_formula(row, "Delay", "T")
    return f"IFERROR(INDEX({q('World NPC Weapon Delays')}!$D:$D,MATCH($C{row},{q('World NPC Weapon Delays')}!$A:$A,0)),{fallback})"


def res_formula(row: int, col: str) -> str:
    base = lookup_res(f"$D{row}", f"$E{row}", f"$G{row}", col)
    return f"MAX(0,MIN(100,{base}+{lookup_mod(f'$H{row}', 'J')}))"


def stat_rows() -> list[dict[str, object]]:
    return [stat_preset(level, difficulty, role) for level in range(1, 101) for difficulty in DIFFICULTIES for role in ROLES]


def resistance_rows() -> list[dict[str, object]]:
    return [resistance_preset(level, difficulty, creature_type) for level in range(1, 101) for difficulty in DIFFICULTIES for creature_type in RESIST_OFFSETS]


def json_value(data: dict[str, object], field: str):
    value = data.get(field)
    return value.get("value") if isinstance(value, dict) else None


def read_json(path: Path) -> dict[str, object] | None:
    if not path.exists():
        return None
    return json.loads(path.read_text(encoding="utf-8-sig"))


def item_delay_values(item: dict[str, object]) -> list[int]:
    result = []
    for prop in json_value(item, "PropertiesList") or []:
        if json_value(prop, "PropertyName") == ITEM_PROPERTY_DELAY:
            result.append(int(json_value(prop, "CostValue")) * 10)
    return result


def equipped_weapon_delay(utc_resref: str) -> dict[str, object] | None:
    if not utc_resref:
        return None

    utc = read_json(Path("Module/utc") / f"{utc_resref}.utc.json")
    if utc is None:
        return None

    sources = []
    delays = []
    base_items = set()
    for equipped in json_value(utc, "Equip_ItemList") or []:
        equipped_slot = int(equipped.get("__struct_id", -1))
        if equipped_slot not in WEAPON_EQUIPMENT_SLOTS:
            continue

        equipped_resref = json_value(equipped, "EquippedRes")
        if not equipped_resref:
            continue

        item = read_json(Path("Module/uti") / f"{equipped_resref}.uti.json")
        if item is None:
            continue

        base_item = json_value(item, "BaseItem")
        if base_item not in WEAPON_BASE_ITEM_TYPES:
            continue

        item_delays = item_delay_values(item)
        if not item_delays:
            continue

        base_items.add(int(base_item))
        delays.extend(item_delays)
        sources.append(equipped_resref)

    if not delays:
        return None

    unique_delays = sorted(set(delays))
    note = "Equipped weapon/natural attack delay."
    if len(unique_delays) > 1:
        note = "Multiple equipped weapon delays; World NPCs uses the fastest value and the row should be reviewed."

    return {
        "delay": min(unique_delays),
        "sources": ", ".join(dict.fromkeys(sources)),
        "base_items": ", ".join(str(value) for value in sorted(base_items)),
        "notes": note,
    }


def world_npc_weapon_delay_lookup(migrated: list[dict[str, object]]) -> dict[str, dict[str, object]]:
    result = {}
    for npc in migrated:
        utc_resref = str(npc.get("UTC/ResRef", "")).strip()
        if not utc_resref or utc_resref in result:
            continue

        delay = equipped_weapon_delay(utc_resref)
        if delay is not None:
            result[utc_resref] = delay
    return result


def header_styles(row: int, column_count: int, styles: dict[str, int]) -> dict[tuple[int, int], int]:
    return {(row, col): styles["header"] for col in range(1, column_count + 1)}


def sheet_stat_presets(rows: list[dict[str, object]], styles: dict[str, int]) -> bytes:
    headers = ["Preset Key", "Level", "Difficulty", "Role", "Ability Count", "MGT", "PER", "WIL", "VIT", "AGI", "HP", "STM", "FP", "DMG", "Attack", "Force Attack", "Evasion", "Physical Defense", "Force Defense", "Delay"]
    data = [headers] + [[row[h] for h in headers] for row in rows]
    widths = {1: 22, 2: 8, 3: 12, 4: 13, 5: 14}
    widths.update({idx: 12 for idx in range(6, len(headers) + 1)})
    return build_sheet(data, widths=widths, auto_filter=True, cell_styles=header_styles(1, len(headers), styles))


def sheet_resistances(rows: list[dict[str, object]], styles: dict[str, int]) -> bytes:
    headers = ["Resistance Key", "Level", "Difficulty", "Creature Type"] + RESISTS + ["Notes"]
    data = [headers] + [[row[h] for h in headers] for row in rows]
    widths = {1: 28, 2: 8, 3: 12, 4: 18, 13: 70}
    widths.update({idx: 13 for idx in range(5, 13)})
    return build_sheet(data, widths=widths, auto_filter=True, cell_styles=header_styles(1, len(headers), styles))


def sheet_abilities(styles: dict[str, int]) -> bytes:
    headers = ["Package Key", "Difficulty", "Role", "Ability Count", "Ability Package", "Optional Creature Flavor Swaps", "Notes"]
    data = [headers]
    for difficulty in DIFFICULTIES:
        for role in ROLES:
            package = ABILITY_PACKAGES[(difficulty, role)]
            data.append([f"{difficulty}|{role}", difficulty, role, len(package), ", ".join(package), "Use one optional swap from the creature-type list only when the concept needs it.", "Exact package first; optional swaps second."])
    data.append([])
    data.append(["Creature Type", "Approved Optional Swaps", "", "", "", "", ""])
    for creature_type, swaps in CREATURE_SWAPS.items():
        data.append([creature_type, ", ".join(swaps), "", "", "", "", ""])
    cell_styles = header_styles(1, len(headers), styles)
    creature_header_row = 2 + len(DIFFICULTIES) * len(ROLES) + 1
    cell_styles.update(header_styles(creature_header_row, 2, styles))
    return build_sheet(data, widths={1: 22, 2: 12, 3: 13, 4: 14, 5: 90, 6: 55, 7: 40}, auto_filter=True, cell_styles=cell_styles)


def sheet_modifiers(styles: dict[str, int]) -> bytes:
    headers = ["Modifier", "HP Mult", "DMG Mult", "STM Mult", "FP Mult", "Attack Adj", "Force Attack Adj", "Evasion Adj", "Defense Adj", "Resistance Adj", "Delay Mult", "Use Limit", "Notes"]
    data = [headers]
    for name, mod in MODIFIERS.items():
        data.append([name, mod["hp"], mod["dmg"], mod["stm"], mod["fp"], mod["attack"], mod["force"], mod["evasion"], mod["defense"], mod["resistance"], mod["delay"], mod["limit"], mod["notes"]])
    return build_sheet(data, widths={1: 18, 2: 10, 3: 10, 4: 10, 5: 10, 6: 12, 7: 17, 8: 12, 9: 12, 10: 15, 11: 11, 12: 55, 13: 70}, auto_filter=True, cell_styles=header_styles(1, len(headers), styles))


def sheet_world_npc_weapon_delays(weapon_delay_lookup: dict[str, dict[str, object]], styles: dict[str, int]) -> bytes:
    headers = ["UTC/ResRef", "Equipped Source", "Base Item", "Delay", "Notes"]
    data = [headers]
    for utc_resref, details in sorted(weapon_delay_lookup.items()):
        data.append([
            utc_resref,
            details["sources"],
            details["base_items"],
            details["delay"],
            details["notes"],
        ])
    return build_sheet(data, widths={1: 24, 2: 55, 3: 14, 4: 10, 5: 80}, auto_filter=True, cell_styles=header_styles(1, len(headers), styles))


def sheet_formula_source(styles: dict[str, int]) -> bytes:
    data = [
        ["Enemy Formula Source - DO NOT EDIT BUILDER PRESETS DIRECTLY"],
        ["Purpose", "Documents the assumptions used to generate Enemy Stat Presets, Resistance Packages, and World NPC preset output."],
        ["Important", "Fortitude, Will/Willpower saves, and Reflex are removed from the enemy-building workflow. WIL is an ability score, not a save."],
        ["Important", "Creature Type changes resistance defaults and ability flavor only. It does not change core stat math."],
        ["Important", "Difficulty normally increases HP, resources, ability count, and pressure. Hard-to-hit enemies are an approved exception modifier."],
        ["Important", f"Delay values are clamped to {MINIMUM_ATTACK_DELAY}; this keeps Hasten II above the 1.75s practical attack floor."],
        [],
        ["Difficulty", "HP Mult", "Resource Mult", "DMG Mult", "Ability Score Bonus", "Offense Bonus", "Defense Bonus", "Evasion Bonus", "Resistance Bonus", "Delay Mult"],
    ]
    for name, row in DIFFICULTIES.items():
        data.append([name, row["hp"], row["resource"], row["dmg"], row["ability"], row["offense"], row["defense"], row["evasion"], row["resistance"], row["delay"]])
    data += [[], ["Role", "Primary Stats", "Secondary Stats", "Tertiary Stats", "HP Mult", "STM Mult", "FP Mult", "DMG Mult", "Attack Bonus", "Force Attack Bonus", "Evasion Bonus", "Physical Defense Bonus", "Force Defense Bonus", "Base Delay"]]
    for name, row in ROLES.items():
        data.append([name, ", ".join(row["primary"]), ", ".join(row["secondary"]), ", ".join(row["tertiary"]), row["hp"], row["stm"], row["fp"], row["dmg"], row["attack"], row["force"], row["evasion"], row["pdef"], row["fdef"], row["delay"]])
    data += [[], ["Creature Type", "Fire", "Poison", "Electrical", "Ice", "Mind", "Mobility", "Trauma", "Disruption", "Notes"]]
    for name, row in RESIST_OFFSETS.items():
        data.append([name] + [row[col] for col in RESISTS] + [row["notes"]])
    data += [
        [],
        ["Base Formula", "Value"],
        ["Ability score base", "8 + ROUND(Level * 0.30) + Difficulty Ability Score Bonus"],
        ["Primary ability score", "Base + ROUND(Level * 0.25) + 4"],
        ["Secondary ability score", "Base + ROUND(Level * 0.14) + 2"],
        ["Tertiary ability score", "MAX(6, Base - 1)"],
        ["HP base", "35 + Level * 12 + Level^2 * 0.18, then difficulty and role multipliers"],
        ["Resource base", "8 + Level * 1.6, then difficulty and role multipliers"],
        ["DMG base", "5 + Level * 1.45, then difficulty and role multipliers"],
        ["Resistance base", "FLOOR(Level * 0.18) + Difficulty Resistance Bonus + Creature Type Offset"],
        ["Delay base", f"Role Base Delay * Difficulty Delay Mult * Modifier Delay Mult, rounded to 10 and clamped to {MINIMUM_ATTACK_DELAY}."],
        ["World NPC delay", "World NPC rows first use equipped weapon/natural attack Delay from the UTC's UTI source; rows without a weapon source fall back to the preset Delay formula."],
    ]
    widths = {1: 30, 2: 45, 3: 26, 4: 26, 5: 18, 6: 15, 7: 15, 8: 15, 9: 18, 10: 18, 11: 18, 12: 20, 13: 20, 14: 20}
    cell_styles = {(1, 1): styles["warning"], (2, 1): styles["header"], (3, 1): styles["warning"], (4, 1): styles["warning"], (5, 1): styles["warning"], (6, 1): styles["warning"]}
    for row_idx, row in enumerate(data, 1):
        if row and row[0] in {"Difficulty", "Role", "Creature Type", "Base Formula"}:
            cell_styles.update(header_styles(row_idx, len(row), styles))
    return build_sheet(data, widths=widths, freeze=None, cell_styles=cell_styles)


def builder_formula_stat(field: str, col: str) -> str:
    base = lookup_stat("$B$4", "$B$5", "$B$6", col)
    if field == "HP":
        return f"ROUND({base}*{lookup_mod('$B$8', 'B')},0)"
    if field == "DMG":
        return f"ROUND({base}*{lookup_mod('$B$8', 'C')},0)"
    if field == "STM":
        return f"ROUND({base}*{lookup_mod('$B$8', 'D')},0)"
    if field == "FP":
        return f"ROUND({base}*{lookup_mod('$B$8', 'E')},0)"
    if field == "Attack":
        return f"MAX(0,{base}+{lookup_mod('$B$8', 'F')})"
    if field == "Force Attack":
        return f"MAX(0,{base}+{lookup_mod('$B$8', 'G')})"
    if field == "Evasion":
        return f"MAX(0,{base}+{lookup_mod('$B$8', 'H')})"
    if field in {"Physical Defense", "Force Defense"}:
        return f"MAX(0,{base}+{lookup_mod('$B$8', 'I')})"
    if field == "Delay":
        return f"MAX({MINIMUM_ATTACK_DELAY},ROUND({base}*{lookup_mod('$B$8', 'K')}/10,0)*10)"
    return base


def builder_formula_res(col: str) -> str:
    base = lookup_res("$B$4", "$B$5", "$B$7", col)
    return f"MAX(0,MIN(100,{base}+{lookup_mod('$B$8', 'J')}))"


def sheet_builder_guide(stat_lookup, res_lookup, styles: dict[str, int]) -> bytes:
    default = dict(Level=10, Difficulty="Normal", Role="Melee", **{"Creature Type": "Beast"}, Modifier="None")
    stat = stat_lookup[(10, "Normal", "Melee")]
    res = res_lookup[(10, "Normal", "Beast")]
    final = {k: stat[k] for k in ["MGT", "PER", "WIL", "VIT", "AGI", "Ability Count"]}
    for k in ["HP", "STM", "FP", "DMG", "Attack", "Force Attack", "Evasion", "Physical Defense", "Force Defense", "Delay"]:
        final[k] = apply_modifier(k, stat[k], "None")
    for k in RESISTS:
        final[f"{k} Res"] = apply_modifier(k, res[k], "None")
    final["Ability Package"] = ", ".join(ABILITY_PACKAGES[("Normal", "Melee")])

    headers = ["MGT", "PER", "WIL", "VIT", "AGI", "HP", "STM", "FP", "DMG", "Attack", "Force Attack", "Evasion", "Physical Defense", "Force Defense"] + [f"{r} Res" for r in RESISTS] + ["Delay", "Ability Count", "Ability Package", "Skill Override"]
    rows = [
        ["Enemy Builder Guide"],
        ["Use this tab as the entry point. Yellow cells are editable dropdown/input cells. Blue cells are generated values to copy."],
        ["Do not use Fortitude, Will/Willpower save, or Reflex values for combat-upgrade enemy setup. WIL below is an ability score, not a save."],
        ["Level", default["Level"]],
        ["Difficulty", default["Difficulty"]],
        ["Role", default["Role"]],
        ["Creature Type", default["Creature Type"]],
        ["Modifier", default["Modifier"]],
        [],
        headers,
        [""] * len(headers),
        [],
        ["Creature Setup Checklist"],
        [1, "Choose Level, Difficulty, Role, Creature Type, and optional Modifier."],
        [2, "Copy the exact output numbers to the creature skin and weapon/natural attack setup."],
        [3, "Add NPCLevel, NPCHP, STM, FP, Attack/Force Attack, Evasion, Defense, and Resistance item properties as listed."],
        [4, f"Set DMG and Delay on the weapon or natural attack source used by the creature; Delay must not go below {MINIMUM_ATTACK_DELAY}."],
        [5, "Add the listed ability feats. Optional swaps must come from the approved creature-type swap list."],
        [6, "Use no hand-entered stat overrides unless design explicitly approves a future override workflow."],
        [],
        ["Encounter Composition"],
        ["Standard Pull", "2-4 Normal enemies."],
        ["Mixed Pull", "2-3 Normal enemies plus 1 Tough enemy."],
        ["Dangerous Pull", "1 Elite plus 1-3 Normal enemies."],
        ["Boss Encounter", "1 Boss with optional waves, adds, or encounter mechanics."],
        ["Swarm Encounter", "5-8 Swarm enemies with low HP and simple abilities."],
        [],
        ["Testing Checklist"],
        [1, "Spawn the enemy at the intended level."],
        [2, "Test against the expected player count."],
        [3, "Confirm players can understand what the enemy is doing."],
        [4, "Confirm the enemy has enough STM/FP to use its package."],
        [5, "Confirm the fight is not decided by unavoidable chain control."],
        [6, "Record one result: Too Easy, Fair, or Too Hard."],
        [7, "If adjustment is needed, choose another preset, difficulty, role, creature type, or approved modifier."],
    ]
    stat_cols = {"MGT": "F", "PER": "G", "WIL": "H", "VIT": "I", "AGI": "J", "HP": "K", "STM": "L", "FP": "M", "DMG": "N", "Attack": "O", "Force Attack": "P", "Evasion": "Q", "Physical Defense": "R", "Force Defense": "S", "Delay": "T", "Ability Count": "E"}
    res_cols = dict(zip([f"{r} Res" for r in RESISTS], list("EFGHIJKL")))
    formulas = {}
    for idx, header in enumerate(headers, 1):
        if header in stat_cols:
            formula = lookup_stat("$B$4", "$B$5", "$B$6", stat_cols[header]) if header in {"MGT", "PER", "WIL", "VIT", "AGI", "Ability Count"} else builder_formula_stat(header, stat_cols[header])
            formulas[(11, idx)] = (formula, final[header], False)
        elif header in res_cols:
            formulas[(11, idx)] = (builder_formula_res(res_cols[header]), final[header], False)
        elif header == "Ability Package":
            formula = f"INDEX({q('Enemy Ability Packages')}!$E:$E,MATCH($B$5&\"|\"&$B$6,{q('Enemy Ability Packages')}!$A:$A,0))"
            formulas[(11, idx)] = (formula, final["Ability Package"], True)
        elif header == "Skill Override":
            formulas[(11, idx)] = ('"None - leave NPCSkill blank unless design requests a specific skill rank"', "None - leave NPCSkill blank unless design requests a specific skill rank", True)
    widths = {1: 18, 2: 35}
    widths.update({idx: 14 for idx in range(3, len(headers) + 1)})
    widths[len(headers) - 1] = 80
    widths[len(headers)] = 58
    cell_styles = {
        (1, 1): styles["header"],
        (2, 1): styles["source"],
        (3, 1): styles["warning"],
        (13, 1): styles["header"],
        (21, 1): styles["header"],
        (28, 1): styles["header"],
    }
    for row in range(4, 9):
        cell_styles[(row, 1)] = styles["source"]
        cell_styles[(row, 2)] = styles["input"]
    cell_styles.update(header_styles(10, len(headers), styles))
    for col in range(1, len(headers) + 1):
        cell_styles[(11, col)] = styles["formula"]
    validations = [
        level_validation("B4"),
        list_validation("B5", list(DIFFICULTIES.keys())),
        list_validation("B6", list(ROLES.keys())),
        list_validation("B7", list(RESIST_OFFSETS.keys())),
        list_validation("B8", list(MODIFIERS.keys())),
    ]
    return build_sheet(rows, formulas=formulas, widths=widths, freeze=None, cell_styles=cell_styles, data_validations=validations)


def sheet_world_npcs(migrated, stat_lookup, res_lookup, styles: dict[str, int], weapon_delay_lookup: dict[str, dict[str, object]]) -> bytes:
    headers = ["Area", "Enemy Name", "UTC/ResRef", "Level", "Difficulty", "Role", "Creature Type", "Modifier", "MGT", "PER", "WIL", "VIT", "AGI", "HP", "STM", "FP", "DMG", "Attack", "Force Attack", "Evasion", "Physical Defense", "Force Defense", "Fire Res", "Poison Res", "Electrical Res", "Ice Res", "Mind Res", "Mobility Res", "Trauma Res", "Disruption Res", "Skill Override", "Delay", "Ability Count", "Ability Package", "Existing Abilities", "Setup Notes"]
    rows = [headers]
    formulas = {}
    stat_cols = {"MGT": "F", "PER": "G", "WIL": "H", "VIT": "I", "AGI": "J", "HP": "K", "STM": "L", "FP": "M", "DMG": "N", "Attack": "O", "Force Attack": "P", "Evasion": "Q", "Physical Defense": "R", "Force Defense": "S", "Delay": "T", "Ability Count": "E"}
    res_cols = {"Fire Res": "E", "Poison Res": "F", "Electrical Res": "G", "Ice Res": "H", "Mind Res": "I", "Mobility Res": "J", "Trauma Res": "K", "Disruption Res": "L"}
    for row_idx, npc in enumerate(migrated, 2):
        rows.append([npc.get(h, "") for h in headers])
        stat = stat_lookup[(npc["Level"], npc["Difficulty"], npc["Role"])]
        res = res_lookup[(npc["Level"], npc["Difficulty"], npc["Creature Type"])]
        for col_idx, header in enumerate(headers, 1):
            if header == "Delay":
                delay_details = weapon_delay_lookup.get(npc["UTC/ResRef"])
                cached = delay_details["delay"] if delay_details is not None else apply_modifier(header, stat[header], npc["Modifier"])
                formulas[(row_idx, col_idx)] = (world_weapon_delay_formula(row_idx), cached, False)
            elif header in stat_cols:
                if header in {"MGT", "PER", "WIL", "VIT", "AGI", "Ability Count"}:
                    formula = lookup_stat(f"$D{row_idx}", f"$E{row_idx}", f"$F{row_idx}", stat_cols[header])
                    cached = stat[header]
                else:
                    formula = stat_formula(row_idx, header, stat_cols[header])
                    cached = apply_modifier(header, stat[header], npc["Modifier"])
                formulas[(row_idx, col_idx)] = (formula, cached, False)
            elif header in res_cols:
                res_name = header[:-4]
                formulas[(row_idx, col_idx)] = (res_formula(row_idx, res_cols[header]), apply_modifier(res_name, res[res_name], npc["Modifier"]), False)
            elif header == "Ability Package":
                formula = f"INDEX({q('Enemy Ability Packages')}!$E:$E,MATCH($E{row_idx}&\"|\"&$F{row_idx},{q('Enemy Ability Packages')}!$A:$A,0))"
                formulas[(row_idx, col_idx)] = (formula, ", ".join(ABILITY_PACKAGES[(npc["Difficulty"], npc["Role"])]), True)
            elif header == "Skill Override":
                formulas[(row_idx, col_idx)] = ('"None"', "None", True)
    widths = {1: 16, 2: 32, 3: 24, 4: 8, 5: 12, 6: 13, 7: 18, 8: 15, 34: 90, 35: 80, 36: 80}
    widths.update({idx: 12 for idx in range(9, 34)})
    cell_styles = header_styles(1, len(headers), styles)
    for row in range(2, len(rows) + 1):
        for col in list(range(1, 9)) + [35, 36]:
            cell_styles[(row, col)] = styles["input"]
        for col in range(9, 35):
            cell_styles[(row, col)] = styles["formula"]
    last_row = max(2, len(rows))
    validations = [
        level_validation(f"D2:D{last_row}"),
        list_validation(f"E2:E{last_row}", list(DIFFICULTIES.keys())),
        list_validation(f"F2:F{last_row}", list(ROLES.keys())),
        list_validation(f"G2:G{last_row}", list(RESIST_OFFSETS.keys())),
        list_validation(f"H2:H{last_row}", list(MODIFIERS.keys())),
    ]
    return build_sheet(rows, formulas=formulas, widths=widths, auto_filter=True, cell_styles=cell_styles, data_validations=validations)


def update_package(files: dict[str, bytes], payloads: dict[str, bytes]) -> dict[str, bytes]:
    wb = ET.fromstring(files["xl/workbook.xml"])
    rels = ET.fromstring(files["xl/_rels/workbook.xml.rels"])
    content = ET.fromstring(files["[Content_Types].xml"])
    sheets_parent = wb.find(f"{{{MAIN_NS}}}sheets")
    rel_by_id = {rel.attrib["Id"]: rel for rel in rels.findall(f"{{{PKG_REL_NS}}}Relationship")}
    entries = []
    for sheet in list(sheets_parent.findall(f"{{{MAIN_NS}}}sheet")):
        rid = sheet.attrib[f"{{{REL_NS}}}id"]
        entries.append(dict(name=sheet.attrib["name"], sheet_id=int(sheet.attrib["sheetId"]), rid=rid, target=rel_by_id[rid].attrib["Target"], elem=sheet))
    used_nums = {int(m.group(1)) for e in entries if (m := re.search(r"sheet(\d+)\.xml$", e["target"]))}
    used_rels = {int(m.group(1)) for rel in rels.findall(f"{{{PKG_REL_NS}}}Relationship") if (m := re.match(r"rId(\d+)$", rel.attrib["Id"]))}
    used_ids = {e["sheet_id"] for e in entries}

    def next_num(used: set[int]) -> int:
        value = 1
        while value in used:
            value += 1
        used.add(value)
        return value

    by_name = {e["name"]: e for e in entries}
    for name in NEW_SHEETS:
        if name in by_name:
            continue
        sheet_num = next_num(used_nums)
        rid = f"rId{next_num(used_rels)}"
        sheet_id = next_num(used_ids)
        target = f"worksheets/sheet{sheet_num}.xml"
        ET.SubElement(rels, f"{{{PKG_REL_NS}}}Relationship", {"Id": rid, "Type": WORKSHEET_REL_TYPE, "Target": target})
        elem = ET.Element(f"{{{MAIN_NS}}}sheet", {"name": name, "sheetId": str(sheet_id), f"{{{REL_NS}}}id": rid})
        entry = dict(name=name, sheet_id=sheet_id, rid=rid, target=target, elem=elem)
        entries.append(entry)
        by_name[name] = entry

    for sheet in list(sheets_parent.findall(f"{{{MAIN_NS}}}sheet")):
        sheets_parent.remove(sheet)
    ordered = []
    inserted = False
    for entry in entries:
        if entry["name"] in NEW_SHEETS:
            continue
        if entry["name"] == "World NPCs" and not inserted:
            ordered.extend(by_name[name] for name in NEW_SHEETS)
            inserted = True
        ordered.append(entry)
    if not inserted:
        ordered.extend(by_name[name] for name in NEW_SHEETS)
    for entry in ordered:
        sheets_parent.append(entry["elem"])

    existing_overrides = {node.attrib["PartName"] for node in content.findall(f"{{{CONTENT_NS}}}Override")}
    for name in NEW_SHEETS:
        part = "/xl/" + by_name[name]["target"]
        if part not in existing_overrides:
            ET.SubElement(content, f"{{{CONTENT_NS}}}Override", {"PartName": part, "ContentType": WORKSHEET_CONTENT_TYPE})
            existing_overrides.add(part)

    calc = wb.find(f"{{{MAIN_NS}}}calcPr")
    if calc is None:
        calc = ET.SubElement(wb, f"{{{MAIN_NS}}}calcPr")
    calc.set("fullCalcOnLoad", "1")
    calc.set("forceFullCalc", "1")

    files["xl/workbook.xml"] = ET.tostring(wb, encoding="utf-8", xml_declaration=True)
    files["xl/_rels/workbook.xml.rels"] = ET.tostring(rels, encoding="utf-8", xml_declaration=True)
    files["[Content_Types].xml"] = ET.tostring(content, encoding="utf-8", xml_declaration=True)
    for name, payload in payloads.items():
        target = by_name[name]["target"] if name in by_name else next(e["target"] for e in entries if e["name"] == name)
        files["xl/" + target] = payload
    files.pop("xl/calcChain.xml", None)
    return files


def read_xlsx(path: Path) -> dict[str, bytes]:
    with zipfile.ZipFile(path, "r") as source:
        return {name: source.read(name) for name in source.namelist()}


def find_world_target(files: dict[str, bytes]) -> str:
    wb = ET.fromstring(files["xl/workbook.xml"])
    rels = ET.fromstring(files["xl/_rels/workbook.xml.rels"])
    rel_by_id = {rel.attrib["Id"]: rel.attrib["Target"] for rel in rels.findall(f"{{{PKG_REL_NS}}}Relationship")}
    for sheet in wb.find(f"{{{MAIN_NS}}}sheets").findall(f"{{{MAIN_NS}}}sheet"):
        if sheet.attrib["name"] == "World NPCs":
            return rel_by_id[sheet.attrib[f"{{{REL_NS}}}id"]]
    raise RuntimeError("World NPCs sheet not found")


def main() -> None:
    source_workbook = Path(sys.argv[1]) if len(sys.argv) > 1 else WORKBOOK
    files = read_xlsx(WORKBOOK)
    styles = ensure_enemy_styles(files)
    source_files = files if source_workbook == WORKBOOK else read_xlsx(source_workbook)
    source_world_target = find_world_target(source_files)

    if is_new_world_format(source_files, source_world_target):
        migrated = converted_world_rows(source_files, source_world_target)
    else:
        migrated = migrate(old_world_rows(source_files, source_world_target))
    stats = stat_rows()
    resistances = resistance_rows()
    stat_lookup = {(row["Level"], row["Difficulty"], row["Role"]): row for row in stats}
    res_lookup = {(row["Level"], row["Difficulty"], row["Creature Type"]): row for row in resistances}
    weapon_delay_lookup = world_npc_weapon_delay_lookup(migrated)
    payloads = {
        "Enemy Builder Guide": sheet_builder_guide(stat_lookup, res_lookup, styles),
        "Enemy Stat Presets": sheet_stat_presets(stats, styles),
        "Enemy Resistance Packages": sheet_resistances(resistances, styles),
        "Enemy Ability Packages": sheet_abilities(styles),
        "Enemy Modifiers": sheet_modifiers(styles),
        "World NPC Weapon Delays": sheet_world_npc_weapon_delays(weapon_delay_lookup, styles),
        "Enemy Formula Source": sheet_formula_source(styles),
        "World NPCs": sheet_world_npcs(migrated, stat_lookup, res_lookup, styles, weapon_delay_lookup),
    }
    files = update_package(files, payloads)

    tmp = WORKBOOK.with_suffix(".tmp.xlsx")
    with zipfile.ZipFile(tmp, "w", zipfile.ZIP_DEFLATED) as output:
        for name, data in files.items():
            output.writestr(name, data)
    tmp.replace(WORKBOOK)
    print(f"Updated {WORKBOOK}")
    print(f"Migrated {len(migrated)} World NPC rows")
    print(f"Generated {len(stats)} stat rows and {len(resistances)} resistance rows")
    print(f"Matched equipped weapon delays for {len(weapon_delay_lookup)} World NPC UTCs")


if __name__ == "__main__":
    main()
