"""Recording must stay bounded, retain provenance, and reject incomplete output."""
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import subprocess
import unittest
from types import SimpleNamespace
from unittest.mock import Mock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from nwn_live_client import stack


class LiveVideoTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        self.fake = SimpleNamespace(evidence=self.root, compose=Mock(), ensure_owner=Mock())

    def output(self, fps=60, frames=120, duration=2):
        (self.root / 'motion.mp4').write_bytes(b'fixture-video')
        (self.root / 'motion.video.json').write_text(json.dumps({'streams': [{
            'codec_name': 'h264', 'width': 1280, 'height': 720, 'avg_frame_rate': f'{fps}/1',
            'r_frame_rate': f'{fps}/1', 'nb_read_frames': str(frames), 'duration': str(duration)}]}))
        return subprocess.CompletedProcess([], 0, b'', b'')

    def test_invalid_bounds_or_path_do_not_start_a_recorder(self):
        for label, seconds, fps in (('../escape', 2, 60), ('motion', 121, 60),
                                    ('motion', 0, 60), ('motion', 2, 59)):
            with self.subTest(label=label, seconds=seconds, fps=fps), self.assertRaises(stack.LiveStackError):
                stack.record_video(self.fake, label, seconds, fps)
        self.fake.compose.assert_not_called()

    def test_failed_recording_label_is_not_reused_or_overwritten(self):
        original = self.root / 'motion.ffmpeg.log'
        original.write_text('failed capture evidence')
        with self.assertRaisesRegex(stack.LiveStackError, 'already exists'):
            stack.record_video(self.fake, 'motion', 2, 60)
        self.assertEqual('failed capture evidence', original.read_text())
        self.fake.compose.assert_not_called()

    @patch.object(stack, 'require_running')
    def test_incorrect_frame_rate_is_removed_and_not_added_to_the_manifest(self, running):
        self.fake.compose.side_effect = lambda *args, **kwargs: self.output(fps=30)
        with self.assertRaisesRegex(stack.LiveStackError, 'frame rate'):
            stack.record_video(self.fake, 'motion', 2, 60)
        self.assertFalse((self.root / 'motion.mp4').exists())
        self.assertFalse((self.root / 'motion.video.json').exists())
        self.assertFalse((self.root / 'videos.jsonl').exists())
        self.assertEqual(b'fixture-video', (self.root / 'motion.failed.mp4').read_bytes())

    @patch.object(stack, 'require_running')
    def test_frame_count_matches_actual_stream_duration_within_the_duration_tolerance(self, running):
        self.fake.compose.side_effect = lambda *args, **kwargs: self.output(frames=117, duration=1.95)
        self.assertTrue(stack.record_video(self.fake, 'motion', 2, 60).exists())

    @patch.object(stack, 'require_running')
    def test_success_checks_the_live_stack_and_records_video_hash_and_stream(self, running):
        self.fake.compose.side_effect = lambda *args, **kwargs: self.output()
        result = stack.record_video(self.fake, 'motion', 2, 60)
        self.assertEqual(2, running.call_count)
        self.assertEqual(2, self.fake.ensure_owner.call_count)
        self.fake.compose.assert_called_once_with('exec', '-T', 'client', '/bin/bash', '/verify/motion.recorder.sh',
            'motion', '2', '60', client=True, check=False, capture=True, timeout=62)
        record = json.loads((self.root / 'videos.jsonl').read_text())
        self.assertEqual(stack.sha256(result), record['sha256'])
        self.assertEqual(60, record['fps'])
        self.assertEqual('120', record['stream']['nb_read_frames'])
        self.assertEqual(stack.sha256(self.root / 'motion.recorder.sh'), record['recorder_sha256'])

    @patch.object(stack, 'require_running')
    def test_stack_failure_after_recording_removes_the_video(self, running):
        self.fake.compose.side_effect = lambda *args, **kwargs: self.output()
        running.side_effect = [None, stack.LiveStackError('server stopped')]
        with self.assertRaisesRegex(stack.LiveStackError, 'server stopped'):
            stack.record_video(self.fake, 'motion', 2, 60)
        self.assertFalse((self.root / 'motion.mp4').exists())

    @patch.object(stack, 'require_running')
    def test_unsuccessful_recorder_quarantines_the_partial_video(self, running):
        partial = self.root / '.motion.recording.mp4'

        def fail(*args, **kwargs):
            partial.write_bytes(b'unfinished-video')
            return subprocess.CompletedProcess([], 7, b'', b'encoder failed')

        self.fake.compose.side_effect = fail
        with self.assertRaisesRegex(stack.LiveStackError, 'encoder failed'):
            stack.record_video(self.fake, 'motion', 2, 60)
        self.assertEqual(b'unfinished-video', (self.root / 'motion.failed.partial.mp4').read_bytes())
        self.assertFalse((self.root / 'videos.jsonl').exists())


class RecorderShellFailureTests(unittest.TestCase):
    def check_failure(self, encoder_status, probe_status):
        bash = (str(Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'Git/bin/bash.exe')
                if os.name == 'nt' else shutil.which('bash'))
        if not bash or not Path(bash).is_file():
            self.skipTest('Bash is required for recorder shell-path tests')
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            # Redirect only the container's fixed mount/PID paths into the fixture.
            script = (stack.PACKAGE_DIR / 'record-video.sh').read_text().replace('/verify/', './')
            script = script.replace('/tmp/client.pid', './client.pid')
            (root / 'recorder.sh').write_text(script, newline='\n')
            (root / 'client.pid').write_text('123')
            driver = '''
export DISPLAY=:fixture
xdotool() { if [ "$1" = search ]; then echo 123; fi; }
timeout() { shift 3; "$@"; }
ffmpeg() { printf 'unfinished-video' > "${@: -1}"; return ENCODER_STATUS; }
ffprobe() { printf '{'; return PROBE_STATUS; }
source ./recorder.sh motion 2 60
'''.replace('ENCODER_STATUS', str(encoder_status)).replace('PROBE_STATUS', str(probe_status))
            result = subprocess.run([bash, '-c', driver], cwd=root, capture_output=True, timeout=10)
            self.assertEqual(encoder_status or probe_status, result.returncode, result.stderr.decode())
            self.assertEqual(b'unfinished-video', (root / '.motion.recording.mp4').read_bytes())
            self.assertTrue((root / 'motion.ffmpeg.log').is_file())
            self.assertFalse((root / 'motion.mp4').exists())

    def test_encoder_failure_preserves_partial_recording(self):
        self.check_failure(7, 0)

    def test_probe_failure_preserves_partial_recording(self):
        self.check_failure(0, 8)


if __name__ == '__main__':
    unittest.main()
