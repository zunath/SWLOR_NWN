using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
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
        private readonly IPreviewImageSource _renderer;

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
        private readonly ConcurrentDictionary<string, InFlightRender> _inFlight =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _versions =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, object> _keyGates =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ReaderWriterLockSlim _cacheGate = new();
        private long _cacheEpoch;

        private sealed class InFlightRender
        {
            public InFlightRender(long version, long epoch, Action<Bitmap> firstWaiter)
            {
                Version = version;
                Epoch = epoch;
                Waiters.Add(firstWaiter);
            }

            public long Version { get; }
            public long Epoch { get; }
            public List<Action<Bitmap>> Waiters { get; } = new();
        }

        private readonly record struct PreviewResolution(
            Bitmap? Bitmap,
            ThumbnailDiskCache Disk,
            ResourceType Type,
            string ResRef,
            bool UseIndexedBlueprint,
            bool Persist);
        private readonly ConcurrentDictionary<ResourceType, Bitmap> _typeIcons = new();
        private readonly ConcurrentDictionary<ResourceType, Bitmap> _typeChipIcons = new();

        private readonly object _diskGate = new();
        private ThumbnailDiskCache _disk = new(null);
        private string? _diskModuleRoot;

        public ThumbnailService(WorkspaceContext workspaceContext, IPreviewImageSource renderer)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            // A saved blueprint may look different. The memory cache is consulted before the disk
            // cache's timestamp check, so without this an edited appearance or icon kept showing the old
            // picture - including a cached "no artwork" symbol - for the rest of the session.
            _workspaceContext.CatalogEntryRefreshed += Invalidate;
            _workspaceContext.WorkspaceOpened += ResetForWorkspace;
        }

        /// <summary>
        /// Raised whenever a blueprint's cached preview is dropped, naming the resource that changed.
        /// </summary>
        /// <remarks>
        /// The cache invalidation above only affects requests that have not happened yet: a
        /// <see cref="Shell.Panels.PaletteTileViewModel"/> already showing the old picture keeps it,
        /// because nothing told it the picture is stale. <see cref="Shell.Panels.PaletteViewModel"/>
        /// subscribes to this so a currently visible tile drops its stale <c>Preview</c> and
        /// <c>PreviewRequested</c> flag and asks again immediately, instead of the tile only refreshing
        /// the next time its category is closed and reopened.
        /// </remarks>
        public event Action<ResourceType, string>? InvalidatedForResRef;

        /// <summary>Forgets a blueprint's cached preview, so the next request re-renders it.</summary>
        public void Invalidate(ResourceType type, string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return;

            if (type == ResourceType.Uti)
                InvalidateCreaturesUsingItem(resRef);

            foreach (var useIndexedBlueprint in new[] { false, true })
                InvalidateOne(type, resRef, useIndexedBlueprint);
        }

        private void InvalidateOne(ResourceType type, string resRef, bool useIndexedBlueprint)
        {
            _cacheGate.EnterReadLock();
            try
            {
                var key = Key(type, resRef, useIndexedBlueprint);
                lock (GateFor(key))
                {
                    _versions.AddOrUpdate(key, 1, (_, version) => version + 1);
                    _inFlight.TryRemove(key, out _);
                    _memory.Remove(key);
                    Disk.Remove(type, resRef, useIndexedBlueprint);
                }
            }
            finally
            {
                _cacheGate.ExitReadLock();
            }

            // Fired for both the custom and the standard cache identity, and for every dependency this
            // resref invalidated (a creature wearing an edited item) - the palette does not care which
            // cache variant changed, only that this resref's picture is no longer trustworthy.
            InvalidatedForResRef?.Invoke(type, resRef);
        }

        private void InvalidateCreaturesUsingItem(string itemResRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            foreach (var creatureResRef in workspace.EnumerateResRefs(ResourceType.Utc))
            {
                try
                {
                    if (!workspace.TryLoadBlueprint(
                            ResourceType.Utc, creatureResRef, out var creatureBlueprint))
                    {
                        continue;
                    }

                    var creature = creatureBlueprint.Fields;
                    if (string.Equals(
                            BlueprintModelResolver.GetEquippedChestArmorResRef(creature),
                            itemResRef,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        // A loose module UTI cannot affect the independent Standard-content preview.
                        InvalidateOne(ResourceType.Utc, creatureResRef, useIndexedBlueprint: false);
                    }
                }
                catch (Exception)
                {
                    // A malformed creature is independently unrenderable and must not prevent the
                    // remaining dependency invalidations.
                }
            }
        }

        private void ResetForWorkspace()
        {
            _cacheGate.EnterWriteLock();
            try
            {
                Interlocked.Increment(ref _cacheEpoch);
                _inFlight.Clear();
                _memory.Clear();
            }
            finally
            {
                _cacheGate.ExitWriteLock();
            }
        }

        /// <summary>True when game data is loaded well enough to produce any preview at all.</summary>
        public bool IsAvailable => _renderer.IsAvailable;

        /// <summary>Where previews are cached on disk, for the Output log; null when caching is off.</summary>
        public string? CachePath => Disk.RootPath;

        /// <summary>
        /// The preview for a blueprint if it is already decoded in memory, else null. Callers use this to
        /// fill a tile without a round trip through the thread pool.
        /// </summary>
        public Bitmap? Cached(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint = false)
        {
            if (!IsAvailable)
                return null;

            return _memory.TryGet(Key(type, resRef, useIndexedBlueprint), out var bitmap)
                ? bitmap ?? TypeIcon(type)
                : null;
        }

        /// <summary>
        /// Resolves a preview off the UI thread and calls <paramref name="onReady"/> on the UI thread with
        /// the result - real artwork when there is any, the type symbol when there is not. A request for
        /// something already rendering joins it rather than queueing a second render, so a palette that
        /// republishes its tiles on every keystroke does not repeat the work.
        /// </summary>
        public void RequestAsync(
            ResourceType type,
            string resRef,
            Action<Bitmap> onReady) =>
            RequestAsync(type, resRef, useIndexedBlueprint: false, onReady: onReady);

        public void RequestAsync(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint,
            Action<Bitmap> onReady)
        {
            ArgumentNullException.ThrowIfNull(onReady);

            if (!IsAvailable || string.IsNullOrWhiteSpace(resRef))
                return;

            var key = Key(type, resRef, useIndexedBlueprint);
            if (_memory.TryGet(key, out var known))
            {
                var resolved = known ?? TypeIcon(type);
                Dispatcher.UIThread.Post(() => onReady(resolved));
                return;
            }

            var fallback = TypeIcon(type);
            if (!TryStartRender(key, onReady, fallback, out var operation))
                return;

            Task.Run(() =>
            {
                PreviewResolution? result = null;
                try
                {
                    result = Resolve(type, resRef, useIndexedBlueprint);
                }
                catch (Exception)
                {
                    // One bad blueprint must not stop the rest of the grid from filling in.
                }

                CompleteRender(key, operation, result, result?.Bitmap ?? fallback);
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
        public void RequestTileAsync(
            string modelResRef,
            Action<Bitmap> onReady,
            IReadOnlyList<string>? footprintModelResRefs = null,
            int columns = 1,
            int rows = 1)
        {
            ArgumentNullException.ThrowIfNull(onReady);

            if (!IsAvailable || string.IsNullOrWhiteSpace(modelResRef))
                return;

            var composite = IsCompositeFootprint(footprintModelResRefs, columns, rows);
            var key = TileKey(modelResRef, footprintModelResRefs, columns, rows);
            if (_memory.TryGet(key, out var known))
            {
                if (known != null)
                    Dispatcher.UIThread.Post(() => onReady(known));

                return;
            }

            if (!TryStartRender(key, onReady, null, out var operation))
                return;

            Task.Run(() =>
            {
                Bitmap? bitmap = null;
                try
                {
                    var image = composite
                        ? _renderer.RenderTileGroup(footprintModelResRefs!, columns, rows)
                          ?? _renderer.RenderModel(modelResRef)
                        : _renderer.RenderModel(modelResRef);
                    if (image != null)
                        bitmap = ToBitmap(image);
                }
                catch (Exception)
                {
                    // One unparseable tile model must not stop the rest of the grid filling in.
                }

                var result = new PreviewResolution(
                    bitmap, new ThumbnailDiskCache(null), ResourceType.Area, modelResRef,
                    UseIndexedBlueprint: false, Persist: false);
                CompleteRender(key, operation, result, bitmap);
            });
        }

        /// <summary>
        /// Resolves the thumbnail for one <c>appearance.2da</c> row, calling
        /// <paramref name="onReady"/> on the UI thread.
        /// </summary>
        /// <remarks>
        /// Kept beside the tile path rather than the blueprint path for the same reason that one is:
        /// an appearance row is not a module resource, so there is no file to check a timestamp
        /// against and nothing that belongs in the module's on-disk preview cache. Memory only, under
        /// its own key prefix — a row id could otherwise collide with a resref.
        /// </remarks>
        public void RequestAppearanceAsync(int appearanceId, Action<Bitmap> onReady)
        {
            ArgumentNullException.ThrowIfNull(onReady);

            if (!IsAvailable || appearanceId < 0)
                return;

            var key = AppearanceKey(appearanceId);
            if (_memory.TryGet(key, out var known))
            {
                if (known != null)
                    Dispatcher.UIThread.Post(() => onReady(known));

                return;
            }

            if (!TryStartRender(key, onReady, null, out var operation))
                return;

            Task.Run(() =>
            {
                Bitmap? bitmap = null;
                try
                {
                    if (_renderer.RenderCreatureAppearance(appearanceId) is { } image)
                        bitmap = ToBitmap(image);
                }
                catch (Exception)
                {
                    // One unrenderable appearance row must not stop the rest of the grid filling in.
                }

                var result = new PreviewResolution(
                    bitmap, new ThumbnailDiskCache(null), ResourceType.Utc,
                    appearanceId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    UseIndexedBlueprint: false, Persist: false);
                CompleteRender(key, operation, result, bitmap);
            });
        }

        /// <summary>The cached appearance-row thumbnail if it is already decoded, else null.</summary>
        public Bitmap? CachedAppearance(int appearanceId) =>
            _memory.TryGet(AppearanceKey(appearanceId), out var bitmap) ? bitmap : null;

        private static string AppearanceKey(int appearanceId) =>
            "appearance:" + appearanceId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// Registers <paramref name="onReady"/> as a waiter on <paramref name="key"/>, and reports whether
        /// this caller is the one that has to do the render.
        /// </summary>
        private bool TryStartRender(
            string key,
            Action<Bitmap> onReady,
            Bitmap? fallback,
            out InFlightRender operation)
        {
            var mine = new InFlightRender(VersionOf(key), Volatile.Read(ref _cacheEpoch), onReady);
            operation = _inFlight.GetOrAdd(key, mine);
            if (ReferenceEquals(operation, mine))
                return true;

            // Someone else is already rendering this. Join their list - but a render that finished between
            // the GetOrAdd and this lock has already published its result and cleared its own waiters, so
            // that case has to be answered from the cache instead of by waiting forever.
            lock (operation.Waiters)
            {
                if (!_inFlight.TryGetValue(key, out var active) || !ReferenceEquals(active, operation))
                {
                    var resolved = _memory.TryGet(key, out var done) ? done ?? fallback : fallback;
                    if (resolved != null)
                        Dispatcher.UIThread.Post(() => onReady(resolved));

                    return false;
                }

                operation.Waiters.Add(onReady);
            }

            return false;
        }

        /// <summary>
        /// Publishes a finished render to the cache and to every caller that asked for it.
        /// </summary>
        /// <param name="result">What to cache; null means rendering failed and must be retried later.</param>
        /// <param name="delivered">What to hand the waiters, or null to tell them nothing.</param>
        private void CompleteRender(
            string key,
            InFlightRender operation,
            PreviewResolution? result,
            Bitmap? delivered)
        {
            Action<Bitmap>[] callbacks;
            _cacheGate.EnterReadLock();
            try
            {
                lock (GateFor(key))
                {
                    if (!IsCurrent(key, operation))
                    {
                        result?.Bitmap?.Dispose();
                        return;
                    }

                    if (result is { } resolved)
                    {
                        if (resolved.Persist)
                        {
                            if (resolved.Bitmap == null)
                                resolved.Disk.StoreNoArtwork(
                                    resolved.Type, resolved.ResRef, resolved.UseIndexedBlueprint);
                            else
                                resolved.Disk.Store(
                                    resolved.Type, resolved.ResRef,
                                    resolved.UseIndexedBlueprint, resolved.Bitmap);
                        }

                        _memory.Set(key, resolved.Bitmap);
                    }

                    if (!_inFlight.TryRemove(key, out var removed) || !ReferenceEquals(removed, operation))
                        return;

                    lock (operation.Waiters)
                        callbacks = operation.Waiters.ToArray();
                }
            }
            finally
            {
                _cacheGate.ExitReadLock();
            }

            if (delivered == null)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                foreach (var callback in callbacks)
                    callback(delivered);
            });
        }

        private object GateFor(string key) => _keyGates.GetOrAdd(key, _ => new object());

        private long VersionOf(string key) => _versions.TryGetValue(key, out var version) ? version : 0;

        private bool IsCurrent(string key, InFlightRender operation)
        {
            return operation.Epoch == Volatile.Read(ref _cacheEpoch) &&
                   operation.Version == VersionOf(key) &&
                   _inFlight.TryGetValue(key, out var active) &&
                   ReferenceEquals(active, operation);
        }

        /// <summary>
        /// The cached tile thumbnail if it is already decoded, else null.
        /// </summary>
        /// <remarks>
        /// Must be called with the same footprint, columns and rows <see cref="RequestTileAsync"/>
        /// was (or will be) called with, or this looks up the wrong slot: a multi-slot group renders
        /// and caches under a composite key of its whole footprint, not under
        /// <paramref name="modelResRef"/> alone, because two groups can share a first tile and still
        /// look nothing alike. Passing only <paramref name="modelResRef"/> for a real group would
        /// return whatever that one model rendered to on its own - a single tile's image standing in
        /// for the group's - if that happened to already be cached under the plain key.
        /// </remarks>
        public Bitmap? CachedTile(
            string modelResRef,
            IReadOnlyList<string>? footprintModelResRefs = null,
            int columns = 1,
            int rows = 1) =>
            _memory.TryGet(TileKey(modelResRef, footprintModelResRefs, columns, rows), out var bitmap)
                ? bitmap
                : null;

        /// <summary>Whether a footprint is a genuine multi-slot group rather than a single tile.</summary>
        private static bool IsCompositeFootprint(
            IReadOnlyList<string>? footprintModelResRefs, int columns, int rows) =>
            footprintModelResRefs is { Count: > 1 } && columns * rows > 1;

        /// <summary>
        /// The memory-cache key for a tile's preview. A multi-slot group is keyed by its whole
        /// footprint plus shape, so two groups that happen to share a first tile still cache and look
        /// distinct; anything else is keyed by its own model resref.
        /// </summary>
        private static string TileKey(
            string modelResRef, IReadOnlyList<string>? footprintModelResRefs, int columns, int rows) =>
            IsCompositeFootprint(footprintModelResRefs, columns, rows)
                ? "tilegroup:" + columns + "x" + rows + ":" + string.Join(",", footprintModelResRefs!)
                : "tile:" + modelResRef;

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
        /// Renders and stores every missing preview the palette can show, reporting progress as it goes.
        /// Deliberately does not populate the in-memory cache: this walks tens of thousands of
        /// blueprints, and holding its output would defeat the point of bounding that cache.
        /// </summary>
        /// <remarks>
        /// Covers the module's own blueprints and the base game's and haks' - the palette's Standard
        /// group lists the latter, and warming only the module left every Standard tile rendering on
        /// demand or, worse, sitting on its type glyph.
        /// </remarks>
        public async Task<PreviewCacheProgress> WarmAsync(
            IProgress<PreviewCacheProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || !IsAvailable)
                return new PreviewCacheProgress(0, 0, 0, 0, 0);

            var work = new List<(ResourceType Type, string ResRef, bool UseIndexedBlueprint)>();
            foreach (var type in ModuleWorkspace.BlueprintTypes.Where(BlueprintPreviewRenderer.IsSupported))
            {
                foreach (var resRef in workspace.EnumerateResRefs(type))
                    work.Add((type, resRef, UseIndexedBlueprint: false));

                // Standard content is a separate cache identity. A same-resref module override must not
                // suppress it: the Palette offers both sources and they can render differently.
                if (workspace.ResourceIndex is not { } index)
                    continue;

                foreach (var identity in index.EnumerateResources(
                             ResourceIdentity.TypeFromExtension(type.Extension())))
                    work.Add((type, identity.ResRef, UseIndexedBlueprint: true));
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
                var key = Key(item.Type, item.ResRef, item.UseIndexedBlueprint);
                var version = VersionOf(key);
                var epoch = Volatile.Read(ref _cacheEpoch);
                try
                {
                    var blueprintPath = item.UseIndexedBlueprint
                        ? null
                        : workspace.GetResourcePath(item.Type, item.ResRef);
                    var dependencyPaths = DependencyPaths(
                        item.Type, item.ResRef, item.UseIndexedBlueprint);
                    if (disk.Contains(
                            item.Type, item.ResRef, blueprintPath, item.UseIndexedBlueprint,
                            dependencyPaths))
                    {
                        Interlocked.Increment(ref reused);
                    }
                    else
                    {
                        var image = _renderer.Render(
                            item.Type, item.ResRef, item.UseIndexedBlueprint);
                        if (image == null)
                        {
                            if (TryPersistWarmResult(
                                    key, version, epoch,
                                    () => disk.StoreNoArtwork(
                                        item.Type, item.ResRef, item.UseIndexedBlueprint)))
                                Interlocked.Increment(ref withoutArtwork);
                        }
                        else
                        {
                            // Disposed immediately: it exists only to be encoded, and 17,000 live
                            // bitmaps is exactly what this cache is designed to avoid.
                            using var bitmap = ToBitmap(image);
                            if (TryPersistWarmResult(
                                    key, version, epoch,
                                    () => disk.Store(
                                        item.Type, item.ResRef, item.UseIndexedBlueprint, bitmap)))
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

        private bool TryPersistWarmResult(
            string key,
            long version,
            long epoch,
            Action persist)
        {
            _cacheGate.EnterReadLock();
            try
            {
                lock (GateFor(key))
                {
                    if (epoch != Volatile.Read(ref _cacheEpoch) || version != VersionOf(key))
                        return false;

                    persist();
                    return true;
                }
            }
            finally
            {
                _cacheGate.ExitReadLock();
            }
        }

        /// <summary>Drops every cached preview, in memory and on disk, so the next build redoes them.</summary>
        public int ClearCache()
        {
            _cacheGate.EnterWriteLock();
            try
            {
                Interlocked.Increment(ref _cacheEpoch);
                _inFlight.Clear();
                _memory.Clear();
                return Disk.Clear();
            }
            finally
            {
                _cacheGate.ExitWriteLock();
            }
        }

        /// <summary>Deletes cache folders left by an older render pipeline. Returns the number removed.</summary>
        public int PruneSupersededCaches() => Disk.PruneSupersededVersions();

        /// <summary>Disk hit, then render. Null means "no artwork" - the caller substitutes a type symbol.</summary>
        private PreviewResolution Resolve(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint)
        {
            var workspace = _workspaceContext.Workspace;
            var blueprintPath = useIndexedBlueprint
                ? null
                : workspace?.GetResourcePath(type, resRef);
            var disk = Disk;
            var dependencyPaths = DependencyPaths(type, resRef, useIndexedBlueprint);

            switch (disk.TryLoad(
                        type, resRef, blueprintPath, useIndexedBlueprint, out var cached,
                        dependencyPaths))
            {
                case ThumbnailDiskCache.Lookup.Image:
                    return new PreviewResolution(
                        cached, disk, type, resRef, useIndexedBlueprint, Persist: false);
                case ThumbnailDiskCache.Lookup.NoArtwork:
                    return new PreviewResolution(
                        null, disk, type, resRef, useIndexedBlueprint, Persist: false);
            }

            var image = _renderer.Render(type, resRef, useIndexedBlueprint);
            if (image == null)
                return new PreviewResolution(
                    null, disk, type, resRef, useIndexedBlueprint, Persist: true);

            var bitmap = ToBitmap(image);
            return new PreviewResolution(
                bitmap, disk, type, resRef, useIndexedBlueprint, Persist: true);
        }

        private IReadOnlyList<string> DependencyPaths(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null || useIndexedBlueprint || type != ResourceType.Utc)
                return Array.Empty<string>();

            try
            {
                if (!workspace.TryLoadBlueprint(type, resRef, out var creatureBlueprint))
                    return Array.Empty<string>();

                var creature = creatureBlueprint.Fields;
                var itemResRef = BlueprintModelResolver.GetEquippedChestArmorResRef(creature);
                return string.IsNullOrWhiteSpace(itemResRef)
                    ? Array.Empty<string>()
                    : new[] { workspace.GetResourcePath(ResourceType.Uti, itemResRef) };
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
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
                        _disk = new ThumbnailDiskCache(moduleRoot, _renderer.ContentVersionUtc);
                    }

                    return _disk;
                }
            }
        }

        /// <summary>
        /// Copies straight-alpha BGRA into a bitmap row by row, because a locked framebuffer's stride is
        /// allowed to exceed its row width and a single block copy would shear the image when it does.
        /// Internal because the item editor converts its live icon previews through the same path.
        /// </summary>
        internal static Bitmap ToBitmap(IconImage image)
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

        private static string Key(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint) =>
            $"{(useIndexedBlueprint ? "standard" : "custom")}:{type}:{resRef}";
    }
}
