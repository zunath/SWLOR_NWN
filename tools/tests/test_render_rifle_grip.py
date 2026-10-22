import ast
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]


def support_offsets(path):
    """Read SUPPORT_OFFSETS as plain numbers without importing Blender or numpy modules."""
    for node in ast.parse(path.read_text(encoding="utf-8")).body:
        if isinstance(node, ast.Assign) and any(getattr(t, "id", None) == "SUPPORT_OFFSETS" for t in node.targets):
            result = {}
            for key, value in zip(node.value.keys, node.value.values):
                if isinstance(value, ast.Call):  # np.array([...])
                    value = value.args[0]
                result[ast.literal_eval(key)] = tuple(float(v) for v in ast.literal_eval(value))
            return result
    raise AssertionError(f"{path} defines no SUPPORT_OFFSETS")


class RenderRifleGripTests(unittest.TestCase):
    def test_preview_support_palm_matches_the_shipped_hold_banks(self):
        generator = ROOT / "SWLOR_Haks" / "tools" / "GenerateRifleHoldAnimations.py"
        if not generator.is_file():
            self.skipTest("SWLOR_Haks is not checked out")
        self.assertEqual(support_offsets(generator), support_offsets(ROOT / "tools" / "RenderRifleGrip.py"))


if __name__ == "__main__":
    unittest.main()
