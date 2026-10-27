using System.Text;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>The difference between the edges that exist now and the checked-in baseline.</summary>
internal sealed class BaselineComparison
{
    private const int MaximumExampleMembers = 3;

    private BaselineComparison(IReadOnlyList<UnitEdge> newEdges, IReadOnlyList<string> removedEdges)
    {
        NewEdges = newEdges;
        RemovedEdges = removedEdges;
    }

    /// <summary>Edges that exist now but are not in the baseline: a new cross-domain dependency.</summary>
    public IReadOnlyList<UnitEdge> NewEdges { get; }

    /// <summary>Baseline edges that no longer exist: the baseline must shrink with the code.</summary>
    public IReadOnlyList<string> RemovedEdges { get; }

    public bool IsClean => NewEdges.Count == 0 && RemovedEdges.Count == 0;

    public static BaselineComparison Compare(IReadOnlySet<string> baseline, IEnumerable<UnitEdge> actual)
    {
        var current = actual.ToList();
        var currentKeys = current.Select(edge => edge.Key).ToHashSet(StringComparer.Ordinal);

        var added = current
            .Where(edge => !baseline.Contains(edge.Key))
            .OrderBy(edge => edge.DomainPair, StringComparer.Ordinal)
            .ThenBy(edge => edge.Key, StringComparer.Ordinal)
            .ToList();
        var removed = baseline
            .Where(key => !currentKeys.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();
        return new BaselineComparison(added, removed);
    }

    /// <summary>A failure message that says what changed and what to do about it.</summary>
    public string Describe(string baselineName, string regenerateCommand)
    {
        var text = new StringBuilder();

        if (NewEdges.Count > 0)
        {
            text.AppendLine($"{NewEdges.Count} new cross-domain dependency(ies) not in {baselineName}:");
            foreach (var edge in NewEdges)
            {
                var examples = string.Join(", ", edge.Members.OrderBy(m => m, StringComparer.Ordinal).Take(MaximumExampleMembers));
                text.AppendLine($"  + {edge.Key}   [{edge.DomainPair}; {edge.MemberCount} member(s), for example {examples}]");
            }

            text.AppendLine("A unit may not start depending on a unit of another domain. Route the call through the domain that");
            text.AppendLine("owns it, or move the code into the right domain. If the dependency is intended and approved, add the");
            text.AppendLine($"edge to {baselineName} by regenerating it (below) and explain it in the pull request.");
        }

        if (RemovedEdges.Count > 0)
        {
            text.AppendLine($"{RemovedEdges.Count} dependency(ies) in {baselineName} no longer exist (the baseline is kept tight):");
            foreach (var key in RemovedEdges)
                text.AppendLine($"  - {key}");
            text.AppendLine("Good news: regenerate the baseline so the dependency cannot come back unnoticed (below).");
        }

        text.AppendLine("Regenerate and review the diff of the baseline files:");
        text.AppendLine($"  {regenerateCommand}");
        return text.ToString();
    }
}
