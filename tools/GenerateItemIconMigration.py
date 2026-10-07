"""Generate old-to-new appearance entries only inside the one-off item migration."""
import csv
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
START = '    // BEGIN GENERATED APPEARANCE MIGRATION'
END = '    // END GENERATED APPEARANCE MIGRATION'


def generate_migration():
    rows = {}
    for name in ('ShipItemIconBindings.csv', 'ItemIconBindings.csv'):
        with (ROOT / 'SWLOR.Game.Server/Readmes' / name).open(newline='', encoding='utf-8-sig') as stream:
            for row in csv.DictReader(stream):
                key = row['ResRef'].lower()
                if key in rows:
                    raise ValueError(f'Duplicate item migration resref: {key}')
                values = tuple(int(row[column]) for column in ('BaseItem', 'OldModel', 'NewModel'))
                if not 0 <= values[1] <= 255 or not 1 <= values[2] <= 254:
                    raise ValueError(f'Invalid native appearance: {key}: {values}')
                rows[key] = (*values, row['OldIcon'], row['InventoryIcon'])
    path = ROOT / 'SWLOR.Game.Server/Feature/MigrationDefinition/ItemIconMigration.cs'
    text = path.read_text(encoding='utf-8-sig')
    if text.count(START) != 1 or text.count(END) != 1:
        raise ValueError('Missing or duplicate migration generation markers')
    lines = [START, '    // Generated from the reviewed ship/general icon bindings by tools/GenerateItemIconMigration.py.',
             '    private static readonly Dictionary<string, (int BaseItem, int OldModel, int NewModel, string OldIcon, string NewIcon)> Models =',
             '        new(StringComparer.OrdinalIgnoreCase)', '        {']
    for resref, values in sorted(rows.items()):
        lines.append(f'            ["{resref}"] = ({values[0]}, {values[1]}, {values[2]}, \"{values[3]}\", \"{values[4]}\"),')
    lines += ['        };', END]
    text = text[:text.index(START)] + '\n'.join(lines) + text[text.index(END) + len(END):]
    path.write_text(text, encoding='utf-8')
    print(f'{len(rows)} appearance entries generated inside ItemIconMigration; no service catalogue.')


if __name__ == '__main__':
    generate_migration()
