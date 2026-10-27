"""Driving the game client of a live stack for the smoke run.

Everything here goes through Stack.ui (xdotool inside the client container), the client's own
chat log and screenshots; nothing touches the host desktop. The screen is the profile's fixed
1280x720 at UI scale 1.0, so the coordinates below are stable for one client build.

Pillow is imported only by the functions that read screenshots, so the rest of the tool (and
its tests) work without it.
"""
from __future__ import annotations

import re
import time
from pathlib import Path

from . import stack as live
from .stack import LiveStackError, Stack

SCREEN = (1280, 720)
# An empty corner of the game view: the pointer rests here between clicks so a control's
# tooltip is never up when the next click lands (a click on a control whose tooltip is showing
# is dropped).
NEUTRAL_POINT = (1200, 560)
# The player character stands at the screen centre; clicking it targets self.
SELF_POINT = (642, 378)
# Inventory panel: the item grid, the main-hand slot and an empty top-row cell to drop items on.
INVENTORY_GRID = (0, 218, 335, 418)
MAIN_HAND_SLOT = (30, 120)
EMPTY_GRID_CELL = (100, 240)
# Hotbar buttons F1 to F12 run along the bottom edge; a bought active ability takes the next free one.
HOTBAR_Y = 703
HOTBAR_X = {1: 275, 2: 340, 3: 406, 4: 473}
# The "You Are Dead" dialog: its title bar and the Respawn button.
DEATH_TITLE = (500, 143, 780, 166)
RESPAWN_BUTTON = (641, 390)


def scan_points(center: tuple[int, int] = (640, 385)) -> list[tuple[int, int]]:
    """Screen points around the character, nearest first: where a creature fighting it can stand.

    A mynock is small, so the points next to the character are 20 pixels apart; farther out they
    are 40 and 35 apart. The scan stops at the first hostile, which is usually among the first few.
    """
    def grid(reach_x: int, reach_y: int, step_x: int, step_y: int) -> list[tuple[int, int]]:
        return [(center[0] + dx, center[1] + dy)
                for dx in range(-reach_x, reach_x + 1, step_x) for dy in range(-reach_y, reach_y + 1, step_y)]
    points = list(dict.fromkeys(grid(80, 60, 20, 20) + grid(160, 105, 40, 35)))
    points = [(x, y) for x, y in points if 0 <= x < SCREEN[0] and 0 <= y < SCREEN[1]]
    return sorted(points, key=lambda point: ((point[0] - center[0]) ** 2 + ((point[1] - center[1]) * 1.5) ** 2,
                                             point))


# A hostile that is fighting the character, nearest first.
HOSTILE_SCAN = scan_points()

CHAT_PREFIX = re.compile(r'^\[CHAT WINDOW TEXT\] \[[^\]]*\] ?')


def strip_chat_prefix(line: str) -> str:
    """The message of one chat-log line, without its "[CHAT WINDOW TEXT] [timestamp]" prefix."""
    return CHAT_PREFIX.sub('', line.rstrip('\r\n'))


def matching_lines(lines: list[str], pattern: str) -> list[str]:
    """The messages among lines that match a regular expression (searched, case-sensitive)."""
    expression = re.compile(pattern)
    return [message for message in map(strip_chat_prefix, lines) if expression.search(message)]


# ---------------------------------------------------------------- screenshot analysis

def pixels(image, box: tuple[int, int, int, int] | None = None) -> list[tuple[int, int, int]]:
    """The RGB pixels of an image (or of a box of it), whichever Pillow API this version has."""
    region = image.convert('RGB')
    if box is not None:
        region = region.crop(box)
    getter = getattr(region, 'get_flattened_data', None) or region.getdata
    return list(getter())


def hostile_label_visible(image, x: int, y: int) -> bool:
    """True when a hostile creature's red name label sits above a hovered point.

    Hovering a creature shows its name and condition just above the pointer; hostile names
    are red-orange, friendly ones blue or violet, and the player's lilac.
    """
    box = (max(0, x - 70), max(0, y - 45), min(image.width, x + 70), min(image.height, y + 5))
    red = sum(1 for r, g, b in pixels(image, box) if r > 190 and g < 120 and b < 120)
    return red >= 40


def green_text_present(image, box: tuple[int, int, int, int], minimum: int = 100) -> bool:
    """True when enough bright green UI text lies in a box (the crafting recipe list is green)."""
    count = sum(1 for r, g, b in pixels(image, box) if g > 200 and r < 90 and b < 90)
    return count >= minimum


def bright_pixels(image, box: tuple[int, int, int, int]) -> int:
    """How many near-white pixels lie in a box (UI title text on a dark bar)."""
    return sum(1 for r, g, b in pixels(image, box) if min(r, g, b) > 200)


def region_changed(before, after, center: tuple[int, int], half: int = 14, threshold: int = 40,
                   minimum: int = 25) -> bool:
    """True when enough pixels around a point differ between two screenshots (an icon came or went)."""
    from PIL import ImageChops
    box = (max(0, center[0] - half), max(0, center[1] - half), center[0] + half, center[1] + half)
    difference = ImageChops.difference(before.convert('L').crop(box), after.convert('L').crop(box))
    return sum(1 for value in pixels_gray(difference) if value > threshold) >= minimum


