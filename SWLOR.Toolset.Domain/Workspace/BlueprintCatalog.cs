using System.Collections.Concurrent;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// One indexed area or blueprint: its type, resref, parsed Name/Tag (if resolvable), file path,
    /// and the BaseItem already read while indexing an item blueprint.
    /// </summary>
    public sealed record CatalogEntry(
        ResourceType ResourceType,
        string ResRef,
        string? Name,
        string? Tag,
        string FilePath,
        int? BaseItem = null)
    {
        /// <summary>The friendly name for this entry's kind ("Creature", not "Utc"), for result lists.</summary>
        public string ResourceTypeDisplayName => ResourceType.SingularDisplayName();
    }

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
        private readonly Func<uint, string?>? _resolveStrRef;
        private readonly object _snapshotLock = new();
        private readonly object _tlkLabelLock = new();
        private readonly ConcurrentDictionary<string, CatalogEntry> _indexedEntries =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, CatalogNameSource> _nameSources =
            new(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyList<CatalogEntry> _entries = Array.Empty<CatalogEntry>();
        private bool _snapshotStale;
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
        /// <remarks>
        /// Sorted when it is read rather than when it changes. The production corpus is ~17,900
        /// records, and the build used to re-sort all of them every 128 parsed files - 140 full
        /// sorts nobody looked at - while every blueprint save did one more.
        /// </remarks>
        public IReadOnlyList<CatalogEntry> Entries
        {
            get
            {
                lock (_snapshotLock)
                {
                    if (_snapshotStale)
                    {
                        _entries = _indexedEntries.Values
                            .OrderBy(entry => entry.ResourceType)
                            .ThenBy(entry => entry.ResRef, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        _snapshotStale = false;
                    }

                    return _entries;
                }
            }
        }

        /// <summary>
        /// One entry by type and resref, without walking the snapshot.
        /// </summary>
        /// <remarks>
        /// The dictionary the build fills is already keyed this way, so callers that only want one
        /// name never had to scan ~17,900 records for it - and the area editor's selection bar did,
        /// on every click.
        /// </remarks>
        public bool TryGetEntry(ResourceType type, string resRef, out CatalogEntry entry)
        {
            if (string.IsNullOrWhiteSpace(resRef))
            {
                entry = null!;
                return false;
            }

            return _indexedEntries.TryGetValue(IdentityKey(type, resRef), out entry!);
        }

        /// <summary>
        /// Reads the same user-facing name used by catalog entries from resource JSON that does not
        /// belong to the open module, such as a resource staged from an ERF archive.
        /// </summary>
        public string? ReadDisplayName(ResourceType type, byte[] content)
        {
            ArgumentNullException.ThrowIfNull(content);
            if (type != ResourceType.Area && !ModuleWorkspace.BlueprintTypes.Contains(type))
                return null;

            return ExtractMetadata(type, content).Name;
        }

        /// <summary>Every indexed entry of one type, without filtering the whole snapshot.</summary>
        public IReadOnlyList<CatalogEntry> EntriesOfType(ResourceType type)
        {
            var matches = new List<CatalogEntry>();
            foreach (var entry in _indexedEntries.Values)
            {
                if (entry.ResourceType == type)
                    matches.Add(entry);
            }

            return matches;
        }

        /// <param name="workspace">The workspace to index.</param>
        /// <param name="onProgress">
        /// Optional callback invoked from a background thread as entries are processed
        /// (processedCount, totalCount). Not guaranteed to be invoked on any particular thread -
        /// callers updating UI state must marshal back themselves.
        /// </param>
        /// <param name="resolveStrRef">
        /// Optional TLK resolver used when a localized name has a string reference but no inline
        /// English text. Without one, those names remain unresolved while resref/tag indexing
        /// continues normally.
        /// </param>
        public BlueprintCatalog(
            ModuleWorkspace workspace,
            Action<int, int>? onProgress = null,
            Func<uint, string?>? resolveStrRef = null)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            _resolveStrRef = resolveStrRef;
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

            // Small modules publish each entry immediately; the production corpus publishes in
            // batches so searches become useful during the build without repeatedly sorting all
            // ~17,900 entries for every parsed file.
            var publishInterval = TotalCount <= 256 ? 1 : 128;

            Parallel.ForEach(work, item =>
            {
                var key = IdentityKey(item.Type, item.ResRef);

                // A resource deleted while this build is still running was already enumerated into
                // work, so removing it from the dictionary does nothing - the entry has not been added
                // yet. Without the tombstone, the add below resurrects it and the panels list a
                // blueprint whose file is gone until the next restart.
                if (_removed.ContainsKey(key))
                    return;

                var built = BuildEntry(item.Type, item.ResRef);
                if (built != null)
                {
                    lock (_tlkLabelLock)
                    {
                        if (_removed.ContainsKey(key))
                        {
                            _nameSources.TryRemove(key, out _);
                        }
                        else
                        {
                            var entry = built.Entry;
                            // A TLK publication can race this initial build between parsing and
                            // insertion. Resolve once more while sharing the refresh lock so either
                            // this insertion or RefreshTlkLabels observes the new generation.
                            if (built.NameSource != null)
                                entry = entry with { Name = ResolveName(built.NameSource) };
                            if (_indexedEntries.TryAdd(key, entry))
                            {
                                if (built.NameSource != null)
                                    _nameSources[key] = built.NameSource;
                                else
                                    _nameSources.TryRemove(key, out _);
                            }
                        }
                    }
                }
                var processed = Interlocked.Increment(ref _processedCount);
                if (processed % publishInterval == 0)
                    PublishSnapshot();
                onProgress?.Invoke(processed, TotalCount);
            });

            PublishSnapshot();
        }

        /// <summary>
        /// Re-reads one newly created or externally updated resource and publishes it into the
        /// current catalog snapshot immediately. A concurrent initial build also merges the
        /// refreshed entry before publishing its final snapshot, so the update cannot be lost.
        /// </summary>
        public CatalogEntry? RefreshEntry(ResourceType type, string resRef) =>
            RefreshEntry(type, resRef, out _);

        /// <summary>
        /// Re-reads one resource and reports whether its indexed metadata or membership changed.
        /// Content-only edits return the existing entry without invalidating the ordered snapshot.
        /// </summary>
        public CatalogEntry? RefreshEntry(ResourceType type, string resRef, out bool changed)
        {
            var key = IdentityKey(type, resRef);
            changed = false;

            // Recreating a resref that was deleted earlier has to lift its tombstone, or the entry
            // would be published here and then dropped again by a still-running build.
            _removed.TryRemove(key, out _);

            var built = BuildEntry(type, resRef);
            if (built == null)
            {
                changed = RemoveEntry(type, resRef);
                return null;
            }

            var entry = built.Entry;
            lock (_tlkLabelLock)
            {
                if (_removed.ContainsKey(key))
                {
                    _nameSources.TryRemove(key, out _);
                    return null;
                }

                if (built.NameSource != null)
                    entry = entry with { Name = ResolveName(built.NameSource) };

                var sourceUnchanged = built.NameSource == null
                    ? !_nameSources.ContainsKey(key)
                    : _nameSources.TryGetValue(key, out var existingSource) &&
                      existingSource == built.NameSource;
                if (_indexedEntries.TryGetValue(key, out var existing) &&
                    existing == entry && sourceUnchanged)
                    return existing;

                if (built.NameSource != null)
                    _nameSources[key] = built.NameSource;
                else
                    _nameSources.TryRemove(key, out _);
                _indexedEntries[key] = entry;
            }
            PublishSnapshot();
            changed = true;

            return entry;
        }

        /// <summary>
        /// Drops a resource that no longer exists, so panels stop listing a file that has been deleted.
        /// </summary>
        public bool RemoveEntry(ResourceType type, string resRef)
        {
            var key = IdentityKey(type, resRef);
            bool removed;
            lock (_tlkLabelLock)
            {
                _removed[key] = true;
                _nameSources.TryRemove(key, out _);
                removed = _indexedEntries.TryRemove(key, out _);
            }

            if (removed)
                PublishSnapshot();
            return removed;
        }

        /// <summary>
        /// Resources deleted since this catalog was created. Used as a tombstone set so a delete that
        /// races the initial build wins, rather than being undone by the in-flight enumeration.
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> _removed = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Marks the ordered snapshot out of date. Serialized so a slower, older publication can
        /// never replace a newer snapshot with fewer entries.
        /// </summary>
        private void PublishSnapshot()
        {
            lock (_snapshotLock)
                _snapshotStale = true;
        }

        private static string IdentityKey(ResourceType type, string resRef) => $"{(int)type}:{resRef}";

        private CatalogBuildResult? BuildEntry(ResourceType type, string resRef)
        {
            var path = _workspace.GetResourcePath(type, resRef);

            try
            {
                var bytes = File.ReadAllBytes(path);
                var metadata = ExtractMetadata(type, bytes);
                return new CatalogBuildResult(
                    new CatalogEntry(
                        type,
                        resRef,
                        metadata.Name,
                        metadata.Tag,
                        path,
                        metadata.BaseItem),
                    metadata.NameSource);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (Exception)
            {
                // A file that fails to parse still gets an entry (resref/path are known from the
                // directory listing) - just without a Name/Tag. The corpus round-trip gate is the
                // place that should catch a genuinely malformed file; this index tolerates it.
                return new CatalogBuildResult(
                    new CatalogEntry(type, resRef, null, null, path),
                    null);
            }
        }

        private EntryMetadata ExtractMetadata(
            ResourceType type,
            byte[] bytes)
        {
            switch (type)
            {
                case ResourceType.Area:
                {
                    var doc = AreDocument.Parse(bytes);
                    var name = CaptureName(doc.Name);
                    return new EntryMetadata(ResolveName(name), doc.Tag, null, new CatalogNameSource(name));
                }
                case ResourceType.Utc:
                {
                    var doc = UtcDocument.Parse(bytes);
                    var firstName = CaptureName(doc.FirstName);
                    var lastName = CaptureName(doc.LastName);
                    var nameSource = new CatalogNameSource(firstName, lastName);
                    return new EntryMetadata(ResolveName(nameSource), doc.Tag, null, nameSource);
                }
                case ResourceType.Uti:
                {
                    var doc = UtiDocument.Parse(bytes);
                    var name = CaptureName(doc.LocalizedName);
                    return new EntryMetadata(
                        ResolveName(name), doc.Tag, doc.BaseItem, new CatalogNameSource(name));
                }
                case ResourceType.Utp:
                {
                    var doc = UtpDocument.Parse(bytes);
                    var name = CaptureName(doc.LocName);
                    return new EntryMetadata(ResolveName(name), doc.Tag, null, new CatalogNameSource(name));
                }
                case ResourceType.Utd:
                {
                    var doc = UtdDocument.Parse(bytes);
                    var name = CaptureName(doc.LocName);
                    return new EntryMetadata(ResolveName(name), doc.Tag, null, new CatalogNameSource(name));
                }
                case ResourceType.Utm:
                {
                    var doc = UtmDocument.Parse(bytes);
                    var name = CaptureName(doc.LocName);
                    return new EntryMetadata(ResolveName(name), doc.Tag, null, new CatalogNameSource(name));
                }
                case ResourceType.Utt:
                {
                    var doc = UttDocument.Parse(bytes);
                    var name = CaptureName(doc.LocalizedName);
                    return new EntryMetadata(ResolveName(name), doc.Tag, null, new CatalogNameSource(name));
                }
                case ResourceType.Uts:
                {
                    var doc = UtsDocument.Parse(bytes);
                    var name = CaptureName(doc.LocName);
                    return new EntryMetadata(ResolveName(name), doc.Tag, null, new CatalogNameSource(name));
                }
                case ResourceType.Utw:
                {
                    var doc = UtwDocument.Parse(bytes);
                    var name = CaptureName(doc.LocalizedName);
                    return new EntryMetadata(ResolveName(name), doc.Tag, null, new CatalogNameSource(name));
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.");
            }
        }

        /// <summary>
        /// Re-resolves every materialized catalog name from the LocString metadata captured during
        /// the file's last parse. No resource files are reopened when a TLK generation changes.
        /// </summary>
        public bool RefreshTlkLabels()
        {
            var changed = false;
            lock (_tlkLabelLock)
            {
                foreach (var (key, source) in _nameSources)
                {
                    if (!_indexedEntries.TryGetValue(key, out var entry))
                        continue;

                    var name = ResolveName(source);
                    if (string.Equals(name, entry.Name, StringComparison.Ordinal))
                        continue;

                    _indexedEntries[key] = entry with { Name = name };
                    changed = true;
                }
            }

            if (changed)
                PublishSnapshot();
            return changed;
        }

        private static LocStringNameSource CaptureName(LocString value) =>
            new(value.Text, value.StrRef);

        private string? ResolveName(CatalogNameSource source) =>
            source.Last == null
                ? ResolveName(source.First)
                : JoinName(ResolveName(source.First), ResolveName(source.Last.Value));

        private string? ResolveName(LocStringNameSource source)
        {
            if (!string.IsNullOrEmpty(source.InlineText))
                return source.InlineText;

            return source.StrRef is { } strRef && strRef != uint.MaxValue
                ? _resolveStrRef?.Invoke(strRef)
                : null;
        }

        private static string? JoinName(string? first, string? last)
        {
            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
                return null;

            return string.Join(" ", new[] { first, last }.Where(part => !string.IsNullOrEmpty(part)));
        }

        private sealed record EntryMetadata(
            string? Name,
            string? Tag,
            int? BaseItem,
            CatalogNameSource NameSource);

        private sealed record CatalogBuildResult(
            CatalogEntry Entry,
            CatalogNameSource? NameSource);

        private readonly record struct LocStringNameSource(string? InlineText, uint? StrRef);

        private sealed record CatalogNameSource(
            LocStringNameSource First,
            LocStringNameSource? Last = null);

        /// <summary>
        /// Searches the current snapshot of <see cref="Entries"/> for resref/name/tag matches
        /// (case-insensitive), ranked exact resref match first, then any prefix match, then any
        /// contains match. Safe to call while <see cref="BuildTask"/> is still running (searches
        /// whatever has been indexed so far).
        /// </summary>
        /// <param name="limit">
        /// Most results to return. A one-letter query matches most of the corpus, and nobody reads
        /// past the first screen; without a bound the panel sorted fifteen thousand records to show
        /// two hundred, on every keystroke.
        /// </param>
        /// <remarks>
        /// Collected into one bucket per rank rather than sorted by it. There are three ranks, so
        /// the comparison sort was doing log-n work to answer a question with three answers; only
        /// the tie-break inside a bucket needs ordering, and the snapshot is already in resref
        /// order, so each bucket comes out sorted for free.
        /// </remarks>
        public IReadOnlyList<CatalogSearchResult> Search(string query, int limit = int.MaxValue)
        {
            if (string.IsNullOrWhiteSpace(query) || limit <= 0)
                return Array.Empty<CatalogSearchResult>();

            var trimmed = query.Trim();
            var exact = new List<CatalogSearchResult>();
            var prefix = new List<CatalogSearchResult>();
            var contains = new List<CatalogSearchResult>();

            foreach (var entry in Entries)
            {
                switch (Match(entry, trimmed))
                {
                    case CatalogMatchKind.ExactResRef:
                        exact.Add(new CatalogSearchResult(entry, CatalogMatchKind.ExactResRef));
                        break;
                    case CatalogMatchKind.Prefix:
                        prefix.Add(new CatalogSearchResult(entry, CatalogMatchKind.Prefix));
                        break;
                    case CatalogMatchKind.Contains:
                        // Only collected while a better-ranked bucket could still fall short.
                        if (exact.Count + prefix.Count + contains.Count < limit)
                            contains.Add(new CatalogSearchResult(entry, CatalogMatchKind.Contains));
                        break;
                }
            }

            var ranked = new List<CatalogSearchResult>(
                Math.Min(limit, exact.Count + prefix.Count + contains.Count));
            foreach (var bucket in new[] { exact, prefix, contains })
            {
                foreach (var result in bucket)
                {
                    if (ranked.Count == limit)
                        return ranked;

                    ranked.Add(result);
                }
            }

            return ranked;
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
