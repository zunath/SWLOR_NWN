#!/usr/bin/python3
"""Run the server with optional, bounded native crash capture."""

import json
import os
from pathlib import Path
import signal
import subprocess
import sys
import tempfile
import time


def main():
    arguments = sys.argv[1:]
    if not arguments:
        raise SystemExit("No server executable supplied")
    environment = os.environ.copy()
    for name in ("LD_PRELOAD", "LD_LIBRARY_PATH"):
        environment[name] = environment.get("NWN_" + name, "")
    mode = environment.get("SWLOR_CRASH_CAPTURE", "auto").strip().lower()
    enabled = (environment.get("SWLOR_ENVIRONMENT", "").strip().lower() in ("prod", "production")
               if mode == "auto" else mode in ("1", "true", "yes"))
    if not enabled:
        os.execvpe(arguments[0], arguments, environment)

    report_directory = Path(environment.get("SWLOR_CRASH_CAPTURE_DIRECTORY", "/nwn/home/logs/crashes"))
    report_directory.mkdir(mode=0o700, parents=True, exist_ok=True)
    limit = int(environment.get("SWLOR_CRASH_CAPTURE_TIMEOUT_SECONDS", "30"))
    if limit < 1:
        raise SystemExit("SWLOR_CRASH_CAPTURE_TIMEOUT_SECONDS must be positive")
    reports = sorted(report_directory.glob("nwserver-crash-*.log"), key=lambda path: path.stat().st_mtime)
    for report in reports[:-20]:
        report.unlink()

    queued_signals = []
    shutdown_started = None

    def request_shutdown(signum, _frame):
        nonlocal shutdown_started
        queued_signals.append(signum)
        if shutdown_started is None:
            shutdown_started = time.monotonic()

    for signum in (signal.SIGINT, signal.SIGTERM):
        signal.signal(signum, request_shutdown)

    with tempfile.TemporaryDirectory(prefix="swlor-crash-") as control_directory:
        environment["SWLOR_CRASH_CAPTURE_DIRECTORY"] = str(report_directory)
        environment["SWLOR_CRASH_CAPTURE_CONTROL"] = control_directory
        arguments_file = Path(control_directory) / "arguments.json"
        descriptor = os.open(arguments_file, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
        with os.fdopen(descriptor, "w", encoding="utf-8") as output:
            json.dump(arguments, output)
        for name in ("LD_PRELOAD", "LD_LIBRARY_PATH"):
            environment.pop(name, None)
        debugger = subprocess.Popen(
            ["gdb", "-q", "-nx", "--batch", "--return-child-result", "-x",
             "/usr/local/lib/swlor/crash-capture.py", "--args", "/usr/bin/python3",
             "/usr/local/lib/swlor/server-exec.py"],
            env=environment,
        )
        control = Path(control_directory)
        debugger_started = time.monotonic()
        stopped_since = None
        capture_started = None
        server_pid = None
        while debugger.poll() is None:
            try:
                server_pid = int((control / "pid").read_text())
            except (FileNotFoundError, ValueError):
                pass
            if server_pid:
                while queued_signals:
                    try:
                        os.kill(server_pid, queued_signals.pop(0))
                    except ProcessLookupError:
                        break
                try:
                    state = Path(f"/proc/{server_pid}/stat").read_text().rsplit(")", 1)[1].split()[0]
                except (FileNotFoundError, ProcessLookupError):
                    state = None
                if state in ("t", "T"):
                    stopped_since = stopped_since or time.monotonic()
                else:
                    stopped_since = None
            if (control / "capturing").exists():
                capture_started = capture_started or time.monotonic()
            deadlines = [started for started in (stopped_since, capture_started, shutdown_started)
                         if started is not None]
            if server_pid is None:
                deadlines.append(debugger_started)
            if deadlines and time.monotonic() - min(deadlines) > limit:
                print("[crash-capture] Capture/shutdown timed out; releasing container for restart.",
                      file=sys.stderr, flush=True)
                if server_pid:
                    try:
                        os.kill(server_pid, signal.SIGKILL)
                    except ProcessLookupError:
                        pass
                debugger.kill()
                debugger.wait()
                return 139
            time.sleep(0.1)
        return debugger.returncode if debugger.returncode >= 0 else 128 - debugger.returncode


if __name__ == "__main__":
    sys.exit(main())
