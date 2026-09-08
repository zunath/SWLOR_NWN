"""Synchronize the Animations sheet without rewriting unrelated workbook entries.

The animation manifest is the authoring contract; the installed registry supplies
actual resource names. Existing reference rows keep their positions and image links.
"""
from __future__ import annotations

import argparse
import copy
import csv
import io
import json
import os
import re
import tempfile
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
NS = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
REL = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"


def normalize(value):
    return re.sub(r"[^a-z0-9]", "", value.lower())


def cell(column, row, value, style="4"):
    return f'<c r="{column}{row}" s="{style}" t="inlineStr"><is><t>{escape(str(value))}</t></is></c>'


def render_workbook(workbook: Path, entries: list[dict], registry: list[dict]):
    installed = {e["Name"]: e for e in registry}
    if len(installed) != len(registry):
        raise ValueError("Duplicate registry animation identity")
    ids = [e["Id"] for e in entries]
    names = [e["InternalName"] for e in entries]
    if len(set(ids)) != len(ids) or len(set(names)) != len(names):
        raise ValueError("Animation identifiers and internal names must be unique")
    for entry in entries:
        if not re.fullmatch(r"[a-z][a-z0-9_]{0,11}", entry["InternalName"]):
            raise ValueError(f"Invalid internal name: {entry['InternalName']}")
        record = installed.get(entry["Id"])
        if record is None or record["AnimationName"] != entry["InternalName"]:
            raise ValueError(f"Animation is not installed with its assigned name: {entry['Id']}")
    with zipfile.ZipFile(workbook) as source:
        infos = source.infolist()
        original = {i.filename: source.read(i.filename) for i in infos}
    sheets = ET.fromstring(original["xl/workbook.xml"])
    sheet = next(s for s in sheets.findall("s:sheets/s:sheet", NS) if s.get("name") == "Animations")
    links = ET.fromstring(original["xl/_rels/workbook.xml.rels"])
    target = next(r.get("Target") for r in links if r.get("Id") == sheet.get(f"{{{REL}}}id"))
    path = target.lstrip("/") if target.startswith("/") else "xl/" + target
    xml = original[path].decode("utf-8-sig")
    root = ET.fromstring(xml)
    ET.register_namespace("", NS["s"])
    if root.findall(".//s:f", NS):
        raise ValueError("Animation formulas require explicit migration before synchronization")
    hyperlink_nodes = root.findall("s:hyperlinks/s:hyperlink", NS)
    hyperlink_targets = {}
    if hyperlink_nodes:
        relpath = str(Path(path).parent / "_rels" / (Path(path).name + ".rels")).replace("\\", "/")
        relationships = {r.get("Id"): r.get("Target") for r in ET.fromstring(original[relpath])}
        hyperlink_targets = {h.get("ref"): relationships[h.get(f"{{{REL}}}id")] for h in hyperlink_nodes}
    strings = []
    if "xl/sharedStrings.xml" in original:
        strings = ["".join(e.itertext()) for e in ET.fromstring(original["xl/sharedStrings.xml"])]

    def value(c):
        if c.get("t") == "s":
            return strings[int(c.find("s:v", NS).text)]
        return "".join(c.itertext())

    lookup = {(normalize(e["Category"]), normalize(e.get("Name", e.get("DisplayName", e["Id"])))): e for e in entries}
    if len(lookup) != len(entries):
        raise ValueError("Duplicate display names within an animation category")
    seen = set()
    last = 1
    rows = []
    data_styles = {}
    header_styles = {}
    for row in root.findall("s:sheetData/s:row", NS):
        styles = header_styles if row.get("r") == "1" else data_styles
        for existing in row.findall("s:c", NS):
            column = re.sub(r"\d", "", existing.get("r"))
            if existing.get("s") is not None:
                styles.setdefault(column, existing.get("s"))

    def style(column, header=False):
        styles = header_styles if header else data_styles
        return styles.get(column, styles.get("D", "17" if header else "4"))

    def serialize_row(node):
        # Parse complete XML elements: a self-closing blank cell must never consume the
        # following populated cell, and all cells must remain in spreadsheet column order.
        cells = list(node.findall("s:c", NS))
        for existing in cells:
            node.remove(existing)
        def order(existing):
            letters = re.sub(r"\d", "", existing.get("r"))
            return len(letters), letters
        for existing in sorted(cells, key=order):
            node.append(existing)
        return ET.tostring(node, encoding="unicode")

    for node in root.findall("s:sheetData/s:row", NS):
        number = int(node.get("r"))
        cells = {re.sub(r"\d", "", c.get("r")): value(c) for c in node}
        if number == 1:
            extra = {"F": "Internal Name", "G": "Base Motion", "H": "Status", "I": "Source Project"}
        elif cells.get("C"):
            key = normalize(cells.get("B", "")), normalize(cells["C"])
            if key not in lookup:
                raise ValueError(f"Existing Bible row has no active animation: {cells['C']}")
            entry = lookup[key]
            if cells.get("E", "") != entry.get("Reference", ""):
                raise ValueError(f"Reference mismatch for {entry['Id']}; update image and hyperlink together")
            if entry.get("Reference") and hyperlink_targets.get(f"E{number}") != entry["Reference"]:
                raise ValueError(f"Hyperlink mismatch for {entry['Id']}")
            if entry["Id"] in seen:
                raise ValueError(f"Duplicate Bible animation: {entry['Id']}")
            seen.add(entry["Id"])
            entry["BibleAnimationRow"] = number
            extra = details(entry, installed[entry["Id"]])
        else:
            continue
        last = max(last, number)
        for existing in list(node.findall("s:c", NS)):
            column = re.sub(r"\d", "", existing.get("r"))
            if column in extra:
                node.remove(existing)
            elif column in "ABCDEFGHI":
                existing.set("s", style(column, number == 1))
        for column, content in extra.items():
            node.append(ET.fromstring('<root xmlns="' + NS["s"] + '">' +
                cell(column, number, content, style(column, number == 1)) + '</root>')[0])
        rows.append(serialize_row(node))
    for entry in entries:
        if entry["Id"] in seen:
            continue
        last += 1
        entry["BibleAnimationRow"] = last
        values = {"A": "Ability", "B": entry["Category"], "C": entry.get("Name", entry.get("DisplayName", entry["Id"])),
                  "D": entry.get("Description", ""), "E": entry.get("Reference", ""), **details(entry, installed[entry["Id"]])}
        if values["E"]:
            raise ValueError("New image references must be added with a hyperlink relationship")
        added = ET.fromstring(f'<row xmlns="{NS["s"]}" r="{last}">' +
            ''.join(cell(c, last, v, style(c)) for c, v in values.items()) + '</row>')
        rows.append(serialize_row(added))
    # ElementTree reads both default and explicit spreadsheet prefixes. Replace exactly the
    # corresponding serialized sheetData element; never report success on a zero-match edit.
    prefixes = {prefix + ":" if prefix else "" for _, (prefix, uri) in
                ET.iterparse(io.StringIO(xml), events=("start-ns",)) if uri == NS["s"]}
    tag_prefix = "(?:" + "|".join(re.escape(prefix) for prefix in sorted(prefixes)) + ")"
    pattern = rf'<(?P<data_tag>{tag_prefix}sheetData)\b[^>]*(?:/>|>.*?</(?P=data_tag)\s*>)'
    def replace_sheet_data(match):
        opening = match[0].split(">", 1)[0]
        if opening.endswith("/"):
            opening = opening[:-1]
        return opening + ">" + ''.join(rows) + '</' + match["data_tag"] + '>'

    xml, replacements = re.subn(pattern, replace_sheet_data, xml, flags=re.S)
    if replacements != 1:
        raise ValueError("Expected exactly one spreadsheet sheetData element")

    # Retained cells outside the managed A:I columns still belong to the worksheet's
    # used range. Preserve an existing larger range as well as newly added rows.
    def column_number(letters):
        result = 0
        for letter in letters:
            result = result * 26 + ord(letter) - ord("A") + 1
        return result

    used_column, used_row = 9, last
    references = [c.get("r", "") for row in rows for c in ET.fromstring(row).findall("s:c", NS)]
    dimension = root.find("s:dimension", NS)
    if dimension is not None:
        references.extend(dimension.get("ref", "").split(":"))
    for reference in references:
        match = re.fullmatch(r"([A-Z]+)([1-9][0-9]*)", reference)
        if match:
            used_column = max(used_column, column_number(match[1]))
            used_row = max(used_row, int(match[2]))
    letters = ""
    while used_column:
        used_column, remainder = divmod(used_column - 1, 26)
        letters = chr(ord("A") + remainder) + letters
    dimension_ref = f"A1:{letters}{used_row}"
    xml = re.sub(rf'(<{tag_prefix}dimension\b[^>]*\bref=")[^"]+',
                 lambda m: m[1] + dimension_ref, xml)
    xml = re.sub(rf'(<{tag_prefix}autoFilter\b[^>]*\bref=")[^"]+',
                 lambda m: m[1] + f'A1:I{last}', xml)
    output = io.BytesIO()
    with zipfile.ZipFile(output, "w") as dest:
        for info in infos:
            dest.writestr(copy.copy(info), xml.encode("utf-8") if info.filename == path else original[info.filename])
    with zipfile.ZipFile(io.BytesIO(output.getvalue())) as check:
        for name, payload in original.items():
            if name != path and check.read(name) != payload:
                raise AssertionError(f"Unrelated workbook entry changed: {name}")
    return output.getvalue()


