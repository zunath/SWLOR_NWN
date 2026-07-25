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

        [JsonPropertyName("showAreaLighting")]
        public bool ShowAreaLighting { get; set; }

        [JsonPropertyName("showFog")]
        public bool ShowFog { get; set; }
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
        private WindowPlacement _window = WindowPlacement.Unset;
        private double _palettePreviewSize;
        private string _paletteSelection = string.Empty;
        private bool _paletteShowsStandard;
        private string _moduleContentsTab = string.Empty;
        private bool _showAreaLighting;
        private bool _showFog;
        private bool _suppressSave;

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

        /// <summary>
        /// Loads settings from <see cref="SettingsFilePath"/>, or returns defaults (with
        /// <see cref="ModuleRoot"/> auto-detected) if no settings file exists yet or it fails to
        /// parse. Never throws.
        /// </summary>
        public static ToolsetSettings Load()
        {
            var settings = new ToolsetSettings { _suppressSave = true };

            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var data = JsonSerializer.Deserialize<ToolsetSettingsData>(json);
                    if (data != null)
                    {
                        settings._moduleRoot = data.ModuleRoot ?? string.Empty;
                        settings._nwnInstallOverride = data.NwnInstallOverride ?? string.Empty;
                        settings._recentModules = data.RecentModules ?? new List<string>();
                        settings._window = new WindowPlacement(
                            data.WindowWidth, data.WindowHeight,
                            data.WindowLeft ?? double.NaN, data.WindowTop ?? double.NaN,
                            data.WindowMaximized);
                        settings._palettePreviewSize = data.PalettePreviewSize;
                        settings._paletteSelection = data.PaletteSelection ?? string.Empty;
                        settings._paletteShowsStandard = data.PaletteShowsStandard;
                        settings._moduleContentsTab = data.ModuleContentsTab ?? string.Empty;
                        settings._showAreaLighting = data.ShowAreaLighting;
                        settings._showFog = data.ShowFog;
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

        private void Save()
        {
            if (_suppressSave)
                return;

            try
            {
                Directory.CreateDirectory(SettingsDirectory);

                var data = new ToolsetSettingsData
                {
                    ModuleRoot = _moduleRoot,
                    NwnInstallOverride = _nwnInstallOverride,
                    RecentModules = _recentModules,
                    WindowWidth = _window.Width,
                    WindowHeight = _window.Height,
                    WindowLeft = double.IsNaN(_window.Left) ? null : _window.Left,
                    WindowTop = double.IsNaN(_window.Top) ? null : _window.Top,
                    WindowMaximized = _window.IsMaximized,
                    PalettePreviewSize = _palettePreviewSize,
                    PaletteSelection = _paletteSelection,
                    PaletteShowsStandard = _paletteShowsStandard,
                    ModuleContentsTab = _moduleContentsTab,
                    ShowAreaLighting = _showAreaLighting,
                    ShowFog = _showFog
                };

                var json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
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
