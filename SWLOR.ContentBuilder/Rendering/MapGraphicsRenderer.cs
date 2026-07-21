using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.ContentBuilder.Services;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.ContentBuilder.Rendering
{
    /// <summary>
    /// Draws a resolved tile grid as an in-game-style minimap composite: each cell's actual tile
    /// texture (TileRecord.ImageMap2D), rotated per its resolved orientation, scaled with crisp
    /// nearest-neighbor sampling and letterboxed the same way SchematicRenderer sizes its grid.
    /// A tile whose texture can't be found/decoded falls back to a schematic-style solid/open/partial
    /// color so the layout stays legible.
    /// </summary>
    internal static class MapGraphicsRenderer
    {
        // NWN tile orientation n means the tile's fixed corner/edge features are rotated n * 90
        // degrees counterclockwise into the world (see TileRecord's class remarks). WPF's
        // RotateTransform.Angle is clockwise-positive, so "90 degrees counterclockwise" is -90 in
        // RotateTransform terms. Verified empirically against the hand-built moncaladungeon1 area
        // (scratchpad harness rendering both +90 and -90 candidates): at -90 the special room's
        // floor (checker pattern + circular pit + "F" alcove) composites into one coherent,
        // symmetric room; at +90 the same room splits into two visibly mismatched floor textures
        // with a broken seam down the middle. If this ever needs re-deriving, rebuild that harness
        // rather than guessing — mismatched seams are visually obvious on a hand-built area.
        private const double DegreesPerOrientationStep = -90.0;

        private static readonly Color SolidColor = Color.FromRgb(18, 18, 20);
        private static readonly Color OpenColor = Color.FromRgb(150, 140, 120);
        private static readonly Color PartialColor = Color.FromRgb(120, 100, 70);
        private static readonly Color BackgroundColor = Color.FromRgb(10, 10, 12);
        private static readonly Color EntranceColor = Color.FromRgb(46, 139, 87);
        private static readonly Color BossColor = Color.FromRgb(178, 34, 34);
        private static readonly Color StandardRoomColor = Color.FromRgb(70, 105, 140);
        private static readonly Color SetPieceBadgeColor = Color.FromArgb(200, 200, 200, 200);

        public static RenderTargetBitmap Render(
            ResolvedLayout resolved,
            TilesetModel tileset,
            bool showRoomOverlay,
            double pixelWidth,
            double pixelHeight,
            out MapRenderStats stats)
        {
            return Render(resolved, tileset, showRoomOverlay, pixelWidth, pixelHeight, DegreesPerOrientationStep, out stats);
        }

        /// <summary>
        /// Overload taking the per-orientation-step rotation explicitly, used by the empirical
        /// rotation-verification harness to render both candidate directions with the exact same
        /// compositing code. Production callers should use the 5-argument overload above.
        /// </summary>
        public static RenderTargetBitmap Render(
            ResolvedLayout resolved,
            TilesetModel tileset,
            bool showRoomOverlay,
            double pixelWidth,
            double pixelHeight,
            double degreesPerOrientationStep,
            out MapRenderStats stats)
        {
            stats = new MapRenderStats { BaseGameArchiveStatus = MinimapCache.BaseGameArchiveStatus };

            var width = resolved.Width;
            var height = resolved.Height;
            if (width <= 0 || height <= 0 || pixelWidth < 1 || pixelHeight < 1)
                return null;

            var cell = Math.Max(1.0, Math.Floor(Math.Min(pixelWidth / width, pixelHeight / height)));
            var gridWidth = cell * width;
            var gridHeight = cell * height;
            var offsetX = Math.Floor((pixelWidth - gridWidth) / 2.0);
            var offsetY = Math.Floor((pixelHeight - gridHeight) / 2.0);

            var roomByTile = new Dictionary<(int X, int Y), RoomRole>();
            foreach (var room in resolved.Rooms)
            {
                foreach (var tileCoord in room.Tiles)
                    roomByTile[tileCoord] = room.Role;
            }

            var visual = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased);

            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(new SolidColorBrush(BackgroundColor), null, new Rect(0, 0, pixelWidth, pixelHeight));

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var resolvedTile = resolved.GetTile(x, y);
                        var tileRecord = resolvedTile.TileId >= 0 && resolvedTile.TileId < tileset.Tiles.Count
                            ? tileset.Tiles[resolvedTile.TileId]
                            : null;

                        // Corners.Labels/tile grid is y-up (y=0 south); the bitmap is drawn top-down.
                        var screenY = height - 1 - y;
                        var cellRect = new Rect(offsetX + x * cell, offsetY + screenY * cell, cell, cell);

                        var entry = tileRecord != null ? MinimapCache.GetOrLoad(tileset, tileRecord) : null;

                        if (entry?.Image != null)
                        {
                            switch (entry.Source)
                            {
                                case MinimapImageSource.Loose: stats.LooseHits++; break;
                                case MinimapImageSource.BaseGameArchive: stats.ArchiveHits++; break;
                            }

                            var angle = resolvedTile.Orientation * degreesPerOrientationStep;
                            var center = new Point(cellRect.X + cellRect.Width / 2.0, cellRect.Y + cellRect.Height / 2.0);

                            context.PushTransform(new RotateTransform(angle, center.X, center.Y));
                            context.DrawImage(entry.Image, cellRect);
                            context.Pop();
                        }
                        else
                        {
                            stats.Misses++;
                            var color = FallbackColor(tileRecord, resolvedTile.Orientation, tileset, resolved.OpenTerrain, resolved.SecondaryOpenTerrain, roomByTile, (x, y));
                            context.DrawRectangle(new SolidColorBrush(color), null, cellRect);
                        }
                    }
                }

                if (showRoomOverlay)
                    DrawRoomOverlay(context, resolved, cell, offsetX, offsetY, height);

                TransitionMarkerRenderer.Draw(context, resolved.Transitions, cell, offsetX, offsetY, height);
            }

            var bitmap = new RenderTargetBitmap(
                (int)Math.Ceiling(pixelWidth), (int)Math.Ceiling(pixelHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }

        private static Color FallbackColor(
            TileRecord tile,
            int orientation,
            TilesetModel tileset,
            string layoutOpenTerrain,
            string layoutSecondaryOpenTerrain,
            Dictionary<(int X, int Y), RoomRole> roomByTile,
            (int X, int Y) coord)
        {
            if (tile == null) return PartialColor;

            var openTerrain = string.IsNullOrEmpty(layoutOpenTerrain) ? tileset.FloorTerrain : layoutOpenTerrain;

            var tl = tile.GetCornerAt(orientation, CornerSlot.TopLeft);
            var tr = tile.GetCornerAt(orientation, CornerSlot.TopRight);
            var br = tile.GetCornerAt(orientation, CornerSlot.BottomRight);
            var bl = tile.GetCornerAt(orientation, CornerSlot.BottomLeft);

            var allSolid = LabelEquals(tl, tileset.DefaultTerrain) && LabelEquals(tr, tileset.DefaultTerrain) &&
                           LabelEquals(br, tileset.DefaultTerrain) && LabelEquals(bl, tileset.DefaultTerrain);
            if (allSolid) return SolidColor;

            // A tile's four corners are always a single terrain when fully open, so it either matches
            // the primary open terrain OR (when multi-terrain districts are active) the secondary one
            // -- never a mix (see MacroLayoutParameters.SecondaryOpenTerrain).
            var allOpenPrimary = LabelEquals(tl, openTerrain) && LabelEquals(tr, openTerrain) &&
                                 LabelEquals(br, openTerrain) && LabelEquals(bl, openTerrain);

            var allOpenSecondary = !string.IsNullOrEmpty(layoutSecondaryOpenTerrain) &&
                                    LabelEquals(tl, layoutSecondaryOpenTerrain) && LabelEquals(tr, layoutSecondaryOpenTerrain) &&
                                    LabelEquals(br, layoutSecondaryOpenTerrain) && LabelEquals(bl, layoutSecondaryOpenTerrain);

            if (allOpenPrimary || allOpenSecondary)
            {
                return roomByTile.TryGetValue(coord, out var role) ? RoomColor(role) : OpenColor;
            }

            return PartialColor;
        }

        private static void DrawRoomOverlay(
            DrawingContext context, ResolvedLayout resolved, double cell, double offsetX, double offsetY, int height)
        {
            var typeface = new Typeface("Segoe UI");
            foreach (var room in resolved.Rooms)
            {
                var (roomX, roomY) = room.CenterTile;
                var screenY = height - 1 - roomY;
                var centerX = offsetX + roomX * cell + cell / 2.0;
                var centerY = offsetY + screenY * cell + cell / 2.0;
                var radius = Math.Max(6.0, cell / 2.0 - 2.0);

                // Set-piece rooms (decorative wall chambers/alcoves stamped by LayoutGroupStamper)
                // are not gameplay rooms: no spawns, objectives, or transitions ever land in them.
                // Render a smaller hollow gray badge so previews don't imply a reachability problem.
                if (room.IsSetPiece)
                {
                    var decoRadius = radius * 0.6;
                    var decoPen = new Pen(new SolidColorBrush(SetPieceBadgeColor), Math.Max(1.0, cell * 0.06));
                    context.DrawEllipse(null, decoPen, new Point(centerX, centerY), decoRadius, decoRadius);

                    var decoText = new FormattedText(
                        "D",
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        Math.Max(8.0, decoRadius),
                        new SolidColorBrush(SetPieceBadgeColor),
                        1.0);
                    context.DrawText(
                        decoText,
                        new Point(centerX - decoText.Width / 2.0, centerY - decoText.Height / 2.0));
                    continue;
                }

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
