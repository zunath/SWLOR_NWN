namespace SWLOR.CraftSim.Cli
{
    /// <summary>
    /// Parses a command's "--name value" options and "--flag" switches. Anything the command does not take
    /// is rejected, so a typo fails instead of quietly changing the run.
    /// </summary>
    public static class CommandLineOptions
    {
        public static Dictionary<string, List<string>> Parse(
            IReadOnlyList<string> args,
            IEnumerable<string> valueOptions,
            IEnumerable<string> flags = null,
            IEnumerable<string> repeatableOptions = null)
        {
            var values = new HashSet<string>(valueOptions, StringComparer.OrdinalIgnoreCase);
            var switches = new HashSet<string>(flags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var repeatable = new HashSet<string>(repeatableOptions ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var options = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < args.Count; i++)
            {
                var name = args[i];
                if (!name.StartsWith("--"))
                    throw new ArgumentException($"Unexpected argument '{name}'.");

                if (!values.Contains(name) && !switches.Contains(name))
                    throw new ArgumentException(
                        $"Unknown option '{name}'. This command takes: {string.Join(", ", values.Concat(switches).OrderBy(o => o))}.");

                if (options.TryGetValue(name, out var given) && !repeatable.Contains(name))
                    throw new ArgumentException($"{name} can only be given once.");

                if (given == null)
                    options[name] = given = new List<string>();

                if (switches.Contains(name))
                    continue;

                if (i + 1 >= args.Count || args[i + 1].StartsWith("--"))
                    throw new ArgumentException($"{name} needs a value.");

                given.Add(args[++i]);
            }

            return options;
        }

        /// <summary>
        /// Whether two paths name the same file once symbolic links and junctions anywhere in them are followed, ignoring
        /// case where the file system does. A hard link needs no check: outputs are moved into place (<see cref="OutputFile"/>).
        /// </summary>
        public static bool AreSameFile(string first, string second) =>
            string.Equals(
                ResolveLinks(first),
                ResolveLinks(second),
                OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        private static string ResolveLinks(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath) ?? string.Empty;
            var resolved = root;
            foreach (var part in fullPath[root.Length..].Split(
                         new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
            {
                resolved = Path.Combine(resolved, part);
                FileSystemInfo entry = Directory.Exists(resolved) ? new DirectoryInfo(resolved) : new FileInfo(resolved);
                try
                {
                    if (entry.LinkTarget != null && entry.ResolveLinkTarget(returnFinalTarget: true) is { } target)
                        resolved = Path.GetFullPath(target.FullName);
                }
                catch (IOException)
                {
                    // A link that cannot be followed is compared by its own name.
                }
            }

            return resolved;
        }

        /// <summary>
        /// Why an output would overwrite one of the inputs or another output, or null when every output is its own file.
        /// </summary>
        public static string FindOutputCollision(
            IReadOnlyList<(string Option, string Path)> outputs, IReadOnlyList<(string Name, string Path)> inputs)
        {
            for (var index = 0; index < outputs.Count; index++)
            {
                var output = outputs[index];
                foreach (var input in inputs)
                {
                    if (AreSameFile(output.Path, input.Path))
                        return $"{output.Option} would overwrite the {input.Name} {input.Path}.";
                }

                for (var other = index + 1; other < outputs.Count; other++)
                {
                    if (AreSameFile(output.Path, outputs[other].Path))
                        return $"{output.Option} and {outputs[other].Option} name the same file, so one would overwrite the other.";
                }
            }

            return null;
        }

        /// <summary>A whole-number option from <see cref="Parse"/>'s result, or the default when it was not given.</summary>
        public static int GetInt(
            IReadOnlyDictionary<string, List<string>> options, string name, int defaultValue, int minimum, int maximum = int.MaxValue)
        {
            if (!options.TryGetValue(name, out var values))
                return defaultValue;

            if (values.Count != 1 ||
                !int.TryParse(values[0], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value) ||
                value < minimum || value > maximum)
            {
                throw new ArgumentException(minimum == int.MinValue
                    ? $"{name} needs a whole number."
                    : maximum == int.MaxValue
                        ? $"{name} needs a whole number of at least {minimum}."
                        : $"{name} needs a whole number from {minimum} to {maximum}.");
            }

            return value;
        }

        /// <summary>A number option above zero from <see cref="Parse"/>'s result, or the default when it was not given.</summary>
        public static double GetPositiveNumber(IReadOnlyDictionary<string, List<string>> options, string name, double defaultValue)
        {
            if (!options.TryGetValue(name, out var values))
                return defaultValue;

            if (values.Count != 1 ||
                !double.TryParse(values[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                throw new ArgumentException($"{name} needs a number above zero.");
            }

            return value;
        }
    }
}
