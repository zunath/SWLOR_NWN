"""Guards tools/UpdateAstromechSkillBible.py: the tab has one row per mirrored perk level and a rerun changes nothing."""
import hashlib
import json
import shutil
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))
import UpdateAstromechSkillBible as tool  # noqa: E402
import UpdateCraftingProgressionBible as bible  # noqa: E402


def read_tab(path: Path):
    with zipfile.ZipFile(path) as archive:
        paths = bible.resolve_sheets(archive)
        sheet = bible.Sheet(tool.TAB, archive.read(paths[tool.TAB]).decode("utf-8"), bible.read_strings(archive))
    header = next(n for n in range(1, sheet.last_data_row() + 1) if sheet.text_of(sheet.row_cells(n).get("A", "")) == "Skill"
                  and "C" in sheet.row_cells(n))
    rows = []
    for n in range(header + 1, sheet.last_data_row() + 1):
        cells = sheet.row_cells(n)
        rows.append({col: sheet.text_of(cell) for col, cell in cells.items()})
    return rows


class AstromechSkillBibleTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.directory = tempfile.mkdtemp()
        cls.workbook = Path(cls.directory) / "bible.xlsx"
        shutil.copy(tool.DEFAULT_WORKBOOK, cls.workbook)
        tool.update_workbook(cls.workbook)

    @classmethod
    def tearDownClass(cls):
        shutil.rmtree(cls.directory, ignore_errors=True)

    def test_one_row_per_mirrored_perk_level_with_config_names(self):
        config = json.loads(tool.MIRRORS.read_text(encoding="utf-8"))
        expected = []
        for mirror in config["mirrors"]:
            for line in mirror["lines"]:
                names = {data["name"] for data in line["perks"].values()}
                expected.append((mirror["skill_name"], line["name"], names))
        rows = read_tab(self.workbook)
        self.assertEqual(len(rows), 70)
        self.assertEqual(sum(1 for _ in rows), len(tool.mirror_rows()))
        for skill, line, names in expected:
            found = [r for r in rows if r["A"] == skill and r["B"] == line]
            self.assertEqual({r["C"] for r in found}, names, f"{skill} / {line}")
            for perk in names:
                levels = sorted(int(r["D"]) for r in found if r["C"] == perk)
                self.assertEqual(levels, list(range(1, len(levels) + 1)), f"{perk} levels")
        for row in rows:
            self.assertTrue(row["G"], f"{row['C']} has no description")
            self.assertTrue(row["H"].startswith(row["I"] + " - "))

    def test_tab_is_registered_once(self):
        with zipfile.ZipFile(self.workbook) as archive:
            workbook = archive.read("xl/workbook.xml").decode("utf-8")
            types = archive.read("[Content_Types].xml").decode("utf-8")
            entry = bible.resolve_sheets(archive)[tool.TAB]
        self.assertEqual(workbook.count(f'<sheet name="{tool.TAB}"'), 1)
        self.assertEqual(types.count(f'PartName="/{entry}"'), 1)

    def test_rerun_is_byte_identical_and_other_entries_untouched(self):
        first = hashlib.sha256(self.workbook.read_bytes()).hexdigest()
        tool.update_workbook(self.workbook)
        self.assertEqual(hashlib.sha256(self.workbook.read_bytes()).hexdigest(), first)
        # A run over a workbook that already has the tab (the repository copy) must not touch any other sheet either.
        with zipfile.ZipFile(tool.DEFAULT_WORKBOOK) as before, zipfile.ZipFile(self.workbook) as after:
            allowed = {tool.TAB, "xl/workbook.xml", "xl/_rels/workbook.xml.rels", "[Content_Types].xml"}
            sheet_entry = bible.resolve_sheets(after)[tool.TAB]
            for name in before.namelist():
                if name in allowed or name == sheet_entry:
                    continue
                self.assertEqual(before.read(name), after.read(name), name)


if __name__ == "__main__":
    unittest.main()
