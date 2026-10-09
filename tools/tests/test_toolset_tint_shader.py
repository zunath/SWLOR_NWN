"""Draw the actual Toolset tint shader against the shipped palette on Windows.

Requires initialized HAK sources and an OpenGL driver. Reuses the hidden WGL
test context; no Toolset window or game process is launched.
Run: python -B -m unittest discover -s tools/tests -p test_toolset_tint_shader.py
"""
import ctypes as ct
import json
from pathlib import Path
import re
import struct
import sys
import unittest


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "SWLOR_Haks/tools"))
from TestTintShaderMaterials import MaterialTest, OpenGL


@unittest.skipUnless(sys.platform == "win32", "Requires the Windows WGL test context")
class ToolsetTintShaderTests(unittest.TestCase):
    def setUp(self):
        source = (ROOT / "SWLOR.Toolset/Viewport/GlAreaControl.cs").read_text()
        def body(name):
            return re.search(r'private const string ' + name + r' = @"((?:[^"]|"")*)";',
                             source).group(1).replace('""', '"')
        self.vertex = body("VertexShaderBody")
        self.fragment = body("FragmentShaderBody")
        self.prefixes = [json.loads('"' + re.search(
            r'private const string ' + name + r' = "((?:\\.|[^"\\])*)";', source).group(1) + '"')
            for name in ("VersionDesktop", "VersionEs")]
        atlas = (ROOT / "SWLOR_Haks/sw_item/plt_palette.tga").read_bytes()
        self.assertEqual(struct.unpack_from("<HH", atlas, 12), (256, 2048))
        self.assertEqual(atlas[16:18], bytes((32, 8)), "Expected bottom-first BGRA palette")
        self.palette_bytes = atlas[18:]
        self.gl = OpenGL()
        self.addCleanup(self.gl.close)
        self.test = MaterialTest(self.gl)
        self.uniform3 = self.gl.function("glUniform3f", None, ct.c_int, ct.c_float, ct.c_float, ct.c_float)
        self.matrix = self.gl.function("glUniformMatrix4fv", None, ct.c_int, ct.c_int, ct.c_ubyte, ct.c_void_p)
        self.attribute = self.gl.function("glGetAttribLocation", ct.c_int, ct.c_uint, ct.c_char_p)
        self.pointer = self.gl.function("glVertexAttribPointer", None, ct.c_uint, ct.c_int, ct.c_uint,
                                        ct.c_ubyte, ct.c_int, ct.c_void_p)
        self.enable = self.gl.function("glEnableVertexAttribArray", None, ct.c_uint)
        self.disable = self.gl.function("glDisableVertexAttribArray", None, ct.c_uint)
        self.draw = self.gl.function("glDrawArrays", None, ct.c_uint, ct.c_int, ct.c_int)
        self.identity = (ct.c_float * 16)(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)
        self.geometry = (("aPosition", 3, (ct.c_float * 9)(-1, -1, 0, 3, -1, 0, -1, 3, 0)),
                         ("aNormal", 3, (ct.c_float * 9)(0, 0, 1, 0, 0, 1, 0, 0, 1)),
                         ("aTexCoord", 2, (ct.c_float * 6)(0, 0, 2, 0, 0, 2)))
        self.test.bind_texture(0x0DE1, 100)
        self.test.texture_image(0x0DE1, 0, 0x8814, 256, 10, 0, 0x1908, 0x1406, None)
        self.gl.function("glViewport", None, ct.c_int, ct.c_int, ct.c_int, ct.c_int)(0, 0, 256, 10)
        mask = (ct.c_float * (256 * 10 * 2))(*(value for layer in range(10) for shade in range(256)
            for value in (shade / 255, (layer + .5) / 10)))
        pixels = ct.create_string_buffer(self.palette_bytes)
        for unit, width, height, internal, external, kind, data in (
                (0, 1, 1, 0x8814, 0x1908, 0x1406, (ct.c_float * 4)(1, 1, 1, 1)),
                (1, 1, 1, 0x8814, 0x1908, 0x1406, (ct.c_float * 4)(.15, .7, .2, 1)),
                (2, 256, 10, 0x8230, 0x8227, 0x1406, mask),
                (3, 256, 2048, 0x8058, 0x80E1, 0x1401, pixels)):
            self.test.active_texture(0x84C0 + unit)
            self.test.bind_texture(0x0DE1, unit + 1)
            self.test.texture_image(0x0DE1, 0, internal, width, height, 0, external, kind, data)
            for parameter in (0x2800, 0x2801):
                self.test.texture_parameter(0x0DE1, parameter, 0x2600)
            self.test.texture_parameter(0x0DE1, 0x813D, 0)

    def palette(self, layer, color, shade):
        base = (0, 176, 352, 528, 704, 704, 880, 880, 1056, 1056)[layer]
        offset = ((base + color) * 256 + shade) * 4
        return tuple(self.palette_bytes[offset + channel] / 255 for channel in (2, 1, 0, 3))

    def expected(self, rgb, preset, lit=False):
        values = []
        weights = (.2126, .7152, .0722)
        for layer in range(10):
            midpoint = sum(value * weight for value, weight in zip(self.palette(layer, 0, 128), weights))
            for shade in range(256):
                if rgb is None:
                    color = self.palette(layer, preset, shade)
                else:
                    reference = self.palette(layer, 0, shade)
                    scale = sum(value * weight for value, weight in zip(reference, weights)) / max(midpoint, 1 / 255)
                    color = tuple(min(value / 255 * scale, 1) for value in rgb) + (
                        reference[3] if layer in (2, 3) else 1,)
                if lit:
                    values.extend(env * (1 - color[3]) + value * .8 * color[3]
                                  for env, value in zip((.15, .7, .2), color))
                    values.append(1)
                else:
                    values.extend(color)
        return values

    def render(self, program, rgb, preset):
        self.test.use(program)
        location = lambda name: self.test.location(program, name.encode())
        for name in ("model", "view", "projection"):
            self.matrix(location(name), 1, 0, self.identity)
        uniform2 = self.gl.function("glUniform2f", None, ct.c_int, ct.c_float, ct.c_float)
        uniform2(location("uvScale"), 1, 1)
        uniform2(location("uvOffset"), 0, 0)
        for name in ("hasTexture", "hasTintMap", "hasEnvironmentMap"):
            self.test.integer(location(name), 1)
        for name, value in {"lightDir": (0, 0, 1), "lightColor": (.5, .5, .5),
                            "ambientColor": (.3, .3, .3), "cameraPos": (0, 0, 10)}.items():
            self.uniform3(location(name), *value)
        self.test.scalar(location("flatAlpha"), 1)
        for name, unit in {"diffuseTexture": 0, "environmentTexture": 1,
                           "tintMapTexture": 2, "tintPaletteTexture": 3}.items():
            self.test.integer(location(name), unit)
        bases = (0, 176, 352, 528, 704, 704, 880, 880, 1056, 1056)
        for layer, base in enumerate(bases):
            self.test.vector(location(f"tintColor{layer}"),
                             *(tuple(value / 255 for value in rgb) + (1,) if rgb else (0, 0, 0, 0)))
            self.test.scalar(location(f"tintPaletteRow{layer}"), (base + preset + .5) / 2048)
        attributes = []
        try:
            for name, size, data in self.geometry:
                index = self.attribute(program, name.encode())
                if index >= 0:
                    self.enable(index)
                    self.pointer(index, size, 0x1406, 0, 0, data)
                    attributes.append(index)
            self.draw(0x0004, 0, 3)
            output = (ct.c_float * (256 * 10 * 4))()
            self.test.read(0, 0, 256, 10, 0x1908, 0x1406, output)
            self.assertEqual(self.test.error(), 0)
            return tuple(output)
        finally:
            for index in attributes:
                self.disable(index)

    def assert_pixels(self, actual, expected, prefix):
        difference = max(abs(a - b) for a, b in zip(actual, expected))
        # ES sampler results may use mediump precision despite highp arithmetic.
        # One 10-bit unit is still below one RGBA8 step; desktop stays float-exact.
        tolerance = 1 / 1024 if " es" in prefix else 1e-5
        self.assertLess(difference, tolerance, f"Maximum RGBA difference: {difference}")

    def test_rgb_and_preset_coverage_at_every_shade(self):
        # Expose the production color function before lighting so coverage and
        # albedo can be compared with the original palette independently.
        diagnostic = self.fragment.replace("void main()", "void RenderMain()") + (
            "\nvoid main() { FragColor = ResolveTintMapColor(); }\n")
        for prefix in self.prefixes:
            program = self.test.program(prefix + diagnostic, prefix + self.vertex)
            try:
                for rgb in (None, (205, 228, 197), (0, 0, 0), (255, 255, 255)):
                    for preset in (0, 175):
                        with self.subTest(version=prefix.splitlines()[0], rgb=rgb, preset=preset):
                            self.assert_pixels(self.render(program, rgb, preset), self.expected(rgb, preset), prefix)
            finally:
                self.test.delete_program(program)

    def test_lit_rgb_metal_reflects_without_normal_or_specular_maps(self):
        rgb = (205, 228, 197)
        for prefix in self.prefixes:
            program = self.test.program(prefix + self.fragment, prefix + self.vertex)
            try:
                self.assert_pixels(self.render(program, rgb, 175), self.expected(rgb, 175, lit=True), prefix)
            finally:
                self.test.delete_program(program)
            # Only remove the custom coverage repair; the actual lighting and
            # requested RGB remain intact, reproducing the former matte metal.
            legacy, count = re.subn(r"paletteColor\.a = [^;]+;", "paletteColor.a = 1.0;", self.fragment)
            self.assertEqual(count, 1)
            program = self.test.program(prefix + legacy, prefix + self.vertex)
            try:
                actual = self.render(program, rgb, 175)
                expected = self.expected(rgb, 175, lit=True)
                for layer in (2, 3):
                    offset = (layer * 256 + 128) * 4
                    self.assertGreater(max(abs(a - b) for a, b in zip(
                        actual[offset:offset + 3], expected[offset:offset + 3])), .02)
            finally:
                self.test.delete_program(program)


if __name__ == "__main__":
    unittest.main()
