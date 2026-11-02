"""Write the Fermentation Vat's recipe row into the Design Bible's Fabrication Recipes tab at the zip/XML level.

usage: python tools/UpdateFermentationVatBible.py [--check] [--workbook PATH]

The row is the Cookpot's (the other Agriculture crafting station): the same level and bill, laid out like the tab's Cookpot row,
and appended after the tab's last row. Re-running removes the row written before (matched by Recipe Enum) and writes it again,
so a workbook that took another branch's rows can be fixed by taking the base workbook and running this script again;
--check only reports whether the workbook is current and exits 1 when it is not. Every other zip entry is copied byte for byte,
and the rest of the worksheet XML is left untouched.

The recipe's Fabricator parameter columns are not written here: run `tools/UpdateRecipeParameterBible.py` afterwards, as
`tools/SWLOR.RecipeParameters/README.md` describes. The enzyme cultures have no recipe rows, since a culture is a job picked at
the vat (`EnzymeCultureRules`), not a recipe.
"""
import html
import os
import re
import sys
import tempfile
import zipfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
WORKBOOK = REPO / "design" / "bible" / "SWLOR Design Bible.xlsx"
SHEET_NAME = "Fabrication Recipes"
STYLE = 14  # centered body style the tab's newest rows use
COMPONENT_COLUMNS = ["N", "P", "R", "T", "V", "X", "Z", "AB"]  # quantity sits in the next column
RESEARCH_COLUMN = "AD"

ROWS = [
    dict(enum="FermentationVat", category="Crafting", name="Fermentation Vat", level=25, resref="structure_5105",
         perk="Furniture Blueprints", perk_level=3, storage=0, enhancement="N/A", slots=0,
         components=[("ref_plagionite", 5), ("ancient_wood", 3)]),
]


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


def cell(ref, value):
    if isinstance(value, int):
        return f'<c r="{ref}" s="{STYLE}" t="n"><v>{value}</v></c>'
    return f'<c r="{ref}" s="{STYLE}" t="inlineStr"><is><t>{html.escape(value, quote=False)}</t></is></c>'


def row_xml(number, r):
    cells = [("A", "Fabrication"), ("C", r["perk"]), ("D", r["enum"]), ("E", r["category"]), ("F", r["perk_level"]),
             ("G", r["name"]), ("H", r["level"]), ("I", r.get("quantity", 1)), ("J", r["resref"]), ("K", r["storage"]),
             ("L", r["enhancement"]), ("M", r["slots"])]
    assert len(r["components"]) <= len(COMPONENT_COLUMNS), r["enum"]
    for column, (resref, quantity) in zip(COMPONENT_COLUMNS, r["components"]):
        cells.append((column, resref))
        cells.append((col_after(column), quantity))
    cells.append((RESEARCH_COLUMN, 0))
    return f'<row r="{number}">' + "".join(cell(f"{col}{number}", v) for col, v in cells) + "</row>"


def sheet_path(zf, name):
    workbook = zf.read("xl/workbook.xml").decode("utf-8")
    rid = re.search(r'<sheet [^>]*name="%s"[^>]*r:id="(\w+)"' % re.escape(name), workbook).group(1)
    rels = zf.read("xl/_rels/workbook.xml.rels").decode("utf-8")
    target = re.search(r'<Relationship [^>]*Id="%s"[^>]*Target="([^"]+)"' % rid, rels) or \
        re.search(r'<Relationship [^>]*Target="([^"]+)"[^>]*Id="%s"' % rid, rels)
    return "xl/" + target.group(1)


def rewrite(xml, rows):
    """Drops the rows this script wrote before (by Recipe Enum) and appends them after the last row with a value."""
    enums = {r["enum"] for r in rows}

    def written_before(match):
        number = match.group(1)
        d = re.search(r'<c r="D%s"[^>]*t="inlineStr"><is><t[^>]*>([^<]*)</t>' % number, match.group(0))
        return d is not None and d.group(1) in enums

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
    new_rows = [(last + 1 + i, row_xml(last + 1 + i, r)) for i, r in enumerate(rows)]
    taken = {n for n, _ in new_rows}
    merged = sorted([x for x in kept if x[0] not in taken] + new_rows, key=lambda x: x[0])
    end = last + len(new_rows)
    tail = re.sub(r'(<autoFilter ref="\$A\$1:\$[A-Z]+\$)\d+', lambda mo: mo.group(1) + str(max(end, last)), tail, count=1)
    return head + "<sheetData>" + "".join(x for _, x in merged) + "</sheetData>" + tail, end


def main():
    workbook = Path(sys.argv[sys.argv.index("--workbook") + 1]) if "--workbook" in sys.argv else WORKBOOK
    with zipfile.ZipFile(workbook) as zf:
        path = sheet_path(zf, SHEET_NAME)
        original = zf.read(path).decode("utf-8")
        updated, end = rewrite(original, ROWS)
        if updated == original:
            print(f"{SHEET_NAME}: Fermentation Vat row already current (last row {end})")
            return
        if "--check" in sys.argv:
            print(f"{SHEET_NAME}: Fermentation Vat row out of date")
            sys.exit(1)
        fd, tmp = tempfile.mkstemp(suffix=".xlsx", dir=workbook.parent)
        os.close(fd)
        with zipfile.ZipFile(tmp, "w") as out:
            for info in zf.infolist():
                data = updated.encode("utf-8") if info.filename == path else zf.read(info.filename)
                out.writestr(info, data, compress_type=info.compress_type)
    os.replace(tmp, workbook)
    print(f"{SHEET_NAME}: Fermentation Vat row written (last row {end})")


if __name__ == "__main__":
    main()
