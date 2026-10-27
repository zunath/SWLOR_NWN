"""CoverageSummary: per-assembly and per-top-level-folder line and branch tallies from a Cobertura file."""
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import CoverageSummary

COBERTURA = """<?xml version="1.0"?>
<coverage>
  <packages>
    <package name="SWLOR.Game.Server">
      <classes>
        <class name="A.Stat" filename="C:\\repo\\SWLOR.Game.Server\\Service\\Stat.cs">
          <lines>
            <line number="1" hits="3" branch="False" />
            <line number="2" hits="0" branch="True" condition-coverage="50% (1/2)" />
            <line number="3" hits="1" branch="True" condition-coverage="100% (2/2)" />
          </lines>
        </class>
        <class name="A.Stat.Nested" filename="C:\\repo\\SWLOR.Game.Server\\Service\\Stat.cs">
          <lines>
            <line number="3" hits="0" branch="True" condition-coverage="0% (0/2)" />
            <line number="4" hits="0" branch="False" />
          </lines>
        </class>
        <class name="A.Door" filename="C:/repo/SWLOR.Game.Server/Feature/Door.cs">
          <lines><line number="1" hits="2" branch="False" /></lines>
        </class>
        <class name="A.Settings" filename="C:/repo/SWLOR.Game.Server/ApplicationSettings.cs">
          <lines><line number="1" hits="0" branch="False" /></lines>
        </class>
      </classes>
    </package>
    <package name="SWLOR.CLI">
      <classes>
        <class name="C.Run" filename="C:/repo/SWLOR.CLI/Run.cs">
          <lines><line number="1" hits="1" branch="False" /></lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>
"""


class CoverageSummaryTests(unittest.TestCase):
    def read(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "Cobertura.xml"
            path.write_text(COBERTURA, encoding="utf-8")
            return CoverageSummary.read_lines(str(path))

    def test_a_line_two_classes_report_is_counted_once_with_the_best_hits_and_branches(self):
        merged = self.read()
        self.assertEqual((1, 2, 2), merged[("SWLOR.Game.Server", "C:/repo/SWLOR.Game.Server/Service/Stat.cs", 3)])

    def test_folder_is_the_first_directory_below_the_assembly_directory(self):
        self.assertEqual("Service", CoverageSummary.top_level_folder(
            "SWLOR.Game.Server", "C:/repo/SWLOR.Game.Server/Service/Sub/Stat.cs"))
        self.assertEqual(CoverageSummary.ROOT_FILES, CoverageSummary.top_level_folder(
            "SWLOR.Game.Server", "C:/repo/SWLOR.Game.Server/ApplicationSettings.cs"))
        self.assertEqual("Service", CoverageSummary.top_level_folder("SWLOR.Game.Server", "Service/Stat.cs"))

    def test_tally_counts_covered_lines_and_taken_branches(self):
        rows = [(3, 0, 0), (0, 1, 2), (1, 2, 2), (0, 0, 0)]
        self.assertEqual((2, 4, 3, 4), CoverageSummary.tally(rows))
        self.assertEqual("n/a", CoverageSummary.percent(0, 0))
        self.assertEqual("75.0%", CoverageSummary.percent(3, 4))

    def test_tables_break_the_chosen_assembly_down_by_folder_and_end_with_its_total(self):
        text = CoverageSummary.summarise(self.read(), "SWLOR.Game.Server")
        self.assertIn("| SWLOR.CLI | 1 | 1 | 100.0% | 0 | 0 | n/a |", text)
        self.assertIn("| SWLOR.Game.Server | 3 | 6 | 50.0% | 3 | 4 | 75.0% |", text)
        self.assertIn("| Feature | 1 | 1 | 100.0% | 0 | 0 | n/a |", text)
        self.assertIn("| Service | 2 | 4 | 50.0% | 3 | 4 | 75.0% |", text)
        self.assertIn("| (files in the project root) | 0 | 1 | 0.0% | 0 | 0 | n/a |", text)
        self.assertIn("| **SWLOR.Game.Server total** | 3 | 6 | 50.0% | 3 | 4 | 75.0% |", text)
        self.assertLess(text.index("| Feature |"), text.index("| Service |"))
        self.assertLess(text.index("| Service |"), text.index("| (files in the project root) |"))


if __name__ == "__main__":
    unittest.main()
