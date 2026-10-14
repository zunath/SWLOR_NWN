namespace SWLOR.CombatSim.Cli
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

        /// <summary>Whether two paths name the same file once resolved, ignoring case where the file system does.</summary>
        public static bool AreSameFile(string first, string second) =>
            string.Equals(
                Path.GetFullPath(first),
                Path.GetFullPath(second),
                OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

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
    }
}
