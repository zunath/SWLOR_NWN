"""WaitOutServerRestart: when a run of a given length has to wait so it stays clear of the 10:00 UTC server restart."""
import contextlib
import datetime
import io
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import WaitOutServerRestart


def at(text):
    return datetime.datetime.fromisoformat(text + "+00:00")


class SecondsToWaitTests(unittest.TestCase):
    def wait(self, now, minutes):
        return WaitOutServerRestart.seconds_to_wait(at(now), minutes * 60)

    def test_a_run_that_ends_well_before_the_restart_does_not_wait(self):
        self.assertEqual(0, self.wait("2026-10-28T04:00:00", 150))
        self.assertEqual(0, self.wait("2026-10-28T07:00:00", 150))

    def test_a_run_that_starts_after_the_restart_does_not_wait(self):
        self.assertEqual(0, self.wait("2026-10-28T10:01:30", 150))
        self.assertEqual(0, self.wait("2026-10-28T10:30:00", 150))
        self.assertEqual(0, self.wait("2026-10-28T23:59:00", 150), "the next restart is ten hours away")

    def test_a_run_that_would_cross_the_restart_waits_until_just_after_it(self):
        # 08:00 plus 150 minutes ends at 10:30; resume at 10:01:30.
        self.assertEqual(2 * 3600 + 90, self.wait("2026-10-28T08:00:00", 150))
        self.assertEqual(90, self.wait("2026-10-28T10:00:00", 150))

    def test_a_start_inside_the_window_waits_for_it_to_close(self):
        self.assertEqual(80, self.wait("2026-10-28T10:00:10", 150))
        self.assertEqual(1, self.wait("2026-10-28T10:01:29", 150))

    def test_the_margin_before_the_restart_counts(self):
        # A run ending at 09:59:00 or earlier fits; one second later does not.
        self.assertEqual(0, self.wait("2026-10-28T07:29:00", 150))
        self.assertEqual(2 * 3600 + 32 * 60, self.wait("2026-10-28T07:29:30", 150))

    def test_a_run_that_started_before_midnight_is_checked_against_the_next_day(self):
        self.assertEqual(0, self.wait("2026-10-27T23:00:00", 150))
        self.assertEqual(0, self.wait("2026-10-27T23:00:00", 600), "ten hours from 23:00 ends at 09:00")
        self.assertEqual(11 * 3600 + 90, self.wait("2026-10-27T23:00:00", 660))


class CommandLineTests(unittest.TestCase):
    def run_main(self, *arguments):
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            code = WaitOutServerRestart.main(list(arguments))
        return code, output.getvalue()

    def test_reports_nothing_to_wait_for(self):
        code, output = self.run_main("--run-minutes", "150", "--now", "2026-10-28T04:00:00Z", "--dry-run")
        self.assertEqual(0, code)
        self.assertIn("does not touch the daily 10:00 UTC server restart", output)

    def test_dry_run_names_the_wait_and_does_not_sleep(self):
        code, output = self.run_main("--run-minutes", "150", "--now", "2026-10-28T09:00:00Z", "--dry-run")
        self.assertEqual(0, code)
        self.assertIn("::notice title=Waiting out the daily server restart::", output)
        self.assertIn("Waiting 61 minutes 30 seconds, until 10:01:30 UTC.", output)

    def test_a_run_of_a_day_is_refused(self):
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            WaitOutServerRestart.main(["--run-minutes", "1440", "--dry-run"])


if __name__ == "__main__":
    unittest.main()
