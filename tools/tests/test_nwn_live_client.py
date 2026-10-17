"""Pure helpers of the live-server client stack; the Docker run itself is verified in-engine."""
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from nwn_live_client import stack
from nwn_live_client.stack import LiveStackError


def gff(fields):
    """Build a minimal GFF V3.2 .bic with top-level CExoString fields."""
    labels = b''.join(label.encode('ascii').ljust(16, b'\0') for label, _ in fields)
    field_data = b''
    entries = b''
    for index, (_, value) in enumerate(fields):
        entries += struct.pack('<3I', 10, index, len(field_data))
        encoded = value.encode('utf-8')
        field_data += struct.pack('<I', len(encoded)) + encoded
    indices = b''.join(struct.pack('<I', index) for index in range(len(fields)))
    struct_offset = 56
    field_offset = struct_offset + 12
    label_offset = field_offset + len(entries)
    field_data_offset = label_offset + len(labels)
    indices_offset = field_data_offset + len(field_data)
    # One field: the struct stores its index (0). Several: a byte offset into the indices (0).
    struct_data = 0
    header = b'BIC V3.2' + struct.pack(
        '<12I', struct_offset, 1, field_offset, len(fields), label_offset, len(fields),
        field_data_offset, len(field_data), indices_offset, len(indices), indices_offset + len(indices), 0)
    top = struct.pack('<3I', 0xFFFFFFFF, struct_data, len(fields))
    return header + top + entries + labels + field_data + indices


class LiveClientHelperTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.root = Path(self.directory.name)

    def tearDown(self):
        self.directory.cleanup()

    def write(self, name, data):
        path = self.root / name
        path.write_bytes(data)
        return path

    def test_reads_character_uuid(self):
        uuid = 'a27c0f71-93de-4dcb-aa02-0047c02e6b2d'
        path = self.write('test.bic', gff([('Tag', ''), ('UUID', uuid)]))
        self.assertEqual(stack.character_uuid(path), uuid)

    def test_reads_single_field_struct(self):
        path = self.write('one.bic', gff([('UUID', 'a27c0f71-93de-4dcb-aa02-0047c02e6b2d')]))
        self.assertEqual(stack.read_gff_string(path, 'UUID'), 'a27c0f71-93de-4dcb-aa02-0047c02e6b2d')

    def test_character_without_uuid_fails(self):
        path = self.write('none.bic', gff([('Tag', 'x'), ('Deity', '')]))
        with self.assertRaises(LiveStackError):
            stack.character_uuid(path)

    def test_rejects_non_gff(self):
        with self.assertRaises(LiveStackError):
            stack.read_gff_string(self.write('bad.bic', b'not a gff' * 10), 'UUID')

    def test_project_names_are_prefixed_and_safe(self):
        self.assertEqual(stack.project_name('Ship-Check'), 'swlor-live-ship-check')
        self.assertEqual(stack.project_name('swlor-live-ship'), 'swlor-live-ship')
        self.assertRegex(stack.project_name(None), r'^swlor-live-[0-9a-f]{6}$')
        for bad in ('../x', 'a b', 'x;y'):
            with self.assertRaises(LiveStackError):
                stack.project_name(bad)

    def test_public_cdkey_must_be_public_form(self):
        self.assertEqual(stack.validate_public_cdkey(' abcd1234 '), 'ABCD1234')
        for bad in ('', 'ABCDE-FGHIJ-KLMNO', 'ABC123'):
            with self.assertRaises(LiveStackError):
                stack.validate_public_cdkey(bad)

    def test_env_files_are_literal_and_refuse_injection(self):
        text = stack.render_env_file({'A_PATH': 'C:/Program Files (x86)/x', 'EMPTY': ''})
        self.assertEqual(text, "A_PATH='C:/Program Files (x86)/x'\nEMPTY=''\n")
        for values in ({'lower': 'x'}, {'OK': "it's"}, {'OK': 'a\nB=c'}):
            with self.assertRaises(LiveStackError):
                stack.render_env_file(values)
        self.assertEqual(stack.parse_server_env(['SWLOR_TEST_DRIVER=1', 'X=a=b']),
                         {'SWLOR_TEST_DRIVER': '1', 'X': 'a=b'})
        with self.assertRaises(LiveStackError):
            stack.parse_server_env(['NO_VALUE'])

    def test_client_settings_are_silent_and_fixed_size(self):
        text = stack.client_settings('SwlorLive')
        for channel in ('effects', 'music', 'streams', 'voices'):
            self.assertIn(f'[sound.{channel}]\nvolume = 0.0', text)
        for section in ('graphics.intro.splash', 'graphics.movies', 'graphics.movies.intro'):
            self.assertIn(f'[{section}]\nenabled = false', text)
        self.assertIn('width = 1280\nheight = 720', text)
        self.assertIn('[ui]\nscale = 1.0', text)
        with self.assertRaises(LiveStackError):
            stack.client_settings('bad"name')

    def test_profile_aliases_point_at_mounted_haks(self):
        self.assertIn('HAK=/hak\n', stack.CLIENT_ALIASES)
        self.assertIn('TLK=/tlk\n', stack.CLIENT_ALIASES)

    def test_join_and_leave_patterns_match_server_log(self):
        log = self.write('nwserverLog1.txt', (
            'Tester (ABCD1234) Joined as Player 1\n'
            'Tester Left as a Player (0 players left)\n'
            'Tester (ABCD1234) Joined as Game Master 1\n'
            'Tester Left as a Game Master (0 players left)\n'
            'Tester (ABCD1234) Joined as Player 1\n').encode())
        self.assertEqual(stack.count_matches(log, stack.JOIN_PATTERN), 3)
        self.assertEqual(stack.count_matches(log, stack.LEFT_PATTERN), 2)

    def test_entry_and_loading_tip_patterns_match_swlor_logs(self):
        audit = self.write('Connection_20261017.log', (
            '2026-10-17 12:56:49.989 +00:00 [INF] Tester [PlayerId=x] - Tester - ABCD1234 - 172.26.0.4: '
            'Connected to server\n'
            '2026-10-17 12:58:06.410 +00:00 [INF] Tester [PlayerId=x] - Tester - ABCD1234 - 172.26.0.4: '
            'Disconnected from server\r\n').encode())
        self.assertEqual(stack.count_matches(audit, stack.ENTRY_PATTERN), 1)
        chat = self.write('nwclientLog1.txt', (
            '[CHAT WINDOW TEXT] [Sat Oct 17 12:56:51] Acquired 200 credits\n'
            '[CHAT WINDOW TEXT] [Sat Oct 17 12:56:52] Loading Screen: Each fishing location differs.\n'
            '[CHAT WINDOW TEXT] [Sat Oct 17 12:57:04] Tester: Loading Screen: typed by a player\n').encode())
        self.assertEqual(stack.count_matches(chat, stack.LOADING_TIP_PATTERN), 1)

    def test_project_reservation_reports_the_holding_run(self):
        def completed(code, out=b''):
            return subprocess.CompletedProcess([], code, out, b'Conflict. The container name is already in use')

        with patch.object(stack, 'run', side_effect=[completed(0)]) as docker:
            stack.reserve_project('swlor-live-lock', self.root / 'first', 'image:tag')
        self.assertEqual(docker.call_args.args[0][:4], ['docker', 'create', '--name', 'swlor-live-lock-reservation'])
        with patch.object(stack, 'run', side_effect=[completed(1), completed(0, b'0123abcd C:\\runs\\first\n')]):
            with self.assertRaisesRegex(LiveStackError, r'reserved by run C:\\runs\\first'):
                stack.reserve_project('swlor-live-lock', self.root / 'second', 'image:tag')

    def test_down_refuses_a_project_reserved_by_another_run(self):
        run_dir = self.root / 'stale-run'
        run_dir.mkdir()
        (run_dir / 'state.json').write_text('{"project": "swlor-live-reused"}', 'utf-8')
        stale = stack.Stack(run_dir)
        with patch.object(stack, 'reservation_holder', return_value=str(self.root / 'new-run')), \
                patch.object(stack, 'boot_players') as boot:
            with self.assertRaisesRegex(LiveStackError, 'now reserved by run'):
                stack.destroy(stale)
        boot.assert_not_called()

    def test_release_removes_only_this_runs_reservation_by_id(self):
        def completed(code, out=b''):
            return subprocess.CompletedProcess([], code, out, b'')

        other = completed(0, b'0123abcd C:\\runs\\new-run\n')
        with patch.object(stack, 'run', side_effect=[other]) as docker:
            self.assertTrue(stack.release_project('swlor-live-lock', Path('C:/runs/stale-run')))
        self.assertEqual(docker.call_count, 1)
        mine = completed(0, b'0123abcd ' + str(self.root).encode() + b'\n')
        with patch.object(stack, 'run', side_effect=[mine, completed(0), completed(1)]) as docker:
            self.assertTrue(stack.release_project('swlor-live-lock', self.root))
        self.assertEqual(docker.call_args_list[1].args[0], ['docker', 'rm', '0123abcd'])

    def test_live_commands_need_this_runs_reservation(self):
        run_dir = self.root / 'run'
        run_dir.mkdir()
        (run_dir / 'state.json').write_text('{"project": "swlor-live-own"}', 'utf-8')
        own = stack.Stack(run_dir)
        with patch.object(stack, 'reservation_holder', return_value=str(run_dir)):
            own.ensure_owner()
        with patch.object(stack, 'reservation_holder', return_value=None):
            own.ensure_owner(require_reservation=False)
            with self.assertRaisesRegex(LiveStackError, 'has no reservation'):
                own.ensure_owner()

    def test_build_digest_changes_with_content_and_names(self):
        build = self.root / 'build'
        build.mkdir()
        (build / 'a.dll').write_bytes(b'one')
        first = stack.build_digest(build)
        (build / 'a.dll').write_bytes(b'two')
        self.assertNotEqual(first, stack.build_digest(build))
        (build / 'a.dll').rename(build / 'b.dll')
        self.assertNotEqual(first, stack.build_digest(build))

    def test_hak_manifest_detects_a_replaced_archive(self):
        haks = self.root / 'hak'
        haks.mkdir()
        (haks / 'a.hak').write_bytes(b'one')
        before = stack.hak_manifest(haks)
        (haks / 'a.hak').write_bytes(b'longer')
        self.assertNotEqual(before, stack.hak_manifest(haks))

    def test_run_directory_must_be_outside_repository(self):
        self.assertTrue(stack.is_within(stack.REPO_ROOT / 'tools' / 'x', stack.REPO_ROOT))
        self.assertFalse(stack.is_within(self.root, stack.REPO_ROOT))

    def test_compose_and_settings_keep_development_only_blocks(self):
        compose = stack.COMPOSE_FILE.read_text('utf-8')
        for host in ('nwmaster2.beamdog.net', 'nwn.beamdog.net', 'api.nwn.beamdog.net'):
            self.assertIn(f'"{host}:127.0.0.1"', compose)
        self.assertIn('NWN_PUBLICSERVER: "0"', compose)
        self.assertIn('NWN_NWSYNCURL: ""', compose)
        self.assertNotIn('ports:', compose)
        self.assertIn('user: ${SWLOR_LIVE_CLIENT_USER}', compose)
        self.assertRegex(stack.host_user(), r'^\d+:\d+$')
        self.assertRegex(stack.server_image(), r'^zunath/nwn-dotnet:')


if __name__ == '__main__':
    unittest.main()
