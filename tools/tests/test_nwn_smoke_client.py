"""Client-driving helpers of the smoke run: chat-log reading and screenshot analysis."""
from pathlib import Path
import sys
import tempfile
import unittest
from types import SimpleNamespace
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from nwn_live_client import smoke_client, smoke_steps
from nwn_live_client.smoke_client import GameClient

try:
    from PIL import Image, ImageDraw
except ImportError:  # pragma: no cover - Pillow is a prerequisite of the tools tests
    Image = None


def blank(width=1280, height=720, color=(0, 0, 0)):
    return Image.new('RGB', (width, height), color)


def log_line(message, stamp='Tue Oct 27 17:00:11'):
    return f'[CHAT WINDOW TEXT] [{stamp}] {message}'


class ChatLogTests(unittest.TestCase):
    def test_the_prefix_is_stripped_and_other_text_is_kept(self):
        self.assertEqual('Acquired Item: Stim Pack', smoke_client.strip_chat_prefix(log_line('Acquired Item: Stim Pack')))
        self.assertEqual('plain', smoke_client.strip_chat_prefix('plain\r\n'))

    def test_matching_lines_searches_the_message_not_the_timestamp(self):
        lines = [log_line('Mynock attacks Agan Tarn: *hit*'), log_line('Agan Tarn attacks Mynock: *miss*'),
                 log_line('Tue Oct 27 is a date')]
        self.assertEqual(['Agan Tarn attacks Mynock: *miss*'], smoke_client.matching_lines(lines, r'^Agan Tarn attacks'))
        self.assertEqual(['Tue Oct 27 is a date'], smoke_client.matching_lines(lines, r'Oct 27'))

    def client(self, folder, lines):
        profile = Path(folder) / 'profile'
        (profile / 'logs').mkdir(parents=True)
        (profile / 'logs' / 'nwclientLog1.txt').write_text('\n'.join(lines) + '\n', 'utf-8')
        return GameClient(SimpleNamespace(profile=profile))

    def test_marks_split_old_lines_from_new_ones(self):
        with tempfile.TemporaryDirectory() as folder:
            client = self.client(folder, [log_line('one'), log_line('two')])
            mark = client.mark()
            self.assertEqual(2, mark)
            with client.log_path.open('a', encoding='utf-8') as log:
                log.write(log_line('Acquired Item: Basic Rifle') + '\n')
            self.assertEqual([log_line('Acquired Item: Basic Rifle')], client.lines_since(mark))
            self.assertEqual('Acquired Item: Basic Rifle', client.wait_for(r'Acquired Item', mark, timeout=1, poll=0.05))

    def test_wait_for_ignores_matches_before_the_mark_and_times_out(self):
        with tempfile.TemporaryDirectory() as folder:
            client = self.client(folder, [log_line('Acquired Item: Basic Rifle')])
            self.assertIsNone(client.wait_for(r'Acquired Item', client.mark(), timeout=0.2, poll=0.05))

    def test_a_missing_log_reads_as_empty(self):
        client = GameClient(SimpleNamespace(profile=Path(tempfile.gettempdir()) / 'no-such-profile'))
        self.assertEqual([], client.log_lines())
        self.assertEqual(0, client.mark())


@unittest.skipIf(Image is None, 'Pillow is not installed')
class ScreenshotTests(unittest.TestCase):
    def test_a_red_label_above_the_pointer_marks_a_hostile(self):
        image = blank()
        ImageDraw.Draw(image).rectangle((600, 290, 680, 320), fill=(230, 60, 50))
        self.assertTrue(smoke_client.hostile_label_visible(image, 640, 330))
        self.assertFalse(smoke_client.hostile_label_visible(image, 100, 100))

    def test_friendly_and_player_labels_are_not_hostile(self):
        image = blank()
        ImageDraw.Draw(image).rectangle((600, 290, 680, 320), fill=(140, 140, 255))
        self.assertFalse(smoke_client.hostile_label_visible(image, 640, 330))

    def test_a_label_at_the_screen_edge_does_not_fail(self):
        self.assertFalse(smoke_client.hostile_label_visible(blank(), 2, 2))
        self.assertFalse(smoke_client.hostile_label_visible(blank(), 1279, 719))

    def test_green_ui_text_and_bright_title_text_are_counted(self):
        image = blank()
        draw = ImageDraw.Draw(image)
        draw.rectangle((40, 200, 140, 230), fill=(40, 230, 40))
        draw.rectangle((10, 4, 120, 14), fill=(250, 250, 250))
        self.assertTrue(smoke_client.green_text_present(image, (30, 190, 280, 300)))
        self.assertFalse(smoke_client.green_text_present(blank(), (30, 190, 280, 300)))
        self.assertGreaterEqual(smoke_client.bright_pixels(image, (4, 2, 300, 16)), 40)
        self.assertEqual(0, smoke_client.bright_pixels(blank(), (4, 2, 300, 16)))

    def test_the_new_icon_is_found_by_comparing_two_screens(self):
        before, after = blank(), blank()
        ImageDraw.Draw(after).rectangle((130, 300, 161, 363), fill=(220, 220, 220))
        x, y = smoke_client.new_item_center(before, after)
        self.assertTrue(130 <= x <= 162 and 300 <= y <= 364, (x, y))

    def test_noise_elsewhere_does_not_move_the_answer(self):
        before, after = blank(), blank()
        draw = ImageDraw.Draw(after)
        draw.rectangle((20, 380, 51, 411), fill=(220, 220, 220))
        for x, y in ((200, 230), (260, 250), (310, 280), (90, 225)):
            draw.point((x, y), fill=(255, 255, 255))
        x, y = smoke_client.new_item_center(before, after)
        self.assertTrue(20 <= x <= 52 and 380 <= y <= 412, (x, y))

    def test_no_change_means_no_item(self):
        self.assertIsNone(smoke_client.new_item_center(blank(), blank()))

    def test_changes_outside_the_inventory_grid_are_ignored(self):
        before, after = blank(), blank()
        ImageDraw.Draw(after).rectangle((800, 300, 860, 360), fill=(255, 255, 255))
        self.assertIsNone(smoke_client.new_item_center(before, after))


