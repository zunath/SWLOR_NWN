using System.Text.Json;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// The module placement scan could not read every area, so its results would be incomplete.
    /// The failed areas are retained for diagnostics and a later query retries the complete scan.
    /// </summary>
    public sealed class PlacementIndexIncompleteException : IOException
    {
        public IReadOnlyList<string> AreaResRefs { get; }

        internal PlacementIndexIncompleteException(
            IReadOnlyList<(string AreaResRef, Exception Error)> failures)
            : base(
                $"Could not scan placements in {failures.Count} " +
                $"area{(failures.Count == 1 ? string.Empty : "s")}: " +
                $"{string.Join(", ", failures.Select(failure => failure.AreaResRef))}. " +
                "Refresh to retry.",
                new AggregateException(failures.Select(failure => failure.Error)))
        {
            AreaResRefs = failures.Select(failure => failure.AreaResRef).ToList();
        }
    }

    /// <summary>
    /// Module-wide map from a blueprint to every area instance placed from it. The first query builds
    /// all supported kinds together on a bounded worker pool; subsequent Source tabs are lookups.
    /// </summary>
    public sealed class ModulePlacementIndex
    {
        private static readonly (ResourceType Type, string List, string Template, string X, string Y, string Z)[]
            Lists =
            {
                (ResourceType.Utc, "Creature List", "TemplateResRef", "XPosition", "YPosition", "ZPosition"),
                (ResourceType.Utp, "Placeable List", "TemplateResRef", "X", "Y", "Z"),
                (ResourceType.Utd, "Door List", "TemplateResRef", "X", "Y", "Z"),
                (ResourceType.Utw, "WaypointList", "TemplateResRef", "XPosition", "YPosition", "ZPosition"),
                (ResourceType.Utm, "StoreList", "ResRef", "XPosition", "YPosition", "ZPosition"),
                (ResourceType.Uts, "SoundList", "TemplateResRef", "XPosition", "YPosition", "ZPosition"),
                (ResourceType.Utt, "TriggerList", "TemplateResRef", "XPosition", "YPosition", "ZPosition"),
                (ResourceType.Uti, "List", "TemplateResRef", "XPosition", "YPosition", "ZPosition")
            };

        private readonly ModuleWorkspace _workspace;
        private readonly object _syncRoot = new();
        private Task<IReadOnlyDictionary<string, IReadOnlyList<ObjectPlacement>>>? _buildTask;
        private CancellationTokenSource? _buildCancellation;
        private int _generation;

        public ModulePlacementIndex(ModuleWorkspace workspace) =>
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

        /// <summary>Raised on the scanning worker when one area's GIT cannot be indexed.</summary>
        public event Action<string, Exception>? AreaReadFailed;

        /// <summary>Starts or joins the shared module-wide scan without requesting a specific blueprint.</summary>
        public async Task WarmAsync() => _ = await GetIndexAsync().ConfigureAwait(false);

        public async Task<IReadOnlyList<ObjectPlacement>> FindAsync(
            ResourceType type,
            string blueprintResRef)
        {
            if (string.IsNullOrWhiteSpace(blueprintResRef))
                return Array.Empty<ObjectPlacement>();

            var index = await GetIndexAsync().ConfigureAwait(false);
            return index.TryGetValue(Key(type, blueprintResRef), out var placements)
                ? placements
                : Array.Empty<ObjectPlacement>();
        }

        private async Task<IReadOnlyDictionary<string, IReadOnlyList<ObjectPlacement>>> GetIndexAsync()
        {
            while (true)
            {
                Task<IReadOnlyDictionary<string, IReadOnlyList<ObjectPlacement>>> task;
                int generation;
                lock (_syncRoot)
                {
                    generation = _generation;
                    if (_buildTask == null)
                    {
                        _buildCancellation = new CancellationTokenSource();
                        var cancellationToken = _buildCancellation.Token;
                        _buildTask = Task.Run(
                            () => Build(cancellationToken),
                            CancellationToken.None);
                    }

                    task = _buildTask;
                }

                IReadOnlyDictionary<string, IReadOnlyList<ObjectPlacement>> index;
                try
                {
                    index = await task.ConfigureAwait(false);
                }
                catch
                {
                    CancellationTokenSource? completedCancellation = null;
                    lock (_syncRoot)
                    {
                        if (ReferenceEquals(_buildTask, task))
                        {
                            _buildTask = null;
                            completedCancellation = _buildCancellation;
                            _buildCancellation = null;
                        }
                    }

                    completedCancellation?.Dispose();
                    throw;
                }

                CancellationTokenSource? successfulCancellation = null;
                var retry = false;
                lock (_syncRoot)
                {
                    if (ReferenceEquals(_buildTask, task))
                    {
                        successfulCancellation = _buildCancellation;
                        _buildCancellation = null;
                    }

                    if (generation != _generation)
                        retry = true;
                }

                successfulCancellation?.Dispose();
                if (retry)
                    continue;

                return index;
            }
        }

        public void Invalidate()
        {
            Task<IReadOnlyDictionary<string, IReadOnlyList<ObjectPlacement>>>? obsoleteTask;
            CancellationTokenSource? obsoleteCancellation;
            lock (_syncRoot)
            {
                _generation++;
                obsoleteTask = _buildTask;
                _buildTask = null;
                obsoleteCancellation = _buildCancellation;
                _buildCancellation = null;
            }

            if (obsoleteCancellation == null)
                return;

            obsoleteCancellation.Cancel();
            if (obsoleteTask == null || obsoleteTask.IsCompleted)
            {
                obsoleteCancellation.Dispose();
                return;
            }

            _ = obsoleteTask.ContinueWith(
                _ => obsoleteCancellation.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private IReadOnlyDictionary<string, IReadOnlyList<ObjectPlacement>> Build(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var areas = _workspace.EnumerateAreaResRefs();
            var perArea = new List<ObjectPlacement>?[areas.Count];
            var failures = new (string AreaResRef, Exception Error)?[areas.Count];
            Parallel.For(
                0,
                areas.Count,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 4)),
                    CancellationToken = cancellationToken
                },
                index =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        perArea[index] = ReadArea(areas[index]);
                    }
                    catch (Exception ex)
                    {
                        failures[index] = (areas[index], ex);
                        AreaReadFailed?.Invoke(areas[index], ex);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                });

            cancellationToken.ThrowIfCancellationRequested();
            var failedAreas = failures
                .Where(failure => failure.HasValue)
                .Select(failure => failure!.Value)
                .ToList();
            if (failedAreas.Count > 0)
                throw new PlacementIndexIncompleteException(failedAreas);

            var result = new Dictionary<string, List<ObjectPlacement>>(StringComparer.OrdinalIgnoreCase);
            foreach (var placements in perArea)
            foreach (var placement in placements ?? Enumerable.Empty<ObjectPlacement>())
            {
                var key = Key(placement.BlueprintType, placement.BlueprintResRef);
                if (!result.TryGetValue(key, out var values))
                {
                    values = new List<ObjectPlacement>();
                    result.Add(key, values);
                }

                values.Add(placement);
            }

            return result.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<ObjectPlacement>)pair.Value
                    .OrderBy(value => value.AreaResRef, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(value => value.InstanceIndex)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
        }

        private List<ObjectPlacement> ReadArea(string areaResRef)
        {
            var path = Path.Combine(_workspace.ModuleRoot, "git", areaResRef + ".git.json");
            using var document = JsonDocument.Parse(NwnJsonEncoding.ReadFileAsUtf8(path));
            var root = document.RootElement;
            var placements = new List<ObjectPlacement>();

            foreach (var config in Lists)
            {
                if (!root.TryGetProperty(config.List, out var field) ||
                    !field.TryGetProperty("value", out var values) ||
                    values.ValueKind != JsonValueKind.Array)
                    continue;

                var instanceIndex = 0;
                foreach (var instance in values.EnumerateArray())
                {
                    var resRef = FieldString(instance, config.Template);
                    if (!string.IsNullOrWhiteSpace(resRef))
                    {
                        placements.Add(new ObjectPlacement(
                            config.Type,
                            resRef,
                            areaResRef,
                            instanceIndex,
                            FieldString(instance, "Tag") ?? string.Empty,
                            FieldSingle(instance, config.X),
                            FieldSingle(instance, config.Y),
                            FieldSingle(instance, config.Z)));
                    }

                    instanceIndex++;
                }
            }

            return placements;
        }

        private static string Key(ResourceType type, string resRef) => $"{(int)type}:{resRef}";

        private static string? FieldString(JsonElement instance, string name) =>
            instance.TryGetProperty(name, out var field) &&
            field.TryGetProperty("value", out var value) &&
            value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static float FieldSingle(JsonElement instance, string name) =>
            instance.TryGetProperty(name, out var field) &&
            field.TryGetProperty("value", out var value) &&
            value.ValueKind == JsonValueKind.Number &&
            value.TryGetSingle(out var result)
                ? result
                : 0f;

    }
}
