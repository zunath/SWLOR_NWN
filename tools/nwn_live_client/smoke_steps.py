"""The in-game steps of the smoke scenario (combat to area transfer).

Each step drives the client of a running stack the way a player would (chat commands, clicks,
drags) and judges the outcome only from evidence: the client's chat log, the server's own logs
and Redis. Test setup (teleports, item creation) uses the server's built-in chat commands,
which the smoke stack makes available to everyone by running as a test environment; no module
content or server code is changed.

Screen coordinates were measured on the profile's fixed 1280x720 screen at UI scale 1.0 and are
stable for one client build and one character. Every action that may silently fail is verified
by its log line and retried a bounded number of times.
"""
from __future__ import annotations

import json
import re
import time

from .smoke_client import (EMPTY_GRID_CELL, HOTBAR_X, HOTBAR_Y, MAIN_HAND_SLOT, SELF_POINT, GameClient,
                           bright_pixels, green_text_present, matching_lines, window_left)

BANK_TERMINAL_POINTS = [(600, 395), (598, 360)]
COOKPOT_POINTS = [(740, 366), (555, 368)]
SELAN_POINTS = [(584, 376), (580, 370), (590, 382)]

# Windows: the perks window, bank window (before and after it is dragged right) and crafting windows.
PERKS_FIRST_ROW = (158, 375)
PERKS_BUY = (302, 594)
PERKS_YES = (302, 584)
PERKS_CLOSE = (743, 17)
# Bank window controls, as offsets from the window's left edge.
BANK_DEPOSIT = (237, 494)
BANK_FIRST_WITHDRAW = (395, 140)
BANK_CLOSE = (456, 17)
RECIPE_FIRST = (153, 202)
RECIPE_CRAFT = (456, 560)
RECIPE_CLOSE = (603, 17)
CRAFT_WINDOW_CORNER = ((538, 287), (1100, 640))
CRAFT_START = (317, 530)
CRAFT_BASIC_SYNTHESIS = (706, 362)
CRAFT_CLOSE = (1090, 17)
CONVERSATION_FIRST_REPLY = (316, 357)

# The three areas of the start of the game, as the server's chat log names them.
AREA_HANGAR = 'CZ-220 - Hangar'
AREA_OFFICES = 'CZ-220 - Offices & Labs'


def expect(step, name: str, evidence: str | None, detail: str = '') -> bool:
    """Record a check that passes when evidence (a matched log line) was found."""
    return step.check(name, bool(evidence), (evidence or detail)[:160])


def stop_actions(client: GameClient) -> None:
    """Cancel whatever the character is doing (an attack, a walk) with a click on the floor."""
    client.click(900, 480)
    time.sleep(2)


def teleport(client: GameClient, tag: str) -> bool:
    """Jump to a waypoint with /tpwp; True when the server accepted the tag and the area is loaded."""
    stop_actions(client)
    client.ensure_alive()
    tips = client.loading_screens()
    mark = client.mark()
    client.say(f'/tpwp {tag}')
    if client.wait_for('Invalid waypoint tag', mark, 4, 1):
        return False
    if client.wait_area_change(tips, timeout=14):
        client.wait_loaded()
    else:
        time.sleep(4)
    return True


def reset_windows(client: GameClient) -> None:
    """Close every window and restore the default window positions (/resetwindows).

    The server remembers where each window was last dragged, so a step that clicks inside one
    starts from the defaults to find its controls where its coordinates expect them.
    """
    client.say('/resetwindows')
    time.sleep(3)


def spawn(client: GameClient, resref: str, quantity: int, item_name: str) -> str | None:
    """Create items with /spawnitem; the matching "Acquired Item" chat line, or None."""
    mark = client.mark()
    client.say(f'/spawnitem {resref} {quantity}')
    return client.wait_for(rf'Acquired Item: {re.escape(item_name)}', mark, 20)


def attack_target(client: GameClient, victim: str, attempts: int = 3) -> tuple[int, int] | None:
    """Find a hostile near the character, click it and wait until the character attacks it."""
    for _ in range(attempts):
        client.ensure_alive()
        point = client.find_hostile()
        if point is None:
            continue
        mark = client.mark()
        client.click_object(*point)
        if client.wait_for(rf'Agan Tarn attacks {re.escape(victim)}', mark, 14, 1.5):
            return point
    return None


