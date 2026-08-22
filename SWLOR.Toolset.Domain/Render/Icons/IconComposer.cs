namespace SWLOR.Toolset.Domain.Render.Icons
{
    /// <summary>
    /// Turns decoded icon textures into a single tile image.
    /// </summary>
    /// <remarks>
    /// NWN's composite weapon icons are three separate textures meant to be drawn on top of one
    /// another, so compositing - not just conversion - is the job here. Layers are drawn in the order
    /// given, at the size of the largest one; a smaller layer is point-sampled up to fit, which is what
    /// pixel artwork wants and what the game does. Nothing is resampled otherwise: the output keeps the
    /// artwork's own resolution and the view scales it once, at draw time.
    /// </remarks>
    public static class IconComposer
    {
        /// <summary>
        /// Composites <paramref name="layers"/> bottom-first into one image, or returns null when the
        /// list is empty or every layer is degenerate.
        /// </summary>
        public static IconImage? Compose(IReadOnlyList<TextureImage> layers)
        {
            ArgumentNullException.ThrowIfNull(layers);

            var usable = layers.Where(IsUsable).ToList();
            if (usable.Count == 0)
                return null;

            var width = usable.Max(layer => layer.Width);
            var height = usable.Max(layer => layer.Height);
            var pixels = new byte[width * height * IconImage.BytesPerPixel];

            foreach (var layer in usable)
                DrawLayer(layer, pixels, width, height);

            return new IconImage(width, height, pixels);
        }

        private static bool IsUsable(TextureImage? layer) =>
            layer is { Width: > 0, Height: > 0 } &&
            layer.Pixels.Length >= layer.Width * layer.Height * 4;

        /// <summary>
        /// Source-over composite of one RGBA layer onto the BGRA canvas, point-sampling the source when
        /// its size differs from the canvas.
        /// </summary>
        private static void DrawLayer(TextureImage layer, byte[] canvas, int width, int height)
        {
            for (var y = 0; y < height; y++)
            {
                var sourceY = layer.Height == height ? y : y * layer.Height / height;
                for (var x = 0; x < width; x++)
                {
                    var sourceX = layer.Width == width ? x : x * layer.Width / width;
                    var source = (sourceY * layer.Width + sourceX) * 4;

                    var alpha = layer.Pixels[source + 3];
                    if (alpha == 0)
                        continue;

                    var target = (y * width + x) * IconImage.BytesPerPixel;
                    if (alpha == 255)
                    {
                        canvas[target] = layer.Pixels[source + 2];     // B
                        canvas[target + 1] = layer.Pixels[source + 1]; // G
                        canvas[target + 2] = layer.Pixels[source];     // R
                        canvas[target + 3] = 255;
                        continue;
                    }

                    Blend(canvas, target, layer.Pixels[source], layer.Pixels[source + 1], layer.Pixels[source + 2], alpha);
                }
            }
        }

        /// <summary>
        /// Straight-alpha source-over. Both operands are unpremultiplied, so the destination colour has
        /// to be re-derived from the composited alpha rather than simply lerped.
        /// </summary>
        private static void Blend(byte[] canvas, int target, byte r, byte g, byte b, byte alpha)
        {
            var sourceAlpha = alpha / 255f;
            var destinationAlpha = canvas[target + 3] / 255f;
            var outAlpha = sourceAlpha + destinationAlpha * (1f - sourceAlpha);
            if (outAlpha <= 0f)
                return;

            canvas[target] = Mix(b, canvas[target], sourceAlpha, destinationAlpha, outAlpha);
            canvas[target + 1] = Mix(g, canvas[target + 1], sourceAlpha, destinationAlpha, outAlpha);
            canvas[target + 2] = Mix(r, canvas[target + 2], sourceAlpha, destinationAlpha, outAlpha);
            canvas[target + 3] = (byte)Math.Clamp(MathF.Round(outAlpha * 255f), 0, 255);
        }

        private static byte Mix(byte source, byte destination, float sourceAlpha, float destinationAlpha, float outAlpha)
        {
            var value = (source * sourceAlpha + destination * destinationAlpha * (1f - sourceAlpha)) / outAlpha;
            return (byte)Math.Clamp(MathF.Round(value), 0, 255);
        }
    }
}
