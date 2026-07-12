using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Rendering
{
    /// <summary>
    /// Shared entrance/exit transition marker drawing, used by both preview renderers (Schematic
    /// and Map graphics). Markers are always drawn — unlike the room overlay, they are not gated
    /// by a checkbox, since entrance/exit placement is load-bearing composition info rather than a
    /// debug aid.
    ///
    /// Entrance: filled upward-pointing chevron/triangle, warm gold. Exit: filled downward-pointing
    /// chevron/triangle, bright cyan. Both get a thin dark outline so they stay legible over the
    /// schematic's flat room colors and over real minimap tile art alike. At cell sizes >= 24px a
    /// tiny "IN"/"OUT" label is drawn below the marker on a small dark backdrop (again for
    /// legibility over bright art); smaller cells omit the label since there's no room to render it
    /// cleanly.
    ///
    /// Door-style transitions (TileDoorPlanner substituted a real tileset door for the placeable)
    /// additionally get a small filled square at the door's actual world position — converted from
    /// world units (10 per tile) to grid space the same way tile cells are — so the reviewer can see
    /// exactly where the door sits in the wall, straddling the boundary between the room-edge and
    /// solid-side tiles.
    /// </summary>
    internal static class TransitionMarkerRenderer
    {
        private static readonly Color EntranceFill = Color.FromRgb(230, 178, 46); // warm gold
        private static readonly Color ExitFill = Color.FromRgb(64, 224, 240); // bright cyan
        private static readonly Color OutlineColor = Color.FromRgb(20, 18, 16);
        private static readonly Color LabelBackdrop = Color.FromArgb(190, 15, 15, 15);

        private const double MinCellForLabel = 24.0;

        public static void Draw(
            DrawingContext context,
            IEnumerable<TransitionPoint> transitions,
            double cell,
            double offsetX,
            double offsetY,
            int heightInTiles)
        {
            if (transitions == null) return;

            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var outlinePen = new Pen(new SolidColorBrush(OutlineColor), Math.Max(1.0, cell * 0.05)) { LineJoin = PenLineJoin.Round };

            foreach (var transition in transitions)
            {
                var (tileX, tileY) = transition.Tile;
                var screenY = heightInTiles - 1 - tileY;
                var centerX = offsetX + tileX * cell + cell / 2.0;
                var centerY = offsetY + screenY * cell + cell / 2.0;

                var isEntrance = transition.Kind == TransitionKind.Entrance;
                var size = Math.Max(5.0, cell * 0.30);
                var geometry = Chevron(centerX, centerY, size, pointingUp: isEntrance);
                var fillBrush = new SolidColorBrush(isEntrance ? EntranceFill : ExitFill);

                context.DrawGeometry(fillBrush, outlinePen, geometry);

                if (transition.Style is TransitionStyle.Door or TransitionStyle.GroupExit)
                {
                    var doorGridX = transition.DoorX / 10.0;
                    var doorGridY = transition.DoorY / 10.0;
                    var doorScreenX = offsetX + doorGridX * cell;
                    var doorScreenY = offsetY + (heightInTiles - doorGridY) * cell;
                    var doorSize = Math.Max(4.0, cell * 0.22);

                    context.DrawRectangle(
                        fillBrush, outlinePen,
                        new Rect(doorScreenX - doorSize / 2.0, doorScreenY - doorSize / 2.0, doorSize, doorSize));
                }

                if (cell < MinCellForLabel) continue;

                var label = isEntrance ? "IN" : "OUT";
                var formattedText = new FormattedText(
                    label,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    Math.Max(8.0, cell * 0.20),
                    Brushes.White,
                    1.0);

                var textX = centerX - formattedText.Width / 2.0;
                var textY = centerY + size * 0.75;

                context.DrawRectangle(
                    new SolidColorBrush(LabelBackdrop), null,
                    new Rect(textX - 2, textY - 1, formattedText.Width + 4, formattedText.Height + 2));
                context.DrawText(formattedText, new Point(textX, textY));
            }
        }

        private static StreamGeometry Chevron(double centerX, double centerY, double size, bool pointingUp)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                Point apex, left, right;
                if (pointingUp)
                {
                    apex = new Point(centerX, centerY - size);
                    left = new Point(centerX - size * 0.9, centerY + size * 0.65);
                    right = new Point(centerX + size * 0.9, centerY + size * 0.65);
                }
                else
                {
                    apex = new Point(centerX, centerY + size);
                    left = new Point(centerX - size * 0.9, centerY - size * 0.65);
                    right = new Point(centerX + size * 0.9, centerY - size * 0.65);
                }

                ctx.BeginFigure(apex, true, true);
                ctx.LineTo(right, true, false);
                ctx.LineTo(left, true, false);
            }
            geometry.Freeze();
            return geometry;
        }
    }
}