def replace_outputs(outputs: dict[Path, bytes]):
    """Stage every replacement and rollback copy before replacing any existing file."""
    paths = [path.resolve() for path in outputs]
    if len(set(paths)) != len(paths):
        raise ValueError("Output paths must be distinct")
    staged = {}
    backups = {}
    committed = []
    temporary = []

    def stage(path, payload):
        with tempfile.NamedTemporaryFile(dir=path.parent, prefix=".animation-sync-", delete=False) as stream:
            temporary.append(Path(stream.name))
            stream.write(payload)
            stream.flush()
            os.fsync(stream.fileno())
        return temporary[-1]

    try:
        for path, payload in outputs.items():
            backups[path] = stage(path, path.read_bytes())
            staged[path] = stage(path, payload)
        try:
            for path in outputs:
                os.replace(staged[path], path)
                committed.append(path)
        except Exception as failure:
            rollback_failures = []
            for path in reversed(committed):
                try:
                    os.replace(backups[path], path)
                except Exception as rollback_failure:
                    # Retain this recovery copy if the filesystem also rejects rollback.
                    temporary.remove(backups[path])
                    rollback_failures.append(OSError(f"Could not restore {path}; original saved at {backups[path]}: {rollback_failure}"))
            if rollback_failures:
                raise ExceptionGroup("Animation synchronization and rollback failed", [failure, *rollback_failures])
            raise
    finally:
        for path in temporary:
            path.unlink(missing_ok=True)


