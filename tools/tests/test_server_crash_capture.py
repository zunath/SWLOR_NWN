"""Exercise real GDB and Docker process lifecycles with a native fatal-handler fixture."""

import os
from pathlib import Path
import subprocess
import tempfile
import time
import unittest
import uuid


IMAGE = os.environ.get("SWLOR_CRASH_TEST_IMAGE", "swlor-crash-capture-tests:local")


class ServerCrashCaptureTests(unittest.TestCase):
    def setUp(self):
        self.name = "swlor-crash-test-" + uuid.uuid4().hex[:12]
        self.addCleanup(self.docker, "rm", "-f", self.name, check=False)

    @staticmethod
    def docker(*arguments, check=True, timeout=60):
        return subprocess.run(["docker", *arguments], check=check, capture_output=True,
                              text=True, timeout=timeout)

    def create(self, mode, *extra, enabled=True, policy=None, server_environment=None):
        arguments = ["create", "--name", self.name, "--network", "none",
                     "-e", "SWLOR_CRASH_CAPTURE_TIMEOUT_SECONDS=3"]
        if enabled is not None:
            arguments.extend(["-e", f"SWLOR_CRASH_CAPTURE={int(enabled)}"])
        if server_environment is not None:
            arguments.extend(["-e", f"SWLOR_ENVIRONMENT={server_environment}"])
        if policy:
            arguments.extend(["--restart", policy])
        self.docker(*arguments, IMAGE, mode, *extra)

    def run_to_exit(self):
        result = self.docker("start", "-a", self.name, check=False, timeout=25)
        code = self.docker("inspect", "-f", "{{.State.ExitCode}}", self.name).stdout.strip()
        return int(code), result.stdout + result.stderr

    def wait_ready(self):
        deadline = time.monotonic() + 20
        self.docker("start", self.name)
        while time.monotonic() < deadline:
            output = self.docker("logs", self.name).stdout
            if "fixture ready" in output:
                return
            time.sleep(0.1)
        self.fail("Native fixture did not start")

    def test_normal_exit_releases_container(self):
        self.create("normal")
        code, output = self.run_to_exit()
        self.assertEqual(code, 0, output)
        self.assertIn("Native crash capture armed", output)
        self.assertNotIn("Saved native crash trace", output)

    def test_default_production_arms_capture(self):
        self.create("normal", enabled=None, server_environment="Production")
        code, output = self.run_to_exit()
        self.assertEqual(code, 0, output)
        self.assertIn("Native crash capture armed", output)

    def test_default_development_keeps_debugger_free(self):
        self.create("normal", enabled=None, server_environment="dev")
        code, output = self.run_to_exit()
        self.assertEqual(code, 0, output)
        self.assertNotIn("Native crash capture armed", output)

    def test_fatal_handler_writes_trace_and_terminates_looping_process(self):
        self.create("crash")
        code, output = self.run_to_exit()
        self.assertEqual(code, 139, output)
        self.assertIn("Saved native crash trace", output)
        with tempfile.TemporaryDirectory() as scratch:
            self.docker("cp", f"{self.name}:/nwn/home/logs/crashes/.", scratch)
            reports = list(Path(scratch).glob("nwserver-crash-*.log"))
            self.assertEqual(len(reports), 1)
            report = reports[0].read_text()
            self.assertIn("nwnx_signal_handler", report)
            self.assertIn("trigger_crash", report)

    def assert_shutdown_signal(self, stop_signal):
        self.create("wait")
        self.wait_ready()
        self.docker("kill", "--signal", stop_signal, self.name)
        self.docker("wait", self.name, timeout=15)
        code = self.docker("inspect", "-f", "{{.State.ExitCode}}", self.name).stdout.strip()
        self.assertEqual(code, "0")

    def test_sigint_exits_without_stopping_in_gdb(self):
        self.assert_shutdown_signal("SIGINT")

    def test_sigterm_exits_without_stopping_in_gdb(self):
        self.assert_shutdown_signal("SIGTERM")

    def test_stuck_trace_stop_is_bounded(self):
        self.create("wait")
        self.wait_ready()
        freeze = (
            "import os,signal; from pathlib import Path; "
            "p=int(next(Path('/tmp').glob('swlor-crash-*/pid')).read_text()); "
            "s=Path('/proc/%s/status'%p).read_text(); "
            "d=int(next(x for x in s.splitlines() if x.startswith('PPid:')).split()[1]); "
            "os.kill(d,signal.SIGSTOP); os.kill(p,signal.SIGSTOP)"
        )
        self.docker("exec", self.name, "python3", "-c", freeze)
        code = self.docker("wait", self.name, timeout=15).stdout.strip()
        output = self.docker("logs", self.name)
        self.assertEqual(code, "139", output.stdout + output.stderr)
        self.assertIn("timed out", output.stdout + output.stderr)

    def test_arguments_and_exit_code_survive_debugger(self):
        self.create("arguments", "a space 'quote' $literal ; value")
        code, output = self.run_to_exit()
        self.assertEqual(code, 42, output)

    def test_capture_can_be_disabled(self):
        self.create("arguments", "a space 'quote' $literal ; value", enabled=False)
        code, output = self.run_to_exit()
        self.assertEqual(code, 42, output)
        self.assertNotIn("Native crash capture armed", output)

    def test_docker_always_restarts_after_clean_daily_exit(self):
        self.create("restart", policy="always")
        self.wait_ready()
        deadline = time.monotonic() + 35
        while time.monotonic() < deadline:
            count = int(self.docker("inspect", "-f", "{{.RestartCount}}", self.name).stdout.strip())
            if count:
                return
            time.sleep(0.5)
        self.fail("Docker did not restart the cleanly exited server")


if __name__ == "__main__":
    unittest.main()
