"""Stock the astromech form of an item in every placed copy of a store that sells the humanoid item.

A store placed in an area (Module/git/*.git.json) is a copy of its blueprint with its own item list, holding each item
in full. The game opens the placed copy, so an item added only to Module/utm/<store>.utm.json never reaches players.
The astromech gear generators call `stock` so a placed store lists the astromech form next to the item it copies.

Area files are edited as JSON and written back byte for byte except for the added items: numbers keep the text they
had (the files hold floats in a form Python would rewrite), and the line endings of the file are kept.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
GIT = ROOT / "Module" / "git"

_FLOAT = "\u0000F"
_FLOAT_OUT = re.compile(r'"\\u0000F([^"]*)"')
# Fields a placed store item has that its blueprint does not, and the blueprint fields a placed item drops.
INSTANCE_FIELDS = ("Infinite", "XOrientation", "XPosition", "YOrientation", "YPosition", "ZPosition")
BLUEPRINT_ONLY = ("Comment", "PaletteID", "__data_type")


def load(raw: bytes):
    """A GFF JSON file as data, with every float kept as the text it was written in."""
    return json.loads(raw.decode("latin-1"), parse_float=lambda text: _FLOAT + text)


def dump(data, raw: bytes) -> bytes:
    """The data in the layout of the file it came from (two-space indent, the file's line endings and ending)."""
    newline = "\r\n" if b"\r\n" in raw else "\n"
    text = _FLOAT_OUT.sub(lambda m: m.group(1), json.dumps(data, indent=2, ensure_ascii=False))
    text = text.replace("\n", newline)
    if raw.endswith(b"\n"):
        text += newline
    return text.encode("latin-1")


def _placed_item(blueprint: dict, source_item: dict, struct_id: int, x: int, y: int) -> dict:
    item = {key: value for key, value in blueprint.items() if key not in BLUEPRINT_ONLY}
    for field in INSTANCE_FIELDS:
        item[field] = source_item[field]
    item["Repos_PosX"] = {"type": "word", "value": x}
    item["Repos_Posy"] = {"type": "word", "value": y}
    ordered = {"__struct_id": struct_id}
    for key in sorted(item, key=str.lower):
        ordered[key] = item[key]
    return ordered


def _refresh(tabs: list, resref: str, blueprint: dict) -> bool:
    """Bring the placed copies of an item up to its blueprint, in place; true when one changed."""
    changed = False
    for tab in tabs:
        for index, item in enumerate(tab):
            if item["TemplateResRef"]["value"] != resref:
                continue
            current = _placed_item(blueprint, item, item["__struct_id"],
                                   item["Repos_PosX"]["value"], item["Repos_Posy"]["value"])
            if current != item:
                tab[index] = current
                changed = True
    return changed


def stock(additions: dict, blueprint, git: Path | None = None) -> dict:
    """{path: new bytes} for every area file whose placed stores need an item added or brought up to date.

    `additions` maps a store blueprint name to (source resref, new resref) pairs. In each placed copy of that store
    (found by its ResRef, or by its tag when it has none) the new item
    is added to the tab that holds the source, once; a copy already there is rewritten when its blueprint has changed.
    A placed store that does not sell the source is left alone.
    `blueprint` returns the bytes of an item blueprint by resref.
    """
    git = git or GIT
    outputs = {}
    tags = [tag.encode("latin-1") for tag in additions]
    for path in sorted(git.glob("*.git.json")):
        raw = path.read_bytes()
        if not any(b'"' + tag + b'"' in raw for tag in tags):
            continue
        data = load(raw)
        changed = False
        for store in data.get("StoreList", {}).get("value", []):
            # A placed store keeps the name of its blueprint in ResRef; its tag is often another name.
            pairs = additions.get(store.get("ResRef", {}).get("value")) or additions.get(store.get("Tag", {}).get("value"))
            if not pairs:
                continue
            tabs = [tab.get("ItemList", {}).get("value", []) for tab in store["StoreList"]["value"]]
            present = {item["TemplateResRef"]["value"] for tab in tabs for item in tab}
            for source, new in pairs:
                if new in present:
                    # A placed copy holds the item in full, so it follows its blueprint only when rewritten here.
                    changed |= _refresh(tabs, new, load(blueprint(new)))
                    continue
                homes = [tab for tab in tabs if any(item["TemplateResRef"]["value"] == source for item in tab)]
                if not homes:
                    continue
                tab = homes[0]
                source_item = next(item for item in tab if item["TemplateResRef"]["value"] == source)
                y, x = max((item["Repos_Posy"]["value"], item["Repos_PosX"]["value"]) for item in tab)
                x += 2
                if x > 8:
                    x, y = 0, y + 2
                tab.append(_placed_item(load(blueprint(new)), source_item, len(tab), x, y))
                present.add(new)
                changed = True
        if changed:
            outputs[path] = dump(data, raw)
    return outputs
