using System.Collections.Concurrent;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>One indexed area or blueprint: its type, resref, parsed Name/Tag (if resolvable), and file path.</summary>
    public sealed record CatalogEntry(ResourceType ResourceType, string ResRef, string? Name, string? Tag, string FilePath);

    /// <summary>How a <see cref="CatalogEntry"/> matched a <see cref="BlueprintCatalog.Search"/> query.</summary>
    public enum CatalogMatchKind
    {
        /// <summary>The resref equals the query exactly (case-insensitive).</summary>
        ExactResRef,

        /// <summary>The resref, name, or tag starts with the query (case-insensitive).</summary>
        Prefix,

        /// <summary>The resref, name, or tag contains the query anywhere (case-insensitive).</summary>
        Contains
    }

    /// <summary>One <see cref="Search"/> hit paired with how strongly it matched, for ranked display.</summary>
    public sealed record CatalogSearchResult(CatalogEntry Entry, CatalogMatchKind MatchKind);

    /// <summary>
    /// A background-built index over every area and blueprint in a <see cref="ModuleWorkspace"/>:
    /// one <see cref="CatalogEntry"/> per resource, with its display Name and Tag parsed out of the
    /// JSON. Building the full SWLOR module corpus (~17,900 files) takes roughly 15-20 seconds
    /// running parallel parses, so the constructor kicks the build off on a background task rather
    /// than blocking - callers either await <see cref="BuildTask"/> or read <see cref="Entries"/>
    /// (a safe, if partial/empty, snapshot) at any time.
    /// </summary>
    public sealed class BlueprintCatalog
    {
        private readonly ModuleWorkspace _workspace;
        private readonly object _snapshotLock = new();
        private readonly Dictionary<string, CatalogEntry> _refreshedEntries = new(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyList<CatalogEntry> _entries = Array.Empty<CatalogEntry>();
        private int _processedCount;

        /// <summary>Completes once every area/blueprint has been parsed (or attempted) and <see cref="Entries"/> is final.</summary>
        public Task BuildTask { get; }

        /// <summary>Total number of resources to index, known as soon as directory enumeration finishes (before BuildTask completes).</summary>
        public int TotalCount { get; private set; }

        /// <summary>Number of resources parsed (successfully or not) so far. Safe to read from any thread.</summary>
        public int ProcessedCount => Volatile.Read(ref _processedCount);

        /// <summary>
        /// A thread-safe snapshot of the entries indexed so far. Empty until directory enumeration
        /// completes; grows to the full set once <see cref="BuildTask"/> completes.
        /// </summary>
        public IReadOnlyList<CatalogEntry> Entries
        {
            get { lock (_snapshotLock) return _entries; }
        }

        /// <param name="workspace">The workspace to index.</param>
        /// <param name="onProgress">
        /// Optional callback invoked from a background thread as entries are processed
        /// (processedCount, totalCount). Not guaranteed to be invoked on any particular thread -
        /// callers updating UI state must marshal back themselves.
        /// </param>
        public BlueprintCatalog(ModuleWorkspace workspace, Action<int, int>? onProgress = null)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            BuildTask = Task.Run(() => Build(onProgress));
        }

        private void Build(Action<int, int>? onProgress)
        {
            var work = new List<(ResourceType Type, string ResRef)>();

            foreach (var resRef in _workspace.EnumerateAreaResRefs())
                work.Add((ResourceType.Area, resRef));

            foreach (var type in ModuleWorkspace.BlueprintTypes)
                foreach (var resRef in _workspace.EnumerateResRefs(type))
                    work.Add((type, resRef));

            TotalCount = work.Count;
            onProgress?.Invoke(0, TotalCount);

            var results = new ConcurrentBag<CatalogEntry>();

            Parallel.ForEach(work, item =>
            {
                results.Add(BuildEntry(item.Type, item.ResRef));
                var processed = Interlocked.Increment(ref _processedCount);
                onProgress?.Invoke(processed, TotalCount);
            });

            var ordered = results
                .OrderBy(entry => entry.ResourceType)
                .ThenBy(entry => entry.ResRef, StringComparer.OrdinalIgnoreCase)
                .ToList();

            lock (_snapshotLock)
            {
                foreach (var refreshed in _refreshedEntries.Values)
                    ReplaceEntry(ordered, refreshed);

                _entries = ordered;
            }
        }

        /// <summary>
        /// Re-reads one newly created or externally updated resource and publishes it into the
        /// current catalog snapshot immediately. A concurrent initial build also merges the
        /// refreshed entry before publishing its final snapshot, so the update cannot be lost.
        /// </summary>
        public CatalogEntry RefreshEntry(ResourceType type, string resRef)
        {
            var entry = BuildEntry(type, resRef);
            lock (_snapshotLock)
            {
                _refreshedEntries[IdentityKey(type, resRef)] = entry;
                var updated = _entries.ToList();
                ReplaceEntry(updated, entry);
                _entries = updated;
            }

            return entry;
        }

        private static void ReplaceEntry(List<CatalogEntry> entries, CatalogEntry replacement)
        {
            entries.RemoveAll(entry =>
                entry.ResourceType == replacement.ResourceType &&
                entry.ResRef.Equals(replacement.ResRef, StringComparison.OrdinalIgnoreCase));
            entries.Add(replacement);
            entries.Sort((left, right) =>
            {
                var typeComparison = left.ResourceType.CompareTo(right.ResourceType);
                return typeComparison != 0
                    ? typeComparison
                    : StringComparer.OrdinalIgnoreCase.Compare(left.ResRef, right.ResRef);
            });
        }

        private static string IdentityKey(ResourceType type, string resRef) => $"{(int)type}:{resRef}";

        private CatalogEntry BuildEntry(ResourceType type, string resRef)
        {
            var path = _workspace.GetResourcePath(type, resRef);

            try
            {
                var bytes = File.ReadAllBytes(path);
                var (name, tag) = ExtractNameAndTag(type, bytes);
                return new CatalogEntry(type, resRef, name, tag, path);
            }
            catch (Exception)
            {
                // A file that fails to parse still gets an entry (resref/path are known from the
                // directory listing) - just without a Name/Tag. The corpus round-trip gate is the
                // place that should catch a genuinely malformed file; this index tolerates it.
                return new CatalogEntry(type, resRef, null, null, path);
            }
        }

        private static (string? Name, string? Tag) ExtractNameAndTag(ResourceType type, byte[] bytes)
        {
            switch (type)
            {
                case ResourceType.Area:
                {
                    var doc = AreDocument.Parse(bytes);
                    return (doc.Name.Text, doc.Tag);
                }
                case ResourceType.Utc:
                {
                    var doc = UtcDocument.Parse(bytes);
                    return (JoinName(doc.FirstName.Text, doc.LastName.Text), doc.Tag);
                }
                case ResourceType.Uti:
                {
                    var doc = UtiDocument.Parse(bytes);
                    return (doc.LocalizedName.Text, doc.Tag);
                }
                case ResourceType.Utp:
                {
                    var doc = UtpDocument.Parse(bytes);
                    return (doc.LocName.Text, doc.Tag);
                }
                case ResourceType.Utd:
                {
                    var doc = UtdDocument.Parse(bytes);
                    return (doc.LocName.Text, doc.Tag);
                }
                case ResourceType.Utm:
                {
                    var doc = UtmDocument.Parse(bytes);
                    return (doc.LocName.Text, doc.Tag);
                }
                case ResourceType.Utt:
                {
                    var doc = UttDocument.Parse(bytes);
                    return (doc.LocalizedName.Text, doc.Tag);
                }
                case ResourceType.Uts:
                {
                    var doc = UtsDocument.Parse(bytes);
                    return (doc.LocName.Text, doc.Tag);
                }
                case ResourceType.Utw:
                {
                    var doc = UtwDocument.Parse(bytes);
                    return (doc.LocalizedName.Text, doc.Tag);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.");
            }
        }

        private static string? JoinName(string? first, string? last)
        {
            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
                return null;

            return string.Join(" ", new[] { first, last }.Where(part => !string.IsNullOrEmpty(part)));
        }

        /// <summary>
        /// Searches the current snapshot of <see cref="Entries"/> for resref/name/tag matches
        /// (case-insensitive), ranked exact resref match first, then any prefix match, then any
        /// contains match. Safe to call while <see cref="BuildTask"/> is still running (searches
        /// whatever has been indexed so far).
        /// </summary>
        public IReadOnlyList<CatalogSearchResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Array.Empty<CatalogSearchResult>();

            var trimmed = query.Trim();
            var results = new List<CatalogSearchResult>();

            foreach (var entry in Entries)
            {
                var kind = Match(entry, trimmed);
                if (kind != null)
                    results.Add(new CatalogSearchResult(entry, kind.Value));
            }

            return results
                .OrderBy(result => result.MatchKind)
                .ThenBy(result => result.Entry.ResRef, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static CatalogMatchKind? Match(CatalogEntry entry, string query)
        {
            if (entry.ResRef.Equals(query, StringComparison.OrdinalIgnoreCase))
                return CatalogMatchKind.ExactResRef;

            if (StartsWith(entry.ResRef, query) || StartsWith(entry.Name, query) || StartsWith(entry.Tag, query))
                return CatalogMatchKind.Prefix;

            if (Contains(entry.ResRef, query) || Contains(entry.Name, query) || Contains(entry.Tag, query))
                return CatalogMatchKind.Contains;

            return null;
        }

        private static bool StartsWith(string? value, string query) =>
            !string.IsNullOrEmpty(value) && value.StartsWith(query, StringComparison.OrdinalIgnoreCase);

        private static bool Contains(string? value, string query) =>
            !string.IsNullOrEmpty(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
