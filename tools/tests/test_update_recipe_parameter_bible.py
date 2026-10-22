import importlib.util
import json
import tempfile
import unittest
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("update_recipe_parameter_bible", ROOT / "tools" / "UpdateRecipeParameterBible.py")
BIBLE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(BIBLE)

SHEET_OPEN = (
    '<?xml version="1.0" encoding="utf-8" standalone="yes"?>'
    '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" '
    'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">'
    '<sheetPr><outlinePr summaryBelow="0" summaryRight="0" /></sheetPr>'
    '<sheetFormatPr customHeight="1" defaultColWidth="12.63" defaultRowHeight="15.0" />'
)
COLS = '<cols>{}</cols>'
SHEET_CLOSE = '<autoFilter ref="{}" /><drawing r:id="rId1" /></worksheet>'

HEADERS = ["TestForge Center", "TestForge Cooling", "TestForge Style", "TestForge Override"]
PROFILE = {"key": "TestForge", "skill": "Smithery", "headers": HEADERS}
DATA = {
    "profiles": [PROFILE],
    "recipes": {
        "AdventTunic": {"TestForge Center": 50, "TestForge Cooling": 1.5, "TestForge Style": "Warm", "TestForge Override": ""},
        "AmateurTunic": {"TestForge Center": 60, "TestForge Cooling": 2.0, "TestForge Style": "Hot",
                         "TestForge Override": "Starter set stays forgiving & forgiving"},
        "SpiritmasterTunic": {"TestForge Center": 0, "TestForge Cooling": 0.5, "TestForge Style": "Cold", "TestForge Override": ""},
    },
}


class Strings:
    def __init__(self):
        self.values = []

    def index(self, text):
        if text not in self.values:
            self.values.append(text)
        return self.values.index(text)

    def cell(self, ref, style, text):
        return f'<c r="{ref}" s="{style}" t="s"><v>{self.index(text)}</v></c>'


def number(ref, style, value):
    return f'<c r="{ref}" s="{style}"><v>{value}</v></c>'


def blank(ref, style):
    return f'<c r="{ref}" s="{style}" />'


def inline(ref, style, text):
    return f'<c r="{ref}" s="{style}" t="inlineStr"><is><t>{text}</t></is></c>'


def row(number_, cells):
    return f'<row r="{number_}">{"".join(cells)}</row>'


def smithery_sheet(strings, extra_header_cells, rows_after, widths, filter_ref):
    header = [
        strings.cell("A1", 17, "Skill"),
        strings.cell("B1", 17, "Recipe\nEnum"),
        strings.cell("C1", 17, "Skill\nLevel"),
        strings.cell("D1", 17, "Name"),
    ] + extra_header_cells
    rows = [row(1, header)] + rows_after
    cols = "".join(f'<col min="{n}" max="{n}" width="{w}" customWidth="1" />' for n, w in widths)
    return SHEET_OPEN + COLS.format(cols) + "<sheetData>" + "".join(rows) + "</sheetData>" + SHEET_CLOSE.format(filter_ref)


def original_rows(strings):
    return [
        row(2, [strings.cell("A2", 6, "Smithery"), strings.cell("B2", 6, "AdventTunic"), number("C2", 6, "10.0"),
                strings.cell("D2", 6, "Advent Tunic"), blank("E2", 6), blank("F2", 6)]),
        row(3, [strings.cell("A3", 14, "Smithery"), strings.cell("B3", 14, "AmateurTunic"), number("C3", 14, "1.0"),
                strings.cell("D3", 14, "Amateur Tunic")]),
        row(4, [strings.cell("A4", 14, "Smithery"), strings.cell("B4", 14, "CombatTunic"), number("C4", 14, "3.0"),
                strings.cell("D4", 14, "Combat Tunic"), blank("E4", 14), blank("F4", 14)]),
        '<row r="5" />',
        row(6, [strings.cell("A6", 6, "Smithery"), strings.cell("B6", 6, "SpiritmasterTunic"), number("C6", 6, "7.0"),
                strings.cell("D6", 6, "Spiritmaster Tunic")]),
    ]


