using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// Indexes numbered appearance models. Armor rows use the male/female body-part union; simple
    /// and composite item galleries use an exact model prefix so custom parts above 255 are not
    /// hidden merely because they have no unique inventory icon.
    /// </summary>
    /// <remarks>
    /// The part models are named <c>{prefix}_{part}{number:D3}</c> and the numbering is sparse -
    /// a torso has 64 variants and a hand 12, neither of them contiguous. Typing a number that has
    /// no model silently drops that piece off the body, which is indistinguishable from the armor
    /// simply not covering it.
    /// <para>
    /// Male and female variants are unioned: the two bodies carry the same armor at the same number,
    /// but one gender occasionally ships a variant the other lacks, and hiding it would make the
    /// list depend on which mannequin happened to be selected.
    /// </para>
    /// </remarks>
    public sealed class ArmorPartCatalog
    {
        private static readonly string[] Prefixes = { "pmh0", "pfh0" };
        private static readonly ushort ModelResourceType = ResourceIdentity.TypeFromExtension("mdl");
        private static readonly ushort LodResourceType = ResourceIdentity.TypeFromExtension("lod");

        private readonly ResourceIndex? _resources;
        private readonly object _stateGate = new();
        // Each resource generation pairs its own index with its own per-prefix answers, so a
        // query racing a reload can never publish stale numbers into the new generation.
        private Lazy<IReadOnlyDictionary<string, IReadOnlyList<(int Number, string ResRef, bool Redirected)>>>? _modelsByPrefix;
        private ConcurrentDictionary<string, IReadOnlyList<int>> _numbersByModelPrefix =
            new(StringComparer.OrdinalIgnoreCase);
        private Lazy<Task>? _buildTask;

        public bool IsBuilt
        {
            get
            {
                lock (_stateGate)
                    return _modelsByPrefix?.IsValueCreated != false;
            }
        }

        public ArmorPartCatalog(ResourceIndex? resources)
        {
            _resources = resources;
            if (_resources != null)
            {
                Reset();
                _resources.ResourcesReloaded += Reset;
            }
        }

        private void Reset()
        {
            var models = new Lazy<IReadOnlyDictionary<string, IReadOnlyList<(int Number, string ResRef, bool Redirected)>>>(
                BuildIndex,
                LazyThreadSafetyMode.ExecutionAndPublication);
            var build = new Lazy<Task>(
                () => Task.Run(() => _ = models.Value),
                LazyThreadSafetyMode.ExecutionAndPublication);
            lock (_stateGate)
            {
                _modelsByPrefix = models;
                _buildTask = build;
                _numbersByModelPrefix = new ConcurrentDictionary<string, IReadOnlyList<int>>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Builds the shared model-prefix index away from the UI thread. The index remains the same
        /// lazy singleton used by item editors; this merely gives expensive first-use callers a way
        /// to warm it without blocking tab navigation.
        /// </summary>
        public Task EnsureBuiltAsync()
        {
            Lazy<Task>? build;
            lock (_stateGate)
                build = _buildTask;
            return build?.Value ?? Task.CompletedTask;
        }

        /// <summary>
        /// The numbers that resolve to a real model for a part category ("chest", "shol", ...),
        /// ascending. Empty when no game data is loaded, which leaves the caller on a plain number
        /// box rather than an empty list.
        /// </summary>
        public IReadOnlyList<int> Numbers(string partType)
        {
            if (_resources == null || string.IsNullOrWhiteSpace(partType))
                return Array.Empty<int>();

            var part = partType.Trim();
            return Merge(
                NumbersForModelPrefix(Prefixes[0] + "_" + part),
                NumbersForModelPrefix(Prefixes[1] + "_" + part));
        }

        /// <summary>
        /// Every numeric suffix of an exact model prefix. For example, <c>helm_</c> returns the
        /// available helmet parts, including NWN:EE/custom-content values above the legacy byte
        /// range.
        /// </summary>
        public IReadOnlyList<int> NumbersForModelPrefix(string modelPrefix)
        {
            if (_resources == null || string.IsNullOrWhiteSpace(modelPrefix))
                return Array.Empty<int>();

            Lazy<IReadOnlyDictionary<string, IReadOnlyList<(int Number, string ResRef, bool Redirected)>>> index;
            ConcurrentDictionary<string, IReadOnlyList<int>> numbers;
            lock (_stateGate)
            {
                index = _modelsByPrefix!;
                numbers = _numbersByModelPrefix;
            }

            // LOD redirects are read only for prefixes an editor asks for; most belong to
            // generated cloak and robe phenotypes that no part list shows.
            return numbers.GetOrAdd(modelPrefix.Trim(), prefix =>
                index.Value.TryGetValue(prefix, out var models)
                    ? models.Where(model => !model.Redirected ||
                                            ModelResourceResolver.TryResolve(_resources, model.ResRef, out _))
                        .Select(model => model.Number)
                        .Distinct()
                        .OrderBy(number => number)
                        .ToList()
                    : Array.Empty<int>());
        }

        /// <summary>
        /// Adds the engine's zero-valued "no model" choice to a real model list. Both item armor
        /// and optional creature body pieces use this representation when a part is removed.
        /// </summary>
        public static IReadOnlyList<int> WithNone(IReadOnlyList<int> numbers)
        {
            ArgumentNullException.ThrowIfNull(numbers);
            if (numbers.Count == 0 || numbers[0] == 0)
                return numbers;

            return new[] { 0 }.Concat(numbers).ToList();
        }

        private IReadOnlyDictionary<string, IReadOnlyList<(int Number, string ResRef, bool Redirected)>> BuildIndex()
        {
            // The client loads a same-name LOD before the MDL, so a redirect's target decides.
            var redirected = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var identity in _resources!.EnumerateResources(ModelResourceType))
                redirected.TryAdd(identity.ResRef, false);
            foreach (var identity in _resources.EnumerateResources(LodResourceType))
                redirected[identity.ResRef] = true;

            var found = new Dictionary<string, List<(int Number, string ResRef, bool Redirected)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (resRef, isRedirected) in redirected)
            {
                var body = resRef.AsSpan();
                var digitStart = body.Length;
                while (digitStart > 0 && char.IsAsciiDigit(body[digitStart - 1]))
                    digitStart--;

                var modelPrefix = body[..digitStart].ToString();
                var suffix = body[digitStart..];
                if (modelPrefix.Length == 0 ||
                    suffix.Length < 3 ||
                    !int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ||
                    number is < 0 or > ushort.MaxValue)
                {
                    continue;
                }

                if (!found.TryGetValue(modelPrefix, out var models))
                {
                    models = new List<(int Number, string ResRef, bool Redirected)>();
                    found[modelPrefix] = models;
                }

                models.Add((number, resRef, isRedirected));
            }

            return found.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<(int Number, string ResRef, bool Redirected)>)pair.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<int> Merge(
            IReadOnlyList<int> first,
            IReadOnlyList<int> second)
        {
            if (first.Count == 0)
                return second;
            if (second.Count == 0)
                return first;

            return first.Concat(second).Distinct().OrderBy(number => number).ToList();
        }
    }
}
