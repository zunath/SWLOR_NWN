#!/usr/bin/env python3
"""Reconcile the two description fields on every item blueprint.

A uti carries both Description (unidentified) and DescIdentified. Only the second
is live: NWScript's GetDescription defaults to the identified string and every
examine surface on the server takes that default. The corpus had drifted anyway,
so the two rules below put both fields in agreement:

  1. Ignore a field that holds nothing but the item's own name. An item described
     as itself has no description there, and echoing the name under the name is
     not worth keeping.
  2. Whatever real description is left goes in both fields. If rule 1 ate the only
     text there was, both fields end up blank.

No blueprint holds two different real descriptions, so rule 2 never has to choose
between them.

Rule 1 discards a field, not an item. 195 blueprints have the name in one field and
authored prose in the other - "Crafted cuirass salvaged from Banecaller Vex" on the
Bane Cuirass - and that prose is the description; only the name-echo is dropped.

Any StringRef ("id") on a field this script writes is dropped. A CExoLocString
with a StringRef can resolve through the TLK instead of its inline text, so
leaving one behind could quietly resurrect the string we just cleared - the
whole point is that the file says what the editor shows.

Usage:
    python tools/NormalizeItemDescriptions.py           # apply
    python tools/NormalizeItemDescriptions.py --check   # audit only; exit 1 if any drift

Edits are pure text surgery on the language-0 substring, so every other byte -
key order, indentation, line endings, float lexemes - survives untouched.
Requires a module repack on deploy.
"""
import glob
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FIELDS = ("Description", "DescIdentified")

# A JSON string body: any run of non-quote, non-backslash characters and any escape pair.
# A lazy ".*?" cannot be used here - it stops at the first quote, which inside an escaped
# \" is the middle of the string, and the truncated literal it yields ends in a lone
# backslash that escapes the closing quote and swallows the rest of the file.
LANG0 = re.compile(r'("0": ")((?:[^"\\]|\\.)*)(")', re.DOTALL)


def read_text(path):
    # surrogateescape preserves any non-UTF-8 bytes (some blueprints use CP-1252).
    with open(path, "rb") as handle:
        return handle.read().decode("utf-8", "surrogateescape")


def plain(field):
    """The language-0 text of a parsed CExoLocString field, stripped."""
    if not isinstance(field, dict):
        return ""
    value = field.get("value")
    if not isinstance(value, dict) or not value:
        return ""
    return (value.get("0") or "").strip()


def field_span(text, field):
    """The [start, end) span of one top-level field's block, or None."""
    match = re.search(r'\n(\s*)"' + field + r'": \{', text)
    if match is None:
        return None

    indent = match.group(1)
    close = text.index("\n" + indent + "}", match.end())
    return match.start() + 1, close + len("\n" + indent + "}")


def rewrite_field(text, field, escaped):
    """Set a field's language-0 substring to the already-escaped literal, dropping its id."""
    span = field_span(text, field)
    if span is None:
        return text

    start, end = span
    block = text[start:end]

    existing = LANG0.search(block)
    if existing is not None:
        block = block[:existing.start(2)] + escaped + block[existing.end(2):]
    elif escaped:
        # An empty "value": {} has no language slot to overwrite; give it one, matching
        # the surrounding indentation so the file keeps its shape.
        empty = re.search(r'("value": \{)(\})', block)
        if empty is None:
            return text
        eol = "\r\n" if "\r\n" in block else "\n"
        indent = re.search(r'([ \t]*)"value"', block).group(1)
        filled = ('{' + eol + indent + '  "0": "' + escaped + '"' + eol + indent + '}')
        block = block[:empty.start(1)] + '"value": ' + filled + block[empty.end(2):]

    block = re.sub(r'[ \t]*"id": \d+,\r?\n', "", block)
    return text[:start] + block + text[end:]


def escaped_lang0(text, field):
    """The field's language-0 text exactly as escaped on disk, so a copy is byte-faithful."""
    span = field_span(text, field)
    if span is None:
        return ""
    found = LANG0.search(text[span[0]:span[1]])
    return found.group(2) if found else ""


def plan(path):
    """(reason, {field: escaped}) for a blueprint needing changes, or None."""
    text = read_text(path)
    try:
        document = json.loads(text)
    except ValueError:
        return None

    name = plain(document.get("LocalizedName"))
    values = {field: plain(document.get(field)) for field in FIELDS}
    echoes_name = name and name in values.values()

    # Rule 1: a field holding only the item's name is not a description.
    real = [field for field in FIELDS if values[field] and values[field] != name]

    if not real:
        if not echoes_name:
            return None
        return "name-only", {field: "" for field in FIELDS if values[field]}

    # Rule 2: the surviving description goes in both fields.
    source = real[0]
    escaped = escaped_lang0(text, source)
    edits = {field: escaped for field in FIELDS if values[field] != values[source]}
    if not edits:
        return None

    return "name-echo-replaced" if echoes_name else "one-sided", edits


def main():
    check = "--check" in sys.argv
    changed = []

    for path in sorted(glob.glob(os.path.join(ROOT, "Module", "uti", "*.uti.json"))):
        planned = plan(path)
        if planned is None:
            continue

        reason, edits = planned
        changed.append((os.path.basename(path), reason))
        if check:
            continue

        text = read_text(path)
        for field, escaped in edits.items():
            text = rewrite_field(text, field, escaped)
        with open(path, "wb") as handle:
            handle.write(text.encode("utf-8", "surrogateescape"))

    counts = {reason: sum(1 for _, r in changed if r == reason)
              for reason in ("name-only", "name-echo-replaced", "one-sided")}
    verb = "need" if check else "updated:"
    print("%d blueprint(s) %s %d blanked (nothing but the item's name), "
          "%d name-echo replaced with the real description, %d copied across"
          % (len(changed), verb, counts["name-only"], counts["name-echo-replaced"],
             counts["one-sided"]))

    if check and changed:
        for name, reason in changed[:10]:
            print("  %s (%s)" % (name, reason))
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