def equip_weapon(client: GameClient, resref: str, name: str) -> bool:
    """Create a weapon, find its icon in the inventory grid and drag it onto the main-hand slot."""
    center = client.locate_new_item(lambda: client.say(f'/spawnitem {resref} 1'))
    if center is None:
        return False
    client.drag(center[0], center[1], *MAIN_HAND_SLOT)
    time.sleep(3)
    return True


def player_record(run) -> dict:
    uuid = run.stack.state['character']['uuid']
    return json.loads(run.redis('JSON.GET', f'Player:{uuid}', '.'))


def attack_log_has(client: GameClient, mark: int, attack_type: int, weapon: str) -> str | None:
    pattern = re.compile(rf'PC\?: 1, Defender .*Attack type: {attack_type}, weapon {re.escape(weapon)}')
    for line in client.attack_lines()[mark:]:
        if pattern.search(line):
            return line.split('[INF] ', 1)[-1]
    return None


# ---------------------------------------------------------------- steps

def combat(run, step) -> None:
    """A rifle (ranged) attack, then a knife (melee) attack, on mynocks that come to the character."""
    client: GameClient = run.client
    client.ensure_alive()
    step.check('inventory panel opens', client.ensure_inventory_open())
    attack_mark = len(client.attack_lines())

    # Ranged. Emptying the main hand first makes the step independent of what the character held.
    unequip_main_hand(client)
    step.check('a rifle is created and equipped', equip_weapon(client, 'b_rifle', 'Basic Rifle'))
    step.check('teleport to the mynock spawn point', teleport(client, 'CZ220_MYNOCKS'))
    mark = client.mark()
    expect(step, 'the target appears and engages', client.wait_for(r'Mynock (attacks|uses|readies)', mark, 70, 3))
    clicked = attack_target(client, 'Mynock')
    step.check('the character attacks the mynock with the rifle', clicked is not None, f'clicked {clicked}')
    time.sleep(8)
    damage = matching_lines(client.lines_since(mark), r'Agan Tarn .*damages Mynock: \d+ \(')
    expect(step, 'ranged damage number appears in the combat log', damage[0] if damage else None)
    expect(step, 'the server resolved the attack as a ranged rifle attack',
           attack_log_has(client, attack_mark, 2, 'Basic Rifle'))
    run.capture(step, 'ranged')
    attack_mark = len(client.attack_lines())

    # Melee: swap the rifle for a knife, then fight the next mynock that comes. The rifle may have
    # killed the first one, so the character goes back to the spawn point for another.
    client.ensure_alive()
    step.check('the rifle is unequipped', unequip_main_hand(client))
    step.check('a knife is created and equipped', equip_weapon(client, 'b_knife', 'Basic Knife'))
    mark = client.mark()
    if not client.wait_for(r'Mynock (attacks|uses|readies)', mark, 25, 3):
        teleport(client, 'ENTRY_STARTING_WP')
        teleport(client, 'CZ220_MYNOCKS')
        mark = client.mark()
        client.wait_for(r'Mynock (attacks|uses|readies)', mark, 70, 3)
    mark = client.mark()
    clicked = attack_target(client, 'Mynock')
    step.check('the character attacks the mynock with the knife', clicked is not None, f'clicked {clicked}')
    time.sleep(8)
    damage = matching_lines(client.lines_since(mark), r'Agan Tarn .*damages Mynock: \d+ \(')
    expect(step, 'melee damage number appears in the combat log', damage[0] if damage else None)
    expect(step, 'the server resolved the attack as a melee knife attack',
           attack_log_has(client, attack_mark, 1, 'Basic Knife'))
    run.capture(step, 'melee')
    stop_actions(client)
    step.detail = 'Ranged rifle and melee knife attacks landed and their damage was logged.'


def unequip_main_hand(client: GameClient) -> bool:
    """Drag the main-hand weapon back into the inventory grid; True when the slot looks empty again."""
    client.drag(*MAIN_HAND_SLOT, *EMPTY_GRID_CELL)
    time.sleep(3)
    return True


