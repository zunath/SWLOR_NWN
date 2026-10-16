using SWLOR.CombatSim.Rules;

namespace SWLOR.CombatSim.Cli
{
    /// <summary>
    /// The rule sets a run compares: presets named by --rules (comma-separated, repeatable) and files from
    /// --rules-file; every preset when neither is given.
    /// </summary>
    public static class RuleSetSelection
    {
        public static List<RuleSet> Resolve(IReadOnlyDictionary<string, List<string>> options)
        {
            var names = options.TryGetValue("--rules", out var ruleNames)
                ? ruleNames.SelectMany(n => n.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                : options.ContainsKey("--rules-file") ? Enumerable.Empty<string>() : RuleSets.Names;
            var ruleSets = names.Select(RuleSets.Get).ToList();
            if (options.TryGetValue("--rules-file", out var files))
                ruleSets.AddRange(files.Select(RuleSets.Load));

            // An empty list would run nothing and still print a normal-looking report.
            if (ruleSets.Count == 0)
                throw new ArgumentException(
                    $"--rules names no rule sets; give at least one of {string.Join(", ", RuleSets.Names)} or a --rules-file.");

            return ruleSets;
        }
    }
}
