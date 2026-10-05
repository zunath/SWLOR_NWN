"""Align documented NPC combat budgets, including native armor and placed copies.

Normal enemies use a minimum 75% hit rate against an unbuffed reference character
with 20 starting Accuracy ability score and 0.4 points per rank (148 at rank 50).
This benchmark needs neither rare equipment nor a temporary accuracy effect.
Workbook updates preserve formulas, styles, and every untouched ZIP entry.
Run with --check-only to audit without changing files.
"""

import argparse
import json
import re
import zipfile
from xml.sax.saxutils import escape
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
BOOK = ROOT / "design/bible/SWLOR Design Bible - Combat Upgrade.xlsx"
NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
REL = "{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id"
SKIN_SLOT = 131072
# Reward families whose final encounters warrant a small group, rather than an
# ordinary Boss preset. Membership is read from each creature's real loot wiring.
GROUP_BOSS_LOOT = {
    "DATHOMIR_CHIRODACTYL_RECIPES", "DANTOOINE_KINRATH_QUEEN_RECIPES",
    "DANTOOINE_BOL_BOSS_RECIPES", "QIONHIVE_BROODMOTHER_RECIPE",
    "TATOOINE_ANCIENT_WORM_STOLEN_GOODS", "FROG_BOSS_RECIPE",
    "KORRIBAN_MASTER_RECIPE", "KORRIBAN_MASTER_RESOURCE",
}
GROUP_MODIFIER = {
    "A": "Loot Boss", "B": 3, "C": 1.5, "D": 2, "E": 2,
    "F": 15, "G": 15, "H": -9, "I": 20, "J": 0, "K": 1,
    "L": "Final bosses with premium recipe, gem and resource reward tables.",
    "M": "Target: 3-4 geared players. Triple Boss HP, 50% more DMG, +15 offense, "
         "+20 Defense and doubled resources. Lower Evasion preserves reliable hits; "
         "difficulty comes from sustained pressure and durability, not misses.",
}


def value(obj, field, default=0):
    return obj.get(field, {}).get("value", default)


def read_text(path):
    data = path.read_bytes()
    try:
        return data.decode("utf-8"), "utf-8"
    except UnicodeDecodeError:
        return data.decode("cp1252"), "cp1252"


def skin_ref(creature):
    return next((value(entry, "EquippedRes", value(entry, "TemplateResRef", "")) for entry in
                 value(creature, "Equip_ItemList", []) if entry["__struct_id"] == SKIN_SLOT), "")


def properties(item, kind):
    return [p for p in value(item, "PropertiesList", []) if value(p, "PropertyName") == kind]


def hp_budget(item):
    return sum(value(p, "CostValue") for p in properties(item, 96))


