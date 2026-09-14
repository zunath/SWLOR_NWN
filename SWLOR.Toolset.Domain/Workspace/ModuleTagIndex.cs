using System.Text.Json;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// Which area defines each waypoint and door tag in the module — what lets a transition's
    /// destination say "in moseis_cantina" rather than leaving the builder to guess.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built from every area's .git. The small transition-destination subset is independently
    /// prewarmed in the background when a workspace opens; broader tag-to-area maps remain lazy.
    /// </para>
    /// <para>
    /// An instance's own Tag wins, and a blank one falls back to the blueprint's — which is how the
    /// engine resolves it, and the difference between 17 apparently-dangling transitions and the 16
    /// that really are.
    /// </para>
    /// </remarks>
    public sealed class ModuleTagIndex
    {
        private readonly ModuleWorkspace _workspace;
        private readonly object _syncRoot = new();
        private readonly object _transitionSyncRoot = new();
        private Dictionary<string, string>? _areasByTag;
        private HashSet<string>? _transitionDestinationTags;
        private Dictionary<string, string>? _waypointAreasByTag;
        private Dictionary<string, Dictionary<string, int>>? _waypointCountsByArea;
        private Dictionary<string, string>? _doorAreasByTag;
        private Dictionary<string, string>? _storeAreasByTag;
        private Dictionary<string, string>? _itemResRefsByTag;
        private readonly Dictionary<string, string?> _blueprintTags =
            new(StringComparer.OrdinalIgnoreCase);
        private Task<IReadOnlyCollection<string>>? _transitionWarmTask;
        private int _transitionGeneration;

        public ModuleTagIndex(ModuleWorkspace workspace)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        /// <summary>The area defining this waypoint or door tag, or null when nothing does.</summary>
        public string? FindAreaDefiningTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return null;

            return Index().GetValueOrDefault(tag);
        }

        /// <summary>The area defining a tag on one specific supported instance kind.</summary>
        public string? FindAreaDefiningTag(string tag, ResourceType type)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return null;

            _ = Index();
            return type switch
            {
                ResourceType.Utw => _waypointAreasByTag!.GetValueOrDefault(tag),
                ResourceType.Utd => _doorAreasByTag!.GetValueOrDefault(tag),
                _ => null
            };
        }

        /// <summary>The module item blueprint carrying this tag, or null when none does.</summary>
        public string? FindItemBlueprintDefiningTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return null;

            lock (_syncRoot)
            {
                _itemResRefsByTag ??= IndexItemBlueprints();
                return _itemResRefsByTag.GetValueOrDefault(tag);
            }
        }

        /// <summary>
        /// Every tag the module defines, for the pickers that offer one rather than asking
        /// about a tag already stored - the placeable teleporter's destination, for instance.
        /// </summary>
        public IReadOnlyCollection<string> Tags => Index().Keys.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>Placed-instance tags for one supported resource kind.</summary>
        public IReadOnlyCollection<string> TagsFor(ResourceType type)
        {
            _ = Index();
            var typed = type switch
            {
                ResourceType.Utw => _waypointAreasByTag,
                ResourceType.Utd => _doorAreasByTag,
                ResourceType.Utm => _storeAreasByTag,
                _ => null
            };

            if (typed == null)
                return Array.Empty<string>();

            return typed.Keys
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Counts placed waypoints carrying <paramref name="tag"/> outside one area. The live area
        /// editor supplies its own in-memory placements and uses this to avoid counting the current
        /// instance against itself while still seeing every other area on disk.
        /// </summary>
        public int CountWaypointPlacementsOutsideArea(string tag, string areaResRef)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return 0;

            lock (_syncRoot)
            {
                _ = Index();
                return _waypointCountsByArea!.TryGetValue(tag, out var byArea)
                    ? byArea.Where(pair => !string.Equals(
                            pair.Key, areaResRef, StringComparison.OrdinalIgnoreCase))
                        .Sum(pair => pair.Value)
                    : 0;
            }
        }

        /// <summary>Every effective placed-waypoint tag and its module-wide occurrence count.</summary>
        public IReadOnlyDictionary<string, int> WaypointTagCounts
        {
            get
            {
                lock (_syncRoot)
                {
                    _ = Index();
                    return _waypointCountsByArea!.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value.Values.Sum(),
                        StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        /// <summary>Resolves a placed waypoint's own tag, falling back to its blueprint tag.</summary>
        public string? ResolveWaypointTag(Gff.JsonGffStruct waypoint)
        {
            ArgumentNullException.ThrowIfNull(waypoint);
            lock (_syncRoot)
            {
                var tag = waypoint.GetStringOrNull("Tag");
                return string.IsNullOrWhiteSpace(tag)
                    ? BlueprintTag(ResourceType.Utw, waypoint.GetStringOrNull("TemplateResRef"))
                    : tag;
            }
        }

        /// <summary>
        /// Every non-empty destination tag named by an area transition trigger or door.
        /// </summary>
        public IReadOnlyCollection<string> TransitionDestinationTags
        {
            get
            {
                lock (_transitionSyncRoot)
                {
                    _transitionDestinationTags ??= IndexTransitionDestinations();
                    return SortedTransitionDestinations();
                }
            }
        }

        /// <summary>
        /// Returns transition destinations without ever making the caller perform a cold scan. This
        /// is the UI-facing path; it also remains safe if a file change invalidates a prior warm-up.
        /// </summary>
        public Task<IReadOnlyCollection<string>> GetTransitionDestinationTagsAsync()
        {
            lock (_transitionSyncRoot)
            {
                if (_transitionDestinationTags != null)
                    return Task.FromResult(SortedTransitionDestinations());
                if (_transitionWarmTask != null)
                    return _transitionWarmTask;

                var generation = _transitionGeneration;
                _transitionWarmTask = Task.Run(() =>
                {
                    try
                    {
                        return TransitionDestinationTags;
                    }
                    finally
                    {
                        lock (_transitionSyncRoot)
                        {
                            if (_transitionGeneration == generation)
                                _transitionWarmTask = null;
                        }
                    }
                });
                return _transitionWarmTask;
            }
        }

        /// <summary>Drops the cache so the next question re-reads the module.</summary>
        public void Invalidate()
        {
            lock (_syncRoot)
            {
                _areasByTag = null;
                _waypointAreasByTag = null;
                _waypointCountsByArea = null;
                _doorAreasByTag = null;
                _storeAreasByTag = null;
                _itemResRefsByTag = null;
                _blueprintTags.Clear();
            }

            lock (_transitionSyncRoot)
            {
                _transitionGeneration++;
                _transitionDestinationTags = null;
                _transitionWarmTask = null;
            }
        }

        private Dictionary<string, string> Index()
        {
            lock (_syncRoot)
            {
                if (_areasByTag != null)
                    return _areasByTag;

                var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var waypoints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var waypointCounts = new Dictionary<string, Dictionary<string, int>>(
                    StringComparer.OrdinalIgnoreCase);
                var doors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var stores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var areaResRef in _workspace.EnumerateAreaResRefs())
                {
                    try
                    {
                        IndexGit(
                            index,
                            waypoints,
                            doors,
                            stores,
                            waypointCounts,
                            areaResRef);
                    }
                    catch (Exception)
                    {
                        // One unreadable area must not cost every other area its tags.
                        continue;
                    }
                }

                _waypointAreasByTag = waypoints;
                _waypointCountsByArea = waypointCounts;
                _doorAreasByTag = doors;
                _storeAreasByTag = stores;
                _areasByTag = index;
                return index;
            }
        }

        private void IndexGit(
            Dictionary<string, string> index,
            Dictionary<string, string> waypoints,
            Dictionary<string, string> doors,
            Dictionary<string, string> stores,
            Dictionary<string, Dictionary<string, int>> waypointCounts,
            string areaResRef)
        {
            var path = Path.Combine(_workspace.ModuleRoot, "git", areaResRef + ".git.json");
            using var document = JsonDocument.Parse(NwnJsonEncoding.ReadFileAsUtf8(path));
            var root = document.RootElement;

            // A GIT can contain tens of thousands of fields, but this index needs only two lists
            // and two scalar fields within their entries. JsonDocument avoids materializing the
            // complete editable GFF object graph that GitDocument.Load intentionally creates.
            AddTags(
                index,
                waypoints,
                areaResRef,
                Instances(root, "WaypointList"),
                ResourceType.Utw,
                countsByArea: waypointCounts);
            AddTags(index, doors, areaResRef, Instances(root, "Door List"), ResourceType.Utd);
            AddTags(
                index,
                stores,
                areaResRef,
                Instances(root, "StoreList"),
                ResourceType.Utm,
                "ResRef");
        }

        private HashSet<string> IndexTransitionDestinations()
        {
            var areaResRefs = _workspace.EnumerateAreaResRefs();
            var perArea = new HashSet<string>?[areaResRefs.Count];
            var options = new ParallelOptions
            {
                // Parsing GIT JSON is CPU-heavy. Four workers reduce cold-start latency without
                // allowing the largest area files to create an unbounded memory spike.
                MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 4))
            };

            Parallel.For(0, areaResRefs.Count, options, index =>
            {
                try
                {
                    perArea[index] = ReadTransitionDestinations(areaResRefs[index]);
                }
                catch (Exception)
                {
                    // One unreadable area must not cost every other area its destinations.
                }
            });

            var destinations = new HashSet<string>(StringComparer.Ordinal);
            foreach (var areaDestinations in perArea)
            {
                if (areaDestinations != null)
                    destinations.UnionWith(areaDestinations);
            }

            return destinations;
        }

        private HashSet<string> ReadTransitionDestinations(string areaResRef)
        {
            var path = Path.Combine(_workspace.ModuleRoot, "git", areaResRef + ".git.json");
            var reader = new Utf8JsonReader(NwnJsonEncoding.ReadFileAsUtf8(path));
            var destinations = new HashSet<string>(StringComparer.Ordinal);

            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1)
                    continue;

                var isTransitionList =
                    reader.ValueTextEquals("TriggerList") ||
                    reader.ValueTextEquals("Door List");
                if (!reader.Read())
                    break;

                if (isTransitionList)
                    ReadTransitionList(ref reader, destinations);
                else
                    reader.Skip();
            }

            return destinations;
        }

        private static void ReadTransitionList(
            ref Utf8JsonReader reader,
            HashSet<string> destinations)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                reader.Skip();
                return;
            }

            var fieldDepth = reader.CurrentDepth;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == fieldDepth)
                    return;
                if (reader.TokenType != JsonTokenType.PropertyName ||
                    !reader.ValueTextEquals("value"))
                {
                    continue;
                }

                if (!reader.Read())
                    return;
                if (reader.TokenType == JsonTokenType.StartArray)
                    ReadLinkedToValues(ref reader, destinations);
                else
                    reader.Skip();
            }
        }

        private static void ReadLinkedToValues(
            ref Utf8JsonReader reader,
            HashSet<string> destinations)
        {
            var listDepth = reader.CurrentDepth;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == listDepth)
                    return;
                if (reader.TokenType != JsonTokenType.PropertyName ||
                    !reader.ValueTextEquals("LinkedTo"))
                {
                    continue;
                }

                if (!reader.Read())
                    return;
                ReadLinkedToField(ref reader, destinations);
            }
        }

        private static void ReadLinkedToField(
            ref Utf8JsonReader reader,
            HashSet<string> destinations)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                reader.Skip();
                return;
            }

            var fieldDepth = reader.CurrentDepth;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == fieldDepth)
                    return;
                if (reader.TokenType != JsonTokenType.PropertyName ||
                    !reader.ValueTextEquals("value"))
                {
                    continue;
                }

                if (!reader.Read())
                    return;
                if (reader.TokenType == JsonTokenType.String)
                {
                    var linkedTo = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(linkedTo))
                        destinations.Add(linkedTo);
                }
                else
                {
                    reader.Skip();
                }
            }
        }

        private IReadOnlyCollection<string> SortedTransitionDestinations() =>
            _transitionDestinationTags!
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static IEnumerable<JsonElement> Instances(JsonElement root, string label)
        {
            if (!root.TryGetProperty(label, out var field) ||
                !field.TryGetProperty("value", out var value) ||
                value.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            foreach (var instance in value.EnumerateArray())
                yield return instance;
        }

        private static void AddTransitionDestinations(
            HashSet<string> destinations,
            IEnumerable<JsonElement> instances)
        {
            foreach (var instance in instances)
            {
                var linkedTo = FieldString(instance, "LinkedTo");
                if (!string.IsNullOrWhiteSpace(linkedTo))
                    destinations.Add(linkedTo);
            }
        }

        private void AddTags(
            Dictionary<string, string> index,
            Dictionary<string, string> typedIndex,
            string areaResRef,
            IEnumerable<JsonElement> instances,
            ResourceType blueprintType,
            string templateField = "TemplateResRef",
            Dictionary<string, Dictionary<string, int>>? countsByArea = null)
        {
            foreach (var instance in instances)
            {
                var tag = FieldString(instance, "Tag");
                if (string.IsNullOrEmpty(tag))
                    tag = BlueprintTag(blueprintType, FieldString(instance, templateField));

                if (!string.IsNullOrEmpty(tag))
                {
                    index.TryAdd(tag, areaResRef);
                    typedIndex.TryAdd(tag, areaResRef);
                    if (countsByArea != null)
                    {
                        if (!countsByArea.TryGetValue(tag, out var byArea))
                        {
                            byArea = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            countsByArea.Add(tag, byArea);
                        }

                        byArea[areaResRef] = byArea.GetValueOrDefault(areaResRef) + 1;
                    }
                }
            }
        }

        private static string? FieldString(JsonElement instance, string label)
        {
            return instance.TryGetProperty(label, out var field) &&
                   field.TryGetProperty("value", out var value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private string? BlueprintTag(ResourceType type, string? resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return null;

            var cacheKey = $"{type}:{resRef}";
            if (_blueprintTags.TryGetValue(cacheKey, out var cachedTag))
                return cachedTag;

            string? tag;
            try
            {
                var path = _workspace.GetResourcePath(type, resRef);
                tag = File.Exists(path)
                    ? Gff.JsonGffDocument.Load(path).Root.GetStringOrNull("Tag")
                    : null;
            }
            catch (Exception)
            {
                tag = null;
            }

            _blueprintTags[cacheKey] = tag;
            return tag;
        }

        private Dictionary<string, string> IndexItemBlueprints()
        {
            var items = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var resRef in _workspace.EnumerateResRefs(ResourceType.Uti))
            {
                var tag = BlueprintTag(ResourceType.Uti, resRef);
                if (!string.IsNullOrWhiteSpace(tag))
                    items.TryAdd(tag, resRef);
            }

            return items;
        }
    }
}
