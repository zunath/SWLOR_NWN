#!/usr/bin/env python3
"""Generate the astromech skills as mirrors of existing skill families.

An astromech cannot train the humanoid skills, so it has its own. They introduce no balance numbers of their own:
each astromech perk line is a copy of an existing line with new names. This tool is the one place the pairing lives.
It reads the source line's perk and ability definitions, feat and spell rows and strings, and writes the renamed
copies. Run it again after the source family changes; `--check` fails when the copies on disk are out of date.

What it writes for each mirror in `tools/AstromechSkillMirrors.json`:
  - the perk definition file of each line and one ability definition file per active perk (C#)
  - enum values: SkillType, PerkCategoryType, PerkType, FeatType, Spell, RecastGroup
  - rows in feat.2da, spells.2da, CLS_FEAT_FIGHT.2da and iprp_skill.2da, and the feat and skill strings in the TLK
Status effects, visual effects, sounds, animations and consumable items are shared with the source, on purpose: a
mirrored effect and its source are the same effect and must not stack.

Usage (from the repository root):
  python -B tools/GenerateAstromechSkillMirrors.py            # write
  python -B tools/GenerateAstromechSkillMirrors.py --check    # verify, write nothing
After writing, regenerate the binary TLK with nwn_tlk and run the project's feat linkage tools as for any new ability.
"""
import argparse
import io
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / 'SWLOR.Game.Server'
API = ROOT / 'SWLOR.NWN.API'
HAKS = ROOT / 'SWLOR_Haks'
CONFIG = ROOT / 'tools' / 'AstromechSkillMirrors.json'
TLK_OFFSET = 16777216
TOKEN = re.compile(r'"[^"]*"|\S+')
# Spans that name shared code and resources and must survive the rename untouched: status effects, shared
# helpers, and every member of a type other than the ones a mirror gets its own values in.
PROTECTED = re.compile(
    r'\b\w+StatusEffect\b|\b\w+TreatmentAdjustments\b|'
    r'\b(?!(?:PerkType|FeatType|RecastGroup|Spell|PerkCategoryType|SkillType)\.)[A-Z]\w*\.[A-Z]\w*')


class Changes:
    """Collects file writes so --check can compare instead of writing."""

    def __init__(self, check):
        self.check = check
        self.stale = []

    def write_text(self, path, text, newline):
        data = text.replace('\r\n', '\n').replace('\n', newline).encode('utf-8')
        self.write_bytes(path, data)

    def write_bytes(self, path, data):
        current = path.read_bytes() if path.exists() else None
        if current == data:
            return
        if self.check:
            self.stale.append(str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)


def read(path):
    raw = path.read_bytes()
    return raw.decode('utf-8-sig' if raw.startswith(b'\xef\xbb\xbf') else 'utf-8'), '\r\n' if b'\r\n' in raw else '\n'


class TwoDA:
    def __init__(self, name):
        self.path = HAKS / 'sw_2da' / f'{name}.2da'
        raw = self.path.read_bytes()
        self.newline = '\r\n' if b'\r\n' in raw else '\n'
        self.lines = raw.decode('cp1252').split(self.newline)
        self.columns = ['#'] + self.lines[2].split()

    def rows(self):
        return [(i, TOKEN.findall(line)) for i, line in enumerate(self.lines) if i > 2 and line.strip()]

    def find(self, column, value):
        index = self.columns.index(column)
        for line_index, tokens in self.rows():
            if len(tokens) > index and tokens[index] == value:
                return line_index, tokens
        return None

    def by_id(self, row_id):
        return self.find('#', str(row_id))

    def last_id(self):
        return int(self.rows()[-1][1][0])

    def set_row(self, tokens):
        """Replace the row with this id, or append it when it continues the table."""
        existing = self.by_id(tokens[0])
        line = '   '.join(tokens)
        if existing:
            if TOKEN.findall(self.lines[existing[0]]) != tokens:
                self.lines[existing[0]] = line
            return
        if int(tokens[0]) != self.last_id() + 1:
            raise ValueError(f'{self.path.name}: row {tokens[0]} does not continue the table at {self.last_id()}')
        last_line = self.rows()[-1][0]
        self.lines.insert(last_line + 1, line)

    def save(self, changes):
        changes.write_bytes(self.path, self.newline.join(self.lines).encode('cp1252'))


