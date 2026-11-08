"""Recording must stay bounded, retain provenance, and reject incomplete output."""
import json
from pathlib import Path
import sys
import tempfile
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
        self.fake = SimpleNamespace(evidence=self.root, ui=Mock(), ensure_owner=Mock())

    def output(self, fps=60):
        (self.root / 'motion.mp4').write_bytes(b'fixture-video')
        (self.root / 'motion.video.json').write_text(json.dumps({'streams': [{
            'codec_name': 'h264', 'width': 1280, 'height': 720, 'avg_frame_rate': f'{fps}/1',
            'r_frame_rate': f'{fps}/1', 'nb_read_frames': '120', 'duration': '2.000000'}]}))

    def test_invalid_bounds_or_path_do_not_start_a_recorder(self):
        for label, seconds, fps in (('../escape', 2, 60), ('motion', 121, 60),
                                    ('motion', 0, 60), ('motion', 2, 59)):
            with self.subTest(label=label, seconds=seconds, fps=fps), self.assertRaises(stack.LiveStackError):
                stack.record_video(self.fake, label, seconds, fps)
        self.fake.ui.assert_not_called()

    @patch.object(stack, 'require_running')
    def test_incorrect_frame_rate_is_removed_and_not_added_to_the_manifest(self, running):
        self.fake.ui.side_effect = lambda *args, **kwargs: self.output(fps=30)
        with self.assertRaisesRegex(stack.LiveStackError, 'frame rate'):
            stack.record_video(self.fake, 'motion', 2, 60)
        self.assertFalse((self.root / 'motion.mp4').exists())
        self.assertFalse((self.root / 'motion.video.json').exists())
        self.assertFalse((self.root / 'videos.jsonl').exists())

    @patch.object(stack, 'require_running')
    def test_success_checks_the_live_stack_and_records_video_hash_and_stream(self, running):
        self.fake.ui.side_effect = lambda *args, **kwargs: self.output()
        result = stack.record_video(self.fake, 'motion', 2, 60)
        self.assertEqual(2, running.call_count)
        self.assertEqual(2, self.fake.ensure_owner.call_count)
        self.fake.ui.assert_called_once_with('record', 'motion', '2', '60', timeout=62)
        record = json.loads((self.root / 'videos.jsonl').read_text())
        self.assertEqual(stack.sha256(result), record['sha256'])
        self.assertEqual(60, record['fps'])
        self.assertEqual('120', record['stream']['nb_read_frames'])

    @patch.object(stack, 'require_running')
    def test_stack_failure_after_recording_removes_the_video(self, running):
        self.fake.ui.side_effect = lambda *args, **kwargs: self.output()
        running.side_effect = [None, stack.LiveStackError('server stopped')]
        with self.assertRaisesRegex(stack.LiveStackError, 'server stopped'):
            stack.record_video(self.fake, 'motion', 2, 60)
        self.assertFalse((self.root / 'motion.mp4').exists())


if __name__ == '__main__':
    unittest.main()
