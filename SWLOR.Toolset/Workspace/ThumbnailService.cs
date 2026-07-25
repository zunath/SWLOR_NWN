using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Supplies palette tiles with their preview images, and owns the two caches that keep that cheap.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three layers, in order: a bounded in-memory cache of decoded bitmaps, a persistent PNG cache on
    /// disk, and - only when both miss - an actual render on a thread-pool thread. The module has some
    /// 17,000 blueprints, so the disk layer is what makes previews feel instant on the second launch and
    /// the memory bound is what stops the first one from turning a gigabyte of pixels into a resident
    /// set.
    /// </para>
    /// <para>
    /// Every tile gets an image. Blueprints with no artwork of their own - merchants, triggers, sound
    /// sets, waypoints, and the placeables whose 2DA appearance row is blank - resolve to their type's
    /// symbol, which is drawn once and shared, so the grid never falls back to a bare letter while game
    /// data is loaded.
    /// </para>
    /// </remarks>
    public sealed class ThumbnailService
    {
        /// <summary>
        /// Decoded bitmaps held at once. A palette shows at most a couple of hundred tiles, so this is
        /// several screens of history; at 128px square it is roughly 65 MB worst case.
        /// </summary>
        private const int MemoryCacheCapacity = 1024;

        /// <summary>Square size the type symbols are drawn at for a tile's fallback image.</summary>
        private const int TypeIconSize = 128;

        /// <summary>Square size for the palette's type-row chips.</summary>
        private const int TypeChipIconSize = 20;

        /// <summary>Upper bound on concurrent renders during a cache build, regardless of core count.</summary>
        private const int MaxBuildWorkers = 4;

        private readonly WorkspaceContext _workspaceContext;
        private readonly BlueprintPreviewRenderer _renderer;

        private readonly BitmapMemoryCache _memory = new(MemoryCacheCapacity);

        /// <summary>
        /// Renders already running, each holding every caller still waiting on that key.
        /// </summary>
        /// <remarks>
        /// A list of waiters rather than a bare "is running" flag, because two visible cells can want the
        /// same image: a tileset's groups routinely share a preview model, and four groups asking for
        /// fci01_b01_01 at once used to leave three of them permanently blank - the second request saw the
        /// first in flight and returned without ever being called back.
        /// </remarks>
        private readonly ConcurrentDictionary<string, List<Action<Bitmap>>> _inFlight =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<ResourceType, Bitmap> _typeIcons = new();
        private readonly ConcurrentDictionary<ResourceType, Bitmap> _typeChipIcons = new();

        private readonly object _diskGate = new();
        private ThumbnailDiskCache _disk = new(null);
        private string? _diskModuleRoot;

        public ThumbnailService(WorkspaceContext workspaceContext, BlueprintPreviewRenderer renderer)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        /// <summary>True when game data is loaded well enough to produce any preview at all.</summary>
        public bool IsAvailable => _renderer.IsAvailable;

        /// <summary>Where previews are cached on disk, for the Output log; null when caching is off.</summary>
        public string? CachePath => Disk.RootPath;

        /// <summary>
        /// The preview for a blueprint if it is already decoded in memory, else null. Callers use this to
        /// fill a tile without a round trip through the thread pool.
        /// </summary>
        public Bitmap? Cached(ResourceType type, string resRef)
        {
            if (!IsAvailable)
                return null;

            return _memory.TryGet(Key(type, resRef), out var bitmap)
                ? bitmap ?? TypeIcon(type)
                : null;
        }

        /// <summary>
        /// Resolves a preview off the UI thread and calls <paramref name="onReady"/> on the UI thread with
        /// the result - real artwork when there is any, the type symbol when there is not. A request for
        /// something already rendering joins it rather than queueing a second render, so a palette that
        /// republishes its tiles on every keystroke does not repeat the work.
        /// </summary>
        public void RequestAsync(ResourceType type, string resRef, Action<Bitmap> onReady)
        {
            ArgumentNullException.ThrowIfNull(onReady);

            if (!IsAvailable || string.IsNullOrWhiteSpace(resRef))
                return;

            var key = Key(type, resRef);
            if (_memory.TryGet(key, out var known))
            {
                var resolved = known ?? TypeIcon(type);
                Dispatcher.UIThread.Post(() => onReady(resolved));
                return;
            }

            if (!TryStartRender(key, onReady))
                return;

            Task.Run(() =>
            {
                Bitmap? bitmap = null;
                try
                {
                    bitmap = Resolve(type, resRef);
                }
                catch (Exception)
                {
                    // One bad blueprint must not stop the rest of the grid from filling in.
                }

                CompleteRender(key, bitmap, bitmap ?? TypeIcon(type));
            });
        }

        /// <summary>
        /// Resolves a thumbnail for a tile's model, calling <paramref name="onReady"/> on the UI thread.
        /// </summary>
        /// <remarks>
        /// Kept separate from the blueprint path because a tile is not a module resource: its "resref" is
        /// a model name out of a .set file, there is no file under Module\ to check a timestamp against,
        /// and a model name could collide with a blueprint resref. So these are cached in memory only,
        /// under their own key prefix - a tileset's few hundred models are cheap to re-render on the next
        /// launch, and never writing them to the module's preview cache keeps that cache honest.
        /// </remarks>
        public void RequestTileAsync(string modelResRef, Action<Bitmap> onReady)
        {
            ArgumentNullException.ThrowIfNull(onReady);

            if (!IsAvailable || string.IsNullOrWhiteSpace(modelResRef))
                return;

            var key = "tile:" + modelResRef;
            if (_memory.TryGet(key, out var known))
            {
                if (known != null)
                    Dispatcher.UIThread.Post(() => onReady(known));

                return;
            }

            if (!TryStartRender(key, onReady))
                return;

            Task.Run(() =>
            {
                Bitmap? bitmap = null;
                try
                {
                    var image = _renderer.RenderModel(modelResRef);
                    if (image != null)
                        bitmap = ToBitmap(image);
                }
                catch (Exception)
                {
                    // One unparseable tile model must not stop the rest of the grid filling in.
                }

                CompleteRender(key, bitmap, bitmap);
            });
        }

        /// <summary>
        /// Registers <paramref name="onReady"/> as a waiter on <paramref name="key"/>, and reports whether
        /// this caller is the one that has to do the render.
        /// </summary>
        private bool TryStartRender(string key, Action<Bitmap> onReady)
        {
            var mine = new List<Action<Bitmap>> { onReady };
            var waiters = _inFlight.GetOrAdd(key, mine);
            if (ReferenceEquals(waiters, mine))
                return true;

            // Someone else is already rendering this. Join their list - but a render that finished between
            // the GetOrAdd and this lock has already published its result and cleared its own waiters, so
            // that case has to be answered from the cache instead of by waiting forever.
            lock (waiters)
            {
                if (!_inFlight.ContainsKey(key))
                {
                    if (_memory.TryGet(key, out var done) && done != null)
                        Dispatcher.UIThread.Post(() => onReady(done));

                    return false;
                }

                waiters.Add(onReady);
            }

            return false;
        }

        /// <summary>
        /// Publishes a finished render to the cache and to every caller that asked for it.
        /// </summary>
        /// <param name="cached">What to store - null records "no artwork", which is a real answer.</param>
        /// <param name="delivered">What to hand the waiters, or null to tell them nothing.</param>
        private void CompleteRender(string key, Bitmap? cached, Bitmap? delivered)
        {
            _memory.Set(key, cached);

            List<Action<Bitmap>>? waiters;
            if (!_inFlight.TryRemove(key, out waiters) || waiters == null)
                return;

            Action<Bitmap>[] callbacks;
            lock (waiters)
                callbacks = waiters.ToArray();

            if (delivered == null)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                foreach (var callback in callbacks)
                    callback(delivered);
            });
        }

        /// <summary>The cached tile thumbnail if it is already decoded, else null.</summary>
        public Bitmap? CachedTile(string modelResRef) =>
            _memory.TryGet("tile:" + modelResRef, out var bitmap) ? bitmap : null;

        /// <summary>
        /// The shared symbol for a blueprint type, drawn on first use. Shared rather than per-blueprint
        /// because thousands of tiles can want the same one and they are all identical.
        /// </summary>
        public Bitmap TypeIcon(ResourceType type) =>
            _typeIcons.GetOrAdd(type, key => ToBitmap(TypeIconRenderer.Render(key, TypeIconSize)));

        /// <summary>
        /// The same symbol at the palette type row's size. Rendered at the size it is shown at rather
        /// than scaled down from the tile version: at 20px the difference between a drawn-for-20px
        /// symbol and a downscaled 128px one is the difference between legible and mud.
        /// </summary>
        public Bitmap TypeChipIcon(ResourceType type) =>
            _typeChipIcons.GetOrAdd(type, key => ToBitmap(TypeIconRenderer.Render(key, TypeChipIconSize)));

        private Bitmap? _tileChipIcon;

        /// <summary>
        /// The Tiles chip's icon. Tiles have no <see cref="ResourceType"/>, so they cannot use the symbol
        /// table above; the generic plate the renderer falls back to reads as "a flat piece of ground",
        /// which is what a tile is.
        /// </summary>
        public Bitmap TileChipIcon() =>
            _tileChipIcon ??= ToBitmap(TypeIconRenderer.Render(ResourceType.Area, TypeChipIconSize));

        /// <summary>
        /// Renders and stores every missing preview for the open module, reporting progress as it goes.
        /// Deliberately does not populate the in-memory cache: this walks the whole module, and holding
        /// its output would defeat the point of bounding that cache.
        /// </summary>
        public async Task<PreviewCacheProgress> WarmAsync(
            IProgress<PreviewCacheProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || !IsAvailable)
                return new PreviewCacheProgress(0, 0, 0, 0, 0);

            var work = new List<(ResourceType Type, string ResRef)>();
            foreach (var type in ModuleWorkspace.BlueprintTypes.Where(BlueprintPreviewRenderer.IsSupported))
            {
                foreach (var resRef in workspace.EnumerateResRefs(type))
                    work.Add((type, resRef));
            }

            var disk = Disk;
            var total = work.Count;
            var processed = 0;
            var rendered = 0;
            var reused = 0;
            var withoutArtwork = 0;
            var failed = 0;
            var lastReportedPercent = -1;

            // Capped low on purpose, and not scaled to core count. Each worker can hold a fully expanded
            // model mesh while it rasterizes, so parallelism here buys throughput in units of tens of
            // megabytes - and this runs while the builder is working, where a preview build that makes the
            // editor stutter is worse than one that takes a little longer.
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount - 1, 1, MaxBuildWorkers),
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(work, options, (item, _) =>
            {
                try
                {
                    if (disk.Contains(item.Type, item.ResRef, workspace.GetResourcePath(item.Type, item.ResRef)))
                    {
                        Interlocked.Increment(ref reused);
                    }
                    else
                    {
                        var image = _renderer.Render(item.Type, item.ResRef);
                        if (image == null)
                        {
                            disk.StoreNoArtwork(item.Type, item.ResRef);
                            Interlocked.Increment(ref withoutArtwork);
                        }
                        else
                        {
                            // Disposed immediately: it exists only to be encoded, and 17,000 live
                            // bitmaps is exactly what this cache is designed to avoid.
                            using var bitmap = ToBitmap(image);
                            disk.Store(item.Type, item.ResRef, bitmap);
                            Interlocked.Increment(ref rendered);
                        }
                    }
                }
                catch (Exception)
                {
                    // Counted as processed but deliberately not marked: a failure here is a failure to
                    // render, not proof that there is nothing to render, so the next build tries again.
                    Interlocked.Increment(ref failed);
                }

                var done = Interlocked.Increment(ref processed);
                if (progress != null)
                {
                    var percent = (int)(done * 100L / Math.Max(1, total));
                    if (percent != Volatile.Read(ref lastReportedPercent) && percent % 5 == 0)
                    {
                        Volatile.Write(ref lastReportedPercent, percent);
                        progress.Report(new PreviewCacheProgress(
                            done, total, Volatile.Read(ref rendered),
                            Volatile.Read(ref reused), Volatile.Read(ref withoutArtwork),
                            Volatile.Read(ref failed)));
                    }
                }

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

            return new PreviewCacheProgress(processed, total, rendered, reused, withoutArtwork, failed);
        }

        /// <summary>Drops every cached preview, in memory and on disk, so the next build redoes them.</summary>
        public int ClearCache()
        {
            _memory.Clear();
            return Disk.Clear();
        }

        /// <summary>Deletes cache folders left by an older render pipeline. Returns the number removed.</summary>
        public int PruneSupersededCaches() => Disk.PruneSupersededVersions();

        /// <summary>Disk hit, then render. Null means "no artwork" - the caller substitutes a type symbol.</summary>
        private Bitmap? Resolve(ResourceType type, string resRef)
        {
            var workspace = _workspaceContext.Workspace;
            var blueprintPath = workspace?.GetResourcePath(type, resRef);
            var disk = Disk;

            switch (disk.TryLoad(type, resRef, blueprintPath, out var cached))
            {
                case ThumbnailDiskCache.Lookup.Image:
                    return cached;
                case ThumbnailDiskCache.Lookup.NoArtwork:
                    return null;
            }

            var image = _renderer.Render(type, resRef);
            if (image == null)
            {
                disk.StoreNoArtwork(type, resRef);
                return null;
            }

            var bitmap = ToBitmap(image);
            disk.Store(type, resRef, bitmap);
            return bitmap;
        }

        /// <summary>
        /// The disk cache for whichever module is open, rebuilt when that changes - the service is a
        /// singleton but the module root is only known after startup, and can be reopened elsewhere.
        /// </summary>
        private ThumbnailDiskCache Disk
        {
            get
            {
                var moduleRoot = _workspaceContext.Workspace?.ModuleRoot;
                lock (_diskGate)
                {
                    if (!string.Equals(moduleRoot, _diskModuleRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        _diskModuleRoot = moduleRoot;
                        _disk = new ThumbnailDiskCache(moduleRoot);
                    }

                    return _disk;
                }
            }
        }

        /// <summary>
        /// Copies straight-alpha BGRA into a bitmap row by row, because a locked framebuffer's stride is
        /// allowed to exceed its row width and a single block copy would shear the image when it does.
        /// </summary>
        private static Bitmap ToBitmap(IconImage image)
        {
            var bitmap = new WriteableBitmap(
                new Avalonia.PixelSize(image.Width, image.Height),
                new Avalonia.Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using var buffer = bitmap.Lock();
            for (var y = 0; y < image.Height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(
                    image.Bgra,
                    y * image.Stride,
                    buffer.Address + y * buffer.RowBytes,
                    image.Stride);
            }

            return bitmap;
        }

        private static string Key(ResourceType type, string resRef) => $"{type}:{resRef}";
    }
}