def base_hp(creature, hp):
    levels = sum(value(c, "ClassLevel") for c in value(creature, "ClassList", []))
    feats = {value(f, "Feat") for f in value(creature, "FeatList", [])}
    bonus = ((value(creature, "Con") - 10) // 2) * levels
    bonus += levels if 40 in feats else 0
    bonus += 20 * sum(754 <= f <= 763 for f in feats)
    result = hp - bonus
    if not 1 <= result <= 32767:
        raise ValueError(f"Native-adjusted base HP {result} is outside the UTC short range")
    return result


def set_scalar(text, field, number):
    pattern = rf'("{re.escape(field)}"\s*:\s*\{{\s*"type"\s*:\s*"[^\"]+"\s*,\s*"value"\s*:\s*)-?\d+'
    if len(re.findall(pattern, text)) != 1:
        raise ValueError(f"Expected exactly one typed {field} field")
    return re.sub(pattern, lambda m: m[1] + str(number), text)


def replace_list(text, field, entries):
    match = re.search(rf'"{re.escape(field)}"\s*:\s*\{{\s*"type"\s*:\s*"list"\s*,\s*"value"\s*:\s*', text)
    if match is None:
        raise ValueError(f"Missing {field} list")
    _, length = json.JSONDecoder().raw_decode(text[match.end():])
    indent = re.search(r'(?m)^( *)"value"', text[match.start():match.end()])[1]
    serialized = json.dumps(entries, indent=2, ensure_ascii=False)
    serialized = serialized.replace("\n", "\n" + indent)
    if "\r\n" in text:
        serialized = serialized.replace("\n", "\r\n")
    return text[:match.end()] + serialized + text[match.end() + length:]


def read_workbook():
    with zipfile.ZipFile(BOOK) as archive:
        entries = [(info, archive.read(info.filename)) for info in archive.infolist()]
    source = {info.filename: data for info, data in entries}
    relationships = {r.attrib["Id"]: r.attrib["Target"].lstrip("/")
                     for r in ET.fromstring(source["xl/_rels/workbook.xml.rels"])}
    shared = []
    if "xl/sharedStrings.xml" in source:
        shared = ["".join(t.itertext()) for t in ET.fromstring(source["xl/sharedStrings.xml"])]
    sheets = {}
    for sheet in ET.fromstring(source["xl/workbook.xml"]).find("m:sheets", NS):
        name = sheet.attrib["name"]
        if name not in ("Enemy Stat Presets", "World NPCs", "Enemy Formula Source", "Enemy Modifiers",
                        "World NPC Weapon Delays"):
            continue
        path = relationships[sheet.attrib[REL]]
        if not path.startswith("xl/"):
            path = "xl/" + path
        text = source[path].decode("utf-8")
        rows = {}
        for row in ET.fromstring(text).findall(".//m:sheetData/m:row", NS):
            cells = {}
            for cell in row:
                column = re.sub(r"\d", "", cell.attrib["r"])
                v = cell.find("m:v", NS)
                result = v.text if v is not None else ""
                if cell.attrib.get("t") == "inlineStr":
                    result = "".join(cell.find("m:is", NS).itertext())
                elif cell.attrib.get("t") == "s":
                    result = shared[int(result)]
                cells[column] = result or ""
            rows[int(row.attrib["r"])] = cells
        sheets[name] = (path, text, rows)
    return entries, sheets


def set_cell_number(text, address, number):
    pattern = rf'(<c\b[^>]*\br="{address}"[^>]*>)(.*?)(</c>)'
    def patch(match):
        content, count = re.subn(r"(<v>)[^<]*(</v>)", lambda v: v[1] + str(number) + v[2], match[2])
        if count != 1:
            raise ValueError(f"Expected a numeric value/cache in {address}")
        return match[1] + content + match[3]
    result, count = re.subn(pattern, patch, text, flags=re.S)
    if count != 1:
        raise ValueError(f"Expected exactly one cell {address}")
    return result


def set_cell_text(text, address, content):
    pattern = rf'(<c\b[^>]*\br="{address}"[^>]*>).*?(</c>)'
    def patch(match):
        opening = re.sub(r'\s+t="[^"]+"', '', match[1])
        return opening[:-1] + ' t="inlineStr"><is><t>' + escape(content) + '</t></is>' + match[2]
    result, count = re.subn(pattern, patch, text, flags=re.S)
    if count != 1:
        raise ValueError(f"Expected exactly one text cell {address}")
    return result


def set_group_modifier(text):
    cells = []
    for column, content in GROUP_MODIFIER.items():
        style_match = re.search(rf'<c\b[^>]*\br="{column}8"[^>]*\bs="(\d+)"', text)
        style = f' s="{style_match[1]}"' if style_match else ''
        body = f'<v>{content}</v>' if isinstance(content, (int, float)) else f'<is><t>{escape(content)}</t></is>'
        cell_type = '' if isinstance(content, (int, float)) else ' t="inlineStr"'
        cells.append(f'<c r="{column}9"{style}{cell_type}>{body}</c>')
    row = '<row r="9">' + ''.join(cells) + '</row>'
    if re.search(r'<row\b[^>]*\br="9"', text):
        result = re.sub(r'<row\b[^>]*\br="9"[^>]*(?:/>|>.*?</row>)', row, text, flags=re.S)
    else:
        result = re.sub(r'(?=<row\b[^>]*\br="21")', lambda _: row, text, count=1)
    return result.replace('ref="$A$1:$M$8"', 'ref="$A$1:$M$9"')


def append_delay_row(text, row, cells, style_row):
    content = []
    for column, value in cells.items():
        style_match = re.search(rf'<c\b[^>]*\br="{column}{style_row}"[^>]*\bs="(\d+)"', text)
        style = f' s="{style_match[1]}"' if style_match else ''
        cell_type = '' if isinstance(value, int) else ' t="inlineStr"'
        body = f'<v>{value}</v>' if isinstance(value, int) else f'<is><t>{escape(value)}</t></is>'
        content.append(f'<c r="{column}{row}"{style}{cell_type}>{body}</c>')
    pattern = rf'(<row\b[^>]*\br="{row}"[^>]*>).*?(</row>)'
    result, count = re.subn(pattern, lambda m: m[1] + ''.join(content) + m[2], text, flags=re.S)
    if count == 0:
        result, count = re.subn(rf'(<row\b[^>]*\br="{row}"[^>]*)/>',
                               lambda m: m[1] + '>' + ''.join(content) + '</row>', text)
    if count != 1:
        raise ValueError(f'Expected empty weapon-delay row {row}')
    return result


def is_group_boss(creature, cells):
    tables = {value(v, "Value", "").split(',')[0] for v in value(creature, "VarTable", [])
              if value(v, "Name", "").startswith("LOOT") and isinstance(value(v, "Value", ""), str)}
    return cells.get("E") == "Boss" and bool(tables & GROUP_BOSS_LOOT)


def rounded(number):
    # Excel ROUND for the non-negative NPC budgets used here.
    return int(number + 0.5)


def normal_evasion(level, agility, existing):
    if level > 50:
        return existing  # These are above the player skill cap, not at-level solo targets.
    # Integer arithmetic avoids floating-point floor errors at rank boundaries.
    accuracy_ability = 20 + min(level, 50) * 2 // 5
    return min(existing, max(0, accuracy_ability - agility))


def set_formula_source_note(text):
    cells = []
    for column, note in (("A", "Normal hit rate"), ("B",
        "At levels 1-50, Normal Evasion is capped at MAX(0, 20 + FLOOR(Level * 0.4) - AGI). "
        "This gives at least 75% unbuffed hit chance with a 20 starting Accuracy ability score "
        "and 0.4 points per rank (148 Accuracy at rank 50), without rare gear or temporary buffs. "
        "Remove native NaturalAC and AC Bonus properties from documented NPCs; their stat skin "
        "already carries the reviewed Evasion budget. Native damage immunity/resistance/reduction "
        "on NPC-only body armor is removed because it duplicates the reviewed Defense budget. "
        "Runtime Evasion for NPCs with an NPCLevel stat budget excludes native AC, while "
        "StatType.Evasion effects remain active. "
        "Tough, Elite, Boss and level 51-100 presets are unchanged; premium loot encounters use "
        "the Loot Boss modifier from Enemy Modifiers.")):
        style_match = re.search(rf'<c\b[^>]*\br="{column}45"[^>]*\bs="(\d+)"', text)
        style = f' s="{style_match[1]}"' if style_match else ""
        cells.append(f'<c r="{column}46"{style} t="inlineStr"><is><t>{escape(note)}</t></is></c>')
    result, count = re.subn(r'(<row\b[^>]*\br="46"[^>]*>).*?(</row>)',
                          lambda m: m[1] + "".join(cells) + m[2], text, flags=re.S)
    if count == 0:
        result, count = re.subn(r'(<row\b[^>]*\br="46"[^>]*)/>',
                              lambda m: m[1] + ">" + "".join(cells) + "</row>", text)
    if count != 1:
        raise ValueError("Expected existing empty/formula-source note row 46")
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    pending = {}
    def save(path, original, updated, encoding):
        if original != updated:
            pending[path] = updated.encode(encoding)

    entries, sheets = read_workbook()
    preset_path, preset_text, preset_rows = sheets["Enemy Stat Presets"]
    preset_lookup = {}
    for row, cells in preset_rows.items():
        if cells.get("C") == "Normal":
            level, agility, old = (int(float(cells[c])) for c in ("B", "J", "Q"))
            new = normal_evasion(level, agility, old)
            if new != old:
                preset_text = set_cell_number(preset_text, f"Q{row}", new)
            preset_lookup[cells["A"]] = new
    world_path, world_text, world_rows = sheets["World NPCs"]
    items = {}
    creatures = {}
    world = {cells["C"]: cells for cells in world_rows.values() if cells.get("C") and cells["C"] != "UTC/ResRef"}
    equipped_refs = set()
    for ref, cells in world.items():
        path = ROOT / "Module/utc" / f"{ref}.utc.json"
        text, encoding = read_text(path)
        creature = json.loads(text)
        text = set_scalar(text, "NaturalAC", 0)
        creatures[ref] = creature
        equipped_refs.update(value(e, "EquippedRes", "") for e in value(creature, "Equip_ItemList", []))
        save(path, read_text(path)[0], text, encoding)

    for ref in equipped_refs:
        path = ROOT / "Module/uti" / f"{ref}.uti.json"
        if not path.exists():
            # Some ambient/DM templates reference retired cosmetic clothing.
            # Stat skins are required and validated below.
            continue
        text, encoding = read_text(path)
        item = json.loads(text)
        # Native armor duplicates the Evasion budget already authored on the stat skin.
        original = value(item, "PropertiesList", [])
        restricted = (value(item, "BaseItem") == 73 or
                      any(value(v, "Name", "") == "NO_ECONOMY" and value(v, "Value") == 1
                          for v in value(item, "VarTable", [])))
        removed = {1}
        if value(item, "BaseItem") == 16 and restricted:
            removed.update((20, 22, 23))
        cleaned = [p for p in original if value(p, "PropertyName") not in removed]
        if cleaned != original:
            if not restricted:
                raise ValueError(f"Refusing to change native armor on player-obtainable equipment {ref}")
            text = replace_list(text, "PropertiesList", cleaned)
            item["PropertiesList"]["value"] = cleaned
        items[ref] = item
        save(path, read_text(path)[0], text, encoding)

    presets = {c['A']: c for c in preset_rows.values() if c.get('A') and c.get('B')}
    bosses = 0
    for row, cells in world_rows.items():
        ref = cells.get("C", "")
        if ref not in creatures or not is_group_boss(creatures[ref], cells):
            continue
        preset = presets[f"{int(float(cells['D']))}|Boss|{cells['F']}"]
        if cells.get('H') != GROUP_MODIFIER['A']:
            world_text = set_cell_text(world_text, f'H{row}', GROUP_MODIFIER['A'])
        for dest, src, modifier, multiply in (
            ('N', 'K', 'B', True), ('O', 'L', 'D', True), ('P', 'M', 'E', True),
            ('Q', 'N', 'C', True), ('R', 'O', 'F', False), ('S', 'P', 'G', False),
            ('T', 'Q', 'H', False), ('U', 'R', 'I', False), ('V', 'S', 'I', False),
        ):
            source = float(preset[src])
            target = max(0, rounded(source * GROUP_MODIFIER[modifier] if multiply
                                    else source + GROUP_MODIFIER[modifier]))
            world_text = set_cell_number(world_text, f'{dest}{row}', target)
            cells[dest] = str(target)
        cells['H'] = GROUP_MODIFIER['A']
        bosses += 1
    # Apply the reviewed group budgets to NPC-only skins and weapon sources.
    skin_columns = {'N': (96, None), 'O': (92, None), 'P': (91, None),
                    'R': (111, None), 'S': (112, None), 'T': (117, None),
                    'U': (94, 1), 'V': (94, 2)}
    for ref, cells in world.items():
        if cells.get('H') != GROUP_MODIFIER['A']:
            continue
        creature = creatures[ref]
        skin = items[skin_ref(creature)]
        for col, (kind, subtype) in skin_columns.items():
            matches = [p for p in properties(skin, kind) if subtype is None or value(p, 'Subtype') == subtype]
            if len(matches) != 1:
                raise ValueError(f'Expected one {col} budget on {ref}')
            matches[0]['CostValue']['value'] = int(cells[col])
        weapons = dict.fromkeys(value(e, 'EquippedRes', '') for e in value(creature, 'Equip_ItemList', [])
                                if e['__struct_id'] in (16, 32, 16384, 32768, 65536)
                                and properties(items.get(value(e, 'EquippedRes', ''), {}), 93))
        total = int(cells['Q'])
        for index, weapon_ref in enumerate(weapons):
            damage = total // len(weapons) + (1 if index < total % len(weapons) else 0)
            matches = properties(items[weapon_ref], 93)
            if len(matches) != 1:
                raise ValueError(f'Expected one DMG property on {weapon_ref}')
            matches[0]['CostValue']['value'] = damage
        hp = int(cells['N'])
        path = ROOT / 'Module/utc' / f'{ref}.utc.json'
        original, encoding = read_text(path)
        text = pending.get(path, original.encode(encoding)).decode(encoding)
        for field, number in (('HitPoints', base_hp(creature, hp)), ('MaxHitPoints', hp), ('CurrentHitPoints', hp)):
            text = set_scalar(text, field, number)
        save(path, original, text, encoding)

    # Weapon cadence is defined by the weapon type. Missing lookup entries must
    # not silently replace that cadence with the role's preset fallback.
    delay_path, delay_text, delay_rows = sheets['World NPC Weapon Delays']
    lookup = {c['A'] for r, c in delay_rows.items() if r > 1 and c.get('A')}
    style_row = max(r for r, c in delay_rows.items() if r > 1 and c.get('A'))
    last_delay_row = style_row
    for row, cells in world_rows.items():
        ref = cells.get('C', '')
        if ref not in creatures:
            continue
        weapons = {value(e, 'EquippedRes', '') for e in value(creatures[ref], 'Equip_ItemList', [])
                   if e['__struct_id'] in (16, 32, 16384, 32768, 65536)
                   and value(items.get(value(e, 'EquippedRes', ''), {}), 'BaseItem') not in (14, 56, 57)
                   and properties(items.get(value(e, 'EquippedRes', ''), {}), 98)}
        if not weapons:
            continue
        delay = max(sum(value(p, 'CostValue') for p in properties(items[w], 98)) * 10 for w in weapons)
        if ref not in lookup:
            bases = sorted({value(items[w], 'BaseItem') for w in weapons})
            last_delay_row += 1
            delay_text = append_delay_row(delay_text, last_delay_row,
                {'A': ref, 'B': ', '.join(sorted(weapons)),
                 'C': bases[0] if len(bases) == 1 else ', '.join(map(str, bases)),
                 'D': delay, 'E': 'Equipped weapon Delay; shields excluded.'}, style_row)
            lookup.add(ref)
        world_text = set_cell_number(world_text, f'AN{row}', delay)
    delay_text = re.sub(r'(<autoFilter\b[^>]*\bref="\$A\$1:\$E\$)\d+',
                        lambda m: m[1] + str(last_delay_row), delay_text)
    # Droids do not bleed. Keep this in the resistance budget, including named
    # rares added after the initial droid corpus was migrated.
    for row, cells in world_rows.items():
        ref = cells.get('C', '')
        if ref not in creatures or cells.get('G') != 'Droid':
            continue
        if int(float(cells['AC'])) != 100:
            adjustment = int(float(cells['AK'])) + 100 - int(float(cells['AC']))
            world_text = set_cell_number(world_text, f'AK{row}', adjustment)
            world_text = set_cell_number(world_text, f'AC{row}', 100)
        skin = items[skin_ref(creatures[ref])]
        matches = [p for p in properties(skin, 133) if value(p, 'Subtype') == 102]
        if len(matches) != 1:
            raise ValueError(f'Expected one Trauma resistance on {ref}')
        matches[0]['CostValue']['value'] = 100
    for ref, item in items.items():
        path = ROOT / 'Module/uti' / f'{ref}.uti.json'
        original, encoding = read_text(path)
        text = pending.get(path, original.encode(encoding)).decode(encoding)
        # Re-serialize only changed property lists, preserving every other field.
        current = json.loads(text)
        if value(current, 'PropertiesList', []) != value(item, 'PropertiesList', []):
            text = replace_list(text, 'PropertiesList', value(item, 'PropertiesList', []))
        save(path, original, text, encoding)

    skin_targets = {}
    tuned = 0
    for row, cells in world_rows.items():
        ref = cells.get("C", "")
        if ref not in creatures or cells.get("E") != "Normal":
            continue
        key = f"{int(float(cells['D']))}|Normal|{cells['F']}"
        target = preset_lookup[key]
        if int(float(cells["T"])) != target:
            world_text = set_cell_number(world_text, f"T{row}", target)
            tuned += 1
        skin = skin_ref(creatures[ref])
        if skin in skin_targets and skin_targets[skin] != target:
            raise ValueError(f"Conflicting Evasion budgets for shared skin {skin}")
        skin_targets[skin] = target
    for ref, target in skin_targets.items():
        path = ROOT / "Module/uti" / f"{ref}.uti.json"
        original, encoding = read_text(path)
        text = pending.get(path, original.encode(encoding)).decode(encoding)
        item = json.loads(text)
        ips = properties(item, 117)
        if len(ips) != 1:
            raise ValueError(f"Expected one Evasion property on {ref}")
        if value(ips[0], "CostValue") != target:
            ips[0]["CostValue"]["value"] = target
            text = replace_list(text, "PropertiesList", value(item, "PropertiesList", []))
        save(path, original, text, encoding)
        items[ref] = item

    placed = 0
    for path in sorted((ROOT / "Module/git").glob("*.git.json")):
        original, encoding = read_text(path)
        match = re.search(r'"Creature List"\s*:\s*\{\s*"type"\s*:\s*"list"\s*,\s*"value"\s*:\s*\[', original)
        if match is None:
            continue
        cursor = match.end()
        edits = []
        while True:
            cursor += len(original[cursor:]) - len(original[cursor:].lstrip())
            if original[cursor] == "]":
                break
            creature, length = json.JSONDecoder().raw_decode(original[cursor:])
            ref = value(creature, "TemplateResRef", "")
            template_skin = skin_ref(creatures[ref]) if ref in creatures else ""
            if ref in creatures and hp_budget(items.get(template_skin, {})) > 0:
                text = original[cursor:cursor + length]
                old_equipment = {e["__struct_id"]: e for e in value(creature, "Equip_ItemList", [])}
                equipment = []
                for entry in value(creatures[ref], "Equip_ItemList", []):
                    slot = entry["__struct_id"]
                    item_ref = value(entry, "EquippedRes", "")
                    if item_ref not in items:
                        # Preserve retired cosmetic clothing if there is no replacement source.
                        if slot in old_equipment:
                            equipment.append(old_equipment[slot])
                        continue
                    blueprint = items[item_ref]
                    item = old_equipment.get(slot)
                    if item is None or value(item, "BaseItem") != value(blueprint, "BaseItem"):
                        item = {k: v for k, v in blueprint.items() if k != "__data_type"}
                        item["__struct_id"] = slot
                    else:
                        item["PropertiesList"] = blueprint["PropertiesList"]
                        item["TemplateResRef"] = blueprint["TemplateResRef"]
                        # Keep authored cosmetic/local data, but propagate the NPC economy opt-out.
                        npc_flag = next((v for v in value(blueprint, "VarTable", [])
                                         if value(v, "Name", "") == "NO_ECONOMY"), None)
                        if npc_flag is not None:
                            variables = [v for v in value(item, "VarTable", [])
                                         if value(v, "Name", "") != "NO_ECONOMY"]
                            item["VarTable"] = {"type": "list", "value": variables + [npc_flag]}
                    equipment.append(item)
                text = replace_list(text, "Equip_ItemList", equipment)
                creature["FeatList"] = creatures[ref]["FeatList"]
                text = replace_list(text, "FeatList", value(creature, "FeatList", []))
                for field in ("Str", "Dex", "Wis", "Con", "Int"):
                    score = value(creatures[ref], field)
                    text = set_scalar(text, field, score)
                    creature[field]["value"] = score
                text = set_scalar(text, "NaturalAC", 0)
                hp = hp_budget(items[template_skin])
                for field, number in (("HitPoints", base_hp(creature, hp)), ("MaxHitPoints", hp), ("CurrentHitPoints", hp)):
                    text = set_scalar(text, field, number)
                if text != original[cursor:cursor + length]:
                    edits.append((cursor, cursor + length, text))
                    placed += 1
            cursor += length
            cursor += len(original[cursor:]) - len(original[cursor:].lstrip())
            if original[cursor] == ",":
                cursor += 1
        text = original
        for start, end, replacement in reversed(edits):
            text = text[:start] + replacement + text[end:]
        save(path, original, text, encoding)

    formula_path, formula_text, _ = sheets["Enemy Formula Source"]
    modifier_path, modifier_text, _ = sheets['Enemy Modifiers']
    replacements = {preset_path: preset_text.encode(), world_path: world_text.encode(),
                    formula_path: set_formula_source_note(formula_text).encode(),
                    modifier_path: set_group_modifier(modifier_text).encode(), delay_path: delay_text.encode()}
    for info, data in entries:
        if info.filename == 'xl/workbook.xml':
            text = data.decode().replace("'Enemy Modifiers'!$A$1:$M$8", "'Enemy Modifiers'!$A$1:$M$9")
            text = re.sub(r"('World NPC Weapon Delays'!\$A\$1:\$E\$)\d+",
                          lambda m: m[1] + str(last_delay_row), text)
            replacements[info.filename] = text.encode()
        elif info.filename.startswith('xl/worksheets/') and info.filename.endswith('.xml'):
            text = replacements.get(info.filename, data).decode()
            text = text.replace('Low Resource,Glass Cannon"</formula1>',
                                'Low Resource,Glass Cannon,Loot Boss"</formula1>')
            if text.encode() != data:
                replacements[info.filename] = text.encode()
    if any(data != replacements.get(info.filename, data) for info, data in entries):
        pending[BOOK] = None
    print(f"Audited {len(world)} World NPCs and {bosses} premium loot boss rows; {tuned} ordinary Evasion profiles and {placed} placed creatures need alignment.")
    print(f"{len(pending)} files {'need changes' if args.check_only else 'updated'}.")
    if args.check_only:
        if pending:
            for path in pending:
                print(path.relative_to(ROOT))
            raise SystemExit(1)
        return
    # Validate the complete plan before applying any mutation.
    for path, data in pending.items():
        if path != BOOK:
            path.write_bytes(data)
    if BOOK in pending:
        temporary = BOOK.with_suffix(".xlsx.tmp")
        with zipfile.ZipFile(temporary, "w") as archive:
            for info, data in entries:
                archive.writestr(info, replacements.get(info.filename, data))
        temporary.replace(BOOK)


if __name__ == "__main__":
    main()
