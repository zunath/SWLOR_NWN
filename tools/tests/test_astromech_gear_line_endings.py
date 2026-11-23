"""The astromech gear generators work in a checkout of either line-ending form.

`.gitattributes` normalizes text files, so the blueprints, the store and the C# sources are LF on Linux and CRLF on
Windows. Both generators must find the committed content up to date in either form, and must write each file back in
the form the checkout holds.
"""
import importlib
import sys
import tempfile
import unittest
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))
modules = importlib.import_module("GenerateAstromechModules")
tools = importlib.import_module("GenerateAstromechTools")

PATH_NAMES = {
    modules: ("UTI", "SMITHERY", "RECIPES_CS", "RECIPE_TYPE_CS", "STORE"),
    tools: ("UTI", "RECIPES_CS", "RECIPE_TYPE_CS", "STORE"),
    modules.placed: ("GIT",),
}


def with_newline(data: bytes, newline: bytes) -> bytes:
    return data.replace(b"\r\n", b"\n").replace(b"\n", newline)


class GearGeneratorLineEndingTests(unittest.TestCase):
    def setUp(self):
        self.root = modules.ROOT
        self.saved = {(module, name): getattr(module, name) for module, names in PATH_NAMES.items() for name in names}

    def tearDown(self):
        for (module, name), value in self.saved.items():
            setattr(module, name, value)

    def checkout(self, target: Path, newline: bytes):
        """A copy of every file the generators read or write, in one line-ending form, with the tools pointed at it."""
        needed = set(modules.expected_outputs()) | set(tools.expected_outputs())
        needed |= {modules.UTI / f"{m.source}.uti.json" for m in modules.MODULES}
        needed.add(modules.UTI / f"{modules.STARTER_SOURCE}.uti.json")
        needed |= {tools.UTI / f"{t.source}.uti.json" for t in tools.TOOLS}
        needed |= set(modules.SMITHERY.glob("*.cs"))
        # The placed copy of the store, which both generators stock.
        needed |= {path for path in modules.placed.GIT.glob("*.git.json")
                   if b'"' + modules.STORE.name.split(".")[0].encode() + b'"' in path.read_bytes()}
        for path in needed:
            copy = target / path.relative_to(self.root)
            copy.parent.mkdir(parents=True, exist_ok=True)
            copy.write_bytes(with_newline(path.read_bytes(), newline))
        for (module, name), value in self.saved.items():
            setattr(module, name, target / value.relative_to(self.root))

    def assert_up_to_date(self, newline: bytes):
        with tempfile.TemporaryDirectory() as folder:
            self.checkout(Path(folder), newline)
            for generator in (modules, tools):
                outputs = generator.expected_outputs()
                self.assertTrue(outputs)
                stale = [path.name for path, content in outputs.items() if path.read_bytes() != content]
                self.assertEqual([], stale, f"{generator.__name__} with {newline!r} line endings")
                for path, content in outputs.items():
                    # No line of the output may end the other way.
                    foreign = content.replace(b"\r\n", b"") if newline == b"\r\n" else content
                    self.assertNotIn(b"\n" if newline == b"\r\n" else b"\r\n", foreign,
                                     f"{path.name} keeps the checkout's line endings")

    def test_an_lf_checkout_is_up_to_date(self):
        self.assert_up_to_date(b"\n")

    def test_a_crlf_checkout_is_up_to_date(self):
        self.assert_up_to_date(b"\r\n")

    def test_a_new_file_takes_the_line_endings_of_the_checkout(self):
        with tempfile.TemporaryDirectory() as folder:
            self.checkout(Path(folder), b"\n")
            first = modules.UTI / f"{modules.MODULES[0].resref}.uti.json"
            first.unlink()
            content = modules.expected_outputs()[first]
            self.assertNotIn(b"\r\n", content)
            self.assertIn(b"\n", content)


if __name__ == "__main__":
    unittest.main()
