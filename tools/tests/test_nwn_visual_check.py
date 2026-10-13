"""Profile boundaries plus opt-in real Windows isolation/lifetime checks."""
from pathlib import Path
import json
import os
import socket
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from nwn_visual_check import offline_client, private_desktop, profile
from nwn_visual_check.__main__ import parse_captures, main
from nwn_visual_check.result import ClientRun


class ProfileBoundaryTests(unittest.TestCase):
    def setUp(self):
        self.owned = profile.create_profile()
        self.command = ['nwmain.exe','-userdirectory',str(self.owned)]

    def tearDown(self):
        profile.cleanup(self.owned)

    def validate(self):
        profile.validate_profile(self.owned,self.command,5)

    def settings(self,old,new):
        path = self.owned/'settings.tml'
        path.write_text(path.read_text().replace(old,new))

    def test_generated_profile_is_silent_private_and_background_ready(self):
        self.validate()

    def test_native_refusal_happens_before_process_creation(self):
        self.settings('volume=0.0','volume=0.5')
        with patch.object(offline_client.ctypes,'WinDLL',create=True) as native:
            with self.assertRaisesRegex(RuntimeError,'sound channels'):
                offline_client.run(self.command,profile=self.owned,cwd=self.owned,timeout=5)
        native.assert_not_called()

    def test_each_audio_channel_is_required(self):
        path=self.owned/'settings.tml'
        original=path.read_text()
        for channel in ('effects','music','streams','voices'):
            with self.subTest(channel=channel):
                path.write_text(original.replace(f'[sound.{channel}]\nvolume=0.0',f'[sound.{channel}]\nvolume=1.0'))
                with self.assertRaisesRegex(RuntimeError,'sound channels'):self.validate()
        path.write_text(original)

    def test_boolean_volume_is_not_numeric_silence(self):
        self.settings('volume=0.0','volume=false')
        with self.assertRaisesRegex(RuntimeError,'sound channels'):self.validate()

    def test_duplicate_userdirectory_is_refused(self):
        with self.assertRaisesRegex(RuntimeError,'validated profile'):
            profile.validate_profile(self.owned,self.command+['-userdirectory',str(self.owned.parent)],5)

    def test_intro_movies_cannot_bypass_silence(self):
        self.settings('[graphics.movies.intro]\nenabled=false','[graphics.movies.intro]\nenabled=true')
        with self.assertRaisesRegex(RuntimeError,'movies'):self.validate()

    def test_forbidden_listener_is_refused(self):
        self.settings('port=', 'unused-port=')
        with (self.owned/'settings.tml').open('a') as stream:stream.write('port=5121\n')
        with self.assertRaisesRegex(RuntimeError,'permitted port'):self.validate()

    def test_public_profile_is_refused(self):
        self.settings('public=false','public=true')
        with self.assertRaisesRegex(RuntimeError,'private local'):self.validate()

    def test_focus_pause_is_refused(self):
        self.settings('[game.auto-pause.on-focus-loss]\nenabled=false','[game.auto-pause.on-focus-loss]\nenabled=true')
        with self.assertRaisesRegex(RuntimeError,'without focus'):self.validate()

    def test_userdirectory_cannot_point_elsewhere(self):
        with self.assertRaisesRegex(RuntimeError,'validated profile'):
            profile.validate_profile(self.owned,['nwmain.exe','-userdirectory',str(self.owned.parent)],5)

    def test_existing_key_is_refused(self):
        (self.owned/'cdkey.ini').write_text('fixture only')
        with self.assertRaisesRegex(RuntimeError,'key-free'):self.validate()

    def test_ordinary_temp_directory_is_not_owned(self):
        with tempfile.TemporaryDirectory() as temporary:
            with self.assertRaisesRegex(RuntimeError,'owned temporary'):
                profile.validate_profile(Path(temporary),['nwmain.exe'],5)

    def test_hardlinks_to_shared_assets_are_refused(self):
        with tempfile.TemporaryDirectory() as temporary:
            original=Path(temporary)/'shared.hak';original.write_bytes(b'shared data')
            link=self.owned/'hak/shared.hak'
            try:
                os.link(original,link)
                with self.assertRaisesRegex(RuntimeError,'hardlinks'):self.validate()
            finally:
                link.unlink(missing_ok=True)
            self.assertEqual(original.read_bytes(),b'shared data')

    def test_staging_copies_do_not_share_source_file_identity(self):
        with tempfile.TemporaryDirectory() as temporary:
            source=Path(temporary)/'shared.hak';source.write_bytes(b'unchanged')
            target=self.owned/'hak/shared.hak';profile.copy_input(source,target)
            self.assertFalse(os.path.samefile(source,target))
            target.write_bytes(b'changed in isolated profile')
            self.assertEqual(source.read_bytes(),b'unchanged')

    def test_directory_junction_is_refused(self):
        with tempfile.TemporaryDirectory() as temporary:
            link=self.owned/'external'
            try:
                if sys.platform=='win32':
                    subprocess.run(['powershell','-NoProfile','-NonInteractive','-Command',f"New-Item -ItemType Junction -Path '{link}' -Value '{temporary}' | Out-Null"],check=True,capture_output=True,timeout=20)
                else:
                    link.symlink_to(temporary,target_is_directory=True)
                with self.assertRaisesRegex(RuntimeError,'reparse'):self.validate()
            finally:
                if link.is_junction():link.rmdir()
                elif link.is_symlink():link.unlink()

    def test_timeout_is_bounded_before_native_start(self):
        for timeout in (0,-1,601,float('inf'),float('nan')):
            with self.subTest(timeout=timeout),self.assertRaises(ValueError):
                profile.validate_profile(self.owned,self.command,timeout)

    def test_capture_filenames_cannot_escape_output(self):
        for value in ('READY=../escape','READY=bad/name','READY=','missing'):
            with self.subTest(value=value),self.assertRaises(ValueError):parse_captures([value])
        with self.assertRaisesRegex(ValueError,'unique'):parse_captures(['ONE=same','TWO=same'])
        self.assertEqual(parse_captures(['READY=close']),{'READY':'close'})


