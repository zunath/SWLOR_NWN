namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>
    /// Describes the stale-bytecode warning that must be resolved before packing a module.
    /// </summary>
    public sealed record ScriptPackStalenessWarning(
        string Headline,
        string Message,
        string ConfirmLabel,
        IReadOnlyList<string> OutputLines);

    /// <summary>
    /// Turns staleness scan results into the pack-time decision text shared by the shell and tests.
    /// </summary>
    public static class ScriptPackReadiness
    {
        public static ScriptPackStalenessWarning? Evaluate(IReadOnlyList<StaleScript> stale)
        {
            if (stale.Count == 0)
                return null;

            var count = stale.Count;
            var noun = count == 1 ? "script has" : "scripts have";
            var lines = stale.Select(s => s.Describe()).ToList();

            return new ScriptPackStalenessWarning(
                $"{count} stale compiled {Pluralize(count, "script")} would ship",
                $"The packer copies Module/nss and Module/ncs as-is, and {count} compiled {noun} stale bytecode. Build all scripts now, then pack only if the stale list clears.",
                "Build then Pack",
                lines);
        }

        private static string Pluralize(int count, string singular) =>
            count == 1 ? singular : singular + "s";
    }
}