class ClientActionTests(unittest.TestCase):
    def client(self, ui_answer=''):
        calls = []

        def ui(*arguments, timeout=180):
            calls.append(arguments)
            return ui_answer
        return GameClient(SimpleNamespace(ui=ui, profile=Path(tempfile.gettempdir()))), calls

    def test_scan_points_start_at_the_character_and_stay_on_screen(self):
        points = smoke_client.scan_points()
        self.assertEqual((640, 385), points[0])
        self.assertEqual(len(points), len(set(points)))
        width, height = smoke_client.SCREEN
        self.assertTrue(all(0 <= x < width and 0 <= y < height for x, y in points))
        distances = [(x - 640) ** 2 + ((y - 385) * 1.5) ** 2 for x, y in points]
        self.assertEqual(sorted(distances), distances)

    def test_find_hostile_asks_the_container_to_scan_and_reads_the_answer(self):
        client, calls = self.client('640 350\n')
        self.assertEqual((640, 350), client.find_hostile([(640, 385), (640, 350)]))
        self.assertEqual([('hostile-scan', '640', '385', '640', '350')], calls)

    def test_find_hostile_reports_none_when_nothing_is_red(self):
        client, _ = self.client('')
        self.assertIsNone(client.find_hostile([(640, 385)]))

    def test_objects_are_hovered_before_they_are_clicked_and_ui_controls_avoid_tooltips(self):
        client, calls = self.client()
        with patch.object(smoke_client.time, 'sleep'):
            client.click_object(700, 300)
            client.nui_click(100, 200)
        neutral = tuple(str(value) for value in smoke_client.NEUTRAL_POINT)
        self.assertEqual([('move',) + neutral, ('move', '700', '300'), ('click', '700', '300'),
                          ('move',) + neutral, ('click', '100', '200')], calls)

    def test_say_types_a_line_between_two_enters(self):
        client, calls = self.client()
        client.say('/tpwp MN_Bank')
        self.assertEqual([('key', 'Return'), ('type', '/tpwp MN_Bank'), ('key', 'Return')], calls)

    @unittest.skipIf(Image is None, 'Pillow is not installed')
    def test_death_is_recognized_from_the_dialog_title_and_ends_with_a_respawn_click(self):
        client, calls = self.client()
        dead = blank()
        ImageDraw.Draw(dead).rectangle((560, 148, 720, 160), fill=(250, 250, 250))
        with patch.object(client, 'snapshot', return_value=dead), patch.object(smoke_client.time, 'sleep'):
            self.assertTrue(client.is_dead())
            self.assertTrue(client.ensure_alive())
        self.assertIn(('click', '641', '390'), calls)
        calls.clear()
        with patch.object(client, 'snapshot', return_value=blank()):
            self.assertFalse(client.is_dead())
            self.assertFalse(client.ensure_alive())
        self.assertEqual([], calls)

    @unittest.skipIf(Image is None, 'Pillow is not installed')
    def test_closing_a_window_is_verified_and_repeated_until_its_title_is_gone(self):
        client, calls = self.client()
        titled = blank()
        ImageDraw.Draw(titled).rectangle((10, 12, 150, 22), fill=(250, 250, 250))
        screens = iter([titled, titled, blank(), blank()])
        with patch.object(client, 'snapshot', side_effect=lambda: next(screens)),                 patch.object(smoke_client.time, 'sleep'):
            self.assertTrue(client.close_window(743, 17, (4, 8, 220, 26)))
        self.assertEqual(2, len([call for call in calls if call[0] == 'click']))

    @unittest.skipIf(Image is None, 'Pillow is not installed')
    def test_a_window_that_never_closes_is_reported(self):
        client, calls = self.client()
        titled = blank()
        ImageDraw.Draw(titled).rectangle((10, 12, 150, 22), fill=(250, 250, 250))
        with patch.object(client, 'snapshot', return_value=titled), patch.object(smoke_client.time, 'sleep'):
            self.assertFalse(client.close_window(743, 17, (4, 8, 220, 26), attempts=2))
        self.assertEqual(2, len([call for call in calls if call[0] == 'click']))

    @unittest.skipIf(Image is None, 'Pillow is not installed')
    def test_an_icon_that_left_its_cell_is_a_change_and_an_untouched_cell_is_not(self):
        before, after = blank(), blank()
        ImageDraw.Draw(before).rectangle((120, 340, 150, 380), fill=(200, 200, 200))
        self.assertTrue(smoke_client.region_changed(before, after, (136, 350)))
        self.assertFalse(smoke_client.region_changed(before, before.copy(), (136, 350)))
        self.assertFalse(smoke_client.region_changed(before, after, (600, 600)))

    @unittest.skipIf(Image is None, 'Pillow is not installed')
    def test_a_dropped_drag_is_repeated_until_the_icon_has_moved(self):
        client, calls = self.client()
        icon, empty = blank(), blank()
        ImageDraw.Draw(icon).rectangle((120, 340, 150, 380), fill=(200, 200, 200))
        screens = iter([icon, icon, icon, empty])
        with patch.object(client, 'snapshot', side_effect=lambda: next(screens)),                 patch.object(smoke_client.time, 'sleep'):
            self.assertTrue(client.move_item((136, 350), (30, 120)))
        self.assertEqual(2, len([call for call in calls if call[0] == 'drag-slow']))

    @unittest.skipIf(Image is None, 'Pillow is not installed')
    def test_a_drag_that_never_lands_is_reported(self):
        client, calls = self.client()
        icon = blank()
        ImageDraw.Draw(icon).rectangle((120, 340, 150, 380), fill=(200, 200, 200))
        with patch.object(client, 'snapshot', return_value=icon), patch.object(smoke_client.time, 'sleep'):
            self.assertFalse(client.move_item((136, 350), (30, 120), attempts=2))
        self.assertEqual(2, len([call for call in calls if call[0] == 'drag-slow']))

    @unittest.skipIf(Image is None, 'Pillow is not installed')
    def test_a_windows_left_edge_comes_from_its_copper_frame(self):
        image = blank()
        draw = ImageDraw.Draw(image)
        draw.line((527, 0, 527, 530), fill=(182, 133, 79))
        draw.line((1000, 0, 1000, 530), fill=(182, 133, 79))
        self.assertEqual(526, smoke_client.window_left(image))
        self.assertIsNone(smoke_client.window_left(blank()))


