namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>The custom-content aliases declared by the user's Neverwinter Nights nwn.ini.</summary>
    public sealed record NwnIniProfile(string IniPath, string? HakDirectory, string? TlkDirectory)
    {
        public static string DefaultIniPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Neverwinter Nights",
            "nwn.ini");

        public static NwnIniProfile Load(string? iniPath = null)
        {
            iniPath = Path.GetFullPath(iniPath ?? DefaultIniPath);
            if (!File.Exists(iniPath))
                return new NwnIniProfile(iniPath, null, null);

            string? hak = null;
            string? tlk = null;
            var inAliasSection = false;
            foreach (var rawLine in File.ReadLines(iniPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
                    continue;

                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    inAliasSection = line[1..^1].Trim()
                        .Equals("Alias", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inAliasSection)
                    continue;

                var separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim().Trim('"');
                if (key.Equals("HAK", StringComparison.OrdinalIgnoreCase))
                    hak = ResolveAliasPath(iniPath, value);
                else if (key.Equals("TLK", StringComparison.OrdinalIgnoreCase))
                    tlk = ResolveAliasPath(iniPath, value);
            }

            return new NwnIniProfile(iniPath, hak, tlk);
        }

        public IReadOnlyList<string> EnumerateHakNames() =>
            EnumerateBaseNames(HakDirectory, ".hak");

        public IReadOnlyList<string> EnumerateTlkNames() =>
            EnumerateBaseNames(TlkDirectory, ".tlk");

        public string? FindHakPath(string name) => FindFile(HakDirectory, name, ".hak");

        public string? FindTlkPath(string name) => FindFile(TlkDirectory, name, ".tlk");

        private static string? ResolveAliasPath(string iniPath, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            try
            {
                return Path.GetFullPath(Path.IsPathRooted(value)
                    ? value
                    : Path.Combine(Path.GetDirectoryName(iniPath)!, value));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static IReadOnlyList<string> EnumerateBaseNames(string? directory, string extension)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return Array.Empty<string>();

            try
            {
                return Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                    .Where(path => Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase))
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
        }

        private static string? FindFile(string? directory, string name, string extension)
        {
            if (string.IsNullOrWhiteSpace(directory) ||
                string.IsNullOrWhiteSpace(name) ||
                !Directory.Exists(directory))
            {
                return null;
            }

            try
            {
                return Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(path =>
                        Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase) &&
                        Path.GetFileNameWithoutExtension(path).Equals(name, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