class Tlk:
    def __init__(self):
        self.path = HAKS / 'sw_tlk' / 'sw_tlk.tlk.json'
        self.text, self.newline = read(self.path)
        self.entries = {entry['id']: entry['text'] for entry in json.loads(self.text)['entries']}

    def get(self, strref):
        return self.entries[int(strref) - TLK_OFFSET]

    def set(self, tlk_id, value):
        if tlk_id in self.entries:
            if self.entries[tlk_id] != value:
                pattern = re.compile(r'("id":\s*%d,\s*"text":\s*)"(?:[^"\\]|\\.)*"' % tlk_id)
                self.text, count = pattern.subn(lambda m: m.group(1) + json.dumps(value, ensure_ascii=False), self.text)
                if count != 1:
                    raise ValueError(f'TLK entry {tlk_id} could not be rewritten')
                self.entries[tlk_id] = value
            return
        if tlk_id != max(self.entries) + 1:
            raise ValueError(f'TLK id {tlk_id} does not continue the table at {max(self.entries)}')
        tail = re.search(r'\}(\s*)\]\s*\}\s*$', self.text)
        block = (',' + self.newline + '    {' + self.newline + '      "id": %d,' % tlk_id + self.newline +
                 '      "text": %s' % json.dumps(value, ensure_ascii=False) + self.newline + '    }')
        self.text = self.text[:tail.start()] + '}' + block + self.text[tail.start() + 1:]
        self.entries[tlk_id] = value

    def save(self, changes):
        json.loads(self.text)
        changes.write_text(self.path, self.text, self.newline)


class Renamer:
    """Renames one source family to its mirror in identifiers and in player-facing text."""

    def __init__(self, mirror):
        self.identifiers = []
        self.display = []
        for line in mirror['lines']:
            self.identifiers.append((line['source_category'], line['category']))
            self.display.append((line['source_name'], line['name']))
            for source, perk in line['perks'].items():
                self.identifiers.append((source, perk['id']))
                self.display.append((perk['source_name'], perk['name']))
        self.identifiers.sort(key=lambda pair: -len(pair[0]))
        self.display.sort(key=lambda pair: -len(pair[0]))
        self.skill = (mirror['source_skill'], mirror['skill'])
        self.skill_display = (mirror['source_skill_name'], mirror['skill_name'])
        self.words = mirror.get('words', {})

    def identifier(self, name):
        for source, target in self.identifiers:
            if name.startswith(source):
                return target + name[len(source):]
        raise KeyError(name)

    def text(self, value):
        for source, target in self.display:
            value = value.replace(source, target)
        value = value.replace(self.skill_display[0], self.skill_display[1])
        for source, target in self.words.items():
            value = value.replace(source, target)
        return value

    def code(self, source):
        kept = []

        def protect(match):
            kept.append(match.group(0))
            return '\x00%d\x00' % (len(kept) - 1)

        # String literals carry player-facing text; rename them as text, then protect them.
        def literal(match):
            value = match.group(0)
            if re.fullmatch(r'"[a-z0-9_]+"', value):
                kept.append(value)
            else:
                kept.append(self.text(value))
            return '\x00%d\x00' % (len(kept) - 1)

        source = re.sub(r'"(?:[^"\\\n]|\\.)*"', literal, source)
        # The mirror lives in its own namespace; using directives and the rest of the namespace path are shared.
        source = re.sub(r'(namespace [\w.]*\bAbilityDefinition\.)%s\b' % self.skill[0], r'\g<1>' + self.skill[1], source)
        source = re.sub(r'^(using|namespace) [^\n]*', protect, source, flags=re.M)
        source = PROTECTED.sub(protect, source)
        for old, new in self.identifiers:
            source = re.sub(r'%s(?![a-z])' % re.escape(old), new, source)
        source = re.sub(r'SkillType\.%s\b' % self.skill[0], 'SkillType.' + self.skill[1], source)
        return re.sub('\x00(\\d+)\x00', lambda m: kept[int(m.group(1))], source)


def enum_values(path):
    text, _ = read(path)
    return {name: int(value) for name, value in re.findall(r'^\s*(\w+)\s*=\s*(\d+)\s*,?\s*(?://.*)?$', text, re.M)}


