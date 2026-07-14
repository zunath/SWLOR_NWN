using System;
using System.IO;
using Newtonsoft.Json;
using SWLOR.ContentBuilder.Models;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>
    /// Loads/saves persistent, user-editable Content Builder app settings (File -> Settings...) as
    /// versioned JSON at %APPDATA%\SWLOR\ContentBuilder\settings.json. Deliberately app-level, not
    /// project-level: kept out of .swproj files (see ProjectFileService) since these are per-machine
    /// paths (NWN user directory, install directory), not per-project generation state.
    ///
    /// Never throws: a missing, unreadable, or malformed settings file silently falls back to
    /// <see cref="CreateDefault"/> rather than crashing the app on startup.
    /// </summary>
    public static class SettingsService
    {
        public const int CurrentVersion = 1;

        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            Formatting = Formatting.Indented
        };

        private static ContentBuilderSettings _current;

        public static string SettingsFilePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SWLOR", "ContentBuilder", "settings.json");

        /// <summary>The in-memory settings instance the rest of the app reads from (KeyBifReader,
        /// MainWindow's build buttons). Loaded from disk on first access; call
        /// <see cref="UpdateCurrent"/> after the Settings dialog saves so subsequent reads see the
        /// new values without restarting the app.</summary>
        public static ContentBuilderSettings Current => _current ??= Load();

        /// <summary>Persists <paramref name="settings"/> to disk and makes it the new
        /// <see cref="Current"/> instance.</summary>
        public static void UpdateCurrent(ContentBuilderSettings settings)
        {
            Save(settings);
            _current = settings;
        }

        /// <summary>Loads settings from disk, falling back to <see cref="CreateDefault"/> when the
        /// file is missing, unreadable, or not valid JSON for this shape.</summary>
        public static ContentBuilderSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return CreateDefault();

                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonConvert.DeserializeObject<ContentBuilderSettings>(json, SerializerSettings);
                return settings ?? CreateDefault();
            }
            catch
            {
                return CreateDefault();
            }
        }

        public static void Save(ContentBuilderSettings settings)
        {
            settings.Version = CurrentVersion;

            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonConvert.SerializeObject(settings, SerializerSettings);
            File.WriteAllText(SettingsFilePath, json);
        }

        /// <summary>
        /// Auto-detects the NWN:EE default user directory (Documents\Neverwinter Nights); leaves the
        /// game install directory blank so KeyBifReader's existing Steam/GOG/env-var probes remain
        /// the fallback until the user configures it explicitly.
        /// </summary>
        public static ContentBuilderSettings CreateDefault()
        {
            string defaultUserDirectory;
            try
            {
                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var candidate = string.IsNullOrEmpty(documents) ? null : Path.Combine(documents, "Neverwinter Nights");
                defaultUserDirectory = candidate != null && Directory.Exists(candidate) ? candidate : string.Empty;
            }
            catch
            {
                defaultUserDirectory = string.Empty;
            }

            return new ContentBuilderSettings
            {
                Version = CurrentVersion,
                NwnUserDirectory = defaultUserDirectory,
                NwnGameInstallDirectory = string.Empty
            };
        }
    }
}
