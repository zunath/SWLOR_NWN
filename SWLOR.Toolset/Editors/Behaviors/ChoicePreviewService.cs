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
    /// Resolves the picture a choice is picked by: load screens, blueprint thumbnails, door
    /// appearances, portraits, and waypoint markers.
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
    /// A choice can name a model or a blueprint instead of a texture, which has to be rendered rather
    /// than decoded. That work belongs to <see cref="ThumbnailService"/>, which already owns the
    /// render queue and its caches, so this forwards to it rather than growing a second one.
    /// </para>
    /// </remarks>
    public sealed class ChoicePreviewService
    {
        private enum ImageCropMode
        {
            None,
            TransparentCanvas,
            NeverwinterPortrait
        }

        /// <summary>Gallery thumbnails. Small enough that twenty of them cost little memory.</summary>
        public const int ThumbnailWidth = 192;

        /// <summary>The chosen option, shown large enough to actually judge.</summary>
        public const int PreviewWidth = 384;

        /// <summary>
        /// Large enough that a full pass over the biggest gallery (portraits, at both widths) mostly
        /// stays warm, while bounding what a long session can pin — this service lives for the whole
        /// process, so an unbounded dictionary here would only ever grow.
        /// </summary>
        private const int MemoryCacheCapacity = 1024;

        private readonly ResourceIndex? _resources;
        private readonly ThumbnailService? _models;
        private readonly Placeables.VfxPreviewService? _remoteImages;
        private readonly SemaphoreSlim _decodeSlots = new(4);
        private readonly BitmapMemoryCache _cache = new(MemoryCacheCapacity);

        public ChoicePreviewService(
            ResourceIndex? resources,
            ThumbnailService? models = null,
            Placeables.VfxPreviewService? remoteImages = null)
        {
            _resources = resources;
            _models = models;
            _remoteImages = remoteImages;
        }

        /// <summary>The already-resolved picture for a choice, or null. Never starts work.</summary>
        public Bitmap? Cached(
            BehaviorChoice choice,
            int maxWidth,
            bool cropTransparentCanvas = false)
        {
            ArgumentNullException.ThrowIfNull(choice);

            if (choice.BlueprintPreviewType is { } blueprintType &&
                choice.BlueprintPreviewResRef is { Length: > 0 } blueprintResRef)
            {
                return _models?.Cached(blueprintType, blueprintResRef);
            }
            if (choice.ModelResRef is { Length: > 0 } model)
                return _models?.CachedTile(model);
            if (choice.ImageUrl is { Length: > 0 } imageUrl)
                return _remoteImages?.Cached(imageUrl);
            return CachedInternal(choice.ImageResRef, maxWidth, CropMode(choice, cropTransparentCanvas));
        }

        /// <summary>
        /// Resolves a choice's picture and hands it to <paramref name="onReady"/> on the UI thread.
        /// </summary>
        /// <remarks>
        /// A choice whose picture cannot be produced is simply never called back, and its tile stays
        /// on the letter it started with. That is the degrade path on purpose: a gallery that waited
        /// on every unrenderable row would hold the ones that do render behind them.
        /// </remarks>
        public async Task RequestAsync(
            BehaviorChoice choice,
            int maxWidth,
            Action<Bitmap> onReady,
            bool cropTransparentCanvas = false)
        {
            ArgumentNullException.ThrowIfNull(choice);
            ArgumentNullException.ThrowIfNull(onReady);

            if (choice.BlueprintPreviewType is { } blueprintType &&
                choice.BlueprintPreviewResRef is { Length: > 0 } blueprintResRef)
            {
                if (_models?.Cached(blueprintType, blueprintResRef) is { } cached)
                    onReady(cached);
                else
                    _models?.RequestAsync(blueprintType, blueprintResRef, onReady);

                return;
            }

            if (choice.ModelResRef is { Length: > 0 } model)
            {
                if (_models?.CachedTile(model) is { } cached)
                    onReady(cached);
                else
                    _models?.RequestTileAsync(model, onReady);

                return;
            }

            if (choice.ImageUrl is { Length: > 0 } imageUrl)
            {
                if (_remoteImages?.Cached(imageUrl) is { } cached)
                    onReady(cached);
                else
                    _remoteImages?.RequestAsync(imageUrl, onReady);
                return;
            }

            if (await ResolveInternalAsync(
                    choice.ImageResRef,
                    maxWidth,
                    CropMode(choice, cropTransparentCanvas)).ConfigureAwait(true) is { } bitmap)
                onReady(bitmap);
        }

        /// <summary>
        /// The artwork for a resref at the given width, decoding off the UI thread on first use.
        /// Returns null when there is no artwork, none can be decoded, or no resource index exists.
        /// </summary>
        public async Task<Bitmap?> ResolveAsync(
            string? resRef,
            int maxWidth,
            bool cropTransparentCanvas = false)
        {
            return await ResolveInternalAsync(
                resRef,
                maxWidth,
                cropTransparentCanvas ? ImageCropMode.TransparentCanvas : ImageCropMode.None);
        }

        private async Task<Bitmap?> ResolveInternalAsync(
            string? resRef,
            int maxWidth,
            ImageCropMode cropMode)
        {
            if (_resources == null || string.IsNullOrWhiteSpace(resRef))
                return null;

            var key = CacheKey(resRef, maxWidth, cropMode);
            if (_cache.TryGet(key, out var cached))
                return cached;

            // Hundreds of item-part tiles can be published at once. Queue only a small number of
            // decodes concurrently so the editor remains responsive instead of flooding the thread
            // pool and memory bus with every texture in the gallery.
            await _decodeSlots.WaitAsync().ConfigureAwait(false);
            ScaledImage? scaled;
            try
            {
                // Decode and scale on a worker; only the bitmap handoff needs the UI thread.
                scaled = await Task.Run(
                    () => Decode(resRef, maxWidth, cropMode)).ConfigureAwait(false);
            }
            finally
            {
                _decodeSlots.Release();
            }

            var bitmap = scaled == null ? null : await ToBitmapAsync(scaled).ConfigureAwait(true);

            _cache.Set(key, bitmap);

            return bitmap;
        }

        /// <summary>An already-decoded bitmap, or null. Never starts work — for binding without waiting.</summary>
        public Bitmap? Cached(
            string? resRef,
            int maxWidth,
            bool cropTransparentCanvas = false)
        {
            return CachedInternal(
                resRef,
                maxWidth,
                cropTransparentCanvas ? ImageCropMode.TransparentCanvas : ImageCropMode.None);
        }

        private Bitmap? CachedInternal(string? resRef, int maxWidth, ImageCropMode cropMode)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return null;

            return _cache.TryGet(CacheKey(resRef, maxWidth, cropMode), out var cached) ? cached : null;
        }

        private static ImageCropMode CropMode(BehaviorChoice choice, bool cropTransparentCanvas) =>
            cropTransparentCanvas
                ? ImageCropMode.TransparentCanvas
                : choice.ImageCrop == BehaviorChoiceImageCrop.NeverwinterPortrait
                    ? ImageCropMode.NeverwinterPortrait
                    : ImageCropMode.None;

        private static string CacheKey(string resRef, int maxWidth, ImageCropMode cropMode) =>
            $"{resRef}@{maxWidth}:{cropMode}";

        private ScaledImage? Decode(string resRef, int maxWidth, ImageCropMode cropMode)
        {
            try
            {
                if (TextureLoader.Load(_resources!, resRef) is not { } texture)
                    return null;

                texture = cropMode switch
                {
                    ImageCropMode.TransparentCanvas => CropTransparentCanvas(texture),
                    ImageCropMode.NeverwinterPortrait => CropNeverwinterPortrait(texture),
                    _ => texture
                };

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
        /// Removes empty alpha around artwork whose source canvas is much larger than the visible
        /// subject. Composite item icons deliberately occupy only their top, middle, or bottom
        /// slice of a full inventory-icon canvas; showing that whole canvas makes small hilts and
        /// connectors look like dots. The ordinary preview path keeps the authored canvas.
        /// </summary>
        private static TextureImage CropTransparentCanvas(TextureImage texture)
        {
            const byte visibleAlphaThreshold = 8;

            var left = texture.Width;
            var top = texture.Height;
            var right = -1;
            var bottom = -1;

            for (var y = 0; y < texture.Height; y++)
            {
                for (var x = 0; x < texture.Width; x++)
                {
                    var alpha = texture.Pixels[(y * texture.Width + x) * 4 + 3];
                    if (alpha <= visibleAlphaThreshold)
                        continue;

                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x);
                    bottom = Math.Max(bottom, y);
                }
            }

            if (right < left || bottom < top ||
                (left == 0 && top == 0 && right == texture.Width - 1 && bottom == texture.Height - 1))
            {
                return texture;
            }

            var width = right - left + 1;
            var height = bottom - top + 1;
            var pixels = new byte[checked(width * height * 4)];
            var rowBytes = width * 4;

            for (var y = 0; y < height; y++)
            {
                Buffer.BlockCopy(
                    texture.Pixels,
                    ((top + y) * texture.Width + left) * 4,
                    pixels,
                    y * rowBytes,
                    rowBytes);
            }

            return new TextureImage
            {
                Width = width,
                Height = height,
                Pixels = pixels,
                SourceFormat = texture.SourceFormat,
                AlphaMean = texture.AlphaMean
            };
        }

        /// <summary>
        /// NWN portrait textures reserve the bottom 28 pixels of a 64x128 medium portrait for
        /// engine data. The game displays only the 64x100 picture (a 16:25 aspect ratio), and the
        /// Toolset should do the same at every portrait resolution.
        /// </summary>
        private static TextureImage CropNeverwinterPortrait(TextureImage texture)
        {
            var visibleHeight = Math.Min(
                texture.Height,
                Math.Max(1, (texture.Width * 25 + 15) / 16));
            if (visibleHeight >= texture.Height)
                return texture;

            var pixels = new byte[checked(texture.Width * visibleHeight * 4)];
            Buffer.BlockCopy(texture.Pixels, 0, pixels, 0, pixels.Length);
            return new TextureImage
            {
                Width = texture.Width,
                Height = visibleHeight,
                Pixels = pixels,
                SourceFormat = texture.SourceFormat,
                AlphaMean = texture.AlphaMean
            };
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
