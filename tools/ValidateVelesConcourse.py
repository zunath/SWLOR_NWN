"""Validate recovered Trade Concourse travel, merchant, and resource wiring."""
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    data = (ROOT / path).read_bytes()
    try:
        return json.loads(data.decode("utf-8"))
    except UnicodeDecodeError:
        return json.loads(data.decode("cp1252"))


def value(obj, key):
    return obj.get(key, {}).get("value")


def validate():
    area = read("Module/are/veles_tradecon.are.json")
    interior = read("Module/git/veles_tradecon.git.json")
    exterior = read("Module/git/veles_exterior.git.json")
    module = read("Module/ifo/module.ifo.json")
    assert sum(value(a, "Area_Name") == "veles_tradecon" for a in value(module, "Mod_Area_list")) == 1
    assert sum(value(a, "Area_Name") == "veles_exterior" for a in value(module, "Mod_Area_list")) == 1
    assert len(value(area, "Tile_List")) == value(area, "Width") * value(area, "Height")
    assert value(area, "Tileset") == value(read("Module/are/velesinterior.are.json"), "Tileset")
    read("Module/gic/veles_tradecon.gic.json")
    waypoints = [value(o, "Tag") for data in [interior, exterior] for o in value(data, "WaypointList")]
    for door in value(interior, "Door List"):
        if value(door, "LinkedToFlags"):
            assert waypoints.count(value(door, "LinkedTo")) == 1
    entrance = next(o for o in value(exterior, "Placeable List") if value(o, "Tag") == "veles_concourse_entry")
    def tagged(data, collection, tag):
        matches = [o for o in value(data, collection) if value(o, "Tag") == tag]
        assert len(matches) == 1, tag
        return matches[0]

    def landing(data, tag, exit_object):
        point = tagged(data, "WaypointList", tag)
        distance = math.hypot(value(point, "XPosition") - value(exit_object, "X"),
                              value(point, "YPosition") - value(exit_object, "Y"))
        assert 0.75 <= distance <= 3, (tag, distance)
        assert abs(value(point, "ZPosition") - value(exit_object, "Z")) < 0.5
        return point

    outside_door = tagged(interior, "Door List", "concourse_exterior_door")
    warehouse_door = tagged(interior, "Door List", "concourse_warehouse_door")
    warehouse_exit = tagged(interior, "Placeable List", "concourse_warehouse_exit")
    assert value(outside_door, "LinkedTo") == "V_Concourse_To_Veles"
    assert value(warehouse_door, "LinkedTo") == "V_Concourse_To_Warehouse"
    landing(interior, "V_Veles_To_Concourse", outside_door)
    landing(interior, "V_Concourse_To_Warehouse", warehouse_exit)
    landing(interior, "V_Warehouse_To_Concourse", warehouse_door)
    outside_landing = landing(exterior, "V_Concourse_To_Veles", entrance)
    # The current exterior has a fence and an invisible wall east of the road.
    # Both interaction and return landing must stay on their street (west) side.
    barriers = [o for o in value(exterior, "Placeable List")
                if value(o, "Tag") in ["SWTOR_FENCE", "_mdrn_pl_fleng06"]
                and 177 < value(o, "X") < 181 and 20 < value(o, "Y") < 24]
    assert len(barriers) == 2
    for barrier in barriers:
        assert value(entrance, "X") < value(barrier, "X") - 1
        assert value(outside_landing, "XPosition") < value(barrier, "X") - 2
    for door in [outside_door, warehouse_door]:
        assert value(door, "LinkedToFlags") == 2
        assert value(door, "Plot") == 1 and value(door, "Locked") == 0 and value(door, "Lockable") == 0
    closed_lift = tagged(interior, "Door List", "concourse_closed_lift")
    assert value(closed_lift, "Locked") == 1 and value(closed_lift, "Plot") == 1
    assert value(closed_lift, "KeyRequired") == 1 and value(closed_lift, "LinkedToFlags") == 0
    assert "Out of Service" in value(closed_lift, "LocName")["0"]
    for obj in [entrance] + value(interior, "Placeable List"):
        if value(obj, "OnUsed") == "teleport":
            assert value(obj, "Useable") == 1 and value(obj, "Static") == 0
            destination = next(value(v, "Value") for v in value(obj, "VarTable") if value(v, "Name") == "DESTINATION")
            assert waypoints.count(destination) == 1
            if obj is entrance:
                assert destination == "V_Veles_To_Concourse"
    stores = [value(o, "Tag") for o in value(interior, "StoreList")]
    opened = set()
    for npc in value(interior, "Creature List"):
        conversation = value(npc, "Conversation")
        if conversation:
            assert value(npc, "ScriptDialogue") == "dialog_start"
            graph = read(f"SWLOR.Game.Server/ConversationData/{conversation}.conversation.json")
            assert graph["Id"] == conversation
            for choice in graph["Choices"].values():
                for action in choice["Actions"]:
                    if action["Key"] == "action-open-store":
                        tag, = action["Arguments"]
                        assert stores.count(tag) == 1
                        opened.add(tag)
    assert opened == set(stores), "Every placed store must have a usable merchant."
    for path in (ROOT / "Module/git").glob("*.json"):
        if path.name == "veles_tradecon.git.json":
            continue
        data = read(path)
        assert not opened.intersection(value(o, "Tag") for o in data.get("StoreList", {}).get("value", [])), path
        if path.name != "veles_exterior.git.json":
            assert not set(["V_Veles_To_Concourse", "V_Concourse_To_Veles",
                            "V_Concourse_To_Warehouse", "V_Warehouse_To_Concourse"]).intersection(
                value(o, "Tag") for o in data.get("WaypointList", {}).get("value", [])), path

    def check_items(obj):
        if isinstance(obj, dict):
            for key, item in obj.items():
                if key == "InventoryRes":
                    assert (ROOT / f"Module/uti/{item['value']}.uti.json").exists(), item
                check_items(item)
        elif isinstance(obj, list):
            for item in obj:
                check_items(item)
    check_items(interior)
    print("Trade Concourse: registration, tiles, travel, conversations, unique stores, and inventory references passed.")


if __name__ == "__main__":
    validate()
