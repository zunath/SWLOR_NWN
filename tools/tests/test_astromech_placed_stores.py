"""A placed store holds each item in full: the stocking helper adds the astromech form and keeps it up to date."""
import importlib
import json
import sys
import tempfile
import unittest
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))
placed = importlib.import_module("AstromechPlacedStores")


def blueprint(resref: str, torso: int) -> bytes:
    return json.dumps({
        "__data_type": "UTI ", "Comment": {"type": "cexostring", "value": ""}, "PaletteID": {"type": "byte", "value": 1},
        "ArmorPart_Torso": {"type": "byte", "value": torso},
        "TemplateResRef": {"type": "resref", "value": resref},
    }).encode("latin-1")


def instance(resref: str, torso: int, struct_id: int, x: int, y: int) -> dict:
    return {
        "__struct_id": struct_id,
        "ArmorPart_Torso": {"type": "byte", "value": torso},
        "Infinite": {"type": "byte", "value": 1},
        "Repos_PosX": {"type": "word", "value": x}, "Repos_Posy": {"type": "word", "value": y},
        "TemplateResRef": {"type": "resref", "value": resref},
        "XOrientation": {"type": "float", "value": 0.0}, "XPosition": {"type": "float", "value": 1.5},
        "YOrientation": {"type": "float", "value": 1.0}, "YPosition": {"type": "float", "value": 2.5},
        "ZPosition": {"type": "float", "value": 0.0},
    }


def area(items: list) -> bytes:
    store = {"Tag": {"type": "cexostring", "value": "shop"},
             "StoreList": {"type": "list", "value": [{"ItemList": {"type": "list", "value": items}}]}}
    return (json.dumps({"StoreList": {"type": "list", "value": [store]}}, indent=2) + "\n").encode("latin-1")


class PlacedStoreTests(unittest.TestCase):
    def stock(self, items, blueprints):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "area.git.json"
            path.write_bytes(area(items))
            outputs = placed.stock({"shop": [("tunic", "plating")]}, lambda resref: blueprints[resref], Path(folder))
            return {name.name: json.loads(data.decode("latin-1")) for name, data in outputs.items()}

    def items(self, outputs):
        return outputs["area.git.json"]["StoreList"]["value"][0]["StoreList"]["value"][0]["ItemList"]["value"]

    def test_the_astromech_form_is_added_beside_the_item_it_copies(self):
        outputs = self.stock([instance("tunic", 53, 0, 0, 0)], {"plating": blueprint("plating", 240)})
        added = self.items(outputs)[1]
        self.assertEqual(added["TemplateResRef"]["value"], "plating")
        self.assertEqual(added["ArmorPart_Torso"]["value"], 240)
        self.assertEqual((added["Repos_PosX"]["value"], added["Repos_Posy"]["value"]), (2, 0))
        self.assertNotIn("PaletteID", added)

    def test_a_copy_already_there_follows_its_blueprint(self):
        stale = instance("plating", 222, 1, 4, 2)
        outputs = self.stock([instance("tunic", 53, 0, 0, 0), stale], {"plating": blueprint("plating", 240)})
        tunic, refreshed = self.items(outputs)
        self.assertEqual(tunic["ArmorPart_Torso"]["value"], 53, "the humanoid item is left alone")
        self.assertEqual(refreshed["ArmorPart_Torso"]["value"], 240)
        # Its place in the store and on the grid stays.
        self.assertEqual(refreshed["__struct_id"], 1)
        self.assertEqual((refreshed["Repos_PosX"]["value"], refreshed["Repos_Posy"]["value"]), (4, 2))
        self.assertEqual(refreshed["XPosition"]["value"], 1.5)

    def test_an_up_to_date_store_is_not_rewritten(self):
        current = instance("plating", 240, 1, 4, 2)
        self.assertEqual(self.stock([instance("tunic", 53, 0, 0, 0), current],
                                    {"plating": blueprint("plating", 240)}), {})


if __name__ == "__main__":
    unittest.main()
