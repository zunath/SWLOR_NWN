#!/usr/bin/env python3
"""Summarise an engine-test run (engine-test-results.json plus the runner log) as Markdown and workflow annotations.

.github/workflows/engine-tests.yml runs it after scripts/run-engine-tests.sh and appends the Markdown to the job
summary. It never decides the job result (the runner's exit code does); it explains it, and says so plainly when the
run ended on the wall clock instead of on a failed test. Run it locally the same way:

    python tools/EngineTestSummary.py --report <report.json> --log <runner log> --timeout-minutes 150 --annotations

Markdown goes to stdout, or is appended to the file named by --markdown-out. With --annotations, GitHub "::error::" and
"::warning::" lines go to stdout (the Actions log turns them into annotations).
"""

import argparse
import json
import re
import sys
from collections import OrderedDict

OUTCOMES = {0: "Passed", 1: "Failed", 2: "Skipped"}
TIMED_OUT = re.compile(r"TIMED OUT after (\d+) minute")
RESTARTED = re.compile(r"Server shutting down for automated restart")
# The lines run-engine-tests.sh prints when it fails although the report may be green.
RUNNER_FAILURE = re.compile(r"Engine tests failed: server container exited with code \d+\.?|"
                            r"Could not (?:tear down|restore|prepare)[^\r\n]*")
# Only this many failures become annotations (GitHub shows at most ten per step); the Markdown lists them all.
MAX_ANNOTATIONS = 10
SLOWEST = 10


def escape_data(text):
    """Escapes a workflow command's message the way the Actions runner expects."""
    return text.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")


def escape_cell(text):
    return (text or "").replace("|", "\\|").replace("\r", " ").replace("\n", " ")


def read_report(path):
    """Returns the parsed report, or None when the file is missing or unreadable."""
    try:
        with open(path, encoding="utf-8-sig") as handle:
            return json.load(handle)
    except (OSError, ValueError):
        return None


def read_log(path):
    if not path:
        return ""
    try:
        with open(path, encoding="utf-8", errors="replace") as handle:
            return handle.read()
    except OSError:
        return ""


def outcome_name(result):
    return OUTCOMES.get(result.get("Outcome"), str(result.get("Outcome")))


def category_rows(results):
    rows = OrderedDict()
    for result in sorted(results, key=lambda item: (item.get("Category") or "", item.get("Name") or "")):
        row = rows.setdefault(result.get("Category") or "(none)", {"Passed": 0, "Failed": 0, "Skipped": 0, "ms": 0})
        row[outcome_name(result)] = row.get(outcome_name(result), 0) + 1
        row["ms"] += result.get("DurationMilliseconds") or 0
    return rows


