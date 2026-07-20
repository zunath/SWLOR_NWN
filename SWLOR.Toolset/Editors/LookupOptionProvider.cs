using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>One selectable option of a 2DA-backed dropdown.</summary>
    public sealed record LookupOption(long Id, string Display)
    {
        public override string ToString() => $"{Id}: {Display}";
    }

    /// <summary>
    /// Maps schema LookupKeys to option lists built from the lookup services. Every service is
    /// optional — a missing service (or unknown key) yields an empty list, and the editor
    /// degrades that field to a plain numeric box.
    /// </summary>
    public sealed class LookupOptionProvider
    {
        private readonly AppearanceService? _appearances;
        private readonly PortraitService? _portraits;
        private readonly WorkspaceContext _workspaceContext;
        private readonly Dictionary<string, IReadOnlyList<LookupOption>> _cache = new(StringComparer.OrdinalIgnoreCase);

        public LookupOptionProvider(
            WorkspaceContext workspaceContext,
            AppearanceService? appearances = null,
            PortraitService? portraits = null)
        {
            _workspaceContext = workspaceContext;
            _appearances = appearances;
            _portraits = portraits;
        }

        public IReadOnlyList<LookupOption> GetOptions(string? lookupKey)
        {
            if (string.IsNullOrEmpty(lookupKey))
                return Array.Empty<LookupOption>();

            if (_cache.TryGetValue(lookupKey, out var cached))
                return cached;

            var options = Build(lookupKey);
            _cache[lookupKey] = options;
            return options;
        }

        private IReadOnlyList<LookupOption> Build(string lookupKey)
        {
            try
            {
                switch (lookupKey)
                {
                    case LookupKeys.Appearance when _appearances != null:
                        return _appearances.GetAll()
                            .Select(row => new LookupOption(row.Id, row.DisplayName))
                            .ToList();
                    case LookupKeys.Portraits when _portraits != null:
                        return _portraits.GetAll()
                            .Select(row => new LookupOption(row.Id, row.BaseResRef))
                            .ToList();
                    case LookupKeys.Factions:
                        return BuildFactions();
                    default:
                        return Array.Empty<LookupOption>();
                }
            }
            catch (Exception)
            {
                // A malformed lookup source must never break the editor; degrade to numeric.
                return Array.Empty<LookupOption>();
            }
        }

        /// <summary>Faction ids come from the module's repute.fac: FactionList index = id.</summary>
        private IReadOnlyList<LookupOption> BuildFactions()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return Array.Empty<LookupOption>();

            var facPath = Path.Combine(workspace.ModuleRoot, "fac", "repute.fac.json");
            if (!File.Exists(facPath))
                return Array.Empty<LookupOption>();

            var document = JsonGffDocument.Load(facPath);
            var factionList = document.Root.GetOrNull("FactionList");
            if (factionList?.Elements == null)
                return Array.Empty<LookupOption>();

            var options = new List<LookupOption>(factionList.Elements.Count);
            for (var i = 0; i < factionList.Elements.Count; i++)
            {
                var name = factionList.Elements[i].GetOrNull("FactionName")?.GetString() ?? $"Faction {i}";
                options.Add(new LookupOption(i, name));
            }

            return options;
        }
    }
}
