"""Validate recovered Trade Concourse travel, merchant, and resource wiring."""
import json
import math
import hashlib
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
    area_locals = {value(local, "Name"): local for local in value(interior, "VarTable")}
    for name, expected in [("MAP_KEY_ITEM_ID", 40), ("PLANET_TYPE_ID", 1)]:
        assert value(area_locals[name], "Type") == 1 and value(area_locals[name], "Value") == expected
    assert sum(value(a, "Area_Name") == "veles_tradecon" for a in value(module, "Mod_Area_list")) == 1
    assert sum(value(a, "Area_Name") == "veles_exterior" for a in value(module, "Mod_Area_list")) == 1
    assert len(value(area, "Tile_List")) == value(area, "Width") * value(area, "Height")
    assert value(area, "Tileset") == value(read("Module/are/velesinterior.are.json"), "Tileset")
    comments = read("Module/gic/veles_tradecon.gic.json")
    for collection in ["Creature List", "Placeable List", "WaypointList"]:
        assert len(value(comments, collection)) == len(value(interior, collection)), collection
    manifest = read("tools/VelesConcourseContent.json")
    placement_fields = {"Tag", "X", "Y", "Z", "XPosition", "YPosition", "ZPosition",
                        "Bearing", "XOrientation", "YOrientation"}
    def gameplay(obj):
        return {key: item for key, item in obj.items() if key not in placement_fields}

    for entry in manifest["placements"]:
        source = read(f"Module/git/{entry['area']}.git.json")
        matches = [o for o in value(interior, entry["list"]) if value(o, "Tag") == entry["tag"]]
        assert len(matches) == 1, entry["tag"]
        placed = matches[0]
        if entry.get("relocated"):
            assert not any(value(o, "Tag") == entry["sourceTag"] for o in value(source, entry["list"]))
            digest = hashlib.sha256(json.dumps(gameplay(placed), sort_keys=True).encode()).hexdigest()
            assert digest == entry["gameplaySha256"], f"Changed relocated NPC data: {entry['tag']}"
        else:
            original = next(o for o in value(source, entry["list"]) if value(o, "Tag") == entry["sourceTag"])
            assert gameplay(placed) == gameplay(original), f"Stale gameplay data: {entry['tag']}"
        x, y = ("X", "Y") if entry["list"] == "Placeable List" else ("XPosition", "YPosition")
        assert (value(placed, x), value(placed, y)) == (entry["x"], entry["y"])
        assert 2 < entry["x"] < 28 and 2 < entry["y"] < 28
        if entry["list"] == "WaypointList":
            continue
        # Keep the new interactions away from existing NPCs, props and exits.
        # Floor panels/rugs and overhead dressing are not obstacles at their origins.
        for collection, ox, oy, oz in [("Creature List", "XPosition", "YPosition", "ZPosition"),
                                       ("Placeable List", "X", "Y", "Z"), ("Door List", "X", "Y", "Z")]:
            for other in value(interior, collection):
                if other is placed or value(other, oz) > 2:
                    continue
                name = str(value(other, "LocName") or "")
                if "Floor -" in name or "Rug" in name:
                    continue
                assert math.hypot(entry["x"] - value(other, ox), entry["y"] - value(other, oy)) >= 1.25, entry["tag"]
    for entry in manifest["stores"]:
        source = read(f"Module/git/{entry['area']}.git.json")
        original = next(o for o in value(source, "StoreList") if value(o, "Tag") == entry["sourceTag"])
        placed = next(o for o in value(interior, "StoreList") if value(o, "Tag") == entry["tag"])
        assert gameplay(placed) == gameplay(original), f"Stale store: {entry['tag']}"
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
            party_flags = [v for v in value(obj, "VarTable") if value(v, "Name") == "TELEPORT_PARTY_MEMBERS"]
            assert len(party_flags) == 1 and value(party_flags[0], "Type") == 1 and value(party_flags[0], "Value") == 0
            destination = next(value(v, "Value") for v in value(obj, "VarTable") if value(v, "Name") == "DESTINATION")
            assert waypoints.count(destination) == 1
            if obj is entrance:
                assert destination == "V_Veles_To_Concourse"
    stores = [value(o, "Tag") for o in value(interior, "StoreList")]
    datapad_store = next(o for o in value(interior, "StoreList") if value(o, "Tag") == "DataStore")
    assert value(datapad_store, "ResRef") == "concourse_data"
    def npc_name(npc):
        name = " ".join((value(npc, key) or {}).get("0", "") for key in ["FirstName", "LastName"]).strip()
        return name.removeprefix("Flower Shop ")
    npc_names = [npc_name(npc) for npc in value(interior, "Creature List")]
    assert len(npc_names) == len(set(npc_names))
    for merchant in manifest["relocatedMerchants"]:
        npc = next(npc for npc in value(interior, "Creature List") if npc_name(npc) == merchant["name"])
        digest = hashlib.sha256(json.dumps(gameplay(npc), sort_keys=True).encode()).hexdigest()
        assert digest == merchant["gameplaySha256"], f"Changed merchant appearance/gameplay: {merchant['name']}"
    canonical_merchants = {"Adega Jorgan": "vendor_merchant", "Volnatu Gigg": "veles_volnatu", "Hana": "night_viscflower"}
    for name, conversation in canonical_merchants.items():
        npc = next(npc for npc in value(interior, "Creature List") if npc_name(npc) == name)
        assert value(npc, "Conversation") == conversation
    for retired in ["concourse_treat", "concourse_food", "flowershop"]:
        assert not (ROOT / f"SWLOR.Game.Server/ConversationData/{retired}.conversation.json").exists()
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
        if "veles" in path.name or "vels" in path.name:
            assert not set(npc_names).intersection(npc_name(npc) for npc in value(data, "Creature List")), path
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
    def inventory_entries(obj):
        if isinstance(obj, dict):
            if "TemplateResRef" in obj:
                yield obj
            for item in obj.values():
                yield from inventory_entries(item)
        elif isinstance(obj, list):
            for item in obj:
                yield from inventory_entries(item)
    datapad_items = list(inventory_entries(datapad_store))
    assert len(datapad_items) == 1 and value(datapad_items[0], "TemplateResRef") == "handdatapad"
    assert value(datapad_items[0], "Infinite") == 1
    check_items(interior)
    print("Trade Concourse: registration, tiles, travel, conversations, unique stores, and inventory references passed.")


if __name__ == "__main__":
    validate()
