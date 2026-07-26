using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>
    /// Decodes a choice's artwork resref into a bitmap the picker can show — the load screens being
    /// the reason it exists. Results are cached, including the misses, because a resref that does not
    /// resolve will not resolve on the next scroll either.
    /// </summary>
    public sealed class ChoicePreviewService
    {
        private readonly ResourceIndex? _resources;
        private readonly Dictionary<string, Bitmap?> _cache = new(StringComparer.OrdinalIgnoreCase);

        public ChoicePreviewService(ResourceIndex? resources)
        {
            _resources = resources;
        }

        /// <summary>The artwork for a resref, or null when there is none or it cannot be decoded.</summary>
        public Bitmap? Resolve(string? resRef)
        {
            if (_resources == null || string.IsNullOrWhiteSpace(resRef))
                return null;

            if (_cache.TryGetValue(resRef, out var cached))
                return cached;

            Bitmap? bitmap = null;
            try
            {
                if (TextureLoader.Load(_resources, resRef) is { } texture)
                    bitmap = ToBitmap(texture);
            }
            catch (Exception)
            {
                // A picker must never be the thing that takes the editor down; a missing preview
                // degrades to the name it already shows.
                bitmap = null;
            }

            _cache[resRef] = bitmap;
            return bitmap;
        }

        /// <summary>
        /// TextureLoader hands back straight RGBA; Avalonia wants BGRA, so the channels swap on the
        /// way in rather than every time the image is drawn.
        /// </summary>
        private static Bitmap ToBitmap(TextureImage texture)
        {
            var bgra = new byte[texture.Pixels.Length];
            for (var i = 0; i < texture.Pixels.Length; i += 4)
            {
                bgra[i] = texture.Pixels[i + 2];
                bgra[i + 1] = texture.Pixels[i + 1];
                bgra[i + 2] = texture.Pixels[i];
                bgra[i + 3] = texture.Pixels[i + 3];
            }

            var bitmap = new WriteableBitmap(
                new Avalonia.PixelSize(texture.Width, texture.Height),
                new Avalonia.Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using var buffer = bitmap.Lock();
            var stride = texture.Width * 4;
            for (var y = 0; y < texture.Height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(
                    bgra, y * stride, buffer.Address + y * buffer.RowBytes, stride);
            }

            return bitmap;
        }
    }
}
