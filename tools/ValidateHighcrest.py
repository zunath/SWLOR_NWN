"""Validate Highcrest NPC, conversation, equipment and shop wiring without NWN."""
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AREA = "pw_ar_sc_eshancm"


def read(path):
    return json.loads((ROOT / path).read_text(encoding="utf-8"))


def value(obj, key):
    return obj.get(key, {}).get("value")


def validate_graph(graph):
    nodes, choices = graph["Nodes"], graph["Choices"]
    visited_nodes, visited_choices = set(), set()
    pending = [link["TargetNodeId"] for link in graph["EntryPoints"]]
    while pending:
        key = pending.pop()
        assert key in nodes, (graph["Id"], "missing node", key)
        if key in visited_nodes:
            continue
        visited_nodes.add(key)
        node = nodes[key]
        assert node["Id"] == key and any(t["Text"].strip() for t in node["Text"])
        for link in node["Choices"]:
            choice_id = link["ChoiceId"]
            assert choice_id in choices, (graph["Id"], "missing choice", choice_id)
            visited_choices.add(choice_id)
            choice = choices[choice_id]
            assert choice["Id"] == choice_id and choice["Text"]["Text"].strip()
            assert choice["EndsConversation"] == (not choice["Next"])
            pending.extend(route["TargetNodeId"] for route in choice["Next"])
    assert visited_nodes == set(nodes) and visited_choices == set(choices)
    # Every line must have a route to an exit, including after optional lore.
    can_exit = set()
    while True:
        before = set(can_exit)
        for key, node in nodes.items():
            for link in node["Choices"]:
                choice = choices[link["ChoiceId"]]
                if choice["EndsConversation"] or any(r["TargetNodeId"] in can_exit for r in choice["Next"]):
                    can_exit.add(key)
        if before == can_exit:
            break
    assert can_exit == set(nodes), (graph["Id"], "conversation trap")


def validate():
    area = read(f"Module/git/{AREA}.git.json")
    metadata = read(f"Module/are/{AREA}.are.json")
    comments = read(f"Module/gic/{AREA}.gic.json")
    assert value(metadata, "Tag") == "pw_ar_sc_eshancmount"
    npcs = value(area, "Creature List")
    shops = value(area, "StoreList")
    assert len(npcs) == 15 and len(shops) == 3
    assert len({value(n, "Tag") for n in npcs}) == len(npcs)
    for collection in ["Creature List", "StoreList"]:
        assert len(value(area, collection)) == len(value(comments, collection))
    shop_tags = {value(s, "Tag") for s in shops}
    wanderers, greetings, shop_users, guard_genders = set(), set(), set(), set()
    positions = []
    for npc in npcs:
        ref = value(npc, "TemplateResRef")
        assert len(ref) <= 16 and npc["__struct_id"] == 4
        blueprint = read(f"Module/utc/{ref}.utc.json")
        for key, item in blueprint.items():
            if key not in {"__data_type", "Comment", "PaletteID", "Equip_ItemList"}:
                assert npc[key] == item, (ref, "placed blueprint mismatch", key)
        assert value(npc, "ScriptDialogue") == "dialog_start"
        assert value(npc, "ScriptHeartbeat") == "crea_hb_aft"
        graph = read(f"SWLOR.Game.Server/ConversationData/{ref}.conversation.json")
        assert value(npc, "Conversation") == graph["Id"] == ref
        assert not (ROOT / f"Module/dlg/{ref}.dlg.json").exists()
        validate_graph(graph)
        greeting = graph["Nodes"][graph["EntryPoints"][0]["TargetNodeId"]]["Text"][0]["Text"]
        assert greeting not in greetings
        greetings.add(greeting)
        for choice in graph["Choices"].values():
            for action in choice["Actions"]:
                assert action["Key"] == "action-open-store"
                assert len(action["Arguments"]) == 1 and action["Arguments"][0] in shop_tags
                shop_users.add(action["Arguments"][0])
        flags = {value(v, "Name"): value(v, "Value") for v in value(npc, "VarTable")}
        if flags["AI_FLAGS"]:
            assert flags["AI_FLAGS"] == 3  # RandomWalk | ReturnHome.
            wanderers.add(ref)
        x, y, z = [value(npc, key) for key in ["XPosition", "YPosition", "ZPosition"]]
        assert 0 < x < value(metadata, "Width") * 10 and 0 < y < value(metadata, "Height") * 10
        assert math.isclose(value(npc, "XOrientation") ** 2 + value(npc, "YOrientation") ** 2, 1, abs_tol=1e-5)
        assert all(math.dist((x, y, z), other) > 1 for other in positions)
        positions.append((x, y, z))
        expected = {e["__struct_id"]: value(e, "EquippedRes") for e in value(blueprint, "Equip_ItemList")}
        assert len(expected) == len(value(npc, "Equip_ItemList"))
        for equipment in value(npc, "Equip_ItemList"):
            item_ref = expected[equipment["__struct_id"]]
            item = read(f"Module/uti/{item_ref}.uti.json")
            assert value(equipment, "Dropable") == 0
            assert all(equipment[k] == v for k, v in item.items() if k != "__data_type")
            if item_ref.startswith("hc_"):
                assert value(item, "Plot") == 1
                assert any(value(v, "Name") == "NO_ECONOMY" and value(v, "Value") == 1 for v in value(item, "VarTable"))
        if "hc_guard_helm" in expected.values():
            guard_genders.add(value(npc, "Gender"))
    assert wanderers == {"hc_pell", "hc_mira", "hc_tellis"}
    assert guard_genders == {0, 1} and shop_users == shop_tags
    for shop in shops:
        ref = value(shop, "ResRef")
        blueprint = read(f"Module/utm/{ref}.utm.json")
        assert shop["__struct_id"] == 11 and value(shop, "Tag") == ref
        for key in ["MarkUp", "MarkDown", "OnOpenStore", "OnStoreClosed"]:
            assert shop[key] == blueprint[key]
        stock_count = 0
        for placed_category, category in zip(value(shop, "StoreList"), value(blueprint, "StoreList"), strict=True):
            assert placed_category["__struct_id"] == category["__struct_id"]
            for placed, entry in zip(value(placed_category, "ItemList"), value(category, "ItemList"), strict=True):
                item = read(f"Module/uti/{value(entry, 'InventoryRes')}.uti.json")
                assert all(placed[k] == v for k, v in item.items() if k != "__data_type")
                assert value(placed, "Infinite") == value(entry, "Infinite")
                stock_count += 1
        assert stock_count > 0
    print("PASS: 15 distinct conversations, NPC/equipment parity, 3 bounded wanderers, both guard genders and 3 linked shops.")


if __name__ == "__main__":
    validate()