def build_workbook(path, filter_ref="$A$1:$F$6"):
    strings = Strings()
    smithery = smithery_sheet(
        strings,
        [blank("E1", 3), blank("F1", 3)],
        original_rows(strings),
        [(1, "13.0"), (2, "30.0"), (3, "8.0"), (4, "30.0")],
        filter_ref,
    )
    cooking = SHEET_OPEN + "<sheetData>" + row(1, [strings.cell("A1", 17, "Recipe\nEnum")]) + \
        row(2, [strings.cell("A2", 6, "SomeSoup")]) + "</sheetData></worksheet>"
    other = SHEET_OPEN + "<sheetData>" + row(1, [strings.cell("A1", 17, "Notes")]) + "</sheetData></worksheet>"
    shared = "".join(f"<si><t>{text}</t></si>" for text in strings.values)
    entries = {
        "[Content_Types].xml": '<?xml version="1.0"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"/>',
        "xl/workbook.xml": (
            '<?xml version="1.0" encoding="utf-8" standalone="yes"?>'
            '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" '
            'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>'
            '<sheet name="Smithery Recipes" sheetId="1" r:id="rId1"/>'
            '<sheet name="Cooking Recipes" sheetId="2" r:id="rId2"/>'
            '<sheet name="Other" sheetId="3" r:id="rId3"/></sheets><definedNames>'
            f'<definedName hidden="1" localSheetId="0" name="_xlnm._FilterDatabase">\'Smithery Recipes\'!{filter_ref}</definedName>'
            '</definedNames></workbook>'),
        "xl/_rels/workbook.xml.rels": (
            '<?xml version="1.0"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
            '<Relationship Id="rId1" Target="worksheets/sheet1.xml"/>'
            '<Relationship Id="rId2" Target="worksheets/sheet2.xml"/>'
            '<Relationship Id="rId3" Target="worksheets/sheet3.xml"/></Relationships>'),
        "xl/sharedStrings.xml": f'<?xml version="1.0"?><sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">{shared}</sst>',
        "xl/worksheets/sheet1.xml": smithery,
        "xl/worksheets/sheet2.xml": cooking,
        "xl/worksheets/sheet3.xml": other,
        "xl/drawings/drawing1.xml": "<drawing/>",
    }
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, text in entries.items():
            archive.writestr(name, text)
    return strings


def read_entries(path):
    with zipfile.ZipFile(path) as archive:
        return {name: archive.read(name).decode("utf-8") for name in archive.namelist()}


LAYOUT = {"generatedFrom": "test", "columnsBySheet": {"Smithery Recipes": [{"min": 1, "max": 4, "width": 12.63}]}}


