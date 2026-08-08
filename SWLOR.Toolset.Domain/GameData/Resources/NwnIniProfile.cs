namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>
    /// The module-assigned HAK archives found through an NWN profile, plus names whose archives
    /// were absent. Layers preserve module.ifo order because that order defines resource precedence.
    /// </summary>
    public sealed record HakLayerResolution(
        IReadOnlyList<ResourceIndex.HakLayer> Layers,
        IReadOnlyList<string> MissingHakNames);

    /// <summary>The custom-content aliases declared by the user's Neverwinter Nights nwn.ini.</summary>
    public sealed record NwnIniProfile(
        string IniPath,
        string? HakDirectory,
        string? TlkDirectory,
        string? MovieDirectory)
    {
        public static string DefaultIniPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Neverwinter Nights",
            "nwn.ini");

        public static NwnIniProfile Load(string? iniPath = null)
        {
            iniPath = Path.GetFullPath(iniPath ?? DefaultIniPath);
            if (!File.Exists(iniPath))
                return new NwnIniProfile(iniPath, null, null, null);

            string? hak = null;
            string? tlk = null;
            string? movies = null;
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
                else if (key.Equals("MOVIES", StringComparison.OrdinalIgnoreCase))
                    movies = ResolveAliasPath(iniPath, value);
            }

            return new NwnIniProfile(iniPath, hak, tlk, movies);
        }

        public IReadOnlyList<string> EnumerateHakNames() =>
            EnumerateBaseNames(HakDirectory, ".hak");

        public IReadOnlyList<string> EnumerateTlkNames() =>
            EnumerateBaseNames(TlkDirectory, ".tlk");

        /// <summary>
        /// Movie resrefs available to a module: custom movies from nwn.ini plus the movies bundled
        /// with the detected NWN:EE installation. Both legacy BIK and Enhanced Edition WBM files
        /// are valid starting-movie sources.
        /// </summary>
        public IReadOnlyList<string> EnumerateMovieNames(string? installDirectory = null)
        {
            var directories = new[]
            {
                MovieDirectory,
                installDirectory == null ? null : Path.Combine(installDirectory, "data", "mov"),
                installDirectory == null ? null : Path.Combine(installDirectory, "movies")
            };

            return directories
                .SelectMany(directory => EnumerateBaseNames(directory, ".bik", ".wbm"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public string? FindHakPath(string name) => FindFile(HakDirectory, name, ".hak");

        public string? FindTlkPath(string name) => FindFile(TlkDirectory, name, ".tlk");

        /// <summary>
        /// Resolves a complete module HAK list with one directory enumeration. Calling
        /// <see cref="FindHakPath"/> once per assigned HAK repeatedly walks the same directory;
        /// production modules carry more than one hundred layers, so startup uses this batch path.
        /// </summary>
        public HakLayerResolution ResolveHakLayers(IEnumerable<string> hakNames)
        {
            ArgumentNullException.ThrowIfNull(hakNames);

            var pathsByName = IndexFilesByBaseName(HakDirectory, ".hak");
            var layers = new List<ResourceIndex.HakLayer>();
            var missing = new List<string>();

            foreach (var rawName in hakNames)
            {
                var name = rawName?.Trim() ?? string.Empty;
                if (name.Length == 0)
                    continue;

                if (pathsByName.TryGetValue(name, out var path))
                    layers.Add(new ResourceIndex.HakLayer(name, path));
                else
                    missing.Add(name);
            }

            return new HakLayerResolution(layers, missing);
        }

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

        private static IReadOnlyList<string> EnumerateBaseNames(string? directory, params string[] extensions)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return Array.Empty<string>();

            try
            {
                return Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                    .Where(path => extensions.Contains(
                        Path.GetExtension(path),
                        StringComparer.OrdinalIgnoreCase))
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

        private static IReadOnlyDictionary<string, string> IndexFilesByBaseName(
            string? directory,
            string extension)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return result;

            try
            {
                foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
                {
                    if (!Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var name = Path.GetFileNameWithoutExtension(path);
                    if (!string.IsNullOrWhiteSpace(name))
                        result.TryAdd(name, path);
                }
            }
            catch (Exception)
            {
                // Match the profile's other discovery methods: an unavailable alias is an empty set.
            }

            return result;
        }
    }
}
