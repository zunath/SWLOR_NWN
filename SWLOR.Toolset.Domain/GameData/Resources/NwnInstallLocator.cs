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

        /// <summary>Where the game sits beneath a Steam library root.</summary>
        private static readonly string[] SteamLibrarySuffix = { "steamapps", "common", "Neverwinter Nights" };

        /// <summary>
        /// Fixed paths the non-Steam distributions use. Beamdog's own client and GOG both install to a
        /// predictable place and neither writes a registry key this locator can key off the way Steam
        /// does, so probing the paths is the only route to them.
        /// </summary>
        private static readonly string[] WellKnownInstallPaths =
        {
            @"C:\Program Files (x86)\GOG Galaxy\Games\Neverwinter Nights Enhanced Edition",
            @"C:\Program Files\GOG Galaxy\Games\Neverwinter Nights Enhanced Edition",
            @"C:\GOG Games\Neverwinter Nights Enhanced Edition",
            @"C:\Program Files (x86)\Beamdog Library\00785",
            @"C:\Program Files\Beamdog Library\00785"
        };

        /// <summary>
        /// Find the NWN:EE install directory, or null when none validates - a valid install must have a
        /// "data" subdirectory.
        /// </summary>
        /// <remarks>
        /// Check order: the explicit override, Steam's default path, the Steam registry InstallPath, every
        /// other library the Steam client lists, the GOG registry, then the fixed GOG and Beamdog paths.
        /// The override comes first so a builder can always win, and the extra Steam libraries matter
        /// because installing games to a second drive is the normal case rather than the exotic one.
        /// </remarks>
        public static string? Locate(string? overridePath = null)
        {
            foreach (var candidate in Candidates(overridePath))
            {
                if (candidate != null && IsValidInstall(candidate))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Everywhere <see cref="Locate"/> looks, in order - so a "no install found" message can say what
        /// was checked rather than only that nothing turned up.
        /// </summary>
        public static IReadOnlyList<string> ProbedPaths(string? overridePath = null) =>
            Candidates(overridePath).Where(path => path != null).Select(path => path!).ToList();

        private static IEnumerable<string?> Candidates(string? overridePath)
        {
            if (!string.IsNullOrWhiteSpace(overridePath))
                yield return overridePath;

            yield return SteamDefaultInstallPath;
            yield return TryGetSteamInstallPath();

            foreach (var library in TryGetSteamLibraryPaths())
                yield return library;

            yield return TryGetGogInstallPath();

            foreach (var path in WellKnownInstallPaths)
                yield return path;
        }

        /// <summary>
        /// The game under each library root Steam's libraryfolders.vdf lists. Scanned for quoted "path"
        /// values rather than parsed as real VDF: the file's only job here is to hand over directory
        /// names, and every one is validated afterwards anyway.
        /// </summary>
        private static IEnumerable<string> TryGetSteamLibraryPaths()
        {
            var steamRoot = TryGetSteamRoot();
            if (steamRoot == null)
                yield break;

            string[] lines;
            try
            {
                var manifest = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(manifest))
                    yield break;

                lines = File.ReadAllLines(manifest);
            }
            catch (Exception)
            {
                // An unreadable or locked manifest just means no extra libraries were found.
                yield break;
            }

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith(PathKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = trimmed.Split('\"', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    continue;

                // VDF escapes its separators, so "D:\\Games" means D:\Games.
                var root = parts[^1].Replace(DoubleSeparator, Separator).Trim();
                if (root.Length == 0)
                    continue;

                string combined;
                try
                {
                    combined = Path.Combine(new[] { root }.Concat(SteamLibrarySuffix).ToArray());
                }
                catch (Exception)
                {
                    // A malformed path in the manifest is skipped rather than fatal.
                    continue;
                }

                yield return combined;
            }
        }

        /// <summary>The quoted key a library root sits behind in libraryfolders.vdf.</summary>
        private static readonly string PathKey = "\"path\"";

        private const string Separator = "\\";
        private const string DoubleSeparator = "\\\\";

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
            var root = TryGetSteamRoot();
            return root == null
                ? null
                : Path.Combine(new[] { root }.Concat(SteamLibrarySuffix).ToArray());
        }

        /// <summary>Steam's own install directory from the registry, or null.</summary>
        private static string? TryGetSteamRoot()
        {
            if (!OperatingSystem.IsWindows())
                return null;

            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(SteamRegistryKeyPath);
                var installPath = key?.GetValue("InstallPath") as string;
                return string.IsNullOrWhiteSpace(installPath) ? null : installPath;
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