def pixels_gray(image) -> list[int]:
    """The values of a greyscale image, whichever Pillow API this version has."""
    getter = getattr(image, 'get_flattened_data', None) or image.getdata
    return list(getter())


def window_left(image, y: int = 300) -> int | None:
    """The left edge of the leftmost UI window, from its copper frame line at one screen row.

    NUI windows are drawn with a one-pixel copper frame on a black background; windows can open
    wherever the server last put them, so controls are located relative to this edge.
    """
    width = image.width
    row = pixels(image, (0, y, width, y + 1))
    for x, (r, g, b) in enumerate(row):
        if r > 150 and 80 < g < 150 and b < 90 and r > g * 1.3:
            return max(0, x - 1)
    return None


def new_item_center(before, after, region: tuple[int, int, int, int] = INVENTORY_GRID, cell: int = 8,
                    threshold: int = 20, cell_minimum: int = 6) -> tuple[int, int] | None:
    """The centre of the biggest area of a region that changed between two screenshots.

    A freshly created item appears as a new icon in the inventory grid; the changed area is
    found in 8x8 cells and the largest connected group of changed cells wins, so a few noisy
    pixels from the world behind the panel never move the answer.
    """
    from PIL import ImageChops
    difference = ImageChops.difference(before.convert('L'), after.convert('L')).crop(region)
    mask = difference.point(lambda value: 255 if value > threshold else 0)
    columns, rows = mask.width // cell, mask.height // cell
    changed = set()
    for row in range(rows):
        for column in range(columns):
            tile = mask.crop((column * cell, row * cell, (column + 1) * cell, (row + 1) * cell))
            if tile.histogram()[255] >= cell_minimum:
                changed.add((column, row))
    best: list[tuple[int, int]] = []
    seen: set[tuple[int, int]] = set()
    for start in sorted(changed):
        if start in seen:
            continue
        group, queue = [], [start]
        seen.add(start)
        while queue:
            column, row = queue.pop()
            group.append((column, row))
            for step_column in (-1, 0, 1):
                for step_row in (-1, 0, 1):
                    neighbour = (column + step_column, row + step_row)
                    if neighbour in changed and neighbour not in seen:
                        seen.add(neighbour)
                        queue.append(neighbour)
        if len(group) > len(best):
            best = group
    if len(best) < 2:
        return None
    left = min(column for column, _ in best) * cell
    right = (max(column for column, _ in best) + 1) * cell
    top = min(row for _, row in best) * cell
    bottom = (max(row for _, row in best) + 1) * cell
    return region[0] + (left + right) // 2, region[1] + (top + bottom) // 2


