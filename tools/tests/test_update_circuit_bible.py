import importlib.util
import re
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
SPEC = importlib.util.spec_from_file_location("update_circuit_bible", ROOT / "tools" / "UpdateCircuitBible.py")
CIRCUIT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(CIRCUIT)

RULES = """
    public const double XpMultiplierLevels1To10 = 2.29;
    public const double XpMultiplierLevels11To20 = 1.52;
    public const double XpMultiplierLevels21To30 = 1.13;
    public const double XpMultiplierLevels31To40 = 1.0;
    public const double XpMultiplierLevels41AndUp = 1.0;
    public const double MaxEnhancementAssemblyShare = 0.3;
"""


def sheet(rows):
    return (
        '<?xml version="1.0" encoding="utf-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">'
        f"<sheetData>{rows}</sheetData></worksheet>"
    )


def numbered(number, ordinal, text):
    return (
        f'<row r="{number}"><c r="A{number}" s="3"><v>{ordinal}</v></c>'
        f'<c r="B{number}" s="4" t="inlineStr"><is><t xml:space="preserve">{text}</t></is></c></row>'
    )


def system_changes():
    rows = '<row r="1"><c r="A1" s="1" t="inlineStr"><is><t>Living document</t></is></c></row>' + numbered(3, "1.0", "First") + numbered(4, "2.0", "Second")
    return CIRCUIT.Bible.Sheet("System Changes & Migrations", sheet(rows), [])


class UpdateCircuitBibleTests(unittest.TestCase):
    def test_the_entry_text_reads_every_value_from_the_rules(self):
        text = CIRCUIT.entry_text(RULES)

        self.assertTrue(text.isascii())
        self.assertTrue(text.startswith(CIRCUIT.ENTRY_OPENING))
        self.assertIn("2.29 at recipe levels 1-10, 1.52 at recipe levels 11-20, 1.13 at recipe levels 21-30", text)
        self.assertIn("unchanged at 31-40 and 41 and up", text)
        self.assertIn("at most 30% of the Assembly", text)

    def test_the_entry_text_follows_the_values_it_reads(self):
        text = CIRCUIT.entry_text(RULES.replace("= 2.29;", "= 3.0;").replace("= 0.3;", "= 0.6;"))

        self.assertIn("3 at recipe levels 1-10", text)
        self.assertIn("at most 60% of the Assembly", text)

    def test_a_missing_value_is_an_error(self):
        with self.assertRaises(ValueError):
            CIRCUIT.entry_text(RULES.replace("XpMultiplierLevels21To30", "Other"))

    def test_a_new_entry_goes_after_the_last_numbered_one_and_a_rerun_rewrites_it_in_place(self):
        target = system_changes()

        CIRCUIT.update_sheet(target, "Engineering crafts use the Circuit minigame. First text.")
        CIRCUIT.update_sheet(target, "Engineering crafts use the Circuit minigame. Second text.")

        self.assertEqual(len(re.findall(r'<row r="5"', target.xml)), 1)
        self.assertEqual(len(re.findall(r'<row r="6"', target.xml)), 0)
        cells = target.row_cells(5)
        self.assertEqual(target.text_of(cells["A"]), "3.0")
        self.assertEqual(target.text_of(cells["B"]), "Engineering crafts use the Circuit minigame. Second text.")
        self.assertEqual(CIRCUIT.Bible.cell_style(cells["A"]), "3")
        self.assertEqual(CIRCUIT.Bible.cell_style(cells["B"]), "4")

    def test_the_real_rules_file_yields_the_bands_the_game_pays(self):
        text = CIRCUIT.entry_text(CIRCUIT.DEFAULT_RULES.read_text(encoding="utf-8"))

        self.assertIn(" at recipe levels 1-10", text)


if __name__ == "__main__":
    unittest.main()
