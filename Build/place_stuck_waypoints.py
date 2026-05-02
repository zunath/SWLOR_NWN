"""
Add one stuck-recovery waypoint per area GIT (Module/git/*.git.json).

Tag on every instance: STUCK_WAYPOINT (see SystemChatCommand /stuck).

Placement:
  1) **Always copy an existing waypoint in that area** when there is at least
     one non-STUCK waypoint with coordinates. Same X/Y/Z — no invented points.
     Preference order (tier, then tag name): trigger LinkedTo targets, property/
     entrance/landing-style tags, WP_*_to_*, exit-ish tags, any other waypoint,
     spawn/farm waypoints last.
  2) **Only if the area has zero other waypoints**: door-offset / ARE heuristics
     (rare — most areas have at least one waypoint).

Run from repo root: python Build/place_stuck_waypoints.py
"""
from __future__ import annotations

import json
import math
import re
import sys
from pathlib import Path

STUCK_WAYPOINT_TAG = "STUCK_WAYPOINT"

WP_TO_WP = re.compile(r"^WP_.+_to_.+", re.I)
EXITISH = re.compile(r"(^EXIT_|_EXIT$|_exit$|^exit_)", re.I)
SPAWNISH = re.compile(r"spwn|spawn|_spn$", re.I)

PREFERRED_WP_TAG = re.compile(
    r"ENTRANCE|LANDING|SPAWN|HUB|STARPORT|DOCK|_TO_|_FROM_|"
    r"TRANSITION|ARRIV|DEPART|EXIT|ENTRY|SAFE",
    re.I,
)


def repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def read_json(path: Path) -> dict:
    raw = path.read_bytes()
    try:
        text = raw.decode("utf-8")
    except UnicodeDecodeError:
        text = raw.decode("latin-1")
    return json.loads(text)


def write_json(path: Path, data: dict) -> None:
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def xyz_from_obj(o: dict) -> tuple[float, float, float] | None:
    if "XPosition" in o:
        return (
            float(o["XPosition"]["value"]),
            float(o["YPosition"]["value"]),
            float(o["ZPosition"]["value"]),
        )
    if "X" in o and "Y" in o and "Z" in o:
        return float(o["X"]["value"]), float(o["Y"]["value"]), float(o["Z"]["value"])
    return None


def collect_trigger_link_targets(git_dir: Path) -> set[str]:
    """Every non-empty trigger LinkedTo in the module (destination waypoint tags)."""
    targets: set[str] = set()
    for git_path in sorted(git_dir.glob("*.git.json")):
        try:
            git = read_json(git_path)
        except (OSError, json.JSONDecodeError):
            continue
        for tr in git.get("TriggerList", {}).get("value") or []:
            v = str(tr.get("LinkedTo", {}).get("value") or "").strip()
            if v:
                targets.add(v)
    return targets


def waypoint_clone_rank(tag: str, link_targets: set[str]) -> tuple[int, str]:
    """
    Sort key for picking which existing waypoint to copy. Lower = prefer.
    Every non-spawn designer waypoint is included (tier 5+) so PROPERTY_ENTRANCE
    etc. are never skipped in favor of heuristics.
    """
    t = (tag or "").strip()
    low = t.lower()
    if SPAWNISH.search(t):
        return (6, low)
    if t in link_targets:
        return (0, low)
    if low == "property_entrance" or ("property" in low and "entrance" in low):
        return (1, low)
    if "entrance" in low or "landing" in low or low.endswith("_entry"):
        return (2, low)
    if WP_TO_WP.match(t):
        return (3, low)
    if EXITISH.search(t):
        return (4, low)
    return (5, low)


def pick_position_from_existing_waypoint(
    git: dict,
    link_targets: set[str],
) -> tuple[float, float, float] | None:
    """Copy coords from the best-ranked non-STUCK waypoint, if any."""
    best_key: tuple[int, str] | None = None
    best_pos: tuple[float, float, float] | None = None

    for w in git.get("WaypointList", {}).get("value") or []:
        tag = str(w.get("Tag", {}).get("value") or "")
        if not tag or tag.upper().startswith("STUCK_"):
            continue
        p = xyz_from_obj(w)
        if not p:
            continue
        key = waypoint_clone_rank(tag, link_targets)
        if best_key is None or key < best_key:
            best_key = key
            best_pos = p

    return best_pos


def obstacle_xy_list(git: dict) -> list[tuple[float, float]]:
    out: list[tuple[float, float]] = []
    for key in ("Placeable List", "Creature List", "Door List"):
        for o in git.get(key, {}).get("value") or []:
            p = xyz_from_obj(o)
            if p:
                out.append((p[0], p[1]))
    return out


def min_clearance(px: float, py: float, obstacles: list[tuple[float, float]]) -> float:
    if not obstacles:
        return 1e9
    best = 1e9
    for ox, oy in obstacles:
        d = math.hypot(px - ox, py - oy)
        if d < best:
            best = d
    return best


def door_offset_candidates(doors: list) -> list[tuple[float, float, float]]:
    cands: list[tuple[float, float, float]] = []
    for d in doors:
        p = xyz_from_obj(d)
        if not p:
            continue
        b = float(d.get("Bearing", {}).get("value", 0.0))
        ox = math.cos(b + math.pi / 2) * 2.8
        oy = math.sin(b + math.pi / 2) * 2.8
        cands.append((p[0] + ox, p[1] + oy, p[2]))
        cands.append((p[0] - ox, p[1] - oy, p[2]))
        fx = math.sin(b) * 1.5
        fy = math.cos(b) * 1.5
        cands.append((p[0] + fx, p[1] + fy, p[2]))
        cands.append((p[0] - fx, p[1] - fy, p[2]))
    return cands


