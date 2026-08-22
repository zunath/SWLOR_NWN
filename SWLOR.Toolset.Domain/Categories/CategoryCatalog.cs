using System.Text.Json;
using SWLOR.Toolset.Domain.Categories.Json;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Categories
{
    /// <summary>
    /// The toolset's category sidecar: how a builder has chosen to organise areas and blueprints, held
    /// in one file per repository at <c>toolset/categories.json</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This must not live in the module.</b> NWN's own category trees are the <c>.itp</c> palettes
    /// under <c>Module/itp/</c>, which look like the obvious home and are the wrong one: the game and the
    /// Aurora toolset rewrite them, so anything stored there is eventually wiped. The same goes for the
    /// blueprints and areas themselves. Categories are toolset metadata, so they live beside the module
    /// rather than inside it, in a file nothing else owns.
    /// </para>
    /// <para>
    /// The catalog stores names, nesting and resref membership - never resource content. That keeps it
    /// additive: the module remains the single source of truth for what exists, and the sidecar only says
    /// how to arrange it. Deleting the file loses an arrangement and nothing else.
    /// </para>
    /// </remarks>
    public sealed class CategoryCatalog
    {
        /// <summary>Bumped only when the on-disk shape changes incompatibly.</summary>
        public const int CurrentVersion = 1;

        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly Dictionary<ResourceType, CategorySection> _sections = new();

        /// <summary>Where the sidecar was loaded from, or where <see cref="Save"/> will write it.</summary>
        public string? FilePath { get; private set; }

        /// <summary>True once anything has been changed since the last load or save.</summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// True when the file on disk is a version this build must not rewrite. Saving is refused rather
        /// than silently downgrading it.
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Why this catalog will not be written back, or null when it is writable. Carried rather than
        /// assumed, because there is now more than one reason and telling a builder their categories came
        /// from a newer Toolset when the file is actually truncated sends them looking in the wrong place.
        /// </summary>
        public string? ReadOnlyReason { get; private set; }

        /// <summary>
        /// The sidecar's conventional location for a module: a <c>toolset</c> folder beside the module
        /// directory, so it sits in the repository but outside anything pack, unpack or the game touches.
        /// </summary>
        public static string DefaultPathFor(string moduleRoot)
        {
            if (string.IsNullOrWhiteSpace(moduleRoot))
                throw new ArgumentException("A module root is required.", nameof(moduleRoot));

            var parent = Directory.GetParent(Path.GetFullPath(moduleRoot))?.FullName
                         ?? Path.GetFullPath(moduleRoot);

            return Path.Combine(parent, "toolset", "categories.json");
        }

        /// <summary>
        /// Loads the sidecar, or returns an empty catalog when it does not exist yet - a missing file is
        /// the normal first-run state, not an error. A malformed file is also non-fatal: callers get an
        /// empty catalog and the reason, because losing an arrangement must never block opening a module.
        /// </summary>
        public static CategoryCatalog Load(string path, out string? warning)
        {
            warning = null;
            var catalog = new CategoryCatalog { FilePath = path };

            if (!File.Exists(path))
                return catalog;

            CategoryFileDto? document;
            try
            {
                using var stream = File.OpenRead(path);
                document = JsonSerializer.Deserialize<CategoryFileDto>(stream, ReadOptions);
            }
            catch (Exception ex)
            {
                // Read-only, not merely empty. A sidecar that fails to parse is usually a live file in
                // trouble - unresolved merge-conflict markers, a truncated write - not an absent one. It
                // used to come back writable, and then startup seeded the Area section and saved over it,
                // destroying the builder's arrangement on nothing worse than opening the toolset.
                warning =
                    $"Could not read categories from '{path}': {ex.Message}. No categories are shown, and " +
                    "the file will not be overwritten - repair or delete it to start again.";
                catalog.IsReadOnly = true;
                catalog.ReadOnlyReason = warning;
                return catalog;
            }

            if (document == null)
            {
                // The JSON token `null` deserializes cleanly - no exception, so the catch above never
                // runs - but is not usable data. Read as a writable empty catalog here, it let the
                // normal section-seeding path save fresh sections straight over the file, destroying
                // whatever a truncated or corrupted write actually left behind. Treated as invalid
                // instead, the same as a file that fails to parse at all.
                warning =
                    $"Could not read categories from '{path}': the file contains only the JSON value " +
                    "'null'. No categories are shown, and the file will not be overwritten - repair or " +
                    "delete it to start again.";
                catalog.IsReadOnly = true;
                catalog.ReadOnlyReason = warning;
                return catalog;
            }

            // A sidecar from a newer, incompatible Toolset must not be read as v1 and then rewritten as
            // v1 - that silently discards whatever this build does not understand. Left read-only
            // instead, but still read: the warning promises the categories are shown, and returning here
            // broke that promise, hiding the whole saved arrangement behind freshly seeded empties the
            // moment an older build opened a newer file. Whatever this version understands is loaded
            // below; what it does not is what the read-only flag is protecting.
            if (document.Version > CurrentVersion)
            {
                warning =
                    $"'{path}' was written by a newer Toolset (version {document.Version}; this build " +
                    $"understands {CurrentVersion}). Categories are shown as loaded but will not be saved.";
                catalog.IsReadOnly = true;
                catalog.ReadOnlyReason = warning;
            }

            if (document.Sections == null)
                return catalog;

            var repairedNames = new List<string>();
            foreach (var (key, sectionDto) in document.Sections)
            {
                if (!ResourceTypeExtensions.TryFromExtension(key, out var type) || sectionDto == null)
                    continue;

                catalog._sections[type] = ReadSection(sectionDto, repairedNames);
            }

            // Said rather than done silently: the builder is looking at a name this build changed, and the
            // change reaches the file the next time anything is saved. Not a read-only condition - the
            // sidecar is fine, it just predates the rule that a name may not hold a path separator.
            if (repairedNames.Count > 0)
            {
                var repair =
                    $"Repaired {repairedNames.Count} category name(s) in '{path}' that contained " +
                    $"'{CategoryFolder.PathSeparator}', which a category name cannot hold: " +
                    string.Join(", ", repairedNames.Select(name => $"'{name}'")) + ".";

                // Appended, never overwriting: the version warning above carries the read-only reason, and
                // losing it would leave a builder wondering why nothing they arrange is being saved.
                warning = warning == null ? repair : warning + " " + repair;
            }

            return catalog;
        }

        public static CategoryCatalog Load(string path) => Load(path, out _);

        /// <summary>The section for a type, created empty on first use.</summary>
        public CategorySection Section(ResourceType type)
        {
            if (!_sections.TryGetValue(type, out var section))
            {
                section = new CategorySection();
                _sections[type] = section;
            }

            return section;
        }

        /// <summary>The section for a type only if one already exists - does not create it.</summary>
        public CategorySection? SectionOrNull(ResourceType type) =>
            _sections.TryGetValue(type, out var section) ? section : null;

        public IEnumerable<ResourceType> Types => _sections.Keys;

        /// <summary>
        /// Returns an independent in-memory copy of this catalog.
        /// </summary>
        /// <remarks>
        /// Category edits are applied to the live tree before the sidecar is written. The workspace keeps
        /// this snapshot so a refused or failed write can restore the last persisted tree instead of
        /// leaving a change visible only until restart.
        /// </remarks>
        public CategoryCatalog DeepClone()
        {
            var clone = new CategoryCatalog
            {
                FilePath = FilePath,
                IsDirty = IsDirty,
                IsReadOnly = IsReadOnly,
                ReadOnlyReason = ReadOnlyReason
            };

            foreach (var (type, section) in _sections)
                clone._sections[type] = CloneSection(section);

            return clone;
        }

        /// <summary>Marks the catalog changed. Callers do this after mutating a section or folder.</summary>
        public void MarkDirty() => IsDirty = true;

        /// <summary>
        /// Writes the sidecar atomically, creating its folder when needed. Empty sections are dropped so
        /// the file stays a record of decisions rather than accumulating the residue of visited types.
        /// </summary>
        public void Save(string? path = null)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(
                    ReadOnlyReason ?? "This category sidecar will not be overwritten.");
            }

            var target = path ?? FilePath
                ?? throw new InvalidOperationException("No path to save the category sidecar to.");

            var directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var temporaryPath = target + ".tmp";
            File.WriteAllBytes(temporaryPath, ToJsonBytes());
            File.Move(temporaryPath, target, overwrite: true);

            FilePath = target;
            IsDirty = false;
        }

        public byte[] ToJsonBytes() =>
            System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ToDto(), WriteOptions));

        private CategoryFileDto ToDto()
        {
            var sections = new Dictionary<string, CategorySectionDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var (type, section) in _sections.OrderBy(pair => pair.Key.Extension(), StringComparer.Ordinal))
            {
                if (section.Folders.Count == 0 &&
                    section.Pinned.Count == 0 &&
                    !section.IsSeeded &&
                    section.Grouping == CategoryGrouping.Automatic)
                    continue;

                sections[type.Extension()] = new CategorySectionDto
                {
                    Seeded = section.IsSeeded,
                    GroupBy = section.Grouping.ToString().ToLowerInvariant(),
                    Pinned = section.Pinned.Count == 0 ? null : section.Pinned.ToList(),
                    Folders = section.Folders.Count == 0 ? null : section.Folders.Select(ToDto).ToList()
                };
            }

            return new CategoryFileDto { Version = CurrentVersion, Sections = sections };
        }

        private static CategoryFolderDto ToDto(CategoryFolder folder) => new()
        {
            Name = folder.Name,
            Children = folder.Children.Count == 0 ? null : folder.Children.Select(ToDto).ToList(),
            Members = folder.Members.Count == 0 ? null : folder.Members.ToList(),
            Placeholder = folder.IsUnresolvedPlaceholder
        };

        private static CategorySection CloneSection(CategorySection source)
        {
            var clone = new CategorySection
            {
                Grouping = source.Grouping,
                IsSeeded = source.IsSeeded
            };

            foreach (var pinned in source.Pinned)
                clone.Pin(pinned);

            foreach (var folder in source.Folders)
                clone.AddFolder(CloneFolder(folder));

            return clone;
        }

        private static CategoryFolder CloneFolder(CategoryFolder source)
        {
            var clone = new CategoryFolder(source.Name) { IsUnresolvedPlaceholder = source.IsUnresolvedPlaceholder };
            foreach (var member in source.Members)
                clone.AddMember(member);
            foreach (var child in source.Children)
                clone.AddChild(CloneFolder(child));

            return clone;
        }

        private static CategorySection ReadSection(CategorySectionDto dto, List<string> repairedNames)
        {
            var section = new CategorySection
            {
                Grouping = Enum.TryParse<CategoryGrouping>(dto.GroupBy, ignoreCase: true, out var grouping)
                    ? grouping
                    : CategoryGrouping.Automatic,

                // Sidecars written before the flag existed carry folders but no marker; treating those as
                // seeded is right, and is what stops them being re-imported on the next launch.
                IsSeeded = dto.Seeded || (dto.Folders?.Count ?? 0) > 0
            };

            foreach (var name in dto.Pinned ?? new List<string>())
                section.Pin(name);

            var repaired = new List<(string StoredPathKey, CategoryFolder Folder)>();
            foreach (var folderDto in dto.Folders ?? new List<CategoryFolderDto>())
            {
                var folder = ReadFolder(folderDto, storedParentPath: null, repaired);
                if (folder != null)
                    section.AddFolder(folder);
            }

            // A pin is stored as a path key built from names, so a name that had to be repaired takes the
            // pins that named it with it. Done after the folders are in the section, since the new key is
            // the folder's path and it does not have one until then.
            foreach (var (storedPathKey, folder) in repaired)
            {
                section.RepathPins(storedPathKey, section.PathKey(folder));
                repairedNames.Add(folder.Name);
            }

            return section;
        }

        /// <summary>
        /// Reads one folder and its subtree. <paramref name="storedParentPath"/> is the path its parents
        /// were written under - the names as the file has them, which is what any stored pin used - and
        /// <paramref name="repaired"/> collects the folders whose names this build had to change.
        /// </summary>
        private static CategoryFolder? ReadFolder(
            CategoryFolderDto dto,
            string? storedParentPath,
            List<(string StoredPathKey, CategoryFolder Folder)> repaired)
        {
            // A nameless folder cannot be shown or addressed, so it is dropped rather than guessed at.
            var stored = dto.Name?.Trim();
            if (CategoryFolder.Sanitize(stored) is not { } name)
                return null;

            // Absent in every sidecar written before this marker existed - which is exactly the
            // legacy case that must not be auto-renamed from name text alone (see
            // CategoryFolder.IsUnresolvedPlaceholder and CategoryService.RepairPlaceholderNames).
            var folder = new CategoryFolder(name) { IsUnresolvedPlaceholder = dto.Placeholder };
            var storedPath = storedParentPath == null
                ? stored!
                : storedParentPath + CategorySection.PathSeparator + stored;

            if (!string.Equals(name, stored, StringComparison.Ordinal))
                repaired.Add((storedPath, folder));

            foreach (var member in dto.Members ?? new List<string>())
                folder.AddMember(member);

            foreach (var childDto in dto.Children ?? new List<CategoryFolderDto>())
            {
                var child = ReadFolder(childDto, storedPath, repaired);
                if (child != null)
                    folder.AddChild(child);
            }

            return folder;
        }
    }
}
