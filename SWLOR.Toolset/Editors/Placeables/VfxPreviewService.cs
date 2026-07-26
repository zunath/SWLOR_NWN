using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>
    /// Lazily downloads the project-curated NWN Lexicon screenshots used by the VFX gallery.
    /// Only visible choices are requested, and each URL is decoded once for the process.
    /// </summary>
    public sealed class VfxPreviewService
    {
        private static readonly HttpClient Client = CreateClient();
        private static readonly SemaphoreSlim DownloadGate = new(initialCount: 4, maxCount: 4);
        private readonly ConcurrentDictionary<string, Bitmap> _cache =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _failed =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<Action<Bitmap>>> _inFlight =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new();

        public Bitmap? Cached(string? imageUrl) =>
            !string.IsNullOrWhiteSpace(imageUrl) && _cache.TryGetValue(imageUrl, out var bitmap)
                ? bitmap
                : null;

        public void RequestAsync(string? imageUrl, Action<Bitmap> onReady)
        {
            ArgumentNullException.ThrowIfNull(onReady);
            if (string.IsNullOrWhiteSpace(imageUrl) || _failed.ContainsKey(imageUrl))
                return;

            if (_cache.TryGetValue(imageUrl, out var cached))
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
                bitmap = new Bitmap(stream);
                _cache[imageUrl] = bitmap;
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
