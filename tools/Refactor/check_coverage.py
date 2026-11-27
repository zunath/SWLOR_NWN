#!/usr/bin/env python3
"""Coverage ratchet: fail when line coverage of non-generated code drops below the committed floors.

Reads the Cobertura files the unit test run writes (scripts/ci/run-unit-tests.sh puts one per project in
<out>/coverage/), counts every source line once however many test projects hit it, drops the files listed in the
"excludes" section of the floors file, and compares the overall figure and each area with tools/Refactor/CoverageFloors.json.

    python tools/Refactor/check_coverage.py <cobertura file or directory> [...]      check (exit 1 on a drop)
    python tools/Refactor/check_coverage.py <...> --update                            raise floors to the measurement
    python tools/Refactor/check_coverage.py <...> --top-files 25                      also list the biggest gaps

Areas (Readmes/EngineTesting.md, "Coverage floors"):
  SWLOR.Game.Server files    Domain/<Domain>, Service (the static helpers at the folder root, empty since P2-Z3),
                             Service/folders (the old <Name>Service support folders, empty since P2-Z2), and every other top-level
                             folder by name;
                             files in the project root count as "(project root)".
  other SWLOR.* assemblies   one area each, named after the project folder (for example SWLOR.CombatSim).
The overall figure is SWLOR.Game.Server only: that is the code the refactor and the 70% target are about.

A floor is a whole percent. The check compares the exact percentage with the floor, so a floor equal to the
measurement rounded down always passes it. --update raises floors, never lowers them (lowering is a by-hand edit of
the JSON, with the reason in the pull request).
"""

import argparse
import glob
import json
import math
import os
import re
import sys
import xml.etree.ElementTree as ET

GAME_SERVER = "SWLOR.Game.Server"
ROOT_AREA = "(project root)"
DEFAULT_FLOORS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "CoverageFloors.json")
PROJECT_DIR = re.compile(r"^SWLOR\.")


def relative_path(filename):
    """Repository-relative path (forward slashes) starting at the first SWLOR.* project folder."""
    parts = [part for part in filename.replace("\\", "/").split("/") if part]
    for index, part in enumerate(parts):
        if PROJECT_DIR.match(part):
            return "/".join(parts[index:])
    return "/".join(parts)


def expand_inputs(paths):
    files = []
    for path in paths:
        if os.path.isdir(path):
            files.extend(sorted(glob.glob(os.path.join(path, "*.cobertura.xml"))))
        else:
            matches = sorted(glob.glob(path))
            files.extend(matches if matches else [path])
    return files


def is_absolute(filename):
    return filename.startswith(("/", "\\")) or re.match(r"^[A-Za-z]:", filename) is not None


def read_lines(files):
    """Returns {(relative file, line number): hits}; a line hit by any file counts as covered."""
    merged = {}
    for path in files:
        source = ""
        for _event, element in ET.iterparse(path, events=("end",)):
            if element.tag == "source" and not source:
                # Coverlet writes file names relative to <sources> when it maps source roots (the Formats tests do).
                source = (element.text or "").strip()
                continue
            if element.tag != "class":
                continue
            filename = element.get("filename") or ""
            if source and not is_absolute(filename):
                filename = source.rstrip("/\\") + "/" + filename
            filename = relative_path(filename)
            for line in element.findall("lines/line"):
                key = (filename, int(line.get("number")))
                hits = int(line.get("hits", "0"))
                if hits > merged.get(key, -1):
                    merged[key] = hits
            element.clear()
    return merged


def glob_to_regex(pattern):
    """'**' crosses folders, '*' and '?' stay inside one."""
    out = []
    index = 0
    while index < len(pattern):
        if pattern.startswith("**/", index):
            out.append("(?:.*/)?")
            index += 3
        elif pattern.startswith("**", index):
            out.append(".*")
            index += 2
        elif pattern[index] == "*":
            out.append("[^/]*")
            index += 1
        elif pattern[index] == "?":
            out.append("[^/]")
            index += 1
        else:
            out.append(re.escape(pattern[index]))
            index += 1
    return re.compile("^" + "".join(out) + "$", re.IGNORECASE)


def area_of(path):
    """Returns (area name, True when the file belongs to the overall figure)."""
    parts = path.split("/")
    project = parts[0]
    if project != GAME_SERVER:
        return project, False
    rest = parts[1:]
    if len(rest) == 1:
        return ROOT_AREA, True
    top = rest[0]
    if top == "Domain" and len(rest) > 2:
        return "Domain/" + rest[1], True
    if top == "Service":
        return ("Service" if len(rest) == 2 else "Service/folders"), True
    return top, True


def measure(lines, excludes):
    """Returns (areas {name: [covered, valid]}, overall [covered, valid], excluded line count)."""
    patterns = [glob_to_regex(item["pattern"]) for item in excludes]
    areas = {}
    overall = [0, 0]
    excluded = 0
    verdicts = {}
    for (path, _number), hits in lines.items():
        verdict = verdicts.get(path)
        if verdict is None:
            if any(pattern.match(path) for pattern in patterns):
                verdict = (None, False)
            else:
                verdict = area_of(path)
            verdicts[path] = verdict
        area, in_overall = verdict
        if area is None:
            excluded += 1
            continue
        row = areas.setdefault(area, [0, 0])
        row[1] += 1
        row[0] += 1 if hits > 0 else 0
        if in_overall:
            overall[1] += 1
            overall[0] += 1 if hits > 0 else 0
    return areas, overall, excluded


def percent(row):
    return 100.0 if row[1] == 0 else 100.0 * row[0] / row[1]


def rounded_down(value):
    # The epsilon keeps float noise on an exact whole percent (89.99999999) from losing a point.
    return int(math.floor(value + 1e-9))


