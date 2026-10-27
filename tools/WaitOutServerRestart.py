#!/usr/bin/env python3
"""Waits until a run of a given length can finish before the game server's daily automated restart, or start after it.

SWLOR.Game.Server/Feature/ServerTasks.cs shuts the server down for the whole 30 seconds from 10:00:00 UTC every day, tests
running or not (the module heartbeat checks every six seconds). An engine-test run that crosses that moment is killed
without a report. .github/workflows/engine-tests.yml calls this before the run:

    python tools/WaitOutServerRestart.py --run-minutes 150

It prints how long it waits (nothing when the run fits) and sleeps that long. --dry-run only prints. --now takes an
ISO 8601 UTC time for testing.

This is a workaround for the server's own behaviour; once ProcessAutoRestart skips engine-test runs, delete the call.
"""

import argparse
import datetime
import math
import sys
import time

RESTART_AT = datetime.time(10, 0, 0)  # ServerTasks.RestartTime, UTC
RESTART_WINDOW_SECONDS = 30  # ServerTasks restarts within 30 seconds after RestartTime
MARGIN_SECONDS = 60  # kept clear on both sides of the window (clock skew, the six-second heartbeat, shutdown time)


def seconds_to_wait(now, run_seconds):
    """Seconds to wait before starting a run of run_seconds so that it stays clear of the restart; 0 when it already does.

    now is a timezone-aware datetime. A run of a day or longer cannot avoid the restart and is not handled.
    """
    run_end = now + datetime.timedelta(seconds=run_seconds)
    wait = 0.0
    for day in (-1, 0, 1):
        restart = datetime.datetime.combine(now.date() + datetime.timedelta(days=day), RESTART_AT,
                                            tzinfo=datetime.timezone.utc)
        window_start = restart - datetime.timedelta(seconds=MARGIN_SECONDS)
        window_end = restart + datetime.timedelta(seconds=RESTART_WINDOW_SECONDS + MARGIN_SECONDS)
        if now < window_end and run_end > window_start:
            wait = max(wait, (window_end - now).total_seconds())
    return math.ceil(wait)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--run-minutes", type=int, required=True, help="the longest the run may take")
    parser.add_argument("--now", help="pretend it is this ISO 8601 UTC time (tests)")
    parser.add_argument("--dry-run", action="store_true", help="print the wait without sleeping")
    args = parser.parse_args(argv)

    if args.run_minutes * 60 >= 24 * 3600:
        parser.error("a run of a day or longer cannot avoid the daily restart")
    now = (datetime.datetime.fromisoformat(args.now.replace("Z", "+00:00")) if args.now
           else datetime.datetime.now(datetime.timezone.utc))
    wait = seconds_to_wait(now, args.run_minutes * 60)
    if wait == 0:
        print(f"A run of up to {args.run_minutes} minutes from {now:%H:%M:%S} UTC does not touch the daily "
              f"{RESTART_AT:%H:%M} UTC server restart.")
        return 0
    resume = now + datetime.timedelta(seconds=wait)
    print(f"::notice title=Waiting out the daily server restart::The game server restarts itself at "
          f"{RESTART_AT:%H:%M} UTC and a run of up to {args.run_minutes} minutes from {now:%H:%M:%S} UTC would be "
          f"killed by it. Waiting {wait // 60} minutes {wait % 60} seconds, until {resume:%H:%M:%S} UTC.")
    sys.stdout.flush()
    if not args.dry_run:
        time.sleep(wait)
    return 0


if __name__ == "__main__":
    sys.exit(main())
