import importlib.util
import re
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "PlanetIdentity"))
import catalog as c  # noqa: E402
import generate as g  # noqa: E402

SPEC = importlib.util.spec_from_file_location("update_planet_identity_bible", ROOT / "tools" / "UpdatePlanetIdentityBible.py")
BIBLE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(BIBLE)

HEAD = '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>'
TAIL = '</sheetData><autoFilter ref="$A$1:$AQ$3" /></worksheet>'


def inline(ref, text):
    return f'<c r="{ref}" t="inlineStr"><is><t>{text}</t></is></c>'


EXISTING = (
    '<row r="1">' + inline("D1", "Recipe Enum") + '</row>'
    '<row r="2">' + inline("D2", "CookedMackerel") + '</row>'
    '<row r="3">' + inline("D3", "StatueGuardian") + '</row>'
    '<row r="4"><c r="A4" s="14" /></row>'
)


def sheet(body):
    return HEAD + body + TAIL


class CatalogTests(unittest.TestCase):
    def test_the_catalog_has_the_seven_city_planets_and_the_stations(self):
        planets = [entry["planet"] for entry in c.SETS]

        self.assertEqual(planets, ["Viscara", "MonCala", "Korriban", "Hutlar", "Tatooine", "Dantooine", "Dathomir", None])

    def test_each_set_has_a_dish_a_drink_and_four_decor_pieces(self):
        for entry in c.SETS:
            self.assertEqual(len(entry["decor"]), 4, entry["key"])
            for kind in ("dish", "drink"):
                self.assertTrue(entry[kind]["components"], entry[kind]["enum"])

    def test_ids_stay_in_the_reserved_ranges(self):
        for si, entry in enumerate(c.SETS):
            for kind_id in c.signature_recipe_ids(si):
                self.assertTrue(8100 <= kind_id <= 8119, kind_id)
            for pi, _ in enumerate(entry["decor"]):
                self.assertTrue(5130 <= c.structure_id(si, pi) <= 5189)
                self.assertTrue(8120 <= c.decor_recipe_id(si, pi) <= 8199)
                self.assertEqual(c.decor_recipe_id(si, pi), c.structure_id(si, pi) - 5130 + 8120)

    def test_every_player_facing_word_is_plain_ascii(self):
        for entry in c.SETS:
            texts = [entry["dish"]["name"], entry["dish"]["text"], entry["drink"]["name"], entry["drink"]["text"]]
            for piece in entry["decor"]:
                texts += [piece["name"], piece["text"]]
            for text in texts:
                self.assertRegex(text, r"^[\x20-\x7E]+$", text)

    def test_names_enums_and_resrefs_are_unique(self):
        enums = [x for e in c.SETS for x in [e["dish"]["enum"], e["drink"]["enum"]] + [p["enum"] for p in e["decor"]]]
        names = [x for e in c.SETS for x in [e["dish"]["name"], e["drink"]["name"]] + [p["name"] for p in e["decor"]]]
        placeables = [p["placeable"] for e in c.SETS for p in e["decor"]]

        self.assertEqual(len(enums), len(set(enums)))
        self.assertEqual(len(names), len(set(names)))
        self.assertEqual(len(placeables), len(set(placeables)))

    def test_the_catalog_passes_its_own_checks(self):
        g.check_catalog()

    def test_the_generated_files_are_current(self):
        stale = [str(path.relative_to(ROOT)) for path, text in g.build().items() if not path.exists() or g.read_text(path) != text]

        self.assertEqual(stale, [], "run python tools/PlanetIdentity/generate.py")

    def test_a_drink_is_named_so_the_kitchen_classifies_it_as_sweet(self):
        sweet = ("Cookies", "Juice", "AuLait", "Cake", "FlapJack", "Fizz", "Gardula", "Noga", "Treat")
        soup = ("Soup", "Broth", "Stew", "Chowder", "Curry", "Noodles", "Ramen", "Gumbo", "Consomme", "Tea", "Bowl")
        for entry in c.SETS:
            self.assertTrue(any(w in entry["drink"]["enum"] for w in sweet), entry["drink"]["enum"])
            self.assertFalse(any(w in entry["dish"]["enum"] for w in sweet), entry["dish"]["enum"])
            self.assertFalse(any(w in entry["drink"]["enum"] for w in soup), entry["drink"]["enum"])


