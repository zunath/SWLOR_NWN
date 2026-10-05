"""Linux fixture tests. Docker/Git are faked; no daemon or server is contacted."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

SCRIPTS = Path(__file__).resolve().parents[1]
COMMIT = "a" * 40
MANIFEST = "b" * 40
IMAGE_ID = "sha256:" + "c" * 64

DOCKER = r'''#!/usr/bin/env python3
import json, os, sys
from pathlib import Path
a=sys.argv[1:]
with open(os.environ['COMMAND_LOG'],'a') as f: f.write(json.dumps(['docker']+a)+'\n')
if a[0]=='info': print('linux')
elif a[0]=='image':
    print('1000:1000' if '.Config.User' in ' '.join(a) else 'sha256:'+'c'*64)
elif a[0]=='inspect':
    text=' '.join(a)
    if '.Image' in text: print('sha256:'+'d'*64)
    elif '.RestartCount' in text: print('false 0' if os.environ.get('FAIL_HEALTH') else 'true 0')
    else: print('false')
elif a[0]=='logs': print('Server: Module loaded')
elif a[0]=='compose':
    if 'ps' in a: print('new' if Path(os.environ['STARTED']).exists() else 'old')
    if 'stop' in a and os.environ.get('FAIL_STOP'): sys.exit(1)
    if 'up' in a:
        if os.environ.get('FAIL_START'): sys.exit(1)
        Path(os.environ['STARTED']).touch()
elif a[0]=='run':
    assert not any('/server,' in x for x in a), 'live server mounted in builder'
    mount=next(x for x in a if 'dst=/artifacts' in x)
    root=Path(mount.split('src=')[1].split(',')[0])
    for group,file in [('hak','test.hak'),('tlk','sw_tlk.tlk'),('modules','Star Wars LOR v2.mod'),('dotnet','SWLOR.Game.Server.dll')]:
        (root/group).mkdir(exist_ok=True)
        (root/group/file).write_text('fixture')
    (root/'dotnet/SWLOR.Game.Server.runtimeconfig.json').write_text('{"runtimeOptions":{"tfm":"net10.0"}}')
'''
GIT = r'''#!/usr/bin/env python3
import os,sys
a=sys.argv[1:]
if 'merge-base' in a and os.environ.get('REJECT_COMMIT'): sys.exit(1)
if 'get-url' in a: print('https://github.com/zunath/SWLOR_NWN')
elif 'rev-parse' in a: print('a'*40)
'''
BACKUP = r'''#!/usr/bin/env python3
import json,os,sys
from pathlib import Path
with open(os.environ['COMMAND_LOG'],'a') as f: f.write(json.dumps(['backup']+sys.argv[1:])+'\n')
if sys.argv[1]=='--check': sys.exit(1 if os.environ.get('FAIL_BACKUP_CHECK') else 0)
if os.environ.get('FAIL_BACKUP'): sys.exit(1)
if not os.environ.get('NO_BACKUP_MARKER'): (Path(sys.argv[1])/'backup-verified').write_text('fixture snapshot')
'''


class ProductionTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="production-test-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.server = self.root / "server"
        self.deployment = self.root / "deployment"
        self.server.mkdir()
        self.deployment.mkdir()
        for group in ("hak", "tlk", "modules", "dotnet", "servervault"):
            (self.server / group).mkdir()
            (self.server / group / "original").write_text("old")
        self.env_file = self.server / "swlor.env"
        self.env_file.write_text("NWN_NWSYNCURL=https://nwsync2.starwarsnwn.com\nNWN_NWSYNCHASH=" + "e"*40 + "\nSECRET=preserve-me\nNWNX_RENAME_SKIP=y\n")
        (self.server / "settings.tml").write_text('[ruleset]\nenforce-legal-characters = true\n[server]\nplayer-party-control = false\n["~~schema".binds."server.player-party-control"]\ndefault = false\n')
        (self.server / "docker-compose.yml").write_text("services: {}\n")
        self.bin = self.root / "bin"
        self.bin.mkdir()
        for name, content in {"docker": DOCKER, "git": GIT, "backup": BACKUP}.items():
            (self.bin / name).write_text(content)
            (self.bin / name).chmod(0o750)
        self.script = self.root / "deploy.sh"
        shutil.copyfile(SCRIPTS / "swlor-production-deploy.sh", self.script)
        self.build_script = self.root / "build.sh"
        shutil.copyfile(SCRIPTS / "swlor-production-build.sh", self.build_script)
        self.build_script.chmod(0o750)
        example = (SCRIPTS / "swlor-production.conf.example").read_text()
        example = example.replace("SERVER_ROOT=/home/nwn/server", f"SERVER_ROOT={self.server}")
        example = example.replace("DEPLOYMENT_ROOT=/home/nwn/production-deployment", f"DEPLOYMENT_ROOT={self.deployment}")
        example = example.replace("BUILD_SCRIPT=/usr/local/lib/swlor-production/build.sh", f"BUILD_SCRIPT={self.build_script}")
        example += f"\nMIN_FREE_GIB=0\nHEALTH_STABLE_SECONDS=0\nBACKUP_COMMAND={self.bin / 'backup'}\n"
        self.config = self.root / "config"
        self.config.write_text(example)
        self.config.chmod(0o640)
        self.command_log = self.root / "commands"
        self.environ = dict(os.environ, SWLOR_PRODUCTION_CONFIG=str(self.config), PATH=f"{self.bin}:{os.environ['PATH']}",
                            COMMAND_LOG=str(self.command_log), STARTED=str(self.root / "started"))
        source = self.deployment / "source"
        (source / ".git").mkdir(parents=True)
        (source / "SWLOR_Haks").mkdir()
        (source / "Build").mkdir()
        (source / "Build/hakbuilder.json").write_text(json.dumps({"TlkPath":"../SWLOR_Haks/sw_tlk/sw_tlk.tlk","OutputPath":"../debugserver/","HakList":[{"Name":"test","Path":"../SWLOR_Haks/test/"}]}))
        (source / "scripts/deployment").mkdir(parents=True)
        (source / "scripts/deployment/server-image.txt").write_text("zunath/nwn-dotnet:8193.37.17-2\n")
        checksum = "f9cc2e50fbe6f750954d11b824434a92b12913cc0a3442e95dcc5eefd4ddc387"
        (self.deployment / f"cache/tools-{checksum}").mkdir(parents=True)
        for file in ("nwn_erf", "nwn_gff", "nwn_tlk"):
            (self.deployment / f"cache/tools-{checksum}" / file).write_text("fixture")

    def run_mode(self, mode, **environ):
        args = ["bash", str(self.script), f"--{mode}"]
        if mode != "check": args += ["--commit", COMMIT, "--hash", MANIFEST]
        return subprocess.run(args, env=dict(self.environ, **environ), text=True, capture_output=True, timeout=20)

    def commands(self):
        return [json.loads(line) for line in self.command_log.read_text().splitlines()] if self.command_log.exists() else []

    def dispatch(self, **record_changes):
        state = self.deployment / "state"
        state.mkdir(exist_ok=True)
        (state / "github-dispatch-enabled-at").write_text("2026-10-05T00:00:00Z\n")
        record = dict(id=123, event="workflow_dispatch", status="completed", conclusion="success", head_branch="master",
                      actor=dict(login="zunath"), triggering_actor=dict(login="zunath"), created_at="2026-10-06T00:00:00Z",
                      display_title=f"Deploy production {COMMIT} {MANIFEST}")
        record.update(record_changes)
        (self.root / "response.json").write_text(json.dumps(dict(workflow_runs=[record])))
        (self.bin / "curl").write_text(f'#!/bin/bash\ncat "{self.root / "response.json"}"\n')
        (self.bin / "curl").chmod(0o750)
        command = self.bin / "accepted"
        command.write_text(f'#!/bin/bash\nprintf "%s\\n" "$@" > "{self.root / "accepted-args"}"\n')
        command.chmod(0o750)
        return subprocess.run(["bash", str(SCRIPTS / "swlor-production-dispatch.sh")],
                              env=dict(self.environ, SWLOR_PRODUCTION_COMMAND=str(command)), capture_output=True, text=True)

    def enable(self):
        self.config.write_text(self.config.read_text().replace("DEPLOYMENTS_ENABLED=false", "DEPLOYMENTS_ENABLED=true"))

    def prepare(self):
        result = self.run_mode("prepare")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        return self.deployment / f"releases/{COMMIT}-{MANIFEST}"

    def no_cutover(self):
        self.assertFalse(any("stop" in call or "up" in call or "down" in call for call in self.commands()))

    def test_check_is_read_only(self):
        before = set(self.deployment.iterdir())
        result = self.run_mode("check")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(before, set(self.deployment.iterdir()))
        self.no_cutover()

    def test_disabled_deploy_fails_before_docker(self):
        result = self.run_mode("deploy")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("disabled", result.stderr)
        self.assertEqual(self.commands(), [])

    def test_disabled_dispatch_ignores_requests(self):
        result = self.dispatch()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertFalse((self.root / "accepted-args").exists())
        self.assertFalse((self.deployment / "state/github-dispatch-run").exists())

    def test_dispatch_requires_both_owner_identities(self):
        self.enable()
        for field in ("actor", "triggering_actor"):
            result = self.dispatch(**{field:dict(login="someone-else")})
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertFalse((self.root / "accepted-args").exists())

    def test_dispatch_rejects_old_failed_or_wrong_branch_requests(self):
        self.enable()
        for changes in (dict(head_branch="feature"),dict(conclusion="failure"),dict(created_at="2026-10-04T00:00:00Z")):
            result = self.dispatch(**changes)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertFalse((self.root / "accepted-args").exists())

    def test_dispatch_rejects_malformed_or_injected_inputs(self):
        self.enable()
        for title in ("Deploy production master " + MANIFEST, f"Deploy production {COMMIT} {MANIFEST}; reboot",
                      f"Deploy production {COMMIT}\t{MANIFEST}"):
            result = self.dispatch(display_title=title)
            self.assertNotEqual(result.returncode, 0)
            self.assertFalse((self.root / "accepted-args").exists())

    def test_dispatch_uses_exact_commit_hash_and_claims_once(self):
        self.enable()
        result = self.dispatch()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.root / "accepted-args").read_text().splitlines(),["--deploy","--commit",COMMIT,"--hash",MANIFEST])
        self.assertEqual((self.deployment / "state/github-dispatch-run").read_text().strip(),"123")
        (self.root / "accepted-args").unlink()
        self.assertEqual(self.dispatch().returncode, 0)
        self.assertFalse((self.root / "accepted-args").exists())

    def test_dispatch_refuses_requests_during_recovery(self):
        self.enable()
        state = self.deployment / "state"
        state.mkdir()
        (state / "recovery-required").write_text("restore-needed")
        self.assertNotEqual(self.dispatch().returncode, 0)
        self.assertFalse((self.root / "accepted-args").exists())

    def test_prepare_preserves_live_files_and_secrets(self):
        before = {str(p): p.read_bytes() for p in self.server.rglob("*") if p.is_file()}
        release = self.prepare()
        self.assertEqual(before, {str(p): p.read_bytes() for p in self.server.rglob("*") if p.is_file()})
        self.assertIn("SECRET=preserve-me", (release / "swlor.env").read_text())
        self.assertIn("NWN_NWSYNCHASH=" + MANIFEST, (release / "swlor.env").read_text())
        self.assertIn("NWNX_RENAME_SKIP=n", (release / "swlor.env").read_text())
        self.assertIn("player-party-control = true", (release / "settings.tml").read_text())
        self.assertIn('default = false', (release / "settings.tml").read_text())
        self.no_cutover()

    def test_invalid_sha_is_rejected_without_commands(self):
        result = subprocess.run(["bash",str(self.script),"--prepare","--commit","master","--hash",MANIFEST],env=self.environ,capture_output=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(self.commands(), [])

    def test_unreleased_commit_fails_before_builder(self):
        result = self.run_mode("prepare", REJECT_COMMIT="1")
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(any(call[1] == "run" for call in self.commands()))
        self.no_cutover()

    def test_modified_artifact_rejected_before_stop(self):
        release = self.prepare()
        (release / "artifacts/hak/test.hak").write_text("tampered")
        self.enable()
        self.assertNotEqual(self.run_mode("deploy").returncode, 0)
        self.no_cutover()

    def test_extra_artifact_rejected_before_stop(self):
        release = self.prepare()
        (release / "artifacts/hak/unexpected.hak").write_text("extra")
        self.enable()
        self.assertNotEqual(self.run_mode("deploy").returncode, 0)
        self.no_cutover()

    def test_symlink_in_stage_rejected_before_stop(self):
        release = self.prepare()
        (release / "artifacts/dotnet/external").symlink_to(self.env_file)
        self.enable()
        self.assertNotEqual(self.run_mode("deploy").returncode, 0)
        self.no_cutover()

    def test_installer_refuses_enabled_deployment_gate(self):
        self.enable()
        result = subprocess.run(["bash",str(SCRIPTS / "install-production.sh"),str(self.config)],
                                env=self.environ,text=True,capture_output=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("deployments disabled",result.stderr)
        self.assertFalse((self.deployment / "state").exists())

    def test_installer_refuses_active_timer_before_writing(self):
        systemctl = self.bin / "systemctl"
        systemctl.write_text('#!/bin/bash\n[[ "$*" == *"is-active"*"swlor-production-deploy.timer"* ]]\n')
        systemctl.chmod(0o750)
        result = subprocess.run(["bash",str(SCRIPTS / "install-production.sh"),str(self.config)],
                                env=self.environ,text=True,capture_output=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("timer is running",result.stderr)
        self.assertFalse((self.deployment / "state").exists())

    def test_changed_host_config_rejected_before_stop(self):
        self.prepare()
        self.env_file.write_text(self.env_file.read_text() + "CHANGE=later\n")
        self.enable()
        result = self.run_mode("deploy")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("configuration changed", result.stderr)
        self.no_cutover()

    def test_symlink_runtime_path_rejected_before_stop(self):
        self.prepare()
        (self.server / "logs").symlink_to(self.deployment)
        self.enable()
        self.assertNotEqual(self.run_mode("deploy").returncode, 0)
        self.no_cutover()

    def test_backup_preflight_failure_rejected_before_stop(self):
        self.prepare()
        self.enable()
        self.assertNotEqual(self.run_mode("deploy", FAIL_BACKUP_CHECK="1").returncode, 0)
        self.no_cutover()

    def test_unverified_backup_never_swaps_files(self):
        self.prepare()
        self.enable()
        self.assertNotEqual(self.run_mode("deploy", NO_BACKUP_MARKER="1").returncode, 0)
        self.assertEqual((self.server / "hak/original").read_text(), "old")
        self.assertTrue((self.deployment / "state/recovery-required").exists())
        self.assertFalse(any("up" in call for call in self.commands()))

    def test_failed_start_leaves_hold_and_disables_restart(self):
        self.prepare()
        self.enable()
        result = self.run_mode("deploy", FAIL_START="1")
        self.assertNotEqual(result.returncode, 0, result.stdout)
        self.assertTrue((self.deployment / "state/recovery-required").exists())
        self.assertTrue(any("--restart=no" in call for call in self.commands()))
        self.assertEqual(sum("up" in call for call in self.commands()), 1)
        calls_before = len(self.commands())
        self.assertNotEqual(self.run_mode("deploy").returncode, 0)
        self.assertEqual(len(self.commands()), calls_before)

    def test_health_failure_never_starts_old_image(self):
        self.prepare()
        self.enable()
        self.assertNotEqual(self.run_mode("deploy", FAIL_HEALTH="1").returncode, 0)
        self.assertTrue((self.deployment / "state/recovery-required").exists())
        self.assertEqual(sum("up" in call for call in self.commands()), 1)

    def test_failed_stop_reports_unconfirmed_container_state(self):
        self.prepare()
        self.enable()
        result = self.run_mode("deploy", FAIL_STOP="1")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Could not confirm", result.stdout)
        self.assertTrue((self.deployment / "state/recovery-required").exists())
        self.assertFalse(any("up" in call for call in self.commands()))

    def test_success_stops_only_game_after_backup_preflight(self):
        self.prepare()
        self.enable()
        result = self.run_mode("deploy")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        calls = self.commands()
        stop = next(i for i,c in enumerate(calls) if "stop" in c)
        backup = next(i for i,c in enumerate(calls) if c[0] == "backup" and c[1] != "--check")
        up = next(i for i,c in enumerate(calls) if "up" in c)
        self.assertLess(stop, backup)
        self.assertLess(backup, up)
        self.assertEqual(calls[stop][-1], "swlor-server")
        self.assertIn("--no-deps", calls[up])
        self.assertFalse(any("down" in call for call in calls))
        self.assertEqual((self.deployment / "state/active-release").read_text().strip(), COMMIT + " " + MANIFEST)
        self.assertFalse((self.deployment / "state/recovery-required").exists())


if __name__ == "__main__":
    unittest.main(verbosity=2)
