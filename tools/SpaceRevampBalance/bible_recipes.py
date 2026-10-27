"""Write the space revamp recipe rows into the Design Bible's Engineering Recipes tab at the zip/XML level.

usage: python bible_recipes.py [--check]
Rows come from catalog.py (module steps and ship deeds), one per RevampStarshipRecipes recipe, under the tab's
existing headers. They are appended after the last row. Re-running removes the previously generated rows (matched
by Recipe Enum) and writes them again; --check only reports whether the workbook is current. Every other zip
entry is copied byte for byte, and the rest of the worksheet XML is left untouched.
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
STYLE = 14  # centered body style the tab's newest rows use
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
    steps, hulls = c.catalog()
    rows = []
    for s in steps:
        rows.append(dict(enum=s["RecipeName"], category="ShipModule", name=s["Name"], level=s["RecipeLevel"],
                         resref=s["Tag"], enhancement="Module", components=s["Components"]))
    for h in hulls:
        rows.append(dict(enum=h["RecipeName"], category="Starship", name="Ship Deed: " + h["Name"], level=h["RecipeLevel"],
                         resref=h["Tag"], enhancement="Starship", components=h["Components"]))
    for r in rows:
        assert len(r["components"]) <= len(COMPONENT_COLUMNS), r["enum"]
    return rows


def cell(ref, value):
    if isinstance(value, int):
        return f'<c r="{ref}" s="{STYLE}" t="n"><v>{value}</v></c>'
    return f'<c r="{ref}" s="{STYLE}" t="inlineStr"><is><t>{html.escape(value, quote=False)}</t></is></c>'


def row_xml(number, r):
    cells = [("A", "Engineering"), ("D", r["enum"]), ("E", r["category"]), ("G", r["name"]), ("H", r["level"]),
             ("I", 1), ("J", r["resref"]), ("K", r["enhancement"]), ("L", 1)]
    for column, (resref, quantity) in zip(COMPONENT_COLUMNS, r["components"]):
        cells.append((column, resref))
        cells.append((col_after(column), quantity))
    cells.append(("AC", 0))
    return f'<row r="{number}">' + "".join(cell(f"{col}{number}", v) for col, v in cells) + "</row>"


def sheet_path(zf):
    workbook = zf.read("xl/workbook.xml").decode("utf-8")
    rid = re.search(r'<sheet [^>]*name="%s"[^>]*r:id="(\w+)"' % re.escape(SHEET_NAME), workbook).group(1)
    rels = zf.read("xl/_rels/workbook.xml.rels").decode("utf-8")
    target = re.search(r'<Relationship [^>]*Id="%s"[^>]*Target="([^"]+)"' % rid, rels) or \
        re.search(r'<Relationship [^>]*Target="([^"]+)"[^>]*Id="%s"' % rid, rels)
    return "xl/" + target.group(1)


def rewrite(xml, rows):
    enums = {r["enum"] for r in rows}

    def generated(match):
        number = match.group(1)
        d = re.search(r'<c r="D%s"[^>]*t="inlineStr"><is><t[^>]*>([^<]*)</t>' % number, match.group(0))
        return d is not None and d.group(1) in enums

    kept, last = [], 1
    head, rest = xml.split("<sheetData>", 1)
    body, tail = rest.split("</sheetData>", 1)
    matches = list(re.finditer(r'<row r="(\d+)"[^>]*?(?:/>|>.*?</row>)', body, re.S))
    assert "".join(x.group(0) for x in matches) == body, "unexpected content between rows"
    for match in matches:
        if generated(match):
            continue
        kept.append(match.group(0))
        last = max(last, int(match.group(1)))
    new_rows = [row_xml(last + 1 + i, r) for i, r in enumerate(rows)]
    end = last + len(new_rows)
    tail = re.sub(r'(<autoFilter ref="\$A\$1:\$[A-Z]+\$)\d+', lambda mo: mo.group(1) + str(end), tail, count=1)
    return head + "<sheetData>" + "".join(kept + new_rows) + "</sheetData>" + tail, end


def main():
    rows = recipe_rows()
    with zipfile.ZipFile(WORKBOOK) as zf:
        path = sheet_path(zf)
        original = zf.read(path).decode("utf-8")
        updated, end = rewrite(original, rows)
        if updated == original:
            print(f"{len(rows)} recipe rows already current (last row {end})")
            return
        if "--check" in sys.argv:
            print("Engineering Recipes rows out of date")
            sys.exit(1)
        fd, tmp = tempfile.mkstemp(suffix=".xlsx", dir=WORKBOOK.parent)
        os.close(fd)
        with zipfile.ZipFile(tmp, "w") as out:
            for info in zf.infolist():
                data = updated.encode("utf-8") if info.filename == path else zf.read(info.filename)
                out.writestr(info, data, compress_type=info.compress_type)
    os.replace(tmp, WORKBOOK)
    print(f"{len(rows)} recipe rows written to {SHEET_NAME} (last row {end})")


if __name__ == "__main__":
    main()
