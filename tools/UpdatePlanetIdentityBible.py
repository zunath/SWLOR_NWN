"""Write the planet identity recipe rows into the Design Bible's Cooking Recipes and Fabrication Recipes tabs at the zip/XML level.

usage: python tools/UpdatePlanetIdentityBible.py [--check] [--workbook PATH]

The rows come from tools/PlanetIdentity/catalog.py (the 16 signature dishes and drinks, then the 32 decor pieces). A dish row is laid
out like the tab's other dishes, with the Stats text of the dish it copies; a decor row like the tab's furniture rows, with the
Furniture Blueprints perk at the tier of its level. Each is appended after its tab's last row with a value. Re-running leaves a row that
is already current exactly as it is, parameter columns included, and replaces one that differs. A workbook that took another branch's
rows is fixed by taking the base workbook, running this script, and then running tools/UpdateRecipeParameterBible.py. --check only
reports whether the rows are current and exits 1 when they are not. Every other zip entry is copied byte for byte, and the rest of the
worksheet XML is left untouched.

The recipes' minigame parameter columns are not written here: run `tools/UpdateRecipeParameterBible.py` afterwards, as
`tools/SWLOR.RecipeParameters/README.md` describes.
"""
import html
import os
import re
import sys
import tempfile
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "tools" / "PlanetIdentity"))
import catalog as c  # noqa: E402
import generate as g  # noqa: E402

WORKBOOK = REPO / "design" / "bible" / "SWLOR Design Bible.xlsx"
COOKING_SHEET = "Cooking Recipes"
FABRICATION_SHEET = "Fabrication Recipes"
STYLE = 14  # centered body style the tabs' newest rows use
COOKING_COMPONENT_COLUMNS = ["M", "O", "Q", "S", "U", "W", "Y", "AA"]  # quantity sits in the next column
FABRICATION_COMPONENT_COLUMNS = ["N", "P", "R", "T", "V", "X", "Z", "AB"]
RESEARCH_COLUMN = {COOKING_SHEET: "AD", FABRICATION_SHEET: "AD"}
STATS_COLUMN = "AC"
ENUM_COLUMN = "D"


def col_after(letter):
    n = 0
    for ch in letter:
        n = n * 26 + ord(ch) - 64
    n += 1
    s = ""
    while n:
        n, r = divmod(n - 1, 26)
        s = chr(65 + r) + s
    return s


def shared_strings(zf):
    if "xl/sharedStrings.xml" not in zf.namelist():
        return []
    root = ET.fromstring(zf.read("xl/sharedStrings.xml"))
    return ["".join(item.itertext()) for item in root]


def sheet_path(zf, name):
    workbook = zf.read("xl/workbook.xml").decode("utf-8")
    rid = re.search(r'<sheet [^>]*name="%s"[^>]*r:id="(\w+)"' % re.escape(name), workbook).group(1)
    rels = zf.read("xl/_rels/workbook.xml.rels").decode("utf-8")
    target = re.search(r'<Relationship [^>]*Id="%s"[^>]*Target="([^"]+)"' % rid, rels) or \
        re.search(r'<Relationship [^>]*Target="([^"]+)"[^>]*Id="%s"' % rid, rels)
    return "xl/" + target.group(1)


def read_cells(row_text, strings):
    """Column letter -> text of every cell of a row."""
    cells = {}
    for match in re.finditer(r'<c r="([A-Z]+)\d+"([^>]*?)(?:/>|>(.*?)</c>)', row_text, re.S):
        attrs, body = match.group(2), match.group(3) or ""
        inline = re.search(r"<t[^>]*>(.*?)</t>", body, re.S)
        value = re.search(r"<v>(.*?)</v>", body, re.S)
        if 't="s"' in attrs and value:
            text = strings[int(value.group(1))]
        elif inline:
            text = html.unescape(inline.group(1))
        elif value:
            text = html.unescape(value.group(1))
        else:
            text = ""
        cells[match.group(1)] = text
    return cells


def find_row(sheet_xml, strings, enum):
    for match in re.finditer(r'<row r="\d+"[^>]*?(?:/>|>.*?</row>)', sheet_xml, re.S):
        cells = read_cells(match.group(0), strings)
        if cells.get(ENUM_COLUMN) == enum:
            return cells
    return None


def recipe_rows(stats_of):
    """Every row this script writes. stats_of(enum) is the Stats text of an existing dish."""
    rows = []
    for si, entry in enumerate(c.SETS):
        dish_id, drink_id = c.signature_recipe_ids(si)
        for kind in ("dish", "drink"):
            item = entry[kind]
            source = g.cooking_source(item["copies"])
            rows.append(dict(
                sheet=COOKING_SHEET, enum=item["enum"], name=item["name"], category="Food", level=source["level"],
                resref=g.signature_resref(entry, kind), perk="Cooking Recipes", perk_level=g.tier_of(source["level"]),
                enhancement="Food", slots=source["slots"][1], components=item["components"], stats=stats_of(item["copies"])))
        for pi, piece in enumerate(entry["decor"]):
            source = g.furniture_source(piece["copies"])
            rows.append(dict(
                sheet=FABRICATION_SHEET, enum=piece["enum"], name=piece["name"], category=source["category"], level=source["level"],
                resref=f"structure_{c.structure_id(si, pi)}", perk="Furniture Blueprints", perk_level=g.tier_of(source["level"]),
                enhancement="Structure", slots=source["slots"][1], components=source["components"], storage=c.DECOR_STORAGE))
    return rows


