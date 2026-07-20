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
    }

    /// <summary>
    /// Toolset-wide persisted settings: the module root to open, an optional NWN:EE install
    /// override, and a most-recently-used module list. Backed by a JSON file at
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
        private bool _suppressSave;

        public static string SettingsDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SWLOR.Toolset");

        public static string SettingsFilePath => Path.Combine(SettingsDirectory, "settings.json");

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
                    RecentModules = _recentModules
                };

                var json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception)
            {
                // Settings persistence is best-effort - a locked/unwritable settings file should
                // not crash the toolset.
            }
        }
    }
}
