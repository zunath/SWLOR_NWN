"""GDB batch driver; imported by GDB, never by the game runtime."""

from datetime import datetime, timezone
import os
from pathlib import Path

import gdb


control = Path(os.environ["SWLOR_CRASH_CAPTURE_CONTROL"])
report_directory = Path(os.environ["SWLOR_CRASH_CAPTURE_DIRECTORY"])
exit_code = None
last_stop = None


def on_exit(event):
    global exit_code
    exit_code = getattr(event, "exit_code", 1)


def on_stop(event):
    global last_stop
    last_stop = event


def kill_inferior():
    if gdb.selected_inferior().pid:
        gdb.execute("kill", to_string=True)


def capture(reason):
    (control / "capturing").touch()
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S.%fZ")
    report = report_directory / f"nwserver-crash-{stamp}-{gdb.selected_inferior().pid}.log"
    descriptor = os.open(report, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(descriptor, "w", encoding="utf-8") as output:
        output.write(f"Native crash capture at {stamp}: {reason}\n")
        output.flush()
        for command in ("bt 80", "thread apply all -c bt 80", "info sharedlibrary", "info proc mappings"):
            output.write(f"\n--- {command} ---\n")
            try:
                output.write(gdb.execute(command, to_string=True))
            except Exception as error:
                output.write(f"Capture command failed: {error}\n")
            output.flush()
    print(f"[crash-capture] Saved native crash trace: {report}", flush=True)


try:
    gdb.execute("set confirm off")
    gdb.execute("set pagination off")
    gdb.execute("set print thread-events off")
    gdb.execute("set startup-with-shell off")
    gdb.execute("set disable-randomization off")
    gdb.execute("set debuginfod enabled off")
    gdb.execute("set breakpoint pending on")
    gdb.execute("handle SIGINT SIGTERM SIGSEGV SIGILL SIGFPE SIGABRT SIGPIPE SIG34 nostop noprint pass",
                to_string=True)
    breakpoint = gdb.Breakpoint("nwnx_signal_handler", internal=True)
    gdb.events.exited.connect(on_exit)
    gdb.events.stop.connect(on_stop)
    gdb.execute("starti", to_string=True)
    (control / "pid").write_text(str(gdb.selected_inferior().pid))
    print("[crash-capture] Native crash capture armed.", flush=True)
    while exit_code is None:
        last_stop = None
        gdb.execute("continue", to_string=True)
        if isinstance(last_stop, gdb.BreakpointEvent) and breakpoint in last_stop.breakpoints:
            capture("NWNX fatal signal handler")
            kill_inferior()
            gdb.execute("quit 139")
    gdb.execute(f"quit {exit_code}")
except Exception as error:
    print(f"[crash-capture] Debugger failed: {error}", flush=True)
    try:
        kill_inferior()
    finally:
        gdb.execute("quit 70")
