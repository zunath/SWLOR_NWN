using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Behaviors
{
    /// <summary>
    /// Resolves the picture a choice is picked by: the load screens, the door appearances, the
    /// portraits, and the waypoint markers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each load screen is a 1.4 MB DDS and there are around twenty of them, so decoding the set
    /// costs about thirty megabytes of work. Doing that inline while the Behavior tab rebuilt is
    /// what made switching to Area Transition stall: the editor decoded every screen before it drew
    /// anything, to fill a list the builder had not opened yet.
    /// </para>
    /// <para>
    /// So decoding happens off the UI thread and only when something actually needs the picture —
    /// the tiles a gallery has actually published, and no more. Results are cached per size,
    /// including the misses, since a resref that will not resolve now will not resolve on the next
    /// scroll either.
    /// </para>
    /// <para>
    /// A choice can name a model instead of a texture, which has to be rendered rather than decoded.
    /// That work belongs to <see cref="ThumbnailService"/>, which already owns the render queue and
    /// its caches, so this forwards to it rather than growing a second one.
    /// </para>
    /// </remarks>
    public sealed class ChoicePreviewService
    {
        /// <summary>Gallery thumbnails. Small enough that twenty of them cost little memory.</summary>
        public const int ThumbnailWidth = 192;

        /// <summary>The chosen option, shown large enough to actually judge.</summary>
        public const int PreviewWidth = 384;

        private readonly ResourceIndex? _resources;
        private readonly ThumbnailService? _models;
        private readonly object _syncRoot = new();
        private readonly Dictionary<string, Bitmap?> _cache = new(StringComparer.OrdinalIgnoreCase);

        public ChoicePreviewService(ResourceIndex? resources, ThumbnailService? models = null)
        {
            _resources = resources;
            _models = models;
        }

        /// <summary>The already-resolved picture for a choice, or null. Never starts work.</summary>
        public Bitmap? Cached(BehaviorChoice choice, int maxWidth)
        {
            ArgumentNullException.ThrowIfNull(choice);

            return choice.ModelResRef is { Length: > 0 } model
                ? _models?.CachedTile(model)
                : Cached(choice.ImageResRef, maxWidth);
        }

        /// <summary>
        /// Resolves a choice's picture and hands it to <paramref name="onReady"/> on the UI thread.
        /// </summary>
        /// <remarks>
        /// A choice whose picture cannot be produced is simply never called back, and its tile stays
        /// on the letter it started with. That is the degrade path on purpose: a gallery that waited
        /// on every unrenderable row would hold the ones that do render behind them.
        /// </remarks>
        public async Task RequestAsync(BehaviorChoice choice, int maxWidth, Action<Bitmap> onReady)
        {
            ArgumentNullException.ThrowIfNull(choice);
            ArgumentNullException.ThrowIfNull(onReady);

            if (choice.ModelResRef is { Length: > 0 } model)
            {
                if (_models?.CachedTile(model) is { } cached)
                    onReady(cached);
                else
                    _models?.RequestTileAsync(model, onReady);

                return;
            }

            if (await ResolveAsync(choice.ImageResRef, maxWidth).ConfigureAwait(true) is { } bitmap)
                onReady(bitmap);
        }

        /// <summary>
        /// The artwork for a resref at the given width, decoding off the UI thread on first use.
        /// Returns null when there is no artwork, none can be decoded, or no resource index exists.
        /// </summary>
        public async Task<Bitmap?> ResolveAsync(string? resRef, int maxWidth)
        {
            if (_resources == null || string.IsNullOrWhiteSpace(resRef))
                return null;

            var key = $"{resRef}@{maxWidth}";
            lock (_syncRoot)
            {
                if (_cache.TryGetValue(key, out var cached))
                    return cached;
            }

            // Decode and scale on a worker; only the bitmap handoff needs the UI thread.
            var scaled = await Task.Run(() => Decode(resRef, maxWidth)).ConfigureAwait(true);
            var bitmap = scaled == null ? null : await ToBitmapAsync(scaled).ConfigureAwait(true);

            lock (_syncRoot)
                _cache[key] = bitmap;

            return bitmap;
        }

        /// <summary>An already-decoded bitmap, or null. Never starts work — for binding without waiting.</summary>
        public Bitmap? Cached(string? resRef, int maxWidth)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return null;

            lock (_syncRoot)
                return _cache.GetValueOrDefault($"{resRef}@{maxWidth}");
        }

        private ScaledImage? Decode(string resRef, int maxWidth)
        {
            try
            {
                if (TextureLoader.Load(_resources!, resRef) is not { } texture)
                    return null;

                return Downscale(texture, maxWidth);
            }
            catch (Exception)
            {
                // A picker must never be the thing that takes the editor down; a missing preview
                // degrades to the name the row already shows.
                return null;
            }
        }

        /// <summary>
        /// Box-averages down to the display width and swaps RGBA to the BGRA Avalonia wants, in one
        /// pass. Keeping the full 1024-pixel image would cost ~4 MB per screen for a thumbnail.
        /// </summary>
        private static ScaledImage Downscale(TextureImage texture, int maxWidth)
        {
            var factor = Math.Max(1, texture.Width / Math.Max(1, maxWidth));
            var width = Math.Max(1, texture.Width / factor);
            var height = Math.Max(1, texture.Height / factor);
            var pixels = new byte[width * height * 4];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    int b = 0, g = 0, r = 0, a = 0, taken = 0;
                    for (var sy = y * factor; sy < (y + 1) * factor && sy < texture.Height; sy++)
                    {
                        for (var sx = x * factor; sx < (x + 1) * factor && sx < texture.Width; sx++)
                        {
                            var source = (sy * texture.Width + sx) * 4;
                            r += texture.Pixels[source];
                            g += texture.Pixels[source + 1];
                            b += texture.Pixels[source + 2];
                            a += texture.Pixels[source + 3];
                            taken++;
                        }
                    }

                    if (taken == 0)
                        continue;

                    var target = (y * width + x) * 4;
                    pixels[target] = (byte)(b / taken);
                    pixels[target + 1] = (byte)(g / taken);
                    pixels[target + 2] = (byte)(r / taken);
                    pixels[target + 3] = (byte)(a / taken);
                }
            }

            return new ScaledImage(width, height, pixels);
        }

        private static async Task<Bitmap?> ToBitmapAsync(ScaledImage image)
        {
            if (!Dispatcher.UIThread.CheckAccess())
                return await Dispatcher.UIThread.InvokeAsync(() => ToBitmap(image));

            return ToBitmap(image);
        }

        private static Bitmap ToBitmap(ScaledImage image)
        {
            var bitmap = new WriteableBitmap(
                new Avalonia.PixelSize(image.Width, image.Height),
                new Avalonia.Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using var buffer = bitmap.Lock();
            var stride = image.Width * 4;
            for (var y = 0; y < image.Height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(
                    image.Pixels, y * stride, buffer.Address + y * buffer.RowBytes, stride);
            }

            return bitmap;
        }

        private sealed record ScaledImage(int Width, int Height, byte[] Pixels);
    }
}
