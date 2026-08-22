using System.Diagnostics;

namespace SWLOR.Toolset.Domain.Validation
{
    /// <summary>How long one rule took to run during a <see cref="ModuleValidator.Run"/> pass.</summary>
    public sealed record RuleTiming(string RuleId, TimeSpan Elapsed);

    /// <summary>The aggregated result of one <see cref="ModuleValidator.Run"/> pass: every issue
    /// found across all rules, plus per-rule timing.</summary>
    public sealed record ValidationResult(IReadOnlyList<ValidationIssue> Issues, IReadOnlyList<RuleTiming> Timings)
    {
        public int ErrorCount => Issues.Count(issue => issue.Severity == ValidationSeverity.Error);

        public int WarningCount => Issues.Count(issue => issue.Severity == ValidationSeverity.Warning);

        public TimeSpan TotalElapsed => Timings.Aggregate(TimeSpan.Zero, (total, timing) => total + timing.Elapsed);
    }

    /// <summary>
    /// Runs every convention rule over a module workspace and aggregates the results. Rules run
    /// sequentially against one shared <see cref="ValidationContext"/> so file parses are cached
    /// across rules; a rule that throws is caught and downgraded to a Warning issue rather than
    /// aborting the run, so a single bug in one rule cannot hide the rest. Intended to be run on a
    /// background thread - see <see cref="RunAsync"/>.
    /// </summary>
    public sealed class ModuleValidator
    {
        private readonly IReadOnlyList<IValidationRule> _rules;

        public ModuleValidator(IReadOnlyList<IValidationRule>? rules = null)
        {
            _rules = rules ?? DefaultRules();
        }

        /// <summary>The full set of rules this package ships, in a stable, deterministic order.</summary>
        public static IReadOnlyList<IValidationRule> DefaultRules() => new IValidationRule[]
        {
            // First: the floor beneath every convention below. If a resource will not parse, the rules
            // that only read the files they care about cannot report it, and a module with a file broken
            // by an external edit or a bad merge validated clean.
            new GffParseRule(),
            new ResRefLengthRule(),
            new DanglingInstanceTemplateRule(),
            new VarTableEnumRule(),
            new QuestActivatorNotInPaletteRule(),
            new SpawnWaypointPaletteRule(),
            new SingletonWaypointDestinationRule(),
            new PaletteOrphanRule(),
            new DanglingConversationRule(),
            new UnreferencedConversationRule()
        };

        /// <summary>Runs every rule synchronously on the calling thread. Prefer <see cref="RunAsync"/> from UI code.</summary>
        public ValidationResult Run(ValidationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var issues = new List<ValidationIssue>();
            var timings = new List<RuleTiming>();
            var stopwatch = new Stopwatch();

            foreach (var rule in _rules)
            {
                stopwatch.Restart();
                try
                {
                    issues.AddRange(rule.Validate(context));
                }
                catch (Exception ex)
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        rule.RuleId,
                        $"Rule '{rule.RuleId}' failed unexpectedly: {ex.Message}",
                        null,
                        null));
                }
                finally
                {
                    stopwatch.Stop();
                    timings.Add(new RuleTiming(rule.RuleId, stopwatch.Elapsed));
                }
            }

            return new ValidationResult(issues, timings);
        }

        /// <summary>Runs <see cref="Run"/> on a background thread pool thread.</summary>
        public Task<ValidationResult> RunAsync(ValidationContext context) => Task.Run(() => Run(context));
    }
}
