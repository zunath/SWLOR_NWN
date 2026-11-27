"""EngineTestSummary: verdict, failure table and annotations from an engine-test report and the runner log."""
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import EngineTestSummary


def result(name, outcome, category="Combat", message=None, ms=1000):
    return {"Name": name, "Category": category, "Outcome": outcome, "Message": message, "DurationMilliseconds": ms}


def report(*results):
    counts = {0: 0, 1: 0, 2: 0}
    for item in results:
        counts[item["Outcome"]] += 1
    return {"StartedUtc": "2026-10-27T09:00:00Z", "FinishedUtc": "2026-10-27T10:00:00Z", "Total": len(results),
            "Passed": counts[0], "Failed": counts[1], "Skipped": counts[2], "Results": list(results)}


class SummaryTests(unittest.TestCase):
    def test_all_passed(self):
        verdict, markdown, notes = EngineTestSummary.summarise(report(result("A", 0), result("B", 0)), "", 150)
        self.assertEqual("PASSED", verdict)
        self.assertEqual([], notes)
        text = "\n".join(markdown)
        self.assertIn("## Engine tests: PASSED", text)
        self.assertIn("Total 2, passed 2, failed 0, skipped 0", text)
        self.assertIn("(not reached)", text)
        self.assertNotIn("### Failed tests", text)

    def test_failed_tests_are_listed_and_annotated(self):
        failing = result("Bad|Name", 1, "Ability", "Expected 5\nbut was 4")
        verdict, markdown, notes = EngineTestSummary.summarise(report(result("A", 0), failing), "", 150)
        self.assertEqual("1 FAILED", verdict)
        text = "\n".join(markdown)
        self.assertIn("### Failed tests", text)
        self.assertIn("| Ability | Bad\\|Name | Expected 5 but was 4 |", text)
        self.assertEqual(["::error title=Engine test failed::Ability/Bad|Name: Expected 5%0Abut was 4"], notes)

    def test_annotations_are_capped_but_the_table_is_complete(self):
        many = [result(f"T{i}", 1, message="no") for i in range(14)]
        _, markdown, notes = EngineTestSummary.summarise(report(*many), "", None)
        self.assertEqual(11, len(notes))
        self.assertIn("4 more failed tests", notes[-1])
        self.assertEqual(14, sum(1 for line in markdown if line.startswith("| Combat | T") and line.endswith("| no |")))

    def test_timeout_is_named_and_is_not_called_a_failed_test(self):
        log = "docker compose up\nTIMED OUT after 150 minute(s) - the run was killed.\n"
        verdict, markdown, notes = EngineTestSummary.summarise(None, log, 150)
        self.assertEqual("TIMED OUT after 150 minutes (not a test failure)", verdict)
        self.assertEqual(1, len(notes))
        self.assertIn("timeout, not a failed test", notes[0])
        self.assertIn("## Engine tests: TIMED OUT after 150 minutes", markdown[0])

    def test_timeout_with_a_partial_report_reports_the_failures_too(self):
        log = "TIMED OUT after 90 minute(s)"
        verdict, markdown, notes = EngineTestSummary.summarise(report(result("A", 1, message="boom")), log, 90)
        self.assertTrue(verdict.startswith("TIMED OUT"))
        self.assertIn("1 test(s) had already failed", notes[0])
        self.assertIn("### Failed tests", "\n".join(markdown))

    def test_missing_report_without_a_timeout_is_a_crash(self):
        verdict, _, notes = EngineTestSummary.summarise(None, "docker compose exit code: 137", 150)
        self.assertTrue(verdict.startswith("NO REPORT"))
        self.assertIn("never wrote", notes[0])

    def test_the_daily_server_restart_is_named_and_is_not_called_a_failed_test(self):
        log = "Server shutting down for automated restart.\nNo report found at /nwn/home/app_logs/x.json\n"
        verdict, markdown, notes = EngineTestSummary.summarise(None, log, 150)
        self.assertTrue(verdict.startswith("CUT OFF BY THE SERVER'S DAILY AUTOMATED RESTART"))
        self.assertIn("not a failed test", notes[0])
        self.assertIn("## Engine tests: CUT OFF", markdown[0])

    def test_a_restart_message_next_to_a_complete_report_does_not_hide_the_results(self):
        log = "Server shutting down for automated restart.\n"
        verdict, _, notes = EngineTestSummary.summarise(report(result("A", 0)), log, 150)
        self.assertEqual("PASSED", verdict)
        self.assertEqual([], notes)

    def test_a_green_report_is_not_passed_when_the_runner_failed(self):
        log = "docker compose exit code: 139\nEngine tests failed: server container exited with code 139.\n"
        verdict, markdown, notes = EngineTestSummary.summarise(report(result("A", 0)), log, 150, "failure")
        self.assertTrue(verdict.startswith("RUNNER FAILED"))
        self.assertIn("server container exited with code 139", notes[0])
        self.assertIn("## Engine tests: RUNNER FAILED", markdown[0])

    def test_a_runner_failure_without_a_known_message_still_fails_the_summary(self):
        verdict, _, notes = EngineTestSummary.summarise(report(result("A", 0)), "something else", 150, "failure")
        self.assertTrue(verdict.startswith("RUNNER FAILED"))
        self.assertIn("the runner script failed", notes[0])

    def test_a_successful_runner_keeps_the_passed_verdict(self):
        verdict, _, _ = EngineTestSummary.summarise(report(result("A", 0)), "", 150, "success")
        self.assertEqual("PASSED", verdict)

    def test_failed_tests_win_over_a_failed_runner(self):
        verdict, _, _ = EngineTestSummary.summarise(report(result("A", 1, message="x")), "", 150, "failure")
        self.assertEqual("1 FAILED", verdict)

    def test_empty_report_means_no_tests_ran(self):
        verdict, _, notes = EngineTestSummary.summarise(report(), "", 150)
        self.assertEqual("NO TESTS RAN", verdict)
        self.assertEqual(1, len(notes))

    def test_aborted_suite_warns(self):
        skipped = result("Late", 2, message="Skipped: suite aborted after a test would not stop")
        _, markdown, notes = EngineTestSummary.summarise(report(result("A", 0), skipped), "", 150)
        self.assertEqual(1, len(notes))
        self.assertTrue(notes[0].startswith("::warning title=Engine test run aborted"))
        self.assertIn("### Skipped tests", "\n".join(markdown))

    def test_category_table_and_slowest_tests(self):
        results = [result("A", 0, "Combat", ms=2000), result("B", 1, "Combat", "x", ms=3000),
                   result("C", 0, "Ability", ms=500)]
        _, markdown, _ = EngineTestSummary.summarise(report(*results), "", None)
        text = "\n".join(markdown)
        self.assertIn("| Ability | 1 | 0 | 0 | 0 s |", text)
        self.assertIn("| Combat | 1 | 1 | 0 | 5 s |", text)
        self.assertLess(text.index("| Combat | B | Failed | 3.0 s |"), text.index("| Combat | A | Passed | 2.0 s |"))


