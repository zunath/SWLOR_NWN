"""check_coverage: dedupe across projects, exclusions, area split, floors that fail on a drop and ratchet on a rise."""
import contextlib
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "Refactor"))
import check_coverage


def cobertura(classes):
    """classes: [(filename, [(line, hits), ...])] -> Cobertura XML text."""
    body = []
    for filename, lines in classes:
        rows = "".join(f'<line number="{n}" hits="{h}" branch="False" />' for n, h in lines)
        body.append(f'<class name="C" filename="{filename}"><lines>{rows}</lines></class>')
    return '<?xml version="1.0"?><coverage><packages><package name="p"><classes>' + "".join(body) + "</classes></package></packages></coverage>"


SERVICE = "C:\\repo\\SWLOR.Game.Server\\Service\\Bank.cs"
SERVICE_DIR = "C:\\repo\\SWLOR.Game.Server\\Service\\BankService\\Page.cs"
DOMAIN = "C:/repo/SWLOR.Game.Server/Domain/Economy/Bank.cs"
FEATURE = "C:/repo/SWLOR.Game.Server/Feature/Door.cs"
DESIGNER = "C:/repo/SWLOR.Game.Server/Feature/Door.Designer.cs"
SIM = "C:/repo/SWLOR.CombatSim/Sim.cs"


class CheckCoverageTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.dir = Path(self.temp.name)
        self.floors = self.dir / "floors.json"
        self.addCleanup(self.temp.cleanup)

    def write_run(self, name, classes):
        path = self.dir / f"{name}.cobertura.xml"
        path.write_text(cobertura(classes), encoding="utf-8")
        return str(path)

    def write_floors(self, overall, areas, excludes=(), tolerance=0.0):
        self.floors.write_text(json.dumps({"overall": overall, "tolerance": tolerance, "areas": areas,
                                           "excludes": list(excludes)}), encoding="utf-8")

    def run_tool(self, *args):
        out, err = io.StringIO(), io.StringIO()
        with contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
            code = check_coverage.main(["--floors", str(self.floors), *args])
        return code, out.getvalue(), err.getvalue()

    def sample(self):
        # Service/Bank.cs 2 of 4, Service/BankService 1 of 1, Domain/Economy 1 of 2, Feature 1 of 1, sim 1 of 2.
        return self.write_run("a", [(SERVICE, [(1, 1), (2, 1), (3, 0), (4, 0)]), (SERVICE_DIR, [(1, 1)]),
                                    (DOMAIN, [(1, 1), (2, 0)]), (FEATURE, [(1, 1)]), (SIM, [(1, 1), (2, 0)])])

    def test_lines_hit_by_any_project_count_once(self):
        first = self.write_run("a", [(SERVICE, [(1, 1), (2, 0)])])
        second = self.write_run("b", [(SERVICE, [(1, 0), (2, 3)])])
        lines = check_coverage.read_lines([first, second])
        areas, overall, _ = check_coverage.measure(lines, [])
        self.assertEqual(areas["Service"], [2, 2])
        self.assertEqual(overall, [2, 2])

    def test_relative_file_names_resolve_against_the_source_root(self):
        path = self.dir / "formats.cobertura.xml"
        path.write_text(r'<coverage><sources><source>C:\repo\SWLOR.NWN.Formats\</source></sources><packages><package name="p"><classes>'
                        r'<class name="C" filename="Bif\BifFile.cs"><lines><line number="1" hits="1" /></lines></class>'
                        '</classes></package></packages></coverage>', encoding="utf-8")
        areas, _, _ = check_coverage.measure(check_coverage.read_lines([str(path)]), [])
        self.assertEqual(areas, {"SWLOR.NWN.Formats": [1, 1]})

    def test_areas_split_domain_service_and_other_assemblies(self):
        areas, overall, _ = check_coverage.measure(check_coverage.read_lines([self.sample()]), [])
        self.assertEqual(areas["Service"], [2, 4])
        self.assertEqual(areas["Service/folders"], [1, 1])
        self.assertEqual(areas["Domain/Economy"], [1, 2])
        self.assertEqual(areas["Feature"], [1, 1])
        self.assertEqual(areas["SWLOR.CombatSim"], [1, 2])
        self.assertEqual(overall, [5, 8], "other assemblies stay out of the overall figure")

    def test_exclusions_drop_matching_files(self):
        path = self.write_run("a", [(FEATURE, [(1, 1)]), (DESIGNER, [(1, 0), (2, 0)])])
        excludes = [{"pattern": "**/*.Designer.cs", "reason": "generated"}]
        areas, overall, excluded = check_coverage.measure(check_coverage.read_lines([path]), excludes)
        self.assertEqual(overall, [1, 1])
        self.assertEqual(excluded, 2)
        self.assertEqual(areas["Feature"], [1, 1])

    def test_exact_floor_passes_and_reports_nothing_to_raise(self):
        self.write_floors(62, {"Service": 50, "Service/folders": 100, "Domain/Economy": 50, "Feature": 100, "SWLOR.CombatSim": 50})
        code, out, _ = self.run_tool(self.sample())
        self.assertEqual(code, 0)
        self.assertNotIn("BELOW", out)
        self.assertNotIn("rose above", out)

    def test_a_drop_in_one_area_fails_even_when_overall_holds(self):
        self.write_floors(50, {"Service": 60})
        code, out, err = self.run_tool(self.sample())
        self.assertEqual(code, 1)
        self.assertIn("Service: 50.00% is below its floor of 60%", err)
        self.assertIn("BELOW", out)

    def test_a_drop_in_the_overall_figure_fails(self):
        self.write_floors(70, {})
        code, _, err = self.run_tool(self.sample())
        self.assertEqual(code, 1)
        self.assertIn("SWLOR.Game.Server (overall)", err)

    def test_a_rise_passes_says_how_to_raise_and_update_ratchets(self):
        self.write_floors(10, {"Service": 10})
        code, out, _ = self.run_tool(self.sample())
        self.assertEqual(code, 0)
        self.assertIn("--update", out)
        code, out, _ = self.run_tool(self.sample(), "--update")
        self.assertEqual(code, 0)
        saved = json.loads(self.floors.read_text(encoding="utf-8"))
        self.assertEqual(saved["overall"], 62)
        self.assertEqual(saved["areas"]["Service"], 50)
        self.assertEqual(saved["areas"]["Domain/Economy"], 50, "areas without a floor get one")

    def test_update_never_lowers_a_floor(self):
        self.write_floors(80, {"Service": 90})
        code, _, _ = self.run_tool(self.sample(), "--update")
        self.assertEqual(code, 1, "the drop still fails")
        saved = json.loads(self.floors.read_text(encoding="utf-8"))
        self.assertEqual(saved["overall"], 80)
        self.assertEqual(saved["areas"]["Service"], 90)

    def test_tolerance_allows_a_small_dip(self):
        self.write_floors(62, {"Service": 51}, tolerance=1.5)
        code, _, _ = self.run_tool(self.sample())
        self.assertEqual(code, 0)

    def test_an_area_with_a_floor_that_was_not_measured_fails(self):
        self.write_floors(0, {"Service": 10, "Domain/Gone": 50})
        code, out, err = self.run_tool(self.sample())
        self.assertEqual(code, 1)
        self.assertIn("Domain/Gone: no lines were measured", err)
        self.assertIn("NOT MEASURED", out)

    def test_missing_files_are_an_error(self):
        self.write_floors(0, {})
        code, _, err = self.run_tool(str(self.dir / "none.cobertura.xml"))
        self.assertEqual(code, 2)
        self.assertIn("No coverage files", err)

    def test_directory_input_reads_every_cobertura_file(self):
        self.sample()
        self.write_run("b", [(FEATURE, [(2, 1)])])
        self.write_floors(0, {})
        code, out, _ = self.run_tool(str(self.dir))
        self.assertEqual(code, 0)
        self.assertIn("2 file(s)", out)

    def test_floor_equal_to_rounded_down_measurement_survives_float_noise(self):
        self.assertEqual(check_coverage.rounded_down(100.0 * 29 / 100), 29)
        self.assertEqual(check_coverage.rounded_down(100.0 * 57 / 100), 57)


if __name__ == "__main__":
    unittest.main()