class GameClient:
    """Input, chat log and screenshots of the client container of one run."""

    def __init__(self, stack: Stack):
        self.stack = stack
        self._temporary = 0

    # -- input ----------------------------------------------------------

    def move(self, x: int, y: int) -> None:
        self.stack.ui('move', str(x), str(y))

    def click(self, x: int, y: int) -> None:
        self.stack.ui('click', str(x), str(y))

    def rightclick(self, x: int, y: int) -> None:
        self.stack.ui('rightclick', str(x), str(y))

    def key(self, keysym: str) -> None:
        self.stack.ui('key', keysym)

    def click_object(self, x: int, y: int) -> None:
        """Click a creature, placeable or door: hover first so the client has picked it as the target."""
        self.move(*NEUTRAL_POINT)
        self.move(x, y)
        time.sleep(1)
        self.click(x, y)

    def nui_click(self, x: int, y: int) -> None:
        """Click a NUI control or any other widget that has a tooltip."""
        self.move(*NEUTRAL_POINT)
        self.click(x, y)

    def drag(self, x1: int, y1: int, x2: int, y2: int) -> None:
        """Press at one point and release at another with intermediate moves (windows, equipment)."""
        self.stack.ui('drag-slow', str(x1), str(y1), str(x2), str(y2))

    def say(self, text: str) -> None:
        """Type one chat line or slash command, as a player would: Enter, the text, Enter."""
        self.key('Return')
        self.stack.ui('type', text)
        self.key('Return')

    # -- chat log -------------------------------------------------------

    @property
    def log_path(self) -> Path:
        return self.stack.profile / 'logs' / 'nwclientLog1.txt'

    def log_lines(self) -> list[str]:
        if not self.log_path.is_file():
            return []
        return self.log_path.read_text('utf-8', 'replace').splitlines()

    def mark(self) -> int:
        """A position in the chat log: later reads ask for what came after it."""
        return len(self.log_lines())

    def lines_since(self, mark: int) -> list[str]:
        return self.log_lines()[mark:]

    def wait_for(self, pattern: str, mark: int, timeout: float = 20, poll: float = 2) -> str | None:
        """The first chat-log message after mark that matches pattern, or None after timeout seconds."""
        deadline = time.monotonic() + timeout
        while True:
            found = matching_lines(self.lines_since(mark), pattern)
            if found:
                return found[0]
            if time.monotonic() >= deadline:
                return None
            time.sleep(poll)

    # -- server logs ----------------------------------------------------

    def attack_lines(self) -> list[str]:
        """The server's combat-resolution log lines (Attack folder), oldest first."""
        folder = self.stack.home / 'app_logs' / 'Attack'
        lines: list[str] = []
        for path in sorted(folder.glob('*.log')):
            lines += path.read_text('utf-8', 'replace').splitlines()
        return lines

    def server_chat_lines(self) -> list[str]:
        """The nwserver log's <Conv> lines: area name, speaker and text of every chat line."""
        path = self.stack.server_log
        if not path.is_file():
            return []
        return [line for line in path.read_text('utf-8', 'replace').splitlines() if line.startswith('<Conv>')]

    # -- screen ---------------------------------------------------------

    def snapshot(self):
        """The current screen as a Pillow image; the temporary capture file is removed."""
        from PIL import Image
        self._temporary += 1
        label = f'tmp-{int(time.time())}-{self._temporary}'
        self.stack.ui('capture', label)
        path = self.stack.evidence / f'{label}.png'
        if not path.is_file():
            raise LiveStackError(f'The capture {path} was not written.')
        with Image.open(path) as opened:
            image = opened.convert('RGB')
        path.unlink(missing_ok=True)
        return image

    def hover_finds_hostile(self, x: int, y: int) -> bool:
        """Hover a point and report whether a hostile creature's label appears above it."""
        self.move(*NEUTRAL_POINT)
        self.move(x, y)
        time.sleep(1)
        return hostile_label_visible(self.snapshot(), x, y)

    def inventory_open(self) -> bool:
        """True when the inventory panel's title bar text is on screen."""
        return bright_pixels(self.snapshot(), (4, 2, 300, 16)) >= 40

    def ensure_inventory_open(self) -> bool:
        if self.inventory_open():
            return True
        self.key('i')
        time.sleep(2)
        return self.inventory_open()

    def ensure_inventory_closed(self) -> bool:
        if not self.inventory_open():
            return True
        self.key('i')
        time.sleep(2)
        return not self.inventory_open()

    def close_window(self, x: int, y: int, title_box: tuple[int, int, int, int], attempts: int = 3) -> bool:
        """Click a window's close button until its title text is gone from the screen.

        A click can be dropped without a trace, and an open window swallows later clicks meant for
        the game view, so closing is verified rather than assumed. The title area must be clear of
        other windows (close the inventory panel first).
        """
        for _ in range(attempts):
            if bright_pixels(self.snapshot(), title_box) < 40:
                return True
            self.nui_click(x, y)
            time.sleep(2)
        return bright_pixels(self.snapshot(), title_box) < 40

    def move_item(self, source: tuple[int, int], target: tuple[int, int], attempts: int = 3) -> bool:
        """Drag an inventory or equipment icon to another place until the source shows the change.

        The server does not tell a dropped drag from a done one, so the screen is compared: an icon
        that moved leaves its source cell different. Use it only in a quiet scene, where nothing else
        changes behind the inventory panel.
        """
        for _ in range(attempts):
            before = self.snapshot()
            self.drag(*source, *target)
            time.sleep(3)
            if region_changed(before, self.snapshot(), source):
                return True
        return False

    def locate_new_item(self, create) -> tuple[int, int] | None:
        """Run create() (which adds an item) and return where the new icon appeared in the grid."""
        before = self.snapshot()
        create()
        time.sleep(2)
        return new_item_center(before, self.snapshot())

    def is_dead(self) -> bool:
        """True when the "You Are Dead" dialog is on screen."""
        return bright_pixels(self.snapshot(), DEATH_TITLE) >= 60

    def ensure_alive(self) -> bool:
        """Respawn when the character is dead; True when the character was dead and is back."""
        if not self.is_dead():
            return False
        self.move(*NEUTRAL_POINT)
        self.click(*RESPAWN_BUTTON)
        time.sleep(15)
        return True

    def find_hostile(self, points=HOSTILE_SCAN) -> tuple[int, int] | None:
        """The first point (nearest the character first) where hovering shows a hostile's label.

        The scan runs inside the client container (ui.sh hostile-scan), one call for the lot.
        """
        arguments = [str(value) for point in points for value in point]
        answer = self.stack.ui('hostile-scan', *arguments, timeout=240).split()
        if len(answer) < 2:
            return None
        return int(answer[-2]), int(answer[-1])

    def loading_screens(self) -> int:
        return live.loading_tips(self.stack)

    def wait_area_change(self, started_after: int, timeout: float = 25) -> bool:
        """True when SWLOR's loading-screen tip shows a new area transition began after started_after."""
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            if self.loading_screens() > started_after:
                return True
            time.sleep(1)
        return False

    def wait_loaded(self, timeout: float = 120, settle: float = 4) -> bool:
        """Wait until the loading-screen frame is gone, then a short settle."""
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            if not live.showing_loading_screen(self.stack):
                time.sleep(settle)
                return True
            time.sleep(2)
        return False
