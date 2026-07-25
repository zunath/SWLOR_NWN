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
                warning = $"Could not read categories from '{path}': {ex.Message}. Starting with no categories.";
                return catalog;
            }

            if (document == null)
                return catalog;

            // A sidecar from a newer, incompatible Toolset must not be read as v1 and then rewritten as
            // v1 - that silently discards whatever this build does not understand. Left read-only instead.
            if (document.Version > CurrentVersion)
            {
                warning =
                    $"'{path}' was written by a newer Toolset (version {document.Version}; this build " +
                    $"understands {CurrentVersion}). Categories are shown as loaded but will not be saved.";
                catalog.IsReadOnly = true;
                return catalog;
            }

            if (document.Sections == null)
                return catalog;

            foreach (var (key, sectionDto) in document.Sections)
            {
                if (!ResourceTypeExtensions.TryFromExtension(key, out var type) || sectionDto == null)
                    continue;

                catalog._sections[type] = ReadSection(sectionDto);
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
                    "This category sidecar was written by a newer Toolset and will not be overwritten.");
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
            Members = folder.Members.Count == 0 ? null : folder.Members.ToList()
        };

        private static CategorySection ReadSection(CategorySectionDto dto)
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

            foreach (var folderDto in dto.Folders ?? new List<CategoryFolderDto>())
            {
                var folder = ReadFolder(folderDto);
                if (folder != null)
                    section.AddFolder(folder);
            }

            return section;
        }

        private static CategoryFolder? ReadFolder(CategoryFolderDto dto)
        {
            // A nameless folder cannot be shown or addressed, so it is dropped rather than guessed at.
            if (string.IsNullOrWhiteSpace(dto.Name))
                return null;

            var folder = new CategoryFolder(dto.Name);

            foreach (var member in dto.Members ?? new List<string>())
                folder.AddMember(member);

            foreach (var childDto in dto.Children ?? new List<CategoryFolderDto>())
            {
                var child = ReadFolder(childDto);
                if (child != null)
                    folder.AddChild(child);
            }

            return folder;
        }
    }
}
