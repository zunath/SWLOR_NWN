using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Rendering
{
    /// <summary>
    /// Draws a corner-grid macro layout as a schematic bitmap: solid corners near-black, tiles
    /// fully open colored by room role (or warm gray for open non-room space), partially open
    /// tiles dark tan, any tile touching an accent corner deep blue-teal, and room centers labeled
    /// E/B/S in a white circle. Rendered via DrawingVisual/DrawingContext into a RenderTargetBitmap
    /// so it can be redrawn crisply at any PictureBox-equivalent (Image control) size.
    /// </summary>
    internal static class SchematicRenderer
    {
        private static readonly Color SolidColor = Color.FromRgb(18, 18, 20);
        private static readonly Color EntranceColor = Color.FromRgb(46, 139, 87);
        private static readonly Color BossColor = Color.FromRgb(178, 34, 34);
        private static readonly Color StandardRoomColor = Color.FromRgb(70, 105, 140);
        private static readonly Color OpenNonRoomColor = Color.FromRgb(150, 140, 120);
        private static readonly Color PartialColor = Color.FromRgb(120, 100, 70);
        private static readonly Color AccentColor = Color.FromRgb(20, 70, 90);
        private static readonly Color BackgroundColor = Color.FromRgb(10, 10, 12);

        public static RenderTargetBitmap Render(
            MacroLayout layout,
            MacroLayoutParameters parameters,
            double pixelWidth,
            double pixelHeight)
        {
            var width = layout.Corners.Width;
            var height = layout.Corners.Height;
            if (width <= 0 || height <= 0 || pixelWidth < 1 || pixelHeight < 1)
                return null;

            // Integer cell size, floored so the full grid always fits inside the canvas; the grid
            // is drawn at exactly gridWidth x gridHeight and letterboxed (centered on the
            // background fill). Offsets are floored too so cell edges land on pixel boundaries.
            var cell = Math.Max(1.0, Math.Floor(Math.Min(pixelWidth / width, pixelHeight / height)));
            var gridWidth = cell * width;
            var gridHeight = cell * height;
            var offsetX = Math.Floor((pixelWidth - gridWidth) / 2.0);
            var offsetY = Math.Floor((pixelHeight - gridHeight) / 2.0);

            var roomByTile = new Dictionary<(int X, int Y), RoomRole>();
            foreach (var room in layout.Rooms)
            {
                foreach (var tile in room.Tiles)
                    roomByTile[tile] = room.Role;
            }

            var hasAccent = !string.IsNullOrEmpty(parameters.AccentTerrain);

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(new SolidColorBrush(BackgroundColor), null, new Rect(0, 0, pixelWidth, pixelHeight));

                for (var ty = 0; ty < height; ty++)
                {
                    for (var tx = 0; tx < width; tx++)
                    {
                        var topLeft = layout.Corners.Labels[tx, ty + 1];
                        var topRight = layout.Corners.Labels[tx + 1, ty + 1];
                        var bottomRight = layout.Corners.Labels[tx + 1, ty];
                        var bottomLeft = layout.Corners.Labels[tx, ty];

                        // Terrain labels originate in hand-authored .set files, so compare
                        // case-insensitively (the same discipline TileResolver's corner keys use).
                        var allSolid =
                            LabelEquals(topLeft, parameters.SolidTerrain) && LabelEquals(topRight, parameters.SolidTerrain) &&
                            LabelEquals(bottomRight, parameters.SolidTerrain) && LabelEquals(bottomLeft, parameters.SolidTerrain);

                        var hasAccentCorner = hasAccent &&
                            (LabelEquals(topLeft, parameters.AccentTerrain) || LabelEquals(topRight, parameters.AccentTerrain) ||
                             LabelEquals(bottomRight, parameters.AccentTerrain) || LabelEquals(bottomLeft, parameters.AccentTerrain));

                        var allOpen =
                            LabelEquals(topLeft, parameters.OpenTerrain) && LabelEquals(topRight, parameters.OpenTerrain) &&
                            LabelEquals(bottomRight, parameters.OpenTerrain) && LabelEquals(bottomLeft, parameters.OpenTerrain);

                        Color color;
                        if (allSolid)
                            color = SolidColor;
                        else if (hasAccentCorner)
                            color = AccentColor;
                        else if (allOpen)
                        {
                            color = roomByTile.TryGetValue((tx, ty), out var role)
                                ? RoomColor(role)
                                : OpenNonRoomColor;
                        }
                        else
                            color = PartialColor;

                        // Corners.Labels is y-up (y=0 south); the bitmap is drawn top-down, so flip.
                        var screenY = height - 1 - ty;
                        var rect = new Rect(offsetX + tx * cell, offsetY + screenY * cell, cell, cell);
                        context.DrawRectangle(new SolidColorBrush(color), null, rect);
                    }
                }

                var typeface = new Typeface("Segoe UI");
                foreach (var room in layout.Rooms)
                {
                    var (roomX, roomY) = room.CenterTile;
                    var screenY = height - 1 - roomY;
                    var centerX = offsetX + roomX * cell + cell / 2.0;
                    var centerY = offsetY + screenY * cell + cell / 2.0;
                    var radius = Math.Max(6.0, cell / 2.0 - 2.0);

                    context.DrawEllipse(Brushes.White, null, new Point(centerX, centerY), radius, radius);

                    var label = room.Role switch
                    {
                        RoomRole.Entrance => "E",
                        RoomRole.Boss => "B",
                        _ => "S"
                    };

                    var formattedText = new FormattedText(
                        label,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        Math.Max(10.0, radius),
                        new SolidColorBrush(RoomColor(room.Role)),
                        1.0);

                    context.DrawText(
                        formattedText,
                        new Point(centerX - formattedText.Width / 2.0, centerY - formattedText.Height / 2.0));
                }

                TransitionMarkerRenderer.Draw(context, layout.Transitions, cell, offsetX, offsetY, height);
            }

            var bitmap = new RenderTargetBitmap(
                (int)Math.Ceiling(pixelWidth), (int)Math.Ceiling(pixelHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// Renders a text message (e.g. a generation failure reason) onto the preview background,
        /// so failures are visible in the preview pane itself rather than only in the status bar.
        /// </summary>
        public static RenderTargetBitmap RenderMessage(string message, double pixelWidth, double pixelHeight)
        {
            if (pixelWidth < 1 || pixelHeight < 1 || string.IsNullOrEmpty(message))
                return null;

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(new SolidColorBrush(BackgroundColor), null, new Rect(0, 0, pixelWidth, pixelHeight));

                var formattedText = new FormattedText(
                    message,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    14.0,
                    Brushes.OrangeRed,
                    1.0)
                {
                    MaxTextWidth = Math.Max(50.0, pixelWidth - 40.0),
                    MaxTextHeight = Math.Max(30.0, pixelHeight - 40.0)
                };

                context.DrawText(
                    formattedText,
                    new Point((pixelWidth - Math.Min(formattedText.Width, formattedText.MaxTextWidth)) / 2.0,
                              (pixelHeight - formattedText.Height) / 2.0));
            }

            var bitmap = new RenderTargetBitmap(
                (int)Math.Ceiling(pixelWidth), (int)Math.Ceiling(pixelHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }

        private static bool LabelEquals(string label, string terrain) =>
            string.Equals(label, terrain, StringComparison.OrdinalIgnoreCase);

        private static Color RoomColor(RoomRole role) => role switch
        {
            RoomRole.Entrance => EntranceColor,
            RoomRole.Boss => BossColor,
            _ => StandardRoomColor
        };
    }
}
