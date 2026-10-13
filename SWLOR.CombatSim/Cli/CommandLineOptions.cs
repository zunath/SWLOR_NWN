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
    }
}