class BibleRowTests(unittest.TestCase):
    def rows(self):
        return BIBLE.recipe_rows(lambda enum: "10 HP")

    def test_there_is_a_row_for_every_recipe_on_the_tab_that_lists_it(self):
        rows = self.rows()

        self.assertEqual(sum(1 for r in rows if r["sheet"] == BIBLE.COOKING_SHEET), 16)
        self.assertEqual(sum(1 for r in rows if r["sheet"] == BIBLE.FABRICATION_SHEET), 32)

    def test_rows_are_appended_after_the_last_row_with_a_value_and_replace_a_blank_formatting_row(self):
        rows = [r for r in self.rows() if r["sheet"] == BIBLE.FABRICATION_SHEET][:2]

        updated, end = BIBLE.rewrite(sheet(EXISTING), rows, [])

        self.assertEqual(end, 5)
        self.assertNotIn('<c r="A4" s="14" />', updated)
        self.assertIn(rows[0]["enum"], updated)
        self.assertIn('<autoFilter ref="$A$1:$AQ$5" />', updated)

    def test_a_furniture_row_carries_the_furniture_perk_the_structure_item_and_the_copied_bill(self):
        row = next(r for r in self.rows() if r["enum"] == "ViscaraFountainStatue")

        updated, _ = BIBLE.rewrite(sheet(EXISTING), [row], [])

        for expected in ("Furniture Blueprints", "structure_5130", "Statues", "ref_scordspar", "fine_wood", "Veles Fountain Statue"):
            self.assertIn(expected, updated)
        self.assertIn('<c r="F4" s="14" t="n"><v>2</v></c>', updated, "the perk level is the tier of the level")
        self.assertIn('<c r="K4" s="14" t="n"><v>1</v></c>', updated, "one item storage slot like every crafted furniture piece")

    def test_a_dish_row_carries_its_stats_and_its_food_slots(self):
        row = next(r for r in self.rows() if r["enum"] == "ViscaraOrchardRoast")

        updated, _ = BIBLE.rewrite(sheet(EXISTING), [row], [])

        self.assertIn('<c r="AC4" s="14" t="inlineStr"><is><t>10 HP</t></is></c>', updated)
        self.assertIn("sig_visc_dish", updated)
        self.assertIn('<c r="M4" s="14" t="inlineStr"><is><t>cairnmog_meat</t></is></c><c r="N4" s="14" t="n"><v>2</v></c>', updated)

    def test_running_it_again_changes_nothing(self):
        rows = self.rows()
        fabrication = [r for r in rows if r["sheet"] == BIBLE.FABRICATION_SHEET]
        once, _ = BIBLE.rewrite(sheet(EXISTING), fabrication, [])

        twice, _ = BIBLE.rewrite(once, fabrication, [])

        self.assertEqual(once, twice)
        self.assertEqual(twice.count("ViscaraFountainStatue"), 1)

    def test_a_current_row_keeps_the_parameter_columns_another_script_added(self):
        row = next(r for r in self.rows() if r["enum"] == "ViscaraStreetLamp")
        once, _ = BIBLE.rewrite(sheet(EXISTING), [row], [])
        number = re.search(r'<row r="(\d+)">(?:(?!</row>).)*ViscaraStreetLamp', once).group(1)
        with_parameters = once.replace(
            f'<c r="AD{number}" s="14" t="n"><v>0</v></c></row>',
            f'<c r="AD{number}" s="14" t="n"><v>0</v></c><c r="AE{number}" s="14"><v>20</v></c></row>')
        self.assertIn(f'<c r="AE{number}" s="14"><v>20</v></c>', with_parameters)

        again, _ = BIBLE.rewrite(with_parameters, [row], [])

        self.assertEqual(again, with_parameters)

    def test_a_row_that_differs_is_written_again(self):
        row = next(r for r in self.rows() if r["enum"] == "ViscaraStreetLamp")
        once, _ = BIBLE.rewrite(sheet(EXISTING), [row], [])
        stale = once.replace("structure_5131", "structure_9999")

        fixed, _ = BIBLE.rewrite(stale, [row], [])

        self.assertIn("structure_5131", fixed)
        self.assertNotIn("structure_9999", fixed)
        self.assertEqual(fixed.count("ViscaraStreetLamp"), 1)


if __name__ == "__main__":
    unittest.main()
