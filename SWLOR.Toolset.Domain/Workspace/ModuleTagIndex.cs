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
    /// Built once by reading every area's .git. The application warms it in the background when a
    /// workspace opens; direct domain consumers still build it lazily on their first tag question.
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
        private Dictionary<string, string>? _areasByTag;
        private HashSet<string>? _transitionDestinationTags;
        private Dictionary<string, string>? _waypointAreasByTag;
        private Dictionary<string, string>? _doorAreasByTag;
        private Dictionary<string, string>? _itemResRefsByTag;
        private readonly Dictionary<string, string?> _blueprintTags =
            new(StringComparer.OrdinalIgnoreCase);
        private Task? _warmTask;
        private int _generation;

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

        /// <summary>
        /// Every non-empty destination tag named by an area transition trigger or door.
        /// </summary>
        public IReadOnlyCollection<string> TransitionDestinationTags
        {
            get
            {
                lock (_syncRoot)
                {
                    _ = Index();
                    return _transitionDestinationTags!
                        .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }
        }

        /// <summary>
        /// Returns transition destinations without ever making the caller perform a cold scan. This
        /// is the UI-facing path; it also remains safe if a file change invalidates a prior warm-up.
        /// </summary>
        public Task<IReadOnlyCollection<string>> GetTransitionDestinationTagsAsync() =>
            Task.Run(() => TransitionDestinationTags);

        /// <summary>
        /// Builds the index on a worker thread. Concurrent callers share the same build, while
        /// callers after an invalidation receive a fresh one.
        /// </summary>
        public Task WarmAsync()
        {
            lock (_syncRoot)
            {
                if (_areasByTag != null)
                    return Task.CompletedTask;
                if (_warmTask != null)
                    return _warmTask;

                var generation = _generation;
                _warmTask = Task.Run(() =>
                {
                    try
                    {
                        _ = Index();
                    }
                    finally
                    {
                        lock (_syncRoot)
                        {
                            if (_generation == generation)
                                _warmTask = null;
                        }
                    }
                });
                return _warmTask;
            }
        }

        /// <summary>Drops the cache so the next question re-reads the module.</summary>
        public void Invalidate()
        {
            lock (_syncRoot)
            {
                _generation++;
                _areasByTag = null;
                _transitionDestinationTags = null;
                _waypointAreasByTag = null;
                _doorAreasByTag = null;
                _itemResRefsByTag = null;
                _blueprintTags.Clear();
                _warmTask = null;
            }
        }

        private Dictionary<string, string> Index()
        {
            lock (_syncRoot)
            {
                if (_areasByTag != null)
                    return _areasByTag;

                var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var transitionDestinations = new HashSet<string>(StringComparer.Ordinal);
                var waypoints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var doors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var areaResRef in _workspace.EnumerateAreaResRefs())
                {
                    try
                    {
                        IndexGit(
                            index,
                            transitionDestinations,
                            waypoints,
                            doors,
                            areaResRef);
                    }
                    catch (Exception)
                    {
                        // One unreadable area must not cost every other area its tags.
                        continue;
                    }
                }

                _waypointAreasByTag = waypoints;
                _doorAreasByTag = doors;
                _areasByTag = index;
                _transitionDestinationTags = transitionDestinations;
                return index;
            }
        }

        private void IndexGit(
            Dictionary<string, string> index,
            HashSet<string> transitionDestinations,
            Dictionary<string, string> waypoints,
            Dictionary<string, string> doors,
            string areaResRef)
        {
            var path = Path.Combine(_workspace.ModuleRoot, "git", areaResRef + ".git.json");
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;

            // A GIT can contain tens of thousands of fields, but this index needs only three lists
            // and three scalar fields within their entries. JsonDocument avoids materializing the
            // complete editable GFF object graph that GitDocument.Load intentionally creates.
            AddTags(index, waypoints, areaResRef, Instances(root, "WaypointList"), ResourceType.Utw);
            AddTags(index, doors, areaResRef, Instances(root, "Door List"), ResourceType.Utd);
            AddTransitionDestinations(transitionDestinations, Instances(root, "TriggerList"));
            AddTransitionDestinations(transitionDestinations, Instances(root, "Door List"));
        }

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
            ResourceType blueprintType)
        {
            foreach (var instance in instances)
            {
                var tag = FieldString(instance, "Tag");
                if (string.IsNullOrEmpty(tag))
                    tag = BlueprintTag(blueprintType, FieldString(instance, "TemplateResRef"));

                if (!string.IsNullOrEmpty(tag))
                {
                    index.TryAdd(tag, areaResRef);
                    typedIndex.TryAdd(tag, areaResRef);
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
