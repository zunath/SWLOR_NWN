using SWLOR.NWN.Formats.Gff;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// Opens a module ROOT directory (the folder directly containing "are", "utc", "uti", etc.
    /// subfolders - i.e. the repository's <c>Module</c> folder) and provides typed, lazy access to
    /// its contents. Enumeration is a cheap directory listing (no file parsing); loading a specific
    /// area or blueprint parses only that resource's JSON. This class watches nothing itself -
    /// reacting to on-disk changes (FileSystemWatcher) is an app-layer concern.
    /// </summary>
    public sealed class ModuleWorkspace
    {
        /// <summary>Every blueprint type this package supports, in the same order the plan lists them.</summary>
        public static readonly IReadOnlyList<ResourceType> BlueprintTypes = new[]
        {
            ResourceType.Utc, ResourceType.Uti, ResourceType.Utp, ResourceType.Utd,
            ResourceType.Utm, ResourceType.Utt, ResourceType.Uts, ResourceType.Utw
        };

        /// <summary>The resolved, absolute module root directory (the folder containing "are", "utc", ...).</summary>
        public string ModuleRoot { get; }

        /// <summary>
        /// The layered game-resource view (base-game KEY/BIF plus the hak stack), when one is available.
        /// Only <see cref="LoadBlueprint"/> uses it, and only as a fallback: it is what lets the palette's
        /// Standard group hand back a blueprint the module has no file of its own for.
        /// </summary>
        public ResourceIndex? ResourceIndex { get; }

        public ModuleWorkspace(string moduleRoot, ResourceIndex? resourceIndex = null)
        {
            if (string.IsNullOrWhiteSpace(moduleRoot))
                throw new ArgumentException("Module root path must be provided.", nameof(moduleRoot));

            var fullPath = Path.GetFullPath(moduleRoot);
            if (!Directory.Exists(fullPath))
                throw new DirectoryNotFoundException($"Module root directory not found: {fullPath}");

            if (!LooksLikeModuleRoot(fullPath))
            {
                throw new InvalidOperationException(
                    $"'{fullPath}' does not look like a module root (expected \"are\" and \"utc\" subfolders).");
            }

            ModuleRoot = fullPath;
            ResourceIndex = resourceIndex;
            _tagIndex = new Lazy<ModuleTagIndex>(() => new ModuleTagIndex(this));
            _placementIndex = new Lazy<ModulePlacementIndex>(() => new ModulePlacementIndex(this));
        }

        /// <summary>
        /// Cheap layout check: a real module root has at least an "are" folder (areas) and a "utc"
        /// folder (creature blueprints). Does not require every blueprint-type folder to exist -
        /// a synthetic/partial module directory (e.g. a test fixture) still counts.
        /// </summary>
        public static bool LooksLikeModuleRoot(string path)
        {
            return Directory.Exists(Path.Combine(path, "are")) && Directory.Exists(Path.Combine(path, "utc"));
        }

        /// <summary>The subfolder for a resource type (e.g. ".../Module/utc").</summary>
        /// <summary>
        /// Resolves placed waypoint/door tags and module item-blueprint tags. Built lazily on first
        /// use; behavior editors consume it to validate transition destinations and door keys.
        /// </summary>
        public ModuleTagIndex TagIndex => _tagIndex.Value;

        private readonly Lazy<ModuleTagIndex> _tagIndex;

        /// <summary>Every blueprint-backed object placement in the module, built lazily in the background.</summary>
        public ModulePlacementIndex PlacementIndex => _placementIndex.Value;

        private readonly Lazy<ModulePlacementIndex> _placementIndex;

        public string GetResourceFolder(ResourceType type) => Path.Combine(ModuleRoot, type.Extension());

        /// <summary>
        /// The on-disk path for one resource (e.g. ".../Module/utc/mynpc.utc.json", or
        /// ".../Module/nss/myscript.nss" for the one type that is not JSON-encoded).
        /// </summary>
        public string GetResourcePath(ResourceType type, string resRef) =>
            Path.Combine(GetResourceFolder(type), resRef + FileSuffix(type));

        /// <summary>
        /// The server-owned NUI conversation directory beside the unpacked Module directory.
        /// Conversations are source data embedded into SWLOR.Game.Server at build time, not Aurora
        /// DLG resources stored under Module/dlg.
        /// </summary>
        public string ConversationDataRoot => ResolveConversationDataRoot(ModuleRoot);

        /// <summary>
        /// Resolves the server-owned conversation source directory beside an unpacked module.
        /// Shared by the workspace, file watcher, and pack lock so all three agree on its identity.
        /// </summary>
        public static string ResolveConversationDataRoot(string moduleRoot)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
            return Path.GetFullPath(Path.Combine(
                moduleRoot,
                "..",
                "SWLOR.Game.Server",
                "ConversationData"));
        }

        /// <summary>The authored graph path for one conversation resref.</summary>
        public string GetConversationGraphPath(string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));

            return Path.Combine(ConversationDataRoot, resRef + ".conversation.json");
        }

        /// <summary>
        /// Enumerates graph-native conversations without parsing them. A malformed graph remains
        /// visible in Module Contents so the builder can open it and see the error.
        /// </summary>
        public IReadOnlyList<string> EnumerateConversationGraphResRefs()
        {
            if (!Directory.Exists(ConversationDataRoot))
                return Array.Empty<string>();

            const string suffix = ".conversation.json";
            var results = Directory.EnumerateFiles(ConversationDataRoot, "*" + suffix)
                .Select(Path.GetFileName)
                .Where(fileName => fileName != null && fileName.Length > suffix.Length)
                .Select(fileName => fileName![..^suffix.Length])
                .OrderBy(resRef => resRef, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return results;
        }

        /// <summary>The filename suffix a resource of this type carries, leading dot included.</summary>
        private static string FileSuffix(ResourceType type) =>
            type.IsJsonEncoded() ? "." + type.Extension() + ".json" : "." + type.Extension();

        /// <summary>
        /// Enumerates every resref present for a resource type by listing the folder - no file is
        /// opened or parsed. Returns an empty list if the type's folder does not exist.
        /// </summary>
        public IReadOnlyList<string> EnumerateResRefs(ResourceType type)
        {
            var folder = GetResourceFolder(type);
            if (!Directory.Exists(folder))
                return Array.Empty<string>();

            var suffix = FileSuffix(type);
            var results = new List<string>();

            foreach (var file in Directory.EnumerateFiles(folder, "*" + suffix))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.Length > suffix.Length)
                    results.Add(fileName[..^suffix.Length]);
            }

            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        /// <summary>Convenience alias for <see cref="EnumerateResRefs"/> with <see cref="ResourceType.Area"/>.</summary>
        public IReadOnlyList<string> EnumerateAreaResRefs() => EnumerateResRefs(ResourceType.Area);

        /// <summary>
        /// Loads only the placed-object document for an area. Module-wide instance indexes use this
        /// instead of <see cref="LoadArea"/> so they do not parse the unrelated ARE and GIC files.
        /// </summary>
        public GitDocument LoadGit(string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));

            return GitDocument.Load(Path.Combine(ModuleRoot, "git", resRef + ".git.json"));
        }

        /// <summary>
        /// Loads the three files that make up one area instance: the .are (static area properties),
        /// .git (placed object instances), and .gic (toolset-only comments) documents for the same
        /// resref.
        /// </summary>
        public (AreDocument Are, GitDocument Git, GicDocument Gic) LoadArea(string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));

            var are = AreDocument.Load(GetResourcePath(ResourceType.Area, resRef));
            var git = LoadGit(resRef);
            var gic = GicDocument.Load(Path.Combine(ModuleRoot, "gic", resRef + ".gic.json"));

            return (are, git, gic);
        }

        /// <summary>
        /// Loads a single blueprint document by type and resref. <paramref name="type"/> must be
        /// one of <see cref="BlueprintTypes"/> (use <see cref="LoadArea"/> for areas).
        /// </summary>
        /// <remarks>
        /// The module's own unpacked JSON always wins, so a resref the module overrides reads as the
        /// module authored it. Only a miss reaches <see cref="ResourceIndex"/>, which is how the palette's
        /// Standard group can open and place a base-game blueprint that exists nowhere in the module.
        /// </remarks>
        public GffDocumentBase LoadBlueprint(ResourceType type, string resRef)
        {
            if (TryLoadBlueprint(type, resRef, out var document))
                return document;

            var path = GetResourcePath(type, resRef);
            throw new FileNotFoundException(
                $"Blueprint '{resRef}.{type.Extension()}' was not found in the module at '{path}'" +
                (ResourceIndex == null
                    ? ", and no game resource index is available to fall back to."
                    : ", nor in the base game / hak resource index."),
                path);
        }

        /// <summary>
        /// Attempts to load a blueprint using the same module-first, indexed-resource fallback as
        /// <see cref="LoadBlueprint"/>. An absent resource returns false instead of throwing; malformed
        /// or unreadable resources still throw because those are content failures, not ordinary misses.
        /// </summary>
        public bool TryLoadBlueprint(
            ResourceType type,
            string resRef,
            out GffDocumentBase document)
        {
            ValidateBlueprintRequest(type, resRef);

            var path = GetResourcePath(type, resRef);
            if (File.Exists(path))
            {
                document = Wrap(type, JsonGffDocument.Parse(File.ReadAllBytes(path)));
                return true;
            }

            if (TryLoadFromResourceIndex(type, resRef, out var indexed))
            {
                document = Wrap(type, indexed);
                return true;
            }

            document = null!;
            return false;
        }

        /// <summary>
        /// Loads a blueprint specifically from the indexed Standard/HAK layers, bypassing any
        /// same-resref module override. This preserves the Palette's source choice through placement.
        /// </summary>
        public GffDocumentBase LoadIndexedBlueprint(ResourceType type, string resRef)
        {
            if (TryLoadIndexedBlueprint(type, resRef, out var document))
                return document;

            throw new FileNotFoundException(
                $"Standard blueprint '{resRef}.{type.Extension()}' was not found in the base game / hak resource index.");
        }

        /// <summary>
        /// Attempts to load a blueprint from only the indexed Standard/HAK layers. An absent resource
        /// returns false instead of using <see cref="FileNotFoundException"/> as normal control flow.
        /// </summary>
        public bool TryLoadIndexedBlueprint(
            ResourceType type,
            string resRef,
            out GffDocumentBase document)
        {
            ValidateBlueprintRequest(type, resRef);

            if (TryLoadFromResourceIndex(type, resRef, out var indexed))
            {
                document = Wrap(type, indexed);
                return true;
            }

            document = null!;
            return false;
        }

        private static void ValidateBlueprintRequest(ResourceType type, string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));
            if (type == ResourceType.Area)
                throw new ArgumentException("Use LoadArea for area resources.", nameof(type));

            // Rejected before any I/O: a conversation or a script is not a blueprint, and reading one
            // first would surface a parse failure instead of the actual mistake.
            if (!BlueprintTypes.Contains(type))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Not a blueprint resource type.");
        }

        /// <summary>
        /// Resolves a blueprint through the layered resource index and bridges the binary GFF the game
        /// ships into the same JSON document model the module's files parse to, so everything downstream
        /// (editors, previews, placement) cannot tell the two apart.
        /// </summary>
        private bool TryLoadFromResourceIndex(ResourceType type, string resRef, out JsonGffDocument document)
        {
            document = null!;
            if (ResourceIndex == null)
                return false;

            var identity = new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension(type.Extension()));
            if (!ResourceIndex.TryLookup(identity, out var handle))
                return false;

            document = GffJsonBridge.ToJsonDocument(GffReader.Read(handle.GetBytes()));
            return true;
        }

        private static GffDocumentBase Wrap(ResourceType type, JsonGffDocument document)
        {
            return type switch
            {
                ResourceType.Utc => new UtcDocument(document),
                ResourceType.Uti => new UtiDocument(document),
                ResourceType.Utp => new UtpDocument(document),
                ResourceType.Utd => new UtdDocument(document),
                ResourceType.Utm => new UtmDocument(document),
                ResourceType.Utt => new UttDocument(document),
                ResourceType.Uts => new UtsDocument(document),
                ResourceType.Utw => new UtwDocument(document),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.")
            };
        }
    }
}