def enum_span(text, path):
    """Offsets of the body of the first enum in a file: after its opening brace and at its closing brace."""
    declaration = re.search(r'\benum\s+\w+[^{]*\{', text)
    if not declaration:
        raise ValueError(f'{path.name}: no enum found')
    depth, position = 1, declaration.end()
    while depth:
        if text[position] == '{':
            depth += 1
        elif text[position] == '}':
            depth -= 1
        position += 1
    return declaration.end(), position - 1


def add_enum_values(path, entries, changes, before=None):
    """Insert enum members (each a list of source lines) at the end of the enum, or ahead of a marker line."""
    text, newline = read(path)
    existing = enum_values(path)
    block = ''
    for name, value, lines in entries:
        if name in existing:
            if existing[name] != value:
                raise ValueError(f'{path.name}: {name} is {existing[name]}, expected {value}')
            continue
        if value in existing.values():
            raise ValueError(f'{path.name}: id {value} for {name} is already taken')
        block += ''.join(line + '\n' for line in lines)
    if not block:
        return
    normalized = text.replace('\r\n', '\n')
    _, closing = enum_span(normalized, path)
    if before and before in normalized[:closing]:
        position = normalized.rfind('\n', 0, normalized.index(before)) + 1
        head = normalized[:position].rstrip()
        tail = normalized[position:]
        if not head.endswith(','):
            head += ','
        normalized = head + '\n' + block + '\n' + tail
    else:
        position = normalized.rfind('\n', 0, closing) + 1
        head = normalized[:position].rstrip()
        if not head.endswith(',') and not head.endswith('{'):
            head += ','
        normalized = head + '\n' + block + normalized[position:]
    changes.write_text(path, normalized, newline)


