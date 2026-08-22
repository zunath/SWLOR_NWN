using System.Numerics;

namespace SWLOR.Toolset.Domain.Render.Icons
{
    /// <summary>
    /// A tiny anti-aliased BGRA drawing surface: enough polygons, discs and strokes to draw a symbol,
    /// and nothing more.
    /// </summary>
    /// <remarks>
    /// Domain has no drawing stack and must not take one - it is referenced by headless tests and by the
    /// server project. The shapes drawn on this are a fixed, small set (one symbol per blueprint type,
    /// rendered once and shared by every tile that needs it), so a straightforward supersampled scanline
    /// fill costs nothing worth optimising and keeps the whole thing testable.
    /// </remarks>
    public sealed class IconCanvas
    {
        /// <summary>Samples per axis inside each pixel. 3x3 is enough to hide the staircase at tile sizes.</summary>
        private const int SamplesPerAxis = 3;

        private readonly int _width;
        private readonly int _height;
        private readonly byte[] _pixels;

        public IconCanvas(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _width = width;
            _height = height;
            _pixels = new byte[width * height * IconImage.BytesPerPixel];
        }

        public IconImage ToImage() => new(_width, _height, _pixels);

        /// <summary>Fills a closed polygon (even-odd rule) in straight-alpha BGRA <paramref name="color"/>.</summary>
        public void FillPolygon(IReadOnlyList<Vector2> points, uint color)
        {
            ArgumentNullException.ThrowIfNull(points);
            if (points.Count < 3)
                return;

            var minX = Math.Max(0, (int)MathF.Floor(points.Min(p => p.X)));
            var maxX = Math.Min(_width - 1, (int)MathF.Ceiling(points.Max(p => p.X)));
            var minY = Math.Max(0, (int)MathF.Floor(points.Min(p => p.Y)));
            var maxY = Math.Min(_height - 1, (int)MathF.Ceiling(points.Max(p => p.Y)));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var coverage = Coverage(x, y, point => Contains(points, point));
                    if (coverage > 0f)
                        Blend(x, y, color, coverage);
                }
            }
        }

        /// <summary>Fills a disc, used for round joints and for shapes that are just dots.</summary>
        public void FillCircle(Vector2 center, float radius, uint color)
        {
            if (radius <= 0f)
                return;

            var minX = Math.Max(0, (int)MathF.Floor(center.X - radius));
            var maxX = Math.Min(_width - 1, (int)MathF.Ceiling(center.X + radius));
            var minY = Math.Max(0, (int)MathF.Floor(center.Y - radius));
            var maxY = Math.Min(_height - 1, (int)MathF.Ceiling(center.Y + radius));
            var radiusSquared = radius * radius;

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var coverage = Coverage(x, y, point => (point - center).LengthSquared() <= radiusSquared);
                    if (coverage > 0f)
                        Blend(x, y, color, coverage);
                }
            }
        }

        /// <summary>Fills an axis-aligned ellipse - the flat top of a stacked-coin or a speaker cone.</summary>
        public void FillEllipse(Vector2 center, float radiusX, float radiusY, uint color)
        {
            if (radiusX <= 0f || radiusY <= 0f)
                return;

            var minX = Math.Max(0, (int)MathF.Floor(center.X - radiusX));
            var maxX = Math.Min(_width - 1, (int)MathF.Ceiling(center.X + radiusX));
            var minY = Math.Max(0, (int)MathF.Floor(center.Y - radiusY));
            var maxY = Math.Min(_height - 1, (int)MathF.Ceiling(center.Y + radiusY));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var coverage = Coverage(x, y, point =>
                    {
                        var dx = (point.X - center.X) / radiusX;
                        var dy = (point.Y - center.Y) / radiusY;
                        return dx * dx + dy * dy <= 1f;
                    });

                    if (coverage > 0f)
                        Blend(x, y, color, coverage);
                }
            }
        }

        /// <summary>Strokes a line as a rectangle plus round caps, so joints in a polyline meet cleanly.</summary>
        public void StrokeLine(Vector2 from, Vector2 to, float thickness, uint color)
        {
            var direction = to - from;
            var length = direction.Length();
            var half = thickness / 2f;

            if (length < 1e-4f)
            {
                FillCircle(from, half, color);
                return;
            }

            var normal = new Vector2(-direction.Y, direction.X) / length * half;
            FillPolygon(new[] { from + normal, to + normal, to - normal, from - normal }, color);
            FillCircle(from, half, color);
            FillCircle(to, half, color);
        }

        /// <summary>Strokes a polyline, optionally closing it back to the first point.</summary>
        public void StrokePath(IReadOnlyList<Vector2> points, float thickness, uint color, bool closed = false)
        {
            ArgumentNullException.ThrowIfNull(points);

            for (var i = 0; i + 1 < points.Count; i++)
                StrokeLine(points[i], points[i + 1], thickness, color);

            if (closed && points.Count > 2)
                StrokeLine(points[^1], points[0], thickness, color);
        }

        /// <summary>Fraction of the pixel's sample grid for which <paramref name="inside"/> holds.</summary>
        private static float Coverage(int x, int y, Func<Vector2, bool> inside)
        {
            var hits = 0;
            for (var sampleY = 0; sampleY < SamplesPerAxis; sampleY++)
            {
                for (var sampleX = 0; sampleX < SamplesPerAxis; sampleX++)
                {
                    var point = new Vector2(
                        x + (sampleX + 0.5f) / SamplesPerAxis,
                        y + (sampleY + 0.5f) / SamplesPerAxis);
                    if (inside(point))
                        hits++;
                }
            }

            return hits / (float)(SamplesPerAxis * SamplesPerAxis);
        }

        /// <summary>Even-odd crossing test against the polygon's edges.</summary>
        private static bool Contains(IReadOnlyList<Vector2> points, Vector2 point)
        {
            var insideShape = false;
            for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
            {
                var a = points[i];
                var b = points[j];
                if (point.Y < a.Y == point.Y < b.Y)
                    continue;

                if (point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                    insideShape = !insideShape;
            }

            return insideShape;
        }

        private void Blend(int x, int y, uint color, float coverage)
        {
            var sourceAlpha = ((color >> 24) & 0xFF) / 255f * Math.Clamp(coverage, 0f, 1f);
            if (sourceAlpha <= 0f)
                return;

            var offset = (y * _width + x) * IconImage.BytesPerPixel;
            var destinationAlpha = _pixels[offset + 3] / 255f;
            var outAlpha = sourceAlpha + destinationAlpha * (1f - sourceAlpha);
            if (outAlpha <= 0f)
                return;

            _pixels[offset] = Mix((byte)(color & 0xFF), _pixels[offset], sourceAlpha, destinationAlpha, outAlpha);
            _pixels[offset + 1] = Mix((byte)((color >> 8) & 0xFF), _pixels[offset + 1], sourceAlpha, destinationAlpha, outAlpha);
            _pixels[offset + 2] = Mix((byte)((color >> 16) & 0xFF), _pixels[offset + 2], sourceAlpha, destinationAlpha, outAlpha);
            _pixels[offset + 3] = (byte)Math.Clamp(MathF.Round(outAlpha * 255f), 0, 255);
        }

        private static byte Mix(byte source, byte destination, float sourceAlpha, float destinationAlpha, float outAlpha)
        {
            var value = (source * sourceAlpha + destination * destinationAlpha * (1f - sourceAlpha)) / outAlpha;
            return (byte)Math.Clamp(MathF.Round(value), 0, 255);
        }
    }
}
