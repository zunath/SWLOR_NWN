import importlib.util
import re
import shutil
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
SPEC = importlib.util.spec_from_file_location("update_crafting_minigames_bible", ROOT / "tools" / "UpdateCraftingMinigamesBible.py")
MINIGAMES = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MINIGAMES)

OPENINGS = {
    "fabricator": MINIGAMES.FABRICATOR_OPENING,
    "kitchen": MINIGAMES.KITCHEN_OPENING,
    "fishing": MINIGAMES.FISHING_OPENING,
    "prospecting": MINIGAMES.PROSPECTING_OPENING,
    "search": MINIGAMES.SEARCH_OPENING,
    "research": MINIGAMES.RESEARCH_OPENING,
    "shared": MINIGAMES.SHARED_OPENING,
    "perk_lines": MINIGAMES.PERK_LINES_OPENING,
}


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
    return MINIGAMES.Bible.Sheet("System Changes & Migrations", sheet(rows), [])


class SourceTree:
    """A temporary copy of the source files the script reads, so a test can change one value and see the text follow it."""

    def __init__(self):
        self.directory = tempfile.TemporaryDirectory()
        self.root = Path(self.directory.name)
        for relative in MINIGAMES.SOURCE_FILES.values():
            target = self.root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / relative, target)

    def close(self):
        self.directory.cleanup()

    def path(self, key):
        return self.root / MINIGAMES.SOURCE_FILES[key]

    def set_constant(self, key, name, value):
        path = self.path(key)
        text = path.read_text(encoding="utf-8-sig")
        changed, count = re.subn(rf"(\bconst\s+(?:double|int|float)\s+{name}\s*=\s*)[^;]+;", rf"\g<1>{value};", text)
        assert count == 1, f"{name} was not found once in {path.name}"
        path.write_text(changed, encoding="utf-8")

    def replace(self, key, old, new):
        path = self.path(key)
        text = path.read_text(encoding="utf-8-sig")
        assert old in text, f"{old!r} is not in {path.name}"
        path.write_text(text.replace(old, new), encoding="utf-8")

    def entries(self):
        return dict(zip(OPENINGS, (text for _, text in MINIGAMES.build_entries(self.root))))