def generate(mirror, state, changes):
    renamer = Renamer(mirror)
    feat_table, spell_table, class_table = state['feat'], state['spells'], state['class']
    tlk = state['tlk']
    ledger = state['ledger'].setdefault(mirror['skill'], {})
    indent = '        '

    # ---- source feats: every feat.2da row whose label belongs to a mirrored perk
    perk_sources = [source for line in mirror['lines'] for source in line['perks']]
    feats = []
    for _, tokens in feat_table.rows():
        label = tokens[1]
        match = re.fullmatch(r'(%s)(\d+|Trait)' % '|'.join(sorted(perk_sources, key=len, reverse=True)), label)
        if match:
            feats.append((label, tokens))
    if not feats:
        raise ValueError(f"{mirror['skill']}: no source feats found")

    next_feat = mirror['ids']['feat']
    next_spell = mirror['ids']['spell']
    feat_enum, spell_enum = [], []
    for label, tokens in feats:
        new_label = renamer.identifier(label)
        entry = ledger.setdefault(new_label, {})
        if 'feat' not in entry:
            entry['feat'] = next_feat + sum(1 for e in ledger.values() if 'feat' in e)
        feat_id = entry['feat']
        columns = feat_table.columns
        row = list(tokens)
        row[0] = str(feat_id)
        row[columns.index('LABEL')] = new_label
        for column, key in (('FEAT', 'name'), ('DESCRIPTION', 'description')):
            source_text = tlk.get(tokens[columns.index(column)])
            if key not in entry:
                entry[key] = max(max(tlk.entries), state['tlk_high']) + 1
                state['tlk_high'] = entry[key]
            tlk.set(entry[key], renamer.text(source_text))
            row[columns.index(column)] = str(TLK_OFFSET + entry[key])
        spell_id = tokens[columns.index('SPELLID')]
        if spell_id != '****':
            source_spell = spell_table.by_id(spell_id)[1]
            if 'spell' not in entry:
                entry['spell'] = next_spell + len([e for e in ledger.values() if 'spell' in e])
            spell_columns = spell_table.columns
            spell_row = list(source_spell)
            spell_row[0] = str(entry['spell'])
            spell_row[spell_columns.index('Label')] = new_label
            spell_row[spell_columns.index('Name')] = row[columns.index('FEAT')]
            spell_row[spell_columns.index('SpellDesc')] = row[columns.index('DESCRIPTION')]
            spell_row[spell_columns.index('FeatID')] = str(feat_id)
            spell_table.set_row(spell_row)
            row[columns.index('SPELLID')] = str(entry['spell'])
            spell_enum.append((new_label, entry['spell'], [f'{indent}{new_label} = {entry["spell"]},']))
        feat_table.set_row(row)
        feat_enum.append((new_label, feat_id, [f'{indent}{new_label} = {feat_id},']))
        source_class = class_table.find('FeatLabel', label)
        if source_class and not class_table.find('FeatLabel', new_label):
            class_row = list(source_class[1])
            class_row[0] = str(class_table.last_id() + 1)
            class_row[1] = new_label
            class_row[2] = str(feat_id)
            class_table.set_row(class_row)

    add_enum_values(API / 'NWScript/Enum/FeatType.cs', feat_enum, changes)
    add_enum_values(API / 'NWScript/Enum/spell.cs', spell_enum, changes)

    # ---- skill, categories, perks, recast groups
    skill_text, _ = read(SERVER / 'Service/SkillService/SkillType.cs')
    source_attribute = re.search(r'(        \[Skill\((?:(?!\[Skill\().)*?\)\]\n)        %s = \d+,' % mirror['source_skill'],
                                 skill_text.replace('\r\n', '\n'), re.S).group(1)
    attribute = source_attribute.replace(f'"{mirror["source_skill_name"]}"', f'"{mirror["skill_name"]}"')
    attribute = re.sub(r'"Ability to [^"]*"', json.dumps(mirror['skill_description']), attribute)
    attribute = attribute.replace('BodyFrameRestriction.HumanoidOnly', 'BodyFrameRestriction.AstromechOnly')
    if 'AstromechOnly' not in attribute:
        raise ValueError('the source skill carries no body frame restriction to swap')
    add_enum_values(SERVER / 'Service/SkillService/SkillType.cs',
                    [(mirror['skill'], mirror['ids']['skill'],
                      [''] + attribute.rstrip('\n').split('\n') + [f'{indent}{mirror["skill"]} = {mirror["ids"]["skill"]},'])],
                    changes)
    skill_table = state['skill']
    skill_entry = ledger.setdefault('#skill', {})
    if 'name' not in skill_entry:
        skill_entry['name'] = max(max(tlk.entries), state['tlk_high']) + 1
        state['tlk_high'] = skill_entry['name']
    tlk.set(skill_entry['name'], mirror['skill_name'])
    skill_table.set_row([str(mirror['ids']['skill']), str(TLK_OFFSET + skill_entry['name']), mirror['skill']])

    categories, perk_enum, recast_enum = [], [], []
    recast_text, _ = read(SERVER / 'Service/AbilityService/RecastGroup.cs')
    recast_text = recast_text.replace('\r\n', '\n')
    perk_id = mirror['ids']['perk']
    recast_id = mirror['ids']['recast']
    for line in mirror['lines']:
        categories.append((line['category'], line['category_id'],
                           [f'{indent}[PerkCategory("{mirror["skill_name"]} - {line["name"]}", true, '
                            'BodyFrameRestriction = BodyFrameRestriction.AstromechOnly)]',
                            f'{indent}{line["category"]} = {line["category_id"]},']))
        for source, perk in line['perks'].items():
            perk_enum.append((perk['id'], perk_id, [f'{indent}{perk["id"]} = {perk_id},']))
            perk_id += 1
            if re.search(r'\n\s*%s = \d+,' % source, recast_text):
                short = perk.get('short', perk['name'])
                if len(short) > 14:
                    raise ValueError(f'{perk["name"]}: give a short recast name of at most 14 characters')
                recast_enum.append((perk['id'], recast_id,
                                    [f'{indent}[RecastGroup("{perk["name"]}", "{short}", true)]',
                                     f'{indent}{perk["id"]} = {recast_id},']))
                recast_id += 1
    add_enum_values(SERVER / 'Service/PerkService/PerkCategoryType.cs', categories, changes)
    add_enum_values(SERVER / 'Service/PerkService/PerkType.cs', perk_enum, changes, before='// IDs 650,')
    add_enum_values(SERVER / 'Service/AbilityService/RecastGroup.cs', recast_enum, changes)

    # ---- C# definitions
    for line in mirror['lines']:
        source_path = SERVER / 'Feature/PerkDefinition' / f'{line["source_category"]}PerkDefinition.cs'
        text, newline = read(source_path)
        text = renamer.code(text.replace('\r\n', '\n'))
        text = re.sub(r'\n\s*\.DroidAISlots\(\d+\)', '', text)
        text = HEADER + text
        changes.write_text(SERVER / 'Feature/PerkDefinition' / f'{line["category"]}PerkDefinition.cs', text, newline)
        for source in line['perks']:
            source_ability = SERVER / 'Feature/AbilityDefinition' / mirror['source_skill'] / f'{source}AbilityDefinition.cs'
            if not source_ability.exists():
                continue
            text, newline = read(source_ability)
            text = renamer.code(text.replace('\r\n', '\n'))
            # Shared helpers stay in the source family's namespace, and credit the mirror's own skill.
            text = text.replace('\n\nnamespace ', '\nusing SWLOR.Game.Server.Feature.AbilityDefinition.%s;\n\nnamespace '
                                % mirror['source_skill'], 1)
            text = text.replace('TreatmentAdjustments.GrantCombatPoint(',
                                'TreatmentAdjustments.GrantCombatPointFor(SkillType.%s, ' % mirror['skill'])
            text = HEADER + chassis_animation(text, mirror['animation'])
            target = SERVER / 'Feature/AbilityDefinition' / mirror['skill'] / f'{renamer.identifier(source)}AbilityDefinition.cs'
            changes.write_text(target, text, newline)

    # ---- engine tests: one behaviour case per ability feat, and the perk coverage of each line
    engine = ROOT / 'SWLOR.Game.Server.EngineTests' / 'Definitions'
    sources = [(engine / 'AbilityBehaviors' / f'{mirror["source_skill"]}AbilityBehaviors.cs',
                engine / 'AbilityBehaviors' / f'{mirror["skill"]}AbilityBehaviors.cs')]
    for line in mirror['lines']:
        sources.append((engine / 'PerkCoverage' / f'{line["source_category"]}PerkCoverage.cs',
                        engine / 'PerkCoverage' / f'{line["category"]}PerkCoverage.cs'))
    for source_path, target in sources:
        if not source_path.exists():
            continue
        text, newline = read(source_path)
        text = renamer.code(text.replace('\r\n', '\n'))
        text = text.replace(mirror['source_skill'] + 'AbilityBehaviors', mirror['skill'] + 'AbilityBehaviors')
        changes.write_text(target, HEADER + text, newline)


