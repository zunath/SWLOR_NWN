"""Incremental private SWLOR chat extraction; Python standard library only."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import shutil

STAMP = re.compile(r"^(\d{4}-\d\d-\d\d \d\d:\d\d:\d\d)(?:\.\d+)? ([+-]\d\d:\d\d) \[\w+\] (.*)$")
CHAT = re.compile(r"^(.*?) - (.*?) - ([A-Za-z0-9]{8}|) - ([\da-fA-F.:]*) - ((?:Player|DM)\w+)(?: \(SENT TO .*? - [\da-fA-F.:]*\))?: (.*)$")
TERMS = re.compile(r"bug|crash|lag|stuck|broken|stamina|\bstm\b|\bfp\b|regen|recover|enmity|threat|taunt|balance|nerf|buff|capstone|loot|drop|disc|player list|grouped|party|invite|accept|teleport|spawn|cooldown|recast|missing|disappear|deleted|accuracy|damage|cost|slow|stance|mimicry|armor|armour|helmet|visor|craft|quest|dungeon|permission|disguise|introduc|update|patch|restart", re.I)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def read_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def utc(value):
    parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    # Legacy checkpoints explicitly stored UTC without an offset.
    return parsed.replace(tzinfo=timezone.utc).isoformat(timespec="seconds") if parsed.tzinfo is None else parsed.astimezone(timezone.utc).isoformat(timespec="seconds")


def default_state():
    base = Path(os.environ["LOCALAPPDATA"]) / "SWLOR" if os.environ.get("LOCALAPPDATA") else Path.home() / ".local/state/swlor"
    return base / "feedback-review/state.json"


def fingerprint(path):
    return digest(path.read_bytes()) if path.exists() else None


def write_json(path, data):
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def scan(logs, state_path, out, since=None, all_history=False):
    logs, state_path, out = Path(logs).resolve(), Path(state_path).resolve(), Path(out).resolve()
    if out.exists():
        raise ValueError("Choose a new run directory; evidence is never overwritten.")
    if since and datetime.fromisoformat(since.replace("Z", "+00:00")).tzinfo is None:
        raise ValueError("--since requires an explicit timezone.")
    chat = next((p for p in logs.iterdir() if p.is_dir() and p.name.casefold() == "chat"), None)
    if chat is None:
        raise ValueError("No Chat directory in the supplied export.")
    legacy = Path(os.environ.get("TEMP", "/tmp")) / "cu-feedback-baseline.json"
    source = state_path if state_path.exists() else legacy if legacy.exists() and state_path == default_state().resolve() else None
    source_bytes = source.read_bytes() if source else None
    old = json.loads(source_bytes.decode("utf-8-sig")) if source_bytes is not None else {}
    expected_state = digest(source_bytes) if source == state_path else None
    import_hash = digest(source_bytes) if source and source != state_path else None
    if old and (since or all_history):
        raise ValueError("Existing checkpoint: use a separate state path for historical reviews.")
    if not old and not since and not all_history:
        raise ValueError("No checkpoint: supply --since or explicitly request --all.")
    cutoff = utc(old.get("reviewed_through_utc", since or "0001-01-01T00:00:00+00:00"))
    prior = {Path(f["path"].replace("\\", "/")).name.casefold(): f for f in old.get("files", [])}
    messages, unknown, files, secrets, aliases = [], [], [], set(), {}
    for path in sorted(chat.iterdir(), key=lambda p: p.name.casefold()):
        if not path.is_file() or not re.fullmatch(r"Chat_\d{8}\.log", path.name, re.I):
            continue
        raw = path.read_bytes()
        data = raw[:raw.rfind(b"\n") + 1]
        before = prior.get(path.name.casefold())
        offset = before["bytes"] if before else 0
        if before and (offset > len(data) or digest(data[:offset]) != before["sha256"]):
            raise ValueError("Reviewed prefix changed: " + str(path))
        files.append({"path": str(path), "bytes": len(data), "sha256": digest(data)})
        previous, active = None, bool(before or all_history)
        line = data[:offset].count(b"\n")
        for text in data[offset:].decode("utf-8-sig").splitlines():
            line += 1
            stamp = STAMP.match(text)
            if not stamp:
                if not active:
                    continue
                if previous is not None and not re.match(r"^\d{4}-\d\d-\d\d ", text):
                    previous["message"] += "\n" + text
                elif text.strip():
                    unknown.append({"file": path.name, "line": line, "reason": "Unrecognized line"})
                continue
            clock, zone, body = stamp.groups()
            time = utc(clock + zone)
            previous, active = None, bool(before or time > cutoff)
            if not active:
                continue
            match = CHAT.match(body)
            if not match:
                unknown.append({"file": path.name, "line": line, "time": time, "reason": "Unrecognized header"})
                continue
            character, account, key, address, channel, message = match.groups()
            secrets.update(v for v in (account, key, address) if v)
            identity = key or account or character
            aliases.setdefault(identity, "P" + str(len(aliases) + 1))
            previous = {"file": path.name, "line": line, "time": time, "speaker": aliases[identity], "channel": channel, "message": message}
            messages.append(previous)
    candidate_sources = {(m["file"], m["line"]) for m in messages
                         if TERMS.search(m["message"])
                         or any(marker in m["message"] for marker in ("//", "((", "\\\\"))
                         or m["channel"] in {"PlayerTell", "PlayerDM"}
                         or m["channel"].startswith("DM")}
    patterns = [re.compile(r"(?<!\w)" + re.escape(value) + r"(?!\w)", re.I) for value in sorted(secrets, key=len, reverse=True)]
    for message in messages:
        for pattern in patterns:
            message["message"] = pattern.sub("[identifier]", message["message"])
    messages.sort(key=lambda m: (m["time"], m["file"], m["line"]))
    candidates = [m for m in messages if (m["file"], m["line"]) in candidate_sources]
    times = [m["time"] for m in messages + unknown if "time" in m]
    consumed_files = list(files)
    merged_files = {Path(f["path"].replace("\\", "/")).name.casefold(): f for f in old.get("files", [])}
    merged_files.update({Path(f["path"]).name.casefold(): f for f in files})
    files = list(merged_files.values())
    proposed = dict(old, reviewed_through_utc=max([cutoff] + times), files=files, summary_scope_start_utc=cutoff, sanitized_snapshot=str(out / "messages.jsonl"), snapshot_format="jsonl")
    out.mkdir(parents=True)
    for name, rows in (("messages", messages), ("candidates", candidates), ("unparsed", unknown)):
        with (out / (name + ".jsonl")).open("w", encoding="utf-8") as stream:
            for row in rows:
                stream.write(json.dumps(row, ensure_ascii=False) + "\n")
    manifest = {"consumed_files": consumed_files, "state_path": str(state_path), "expected_state": expected_state, "import_source": str(source) if source and source != state_path else None, "import_hash": import_hash, "proposed_state": proposed, "messages": len(messages), "candidates": len(candidates), "unparsed": len(unknown), "first": messages[0]["time"] if messages else None, "last": messages[-1]["time"] if messages else None}
    write_json(out / "run.json", manifest)
    return {key: manifest[key] for key in ("messages", "candidates", "unparsed", "first", "last")}


def commit(run, master, deployment=None, notes=None, allow_unparsed=False):
    manifest = read_json(Path(run) / "run.json")
    if not re.fullmatch(r"[0-9a-f]{40}", master):
        raise ValueError("Record a verified full master SHA.")
    if manifest["unparsed"] and not allow_unparsed:
        raise ValueError("Review unparsed records, then acknowledge with --allow-unparsed.")
    state = Path(manifest["state_path"])
    state.parent.mkdir(parents=True, exist_ok=True)
    lock = state.with_suffix(".lock")
    handle = lock.open("x", encoding="utf-8")
    try:
        if fingerprint(state) != manifest["expected_state"]:
            raise ValueError("Another review advanced the checkpoint; reconcile first.")
        if manifest["import_source"] and fingerprint(Path(manifest["import_source"])) != manifest["import_hash"]:
            raise ValueError("Imported checkpoint changed during review.")
        proposed = manifest["proposed_state"]
        for entry in manifest["consumed_files"]:
            with Path(entry["path"]).open("rb") as stream:
                if digest(stream.read(entry["bytes"])) != entry["sha256"]:
                    raise ValueError("Consumed log prefix changed: " + entry["path"])
        proposed["last_master_comparison"] = master
        if deployment:
            proposed["deployment"] = read_json(deployment)
        if notes:
            proposed["review_notes_file"] = str(Path(notes).resolve())
            proposed["review_notes"] = Path(notes).read_text(encoding="utf-8-sig")
        temp = state.with_suffix(".new")
        write_json(temp, proposed)
        if state.exists():
            shutil.copy2(state, state.with_suffix(".previous.json"))
        os.replace(temp, state)
    finally:
        handle.close()
        lock.unlink()
    return {"reviewed_through_utc": proposed["reviewed_through_utc"], "state": str(state)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    extract = commands.add_parser("scan")
    extract.add_argument("--logs", required=True)
    extract.add_argument("--state", dest="state_path", type=Path, default=default_state())
    extract.add_argument("--out", required=True)
    boundary = extract.add_mutually_exclusive_group()
    boundary.add_argument("--since")
    boundary.add_argument("--all", action="store_true", dest="all_history")
    save = commands.add_parser("commit")
    save.add_argument("--run", required=True)
    save.add_argument("--master", required=True)
    save.add_argument("--deployment")
    save.add_argument("--notes")
    save.add_argument("--allow-unparsed", action="store_true")
    args = vars(parser.parse_args())
    command = args.pop("command")
    try:
        print(json.dumps(scan(**args) if command == "scan" else commit(**args)))
    except (ValueError, OSError) as error:
        parser.exit(1, str(error) + "\n")


if __name__ == "__main__":
    main()
