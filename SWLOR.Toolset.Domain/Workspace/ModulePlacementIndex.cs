using System.Text;
using System.Text.Json;
using System.Text.Unicode;

namespace SWLOR.Toolset.Domain.Workspace
{
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
        private int _generation;

        public ModulePlacementIndex(ModuleWorkspace workspace) =>
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

        public async Task<IReadOnlyList<ObjectPlacement>> FindAsync(
            ResourceType type,
            string blueprintResRef)
        {
            if (string.IsNullOrWhiteSpace(blueprintResRef))
                return Array.Empty<ObjectPlacement>();

            while (true)
            {
                Task<IReadOnlyDictionary<string, IReadOnlyList<ObjectPlacement>>> task;
                int generation;
                lock (_syncRoot)
                {
                    generation = _generation;
                    task = _buildTask ??= Task.Run(Build);
                }

                var index = await task.ConfigureAwait(false);
                lock (_syncRoot)
                {
                    if (generation != _generation)
                        continue;
                }

                return index.TryGetValue(Key(type, blueprintResRef), out var placements)
                    ? placements
                    : Array.Empty<ObjectPlacement>();
            }
        }

        public void Invalidate()
        {
            lock (_syncRoot)
            {
                _generation++;
                _buildTask = null;
            }
        }

        private IReadOnlyDictionary<string, IReadOnlyList<ObjectPlacement>> Build()
        {
            var areas = _workspace.EnumerateAreaResRefs();
            var perArea = new List<ObjectPlacement>?[areas.Count];
            Parallel.For(
                0,
                areas.Count,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 4))
                },
                index =>
                {
                    try
                    {
                        perArea[index] = ReadArea(areas[index]);
                    }
                    catch
                    {
                        // One malformed GIT must not hide valid placements in every other area.
                    }
                });

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
            using var document = JsonDocument.Parse(ReadAsUtf8(path));
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

        private static byte[] ReadAsUtf8(string path)
        {
            var raw = File.ReadAllBytes(path);
            return Utf8.IsValid(raw) ? raw : Encoding.UTF8.GetBytes(NwnText.GetString(raw));
        }

        private static readonly Encoding NwnText = CreateNwnTextEncoding();

        private static Encoding CreateNwnTextEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252);
        }
    }
}
