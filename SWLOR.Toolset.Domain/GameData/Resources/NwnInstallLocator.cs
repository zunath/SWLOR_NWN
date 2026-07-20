namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>
    /// Locates an NWN:EE installation directory on disk. Never throws - every probe is wrapped so
    /// a locked-down environment (no registry access, no Steam/GOG installed, non-Windows host)
    /// degrades to "not found" rather than surfacing an exception, letting callers fall back to
    /// hak-only operation (see <see cref="ResourceIndex"/>).
    /// </summary>
    public static class NwnInstallLocator
    {
        private const string SteamDefaultInstallPath =
            @"C:\Program Files (x86)\Steam\steamapps\common\Neverwinter Nights";

        private const string SteamRegistryKeyPath = @"SOFTWARE\WOW6432Node\Valve\Steam";
        private const string GogRegistryKeyPath = @"SOFTWARE\WOW6432Node\GOG.com\Games";

        /// <summary>
        /// Find the NWN:EE install directory. Check order: explicit override path, the Steam
        /// default path, the Steam registry InstallPath (steamapps\common\Neverwinter Nights
        /// under it), then a GOG registry scan for an NWN entry. Returns null if nothing
        /// validates - a valid install must have a "data" subdirectory.
        /// </summary>
        public static string? Locate(string? overridePath = null)
        {
            if (!string.IsNullOrWhiteSpace(overridePath) && IsValidInstall(overridePath))
                return overridePath;

            if (IsValidInstall(SteamDefaultInstallPath))
                return SteamDefaultInstallPath;

            var steamPath = TryGetSteamInstallPath();
            if (steamPath != null && IsValidInstall(steamPath))
                return steamPath;

            var gogPath = TryGetGogInstallPath();
            if (gogPath != null && IsValidInstall(gogPath))
                return gogPath;

            return null;
        }

        private static bool IsValidInstall(string path)
        {
            try
            {
                return Directory.Exists(Path.Combine(path, "data"));
            }
            catch (Exception)
            {
                // Malformed override paths, unreadable mount points, etc. - just not a valid install.
                return false;
            }
        }

        private static string? TryGetSteamInstallPath()
        {
            if (!OperatingSystem.IsWindows())
                return null;

            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(SteamRegistryKeyPath);
                var installPath = key?.GetValue("InstallPath") as string;
                if (string.IsNullOrWhiteSpace(installPath))
                    return null;

                return Path.Combine(installPath, "steamapps", "common", "Neverwinter Nights");
            }
            catch (Exception)
            {
                // Registry access can fail for many reasons (missing key, permissions, running
                // under a restricted account); any failure just means "no Steam install found".
                return null;
            }
        }

        private static string? TryGetGogInstallPath()
        {
            if (!OperatingSystem.IsWindows())
                return null;

            try
            {
                using var gamesKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(GogRegistryKeyPath);
                if (gamesKey == null)
                    return null;

                foreach (var subKeyName in gamesKey.GetSubKeyNames())
                {
                    var path = TryGetGogGamePathIfNwn(gamesKey, subKeyName);
                    if (path != null)
                        return path;
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static string? TryGetGogGamePathIfNwn(Microsoft.Win32.RegistryKey gamesKey, string subKeyName)
        {
            try
            {
                using var subKey = gamesKey.OpenSubKey(subKeyName);
                var gameName = subKey?.GetValue("gameName") as string;
                var path = subKey?.GetValue("path") as string;

                if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(gameName))
                    return null;

                return gameName.Contains("Neverwinter Nights", StringComparison.OrdinalIgnoreCase)
                    ? path
                    : null;
            }
            catch (Exception)
            {
                // Skip unreadable subkeys and keep scanning the rest of GOG's Games list.
                return null;
            }
        }
    }
}
