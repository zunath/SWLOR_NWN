using System.Text.Json;
using System.Text.Json.Serialization;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Settings
{
    /// <summary>
    /// The on-disk shape of <see cref="ToolsetSettings"/>, persisted as JSON.
    /// </summary>
    internal sealed class ToolsetSettingsData
    {
        [JsonPropertyName("moduleRoot")]
        public string ModuleRoot { get; set; } = string.Empty;

        [JsonPropertyName("nwnInstallOverride")]
        public string NwnInstallOverride { get; set; } = string.Empty;

        [JsonPropertyName("recentModules")]
        public List<string> RecentModules { get; set; } = new();

        [JsonPropertyName("recentErfArchives")]
        public List<string> RecentErfArchives { get; set; } = new();

        [JsonPropertyName("windowWidth")]
        public double WindowWidth { get; set; }

        [JsonPropertyName("windowHeight")]
        public double WindowHeight { get; set; }

        // Nullable, not NaN: System.Text.Json refuses to serialize NaN by default, and because Save()
        // swallows its exceptions that turned every settings write into a silent no-op. NaN stays the
        // in-memory "no position" marker; on the wire, absent means absent.
        [JsonPropertyName("windowLeft")]
        public double? WindowLeft { get; set; }

        [JsonPropertyName("windowTop")]
        public double? WindowTop { get; set; }

        [JsonPropertyName("windowMaximized")]
        public bool WindowMaximized { get; set; }

        [JsonPropertyName("palettePreviewSize")]
        public double PalettePreviewSize { get; set; }

        [JsonPropertyName("paletteSelection")]
        public string PaletteSelection { get; set; } = string.Empty;

        [JsonPropertyName("paletteShowsStandard")]
        public bool PaletteShowsStandard { get; set; }

        [JsonPropertyName("moduleContentsTab")]
        public string ModuleContentsTab { get; set; } = string.Empty;

        [JsonPropertyName("tilePaintMode")]
        public string TilePaintMode { get; set; } = string.Empty;

        [JsonPropertyName("showAreaLighting")]
        public bool ShowAreaLighting { get; set; }

        [JsonPropertyName("showFog")]
        public bool ShowFog { get; set; }

        [JsonPropertyName("showCeilings")]
        public bool ShowCeilings { get; set; }

        // Defaults true, unlike the other display switches: material maps are what the game
        // renders, so a settings file from before the switch existed keeps them on.
        [JsonPropertyName("showMaterialMaps")]
        public bool ShowMaterialMaps { get; set; } = true;

        [JsonPropertyName("dockProportions")]
        public Dictionary<string, double> DockProportions { get; set; } = new();

        [JsonPropertyName("paletteCategoryProportion")]
        public double PaletteCategoryProportion { get; set; }
    }

    /// <summary>
    /// Toolset-wide persisted settings: the module root to open, an optional NWN:EE install
    /// override, a most-recently-used module list, and the window and panel state a builder set
    /// last time. Backed by a JSON file at
    /// <c>%LOCALAPPDATA%\SWLOR.Toolset\settings.json</c>. Loaded once at startup
    /// (<see cref="Load"/>); every property setter here saves the file immediately, so callers
    /// never need to remember to persist changes themselves.
    /// </summary>
    public sealed class ToolsetSettings
    {
        private const int MaxRecentModules = 10;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private string _moduleRoot = string.Empty;
        private string _nwnInstallOverride = string.Empty;
        private List<string> _recentModules = new();
        private List<string> _recentErfArchives = new();
        private WindowPlacement _window = WindowPlacement.Unset;
        private double _palettePreviewSize;
        private string _paletteSelection = string.Empty;
        private bool _paletteShowsStandard;
        private string _moduleContentsTab = string.Empty;
        private string _tilePaintMode = string.Empty;
        private bool _showAreaLighting;
        private bool _showFog;
        private bool _showCeilings;
        private bool _showMaterialMaps = true;
        private Dictionary<string, double> _dockProportions = new(StringComparer.Ordinal);
        private double _paletteCategoryProportion;
        private bool _suppressSave;

        /// <summary>The file this instance reads and writes - the default one unless a caller named another.</summary>
        private readonly string _filePath;

        private ToolsetSettings(string filePath)
        {
            _filePath = filePath;
        }

        public static string SettingsDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SWLOR.Toolset");

        public static string SettingsFilePath => Path.Combine(SettingsDirectory, "settings.json");

        /// <summary>Why the last save failed, or null when the last one succeeded.</summary>
        public string? LastSaveError { get; private set; }

        /// <summary>
        /// The module root directory to open at startup. Empty when none has been chosen and
        /// auto-detection (walking up from the executable's directory looking for a "Module"
        /// folder) also failed.
        /// </summary>
        public string ModuleRoot
        {
            get => _moduleRoot;
            set
            {
                if (_moduleRoot == value)
                    return;

                _moduleRoot = value ?? string.Empty;
                Save();
            }
        }

        /// <summary>Optional explicit NWN:EE install path, overriding auto-detection.</summary>
        public string NwnInstallOverride
        {
            get => _nwnInstallOverride;
            set
            {
                if (_nwnInstallOverride == value)
                    return;

                _nwnInstallOverride = value ?? string.Empty;
                Save();
            }
        }

        /// <summary>
        /// Where and how big the main window was left. <see cref="WindowPlacement.Unset"/> until a
        /// window has reported one, which is how a first run gets the default size instead of a 0x0
        /// window.
        /// </summary>
        public WindowPlacement Window
        {
            get => _window;
            set
            {
                if (_window.Equals(value))
                    return;

                _window = value;
                Save();
            }
        }

        /// <summary>
        /// Palette preview tile width in pixels, or 0 for "use the panel's own default".
        /// </summary>
        public double PalettePreviewSize
        {
            get => _palettePreviewSize;
            set
            {
                if (Math.Abs(_palettePreviewSize - value) < 0.5)
                    return;

                _palettePreviewSize = value;
                Save();
            }
        }

        /// <summary>The value <see cref="PaletteSelection"/> carries when the palette was on Tiles.</summary>
        public const string TilesSelection = "tiles";

        /// <summary>
        /// What the palette was last showing: a resource extension, <see cref="TilesSelection"/>, or empty
        /// for "nothing saved".
        /// </summary>
        /// <remarks>
        /// A string with three states rather than a nullable type, because Tiles is a real remembered
        /// choice and "no setting yet" is not, and collapsing the two made a fresh install open on Tiles.
        /// Stored by extension rather than enum name, matching the category sidecar, so reordering the enum
        /// cannot silently change what a saved setting means.
        /// </remarks>
        public string PaletteSelection
        {
            get => _paletteSelection;
            set
            {
                var normalized = value ?? string.Empty;
                if (_paletteSelection == normalized)
                    return;

                _paletteSelection = normalized;
                Save();
            }
        }

        /// <summary>True when the palette was last showing base-game content rather than the module's.</summary>
        public bool PaletteShowsStandard
        {
            get => _paletteShowsStandard;
            set
            {
                if (_paletteShowsStandard == value)
                    return;

                _paletteShowsStandard = value;
                Save();
            }
        }

        /// <summary>Whether the Tiles palette picks the tile for the builder ("Auto") or is told which one ("Manual"). Empty until chosen.</summary>
        public string TilePaintMode
        {
            get => _tilePaintMode;
            set
            {
                if (string.Equals(_tilePaintMode, value, StringComparison.Ordinal))
                    return;

                _tilePaintMode = value ?? string.Empty;
                Save();
            }
        }

        /// <summary>Whether the viewport lights areas with their own sun/moon colours.</summary>
        public bool ShowAreaLighting
        {
            get => _showAreaLighting;
            set
            {
                if (_showAreaLighting == value)
                    return;

                _showAreaLighting = value;
                Save();
            }
        }

        /// <summary>Whether the viewport applies the area's distance fog.</summary>
        public bool ShowFog
        {
            get => _showFog;
            set
            {
                if (_showFog == value)
                    return;

                _showFog = value;
                Save();
            }
        }

        /// <summary>Whether the viewport draws the tileset's overhead geometry (ceilings, canopy).</summary>
        public bool ShowCeilings
        {
            get => _showCeilings;
            set
            {
                if (_showCeilings == value)
                    return;

                _showCeilings = value;
                Save();
            }
        }

        /// <summary>Whether the viewport renders normal/specular/roughness material maps.</summary>
        public bool ShowMaterialMaps
        {
            get => _showMaterialMaps;
            set
            {
                if (_showMaterialMaps == value)
                    return;

                _showMaterialMaps = value;
                Save();
            }
        }

        /// <summary>
        /// Which Module Contents tab was open, as a resource extension; empty when none was saved.
        /// </summary>
        public string ModuleContentsTab
        {
            get => _moduleContentsTab;
            set
            {
                var normalized = value ?? string.Empty;
                if (_moduleContentsTab == normalized)
                    return;

                _moduleContentsTab = normalized;
                Save();
            }
        }

        /// <summary>
        /// Where the builder last left each dock divider, keyed by the dock's Id - the fraction of its
        /// parent that dock occupies. Empty until a layout has been recorded, which is how a first run
        /// gets the designed layout instead of an empty one.
        /// </summary>
        /// <remarks>
        /// Stored per-Id rather than as a serialized layout tree: the panels are DI-resolved singletons
        /// wired to the rest of the app, so re-hydrating a whole layout from disk would replace them with
        /// fresh instances nothing else is talking to. Ids that are no longer in the layout are simply
        /// never looked up, so this survives the layout being rearranged in code.
        /// </remarks>
        public IReadOnlyDictionary<string, double> DockProportions => _dockProportions;

        /// <summary>
        /// Records where the dock dividers are now, replacing what was there, and saves. A no-op when
        /// nothing actually moved, so the shell can call this on every layout change without rewriting
        /// the file each time.
        /// </summary>
        public void SetDockProportions(IReadOnlyDictionary<string, double> proportions)
        {
            if (proportions == null)
                return;

            var sanitized = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var (id, proportion) in proportions)
            {
                // Anything outside (0,1) is not a divider position - and a NaN would take the whole
                // settings file down with it, because System.Text.Json refuses to write one.
                if (!string.IsNullOrEmpty(id) && proportion > 0 && proportion < 1)
                    sanitized[id] = proportion;
            }

            if (SameProportions(_dockProportions, sanitized))
                return;

            _dockProportions = sanitized;
            Save();
        }

        private static bool SameProportions(
            Dictionary<string, double> left, Dictionary<string, double> right)
        {
            if (left.Count != right.Count)
                return false;

            foreach (var (id, proportion) in left)
            {
                // A divider drag lands on sub-pixel values; a difference this small is not a move a
                // builder made, and writing the file for one would mean a write per mouse event.
                if (!right.TryGetValue(id, out var other) || Math.Abs(proportion - other) > 0.001)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Share of the Palette panel's flexible height the category tree keeps, or 0 when the builder
        /// has not moved that divider yet.
        /// </summary>
        public double PaletteCategoryProportion
        {
            get => _paletteCategoryProportion;
            set
            {
                var normalized = value > 0 && value < 1 ? value : 0;
                if (Math.Abs(_paletteCategoryProportion - normalized) < 0.001)
                    return;

                _paletteCategoryProportion = normalized;
                Save();
            }
        }

        /// <summary>Most-recently-opened module roots, most recent first.</summary>
        public IReadOnlyList<string> RecentModules => _recentModules;

        /// <summary>Records <paramref name="moduleRoot"/> as the most recent module (moving it to the front if already present) and saves.</summary>
        public void AddRecentModule(string moduleRoot)
        {
            if (string.IsNullOrWhiteSpace(moduleRoot))
                return;

            _recentModules.RemoveAll(path => string.Equals(path, moduleRoot, StringComparison.OrdinalIgnoreCase));
            _recentModules.Insert(0, moduleRoot);

            if (_recentModules.Count > MaxRecentModules)
                _recentModules.RemoveRange(MaxRecentModules, _recentModules.Count - MaxRecentModules);

            Save();
        }

        /// <summary>Most-recently-opened ERF archives, most recent first.</summary>
        public IReadOnlyList<string> RecentErfArchives => _recentErfArchives;

        /// <summary>
        /// Records an ERF source after it has been scanned successfully. A cancelled or invalid file
        /// never enters the recent list, so every item remains a useful one-click choice.
        /// </summary>
        public void AddRecentErfArchive(string archivePath)
        {
            if (string.IsNullOrWhiteSpace(archivePath))
                return;

            _recentErfArchives.RemoveAll(path =>
                string.Equals(path, archivePath, StringComparison.OrdinalIgnoreCase));
            _recentErfArchives.Insert(0, archivePath);

            if (_recentErfArchives.Count > MaxRecentModules)
            {
                _recentErfArchives.RemoveRange(
                    MaxRecentModules,
                    _recentErfArchives.Count - MaxRecentModules);
            }

            Save();
        }

        /// <summary>
        /// Loads settings from <see cref="SettingsFilePath"/>, or returns defaults (with
        /// <see cref="ModuleRoot"/> auto-detected) if no settings file exists yet or it fails to
        /// parse. Never throws.
        /// </summary>
        public static ToolsetSettings Load() => Load(SettingsFilePath);

        /// <summary>
        /// <see cref="Load()"/>, against a named file rather than the per-user one. Everything this
        /// instance saves afterwards goes back to the same file.
        /// </summary>
        public static ToolsetSettings Load(string filePath)
        {
            var settings = new ToolsetSettings(filePath) { _suppressSave = true };

            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var data = JsonSerializer.Deserialize<ToolsetSettingsData>(json);
                    if (data != null)
                    {
                        settings._moduleRoot = data.ModuleRoot ?? string.Empty;
                        settings._nwnInstallOverride = data.NwnInstallOverride ?? string.Empty;
                        settings._recentModules = data.RecentModules ?? new List<string>();
                        settings._recentErfArchives = data.RecentErfArchives ?? new List<string>();
                        settings._window = new WindowPlacement(
                            data.WindowWidth, data.WindowHeight,
                            data.WindowLeft ?? double.NaN, data.WindowTop ?? double.NaN,
                            data.WindowMaximized);
                        settings._palettePreviewSize = data.PalettePreviewSize;
                        settings._paletteSelection = data.PaletteSelection ?? string.Empty;
                        settings._paletteShowsStandard = data.PaletteShowsStandard;
                        settings._moduleContentsTab = data.ModuleContentsTab ?? string.Empty;
                        settings._tilePaintMode = data.TilePaintMode ?? string.Empty;
                        settings._showAreaLighting = data.ShowAreaLighting;
                        settings._showFog = data.ShowFog;
                        settings._showCeilings = data.ShowCeilings;
                        settings._showMaterialMaps = data.ShowMaterialMaps;
                        settings._paletteCategoryProportion =
                            data.PaletteCategoryProportion > 0 && data.PaletteCategoryProportion < 1
                                ? data.PaletteCategoryProportion
                                : 0;

                        if (data.DockProportions != null)
                        {
                            foreach (var (id, proportion) in data.DockProportions)
                            {
                                if (!string.IsNullOrEmpty(id) && proportion > 0 && proportion < 1)
                                    settings._dockProportions[id] = proportion;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Corrupt or unreadable settings file: fall through to defaults below.
            }

            if (string.IsNullOrWhiteSpace(settings._moduleRoot))
            {
                var detected = AutoDetectModuleRoot();
                if (detected != null)
                    settings._moduleRoot = detected;
            }

            settings._suppressSave = false;
            return settings;
        }

        /// <summary>
        /// Walks up from the executable's directory looking for a "Module" folder that looks like
        /// a real module root (has "are" and "utc" subfolders) - the repo layout this toolset is
        /// normally run from. Returns null if none is found.
        /// </summary>
        public static string? AutoDetectModuleRoot()
        {
            try
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var candidate = Path.Combine(current.FullName, "Module");
                    if (ModuleWorkspace.LooksLikeModuleRoot(candidate))
                        return candidate;

                    current = current.Parent;
                }
            }
            catch (Exception)
            {
                // Any I/O failure while probing just means auto-detection found nothing.
            }

            return null;
        }

        /// <summary>The value if it is a real number, or null - the JSON writer rejects NaN and infinity.</summary>
        private static double? Finite(double value) => double.IsFinite(value) ? value : null;

        private void Save()
        {
            if (_suppressSave)
                return;

            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var data = new ToolsetSettingsData
                {
                    ModuleRoot = _moduleRoot,
                    NwnInstallOverride = _nwnInstallOverride,
                    RecentModules = _recentModules,
                    RecentErfArchives = _recentErfArchives,
                    // Every double on the way out is filtered: System.Text.Json throws on NaN and
                    // infinity, Save() swallows that, and one bad number would take every other setting
                    // in the file down with it silently.
                    WindowWidth = Finite(_window.Width) ?? 0,
                    WindowHeight = Finite(_window.Height) ?? 0,
                    WindowLeft = Finite(_window.Left),
                    WindowTop = Finite(_window.Top),
                    WindowMaximized = _window.IsMaximized,
                    PalettePreviewSize = Finite(_palettePreviewSize) ?? 0,
                    PaletteSelection = _paletteSelection,
                    PaletteShowsStandard = _paletteShowsStandard,
                    ModuleContentsTab = _moduleContentsTab,
                    TilePaintMode = _tilePaintMode,
                    ShowAreaLighting = _showAreaLighting,
                    ShowFog = _showFog,
                    ShowCeilings = _showCeilings,
                    ShowMaterialMaps = _showMaterialMaps,
                    DockProportions = new Dictionary<string, double>(_dockProportions, StringComparer.Ordinal),
                    PaletteCategoryProportion = Finite(_paletteCategoryProportion) ?? 0
                };

                var json = JsonSerializer.Serialize(data, JsonOptions);
                var temporaryPath = _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllText(temporaryPath, json);
                    File.Move(temporaryPath, _filePath, overwrite: true);
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
                LastSaveError = null;
            }
            catch (Exception ex)
            {
                // Settings persistence is best-effort - a locked/unwritable settings file should not
                // crash the toolset. Recorded rather than dropped: a silent failure here looks exactly
                // like "settings are not implemented", which is how a NaN serialization bug went unseen.
                LastSaveError = ex.Message;
            }
        }
    }
}
