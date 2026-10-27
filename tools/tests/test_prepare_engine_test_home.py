"""The HAK checks and staging in scripts/prepare-engine-test-home.sh accept the sparse SWLOR_Haks checkout that
scripts/ci/fetch-engine-test-haks.sh makes (stubbed file types left out) and stage the same entries as a full checkout.

The two Python blocks are cut out of the shell script and run against small fixture repositories."""
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import unittest

REPO = Path(__file__).resolve().parents[2]
SCRIPT = (REPO / "scripts" / "prepare-engine-test-home.sh").read_text(encoding="utf-8")
FETCH_SCRIPT = (REPO / "scripts" / "ci" / "fetch-engine-test-haks.sh").read_text(encoding="utf-8")


def heredoc(name):
    return re.search(r"<<'%s'\n(.*?)\n%s\n" % (name, name), SCRIPT, re.S).group(1)


CHECK_HAKS = heredoc("CHECK_HAKS")
STAGE_HAKS = heredoc("STAGE_HAKS")
STUBBED_TYPES = re.search(r'^export STUBBED_TYPES="([^"]*)"', SCRIPT, re.M).group(1)

FILES = {
    "sw_a/one.2da": "2da",
    "sw_a/model.mdl": "mdl",
    "sw_a/sub/deep.MDL": "MDL",
    "sw_a/sub/tex.tga": "tga",
    "sw_music/song.bmu": "bmu",
    "sw_b/two.2da": "2da",
    "sw_b/walk.wok": "wok",
    "sw_b/snd.wav": "wav",
    "sw_tlk/sw_tlk.tlk": "tlk",
}


def git(*args, cwd, stdin=None):
    return subprocess.run(["git", *args], cwd=cwd, check=True, capture_output=True, text=True, input=stdin).stdout


def remove_tree(path):
    shutil.rmtree(path, onexc=lambda function, target, error: (os.chmod(target, 0o700), function(target)))


class PrepareScriptTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.mkdtemp(prefix="prepare-test-")
        self.addCleanup(remove_tree, self.temp)

    def make_repo(self, name, sparse_patterns=None):
        """A parent folder with Build/hakbuilder.json and a SWLOR_Haks repository holding three HAKs."""
        root = os.path.join(self.temp, name)
        haks = os.path.join(root, "SWLOR_Haks")
        os.makedirs(os.path.join(root, "Build"))
        os.makedirs(haks)
        git("init", "-q", cwd=haks)
        git("config", "user.email", "test@example.com", cwd=haks)
        git("config", "user.name", "test", cwd=haks)
        for relative, content in FILES.items():
            path = os.path.join(haks, *relative.split("/"))
            os.makedirs(os.path.dirname(path), exist_ok=True)
            Path(path).write_text(content)
        git("add", "-A", cwd=haks)
        git("commit", "-q", "-m", "haks", cwd=haks)
        with open(os.path.join(root, "Build", "hakbuilder.json"), "w") as handle:
            json.dump({"TlkPath": "../SWLOR_Haks/sw_tlk/sw_tlk.tlk", "OutputPath": "../out/",
                       "EnableChecksumChecking": False,
                       "HakList": [{"Name": hak, "Path": f"../SWLOR_Haks/{hak}/", "CompileModels": False}
                                   for hak in ("sw_a", "sw_b", "sw_music")]}, handle)
        if sparse_patterns:
            git("sparse-checkout", "set", "--no-cone", "--stdin", cwd=haks, stdin="\n".join(sparse_patterns) + "\n")
        return root, haks

    def sparse_like_the_fetch_script(self, folders):
        return ["/%s/" % folder for folder in folders] + ["!*" + suffix for suffix in STUBBED_TYPES.split()]

    def run_block(self, code, *arguments):
        environment = dict(os.environ, STUBBED_TYPES=STUBBED_TYPES)
        return subprocess.run([sys.executable, "-", *arguments], input=code, text=True, capture_output=True,
                              env=environment)

    def stage(self, root):
        work = os.path.join(root, "work")
        os.makedirs(work)
        result = self.run_block(STAGE_HAKS, root, work, os.path.join(root, "out"))
        self.assertEqual(0, result.returncode, result.stderr)
        entries = {}
        for folder, _, names in os.walk(work):
            for name in names:
                if name != "hakbuilder.json":
                    path = os.path.join(folder, name)
                    entries[os.path.relpath(path, work).replace(os.sep, "/")] = os.path.getsize(path)
        return entries

    def test_the_fetch_script_reads_the_stubbed_types_from_the_prepare_script(self):
        self.assertIn("sed -n -E 's/^export STUBBED_TYPES=", FETCH_SCRIPT)
        self.assertEqual({".mdl", ".tga", ".dds", ".plt", ".wav", ".bmu"}, set(STUBBED_TYPES.split()))

    def test_full_checkout_passes_and_stubs_only_the_files_on_disk(self):
        root, _ = self.make_repo("full")
        self.assertEqual(0, self.run_block(CHECK_HAKS, root).returncode)
        entries = self.stage(root)
        self.assertEqual(8, len(entries))
        self.assertEqual(0, entries["sw_a/model.mdl"])
        self.assertEqual(0, entries["sw_a/sub/deep.MDL"])
        self.assertEqual(3, entries["sw_a/one.2da"])

    def test_sparse_checkout_without_the_stubbed_types_stages_the_same_entries_as_a_full_one(self):
        full_root, _ = self.make_repo("full")
        sparse_root, haks = self.make_repo(
            "sparse", self.sparse_like_the_fetch_script(["sw_a", "sw_b", "sw_music", "sw_tlk"]))
        on_disk = {name.lower() for _, _, names in os.walk(haks) for name in names}
        self.assertFalse(any(name.endswith(tuple(STUBBED_TYPES.split())) for name in on_disk))
        self.assertFalse(os.path.isdir(os.path.join(haks, "sw_music")), "a folder of stubbed files only is absent")

        self.assertEqual(0, self.run_block(CHECK_HAKS, sparse_root).returncode)
        self.assertEqual(self.stage(full_root), self.stage(sparse_root))

    def test_sparse_checkout_missing_a_folder_fails_the_check(self):
        root, _ = self.make_repo("broken", self.sparse_like_the_fetch_script(["sw_a", "sw_music", "sw_tlk"]))
        result = self.run_block(CHECK_HAKS, root)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("sw_b", result.stderr)

    def test_full_checkout_with_a_deleted_folder_fails_the_check(self):
        root, haks = self.make_repo("deleted")
        remove_tree(os.path.join(haks, "sw_b"))
        result = self.run_block(CHECK_HAKS, root)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("sw_b", result.stderr)

    def test_an_exported_copy_without_git_behaves_as_before(self):
        full_root, _ = self.make_repo("full")
        root, haks = self.make_repo("export")
        remove_tree(os.path.join(haks, ".git"))
        self.assertEqual(0, self.run_block(CHECK_HAKS, root).returncode)
        self.assertEqual(self.stage(full_root), self.stage(root))


if __name__ == "__main__":
    unittest.main()
