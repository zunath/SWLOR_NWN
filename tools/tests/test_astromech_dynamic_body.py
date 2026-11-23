"""The astromech's dynamic body: the skeleton, the animation names, the part cut and the held tools."""
import importlib
import re
import sys
import unittest
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))
body = importlib.import_module("AstromechDynamicBody")

CHASSIS = """beginmodelgeom c_astro_01
node dummy c_astro_01
  parent NULL
endnode
node dummy rootdummy
  parent c_astro_01
  position 0 0 0
endnode
node dummy torso
  parent rootdummy
  position 0 0.004 0.559
endnode
node trimesh body
  parent torso
  position 0 0 0.1
  bitmap c_astro_01
  verts 2
    0 0 0
    1 2 3
  faces 0
endnode
node dummy toolr_socket
  parent torso
  position 0.065 0.586 0.049
endnode
node trimesh toolr
  parent toolr_socket
  position 0 0 0
  bitmap c_astro_prod
  verts 2
    0 0 0
    0 -0.4 0
  faces 0
endnode
node dummy tooll_socket
  parent torso
  position -0.065 0.486 0.049
endnode
node trimesh tooll
  parent tooll_socket
  position 0 0 0
  bitmap c_astro_blast
  verts 1
    0 0 0
  faces 0
endnode
endmodelgeom c_astro_01
"""


class SkeletonTests(unittest.TestCase):
    def test_the_skeleton_has_the_part_nodes_a_dynamic_body_is_drawn_on(self):
        names = [name for name, _, _ in body.SKELETON]
        self.assertEqual(len(names), len(set(names)))
        for needed in ("rootdummy", "torso_g", "neck_g", "head_g", "lthigh_g", "rthigh_g", "rhand", "lhand",
                       "pelvis_g", "belt_g", "cloak_g", "impact", "handconjure", "headconjure"):
            self.assertIn(needed, names)
        parents = {name: parent for name, parent, _ in body.SKELETON}
        for name, parent in parents.items():
            self.assertTrue(parent is None or parent in parents, name)

    def test_both_hands_rest_on_one_point_at_the_face_of_the_barrel(self):
        places = {}
        for name, parent, position in body.SKELETON:
            origin = places.get(parent, (0.0, 0.0, 0.0))
            places[name] = tuple(origin[axis] + position[axis] for axis in range(3))
        for hand in ("rhand", "lhand"):
            for axis in range(3):
                self.assertAlmostEqual(places[hand][axis], body.HAND[axis], places=6)

    def test_both_genders_share_one_skeleton_and_animation_bank(self):
        self.assertEqual(body.ROOTS, ("pmr0", "pfr0"))
        text = "\n".join(body.skeleton_model("pmr0", body.BANK))
        self.assertIn(f"setsupermodel pmr0 {body.BANK}", text)
        self.assertNotIn("trimesh", text)


class AnimationTests(unittest.TestCase):
    def test_every_native_name_plays_a_chassis_clip(self):
        self.assertEqual(len(body.NATIVE_ANIMATIONS), len(set(body.NATIVE_ANIMATIONS)))
        for name in body.NATIVE_ANIMATIONS:
            self.assertRegex(body.source_clip(name), r"^c[a-z0-9]+$", name)
        self.assertEqual(body.source_clip("walk"), "cwalk")
        self.assertEqual(body.source_clip("run_swordr"), "crun")
        self.assertEqual(body.source_clip("1hslashl"), "ca1slashl")
        self.assertEqual(body.source_clip("getmidlp"), "cgetmidlp")
        self.assertEqual(body.source_clip("kdbckdie"), "ckdbckdie")
        self.assertEqual(body.source_clip("sitcross"), "cpause1", "a motion the chassis lacks stands still")

    def test_ranged_and_ability_names_slide_the_blaster_out_and_the_rest_the_prod(self):
        for name in ("bowshot", "xbowshot", "throwr", "castout", "castselflp"):
            self.assertEqual(body.hand_track(name), body.RANGED_TRACK, name)
        for name in ("1hslashr", "2wstab", "pause1", "getmid"):
            self.assertEqual(body.hand_track(name), body.MELEE_TRACK, name)

    def test_a_clip_moves_the_hands_as_it_moved_the_tool(self):
        clip = {"length": 1.0, "transtime": 0.25, "events": [], "nodes": {
            "torso": {"orientationkey": [[0.0, 1.0, 0.0, 0.0, 0.1]]},
            "toolr": {"positionkey": [[0.0, 0.0, -0.42, 0.0], [1.0, 0.0, 0.0, 0.0]]},
            "tooll": {"positionkey": [[0.0, 0.0, -0.30, 0.0]]}}}
        nodes = body.convert_clip(clip, body.MELEE_TRACK, fishing=False)
        rest = body.relative(body.HAND, body.TORSO)
        self.assertEqual(nodes["torso_g"], clip["nodes"]["torso"])
        for hand in body.HAND_NODES:
            keys = nodes[hand]["positionkey"]
            self.assertAlmostEqual(keys[0][2], rest[1] - 0.42)
            self.assertAlmostEqual(keys[1][2], rest[1])
        self.assertNotIn("toolr", nodes)

    def test_a_fishing_clip_holds_the_hand_out_and_swings_it_as_the_rod_swung(self):
        swing = [[0.0, 1.0, 0.0, 0.0, 0.5], [1.0, 1.0, 0.0, 0.0, 1.0]]
        clip = {"length": 1.0, "transtime": 0.25, "events": [], "nodes": {
            "toolr": {"positionkey": [[0.0, 0.0, -0.42, 0.0]]}, "rod": {"orientationkey": swing}}}
        nodes = body.convert_clip(clip, body.MELEE_TRACK, fishing=True)
        rest = body.relative(body.HAND, body.TORSO)
        self.assertEqual(nodes["rhand_g"]["orientationkey"], swing)
        for row in nodes["rbicep_g"]["positionkey"]:
            self.assertAlmostEqual(row[2], rest[1])
        self.assertEqual(len(body.FISHING_CLIPS), 8)