def waypoint_candidates(
    waypoint_list: list,
) -> list[tuple[tuple[float, float, float], float, str]]:
    out: list[tuple[tuple[float, float, float], float, str]] = []
    for w in waypoint_list:
        tag = str(w.get("Tag", {}).get("value") or "")
        if tag.upper().startswith("STUCK_"):
            continue
        p = xyz_from_obj(w)
        if not p:
            continue
        bonus = 0.15 if PREFERRED_WP_TAG.search(tag) else 0.0
        out.append((p, bonus, tag))
    return out


def best_candidate(
    candidates: list[tuple[float, float, float]],
    obstacles: list[tuple[float, float]],
    wp_meta: list[tuple[tuple[float, float, float], float, str]] | None = None,
) -> tuple[float, float, float] | None:
    best: tuple[float, float, float] | None = None
    best_score = -1.0

    for x, y, z in candidates:
        s = min_clearance(x, y, obstacles)
        if s > best_score:
            best_score = s
            best = (x, y, z)

    if wp_meta:
        for p, bonus, _tag in wp_meta:
            x, y, z = p
            s = min_clearance(x, y, obstacles) + bonus
            if s > best_score:
                best_score = s
                best = (x, y, z)

    return best


def area_center_from_are(are_path: Path) -> tuple[float, float, float] | None:
    if not are_path.is_file():
        return None
    try:
        are = read_json(are_path)
    except (OSError, json.JSONDecodeError):
        return None
    w = int(are.get("Width", {}).get("value", 0))
    h = int(are.get("Height", {}).get("value", 0))
    if w <= 0 or h <= 0:
        return None
    return (w * 5.0, h * 5.0, 0.0)


def pick_position_fallback(
    git: dict, area_resref: str, module: Path
) -> tuple[float, float, float]:
    obstacles = obstacle_xy_list(git)
    doors = git.get("Door List", {}).get("value") or []
    wps = git.get("WaypointList", {}).get("value") or []

    candidates: list[tuple[float, float, float]] = []
    candidates.extend(door_offset_candidates(doors))
    wp_meta = waypoint_candidates(wps)

    chosen = best_candidate(candidates, obstacles, wp_meta)
    if chosen is None and wp_meta:
        chosen = best_candidate([], obstacles, wp_meta)

    if chosen is None:
        c = area_center_from_are(module / "are" / f"{area_resref}.are.json")
        if c:
            chosen = c

    if chosen is None:
        return (10.0, 10.0, 0.0)
    return chosen


def pick_position(
    git: dict,
    area_resref: str,
    module: Path,
    link_targets: set[str],
) -> tuple[float, float, float]:
    cloned = pick_position_from_existing_waypoint(git, link_targets)
    if cloned is not None:
        return cloned
    return pick_position_fallback(git, area_resref, module)


def make_waypoint_entry(tag: str, display: str, x: float, y: float, z: float) -> dict:
    return {
        "__struct_id": 5,
        "Appearance": {"type": "byte", "value": 1},
        "Description": {"type": "cexolocstring", "value": {"0": ""}},
        "HasMapNote": {"type": "byte", "value": 0},
        "LinkedTo": {"type": "cexostring", "value": ""},
        "LocalizedName": {"type": "cexolocstring", "value": {"0": display}},
        "MapNote": {"type": "cexolocstring", "value": {}},
        "MapNoteEnabled": {"type": "byte", "value": 0},
        "Tag": {"type": "cexostring", "value": tag},
        "TemplateResRef": {"type": "resref", "value": "wp_stuck"},
        "XOrientation": {"type": "float", "value": 0.0},
        "XPosition": {"type": "float", "value": x},
        "YOrientation": {"type": "float", "value": 1.0},
        "YPosition": {"type": "float", "value": y},
        "ZPosition": {"type": "float", "value": z},
    }


def main() -> int:
    root = repo_root()
    git_dir = root / "Module" / "git"
    if not git_dir.is_dir():
        print("Module/git not found", file=sys.stderr)
        return 1

    if len(STUCK_WAYPOINT_TAG) > 32:
        print("STUCK_WAYPOINT_TAG exceeds NWN tag length", file=sys.stderr)
        return 1

    print("Collecting trigger LinkedTo targets across module...", file=sys.stderr)
    link_targets = collect_trigger_link_targets(git_dir)

    files = sorted(git_dir.glob("*.git.json"))
    updated = 0
    for git_path in files:
        if not git_path.name.endswith(".git.json"):
            continue
        area_resref = git_path.name[: -len(".git.json")]

        git = read_json(git_path)
        wpl = git.setdefault("WaypointList", {"type": "list", "value": []})
        if wpl.get("type") != "list":
            wpl["type"] = "list"
        lst: list = wpl.setdefault("value", [])

        lst[:] = [
            w
            for w in lst
            if not str(w.get("Tag", {}).get("value") or "").upper().startswith("STUCK_")
        ]

        x, y, z = pick_position(git, area_resref, root / "Module", link_targets)
        display = f"Stuck — {area_resref}"
        lst.append(make_waypoint_entry(STUCK_WAYPOINT_TAG, display, x, y, z))
        write_json(git_path, git)
        updated += 1

    print(f"Updated {updated} GIT files with {updated} STUCK waypoints.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
