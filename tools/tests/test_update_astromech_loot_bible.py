"""Guards tools/UpdateAstromechLootBible.py: row building for modules and tools (pure, no workbook write)."""
import sys
import unittest
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))
import UpdateAstromechLootBible as tool  # noqa: E402
import UpdateCraftingProgressionBible as bible  # noqa: E402


class FakeTwin:
    def __init__(self, weapon=None):
        self.name, self.resref, self.source, self.weapon, self.found = "Form Name", "am_form", "src_item", weapon, True


def source_row(columns):
    return {c: bible.text_cell(f"{c}7", "6", f"src-{c}") for c in columns}


class LootBibleRowTests(unittest.TestCase):
    def test_module_row_copies_source_and_sets_identity(self):
        cells = tool.form_cells(source_row("ABCDEFVW"), 20, FakeTwin(), "6")
        self.assertIn("Form Name", cells["A"])
        self.assertIn("am_form", cells["B"])
        self.assertIn("Chassis", cells["D"])
        self.assertIn('r="C20"', cells["C"])
        self.assertIn("src-V", cells["V"])  # sources are kept from the humanoid row
        self.assertIn("src-W", cells["W"])

    def test_tool_row_sets_armaments_and_base_item(self):
        cells = tool.form_cells(source_row("ABCDEFOP"), 9, FakeTwin(tool.rewards.RIFLE), "6")
        self.assertIn("Armaments", cells["C"])
        self.assertIn("blastermount", cells["D"])
        self.assertIn("src-P", cells["P"])
        staff = tool.form_cells(source_row("ABCD"), 9, FakeTwin(tool.rewards.STAFF), "6")
        self.assertIn("shockprod", staff["D"])

    def test_tab_routing_and_ascii(self):
        self.assertEqual(tool.tab_for(FakeTwin()), tool.ARMOR_TAB)
        self.assertEqual(tool.tab_for(FakeTwin(tool.rewards.STAFF)), tool.WEAPON_TAB)
        for twin in tool.rewards.TWINS:
            self.assertTrue((twin.name + twin.resref).isascii(), twin.resref)


if __name__ == "__main__":
    unittest.main()
