using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Merchants
{
    /// <summary>One placed instance of the merchant, including its owning area and sync state.</summary>
    public sealed record MerchantInstancePlacement(
        string AreaName,
        string AreaResRef,
        string Tag,
        int InstanceIndex,
        bool IsCurrent)
    {
        public string Status => IsCurrent ? "Up to date" : "Out of date";
    }

    /// <summary>
    /// Finds placed stores and updates them with the same canonical expansion used by the CLI.
    /// </summary>
    public sealed class MerchantInstanceService
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly OutputLogService _log;
        private readonly Func<string, bool>? _isAreaOpen;

        public MerchantInstanceService(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            Func<string, bool>? isAreaOpen = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _isAreaOpen = isAreaOpen;
        }

        public Task<IReadOnlyList<MerchantInstancePlacement>> FindAsync(string merchantResRef)
        {
            var workspace = _workspaceContext.Workspace;
            var catalog = _workspaceContext.Catalog;
            if (workspace == null)
                return Task.FromResult<IReadOnlyList<MerchantInstancePlacement>>(
                    Array.Empty<MerchantInstancePlacement>());

            return Task.Run(() => Find(workspace, catalog, merchantResRef));
        }

        public async Task<int> UpdateOutOfDateAsync(string merchantResRef)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return 0;

            var openAreas = _isAreaOpen == null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : workspace.EnumerateAreaResRefs()
                    .Where(_isAreaOpen)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var updated = await Task.Run(
                () => Update(workspace, merchantResRef, openAreas)).ConfigureAwait(true);
            if (updated > 0)
            {
                _workspaceContext.InvalidateTagIndex();
                _workspaceContext.InvalidateScriptUsages();
                _log.AppendLine(
                    $"Updated {updated} placed instance{(updated == 1 ? string.Empty : "s")} of merchant " +
                    $"'{merchantResRef}'.");
            }

            return updated;
        }

        private static IReadOnlyList<MerchantInstancePlacement> Find(
            ModuleWorkspace workspace,
            BlueprintCatalog? catalog,
            string merchantResRef)
        {
            var merchant = JsonGffDocument.Load(
                workspace.GetResourcePath(ResourceType.Utm, merchantResRef));
            var itemCache = new Dictionary<string, JsonGffDocument?>(StringComparer.OrdinalIgnoreCase);
            JsonGffDocument? LoadItem(string resRef) =>
                itemCache.TryGetValue(resRef, out var cached)
                    ? cached
                    : itemCache[resRef] = TryLoadItem(workspace, resRef);

            var placements = new List<MerchantInstancePlacement>();
            foreach (var areaResRef in workspace.EnumerateAreaResRefs())
            {
                GitDocument git;
                try
                {
                    git = workspace.LoadGit(areaResRef);
                }
                catch
                {
                    continue;
                }

                var areaName = ResolveAreaName(workspace, catalog, areaResRef);
                for (var index = 0; index < git.Stores.Count; index++)
                {
                    var store = git.Stores[index];
                    if (!string.Equals(
                            store.GetStringOrNull("ResRef"),
                            merchantResRef,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    placements.Add(new MerchantInstancePlacement(
                        areaName,
                        areaResRef,
                        store.GetStringOrNull("Tag") ?? string.Empty,
                        index,
                        StoreInstanceSynchronizer.IsCurrent(
                            merchant, store, merchantResRef, LoadItem)));
                }
            }

            return placements
                .OrderBy(placement => placement.AreaName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(placement => placement.AreaResRef, StringComparer.OrdinalIgnoreCase)
                .ThenBy(placement => placement.InstanceIndex)
                .ToList();
        }

        private static int Update(
            ModuleWorkspace workspace,
            string merchantResRef,
            IReadOnlySet<string> openAreas)
        {
            var merchant = JsonGffDocument.Load(
                workspace.GetResourcePath(ResourceType.Utm, merchantResRef));
            var itemCache = new Dictionary<string, JsonGffDocument?>(StringComparer.OrdinalIgnoreCase);
            JsonGffDocument? LoadItem(string resRef) =>
                itemCache.TryGetValue(resRef, out var cached)
                    ? cached
                    : itemCache[resRef] = TryLoadItem(workspace, resRef);

            var staged = new List<SaveService.StagedWrite>();
            var updated = 0;
            try
            {
                foreach (var areaResRef in workspace.EnumerateAreaResRefs())
                {
                    var path = Path.Combine(workspace.ModuleRoot, "git", areaResRef + ".git.json");
                    GitDocument git;
                    try
                    {
                        git = GitDocument.Load(path);
                    }
                    catch
                    {
                        continue;
                    }

                    var replacements = new List<(int Index, JsonGffStruct Expected)>();
                    for (var index = 0; index < git.Stores.Count; index++)
                    {
                        var store = git.Stores[index];
                        if (!string.Equals(
                                store.GetStringOrNull("ResRef"),
                                merchantResRef,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var expected = StoreInstanceSynchronizer.BuildExpected(
                            merchant, store, merchantResRef, LoadItem);
                        if (!StoreInstanceSynchronizer.Equivalent(store, expected))
                            replacements.Add((index, expected));
                    }

                    if (replacements.Count == 0)
                        continue;

                    if (openAreas.Contains(areaResRef))
                    {
                        throw new InvalidOperationException(
                            $"Close the open area '{areaResRef}' before updating its placed merchant instance.");
                    }

                    using (EditScope.EnterConstruction())
                    {
                        var list = git.Fields.GetOrNull("StoreList")
                                   ?? throw new InvalidDataException(
                                       $"Area '{areaResRef}' has stores but no StoreList field.");
                        foreach (var replacement in replacements.OrderByDescending(value => value.Index))
                        {
                            list.RemoveElementAt(replacement.Index);
                            list.InsertElement(replacement.Index, replacement.Expected);
                        }
                    }

                    staged.Add(SaveService.Stage(path, git.ToBytes()));
                    updated += replacements.Count;
                }

                SaveService.CommitAll(staged);
                return updated;
            }
            catch
            {
                foreach (var write in staged)
                    SaveService.Discard(write);
                throw;
            }
        }

        private static JsonGffDocument? TryLoadItem(ModuleWorkspace workspace, string resRef)
        {
            try
            {
                return workspace.LoadBlueprint(ResourceType.Uti, resRef).Document;
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveAreaName(
            ModuleWorkspace workspace,
            BlueprintCatalog? catalog,
            string areaResRef)
        {
            if (catalog?.TryGetEntry(ResourceType.Area, areaResRef, out var entry) == true &&
                !string.IsNullOrWhiteSpace(entry.Name))
            {
                return entry.Name!;
            }

            try
            {
                return AreDocument.Load(workspace.GetResourcePath(ResourceType.Area, areaResRef))
                           .Name.Text
                       ?? areaResRef;
            }
            catch
            {
                return areaResRef;
            }
        }
    }
}