class UpdateCraftingMinigamesBibleTests(unittest.TestCase):
    def setUp(self):
        self.tree = SourceTree()
        self.addCleanup(self.tree.close)

    def test_every_entry_is_plain_ascii_and_starts_with_its_opening_words(self):
        entries = MINIGAMES.build_entries()

        self.assertEqual(8, len(entries))
        self.assertEqual(len(entries), len({opening for opening, _ in entries}))
        for opening, text in entries:
            self.assertTrue(text.isascii(), opening)
            self.assertTrue(text.startswith(opening), opening)

    def test_the_real_sources_give_the_values_the_game_uses(self):
        entries = dict(zip(OPENINGS, (text for _, text in MINIGAMES.build_entries())))

        for key in ("fabricator", "kitchen"):
            self.assertRegex(entries[key], r"multiplied by [0-9.]+ at recipe levels 1-10")
        self.assertRegex(entries["fishing"], r"bite's XP times [0-9.]+ at fish levels 0-10")
        self.assertRegex(entries["prospecting"], r"Strike takes \d+ or \d+ units")
        self.assertRegex(entries["search"], r"on promising ground, \d+% at Gathering 0")
        self.assertRegex(entries["research"], r"At research levels \d+, \d+ and \d+")
        self.assertRegex(entries["shared"], r"\d+% for the first slot, \d+% for the second and \d+% for the third")
        self.assertRegex(entries["perk_lines"], r"Engineering: Overclocker and Technician \(Circuit\)")
        self.assertIn("Slow Cooking and Flash Cooking (Chef)", entries["perk_lines"])
        self.assertIn("Strip Mining (Prospector) and Careful Hands (Treasure Seeker)", entries["perk_lines"])

    def test_the_xp_multipliers_follow_the_constants_in_code(self):
        self.tree.set_constant("fabricator", "XpMultiplierLevels1To10", "3.7")
        self.tree.set_constant("fabricator", "XpMultiplierLevels41Up", "1.0")
        self.tree.set_constant("kitchen", "XpMultiplierLevels31To40", "1.35")
        self.tree.set_constant("line", "XpMultiplierLevels0To10", "9.5")
        self.tree.set_constant("search", "XpMultiplierLevel3", "1.5")

        entries = self.tree.entries()

        self.assertIn("multiplied by 3.7 at recipe levels 1-10", entries["fabricator"])
        self.assertIn("and unchanged at 41 and up", entries["fabricator"])
        self.assertIn("1.35 at recipe levels 31-40", entries["kitchen"])
        self.assertIn("9.5 at fish levels 0-10", entries["fishing"])
        self.assertIn("1.5, 1.12 and 1.12 at site levels", entries["search"])

    def test_prospecting_names_a_session_multiplier_only_when_one_exists(self):
        self.assertIn("is 1 in every vein band", self.tree.entries()["prospecting"])

        self.tree.set_constant("prospecting", "XpMultiplierVeinLevel15", "1.2")

        text = self.tree.entries()["prospecting"]
        self.assertNotIn("is 1 in every vein band", text)
        self.assertIn("1.2 at vein levels up to 15", text)

    def test_the_shared_rules_follow_the_constants_in_code(self):
        self.tree.set_constant("outcome", "ChanceToLoseItemPercent", "70")
        self.tree.set_constant("outcome", "FailureXpFraction", "0.25")
        self.tree.set_constant("outcome", "SecondSlotQualityThresholdPercent", "80")
        self.tree.set_constant("outcome", "FoodDurationSecondsPerUnit", "10 * 60")
        self.tree.set_constant("quick_craft", "MaxBatchSize", "12")
        self.tree.set_constant("quick_craft", "RankMargin", "6")

        text = self.tree.entries()["shared"]

        self.assertIn("80% for the second", text)
        self.assertIn("on a 70% roll and pays 25% of the XP", text)
        self.assertIn("to 10 minutes at 100%", text)
        self.assertIn("up to 12 items", text)
        self.assertIn("at least 6 above the recipe's level", text)

    def test_the_gathering_and_research_rules_follow_the_code(self):
        self.tree.set_constant("prospecting", "ExtractCharges", "3")
        self.tree.set_constant("search", "ActionSeconds", "7.5")
        self.tree.replace("milestones", "{ (0, 29), (10, 32), (20, 36), (30, 39), (40, 43) }", "{ (0, 21), (25, 44) }")
        self.tree.set_constant("experiment", "Trials", "9")
        self.tree.set_constant("experiment", "RefineCp", "35")

        entries = self.tree.entries()

        self.assertIn("Extract opens a found seam (3 charges", entries["prospecting"])
        self.assertIn("each later one waits 7.5 seconds", entries["search"])
        self.assertIn("21% at Gathering 0 and 44% at 25", entries["search"])
        self.assertIn("The experiment has 9 trials", entries["research"])
        self.assertIn("Refine Method costs 35 Fabrication CP", entries["research"])

    def test_a_value_the_code_no_longer_has_is_an_error(self):
        self.tree.replace("fabricator", "XpMultiplierLevels21To30 =", "OtherMultiplier =")

        with self.assertRaises(ValueError):
            MINIGAMES.build_entries(self.tree.root)

    def test_a_perk_line_the_manifest_no_longer_lists_is_an_error(self):
        manifest = self.tree.path("perk_manifest")
        manifest.write_text(manifest.read_text(encoding="utf-8-sig").replace('"Chef"', '"Cook"'), encoding="utf-8")

        with self.assertRaises(ValueError):
            MINIGAMES.build_entries(self.tree.root)

    def test_a_style_that_opens_at_another_rank_is_an_error(self):
        self.tree.replace("perks_chef", "RequirementSkill(SkillType.Agriculture, 20)", "RequirementSkill(SkillType.Agriculture, 25)")

        with self.assertRaises(ValueError):
            MINIGAMES.build_entries(self.tree.root)

    def test_the_source_reader_evaluates_the_expressions_the_constants_use(self):
        path = self.tree.root / "Reader.cs"
        path.write_text(
            "public const int Size = 3;\n"
            "public const int Spots = Size * Size;\n"
            "public const int Seconds = 5 * 60;\n"
            "public const int Halved = 7 / 2;\n"
            "public const float Fraction = 0.15f;\n"
            "public const double Negative = -2.5;\n"
            "private static readonly int[] Ranks = { 15, 30, 45 };\n"
            "private static readonly (int Rank, int Value)[] Steps = { (0, 6), (10, 8) };\n"
            "public enum Heat { Low = 0, High = 1 }\n",
            encoding="utf-8")

        source = MINIGAMES.Source(path)

        self.assertEqual(9, source.const("Spots"))
        self.assertEqual(300, source.const("Seconds"))
        self.assertEqual(3, source.const("Halved"))
        self.assertEqual(0.15, source.const("Fraction"))
        self.assertEqual(-2.5, source.const("Negative"))
        self.assertEqual([15, 30, 45], source.int_array("Ranks"))
        self.assertEqual([(0, 6), (10, 8)], source.steps("Steps"))
        self.assertEqual(["Low", "High"], source.enum_names("Heat"))
        with self.assertRaises(ValueError):
            source.const("Missing")

    def test_numbers_are_written_without_trailing_zeros(self):
        self.assertEqual("5", MINIGAMES.number(5.0))
        self.assertEqual("2.4", MINIGAMES.number(2.40))
        self.assertEqual("8.55", MINIGAMES.number(8.55))
        self.assertEqual("1.05", MINIGAMES.number(1.05))
        self.assertEqual("15%", MINIGAMES.percent(0.15))

    def test_new_entries_go_after_the_last_numbered_one_in_order_and_a_rerun_rewrites_them_in_place(self):
        target = system_changes()
        entries = [(f"Opening {index}.", f"Opening {index}. First text.") for index in range(3)]

        MINIGAMES.update_sheet(target, entries)
        MINIGAMES.update_sheet(target, [(opening, text.replace("First", "Second")) for opening, text in entries])

        for index in range(3):
            row = 5 + index
            self.assertEqual(len(re.findall(rf'<row r="{row}"', target.xml)), 1)
            cells = target.row_cells(row)
            self.assertEqual(target.text_of(cells["A"]), f"{3 + index}.0")
            self.assertEqual(target.text_of(cells["B"]), f"Opening {index}. Second text.")
            self.assertEqual(MINIGAMES.Bible.cell_style(cells["A"]), "3")
            self.assertEqual(MINIGAMES.Bible.cell_style(cells["B"]), "4")
        self.assertEqual(len(re.findall(r'<row r="8"', target.xml)), 0)

    def test_a_rerun_keeps_the_cells_an_existing_entry_holds_beyond_columns_a_and_b(self):
        target = system_changes()
        MINIGAMES.update_sheet(target, [("Opening.", "Opening. First text.")])
        cells = target.row_cells(5)
        cells["C"] = MINIGAMES.Bible.text_cell("C5", "4", "Reviewer note")
        target.write_row(5, cells)

        MINIGAMES.update_sheet(target, [("Opening.", "Opening. Second text.")])

        after = target.row_cells(5)
        self.assertEqual(target.text_of(after["B"]), "Opening. Second text.")
        self.assertEqual(target.text_of(after["C"]), "Reviewer note")

    def test_the_committed_workbook_matches_the_code(self):
        self.assertFalse(
            MINIGAMES.update_workbook(MINIGAMES.DEFAULT_WORKBOOK, MINIGAMES.build_entries(), check=True),
            "run python tools/UpdateCraftingMinigamesBible.py after changing a crafting minigame rule or perk line")


if __name__ == "__main__":
    unittest.main()