class PartTests(unittest.TestCase):
    def test_a_part_is_moved_to_the_skeleton_joint_and_keeps_its_material(self):
        nodes = body.read_nodes(CHASSIS)
        place = body.world_position(nodes, "body")
        self.assertEqual(tuple(round(value, 3) for value in place), (0.0, 0.004, 0.659))
        shift = body.CHASSIS_SHIFT[1]
        offset = tuple(place[axis] + shift[axis] - body.TORSO[axis] for axis in range(3))
        text = body.part_model("pmr0_chest001", nodes["body"]["block"], offset)
        self.assertIn("newmodel pmr0_chest001", text)
        self.assertIn("parent pmr0_chest001", text)
        self.assertNotRegex(text, r"(?m)^\s*position\s")
        self.assertIn("bitmap c_astro_01", text)
        rows = re.search(r"verts 2\n(.*)\n(.*)\n", text)
        self.assertEqual([round(float(value), 3) for value in rows[1].split()], [0.0, 0.0, 0.1])
        self.assertEqual([round(float(value), 3) for value in rows[2].split()], [1.0, 2.0, 3.1])

    def test_every_chassis_is_cut_into_a_dome_a_barrel_and_two_legs(self):
        self.assertEqual(sorted(body.PARTS), ["chest", "head", "legl", "legr"])
        self.assertEqual({folder for _, _, folder in body.PARTS.values()},
                         {"sw_pt_head", "sw_pt_chest", "sw_pt_lthigh", "sw_pt_rthigh"})
        self.assertEqual(body.CHASSIS_COUNT, 8)


class HeldToolTests(unittest.TestCase):
    def test_every_tool_blueprint_is_a_prod_or_a_blaster_with_parts_to_build(self):
        used = body.tool_part_numbers()
        self.assertEqual(sorted(used), [545, 546])
        for base, slots in used.items():
            for slot in ("b", "m", "t"):
                self.assertTrue(slots[slot], f"base item {base} part {slot}")

    def test_the_tool_is_the_bottom_part_held_on_its_axis_and_the_other_parts_are_empty(self):
        models = body.tool_models(CHASSIS)
        prods = sorted(name for name in models if name.startswith("wampr_b_"))
        self.assertTrue(prods)
        text = models[prods[0]]
        self.assertIn("bitmap c_astro_prod", text)
        rows = re.search(r"verts 2\n(.*)\n(.*)\n", text)
        tip = [round(float(value), 3) for value in rows[1].split()]
        base = [round(float(value), 3) for value in rows[2].split()]
        # The tool tip is 0.59 ahead in chassis 1, which is built 0.047 behind the shared frame; the hand is 0.30 ahead.
        self.assertEqual(tip, [0.0, 0.337, 0.0])
        self.assertEqual(base, [0.0, -0.063, 0.0])
        for name, text in models.items():
            if "_m_" in name or "_t_" in name:
                self.assertNotIn("trimesh", text, name)

    def test_names_fit_a_resource_name(self):
        for item_class, _ in body.TOOL_ITEMS.values():
            self.assertLessEqual(len(f"i{item_class}_b_000"), 16)
            self.assertRegex(item_class, r"^w[a-z]{4}$", "FixWeaponInventoryIcons.py audits icons of this shape")


if __name__ == "__main__":
    unittest.main()