def synchronize(workbook: Path, entries: list[dict], registry: list[dict]):
    replace_outputs({workbook: render_workbook(workbook, entries, registry)})
    return entries


def details(entry, installed):
    base = entry.get("Profile") or "Authored poses"
    source = entry.get("SourceModel", "")
    animation = entry.get("SourceAnimation")
    if source and animation:
        base += f" ({source}/{animation})"
    return {"F": entry["InternalName"], "G": base,
            "H": "Installed; in-game visual review required", "I": installed["ProjectPath"]}


def synchronize_files(manifest_path, workbook_path, registry_path, provenance_path, plan_path):
    data = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    source_entries = data if isinstance(data, list) else data["Animations"]
    entries = copy.deepcopy(source_entries)
    provenance = {e["Id"]: e for e in json.loads(provenance_path.read_text(encoding="utf-8-sig"))["Animations"]}
    for entry in entries:
        if entry["Id"] not in provenance:
            raise ValueError(f"Missing generation provenance: {entry['Id']}")
        for key in ("SourceModel", "SourceAnimation", "Profile"):
            entry[key] = provenance[entry["Id"]].get(key)
    workbook_bytes = render_workbook(workbook_path, entries, json.loads(registry_path.read_text(encoding="utf-8-sig")))
    rows = {e["Id"]: e["BibleAnimationRow"] for e in entries}
    for entry in source_entries:
        entry["BibleAnimationRow"] = rows[entry["Id"]]
    with plan_path.open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.DictReader(stream)
        fields = list(reader.fieldnames)
        plan = list(reader)
    if "InternalName" not in fields:
        fields.append("InternalName")
    by_id = {e["Id"]: e for e in entries}
    for row in plan:
        entry = by_id[row["PerkId"]]
        row.update(Status="Installed", InternalName=entry["InternalName"], BibleAnimationRow=entry["BibleAnimationRow"])
    stream = io.StringIO(newline="")
    writer = csv.DictWriter(stream, fieldnames=fields)
    writer.writeheader()
    writer.writerows(plan)
    replace_outputs({workbook_path: workbook_bytes,
                     manifest_path: (json.dumps(data, indent=2, ensure_ascii=False) + "\n").encode("utf-8"),
                     plan_path: stream.getvalue().encode("utf-8")})
    return len(entries)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("--workbook", type=Path, default=ROOT / "design/bible/SWLOR Design Bible - Combat Upgrade.xlsx")
    parser.add_argument("--registry", type=Path, default=ROOT / "design/animations/registry.json")
    parser.add_argument("--provenance", type=Path, default=ROOT / "design/animations/active-manifest.json")
    args = parser.parse_args()
    count = synchronize_files(args.manifest, args.workbook, args.registry, args.provenance,
                              ROOT / "design/animations/animation-plan.csv")
    print(f"Documented {count} installed animations; unrelated workbook entries preserved.")


if __name__ == "__main__":
    main()
