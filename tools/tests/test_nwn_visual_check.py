"""Profile boundaries plus opt-in real Windows isolation/lifetime checks."""
from pathlib import Path
import json
import ctypes
import struct
from types import SimpleNamespace
import os
import socket
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch, Mock

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from nwn_visual_check import offline_client, private_desktop, profile, module, firewall
from nwn_visual_check.__main__ import parse_captures, main
from nwn_visual_check.result import ClientRun
from nwn_visual_check.process_job import ProcessJob


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


class ProfileCreationTests(unittest.TestCase):
    def test_missing_or_partial_marker_write_cleans_fresh_directory(self):
        mkdtemp = tempfile.mkdtemp
        write_text = Path.write_text
        for partial in (False, True):
            with self.subTest(partial=partial):
                created = []
                def allocate(*args, **kwargs):
                    result = mkdtemp(*args, **kwargs)
                    created.append(Path(result))
                    return result
                def fail_marker(path, *args, **kwargs):
                    if path.name == profile.MARKER:
                        if partial:
                            write_text(path, '{partial')
                        raise OSError('Marker storage failed')
                    return write_text(path, *args, **kwargs)
                with patch.object(profile.tempfile,'mkdtemp',allocate),patch.object(Path,'write_text',fail_marker):
                    with self.assertRaisesRegex(OSError,'Marker storage failed'):
                        profile.create_profile()
                self.assertEqual(len(created),1)
                self.assertFalse(created[0].exists())


class FirewallStateTests(unittest.TestCase):
    def test_structured_booleans_accept_enabled_profiles_without_display_labels(self):
        output=subprocess.CompletedProcess([],0,stdout='[true,true,true]',stderr='')
        with patch.object(firewall.subprocess,'run',return_value=output) as run:
            firewall.require_firewall()
        command=run.call_args.args[0]
        self.assertIn('ActiveStore',command[-1])
        self.assertNotIn('netsh',command)

    def test_disabled_missing_or_untyped_state_fails_closed(self):
        for value in ('[true,false,true]','[true,true]','["True","True","True"]','[1,1,1]','null','{}'):
            with self.subTest(value=value),patch.object(firewall.subprocess,'run',
                    return_value=subprocess.CompletedProcess([],0,stdout=value,stderr='')):
                with self.assertRaises(RuntimeError):
                    firewall.require_firewall()


class RetainedCleanupTests(unittest.TestCase):
    def test_retry_removes_owned_profile_before_dropping_record(self):
        owned=profile.create_profile()
        (owned/'hak/copied.hak').write_bytes(b'copied input')
        kernel=Mock()
        kernel.WaitForSingleObject.return_value=0
        kernel.CloseHandle.return_value=True
        process=SimpleNamespace(hProcess=111,hThread=112)
        job=Mock();desktop=Mock();pid=0x7ffffffd
        offline_client._retained_processes[pid]=(kernel,process,job,desktop,owned)
        try:
            with patch.object(offline_client,'cleanup_profile',side_effect=OSError('Temporary cleanup refusal')):
                with self.assertRaises(ExceptionGroup):
                    offline_client.retry_retained_processes()
            self.assertTrue(owned.exists())
            self.assertIn(pid,offline_client._retained_processes)
            kernel.CloseHandle.assert_not_called()
            offline_client.retry_retained_processes()
            self.assertFalse(owned.exists())
            self.assertNotIn(pid,offline_client._retained_processes)
            self.assertEqual(kernel.CloseHandle.call_count,2)
            desktop.close.assert_called_once()
        finally:
            offline_client._retained_processes.pop(pid,None)
            if owned.exists():profile.cleanup(owned)

    @unittest.skipUnless(sys.platform=='win32','Native ABI requires Windows')
    def test_resume_failure_is_unsigned_and_reported(self):
        callback=ctypes.WINFUNCTYPE(ctypes.wintypes.DWORD,ctypes.wintypes.HANDLE)(lambda _:0xffffffff)
        callback.restype=ctypes.c_int  # Match the undeclared ctypes default.
        kernel=SimpleNamespace(ResumeThread=callback)
        with self.assertRaises(OSError):
            offline_client._resume_thread(kernel,1)
        self.assertIs(callback.restype,ctypes.wintypes.DWORD)


