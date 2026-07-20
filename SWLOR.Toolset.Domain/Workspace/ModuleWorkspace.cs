using SWLOR.Toolset.Domain.Documents;

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

        public ModuleWorkspace(string moduleRoot)
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
        public string GetResourceFolder(ResourceType type) => Path.Combine(ModuleRoot, type.Extension());

        /// <summary>The on-disk path for one resource (e.g. ".../Module/utc/mynpc.utc.json").</summary>
        public string GetResourcePath(ResourceType type, string resRef) =>
            Path.Combine(GetResourceFolder(type), resRef + "." + type.Extension() + ".json");

        /// <summary>
        /// Enumerates every resref present for a resource type by listing the folder - no file is
        /// opened or parsed. Returns an empty list if the type's folder does not exist.
        /// </summary>
        public IReadOnlyList<string> EnumerateResRefs(ResourceType type)
        {
            var folder = GetResourceFolder(type);
            if (!Directory.Exists(folder))
                return Array.Empty<string>();

            var suffix = "." + type.Extension() + ".json";
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
        /// Loads the three files that make up one area instance: the .are (static area properties),
        /// .git (placed object instances), and .gic (toolset-only comments) documents for the same
        /// resref.
        /// </summary>
        public (AreDocument Are, GitDocument Git, GicDocument Gic) LoadArea(string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));

            var are = AreDocument.Load(GetResourcePath(ResourceType.Area, resRef));
            var git = GitDocument.Load(Path.Combine(ModuleRoot, "git", resRef + ".git.json"));
            var gic = GicDocument.Load(Path.Combine(ModuleRoot, "gic", resRef + ".gic.json"));

            return (are, git, gic);
        }

        /// <summary>
        /// Loads a single blueprint document by type and resref. <paramref name="type"/> must be
        /// one of <see cref="BlueprintTypes"/> (use <see cref="LoadArea"/> for areas).
        /// </summary>
        public GffDocumentBase LoadBlueprint(ResourceType type, string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));

            var path = GetResourcePath(type, resRef);
            var bytes = File.ReadAllBytes(path);

            return type switch
            {
                ResourceType.Utc => UtcDocument.Parse(bytes),
                ResourceType.Uti => UtiDocument.Parse(bytes),
                ResourceType.Utp => UtpDocument.Parse(bytes),
                ResourceType.Utd => UtdDocument.Parse(bytes),
                ResourceType.Utm => UtmDocument.Parse(bytes),
                ResourceType.Utt => UttDocument.Parse(bytes),
                ResourceType.Uts => UtsDocument.Parse(bytes),
                ResourceType.Utw => UtwDocument.Parse(bytes),
                ResourceType.Area => throw new ArgumentException("Use LoadArea for area resources.", nameof(type)),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.")
            };
        }
    }
}
