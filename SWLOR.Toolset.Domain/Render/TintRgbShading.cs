using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// The tint shaders' exact-RGB shading (the RGB block of <c>fs_plt_tinter</c> and the other
    /// tint shaders), ported from <c>SWLOR_Haks/tools/TintRgbShading.py</c>.
    /// </summary>
    public static class TintRgbShading
    {
        private const double LuminanceRed = 0.2126;
        private const double LuminanceGreen = 0.7152;
        private const double LuminanceBlue = 0.0722;

        // Orthonormal basis of the chroma plane (orthogonal to the luminance weights), as the shaders write it.
        private static readonly (double R, double G, double B) ChromaU = (0.675495915, -0.127481049, -0.726260181);
        private static readonly (double R, double G, double B) ChromaV = (-0.680638933, 0.271037427, -0.680638933);

        // Squared midtone chroma (8 levels) at which a swatch's hue counts half.
        private const double ChromaConfidence = 0.000984237;

        // A swatch whose midtone luminance is below this lends the layer's first row's shading and coverage.
        private const double DarkSwatch = 2.0 / 255.0;

        // Colors this many levels from their swatch take none of a near-neutral swatch's leftover hue.
        private const double SwatchReach = 16.0;

        /// <summary>
        /// Shades a custom color across one palette row. The swatch lends its contrast (row luminance
        /// over midtone luminance) to the color's own luminance, and its change of hue and saturation
        /// from shade to shade relative to the color's own chroma, so a neutral color stays neutral on
        /// a colored swatch's row and a color equal to a swatch renders exactly like that row. A swatch
        /// darker than two levels (the black rows, skin row 31) is replaced by the layer's first row,
        /// its shading and coverage.
        /// </summary>
        /// <param name="color">The requested color.</param>
        /// <param name="palette">The decoded palette atlas, top-first RGBA.</param>
        /// <param name="swatchImageRow">Image row (top-first) of the color's nearest swatch.</param>
        /// <param name="firstImageRow">Image row (top-first) of the layer's first palette row.</param>
        /// <returns><c>palette.Width</c> RGBA pixels; alpha is the row's environment coverage.</returns>
        public static byte[] ShadeRow(TintMapColor color, TextureImage palette, int swatchImageRow, int firstImageRow)
        {
            var width = palette.Width;
            var pixels = palette.Pixels;
            var midpointX = 128 * (width - 1) / 255;

            var rowStart = swatchImageRow * width * 4;
            var midpoint = Read(pixels, rowStart + midpointX * 4);
            if (Luminance(midpoint) < DarkSwatch)
            {
                rowStart = firstImageRow * width * 4;
                midpoint = Read(pixels, rowStart + midpointX * 4);
            }

            var target = (R: color.Red / 255.0, G: color.Green / 255.0, B: color.Blue / 255.0);
            var rampLuminance = Math.Max(Luminance(midpoint), 1.0 / 255.0);
            var colorChroma = Chroma(target);
            var midpointChroma = Chroma(midpoint);
            var midpointSquared = midpointChroma.U * midpointChroma.U + midpointChroma.V * midpointChroma.V;
            var weight = 1.0 / (midpointSquared + ChromaConfidence);
            var confidence = midpointSquared * weight;
            var relativeU = (colorChroma.U * midpointChroma.U + colorChroma.V * midpointChroma.V) * weight;
            var relativeV = (colorChroma.V * midpointChroma.U - colorChroma.U * midpointChroma.V) * weight;
            var distance = Math.Sqrt(
                Square(target.R - midpoint.R) + Square(target.G - midpoint.G) + Square(target.B - midpoint.B));
            var near = Math.Clamp(1.0 - distance * 255.0 / SwatchReach, 0.0, 1.0);
            var luminance = Luminance(target);

            var output = new byte[width * 4];
            for (var x = 0; x < width; x++)
            {
                var offset = rowStart + x * 4;
                var shade = Read(pixels, offset);
                var scale = Math.Max(Luminance(shade) / rampLuminance, 0.0);
                var shadeChroma = Chroma(shade);
                var leftover = (1.0 - confidence) * near;
                var u = colorChroma.U * scale * (1.0 - confidence)
                        + shadeChroma.U * relativeU - shadeChroma.V * relativeV
                        + (shadeChroma.U - midpointChroma.U * scale) * leftover;
                var v = colorChroma.V * scale * (1.0 - confidence)
                        + shadeChroma.U * relativeV + shadeChroma.V * relativeU
                        + (shadeChroma.V - midpointChroma.V * scale) * leftover;
                var grey = luminance * scale;
                output[x * 4] = ToByte(grey + u * ChromaU.R + v * ChromaV.R);
                output[x * 4 + 1] = ToByte(grey + u * ChromaU.G + v * ChromaV.G);
                output[x * 4 + 2] = ToByte(grey + u * ChromaU.B + v * ChromaV.B);
                output[x * 4 + 3] = pixels[offset + 3];
            }

            return output;
        }

        private static (double R, double G, double B) Read(byte[] pixels, int offset) =>
            (pixels[offset] / 255.0, pixels[offset + 1] / 255.0, pixels[offset + 2] / 255.0);

        private static double Luminance((double R, double G, double B) color) =>
            color.R * LuminanceRed + color.G * LuminanceGreen + color.B * LuminanceBlue;

        private static (double U, double V) Chroma((double R, double G, double B) color)
        {
            var grey = Luminance(color);
            var (r, g, b) = (color.R - grey, color.G - grey, color.B - grey);
            return (r * ChromaU.R + g * ChromaU.G + b * ChromaU.B, r * ChromaV.R + g * ChromaV.G + b * ChromaV.B);
        }

        private static double Square(double value) => value * value;

        private static byte ToByte(double value) =>
            (byte)Math.Clamp(Math.Round(Math.Clamp(value, 0.0, 1.0) * 255.0), 0, 255);
    }
}
