#nullable enable
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>
    /// Renders a generated draft without UI dependencies. Map mode composites each tile's real
    /// ImageMap2D resource; missing artwork falls back to the same semantic schematic colors.
    /// </summary>
    public sealed class AreaGenerationPreviewRenderer
    {
        private readonly ResourceIndex? _resources;
        private readonly Dictionary<string, TextureImage?> _images =
            new(StringComparer.OrdinalIgnoreCase);

        public AreaGenerationPreviewRenderer(ResourceIndex? resources)
        {
            _resources = resources;
        }

        public AreaPreviewImage Render(
            AreaGenerationDraft draft,
            AreaPreviewMode mode,
            bool showRoomOverlay,
            bool showTransitions = true,
            bool showDecorations = true,
            int pixelsPerTile = 24)
        {
            ArgumentNullException.ThrowIfNull(draft);
            if (!draft.Result.Success || draft.Result.Resolved == null)
                throw new InvalidOperationException("A successful generated draft is required.");
            if (pixelsPerTile is < 8 or > 96)
                throw new ArgumentOutOfRangeException(nameof(pixelsPerTile));

            var resolved = draft.Result.Resolved;
            var width = checked(resolved.Width * pixelsPerTile);
            var height = checked(resolved.Height * pixelsPerTile);
            var pixels = new byte[checked(width * height * 4)];
            Fill(pixels, 10, 10, 12, 255);

            var roomByTile = new Dictionary<(int X, int Y), RoomRole>();
            foreach (var room in resolved.Rooms)
            foreach (var tile in room.Tiles)
                roomByTile[tile] = room.Role;

            var missing = 0;
            for (var y = 0; y < resolved.Height; y++)
            for (var x = 0; x < resolved.Width; x++)
            {
                var tile = resolved.GetTile(x, y);
                var record = tile.TileId >= 0 && tile.TileId < draft.Tileset.Tiles.Count
                    ? draft.Tileset.Tiles[tile.TileId]
                    : null;
                var screenY = resolved.Height - 1 - y;
                var drawn = mode == AreaPreviewMode.MapGraphics && record != null &&
                            TryDrawTileGraphic(
                                pixels,
                                width,
                                x * pixelsPerTile,
                                screenY * pixelsPerTile,
                                pixelsPerTile,
                                record,
                                tile.Orientation);
                if (!drawn)
                {
                    if (mode == AreaPreviewMode.MapGraphics)
                        missing++;
                    var color = FallbackColor(
                        record,
                        tile.Orientation,
                        draft.Tileset,
                        resolved.OpenTerrain,
                        resolved.SecondaryOpenTerrain,
                        roomByTile.TryGetValue((x, y), out var role) ? role : null);
                    FillRect(
                        pixels,
                        width,
                        x * pixelsPerTile,
                        screenY * pixelsPerTile,
                        pixelsPerTile,
                        pixelsPerTile,
                        color);
                }

                StrokeRect(
                    pixels,
                    width,
                    x * pixelsPerTile,
                    screenY * pixelsPerTile,
                    pixelsPerTile,
                    pixelsPerTile,
                    (34, 34, 38, 180));
            }

            if (showRoomOverlay)
                DrawRoomOverlay(pixels, width, resolved, pixelsPerTile);
            if (showTransitions)
                DrawTransitions(pixels, width, resolved, pixelsPerTile);
            if (showDecorations)
                DrawDecorations(pixels, width, resolved.Height, draft, pixelsPerTile);

            return new AreaPreviewImage(width, height, pixels, missing);
        }

        private bool TryDrawTileGraphic(
            byte[] output,
            int outputWidth,
            int left,
            int top,
            int size,
            TileRecord tile,
            int orientation)
        {
            if (_resources == null || string.IsNullOrWhiteSpace(tile.ImageMap2D))
                return false;

            if (!_images.TryGetValue(tile.ImageMap2D, out var image))
            {
                image = TextureLoader.LoadTga(_resources, tile.ImageMap2D);
                _images[tile.ImageMap2D] = image;
            }

            if (image == null)
                return false;

            orientation = ((orientation % 4) + 4) % 4;
            for (var dy = 0; dy < size; dy++)
            for (var dx = 0; dx < size; dx++)
            {
                var normalizedX = dx * image.Width / size;
                var normalizedY = dy * image.Height / size;
                var (sourceX, sourceY) = orientation switch
                {
                    1 => (image.Width - 1 - normalizedY, normalizedX),
                    2 => (image.Width - 1 - normalizedX, image.Height - 1 - normalizedY),
                    3 => (normalizedY, image.Height - 1 - normalizedX),
                    _ => (normalizedX, normalizedY)
                };
                sourceX = Math.Clamp(sourceX, 0, image.Width - 1);
                sourceY = Math.Clamp(sourceY, 0, image.Height - 1);
                var source = (sourceY * image.Width + sourceX) * 4;
                BlendPixel(
                    output,
                    outputWidth,
                    left + dx,
                    top + dy,
                    (image.Pixels[source], image.Pixels[source + 1], image.Pixels[source + 2], image.Pixels[source + 3]));
            }

            return true;
        }

        private static (byte R, byte G, byte B, byte A) FallbackColor(
            TileRecord? tile,
            int orientation,
            TilesetModel tileset,
            string openTerrain,
            string secondaryOpenTerrain,
            RoomRole? role)
        {
            if (tile == null)
                return (120, 100, 70, 255);

            openTerrain = string.IsNullOrEmpty(openTerrain) ? tileset.FloorTerrain : openTerrain;
            var corners = Enumerable.Range(0, 4)
                .Select(slot => tile.GetCornerAt(orientation, slot))
                .ToArray();
            if (corners.All(label => label.Equals(tileset.DefaultTerrain, StringComparison.OrdinalIgnoreCase)))
                return (18, 18, 20, 255);

            var allOpen = corners.All(label => label.Equals(openTerrain, StringComparison.OrdinalIgnoreCase)) ||
                          !string.IsNullOrEmpty(secondaryOpenTerrain) && corners.All(label =>
                              label.Equals(secondaryOpenTerrain, StringComparison.OrdinalIgnoreCase));
            if (!allOpen)
                return (120, 100, 70, 255);

            return role switch
            {
                RoomRole.Entrance => (46, 139, 87, 255),
                RoomRole.Boss => (178, 34, 34, 255),
                RoomRole.Standard => (70, 105, 140, 255),
                _ => (150, 140, 120, 255)
            };
        }

        private static void DrawRoomOverlay(
            byte[] pixels,
            int width,
            ResolvedLayout resolved,
            int cell)
        {
            foreach (var room in resolved.Rooms)
            {
                var screenY = resolved.Height - 1 - room.CenterTile.Y;
                var centerX = room.CenterTile.X * cell + cell / 2;
                var centerY = screenY * cell + cell / 2;
                var radius = Math.Max(3, cell / 4);
                (byte R, byte G, byte B, byte A) color = room.IsSetPiece
                    ? (R: (byte)200, G: (byte)200, B: (byte)200, A: (byte)210)
                    : room.Role switch
                    {
                        RoomRole.Entrance => (R: (byte)46, G: (byte)210, B: (byte)122, A: (byte)230),
                        RoomRole.Boss => (R: (byte)230, G: (byte)60, B: (byte)60, A: (byte)230),
                        _ => (R: (byte)100, G: (byte)160, B: (byte)220, A: (byte)230)
                    };
                DrawCircle(pixels, width, centerX, centerY, radius, color, hollow: room.IsSetPiece);
            }
        }

        private static void DrawTransitions(
            byte[] pixels,
            int width,
            ResolvedLayout resolved,
            int cell)
        {
            foreach (var transition in resolved.Transitions)
            {
                var screenY = resolved.Height - 1 - transition.Tile.Y;
                var color = transition.Kind == TransitionKind.Entrance
                    ? (R: (byte)80, G: (byte)255, B: (byte)150, A: (byte)255)
                    : (R: (byte)255, G: (byte)210, B: (byte)70, A: (byte)255);
                DrawCircle(
                    pixels,
                    width,
                    transition.Tile.X * cell + cell / 2,
                    screenY * cell + cell / 2,
                    Math.Max(2, cell / 7),
                    color,
                    hollow: false);
            }
        }

        private static void DrawDecorations(
            byte[] pixels,
            int width,
            int areaHeight,
            AreaGenerationDraft draft,
            int cell)
        {
            foreach (var decoration in draft.Result.PlannedDecorations)
            {
                var x = (int)MathF.Round(decoration.Position.X / 10f * cell);
                var y = (int)MathF.Round((areaHeight - decoration.Position.Y / 10f) * cell);
                DrawCircle(pixels, width, x, y, Math.Max(1, cell / 12), (210, 150, 255, 210), hollow: false);
            }
        }

        private static void Fill(byte[] pixels, byte r, byte g, byte b, byte a)
        {
            for (var index = 0; index < pixels.Length; index += 4)
            {
                pixels[index] = r;
                pixels[index + 1] = g;
                pixels[index + 2] = b;
                pixels[index + 3] = a;
            }
        }

        private static void FillRect(
            byte[] pixels,
            int width,
            int left,
            int top,
            int rectWidth,
            int rectHeight,
            (byte R, byte G, byte B, byte A) color)
        {
            var height = pixels.Length / 4 / width;
            for (var y = Math.Max(0, top); y < Math.Min(height, top + rectHeight); y++)
            for (var x = Math.Max(0, left); x < Math.Min(width, left + rectWidth); x++)
                SetPixel(pixels, width, x, y, color);
        }

        private static void StrokeRect(
            byte[] pixels,
            int width,
            int left,
            int top,
            int rectWidth,
            int rectHeight,
            (byte R, byte G, byte B, byte A) color)
        {
            for (var x = left; x < left + rectWidth; x++)
            {
                BlendPixel(pixels, width, x, top, color);
                BlendPixel(pixels, width, x, top + rectHeight - 1, color);
            }
            for (var y = top; y < top + rectHeight; y++)
            {
                BlendPixel(pixels, width, left, y, color);
                BlendPixel(pixels, width, left + rectWidth - 1, y, color);
            }
        }

        private static void DrawCircle(
            byte[] pixels,
            int width,
            int centerX,
            int centerY,
            int radius,
            (byte R, byte G, byte B, byte A) color,
            bool hollow)
        {
            var inner = Math.Max(0, radius - 2);
            for (var y = centerY - radius; y <= centerY + radius; y++)
            for (var x = centerX - radius; x <= centerX + radius; x++)
            {
                var distance = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                if (distance > radius * radius || hollow && distance < inner * inner)
                    continue;
                BlendPixel(pixels, width, x, y, color);
            }
        }

        private static void SetPixel(
            byte[] pixels,
            int width,
            int x,
            int y,
            (byte R, byte G, byte B, byte A) color)
        {
            var height = pixels.Length / 4 / width;
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;
            var index = (y * width + x) * 4;
            pixels[index] = color.R;
            pixels[index + 1] = color.G;
            pixels[index + 2] = color.B;
            pixels[index + 3] = color.A;
        }

        private static void BlendPixel(
            byte[] pixels,
            int width,
            int x,
            int y,
            (byte R, byte G, byte B, byte A) color)
        {
            var height = pixels.Length / 4 / width;
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;
            var index = (y * width + x) * 4;
            var alpha = color.A / 255f;
            pixels[index] = (byte)Math.Clamp(color.R * alpha + pixels[index] * (1f - alpha), 0, 255);
            pixels[index + 1] = (byte)Math.Clamp(color.G * alpha + pixels[index + 1] * (1f - alpha), 0, 255);
            pixels[index + 2] = (byte)Math.Clamp(color.B * alpha + pixels[index + 2] * (1f - alpha), 0, 255);
            pixels[index + 3] = 255;
        }
    }
}
