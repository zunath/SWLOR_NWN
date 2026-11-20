"""Guards the generated Astromech module files: tools/GenerateAstromechModules.py --check must find nothing to change."""
import subprocess
import sys
import unittest
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
TOOL = TOOLS / "GenerateAstromechModules.py"
sys.path.insert(0, str(TOOLS))
import GenerateAstromechModules as mod  # noqa: E402


def blueprint(identified: str, unidentified: str) -> str:
    def field(name: str, text: str) -> str:
        value = f'{{\r\n      "0": "{text}"\r\n    }}' if text else "{}"
        return f'  "{name}": {{\r\n    "type": "cexolocstring",\r\n    "value": {value}\r\n  }},\r\n'
    return "{\r\n" + field("DescIdentified", identified) + field("Description", unidentified) + '  "Identified": 1\r\n}'


class DescriptionTests(unittest.TestCase):
    def test_a_form_takes_its_own_description_in_both_fields(self):
        text = mod.described(blueprint("A tunic.", r"Some \"old\" tunic."), "piece", "Plating for a chassis.")
        self.assertEqual(text, blueprint("Plating for a chassis.", "Plating for a chassis."))

    def test_a_piece_described_only_when_unidentified_still_gives_its_form_one(self):
        text = mod.described(blueprint("", "A tunic."), "piece", "Plating for a chassis.")
        self.assertEqual(text, blueprint("Plating for a chassis.", "Plating for a chassis."))

    def test_a_piece_with_no_description_gives_its_form_none(self):
        self.assertEqual(mod.described(blueprint("", ""), "no_such_piece"), blueprint("", ""))

    def test_a_described_piece_with_no_written_description_stops_the_generator(self):
        with self.assertRaises(SystemExit):
            mod.described(blueprint("A tunic.", "A tunic."), "no_such_piece")

    def test_a_description_must_be_plain_ascii_on_one_line(self):
        for bad in ("A ’curly’ quote.", 'A "quoted" word.', "Two\nlines."):
            with self.assertRaises(SystemExit):
                mod.described(blueprint("A tunic.", "A tunic."), "piece", bad)

    def test_every_written_description_belongs_to_a_piece_with_a_form(self):
        import GenerateAstromechRewards as rewards
        forms = dict(rewards.all_forms())
        self.assertFalse(sorted(set(mod.descriptions()) - set(forms)))
        self.assertGreater(len(mod.descriptions()), 400)


class GeneratedModulesTests(unittest.TestCase):
    def test_generated_files_match_the_generator(self):
        result = subprocess.run([sys.executable, str(TOOL), "--check"], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
