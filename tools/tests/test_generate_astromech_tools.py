"""Guards the generated Astromech tool files: tools/GenerateAstromechTools.py --check must find nothing to change."""
import subprocess
import sys
import unittest
from pathlib import Path

TOOL = Path(__file__).resolve().parents[1] / "GenerateAstromechTools.py"


class GeneratedToolsTests(unittest.TestCase):
    def test_generated_files_match_the_generator(self):
        result = subprocess.run([sys.executable, str(TOOL), "--check"], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