class CommandLineTests(unittest.TestCase):
    def test_writes_markdown_to_a_file_and_annotations_to_stdout(self):
        with tempfile.TemporaryDirectory() as folder:
            folder = Path(folder)
            (folder / "report.json").write_text(json.dumps(report(result("A", 1, message="boom"))), encoding="utf-8")
            (folder / "run.log").write_text("ok", encoding="utf-8")
            summary = folder / "summary.md"
            summary.write_text("earlier\n", encoding="utf-8")
            import contextlib
            import io
            captured = io.StringIO()
            with contextlib.redirect_stdout(captured):
                code = EngineTestSummary.main(["--report", str(folder / "report.json"), "--log", str(folder / "run.log"),
                                               "--annotations", "--markdown-out", str(summary)])
            self.assertEqual(0, code)
            self.assertEqual("::error title=Engine test failed::Combat/A: boom\n", captured.getvalue())
            written = summary.read_text(encoding="utf-8")
            self.assertTrue(written.startswith("earlier\n## Engine tests: 1 FAILED"))

    def test_a_missing_report_file_is_handled(self):
        with tempfile.TemporaryDirectory() as folder:
            self.assertIsNone(EngineTestSummary.read_report(str(Path(folder) / "nope.json")))
            self.assertEqual("", EngineTestSummary.read_log(str(Path(folder) / "nope.log")))


if __name__ == "__main__":
    unittest.main()
