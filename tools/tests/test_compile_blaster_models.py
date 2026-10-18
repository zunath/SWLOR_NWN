"""Publication guards for staged model compilation."""
from pathlib import Path
import struct
import re
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import CompileBlasterModels as compiler


class ModelCompilationTests(unittest.TestCase):
    def test_failed_batch_publishes_no_models_and_keeps_log(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in ("first", "second"):
                (root / (name + ".mdl")).write_text("newmodel " + name)
            output = root / "output"
            def compile_batch(executable, staging, command, models, binary, log, resources):
                (staging / log).write_text("batch diagnostics")
                (binary / "first.mdl").write_bytes(struct.pack("<III", 0, 232, 0) + bytes(232))
                (binary / "second.mdl").write_bytes(b"truncated")
            with patch.object(compiler.CleanModels, "find_compiler", return_value=Path("compiler.exe")), \
                 patch.object(compiler.CleanModels, "run", side_effect=compile_batch), \
                 self.assertRaisesRegex(ValueError, "Truncated"):
                compiler.compile_models(None, root, output, [])
            self.assertEqual([], list(output.glob("*.mdl")))
            self.assertEqual("batch diagnostics", (output / "compile.log").read_text())

    def test_compiler_error_points_to_the_retained_output_log(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "sample.mdl").write_text("newmodel sample")
            output = root / "output"
            def fail(executable, staging, command, models, binary, log, resources):
                (staging / log).write_text("compiler failure diagnostics")
                raise RuntimeError(f"compiler error; see {staging / log}")
            with patch.object(compiler.CleanModels, "find_compiler", return_value=Path("compiler.exe")), \
                 patch.object(compiler.CleanModels, "run", side_effect=fail), \
                 self.assertRaisesRegex(RuntimeError, re.escape(str(output / "compile.log"))):
                compiler.compile_models(None, root, output, ["sample"])
            self.assertEqual("compiler failure diagnostics", (output / "compile.log").read_text())
            self.assertEqual([], list(output.glob("*.mdl")))

    def test_existing_output_and_invalid_resrefs_are_rejected_before_compilation(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "sample.mdl").write_text("newmodel sample")
            output = root / "output"
            output.mkdir()
            (output / "sample.mdl").write_bytes(b"keep")
            with patch.object(compiler.CleanModels, "find_compiler", return_value=Path("compiler.exe")), \
                 patch.object(compiler.CleanModels, "run") as run:
                with self.assertRaises(FileExistsError):
                    compiler.compile_models(None, root, output, ["sample"])
                with self.assertRaises(ValueError):
                    compiler.compile_models(None, root, output, ["../sample"])
                run.assert_not_called()
            self.assertEqual(b"keep", (output / "sample.mdl").read_bytes())


if __name__ == "__main__":
    unittest.main()