class EvidenceCleanupTests(unittest.TestCase):
    @unittest.skipUnless(sys.platform == 'win32', 'CLI requires Windows')
    def test_evidence_write_failure_still_removes_owned_profile(self):
        owned = profile.create_profile()
        write_text = Path.write_text
        def fail_log(path, *args, **kwargs):
            if path.name == 'client.log':
                raise OSError('Evidence storage failed')
            return write_text(path, *args, **kwargs)
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary)
            fixture=root/'fixture.mod';fixture.write_bytes(b'fixture')
            try:
                with patch('nwn_visual_check.module.discover_client',return_value=Path('C:/nwmain.exe')), \
                        patch('nwn_visual_check.profile.create_profile',return_value=owned), \
                        patch('nwn_visual_check.offline_client.run',return_value=ClientRun(0,'',True,False)), \
                        patch.object(Path,'write_text',fail_log):
                    with self.assertRaisesRegex(OSError,'Evidence storage failed'):
                        main(['--module',str(fixture),'--output',str(root/'evidence'),'--capture','READY=close'])
                self.assertFalse(owned.exists())
                manifest=json.loads((root/'evidence/manifest.json').read_text())
                self.assertFalse(manifest['success'])
                self.assertTrue(manifest['profile_removed'])
                self.assertEqual(manifest['evidence_failure'],'Evidence storage failed')
            finally:
                if owned.exists():profile.cleanup(owned)


@unittest.skipUnless(sys.platform=='win32' and os.environ.get('SWLOR_NATIVE_ISOLATION_TESTS')=='1','Set SWLOR_NATIVE_ISOLATION_TESTS=1 for actual Windows probes')
class WindowsIsolationTests(unittest.TestCase):
    def setUp(self):self.owned=profile.create_profile()
    def tearDown(self):profile.cleanup(self.owned)

    def command(self,code):
        return [str(Path(os.environ['SystemRoot'])/'System32/WindowsPowerShell/v1.0/powershell.exe'),'-NoProfile','-NonInteractive','-Command',code]

    def evidence(self):return json.loads((self.owned/'isolation.json').read_text())

    def test_callback_failure_stops_owned_process_and_releases_desktop_and_acl(self):
        def fail(_):raise RuntimeError('Capture failed')
        with self.assertRaisesRegex(RuntimeError,'Capture failed'):
            offline_client.run(self.command('Start-Sleep -Seconds 20'),profile=self.owned,cwd=self.owned,timeout=5,on_start=fail)
        evidence=self.evidence()
        self.assertTrue(evidence['process_exited'])
        self.assertIsNone(private_desktop.process_desktop(evidence['pid']))
        permissions=subprocess.run(['icacls',str(self.owned)],check=True,capture_output=True,text=True).stdout
        self.assertNotIn(evidence['appcontainer_sid'],permissions)

    def test_timeout_stops_owned_process(self):
        with self.assertRaises(subprocess.TimeoutExpired):
            offline_client.run(self.command('Start-Sleep -Seconds 20'),profile=self.owned,cwd=self.owned,timeout=1)
        self.assertTrue(self.evidence()['timed_out'])
        self.assertTrue(self.evidence()['process_exited'])

    def test_zero_capability_process_cannot_reach_verified_loopback_receiver(self):
        with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as receiver:
            receiver.bind(('127.0.0.1',0));port=receiver.getsockname()[1]
            code=f"$s=[Net.Sockets.UdpClient]::new();$b=[Text.Encoding]::UTF8.GetBytes('probe');try{{[void]$s.Send($b,$b.Length,'127.0.0.1',{port})}}catch{{Write-Output 'denied'}};$s.Dispose();Write-Output 'finished'"
            command=self.command(code)
            subprocess.run(command,check=True,capture_output=True,timeout=20)
            receiver.settimeout(3)
            self.assertEqual(receiver.recvfrom(128)[0],b'probe')
            result=offline_client.run(command,profile=self.owned,cwd=self.owned,timeout=20)
            self.assertIn('finished',result.output)
            with self.assertRaises(socket.timeout):receiver.recvfrom(128)
        evidence=self.evidence()
        self.assertEqual(evidence['token_capability_count'],0)
        self.assertTrue(evidence['token_is_appcontainer'])
        self.assertFalse(evidence['loopback_exemption'])
        self.assertFalse(evidence['desktop_switched'])
        self.assertTrue(evidence['process_exited'])


if __name__=='__main__':unittest.main()
