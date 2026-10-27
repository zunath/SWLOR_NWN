#!/usr/bin/env python3
"""Summarise a Cobertura coverage file as Markdown tables: per assembly, and per top-level folder of one assembly.

The maintainability CI (.github/workflows/maintainability-ci.yml) feeds it the merged Cobertura file that
ReportGenerator writes, and appends the output to the job summary. Run it locally the same way:

    python tools/CoverageSummary.py <Cobertura.xml> [--assembly SWLOR.Game.Server]

A line counts as covered when any test hit it; a branch (one condition of a decision) counts as covered when it
was taken. Lines that two classes both report (partial classes, nested types) are counted once.
"""

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from collections import OrderedDict

ROOT_FILES = "(files in the project root)"
CONDITION = re.compile(r"\((\d+)/(\d+)\)")


def read_lines(path):
    """Returns {(assembly, source file, line number): (hits, branches covered, branches total)}."""
    merged = {}
    tree = ET.parse(path)
    for package in tree.getroot().iter("package"):
        assembly = package.get("name", "")
        for cls in package.iter("class"):
            filename = (cls.get("filename") or "").replace("\\", "/")
            for line in cls.findall("lines/line"):
                key = (assembly, filename, int(line.get("number")))
                hits = int(line.get("hits", "0"))
                covered = total = 0
                match = CONDITION.search(line.get("condition-coverage", ""))
                if line.get("branch", "").lower() == "true" and match:
                    covered, total = int(match.group(1)), int(match.group(2))
                previous = merged.get(key)
                if previous is None:
                    merged[key] = (hits, covered, total)
                else:
                    merged[key] = (max(previous[0], hits), max(previous[1], covered), max(previous[2], total))
    return merged


def top_level_folder(assembly, filename):
    parts = [part for part in filename.split("/") if part]
    if assembly in parts:
        rest = parts[parts.index(assembly) + 1:]
    else:
        rest = parts
    return rest[0] if len(rest) > 1 else ROOT_FILES


def tally(rows):
    lines_valid = lines_covered = branches_valid = branches_covered = 0
    for hits, covered, total in rows:
        lines_valid += 1
        lines_covered += 1 if hits > 0 else 0
        branches_valid += total
        branches_covered += covered
    return lines_covered, lines_valid, branches_covered, branches_valid


def percent(covered, valid):
    return "n/a" if valid == 0 else f"{100.0 * covered / valid:.1f}%"


def table(title, first_column, groups):
    out = [f"### {title}", "", f"| {first_column} | Lines covered | Lines | Line % | Branches covered | Branches | Branch % |",
           "|---|---:|---:|---:|---:|---:|---:|"]
    for name, rows in groups.items():
        lc, lv, bc, bv = tally(rows)
        out.append(f"| {name} | {lc} | {lv} | {percent(lc, lv)} | {bc} | {bv} | {percent(bc, bv)} |")
    out.append("")
    return "\n".join(out)


def summarise(merged, assembly):
    by_assembly = OrderedDict()
    by_folder = {}
    for (name, filename, _number), row in sorted(merged.items()):
        by_assembly.setdefault(name, []).append(row)
        if name == assembly:
            by_folder.setdefault(top_level_folder(name, filename), []).append(row)
    sections = [table("Coverage by assembly", "Assembly", by_assembly)]
    if by_folder:
        groups = OrderedDict(sorted(by_folder.items(), key=lambda item: (item[0] == ROOT_FILES, item[0].lower())))
        groups[f"**{assembly} total**"] = by_assembly[assembly]
        sections.append(table(f"{assembly} by top-level folder", "Folder", groups))
    else:
        sections.append(f"No lines were reported for assembly `{assembly}`.\n")
    return "\n".join(sections)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("cobertura", help="merged Cobertura XML file")
    parser.add_argument("--assembly", default="SWLOR.Game.Server", help="assembly to break down by folder")
    args = parser.parse_args(argv)
    sys.stdout.write(summarise(read_lines(args.cobertura), args.assembly))
    return 0


if __name__ == "__main__":
    sys.exit(main())
