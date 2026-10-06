"""Ensure Bible cross-references preserve neighbouring cells and row structure."""
import sys
import unittest
from pathlib import Path
from xml.etree import ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from UpdateSpaceDesignBible import MAIN, NS, cell_snapshot, replace_reference_cell


class SpaceBibleAuthoringTests(unittest.TestCase):
    def test_self_closing_reference_preserves_blank_siblings_and_next_header(self):
        suffix = '<c r="B1" s="15" /></row><row r="2"><c r="A2" s="29" t="inlineStr"><is><t>Name</t></is></c></row></sheetData></worksheet>'
        prefix = f'<worksheet xmlns="{MAIN}"><sheetData><row r="1">'
        original = prefix + '<c r="A1" s="15" />' + suffix
        updated = replace_reference_cell(original, "A1", "New & planned", 99)
        self.assertTrue(updated.endswith(suffix))
        root = ET.fromstring(updated)
        rows = root.findall("m:sheetData/m:row", NS)
        self.assertEqual([c.attrib["r"] for c in rows[0]], ["A1", "B1"])
        self.assertEqual(rows[0][0].attrib["s"], "15")
        self.assertEqual(rows[0][0].find("m:is/m:t", NS).text, "New & planned")
        self.assertEqual(rows[1][0].find("m:is/m:t", NS).text, "Name")
        self.assertEqual(
            cell_snapshot({"sheet.xml": original.encode()}, {"Starships": "sheet.xml"}, {("Starships", "A1")}),
            cell_snapshot({"sheet.xml": updated.encode()}, {"Starships": "sheet.xml"}, {("Starships", "A1")}),
        )

    def test_paired_reference_replaces_only_the_selected_cell(self):
        prefix = '<row r="3"><c r="A3"><v>10</v></c>'
        suffix = '<c r="C3"><f>A3*2</f><v>20</v></c></row>'
        original = prefix + '<c r="B3" s="7" t="inlineStr"><is><t>Old</t></is></c>' + suffix
        updated = replace_reference_cell(original, "B3", "New", 99)
        self.assertEqual(updated, prefix + '<c r="B3" s="7" t="inlineStr"><is><t>New</t></is></c>' + suffix)


if __name__ == "__main__":
    unittest.main()