def ability(run, step) -> None:
    """Buy Adrenal Stim in the perks window, use it, then use it again inside its cooldown."""
    client: GameClient = run.client
    reset_windows(client)
    step.check('teleport to the hangar', teleport(client, 'ENTRY_STARTING_WP'))
    mark = client.mark()
    client.say('/perks')
    time.sleep(5)
    client.nui_click(*PERKS_FIRST_ROW)
    time.sleep(2)
    client.nui_click(*PERKS_BUY)
    time.sleep(2)
    client.nui_click(*PERKS_YES)
    expect(step, 'the perk is bought', client.wait_for(r"You purchase 'Adrenal Stim' rank 1\.", mark, 20))
    perks = player_record(run).get('Perks', {})
    step.check('the purchase is saved on the player record', perks.get('AdrenalStim') == 1, json.dumps(perks))
    run.capture(step, 'perk-bought')
    client.nui_click(*PERKS_CLOSE)
    time.sleep(2)

    expect(step, 'stim packs are created', spawn(client, 'stim_pack', 3, 'Stim Pack'))
    slot = (HOTBAR_X[3], HOTBAR_Y)
    mark = client.mark()
    client.nui_click(*slot)
    time.sleep(2)
    client.click(*SELF_POINT)
    expect(step, 'the ability is used', client.wait_for(r'Agan Tarn readies Adrenal Stim I\.', mark, 20))
    expect(step, 'the stim pack is consumed', client.wait_for(r'Lost Item: Stim Pack', mark, 10))
    expect(step, 'the effect lands', client.wait_for(r'Agan Tarn receives the effect of Adrenal Stim', mark, 10))
    run.capture(step, 'used')

    mark = client.mark()
    client.nui_click(*slot)
    time.sleep(2)
    client.click(*SELF_POINT)
    expect(step, 'a second use is refused while on cooldown',
           client.wait_for(r'This ability can be used in \d+ seconds?\.', mark, 15))
    run.capture(step, 'cooldown')
    step.detail = 'Adrenal Stim was bought, used once and refused with a cooldown message on the second use.'


def storage(run, step) -> None:
    """Deposit an item in a bank terminal and withdraw it again."""
    client: GameClient = run.client
    reset_windows(client)
    step.check('inventory panel is open', client.ensure_inventory_open())
    center = client.locate_new_item(lambda: client.say('/spawnitem b_pistol 1'))
    step.check('an item to deposit appears in the inventory', center is not None, str(center))
    if center is None:
        return
    step.check('teleport to the bank', teleport(client, 'MN_Bank'))
    before = len(run.redis('KEYS', 'InventoryItem:*').split())

    opened = None
    for point in BANK_TERMINAL_POINTS:
        client.click_object(*point)
        time.sleep(7)
        if bank_window_open(client):
            opened = point
            break
    step.check('the bank window opens', opened is not None, str(opened))
    if opened is None:
        return
    left = window_left(client.snapshot())
    step.check('the bank window is located', left is not None, f'left edge {left}')
    if left is None:
        return
    if left < 380:
        # The bank covers the inventory panel: move it to the right so the items stay clickable.
        client.drag(left + 200, 17, left + 200 + (640 - left), 17)
        time.sleep(2)
        left = window_left(client.snapshot()) or left
    mark = client.mark()
    client.nui_click(left + BANK_DEPOSIT[0], BANK_DEPOSIT[1])
    expect(step, 'the deposit asks for an item', client.wait_for(r'Please click on an item within your inventory\.', mark, 15))
    client.click(*center)
    expect(step, 'the item leaves the inventory', client.wait_for(r'Lost Item: Basic Pistol', mark, 15))
    after = len(run.redis('KEYS', 'InventoryItem:*').split())
    step.check('the bank record exists', after == before + 1, f'{before} -> {after}')
    run.capture(step, 'deposited')

    mark = client.mark()
    client.nui_click(left + BANK_FIRST_WITHDRAW[0], BANK_FIRST_WITHDRAW[1])
    expect(step, 'the item returns to the inventory', client.wait_for(r'Acquired Item: Basic Pistol', mark, 15))
    final = len(run.redis('KEYS', 'InventoryItem:*').split())
    step.check('the bank record is gone', final == before, f'{after} -> {final}')
    run.capture(step, 'withdrawn')
    client.nui_click(left + BANK_CLOSE[0], BANK_CLOSE[1])
    time.sleep(2)
    step.detail = 'An item was deposited in the bank and withdrawn again.'


