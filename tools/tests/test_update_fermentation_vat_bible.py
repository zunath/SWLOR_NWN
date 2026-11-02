import importlib.util
import tempfile
import unittest
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("update_fermentation_vat_bible", ROOT / "tools" / "UpdateFermentationVatBible.py")
VAT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VAT)

HEAD = '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>'
TAIL = '</sheetData><autoFilter ref="$A$1:$AJ$3" /></worksheet>'


def inline(ref, text):
    return f'<c r="{ref}" t="inlineStr"><is><t>{text}</t></is></c>'


EXISTING = (
    '<row r="1">' + inline("D1", "Recipe Enum") + '</row>'
    '<row r="2">' + inline("D2", "Cookpot") + '</row>'
    '<row r="3">' + inline("D3", "HoloStarGlobe") + '</row>'
    '<row r="4"><c r="A4" s="14" /></row>'
)


def sheet(body):
    return HEAD + body + TAIL


class FermentationVatBibleTests(unittest.TestCase):
    def test_the_row_is_appended_after_the_last_row_with_a_value_and_replaces_a_blank_formatting_row(self):
        updated, end = VAT.rewrite(sheet(EXISTING), VAT.ROWS)

        self.assertEqual(end, 4)
        self.assertIn('<row r="4">', updated)
        self.assertNotIn('<c r="A4" s="14" />', updated, "the blank formatting row it lands on is replaced")
        self.assertIn(inline("D4", "FermentationVat").replace(' t="inlineStr"', ' s="14" t="inlineStr"'), updated)
        self.assertIn('<autoFilter ref="$A$1:$AJ$4" />', updated)

    def test_the_row_carries_the_cookpots_level_and_bill_and_the_structure_item(self):
        updated, _ = VAT.rewrite(sheet(EXISTING), VAT.ROWS)

        for expected in ('structure_5105', 'ref_plagionite', 'ancient_wood', 'Crafting', 'Fermentation Vat', '>25<'):
            self.assertIn(expected, updated)
        self.assertIn('<c r="N4" s="14" t="inlineStr"><is><t>ref_plagionite</t></is></c><c r="O4" s="14" t="n"><v>5</v></c>', updated)
        self.assertIn('<c r="P4" s="14" t="inlineStr"><is><t>ancient_wood</t></is></c><c r="Q4" s="14" t="n"><v>3</v></c>', updated)
        self.assertIn('<c r="AD4" s="14" t="n"><v>0</v></c>', updated, "the research modifier column")

    def test_running_it_again_changes_nothing(self):
        once, _ = VAT.rewrite(sheet(EXISTING), VAT.ROWS)
        twice, _ = VAT.rewrite(once, VAT.ROWS)

        self.assertEqual(once, twice)
        self.assertEqual(twice.count("FermentationVat"), 1)

    def test_a_stale_row_is_replaced_in_place_and_other_rows_are_untouched(self):
        stale = EXISTING.replace(
            '<row r="4"><c r="A4" s="14" /></row>',
            '<row r="4">' + inline("D4", "FermentationVat") + inline("H4", "99") + '</row>')

        updated, end = VAT.rewrite(sheet(stale), VAT.ROWS)

        self.assertEqual(end, 4)
        self.assertNotIn("99", updated)
        self.assertEqual(updated.count("FermentationVat"), 1)
        for untouched in ('<row r="1">', '<row r="2">', '<row r="3">'):
            self.assertIn(untouched, updated)

    def test_a_workbook_is_rewritten_byte_for_byte_except_the_sheet(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bible.xlsx"
            with zipfile.ZipFile(path, "w") as archive:
                archive.writestr("xl/workbook.xml", '<workbook xmlns:r="r"><sheets><sheet name="Fabrication Recipes" sheetId="1" r:id="rId1"/></sheets></workbook>')
                archive.writestr("xl/_rels/workbook.xml.rels", '<Relationships><Relationship Id="rId1" Target="worksheets/sheet1.xml"/></Relationships>')
                archive.writestr("xl/worksheets/sheet1.xml", sheet(EXISTING))
                archive.writestr("xl/other.xml", "<other/>")

            with zipfile.ZipFile(path) as archive:
                self.assertEqual(VAT.sheet_path(archive, "Fabrication Recipes"), "xl/worksheets/sheet1.xml")
                updated, _ = VAT.rewrite(archive.read("xl/worksheets/sheet1.xml").decode("utf-8"), VAT.ROWS)

            self.assertIn("FermentationVat", updated)


if __name__ == "__main__":
    unittest.main()