class FixtureCompilerTests(unittest.TestCase):
    def test_nonzero_clean_include_skip_is_accepted_but_diagnostics_are_not(self):
        for output,accepted in (
                ('0 successful, 1 skipped, 0 errored',True),
                ('fixture.nss(2): ERROR: broken\n0 successful, 1 skipped, 0 errored',False),
                ('0 successful, 1 skipped, 1 errored',False)):
            with self.subTest(output=output),patch.object(module,'find_tool',return_value=Path('compiler.exe')), \
                    patch.object(module.subprocess,'run',return_value=subprocess.CompletedProcess([],623,stdout=output,stderr='')):
                args=(Path('staging/include.nss'),Path('profile'),Path('game/bin/win32/nwmain.exe'))
                if accepted:
                    module.compile_script(*args)
                else:
                    with self.assertRaises(RuntimeError):module.compile_script(*args)


@unittest.skipUnless(sys.platform=='win32' and os.environ.get('SWLOR_NATIVE_ISOLATION_TESTS')=='1','Set SWLOR_NATIVE_ISOLATION_TESTS=1 for actual fixture tools')
class NativeFixtureTests(unittest.TestCase):
    def test_local_include_is_staged_before_alphabetically_earlier_entry_point(self):
        owned=profile.create_profile()
        with tempfile.TemporaryDirectory() as temporary:
            source=Path(temporary)
            (source/'module.ifo').write_bytes(b'fixture resource')
            (source/'aaa_entry.nss').write_text('#include "zzz_include"\nvoid main() { PrintInteger(ReviewValue()); }')
            (source/'zzz_include.nss').write_text('int ReviewValue() { return 7; }')
            try:
                output=module.build_module(source,owned,module.discover_client(),os.environ.get('SWLOR_NWN_TOOLS'))
                self.assertTrue(output.is_file())
                self.assertGreater((owned/'build/aaa_entry.ncs').stat().st_size,8)
                self.assertEqual((owned/'build/zzz_include.nss').read_text(),(source/'zzz_include.nss').read_text())
                self.assertFalse((owned/'build/zzz_include.ncs').exists())
            finally:
                profile.cleanup(owned)

    def test_lod_redirect_is_preserved_with_exact_payload(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary);source=root/'source';source.mkdir()
            payload=b'geometry\n'
            (source/'proxy.lod').write_bytes(payload)
            (source/'geometry.mdl').write_bytes(b'model payload')
            output=root/'parts.hak'
            module.pack_hak(source,output,os.environ.get('SWLOR_NWN_TOOLS'))
            data=output.read_bytes()
            count=struct.unpack_from('<I',data,16)[0]
            keys,resources=struct.unpack_from('<II',data,24)
            found={}
            for index in range(count):
                name,resource,kind=struct.unpack_from('<16sIH',data,keys+index*24)
                offset,size=struct.unpack_from('<II',data,resources+resource*8)
                found[(name.rstrip(b'\0'),kind)]=data[offset:offset+size]
            self.assertEqual(found[(b'proxy',2078)],payload)
            self.assertEqual(found[(b'geometry',2002)],b'model payload')


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
                    with self.assertRaises(ExceptionGroup) as raised:
                        main(['--module',str(fixture),'--output',str(root/'evidence'),'--capture','READY=close'])
                self.assertTrue(any(isinstance(error,RuntimeError) and 'before required evidence' in str(error) for error in raised.exception.exceptions))
                self.assertTrue(any(isinstance(error,OSError) and str(error)=='Evidence storage failed' for error in raised.exception.exceptions))
                self.assertFalse(owned.exists())
                manifest=json.loads((root/'evidence/manifest.json').read_text())
                self.assertFalse(manifest['success'])
                self.assertTrue(manifest['profile_removed'])
                self.assertEqual(manifest['evidence_failure'],'Evidence storage failed')
            finally:
                if owned.exists():profile.cleanup(owned)

    @unittest.skipUnless(sys.platform == 'win32', 'CLI requires Windows')
    def test_retained_process_profile_is_preserved_without_isolation_file(self):
        owned=profile.create_profile()
        sentinel=0x7ffffffe
        offline_client._retained_processes[sentinel]=(None,None,None,None,owned)
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary);fixture=root/'fixture.mod';fixture.write_bytes(b'fixture')
            try:
                with patch('nwn_visual_check.module.discover_client',return_value=Path('C:/nwmain.exe')), \
                        patch('nwn_visual_check.profile.create_profile',return_value=owned), \
                        patch('nwn_visual_check.offline_client.run',side_effect=RuntimeError('Shutdown unconfirmed')):
                    with self.assertRaises(ExceptionGroup):
                        main(['--module',str(fixture),'--output',str(root/'evidence'),'--capture','READY=close'])
                self.assertTrue(owned.exists())
                self.assertFalse((owned/'isolation.json').exists())
                manifest=json.loads((root/'evidence/manifest.json').read_text())
                self.assertFalse(manifest['success'])
                self.assertFalse(manifest['profile_removed'])
                self.assertIn('preserve profile',manifest['cleanup_failure'])
            finally:
                del offline_client._retained_processes[sentinel]
                profile.cleanup(owned)


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

    def test_resume_failure_stops_suspended_child_without_capture_timeout(self):
        with patch.object(offline_client,'_resume_thread',side_effect=OSError('Simulated ResumeThread failure')):
            with self.assertRaisesRegex(OSError,'ResumeThread failure'):
                offline_client.run(self.command('Start-Sleep -Seconds 20'),profile=self.owned,cwd=self.owned,timeout=5)
        evidence=self.evidence()
        self.assertTrue(evidence['process_exited'])
        self.assertNotIn('pid',evidence)
        self.assertFalse(evidence['timed_out'])

    def test_timeout_stops_owned_process(self):
        with self.assertRaises(subprocess.TimeoutExpired):
            offline_client.run(self.command('Start-Sleep -Seconds 20'),profile=self.owned,cwd=self.owned,timeout=1)
        self.assertTrue(self.evidence()['timed_out'])
        self.assertTrue(self.evidence()['process_exited'])

    def test_acl_failure_preserves_original_error_and_still_deletes_container_and_writes_evidence(self):
        run = subprocess.run
        def fail_revocation(command, *args, **kwargs):
            result = run(command,*args,**kwargs)
            if '/remove:g' in command:
                raise subprocess.CalledProcessError(1,command,stderr=b'Simulated revoke failure')
            return result
        def failed_capture(_):
            raise RuntimeError('Capture failed')
        with patch.object(offline_client.subprocess,'run',fail_revocation), \
                patch.object(offline_client,'_delete_appcontainer_profile',wraps=offline_client._delete_appcontainer_profile) as delete:
            with self.assertRaises(ExceptionGroup) as raised:
                offline_client.run(self.command('Start-Sleep -Seconds 20'),profile=self.owned,cwd=self.owned,timeout=5,on_start=failed_capture)
        delete.assert_called_once()
        self.assertTrue(any(isinstance(error,RuntimeError) and str(error)=='Capture failed' for error in raised.exception.exceptions))
        self.assertTrue(any(isinstance(error,subprocess.CalledProcessError) for error in raised.exception.exceptions))
        evidence=self.evidence()
        self.assertTrue(evidence['process_exited'])
        self.assertEqual(evidence['failure'],'Capture failed')
        self.assertEqual([error['step'] for error in evidence['cleanup_errors']],['revoke profile ACL'])
        self.assertIsNone(private_desktop.process_desktop(evidence['pid']))

    def test_container_delete_failure_does_not_skip_isolation_evidence(self):
        delete = offline_client._delete_appcontainer_profile
        def fail_after_deletion(*args):
            delete(*args)  # Do not leave a real registered container behind for this injected failure.
            raise OSError('Simulated container deletion failure')
        with patch.object(offline_client,'_delete_appcontainer_profile',fail_after_deletion):
            with self.assertRaises(ExceptionGroup):
                offline_client.run(self.command("Write-Output 'finished'"),profile=self.owned,cwd=self.owned,timeout=10)
        evidence=self.evidence()
        self.assertTrue(evidence['process_exited'])
        self.assertEqual([error['step'] for error in evidence['cleanup_errors']],['delete AppContainer profile'])
        self.assertIsNone(private_desktop.process_desktop(evidence['pid']))

    def test_partial_acl_grant_failure_is_rolled_back(self):
        run = subprocess.run
        def fail_grant(command, *args, **kwargs):
            result = run(command,*args,**kwargs)
            if '/grant' in command:
                raise subprocess.CalledProcessError(1,command,stderr=b'Simulated partial grant')
            return result
        with patch.object(offline_client.subprocess,'run',fail_grant), \
                patch.object(offline_client,'_delete_appcontainer_profile',wraps=offline_client._delete_appcontainer_profile) as delete:
            with self.assertRaises(subprocess.CalledProcessError):
                offline_client.run(self.command("Write-Output 'not started'"),profile=self.owned,cwd=self.owned,timeout=10)
        delete.assert_called_once()
        evidence=self.evidence()
        self.assertNotIn('pid',evidence)
        permissions=subprocess.run(['icacls',str(self.owned)],check=True,capture_output=True,text=True).stdout
        self.assertNotIn(evidence['appcontainer_sid'],permissions)

    def test_failed_process_termination_uses_job_and_confirms_exit(self):
        with patch.object(offline_client,'_terminate_process',return_value=False):
            result=offline_client.run(self.command('Start-Sleep -Seconds 20'),profile=self.owned,cwd=self.owned,timeout=5,until=lambda:True)
        self.assertTrue(result.stopped)
        evidence=self.evidence()
        self.assertTrue(evidence['job_assigned_at_creation'])
        self.assertTrue(evidence['job_kill_on_close'])
        self.assertTrue(evidence['process_exited'])
        self.assertEqual(evidence['termination_fallback'],'job')

    def test_failed_process_and_job_termination_use_kill_on_close(self):
        with patch.object(offline_client,'_terminate_process',return_value=False), \
                patch.object(ProcessJob,'terminate',side_effect=OSError('Simulated job termination refusal')):
            result=offline_client.run(self.command('Start-Sleep -Seconds 20'),profile=self.owned,cwd=self.owned,timeout=5,until=lambda:True)
        self.assertTrue(result.stopped)
        evidence=self.evidence()
        self.assertTrue(evidence['process_exited'])
        self.assertEqual(evidence['termination_fallback'],'kill-on-close')
        self.assertFalse(offline_client._retained_processes)

    def test_unrelated_inheritable_file_handle_does_not_reach_isolated_child(self):
        import msvcrt
        marker='unrelated-parent-handle-content'
        with tempfile.TemporaryDirectory() as temporary:
            source=Path(temporary)/'parent-only.txt';source.write_text(marker)
            with source.open('rb',buffering=0) as stream:
                os.set_inheritable(stream.fileno(),True)
                handle=msvcrt.get_osfhandle(stream.fileno())
                code=f"$h=[Microsoft.Win32.SafeHandles.SafeFileHandle]::new([IntPtr]::new({handle}),$false);try{{$s=[IO.FileStream]::new($h,[IO.FileAccess]::Read);$b=New-Object byte[] 128;$n=$s.Read($b,0,$b.Length);Write-Output ([Text.Encoding]::UTF8.GetString($b,0,$n));$s.Dispose()}}catch{{Write-Output 'handle denied'}};$h.Dispose();Write-Output 'finished'"
                command=self.command(code)
                control=subprocess.run(command,check=True,capture_output=True,text=True,timeout=20,close_fds=False,creationflags=subprocess.CREATE_NO_WINDOW)
                self.assertIn(marker,control.stdout)
                stream.seek(0)
                result=offline_client.run(command,profile=self.owned,cwd=self.owned,timeout=20)
                self.assertNotIn(marker,result.output)
                self.assertIn('finished',result.output)
        self.assertEqual(self.evidence()['inherited_handles'],'stdin and combined stdout/stderr only')

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
