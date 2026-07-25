using System.Numerics;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Render.Icons
{
    /// <summary>
    /// Draws the fallback symbol for a blueprint type - the picture a tile shows when there is genuinely
    /// nothing else to show.
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
    /// </remarks>
    public static class TypeIconRenderer
    {
        /// <summary>Fraction of the canvas left clear around the symbol.</summary>
        private const float Margin = 0.14f;

        /// <summary>Draws the symbol for <paramref name="type"/> at <paramref name="size"/> squared.</summary>
        public static IconImage Render(ResourceType type, int size, TypeIconPalette? palette = null)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            palette ??= TypeIconPalette.Default;
            var canvas = new IconCanvas(size, size);
            var stroke = MathF.Max(1.2f, size * 0.035f);

            Vector2 P(float x, float y) => new(
                (Margin + x * (1f - Margin * 2f)) * size,
                (Margin + y * (1f - Margin * 2f)) * size);

            float S(float value) => value * size * (1f - Margin * 2f);

            switch (type)
            {
                case ResourceType.Utp:
                    DrawCrate(canvas, palette, P);
                    break;
                case ResourceType.Utc:
                    DrawFigure(canvas, palette, P, S);
                    break;
                case ResourceType.Utd:
                    DrawDoor(canvas, palette, P, S, stroke);
                    break;
                case ResourceType.Uti:
                    DrawSatchel(canvas, palette, P, stroke);
                    break;
                case ResourceType.Utm:
                    DrawCoins(canvas, palette, P, S);
                    break;
                case ResourceType.Utt:
                    DrawTriggerArea(canvas, palette, P, stroke);
                    break;
                case ResourceType.Uts:
                    DrawSpeaker(canvas, palette, P, stroke);
                    break;
                case ResourceType.Utw:
                    DrawFlag(canvas, palette, P, S, stroke);
                    break;
                default:
                    DrawGenericPlate(canvas, palette, P, stroke);
                    break;
            }

            return canvas.ToImage();
        }

        /// <summary>An isometric box: the one shape that reads as "a thing placed in the world".</summary>
        private static void DrawCrate(IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p)
        {
            canvas.FillPolygon(new[] { p(0.50f, 0.02f), p(0.98f, 0.26f), p(0.50f, 0.50f), p(0.02f, 0.26f) }, palette.Stroke);
            canvas.FillPolygon(new[] { p(0.02f, 0.26f), p(0.50f, 0.50f), p(0.50f, 0.98f), p(0.02f, 0.74f) },
                TypeIconPalette.Shade(palette.Fill, 0.78f));
            canvas.FillPolygon(new[] { p(0.98f, 0.26f), p(0.98f, 0.74f), p(0.50f, 0.98f), p(0.50f, 0.50f) },
                TypeIconPalette.Shade(palette.Fill, 1.25f));
        }

        /// <summary>Head and shoulders. A full body at this size is a smudge; a bust is unmistakable.</summary>
        private static void DrawFigure(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, Func<float, float> s)
        {
            canvas.FillCircle(p(0.50f, 0.20f), s(0.19f), palette.Stroke);
            canvas.FillPolygon(
                new[]
                {
                    p(0.08f, 1.00f), p(0.14f, 0.66f), p(0.30f, 0.51f),
                    p(0.70f, 0.51f), p(0.86f, 0.66f), p(0.92f, 1.00f)
                },
                palette.Fill);
        }

        /// <summary>An arched door leaf with its handle, outlined so it reads against a dark tile.</summary>
        private static void DrawDoor(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, Func<float, float> s, float stroke)
        {
            var leaf = new[] { p(0.20f, 0.32f), p(0.80f, 0.32f), p(0.80f, 1.00f), p(0.20f, 1.00f) };
            canvas.FillEllipse(p(0.50f, 0.32f), s(0.30f), s(0.26f), palette.Fill);
            canvas.FillPolygon(leaf, palette.Fill);
            canvas.StrokePath(leaf, stroke, palette.Stroke);
            canvas.FillCircle(p(0.68f, 0.68f), s(0.055f), palette.Stroke);
        }

        /// <summary>A satchel: the generic "carried thing" for items with no icon artwork at all.</summary>
        private static void DrawSatchel(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, float stroke)
        {
            canvas.StrokePath(
                new[] { p(0.30f, 0.44f), p(0.32f, 0.20f), p(0.50f, 0.10f), p(0.68f, 0.20f), p(0.70f, 0.44f) },
                stroke, palette.Stroke);
            canvas.FillPolygon(new[] { p(0.14f, 0.42f), p(0.86f, 0.42f), p(0.94f, 1.00f), p(0.06f, 1.00f) }, palette.Fill);
            canvas.FillPolygon(new[] { p(0.14f, 0.42f), p(0.86f, 0.42f), p(0.82f, 0.60f), p(0.18f, 0.60f) },
                TypeIconPalette.Shade(palette.Fill, 1.35f));
        }

        /// <summary>Stacked coins. Commerce is the one thing a merchant blueprint always means.</summary>
        private static void DrawCoins(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, Func<float, float> s)
        {
            // Bottom coin first, so each one overlaps the one below it.
            foreach (var y in new[] { 0.80f, 0.53f, 0.26f })
            {
                canvas.FillPolygon(
                    new[] { p(0.08f, y), p(0.92f, y), p(0.92f, y + 0.14f), p(0.08f, y + 0.14f) },
                    TypeIconPalette.Shade(palette.Fill, 0.8f));
                canvas.FillEllipse(p(0.50f, y + 0.14f), s(0.42f), s(0.11f), TypeIconPalette.Shade(palette.Fill, 0.8f));
                canvas.FillEllipse(p(0.50f, y), s(0.42f), s(0.11f), palette.Stroke);
            }
        }

        /// <summary>A dashed ground boundary - a trigger is an area, and the dashes say "not solid".</summary>
        private static void DrawTriggerArea(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, float stroke)
        {
            var corners = new[] { p(0.06f, 0.24f), p(0.94f, 0.06f), p(1.00f, 0.80f), p(0.14f, 1.00f) };
            canvas.FillPolygon(corners, TypeIconPalette.Shade(palette.Fill, 0.6f) & 0x66FFFFFF);

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
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, float stroke)
        {
            canvas.FillPolygon(
                new[]
                {
                    p(0.04f, 0.36f), p(0.22f, 0.36f), p(0.46f, 0.10f),
                    p(0.46f, 0.90f), p(0.22f, 0.64f), p(0.04f, 0.64f)
                },
                palette.Stroke);

            foreach (var radius in new[] { 0.20f, 0.38f })
            {
                var arc = new List<Vector2>();
                for (var step = 0; step <= 6; step++)
                {
                    var angle = -MathF.PI / 3f + step * (2f * MathF.PI / 3f / 6f);
                    arc.Add(p(0.56f + radius * MathF.Cos(angle), 0.50f + radius * MathF.Sin(angle) * 1.25f));
                }

                canvas.StrokePath(arc, stroke, palette.Detail);
            }
        }

        /// <summary>A pennant on a pole with a ground disc, for waypoints.</summary>
        private static void DrawFlag(
            IconCanvas canvas, TypeIconPalette palette, Func<float, float, Vector2> p, Func<float, float> s, float stroke)
        {
            canvas.FillEllipse(p(0.30f, 0.96f), s(0.22f), s(0.06f), TypeIconPalette.Shade(palette.Fill, 0.8f));
            canvas.StrokeLine(p(0.30f, 0.04f), p(0.30f, 0.96f), stroke, palette.Stroke);
            canvas.FillPolygon(new[] { p(0.33f, 0.06f), p(0.94f, 0.26f), p(0.33f, 0.46f) }, palette.Fill);
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