class StepHelperTests(unittest.TestCase):
    class FakeClient:
        def __init__(self, invalid=False, transition=True):
            self.calls, self.invalid, self.transition = [], invalid, transition
            self.logged = 0

        def click(self, x, y):
            self.calls.append(('click', x, y))

        def mark(self):
            return 0

        def loading_screens(self):
            return 5

        def say(self, text):
            self.calls.append(('say', text))

        def ensure_alive(self):
            return False

        def wait_for(self, pattern, mark, timeout=20, poll=2):
            return 'Invalid waypoint tag. Did you enter the right tag?' if self.invalid and 'Invalid' in pattern else None

        def wait_area_change(self, started_after, timeout=25):
            return self.transition

        def wait_loaded(self, timeout=120, settle=4):
            self.calls.append(('loaded',))
            return True

    def test_teleport_cancels_actions_first_then_waits_for_the_new_area(self):
        client = self.FakeClient()
        with patch.object(smoke_steps.time, 'sleep'):
            self.assertTrue(smoke_steps.teleport(client, 'MN_Bank'))
        self.assertEqual([('click', 900, 480), ('say', '/tpwp MN_Bank'), ('loaded',)], client.calls)

    def test_teleport_inside_one_area_has_no_loading_screen_to_wait_for(self):
        client = self.FakeClient(transition=False)
        with patch.object(smoke_steps.time, 'sleep'):
            self.assertTrue(smoke_steps.teleport(client, 'CZ220_MYNOCKS'))
        self.assertNotIn(('loaded',), client.calls)

    def test_an_unknown_waypoint_tag_is_reported(self):
        client = self.FakeClient(invalid=True)
        with patch.object(smoke_steps.time, 'sleep'):
            self.assertFalse(smoke_steps.teleport(client, 'NO_SUCH_TAG'))

    def test_both_mynock_variants_of_the_spawn_table_match(self):
        import re
        pattern = re.compile(rf'{smoke_steps.MYNOCK} (attacks|uses|readies)')
        self.assertTrue(pattern.search('Mynock attacks Agan Tarn: *hit*'))
        self.assertTrue(pattern.search('Coolant-Scarred Mynock readies Sonic Shriek.'))
        self.assertFalse(pattern.search('Malfunctioning Patrol Droid attacks Agan Tarn'))

    def test_the_characters_name_comes_from_its_record_once(self):
        run = SimpleNamespace()
        with patch.object(smoke_steps, 'player_record', return_value={'Name': 'Test Pilot'}) as record:
            self.assertEqual('Test Pilot', smoke_steps.character_name(run))
            self.assertEqual('Test Pilot', smoke_steps.character_name(run))
        self.assertEqual(1, record.call_count)
        with patch.object(smoke_steps, 'player_record', return_value={}):
            with self.assertRaises(smoke_steps.LiveStackError):
                smoke_steps.character_name(SimpleNamespace())

    def test_a_timed_out_area_load_fails_the_teleport(self):
        client = self.FakeClient()
        client.wait_loaded = lambda timeout=120, settle=4: False
        with patch.object(smoke_steps.time, 'sleep'):
            self.assertFalse(smoke_steps.teleport(client, 'MN_Bank'))

    def test_a_dropped_click_is_repeated_until_its_effect_shows_in_the_chat_log(self):
        answers = iter([None, None, 'Acquired Item: Basic Pistol'])
        clicks = []
        client = SimpleNamespace(mark=lambda: 0, nui_click=lambda x, y: clicks.append((x, y)),
                                 wait_for=lambda pattern, mark, timeout=20, poll=2: next(answers))
        self.assertEqual('Acquired Item: Basic Pistol',
                         smoke_steps.click_expect(client, 10, 20, 'Acquired Item', attempts=3))
        self.assertEqual([(10, 20)] * 3, clicks)

    def test_a_click_that_never_takes_effect_gives_up_after_its_attempts(self):
        clicks = []
        client = SimpleNamespace(mark=lambda: 0, nui_click=lambda x, y: clicks.append((x, y)),
                                 wait_for=lambda pattern, mark, timeout=20, poll=2: None)
        self.assertIsNone(smoke_steps.click_expect(client, 1, 2, 'x', attempts=2))
        self.assertEqual(2, len(clicks))

    def test_the_attack_log_names_the_weapon_and_attack_type(self):
        lines = ['x', '2026-10-27 17:20:28 +00:00 [INF] DAMAGE: Attacker: Agan, PC?: 1, Defender Malfunctioning Patrol '
                 'Droid, object type 5, Attack type: 2, weapon Basic Rifle',
                 '2026-10-27 17:20:29 +00:00 [INF] DAMAGE: Attacker: Mynock, PC?: 0, Defender Agan, object type 5, '
                 'Attack type: 1, weapon Mynock Claw']
        client = SimpleNamespace(attack_lines=lambda: lines)
        self.assertIn('Attack type: 2, weapon Basic Rifle', smoke_steps.attack_log_has(client, 0, 2, 'Basic Rifle'))
        self.assertIsNone(smoke_steps.attack_log_has(client, 0, 1, 'Basic Rifle'))
        self.assertIsNone(smoke_steps.attack_log_has(client, 0, 1, 'Mynock Claw'), 'only the player counts')
        self.assertIsNone(smoke_steps.attack_log_has(client, 2, 2, 'Basic Rifle'), 'lines before the mark are ignored')

    def test_every_screen_point_lies_on_the_screen(self):
        width, height = smoke_client.SCREEN
        points = (smoke_client.HOSTILE_SCAN + [smoke_client.RESPAWN_BUTTON] + smoke_steps.BANK_TERMINAL_POINTS
                  + smoke_steps.COOKPOT_POINTS + smoke_steps.SELAN_POINTS
                  + [smoke_client.NEUTRAL_POINT, smoke_client.SELF_POINT, smoke_client.MAIN_HAND_SLOT,
                     smoke_client.EMPTY_GRID_CELL, smoke_steps.PERKS_FIRST_ROW, smoke_steps.PERKS_BUY,
                     smoke_steps.CRAFT_START,
                     smoke_steps.CRAFT_BASIC_SYNTHESIS, smoke_steps.CONVERSATION_FIRST_REPLY,
                     (smoke_client.HOTBAR_X[3], smoke_client.HOTBAR_Y)]
                  + list(smoke_steps.CRAFT_WINDOW_CORNER))
        for x, y in points:
            self.assertTrue(0 <= x < width and 0 <= y < height, (x, y))


if __name__ == '__main__':
    unittest.main()
