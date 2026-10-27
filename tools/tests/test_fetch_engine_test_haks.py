"""scripts/ci/fetch-engine-test-haks.sh against a local fixture remote: the sparse checkout holds every file except the
types prepare-engine-test-home.sh stubs (in any case), keeps those as skip-worktree index entries, and later git commands
in it need no network."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

REPO = Path(__file__).resolve().parents[2]
HAK_COUNT = 101  # the script refuses to run with fewer than 100 folders
KEPT = {
    ".gitattributes": "*.set text eol=crlf\n",
    ".gitignore": "*.tmp\n",
    "sw_h000/one.2da": "2da",
    "sw_h000/walk.wok": "wok",
    "sw_h001/sub/deep.set": "first\nsecond\n",
    "sw_tlk/sw_tlk.tlk": "tlk",
    "unrelated/notes.txt": "not a HAK folder",
}
STUBBED = {
    "sw_h000/model.mdl": "mdl",
    "sw_h000/Model2.MDL": "MDL",
    "sw_h001/tex.tga": "tga",
    "sw_h001/Texture.TGA": "TGA",
    "sw_h001/sub/pic.Dds": "Dds",
    "sw_stubonly/song.bmu": "bmu",
    "sw_h002/wave.WAV": "WAV",
}


def git(*args, cwd, check=True):
    return subprocess.run(["git", *args], cwd=cwd, check=check, capture_output=True, text=True)


def remove_tree(path):
    shutil.rmtree(path, onexc=lambda function, target, error: (os.chmod(target, 0o700), function(target)))


def usable(command):
    try:
        return subprocess.run(command, capture_output=True).returncode == 0
    except OSError:
        return False


# Resolved through PATH here, because on Windows a bare "bash" would start the WSL launcher in System32.
BASH = shutil.which("bash") or "bash"


@unittest.skipUnless(usable([BASH, "-c", "mapfile -t x < /dev/null && python3 --version"]),
                     "needs bash 4 and python3 on bash's PATH")
class FetchEngineTestHaksTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.mkdtemp(prefix="fetch-haks-test-")
        self.addCleanup(remove_tree, self.temp)
        self.remote = os.path.join(self.temp, "remote")
        self.parent = os.path.join(self.temp, "parent")

        os.makedirs(self.remote)
        git("init", "-q", cwd=self.remote)
        git("config", "user.email", "test@example.com", cwd=self.remote)
        git("config", "user.name", "test", cwd=self.remote)
        git("config", "uploadpack.allowFilter", "true", cwd=self.remote)
        git("config", "uploadpack.allowAnySHA1InWant", "true", cwd=self.remote)
        for name, content in {**KEPT, **STUBBED}.items():
            path = os.path.join(self.remote, *name.split("/"))
            os.makedirs(os.path.dirname(path), exist_ok=True)
            Path(path).write_bytes(content.encode())
        for index in range(HAK_COUNT):
            path = os.path.join(self.remote, f"sw_h{index:03d}", "keep.2da")
            os.makedirs(os.path.dirname(path), exist_ok=True)
            Path(path).write_text("2da")
        git("add", "-A", cwd=self.remote)
        git("commit", "-q", "-m", "haks", cwd=self.remote)
        self.sha = git("rev-parse", "HEAD", cwd=self.remote).stdout.strip()

        # The parent checkout: the two scripts, the builder config and a gitlink to the fixture commit.
        for relative in ("scripts/prepare-engine-test-home.sh", "scripts/ci/fetch-engine-test-haks.sh"):
            target = os.path.join(self.parent, *relative.split("/"))
            os.makedirs(os.path.dirname(target), exist_ok=True)
            shutil.copyfile(REPO / relative, target)
        os.makedirs(os.path.join(self.parent, "Build"))
        with open(os.path.join(self.parent, "Build", "hakbuilder.json"), "w") as handle:
            json.dump({"TlkPath": "../SWLOR_Haks/sw_tlk/sw_tlk.tlk", "OutputPath": "../out/",
                       "HakList": [{"Name": name, "Path": f"../SWLOR_Haks/{name}/"}
                                   for name in [f"sw_h{index:03d}" for index in range(HAK_COUNT)] + ["sw_stubonly"]]},
                      handle)
        Path(self.parent, ".gitmodules").write_text('[submodule "SWLOR_Haks"]\n\tpath = SWLOR_Haks\n\turl = unused\n')
        git("init", "-q", cwd=self.parent)
        git("config", "user.email", "test@example.com", cwd=self.parent)
        git("config", "user.name", "test", cwd=self.parent)
        git("config", "core.autocrlf", "false", cwd=self.parent)
        git("add", "-A", cwd=self.parent)
        git("update-index", "--add", "--cacheinfo", f"160000,{self.sha},SWLOR_Haks", cwd=self.parent)
        git("commit", "-q", "-m", "parent", cwd=self.parent)

    def fetch(self):
        # core.ignoreCase=false makes the sparse patterns case-sensitive as they are on Linux (git on Windows ignores case).
        environment = dict(os.environ, SWLOR_HAKS_URL=Path(self.remote).as_uri(), GIT_CONFIG_COUNT="1",
                           GIT_CONFIG_KEY_0="core.ignoreCase", GIT_CONFIG_VALUE_0="false")
        return subprocess.run([BASH, "scripts/ci/fetch-engine-test-haks.sh"], cwd=self.parent, env=environment,
                              capture_output=True, text=True)

    def on_disk(self):
        haks = os.path.join(self.parent, "SWLOR_Haks")
        return {os.path.relpath(os.path.join(folder, name), haks).replace(os.sep, "/")
                for folder, folders, names in os.walk(haks) for name in names
                if ".git" not in os.path.relpath(folder, haks).split(os.sep)}

    def test_fetches_everything_except_the_stubbed_types_in_any_case(self):
        result = self.fetch()
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        haks = os.path.join(self.parent, "SWLOR_Haks")
        self.assertEqual(self.sha, git("rev-parse", "HEAD", cwd=haks).stdout.strip())

        on_disk = self.on_disk()
        for name in ("sw_h000/one.2da", "sw_h000/walk.wok", "sw_h001/sub/deep.set", "sw_tlk/sw_tlk.tlk",
                     ".gitattributes", ".gitignore", "sw_h100/keep.2da"):
            self.assertIn(name, on_disk)
        for name in STUBBED:
            self.assertNotIn(name, on_disk)
        self.assertNotIn("unrelated/notes.txt", on_disk)
        # .set files follow the .gitattributes of the commit (CRLF), as in a full checkout.
        content = Path(haks, "sw_h001", "sub", "deep.set").read_bytes()
        self.assertEqual(b"first\r\nsecond\r\n", content)

    def test_left_out_files_are_skip_worktree_entries_and_nothing_else_is_missing(self):
        self.assertEqual(0, self.fetch().returncode)
        haks = os.path.join(self.parent, "SWLOR_Haks")
        tags = [line.split(" ", 1) for line in git("ls-files", "-t", cwd=haks).stdout.splitlines()]
        left_out = {name for tag, name in tags if tag == "S"}
        self.assertTrue({"sw_h000/model.mdl", "sw_h000/Model2.MDL", "sw_h001/tex.tga", "sw_h001/sub/pic.Dds",
                         "sw_stubonly/song.bmu", "sw_h002/wave.WAV", "sw_h001/Texture.TGA"} <= left_out)
        self.assertIn("unrelated/notes.txt", left_out, "files outside the HAK folders are not fetched")
        self.assertNotIn("sw_h000/one.2da", left_out)
        self.assertFalse(os.path.isdir(os.path.join(haks, "sw_stubonly")), "a folder of stubbed files only is absent")

    def test_git_commands_the_prepare_script_runs_need_no_network(self):
        self.assertEqual(0, self.fetch().returncode)
        haks = os.path.join(self.parent, "SWLOR_Haks")
        # The fixture remote disappears; any command that wanted a missing blob would now fail.
        git("remote", "set-url", "origin", Path(os.path.join(self.temp, "gone")).as_uri(), cwd=haks)
        for command in (["diff", "HEAD", "--binary", "--", "."], ["ls-files", "--others", "--exclude-standard", "-z", "--", "."],
                        ["ls-tree", "HEAD", "--", "."], ["ls-files", "-t", "-z"]):
            result = git(*command, cwd=haks, check=False)
            self.assertEqual(0, result.returncode, f"git {' '.join(command)}: {result.stderr}")
        self.assertEqual("", git("diff", "HEAD", "--binary", "--", ".", cwd=haks).stdout)

    def test_a_missing_stubbed_types_definition_stops_the_script(self):
        script = os.path.join(self.parent, "scripts", "prepare-engine-test-home.sh")
        Path(script).write_text("#!/usr/bin/env bash\n")
        result = self.fetch()
        self.assertNotEqual(0, result.returncode)
        self.assertIn("STUBBED_TYPES", result.stderr)


if __name__ == "__main__":
    unittest.main()
