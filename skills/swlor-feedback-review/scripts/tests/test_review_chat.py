import importlib.util
import json
from pathlib import Path
import tempfile
import subprocess
import sys
import unittest

spec = importlib.util.spec_from_file_location("review_chat", Path(__file__).parents[1] / "review_chat.py")
review = importlib.util.module_from_spec(spec)
spec.loader.exec_module(review)
SHA = "a" * 40


def line(message, clock="2026-10-19 01:00:00", offset="+00:00", channel="PlayerTalk"):
    return f"{clock}.123 {offset} [INF] Character - TestAccount - AB12CD34 - 192.0.2.1 - {channel}: {message}\n"


class ReviewTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.logs = self.root / "logs"
        (self.logs / "Chat").mkdir(parents=True)
        self.chat = self.logs / "Chat/Chat_20261019.log"
        self.state = self.root / "private/state.json"

    def scan(self, name, **kwargs):
        return review.scan(self.logs, self.state, self.root / name, **kwargs)

    def rows(self, name, category="messages"):
        return [json.loads(s) for s in (self.root / name / (category + ".jsonl")).read_text(encoding="utf-8").splitlines()]

    def test_player_to_dm_messages_are_always_candidates(self):
        self.chat.write_text(line("It flickers", channel="PlayerDM"), encoding="utf-8")
        self.assertEqual(1, self.scan("one", all_history=True)["candidates"])

    def test_identifier_redaction_does_not_remove_candidate(self):
        self.chat.write_text(line("damage feels low").replace("TestAccount", "Damage"), encoding="utf-8")
        self.assertEqual(1, self.scan("one", all_history=True)["candidates"])
        self.assertEqual("[identifier] feels low", self.rows("one", "candidates")[0]["message"])

    def test_tell_preserves_metadata_like_text_in_message(self):
        message = "prefix - dead): actual issue"
        self.chat.write_text(line(message, channel="PlayerTell (SENT TO Recipient - 192.0.2.9)"), encoding="utf-8")
        self.scan("one", all_history=True)
        self.assertEqual(message, self.rows("one")[0]["message"])

    def test_cli_scan_and_commit(self):
        self.chat.write_text(line("CLI"), encoding="utf-8")
        script = str(Path(review.__file__))
        result = subprocess.run([sys.executable, "-B", script, "scan", "--logs", str(self.logs), "--state", str(self.state), "--out", str(self.root / "cli"), "--all"], capture_output=True, text=True)
        self.assertEqual(0, result.returncode, result.stderr)
        result = subprocess.run([sys.executable, "-B", script, "commit", "--run", str(self.root / "cli"), "--master", SHA], capture_output=True, text=True)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertTrue(self.state.exists())

    def test_archived_file_checkpoint_survives_absent_export(self):
        self.chat.write_text(line("Old"), encoding="utf-8")
        self.scan("one", all_history=True)
        review.commit(self.root / "one", SHA)
        saved = self.chat.read_bytes()
        self.chat.unlink()
        self.scan("two")
        review.commit(self.root / "two", SHA)
        self.chat.write_bytes(saved + line("Returned append").encode())
        self.assertEqual(1, self.scan("three")["messages"])

    def test_same_second_append_rollover_and_late_timestamp(self):
        self.chat.write_text(line("First"), encoding="utf-8")
        self.scan("one", all_history=True)
        self.assertFalse(self.state.exists())
        review.commit(self.root / "one", SHA)
        with self.chat.open("a", encoding="utf-8") as f:
            f.write(line("Same second") + line("Late arrival", "2026-10-18 23:00:00"))
        (self.logs / "Chat/Chat_20261020.log").write_text(line("Rollover", "2026-10-20 00:00:00"), encoding="utf-8")
        self.assertEqual(3, self.scan("two")["messages"])
        self.assertEqual({"Same second", "Late arrival", "Rollover"}, {m["message"] for m in self.rows("two")})
        review.commit(self.root / "two", SHA)
        self.assertEqual(0, self.scan("three")["messages"])

    def test_timezone_multiline_recipient_and_identifiers(self):
        self.chat.write_text(line("Quoted AB12CD34 192.0.2.1 TestAccount", offset="-04:00", channel="PlayerTell (SENT TO Recipient - 192.0.2.9)") + "continued\n", encoding="utf-8")
        self.scan("one", all_history=True)
        row = self.rows("one")[0]
        self.assertEqual("2026-10-19T05:00:00+00:00", row["time"])
        self.assertEqual("PlayerTell", row["channel"])
        self.assertTrue(row["message"].endswith("\ncontinued"))
        text = json.dumps(row)
        for secret in ("AB12CD34", "192.0.2.1", "TestAccount", "Recipient"):
            self.assertNotIn(secret, text)

    def test_partial_tail_is_not_checkpointed(self):
        first = line("Complete")
        self.chat.write_bytes((first + line("Partial").rstrip("\n")).encode())
        self.assertEqual(1, self.scan("one", all_history=True)["messages"])
        review.commit(self.root / "one", SHA)
        with self.chat.open("ab") as f:
            f.write(b"\n")
        self.assertEqual("Partial", (self.scan("two"), self.rows("two"))[1][0]["message"])

    def test_changed_prefix_and_stale_review_refused(self):
        self.chat.write_text(line("Original"), encoding="utf-8")
        self.scan("one", all_history=True)
        self.scan("stale", all_history=True)
        review.commit(self.root / "one", SHA)
        with self.assertRaisesRegex(ValueError, "Another review"):
            review.commit(self.root / "stale", SHA)
        self.assertFalse(self.state.with_suffix(".lock").exists())
        self.chat.write_text(line("Modified"), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "prefix changed"):
            self.scan("two")

    def test_unknown_header_private_and_review_required(self):
        self.chat.write_text(line("Known") + "2026-10-19 02:00:00 +00:00 [INF] unfamiliar AB12CD34 192.0.2.1\n", encoding="utf-8")
        self.assertEqual(1, self.scan("one", all_history=True)["unparsed"])
        self.assertNotIn("AB12CD34", json.dumps(self.rows("one", "unparsed")))
        with self.assertRaisesRegex(ValueError, "Review unparsed"):
            review.commit(self.root / "one", SHA)
        self.assertFalse(self.state.exists())
        review.commit(self.root / "one", SHA, allow_unparsed=True)

    def test_explicit_boundary_and_no_implicit_history(self):
        self.chat.write_text(line("Earlier", "2026-10-19 00:59:00") + "old continuation\n" + line("New"), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "No checkpoint"):
            self.scan("invalid")
        self.assertEqual(1, self.scan("one", since="2026-10-19T00:59:30Z")["messages"])
        self.assertEqual([], self.rows("one", "unparsed"))

    def test_commit_checks_prefix_but_allows_append(self):
        self.chat.write_text(line("Reviewed"), encoding="utf-8")
        self.scan("one", all_history=True)
        with self.chat.open("a", encoding="utf-8") as f:
            f.write(line("Not reviewed"))
        review.commit(self.root / "one", SHA)
        self.assertEqual(1, self.scan("two")["messages"])
        self.chat.write_text("corrupted\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "prefix changed"):
            review.commit(self.root / "two", SHA)
        self.assertFalse(self.state.with_suffix(".lock").exists())


if __name__ == "__main__":
    unittest.main()