class RecipeParameterBibleTests(unittest.TestCase):
    def setUp(self):
        self._folder = tempfile.TemporaryDirectory()
        self.addCleanup(self._folder.cleanup)
        self.folder = Path(self._folder.name)
        self.workbook = self.folder / "bible.xlsx"
        self.layout = self.folder / "layout.json"
        self.layout.write_text(BIBLE.render_layout(LAYOUT), encoding="utf-8", newline="")
        self.strings = build_workbook(self.workbook)
        self.before = read_entries(self.workbook)

    def run_update(self, data=None, check=False, layout=True):
        return BIBLE.update_files(self.workbook, data or DATA, self.layout if layout else None, check)

    def expected_sheet(self, strings):
        header_cells = [
            inline("E1", 17, "TestForge Center"), inline("F1", 17, "TestForge Cooling"),
            inline("G1", 17, "TestForge Style"), inline("H1", 17, "TestForge Override"),
        ]
        rows = [
            row(2, [strings.cell("A2", 6, "Smithery"), strings.cell("B2", 6, "AdventTunic"), number("C2", 6, "10.0"),
                    strings.cell("D2", 6, "Advent Tunic"), number("E2", 6, 50), number("F2", 6, 1.5), inline("G2", 6, "Warm")]),
            row(3, [strings.cell("A3", 14, "Smithery"), strings.cell("B3", 14, "AmateurTunic"), number("C3", 14, "1.0"),
                    strings.cell("D3", 14, "Amateur Tunic"), number("E3", 14, 60), number("F3", 14, 2), inline("G3", 14, "Hot"),
                    inline("H3", 14, "Starter set stays forgiving &amp; forgiving")]),
            original_rows(strings)[2],
            '<row r="5" />',
            row(6, [strings.cell("A6", 6, "Smithery"), strings.cell("B6", 6, "SpiritmasterTunic"), number("C6", 6, "7.0"),
                    strings.cell("D6", 6, "Spiritmaster Tunic"), number("E6", 6, 0), number("F6", 6, 0.5), inline("G6", 6, "Cold")]),
        ]
        widths = [(1, "13.0"), (2, "30.0"), (3, "8.0"), (4, "30.0")]
        sheet = smithery_sheet(strings, header_cells, rows, widths, "$A$1:$H$6")
        extra = "".join(
            f'<col min="{n}" max="{n}" width="{BIBLE.column_width(h)}" customWidth="1" />'
            for n, h in zip(range(5, 9), HEADERS))
        return sheet.replace("</cols>", extra + "</cols>")

    def test_appends_parameter_columns_after_the_last_header_and_fills_every_recipe_row(self):
        changed, report = self.run_update()

        after = read_entries(self.workbook)
        strings = Strings()
        strings.values = list(self.strings.values)
        self.assertTrue(changed)
        self.assertEqual(self.expected_sheet(strings), after["xl/worksheets/sheet1.xml"])
        self.assertIn("4 parameter columns (4 new), 3 recipe rows", report[0])

    def test_every_other_entry_is_copied_unchanged_except_the_filter_range(self):
        self.run_update()

        after = read_entries(self.workbook)
        for name, text in self.before.items():
            if name in ("xl/worksheets/sheet1.xml", "xl/workbook.xml"):
                continue
            self.assertEqual(text, after[name], name)
        self.assertEqual(
            self.before["xl/workbook.xml"].replace("$A$1:$F$6", "$A$1:$H$6"),
            after["xl/workbook.xml"])

    def test_tabs_without_data_stay_byte_identical(self):
        self.run_update()

        after = read_entries(self.workbook)
        self.assertEqual(self.before["xl/worksheets/sheet2.xml"], after["xl/worksheets/sheet2.xml"])
        self.assertEqual(self.before["xl/worksheets/sheet3.xml"], after["xl/worksheets/sheet3.xml"])

    def test_a_second_run_with_the_same_data_leaves_the_workbook_and_layout_alone(self):
        self.run_update()
        first_workbook = self.workbook.read_bytes()
        first_layout = self.layout.read_bytes()

        changed, _ = self.run_update()

        self.assertFalse(changed)
        self.assertEqual(first_workbook, self.workbook.read_bytes())
        self.assertEqual(first_layout, self.layout.read_bytes())

    def test_a_changed_value_rewrites_only_its_cell(self):
        self.run_update()
        first = read_entries(self.workbook)["xl/worksheets/sheet1.xml"]
        data = json.loads(json.dumps(DATA))
        data["recipes"]["AdventTunic"]["TestForge Center"] = 55

        changed, _ = self.run_update(data)

        after = read_entries(self.workbook)["xl/worksheets/sheet1.xml"]
        self.assertTrue(changed)
        self.assertEqual(first.replace('<c r="E2" s="6"><v>50</v></c>', '<c r="E2" s="6"><v>55</v></c>'), after)

    def test_a_recipe_that_loses_its_parameters_has_its_cells_blanked(self):
        self.run_update()
        data = json.loads(json.dumps(DATA))
        del data["recipes"]["SpiritmasterTunic"]

        self.run_update(data)

        after = read_entries(self.workbook)["xl/worksheets/sheet1.xml"]
        self.assertIn('<c r="E6" s="6" />', after)
        self.assertNotIn("Cold", after)

    def test_a_tab_whose_last_covered_recipe_is_gone_has_every_cell_blanked(self):
        self.run_update()
        data = json.loads(json.dumps(DATA))
        data["recipes"] = {}

        changed, _ = self.run_update(data)

        after = read_entries(self.workbook)["xl/worksheets/sheet1.xml"]
        self.assertTrue(changed)
        for ref in ("E2", "F2", "G2", "E3", "F3", "G3", "H3", "E6", "F6", "G6"):
            self.assertRegex(after, rf'<c r="{ref}" s="\d+" />')
        self.assertEqual(4, after.count("TestForge "))
        self.assertFalse(self.run_update(data)[0])

    def test_zero_is_a_value_and_not_a_blank(self):
        self.run_update()

        self.assertIn('<c r="E6" s="6"><v>0</v></c>', read_entries(self.workbook)["xl/worksheets/sheet1.xml"])

    def test_an_existing_header_is_reused_and_new_headers_follow_it(self):
        first = json.loads(json.dumps(DATA))
        first["profiles"][0]["headers"] = HEADERS[:2]
        self.run_update(first)
        sheet = read_entries(self.workbook)["xl/worksheets/sheet1.xml"]

        self.run_update()

        after = read_entries(self.workbook)["xl/worksheets/sheet1.xml"]
        self.assertIn(inline("E1", 17, "TestForge Center"), sheet)
        self.assertIn(inline("E1", 17, "TestForge Center"), after)
        self.assertIn(inline("G1", 17, "TestForge Style"), after)
        self.assertEqual(1, after.count("TestForge Center"))

    def test_the_layout_records_widths_for_the_new_columns(self):
        self.run_update()

        columns = json.loads(self.layout.read_text(encoding="utf-8"))["columnsBySheet"]["Smithery Recipes"]
        self.assertEqual([1, 5, 6, 7, 8], [item["min"] for item in columns])
        self.assertEqual(BIBLE.column_width("TestForge Cooling"), columns[2]["width"])

    def test_a_layout_that_does_not_round_trip_is_refused(self):
        self.layout.write_text('{ "generatedFrom": "test",  "columnsBySheet": {} }', encoding="utf-8")

        with self.assertRaisesRegex(BIBLE.BibleParameterError, "round-trip"):
            self.run_update()

        self.assertEqual(self.before, read_entries(self.workbook))

    def test_check_mode_reports_a_change_without_writing(self):
        original = self.workbook.read_bytes()

        changed, _ = self.run_update(check=True)

        self.assertTrue(changed)
        self.assertEqual(original, self.workbook.read_bytes())
        self.assertEqual(BIBLE.render_layout(LAYOUT), self.layout.read_text(encoding="utf-8"))

    def test_a_column_that_holds_data_without_a_header_is_refused(self):
        entries = read_entries(self.workbook)
        entries["xl/worksheets/sheet1.xml"] = entries["xl/worksheets/sheet1.xml"].replace(
            blank("E2", 6), number("E2", 6, 99))
        with zipfile.ZipFile(self.workbook, "w") as archive:
            for name, text in entries.items():
                archive.writestr(name, text)

        with self.assertRaisesRegex(BIBLE.BibleParameterError, "holds data without a header"):
            self.run_update()

    def test_a_header_must_start_with_its_profile_key(self):
        data = json.loads(json.dumps(DATA))
        data["profiles"][0]["headers"][0] = "Skill Level"

        with self.assertRaisesRegex(BIBLE.BibleParameterError, "must start with its profile key"):
            self.run_update(data)

    def test_the_filter_range_and_defined_name_are_left_alone_when_the_columns_already_fit(self):
        build_workbook(self.workbook, "$A$1:$J$6")

        self.run_update()

        after = read_entries(self.workbook)
        self.assertIn('<autoFilter ref="$A$1:$J$6" />', after["xl/worksheets/sheet1.xml"])
        self.assertIn("'Smithery Recipes'!$A$1:$J$6", after["xl/workbook.xml"])

    def test_the_defined_filter_name_widens_from_its_own_range(self):
        entries = read_entries(self.workbook)
        entries["xl/workbook.xml"] = entries["xl/workbook.xml"].replace("$A$1:$F$6", "$A$1:$F$1")
        with zipfile.ZipFile(self.workbook, "w") as archive:
            for name, text in entries.items():
                archive.writestr(name, text)

        self.run_update()

        after = read_entries(self.workbook)
        self.assertIn("'Smithery Recipes'!$A$1:$H$1<", after["xl/workbook.xml"])
        self.assertIn('<autoFilter ref="$A$1:$H$6" />', after["xl/worksheets/sheet1.xml"])

    def test_column_letters_round_trip(self):
        for index in (1, 26, 27, 52, 53, 702, 703):
            self.assertEqual(index, BIBLE.column_index(BIBLE.column_letters(index)))
        self.assertEqual("AG", BIBLE.column_letters(33))


if __name__ == "__main__":
    unittest.main()
