using System.Numerics;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Render.Icons
{
    /// <summary>
    /// Draws the fallback symbol for a blueprint type - the picture a tile shows when there is genuinely
    /// nothing else to show, and the same symbol the type selector labels its buttons with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three kinds of blueprint reach this. Merchants, triggers, sound sets and waypoints have no model
    /// anywhere in the NWN format, so Aurora shows a fixed symbol for them too. Placeables and doors
    /// whose appearance row is blank in the 2DA - 2,911 of the SWLOR module's 8,355 placeables, mostly
    /// content whose 2DA rows were dropped - have a model name to look up and nothing behind it. And a
    /// handful of appearances point deliberately at invisible models.
    /// </para>
    /// <para>
    /// A symbol is the honest answer for all three, and it is a far better one than a letter: the grid
    /// stays even, a builder can see at a glance which kind of thing each tile is, and nothing pretends
    /// to be artwork it is not. One image per type is drawn once and shared by every tile that needs it.
    /// </para>
    /// <para>
    /// Small sizes are drawn, not scaled down. A 128px symbol resampled to 18px loses exactly the parts
    /// that carry its meaning, so the caller asks for the size it wants and the renderer picks the detail
    /// level to match - see <see cref="TypeIconDetail"/>.
    /// </para>
    /// </remarks>
    public static class TypeIconRenderer
    {
        /// <summary>Sizes below this are drawn simplified; a symbol needs roughly 32px for its fine detail.</summary>
        public const int CompactSizeThreshold = 32;

        /// <summary>Fraction of the canvas left clear around the symbol.</summary>
        private const float Margin = 0.14f;

        /// <summary>Chip margin. A button has no pixels to donate to whitespace the tile can afford.</summary>
        private const float CompactMargin = 0.06f;

        /// <summary>The detail level <see cref="Render"/> uses for <paramref name="size"/>.</summary>
        public static TypeIconDetail DetailFor(int size) =>
            size < CompactSizeThreshold ? TypeIconDetail.Compact : TypeIconDetail.Full;

        /// <summary>Draws the symbol for <paramref name="type"/> at <paramref name="size"/> squared.</summary>
        public static IconImage Render(ResourceType type, int size, TypeIconPalette? palette = null)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            palette ??= TypeIconPalette.Default;
            var detail = DetailFor(size);
            var canvas = new IconCanvas(size, size);
            var margin = detail == TypeIconDetail.Compact ? CompactMargin : Margin;

            // A chip's strokes are held to two pixels or so: thinner and the anti-aliasing greys them out
            // until the mark is there in the pixel data but invisible on screen.
            var stroke = detail == TypeIconDetail.Compact
                ? MathF.Max(1.7f, size * 0.095f)
                : MathF.Max(1.2f, size * 0.035f);

            Vector2 P(float x, float y) => new(
                (margin + x * (1f - margin * 2f)) * size,
                (margin + y * (1f - margin * 2f)) * size);

            float S(float value) => value * size * (1f - margin * 2f);

            switch (type)
            {
                case ResourceType.Utp:
                    DrawCrate(canvas, palette, P, detail);
                    break;
                case ResourceType.Utc:
                    DrawFigure(canvas, palette, P, S, detail);
                    break;
                case ResourceType.Utd:
                    DrawDoor(canvas, palette, P, S, stroke, detail);
                    break;
                case ResourceType.Uti:
                    DrawSatchel(canvas, palette, P, stroke, detail);
                    break;
                case ResourceType.Utm:
                    DrawCoins(canvas, palette, P, S, detail);
                    break;
                case ResourceType.Utt:
                    DrawTriggerArea(canvas, palette, P, stroke, detail);
                    break;
                case ResourceType.Uts:
                    DrawSpeaker(canvas, palette, P, stroke, detail);
                    break;
                case ResourceType.Utw:
                    DrawFlag(canvas, palette, P, S, stroke, detail);
                    break;
                default:
                    DrawGenericPlate(canvas, palette, P, stroke);
                    break;
            }

            return canvas.ToImage();
        }

        /// <summary>An isometric box: the one shape that reads as "a thing placed in the world".</summary>
        private static void DrawCrate(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, TypeIconDetail detail)
        {
            // The three faces are told apart by tone alone, and a few pixels of each is not enough for the
            // tile's gentle 0.78/1.25 split to register. Push them apart at chip size.
            var compact = detail == TypeIconDetail.Compact;
            var left = TypeIconPalette.Shade(palette.Fill, compact ? 0.6f : 0.78f);
            var right = TypeIconPalette.Shade(palette.Fill, compact ? 1.6f : 1.25f);

            canvas.FillPolygon(new[] { p(0.50f, 0.02f), p(0.98f, 0.26f), p(0.50f, 0.50f), p(0.02f, 0.26f) }, palette.Stroke);
            canvas.FillPolygon(new[] { p(0.02f, 0.26f), p(0.50f, 0.50f), p(0.50f, 0.98f), p(0.02f, 0.74f) }, left);
            canvas.FillPolygon(new[] { p(0.98f, 0.26f), p(0.98f, 0.74f), p(0.50f, 0.98f), p(0.50f, 0.50f) }, right);
        }

        /// <summary>Head and shoulders. A full body at this size is a smudge; a bust is unmistakable.</summary>
        private static void DrawFigure(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, Func<float, float> s,
            TypeIconDetail detail)
        {
            // A head that shares pixels with the shoulders reads as one blob, so the chip's head is both
            // larger and lifted clear of the neckline.
            var compact = detail == TypeIconDetail.Compact;
            canvas.FillCircle(p(0.50f, compact ? 0.19f : 0.20f), s(compact ? 0.22f : 0.19f), palette.Stroke);
            canvas.FillPolygon(
                compact
                    ? new[] { p(0.04f, 1.00f), p(0.12f, 0.70f), p(0.30f, 0.56f), p(0.70f, 0.56f), p(0.88f, 0.70f), p(0.96f, 1.00f) }
                    : new[] { p(0.08f, 1.00f), p(0.14f, 0.66f), p(0.30f, 0.51f), p(0.70f, 0.51f), p(0.86f, 0.66f), p(0.92f, 1.00f) },
                palette.Fill);
        }

        /// <summary>An arched door leaf with its handle, outlined so it reads against a dark tile.</summary>
        private static void DrawDoor(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, Func<float, float> s, float stroke,
            TypeIconDetail detail)
        {
            // The outline is what carries the leaf against a dark tile, but at chip size it also swallows
            // the arch - a stroke laid along the leaf's top edge is exactly as thick as the curve above it,
            // and the result reads as a plain framed rectangle. So the chip drops the outline and draws the
            // doorway as one solid ink silhouette, with the handle punched out of it in the darker tone.
            var compact = detail == TypeIconDetail.Compact;
            var shoulder = compact ? 0.42f : 0.32f;
            var body = compact ? palette.Stroke : palette.Fill;
            var leaf = new[] { p(0.20f, shoulder), p(0.80f, shoulder), p(0.80f, 1.00f), p(0.20f, 1.00f) };

            canvas.FillEllipse(p(0.50f, shoulder), s(0.30f), s(compact ? 0.36f : 0.26f), body);
            canvas.FillPolygon(leaf, body);
            if (!compact)
                canvas.StrokePath(leaf, stroke, palette.Stroke);

            canvas.FillCircle(
                p(compact ? 0.64f : 0.68f, compact ? 0.74f : 0.68f),
                s(compact ? 0.11f : 0.055f),
                compact ? TypeIconPalette.Shade(palette.Fill, 0.55f) : palette.Stroke);
        }

        /// <summary>A satchel: the generic "carried thing" for items with no icon artwork at all.</summary>
        private static void DrawSatchel(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, float stroke,
            TypeIconDetail detail)
        {
            // The handle's five-point curve becomes a two-pixel scribble at chip size; a plain arch over
            // the body keeps the "bag with a strap" silhouette with strokes wide enough to see.
            canvas.StrokePath(
                detail == TypeIconDetail.Compact
                    ? new[] { p(0.28f, 0.44f), p(0.28f, 0.16f), p(0.72f, 0.16f), p(0.72f, 0.44f) }
                    : new[] { p(0.30f, 0.44f), p(0.32f, 0.20f), p(0.50f, 0.10f), p(0.68f, 0.20f), p(0.70f, 0.44f) },
                stroke, palette.Stroke);

            canvas.FillPolygon(new[] { p(0.14f, 0.42f), p(0.86f, 0.42f), p(0.94f, 1.00f), p(0.06f, 1.00f) }, palette.Fill);
            canvas.FillPolygon(
                detail == TypeIconDetail.Compact
                    ? new[] { p(0.14f, 0.42f), p(0.86f, 0.42f), p(0.83f, 0.66f), p(0.17f, 0.66f) }
                    : new[] { p(0.14f, 0.42f), p(0.86f, 0.42f), p(0.82f, 0.60f), p(0.18f, 0.60f) },
                TypeIconPalette.Shade(palette.Fill, 1.35f));
        }

        /// <summary>Stacked coins. Commerce is the one thing a merchant blueprint always means.</summary>
        private static void DrawCoins(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, Func<float, float> s,
            TypeIconDetail detail)
        {
            // Three coins need three faces and two gaps - around nine pixels of vertical detail, which a
            // chip does not have. Two taller coins keep the "stack" reading and still leave each face a
            // couple of pixels of bright ellipse showing below the coin above it.
            var compact = detail == TypeIconDetail.Compact;
            var tops = compact ? new[] { 0.56f, 0.14f } : new[] { 0.80f, 0.53f, 0.26f };
            var height = compact ? 0.26f : 0.14f;
            var radiusY = compact ? 0.15f : 0.11f;
            var rim = TypeIconPalette.Shade(palette.Fill, 0.8f);

            // Bottom coin first, so each one overlaps the one below it.
            foreach (var y in tops)
            {
                canvas.FillPolygon(new[] { p(0.08f, y), p(0.92f, y), p(0.92f, y + height), p(0.08f, y + height) }, rim);
                canvas.FillEllipse(p(0.50f, y + height), s(0.42f), s(radiusY), rim);
                canvas.FillEllipse(p(0.50f, y), s(0.42f), s(radiusY), palette.Stroke);
            }
        }

        /// <summary>A dashed ground boundary - a trigger is an area, and the dashes say "not solid".</summary>
        private static void DrawTriggerArea(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, float stroke,
            TypeIconDetail detail)
        {
            var corners = new[] { p(0.06f, 0.24f), p(0.94f, 0.06f), p(1.00f, 0.80f), p(0.14f, 1.00f) };
            canvas.FillPolygon(corners, TypeIconPalette.Shade(palette.Fill, 0.6f) & 0x66FFFFFF);

            if (detail == TypeIconDetail.Compact)
            {
                // Dashes shorter than a pixel average out to a faint grey line, which loses both the dashes
                // and the outline. A solid ink boundary keeps the skewed patch of ground readable instead.
                canvas.StrokePath(corners, stroke, palette.Stroke, closed: true);
                return;
            }

            const int dashesPerEdge = 4;
            for (var edge = 0; edge < corners.Length; edge++)
            {
                var from = corners[edge];
                var to = corners[(edge + 1) % corners.Length];
                for (var dash = 0; dash < dashesPerEdge; dash++)
                {
                    // Two thirds on, one third off, so the corners always land on a mark.
                    var start = dash / (float)dashesPerEdge;
                    var end = start + 0.66f / dashesPerEdge;
                    canvas.StrokeLine(
                        Vector2.Lerp(from, to, start), Vector2.Lerp(from, to, end), stroke, palette.Detail);
                }
            }
        }

        /// <summary>A speaker cone with two waves, for sound sets.</summary>
        private static void DrawSpeaker(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, float stroke,
            TypeIconDetail detail)
        {
            var compact = detail == TypeIconDetail.Compact;
            canvas.FillPolygon(
                compact
                    ? new[]
                    {
                        p(0.00f, 0.34f), p(0.18f, 0.34f), p(0.44f, 0.04f),
                        p(0.44f, 0.96f), p(0.18f, 0.66f), p(0.00f, 0.66f)
                    }
                    : new[]
                    {
                        p(0.04f, 0.36f), p(0.22f, 0.36f), p(0.46f, 0.10f),
                        p(0.46f, 0.90f), p(0.22f, 0.64f), p(0.04f, 0.64f)
                    },
                palette.Stroke);

            // Two waves a pixel apart merge into a blob, so the chip keeps one - drawn in ink and pushed
            // out to the edge, where it stays clearly separate from the cone.
            foreach (var radius in compact ? new[] { 0.34f } : new[] { 0.20f, 0.38f })
            {
                var arc = new List<Vector2>();
                for (var step = 0; step <= 6; step++)
                {
                    var angle = -MathF.PI / 3f + step * (2f * MathF.PI / 3f / 6f);
                    arc.Add(p(0.56f + radius * MathF.Cos(angle), 0.50f + radius * MathF.Sin(angle) * 1.25f));
                }

                canvas.StrokePath(arc, stroke, compact ? palette.Stroke : palette.Detail);
            }
        }

        /// <summary>A pennant on a pole with a ground disc, for waypoints.</summary>
        private static void DrawFlag(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, Func<float, float> s, float stroke,
            TypeIconDetail detail)
        {
            // The ground disc is a one-pixel smear at chip size and only muddies the foot of the pole, so
            // the chip drops it and gives the pennant the room instead.
            var compact = detail == TypeIconDetail.Compact;
            if (!compact)
                canvas.FillEllipse(p(0.30f, 0.96f), s(0.22f), s(0.06f), TypeIconPalette.Shade(palette.Fill, 0.8f));

            // A pole and a thin pennant also make the sparsest symbol of the set, which reads as a weaker
            // button than its neighbours, so the chip's pennant is deepened to even the weight out.
            canvas.StrokeLine(
                p(compact ? 0.22f : 0.30f, compact ? 0.02f : 0.04f),
                p(compact ? 0.22f : 0.30f, compact ? 0.98f : 0.96f),
                stroke, palette.Stroke);
            canvas.FillPolygon(
                compact
                    ? new[] { p(0.26f, 0.04f), p(1.00f, 0.31f), p(0.26f, 0.58f) }
                    : new[] { p(0.33f, 0.06f), p(0.94f, 0.26f), p(0.33f, 0.46f) },
                palette.Fill);
        }

        /// <summary>Outlined plate for any type without a symbol of its own.</summary>
        private static void DrawGenericPlate(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, float stroke)
        {
            var plate = new[] { p(0.06f, 0.14f), p(0.94f, 0.14f), p(0.94f, 0.86f), p(0.06f, 0.86f) };
            canvas.FillPolygon(plate, palette.Fill);
            canvas.StrokePath(plate, stroke, palette.Stroke, closed: true);
        }
    }
}
