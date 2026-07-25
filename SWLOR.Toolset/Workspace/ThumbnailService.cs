using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Produces and caches blueprint thumbnails for the palette.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rendering happens off the UI thread through <see cref="ThumbnailRenderer"/>, which is a software
    /// rasterizer precisely so this can run on a thread pool while the builder keeps working. Results
    /// are cached in memory by resref; a module has thousands of blueprints but a builder only ever
    /// looks at a few hundred in one sitting.
    /// </para>
    /// <para>
    /// Everything here degrades to null rather than throwing. A blueprint whose model cannot be resolved
    /// - most often because it lives in a hak this install has not indexed - simply keeps the palette's
    /// letter placeholder, which is a worse tile but not a broken one.
    /// </para>
    /// </remarks>
    public sealed class ThumbnailService
    {
        /// <summary>Rendered at a fixed size and scaled by the view, so changing the size slider costs nothing.</summary>
        public const int RenderSize = 128;

        private readonly WorkspaceContext _workspaceContext;
        private readonly TileModelCache? _models;
        private readonly AppearanceService? _appearances;
        private readonly PlaceableAppearanceService? _placeables;
        private readonly DoorTypeService? _doors;

        private readonly ConcurrentDictionary<string, Bitmap?> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _inFlight = new(StringComparer.OrdinalIgnoreCase);

        public ThumbnailService(
            WorkspaceContext workspaceContext,
            TileModelCache? models = null,
            AppearanceService? appearances = null,
            PlaceableAppearanceService? placeables = null,
            DoorTypeService? doors = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _models = models;
            _appearances = appearances;
            _placeables = placeables;
            _doors = doors;
        }

        /// <summary>True when the game data needed to resolve models is available at all.</summary>
        public bool IsAvailable => _models != null;

        /// <summary>The cached thumbnail, or null when it has not been rendered yet.</summary>
        public Bitmap? Cached(ResourceType type, string resRef) =>
            _cache.TryGetValue(Key(type, resRef), out var bitmap) ? bitmap : null;

        /// <summary>
        /// Renders a thumbnail off the UI thread, calling <paramref name="onReady"/> on completion when
        /// one was produced. Requests already cached or already running are dropped, so a palette that
        /// re-publishes its tiles on every keystroke does not queue the same work repeatedly.
        /// </summary>
        public void RequestAsync(ResourceType type, string resRef, Action<Bitmap> onReady)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(resRef))
                return;

            var key = Key(type, resRef);
            if (_cache.ContainsKey(key) || !_inFlight.TryAdd(key, 0))
                return;

            Task.Run(() =>
            {
                Bitmap? bitmap = null;
                try
                {
                    bitmap = Render(type, resRef);
                }
                catch
                {
                    // A single unreadable model must not take the batch - the tile keeps its placeholder.
                }
                finally
                {
                    _cache[key] = bitmap;
                    _inFlight.TryRemove(key, out _);
                }

                if (bitmap != null)
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => onReady(bitmap));
            });
        }

        private Bitmap? Render(ResourceType type, string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || _models == null)
                return null;

            var document = workspace.LoadBlueprint(type, resRef);
            var reference = BlueprintModelResolver.Resolve(
                type, document.Fields, _appearances, _placeables, _doors);

            // Segmented creatures need their parts composed, which is the GL preview's job; only the
            // single-model kinds are rendered here rather than half-drawing a body.
            if (reference.Kind != BlueprintModelKind.Simple || reference.ModelResRef == null)
                return null;

            var pixels = ThumbnailRenderer.Render(_models.GetOrBuild(reference.ModelResRef), RenderSize);
            return pixels == null ? null : ToBitmap(pixels);
        }

        private static Bitmap ToBitmap(byte[] pixels)
        {
            var bitmap = new WriteableBitmap(
                new Avalonia.PixelSize(RenderSize, RenderSize),
                new Avalonia.Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using var buffer = bitmap.Lock();
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, buffer.Address, pixels.Length);
            return bitmap;
        }

        private static string Key(ResourceType type, string resRef) => $"{type}:{resRef}";
    }
}