def load_floors(path):
    data = {"overall": 0, "tolerance": 0.0, "areas": {}, "excludes": []}
    if os.path.exists(path):
        with open(path, encoding="utf-8") as handle:
            data.update(json.load(handle))
    return data


def save_floors(path, data):
    ordered = {
        "comment": data.get("comment", ""),
        "overall": data["overall"],
        "tolerance": data["tolerance"],
        "areas": dict(sorted(data["areas"].items(), key=lambda item: item[0].lower())),
        "excludes": data["excludes"],
    }
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(ordered, handle, indent=2)
        handle.write("\n")


def evaluate(areas, overall, floors):
    """Returns (rows, failures, raises). rows: (name, covered, valid, percent, floor or None, status)."""
    tolerance = float(floors.get("tolerance", 0.0))
    rows, failures, raises = [], [], []

    def judge(name, row, floor):
        value = percent(row)
        if floor is None:
            status = "no floor"
        elif value + tolerance < floor:
            status = "BELOW"
            failures.append(f"{name}: {value:.2f}% is below its floor of {floor}%")
        elif rounded_down(value) > floor:
            status = "can raise"
            raises.append(name)
        else:
            status = "ok"
        rows.append((name, row[0], row[1], value, floor, status))

    judge("SWLOR.Game.Server (overall)", overall, floors.get("overall"))
    for name in sorted(areas, key=str.lower):
        judge(name, areas[name], floors["areas"].get(name))
    for name in floors["areas"]:
        if name not in areas:
            # A missing coverage file or a moved or deleted folder must not pass silently.
            failures.append(f"{name}: no lines were measured, but it has a floor of {floors['areas'][name]}% "
                            "(a coverage file is missing, or the area was emptied: remove its floor from the JSON by hand)")
            rows.append((name, 0, 0, 0.0, floors["areas"][name], "NOT MEASURED"))
    return rows, failures, raises


def format_table(rows):
    out = ["| Area | Covered | Lines | Line % | Floor | Status |", "|---|---:|---:|---:|---:|---|"]
    for name, covered, valid, value, floor, status in rows:
        floor_text = "-" if floor is None else f"{floor}%"
        out.append(f"| {name} | {covered} | {valid} | {value:.1f}% | {floor_text} | {status} |")
    return "\n".join(out)


def top_files(lines, excludes, count):
    patterns = [glob_to_regex(item["pattern"]) for item in excludes]
    totals = {}
    for (path, _number), hits in lines.items():
        if any(pattern.match(path) for pattern in patterns):
            continue
        row = totals.setdefault(path, [0, 0])
        row[1] += 1
        row[0] += 1 if hits > 0 else 0
    ranked = sorted(totals.items(), key=lambda item: item[1][0] - item[1][1])[:count]
    out = ["", "Largest gaps (uncovered lines):", "| File | Uncovered | Lines | Line % |", "|---|---:|---:|---:|"]
    for path, row in ranked:
        out.append(f"| {path} | {row[1] - row[0]} | {row[1]} | {percent(row):.1f}% |")
    return "\n".join(out)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("cobertura", nargs="+", help="Cobertura files, globs, or directories holding *.cobertura.xml")
    parser.add_argument("--floors", default=DEFAULT_FLOORS, help="floors file (default: CoverageFloors.json beside this script)")
    parser.add_argument("--update", action="store_true", help="raise floors to the measurement (never lowers one)")
    parser.add_argument("--top-files", type=int, default=0, metavar="N", help="also list the N files with the most uncovered lines")
    args = parser.parse_args(argv)

    files = expand_inputs(args.cobertura)
    missing = [path for path in files if not os.path.isfile(path)]
    if missing or not files:
        print("No coverage files found: " + ", ".join(missing or args.cobertura), file=sys.stderr)
        return 2

    floors = load_floors(args.floors)
    lines = read_lines(files)
    areas, overall, excluded = measure(lines, floors["excludes"])
    if overall[1] == 0:
        print(f"No {GAME_SERVER} lines in the coverage files (excluded lines: {excluded}).", file=sys.stderr)
        return 2

    if args.update:
        raised = []
        value = rounded_down(percent(overall))
        if value > floors["overall"]:
            raised.append(f"overall {floors['overall']}% -> {value}%")
            floors["overall"] = value
        for name, row in areas.items():
            value = rounded_down(percent(row))
            old = floors["areas"].get(name)
            if old is None or value > old:
                raised.append(f"{name} {'new' if old is None else str(old) + '%'} -> {value}%")
                floors["areas"][name] = value
        save_floors(args.floors, floors)
        print("Floors raised:" if raised else "No floor to raise.")
        for item in raised:
            print("  " + item)

    rows, failures, raises = evaluate(areas, overall, floors)
    print(f"Line coverage of non-generated code ({len(files)} file(s), {excluded} excluded line(s)):")
    print(format_table(rows))
    if args.top_files:
        print(top_files(lines, floors["excludes"], args.top_files))
    if failures:
        print("\nCoverage dropped below its floor:", file=sys.stderr)
        for failure in failures:
            print("  " + failure, file=sys.stderr)
        print("Add tests for the code that lost coverage. Lower a floor only when the covered code moved to another "
              "area or was deleted: edit tools/Refactor/CoverageFloors.json by hand and say why in the pull request.",
              file=sys.stderr)
        return 1
    if raises and not args.update:
        print("\nCoverage rose above the floor of: " + ", ".join(raises) +
              ".\nRaise the floors with: python tools/Refactor/check_coverage.py <the same coverage files> --update "
              "and commit tools/Refactor/CoverageFloors.json.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
