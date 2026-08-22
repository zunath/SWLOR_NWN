using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>
    /// Which kind of layer a resolved resource came from.
    /// </summary>
    public enum ResourceLayerKind
    {
        BaseGame,
        Hak
    }

    /// <summary>
    /// Where a resolved resource came from: which layer (base game vs. a named hak) and the file
    /// or archive path it was pulled from.
    /// </summary>
    public sealed record ResourceProvenance(ResourceLayerKind Kind, string LayerName, string SourcePath);

    /// <summary>
    /// A resolved resource. <see cref="GetBytes"/> defers the actual read/extraction until
    /// called, so a lookup that's only checking existence never pays for file I/O it doesn't need.
    /// </summary>
    public sealed class ResourceHandle
    {
        private readonly Func<byte[]> _load;

        public ResourceIdentity Identity { get; }
        public ResourceProvenance Provenance { get; }

        internal ResourceHandle(ResourceIdentity identity, ResourceProvenance provenance, Func<byte[]> load)
        {
            Identity = identity;
            Provenance = provenance;
            _load = load;
        }

        public byte[] GetBytes() => _load();
    }

    /// <summary>
    /// Layered resource resolver spanning the base game (KEY/BIF) and an ordered stack of loose
    /// HAK-source directories or packed HAK archives, mirroring NWN's own resolution precedence:
    ///
    /// <list type="bullet">
    /// <item>Hak layers always override the base game.</item>
    /// <item>Among haks, earlier entries win over later ones. At startup the saved module's
    /// <c>Mod_HakList</c> and the HAK alias from <c>nwn.ini</c> provide the authoritative packed
    /// archives when available; the repository's <c>hakbuilder.json</c> source folders are the
    /// fallback. Runtime module changes replace either through <see cref="ReloadHakLayersAsync"/>.</item>
    /// </list>
    ///
    /// Index construction is two-phase: the constructor records the layer list synchronously and
    /// kicks off <see cref="InitializationTask"/>, which does the (potentially slow, ~130 folders)
    /// directory scan in the background. <see cref="TryLookup"/> calls <see cref="EnsureInitialized"/>
    /// internally, so callers can either await <see cref="InitializationTask"/> ahead of time or
    /// just call <see cref="TryLookup"/> and accept blocking until the scan completes.
    /// </summary>
    public sealed class ResourceIndex
    {
        public sealed record HakConflict(ResourceIdentity Resource, IReadOnlyList<string> Layers);

        /// <summary>
        /// One hak layer to scan: a display name (matches hakbuilder.json's "Name") and the
        /// loose-file directory to enumerate.
        /// </summary>
        public readonly record struct HakLayer(string Name, string DirectoryPath);

        private KeyBifCatalog? _baseLayer;
        private readonly Func<KeyBifCatalog?>? _baseLayerFactory;
        private IReadOnlyList<HakLayer> _hakLayerSpecs;
        private IReadOnlyList<(string Name, IHakResourceCatalog Catalog)> _hakLayers =
            Array.Empty<(string, IHakResourceCatalog)>();

        private readonly SemaphoreSlim _reloadGate = new(1, 1);

        /// <summary>Raised after a complete replacement index has been built and atomically published.</summary>
        public event Action? ResourcesReloaded;

        public Task InitializationTask { get; }

        /// <summary>
        /// The hak layers this index was configured with, in hakbuilder.json order
        /// (index 0 = highest precedence among haks).
        /// </summary>
        public IReadOnlyList<HakLayer> HakLayers => Volatile.Read(ref _hakLayerSpecs);

        /// <summary>
        /// Conservative version for all indexed game data. It is the newest write time among the base
        /// archives and loose hak resources and is suitable for invalidating derived caches.
        /// </summary>
        public DateTime ContentVersionUtc
        {
            get
            {
                EnsureInitialized();
                var latest = _baseLayer?.ContentVersionUtc ?? DateTime.MinValue;
                foreach (var (_, catalog) in Volatile.Read(ref _hakLayers))
                    latest = latest >= catalog.ContentVersionUtc ? latest : catalog.ContentVersionUtc;
                return latest;
            }
        }

        public ResourceIndex(KeyBifCatalog? baseLayer, IReadOnlyList<HakLayer> hakLayersInOrder)
        {
            _baseLayer = baseLayer;
            _hakLayerSpecs = hakLayersInOrder ?? Array.Empty<HakLayer>();
            InitializationTask = Task.Run(Initialize);
        }

        private ResourceIndex(
            Func<KeyBifCatalog?>? baseLayerFactory,
            IReadOnlyList<HakLayer> hakLayersInOrder)
        {
            _baseLayerFactory = baseLayerFactory;
            _hakLayerSpecs = hakLayersInOrder ?? Array.Empty<HakLayer>();
            InitializationTask = Task.Run(Initialize);
        }

        private void Initialize()
        {
            if (_baseLayerFactory != null)
            {
                try
                {
                    _baseLayer = _baseLayerFactory();
                }
                catch (Exception)
                {
                    // Base-game archives are optional. A damaged or disappearing install must not
                    // prevent the HAK layers from becoming available.
                    _baseLayer = null;
                }
            }

            var specs = Volatile.Read(ref _hakLayerSpecs);
            var scanned = new List<(string Name, IHakResourceCatalog Catalog)>(specs.Count);

            foreach (var layer in specs)
            {
                var catalog = OpenCatalog(layer.DirectoryPath);
                if (catalog != null)
                    scanned.Add((layer.Name, catalog));
            }

            Volatile.Write(ref _hakLayers, scanned);
        }

        /// <summary>
        /// Builds a complete replacement HAK index away from readers, then publishes it in one
        /// pointer swap. Lookups in flight finish against the old immutable catalogs; subsequent
        /// lookups see the new module-defined precedence without a partially scanned interval.
        /// </summary>
        public async Task ReloadHakLayersAsync(
            IReadOnlyList<HakLayer> hakLayersInOrder,
            CancellationToken cancellationToken = default,
            bool rescanWhenUnchanged = true)
        {
            ArgumentNullException.ThrowIfNull(hakLayersInOrder);
            var specs = hakLayersInOrder.ToArray();
            // ReloadSavedModuleInBackground starts from WorkspaceOpened on the UI thread. Await the
            // initial index asynchronously so confirming the saved module stack never turns its cold
            // scan into a synchronous UI stall.
            await InitializationTask.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Startup can already be using module.ifo's packed HAK stack. ModuleCustomContentService
            // confirms that same saved list once the workspace opens; rescanning identical multi-GB
            // archives would add work without changing a single lookup.
            if (!rescanWhenUnchanged && SameHakLayers(Volatile.Read(ref _hakLayerSpecs), specs))
                return;

            await _reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Another reload may have published this list while this caller waited for the gate.
                if (!rescanWhenUnchanged && SameHakLayers(Volatile.Read(ref _hakLayerSpecs), specs))
                    return;

                var scanned = await Task.Run(
                    () =>
                    {
                        var result = new List<(string Name, IHakResourceCatalog Catalog)>(specs.Length);
                        foreach (var layer in specs)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var catalog = OpenCatalog(layer.DirectoryPath);
                            if (catalog != null)
                                result.Add((layer.Name, catalog));
                        }

                        return (IReadOnlyList<(string Name, IHakResourceCatalog Catalog)>)result;
                    },
                    cancellationToken).ConfigureAwait(false);

                Volatile.Write(ref _hakLayerSpecs, specs);
                Volatile.Write(ref _hakLayers, scanned);
            }
            finally
            {
                _reloadGate.Release();
            }

            ResourcesReloaded?.Invoke();
        }

        private static bool SameHakLayers(
            IReadOnlyList<HakLayer> current,
            IReadOnlyList<HakLayer> requested)
        {
            if (current.Count != requested.Count)
                return false;

            var pathComparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            for (var index = 0; index < current.Count; index++)
            {
                if (!current[index].Name.Equals(requested[index].Name, StringComparison.OrdinalIgnoreCase) ||
                    !current[index].DirectoryPath.Equals(requested[index].DirectoryPath, pathComparison))
                {
                    return false;
                }
            }

            return true;
        }

        private static IHakResourceCatalog? OpenCatalog(string path)
        {
            if (Directory.Exists(path))
                return HakDirectoryCatalog.Scan(path);
            if (File.Exists(path) &&
                string.Equals(Path.GetExtension(path), ".hak", StringComparison.OrdinalIgnoreCase))
            {
                return HakArchiveCatalog.Open(path);
            }

            return null;
        }

        /// <summary>
        /// Block synchronously until the background directory scan has completed. Safe to call
        /// repeatedly and from multiple threads (subsequent calls just await the same task).
        /// </summary>
        public void EnsureInitialized() => InitializationTask.GetAwaiter().GetResult();

        /// <summary>
        /// Look up a resource across every layer, applying hak-over-base-game and
        /// first-hak-wins precedence. Blocks on <see cref="EnsureInitialized"/> if the background
        /// scan hasn't finished yet.
        /// </summary>
        public bool TryLookup(ResourceIdentity identity, out ResourceHandle handle)
        {
            EnsureInitialized();

            var layers = Volatile.Read(ref _hakLayers);
            for (var i = 0; i < layers.Count; i++)
            {
                var (name, catalog) = layers[i];
                if (!catalog.Resources.Contains(identity))
                    continue;

                var source = catalog.Describe(identity);
                handle = new ResourceHandle(
                    identity,
                    new ResourceProvenance(ResourceLayerKind.Hak, name, source),
                    () =>
                    {
                        var message =
                            $"The indexed resource '{identity.ResRef}' could not be read from '{source}'.";
                        try
                        {
                            if (catalog.TryGetBytes(identity, out var bytes))
                                return bytes;
                        }
                        catch (Exception exception) when (
                            exception is IOException or UnauthorizedAccessException)
                        {
                            throw new IOException(message, exception);
                        }

                        throw new IOException(message);
                    });
                return true;
            }

            if (_baseLayer != null && _baseLayer.Contains(identity))
            {
                handle = new ResourceHandle(
                    identity,
                    new ResourceProvenance(ResourceLayerKind.BaseGame, "nwn_base", "nwn_base.key"),
                    () =>
                    {
                        var message =
                            $"The KEY index contains '{identity.ResRef}', but its BIF payload could not be read.";
                        try
                        {
                            if (_baseLayer.TryGetBytes(identity, out var bytes))
                                return bytes;
                        }
                        catch (Exception exception) when (
                            exception is IOException or UnauthorizedAccessException)
                        {
                            throw new IOException(message, exception);
                        }

                        throw new IOException(message);
                    });
                return true;
            }

            handle = null!;
            return false;
        }

        /// <summary>
        /// True when the NWN installation's KEY/BIF layer contains the resource, irrespective of
        /// any HAK override. Used by consumers such as the script compiler that receive the game
        /// root but do not mount the module's HAK list.
        /// </summary>
        public bool ContainsBaseGameResource(ResourceIdentity identity)
        {
            EnsureInitialized();
            return _baseLayer?.Contains(identity) == true;
        }

        /// <summary>
        /// Enumerates every visible resource identity of one type across the base game and all hak
        /// layers, deduplicated by resref/type. This exposes names without extracting resource
        /// bytes and retains <see cref="TryLookup"/> as the authority for precedence and loading.
        /// </summary>
        public IReadOnlyList<ResourceIdentity> EnumerateResources(ushort resourceType)
        {
            EnsureInitialized();

            var resources = new HashSet<ResourceIdentity>();
            if (_baseLayer != null)
            {
                foreach (var identity in _baseLayer.Resources)
                {
                    if (identity.ResourceType == resourceType)
                        resources.Add(identity);
                }
            }

            foreach (var (_, catalog) in Volatile.Read(ref _hakLayers))
            {
                foreach (var identity in catalog.Resources)
                {
                    if (identity.ResourceType == resourceType)
                        resources.Add(identity);
                }
            }

            return resources
                .OrderBy(identity => identity.ResRef, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        /// Finds resources supplied by more than one active HAK. Layer order is retained so the
        /// first name in each result is the archive whose resource currently wins.
        /// </summary>
        public IReadOnlyList<HakConflict> FindHakConflicts()
        {
            EnsureInitialized();
            var providers = new Dictionary<ResourceIdentity, List<string>>();
            foreach (var (name, catalog) in Volatile.Read(ref _hakLayers))
            {
                foreach (var resource in catalog.Resources)
                {
                    if (!providers.TryGetValue(resource, out var layers))
                    {
                        layers = new List<string>();
                        providers[resource] = layers;
                    }

                    layers.Add(name);
                }
            }

            return providers
                .Where(pair => pair.Value.Count > 1)
                .OrderBy(pair => pair.Key.ResRef, StringComparer.OrdinalIgnoreCase)
                .ThenBy(pair => pair.Key.ResourceType)
                .Select(pair => new HakConflict(pair.Key, pair.Value.ToArray()))
                .ToArray();
        }

        /// <summary>
        /// Build a <see cref="ResourceIndex"/> from the master hak config (<c>Build\hakbuilder.json</c>),
        /// preserving its <c>HakList</c> order as the hak precedence order.
        /// </summary>
        /// <param name="hakBuilderConfigPath">Path to hakbuilder.json.</param>
        /// <param name="swlorHaksRoot">
        /// Optional override for the SWLOR_Haks root directory. hakbuilder.json's own "Path"
        /// values are relative to the Build directory (e.g. "../SWLOR_Haks/sw_2da/"); when this
        /// is supplied, only the trailing folder name (e.g. "sw_2da") is reused against the
        /// override root instead of resolving relative to the config file's directory. This lets
        /// tests point at an alternate/fixture hak root while still driving the layer order from
        /// the real hakbuilder.json.
        /// </param>
        /// <param name="baseLayer">Optional base-game layer (null = hak-only, no base game).</param>
        public static ResourceIndex FromHakBuilderConfig(
            string hakBuilderConfigPath,
            string? swlorHaksRoot = null,
            KeyBifCatalog? baseLayer = null)
        {
            var layers = ReadHakLayers(hakBuilderConfigPath, swlorHaksRoot);
            return new ResourceIndex(baseLayer, layers);
        }

        /// <summary>
        /// Builds the inexpensive HAK-layer specification immediately while loading the optional
        /// base-game KEY index on <see cref="InitializationTask"/>. This keeps archive parsing off
        /// the application startup path without changing the synchronous lookup contract.
        /// </summary>
        public static ResourceIndex FromHakBuilderConfigDeferred(
            string hakBuilderConfigPath,
            string? swlorHaksRoot = null,
            Func<KeyBifCatalog?>? baseLayerFactory = null)
        {
            var layers = ReadHakLayers(hakBuilderConfigPath, swlorHaksRoot);
            return new ResourceIndex(baseLayerFactory, layers);
        }

        /// <summary>
        /// Starts a deferred index over an already-resolved HAK stack. The application uses this for
        /// module.ifo's authoritative packed archives, avoiding an initial scan of the much larger
        /// loose hakbuilder source tree that would immediately be replaced after the module opens.
        /// </summary>
        public static ResourceIndex CreateDeferred(
            IReadOnlyList<HakLayer> hakLayersInOrder,
            Func<KeyBifCatalog?>? baseLayerFactory = null) =>
            new(baseLayerFactory, hakLayersInOrder);

        private static IReadOnlyList<HakLayer> ReadHakLayers(
            string hakBuilderConfigPath,
            string? swlorHaksRoot)
        {
            var configFullPath = Path.GetFullPath(hakBuilderConfigPath);
            var configDirectory = Path.GetDirectoryName(configFullPath)
                ?? throw new InvalidOperationException(
                    $"Could not determine the containing directory of hakbuilder config '{hakBuilderConfigPath}'.");

            var json = File.ReadAllText(configFullPath);
            var config = JsonSerializer.Deserialize<HakBuilderConfigDto>(json, JsonOptions)
                ?? throw new InvalidDataException(
                    $"hakbuilder config '{hakBuilderConfigPath}' did not parse to a valid object.");

            var layers = new List<HakLayer>(config.HakList.Count);
            foreach (var hak in config.HakList)
            {
                if (string.IsNullOrWhiteSpace(hak.Name) || string.IsNullOrWhiteSpace(hak.Path))
                    continue;

                var directoryPath = string.IsNullOrWhiteSpace(swlorHaksRoot)
                    ? Path.GetFullPath(Path.Combine(configDirectory, hak.Path))
                    : Path.Combine(swlorHaksRoot, Path.GetFileName(hak.Path.TrimEnd('/', '\\')));

                layers.Add(new HakLayer(hak.Name, directoryPath));
            }

            return layers;
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private sealed class HakBuilderConfigDto
        {
            [JsonPropertyName("TlkPath")]
            public string TlkPath { get; set; } = string.Empty;

            [JsonPropertyName("OutputPath")]
            public string OutputPath { get; set; } = string.Empty;

            [JsonPropertyName("HakList")]
            public List<HakEntryDto> HakList { get; set; } = new();
        }

        private sealed class HakEntryDto
        {
            [JsonPropertyName("Name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("Path")]
            public string Path { get; set; } = string.Empty;
        }
    }
}