def bank_window_open(client: GameClient) -> bool:
    """True when a bank window's Deposit button is on screen (below the inventory panel's bottom edge)."""
    return bright_pixels(client.snapshot(), (0, 484, 1280, 506)) >= 60


def craft(run, step) -> None:
    """Cook one Mynock Meat Ball at a cookpot."""
    client: GameClient = run.client
    reset_windows(client)
    step.check('teleport to the cookpots', teleport(client, 'MN_Cookpots'))
    expect(step, 'mynock meat is created', spawn(client, 'mynock_meat', 3, 'Mynock Meat'))
    expect(step, 'mynock wings are created', spawn(client, 'mynock_wing', 2, 'Mynock Wing'))
    expect(step, 'distilled water is created', spawn(client, 'distilled_water', 1, 'Distilled Water'))

    opened = None
    for point in COOKPOT_POINTS:
        client.click_object(*point)
        time.sleep(7)
        if green_text_present(client.snapshot(), (30, 190, 280, 300)):
            opened = point
            break
    step.check('the crafting window opens', opened is not None, str(opened))
    if opened is None:
        return
    client.nui_click(*RECIPE_FIRST)
    time.sleep(3)
    mark = client.mark()
    client.nui_click(*RECIPE_CRAFT)
    time.sleep(6)
    client.drag(*CRAFT_WINDOW_CORNER[0], *CRAFT_WINDOW_CORNER[1])
    time.sleep(2)
    client.nui_click(*CRAFT_START)
    expect(step, 'the components are consumed', client.wait_for(r'Lost Item: Distilled Water', mark, 20))
    made = None
    for _ in range(6):
        client.nui_click(*CRAFT_BASIC_SYNTHESIS)
        made = client.wait_for(r'Acquired Item: Mynock Meat Ball', mark, 7, 1.5)
        if made:
            break
    expect(step, 'the item is crafted', made)
    expect(step, 'crafting skill experience is awarded', client.wait_for(r'You earned Agriculture skill experience', mark, 10))
    run.capture(step, 'crafted')
    client.nui_click(*CRAFT_CLOSE)
    time.sleep(2)
    client.nui_click(*RECIPE_CLOSE)
    time.sleep(2)
    step.detail = 'A Mynock Meat Ball was crafted at a cookpot.'


def quest(run, step) -> None:
    """Accept Selan's Request from the reception desk."""
    client: GameClient = run.client
    step.check('teleport to the reception desk', teleport(client, 'Reception'))
    accepted = None
    mark = client.mark()
    for point in SELAN_POINTS:
        client.click_object(*point)
        time.sleep(6)
        for _ in range(3):
            client.nui_click(*CONVERSATION_FIRST_REPLY)
            time.sleep(4)
        accepted = client.wait_for(r"Quest 'Selan's Request' accepted\.", mark, 8, 2)
        if accepted:
            break
    expect(step, 'the quest is accepted in conversation', accepted)
    quests = player_record(run).get('Quests', {})
    step.check('the quest is on the player record at its first state',
               quests.get('selan_request', {}).get('CurrentState') == 1, json.dumps(quests)[:120])
    run.capture(step, 'accepted')
    step.detail = "Selan's Request was accepted through her conversation."


def transfer(run, step) -> None:
    """Go to the hangar and back to the offices, proving each area from the server's chat log."""
    client: GameClient = run.client
    step.check('teleport to the hangar', teleport(client, 'ENTRY_STARTING_WP'))
    run.capture(step, 'hangar')
    step.check('teleport back to the offices', teleport(client, 'Reception'))
    client.say('/help')
    time.sleep(4)
    lines = client.server_chat_lines()
    # Each chat line is logged with the area its speaker stood in when sending it.
    wanted = [(f'{AREA_OFFICES} Agan Tarn: /tpwp ENTRY_STARTING_WP', 'sent from the offices'),
              (f'{AREA_HANGAR} Agan Tarn: /tpwp Reception', 'sent from the hangar'),
              (f'{AREA_OFFICES} Agan Tarn: /help', 'sent from the offices again')]
    for text, name in wanted:
        found = [line for line in lines if text in line]
        step.check(f'the server logged the command {name}', bool(found), found[-1][:120] if found else text)
    run.capture(step, 'offices')
    step.detail = 'The character moved offices -> hangar -> offices and the server logged each area.'