def chassis_animation(text, animation):
    """An astromech has no authored humanoid clips: every mirrored ability plays one chassis clip instead."""
    def rewrite(match):
        statement = match.group(0)
        authored = re.compile(r'\n\s*\.Uses(?:Immediate)?AuthoredAnimation(?:AtImpact)?\(\)')
        native = re.compile(r'\.UsesAnimation\(Animation\.\w+\)')
        if native.search(statement):
            statement = authored.sub('', statement)
            return native.sub(f'.UsesAnimation(Animation.{animation})', statement)
        first = authored.search(statement)
        if not first:
            return statement
        indent = re.match(r'\n\s*', first.group(0)).group(0)
        statement = statement[:first.start()] + f'{indent}.UsesAnimation(Animation.{animation})' + statement[first.end():]
        return authored.sub('', statement)

    return re.sub(r'\.Create\(FeatType\.[^;]*;', rewrite, text)


HEADER = ('// Generated by tools/GenerateAstromechSkillMirrors.py from the humanoid skill family this mirrors.\n'
          '// Do not edit by hand: change the source family or the tool, then run the tool again.\n')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args(argv)
    config = json.loads(CONFIG.read_text(encoding='utf-8'))
    changes = Changes(args.check)
    state = {'feat': TwoDA('feat'), 'spells': TwoDA('spells'), 'class': TwoDA('CLS_FEAT_FIGHT'), 'skill': TwoDA('iprp_skill'),
             'tlk': Tlk(), 'ledger': config.setdefault('ledger', {}), 'tlk_high': 0}
    for mirror in config['mirrors']:
        generate(mirror, state, changes)
    for key in ('feat', 'spells', 'class', 'skill'):
        state[key].save(changes)
    state['tlk'].save(changes)
    changes.write_text(CONFIG, json.dumps(config, indent=1) + '\n', '\n')
    if args.check:
        if changes.stale:
            print('Out of date; run tools/GenerateAstromechSkillMirrors.py:', file=sys.stderr)
            for path in changes.stale:
                print('  ' + path, file=sys.stderr)
            return 1
        print('Astromech skill mirrors are up to date.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
