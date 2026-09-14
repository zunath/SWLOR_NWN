namespace SWLOR.Toolset.Domain.Script
{
    public sealed record ScriptIncludeRebuildPlan(string IncludeResRef, IReadOnlyList<string> Dependents);

    /// <summary>
    /// Determines which scripts are affected by saving an include.
    /// </summary>
    public static class ScriptIncludeRebuildPlanner
    {
        public static ScriptIncludeRebuildPlan Create(string nssDirectory, string includeResRef)
        {
            if (!Directory.Exists(nssDirectory))
                return new ScriptIncludeRebuildPlan(includeResRef, Array.Empty<string>());

            var graph = ScriptIncludeGraph.Build(nssDirectory);
            var dependents = graph.TransitiveDependents(includeResRef)
                .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ScriptIncludeRebuildPlan(includeResRef, dependents);
        }
    }
}