def summarise(report, log, timeout_minutes=None, runner_outcome=None):
    """Returns (verdict, markdown lines, annotation lines).

    runner_outcome is the result of the step that ran scripts/run-engine-tests.sh ("success", "failure", ...). A green
    report does not make the run green when the runner failed: it also fails on the server container's exit code.
    """
    notes, markdown = [], []
    timed_out = TIMED_OUT.search(log)
    results = (report or {}).get("Results") or []
    failed = [item for item in results if outcome_name(item) == "Failed"]
    skipped = [item for item in results if outcome_name(item) == "Skipped"]

    if timed_out:
        minutes = timed_out.group(1)
        verdict = f"TIMED OUT after {minutes} minutes (not a test failure)"
        notes.append(
            "::error title=Engine tests timed out::The run hit its {0}-minute wall clock and was killed, so the "
            "suite did not finish. This is a timeout, not a failed test{1}. Look for a stalled test at the end of "
            "the log, or raise the limit.".format(
                minutes, f"; {len(failed)} test(s) had already failed" if failed else ""))
    elif report is None and RESTARTED.search(log):
        verdict = "CUT OFF BY THE SERVER'S DAILY AUTOMATED RESTART (10:00 UTC), not a test failure"
        notes.append("::error title=Engine tests cut off by the daily server restart::The game server restarts itself "
                     "at 10:00 UTC (ServerTasks.ProcessAutoRestart) and this run was still going. No report was "
                     "written. This is not a failed test; rerun it clear of 10:00 UTC.")
    elif report is None:
        verdict = "NO REPORT (the server crashed or was killed before it wrote one)"
        notes.append("::error title=Engine tests produced no report::The server never wrote "
                     "engine-test-results.json. Check the server logs in the uploaded artifact.")
    elif report.get("Total", 0) == 0:
        verdict = "NO TESTS RAN"
        notes.append("::error title=No engine tests ran::The report holds no results. Check the filter and the log.")
    elif failed:
        verdict = f"{len(failed)} FAILED"
    elif runner_outcome and runner_outcome != "success":
        verdict = "RUNNER FAILED (every test passed, but the run did not end cleanly)"
        reason = RUNNER_FAILURE.search(log)
        notes.append("::error title=Engine test run failed::Every test in the report passed, but "
                     + (escape_data(reason.group(0).strip()) if reason else "the runner script failed")
                     + ". See the end of the log.")
    else:
        verdict = "PASSED"

    markdown.append(f"## Engine tests: {verdict}")
    markdown.append("")
    if timeout_minutes and not timed_out:
        markdown.append(f"Wall-clock limit for the test run: {timeout_minutes} minutes (not reached).")
        markdown.append("")

    if report is not None:
        markdown.append(
            f"Total {report.get('Total', 0)}, passed {report.get('Passed', 0)}, failed {report.get('Failed', 0)}, "
            f"skipped {report.get('Skipped', 0)}. Started {report.get('StartedUtc', '?')}, "
            f"finished {report.get('FinishedUtc', '?')}.")
        markdown.append("")

    if failed:
        markdown.append("### Failed tests")
        markdown.append("")
        markdown.append("| Category | Test | Message |")
        markdown.append("|---|---|---|")
        for item in failed:
            markdown.append(f"| {escape_cell(item.get('Category'))} | {escape_cell(item.get('Name'))} | "
                            f"{escape_cell(item.get('Message'))} |")
        markdown.append("")
        for item in failed[:MAX_ANNOTATIONS]:
            notes.append("::error title=Engine test failed::" + escape_data(
                f"{item.get('Category')}/{item.get('Name')}: {item.get('Message') or 'no message'}"))
        if len(failed) > MAX_ANNOTATIONS:
            notes.append(f"::error title=More engine test failures::{len(failed) - MAX_ANNOTATIONS} more failed "
                         "tests are listed in the job summary.")

    if skipped:
        markdown.append("### Skipped tests")
        markdown.append("")
        markdown.append("| Category | Test | Reason |")
        markdown.append("|---|---|---|")
        for item in skipped:
            markdown.append(f"| {escape_cell(item.get('Category'))} | {escape_cell(item.get('Name'))} | "
                            f"{escape_cell(item.get('Message'))} |")
        markdown.append("")
        aborted = [item for item in skipped if "abort" in (item.get("Message") or "").lower()]
        if aborted:
            notes.append("::warning title=Engine test run aborted::A test did not stop after it timed out, so "
                         f"{len(aborted)} later test(s) were skipped. See the skipped tests in the job summary.")

    if results:
        markdown.append("### Per category")
        markdown.append("")
        markdown.append("| Category | Passed | Failed | Skipped | Time |")
        markdown.append("|---|---|---|---|---|")
        for category, row in category_rows(results).items():
            markdown.append(f"| {escape_cell(category)} | {row['Passed']} | {row['Failed']} | {row['Skipped']} | "
                            f"{row['ms'] / 1000:.0f} s |")
        markdown.append("")
        slowest = sorted(results, key=lambda item: item.get("DurationMilliseconds") or 0, reverse=True)[:SLOWEST]
        markdown.append(f"<details><summary>{len(slowest)} slowest tests</summary>")
        markdown.append("")
        markdown.append("| Category | Test | Outcome | Time |")
        markdown.append("|---|---|---|---|")
        for item in slowest:
            markdown.append(f"| {escape_cell(item.get('Category'))} | {escape_cell(item.get('Name'))} | "
                            f"{outcome_name(item)} | {(item.get('DurationMilliseconds') or 0) / 1000:.1f} s |")
        markdown.append("")
        markdown.append("</details>")
        markdown.append("")

    return verdict, markdown, notes


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--report", required=True, help="engine-test-results.json (may be missing)")
    parser.add_argument("--log", help="the runner's output, searched for the wall-clock timeout message")
    parser.add_argument("--timeout-minutes", type=int, help="the wall-clock limit the run was given")
    parser.add_argument("--runner-outcome", help="outcome of the runner step (success, failure, cancelled, skipped)")
    parser.add_argument("--annotations", action="store_true", help="print GitHub error/warning annotations")
    parser.add_argument("--markdown-out", help="append the Markdown to this file (the job summary) instead of stdout")
    args = parser.parse_args(argv)

    verdict, markdown, notes = summarise(read_report(args.report), read_log(args.log), args.timeout_minutes,
                                         args.runner_outcome)
    if args.annotations:
        for note in notes:
            print(note)
    text = "\n".join(markdown) + "\n"
    if args.markdown_out:
        with open(args.markdown_out, "a", encoding="utf-8") as handle:
            handle.write(text)
    else:
        sys.stdout.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
