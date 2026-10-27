#!/usr/bin/env python3
"""Keeps an engine-test run clear of the game server's daily automated restart: waits briefly, or refuses to start.

SWLOR.Game.Server/Feature/ServerTasks.cs shuts the server down for the whole 30 seconds from 10:00:00 UTC every day, tests
running or not (the module heartbeat checks every six seconds). An engine-test run that crosses that moment is killed
without a report. .github/workflows/engine-tests.yml calls this first thing in the job:

    python tools/WaitOutServerRestart.py --run-minutes 305 --max-wait-minutes 15

--run-minutes is how long the whole job is expected to need (setup plus the run). When it fits before the next restart, or
starts after the last one, nothing happens. When it would cross the restart, the tool sleeps until 10:01:30 UTC if that is
at most --max-wait-minutes away, and otherwise exits 1 with an error annotation: a job has six hours in all, so a long wait
leaves no room for the run. --dry-run only prints. --now takes an ISO 8601 UTC time for testing.

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
    parser.add_argument("--run-minutes", type=int, required=True, help="how long the job is expected to need")
    parser.add_argument("--max-wait-minutes", type=int, default=15,
                        help="the longest wait that is worth it; beyond that the tool exits 1 (default 15)")
    parser.add_argument("--now", help="pretend it is this ISO 8601 UTC time (tests)")
    parser.add_argument("--dry-run", action="store_true", help="print what would happen without sleeping")
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
    if wait > args.max_wait_minutes * 60:
        print(f"::error title=Too close to the daily server restart::The game server restarts itself at "
              f"{RESTART_AT:%H:%M} UTC and a run of up to {args.run_minutes} minutes from {now:%H:%M:%S} UTC would be "
              f"killed by it. Waiting until {resume:%H:%M:%S} UTC would take {wait // 60} minutes, more than the "
              f"{args.max_wait_minutes} this job can spare. Nothing is wrong with the code: run it again after "
              f"{resume:%H:%M} UTC, or earlier than about "
              f"{(resume - datetime.timedelta(minutes=args.run_minutes + 2)):%H:%M} UTC.")
        return 1
    print(f"::notice title=Waiting out the daily server restart::The game server restarts itself at "
          f"{RESTART_AT:%H:%M} UTC and a run of up to {args.run_minutes} minutes from {now:%H:%M:%S} UTC would be "
          f"killed by it. Waiting {wait // 60} minutes {wait % 60} seconds, until {resume:%H:%M:%S} UTC.")
    sys.stdout.flush()
    if not args.dry_run:
        time.sleep(wait)
    return 0


if __name__ == "__main__":
    sys.exit(main())
