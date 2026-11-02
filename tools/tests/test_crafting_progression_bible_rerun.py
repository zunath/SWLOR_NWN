"""Rerunning UpdateCraftingProgressionBible.py must leave the Design Bible unchanged.

The perk-line tools later took over the Total SP, total rows and descriptions on the Engineering, Fabrication, Gathering
and Agriculture tabs. A rerun used to write them back in the old shape (Engineering's first Overclocker price became a
total formula, Total SP dropped to 0 and the line text was cut from the descriptions).

Run with: python -B -m unittest discover -s tools/tests -p test_crafting_progression_bible_rerun.py
"""
from pathlib import Path
import shutil
import sys
import tempfile
import unittest
import zipfile


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))

import UpdateCraftingProgressionBible as Bible  # noqa: E402


class CraftingProgressionBibleRerunTests(unittest.TestCase):
    def test_rerun_changes_no_workbook_entry(self):
        with tempfile.TemporaryDirectory() as directory:
            copy = Path(directory) / "Bible.xlsx"
            shutil.copyfile(Bible.DEFAULT_WORKBOOK, copy)
            Bible.update_workbook(copy)
            with zipfile.ZipFile(Bible.DEFAULT_WORKBOOK) as before, zipfile.ZipFile(copy) as after:
                self.assertEqual(before.namelist(), after.namelist())
                changed = [name for name in before.namelist() if before.read(name) != after.read(name)]
        self.assertEqual([], changed, "rerunning the tool rewrote these workbook entries")


if __name__ == "__main__":
    unittest.main()