def cell(ref, value):
    if isinstance(value, int):
        return f'<c r="{ref}" s="{STYLE}" t="n"><v>{value}</v></c>'
    return f'<c r="{ref}" s="{STYLE}" t="inlineStr"><is><t>{html.escape(value, quote=False)}</t></is></c>'


def row_cells(r):
    if r["sheet"] == FABRICATION_SHEET:
        cells = [("A", "Fabrication"), ("C", r["perk"]), ("D", r["enum"]), ("E", r["category"]), ("F", r["perk_level"]),
                 ("G", r["name"]), ("H", r["level"]), ("I", 1), ("J", r["resref"]), ("K", r["storage"]),
                 ("L", r["enhancement"]), ("M", r["slots"])]
        columns = FABRICATION_COMPONENT_COLUMNS
    else:
        cells = [("A", "Agriculture"), ("C", r["perk"]), ("D", r["enum"]), ("E", r["category"]), ("F", r["perk_level"]),
                 ("G", r["name"]), ("H", r["level"]), ("I", 1), ("J", r["resref"]), ("K", r["enhancement"]), ("L", r["slots"])]
        columns = COOKING_COMPONENT_COLUMNS
    assert len(r["components"]) <= len(columns), r["enum"]
    for column, (resref, quantity) in zip(columns, r["components"]):
        cells.append((column, resref))
        cells.append((col_after(column), quantity))
    if r["sheet"] == COOKING_SHEET:
        cells.append((STATS_COLUMN, r["stats"]))
    cells.append((RESEARCH_COLUMN[r["sheet"]], 0))
    return cells


def row_xml(number, r):
    return f'<row r="{number}">' + "".join(cell(f"{col}{number}", v) for col, v in row_cells(r)) + "</row>"


def row_is_current(row_text, r, strings):
    """True when the row already holds every cell this script writes, whatever else it carries (the parameter columns)."""
    found = read_cells(row_text, strings)
    return all(found.get(col) == str(value) for col, value in row_cells(r))


def rewrite(xml, rows, strings):
    """
    Writes the rows after the last row with a value. A row this script wrote before (matched by Recipe Enum) that already holds every
    cell it writes is left exactly as it is, parameter columns included; one that differs is dropped and written again.
    """
    by_enum = {r["enum"]: r for r in rows}
    unchanged = set()

    def written_before(match):
        cells = read_cells(match.group(0), strings)
        enum = cells.get(ENUM_COLUMN)
        if enum not in by_enum:
            return False
        if row_is_current(match.group(0), by_enum[enum], strings):
            unchanged.add(enum)
            return False
        return True

    def blank(text):
        return "<v>" not in text and "<is>" not in text

    kept, last = [], 1
    head, rest = xml.split("<sheetData>", 1)
    body, tail = rest.split("</sheetData>", 1)
    matches = list(re.finditer(r'<row r="(\d+)"[^>]*?(?:/>|>.*?</row>)', body, re.S))
    assert "".join(x.group(0) for x in matches) == body, "unexpected content between rows"
    for match in matches:
        if written_before(match):
            continue
        number = int(match.group(1))
        kept.append((number, match.group(0)))
        if not blank(match.group(0)):
            last = max(last, number)
    # New rows follow the last row with a value; formatting-only blank rows they land on are replaced.
    to_write = [r for r in rows if r["enum"] not in unchanged]
    new_rows = [(last + 1 + i, row_xml(last + 1 + i, r)) for i, r in enumerate(to_write)]
    taken = {n for n, _ in new_rows}
    merged = sorted([x for x in kept if x[0] not in taken] + new_rows, key=lambda x: x[0])
    end = last + len(new_rows)
    tail = re.sub(r'(<autoFilter ref="\$A\$1:\$[A-Z]+\$)\d+', lambda mo: mo.group(1) + str(max(end, last)), tail, count=1)
    return head + "<sheetData>" + "".join(x for _, x in merged) + "</sheetData>" + tail, end


def main():
    workbook = Path(sys.argv[sys.argv.index("--workbook") + 1]) if "--workbook" in sys.argv else WORKBOOK
    with zipfile.ZipFile(workbook) as zf:
        strings = shared_strings(zf)
        cooking_path = sheet_path(zf, COOKING_SHEET)
        cooking_xml = zf.read(cooking_path).decode("utf-8")

        def stats_of(enum):
            cells = find_row(cooking_xml, strings, enum)
            if cells is None or not cells.get(STATS_COLUMN):
                raise SystemExit(f"{COOKING_SHEET} has no Stats for the dish {enum} that the catalog copies")
            return cells[STATS_COLUMN]

        rows = recipe_rows(stats_of)
        updates, report = {}, []
        for name in (COOKING_SHEET, FABRICATION_SHEET):
            path = sheet_path(zf, name)
            original = zf.read(path).decode("utf-8")
            updated, end = rewrite(original, [r for r in rows if r["sheet"] == name], strings)
            report.append(f"{name}: {sum(1 for r in rows if r['sheet'] == name)} rows (last row {end})")
            if updated != original:
                updates[path] = updated
        if not updates:
            print("planet identity rows already current; " + "; ".join(report))
            return
        if "--check" in sys.argv:
            print("planet identity rows out of date: " + ", ".join(sorted(updates)))
            sys.exit(1)
        fd, tmp = tempfile.mkstemp(suffix=".xlsx", dir=workbook.parent)
        os.close(fd)
        with zipfile.ZipFile(tmp, "w") as out:
            for info in zf.infolist():
                data = updates[info.filename].encode("utf-8") if info.filename in updates else zf.read(info.filename)
                out.writestr(info, data, compress_type=info.compress_type)
    os.replace(tmp, workbook)
    print("planet identity rows written; " + "; ".join(report))


if __name__ == "__main__":
    main()
