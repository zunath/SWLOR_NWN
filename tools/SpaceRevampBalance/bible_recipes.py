"""Write the space revamp recipe rows into the Design Bible's recipe tabs at the zip/XML level.

usage: python bible_recipes.py [--check]
Rows come from catalog.py: the module steps and ship deeds (Engineering Recipes) and the ship component recipes,
each on its skill's tab (Engineering, Smithery, or Cooking Recipes for Agriculture). They sit under each tab's
existing headers, appended after its last row. Re-running removes the previously generated rows (matched by Recipe
Enum) and writes them again; --check only reports whether the workbook is current. Every other zip entry is copied
byte for byte, and the rest of each worksheet XML is left untouched.
"""
import html
import os
import re
import sys
import tempfile
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import catalog as c  # noqa: E402
import model as m  # noqa: E402

WORKBOOK = m.REPO / "design" / "bible" / "SWLOR Design Bible.xlsx"
SHEET_NAME = "Engineering Recipes"
SHEET_BY_SKILL = {"Engineering": "Engineering Recipes", "Smithery": "Smithery Recipes", "Agriculture": "Cooking Recipes"}
RESEARCH_COLUMN = {"Engineering Recipes": "AC", "Smithery Recipes": "AC", "Cooking Recipes": "AD"}
STYLE = 14  # centered body style the tabs' newest rows use
COMPONENT_COLUMNS = ["M", "O", "Q", "S", "U", "W", "Y", "AA"]  # quantity sits in the next column


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


def recipe_rows():
    """Every generated row with the tab it belongs on."""
    steps, hulls, components = c.full_catalog()
    rows = []
    for s in steps:
        rows.append(dict(sheet=SHEET_NAME, skill="Engineering", enum=s["RecipeName"], category="ShipModule", name=s["Name"],
                         level=s["RecipeLevel"], resref=s["Tag"], enhancement="Module", slots=1, components=s["Components"]))
    for h in hulls:
        rows.append(dict(sheet=SHEET_NAME, skill="Engineering", enum=h["RecipeName"], category="Starship",
                         name="Ship Deed: " + h["Name"], level=h["RecipeLevel"], resref=h["Tag"], enhancement="Starship",
                         slots=1, components=h["Components"]))
    for x in components:
        rows.append(dict(sheet=SHEET_BY_SKILL[x["Skill"]], skill=x["Skill"], enum=x["RecipeName"], category="ShipComponent",
                         name=x["Name"], level=x["RecipeLevel"], resref=x["Resref"], enhancement="N/A", slots=0,
                         components=x["Components"]))
    for r in rows:
        assert len(r["components"]) <= len(COMPONENT_COLUMNS), r["enum"]
    return rows


def cell(ref, value):
    if isinstance(value, int):
        return f'<c r="{ref}" s="{STYLE}" t="n"><v>{value}</v></c>'
    return f'<c r="{ref}" s="{STYLE}" t="inlineStr"><is><t>{html.escape(value, quote=False)}</t></is></c>'


def row_xml(number, r):
    cells = [("A", r["skill"]), ("D", r["enum"]), ("E", r["category"]), ("G", r["name"]), ("H", r["level"]),
             ("I", 1), ("J", r["resref"]), ("K", r["enhancement"]), ("L", r["slots"])]
    for column, (resref, quantity) in zip(COMPONENT_COLUMNS, r["components"]):
        cells.append((column, resref))
        cells.append((col_after(column), quantity))
    cells.append((RESEARCH_COLUMN[r["sheet"]], 0))
    return f'<row r="{number}">' + "".join(cell(f"{col}{number}", v) for col, v in cells) + "</row>"


def sheet_path(zf, name=SHEET_NAME):
    workbook = zf.read("xl/workbook.xml").decode("utf-8")
    rid = re.search(r'<sheet [^>]*name="%s"[^>]*r:id="(\w+)"' % re.escape(name), workbook).group(1)
    rels = zf.read("xl/_rels/workbook.xml.rels").decode("utf-8")
    target = re.search(r'<Relationship [^>]*Id="%s"[^>]*Target="([^"]+)"' % rid, rels) or \
        re.search(r'<Relationship [^>]*Target="([^"]+)"[^>]*Id="%s"' % rid, rels)
    return "xl/" + target.group(1)


def rewrite(xml, rows, generated_enums):
    """Drops every previously generated row (any enum this generator writes, on any tab) and appends this tab's rows."""

    def generated(match):
        number = match.group(1)
        d = re.search(r'<c r="D%s"[^>]*t="inlineStr"><is><t[^>]*>([^<]*)</t>' % number, match.group(0))
        return d is not None and d.group(1) in generated_enums

    def blank(text):
        return "<v>" not in text and "<is>" not in text

    kept, last = [], 1
    head, rest = xml.split("<sheetData>", 1)
    body, tail = rest.split("</sheetData>", 1)
    matches = list(re.finditer(r'<row r="(\d+)"[^>]*?(?:/>|>.*?</row>)', body, re.S))
    assert "".join(x.group(0) for x in matches) == body, "unexpected content between rows"
    for match in matches:
        if generated(match):
            continue
        number = int(match.group(1))
        kept.append((number, match.group(0)))
        if not blank(match.group(0)):
            last = max(last, number)
    # New rows follow the last row with a value; formatting-only blank rows they land on are replaced.
    new_rows = [(last + 1 + i, row_xml(last + 1 + i, r)) for i, r in enumerate(rows)]
    taken = {n for n, _ in new_rows}
    merged = sorted([x for x in kept if x[0] not in taken] + new_rows, key=lambda x: x[0])
    end = last + len(new_rows)
    tail = re.sub(r'(<autoFilter ref="\$A\$1:\$[A-Z]+\$)\d+', lambda mo: mo.group(1) + str(max(end, last)), tail, count=1)
    return head + "<sheetData>" + "".join(x for _, x in merged) + "</sheetData>" + tail, end


def main():
    rows = recipe_rows()
    enums = {r["enum"] for r in rows}
    sheets = sorted({r["sheet"] for r in rows})
    updates, report = {}, []
    with zipfile.ZipFile(WORKBOOK) as zf:
        for name in sheets:
            path = sheet_path(zf, name)
            original = zf.read(path).decode("utf-8")
            updated, end = rewrite(original, [r for r in rows if r["sheet"] == name], enums)
            report.append(f"{name}: {sum(1 for r in rows if r['sheet'] == name)} rows (last row {end})")
            if updated != original:
                updates[path] = updated
        if not updates:
            print("recipe rows already current; " + "; ".join(report))
            return
        if "--check" in sys.argv:
            print("recipe rows out of date: " + ", ".join(sorted(updates)))
            sys.exit(1)
        fd, tmp = tempfile.mkstemp(suffix=".xlsx", dir=WORKBOOK.parent)
        os.close(fd)
        with zipfile.ZipFile(tmp, "w") as out:
            for info in zf.infolist():
                data = updates[info.filename].encode("utf-8") if info.filename in updates else zf.read(info.filename)
                out.writestr(info, data, compress_type=info.compress_type)
    os.replace(tmp, WORKBOOK)
    print("recipe rows written; " + "; ".join(report))


if __name__ == "__main__":
    main()
