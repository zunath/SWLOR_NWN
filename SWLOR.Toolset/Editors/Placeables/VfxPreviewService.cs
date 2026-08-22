using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// Lazily downloads the project-curated NWN Lexicon screenshots used by the VFX gallery.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One instance for the process, so the same picture is fetched and decoded once no matter how
    /// many placeables, behavior pages, or galleries ask for it.
    /// </para>
    /// <para>
    /// Two things bound what that costs. Each image is decoded straight to the width the gallery
    /// draws it at rather than at its published resolution: the reference sheet names 524 effects,
    /// and holding those at full size would be hundreds of megabytes of pixels to show a 158-pixel
    /// tile. And the decoded set is capped, so a builder who scrolls the whole gallery does not
    /// leave every frame of it resident for the rest of the session.
    /// </para>
    /// </remarks>
    public sealed class VfxPreviewService
    {
        /// <summary>
        /// Width the gallery draws a preview at. Decoding to it rather than scaling afterwards is
        /// the difference between ~60 KB and ~1.5 MB per effect.
        /// </summary>
        public const int PreviewWidth = 256;

        /// <summary>
        /// Decoded previews held at once. The gallery pages 48 tiles at a time, so this is several
        /// screens of scrollback; at this width it is roughly 60 MB worst case.
        /// </summary>
        private const int MemoryCacheCapacity = 256;

        private static readonly HttpClient Client = CreateClient();
        private static readonly SemaphoreSlim DownloadGate = new(initialCount: 4, maxCount: 4);

        private readonly BitmapMemoryCache _cache = new(MemoryCacheCapacity);
        private readonly ConcurrentDictionary<string, byte> _failed =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<Action<Bitmap>>> _inFlight =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new();

        public Bitmap? Cached(string? imageUrl) =>
            !string.IsNullOrWhiteSpace(imageUrl) && _cache.TryGet(imageUrl, out var bitmap)
                ? bitmap
                : null;

        public void RequestAsync(string? imageUrl, Action<Bitmap> onReady)
        {
            ArgumentNullException.ThrowIfNull(onReady);
            if (string.IsNullOrWhiteSpace(imageUrl) || _failed.ContainsKey(imageUrl))
                return;

            if (_cache.TryGet(imageUrl, out var cached) && cached != null)
            {
                Dispatcher.UIThread.Post(() => onReady(cached));
                return;
            }

            lock (_gate)
            {
                if (_inFlight.TryGetValue(imageUrl, out var waiters))
                {
                    waiters.Add(onReady);
                    return;
                }

                _inFlight[imageUrl] = new List<Action<Bitmap>> { onReady };
            }

            _ = LoadAsync(imageUrl);
        }

        private async Task LoadAsync(string imageUrl)
        {
            Bitmap? bitmap = null;
            await DownloadGate.WaitAsync().ConfigureAwait(false);
            try
            {
                var bytes = await Client.GetByteArrayAsync(imageUrl).ConfigureAwait(false);
                using var stream = new MemoryStream(bytes, writable: false);

                // Decoded at the size it is drawn. The full-resolution frame is never materialized,
                // so a gallery that has scrolled through every effect has not held one either.
                bitmap = Bitmap.DecodeToWidth(stream, PreviewWidth);
                _cache.Set(imageUrl, bitmap);
            }
            catch (Exception)
            {
                // The gallery keeps its text/glyph fallback when a documentation image is offline.
                _failed[imageUrl] = 0;
            }
            finally
            {
                DownloadGate.Release();
            }

            List<Action<Bitmap>> waiters;
            lock (_gate)
            {
                waiters = _inFlight.Remove(imageUrl, out var pending)
                    ? pending
                    : new List<Action<Bitmap>>();
            }

            if (bitmap == null)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                foreach (var waiter in waiters)
                    waiter(bitmap);
            });
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SWLOR-Toolset/1.0");
            return client;
        }
    }
}
