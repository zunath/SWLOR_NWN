namespace SWLOR.ContentBuilder.Models
{
    /// <summary>
    /// Persistent, per-machine Content Builder settings (File -> Settings...): where the user's NWN
    /// install lives. Stored separately from .swproj project files (see ProjectFileService) since
    /// these are machine-local paths, not per-project generation state -- see SettingsService for
    /// load/save/default-fallback behavior.
    /// </summary>
    public sealed class ContentBuilderSettings
    {
        public int Version { get; set; }

        /// <summary>Directory containing nwn.ini (NWN:EE default: Documents\Neverwinter Nights).</summary>
        public string NwnUserDirectory { get; set; } = string.Empty;

        /// <summary>Directory containing data\nwn_base.key (the game install, e.g. the Steam/GOG
        /// folder). Empty means "not configured" -- KeyBifReader falls back to its existing
        /// Steam/GOG/env-var probes.</summary>
        public string NwnGameInstallDirectory { get; set; } = string.Empty;
    }
}
