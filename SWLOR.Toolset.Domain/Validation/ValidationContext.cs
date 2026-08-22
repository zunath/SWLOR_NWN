using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>
    /// Shared state one <see cref="ModuleValidator"/> run hands to every <see cref="IValidationRule"/>:
    /// the workspace being validated, the optional game-code index (NPC groups / spawn table IDs),
    /// and small caches so that a resref enumerated or a file parsed by one rule is not re-enumerated
    /// or re-parsed by the next. Not thread-safe - a single <see cref="ModuleValidator"/> run walks
    /// rules sequentially against one context instance.
    /// </summary>
    public sealed class ValidationContext
    {
        public ModuleWorkspace Workspace { get; }

        public IGameCodeIndex? GameCodeIndex { get; }

        /// <summary>Optional hak/base-game resource index; lets rules distinguish "missing
        /// everywhere" from "provided by a hak or the base game rather than the module".</summary>
        public ResourceIndex? ResourceIndex { get; }

        private readonly Dictionary<ResourceType, IReadOnlyList<string>> _resRefsByType = new();
        private readonly Dictionary<ResourceType, HashSet<string>> _resRefSetsByType = new();
        private readonly Dictionary<string, (GitDocument? Document, Exception? Error)> _gitCache =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(ResourceType Type, string ResRef), (GffDocumentBase? Document, Exception? Error)> _blueprintCache = new();
        private readonly Dictionary<string, (ItpDocument? Document, Exception? Error)> _paletteCache =
            new(StringComparer.OrdinalIgnoreCase);

        public ValidationContext(
            ModuleWorkspace workspace,
            IGameCodeIndex? gameCodeIndex = null,
            ResourceIndex? resourceIndex = null)
        {
            Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            GameCodeIndex = gameCodeIndex;
            ResourceIndex = resourceIndex;
        }

        /// <summary>
        /// True when a resource of the given type/resref is provided by a hak or the base game
        /// (via the optional <see cref="ResourceIndex"/>) even though it has no module file.
        /// Always false when no index was supplied.
        /// </summary>
        public bool ResolvableOutsideModule(ResourceType type, string? resRef)
        {
            if (ResourceIndex == null || string.IsNullOrEmpty(resRef))
                return false;

            try
            {
                var identity = ResourceIdentity.FromFileName($"{resRef}.{type.Extension()}");
                return ResourceIndex.TryLookup(identity, out _);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Every area resref in the workspace (cached after the first call).</summary>
        public IReadOnlyList<string> AreaResRefs => ResRefsFor(ResourceType.Area);

        /// <summary>Every resref of the given resource type in the workspace (cached after the first call).</summary>
        public IReadOnlyList<string> ResRefsFor(ResourceType type)
        {
            if (_resRefsByType.TryGetValue(type, out var cached))
                return cached;

            var list = type switch
            {
                ResourceType.Area => Workspace.EnumerateAreaResRefs(),
                ResourceType.Dlg => Workspace.EnumerateConversationGraphResRefs()
                    .Concat(Workspace.EnumerateResRefs(ResourceType.Dlg))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(resRef => resRef, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                _ => Workspace.EnumerateResRefs(type)
            };
            _resRefsByType[type] = list;
            return list;
        }

        /// <summary>True if a blueprint/area of the given type and resref exists on disk (a cheap,
        /// case-insensitive lookup against <see cref="ResRefsFor"/> - no file is opened).</summary>
        public bool ResourceExists(ResourceType type, string? resRef)
        {
            if (string.IsNullOrEmpty(resRef))
                return false;

            if (!_resRefSetsByType.TryGetValue(type, out var set))
            {
                set = new HashSet<string>(ResRefsFor(type), StringComparer.OrdinalIgnoreCase);
                _resRefSetsByType[type] = set;
            }

            return set.Contains(resRef);
        }

        public string GetGitPath(string areaResRef) =>
            Path.Combine(Workspace.ModuleRoot, "git", areaResRef + ".git.json");

        public string GetPalettePath(string paletteName) =>
            Path.Combine(Workspace.ModuleRoot, "itp", paletteName + ".itp.json");

        /// <summary>Loads (and caches) the .git document for an area. Never throws - a parse
        /// failure comes back as a non-null <c>Error</c> with a null <c>Document</c>.</summary>
        public (GitDocument? Document, Exception? Error) LoadGit(string areaResRef)
        {
            if (_gitCache.TryGetValue(areaResRef, out var cached))
                return cached;

            (GitDocument? Document, Exception? Error) result;
            try
            {
                result = (GitDocument.Load(GetGitPath(areaResRef)), null);
            }
            catch (Exception ex)
            {
                result = (null, ex);
            }

            _gitCache[areaResRef] = result;
            return result;
        }

        /// <summary>Loads (and caches) one blueprint document. Never throws - a parse failure
        /// comes back as a non-null <c>Error</c> with a null <c>Document</c>.</summary>
        public (GffDocumentBase? Document, Exception? Error) LoadBlueprint(ResourceType type, string resRef)
        {
            var key = (type, resRef);
            if (_blueprintCache.TryGetValue(key, out var cached))
                return cached;

            (GffDocumentBase? Document, Exception? Error) result;
            try
            {
                result = (Workspace.LoadBlueprint(type, resRef), null);
            }
            catch (Exception ex)
            {
                result = (null, ex);
            }

            _blueprintCache[key] = result;
            return result;
        }

        /// <summary>Loads (and caches) one palette (.itp) file by its base file name (e.g.
        /// "waypointpalcus", "placeablepalcus") from the workspace's "itp" folder. Never throws -
        /// a parse failure (including a missing file) comes back as a non-null <c>Error</c>.</summary>
        public (ItpDocument? Document, Exception? Error) LoadPalette(string paletteName)
        {
            if (_paletteCache.TryGetValue(paletteName, out var cached))
                return cached;

            (ItpDocument? Document, Exception? Error) result;
            try
            {
                result = (ItpDocument.Load(GetPalettePath(paletteName)), null);
            }
            catch (Exception ex)
            {
                result = (null, ex);
            }

            _paletteCache[paletteName] = result;
            return result;
        }
    }
}
